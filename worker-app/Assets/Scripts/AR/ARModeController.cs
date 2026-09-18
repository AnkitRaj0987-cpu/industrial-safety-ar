// ARModeController.cs
// Namespace : IndustrialSafetyAR.AR
//
// Authoritative single controller responsible for AR subsystem and physical camera lifecycle:
// - Keeps AR Session, AR Camera Manager, AR Camera Background, Plane Detection, and Tracking OFF by default.
// - Provides explicit EnableAR(), DisableAR(), and IsARActive properties.
// - Handles Android application pause/resume without leaking sessions or duplicating AR instances.
// - Handles camera permission verification gracefully.

using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace IndustrialSafetyAR.AR
{
    public class ARModeController : MonoBehaviour
    {
        private static ARModeController s_Instance;
        public static ARModeController Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindAnyObjectByType<ARModeController>(FindObjectsInactive.Include);
                }
                return s_Instance;
            }
            set => s_Instance = value;
        }

        [Header("AR Foundation Components")]
        [SerializeField] private ARSession _arSession;
        [SerializeField] private ARCameraManager _arCameraManager;
        [SerializeField] private ARCameraBackground _arCameraBackground;
        [SerializeField] private ARPlaneManager _arPlaneManager;
        [SerializeField] private ARRaycastManager _arRaycastManager;
        [SerializeField] private ArSessionFacade _sessionFacade;
        [SerializeField] private Camera _arCamera;
        [SerializeField] private GameObject _trackingStatusCanvas;

        private bool _isARActive;
        private bool _wasActiveBeforePause;

        /// <summary>
        /// True if AR mode is currently active and camera streaming is enabled.
        /// False when on Home, Settings, Certificates, Profile, or before AR is explicitly enabled.
        /// </summary>
        public bool IsARActive => _isARActive;

        /// <summary>
        /// Direct access to the AR Session component.
        /// </summary>
        public ARSession Session => _arSession;

        /// <summary>
        /// Direct access to the AR Camera Manager component.
        /// </summary>
        public ARCameraManager CameraManager => _arCameraManager;

        /// <summary>
        /// Direct access to the AR Camera Background component.
        /// </summary>
        public ARCameraBackground CameraBackground => _arCameraBackground;

        /// <summary>
        /// Direct access to the AR Plane Manager component.
        /// </summary>
        public ARPlaneManager PlaneManager => _arPlaneManager;

        /// <summary>
        /// Direct access to the AR Raycast Manager component.
        /// </summary>
        public ARRaycastManager RaycastManager => _arRaycastManager;

        public event Action OnAREnabled;
        public event Action OnARDisabled;
        public event Action<bool> OnARStateChanged;

        public bool HasCameraPermission
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return Permission.HasUserAuthorizedPermission(Permission.Camera);
#else
                return true;
#endif
            }
        }

        private void Awake()
        {
            if (Instance == null || !Instance)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                    return;
                }
                else
                {
                    Instance = this;
                }
            }

            DiscoverComponents();

            // Default State: AR must be OFF on application launch
            DisableAR();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Auto-discovers AR Foundation components in the scene if not explicitly assigned.
        /// </summary>
        public void DiscoverComponents()
        {
            if (_arSession == null)
            {
                _arSession = FindAnyObjectByType<ARSession>(FindObjectsInactive.Include);
            }

            if (_arCameraManager == null)
            {
                _arCameraManager = FindAnyObjectByType<ARCameraManager>(FindObjectsInactive.Include);
            }

            if (_arCameraBackground == null)
            {
                _arCameraBackground = FindAnyObjectByType<ARCameraBackground>(FindObjectsInactive.Include);
            }

            if (_arPlaneManager == null)
            {
                _arPlaneManager = FindAnyObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
            }

            if (_arRaycastManager == null)
            {
                _arRaycastManager = FindAnyObjectByType<ARRaycastManager>(FindObjectsInactive.Include);
            }

            if (_sessionFacade == null)
            {
                _sessionFacade = FindAnyObjectByType<ArSessionFacade>(FindObjectsInactive.Include);
            }

            if (_arCamera == null)
            {
                _arCamera = Camera.main;
            }

            if (_trackingStatusCanvas == null)
            {
                var canvas = GameObject.Find("TrackingStatusCanvas");
                if (canvas != null)
                {
                    _trackingStatusCanvas = canvas;
                }
            }
        }

        /// <summary>
        /// Authoritatively enables AR: activates physical camera streaming, ARCore tracking session,
        /// camera background rendering, and plane detection.
        /// </summary>
        public void EnableAR(Action onReady = null)
        {
            DiscoverComponents();

            // Verify or request permission on Android
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Debug.Log("[ARModeController] Requesting Camera permission before enabling AR.");
                Permission.RequestUserPermission(Permission.Camera);
            }
#endif

            _isARActive = true;

            // 1. Enable AR Session
            if (_arSession != null)
            {
                _arSession.enabled = true;
            }

            // 2. Enable Camera subsystem & frame streaming
            if (_arCameraManager != null)
            {
                _arCameraManager.enabled = true;
            }

            // 3. Enable URP Camera Background rendering
            if (_arCameraBackground != null)
            {
                _arCameraBackground.enabled = true;
            }

            // 4. Enable Plane Detection
            if (_arPlaneManager != null)
            {
                _arPlaneManager.enabled = true;
                SetTrackablesVisibility(true);
            }

            // 5. Enable Raycasting
            if (_arRaycastManager != null)
            {
                _arRaycastManager.enabled = true;
            }

            // 6. Show Tracking Status Canvas
            if (_trackingStatusCanvas != null)
            {
                _trackingStatusCanvas.SetActive(true);
            }

            Debug.Log("[ARModeController] AR enabled — physical camera, ARSession, and plane tracking active.");

            OnAREnabled?.Invoke();
            OnARStateChanged?.Invoke(true);
            onReady?.Invoke();
        }

        /// <summary>
        /// Authoritatively disables AR: stops physical camera hardware capture, stops AR session,
        /// disables camera background pass, and halts plane detection.
        /// </summary>
        public void DisableAR()
        {
            DiscoverComponents();

            _isARActive = false;

            // 1. Disable Plane Detection and hide active planes
            if (_arPlaneManager != null)
            {
                SetTrackablesVisibility(false);
                _arPlaneManager.enabled = false;
            }

            // 2. Disable Raycasting
            if (_arRaycastManager != null)
            {
                _arRaycastManager.enabled = false;
            }

            // 3. Disable Camera Background rendering (camera clears to solid background)
            if (_arCameraBackground != null)
            {
                _arCameraBackground.enabled = false;
            }

            // 4. Disable Camera subsystem (stops physical camera sensor)
            if (_arCameraManager != null)
            {
                _arCameraManager.enabled = false;
            }

            // 5. Disable AR Session (stops AR tracking engine)
            if (_arSession != null)
            {
                _arSession.enabled = false;
            }

            // 6. Hide Tracking Status Canvas
            if (_trackingStatusCanvas != null)
            {
                _trackingStatusCanvas.SetActive(false);
            }

            Debug.Log("[ARModeController] AR disabled — physical camera OFF, ARSession paused, tracking OFF.");

            OnARDisabled?.Invoke();
            OnARStateChanged?.Invoke(false);
        }

        private void SetTrackablesVisibility(bool visible)
        {
            if (_arPlaneManager == null) return;

            foreach (var plane in _arPlaneManager.trackables)
            {
                if (plane != null && plane.gameObject != null)
                {
                    plane.gameObject.SetActive(visible);
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                if (_isARActive)
                {
                    _wasActiveBeforePause = true;
                    if (_arCameraManager != null) _arCameraManager.enabled = false;
                    if (_arSession != null) _arSession.enabled = false;
                    Debug.Log("[ARModeController] Application paused while AR active — paused AR subsystems.");
                }
            }
            else
            {
                if (_wasActiveBeforePause)
                {
                    _wasActiveBeforePause = false;
                    if (IsTrainingOrArScreenActive())
                    {
                        EnableAR();
                        Debug.Log("[ARModeController] Application resumed — restored AR subsystems.");
                    }
                    else
                    {
                        DisableAR();
                        Debug.Log("[ARModeController] Application resumed on non-AR screen — kept AR OFF.");
                    }
                }
                else
                {
                    if (!IsTrainingOrArScreenActive())
                    {
                        DisableAR();
                    }
                }
            }
        }

        private bool IsTrainingOrArScreenActive()
        {
            var homeCtrl = IndustrialSafetyAR.UI.WorkerHomeController.Instance;
            if (homeCtrl == null) return false;

            return homeCtrl.CurrentState == IndustrialSafetyAR.UI.WorkerHomeController.WorkerAppScreenState.TrainingFire ||
                   homeCtrl.CurrentState == IndustrialSafetyAR.UI.WorkerHomeController.WorkerAppScreenState.TrainingGas ||
                   (homeCtrl.CurrentState == IndustrialSafetyAR.UI.WorkerHomeController.WorkerAppScreenState.AR && _isARActive);
        }

        /// <summary>
        /// Automatically bootstraps ARModeController in any scene containing ArSessionFacade or ARSession.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static GameObject AutoBootstrap()
        {
            if (FindAnyObjectByType<ARModeController>(FindObjectsInactive.Include) == null)
            {
                var session = FindAnyObjectByType<ARSession>(FindObjectsInactive.Include);
                if (session != null)
                {
                    var ctrl = session.gameObject.AddComponent<ARModeController>();
                    ctrl.DiscoverComponents();
                    ctrl.DisableAR();
                    Debug.Log("[ARModeController] Auto-attached ARModeController to AR Session.");
                    return session.gameObject;
                }

                var go = new GameObject("ARModeController");
                var newCtrl = go.AddComponent<ARModeController>();
                newCtrl.DiscoverComponents();
                newCtrl.DisableAR();
                Debug.Log("[ARModeController] Auto-created ARModeController instance.");
                return go;
            }
            return null;
        }
    }
}
