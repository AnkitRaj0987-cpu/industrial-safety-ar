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
using IndustrialSafetyAR.AR;
using IndustrialSafetyAR.Assessment;
using IndustrialSafetyAR.Core;
using IndustrialSafetyAR.Core.Events;
using IndustrialSafetyAR.Modules.GasConfinedSpace;
using IndustrialSafetyAR.UI;
using UnityEngine;

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
            if (!RunTest("31_GasArInteractionController_Initialization", Test_31_GasArInteractionController_Initialization, logMessages)) allPassed = false;
            if (!RunTest("32_Step1_HazardRecognition_EmitsGasHazardRecognized", Test_32_Step1_HazardRecognition_EmitsGasHazardRecognized, logMessages)) allPassed = false;
            if (!RunTest("33_Step1_IncorrectInteraction_DoesNotAdvance", Test_33_Step1_IncorrectInteraction_DoesNotAdvance, logMessages)) allPassed = false;
            if (!RunTest("34_Step2_DangerZoneRecognized_EmitsEvent", Test_34_Step2_DangerZoneRecognized_EmitsEvent, logMessages)) allPassed = false;
            if (!RunTest("35_Step2_UnsafeZoneEntry_EmitsPenaltyEvent", Test_35_Step2_UnsafeZoneEntry_EmitsPenaltyEvent, logMessages)) allPassed = false;
            if (!RunTest("36_Step3_Sequence_StartsAtOxygen", Test_36_Step3_Sequence_StartsAtOxygen, logMessages)) allPassed = false;
            if (!RunTest("37_Step3_Sequence_OxygenMustCompleteBeforeFlammableUnlocks", Test_37_Step3_Sequence_OxygenMustCompleteBeforeFlammableUnlocks, logMessages)) allPassed = false;
            if (!RunTest("38_Step3_Sequence_FlammableMustCompleteBeforeToxicUnlocks", Test_38_Step3_Sequence_FlammableMustCompleteBeforeToxicUnlocks, logMessages)) allPassed = false;
            if (!RunTest("39_Step3_Sequence_H2sCannotBeTestedBeforeLel", Test_39_Step3_Sequence_H2sCannotBeTestedBeforeLel, logMessages)) allPassed = false;
            if (!RunTest("40_Step3_Readings_ValuesMatchRequired", Test_40_Step3_Readings_ValuesMatchRequired, logMessages)) allPassed = false;
            if (!RunTest("41_Step3_CompletingAllThree_EmitsAtmosphereAssessmentCompleted", Test_41_Step3_CompletingAllThree_EmitsAtmosphereAssessmentCompleted, logMessages)) allPassed = false;
            if (!RunTest("42_Step3_FinalAtmosphericStatus_IsUnsafe", Test_42_Step3_FinalAtmosphericStatus_IsUnsafe, logMessages)) allPassed = false;
            if (!RunTest("43_ArLifecycle_EnabledOnlyWhileGasTrainingActive", Test_43_ArLifecycle_EnabledOnlyWhileGasTrainingActive, logMessages)) allPassed = false;
            if (!RunTest("44_GasInteractionFeedbackUI_HierarchyAndDetectorComponents", Test_44_GasInteractionFeedbackUI_HierarchyAndDetectorComponents, logMessages)) allPassed = false;
            if (!RunTest("45_GasHazardMarker_VisualComponentsAndGeometry", Test_45_GasHazardMarker_VisualComponentsAndGeometry, logMessages)) allPassed = false;
            if (!RunTest("46_Detector_Localization_AllLocalesPresent", Test_46_Detector_Localization_AllLocalesPresent, logMessages)) allPassed = false;
            if (!RunTest("47_Step4_PpeSelection_UIHierarchyAndWarning", Test_47_Step4_PpeSelection_UIHierarchyAndWarning, logMessages)) allPassed = false;
            if (!RunTest("48_Step4_PpeSelection_UnsafeWarningText_MatchesRequirement", Test_48_Step4_PpeSelection_UnsafeWarningText_MatchesRequirement, logMessages)) allPassed = false;
            if (!RunTest("49_Step4_PpeSelection_Distractor_DustMask_EmitsIncorrectAndPenalty", Test_49_Step4_PpeSelection_Distractor_DustMask_EmitsIncorrectAndPenalty, logMessages)) allPassed = false;
            if (!RunTest("50_Step4_PpeSelection_Distractor_SurgicalMask_EmitsIncorrectAndPenalty", Test_50_Step4_PpeSelection_Distractor_SurgicalMask_EmitsIncorrectAndPenalty, logMessages)) allPassed = false;
            if (!RunTest("51_Step4_PpeSelection_Distractor_BlocksProgression", Test_51_Step4_PpeSelection_Distractor_BlocksProgression, logMessages)) allPassed = false;
            if (!RunTest("52_Step4_PpeSelection_CorrectionAllowedAfterDistractor", Test_52_Step4_PpeSelection_CorrectionAllowedAfterDistractor, logMessages)) allPassed = false;
            if (!RunTest("53_Step4_PpeSelection_IncompleteSelection_MissingScba_Fails", Test_53_Step4_PpeSelection_IncompleteSelection_MissingScba_Fails, logMessages)) allPassed = false;
            if (!RunTest("54_Step4_PpeSelection_IncompleteSelection_MissingHarness_Fails", Test_54_Step4_PpeSelection_IncompleteSelection_MissingHarness_Fails, logMessages)) allPassed = false;
            if (!RunTest("55_Step4_PpeSelection_CompleteKit_EmitsPpeSelected", Test_55_Step4_PpeSelection_CompleteKit_EmitsPpeSelected, logMessages)) allPassed = false;
            if (!RunTest("56_Step4_PpeSelection_CanGoNext_Gating", Test_56_Step4_PpeSelection_CanGoNext_Gating, logMessages)) allPassed = false;
            if (!RunTest("57_Step5_PpeVerification_UIHierarchy", Test_57_Step5_PpeVerification_UIHierarchy, logMessages)) allPassed = false;
            if (!RunTest("58_Step5_PpeVerification_InitialState_Unverified", Test_58_Step5_PpeVerification_InitialState_Unverified, logMessages)) allPassed = false;
            if (!RunTest("59_Step5_PpeVerification_IncompleteVerification_EmitsFailurePenalty", Test_59_Step5_PpeVerification_IncompleteVerification_EmitsFailurePenalty, logMessages)) allPassed = false;
            if (!RunTest("60_Step5_PpeVerification_CheckSeal_UpdatesState", Test_60_Step5_PpeVerification_CheckSeal_UpdatesState, logMessages)) allPassed = false;
            if (!RunTest("61_Step5_PpeVerification_CheckHarness_UpdatesState", Test_61_Step5_PpeVerification_CheckHarness_UpdatesState, logMessages)) allPassed = false;
            if (!RunTest("62_Step5_PpeVerification_CheckCylinder_UpdatesState", Test_62_Step5_PpeVerification_CheckCylinder_UpdatesState, logMessages)) allPassed = false;
            if (!RunTest("63_Step5_PpeVerification_CompleteAllThree_EmitsPpeVerified", Test_63_Step5_PpeVerification_CompleteAllThree_EmitsPpeVerified, logMessages)) allPassed = false;
            if (!RunTest("64_Step5_PpeVerification_CanGoNext_Gating", Test_64_Step5_PpeVerification_CanGoNext_Gating, logMessages)) allPassed = false;
            if (!RunTest("65_Step6_BuddySystem_UIHierarchy", Test_65_Step6_BuddySystem_UIHierarchy, logMessages)) allPassed = false;
            if (!RunTest("66_Step6_BuddySystem_AttendantMarker_SpawnAndGeometry", Test_66_Step6_BuddySystem_AttendantMarker_SpawnAndGeometry, logMessages)) allPassed = false;
            if (!RunTest("67_Step6_BuddySystem_AttendantMarker_DistanceValidation_InsideDangerZone_Rejected", Test_67_Step6_BuddySystem_AttendantMarker_DistanceValidation_InsideDangerZone_Rejected, logMessages)) allPassed = false;
            if (!RunTest("68_Step6_BuddySystem_AttendantMarker_DistanceValidation_OutsideDangerZone_Accepted", Test_68_Step6_BuddySystem_AttendantMarker_DistanceValidation_OutsideDangerZone_Accepted, logMessages)) allPassed = false;
            if (!RunTest("69_Step6_BuddySystem_AssignAttendant_EmitsAttendantAssigned", Test_69_Step6_BuddySystem_AssignAttendant_EmitsAttendantAssigned, logMessages)) allPassed = false;
            if (!RunTest("70_Step6_BuddySystem_RadioCheck_BeforeAttendantAssigned_Blocked", Test_70_Step6_BuddySystem_RadioCheck_BeforeAttendantAssigned_Blocked, logMessages)) allPassed = false;
            if (!RunTest("71_Step6_BuddySystem_RadioCheck_AfterAttendantAssigned_EmitsCommunicationChecked", Test_71_Step6_BuddySystem_RadioCheck_AfterAttendantAssigned_EmitsCommunicationChecked, logMessages)) allPassed = false;
            if (!RunTest("72_Step6_BuddySystem_CanGoNext_Gating", Test_72_Step6_BuddySystem_CanGoNext_Gating, logMessages)) allPassed = false;
            if (!RunTest("73_Retake_ResetsPhase2State_Idempotently", Test_73_Retake_ResetsPhase2State_Idempotently, logMessages)) allPassed = false;
            if (!RunTest("74_Step4_To_Step6_EndToEnd_Flow", Test_74_Step4_To_Step6_EndToEnd_Flow, logMessages)) allPassed = false;
            if (!RunTest("75_Phase2_LocalizationKeys_ExistAcrossAllLocales", Test_75_Phase2_LocalizationKeys_ExistAcrossAllLocales, logMessages)) allPassed = false;
            if (!RunTest("76_Step4_Distractor_PenaltyScoringDeduction", Test_76_Step4_Distractor_PenaltyScoringDeduction, logMessages)) allPassed = false;
            if (!RunTest("77_GasArInteractionController_RaycastTap_AttendantMarker", Test_77_GasArInteractionController_RaycastTap_AttendantMarker, logMessages)) allPassed = false;

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

        public static void Test_31_GasArInteractionController_Initialization()
        {
            var go = new GameObject("Test_GasArInteractionController_31");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                if (ctrl.Workflow == null) throw new Exception("GasArInteractionController.Workflow is null.");
                if (ctrl.StepNavigator == null) throw new Exception("GasArInteractionController.StepNavigator is null.");
                if (ctrl.State != GasInteractionState.WaitingForTracking && ctrl.State != GasInteractionState.ReadyToPlace)
                    throw new Exception($"Unexpected initial state: {ctrl.State}");

                var marker = ctrl.SpawnHazardMarker(Vector3.zero, Quaternion.identity);
                if (marker == null) throw new Exception("SpawnHazardMarker returned null.");
                if (ctrl.ActiveHazard != marker) throw new Exception("ctrl.ActiveHazard does not match spawned marker.");
                if (ctrl.State != GasInteractionState.AwaitingHazardRecognition && ctrl.State != GasInteractionState.HazardPlaced)
                    throw new Exception($"Expected state AwaitingHazardRecognition, got {ctrl.State}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_32_Step1_HazardRecognition_EmitsGasHazardRecognized()
        {
            var go = new GameObject("Test_GasAr_32");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                ctrl.SpawnHazardMarker(Vector3.zero, Quaternion.identity);

                TrainingEvent received = null;
                ctrl.OnHazardRecognized += evt => received = evt;

                bool success = ctrl.ProcessHazardTap();
                if (!success) throw new Exception("ProcessHazardTap returned false.");
                if (received == null) throw new Exception("OnHazardRecognized event was not received.");
                if (received.EventType != "gas_hazard_recognized") throw new Exception($"EventType mismatch: expected 'gas_hazard_recognized', got '{received.EventType}'");
                if (received.Outcome != "success") throw new Exception($"Outcome mismatch: expected 'success', got '{received.Outcome}'");
                if (!ctrl.ActiveHazard.IsHazardRecognized) throw new Exception("ActiveHazard.IsHazardRecognized should be true.");
                if (!ctrl.StepNavigator.IsStepCompleted(1)) throw new Exception("StepNavigator step 1 should be marked completed.");
                if (!ctrl.StepNavigator.CanGoNext) throw new Exception("StepNavigator.CanGoNext should be true after step 1 completion.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_33_Step1_IncorrectInteraction_DoesNotAdvance()
        {
            var go = new GameObject("Test_GasAr_33");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                ctrl.SpawnHazardMarker(Vector3.zero, Quaternion.identity);

                bool dangerTap = ctrl.ProcessDangerZonePerimeterTap();
                if (dangerTap) throw new Exception("ProcessDangerZonePerimeterTap on step 1 should return false.");
                if (ctrl.StepNavigator.IsStepCompleted(1)) throw new Exception("Step 1 should not be completed.");
                if (ctrl.StepNavigator.CanGoNext) throw new Exception("StepNavigator.CanGoNext should be false.");
                if (bus.DispatchedEvents.Count > 0) throw new Exception("No events should be emitted on invalid interaction.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_34_Step2_DangerZoneRecognized_EmitsEvent()
        {
            var go = new GameObject("Test_GasAr_34");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                ctrl.SpawnHazardMarker(Vector3.zero, Quaternion.identity);

                // Complete step 1
                ctrl.ProcessHazardTap();
                ctrl.AdvanceToNextStep();

                if (ctrl.StepNavigator.CurrentStepIndex != 2) throw new Exception($"Expected StepNavigator at step 2, got {ctrl.StepNavigator.CurrentStepIndex}");

                TrainingEvent received = null;
                ctrl.OnDangerZoneRecognized += evt => received = evt;

                bool success = ctrl.ProcessDangerZonePerimeterTap();
                if (!success) throw new Exception("ProcessDangerZonePerimeterTap returned false.");
                if (received == null) throw new Exception("OnDangerZoneRecognized event was not received.");
                if (received.EventType != "danger_zone_recognized") throw new Exception($"Expected 'danger_zone_recognized', got '{received.EventType}'");
                if (received.Outcome != "success") throw new Exception($"Expected outcome 'success', got '{received.Outcome}'");
                if (!ctrl.ActiveHazard.IsDangerZoneMarked) throw new Exception("ActiveHazard.IsDangerZoneMarked should be true.");
                if (!ctrl.StepNavigator.IsStepCompleted(2)) throw new Exception("Step 2 should be marked completed.");
                if (!ctrl.StepNavigator.CanGoNext) throw new Exception("StepNavigator.CanGoNext should be true after step 2.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_35_Step2_UnsafeZoneEntry_EmitsPenaltyEvent()
        {
            var go = new GameObject("Test_GasAr_35");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                ctrl.SpawnHazardMarker(Vector3.zero, Quaternion.identity);

                // Step 1
                ctrl.ProcessHazardTap();
                ctrl.AdvanceToNextStep();

                TrainingEvent unsafeEvent = null;
                ctrl.OnUnsafeZoneEntry += evt => unsafeEvent = evt;

                bool recorded = ctrl.ProcessUnsafeZoneTap();
                if (!recorded) throw new Exception("ProcessUnsafeZoneTap returned false.");
                if (unsafeEvent == null) throw new Exception("OnUnsafeZoneEntry event was not emitted.");
                if (unsafeEvent.EventType != "unsafe_zone_entry") throw new Exception($"Expected 'unsafe_zone_entry', got '{unsafeEvent.EventType}'");
                if (unsafeEvent.Outcome != "failure") throw new Exception($"Expected outcome 'failure', got '{unsafeEvent.Outcome}'");

                // Evaluate rubric scoring to verify penalty
                var rubric = RubricLoader.LoadGasConfinedSpaceRubric();
                var result = LocalAssessmentEngine.Evaluate(new List<TrainingEvent>(bus.DispatchedEvents), rubric);

                if (result.TotalPenalties != 5.00f)
                {
                    throw new Exception($"Expected 5.00 total penalties for unsafe_zone_entry, got {result.TotalPenalties}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_36_Step3_Sequence_StartsAtOxygen()
        {
            var go = new GameObject("Test_GasAr_36");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                ctrl.SpawnHazardMarker(Vector3.zero, Quaternion.identity);

                ctrl.ProcessHazardTap();
                ctrl.AdvanceToNextStep();
                ctrl.ProcessDangerZonePerimeterTap();
                ctrl.AdvanceToNextStep();

                if (ctrl.StepNavigator.CurrentStepIndex != 3) throw new Exception($"Expected StepNavigator at step 3, got {ctrl.StepNavigator.CurrentStepIndex}");

                var sim = ctrl.Workflow.AtmosphericSimulator;
                if (!sim.IsTestStarted) throw new Exception("Atmospheric simulator test should be started on step 3.");
                if (sim.IsOxygenTested) throw new Exception("Oxygen should not be tested yet.");
                if (sim.IsFlammableTested) throw new Exception("Flammable should not be tested yet.");
                if (sim.IsToxicTested) throw new Exception("Toxic should not be tested yet.");
                if (sim.IsAssessmentCompleted) throw new Exception("Assessment should not be completed yet.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_37_Step3_Sequence_OxygenMustCompleteBeforeFlammableUnlocks()
        {
            var go = new GameObject("Test_GasAr_37");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                ctrl.SpawnHazardMarker(Vector3.zero, Quaternion.identity);

                ctrl.ProcessHazardTap();
                ctrl.AdvanceToNextStep();
                ctrl.ProcessDangerZonePerimeterTap();
                ctrl.AdvanceToNextStep();

                // Attempting flammable before oxygen must be rejected
                bool flammBeforeO2 = ctrl.TestSensor(GasSensorType.Flammable, out string err);
                if (flammBeforeO2) throw new Exception("Testing Flammable before Oxygen should fail.");
                if (string.IsNullOrEmpty(err) || !err.Contains("Oxygen")) throw new Exception($"Expected sequence violation error mentioning Oxygen, got '{err}'");

                // Testing Oxygen succeeds
                bool o2Success = ctrl.TestSensor(GasSensorType.Oxygen, out err);
                if (!o2Success) throw new Exception($"Testing Oxygen failed: {err}");
                if (!ctrl.Workflow.AtmosphericSimulator.IsOxygenTested) throw new Exception("IsOxygenTested should be true.");

                // Now testing Flammable succeeds
                bool flammSuccess = ctrl.TestSensor(GasSensorType.Flammable, out err);
                if (!flammSuccess) throw new Exception($"Testing Flammable after Oxygen failed: {err}");
                if (!ctrl.Workflow.AtmosphericSimulator.IsFlammableTested) throw new Exception("IsFlammableTested should be true.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_38_Step3_Sequence_FlammableMustCompleteBeforeToxicUnlocks()
        {
            var go = new GameObject("Test_GasAr_38");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                ctrl.SpawnHazardMarker(Vector3.zero, Quaternion.identity);

                ctrl.ProcessHazardTap();
                ctrl.AdvanceToNextStep();
                ctrl.ProcessDangerZonePerimeterTap();
                ctrl.AdvanceToNextStep();

                ctrl.TestSensor(GasSensorType.Oxygen, out _);

                // Attempting Toxic before Flammable must be rejected
                bool toxicBeforeFlamm = ctrl.TestSensor(GasSensorType.Toxic, out string err);
                if (toxicBeforeFlamm) throw new Exception("Testing Toxic before Flammable should fail.");
                if (string.IsNullOrEmpty(err) || (!err.Contains("Flammable") && !err.Contains("LEL")))
                    throw new Exception($"Expected sequence violation mentioning Flammable/LEL, got '{err}'");

                ctrl.TestSensor(GasSensorType.Flammable, out _);

                // Now testing Toxic succeeds
                bool toxicSuccess = ctrl.TestSensor(GasSensorType.Toxic, out err);
                if (!toxicSuccess) throw new Exception($"Testing Toxic after Flammable failed: {err}");
                if (!ctrl.Workflow.AtmosphericSimulator.IsToxicTested) throw new Exception("IsToxicTested should be true.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_39_Step3_Sequence_H2sCannotBeTestedBeforeLel()
        {
            var go = new GameObject("Test_GasAr_39");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                ctrl.SpawnHazardMarker(Vector3.zero, Quaternion.identity);

                ctrl.ProcessHazardTap();
                ctrl.AdvanceToNextStep();
                ctrl.ProcessDangerZonePerimeterTap();
                ctrl.AdvanceToNextStep();

                bool toxicFirst = ctrl.TestSensor(GasSensorType.Toxic, out string err);
                if (toxicFirst) throw new Exception("Testing Toxic first should be rejected.");
                if (ctrl.Workflow.AtmosphericSimulator.IsToxicTested) throw new Exception("IsToxicTested must remain false.");

                ctrl.TestSensor(GasSensorType.Oxygen, out _);

                bool toxicSecond = ctrl.TestSensor(GasSensorType.Toxic, out err);
                if (toxicSecond) throw new Exception("Testing Toxic before LEL should be rejected.");
                if (ctrl.Workflow.AtmosphericSimulator.IsToxicTested) throw new Exception("IsToxicTested must remain false.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_40_Step3_Readings_ValuesMatchRequired()
        {
            var o2 = GasAtmosphericSimulator.DefaultOxygenReading;
            var lel = GasAtmosphericSimulator.DefaultFlammableReading;
            var h2s = GasAtmosphericSimulator.DefaultToxicReading;

            if (Math.Abs(o2.Value - 19.1f) > 0.01f) throw new Exception($"O2 reading expected 19.1, got {o2.Value}");
            if (o2.Unit != "% Vol") throw new Exception($"O2 unit expected '% Vol', got '{o2.Unit}'");
            if (o2.IsSafe) throw new Exception("O2 at 19.1% should be unsafe (< 19.5% baseline).");

            if (Math.Abs(lel.Value - 18.0f) > 0.01f) throw new Exception($"LEL reading expected 18.0, got {lel.Value}");
            if (lel.Unit != "% LEL") throw new Exception($"LEL unit expected '% LEL', got '{lel.Unit}'");
            if (lel.IsSafe) throw new Exception("LEL at 18% should be unsafe (> 10% entry limit).");

            if (Math.Abs(h2s.Value - 35.0f) > 0.01f) throw new Exception($"H2S reading expected 35.0, got {h2s.Value}");
            if (h2s.Unit != "ppm") throw new Exception($"H2S unit expected 'ppm', got '{h2s.Unit}'");
            if (h2s.IsSafe) throw new Exception("H2S at 35 ppm should be unsafe (> 10 ppm ceiling).");
        }

        public static void Test_41_Step3_CompletingAllThree_EmitsAtmosphereAssessmentCompleted()
        {
            var go = new GameObject("Test_GasAr_41");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                ctrl.SpawnHazardMarker(Vector3.zero, Quaternion.identity);

                ctrl.ProcessHazardTap();
                ctrl.AdvanceToNextStep();
                ctrl.ProcessDangerZonePerimeterTap();
                ctrl.AdvanceToNextStep();

                TrainingEvent assessmentEvent = null;
                ctrl.OnAtmosphericAssessmentCompleted += evt => assessmentEvent = evt;

                ctrl.TestSensor(GasSensorType.Oxygen, out _);
                ctrl.TestSensor(GasSensorType.Flammable, out _);
                ctrl.TestSensor(GasSensorType.Toxic, out _);

                if (assessmentEvent == null) throw new Exception("OnAtmosphericAssessmentCompleted was not emitted.");
                if (assessmentEvent.EventType != "atmosphere_assessment_completed")
                    throw new Exception($"Expected 'atmosphere_assessment_completed', got '{assessmentEvent.EventType}'");
                if (assessmentEvent.Outcome != "success")
                    throw new Exception($"Expected outcome 'success', got '{assessmentEvent.Outcome}'");
                if (assessmentEvent.TargetId != "confined_space_atmosphere")
                    throw new Exception($"Expected TargetId 'confined_space_atmosphere', got '{assessmentEvent.TargetId}'");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_42_Step3_FinalAtmosphericStatus_IsUnsafe()
        {
            var go = new GameObject("Test_GasAr_42");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                ctrl.SpawnHazardMarker(Vector3.zero, Quaternion.identity);

                ctrl.ProcessHazardTap();
                ctrl.AdvanceToNextStep();
                ctrl.ProcessDangerZonePerimeterTap();
                ctrl.AdvanceToNextStep();

                TrainingEvent assessmentEvent = null;
                ctrl.OnAtmosphericAssessmentCompleted += evt => assessmentEvent = evt;

                ctrl.TestSensor(GasSensorType.Oxygen, out _);
                ctrl.TestSensor(GasSensorType.Flammable, out _);
                ctrl.TestSensor(GasSensorType.Toxic, out _);

                if (ctrl.Workflow.AtmosphericSimulator.OverallAtmosphereSafe)
                    throw new Exception("Overall atmosphere should be UNSAFE.");

                if (!assessmentEvent.Payload.TryGetValue("overall_status", out string status) || status != "unsafe")
                    throw new Exception($"Expected payload 'overall_status' = 'unsafe', got '{status}'");

                if (!assessmentEvent.Payload.TryGetValue("hazard_detected", out string hazard) || hazard != "true")
                    throw new Exception($"Expected payload 'hazard_detected' = 'true', got '{hazard}'");

                if (!ctrl.StepNavigator.IsStepCompleted(3))
                    throw new Exception("StepNavigator step 3 should be marked completed.");
                if (!ctrl.StepNavigator.CanGoNext)
                    throw new Exception("StepNavigator.CanGoNext should be true after step 3 completes.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_43_ArLifecycle_EnabledOnlyWhileGasTrainingActive()
        {
            var homeObj = new GameObject("Test_WorkerHome_43");
            var arObj = new GameObject("Test_ArMode_43");
            try
            {
                var home = homeObj.AddComponent<WorkerHomeController>();
                var arMode = arObj.AddComponent<ARModeController>();

                arMode.DisableAR();
                if (arMode.IsARActive) throw new Exception("AR should be OFF on initial home state.");

                home.StartGasTraining();
                if (home.CurrentState != WorkerHomeController.WorkerAppScreenState.TrainingGas)
                    throw new Exception($"Expected state TrainingGas, got {home.CurrentState}");
                if (!arMode.IsARActive)
                    throw new Exception("AR should be ON during active Gas training.");

                home.ReturnToHome();
                arMode.DisableAR();
                if (home.CurrentState != WorkerHomeController.WorkerAppScreenState.Home)
                    throw new Exception($"Expected state Home, got {home.CurrentState}");
                if (arMode.IsARActive)
                    throw new Exception("AR should be OFF after returning to Home.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(homeObj);
                UnityEngine.Object.DestroyImmediate(arObj);
            }
        }

        public static void Test_44_GasInteractionFeedbackUI_HierarchyAndDetectorComponents()
        {
            var go = new GameObject("Test_FeedbackUI_44");
            try
            {
                var ui = go.AddComponent<GasInteractionFeedbackUI>();
                ui.ShowTrainingUI();

                if (ui.Canvas == null) throw new Exception("GasInteractionFeedbackUI canvas is null.");
                if (ui.NextButton == null) throw new Exception("NextButton is null.");
                if (ui.BackButton == null) throw new Exception("BackButton is null.");
                if (ui.Step1ActionButton == null) throw new Exception("Step1ActionButton is null.");
                if (ui.Step2ActionButton == null) throw new Exception("Step2ActionButton is null.");
                if (ui.BtnTestO2 == null) throw new Exception("BtnTestO2 is null.");
                if (ui.BtnTestLel == null) throw new Exception("BtnTestLel is null.");
                if (ui.BtnTestH2s == null) throw new Exception("BtnTestH2s is null.");
                if (ui.DetectorHeaderStatus == null) throw new Exception("DetectorHeaderStatus is null.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_45_GasHazardMarker_VisualComponentsAndGeometry()
        {
            var go = new GameObject("Test_HazardMarker_45");
            try
            {
                var marker = go.AddComponent<GasHazardMarker>();
                marker.EnsureVisuals();

                if (marker.FloatingLabel == null) throw new Exception("FloatingLabel is null.");
                if (marker.HazardId != "hazard_gas_confined_space")
                    throw new Exception($"Expected hazardId 'hazard_gas_confined_space', got '{marker.HazardId}'");

                marker.AcknowledgeHazard();
                if (!marker.IsHazardRecognized) throw new Exception("IsHazardRecognized should be true.");

                marker.ShowDangerZoneRing(true);
                marker.MarkDangerZoneEstablished();
                if (!marker.IsDangerZoneMarked) throw new Exception("IsDangerZoneMarked should be true.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_46_Detector_Localization_AllLocalesPresent()
        {
            var loc = LocaleService.Instance;
            string[] requiredKeys = new[]
            {
                "gas_header_title",
                "gas_step1_title",
                "gas_step1_prompt",
                "gas_step2_title",
                "gas_step2_prompt",
                "gas_step3_title",
                "gas_step3_prompt",
                "detector_title",
                "detector_status_ready",
                "detector_status_unsafe",
                "detector_o2_label",
                "detector_lel_label",
                "detector_h2s_label",
                "detector_btn_test",
                "detector_locked"
            };

            foreach (var lang in new[] { "en", "hi", "sat" })
            {
                loc.SetLanguage(lang);
                foreach (var key in requiredKeys)
                {
                    string val = loc.Get(key);
                    if (string.IsNullOrEmpty(val))
                    {
                        throw new Exception($"Missing localization key '{key}' in locale '{lang}'");
                    }
                }
            }

            loc.SetLanguage("en");
        }

        private static void SetupArSteps1To3(GasArInteractionController ctrl)
        {
            ctrl.SpawnHazardMarker(Vector3.zero, Quaternion.identity);
            ctrl.ProcessHazardTap();
            ctrl.AdvanceToNextStep();
            ctrl.ProcessDangerZonePerimeterTap();
            ctrl.AdvanceToNextStep();
            ctrl.TestSensor(GasSensorType.Oxygen, out _);
            ctrl.TestSensor(GasSensorType.Flammable, out _);
            ctrl.TestSensor(GasSensorType.Toxic, out _);
            ctrl.AdvanceToNextStep();
        }

        private static void SetupArSteps1To4(GasArInteractionController ctrl)
        {
            SetupArSteps1To3(ctrl);
            ctrl.SelectPpeItem(GasPpeSystem.ItemHelmet);
            ctrl.SelectPpeItem(GasPpeSystem.ItemHarness);
            ctrl.SelectPpeItem(GasPpeSystem.ItemGloves);
            ctrl.SelectPpeItem(GasPpeSystem.ItemBoots);
            ctrl.SelectPpeItem(GasPpeSystem.ItemScba);
            ctrl.SubmitPpeSelection();
            ctrl.AdvanceToNextStep();
        }

        private static void SetupArSteps1To5(GasArInteractionController ctrl)
        {
            SetupArSteps1To4(ctrl);
            ctrl.VerifyScbaSeal();
            ctrl.VerifyHarnessFit();
            ctrl.CheckCylinderPressure();
            ctrl.AdvanceToNextStep();
        }

        public static void Test_47_Step4_PpeSelection_UIHierarchyAndWarning()
        {
            var go = new GameObject("Test_UI_47");
            try
            {
                var ui = go.AddComponent<GasInteractionFeedbackUI>();
                ui.ShowTrainingUI();
                if (ui.PpePaletteRootObj == null) throw new Exception("PpePaletteRootObj is null.");
                if (ui.BtnConfirmPpe == null) throw new Exception("BtnConfirmPpe is null.");

                string[] expectedItems = new[]
                {
                    GasPpeSystem.ItemHelmet,
                    GasPpeSystem.ItemHarness,
                    GasPpeSystem.ItemGloves,
                    GasPpeSystem.ItemBoots,
                    GasPpeSystem.ItemScba,
                    GasPpeSystem.ItemDustMask,
                    GasPpeSystem.ItemClothMask
                };

                foreach (var item in expectedItems)
                {
                    var btn = ui.GetPpeButton(item);
                    if (btn == null) throw new Exception($"Missing PPE button for item '{item}'");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_48_Step4_PpeSelection_UnsafeWarningText_MatchesRequirement()
        {
            var loc = LocaleService.Instance;
            string warningEn = loc.Get("ppe_unsafe_warning");
            if (string.IsNullOrEmpty(warningEn)) throw new Exception("ppe_unsafe_warning is empty in English.");
            if (warningEn.IndexOf("not make an unsafe atmosphere safe", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new Exception($"ppe_unsafe_warning must state PPE does NOT make unsafe atmosphere safe. Got: '{warningEn}'");
            }
        }

        public static void Test_49_Step4_PpeSelection_Distractor_DustMask_EmitsIncorrectAndPenalty()
        {
            var go = new GameObject("Test_GasAr_49");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To3(ctrl);

                if (ctrl.StepNavigator.CurrentStepIndex != 4) throw new Exception($"Expected step 4, got {ctrl.StepNavigator.CurrentStepIndex}");

                TrainingEvent failureEvt = null;
                ctrl.OnPpeSelectionIncorrect += evt => failureEvt = evt;

                bool selected = ctrl.SelectPpeItem(GasPpeSystem.ItemDustMask);
                if (selected) throw new Exception("Dust mask selection should return false.");
                if (failureEvt == null) throw new Exception("OnPpeSelectionIncorrect event was not emitted.");
                if (failureEvt.EventType != "ppe_selection_incorrect") throw new Exception($"Expected 'ppe_selection_incorrect', got '{failureEvt.EventType}'");
                if (failureEvt.Outcome != "failure") throw new Exception($"Expected outcome 'failure', got '{failureEvt.Outcome}'");

                var rubric = RubricLoader.LoadGasConfinedSpaceRubric();
                var result = LocalAssessmentEngine.Evaluate(new List<TrainingEvent>(bus.DispatchedEvents), rubric);
                if (result.TotalPenalties != 5.00f)
                {
                    throw new Exception($"Expected 5.00 total penalties for dust mask distractor, got {result.TotalPenalties}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_50_Step4_PpeSelection_Distractor_SurgicalMask_EmitsIncorrectAndPenalty()
        {
            var go = new GameObject("Test_GasAr_50");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To3(ctrl);

                TrainingEvent failureEvt = null;
                ctrl.OnPpeSelectionIncorrect += evt => failureEvt = evt;

                bool selected = ctrl.SelectPpeItem(GasPpeSystem.ItemClothMask);
                if (selected) throw new Exception("Surgical/cloth mask selection should return false.");
                if (failureEvt == null) throw new Exception("OnPpeSelectionIncorrect event was not emitted.");
                if (failureEvt.EventType != "ppe_selection_incorrect") throw new Exception($"Expected 'ppe_selection_incorrect', got '{failureEvt.EventType}'");

                var rubric = RubricLoader.LoadGasConfinedSpaceRubric();
                var result = LocalAssessmentEngine.Evaluate(new List<TrainingEvent>(bus.DispatchedEvents), rubric);
                if (result.TotalPenalties != 5.00f)
                {
                    throw new Exception($"Expected 5.00 total penalties for surgical mask distractor, got {result.TotalPenalties}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_51_Step4_PpeSelection_Distractor_BlocksProgression()
        {
            var go = new GameObject("Test_GasAr_51");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To3(ctrl);

                // Select valid items
                ctrl.SelectPpeItem(GasPpeSystem.ItemHelmet);
                ctrl.SelectPpeItem(GasPpeSystem.ItemHarness);
                ctrl.SelectPpeItem(GasPpeSystem.ItemGloves);
                ctrl.SelectPpeItem(GasPpeSystem.ItemBoots);
                ctrl.SelectPpeItem(GasPpeSystem.ItemScba);
                // Also select distractor
                ctrl.SelectPpeItem(GasPpeSystem.ItemDustMask);

                bool confirmed = ctrl.SubmitPpeSelection();
                if (confirmed) throw new Exception("PPE confirmation must fail when a distractor is selected.");
                if (ctrl.StepNavigator.CanGoNext) throw new Exception("StepNavigator.CanGoNext must be false.");
                if (ctrl.State == GasInteractionState.PpeSelected) throw new Exception("State should not be PpeSelected.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_52_Step4_PpeSelection_CorrectionAllowedAfterDistractor()
        {
            var go = new GameObject("Test_GasAr_52");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To3(ctrl);

                // First select distractor
                ctrl.SelectPpeItem(GasPpeSystem.ItemDustMask);
                // Worker corrects their mistake by toggling off the distractor
                ctrl.TogglePpeItem(GasPpeSystem.ItemDustMask);
                if (ctrl.IsPpeItemSelected(GasPpeSystem.ItemDustMask)) throw new Exception("Dust mask should no longer be selected.");

                // Select the 5 required items
                ctrl.SelectPpeItem(GasPpeSystem.ItemHelmet);
                ctrl.SelectPpeItem(GasPpeSystem.ItemHarness);
                ctrl.SelectPpeItem(GasPpeSystem.ItemGloves);
                ctrl.SelectPpeItem(GasPpeSystem.ItemBoots);
                ctrl.SelectPpeItem(GasPpeSystem.ItemScba);

                bool confirmed = ctrl.SubmitPpeSelection();
                if (!confirmed) throw new Exception("Correction after distractor should allow valid PPE confirmation.");
                if (ctrl.State != GasInteractionState.PpeSelected) throw new Exception($"Expected state PpeSelected, got {ctrl.State}");
                if (!ctrl.StepNavigator.IsStepCompleted(4)) throw new Exception("Step 4 should be marked completed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_53_Step4_PpeSelection_IncompleteSelection_MissingScba_Fails()
        {
            var go = new GameObject("Test_GasAr_53");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To3(ctrl);

                ctrl.SelectPpeItem(GasPpeSystem.ItemHelmet);
                ctrl.SelectPpeItem(GasPpeSystem.ItemHarness);
                ctrl.SelectPpeItem(GasPpeSystem.ItemGloves);
                ctrl.SelectPpeItem(GasPpeSystem.ItemBoots);
                // Missing SCBA

                bool confirmed = ctrl.SubmitPpeSelection();
                if (confirmed) throw new Exception("Selection missing SCBA should fail.");
                if (ctrl.StepNavigator.IsStepCompleted(4)) throw new Exception("Step 4 should not be completed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_54_Step4_PpeSelection_IncompleteSelection_MissingHarness_Fails()
        {
            var go = new GameObject("Test_GasAr_54");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To3(ctrl);

                ctrl.SelectPpeItem(GasPpeSystem.ItemHelmet);
                ctrl.SelectPpeItem(GasPpeSystem.ItemGloves);
                ctrl.SelectPpeItem(GasPpeSystem.ItemBoots);
                ctrl.SelectPpeItem(GasPpeSystem.ItemScba);
                // Missing Harness

                bool confirmed = ctrl.SubmitPpeSelection();
                if (confirmed) throw new Exception("Selection missing Harness should fail.");
                if (ctrl.StepNavigator.IsStepCompleted(4)) throw new Exception("Step 4 should not be completed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_55_Step4_PpeSelection_CompleteKit_EmitsPpeSelected()
        {
            var go = new GameObject("Test_GasAr_55");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To3(ctrl);

                TrainingEvent successEvt = null;
                ctrl.OnPpeSelected += evt => successEvt = evt;

                ctrl.SelectPpeItem(GasPpeSystem.ItemHelmet);
                ctrl.SelectPpeItem(GasPpeSystem.ItemHarness);
                ctrl.SelectPpeItem(GasPpeSystem.ItemGloves);
                ctrl.SelectPpeItem(GasPpeSystem.ItemBoots);
                ctrl.SelectPpeItem(GasPpeSystem.ItemScba);

                bool confirmed = ctrl.SubmitPpeSelection();
                if (!confirmed) throw new Exception("SubmitPpeSelection returned false for valid kit.");
                if (successEvt == null) throw new Exception("OnPpeSelected event was not emitted.");
                if (successEvt.EventType != "ppe_selected") throw new Exception($"Expected 'ppe_selected', got '{successEvt.EventType}'");
                if (successEvt.Outcome != "success") throw new Exception($"Expected outcome 'success', got '{successEvt.Outcome}'");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_56_Step4_PpeSelection_CanGoNext_Gating()
        {
            var go = new GameObject("Test_GasAr_56");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To3(ctrl);

                if (ctrl.StepNavigator.CanGoNext) throw new Exception("CanGoNext should be false before PPE selection.");

                ctrl.SelectPpeItem(GasPpeSystem.ItemHelmet);
                ctrl.SelectPpeItem(GasPpeSystem.ItemHarness);
                ctrl.SelectPpeItem(GasPpeSystem.ItemGloves);
                ctrl.SelectPpeItem(GasPpeSystem.ItemBoots);
                ctrl.SelectPpeItem(GasPpeSystem.ItemScba);
                ctrl.SubmitPpeSelection();

                if (!ctrl.StepNavigator.CanGoNext) throw new Exception("CanGoNext should be true after PPE selection confirmed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_57_Step5_PpeVerification_UIHierarchy()
        {
            var go = new GameObject("Test_UI_57");
            try
            {
                var ui = go.AddComponent<GasInteractionFeedbackUI>();
                ui.ShowTrainingUI();
                if (ui.PpeVerificationRootObj == null) throw new Exception("PpeVerificationRootObj is null.");
                if (ui.BtnVerifySeal == null) throw new Exception("BtnVerifySeal is null.");
                if (ui.BtnVerifyHarness == null) throw new Exception("BtnVerifyHarness is null.");
                if (ui.BtnCheckPressure == null) throw new Exception("BtnCheckPressure is null.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_58_Step5_PpeVerification_InitialState_Unverified()
        {
            var go = new GameObject("Test_GasAr_58");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To4(ctrl);

                if (ctrl.StepNavigator.CurrentStepIndex != 5) throw new Exception($"Expected step 5, got {ctrl.StepNavigator.CurrentStepIndex}");
                if (ctrl.IsSealCheckPassed) throw new Exception("IsSealCheckPassed should be false initially.");
                if (ctrl.IsHarnessFitPassed) throw new Exception("IsHarnessFitPassed should be false initially.");
                if (ctrl.IsCylinderPressurePassed) throw new Exception("IsCylinderPressurePassed should be false initially.");
                if (ctrl.StepNavigator.IsStepCompleted(5)) throw new Exception("Step 5 should not be completed initially.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_59_Step5_PpeVerification_IncompleteVerification_EmitsFailurePenalty()
        {
            var go = new GameObject("Test_GasAr_59");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To4(ctrl);

                TrainingEvent failureEvt = null;
                ctrl.OnPpeVerificationFailed += evt => failureEvt = evt;

                // Perform only seal check, leaving harness and cylinder unverified
                ctrl.VerifyScbaSeal();
                bool submitted = ctrl.SubmitPpeVerification();
                if (submitted) throw new Exception("SubmitPpeVerification should fail when checks are incomplete.");
                if (failureEvt == null) throw new Exception("OnPpeVerificationFailed was not emitted.");
                if (failureEvt.EventType != "ppe_verification_failed") throw new Exception($"Expected 'ppe_verification_failed', got '{failureEvt.EventType}'");
                if (failureEvt.Outcome != "failure") throw new Exception($"Expected outcome 'failure', got '{failureEvt.Outcome}'");

                var rubric = RubricLoader.LoadGasConfinedSpaceRubric();
                var result = LocalAssessmentEngine.Evaluate(new List<TrainingEvent>(bus.DispatchedEvents), rubric);
                if (result.TotalPenalties != 5.00f)
                {
                    throw new Exception($"Expected 5.00 total penalties for ppe_verification_failed, got {result.TotalPenalties}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_60_Step5_PpeVerification_CheckSeal_UpdatesState()
        {
            var go = new GameObject("Test_GasAr_60");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To4(ctrl);

                bool result = ctrl.VerifyScbaSeal();
                if (!result) throw new Exception("VerifyScbaSeal returned false.");
                if (!ctrl.IsSealCheckPassed) throw new Exception("IsSealCheckPassed should be true.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_61_Step5_PpeVerification_CheckHarness_UpdatesState()
        {
            var go = new GameObject("Test_GasAr_61");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To4(ctrl);

                bool result = ctrl.VerifyHarnessFit();
                if (!result) throw new Exception("VerifyHarnessFit returned false.");
                if (!ctrl.IsHarnessFitPassed) throw new Exception("IsHarnessFitPassed should be true.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_62_Step5_PpeVerification_CheckCylinder_UpdatesState()
        {
            var go = new GameObject("Test_GasAr_62");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To4(ctrl);

                bool result = ctrl.CheckCylinderPressure();
                if (!result) throw new Exception("CheckCylinderPressure returned false.");
                if (!ctrl.IsCylinderPressurePassed) throw new Exception("IsCylinderPressurePassed should be true.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_63_Step5_PpeVerification_CompleteAllThree_EmitsPpeVerified()
        {
            var go = new GameObject("Test_GasAr_63");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To4(ctrl);

                TrainingEvent successEvt = null;
                ctrl.OnPpeVerified += evt => successEvt = evt;

                ctrl.VerifyScbaSeal();
                ctrl.VerifyHarnessFit();
                ctrl.CheckCylinderPressure();

                if (successEvt == null) throw new Exception("OnPpeVerified was not emitted.");
                if (successEvt.EventType != "ppe_verified") throw new Exception($"Expected 'ppe_verified', got '{successEvt.EventType}'");
                if (successEvt.Outcome != "success") throw new Exception($"Expected outcome 'success', got '{successEvt.Outcome}'");
                if (ctrl.State != GasInteractionState.PpeVerified) throw new Exception($"Expected state PpeVerified, got {ctrl.State}");
                if (!ctrl.StepNavigator.IsStepCompleted(5)) throw new Exception("Step 5 should be completed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_64_Step5_PpeVerification_CanGoNext_Gating()
        {
            var go = new GameObject("Test_GasAr_64");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To4(ctrl);

                if (ctrl.StepNavigator.CanGoNext) throw new Exception("CanGoNext should be false before verification.");

                ctrl.VerifyScbaSeal();
                ctrl.VerifyHarnessFit();
                if (ctrl.StepNavigator.CanGoNext) throw new Exception("CanGoNext should still be false before cylinder check.");

                ctrl.CheckCylinderPressure();
                if (!ctrl.StepNavigator.CanGoNext) throw new Exception("CanGoNext should be true after all 3 checks verified.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_65_Step6_BuddySystem_UIHierarchy()
        {
            var go = new GameObject("Test_UI_65");
            try
            {
                var ui = go.AddComponent<GasInteractionFeedbackUI>();
                ui.ShowTrainingUI();
                if (ui.BuddySystemRootObj == null) throw new Exception("BuddySystemRootObj is null.");
                if (ui.BtnAssignAttendant == null) throw new Exception("BtnAssignAttendant is null.");
                if (ui.BtnCheckCommunication == null) throw new Exception("BtnCheckCommunication is null.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_66_Step6_BuddySystem_AttendantMarker_SpawnAndGeometry()
        {
            var go = new GameObject("Test_AttendantMarker_66");
            try
            {
                var marker = go.AddComponent<GasAttendantMarker>();
                marker.EnsureVisuals();

                if (marker.AttendantId != "attendant_guard_outside")
                    throw new Exception($"Expected attendantId 'attendant_guard_outside', got '{marker.AttendantId}'");
                if (marker.IsAssigned) throw new Exception("IsAssigned should be false initially.");
                if (marker.IsCommunicationVerified) throw new Exception("IsCommunicationVerified should be false initially.");
                if (marker.FloatingLabel == null && marker.LabelMesh == null)
                    throw new Exception("Expected either FloatingLabel or LabelMesh on attendant marker.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_67_Step6_BuddySystem_AttendantMarker_DistanceValidation_InsideDangerZone_Rejected()
        {
            var go = new GameObject("Test_Attendant_67");
            try
            {
                go.transform.position = new Vector3(0f, 0f, 2.0f);
                var marker = go.AddComponent<GasAttendantMarker>();
                bool outside = marker.IsPositionOutsideDangerZone(Vector3.zero, 3.0f);
                if (outside) throw new Exception("Position at 2.0m should be rejected as inside 3.0m danger zone.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_68_Step6_BuddySystem_AttendantMarker_DistanceValidation_OutsideDangerZone_Accepted()
        {
            var go = new GameObject("Test_Attendant_68");
            try
            {
                go.transform.position = new Vector3(0f, 0f, 3.5f);
                var marker = go.AddComponent<GasAttendantMarker>();
                bool outside = marker.IsPositionOutsideDangerZone(Vector3.zero, 3.0f);
                if (!outside) throw new Exception("Position at 3.5m should be accepted as outside 3.0m danger zone.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_69_Step6_BuddySystem_AssignAttendant_EmitsAttendantAssigned()
        {
            var go = new GameObject("Test_GasAr_69");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To5(ctrl);

                if (ctrl.StepNavigator.CurrentStepIndex != 6) throw new Exception($"Expected step 6, got {ctrl.StepNavigator.CurrentStepIndex}");

                TrainingEvent assignEvt = null;
                ctrl.OnAttendantAssigned += evt => assignEvt = evt;

                bool assigned = ctrl.AssignAttendant("attendant_guard_outside");
                if (!assigned) throw new Exception("AssignAttendant returned false.");
                if (assignEvt == null) throw new Exception("OnAttendantAssigned was not emitted.");
                if (assignEvt.EventType != "attendant_assigned") throw new Exception($"Expected 'attendant_assigned', got '{assignEvt.EventType}'");
                if (ctrl.ActiveAttendant != null && !ctrl.ActiveAttendant.IsAssigned)
                    throw new Exception("ActiveAttendant.IsAssigned should be true.");
                if (ctrl.State != GasInteractionState.AttendantAssigned)
                    throw new Exception($"Expected state AttendantAssigned, got {ctrl.State}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_70_Step6_BuddySystem_RadioCheck_BeforeAttendantAssigned_Blocked()
        {
            var go = new GameObject("Test_GasAr_70");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To5(ctrl);

                bool checkedComm = ctrl.CheckCommunication();
                if (checkedComm) throw new Exception("CheckCommunication before attendant assigned should return false.");
                if (ctrl.StepNavigator.IsStepCompleted(6)) throw new Exception("Step 6 should not be completed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_71_Step6_BuddySystem_RadioCheck_AfterAttendantAssigned_EmitsCommunicationChecked()
        {
            var go = new GameObject("Test_GasAr_71");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To5(ctrl);

                ctrl.AssignAttendant("attendant_guard_outside");

                TrainingEvent commEvt = null;
                ctrl.OnCommunicationChecked += evt => commEvt = evt;

                bool commSuccess = ctrl.CheckCommunication();
                if (!commSuccess) throw new Exception("CheckCommunication returned false.");
                if (commEvt == null) throw new Exception("OnCommunicationChecked was not emitted.");
                if (commEvt.EventType != "communication_checked") throw new Exception($"Expected 'communication_checked', got '{commEvt.EventType}'");
                if (commEvt.Outcome != "success") throw new Exception($"Expected outcome 'success', got '{commEvt.Outcome}'");
                if (ctrl.ActiveAttendant != null && !ctrl.ActiveAttendant.IsCommunicationVerified)
                    throw new Exception("ActiveAttendant.IsCommunicationVerified should be true.");
                if (ctrl.State != GasInteractionState.CommunicationChecked)
                    throw new Exception($"Expected state CommunicationChecked, got {ctrl.State}");
                if (!ctrl.StepNavigator.IsStepCompleted(6)) throw new Exception("Step 6 should be completed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_72_Step6_BuddySystem_CanGoNext_Gating()
        {
            var go = new GameObject("Test_GasAr_72");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To5(ctrl);

                if (ctrl.StepNavigator.CanGoNext) throw new Exception("CanGoNext should be false before attendant assignment.");

                ctrl.AssignAttendant();
                if (ctrl.StepNavigator.CanGoNext) throw new Exception("CanGoNext should still be false before radio check.");

                ctrl.CheckCommunication();
                if (!ctrl.StepNavigator.CanGoNext) throw new Exception("CanGoNext should be true after radio check completed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_73_Retake_ResetsPhase2State_Idempotently()
        {
            var go = new GameObject("Test_GasAr_73");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To5(ctrl);

                ctrl.AssignAttendant();
                ctrl.CheckCommunication();

                // Reset scenario
                ctrl.ResetScenario();

                if (ctrl.StepNavigator.CurrentStepIndex != 1) throw new Exception($"Expected Step 1 after reset, got {ctrl.StepNavigator.CurrentStepIndex}");
                if (ctrl.IsSealCheckPassed) throw new Exception("IsSealCheckPassed should be false after reset.");
                if (ctrl.IsHarnessFitPassed) throw new Exception("IsHarnessFitPassed should be false after reset.");
                if (ctrl.IsCylinderPressurePassed) throw new Exception("IsCylinderPressurePassed should be false after reset.");
                if (ctrl.Workflow.IsPpeSelected) throw new Exception("Workflow.IsPpeSelected should be false after reset.");
                if (ctrl.Workflow.IsPpeVerified) throw new Exception("Workflow.IsPpeVerified should be false after reset.");
                if (ctrl.Workflow.IsAttendantAssigned) throw new Exception("Workflow.IsAttendantAssigned should be false after reset.");
                if (ctrl.Workflow.IsCommunicationChecked) throw new Exception("Workflow.IsCommunicationChecked should be false after reset.");
                if (ctrl.ActiveAttendant != null && ctrl.ActiveAttendant.IsAssigned) throw new Exception("ActiveAttendant.IsAssigned should be false after reset.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_74_Step4_To_Step6_EndToEnd_Flow()
        {
            var go = new GameObject("Test_GasAr_74");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);

                // Step 1
                ctrl.SpawnHazardMarker(Vector3.zero, Quaternion.identity);
                ctrl.ProcessHazardTap();
                ctrl.AdvanceToNextStep();

                // Step 2
                ctrl.ProcessDangerZonePerimeterTap();
                ctrl.AdvanceToNextStep();

                // Step 3
                ctrl.TestSensor(GasSensorType.Oxygen, out _);
                ctrl.TestSensor(GasSensorType.Flammable, out _);
                ctrl.TestSensor(GasSensorType.Toxic, out _);
                ctrl.AdvanceToNextStep();

                // Step 4
                if (ctrl.StepNavigator.CurrentStepIndex != 4) throw new Exception($"Expected step 4, got {ctrl.StepNavigator.CurrentStepIndex}");
                ctrl.SelectPpeItem(GasPpeSystem.ItemHelmet);
                ctrl.SelectPpeItem(GasPpeSystem.ItemHarness);
                ctrl.SelectPpeItem(GasPpeSystem.ItemGloves);
                ctrl.SelectPpeItem(GasPpeSystem.ItemBoots);
                ctrl.SelectPpeItem(GasPpeSystem.ItemScba);
                ctrl.SubmitPpeSelection();
                ctrl.AdvanceToNextStep();

                // Step 5
                if (ctrl.StepNavigator.CurrentStepIndex != 5) throw new Exception($"Expected step 5, got {ctrl.StepNavigator.CurrentStepIndex}");
                ctrl.VerifyScbaSeal();
                ctrl.VerifyHarnessFit();
                ctrl.CheckCylinderPressure();
                ctrl.AdvanceToNextStep();

                // Step 6
                if (ctrl.StepNavigator.CurrentStepIndex != 6) throw new Exception($"Expected step 6, got {ctrl.StepNavigator.CurrentStepIndex}");
                ctrl.AssignAttendant();
                ctrl.CheckCommunication();

                if (!ctrl.StepNavigator.IsStepCompleted(4)) throw new Exception("Step 4 not completed.");
                if (!ctrl.StepNavigator.IsStepCompleted(5)) throw new Exception("Step 5 not completed.");
                if (!ctrl.StepNavigator.IsStepCompleted(6)) throw new Exception("Step 6 not completed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_75_Phase2_LocalizationKeys_ExistAcrossAllLocales()
        {
            var loc = LocaleService.Instance;
            string[] phase2Keys = new[]
            {
                "gas_step4_title",
                "gas_step4_prompt",
                "gas_step5_title",
                "gas_step5_prompt",
                "gas_step6_title",
                "gas_step6_prompt",
                "ppe_title",
                "ppe_unsafe_warning",
                "ppe_item_helmet",
                "ppe_item_harness",
                "ppe_item_gloves",
                "ppe_item_boots",
                "ppe_item_scba",
                "ppe_item_dust_mask",
                "ppe_item_cloth_mask",
                "ppe_btn_confirm",
                "verify_title",
                "verify_seal",
                "verify_harness",
                "verify_cylinder",
                "attendant_title",
                "attendant_assigned",
                "comm_title",
                "comm_btn_test",
                "comm_check_pass"
            };

            foreach (var lang in new[] { "en", "hi", "sat" })
            {
                loc.SetLanguage(lang);
                foreach (var key in phase2Keys)
                {
                    string val = loc.Get(key);
                    if (string.IsNullOrEmpty(val))
                    {
                        throw new Exception($"Missing Phase 2 key '{key}' in locale '{lang}'");
                    }
                }
            }

            loc.SetLanguage("en");
        }

        public static void Test_76_Step4_Distractor_PenaltyScoringDeduction()
        {
            var go = new GameObject("Test_GasAr_76");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To3(ctrl);

                // Select distractor
                ctrl.SelectPpeItem(GasPpeSystem.ItemClothMask);

                // Then select valid kit and complete step 4
                ctrl.TogglePpeItem(GasPpeSystem.ItemClothMask);
                ctrl.SelectPpeItem(GasPpeSystem.ItemHelmet);
                ctrl.SelectPpeItem(GasPpeSystem.ItemHarness);
                ctrl.SelectPpeItem(GasPpeSystem.ItemGloves);
                ctrl.SelectPpeItem(GasPpeSystem.ItemBoots);
                ctrl.SelectPpeItem(GasPpeSystem.ItemScba);
                ctrl.SubmitPpeSelection();

                var rubric = RubricLoader.LoadGasConfinedSpaceRubric();
                var result = LocalAssessmentEngine.Evaluate(new List<TrainingEvent>(bus.DispatchedEvents), rubric);

                if (result.TotalPenalties != 5.00f)
                {
                    throw new Exception($"Expected 5.00 total penalty for cloth mask distractor, got {result.TotalPenalties}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Test_77_GasArInteractionController_RaycastTap_AttendantMarker()
        {
            var go = new GameObject("Test_GasAr_77");
            try
            {
                var ctrl = go.AddComponent<GasArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                SetupArSteps1To5(ctrl);

                if (ctrl.ActiveAttendant == null)
                {
                    throw new Exception("ActiveAttendant should have been spawned on Step 6.");
                }

                // Simulate direct tap on attendant marker
                ctrl.ActiveAttendant.OnTap();

                if (!ctrl.ActiveAttendant.IsAssigned)
                {
                    throw new Exception("ActiveAttendant.IsAssigned should be true after tap.");
                }
                if (ctrl.State != GasInteractionState.AttendantAssigned)
                {
                    throw new Exception($"Expected state AttendantAssigned, got {ctrl.State}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
