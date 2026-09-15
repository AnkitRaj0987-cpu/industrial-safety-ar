// FireArInteractionController.cs
// Namespace : IndustrialSafetyAR.Modules.FireExplosion
//
// Orchestrates the AR tap interaction:
// 1. Plane raycasting to place the Fire Hazard onto a detected surface.
// 2. Anchoring the placed hazard in the AR world.
// 3. Physics raycasting to detect worker selection/identification of the hazard.
// 4. Emitting the domain TrainingEvent (step_detect_hazard / detect_hazard_acknowledged).

using System;
using IndustrialSafetyAR.AR;
using IndustrialSafetyAR.Core.Events;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IndustrialSafetyAR.Modules.FireExplosion
{
    public enum FireInteractionState
    {
        WaitingForTracking,
        ReadyToPlace,
        HazardPlaced,
        HazardDetected
    }

    /// <summary>
    /// Coordinates AR surface placement and hazard identification for the Fire &amp; Explosion module.
    /// Bridges input, AR raycasting, hazard marker state, and domain training events.
    /// </summary>
    public class FireArInteractionController : MonoBehaviour
    {
        [Header("Dependencies")]
        [Tooltip("Facade exposing AR tracking readiness. Discovered automatically if null.")]
        [SerializeField]
        private ArSessionFacade _arSessionFacade;

        [Tooltip("AR plane raycasting service. Discovered automatically if null.")]
        [SerializeField]
        private ArPlaneRaycastService _raycastService;

        [Tooltip("Main AR Camera used for raycasting. Defaults to Camera.main if null.")]
        [SerializeField]
        private Camera _arCamera;

        [Tooltip("Optional prefab for the fire hazard. If null, generated procedurally.")]
        [SerializeField]
        private FireHazardMarker _hazardPrefab;

        [Header("Module Configuration")]
        [SerializeField]
        private string _moduleId = "fire-explosion-response";

        [SerializeField]
        private string _contentVersion = "1.0.0";

        [SerializeField]
        private string _stepId = "step_detect_hazard";

        [SerializeField]
        private string _actionId = "detect_hazard_acknowledged";

        [SerializeField]
        private string _targetId = "hazard_electrical_conveyor_fire";

        // Runtime state
        private FireInteractionState _state = FireInteractionState.WaitingForTracking;
        private FireHazardMarker _activeHazard;
        private ITrainingEventDispatcher _eventDispatcher;

        public FireInteractionState State => _state;
        public FireHazardMarker ActiveHazard => _activeHazard;

        public event Action<FireInteractionState> OnStateChanged;
        public event Action<FireHazardMarker> OnHazardPlaced;
        public event Action<FireHazardMarker, TrainingEvent> OnHazardDetected;
        public event Action<string> OnFeedbackChanged;

        private void Awake()
        {
            if (_arSessionFacade == null)
            {
                _arSessionFacade = FindAnyObjectByType<ArSessionFacade>();
            }

            if (_raycastService == null)
            {
                _raycastService = GetComponent<ArPlaneRaycastService>() ?? FindAnyObjectByType<ArPlaneRaycastService>();
                if (_raycastService == null)
                {
                    _raycastService = gameObject.AddComponent<ArPlaneRaycastService>();
                }
            }

            if (_arCamera == null)
            {
                _arCamera = Camera.main;
            }

            _eventDispatcher = TrainingEventBus.Instance;
        }

        private void Start()
        {
            UpdateState(FireInteractionState.WaitingForTracking);
        }

        private void Update()
        {
            UpdateTrackingStatus();

            if (TryGetScreenTap(out Vector2 tapPosition))
            {
                HandleTap(tapPosition);
            }
        }

        public void SetEventDispatcher(ITrainingEventDispatcher dispatcher)
        {
            _eventDispatcher = dispatcher ?? TrainingEventBus.Instance;
        }

        private void UpdateTrackingStatus()
        {
            if (_state == FireInteractionState.WaitingForTracking)
            {
                if (_arSessionFacade != null && _arSessionFacade.IsTrackingAvailable)
                {
                    UpdateState(FireInteractionState.ReadyToPlace);
                }
            }
            else if (_state == FireInteractionState.ReadyToPlace)
            {
                if (_arSessionFacade != null && !_arSessionFacade.IsTrackingAvailable)
                {
                    UpdateState(FireInteractionState.WaitingForTracking);
                }
            }
        }

        private void HandleTap(Vector2 screenPosition)
        {
            // Do not process taps over UI elements
            if (IsPointerOverUI(screenPosition))
            {
                return;
            }

            switch (_state)
            {
                case FireInteractionState.ReadyToPlace:
                    TryPlaceHazard(screenPosition);
                    break;

                case FireInteractionState.HazardPlaced:
                    TryIdentifyHazard(screenPosition);
                    break;
            }
        }

        private void TryPlaceHazard(Vector2 screenPosition)
        {
            if (_raycastService == null) return;

            if (_raycastService.TryRaycastPlane(screenPosition, out Pose hitPose))
            {
                if (_hazardPrefab != null)
                {
                    _activeHazard = Instantiate(_hazardPrefab, hitPose.position, hitPose.rotation);
                }
                else
                {
                    GameObject hazardObj = new GameObject("FireHazardMarker");
                    hazardObj.transform.position = hitPose.position;
                    hazardObj.transform.rotation = hitPose.rotation;
                    _activeHazard = hazardObj.AddComponent<FireHazardMarker>();
                }

                // Face the camera on spawn (keeping upright)
                if (_arCamera != null)
                {
                    Vector3 lookDir = _arCamera.transform.position - _activeHazard.transform.position;
                    lookDir.y = 0;
                    if (lookDir != Vector3.zero)
                    {
                        _activeHazard.transform.rotation = Quaternion.LookRotation(lookDir);
                    }
                }

                UpdateState(FireInteractionState.HazardPlaced);
                OnHazardPlaced?.Invoke(_activeHazard);
                Debug.Log($"[FireArInteractionController] Placed fire hazard marker at {hitPose.position}");
            }
        }

        private void TryIdentifyHazard(Vector2 screenPosition)
        {
            if (_arCamera == null)
            {
                _arCamera = Camera.main;
                if (_arCamera == null) return;
            }

            Ray ray = _arCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                var hazard = hit.collider.GetComponentInParent<FireHazardMarker>();
                if (hazard != null && hazard == _activeHazard && !hazard.IsDetected)
                {
                    ConfirmHazardDetection(hazard);
                }
            }
        }

        private void ConfirmHazardDetection(FireHazardMarker hazard)
        {
            hazard.AcknowledgeDetection();
            UpdateState(FireInteractionState.HazardDetected);

            // Construct domain training event matching module and rubric criteria
            var trainingEvent = new TrainingEvent
            {
                ModuleId = _moduleId,
                ContentVersion = _contentVersion,
                StepId = _stepId,
                EventType = "step_completed",
                ActionId = _actionId,
                TargetId = _targetId,
                Outcome = "success",
                Payload =
                {
                    { "rule_id", "rule_detect_hazard" },
                    { "action_id", _actionId },
                    { "target_id", _targetId },
                    { "outcome", "success" }
                }
            };

            // Emit through decoupled interface
            _eventDispatcher?.Dispatch(trainingEvent);

            OnHazardDetected?.Invoke(hazard, trainingEvent);
        }

        private void UpdateState(FireInteractionState newState)
        {
            _state = newState;
            string feedback = GetFeedbackForState(newState);
            OnFeedbackChanged?.Invoke(feedback);
            OnStateChanged?.Invoke(newState);
        }

        private string GetFeedbackForState(FireInteractionState state)
        {
            switch (state)
            {
                case FireInteractionState.WaitingForTracking:
                    return "Searching for surfaces... Move phone slowly.";
                case FireInteractionState.ReadyToPlace:
                    return "Tap on a surface to place the Fire Hazard.";
                case FireInteractionState.HazardPlaced:
                    return "Hazard Located! Tap the hazard to confirm detection.";
                case FireInteractionState.HazardDetected:
                    return "Fire Hazard Detected! Step Complete.";
                default:
                    return string.Empty;
            }
        }

        private bool TryGetScreenTap(out Vector2 position)
        {
            position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    position = touch.position.ReadValue();
                    return true;
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }
#endif

            if (Input.touchCount > 0)
            {
                UnityEngine.Touch t = Input.GetTouch(0);
                if (t.phase == UnityEngine.TouchPhase.Began)
                {
                    position = t.position;
                    return true;
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }

            return false;
        }

        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;

            if (EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
                if (EventSystem.current.IsPointerOverGameObject(touchId))
                {
                    return true;
                }
            }
#endif

            return false;
        }

        /// <summary>
        /// Automatically bootstraps the Fire AR interaction system whenever an AR scene containing
        /// ArSessionFacade is loaded, eliminating manual scene wiring and scene merge conflicts.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapInArScene()
        {
            var facade = FindAnyObjectByType<ArSessionFacade>();
            if (facade != null && FindAnyObjectByType<FireArInteractionController>() == null)
            {
                var go = new GameObject("FireArInteractionManager");
                go.AddComponent<FireArInteractionController>();
                go.AddComponent<IndustrialSafetyAR.UI.FireInteractionFeedbackUI>();
                Debug.Log("[FireArInteractionController] Auto-bootstrapped Fire AR Interaction in AR scene.");
            }
        }
    }
}
