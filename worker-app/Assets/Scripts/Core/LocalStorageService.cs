// LocalStorageService.cs
// Namespace : IndustrialSafetyAR.Core
//
// Persistent client-side repository for SIH 2026 PS 26041.
// Stores Training Records, Outbox Queue, and Worker Session securely
// on local device storage (Application.persistentDataPath) with zero external dependencies.
// Works 100% offline and survives app restarts.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using IndustrialSafetyAR.Assessment;
using UnityEngine;

namespace IndustrialSafetyAR.Core
{
    [Serializable]
    public class StoredTrainingRecord
    {
        public string clientAttemptId;
        public string workerId;
        public string workerCode;
        public string workerName;
        public string moduleId;
        public string moduleTitle;
        public float score;
        public bool passed;
        public string completedAt;
        public int attemptNumber;
        public string status; // "completed"
        public string certificateStatus; // "none", "pending_sync", "issued"
        public string syncStatus; // "saved_on_device", "syncing", "synced", "sync_failed"
        public string serverAttemptId;
        public string certificatePublicId;
        public string certificateId { get => certificatePublicId; set => certificatePublicId = value; }
        public string verificationUrl;
        public List<string> breakdownItems = new List<string>();
        public string rawAttemptJson;
    }

    [Serializable]
    public class StoredRecordsWrapper
    {
        public List<StoredTrainingRecord> records = new List<StoredTrainingRecord>();
    }

    [Serializable]
    public class StoredOutboxItem
    {
        public string clientAttemptId;
        public string workerId;
        public string createdAt;
        public int retryCount;
        public string attemptPayloadJson;
    }

    [Serializable]
    public class StoredOutboxWrapper
    {
        public List<StoredOutboxItem> items = new List<StoredOutboxItem>();
    }

    [Serializable]
    public class StoredSession
    {
        public string workerId;
        public string workerCode;
        public string displayName;
        public string locale;
        public string division;
        public string sessionToken;
        public string saltHex;
        public string pinHashHex;
        public string lastLoginAt;
    }

    public sealed class LocalStorageService
    {
        private static LocalStorageService s_Instance;
        public static LocalStorageService Instance => s_Instance ??= new LocalStorageService();

        private string _customBasePath = null;

        public string BaseStoragePath
        {
            get
            {
                if (!string.IsNullOrEmpty(_customBasePath)) return _customBasePath;
                try
                {
                    return Application.persistentDataPath;
                }
                catch
                {
                    return AppDomain.CurrentDomain.BaseDirectory;
                }
            }
            set => _customBasePath = value;
        }

        private string RecordsFilePath => Path.Combine(BaseStoragePath, "training_records.json");
        private string OutboxFilePath => Path.Combine(BaseStoragePath, "outbox_queue.json");
        private string SessionFilePath => Path.Combine(BaseStoragePath, "worker_session.json");

        private readonly object _lock = new object();
        private StoredRecordsWrapper _recordsCache;
        private StoredOutboxWrapper _outboxCache;
        private StoredSession _sessionCache;

        public event Action OnRecordsChanged;
        public event Action OnOutboxChanged;

        public LocalStorageService()
        {
            LoadAll();
        }

        /// <summary>
        /// Resets or sets an isolated path for unit testing.
        /// </summary>
        public void ConfigureTestStorage(string testDirectory)
        {
            lock (_lock)
            {
                _customBasePath = testDirectory;
                if (!string.IsNullOrEmpty(_customBasePath) && !Directory.Exists(_customBasePath))
                {
                    Directory.CreateDirectory(_customBasePath);
                }
                _recordsCache = null;
                _outboxCache = null;
                _sessionCache = null;
                LoadAll();
            }
        }

        private void LoadAll()
        {
            lock (_lock)
            {
                // 1. Records
                try
                {
                    if (File.Exists(RecordsFilePath))
                    {
                        string json = File.ReadAllText(RecordsFilePath, Encoding.UTF8);
                        _recordsCache = JsonUtility.FromJson<StoredRecordsWrapper>(json) ?? new StoredRecordsWrapper();
                    }
                    else
                    {
                        _recordsCache = new StoredRecordsWrapper();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LocalStorageService] Could not read records: {ex.Message}");
                    _recordsCache = new StoredRecordsWrapper();
                }

                // 2. Outbox
                try
                {
                    if (File.Exists(OutboxFilePath))
                    {
                        string json = File.ReadAllText(OutboxFilePath, Encoding.UTF8);
                        _outboxCache = JsonUtility.FromJson<StoredOutboxWrapper>(json) ?? new StoredOutboxWrapper();
                    }
                    else
                    {
                        _outboxCache = new StoredOutboxWrapper();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LocalStorageService] Could not read outbox: {ex.Message}");
                    _outboxCache = new StoredOutboxWrapper();
                }

                // 3. Session
                try
                {
                    if (File.Exists(SessionFilePath))
                    {
                        string json = File.ReadAllText(SessionFilePath, Encoding.UTF8);
                        _sessionCache = JsonUtility.FromJson<StoredSession>(json);
                    }
                    else
                    {
                        _sessionCache = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LocalStorageService] Could not read session: {ex.Message}");
                    _sessionCache = null;
                }
            }
        }

        private void SaveFileAtomic(string filePath, string content)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string tempPath = filePath + ".tmp";
            File.WriteAllText(tempPath, content, Encoding.UTF8);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            File.Move(tempPath, filePath);
        }

        // ====================================================================
        // TRAINING RECORDS
        // ====================================================================

        public StoredTrainingRecord SaveAttemptRecord(
            TrainingAttempt attempt,
            string moduleTitle,
            string workerCode,
            string workerName,
            List<string> breakdownItems)
        {
            if (attempt == null) return null;

            lock (_lock)
            {
                if (_recordsCache == null) _recordsCache = new StoredRecordsWrapper();

                int previousAttempts = 0;
                foreach (var rec in _recordsCache.records)
                {
                    if (rec.moduleId == attempt.ModuleId && rec.workerId == attempt.WorkerId)
                    {
                        previousAttempts++;
                    }
                }

                var record = new StoredTrainingRecord
                {
                    clientAttemptId = attempt.ClientAttemptId,
                    workerId = attempt.WorkerId ?? "00000000-dead-beef-0001-000000000001",
                    workerCode = string.IsNullOrEmpty(workerCode) ? "DEMO-001" : workerCode,
                    workerName = string.IsNullOrEmpty(workerName) ? "Operator Ramesh Kumar" : workerName,
                    moduleId = attempt.ModuleId,
                    moduleTitle = moduleTitle,
                    score = attempt.ClientScore,
                    passed = attempt.Passed,
                    completedAt = attempt.CompletedAt ?? DateTime.UtcNow.ToString("o"),
                    attemptNumber = previousAttempts + 1,
                    status = attempt.Status,
                    certificateStatus = attempt.Passed ? "pending_sync" : "none",
                    syncStatus = "saved_on_device",
                    breakdownItems = breakdownItems ?? new List<string>(),
                    rawAttemptJson = JsonUtility.ToJson(attempt)
                };

                // Deduplicate if already present
                int existingIdx = _recordsCache.records.FindIndex(r => r.clientAttemptId == record.clientAttemptId);
                if (existingIdx >= 0)
                {
                    _recordsCache.records[existingIdx] = record;
                }
                else
                {
                    _recordsCache.records.Insert(0, record); // newest first
                }

                // Persist to disk
                string json = JsonUtility.ToJson(_recordsCache, true);
                SaveFileAtomic(RecordsFilePath, json);

                // Enqueue to Outbox
                EnqueueOutbox(record);

                OnRecordsChanged?.Invoke();
                return record;
            }
        }

        public void SaveDirectRecord(StoredTrainingRecord record, bool enqueueOutbox = false)
        {
            if (record == null) return;
            lock (_lock)
            {
                if (_recordsCache == null) _recordsCache = new StoredRecordsWrapper();
                int existingIdx = _recordsCache.records.FindIndex(r => r.clientAttemptId == record.clientAttemptId);
                if (existingIdx >= 0)
                {
                    _recordsCache.records[existingIdx] = record;
                }
                else
                {
                    _recordsCache.records.Insert(0, record);
                }
                string json = JsonUtility.ToJson(_recordsCache, true);
                SaveFileAtomic(RecordsFilePath, json);

                if (enqueueOutbox)
                {
                    EnqueueOutbox(record);
                }

                OnRecordsChanged?.Invoke();
            }
        }

        public List<StoredTrainingRecord> GetRecordsForWorker(string workerId)
        {
            lock (_lock)
            {
                if (_recordsCache == null) return new List<StoredTrainingRecord>();
                if (string.IsNullOrEmpty(workerId)) return new List<StoredTrainingRecord>(_recordsCache.records);

                return _recordsCache.records.FindAll(r => r.workerId == workerId);
            }
        }

        public List<StoredTrainingRecord> GetAllRecords()
        {
            lock (_lock)
            {
                if (_recordsCache == null) return new List<StoredTrainingRecord>();
                return new List<StoredTrainingRecord>(_recordsCache.records);
            }
        }

        public StoredTrainingRecord GetRecord(string clientAttemptId)
        {
            lock (_lock)
            {
                if (_recordsCache == null) return null;
                return _recordsCache.records.Find(r => r.clientAttemptId == clientAttemptId);
            }
        }

        public void UpdateRecordSyncStatus(
            string clientAttemptId,
            string syncStatus,
            string serverAttemptId = null,
            string certPublicId = null,
            string verificationUrl = null)
        {
            lock (_lock)
            {
                if (_recordsCache == null) return;
                var record = _recordsCache.records.Find(r => r.clientAttemptId == clientAttemptId);
                if (record != null)
                {
                    record.syncStatus = syncStatus;
                    if (!string.IsNullOrEmpty(serverAttemptId)) record.serverAttemptId = serverAttemptId;
                    if (!string.IsNullOrEmpty(certPublicId))
                    {
                        record.certificatePublicId = certPublicId;
                        record.certificateStatus = "issued";
                    }
                    if (!string.IsNullOrEmpty(verificationUrl)) record.verificationUrl = verificationUrl;

                    SaveFileAtomic(RecordsFilePath, JsonUtility.ToJson(_recordsCache, true));
                    OnRecordsChanged?.Invoke();
                }
            }
        }

        // ====================================================================
        // OUTBOX
        // ====================================================================

        public void EnqueueOutbox(StoredTrainingRecord record)
        {
            lock (_lock)
            {
                if (_outboxCache == null) _outboxCache = new StoredOutboxWrapper();

                int idx = _outboxCache.items.FindIndex(i => i.clientAttemptId == record.clientAttemptId);
                if (idx < 0)
                {
                    _outboxCache.items.Add(new StoredOutboxItem
                    {
                        clientAttemptId = record.clientAttemptId,
                        workerId = record.workerId,
                        createdAt = DateTime.UtcNow.ToString("o"),
                        retryCount = 0,
                        attemptPayloadJson = record.rawAttemptJson
                    });

                    SaveFileAtomic(OutboxFilePath, JsonUtility.ToJson(_outboxCache, true));
                    OnOutboxChanged?.Invoke();
                }
            }
        }

        public List<StoredOutboxItem> GetPendingOutbox()
        {
            lock (_lock)
            {
                if (_outboxCache == null) return new List<StoredOutboxItem>();
                return new List<StoredOutboxItem>(_outboxCache.items);
            }
        }

        public void RemoveFromOutbox(string clientAttemptId)
        {
            lock (_lock)
            {
                if (_outboxCache == null) return;
                int removed = _outboxCache.items.RemoveAll(i => i.clientAttemptId == clientAttemptId);
                if (removed > 0)
                {
                    SaveFileAtomic(OutboxFilePath, JsonUtility.ToJson(_outboxCache, true));
                    OnOutboxChanged?.Invoke();
                }
            }
        }

        // ====================================================================
        // SESSION & CREDENTIAL CACHE
        // ====================================================================

        public void SaveSession(
            string workerId,
            string workerCode,
            string displayName,
            string locale,
            string division,
            string sessionToken,
            string pinToCache = null)
        {
            lock (_lock)
            {
                string saltHex = null;
                string pinHashHex = null;

                if (!string.IsNullOrEmpty(pinToCache))
                {
                    byte[] salt = new byte[16];
                    using (var rng = new RNGCryptoServiceProvider())
                    {
                        rng.GetBytes(salt);
                    }
                    saltHex = BitConverter.ToString(salt).Replace("-", "").ToLowerInvariant();

                    using (var pbkdf2 = new Rfc2898DeriveBytes(pinToCache.Trim(), salt, 10000))
                    {
                        byte[] hash = pbkdf2.GetBytes(32);
                        pinHashHex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
                else if (_sessionCache != null && !string.IsNullOrEmpty(_sessionCache.pinHashHex))
                {
                    saltHex = _sessionCache.saltHex;
                    pinHashHex = _sessionCache.pinHashHex;
                }

                _sessionCache = new StoredSession
                {
                    workerId = workerId,
                    workerCode = workerCode,
                    displayName = displayName,
                    locale = locale,
                    division = division,
                    sessionToken = sessionToken,
                    saltHex = saltHex,
                    pinHashHex = pinHashHex,
                    lastLoginAt = DateTime.UtcNow.ToString("o")
                };

                SaveFileAtomic(SessionFilePath, JsonUtility.ToJson(_sessionCache, true));
            }
        }

        public StoredSession GetCurrentSession()
        {
            lock (_lock)
            {
                return _sessionCache;
            }
        }

        public void ClearSession()
        {
            lock (_lock)
            {
                if (_sessionCache != null)
                {
                    _sessionCache.sessionToken = null;
                    SaveFileAtomic(SessionFilePath, JsonUtility.ToJson(_sessionCache, true));
                }
            }
        }

        public bool VerifyOfflinePin(string pin)
        {
            lock (_lock)
            {
                if (_sessionCache == null || string.IsNullOrEmpty(_sessionCache.pinHashHex) || string.IsNullOrEmpty(_sessionCache.saltHex))
                {
                    // Fallback for default demo PIN if no custom credentials cached yet
                    return pin?.Trim() == "1234";
                }

                try
                {
                    byte[] salt = HexToBytes(_sessionCache.saltHex);
                    using (var pbkdf2 = new Rfc2898DeriveBytes(pin.Trim(), salt, 10000))
                    {
                        byte[] hash = pbkdf2.GetBytes(32);
                        string computedHex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        return string.Equals(computedHex, _sessionCache.pinHashHex, StringComparison.OrdinalIgnoreCase);
                    }
                }
                catch
                {
                    return pin?.Trim() == "1234";
                }
            }
        }

        private static byte[] HexToBytes(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }
    }
}
