// GasTrainingEventTests.cs
// Namespace : IndustrialSafetyAR.Tests
//
// Standalone unit tests validating the decoupled Gas Leak & Confined Space Safety training module:
// - Step 1: Recognize Gas Hazard (step_gas_recognize_hazard / rule_hazard_recognition)
// - Step 2: Recognize Danger Zone (step_gas_danger_zone / rule_danger_zone)
// - Step 3: Atmospheric Testing (step_gas_atmospheric_test / rule_atmospheric_test - OSHA O2 -> LEL -> H2S order)
// - Step 4: Select PPE (step_gas_select_ppe / rule_select_ppe - rejection of dust/surgical masks)
// - Step 5: Verify PPE (step_gas_verify_ppe / rule_verify_ppe)
// - Step 6: Buddy / Attendant System (step_gas_buddy_system / rule_buddy_system)
// - Step 7: Safe Entry Decision (step_gas_entry_decision / rule_entry_decision - DO NOT ENTER)
// - Step 8: Emergency Response (step_gas_emergency_response / rule_emergency_response - Trained Rescue Only)
// - Step 9: Final Safety Check (step_gas_final_safety_check / completion & assessment)
// - Scoring rubric, penalty deductions, retake idempotency, and localization compliance.

using System;
using System.Collections.Generic;
using System.IO;
using IndustrialSafetyAR.Assessment;
using IndustrialSafetyAR.Core;
using IndustrialSafetyAR.Core.Events;
using IndustrialSafetyAR.Modules.GasConfinedSpace;
using IndustrialSafetyAR.UI;

namespace IndustrialSafetyAR.Tests
{
    public static class GasTrainingEventTests
    {
        public static bool RunAllTests(out List<string> logMessages)
        {
            logMessages = new List<string>();
            bool allPassed = true;

            if (!RunTest("01_GasModuleLoads_AndMatchesSchema", Test_01_GasModuleLoads_AndMatchesSchema, logMessages)) allPassed = false;
            if (!RunTest("02_CorrectHazardRecognition_EmitsEvent", Test_02_CorrectHazardRecognition_EmitsEvent, logMessages)) allPassed = false;
            if (!RunTest("03_PrematureOrInvalidHazardRecognition_Rejected", Test_03_PrematureOrInvalidHazardRecognition_Rejected, logMessages)) allPassed = false;
            if (!RunTest("04_DangerZoneRecognized_EmitsEvent", Test_04_DangerZoneRecognized_EmitsEvent, logMessages)) allPassed = false;
            if (!RunTest("05_UnsafeZoneEntry_Penalized", Test_05_UnsafeZoneEntry_Penalized, logMessages)) allPassed = false;
            if (!RunTest("06_AtmosphericTest_StartsCorrectly", Test_06_AtmosphericTest_StartsCorrectly, logMessages)) allPassed = false;
            if (!RunTest("07_AtmosphericTest_OxygenMustComeFirst", Test_07_AtmosphericTest_OxygenMustComeFirst, logMessages)) allPassed = false;
            if (!RunTest("08_AtmosphericTest_FlammableMustComeSecond", Test_08_AtmosphericTest_FlammableMustComeSecond, logMessages)) allPassed = false;
            if (!RunTest("09_AtmosphericTest_ToxicMustComeThird", Test_09_AtmosphericTest_ToxicMustComeThird, logMessages)) allPassed = false;
            if (!RunTest("10_AtmosphericTest_AssessmentCompleted_UnsafeCondition", Test_10_AtmosphericTest_AssessmentCompleted_UnsafeCondition, logMessages)) allPassed = false;
            if (!RunTest("11_PpeSelection_SuccessfulWithRequiredKit", Test_11_PpeSelection_SuccessfulWithRequiredKit, logMessages)) allPassed = false;
            if (!RunTest("12_PpeSelection_DangerousDistractors_RejectedWithPenalty", Test_12_PpeSelection_DangerousDistractors_RejectedWithPenalty, logMessages)) allPassed = false;
            if (!RunTest("13_PpeSelection_IncompleteSelection_Rejected", Test_13_PpeSelection_IncompleteSelection_Rejected, logMessages)) allPassed = false;
            if (!RunTest("14_PpeVerification_SealAndInspection_Success", Test_14_PpeVerification_SealAndInspection_Success, logMessages)) allPassed = false;
            if (!RunTest("15_PpeVerification_FailedChecks_EmitsFailurePenalty", Test_15_PpeVerification_FailedChecks_EmitsFailurePenalty, logMessages)) allPassed = false;
            if (!RunTest("16_AttendantAssignment_StationedOutside", Test_16_AttendantAssignment_StationedOutside, logMessages)) allPassed = false;
            if (!RunTest("17_CommunicationCheck_RadioProtocol", Test_17_CommunicationCheck_RadioProtocol, logMessages)) allPassed = false;
            if (!RunTest("18_EntryDecision_UnsafeAtmosphere_DoNotEnter_Correct", Test_18_EntryDecision_UnsafeAtmosphere_DoNotEnter_Correct, logMessages)) allPassed = false;
            if (!RunTest("19_EntryDecision_UnsafeAtmosphere_EnterAttempt_SeverelyPenalized", Test_19_EntryDecision_UnsafeAtmosphere_EnterAttempt_SeverelyPenalized, logMessages)) allPassed = false;
            if (!RunTest("20_EmergencyAlarm_Acknowledged", Test_20_EmergencyAlarm_Acknowledged, logMessages)) allPassed = false;
            if (!RunTest("21_EmergencyResponse_Started_NoImprovisedRescue", Test_21_EmergencyResponse_Started_NoImprovisedRescue, logMessages)) allPassed = false;
            if (!RunTest("22_SafeArea_ReachedUpwind", Test_22_SafeArea_ReachedUpwind, logMessages)) allPassed = false;
            if (!RunTest("23_EmergencyProcedure_CompletedWithTrainedTeam", Test_23_EmergencyProcedure_CompletedWithTrainedTeam, logMessages)) allPassed = false;
            if (!RunTest("24_FinalSafetyCheck_CompleteGasTraining", Test_24_FinalSafetyCheck_CompleteGasTraining, logMessages)) allPassed = false;
            if (!RunTest("25_PerfectRun_Scores100_AndPasses", Test_25_PerfectRun_Scores100_AndPasses, logMessages)) allPassed = false;
            if (!RunTest("26_FailingScenario_UnsafeEntry_FailsAssessment", Test_26_FailingScenario_UnsafeEntry_FailsAssessment, logMessages)) allPassed = false;
            if (!RunTest("27_Retake_ResetsAllStateIdempotently", Test_27_Retake_ResetsAllStateIdempotently, logMessages)) allPassed = false;
            if (!RunTest("28_OutboxFinalization_StrictlyIdempotent", Test_28_OutboxFinalization_StrictlyIdempotent, logMessages)) allPassed = false;
            if (!RunTest("29_LocalizationKeys_ExistAcrossAllRequiredLocales", Test_29_LocalizationKeys_ExistAcrossAllRequiredLocales, logMessages)) allPassed = false;
            if (!RunTest("30_GuidedStepNavigator_GasCurriculum", Test_30_GuidedStepNavigator_GasCurriculum, logMessages)) allPassed = false;

            return allPassed;
        }

        private static bool RunTest(string testName, Action testMethod, List<string> logs)
        {
            try
            {
                testMethod();
                logs.Add($"[PASS] {testName}");
                return true;
            }
            catch (Exception ex)
            {
                logs.Add($"[FAIL] {testName}: {ex.Message}");
                return false;
            }
        }

        public static void Test_01_GasModuleLoads_AndMatchesSchema()
        {
            var rubric = RubricLoader.LoadGasConfinedSpaceRubric();
            if (rubric == null) throw new Exception("Gas rubric failed to load from RubricLoader.");
            if (rubric.ModuleId != "gas-confined-space") throw new Exception($"ModuleId mismatch: expected 'gas-confined-space', got '{rubric.ModuleId}'");
            if (rubric.ContentVersion != "1.0.0") throw new Exception($"ContentVersion mismatch: expected '1.0.0', got '{rubric.ContentVersion}'");
            if (rubric.PassPercent != 70.00f) throw new Exception($"PassPercent mismatch: expected 70.00, got {rubric.PassPercent}");
            if (rubric.MaxScore != 100.00f) throw new Exception($"MaxScore mismatch: expected 100.00, got {rubric.MaxScore}");
            if (rubric.Rules == null || rubric.Rules.Count != 8) throw new Exception($"Expected 8 rules in gas rubric, found {rubric.Rules?.Count}");

            float sumPoints = 0f;
            foreach (var rule in rubric.Rules)
            {
                sumPoints += rule.Points;
            }
            if (Math.Abs(sumPoints - 100.00f) > 0.01f)
            {
                throw new Exception($"Total points in rules must sum to 100.00, got {sumPoints}");
            }
        }

        public static void Test_02_CorrectHazardRecognition_EmitsEvent()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            workflow.StartWorkflow();

            bool success = workflow.RecognizeHazard(bus, out var evt);
            if (!success) throw new Exception("RecognizeHazard failed.");
            if (evt == null) throw new Exception("Emitted event is null.");
            if (evt.EventType != "gas_hazard_recognized") throw new Exception($"EventType mismatch: expected 'gas_hazard_recognized', got '{evt.EventType}'");
            if (evt.ModuleId != "gas-confined-space") throw new Exception("ModuleId mismatch");
            if (evt.StepId != "step_gas_recognize_hazard") throw new Exception("StepId mismatch");
            if (evt.Outcome != "success") throw new Exception("Outcome mismatch");
            if (workflow.CurrentStepId != "step_gas_danger_zone") throw new Exception("Workflow did not advance to step_gas_danger_zone.");
        }

        public static void Test_03_PrematureOrInvalidHazardRecognition_Rejected()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            workflow.StartWorkflow();
            workflow.RecognizeHazard(bus, out _);

            // Duplicate call should be rejected
            bool duplicate = workflow.RecognizeHazard(bus, out var duplicateEvt);
            if (duplicate) throw new Exception("Duplicate hazard recognition should be rejected.");
            if (duplicateEvt != null) throw new Exception("Duplicate event should be null.");
        }

        public static void Test_04_DangerZoneRecognized_EmitsEvent()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            workflow.StartWorkflow();
            workflow.RecognizeHazard(bus, out _);

            bool success = workflow.MarkDangerZone(bus, out var evt);
            if (!success) throw new Exception("MarkDangerZone failed.");
            if (evt == null) throw new Exception("Emitted event is null.");
            if (evt.EventType != "danger_zone_recognized") throw new Exception($"EventType mismatch: '{evt.EventType}'");
            if (evt.StepId != "step_gas_danger_zone") throw new Exception("StepId mismatch");
            if (workflow.CurrentStepId != "step_gas_atmospheric_test") throw new Exception("Did not advance to atmospheric test step.");
        }

        public static void Test_05_UnsafeZoneEntry_Penalized()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            workflow.StartWorkflow();
            workflow.RecognizeHazard(bus, out _);

            bool breachRecorded = workflow.RecordUnsafeZoneEntry(bus, out var evt);
            if (!breachRecorded) throw new Exception("RecordUnsafeZoneEntry failed.");
            if (evt.EventType != "unsafe_zone_entry") throw new Exception("EventType must be 'unsafe_zone_entry'");
            if (evt.Outcome != "failure") throw new Exception("Outcome must be 'failure'");
            // Must stay at danger zone step to complete properly
            if (workflow.CurrentStepId != "step_gas_danger_zone") throw new Exception("Unsafe zone entry should not advance step.");
        }

        public static void Test_06_AtmosphericTest_StartsCorrectly()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            workflow.StartWorkflow();
            workflow.RecognizeHazard(bus, out _);
            workflow.MarkDangerZone(bus, out _);

            bool started = workflow.StartAtmosphericTest(bus, out var evt);
            if (!started) throw new Exception("StartAtmosphericTest failed.");
            if (evt.EventType != "atmosphere_test_started") throw new Exception($"EventType mismatch: '{evt.EventType}'");
            if (evt.StepId != "step_gas_atmospheric_test") throw new Exception("StepId mismatch");
        }

        public static void Test_07_AtmosphericTest_OxygenMustComeFirst()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            workflow.StartWorkflow();
            workflow.RecognizeHazard(bus, out _);
            workflow.MarkDangerZone(bus, out _);
            workflow.StartAtmosphericTest(bus, out _);

            // Attempt testing Flammable before Oxygen
            bool flammableFirst = workflow.TestSensor(GasSensorType.Flammable, bus, out _, out string reason1);
            if (flammableFirst) throw new Exception("Testing Flammable before Oxygen should be rejected.");
            if (string.IsNullOrEmpty(reason1) || !reason1.Contains("SEQUENCE VIOLATION")) throw new Exception("Expected sequence violation message for Flammable first.");

            // Attempt testing Toxic before Oxygen
            bool toxicFirst = workflow.TestSensor(GasSensorType.Toxic, bus, out _, out string reason2);
            if (toxicFirst) throw new Exception("Testing Toxic before Oxygen should be rejected.");

            // Testing Oxygen succeeds
            bool oxygenFirst = workflow.TestSensor(GasSensorType.Oxygen, bus, out var o2Evt, out _);
            if (!oxygenFirst) throw new Exception("Testing Oxygen first failed.");
            if (o2Evt.EventType != "atmosphere_test_step_completed") throw new Exception("Expected atmosphere_test_step_completed");
            if (o2Evt.GetPayloadValue("gas_type") != "oxygen") throw new Exception("Expected gas_type oxygen");
        }

        public static void Test_08_AtmosphericTest_FlammableMustComeSecond()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            workflow.StartWorkflow();
            workflow.RecognizeHazard(bus, out _);
            workflow.MarkDangerZone(bus, out _);
            workflow.StartAtmosphericTest(bus, out _);
            workflow.TestSensor(GasSensorType.Oxygen, bus, out _, out _);

            // Attempt testing Toxic before Flammable
            bool toxicSecond = workflow.TestSensor(GasSensorType.Toxic, bus, out _, out string reason);
            if (toxicSecond) throw new Exception("Testing Toxic before Flammable should be rejected.");
            if (string.IsNullOrEmpty(reason) || !reason.Contains("SEQUENCE VIOLATION")) throw new Exception("Expected sequence violation message.");

            // Testing Flammable second succeeds
            bool flammableSecond = workflow.TestSensor(GasSensorType.Flammable, bus, out var lelEvt, out _);
            if (!flammableSecond) throw new Exception("Testing Flammable second failed.");
            if (lelEvt.GetPayloadValue("gas_type") != "flammable") throw new Exception("Expected gas_type flammable");
        }

        public static void Test_09_AtmosphericTest_ToxicMustComeThird()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            workflow.StartWorkflow();
            workflow.RecognizeHazard(bus, out _);
            workflow.MarkDangerZone(bus, out _);
            workflow.StartAtmosphericTest(bus, out _);
            workflow.TestSensor(GasSensorType.Oxygen, bus, out _, out _);
            workflow.TestSensor(GasSensorType.Flammable, bus, out _, out _);

            bool toxicThird = workflow.TestSensor(GasSensorType.Toxic, bus, out var toxicEvt, out _);
            if (!toxicThird) throw new Exception("Testing Toxic third failed.");
            if (toxicEvt.GetPayloadValue("gas_type") != "toxic") throw new Exception("Expected gas_type toxic");
            if (!workflow.AtmosphericSimulator.IsAssessmentCompleted) throw new Exception("Simulator should be marked assessment complete.");
        }

        public static void Test_10_AtmosphericTest_AssessmentCompleted_UnsafeCondition()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            workflow.StartWorkflow();
            workflow.RecognizeHazard(bus, out _);
            workflow.MarkDangerZone(bus, out _);
            workflow.StartAtmosphericTest(bus, out _);
            workflow.TestSensor(GasSensorType.Oxygen, bus, out _, out _);
            workflow.TestSensor(GasSensorType.Flammable, bus, out _, out _);
            workflow.TestSensor(GasSensorType.Toxic, bus, out _, out _);

            bool completed = workflow.CompleteAtmosphericAssessment(bus, out var evt);
            if (!completed) throw new Exception("CompleteAtmosphericAssessment failed.");
            if (evt.EventType != "atmosphere_assessment_completed") throw new Exception("EventType mismatch");
            if (evt.GetPayloadValue("overall_status") != "unsafe") throw new Exception("Overall status must be 'unsafe'");
            if (evt.GetPayloadValue("hazard_detected") != "true") throw new Exception("Hazard detected must be 'true'");
            if (workflow.CurrentStepId != "step_gas_select_ppe") throw new Exception("Did not advance to step_gas_select_ppe");
        }

        public static void Test_11_PpeSelection_SuccessfulWithRequiredKit()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteSteps1To3(workflow, bus);

            var validKit = new[]
            {
                GasPpeSystem.ItemHelmet,
                GasPpeSystem.ItemHarness,
                GasPpeSystem.ItemGloves,
                GasPpeSystem.ItemBoots,
                GasPpeSystem.ItemScba
            };

            bool success = workflow.SubmitPpeSelection(validKit, bus, out var evt, out var feedback);
            if (!success) throw new Exception($"SubmitPpeSelection failed: {feedback}");
            if (evt.EventType != "ppe_selected") throw new Exception("EventType must be 'ppe_selected'");
            if (evt.Outcome != "success") throw new Exception("Outcome must be 'success'");
            if (workflow.CurrentStepId != "step_gas_verify_ppe") throw new Exception("Did not advance to step_gas_verify_ppe");
        }

        public static void Test_12_PpeSelection_DangerousDistractors_RejectedWithPenalty()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteSteps1To3(workflow, bus);

            var invalidKit = new[]
            {
                GasPpeSystem.ItemHelmet,
                GasPpeSystem.ItemDustMask // Inappropriate distractor
            };

            bool success = workflow.SubmitPpeSelection(invalidKit, bus, out var evt, out var feedback);
            if (success) throw new Exception("Dust mask selection should have been rejected.");
            if (evt == null || evt.EventType != "ppe_selection_incorrect") throw new Exception("Expected ppe_selection_incorrect penalty event.");
            if (evt.Outcome != "failure") throw new Exception("Outcome must be 'failure'");
        }

        public static void Test_13_PpeSelection_IncompleteSelection_Rejected()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteSteps1To3(workflow, bus);

            var incompleteKit = new[]
            {
                GasPpeSystem.ItemHelmet,
                GasPpeSystem.ItemHarness
                // Missing gloves, boots, scba
            };

            bool success = workflow.SubmitPpeSelection(incompleteKit, bus, out _, out var feedback);
            if (success) throw new Exception("Incomplete PPE selection should be rejected.");
            if (string.IsNullOrEmpty(feedback) || !feedback.Contains("Incomplete")) throw new Exception("Expected Incomplete feedback message.");
        }

        public static void Test_14_PpeVerification_SealAndInspection_Success()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteSteps1To4(workflow, bus);

            bool verified = workflow.VerifyPpe(sealCheckPassed: true, harnessFitPassed: true, cylinderPressurePassed: true, bus, out var evt, out _);
            if (!verified) throw new Exception("VerifyPpe failed.");
            if (evt.EventType != "ppe_verified") throw new Exception("Expected ppe_verified event.");
            if (evt.Outcome != "success") throw new Exception("Outcome must be 'success'");
            if (workflow.CurrentStepId != "step_gas_buddy_system") throw new Exception("Did not advance to step_gas_buddy_system");
        }

        public static void Test_15_PpeVerification_FailedChecks_EmitsFailurePenalty()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteSteps1To4(workflow, bus);

            bool verified = workflow.VerifyPpe(sealCheckPassed: false, harnessFitPassed: true, cylinderPressurePassed: true, bus, out var evt, out _);
            if (verified) throw new Exception("Failed seal check should not verify.");
            if (evt == null || evt.EventType != "ppe_verification_failed") throw new Exception("Expected ppe_verification_failed event.");
            if (evt.Outcome != "failure") throw new Exception("Outcome must be 'failure'");
        }

        public static void Test_16_AttendantAssignment_StationedOutside()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteSteps1To5(workflow, bus);

            bool assigned = workflow.AssignAttendant("standby_attendant_vikram", bus, out var evt);
            if (!assigned) throw new Exception("AssignAttendant failed.");
            if (evt.EventType != "attendant_assigned") throw new Exception("Expected attendant_assigned event.");
            if (evt.GetPayloadValue("attendant_location") != "outside_confined_space") throw new Exception("Attendant must be stationed outside space.");
        }

        public static void Test_17_CommunicationCheck_RadioProtocol()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteSteps1To5(workflow, bus);
            workflow.AssignAttendant("standby_attendant_vikram", bus, out _);

            bool commChecked = workflow.CheckCommunication("intrinsically_safe_two_way_radio", bus, out var evt);
            if (!commChecked) throw new Exception("CheckCommunication failed.");
            if (evt.EventType != "communication_checked") throw new Exception("Expected communication_checked event.");
            if (workflow.CurrentStepId != "step_gas_entry_decision") throw new Exception("Did not advance to step_gas_entry_decision");
        }

        public static void Test_18_EntryDecision_UnsafeAtmosphere_DoNotEnter_Correct()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteSteps1To6(workflow, bus);

            // In simulated unsafe conditions, DO NOT ENTER is the strictly correct safety action
            bool success = workflow.SubmitEntryDecision(GasTrainingWorkflow.DecisionDoNotEnter, bus, out var evt);
            if (!success) throw new Exception("Submitting DO NOT ENTER should succeed.");
            if (evt.EventType != "safe_entry_decision") throw new Exception("Expected safe_entry_decision event.");
            if (evt.Outcome != "success") throw new Exception("Outcome must be 'success'");
            if (workflow.CurrentStepId != "step_gas_emergency_response") throw new Exception("Did not advance to step_gas_emergency_response");
        }

        public static void Test_19_EntryDecision_UnsafeAtmosphere_EnterAttempt_SeverelyPenalized()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteSteps1To6(workflow, bus);

            // Attempting to enter unsafe atmosphere must be rejected and heavily penalized
            bool success = workflow.SubmitEntryDecision(GasTrainingWorkflow.DecisionEnterSpace, bus, out var evt);
            if (success) throw new Exception("Attempting to enter hazardous atmosphere must not succeed!");
            if (evt == null || evt.EventType != "unsafe_entry_attempt") throw new Exception("Expected unsafe_entry_attempt penalty event.");
            if (evt.Outcome != "failure") throw new Exception("Outcome must be 'failure'");
        }

        public static void Test_20_EmergencyAlarm_Acknowledged()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteSteps1To7(workflow, bus);

            bool ack = workflow.AcknowledgeGasAlarm(bus, out var evt);
            if (!ack) throw new Exception("AcknowledgeGasAlarm failed.");
            if (evt.EventType != "gas_alarm_acknowledged") throw new Exception("Expected gas_alarm_acknowledged event.");
        }

        public static void Test_21_EmergencyResponse_Started_NoImprovisedRescue()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteSteps1To7(workflow, bus);
            workflow.AcknowledgeGasAlarm(bus, out _);

            bool started = workflow.StartEmergencyResponse(bus, out var evt);
            if (!started) throw new Exception("StartEmergencyResponse failed.");
            if (evt.EventType != "emergency_response_started") throw new Exception("Expected emergency_response_started event.");
            if (evt.GetPayloadValue("rescue_protocol") != "trained_rescue_only_no_entry") throw new Exception("Rescue protocol must enforce trained rescue only.");
        }

        public static void Test_22_SafeArea_ReachedUpwind()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteSteps1To7(workflow, bus);
            workflow.AcknowledgeGasAlarm(bus, out _);
            workflow.StartEmergencyResponse(bus, out _);

            bool reached = workflow.ReachSafeArea("safe_muster_area_upwind", bus, out var evt);
            if (!reached) throw new Exception("ReachSafeArea failed.");
            if (evt.EventType != "safe_area_reached") throw new Exception("Expected safe_area_reached event.");
            if (evt.GetPayloadValue("evacuation_direction") != "upwind") throw new Exception("Evacuation direction must be 'upwind'.");
        }

        public static void Test_23_EmergencyProcedure_CompletedWithTrainedTeam()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteSteps1To7(workflow, bus);
            workflow.AcknowledgeGasAlarm(bus, out _);
            workflow.StartEmergencyResponse(bus, out _);
            workflow.ReachSafeArea("safe_muster_area_upwind", bus, out _);

            bool completed = workflow.CompleteEmergencyProcedure("alert_emergency_supervisor", bus, out var evt);
            if (!completed) throw new Exception("CompleteEmergencyProcedure failed.");
            if (evt.EventType != "emergency_procedure_completed") throw new Exception("Expected emergency_procedure_completed event.");
            if (workflow.CurrentStepId != "step_gas_final_safety_check") throw new Exception("Did not advance to step_gas_final_safety_check");
        }

        public static void Test_24_FinalSafetyCheck_CompleteGasTraining()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteSteps1To8(workflow, bus);

            bool finished = workflow.CompleteGasTraining(bus, out var evt);
            if (!finished) throw new Exception("CompleteGasTraining failed.");
            if (evt.EventType != "gas_training_completed") throw new Exception("Expected gas_training_completed event.");
            if (!workflow.IsAssessmentCompleted) throw new Exception("Assessment should be completed.");
            if (workflow.LatestAttempt == null) throw new Exception("LatestAttempt should not be null.");
        }

        public static void Test_25_PerfectRun_Scores100_AndPasses()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteFullPerfectRun(workflow, bus);

            var assessment = workflow.LatestAssessment;
            if (assessment == null) throw new Exception("Assessment is null after full run.");
            if (assessment.ClientScore != 100.00f) throw new Exception($"Expected 100.00 points, got {assessment.ClientScore}");
            if (!assessment.Passed) throw new Exception("Perfect run should pass.");
            if (assessment.TotalPenalties != 0.00f) throw new Exception($"Expected 0 penalties, got {assessment.TotalPenalties}");

            var vm = AssessmentSummaryViewModel.Build(workflow.LatestAttempt, assessment);
            if (vm.ClientScore != 100.00f) throw new Exception("ViewModel score mismatch.");
            if (!vm.Passed) throw new Exception("ViewModel should be passed.");
            if (vm.StepSummaries.Count != 9) throw new Exception($"Expected 9 step summaries, got {vm.StepSummaries.Count}");
        }

        public static void Test_26_FailingScenario_UnsafeEntry_FailsAssessment()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();

            // Execute up to Step 6
            ExecuteSteps1To6(workflow, bus);

            // Worker commits unsafe entry attempt!
            workflow.SubmitEntryDecision(GasTrainingWorkflow.DecisionEnterSpace, bus, out _);

            // Even if they subsequently do emergency response and complete:
            workflow.SetStage(GasWorkflowStage.EntryDecisionMade);
            workflow.AcknowledgeGasAlarm(bus, out _);
            workflow.StartEmergencyResponse(bus, out _);
            workflow.ReachSafeArea("safe_muster_area_upwind", bus, out _);
            workflow.CompleteEmergencyProcedure("alert_emergency_supervisor", bus, out _);
            workflow.CompleteGasTraining(bus, out _);

            var assessment = workflow.LatestAssessment;
            if (assessment == null) throw new Exception("Assessment is null.");
            // Rule 7 was failed with 15 pt penalty and never satisfied:
            var rule7 = assessment.GetRuleResult("rule_entry_decision");
            if (rule7 == null || rule7.IsSatisfied) throw new Exception("Rule 7 should not be satisfied.");
            if (rule7.PenaltyDeducted != 15.00f) throw new Exception($"Rule 7 penalty expected 15.00, got {rule7.PenaltyDeducted}");
            if (assessment.Passed) throw new Exception("Worker who entered unsafe atmosphere must NOT pass!");
        }

        public static void Test_27_Retake_ResetsAllStateIdempotently()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteFullPerfectRun(workflow, bus);

            string initialAttemptId = workflow.LatestAttempt.ClientAttemptId;

            // Reset workflow for retake
            workflow.Reset();
            if (workflow.CurrentStage != GasWorkflowStage.NotStarted) throw new Exception("Stage should be NotStarted.");
            if (workflow.CurrentStepId != "step_gas_recognize_hazard") throw new Exception("StepId should be step_gas_recognize_hazard.");
            if (workflow.LatestAttempt != null) throw new Exception("LatestAttempt should be null after reset.");
            if (workflow.LatestAssessment != null) throw new Exception("LatestAssessment should be null after reset.");
            if (workflow.IsAssessmentCompleted) throw new Exception("IsAssessmentCompleted should be false.");
            if (workflow.IsAttendantAssigned) throw new Exception("IsAttendantAssigned should be false.");
            if (workflow.AtmosphericSimulator.IsTestStarted) throw new Exception("Simulator should be reset.");
            if (workflow.PpeSystem.IsPpeSelected) throw new Exception("PPE system should be reset.");

            // Perform second run
            bus.Clear();
            ExecuteFullPerfectRun(workflow, bus);

            string retakeAttemptId = workflow.LatestAttempt.ClientAttemptId;
            if (retakeAttemptId == initialAttemptId) throw new Exception("Retake must generate a fresh unique ClientAttemptId.");
            if (!workflow.LatestAssessment.Passed) throw new Exception("Retake run should pass.");
        }

        public static void Test_28_OutboxFinalization_StrictlyIdempotent()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new GasTrainingWorkflow();
            ExecuteFullPerfectRun(workflow, bus);

            // First finalization succeeds
            bool firstFinalized = workflow.FinalizeAttemptForOutbox(bus, out var attempt1);
            if (!firstFinalized) throw new Exception("First finalization failed.");
            if (attempt1 == null) throw new Exception("Finalized attempt is null.");
            if (attempt1.Status != TrainingAttempt.StatusCompleted) throw new Exception("Status should be completed.");

            // Second finalization call rejected (Idempotency protection)
            bool duplicateFinalized = workflow.FinalizeAttemptForOutbox(bus, out var attempt2);
            if (duplicateFinalized) throw new Exception("Duplicate finalization must be rejected.");
            if (attempt2 != null) throw new Exception("Duplicate finalized attempt should be null.");
        }

        public static void Test_29_LocalizationKeys_ExistAcrossAllRequiredLocales()
        {
            var service = LocaleService.Instance;
            var requiredLocales = new[] { LocaleService.LangEnglish, LocaleService.LangHindi, LocaleService.LangSantali };

            var requiredKeys = new[]
            {
                "gas_header_title",
                "gas_step1_title", "gas_step1_prompt", "gas_step1_next",
                "gas_step2_title", "gas_step2_prompt", "gas_step2_next",
                "gas_step3_title", "gas_step3_prompt", "gas_step3_next",
                "gas_step4_title", "gas_step4_prompt", "gas_step4_next",
                "gas_step5_title", "gas_step5_prompt", "gas_step5_next",
                "gas_step6_title", "gas_step6_prompt", "gas_step6_next",
                "gas_step7_title", "gas_step7_prompt", "gas_step7_next",
                "gas_step8_title", "gas_step8_prompt", "gas_step8_next",
                "gas_step9_title", "gas_step9_prompt", "gas_step9_next"
            };

            foreach (var locale in requiredLocales)
            {
                var catalog = service.GetCatalogForLanguage(locale);
                if (catalog == null) throw new Exception($"Catalog for locale '{locale}' not found.");

                foreach (var key in requiredKeys)
                {
                    if (!catalog.TryGetValue(key, out string val) || string.IsNullOrWhiteSpace(val))
                    {
                        throw new Exception($"Missing key '{key}' in locale '{locale}'.");
                    }
                }
            }

            // Verify German or other locales are NOT added
            if (service.SupportedLanguages.Count != 3)
            {
                throw new Exception($"Expected exactly 3 supported languages (en, hi, sat), found {service.SupportedLanguages.Count}");
            }
            foreach (var lang in service.SupportedLanguages)
            {
                if (lang != "en" && lang != "hi" && lang != "sat")
                {
                    throw new Exception($"Unauthorized language code detected: '{lang}'");
                }
            }
        }

        public static void Test_30_GuidedStepNavigator_GasCurriculum()
        {
            var navigator = new GuidedStepNavigator();
            navigator.InitializeDefaultGasSteps();

            if (navigator.TotalSteps != 9) throw new Exception($"Expected 9 steps, got {navigator.TotalSteps}");
            if (navigator.ModuleId != "gas-confined-space") throw new Exception($"ModuleId mismatch: '{navigator.ModuleId}'");

            string step1Title = navigator.GetStepTitle(1);
            if (string.IsNullOrEmpty(step1Title) || !step1Title.Contains("HAZARD"))
            {
                throw new Exception($"Step 1 title unexpected: '{step1Title}'");
            }

            string step7Title = navigator.GetStepTitle(7);
            if (string.IsNullOrEmpty(step7Title) || !step7Title.Contains("ENTRY"))
            {
                throw new Exception($"Step 7 title unexpected: '{step7Title}'");
            }
        }

        // =============================================================
        // HELPER SEQUENCE EXECUTIONS
        // =============================================================
        private static void ExecuteSteps1To3(GasTrainingWorkflow workflow, TrainingEventBus bus)
        {
            workflow.StartWorkflow();
            workflow.RecognizeHazard(bus, out _);
            workflow.MarkDangerZone(bus, out _);
            workflow.StartAtmosphericTest(bus, out _);
            workflow.TestSensor(GasSensorType.Oxygen, bus, out _, out _);
            workflow.TestSensor(GasSensorType.Flammable, bus, out _, out _);
            workflow.TestSensor(GasSensorType.Toxic, bus, out _, out _);
            workflow.CompleteAtmosphericAssessment(bus, out _);
        }

        private static void ExecuteSteps1To4(GasTrainingWorkflow workflow, TrainingEventBus bus)
        {
            ExecuteSteps1To3(workflow, bus);
            var validKit = new[]
            {
                GasPpeSystem.ItemHelmet,
                GasPpeSystem.ItemHarness,
                GasPpeSystem.ItemGloves,
                GasPpeSystem.ItemBoots,
                GasPpeSystem.ItemScba
            };
            workflow.SubmitPpeSelection(validKit, bus, out _, out _);
        }

        private static void ExecuteSteps1To5(GasTrainingWorkflow workflow, TrainingEventBus bus)
        {
            ExecuteSteps1To4(workflow, bus);
            workflow.VerifyPpe(sealCheckPassed: true, harnessFitPassed: true, cylinderPressurePassed: true, bus, out _, out _);
        }

        private static void ExecuteSteps1To6(GasTrainingWorkflow workflow, TrainingEventBus bus)
        {
            ExecuteSteps1To5(workflow, bus);
            workflow.AssignAttendant("standby_attendant_vikram", bus, out _);
            workflow.CheckCommunication("intrinsically_safe_two_way_radio", bus, out _);
        }

        private static void ExecuteSteps1To7(GasTrainingWorkflow workflow, TrainingEventBus bus)
        {
            ExecuteSteps1To6(workflow, bus);
            workflow.SubmitEntryDecision(GasTrainingWorkflow.DecisionDoNotEnter, bus, out _);
        }

        private static void ExecuteSteps1To8(GasTrainingWorkflow workflow, TrainingEventBus bus)
        {
            ExecuteSteps1To7(workflow, bus);
            workflow.AcknowledgeGasAlarm(bus, out _);
            workflow.StartEmergencyResponse(bus, out _);
            workflow.ReachSafeArea("safe_muster_area_upwind", bus, out _);
            workflow.CompleteEmergencyProcedure("alert_emergency_supervisor", bus, out _);
        }

        private static void ExecuteFullPerfectRun(GasTrainingWorkflow workflow, TrainingEventBus bus)
        {
            ExecuteSteps1To8(workflow, bus);
            workflow.CompleteGasTraining(bus, out _);
        }
    }
}
