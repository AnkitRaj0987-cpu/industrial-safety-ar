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
        AlarmRaised
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
            return true;
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
                    return "Emergency Alarm Activated! Siren sounding, response team alerted.";
                default:
                    return string.Empty;
            }
        }
    }
}
