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
using IndustrialSafetyAR.Assessment;
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
        SafeDistanceMaintained,
        step_use_extinguisher = SafeDistanceMaintained,
        PinPulled,
        AimConfirmed,
        HandleSqueezed,
        ExtinguisherDischarged,
        ProcedureCompleted = ExtinguisherDischarged,
        AwaitingExitIdentification,
        step_identify_exit = AwaitingExitIdentification,
        ExitIdentified,
        AwaitingEvacuationRoute,
        step_evacuate_route = AwaitingEvacuationRoute,
        WaypointMainCorridorReached,
        WaypointBypassCrosscutReached,
        RouteEvacuated,
        EvacuationCompleted = RouteEvacuated,
        AwaitingAssemblyPoint,
        step_reach_assembly = AwaitingAssemblyPoint,
        AssemblyPointReached,
        AssemblyCompleted = AssemblyPointReached,
        TrainingCompleted = AssemblyPointReached
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
        private readonly System.Collections.Generic.List<EmergencyExitMarker> _activeExitMarkers = new System.Collections.Generic.List<EmergencyExitMarker>();
        private readonly System.Collections.Generic.List<EvacuationRouteMarker> _activeRouteMarkers = new System.Collections.Generic.List<EvacuationRouteMarker>();
        private readonly System.Collections.Generic.List<AssemblyPointMarker> _activeAssemblyMarkers = new System.Collections.Generic.List<AssemblyPointMarker>();
        private readonly System.Collections.Generic.List<ExtinguisherMarker> _activeExtinguisherMarkers = new System.Collections.Generic.List<ExtinguisherMarker>();
        private ITrainingEventDispatcher _eventDispatcher;

        public FireInteractionState State => _state;
        public FireWorkflowStage WorkflowStage => _workflow.CurrentStage;
        public FireTrainingWorkflow Workflow => _workflow;
        public FireHazardMarker ActiveHazard => _activeHazard;
        public System.Collections.Generic.IReadOnlyList<EmergencyExitMarker> ActiveExitMarkers => _activeExitMarkers;
        public System.Collections.Generic.IReadOnlyList<EvacuationRouteMarker> ActiveRouteMarkers => _activeRouteMarkers;
        public System.Collections.Generic.IReadOnlyList<AssemblyPointMarker> ActiveAssemblyMarkers => _activeAssemblyMarkers;
        public System.Collections.Generic.IReadOnlyList<ExtinguisherMarker> ActiveExtinguisherMarkers => _activeExtinguisherMarkers;
        public string CurrentStepId => _workflow.CurrentStepId;

        public TrainingAttempt LatestAttempt => _workflow.LatestAttempt;
        public AssessmentResult LatestAssessment => _workflow.LatestAssessment;
        public bool IsAssessmentCompleted => _workflow.IsAssessmentCompleted;

        public event Action<FireInteractionState> OnStateChanged;
        public event Action<FireHazardMarker> OnHazardPlaced;
        public event Action<FireHazardMarker, TrainingEvent> OnHazardDetected;
        public event Action<TrainingEvent> OnHazardIdentified;
        public event Action<TrainingEvent> OnAlarmRaised;
        public event Action<TrainingEvent> OnExtinguisherSelected;
        public event Action<TrainingEvent> OnSafeDistanceDecided;
        public event Action<TrainingEvent> OnExtinguisherProcedureCompleted;
        public event Action<string, TrainingEvent> OnExtinguisherActionCompleted;
        public event Action<TrainingEvent> OnExitIdentified;
        public event Action<string, TrainingEvent> OnExitMarked;
        public event Action<string, TrainingEvent> OnRouteWaypointReached;
        public event Action<TrainingEvent> OnEvacuationCompleted;
        public event Action<TrainingEvent> OnAssemblyPointReached;
        public event Action<TrainingEvent> OnTrainingCompleted;
        public event Action<TrainingAttempt, AssessmentResult> OnAssessmentCompleted;
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
            _workflow.OnAssessmentCompleted += HandleWorkflowAssessmentCompleted;
        }

        private void OnDestroy()
        {
            _workflow.OnStageChanged -= HandleWorkflowStageChanged;
            _workflow.OnFeedbackChanged -= HandleWorkflowFeedbackChanged;
            _workflow.OnAssessmentCompleted -= HandleWorkflowAssessmentCompleted;
        }

        private void HandleWorkflowAssessmentCompleted(TrainingAttempt attempt, AssessmentResult assessment)
        {
            OnAssessmentCompleted?.Invoke(attempt, assessment);
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
            if (stage == FireWorkflowStage.ExtinguisherDischarged || stage == FireWorkflowStage.AwaitingExitIdentification)
            {
                SpawnExitMarkersIfNeeded();
            }
            else if (stage == FireWorkflowStage.ExitIdentified || stage == FireWorkflowStage.AwaitingEvacuationRoute)
            {
                SpawnEvacuationRouteMarkersIfNeeded();
            }
            else if (stage == FireWorkflowStage.RouteEvacuated || stage == FireWorkflowStage.AwaitingAssemblyPoint)
            {
                SpawnAssemblyPointMarkersIfNeeded();
            }
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

        /// <summary>
        /// Explicitly assigns the active fire hazard marker (useful for testing and programmatic initialization).
        /// </summary>
        public void SetActiveHazard(FireHazardMarker hazard)
        {
            _activeHazard = hazard;
        }

        public void RegisterExtinguisherMarker(ExtinguisherMarker marker)
        {
            if (marker != null && !_activeExtinguisherMarkers.Contains(marker))
            {
                _activeExtinguisherMarkers.Add(marker);
            }
        }

        public void RegisterExitMarker(EmergencyExitMarker marker)
        {
            if (marker != null && !_activeExitMarkers.Contains(marker))
            {
                _activeExitMarkers.Add(marker);
            }
        }

        public void RegisterRouteMarker(EvacuationRouteMarker marker)
        {
            if (marker != null && !_activeRouteMarkers.Contains(marker))
            {
                _activeRouteMarkers.Add(marker);
            }
        }

        public void RegisterAssemblyMarker(AssemblyPointMarker marker)
        {
            if (marker != null && !_activeAssemblyMarkers.Contains(marker))
            {
                _activeAssemblyMarkers.Add(marker);
            }
        }

        /// <summary>
        /// Confirms detection of the active placed fire hazard marker.
        /// Can be invoked directly by UI button or after AR surface inspection.
        /// </summary>
        public bool ConfirmHazardDetected() => ConfirmHazardDetection(_activeHazard);

        /// <summary>
        /// Confirms detection of the specified or active fire hazard marker.
        /// Advances workflow to AwaitingIdentification and emits domain step_completed event.
        /// Prevents duplicate completions.
        /// </summary>
        public bool ConfirmHazardDetection(FireHazardMarker hazard = null)
        {
            var targetHazard = hazard ?? _activeHazard;
            if (targetHazard == null) return false;
            if (targetHazard.IsDetected) return false;
            if (_workflow.CurrentStage != FireWorkflowStage.HazardPlaced) return false;

            targetHazard.AcknowledgeDetection();

            if (_workflow.ConfirmHazardDetected(_eventDispatcher, out var trainingEvent))
            {
                OnHazardDetected?.Invoke(targetHazard, trainingEvent);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Processes an AR 3D marker or scene object selection from physical raycasting.
        /// Unifies physical AR marker interaction across all 9 training steps.
        /// Prevents invalid markers from advancing workflow and prevents duplicate completions.
        /// </summary>
        public bool Process3DMarkerHit(GameObject hitObject)
        {
            if (hitObject == null) return false;

            switch (_workflow.CurrentStage)
            {
                // Step 1: Detect Hazard (Confirmation)
                case FireWorkflowStage.HazardPlaced:
                {
                    var hazard = hitObject.GetComponentInParent<FireHazardMarker>();
                    if (hazard != null && (hazard == _activeHazard || _activeHazard == null) && !hazard.IsDetected)
                    {
                        return ConfirmHazardDetection(hazard);
                    }
                    return false;
                }

                // Step 2: Identify Hazard (Classification)
                case FireWorkflowStage.HazardDetected:
                case FireWorkflowStage.AwaitingIdentification:
                {
                    var hazard = hitObject.GetComponentInParent<FireHazardMarker>();
                    if (hazard != null)
                    {
                        return SubmitHazardIdentification(hazard.HazardId);
                    }
                    var identifiable = hitObject.GetComponentInParent<IIdentifiableHazard>();
                    if (identifiable != null)
                    {
                        return SubmitHazardIdentification(identifiable.HazardId);
                    }
                    return false;
                }

                // Step 3: Raise Emergency Alarm
                case FireWorkflowStage.HazardIdentified:
                case FireWorkflowStage.AwaitingAlarm:
                {
                    var hazard = hitObject.GetComponentInParent<FireHazardMarker>();
                    if (hazard != null)
                    {
                        return SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm);
                    }
                    return false;
                }

                // Step 4: Select Extinguisher
                case FireWorkflowStage.AlarmRaised:
                case FireWorkflowStage.AwaitingExtinguisherSelection:
                {
                    var extMarker = hitObject.GetComponentInParent<ExtinguisherMarker>();
                    if (extMarker != null)
                    {
                        bool selected = SubmitExtinguisherSelection(extMarker.ExtinguisherId);
                        if (selected) extMarker.MarkSelected();
                        return selected;
                    }
                    var hazard = hitObject.GetComponentInParent<FireHazardMarker>();
                    if (hazard != null)
                    {
                        return SubmitExtinguisherSelection(FireTrainingWorkflow.TargetExtinguisherCO2);
                    }
                    return false;
                }

                // Step 5: Safe Distance Decision
                case FireWorkflowStage.ExtinguisherSelected:
                case FireWorkflowStage.AwaitingSafeDistance:
                {
                    if (_activeHazard != null)
                    {
                        Vector3 hazardPos = _activeHazard.transform.position;
                        Vector3 hitPos = hitObject.transform.position;
                        float distance = Vector2.Distance(new Vector2(hitPos.x, hitPos.z), new Vector2(hazardPos.x, hazardPos.z));
                        return SubmitDistanceDecision(distance);
                    }
                    return false;
                }

                // Step 6: PASS Extinguisher Procedure
                case FireWorkflowStage.SafeDistanceMaintained:
                {
                    // PASS: P - Pull Pin
                    var hazard = hitObject.GetComponentInParent<FireHazardMarker>();
                    var extMarker = hitObject.GetComponentInParent<ExtinguisherMarker>();
                    if (hazard != null || extMarker != null)
                    {
                        return SubmitPullPin();
                    }
                    return false;
                }

                case FireWorkflowStage.PinPulled:
                {
                    // PASS: A - Aim at base of fire
                    var hazard = hitObject.GetComponentInParent<FireHazardMarker>();
                    if (hazard != null)
                    {
                        return SubmitAim();
                    }
                    return false;
                }

                case FireWorkflowStage.AimConfirmed:
                {
                    // PASS: S - Squeeze handle
                    var hazard = hitObject.GetComponentInParent<FireHazardMarker>();
                    var extMarker = hitObject.GetComponentInParent<ExtinguisherMarker>();
                    if (hazard != null || extMarker != null)
                    {
                        return SubmitSqueeze();
                    }
                    return false;
                }

                case FireWorkflowStage.HandleSqueezed:
                {
                    // PASS: S - Sweep side to side
                    var hazard = hitObject.GetComponentInParent<FireHazardMarker>();
                    if (hazard != null)
                    {
                        return SubmitSweep();
                    }
                    return false;
                }

                // Step 7: Identify Emergency Exit
                case FireWorkflowStage.ExtinguisherDischarged:
                case FireWorkflowStage.AwaitingExitIdentification:
                {
                    var exitMarker = hitObject.GetComponentInParent<EmergencyExitMarker>();
                    if (exitMarker != null)
                    {
                        if (exitMarker.IsIdentified) return false;
                        return SubmitIdentifyExit(exitMarker.ExitId);
                    }
                    return false;
                }

                // Step 8: Evacuate Route Waypoints
                case FireWorkflowStage.ExitIdentified:
                case FireWorkflowStage.AwaitingEvacuationRoute:
                case FireWorkflowStage.WaypointMainCorridorReached:
                case FireWorkflowStage.WaypointBypassCrosscutReached:
                {
                    var routeMarker = hitObject.GetComponentInParent<EvacuationRouteMarker>();
                    if (routeMarker != null)
                    {
                        if (routeMarker.IsTraversed) return false;
                        return SubmitEvacuationWaypoint(routeMarker.WaypointId);
                    }
                    return false;
                }

                // Step 9: Reach Assembly Point
                case FireWorkflowStage.RouteEvacuated:
                case FireWorkflowStage.AwaitingAssemblyPoint:
                {
                    var assemblyMarker = hitObject.GetComponentInParent<AssemblyPointMarker>();
                    if (assemblyMarker != null)
                    {
                        if (assemblyMarker.IsReached) return false;
                        return SubmitReachAssemblyPoint(assemblyMarker.PointId);
                    }
                    return false;
                }

                default:
                    return false;
            }
        }

        public void HandleTap(Vector2 screenPosition)
        {
            // Do not process taps over UI elements
            if (IsPointerOverUI(screenPosition))
            {
                return;
            }

            if (_workflow.CurrentStage == FireWorkflowStage.ReadyToPlace)
            {
                TryPlaceHazard(screenPosition);
                return;
            }

            if (_workflow.CurrentStage == FireWorkflowStage.AwaitingSafeDistance)
            {
                TrySelectSafeDistancePosition(screenPosition);
                return;
            }

            if (_arCamera == null)
            {
                _arCamera = Camera.main;
                if (_arCamera == null) return;
            }

            Ray ray = _arCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                Process3DMarkerHit(hit.collider.gameObject);
            }
        }

        public void ProcessScreenTap(Vector2 screenPosition) => HandleTap(screenPosition);

        public bool RaycastAndProcessHit(Ray ray, float maxDistance = 50f)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                return Process3DMarkerHit(hit.collider.gameObject);
            }
            return false;
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

            if (_arCamera == null)
            {
                _arCamera = Camera.main;
            }

            // 0. Check physical 3D marker or collider hit first
            if (_arCamera != null)
            {
                Ray screenRay = _arCamera.ScreenPointToRay(screenPosition);
                if (Physics.Raycast(screenRay, out RaycastHit markerHit, 50f))
                {
                    if (Process3DMarkerHit(markerHit.collider.gameObject))
                    {
                        return;
                    }
                }
            }

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

        private void TryAimAtBase(Vector2 screenPosition)
        {
            if (_activeHazard == null) return;

            if (_arCamera == null)
            {
                _arCamera = Camera.main;
                if (_arCamera == null) return;
            }

            Ray ray = _arCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                var hazard = hit.collider.GetComponentInParent<FireHazardMarker>();
                if (hazard != null && hazard == _activeHazard)
                {
                    SubmitAim();
                }
            }
        }

        /// <summary>
        /// Submits an interactive action for the Step 6 extinguisher PASS procedure.
        /// </summary>
        public bool SubmitExtinguisherAction(string actionId)
        {
            bool success = _workflow.SubmitExtinguisherAction(actionId, _eventDispatcher, out var trainingEvent);
            if (success)
            {
                if (_workflow.CurrentStage == FireWorkflowStage.PinPulled)
                {
                    _activeHazard?.ShowAimTarget(true);
                }
                else if (_workflow.CurrentStage == FireWorkflowStage.ExtinguisherDischarged)
                {
                    _activeHazard?.TriggerExtinguisherDischargeVisual();
                    OnExtinguisherProcedureCompleted?.Invoke(trainingEvent);
                }

                OnExtinguisherActionCompleted?.Invoke(actionId, trainingEvent);
            }
            return success;
        }

        public bool SubmitPullPin() => SubmitExtinguisherAction(FireTrainingWorkflow.ActionPullPin);
        public bool SubmitAim() => SubmitExtinguisherAction(FireTrainingWorkflow.ActionAim);
        public bool SubmitSqueeze() => SubmitExtinguisherAction(FireTrainingWorkflow.ActionSqueeze);
        public bool SubmitSweep() => SubmitExtinguisherAction(FireTrainingWorkflow.ActionSweep);

        private void TrySelectEmergencyExit(Vector2 screenPosition)
        {
            if (_arCamera == null)
            {
                _arCamera = Camera.main;
                if (_arCamera == null) return;
            }

            Ray ray = _arCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                var marker = hit.collider.GetComponentInParent<EmergencyExitMarker>();
                if (marker != null)
                {
                    SubmitIdentifyExit(marker.ExitId);
                }
            }
        }

        /// <summary>
        /// Submits the emergency exit identification action.
        /// </summary>
        /// <param name="targetId">The selected exit marker ID (e.g. exit_emergency_sector_b).</param>
        /// <param name="actionId">The action identifier (defaults to mark).</param>
        /// <returns>True if correct exit identified; false if incorrect or premature.</returns>
        public bool SubmitIdentifyExit(string targetId, string actionId = FireTrainingWorkflow.ActionMark)
        {
            bool success = _workflow.SubmitIdentifyExit(targetId, actionId, _eventDispatcher, out var trainingEvent);
            if (success)
            {
                if (_activeExitMarkers != null)
                {
                    foreach (var marker in _activeExitMarkers)
                    {
                        if (marker != null && marker.ExitId == targetId)
                        {
                            marker.AcknowledgeIdentification();
                        }
                    }
                }
                OnExitIdentified?.Invoke(trainingEvent);
            }
            OnExitMarked?.Invoke(targetId, trainingEvent);
            return success;
        }

        /// <summary>
        /// Spawns procedural 3D emergency exit markers in AR space along clear egress routes.
        /// </summary>
        public void SpawnExitMarkersIfNeeded()
        {
            if (_activeExitMarkers != null && _activeExitMarkers.Count > 0)
            {
                return;
            }

            Vector3 basePos = _activeHazard != null ? _activeHazard.transform.position : Vector3.zero;
            Quaternion baseRot = _activeHazard != null ? _activeHazard.transform.rotation : Quaternion.identity;

            // 1. Sector B Emergency Exit (Primary / Designated Safe Exit)
            Vector3 sectorBPos = basePos + baseRot * new Vector3(2.5f, 0f, 2.0f);
            var sectorBObj = new GameObject("Marker_ExitEmergencySectorB");
            sectorBObj.transform.position = sectorBPos;
            sectorBObj.transform.rotation = baseRot;
            var sectorBMarker = sectorBObj.AddComponent<EmergencyExitMarker>();
            sectorBMarker.ConfigureExit(FireTrainingWorkflow.TargetExitEmergencySectorB, "Sector B Emergency Exit", true);
            _activeExitMarkers.Add(sectorBMarker);

            // 2. Freight Elevator (Prohibited during fire)
            Vector3 elevatorPos = basePos + baseRot * new Vector3(-2.8f, 0f, 1.2f);
            var elevatorObj = new GameObject("Marker_ExitFreightElevator");
            elevatorObj.transform.position = elevatorPos;
            elevatorObj.transform.rotation = baseRot;
            var elevatorMarker = elevatorObj.AddComponent<EmergencyExitMarker>();
            elevatorMarker.ConfigureExit(FireTrainingWorkflow.TargetExitFreightElevator, "Freight Elevator (Unsafe)", false);
            _activeExitMarkers.Add(elevatorMarker);

            // 3. Corridor Sector A (Blocked by smoke)
            Vector3 corridorPos = basePos + baseRot * new Vector3(0.0f, 0f, 3.8f);
            var corridorObj = new GameObject("Marker_ExitBlockedCorridor");
            corridorObj.transform.position = corridorPos;
            corridorObj.transform.rotation = baseRot;
            var corridorMarker = corridorObj.AddComponent<EmergencyExitMarker>();
            corridorMarker.ConfigureExit(FireTrainingWorkflow.TargetExitBlockedCorridor, "Sector A Route (Smoke Blocked)", false);
            _activeExitMarkers.Add(corridorMarker);

            Debug.Log("[FireArInteractionController] Procedural emergency exit markers spawned in AR space.");
        }

        private void TrySelectEvacuationWaypoint(Vector2 screenPosition)
        {
            if (_arCamera == null)
            {
                _arCamera = Camera.main;
                if (_arCamera == null) return;
            }

            Ray ray = _arCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                var waypoint = hit.collider.GetComponentInParent<EvacuationRouteMarker>();
                if (waypoint != null)
                {
                    SubmitEvacuationWaypoint(waypoint.WaypointId);
                }
            }
        }

        /// <summary>
        /// Submits an evacuation route waypoint along the designated safe path.
        /// </summary>
        public bool SubmitEvacuationWaypoint(string waypointId)
        {
            bool success = _workflow.SubmitEvacuationWaypoint(waypointId, _eventDispatcher, out var trainingEvent);
            if (success)
            {
                if (_activeRouteMarkers != null)
                {
                    foreach (var marker in _activeRouteMarkers)
                    {
                        if (marker != null && string.Equals(marker.WaypointId, waypointId, StringComparison.OrdinalIgnoreCase))
                        {
                            marker.MarkTraversed();
                        }
                    }

                    // Activate next expected waypoint marker
                    int nextIndex = _workflow.CurrentStage == FireWorkflowStage.WaypointMainCorridorReached ? 2
                        : (_workflow.CurrentStage == FireWorkflowStage.WaypointBypassCrosscutReached ? 3 : 0);

                    if (nextIndex > 0)
                    {
                        foreach (var marker in _activeRouteMarkers)
                        {
                            if (marker != null && marker.SequenceOrder == nextIndex)
                            {
                                marker.SetActiveTarget(true);
                            }
                        }
                    }
                }

                OnRouteWaypointReached?.Invoke(waypointId, trainingEvent);

                if (_workflow.CurrentStage == FireWorkflowStage.RouteEvacuated)
                {
                    SpawnAssemblyPointMarkersIfNeeded();
                    OnEvacuationCompleted?.Invoke(trainingEvent);
                }
            }

            return success;
        }

        /// <summary>
        /// Submits an ordered list of waypoints representing the entire evacuation route sequence.
        /// </summary>
        public bool SubmitEvacuationSequence(System.Collections.Generic.IList<string> waypointIds)
        {
            bool success = _workflow.SubmitEvacuationSequence(waypointIds, _eventDispatcher, out var trainingEvent);
            if (success)
            {
                if (_activeRouteMarkers != null)
                {
                    foreach (var marker in _activeRouteMarkers)
                    {
                        if (marker != null && !marker.IsHazardousAlternative)
                        {
                            marker.MarkTraversed();
                        }
                    }
                }

                SpawnAssemblyPointMarkersIfNeeded();
                OnEvacuationCompleted?.Invoke(trainingEvent);
            }

            return success;
        }

        /// <summary>
        /// Spawns procedural 3D evacuation route markers in AR space guiding the worker safely toward the assembly point.
        /// </summary>
        public void SpawnEvacuationRouteMarkersIfNeeded()
        {
            if (_activeRouteMarkers != null && _activeRouteMarkers.Count > 0)
            {
                return;
            }

            Vector3 basePos = _activeHazard != null ? _activeHazard.transform.position : Vector3.zero;
            Quaternion baseRot = _activeHazard != null ? _activeHazard.transform.rotation : Quaternion.identity;

            // 1. Waypoint 1: Main Corridor (Clear route starting near Sector B exit)
            Vector3 wp1Pos = basePos + baseRot * new Vector3(3.2f, 0f, 2.5f);
            var wp1Obj = new GameObject("Marker_WaypointMainCorridor");
            wp1Obj.transform.position = wp1Pos;
            wp1Obj.transform.rotation = baseRot;
            var wp1Marker = wp1Obj.AddComponent<EvacuationRouteMarker>();
            wp1Marker.ConfigureWaypoint(FireTrainingWorkflow.WaypointMainCorridor, "Waypoint 1: Main Corridor", 1, false);
            _activeRouteMarkers.Add(wp1Marker);

            // 2. Waypoint 2: Bypass Crosscut (Diverting away from smoke buildup)
            Vector3 wp2Pos = basePos + baseRot * new Vector3(4.5f, 0f, 3.8f);
            var wp2Obj = new GameObject("Marker_WaypointBypassCrosscut");
            wp2Obj.transform.position = wp2Pos;
            wp2Obj.transform.rotation = baseRot;
            var wp2Marker = wp2Obj.AddComponent<EvacuationRouteMarker>();
            wp2Marker.ConfigureWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, "Waypoint 2: Bypass Crosscut", 2, false);
            _activeRouteMarkers.Add(wp2Marker);

            // 3. Waypoint 3: Fire Door Exit (Boundary leading toward assembly muster point)
            Vector3 wp3Pos = basePos + baseRot * new Vector3(5.8f, 0f, 5.2f);
            var wp3Obj = new GameObject("Marker_WaypointFireDoorExit");
            wp3Obj.transform.position = wp3Pos;
            wp3Obj.transform.rotation = baseRot;
            var wp3Marker = wp3Obj.AddComponent<EvacuationRouteMarker>();
            wp3Marker.ConfigureWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, "Waypoint 3: Fire Door Exit", 3, false);
            _activeRouteMarkers.Add(wp3Marker);

            // 4. Hazard Alternative: Sector A Smoke Corridor (Unsafe corridor)
            Vector3 smokePos = basePos + baseRot * new Vector3(1.0f, 0f, 4.5f);
            var smokeObj = new GameObject("Marker_HazardSmokeCorridor");
            smokeObj.transform.position = smokePos;
            smokeObj.transform.rotation = baseRot;
            var smokeMarker = smokeObj.AddComponent<EvacuationRouteMarker>();
            smokeMarker.ConfigureWaypoint(FireTrainingWorkflow.HazardSmokeCorridor, "Sector A Smoke Corridor", 0, true);
            _activeRouteMarkers.Add(smokeMarker);

            Debug.Log("[FireArInteractionController] Procedural evacuation route waypoints spawned in AR space.");
        }

        private void TrySelectAssemblyPoint(Vector2 screenPosition)
        {
            if (_arCamera == null)
            {
                _arCamera = Camera.main;
                if (_arCamera == null) return;
            }

            Ray ray = _arCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                var marker = hit.collider.GetComponentInParent<AssemblyPointMarker>();
                if (marker != null)
                {
                    SubmitReachAssemblyPoint(marker.PointId);
                }
            }
        }

        /// <summary>
        /// Submits the emergency assembly point identification action.
        /// </summary>
        /// <param name="targetId">The selected assembly point target ID (e.g. assembly_muster_point_alpha).</param>
        /// <param name="actionId">The action identifier (defaults to complete_step).</param>
        /// <returns>True if designated assembly point reached; false if incorrect or premature.</returns>
        public bool SubmitReachAssemblyPoint(string targetId, string actionId = FireTrainingWorkflow.ActionCompleteStep)
        {
            bool success = _workflow.SubmitReachAssemblyPoint(targetId, actionId, _eventDispatcher, out var trainingEvent);
            if (success)
            {
                if (_activeAssemblyMarkers != null)
                {
                    foreach (var marker in _activeAssemblyMarkers)
                    {
                        if (marker != null && string.Equals(marker.PointId, targetId, StringComparison.OrdinalIgnoreCase))
                        {
                            marker.AcknowledgeReached();
                        }
                    }
                }

                OnAssemblyPointReached?.Invoke(trainingEvent);
                OnTrainingCompleted?.Invoke(trainingEvent);
            }

            return success;
        }

        /// <summary>
        /// Spawns procedural 3D assembly point markers in AR space after successful evacuation.
        /// </summary>
        public void SpawnAssemblyPointMarkersIfNeeded()
        {
            if (_activeAssemblyMarkers != null && _activeAssemblyMarkers.Count > 0)
            {
                return;
            }

            Vector3 basePos = _activeHazard != null ? _activeHazard.transform.position : Vector3.zero;
            Quaternion baseRot = _activeHazard != null ? _activeHazard.transform.rotation : Quaternion.identity;

            // 1. Designated Safe Emergency Assembly Area (Muster Point Alpha)
            Vector3 alphaPos = basePos + baseRot * new Vector3(7.0f, 0f, 6.5f);
            var alphaObj = new GameObject("Marker_AssemblyMusterPointAlpha");
            alphaObj.transform.position = alphaPos;
            alphaObj.transform.rotation = baseRot;
            var alphaMarker = alphaObj.AddComponent<AssemblyPointMarker>();
            alphaMarker.ConfigureAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, "Muster Point Alpha", true);
            _activeAssemblyMarkers.Add(alphaMarker);

            // 2. Non-designated / Unauthorized Area (Perimeter Loading Gate - Unsafe during emergency)
            Vector3 betaPos = basePos + baseRot * new Vector3(4.0f, 0f, 7.5f);
            var betaObj = new GameObject("Marker_AssemblyPointBeta");
            betaObj.transform.position = betaPos;
            betaObj.transform.rotation = baseRot;
            var betaMarker = betaObj.AddComponent<AssemblyPointMarker>();
            betaMarker.ConfigureAssemblyPoint(FireTrainingWorkflow.TargetAssemblyPointBeta, "Loading Gate B (Unauthorized)", false);
            _activeAssemblyMarkers.Add(betaMarker);

            Debug.Log("[FireArInteractionController] Procedural emergency assembly point markers spawned in AR space.");
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
        /// Resets the training workflow and interaction environment for a fresh retake attempt,
        /// clearing all spawned 3D markers and event dispatcher log so a new unique client_attempt_id is generated.
        /// </summary>
        public void RetakeTraining()
        {
            if (_activeHazard != null)
            {
                Destroy(_activeHazard.gameObject);
                _activeHazard = null;
            }

            if (_activeExitMarkers != null)
            {
                foreach (var marker in _activeExitMarkers)
                {
                    if (marker != null) Destroy(marker.gameObject);
                }
                _activeExitMarkers.Clear();
            }

            if (_activeRouteMarkers != null)
            {
                foreach (var marker in _activeRouteMarkers)
                {
                    if (marker != null) Destroy(marker.gameObject);
                }
                _activeRouteMarkers.Clear();
            }

            if (_activeAssemblyMarkers != null)
            {
                foreach (var marker in _activeAssemblyMarkers)
                {
                    if (marker != null) Destroy(marker.gameObject);
                }
                _activeAssemblyMarkers.Clear();
            }

            if (_activeExtinguisherMarkers != null)
            {
                foreach (var marker in _activeExtinguisherMarkers)
                {
                    if (marker != null) Destroy(marker.gameObject);
                }
                _activeExtinguisherMarkers.Clear();
            }

            if (_eventDispatcher is TrainingEventBus bus)
            {
                bus.Clear();
            }
            else if (TrainingEventBus.Instance != null)
            {
                TrainingEventBus.Instance.Clear();
            }

            _workflow.Reset();
            _state = FireInteractionState.ReadyToPlace;
            _workflow.SetStage(FireWorkflowStage.ReadyToPlace);
            OnStateChanged?.Invoke(_state);
            Debug.Log("[FireArInteractionController] Retake initiated: Scenario reset for new unique attempt.");
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
                go.AddComponent<IndustrialSafetyAR.UI.FireAssessmentSummaryUI>();
                Debug.Log("[FireArInteractionController] Auto-bootstrapped Fire AR Interaction and Assessment Summary in AR scene.");
            }
        }
    }
}
