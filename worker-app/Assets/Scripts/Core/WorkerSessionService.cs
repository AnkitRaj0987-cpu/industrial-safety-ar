// WorkerSessionService.cs
// Namespace : IndustrialSafetyAR.Core
//
// Manages worker login session, online HTTP authentication, offline PIN verification,
// and session state for SIH 2026 PS 26041.

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace IndustrialSafetyAR.Core
{
    public sealed class WorkerSessionService
    {
        private static WorkerSessionService s_Instance;
        public static WorkerSessionService Instance => s_Instance ??= new WorkerSessionService();

        public const string DefaultWorkerId = "00000000-dead-beef-0001-000000000001";
        public const string DefaultWorkerCode = "DEMO-001";
        public const string DefaultWorkerName = "Operator Ramesh Kumar";
        public const string DefaultDivision = "Mining & Material Handling";

        public string WorkerId { get; private set; } = DefaultWorkerId;
        public string WorkerCode { get; private set; } = DefaultWorkerCode;
        public string DisplayName { get; private set; } = DefaultWorkerName;
        public string Division { get; private set; } = DefaultDivision;
        public string Locale { get; private set; } = "en";
        public string SessionToken { get; private set; } = "wtoken_demo_session";
        public bool IsLoggedIn { get; private set; } = true;

        public string BackendBaseUrl { get; set; } = "http://127.0.0.1:3000";

        public event Action OnSessionStateChanged;

        public WorkerSessionService()
        {
            InitializeFromStorage();
        }

        public void InitializeFromStorage()
        {
            var session = LocalStorageService.Instance.GetCurrentSession();
            if (session != null && !string.IsNullOrEmpty(session.sessionToken))
            {
                WorkerId = session.workerId ?? DefaultWorkerId;
                WorkerCode = session.workerCode ?? DefaultWorkerCode;
                DisplayName = session.displayName ?? DefaultWorkerName;
                Locale = session.locale ?? "en";
                Division = session.division ?? DefaultDivision;
                SessionToken = session.sessionToken;
                IsLoggedIn = true;
            }
            else if (session != null && !string.IsNullOrEmpty(session.workerCode))
            {
                // Cached profile exists, but user logged out
                WorkerId = session.workerId ?? DefaultWorkerId;
                WorkerCode = session.workerCode ?? DefaultWorkerCode;
                DisplayName = session.displayName ?? DefaultWorkerName;
                Division = session.division ?? DefaultDivision;
                SessionToken = null;
                IsLoggedIn = false;
            }
            else
            {
                // Fresh startup: initialize demo credentials
                WorkerId = DefaultWorkerId;
                WorkerCode = DefaultWorkerCode;
                DisplayName = DefaultWorkerName;
                Division = DefaultDivision;
                SessionToken = "wtoken_demo_session";
                IsLoggedIn = true;

                LocalStorageService.Instance.SaveSession(
                    WorkerId,
                    WorkerCode,
                    DisplayName,
                    Locale,
                    Division,
                    SessionToken,
                    "1234"
                );
            }
        }

        public bool LoginOffline(string workerCode, string pin, out string errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrEmpty(workerCode) || string.IsNullOrEmpty(pin))
            {
                errorMessage = "Please enter Worker ID and PIN.";
                return false;
            }

            bool isValidPin = LocalStorageService.Instance.VerifyOfflinePin(pin);
            if (!isValidPin)
            {
                errorMessage = "Incorrect PIN. (Default offline demo PIN: 1234)";
                return false;
            }

            WorkerCode = workerCode.Trim();
            SessionToken = $"wtoken_offline_{Guid.NewGuid():N}";
            IsLoggedIn = true;

            LocalStorageService.Instance.SaveSession(
                WorkerId,
                WorkerCode,
                DisplayName,
                Locale,
                Division,
                SessionToken,
                pin
            );

            OnSessionStateChanged?.Invoke();
            return true;
        }

        [Serializable]
        private class LoginRequestBody
        {
            public string worker_code;
            public string pin;
        }

        [Serializable]
        private class WorkerDataDto
        {
            public string id;
            public string worker_code;
            public string display_name;
            public string locale;
            public string site_label;
            public string division;
        }

        [Serializable]
        private class LoginResponseBody
        {
            public string token;
            public WorkerDataDto worker;
            public string error;
            public string message;
        }

        public IEnumerator RoutineLoginOnline(
            string workerCode,
            string pin,
            Action<bool, string> onComplete)
        {
            if (string.IsNullOrEmpty(workerCode) || string.IsNullOrEmpty(pin))
            {
                onComplete?.Invoke(false, "Worker ID and PIN are required.");
                yield break;
            }

            string url = $"{BackendBaseUrl}/v1/auth/worker/login";
            var payload = new LoginRequestBody
            {
                worker_code = workerCode.Trim(),
                pin = pin.Trim()
            };

            string json = JsonUtility.ToJson(payload);
            using (var req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 5; // 5 second timeout for mobile network check

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success && req.responseCode == 200)
                {
                    try
                    {
                        var resp = JsonUtility.FromJson<LoginResponseBody>(req.downloadHandler.text);
                        if (resp != null && !string.IsNullOrEmpty(resp.token) && resp.worker != null)
                        {
                            WorkerId = resp.worker.id;
                            WorkerCode = resp.worker.worker_code;
                            DisplayName = resp.worker.display_name;
                            Locale = resp.worker.locale ?? "en";
                            Division = resp.worker.division ?? resp.worker.site_label ?? DefaultDivision;
                            SessionToken = resp.token;
                            IsLoggedIn = true;

                            // Cache credentials for subsequent offline logins
                            LocalStorageService.Instance.SaveSession(
                                WorkerId,
                                WorkerCode,
                                DisplayName,
                                Locale,
                                Division,
                                SessionToken,
                                pin
                            );

                            OnSessionStateChanged?.Invoke();
                            onComplete?.Invoke(true, "Login successful.");
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[WorkerSessionService] Failed to parse login response: {ex.Message}");
                    }
                }

                // If backend responded with 401
                if (req.responseCode == 401)
                {
                    onComplete?.Invoke(false, "Invalid Worker ID or PIN.");
                    yield break;
                }

                // If network is offline or server unreachable, attempt automatic offline login fallback
                if (req.result == UnityWebRequest.Result.ConnectionError ||
                    req.result == UnityWebRequest.Result.ProtocolError ||
                    req.timeout > 0)
                {
                    bool offlineOk = LoginOffline(workerCode, pin, out string offlineErr);
                    if (offlineOk)
                    {
                        onComplete?.Invoke(true, "Offline login successful (cached credentials).");
                    }
                    else
                    {
                        onComplete?.Invoke(false, $"Server unreachable and offline login failed: {offlineErr}");
                    }
                    yield break;
                }

                onComplete?.Invoke(false, $"Authentication failed (HTTP {req.responseCode}).");
            }
        }

        public void Logout()
        {
            SessionToken = null;
            IsLoggedIn = false;
            LocalStorageService.Instance.ClearSession();
            OnSessionStateChanged?.Invoke();
        }
    }
}
