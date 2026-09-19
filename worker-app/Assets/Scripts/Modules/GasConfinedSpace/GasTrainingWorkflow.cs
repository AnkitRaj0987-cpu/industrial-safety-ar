// GasTrainingWorkflow.cs
// Namespace : IndustrialSafetyAR.Modules.GasConfinedSpace
//
// Pure C# domain state machine enforcing strict step sequencing, domain event emission,
// and safety rubric evaluation for the Gas Leak & Confined Space Safety training scenario.
// Decoupled from Unity runtime for 100% unit-testability without UnityEngine or network dependencies.
//
// SAFETY PRINCIPLES:
// 1. Workers cannot reliably determine atmospheric safety using human senses alone (NIOSH guidance).
// 2. OSHA 29 CFR 1910.146 testing sequence: 1. Oxygen -> 2. Flammable (LEL) -> 3. Toxic contaminants (H2S).
// 3. PPE selection is data-driven; dust masks/surgical masks are strictly prohibited.
// 4. "PPE DOES NOT MAKE AN UNSAFE ATMOSPHERE SAFE. UNSAFE ATMOSPHERE -> DO NOT ENTER."
// 5. Standby attendant remains outside the space at all times with active two-way communication.
// 6. Emergency rescue: ALERT -> ISOLATE/KEEP OUT -> COMMUNICATE -> TRAINED RESCUE RESPONSE.
//    Improvised worker entry into hazardous space for rescue is strictly penalized.

using System;
using System.Collections.Generic;
using IndustrialSafetyAR.Assessment;
using IndustrialSafetyAR.Core.Events;

namespace IndustrialSafetyAR.Modules.GasConfinedSpace
{
    public enum GasWorkflowStage
    {
        NotStarted,
        AwaitingHazardRecognition,
        HazardRecognized,
        AwaitingDangerZone,
        DangerZoneMarked,
        AwaitingAtmosphericTest,
        AtmosphericTestCompleted,
        AwaitingPpeSelection,
        PpeSelected,
        AwaitingPpeVerification,
        PpeVerified,
        AwaitingBuddySystem,
        AttendantAssigned,
        CommunicationChecked,
        AwaitingEntryDecision,
        EntryDecisionMade,
        GasAlarmAcknowledged,
        EmergencyResponseStarted,
        SafeAreaReached,
        EmergencyProcedureCompleted,
        FinalSafetyCheckCompleted,
        TrainingCompleted
    }

    public class GasTrainingWorkflow
    {
        public const string ModuleId = "gas-confined-space";
        public const string ContentVersion = "1.0.0";
        public const string DefaultOfflineWorkerId = "worker_offline_default";

        // Step 1: Recognize Hazard
        public const string StepRecognizeHazard = "step_gas_recognize_hazard";
        public const string ActionRecognizeHazard = "hazard_recognized";
        public const string TargetGasConfinedSpace = "hazard_gas_confined_space";
        public const string RuleHazardRecognition = "rule_hazard_recognition";

        // Step 2: Recognize Danger Zone
        public const string StepDangerZone = "step_gas_danger_zone";
        public const string ActionMarkPerimeter = "perimeter_marked";
        public const string ActionZoneBreach = "zone_breach";
        public const string TargetDangerZonePerimeter = "danger_zone_perimeter";
        public const string RuleDangerZone = "rule_danger_zone";

        // Step 3: Atmospheric Testing
        public const string StepAtmosphericTest = "step_gas_atmospheric_test";
        public const string RuleAtmosphericTest = "rule_atmospheric_test";

        // Step 4: Select PPE
        public const string StepSelectPpe = "step_gas_select_ppe";
        public const string RuleSelectPpe = "rule_select_ppe";

        // Step 5: Verify PPE
        public const string StepVerifyPpe = "step_gas_verify_ppe";
        public const string RuleVerifyPpe = "rule_verify_ppe";

        // Step 6: Buddy / Attendant System
        public const string StepBuddySystem = "step_gas_buddy_system";
        public const string ActionAssignAttendant = "assign_attendant";
        public const string ActionCheckCommunication = "check_communication";
        public const string TargetAttendant = "outside_standby_attendant";
        public const string TargetCommunicationRadio = "outside_attendant_radio";
        public const string RuleBuddySystem = "rule_buddy_system";

        // Step 7: Safe Entry Decision
        public const string StepEntryDecision = "step_gas_entry_decision";
        public const string ActionDecideEntry = "decide_entry";
        public const string DecisionDoNotEnter = "do_not_enter";
        public const string DecisionEnterSpace = "enter_confined_space";
        public const string RuleEntryDecision = "rule_entry_decision";

        // Step 8: Emergency Response
        public const string StepEmergencyResponse = "step_gas_emergency_response";
        public const string ActionAcknowledgeAlarm = "acknowledge_gas_alarm";
        public const string ActionStartEmergencyResponse = "start_emergency_response";
        public const string ActionReachSafeArea = "reach_safe_area";
        public const string ActionCompleteEmergency = "emergency_procedure_completed";
        public const string TargetSafeMusterArea = "safe_muster_area_upwind";
        public const string TargetSupervisorAlert = "alert_emergency_supervisor";
        public const string RuleEmergencyResponse = "rule_emergency_response";

        // Step 9: Final Safety Check
        public const string StepFinalSafetyCheck = "step_gas_final_safety_check";
        public const string ActionCompleteTraining = "complete_gas_training";
        public const string TargetComplianceChecklist = "gas_compliance_checklist";

        // Outbox finalization constants
        public const string EventTypeAttemptFinalized = "attempt_finalized";
        public const string ActionFinalizeSession = "finalize_session_outbox";

        public GasWorkflowStage CurrentStage { get; private set; } = GasWorkflowStage.NotStarted;
        public string CurrentStepId { get; private set; } = StepRecognizeHazard;
        public string WorkerId { get; set; } = DefaultOfflineWorkerId;
        public string SessionStartedAt { get; private set; }
        public TrainingAttempt LatestAttempt { get; private set; }
        public AssessmentResult LatestAssessment { get; private set; }
        public bool IsAssessmentCompleted => LatestAttempt != null && LatestAssessment != null;
        public bool IsAttemptFinalizedForOutbox { get; private set; }

        public RubricDefinition BoundRubric { get; private set; }
        public GasAtmosphericSimulator AtmosphericSimulator { get; } = new GasAtmosphericSimulator();
        public GasPpeSystem PpeSystem { get; } = new GasPpeSystem();
        public bool IsPpeSelected => PpeSystem.IsPpeSelected;
        public bool IsPpeVerified => PpeSystem.IsPpeVerified;

        private readonly List<TrainingEvent> _sessionEvents = new List<TrainingEvent>();
        public IReadOnlyList<TrainingEvent> SessionEvents
        {
            get
            {
                lock (_sessionEvents)
                {
                    return _sessionEvents.ToArray();
                }
            }
        }

        private void RecordAndDispatch(ITrainingEventDispatcher dispatcher, TrainingEvent evt)
        {
            if (evt == null) return;
            lock (_sessionEvents)
            {
                _sessionEvents.Add(evt);
            }
            dispatcher?.Dispatch(evt);
        }

        public bool IsAttendantAssigned { get; private set; }
        public bool IsCommunicationChecked { get; private set; }
        public bool IsEntryDecisionMade { get; private set; }
        public string EntryDecisionResult { get; private set; }

        public void ResetWorkflow()
        {
            Reset();
        }

        public event Action<GasWorkflowStage> OnStageChanged;
        public event Action<string> OnFeedbackChanged;
        public event Action<TrainingAttempt, AssessmentResult> OnAssessmentCompleted;
        public event Action<TrainingAttempt> OnAttemptFinalizedForOutbox;

        public GasTrainingWorkflow()
        {
            BindRubric(RubricLoader.LoadGasConfinedSpaceRubric());
        }

        public GasTrainingWorkflow(RubricDefinition rubric)
        {
            BindRubric(rubric ?? RubricLoader.LoadGasConfinedSpaceRubric());
        }

        public GasTrainingWorkflow(IRubricProvider rubricProvider)
        {
            BindRubric(rubricProvider != null
                ? rubricProvider.LoadRubric(ModuleId)
                : RubricLoader.LoadGasConfinedSpaceRubric());
        }

        public void BindRubric(RubricDefinition rubric)
        {
            BoundRubric = rubric ?? RubricLoader.LoadGasConfinedSpaceRubric();
        }

        public void SetStage(GasWorkflowStage stage)
        {
            if (string.IsNullOrEmpty(SessionStartedAt) && stage != GasWorkflowStage.NotStarted)
            {
                SessionStartedAt = DateTime.UtcNow.ToString("o");
            }

            CurrentStage = stage;
            switch (stage)
            {
                case GasWorkflowStage.AwaitingHazardRecognition:
                    CurrentStepId = StepRecognizeHazard;
                    break;
                case GasWorkflowStage.HazardRecognized:
                case GasWorkflowStage.AwaitingDangerZone:
                    CurrentStepId = StepDangerZone;
                    break;
                case GasWorkflowStage.DangerZoneMarked:
                case GasWorkflowStage.AwaitingAtmosphericTest:
                    CurrentStepId = StepAtmosphericTest;
                    break;
                case GasWorkflowStage.AtmosphericTestCompleted:
                case GasWorkflowStage.AwaitingPpeSelection:
                    CurrentStepId = StepSelectPpe;
                    break;
                case GasWorkflowStage.PpeSelected:
                case GasWorkflowStage.AwaitingPpeVerification:
                    CurrentStepId = StepVerifyPpe;
                    break;
                case GasWorkflowStage.PpeVerified:
                case GasWorkflowStage.AwaitingBuddySystem:
                case GasWorkflowStage.AttendantAssigned:
                    CurrentStepId = StepBuddySystem;
                    break;
                case GasWorkflowStage.CommunicationChecked:
                case GasWorkflowStage.AwaitingEntryDecision:
                    CurrentStepId = StepEntryDecision;
                    break;
                case GasWorkflowStage.EntryDecisionMade:
                case GasWorkflowStage.GasAlarmAcknowledged:
                case GasWorkflowStage.EmergencyResponseStarted:
                case GasWorkflowStage.SafeAreaReached:
                    CurrentStepId = StepEmergencyResponse;
                    break;
                case GasWorkflowStage.EmergencyProcedureCompleted:
                case GasWorkflowStage.FinalSafetyCheckCompleted:
                case GasWorkflowStage.TrainingCompleted:
                    CurrentStepId = StepFinalSafetyCheck;
                    break;
            }

            OnStageChanged?.Invoke(stage);
            OnFeedbackChanged?.Invoke(GetFeedbackForStage(stage));
        }

        public void StartWorkflow()
        {
            SetStage(GasWorkflowStage.AwaitingHazardRecognition);
        }

        // =============================================================
        // STEP 1: RECOGNIZE GAS HAZARD
        // =============================================================
        public bool RecognizeHazard(ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != GasWorkflowStage.AwaitingHazardRecognition && CurrentStage != GasWorkflowStage.NotStarted)
            {
                return false;
            }

            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepRecognizeHazard,
                EventType = "gas_hazard_recognized",
                ActionId = ActionRecognizeHazard,
                TargetId = TargetGasConfinedSpace,
                Outcome = "success",
                Payload =
                {
                    { "rule_id", RuleHazardRecognition },
                    { "action_id", ActionRecognizeHazard },
                    { "target_id", TargetGasConfinedSpace },
                    { "hazard_type", "confined_space_gas_accumulation" },
                    { "outcome", "success" }
                }
            };

            RecordAndDispatch(dispatcher, emittedEvent);
            SetStage(GasWorkflowStage.HazardRecognized);
            SetStage(GasWorkflowStage.AwaitingDangerZone);
            return true;
        }

        // =============================================================
        // STEP 2: RECOGNIZE DANGER ZONE
        // =============================================================
        public bool MarkDangerZone(ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != GasWorkflowStage.AwaitingDangerZone && CurrentStage != GasWorkflowStage.HazardRecognized)
            {
                return false;
            }

            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepDangerZone,
                EventType = "danger_zone_recognized",
                ActionId = ActionMarkPerimeter,
                TargetId = TargetDangerZonePerimeter,
                Outcome = "success",
                Payload =
                {
                    { "rule_id", RuleDangerZone },
                    { "action_id", ActionMarkPerimeter },
                    { "target_id", TargetDangerZonePerimeter },
                    { "perimeter_radius_meters", "3.0" },
                    { "standoff_maintained", "true" },
                    { "outcome", "success" }
                }
            };

            RecordAndDispatch(dispatcher, emittedEvent);
            SetStage(GasWorkflowStage.DangerZoneMarked);
            SetStage(GasWorkflowStage.AwaitingAtmosphericTest);
            return true;
        }

        public bool RecordUnsafeZoneEntry(ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != GasWorkflowStage.AwaitingDangerZone && CurrentStage != GasWorkflowStage.HazardRecognized)
            {
                return false;
            }

            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepDangerZone,
                EventType = "unsafe_zone_entry",
                ActionId = ActionZoneBreach,
                TargetId = TargetDangerZonePerimeter,
                Outcome = "failure",
                Payload =
                {
                    { "rule_id", RuleDangerZone },
                    { "action_id", ActionZoneBreach },
                    { "target_id", TargetDangerZonePerimeter },
                    { "error_reason", "Worker approached/entered danger perimeter before atmospheric testing." },
                    { "outcome", "failure" }
                }
            };

            RecordAndDispatch(dispatcher, emittedEvent);
            OnFeedbackChanged?.Invoke("DANGER: Stay outside the hazardous perimeter! Atmospheric testing required before approach.");
            return true;
        }

        // =============================================================
        // STEP 3: ATMOSPHERIC TESTING (OSHA SEQUENCE)
        // =============================================================
        public bool StartAtmosphericTest(ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != GasWorkflowStage.AwaitingAtmosphericTest && CurrentStage != GasWorkflowStage.DangerZoneMarked)
            {
                return false;
            }

            bool res = AtmosphericSimulator.StartTest(ModuleId, ContentVersion, dispatcher, out emittedEvent);
            if (res && emittedEvent != null) { lock (_sessionEvents) _sessionEvents.Add(emittedEvent); }
            return res;
        }

        public bool TestSensor(GasSensorType sensorType, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent, out string rejectionReason)
        {
            emittedEvent = null;
            rejectionReason = null;

            if (CurrentStage != GasWorkflowStage.AwaitingAtmosphericTest && CurrentStage != GasWorkflowStage.DangerZoneMarked)
            {
                rejectionReason = "Not at atmospheric testing step.";
                return false;
            }

            bool res = AtmosphericSimulator.TestSensor(sensorType, ModuleId, ContentVersion, dispatcher, out emittedEvent, out rejectionReason);
            if (res && emittedEvent != null) { lock (_sessionEvents) _sessionEvents.Add(emittedEvent); }
            return res;
        }

        public bool CompleteAtmosphericAssessment(ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != GasWorkflowStage.AwaitingAtmosphericTest && CurrentStage != GasWorkflowStage.DangerZoneMarked)
            {
                return false;
            }

            if (!AtmosphericSimulator.CompleteAssessment(ModuleId, ContentVersion, dispatcher, out emittedEvent))
            {
                return false;
            }

            if (emittedEvent != null) { lock (_sessionEvents) _sessionEvents.Add(emittedEvent); }
            SetStage(GasWorkflowStage.AtmosphericTestCompleted);
            SetStage(GasWorkflowStage.AwaitingPpeSelection);
            return true;
        }

        // =============================================================
        // STEP 4: SELECT PPE
        // =============================================================
        public bool SubmitPpeSelection(IEnumerable<string> items, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent, out string feedback)
        {
            emittedEvent = null;
            feedback = null;

            if (CurrentStage != GasWorkflowStage.AwaitingPpeSelection && CurrentStage != GasWorkflowStage.AtmosphericTestCompleted)
            {
                feedback = "Premature action: Complete atmospheric testing first.";
                return false;
            }

            if (!PpeSystem.SubmitPpeSelection(items, ModuleId, ContentVersion, dispatcher, out emittedEvent, out feedback))
            {
                if (emittedEvent != null) { lock (_sessionEvents) _sessionEvents.Add(emittedEvent); }
                return false;
            }

            if (emittedEvent != null) { lock (_sessionEvents) _sessionEvents.Add(emittedEvent); }
            SetStage(GasWorkflowStage.PpeSelected);
            SetStage(GasWorkflowStage.AwaitingPpeVerification);
            return true;
        }

        // =============================================================
        // STEP 5: VERIFY PPE
        // =============================================================
        public bool VerifyPpe(bool sealCheckPassed, bool harnessFitPassed, bool cylinderPressurePassed, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent, out string feedback)
        {
            emittedEvent = null;
            feedback = null;

            if (CurrentStage != GasWorkflowStage.AwaitingPpeVerification && CurrentStage != GasWorkflowStage.PpeSelected)
            {
                feedback = "Premature action: Complete PPE selection first.";
                return false;
            }

            if (!PpeSystem.VerifyPpe(sealCheckPassed, harnessFitPassed, cylinderPressurePassed, ModuleId, ContentVersion, dispatcher, out emittedEvent, out feedback))
            {
                if (emittedEvent != null) { lock (_sessionEvents) _sessionEvents.Add(emittedEvent); }
                return false;
            }

            if (emittedEvent != null) { lock (_sessionEvents) _sessionEvents.Add(emittedEvent); }
            SetStage(GasWorkflowStage.PpeVerified);
            SetStage(GasWorkflowStage.AwaitingBuddySystem);
            return true;
        }

        // =============================================================
        // STEP 6: BUDDY / ATTENDANT SYSTEM
        // =============================================================
        public bool AssignAttendant(string attendantId, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != GasWorkflowStage.AwaitingBuddySystem && CurrentStage != GasWorkflowStage.PpeVerified)
            {
                return false;
            }

            IsAttendantAssigned = true;
            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepBuddySystem,
                EventType = "attendant_assigned",
                ActionId = ActionAssignAttendant,
                TargetId = !string.IsNullOrEmpty(attendantId) ? attendantId : TargetAttendant,
                Outcome = "success",
                Payload =
                {
                    { "rule_id", RuleBuddySystem },
                    { "action_id", ActionAssignAttendant },
                    { "target_id", !string.IsNullOrEmpty(attendantId) ? attendantId : TargetAttendant },
                    { "attendant_location", "outside_confined_space" },
                    { "outcome", "success" }
                }
            };

            RecordAndDispatch(dispatcher, emittedEvent);
            SetStage(GasWorkflowStage.AttendantAssigned);
            return true;
        }

        public bool CheckCommunication(string protocol, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (!IsAttendantAssigned || CurrentStage != GasWorkflowStage.AttendantAssigned)
            {
                return false;
            }

            IsCommunicationChecked = true;
            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepBuddySystem,
                EventType = "communication_checked",
                ActionId = ActionCheckCommunication,
                TargetId = TargetCommunicationRadio,
                Outcome = "success",
                Payload =
                {
                    { "rule_id", RuleBuddySystem },
                    { "action_id", ActionCheckCommunication },
                    { "target_id", TargetCommunicationRadio },
                    { "protocol", !string.IsNullOrEmpty(protocol) ? protocol : "intrinsically_safe_two_way_radio" },
                    { "outcome", "success" }
                }
            };

            RecordAndDispatch(dispatcher, emittedEvent);
            SetStage(GasWorkflowStage.CommunicationChecked);
            SetStage(GasWorkflowStage.AwaitingEntryDecision);
            return true;
        }

        // =============================================================
        // STEP 7: ENTRY DECISION (UNSAFE -> DO NOT ENTER)
        // =============================================================
        public bool SubmitEntryDecision(string decisionId, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != GasWorkflowStage.AwaitingEntryDecision && CurrentStage != GasWorkflowStage.CommunicationChecked)
            {
                return false;
            }

            bool isDoNotEnter = string.Equals(decisionId, DecisionDoNotEnter, StringComparison.OrdinalIgnoreCase);
            bool isAtmosphereUnsafe = !AtmosphericSimulator.OverallAtmosphereSafe;

            // When simulated conditions are unsafe, correct decision is DO NOT ENTER
            bool isCorrectDecision = isAtmosphereUnsafe ? isDoNotEnter : !isDoNotEnter;

            if (isCorrectDecision)
            {
                IsEntryDecisionMade = true;
                EntryDecisionResult = DecisionDoNotEnter;

                emittedEvent = new TrainingEvent
                {
                    ModuleId = ModuleId,
                    ContentVersion = ContentVersion,
                    StepId = StepEntryDecision,
                    EventType = "safe_entry_decision",
                    ActionId = ActionDecideEntry,
                    TargetId = DecisionDoNotEnter,
                    Outcome = "success",
                    Payload =
                    {
                        { "rule_id", RuleEntryDecision },
                        { "action_id", ActionDecideEntry },
                        { "decision_id", DecisionDoNotEnter },
                        { "atmosphere_state", "unsafe" },
                        { "safety_principle", "PPE_DOES_NOT_OVERRIDE_UNSAFE_ATMOSPHERE" },
                        { "outcome", "success" }
                    }
                };

                RecordAndDispatch(dispatcher, emittedEvent);
                SetStage(GasWorkflowStage.EntryDecisionMade);
                return true;
            }
            else
            {
                // CRITICAL SAFETY VIOLATION: Attempted entry into hazardous atmosphere!
                emittedEvent = new TrainingEvent
                {
                    ModuleId = ModuleId,
                    ContentVersion = ContentVersion,
                    StepId = StepEntryDecision,
                    EventType = "unsafe_entry_attempt",
                    ActionId = ActionDecideEntry,
                    TargetId = DecisionEnterSpace,
                    Outcome = "failure",
                    Payload =
                    {
                        { "rule_id", RuleEntryDecision },
                        { "action_id", ActionDecideEntry },
                        { "decision_id", DecisionEnterSpace },
                        { "atmosphere_state", "unsafe" },
                        { "error_reason", "CRITICAL VIOLATION: Worker attempted entry into hazardous atmosphere! PPE does not override unsafe atmosphere." },
                        { "outcome", "failure" }
                    }
                };

                RecordAndDispatch(dispatcher, emittedEvent);
                OnFeedbackChanged?.Invoke("CRITICAL SAFETY VIOLATION: Atmosphere is hazardous! DO NOT ENTER! Entry prohibited even with PPE.");
                return false;
            }
        }

        // =============================================================
        // STEP 8: EMERGENCY RESPONSE (ALERT -> ISOLATE -> COMMUNICATE)
        // =============================================================
        public bool AcknowledgeGasAlarm(ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != GasWorkflowStage.EntryDecisionMade)
            {
                return false;
            }

            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepEmergencyResponse,
                EventType = "gas_alarm_acknowledged",
                ActionId = ActionAcknowledgeAlarm,
                TargetId = "gas_hazard_alarm_siren",
                Outcome = "success",
                Payload =
                {
                    { "rule_id", RuleEmergencyResponse },
                    { "action_id", ActionAcknowledgeAlarm },
                    { "outcome", "success" }
                }
            };

            RecordAndDispatch(dispatcher, emittedEvent);
            SetStage(GasWorkflowStage.GasAlarmAcknowledged);
            return true;
        }

        public bool StartEmergencyResponse(ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != GasWorkflowStage.GasAlarmAcknowledged)
            {
                return false;
            }

            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepEmergencyResponse,
                EventType = "emergency_response_started",
                ActionId = ActionStartEmergencyResponse,
                TargetId = "site_emergency_protocol",
                Outcome = "success",
                Payload =
                {
                    { "rule_id", RuleEmergencyResponse },
                    { "action_id", ActionStartEmergencyResponse },
                    { "rescue_protocol", "trained_rescue_only_no_entry" },
                    { "outcome", "success" }
                }
            };

            RecordAndDispatch(dispatcher, emittedEvent);
            SetStage(GasWorkflowStage.EmergencyResponseStarted);
            return true;
        }

        public bool ReachSafeArea(string safeAreaId, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != GasWorkflowStage.EmergencyResponseStarted)
            {
                return false;
            }

            string target = !string.IsNullOrEmpty(safeAreaId) ? safeAreaId : TargetSafeMusterArea;
            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepEmergencyResponse,
                EventType = "safe_area_reached",
                ActionId = ActionReachSafeArea,
                TargetId = target,
                Outcome = "success",
                Payload =
                {
                    { "rule_id", RuleEmergencyResponse },
                    { "action_id", ActionReachSafeArea },
                    { "target_id", target },
                    { "evacuation_direction", "upwind" },
                    { "outcome", "success" }
                }
            };

            RecordAndDispatch(dispatcher, emittedEvent);
            SetStage(GasWorkflowStage.SafeAreaReached);
            return true;
        }

        public bool CompleteEmergencyProcedure(string procedureId, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != GasWorkflowStage.SafeAreaReached)
            {
                return false;
            }

            string target = !string.IsNullOrEmpty(procedureId) ? procedureId : TargetSupervisorAlert;
            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepEmergencyResponse,
                EventType = "emergency_procedure_completed",
                ActionId = ActionCompleteEmergency,
                TargetId = target,
                Outcome = "success",
                Payload =
                {
                    { "rule_id", RuleEmergencyResponse },
                    { "action_id", ActionCompleteEmergency },
                    { "target_id", target },
                    { "rescue_type", "trained_emergency_response_team" },
                    { "outcome", "success" }
                }
            };

            RecordAndDispatch(dispatcher, emittedEvent);
            SetStage(GasWorkflowStage.EmergencyProcedureCompleted);
            return true;
        }

        // =============================================================
        // STEP 9: FINAL SAFETY CHECK & EVALUATION
        // =============================================================
        public bool CompleteGasTraining(ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (CurrentStage != GasWorkflowStage.EmergencyProcedureCompleted)
            {
                return false;
            }

            emittedEvent = new TrainingEvent
            {
                ModuleId = ModuleId,
                ContentVersion = ContentVersion,
                StepId = StepFinalSafetyCheck,
                EventType = "gas_training_completed",
                ActionId = ActionCompleteTraining,
                TargetId = TargetComplianceChecklist,
                Outcome = "success",
                Payload =
                {
                    { "action_id", ActionCompleteTraining },
                    { "target_id", TargetComplianceChecklist },
                    { "outcome", "success" }
                }
            };

            RecordAndDispatch(dispatcher, emittedEvent);
            SetStage(GasWorkflowStage.TrainingCompleted);
            EvaluateAssessment(dispatcher);
            return true;
        }

        public AssessmentResult EvaluateAssessment(ITrainingEventDispatcher dispatcher, RubricDefinition rubric = null)
        {
            if (CurrentStage != GasWorkflowStage.TrainingCompleted)
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

            var eventList = new List<TrainingEvent>();
            if (events != null)
            {
                foreach (var evt in events)
                {
                    if (evt != null && (string.IsNullOrEmpty(evt.ModuleId) || string.Equals(evt.ModuleId, ModuleId, StringComparison.OrdinalIgnoreCase)))
                    {
                        eventList.Add(evt);
                    }
                }
            }

            // Fallback to internal recorded session events if dispatcher had 0 module events
            if (eventList.Count == 0 && _sessionEvents.Count > 0)
            {
                lock (_sessionEvents)
                {
                    eventList.AddRange(_sessionEvents);
                }
            }

            return EvaluateAssessment(eventList, rubric ?? BoundRubric);
        }

        public AssessmentResult EvaluateAssessment(IEnumerable<TrainingEvent> events, RubricDefinition rubric = null)
        {
            if (CurrentStage != GasWorkflowStage.TrainingCompleted)
            {
                return null;
            }

            if (IsAssessmentCompleted)
            {
                return LatestAssessment;
            }

            if (rubric == null)
            {
                rubric = BoundRubric ?? RubricLoader.LoadGasConfinedSpaceRubric();
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

        public bool FinalizeAttemptForOutbox(ITrainingEventDispatcher dispatcher, out TrainingAttempt finalizedAttempt)
        {
            finalizedAttempt = null;

            if (!IsAssessmentCompleted || LatestAttempt == null || LatestAssessment == null)
            {
                return false;
            }

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
                StepId = StepFinalSafetyCheck,
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

        public void Reset()
        {
            CurrentStage = GasWorkflowStage.NotStarted;
            CurrentStepId = StepRecognizeHazard;
            LatestAttempt = null;
            LatestAssessment = null;
            SessionStartedAt = DateTime.UtcNow.ToString("o");
            IsAttemptFinalizedForOutbox = false;
            IsAttendantAssigned = false;
            IsCommunicationChecked = false;
            IsEntryDecisionMade = false;
            EntryDecisionResult = null;

            lock (_sessionEvents)
            {
                _sessionEvents.Clear();
            }

            AtmosphericSimulator.Reset();
            PpeSystem.Reset();
        }

        public string GetFeedbackForStage(GasWorkflowStage stage)
        {
            switch (stage)
            {
                case GasWorkflowStage.NotStarted:
                case GasWorkflowStage.AwaitingHazardRecognition:
                    return "Confined space opening located. Identify the gas accumulation hazard.";
                case GasWorkflowStage.HazardRecognized:
                case GasWorkflowStage.AwaitingDangerZone:
                    return "Hazard Recognized! Mark the 3m danger perimeter and remain outside.";
                case GasWorkflowStage.DangerZoneMarked:
                case GasWorkflowStage.AwaitingAtmosphericTest:
                    return "Perimeter Marked. Initiate atmospheric testing: 1. Oxygen -> 2. Flammable -> 3. Toxic.";
                case GasWorkflowStage.AtmosphericTestCompleted:
                case GasWorkflowStage.AwaitingPpeSelection:
                    return "Atmospheric Test Complete: UNSAFE conditions detected. Select protective PPE kit.";
                case GasWorkflowStage.PpeSelected:
                case GasWorkflowStage.AwaitingPpeVerification:
                    return "PPE Selected. Verify equipment seal, harness fit, and SCBA cylinder pressure.";
                case GasWorkflowStage.PpeVerified:
                case GasWorkflowStage.AwaitingBuddySystem:
                    return "PPE Verified. Assign outside standby attendant and test communication radio.";
                case GasWorkflowStage.AttendantAssigned:
                    return "Attendant Stationed Outside. Test two-way communication protocol.";
                case GasWorkflowStage.CommunicationChecked:
                case GasWorkflowStage.AwaitingEntryDecision:
                    return "Communication Confirmed. Atmospheric test is UNSAFE. Decide entry authorization.";
                case GasWorkflowStage.EntryDecisionMade:
                case GasWorkflowStage.GasAlarmAcknowledged:
                    return "Safe Decision: DO NOT ENTER. Gas alarm sounding. Initiate emergency response.";
                case GasWorkflowStage.EmergencyResponseStarted:
                    return "Emergency Activated. Evacuate upwind to safe muster area. Do NOT attempt rescue.";
                case GasWorkflowStage.SafeAreaReached:
                    return "Safe Area Reached. Alert emergency supervisor for trained rescue deployment.";
                case GasWorkflowStage.EmergencyProcedureCompleted:
                case GasWorkflowStage.FinalSafetyCheckCompleted:
                case GasWorkflowStage.TrainingCompleted:
                    return "Emergency protocol completed. Review final compliance checklist.";
                default:
                    return string.Empty;
            }
        }
    }
}
