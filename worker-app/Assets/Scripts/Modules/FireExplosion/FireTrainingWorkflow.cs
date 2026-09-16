// FireTrainingWorkflow.cs
// Namespace : IndustrialSafetyAR.Modules.FireExplosion
//
// Pure C# domain workflow managing training stages, step transitions, and
// domain event emission for the Fire & Explosion Response module.
// Fully decoupled from Unity runtime for isolated unit testing.

using System;
using System.Collections.Generic;
using IndustrialSafetyAR.Core.Events;
using IndustrialSafetyAR.Assessment;

namespace IndustrialSafetyAR.Modules.FireExplosion
{
    public enum FireWorkflowStage
    {
        NotStarted,
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
    /// Pure C# domain state machine enforcing strict step sequencing and domain event schema
    /// for the Fire &amp; Explosion Response training scenario.
    /// </summary>
    public class FireTrainingWorkflow
    {
        public const string ModuleId = "fire-explosion-response";
        public const string ContentVersion = "1.0.0";

        // Step 1: Detect Hazard
        public const string StepDetectHazard = "step_detect_hazard";
        public const string ActionDetectHazard = "detect_hazard_acknowledged";
        public const string RuleDetectHazard = "rule_detect_hazard";

        // Step 2: Identify Hazard
        public const string StepIdentifyHazard = "step_identify_hazard";
        public const string ActionIdentify = "identify";
        public const string RuleIdentifyHazard = "rule_identify_hazard";
        public const string TargetElectricalConveyorFire = "hazard_electrical_conveyor_fire";
        public const string HazardClassElectrical = "class_e_electrical";

        // Step 3: Raise Alarm
        public const string StepRaiseAlarm = "step_raise_alarm";
        public const string ActionRaiseAlarm = "manual_call_point_activated";
        public const string RuleRaiseAlarm = "rule_raise_alarm";

        // Step 4: Select Extinguisher
        public const string StepSelectExtinguisher = "step_select_extinguisher";
        public const string ActionSelectExtinguisher = "select";
        public const string RuleSelectExtinguisher = "rule_select_extinguisher";
        public const string TargetExtinguisherCO2 = "extinguisher_co2";
        public const string TargetExtinguisherWater = "extinguisher_water";
        public const string TargetExtinguisherFoam = "extinguisher_foam";
        public const string ToolClassCO2 = "co2_extinguisher";

        // Step 5: Maintain Safe Distance
        public const string StepMaintainDistance = "step_maintain_distance";
        public const string ActionDecide = "decide";
        public const string RuleMaintainDistance = "rule_maintain_distance";
        public const string DecisionSafeDistance2m = "standoff_distance_2m_maintained";
        public const string DecisionUnsafeTooClose = "standoff_distance_too_close";
        public const float MinimumSafeDistanceMeters = 2.0f;

        // Step 6: Use Extinguisher (PASS Procedure)
        public const string StepUseExtinguisher = "step_use_extinguisher";
        public const string RuleUseExtinguisher = "rule_use_extinguisher";
        public const string TargetExtinguisherProcedure = "extinguisher_procedure";
        public const string TargetSafetyPin = "safety_pin";
        public const string TargetHazardBase = "hazard_base";
        public const string TargetExtinguisherHandle = "extinguisher_handle";

        public const string ActionPullPin = "pull_pin";
        public const string ActionAim = "aim";
        public const string ActionSqueeze = "squeeze";
        public const string ActionSweep = "sweep";

        // Step 7: Identify Exit
        public const string StepIdentifyExit = "step_identify_exit";
        public const string ActionMark = "mark";
        public const string RuleIdentifyExit = "rule_identify_exit";
        public const string TargetExitEmergencySectorB = "exit_emergency_sector_b";
        public const string TargetExitFreightElevator = "exit_freight_elevator";
        public const string TargetExitBlockedCorridor = "exit_blocked_corridor_a";
        public const string LocationTypeEmergencyExit = "emergency_exit";

        // Step 8: Evacuate Route
        public const string StepEvacuateRoute = "step_evacuate_route";
        public const string ActionSubmitSequence = "submit_sequence";
        public const string RuleEvacuateRoute = "rule_evacuate_route";

        public const string WaypointMainCorridor = "waypoint_main_corridor";
        public const string WaypointBypassCrosscut = "waypoint_bypass_crosscut";
        public const string WaypointByExit = "waypoint_by_exit";
        public const string WaypointFireDoorExit = "waypoint_fire_door_exit";
        public const string HazardSmokeCorridor = "hazard_heavy_smoke_corridor";
        public const string DecisionAvoidSmoke = "avoid_heavy_smoke_corridor";

        // Step 9: Reach Assembly Point
        public const string StepReachAssembly = "step_reach_assembly";
        public const string RuleReachAssembly = "rule_reach_assembly";
        public const string ActionCompleteStep = "complete_step";
        public const string EventTypeAssemblyReached = "assembly_reached";
        public const string TargetAssemblyMusterPoint = "assembly_muster_point_alpha";
        public const string TargetAssemblyPointBeta = "assembly_muster_point_beta";
        public const string ZoneTypeEmergencyAssembly = "emergency_assembly_area";

        /// <summary>
        /// Stable valid UUID for offline runtime attempts when no remote session is active.
        /// Conforms strictly to RFC 4122 / attempt.schema.json format: uuid.
        /// Matches the demo worker identifier seeded in the platform.
        /// </summary>
        public const string DefaultOfflineWorkerId = "00000000-dead-beef-0001-000000000001";

        public const string EventTypeAttemptFinalized = "attempt_finalized_for_outbox";
        public const string ActionFinalizeSession = "finalize_session";

        public FireWorkflowStage CurrentStage { get; private set; } = FireWorkflowStage.NotStarted;
        public string CurrentStepId { get; private set; } = StepDetectHazard;
        public string WorkerId { get; set; } = DefaultOfflineWorkerId;
        public string SessionStartedAt { get; private set; }
        public TrainingAttempt LatestAttempt { get; private set; }
        public AssessmentResult LatestAssessment { get; private set; }
        public bool IsAssessmentCompleted => LatestAttempt != null && LatestAssessment != null;
        public bool IsAttemptFinalizedForOutbox { get; private set; }

        /// <summary>
        /// The active rubric definition bound to this training workflow session.
        /// Loaded from bundled assets at runtime.
        /// </summary>
        public RubricDefinition BoundRubric { get; private set; }

        public FireTrainingWorkflow()
        {
            BindRubric(RubricLoader.LoadFireExplosionRubric());
        }

        public FireTrainingWorkflow(RubricDefinition rubric)
        {
            BindRubric(rubric ?? RubricLoader.LoadFireExplosionRubric());
        }

        public FireTrainingWorkflow(IRubricProvider rubricProvider)
        {
            BindRubric(rubricProvider != null
                ? rubricProvider.LoadRubric(ModuleId)
                : RubricLoader.LoadFireExplosionRubric());
        }

        /// <summary>
        /// Explicitly binds a loaded RubricDefinition to this workflow instance.
        /// </summary>
        public void BindRubric(RubricDefinition rubric)
        {
            BoundRubric = rubric ?? RubricLoader.LoadFireExplosionRubric();
        }

        public event Action<FireWorkflowStage> OnStageChanged;
        public event Action<string> OnFeedbackChanged;
        public event Action<TrainingAttempt, AssessmentResult> OnAssessmentCompleted;
        public event Action<TrainingAttempt> OnAttemptFinalizedForOutbox;

        public void SetStage(FireWorkflowStage stage)
        {
            if (string.IsNullOrEmpty(SessionStartedAt) && stage != FireWorkflowStage.NotStarted)
            {
                SessionStartedAt = DateTime.UtcNow.ToString("o");
            }

            CurrentStage = stage;
            switch (stage)
            {
                case FireWorkflowStage.WaitingForTracking:
                case FireWorkflowStage.ReadyToPlace:
                case FireWorkflowStage.HazardPlaced:
                    CurrentStepId = StepDetectHazard;
                    break;
                case FireWorkflowStage.HazardDetected:
                case FireWorkflowStage.AwaitingIdentification:
                    CurrentStepId = StepIdentifyHazard;
                    break;
                case FireWorkflowStage.HazardIdentified:
                case FireWorkflowStage.AwaitingAlarm:
                case FireWorkflowStage.AlarmRaised:
                    CurrentStepId = StepRaiseAlarm;
                    break;
                case FireWorkflowStage.AwaitingExtinguisherSelection:
                case FireWorkflowStage.ExtinguisherSelected:
                    CurrentStepId = StepSelectExtinguisher;
                    break;
                case FireWorkflowStage.AwaitingSafeDistance:
                    CurrentStepId = StepMaintainDistance;
                    break;
                case FireWorkflowStage.SafeDistanceMaintained:
                case FireWorkflowStage.PinPulled:
                case FireWorkflowStage.AimConfirmed:
                case FireWorkflowStage.HandleSqueezed:
                    CurrentStepId = StepUseExtinguisher;
                    break;
                case FireWorkflowStage.ExtinguisherDischarged:
                case FireWorkflowStage.AwaitingExitIdentification:
                    CurrentStepId = StepIdentifyExit;
                    break;
                case FireWorkflowStage.ExitIdentified:
                case FireWorkflowStage.AwaitingEvacuationRoute:
                case FireWorkflowStage.WaypointMainCorridorReached:
                case FireWorkflowStage.WaypointBypassCrosscutReached:
                    CurrentStepId = StepEvacuateRoute;
                    break;
                case FireWorkflowStage.RouteEvacuated:
                case FireWorkflowStage.AwaitingAssemblyPoint:
                case FireWorkflowStage.AssemblyPointReached:
                    CurrentStepId = StepReachAssembly;
                    break;
            }

            OnStageChanged?.Invoke(stage);
            OnFeedbackChanged?.Invoke(GetFeedbackForStage(stage));
        }

        /// <summary>
        /// Acknowledges hazard detection in AR space and advances to the identification stage.
        /// </summary>
        public bool ConfirmHazardDetected(ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != FireWorkflowStage.HazardPlaced)
            {
                return false;
            }

            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepDetectHazard,
                EventType = "step_completed",
                ActionId = ActionDetectHazard,
                TargetId = TargetElectricalConveyorFire,
                Outcome = "success",
                Payload =
                {
                    { "rule_id", RuleDetectHazard },
                    { "action_id", ActionDetectHazard },
                    { "target_id", TargetElectricalConveyorFire },
                    { "outcome", "success" }
                }
            };

            dispatcher?.Dispatch(emittedEvent);
            CurrentStepId = StepIdentifyHazard;
            SetStage(FireWorkflowStage.AwaitingIdentification);
            return true;
        }

        /// <summary>
        /// Submits a hazard identification selection. Advances only on correct classification.
        /// </summary>
        public bool SubmitHazardIdentification(string targetId, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != FireWorkflowStage.AwaitingIdentification)
            {
                return false;
            }

            bool isCorrect = string.Equals(targetId, TargetElectricalConveyorFire, StringComparison.OrdinalIgnoreCase);
            string outcome = isCorrect ? "success" : "failure";

            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepIdentifyHazard,
                EventType = "hazard_identified",
                ActionId = ActionIdentify,
                TargetId = targetId,
                Outcome = outcome,
                Payload =
                {
                    { "rule_id", RuleIdentifyHazard },
                    { "action_id", ActionIdentify },
                    { "target_id", targetId },
                    { "hazard_class", HazardClassElectrical },
                    { "outcome", outcome }
                }
            };

            dispatcher?.Dispatch(emittedEvent);

            if (isCorrect)
            {
                CurrentStepId = StepRaiseAlarm;
                SetStage(FireWorkflowStage.AwaitingAlarm);
                return true;
            }
            else
            {
                // Invalid action does NOT advance the stage
                OnFeedbackChanged?.Invoke("Incorrect classification. Re-examine the conveyor equipment.");
                return false;
            }
        }

        /// <summary>
        /// Activates the emergency manual call point alarm.
        /// </summary>
        public bool SubmitRaiseAlarm(string actionId, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != FireWorkflowStage.AwaitingAlarm)
            {
                return false;
            }

            bool isCorrect = string.Equals(actionId, ActionRaiseAlarm, StringComparison.OrdinalIgnoreCase);
            if (!isCorrect)
            {
                // Invalid action does NOT advance the flow
                return false;
            }

            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepRaiseAlarm,
                EventType = "alarm_raised",
                ActionId = ActionRaiseAlarm,
                Outcome = "success",
                Payload =
                {
                    { "rule_id", RuleRaiseAlarm },
                    { "action_id", ActionRaiseAlarm },
                    { "outcome", "success" }
                }
            };

            dispatcher?.Dispatch(emittedEvent);
            SetStage(FireWorkflowStage.AlarmRaised);
            CurrentStepId = StepSelectExtinguisher;
            SetStage(FireWorkflowStage.AwaitingExtinguisherSelection);
            return true;
        }

        /// <summary>
        /// Submits extinguisher selection. Advances only on selecting the correct CO2 extinguisher.
        /// </summary>
        public bool SubmitSelectExtinguisher(string targetId, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != FireWorkflowStage.AwaitingExtinguisherSelection && CurrentStage != FireWorkflowStage.AlarmRaised)
            {
                return false;
            }

            bool isCorrect = string.Equals(targetId, TargetExtinguisherCO2, StringComparison.OrdinalIgnoreCase);
            string outcome = isCorrect ? "success" : "failure";

            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepSelectExtinguisher,
                EventType = "extinguisher_selected",
                ActionId = ActionSelectExtinguisher,
                TargetId = targetId,
                Outcome = outcome,
                Payload =
                {
                    { "rule_id", RuleSelectExtinguisher },
                    { "action_id", ActionSelectExtinguisher },
                    { "target_id", targetId },
                    { "tool_class", ToolClassCO2 },
                    { "outcome", outcome }
                }
            };

            dispatcher?.Dispatch(emittedEvent);

            if (isCorrect)
            {
                CurrentStepId = StepMaintainDistance;
                SetStage(FireWorkflowStage.ExtinguisherSelected);
                SetStage(FireWorkflowStage.AwaitingSafeDistance);
                return true;
            }
            else
            {
                string feedback = string.Equals(targetId, TargetExtinguisherWater, StringComparison.OrdinalIgnoreCase)
                    ? "DANGER: Water conducts electricity! Risk of fatal electrocution on electrical fires."
                    : "INCORRECT: Foam is not safe for energized electrical equipment. Select CO2.";
                OnFeedbackChanged?.Invoke(feedback);
                return false;
            }
        }

        /// <summary>
        /// Submits the safe distance decision based on a standoff distance in meters.
        /// </summary>
        public bool SubmitDistanceDecision(float standoffDistanceMeters, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            string decisionId = standoffDistanceMeters >= MinimumSafeDistanceMeters
                ? DecisionSafeDistance2m
                : DecisionUnsafeTooClose;

            return SubmitDistanceDecision(decisionId, dispatcher, out emittedEvent);
        }

        /// <summary>
        /// Submits a safe distance decision by decision identifier.
        /// </summary>
        public bool SubmitDistanceDecision(string decisionId, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != FireWorkflowStage.AwaitingSafeDistance && CurrentStage != FireWorkflowStage.ExtinguisherSelected)
            {
                return false;
            }

            bool isCorrect = string.Equals(decisionId, DecisionSafeDistance2m, StringComparison.OrdinalIgnoreCase);
            string outcome = isCorrect ? "success" : "failure";

            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepMaintainDistance,
                EventType = "decision_made",
                ActionId = ActionDecide,
                TargetId = decisionId,
                Outcome = outcome,
                Payload =
                {
                    { "rule_id", RuleMaintainDistance },
                    { "action_id", ActionDecide },
                    { "decision_id", decisionId },
                    { "outcome", outcome }
                }
            };

            dispatcher?.Dispatch(emittedEvent);

            if (isCorrect)
            {
                CurrentStepId = "step_use_extinguisher";
                SetStage(FireWorkflowStage.SafeDistanceMaintained);
                return true;
            }
            else
            {
                OnFeedbackChanged?.Invoke("UNSAFE DISTANCE: Inside 2m flashover/shock zone! Back up to maintain safe standoff distance.");
                return false;
            }
        }

        /// <summary>
        /// Submits an interactive action for the Step 6 extinguisher PASS procedure.
        /// Validates ordering: pull_pin -> aim -> squeeze -> sweep.
        /// </summary>
        public bool SubmitExtinguisherAction(string actionId, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;

            bool isInStep6 = CurrentStage == FireWorkflowStage.SafeDistanceMaintained
                || CurrentStage == FireWorkflowStage.PinPulled
                || CurrentStage == FireWorkflowStage.AimConfirmed
                || CurrentStage == FireWorkflowStage.HandleSqueezed;

            if (!isInStep6)
            {
                // Action rejected before Step 5 safe distance confirmation or after completion
                return false;
            }

            string rawAction = actionId?.Trim() ?? string.Empty;
            string normalized = rawAction.ToLowerInvariant();
            if (normalized == "pull" || normalized == "pull_safety_pin") normalized = ActionPullPin;
            else if (normalized == "aim_base" || normalized == "aim_nozzle" || normalized == "aim_at_base") normalized = ActionAim;
            else if (normalized == "squeeze_handle" || normalized == "press_handle") normalized = ActionSqueeze;
            else if (normalized == "sweep_nozzle" || normalized == "sweep_side_to_side") normalized = ActionSweep;

            string expectedAction;
            FireWorkflowStage nextStage;
            string targetId;
            string passStep;
            string successFeedback;

            switch (CurrentStage)
            {
                case FireWorkflowStage.SafeDistanceMaintained:
                    expectedAction = ActionPullPin;
                    nextStage = FireWorkflowStage.PinPulled;
                    targetId = TargetSafetyPin;
                    passStep = "pull";
                    successFeedback = "PASS — PULL: Pin pulled! Extinguisher unlocked. Aim nozzle at base of fire.";
                    break;

                case FireWorkflowStage.PinPulled:
                    expectedAction = ActionAim;
                    nextStage = FireWorkflowStage.AimConfirmed;
                    targetId = TargetHazardBase;
                    passStep = "aim";
                    successFeedback = "PASS — AIM: Aim confirmed at base of fire! Squeeze handle to discharge CO2.";
                    break;

                case FireWorkflowStage.AimConfirmed:
                    expectedAction = ActionSqueeze;
                    nextStage = FireWorkflowStage.HandleSqueezed;
                    targetId = TargetExtinguisherHandle;
                    passStep = "squeeze";
                    successFeedback = "PASS — SQUEEZE: Handle pressed! CO2 discharging. Sweep side to side across fire base.";
                    break;

                case FireWorkflowStage.HandleSqueezed:
                    expectedAction = ActionSweep;
                    nextStage = FireWorkflowStage.ExtinguisherDischarged;
                    targetId = TargetExtinguisherProcedure;
                    passStep = "sweep";
                    successFeedback = "FIRE SUPPRESSED! PASS procedure completed successfully. Proceed to exit.";
                    break;

                default:
                    return false;
            }

            bool isCorrect = string.Equals(normalized, expectedAction, StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                bool isFinalStep = nextStage == FireWorkflowStage.ExtinguisherDischarged;

                emittedEvent = new TrainingEvent
                {
                    ModuleId = ModuleId,
                    ContentVersion = ContentVersion,
                    StepId = StepUseExtinguisher,
                    EventType = isFinalStep ? "procedure_completed" : "procedure_progress",
                    ActionId = isFinalStep ? ActionSweep : normalized,
                    TargetId = targetId,
                    Outcome = "success",
                    Payload =
                    {
                        { "rule_id", RuleUseExtinguisher },
                        { "action_id", isFinalStep ? ActionSweep : normalized },
                        { "target_id", targetId },
                        { "pass_step", passStep },
                        { "outcome", "success" }
                    }
                };

                dispatcher?.Dispatch(emittedEvent);
                SetStage(nextStage);
                OnFeedbackChanged?.Invoke(successFeedback);
                return true;
            }
            else
            {
                // Wrong action or out-of-order action
                emittedEvent = new TrainingEvent
                {
                    ModuleId = ModuleId,
                    ContentVersion = ContentVersion,
                    StepId = StepUseExtinguisher,
                    EventType = "procedure_failed",
                    ActionId = rawAction,
                    TargetId = TargetExtinguisherProcedure,
                    Outcome = "failure",
                    Payload =
                    {
                        { "rule_id", RuleUseExtinguisher },
                        { "action_id", rawAction },
                        { "target_id", TargetExtinguisherProcedure },
                        { "expected_action", expectedAction },
                        { "error", "out_of_order" },
                        { "outcome", "failure" }
                    }
                };

                dispatcher?.Dispatch(emittedEvent);

                string failFeedback = GetFailureFeedback(expectedAction, normalized);
                OnFeedbackChanged?.Invoke(failFeedback);
                return false;
            }
        }

        private string GetFailureFeedback(string expectedAction, string actualAction)
        {
            if (expectedAction == ActionPullPin)
            {
                return "SAFETY PIN LOCKED: You must PULL the safety pin first before aiming or squeezing!";
            }
            if (expectedAction == ActionAim)
            {
                return "UNSAFE DISCHARGE: You must AIM nozzle at the base of the fire before squeezing handle!";
            }
            if (expectedAction == ActionSqueeze)
            {
                return "NO DISCHARGE: SQUEEZE the handle to release extinguishing agent before sweeping!";
            }
            if (expectedAction == ActionSweep)
            {
                return "INCOMPLETE SUPPRESSION: SWEEP side-to-side across the base of the fire to extinguish!";
            }
            return "Incorrect action. Follow P.A.S.S. procedure: Pull, Aim, Squeeze, Sweep.";
        }

        public bool SubmitPullPin(ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
            => SubmitExtinguisherAction(ActionPullPin, dispatcher, out emittedEvent);

        public bool SubmitAim(ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
            => SubmitExtinguisherAction(ActionAim, dispatcher, out emittedEvent);

        public bool SubmitSqueeze(ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
            => SubmitExtinguisherAction(ActionSqueeze, dispatcher, out emittedEvent);

        public bool SubmitSweep(ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
            => SubmitExtinguisherAction(ActionSweep, dispatcher, out emittedEvent);

        /// <summary>
        /// Submits the emergency exit identification action.
        /// </summary>
        public bool SubmitIdentifyExit(string targetId, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
            => SubmitIdentifyExit(targetId, ActionMark, dispatcher, out emittedEvent);

        /// <summary>
        /// Submits the emergency exit identification action with explicit action ID.
        /// Validates that Step 6 is complete and target matches designated safe exit.
        /// Advances workflow on success; emits failure event and remains in stage on incorrect target.
        /// Prevents duplicate completion events.
        /// </summary>
        public bool SubmitIdentifyExit(string targetId, string actionId, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;

            bool isStep6Complete = CurrentStage == FireWorkflowStage.ExtinguisherDischarged
                || CurrentStage == FireWorkflowStage.AwaitingExitIdentification;

            if (!isStep6Complete)
            {
                // Premature action or duplicate completion rejected; emit no event
                return false;
            }

            string rawAction = actionId?.Trim() ?? string.Empty;
            string normalizedAction = rawAction.ToLowerInvariant();
            if (string.IsNullOrEmpty(normalizedAction)) normalizedAction = ActionMark;

            bool isCorrectAction = string.Equals(normalizedAction, ActionMark, StringComparison.OrdinalIgnoreCase);
            bool isCorrectTarget = string.Equals(targetId, TargetExitEmergencySectorB, StringComparison.OrdinalIgnoreCase);
            bool isSuccess = isCorrectAction && isCorrectTarget;

            string outcome = isSuccess ? "success" : "failure";

            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepIdentifyExit,
                EventType = "exit_marked",
                ActionId = isCorrectAction ? ActionMark : normalizedAction,
                TargetId = targetId,
                Outcome = outcome,
                Payload =
                {
                    { "rule_id", RuleIdentifyExit },
                    { "action_id", isCorrectAction ? ActionMark : normalizedAction },
                    { "target_id", targetId },
                    { "location_type", LocationTypeEmergencyExit },
                    { "outcome", outcome }
                }
            };

            if (!isSuccess)
            {
                string warning;
                if (string.Equals(targetId, TargetExitFreightElevator, StringComparison.OrdinalIgnoreCase))
                {
                    warning = "CRITICAL HAZARD: Do NOT use elevators during a fire! Power failure or shaft chimney entrapment risk.";
                    emittedEvent.Payload["hazard_warning"] = "elevator_entrapment_risk";
                }
                else if (string.Equals(targetId, TargetExitBlockedCorridor, StringComparison.OrdinalIgnoreCase))
                {
                    warning = "UNSAFE ROUTE: This corridor is compromised by smoke and heat. Seek the designated emergency exit.";
                    emittedEvent.Payload["hazard_warning"] = "smoke_blocked_corridor";
                }
                else
                {
                    warning = "INCORRECT ROUTE: Locate and mark the designated illuminated green Emergency Exit.";
                    emittedEvent.Payload["hazard_warning"] = "unmarked_or_invalid_exit";
                }

                dispatcher?.Dispatch(emittedEvent);
                SetStage(FireWorkflowStage.AwaitingExitIdentification);
                OnFeedbackChanged?.Invoke(warning);
                return false;
            }

            // Success case: advance to ExitIdentified (CurrentStepId automatically sets to step_evacuate_route)
            dispatcher?.Dispatch(emittedEvent);
            SetStage(FireWorkflowStage.ExitIdentified);
            OnFeedbackChanged?.Invoke("EMERGENCY EXIT IDENTIFIED!\nSector B Exit Marked. Clear egress route verified.");
            return true;
        }

        /// <summary>
        /// Submits an evacuation route waypoint along the designated safe path.
        /// Enforces sequential order: waypoint_main_corridor -> waypoint_bypass_crosscut -> waypoint_fire_door_exit.
        /// </summary>
        public bool SubmitEvacuationWaypoint(string waypointId, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;

            bool isInStep8 = CurrentStage == FireWorkflowStage.ExitIdentified
                || CurrentStage == FireWorkflowStage.AwaitingEvacuationRoute
                || CurrentStage == FireWorkflowStage.WaypointMainCorridorReached
                || CurrentStage == FireWorkflowStage.WaypointBypassCrosscutReached;

            if (!isInStep8)
            {
                // Premature action (before Step 7 complete) or duplicate action after completion
                return false;
            }

            string rawId = waypointId?.Trim() ?? string.Empty;

            // Check dangerous smoke-filled corridor hazard
            if (string.Equals(rawId, HazardSmokeCorridor, StringComparison.OrdinalIgnoreCase)
                || rawId.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                emittedEvent = new TrainingEvent
                {
                    ModuleId = ModuleId,
                    ContentVersion = ContentVersion,
                    StepId = StepEvacuateRoute,
                    EventType = "evacuation_sequence_submitted",
                    ActionId = ActionSubmitSequence,
                    TargetId = rawId,
                    Outcome = "failure",
                    Payload =
                    {
                        { "rule_id", RuleEvacuateRoute },
                        { "action_id", ActionSubmitSequence },
                        { "target_id", rawId },
                        { "outcome", "failure" },
                        { "hazard_warning", DecisionAvoidSmoke }
                    }
                };

                dispatcher?.Dispatch(emittedEvent);
                OnFeedbackChanged?.Invoke("CRITICAL HAZARD: Dense toxic smoke detected! Do not proceed through this corridor.");
                return false;
            }

            // Sequential order validation
            if (CurrentStage == FireWorkflowStage.ExitIdentified || CurrentStage == FireWorkflowStage.AwaitingEvacuationRoute)
            {
                if (string.Equals(rawId, WaypointMainCorridor, StringComparison.OrdinalIgnoreCase))
                {
                    emittedEvent = new TrainingEvent
                    {
                        ModuleId = ModuleId,
                        ContentVersion = ContentVersion,
                        StepId = StepEvacuateRoute,
                        EventType = "evacuation_sequence_submitted",
                        ActionId = ActionSubmitSequence,
                        TargetId = WaypointMainCorridor,
                        Outcome = "success",
                        Payload =
                        {
                            { "rule_id", RuleEvacuateRoute },
                            { "action_id", ActionSubmitSequence },
                            { "target_id", WaypointMainCorridor },
                            { "outcome", "success" },
                            { "waypoint_index", "1" }
                        }
                    };

                    dispatcher?.Dispatch(emittedEvent);
                    SetStage(FireWorkflowStage.WaypointMainCorridorReached);
                    OnFeedbackChanged?.Invoke("WAYPOINT 1 REACHED: Proceed toward emergency exit waypoints.");
                    return true;
                }
            }
            else if (CurrentStage == FireWorkflowStage.WaypointMainCorridorReached)
            {
                // Route branch A: canonical direct exit waypoint (waypoint_main_corridor -> waypoint_by_exit -> step_reach_assembly)
                if (string.Equals(rawId, WaypointByExit, StringComparison.OrdinalIgnoreCase))
                {
                    emittedEvent = new TrainingEvent
                    {
                        ModuleId = ModuleId,
                        ContentVersion = ContentVersion,
                        StepId = StepEvacuateRoute,
                        EventType = "evacuation_sequence_submitted",
                        ActionId = ActionSubmitSequence,
                        TargetId = WaypointByExit,
                        Outcome = "success",
                        Payload =
                        {
                            { "rule_id", RuleEvacuateRoute },
                            { "action_id", ActionSubmitSequence },
                            { "target_id", WaypointByExit },
                            { "ordered_ids", $"{WaypointMainCorridor},{WaypointByExit}" },
                            { "outcome", "success" }
                        }
                    };

                    dispatcher?.Dispatch(emittedEvent);
                    SetStage(FireWorkflowStage.RouteEvacuated);
                    OnFeedbackChanged?.Invoke("EVACUATION ROUTE COMPLETED!\nSafe egress confirmed. Proceed to Assembly Muster Point.");
                    return true;
                }

                // Route branch B: bypass crosscut (waypoint_main_corridor -> waypoint_bypass_crosscut -> waypoint_fire_door_exit)
                if (string.Equals(rawId, WaypointBypassCrosscut, StringComparison.OrdinalIgnoreCase))
                {
                    emittedEvent = new TrainingEvent
                    {
                        ModuleId = ModuleId,
                        ContentVersion = ContentVersion,
                        StepId = StepEvacuateRoute,
                        EventType = "evacuation_sequence_submitted",
                        ActionId = ActionSubmitSequence,
                        TargetId = WaypointBypassCrosscut,
                        Outcome = "success",
                        Payload =
                        {
                            { "rule_id", RuleEvacuateRoute },
                            { "action_id", ActionSubmitSequence },
                            { "target_id", WaypointBypassCrosscut },
                            { "outcome", "success" },
                            { "waypoint_index", "2" }
                        }
                    };

                    dispatcher?.Dispatch(emittedEvent);
                    SetStage(FireWorkflowStage.WaypointBypassCrosscutReached);
                    OnFeedbackChanged?.Invoke("WAYPOINT 2 REACHED: Proceed through Fire Door Exit toward assembly point.");
                    return true;
                }
            }
            else if (CurrentStage == FireWorkflowStage.WaypointBypassCrosscutReached)
            {
                if (string.Equals(rawId, WaypointFireDoorExit, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(rawId, WaypointByExit, StringComparison.OrdinalIgnoreCase))
                {
                    emittedEvent = new TrainingEvent
                    {
                        ModuleId = ModuleId,
                        ContentVersion = ContentVersion,
                        StepId = StepEvacuateRoute,
                        EventType = "evacuation_sequence_submitted",
                        ActionId = ActionSubmitSequence,
                        TargetId = WaypointFireDoorExit,
                        Outcome = "success",
                        Payload =
                        {
                            { "rule_id", RuleEvacuateRoute },
                            { "action_id", ActionSubmitSequence },
                            { "target_id", WaypointFireDoorExit },
                            { "ordered_ids", $"{WaypointMainCorridor},{WaypointBypassCrosscut},{WaypointFireDoorExit}" },
                            { "outcome", "success" }
                        }
                    };

                    dispatcher?.Dispatch(emittedEvent);
                    SetStage(FireWorkflowStage.RouteEvacuated);
                    OnFeedbackChanged?.Invoke("EVACUATION ROUTE COMPLETED!\nSafe egress confirmed. Proceed to Assembly Muster Point.");
                    return true;
                }
            }

            // Out-of-order or invalid waypoint
            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepEvacuateRoute,
                EventType = "evacuation_sequence_submitted",
                ActionId = ActionSubmitSequence,
                TargetId = rawId,
                Outcome = "failure",
                Payload =
                {
                    { "rule_id", RuleEvacuateRoute },
                    { "action_id", ActionSubmitSequence },
                    { "target_id", rawId },
                    { "outcome", "failure" },
                    { "error", "out_of_order" }
                }
            };

            dispatcher?.Dispatch(emittedEvent);
            OnFeedbackChanged?.Invoke("SEQUENCE ERROR: Follow emergency route waypoints in sequential order.");
            return false;
        }

        /// <summary>
        /// Submits an ordered list of waypoints representing the evacuation sequence.
        /// </summary>
        public bool SubmitEvacuationSequence(System.Collections.Generic.IList<string> waypointIds, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;

            bool isInStep8 = CurrentStage == FireWorkflowStage.ExitIdentified
                || CurrentStage == FireWorkflowStage.AwaitingEvacuationRoute
                || CurrentStage == FireWorkflowStage.WaypointMainCorridorReached
                || CurrentStage == FireWorkflowStage.WaypointBypassCrosscutReached;

            if (!isInStep8)
            {
                return false;
            }

            if (waypointIds == null || waypointIds.Count < 2)
            {
                emittedEvent = new TrainingEvent
                {
                    ModuleId = ModuleId,
                    ContentVersion = ContentVersion,
                    StepId = StepEvacuateRoute,
                    EventType = "evacuation_sequence_submitted",
                    ActionId = ActionSubmitSequence,
                    Outcome = "failure",
                    Payload =
                    {
                        { "rule_id", RuleEvacuateRoute },
                        { "action_id", ActionSubmitSequence },
                        { "outcome", "failure" },
                        { "error", "incomplete_sequence" }
                    }
                };
                dispatcher?.Dispatch(emittedEvent);
                OnFeedbackChanged?.Invoke("INCOMPLETE ROUTE: All evacuation waypoints must be traversed.");
                return false;
            }

            bool allValid = false;
            string targetId = WaypointFireDoorExit;
            string orderedIds = $"{WaypointMainCorridor},{WaypointBypassCrosscut},{WaypointFireDoorExit}";

            if (waypointIds.Count == 2)
            {
                bool v0 = string.Equals(waypointIds[0]?.Trim(), WaypointMainCorridor, StringComparison.OrdinalIgnoreCase);
                bool v1 = string.Equals(waypointIds[1]?.Trim(), WaypointByExit, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(waypointIds[1]?.Trim(), WaypointFireDoorExit, StringComparison.OrdinalIgnoreCase);
                allValid = v0 && v1;
                targetId = waypointIds[1]?.Trim();
                orderedIds = $"{WaypointMainCorridor},{targetId}";
            }
            else
            {
                bool valid0 = string.Equals(waypointIds[0]?.Trim(), WaypointMainCorridor, StringComparison.OrdinalIgnoreCase);
                bool valid1 = string.Equals(waypointIds[1]?.Trim(), WaypointBypassCrosscut, StringComparison.OrdinalIgnoreCase);
                bool valid2 = string.Equals(waypointIds[2]?.Trim(), WaypointFireDoorExit, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(waypointIds[2]?.Trim(), WaypointByExit, StringComparison.OrdinalIgnoreCase);

                allValid = valid0 && valid1 && valid2;
                targetId = waypointIds[2]?.Trim();
                orderedIds = $"{WaypointMainCorridor},{WaypointBypassCrosscut},{targetId}";
            }

            string outcome = allValid ? "success" : "failure";

            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepEvacuateRoute,
                EventType = "evacuation_sequence_submitted",
                ActionId = ActionSubmitSequence,
                TargetId = targetId,
                Outcome = outcome,
                Payload =
                {
                    { "rule_id", RuleEvacuateRoute },
                    { "action_id", ActionSubmitSequence },
                    { "target_id", targetId },
                    { "ordered_ids", orderedIds },
                    { "outcome", outcome }
                }
            };

            dispatcher?.Dispatch(emittedEvent);

            if (allValid)
            {
                SetStage(FireWorkflowStage.RouteEvacuated);
                OnFeedbackChanged?.Invoke("EVACUATION ROUTE COMPLETED!\nSafe egress confirmed. Proceed to Assembly Muster Point.");
                return true;
            }
            else
            {
                OnFeedbackChanged?.Invoke("SEQUENCE ERROR: Follow emergency route waypoints in sequential order.");
                return false;
            }
        }

        /// <summary>
        /// Submits identification and arrival at the designated emergency assembly muster point.
        /// </summary>
        /// <param name="targetId">The selected assembly point target ID (e.g. assembly_muster_point_alpha).</param>
        /// <param name="actionId">The action identifier (defaults to complete_step).</param>
        /// <param name="dispatcher">The training event dispatcher.</param>
        /// <param name="emittedEvent">The generated training event, or null if rejected.</param>
        /// <returns>True if designated assembly point identified; false if incorrect or premature.</returns>
        public bool SubmitReachAssemblyPoint(string targetId, string actionId, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;

            bool isReadyForStep9 = CurrentStage == FireWorkflowStage.RouteEvacuated
                || CurrentStage == FireWorkflowStage.AwaitingAssemblyPoint;

            if (!isReadyForStep9)
            {
                // Premature action (before Step 8 completion) or duplicate action after completion
                return false;
            }

            string rawTarget = targetId?.Trim() ?? string.Empty;
            string actId = string.IsNullOrEmpty(actionId) ? ActionCompleteStep : actionId.Trim();

            bool isDesignated = string.Equals(rawTarget, TargetAssemblyMusterPoint, StringComparison.OrdinalIgnoreCase);
            string outcome = isDesignated ? "success" : "failure";

            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepReachAssembly,
                EventType = EventTypeAssemblyReached,
                ActionId = actId,
                TargetId = rawTarget,
                Outcome = outcome,
                Payload =
                {
                    { "rule_id", RuleReachAssembly },
                    { "action_id", actId },
                    { "target_id", rawTarget },
                    { "outcome", outcome }
                }
            };

            if (isDesignated)
            {
                emittedEvent.Payload["zone_type"] = ZoneTypeEmergencyAssembly;
                dispatcher?.Dispatch(emittedEvent);
                SetStage(FireWorkflowStage.AssemblyPointReached);
                OnFeedbackChanged?.Invoke("ASSEMBLY POINT REACHED!\nWorker safely evacuated to Assembly Muster Point Alpha. Fire response training completed.");
                EvaluateAssessment(dispatcher);
                return true;
            }
            else
            {
                emittedEvent.Payload["error"] = "wrong_assembly_point";
                dispatcher?.Dispatch(emittedEvent);
                OnFeedbackChanged?.Invoke("WRONG ASSEMBLY POINT — Move to the designated emergency assembly area.");
                return false;
            }
        }

        public bool SubmitReachAssemblyPoint(string targetId, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
            => SubmitReachAssemblyPoint(targetId, ActionCompleteStep, dispatcher, out emittedEvent);

        /// <summary>
        /// Evaluates the session using the dispatched events and stores the finalized TrainingAttempt.
        /// Ensures evaluation happens exactly once per completed training run.
        /// </summary>
        public AssessmentResult EvaluateAssessment(ITrainingEventDispatcher dispatcher, RubricDefinition rubric = null)
        {
            if (CurrentStage != FireWorkflowStage.AssemblyPointReached)
            {
                return null;
            }

            if (IsAssessmentCompleted)
            {
                return LatestAssessment;
            }

            IEnumerable<TrainingEvent> events = null;
            if (dispatcher is TrainingEventBus bus)
            {
                events = bus.DispatchedEvents;
            }
            else if (TrainingEventBus.Instance != null && TrainingEventBus.Instance.DispatchedEvents.Count > 0)
            {
                events = TrainingEventBus.Instance.DispatchedEvents;
            }
            else
            {
                events = new List<TrainingEvent>();
            }

            return EvaluateAssessment(events, rubric ?? BoundRubric);
        }

        /// <summary>
        /// Explicit overload allowing evaluation of a provided event sequence directly.
        /// </summary>
        public AssessmentResult EvaluateAssessment(IEnumerable<TrainingEvent> events, RubricDefinition rubric = null)
        {
            if (CurrentStage != FireWorkflowStage.AssemblyPointReached)
            {
                return null;
            }

            if (IsAssessmentCompleted)
            {
                return LatestAssessment;
            }

            if (rubric == null)
            {
                rubric = BoundRubric ?? RubricLoader.LoadFireExplosionRubric();
            }

            var eventList = events != null ? new List<TrainingEvent>(events) : new List<TrainingEvent>();
            var assessmentResult = LocalAssessmentEngine.Evaluate(eventList, rubric);

            var attempt = new TrainingAttempt
            {
                SchemaVersion = rubric.SchemaVersion ?? "1.0.0",
                ClientAttemptId = Guid.NewGuid().ToString(),
                WorkerId = !string.IsNullOrEmpty(WorkerId) ? WorkerId : DefaultOfflineWorkerId,
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StartedAt = !string.IsNullOrEmpty(SessionStartedAt) ? SessionStartedAt : DateTime.UtcNow.ToString("o"),
                CompletedAt = DateTime.UtcNow.ToString("o"),
                Status = TrainingAttempt.StatusCompleted,
                ClientScore = assessmentResult.ClientScore,
                Passed = assessmentResult.Passed,
                Events = eventList
            };

            attempt.Complete(assessmentResult.ClientScore, assessmentResult.Passed);

            assessmentResult.Attempt = attempt;
            LatestAttempt = attempt;
            LatestAssessment = assessmentResult;

            OnAssessmentCompleted?.Invoke(attempt, assessmentResult);
            return assessmentResult;
        }

        /// <summary>
        /// Finalizes the completed TrainingAttempt for offline outbox storage/synchronization.
        /// Dispatches an outbox domain event and raises the OnAttemptFinalizedForOutbox hook.
        /// Strictly idempotent: rejects premature calls, null attempts, and duplicate invocations.
        /// </summary>
        /// <param name="dispatcher">The training event dispatcher capturing domain events.</param>
        /// <param name="finalizedAttempt">The finalized TrainingAttempt, or null if rejected.</param>
        /// <returns>True if finalized successfully; false if premature, null, or already finalized.</returns>
        public bool FinalizeAttemptForOutbox(ITrainingEventDispatcher dispatcher, out TrainingAttempt finalizedAttempt)
        {
            finalizedAttempt = null;

            // Reject premature or null attempts
            if (!IsAssessmentCompleted || LatestAttempt == null || LatestAssessment == null)
            {
                return false;
            }

            // Reject duplicate finalizations (Idempotency protection)
            if (IsAttemptFinalizedForOutbox)
            {
                return false;
            }

            finalizedAttempt = LatestAttempt;
            IsAttemptFinalizedForOutbox = true;

            if (finalizedAttempt.Status != TrainingAttempt.StatusCompleted)
            {
                finalizedAttempt.Status = TrainingAttempt.StatusCompleted;
            }

            if (string.IsNullOrEmpty(finalizedAttempt.CompletedAt))
            {
                finalizedAttempt.CompletedAt = DateTime.UtcNow.ToString("o");
            }

            var outboxEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepReachAssembly,
                EventType = EventTypeAttemptFinalized,
                ActionId = ActionFinalizeSession,
                TargetId = finalizedAttempt.ClientAttemptId,
                Outcome = finalizedAttempt.Passed ? "pass" : "fail",
                Payload =
                {
                    { "client_attempt_id", finalizedAttempt.ClientAttemptId },
                    { "worker_id", finalizedAttempt.WorkerId ?? DefaultOfflineWorkerId },
                    { "module_id", finalizedAttempt.ModuleId ?? ModuleId },
                    { "content_version", finalizedAttempt.ContentVersion ?? ContentVersion },
                    { "score", finalizedAttempt.ClientScore.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) },
                    { "passed", finalizedAttempt.Passed ? "true" : "false" },
                    { "status", finalizedAttempt.Status },
                    { "completed_at", finalizedAttempt.CompletedAt ?? string.Empty }
                }
            };

            dispatcher?.Dispatch(outboxEvent);

            if (finalizedAttempt.Events != null && !finalizedAttempt.Events.Contains(outboxEvent))
            {
                finalizedAttempt.Events.Add(outboxEvent);
            }

            OnAttemptFinalizedForOutbox?.Invoke(finalizedAttempt);
            return true;
        }

        public bool FinalizeAttemptForOutbox(ITrainingEventDispatcher dispatcher)
            => FinalizeAttemptForOutbox(dispatcher, out _);

        /// <summary>
        /// Resets the training workflow and clears previous attempt state for a retake.
        /// </summary>
        public void Reset()
        {
            CurrentStage = FireWorkflowStage.NotStarted;
            CurrentStepId = StepDetectHazard;
            LatestAttempt = null;
            LatestAssessment = null;
            SessionStartedAt = DateTime.UtcNow.ToString("o");
            IsAttemptFinalizedForOutbox = false;
        }

        public string GetFeedbackForStage(FireWorkflowStage stage)
        {
            switch (stage)
            {
                case FireWorkflowStage.WaitingForTracking:
                    return "Searching for surfaces... Move phone slowly.";
                case FireWorkflowStage.ReadyToPlace:
                    return "Tap on a surface to place the Fire Hazard.";
                case FireWorkflowStage.HazardPlaced:
                    return "Hazard Located! Tap the hazard to confirm detection.";
                case FireWorkflowStage.HazardDetected:
                case FireWorkflowStage.AwaitingIdentification:
                    return "Hazard detected! Identify the hazard classification.";
                case FireWorkflowStage.HazardIdentified:
                case FireWorkflowStage.AwaitingAlarm:
                    return "Hazard Identified: Class E Electrical Fire. Raise the emergency alarm!";
                case FireWorkflowStage.AlarmRaised:
                case FireWorkflowStage.AwaitingExtinguisherSelection:
                    return "Alarm Active! Select the appropriate extinguisher for this Class E electrical fire.";
                case FireWorkflowStage.ExtinguisherSelected:
                case FireWorkflowStage.AwaitingSafeDistance:
                    return "Maintain Safe Distance: Standoff at least 2m outside the red danger zone.";
                case FireWorkflowStage.SafeDistanceMaintained:
                    return "STEP 6: USE EXTINGUISHER — PASS: Pull the safety pin to unlock handle.";
                case FireWorkflowStage.PinPulled:
                    return "STEP 6: USE EXTINGUISHER — AIM: Aim nozzle at the base of the fire.";
                case FireWorkflowStage.AimConfirmed:
                    return "STEP 6: USE EXTINGUISHER — SQUEEZE: Press the handle to discharge CO2.";
                case FireWorkflowStage.HandleSqueezed:
                    return "STEP 6: USE EXTINGUISHER — SWEEP: Move nozzle side to side across fire base.";
                case FireWorkflowStage.ExtinguisherDischarged:
                case FireWorkflowStage.AwaitingExitIdentification:
                    return "STEP 7: IDENTIFY EMERGENCY EXIT\nLocate and tap the green illuminated Emergency Exit sign in AR space.";
                case FireWorkflowStage.ExitIdentified:
                case FireWorkflowStage.AwaitingEvacuationRoute:
                    return "STEP 8: EVACUATION ROUTE\nFollow the green route waypoints away from fire hazard.";
                case FireWorkflowStage.WaypointMainCorridorReached:
                    return "WAYPOINT 1 REACHED: Proceed through Bypass Crosscut to avoid smoke.";
                case FireWorkflowStage.WaypointBypassCrosscutReached:
                    return "WAYPOINT 2 REACHED: Proceed through Fire Door Exit toward assembly point.";
                case FireWorkflowStage.RouteEvacuated:
                case FireWorkflowStage.AwaitingAssemblyPoint:
                    return "STEP 9: REACH ASSEMBLY POINT — Follow the evacuation route and identify the designated assembly point.";
                case FireWorkflowStage.AssemblyPointReached:
                    return "ASSEMBLY POINT REACHED!\nWorker safely evacuated to Assembly Muster Point Alpha. Fire response training completed.";
                default:
                    return string.Empty;
            }
        }
    }
}
