// FireArInteractionController.cs
// Namespace : IndustrialSafetyAR.Modules.FireExplosion
//
// Orchestrates the Fire & Explosion Response AR interactions:
// 1. Plane raycasting to place the Fire Hazard onto a detected surface.
// 2. Anchoring the placed hazard in the AR world.
// 3. Physics raycasting to detect worker selection/identification of the hazard.
// 4. Emitting domain TrainingEvents for:
//    - step_detect_hazard / rule_detect_hazard
//    - step_identify_hazard / rule_identify_hazard
//    - step_raise_alarm / rule_raise_alarm

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
        HazardDetected,
        AwaitingIdentification,
        HazardIdentified,
        AwaitingAlarm,
        AlarmRaised,
        AwaitingExtinguisherSelection,
        ExtinguisherSelected,
        AwaitingSafeDistance,
        step_maintain_distance = AwaitingSafeDistance,
        SafeDistanceMaintained
    }

    /// <summary>
    /// Coordinates AR surface placement, hazard identification, and alarm activation
    /// for the Fire &amp; Explosion module. Bridges input, AR raycasting, hazard marker state,
    /// and domain training events.
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

        // Domain workflow state machine
        private readonly FireTrainingWorkflow _workflow = new FireTrainingWorkflow();

        // Runtime state
        private FireInteractionState _state = FireInteractionState.WaitingForTracking;
        private FireHazardMarker _activeHazard;
        private ITrainingEventDispatcher _eventDispatcher;

        public FireInteractionState State => _state;
        public FireWorkflowStage WorkflowStage => _workflow.CurrentStage;
        public FireTrainingWorkflow Workflow => _workflow;
        public FireHazardMarker ActiveHazard => _activeHazard;
        public string CurrentStepId => _workflow.CurrentStepId;

        public event Action<FireInteractionState> OnStateChanged;
        public event Action<FireHazardMarker> OnHazardPlaced;
        public event Action<FireHazardMarker, TrainingEvent> OnHazardDetected;
        public event Action<TrainingEvent> OnHazardIdentified;
        public event Action<TrainingEvent> OnAlarmRaised;
        public event Action<TrainingEvent> OnExtinguisherSelected;
        public event Action<TrainingEvent> OnSafeDistanceDecided;
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

            _workflow.OnStageChanged += HandleWorkflowStageChanged;
            _workflow.OnFeedbackChanged += HandleWorkflowFeedbackChanged;
        }

        private void OnDestroy()
        {
            _workflow.OnStageChanged -= HandleWorkflowStageChanged;
            _workflow.OnFeedbackChanged -= HandleWorkflowFeedbackChanged;
        }

        private void Start()
        {
            _workflow.SetStage(FireWorkflowStage.WaitingForTracking);
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

        private void HandleWorkflowStageChanged(FireWorkflowStage stage)
        {
            _state = (FireInteractionState)stage;
            OnStateChanged?.Invoke(_state);
        }

        private void HandleWorkflowFeedbackChanged(string feedback)
        {
            OnFeedbackChanged?.Invoke(feedback);
        }

        private void UpdateTrackingStatus()
        {
            if (_workflow.CurrentStage == FireWorkflowStage.WaitingForTracking)
            {
                if (_arSessionFacade != null && _arSessionFacade.IsTrackingAvailable)
                {
                    _workflow.SetStage(FireWorkflowStage.ReadyToPlace);
                }
            }
            else if (_workflow.CurrentStage == FireWorkflowStage.ReadyToPlace)
            {
                if (_arSessionFacade != null && !_arSessionFacade.IsTrackingAvailable)
                {
                    _workflow.SetStage(FireWorkflowStage.WaitingForTracking);
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

            switch (_workflow.CurrentStage)
            {
                case FireWorkflowStage.ReadyToPlace:
                    TryPlaceHazard(screenPosition);
                    break;

                case FireWorkflowStage.HazardPlaced:
                    TryIdentifyHazard(screenPosition);
                    break;

                case FireWorkflowStage.AwaitingSafeDistance:
                    TrySelectSafeDistancePosition(screenPosition);
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

                _workflow.SetStage(FireWorkflowStage.HazardPlaced);
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

            if (_workflow.ConfirmHazardDetected(_eventDispatcher, out var trainingEvent))
            {
                OnHazardDetected?.Invoke(hazard, trainingEvent);
            }
        }

        /// <summary>
        /// Submits the worker's hazard classification selection.
        /// </summary>
        /// <param name="targetId">The selected hazard target ID.</param>
        /// <returns>True if correct classification and advanced; false if invalid.</returns>
        public bool SubmitHazardIdentification(string targetId)
        {
            bool success = _workflow.SubmitHazardIdentification(targetId, _eventDispatcher, out var trainingEvent);
            if (success)
            {
                if (_activeHazard != null)
                {
                    _activeHazard.MarkIdentified(FireTrainingWorkflow.HazardClassElectrical);
                }
                OnHazardIdentified?.Invoke(trainingEvent);
            }
            return success;
        }

        /// <summary>
        /// Submits the emergency alarm activation action.
        /// </summary>
        /// <param name="actionId">The alarm action ID (defaults to manual_call_point_activated).</param>
        /// <returns>True if successfully activated; false if invalid.</returns>
        public bool SubmitRaiseAlarm(string actionId = FireTrainingWorkflow.ActionRaiseAlarm)
        {
            bool success = _workflow.SubmitRaiseAlarm(actionId, _eventDispatcher, out var trainingEvent);
            if (success)
            {
                if (_activeHazard != null)
                {
                    _activeHazard.TriggerAlarmVisual();
                }
                OnAlarmRaised?.Invoke(trainingEvent);
            }
            return success;
        }

        /// <summary>
        /// Submits the worker's fire extinguisher selection.
        /// </summary>
        /// <param name="targetId">The selected extinguisher ID (e.g. extinguisher_co2).</param>
        /// <returns>True if correct extinguisher and advanced; false if invalid.</returns>
        public bool SubmitExtinguisherSelection(string targetId)
        {
            bool success = _workflow.SubmitSelectExtinguisher(targetId, _eventDispatcher, out var trainingEvent);
            if (success)
            {
                if (_activeHazard != null)
                {
                    _activeHazard.MarkExtinguisherSelected(FireTrainingWorkflow.TargetExtinguisherCO2);
                    _activeHazard.ShowDistanceZoneRing(true);
                }
                OnExtinguisherSelected?.Invoke(trainingEvent);
            }
            return success;
        }

        private void TrySelectSafeDistancePosition(Vector2 screenPosition)
        {
            if (_activeHazard == null) return;

            // 1. Raycast onto detected AR plane if available
            if (_raycastService != null && _raycastService.TryRaycastPlane(screenPosition, out Pose hitPose))
            {
                Vector3 hazardPos = _activeHazard.transform.position;
                Vector3 tapPos = hitPose.position;
                float distance = Vector2.Distance(new Vector2(tapPos.x, tapPos.z), new Vector2(hazardPos.x, hazardPos.z));
                SubmitDistanceDecision(distance);
                return;
            }

            // 2. Fallback ground plane raycast if camera exists
            if (_arCamera != null)
            {
                Ray ray = _arCamera.ScreenPointToRay(screenPosition);
                Plane groundPlane = new Plane(Vector3.up, _activeHazard.transform.position);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    Vector3 worldHit = ray.GetPoint(enter);
                    Vector3 hazardPos = _activeHazard.transform.position;
                    float distance = Vector2.Distance(new Vector2(worldHit.x, worldHit.z), new Vector2(hazardPos.x, hazardPos.z));
                    SubmitDistanceDecision(distance);
                }
            }
        }

        /// <summary>
        /// Submits the safe distance decision based on a standoff distance in meters.
        /// </summary>
        /// <param name="distanceMeters">The distance in meters from the hazard.</param>
        /// <returns>True if distance >= 2.0m (safe); false if too close.</returns>
        public bool SubmitDistanceDecision(float distanceMeters)
        {
            bool success = _workflow.SubmitDistanceDecision(distanceMeters, _eventDispatcher, out var trainingEvent);
            if (success)
            {
                if (_activeHazard != null)
                {
                    _activeHazard.MarkSafeDistanceConfirmed();
                }
                OnSafeDistanceDecided?.Invoke(trainingEvent);
            }
            return success;
        }

        /// <summary>
        /// Submits the safe distance decision using a decision identifier.
        /// </summary>
        /// <param name="decisionId">The decision identifier (e.g. standoff_distance_2m_maintained).</param>
        /// <returns>True if correct safe distance; false if unsafe.</returns>
        public bool SubmitDistanceDecision(string decisionId)
        {
            bool success = _workflow.SubmitDistanceDecision(decisionId, _eventDispatcher, out var trainingEvent);
            if (success)
            {
                if (_activeHazard != null)
                {
                    _activeHazard.MarkSafeDistanceConfirmed();
                }
                OnSafeDistanceDecided?.Invoke(trainingEvent);
            }
            return success;
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
