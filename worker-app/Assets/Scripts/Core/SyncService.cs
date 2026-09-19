// SyncService.cs
// Namespace : IndustrialSafetyAR.Core
//
// Manages offline-first outbox synchronization for SIH 2026 PS 26041.
// Transmits offline completed attempts to POST /v1/sync and updates local
// training records and certificates upon confirmation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using IndustrialSafetyAR.Assessment;
using UnityEngine;
using UnityEngine.Networking;

namespace IndustrialSafetyAR.Core
{
    public sealed class SyncService
    {
        private static SyncService s_Instance;
        public static SyncService Instance => s_Instance ??= new SyncService();

        public bool IsSyncing { get; private set; } = false;
        public string LastSyncResult { get; private set; } = "Ready";
        public string LastSyncTime { get; private set; } = null;

        public event Action<bool, string> OnSyncFinished;

        [Serializable]
        private class SyncWorkerDto
        {
            public string worker_id;
            public string client_device_id;
        }

        [Serializable]
        private class AcceptedDto
        {
            public string client_attempt_id;
            public string server_attempt_id;
            public float server_score;
            public bool server_passed;
        }

        [Serializable]
        private class DuplicateDto
        {
            public string client_attempt_id;
            public string server_attempt_id;
        }

        [Serializable]
        private class CertificateDto
        {
            public string id;
            public string public_id;
            public string attempt_id;
            public string worker_id;
            public string module_id;
            public float score;
            public string issued_at;
            public string verification_url;
        }

        [Serializable]
        private class SyncResponseDto
        {
            public string schema_version;
            public string server_time;
            public List<AcceptedDto> accepted;
            public List<DuplicateDto> duplicates;
            public List<CertificateDto> certificates;
            public string error;
            public string message;
        }

        public IEnumerator RoutineSyncNow(Action<bool, string> onComplete)
        {
            if (IsSyncing)
            {
                onComplete?.Invoke(false, "Sync already in progress.");
                yield break;
            }

            var pendingItems = LocalStorageService.Instance.GetPendingOutbox();
            if (pendingItems == null || pendingItems.Count == 0)
            {
                LastSyncResult = "Up to date (No pending records)";
                LastSyncTime = DateTime.UtcNow.ToString("g");
                onComplete?.Invoke(true, "All records are already synchronized.");
                yield break;
            }

            IsSyncing = true;
            LastSyncResult = "Syncing...";

            // Mark pending records as "syncing"
            foreach (var item in pendingItems)
            {
                LocalStorageService.Instance.UpdateRecordSyncStatus(item.clientAttemptId, "syncing");
            }

            string workerId = WorkerSessionService.Instance.WorkerId;
            string backendUrl = $"{WorkerSessionService.Instance.BackendBaseUrl}/v1/sync";

            // Build attempts JSON array
            var attemptsJsonBuilder = new StringBuilder();
            attemptsJsonBuilder.Append("[");
            for (int i = 0; i < pendingItems.Count; i++)
            {
                if (i > 0) attemptsJsonBuilder.Append(",");
                attemptsJsonBuilder.Append(pendingItems[i].attemptPayloadJson);
            }
            attemptsJsonBuilder.Append("]");

            string clientRequestId = Guid.NewGuid().ToString();
            string deviceId = SystemInfo.deviceUniqueIdentifier ?? Guid.NewGuid().ToString();

            string requestPayload =
                $"{{\"schema_version\":\"1.0.0\"," +
                $"\"client_request_id\":\"{clientRequestId}\"," +
                $"\"worker\":{{\"worker_id\":\"{workerId}\",\"client_device_id\":\"{deviceId}\"}}," +
                $"\"attempts\":{attemptsJsonBuilder}}}";

            using (var req = new UnityWebRequest(backendUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestPayload);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 8;

                yield return req.SendWebRequest();

                IsSyncing = false;

                if (req.result == UnityWebRequest.Result.Success && req.responseCode == 200)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<SyncResponseDto>(req.downloadHandler.text);
                        int syncedCount = 0;

                        // Process accepted attempts
                        if (response.accepted != null)
                        {
                            foreach (var acc in response.accepted)
                            {
                                LocalStorageService.Instance.RemoveFromOutbox(acc.client_attempt_id);
                                LocalStorageService.Instance.UpdateRecordSyncStatus(
                                    acc.client_attempt_id,
                                    "synced",
                                    acc.server_attempt_id
                                );
                                syncedCount++;
                            }
                        }

                        // Process duplicates
                        if (response.duplicates != null)
                        {
                            foreach (var dup in response.duplicates)
                            {
                                LocalStorageService.Instance.RemoveFromOutbox(dup.client_attempt_id);
                                LocalStorageService.Instance.UpdateRecordSyncStatus(
                                    dup.client_attempt_id,
                                    "synced",
                                    dup.server_attempt_id
                                );
                                syncedCount++;
                            }
                        }

                        // Process issued certificates
                        if (response.certificates != null)
                        {
                            foreach (var cert in response.certificates)
                            {
                                // Find matching local record
                                var all = LocalStorageService.Instance.GetAllRecords();
                                var match = all.Find(r => r.serverAttemptId == cert.attempt_id || r.clientAttemptId == cert.attempt_id);
                                if (match != null)
                                {
                                    LocalStorageService.Instance.UpdateRecordSyncStatus(
                                        match.clientAttemptId,
                                        "synced",
                                        cert.attempt_id,
                                        cert.public_id,
                                        cert.verification_url
                                    );
                                }
                            }
                        }

                        LastSyncResult = $"Synced successfully ({syncedCount} records).";
                        LastSyncTime = DateTime.UtcNow.ToString("g");
                        OnSyncFinished?.Invoke(true, LastSyncResult);
                        onComplete?.Invoke(true, LastSyncResult);
                        yield break;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[SyncService] Failed to process sync response: {ex.Message}");
                    }
                }

                // If sync failed (e.g. offline, timeout, server error)
                foreach (var item in pendingItems)
                {
                    LocalStorageService.Instance.UpdateRecordSyncStatus(item.clientAttemptId, "saved_on_device");
                }

                LastSyncResult = "Sync Pending (Saved on device)";
                OnSyncFinished?.Invoke(false, LastSyncResult);
                onComplete?.Invoke(false, LastSyncResult);
            }
        }
    }
}
