// FireTrainingWorkflow.cs
// Namespace : IndustrialSafetyAR.Modules.FireExplosion
//
// Pure C# domain workflow managing training stages, step transitions, and
// domain event emission for the Fire & Explosion Response module.
// Fully decoupled from Unity runtime for isolated unit testing.

using System;
using IndustrialSafetyAR.Core.Events;

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
        ProcedureCompleted = ExtinguisherDischarged
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

        public FireWorkflowStage CurrentStage { get; private set; } = FireWorkflowStage.NotStarted;
        public string CurrentStepId { get; private set; } = StepDetectHazard;

        public event Action<FireWorkflowStage> OnStageChanged;
        public event Action<string> OnFeedbackChanged;

        public void SetStage(FireWorkflowStage stage)
        {
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
                    CurrentStepId = StepIdentifyExit;
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
                    return "FIRE SUPPRESSED! PASS procedure successfully completed. Proceed to exit.";
                default:
                    return string.Empty;
            }
        }
    }
}
