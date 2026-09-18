// FireTrainingEventTests.cs
// Namespace : IndustrialSafetyAR.Tests
//
// Standalone unit tests validating the decoupled Fire & Explosion training workflow:
// - Step 1: Detect Hazard (step_detect_hazard / rule_detect_hazard)
// - Step 2: Identify Hazard (step_identify_hazard / rule_identify_hazard)
// - Step 3: Raise Alarm (step_raise_alarm / rule_raise_alarm)
// - Step 4: Select Extinguisher (step_select_extinguisher / rule_select_extinguisher)
// - Validation that invalid/premature actions do not advance the flow
// - Event ordering and payload schema integrity across Steps 1 to 4

using System;
using System.Collections.Generic;
using IndustrialSafetyAR.Core;
using IndustrialSafetyAR.Core.Events;
using IndustrialSafetyAR.Core.Audio;
using IndustrialSafetyAR.Assessment;
using IndustrialSafetyAR.Modules.FireExplosion;
using IndustrialSafetyAR.UI;
using TMPro;
using UnityEngine;

namespace IndustrialSafetyAR.Tests
{
    public static class FireTrainingEventTests
    {
        public static bool RunAllTests(out List<string> logMessages)
        {
            logMessages = new List<string>();
            bool allPassed = true;

            if (!RunTest("EventCreationMatchesSchema", Test_EventCreationMatchesSchema, logMessages)) allPassed = false;
            if (!RunTest("EventBusRecordsAndNotifies", Test_EventBusRecordsAndNotifies, logMessages)) allPassed = false;
            if (!RunTest("EventPayloadIntegrity", Test_EventPayloadIntegrity, logMessages)) allPassed = false;
            if (!RunTest("SuccessfulHazardIdentificationEvent", Test_SuccessfulHazardIdentificationEvent, logMessages)) allPassed = false;
            if (!RunTest("SuccessfulRaiseAlarmEvent", Test_SuccessfulRaiseAlarmEvent, logMessages)) allPassed = false;
            if (!RunTest("CorrectEventOrdering", Test_CorrectEventOrdering, logMessages)) allPassed = false;
            if (!RunTest("InvalidActionDoesNotAdvanceFlow", Test_InvalidActionDoesNotAdvanceFlow, logMessages)) allPassed = false;
            if (!RunTest("SuccessfulCO2ExtinguisherSelection", Test_SuccessfulCO2ExtinguisherSelection, logMessages)) allPassed = false;
            if (!RunTest("IncorrectWaterExtinguisherSelection", Test_IncorrectWaterExtinguisherSelection, logMessages)) allPassed = false;
            if (!RunTest("IncorrectFoamExtinguisherSelection", Test_IncorrectFoamExtinguisherSelection, logMessages)) allPassed = false;
            if (!RunTest("PrematureExtinguisherSelectionRejected", Test_PrematureExtinguisherSelectionRejected, logMessages)) allPassed = false;
            if (!RunTest("Step3ToStep4EventOrdering", Test_Step3ToStep4EventOrdering, logMessages)) allPassed = false;
            if (!RunTest("SuccessfulSafeDistanceAction", Test_SuccessfulSafeDistanceAction, logMessages)) allPassed = false;
            if (!RunTest("UnsafeTooCloseActionRejected", Test_UnsafeTooCloseActionRejected, logMessages)) allPassed = false;
            if (!RunTest("PrematureSafeDistanceActionRejected", Test_PrematureSafeDistanceActionRejected, logMessages)) allPassed = false;
            if (!RunTest("Step4ToStep5EventOrdering", Test_Step4ToStep5EventOrdering, logMessages)) allPassed = false;
            if (!RunTest("Step6CannotBeginBeforeStep5", Test_Step6CannotBeginBeforeStep5, logMessages)) allPassed = false;
            if (!RunTest("PullPinAccepted", Test_PullPinAccepted, logMessages)) allPassed = false;
            if (!RunTest("AimAcceptedOnlyAfterPin", Test_AimAcceptedOnlyAfterPin, logMessages)) allPassed = false;
            if (!RunTest("SqueezeAcceptedOnlyAfterAim", Test_SqueezeAcceptedOnlyAfterAim, logMessages)) allPassed = false;
            if (!RunTest("SweepAcceptedOnlyAfterSqueeze", Test_SweepAcceptedOnlyAfterSqueeze, logMessages)) allPassed = false;
            if (!RunTest("WrongOutOfOrderActionRejected", Test_WrongOutOfOrderActionRejected, logMessages)) allPassed = false;
            if (!RunTest("FinalProcedureCompletedEventFields", Test_FinalProcedureCompletedEventFields, logMessages)) allPassed = false;
            if (!RunTest("Steps1To6EventOrdering", Test_Steps1To6EventOrdering, logMessages)) allPassed = false;
            if (!RunTest("Step7CannotBeginBeforeStep6", Test_Step7CannotBeginBeforeStep6, logMessages)) allPassed = false;
            if (!RunTest("SuccessfulEmergencyExitIdentification", Test_SuccessfulEmergencyExitIdentification, logMessages)) allPassed = false;
            if (!RunTest("IncorrectElevatorExitRejected", Test_IncorrectElevatorExitRejected, logMessages)) allPassed = false;
            if (!RunTest("IncorrectBlockedCorridorExitRejected", Test_IncorrectBlockedCorridorExitRejected, logMessages)) allPassed = false;
            if (!RunTest("EmergencyExitEventMatchesRubricSchema", Test_EmergencyExitEventMatchesRubricSchema, logMessages)) allPassed = false;
            if (!RunTest("Steps1To7EventOrdering", Test_Steps1To7EventOrdering, logMessages)) allPassed = false;
            if (!RunTest("Step8CannotBeginBeforeStep7", Test_Step8CannotBeginBeforeStep7, logMessages)) allPassed = false;
            if (!RunTest("SuccessfulEvacuationSequence", Test_SuccessfulEvacuationSequence, logMessages)) allPassed = false;
            if (!RunTest("OutOfOrderWaypointRejected", Test_OutOfOrderWaypointRejected, logMessages)) allPassed = false;
            if (!RunTest("SmokeCorridorSelectionEmitsFailure", Test_SmokeCorridorSelectionEmitsFailure, logMessages)) allPassed = false;
            if (!RunTest("EvacuationRouteEventMatchesRubricSchema", Test_EvacuationRouteEventMatchesRubricSchema, logMessages)) allPassed = false;
            if (!RunTest("Steps1To8EventOrdering", Test_Steps1To8EventOrdering, logMessages)) allPassed = false;
            if (!RunTest("DuplicateEvacuationCompletionPrevented", Test_DuplicateEvacuationCompletionPrevented, logMessages)) allPassed = false;
            if (!RunTest("Step9CannotBeginBeforeStep8", Test_Step9CannotBeginBeforeStep8, logMessages)) allPassed = false;
            if (!RunTest("SuccessfulAssemblyPointIdentification", Test_SuccessfulAssemblyPointIdentification, logMessages)) allPassed = false;
            if (!RunTest("WrongAssemblyPointRejected", Test_WrongAssemblyPointRejected, logMessages)) allPassed = false;
            if (!RunTest("AssemblyPointEventMatchesRubricSchema", Test_AssemblyPointEventMatchesRubricSchema, logMessages)) allPassed = false;
            if (!RunTest("Step8ToStep9EventOrdering", Test_Step8ToStep9EventOrdering, logMessages)) allPassed = false;
            if (!RunTest("DuplicateAssemblyCompletionPrevented", Test_DuplicateAssemblyCompletionPrevented, logMessages)) allPassed = false;
            if (!RunTest("PrematureAssemblyPointActionRejected", Test_PrematureAssemblyPointActionRejected, logMessages)) allPassed = false;
            if (!RunTest("Assessment_PerfectStep1To9Sequence_Scores100AndPasses", Test_Assessment_PerfectStep1To9Sequence_Scores100AndPasses, logMessages)) allPassed = false;
            if (!RunTest("Assessment_WrongHazardIdentification_Deducts5Penalty", Test_Assessment_WrongHazardIdentification_Deducts5Penalty, logMessages)) allPassed = false;
            if (!RunTest("Assessment_WrongExtinguisher_Deducts5Penalty", Test_Assessment_WrongExtinguisher_Deducts5Penalty, logMessages)) allPassed = false;
            if (!RunTest("Assessment_UnsafeSmokeCorridor_Deducts5Penalty", Test_Assessment_UnsafeSmokeCorridor_Deducts5Penalty, logMessages)) allPassed = false;
            if (!RunTest("Assessment_RepeatedAward_RespectsAwardLimit", Test_Assessment_RepeatedAward_RespectsAwardLimit, logMessages)) allPassed = false;
            if (!RunTest("Assessment_ScoreBelow70_Fails", Test_Assessment_ScoreBelow70_Fails, logMessages)) allPassed = false;
            if (!RunTest("Assessment_PenaltiesCannotDriveScoreBelowZero", Test_Assessment_PenaltiesCannotDriveScoreBelowZero, logMessages)) allPassed = false;
            if (!RunTest("Assessment_DeterministicRepeatedEvaluation_GivesIdenticalResult", Test_Assessment_DeterministicRepeatedEvaluation_GivesIdenticalResult, logMessages)) allPassed = false;
            if (!RunTest("Workflow_AssessmentStartsOnlyAfterFinalFireCompletion", Test_Workflow_AssessmentStartsOnlyAfterFinalFireCompletion, logMessages)) allPassed = false;
            if (!RunTest("Workflow_FinalCompletionEvaluatesSteps1To9Events", Test_Workflow_FinalCompletionEvaluatesSteps1To9Events, logMessages)) allPassed = false;
            if (!RunTest("Workflow_ClientScoreAndPassedStatusCopiedFromEngine", Test_Workflow_ClientScoreAndPassedStatusCopiedFromEngine, logMessages)) allPassed = false;
            if (!RunTest("Workflow_DuplicateCompletionDoesNotCreateSecondAttempt", Test_Workflow_DuplicateCompletionDoesNotCreateSecondAttempt, logMessages)) allPassed = false;
            if (!RunTest("Workflow_PrematureCompletionCannotFinalizeAttempt", Test_Workflow_PrematureCompletionCannotFinalizeAttempt, logMessages)) allPassed = false;
            if (!RunTest("SummaryViewModel_BuildsCorrectlyForPassingAttempt", Test_SummaryViewModel_BuildsCorrectlyForPassingAttempt, logMessages)) allPassed = false;
            if (!RunTest("SummaryViewModel_BuildsCorrectlyForFailingAttempt", Test_SummaryViewModel_BuildsCorrectlyForFailingAttempt, logMessages)) allPassed = false;
            if (!RunTest("SummaryViewModel_StepBreakdownHasAllNineSteps", Test_SummaryViewModel_StepBreakdownHasAllNineSteps, logMessages)) allPassed = false;
            if (!RunTest("SummaryViewModel_CalculatesDurationCorrectly", Test_SummaryViewModel_CalculatesDurationCorrectly, logMessages)) allPassed = false;
            if (!RunTest("SummaryViewModel_CapturesPenaltiesCorrectly", Test_SummaryViewModel_CapturesPenaltiesCorrectly, logMessages)) allPassed = false;
            if (!RunTest("Workflow_RetakeCreatesNewUniqueAttemptId", Test_Workflow_RetakeCreatesNewUniqueAttemptId, logMessages)) allPassed = false;
            if (!RunTest("Assessment_RepeatedPenaltyCannotExceedRubricRules", Test_Assessment_RepeatedPenaltyCannotExceedRubricRules, logMessages)) allPassed = false;
            if (!RunTest("Assessment_ScoreThresholdAndRequiredRuleCompliance", Test_Assessment_ScoreThresholdAndRequiredRuleCompliance, logMessages)) allPassed = false;
            if (!RunTest("CompletedTrainingAttemptMatchesAttemptSchemaJson", Test_CompletedTrainingAttemptMatchesAttemptSchemaJson, logMessages)) allPassed = false;
            if (!RunTest("AssemblyPoint_PrematureAndDuplicateSubmissionsStrictlyRejected", Test_AssemblyPoint_PrematureAndDuplicateSubmissionsStrictlyRejected, logMessages)) allPassed = false;
            if (!RunTest("Retake_EnsuresZeroIdReuseAndCleanStateReset", Test_Retake_EnsuresZeroIdReuseAndCleanStateReset, logMessages)) allPassed = false;
            if (!RunTest("Workflow_DefaultWorkerId_IsValidNonEmptyUuid", Test_Workflow_DefaultWorkerId_IsValidNonEmptyUuid, logMessages)) allPassed = false;
            if (!RunTest("RubricLoader_LoadsBundledFireRubric", Test_RubricLoader_LoadsBundledFireRubric, logMessages)) allPassed = false;
            if (!RunTest("LoadedFireRubric_IdentityAndVersion", Test_LoadedFireRubric_IdentityAndVersion, logMessages)) allPassed = false;
            if (!RunTest("LoadedFireRubric_AllNineRulesAvailableAndConfigured", Test_LoadedFireRubric_AllNineRulesAvailableAndConfigured, logMessages)) allPassed = false;
            if (!RunTest("Workflow_UsesLoadedRubricForEvaluation", Test_Workflow_UsesLoadedRubricForEvaluation, logMessages)) allPassed = false;
            if (!RunTest("DualInput_Step1ToStep9Equivalence", Test_DualInput_Step1ToStep9Equivalence, logMessages)) allPassed = false;
            if (!RunTest("DualInput_InvalidInteractionsDoNotAdvanceWorkflow", Test_DualInput_InvalidInteractionsDoNotAdvanceWorkflow, logMessages)) allPassed = false;
            if (!RunTest("DualInput_DuplicateTapsPreventDuplicateEvents", Test_DualInput_DuplicateTapsPreventDuplicateEvents, logMessages)) allPassed = false;
            if (!RunTest("DualInput_FullScenarioMixedARAndUI_Scores100AndPasses", Test_DualInput_FullScenarioMixedARAndUI_Scores100AndPasses, logMessages)) allPassed = false;
            if (!RunTest("DualInput_InvalidMarker_IncursPenaltyAndAllowsRecovery", Test_DualInput_InvalidMarker_IncursPenaltyAndAllowsRecovery, logMessages)) allPassed = false;
            if (!RunTest("Workflow_FinalizeAttempt_CompletedAttempt_EmitsEventAndExposesAttempt", Test_Workflow_FinalizeAttempt_CompletedAttempt_EmitsEventAndExposesAttempt, logMessages)) allPassed = false;
            if (!RunTest("Workflow_FinalizeAttempt_PrematureCall_Rejected", Test_Workflow_FinalizeAttempt_PrematureCall_Rejected, logMessages)) allPassed = false;
            if (!RunTest("Workflow_FinalizeAttempt_DuplicateCall_RejectedAndZeroDuplicateEvents", Test_Workflow_FinalizeAttempt_DuplicateCall_RejectedAndZeroDuplicateEvents, logMessages)) allPassed = false;
            if (!RunTest("Workflow_FinalizeAttempt_NullOrResetAttempt_RejectedSafely", Test_Workflow_FinalizeAttempt_NullOrResetAttempt_RejectedSafely, logMessages)) allPassed = false;
            if (!RunTest("Workflow_FinalizeAttempt_PreservesScoreAndOutcomeUnchanged", Test_Workflow_FinalizeAttempt_PreservesScoreAndOutcomeUnchanged, logMessages)) allPassed = false;
            if (!RunTest("SummaryViewModel_SyncPreparedFlag_UpdatesOnFinalization", Test_SummaryViewModel_SyncPreparedFlag_UpdatesOnFinalization, logMessages)) allPassed = false;
            if (!RunTest("RuntimeE2E_CompleteNineStepJourney_AllStagesAndEvents", Test_RuntimeE2E_CompleteNineStepJourney_AllStagesAndEvents, logMessages)) allPassed = false;
            if (!RunTest("RuntimeE2E_InvalidActionsStrictlyRejectedAtEveryStep", Test_RuntimeE2E_InvalidActionsStrictlyRejectedAtEveryStep, logMessages)) allPassed = false;
            if (!RunTest("RuntimeE2E_AlreadyCompletedMarkersCannotBeRepeated", Test_RuntimeE2E_AlreadyCompletedMarkersCannotBeRepeated, logMessages)) allPassed = false;
            if (!RunTest("RuntimeE2E_OutboxFinalizationWorkflowAndHookContract", Test_RuntimeE2E_OutboxFinalizationWorkflowAndHookContract, logMessages)) allPassed = false;
            if (!RunTest("RuntimeE2E_AssessmentSummaryUI_DisplaysAndFinalizes", Test_RuntimeE2E_AssessmentSummaryUI_DisplaysAndFinalizes, logMessages)) allPassed = false;
            if (!RunTest("RuntimeE2E_Retake_ClearsAllFinalizationState_FreshAttemptId", Test_RuntimeE2E_Retake_ClearsAllFinalizationState_FreshAttemptId, logMessages)) allPassed = false;
            if (!RunTest("ArFloatingLabel_CreationAndScale", Test_ArFloatingLabel_CreationAndScale, logMessages)) allPassed = false;
            if (!RunTest("ArFloatingLabel_OrientationAlignsWithCamera", Test_ArFloatingLabel_OrientationAlignsWithCamera, logMessages)) allPassed = false;
            if (!RunTest("FireHazardMarker_VisualStructureAndLabels", Test_FireHazardMarker_VisualStructureAndLabels, logMessages)) allPassed = false;
            if (!RunTest("AllFireMarkers_HaveArFloatingLabels", Test_AllFireMarkers_HaveArFloatingLabels, logMessages)) allPassed = false;
            if (!RunTest("TouchGestureFilter_StationaryTapAccepted", Test_TouchGestureFilter_StationaryTapAccepted, logMessages)) allPassed = false;
            if (!RunTest("TouchGestureFilter_SwipeRejected", Test_TouchGestureFilter_SwipeRejected, logMessages)) allPassed = false;
            if (!RunTest("TouchGestureFilter_HoldDragRejected", Test_TouchGestureFilter_HoldDragRejected, logMessages)) allPassed = false;
            if (!RunTest("TouchGestureFilter_UiStartOrEndRejected", Test_TouchGestureFilter_UiStartOrEndRejected, logMessages)) allPassed = false;
            if (!RunTest("TapGatedButton_SwipeDoesNotTrigger", Test_TapGatedButton_SwipeDoesNotTrigger, logMessages)) allPassed = false;
            if (!RunTest("FireArInteractionController_Process3DMarkerHit_ActionGating", Test_FireArInteractionController_Process3DMarkerHit_ActionGating, logMessages)) allPassed = false;
            if (!RunTest("Nav_Step3Alarm_ShowsNextButton", Test_Nav_Step3Alarm_ShowsNextButton, logMessages)) allPassed = false;
            if (!RunTest("Nav_NextButton_AdvancesExactlyOneStep", Test_Nav_NextButton_AdvancesExactlyOneStep, logMessages)) allPassed = false;
            if (!RunTest("Nav_BackButton_ReturnsExactlyOneCompletedStep", Test_Nav_BackButton_ReturnsExactlyOneCompletedStep, logMessages)) allPassed = false;
            if (!RunTest("Nav_SwipeDoesNotAdvanceWorkflow", Test_Nav_SwipeDoesNotAdvanceWorkflow, logMessages)) allPassed = false;
            if (!RunTest("Nav_StepLocking_NextUnavailableBeforeRequiredAction", Test_Nav_StepLocking_NextUnavailableBeforeRequiredAction, logMessages)) allPassed = false;
            if (!RunTest("Nav_IncorrectActionDoesNotAdvance", Test_Nav_IncorrectActionDoesNotAdvance, logMessages)) allPassed = false;
            if (!RunTest("Nav_CorrectActionCompletesStep", Test_Nav_CorrectActionCompletesStep, logMessages)) allPassed = false;
            if (!RunTest("Nav_FullNineStepSequentialFlow", Test_Nav_FullNineStepSequentialFlow, logMessages)) allPassed = false;
            if (!RunTest("Nav_RetakeStartsFreshAttempt", Test_Nav_RetakeStartsFreshAttempt, logMessages)) allPassed = false;
            if (!RunTest("Nav_Idempotency_GoingBackDoesNotDuplicateScoringEvents", Test_Nav_Idempotency_GoingBackDoesNotDuplicateScoringEvents, logMessages)) allPassed = false;

            // STEP 8B — Test Suite Additions
            if (!RunTest("FireAudioService_GeneratesSynthesizedClipsWithoutAssetFiles", Test_FireAudioService_GeneratesSynthesizedClipsWithoutAssetFiles, logMessages)) allPassed = false;
            if (!RunTest("FireAudioService_RespectsMuteAndVolumeSettings", Test_FireAudioService_RespectsMuteAndVolumeSettings, logMessages)) allPassed = false;
            if (!RunTest("FireAudioService_EmergencyAlarmAudioTriggered", Test_FireAudioService_EmergencyAlarmAudioTriggered, logMessages)) allPassed = false;
            if (!RunTest("EvacuationWorkflow_StrictSequentialProgression", Test_EvacuationWorkflow_StrictSequentialProgression, logMessages)) allPassed = false;
            if (!RunTest("EvacuationWorkflow_DirectToExitFailsGracefully", Test_EvacuationWorkflow_DirectToExitFailsGracefully, logMessages)) allPassed = false;
            if (!RunTest("PassInteraction_TapButtonsTriggerCorrectSubActions", Test_PassInteraction_TapButtonsTriggerCorrectSubActions, logMessages)) allPassed = false;
            if (!RunTest("PassInteraction_InvalidGestureDoesNotProgress", Test_PassInteraction_InvalidGestureDoesNotProgress, logMessages)) allPassed = false;
            if (!RunTest("GuidedStepNavigator_StepActionTextsMatchInteraction", Test_GuidedStepNavigator_StepActionTextsMatchInteraction, logMessages)) allPassed = false;
            if (!RunTest("FeedbackUI_SinglePrimaryActionButtonEnforced", Test_FeedbackUI_SinglePrimaryActionButtonEnforced, logMessages)) allPassed = false;
            if (!RunTest("FeedbackUI_SoundSettingsTogglePersists", Test_FeedbackUI_SoundSettingsTogglePersists, logMessages)) allPassed = false;
            if (!RunTest("AssessmentDeductionExplanation_HumanReadableFormat", Test_AssessmentDeductionExplanation_HumanReadableFormat, logMessages)) allPassed = false;
            if (!RunTest("ArVisual_BillboardLabelsOrientationTowardsCamera", Test_ArVisual_BillboardLabelsOrientationTowardsCamera, logMessages)) allPassed = false;
            if (!RunTest("ArVisual_LabelScalesAreReadableAndNonOverlapping", Test_ArVisual_LabelScalesAreReadableAndNonOverlapping, logMessages)) allPassed = false;
            if (!RunTest("ProceduralFire_ClassEIndicatorActive", Test_ProceduralFire_ClassEIndicatorActive, logMessages)) allPassed = false;
            if (!RunTest("AssessmentModal_OnlyOpensViaExplicitTap", Test_AssessmentModal_OnlyOpensViaExplicitTap, logMessages)) allPassed = false;
            if (!RunTest("FullScenario_CompletesStep1Through9_PassScore", Test_FullScenario_CompletesStep1Through9_PassScore, logMessages)) allPassed = false;
            if (!RunTest("FullScenario_UnsafeActionsResultInClearDeductions", Test_FullScenario_UnsafeActionsResultInClearDeductions, logMessages)) allPassed = false;

            // COMMON WORKER APP FOUNDATION — Test Suite Additions
            if (!RunTest("WorkerHome_LoadsWithAppTitleAndProfile", Test_WorkerHome_LoadsWithAppTitleAndProfile, logMessages)) allPassed = false;
            if (!RunTest("WorkerHome_FireModuleAvailable_GasModuleComingSoon", Test_WorkerHome_FireModuleAvailable_GasModuleComingSoon, logMessages)) allPassed = false;
            if (!RunTest("WorkerHome_SettingsPanelOpensAndCloses", Test_WorkerHome_SettingsPanelOpensAndCloses, logMessages)) allPassed = false;
            if (!RunTest("WorkerHome_SoundSettingsPersistViaAudioService", Test_WorkerHome_SoundSettingsPersistViaAudioService, logMessages)) allPassed = false;
            if (!RunTest("WorkerHome_LanguageSelectionPersistsLocales", Test_WorkerHome_LanguageSelectionPersistsLocales, logMessages)) allPassed = false;
            if (!RunTest("WorkerHome_OfflineIndicatorReflectsNetworkStatus", Test_WorkerHome_OfflineIndicatorReflectsNetworkStatus, logMessages)) allPassed = false;
            if (!RunTest("WorkerHome_StartTrainingActivatesFireModule", Test_WorkerHome_StartTrainingActivatesFireModule, logMessages)) allPassed = false;
            if (!RunTest("WorkerHome_BackNavigationReturnsToHome", Test_WorkerHome_BackNavigationReturnsToHome, logMessages)) allPassed = false;

            // FOUNDATION FIX — 17 REGRESSION TESTS
            if (!RunTest("WorkerHome_VisibleInHomeState", Test_WorkerHome_VisibleInHomeState, logMessages)) allPassed = false;
            if (!RunTest("WorkerHome_StartTrainingHidesHomeUI", Test_WorkerHome_StartTrainingHidesHomeUI, logMessages)) allPassed = false;
            if (!RunTest("FireTraining_IsOnlyActiveTrainingUI", Test_FireTraining_IsOnlyActiveTrainingUI, logMessages)) allPassed = false;
            if (!RunTest("WorkerHome_InputDisabledDuringTraining", Test_WorkerHome_InputDisabledDuringTraining, logMessages)) allPassed = false;
            if (!RunTest("FireTraining_ExitRestoresHomeUI", Test_FireTraining_ExitRestoresHomeUI, logMessages)) allPassed = false;
            if (!RunTest("WorkerHome_SettingsButtonOpensSettings", Test_WorkerHome_SettingsButtonOpensSettings, logMessages)) allPassed = false;
            if (!RunTest("WorkerHome_SettingsClosesCorrectly", Test_WorkerHome_SettingsClosesCorrectly, logMessages)) allPassed = false;
            if (!RunTest("FireAudio_SoundEffectsSettingPersists", Test_FireAudio_SoundEffectsSettingPersists, logMessages)) allPassed = false;
            if (!RunTest("FireAudio_EmergencyAlarmSettingPersists", Test_FireAudio_EmergencyAlarmSettingPersists, logMessages)) allPassed = false;
            if (!RunTest("FireAudio_EmergencyAlarmOnStartsOrPermitsAlarm", Test_FireAudio_EmergencyAlarmOnStartsOrPermitsAlarm, logMessages)) allPassed = false;
            if (!RunTest("FireAudio_EmergencyAlarmOffStopsActiveAlarm", Test_FireAudio_EmergencyAlarmOffStopsActiveAlarm, logMessages)) allPassed = false;
            if (!RunTest("FireAudio_RepeatedAlarmToggleIdempotent", Test_FireAudio_RepeatedAlarmToggleIdempotent, logMessages)) allPassed = false;
            if (!RunTest("FireAudio_LeavingFireStopsActiveAlarm", Test_FireAudio_LeavingFireStopsActiveAlarm, logMessages)) allPassed = false;
            if (!RunTest("FireAudio_RetakeDoesNotInheritPreviousAlarm", Test_FireAudio_RetakeDoesNotInheritPreviousAlarm, logMessages)) allPassed = false;
            if (!RunTest("FireTraining_HUDAlarmButtonTogglesAlarm", Test_FireTraining_HUDAlarmButtonTogglesAlarm, logMessages)) allPassed = false;
            if (!RunTest("SettingsModal_BlocksRaycastsUnderneath", Test_SettingsModal_BlocksRaycastsUnderneath, logMessages)) allPassed = false;
            if (!RunTest("FireAssessmentSummary_ReturnToHomeExitsCleanly", Test_FireAssessmentSummary_ReturnToHomeExitsCleanly, logMessages)) allPassed = false;
            if (!RunTest("Localization_EnglishLocaleLoads", Test_Localization_EnglishLocaleLoads, logMessages)) allPassed = false;
            if (!RunTest("Localization_HindiLocaleLoads", Test_Localization_HindiLocaleLoads, logMessages)) allPassed = false;
            if (!RunTest("Localization_SantaliLocaleLoads", Test_Localization_SantaliLocaleLoads, logMessages)) allPassed = false;
            if (!RunTest("Localization_RequiredKeysExistInAllLocales", Test_Localization_RequiredKeysExistInAllLocales, logMessages)) allPassed = false;
            if (!RunTest("Localization_RuntimeLocaleSwitching", Test_Localization_RuntimeLocaleSwitching, logMessages)) allPassed = false;
            if (!RunTest("Localization_SelectedLocalePersists", Test_Localization_SelectedLocalePersists, logMessages)) allPassed = false;
            if (!RunTest("Localization_MissingKeyDoesNotCrash", Test_Localization_MissingKeyDoesNotCrash, logMessages)) allPassed = false;
            if (!RunTest("Localization_EnglishFallbackWhenTranslationMissing", Test_Localization_EnglishFallbackWhenTranslationMissing, logMessages)) allPassed = false;
            if (!RunTest("Localization_FontFallbackAssetsPresent", Test_Localization_FontFallbackAssetsPresent, logMessages)) allPassed = false;

            return allPassed;
        }

        private static bool RunTest(string testName, Action testAction, List<string> log)
        {
            try
            {
                testAction();
                log.Add($"[PASS] {testName}");
                return true;
            }
            catch (Exception ex)
            {
                log.Add($"[FAIL] {testName}: {ex.Message}");
                return false;
            }
        }

        public static void Test_EventCreationMatchesSchema()
        {
            var evt = new TrainingEvent
            {
                ModuleId = FireTrainingWorkflow.ModuleId,
                ContentVersion = FireTrainingWorkflow.ContentVersion,
                StepId = FireTrainingWorkflow.StepDetectHazard,
                EventType = "step_completed",
                ActionId = FireTrainingWorkflow.ActionDetectHazard,
                TargetId = FireTrainingWorkflow.TargetElectricalConveyorFire,
                Outcome = "success"
            };

            if (evt.ModuleId != "fire-explosion-response") throw new Exception("ModuleId mismatch");
            if (evt.ContentVersion != "1.0.0") throw new Exception("ContentVersion mismatch");
            if (evt.StepId != "step_detect_hazard") throw new Exception("StepId mismatch");
            if (evt.EventType != "step_completed") throw new Exception("EventType mismatch");
            if (evt.ActionId != "detect_hazard_acknowledged") throw new Exception("ActionId mismatch");
            if (evt.Outcome != "success") throw new Exception("Outcome mismatch");
            if (string.IsNullOrEmpty(evt.EventId)) throw new Exception("EventId was not generated");
        }

        public static void Test_EventBusRecordsAndNotifies()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            TrainingEvent received = null;
            bus.OnEventDispatched += (e) => received = e;

            var evt = new TrainingEvent
            {
                ModuleId = FireTrainingWorkflow.ModuleId,
                StepId = FireTrainingWorkflow.StepDetectHazard,
                EventType = "step_completed",
                ActionId = FireTrainingWorkflow.ActionDetectHazard
            };

            bus.Dispatch(evt);

            if (received == null) throw new Exception("Subscriber was not invoked");
            if (received.EventId != evt.EventId) throw new Exception("Received incorrect event");
            if (bus.DispatchedEvents.Count != 1) throw new Exception("Event was not stored in history");
        }

        public static void Test_EventPayloadIntegrity()
        {
            var evt = new TrainingEvent
            {
                Payload = new Dictionary<string, string>
                {
                    { "rule_id", FireTrainingWorkflow.RuleDetectHazard },
                    { "action_id", FireTrainingWorkflow.ActionDetectHazard }
                }
            };

            if (evt.GetPayloadValue("rule_id") != "rule_detect_hazard")
                throw new Exception("Payload rule_id missing or invalid");

            if (evt.GetPayloadValue("nonexistent") != null)
                throw new Exception("Payload returned value for nonexistent key");
        }

        public static void Test_SuccessfulHazardIdentificationEvent()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AwaitingIdentification);

            bool success = workflow.SubmitHazardIdentification(
                FireTrainingWorkflow.TargetElectricalConveyorFire,
                bus,
                out var emittedEvent);

            if (!success) throw new Exception("Hazard identification should succeed with valid target");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingAlarm)
                throw new Exception($"Expected AwaitingAlarm stage, but was {workflow.CurrentStage}");
            if (workflow.CurrentStepId != FireTrainingWorkflow.StepRaiseAlarm)
                throw new Exception($"Expected step_raise_alarm, but was {workflow.CurrentStepId}");

            if (emittedEvent == null) throw new Exception("Emitted event was null");
            if (emittedEvent.ModuleId != "fire-explosion-response") throw new Exception("ModuleId mismatch");
            if (emittedEvent.ContentVersion != "1.0.0") throw new Exception("ContentVersion mismatch");
            if (emittedEvent.StepId != "step_identify_hazard") throw new Exception("StepId mismatch");
            if (emittedEvent.EventType != "hazard_identified") throw new Exception("EventType mismatch");
            if (emittedEvent.ActionId != "identify") throw new Exception("ActionId mismatch");
            if (emittedEvent.TargetId != "hazard_electrical_conveyor_fire") throw new Exception("TargetId mismatch");
            if (emittedEvent.Outcome != "success") throw new Exception("Outcome mismatch");
            if (emittedEvent.GetPayloadValue("rule_id") != "rule_identify_hazard") throw new Exception("rule_id mismatch");
            if (emittedEvent.GetPayloadValue("hazard_class") != "class_e_electrical") throw new Exception("hazard_class mismatch");
        }

        public static void Test_SuccessfulRaiseAlarmEvent()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AwaitingAlarm);

            bool success = workflow.SubmitRaiseAlarm(
                FireTrainingWorkflow.ActionRaiseAlarm,
                bus,
                out var emittedEvent);

            if (!success) throw new Exception("Raise alarm should succeed with valid action");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingExtinguisherSelection)
                throw new Exception($"Expected AwaitingExtinguisherSelection stage, but was {workflow.CurrentStage}");
            if (workflow.CurrentStepId != FireTrainingWorkflow.StepSelectExtinguisher)
                throw new Exception($"Expected step_select_extinguisher, but was {workflow.CurrentStepId}");

            if (emittedEvent == null) throw new Exception("Emitted event was null");
            if (emittedEvent.ModuleId != "fire-explosion-response") throw new Exception("ModuleId mismatch");
            if (emittedEvent.ContentVersion != "1.0.0") throw new Exception("ContentVersion mismatch");
            if (emittedEvent.StepId != "step_raise_alarm") throw new Exception("StepId mismatch");
            if (emittedEvent.EventType != "alarm_raised") throw new Exception("EventType mismatch");
            if (emittedEvent.ActionId != "manual_call_point_activated") throw new Exception("ActionId mismatch");
            if (emittedEvent.Outcome != "success") throw new Exception("Outcome mismatch");
            if (emittedEvent.GetPayloadValue("rule_id") != "rule_raise_alarm") throw new Exception("rule_id mismatch");
        }

        public static void Test_CorrectEventOrdering()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            // Step 1: Detect Hazard
            bool s1 = workflow.ConfirmHazardDetected(bus, out var e1);
            if (!s1) throw new Exception("Step 1 detect hazard failed");

            // Step 2: Identify Hazard
            bool s2 = workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out var e2);
            if (!s2) throw new Exception("Step 2 identify hazard failed");

            // Step 3: Raise Alarm
            bool s3 = workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out var e3);
            if (!s3) throw new Exception("Step 3 raise alarm failed");

            var events = bus.DispatchedEvents;
            if (events.Count != 3) throw new Exception($"Expected 3 events, got {events.Count}");

            // Verify order:
            if (events[0].StepId != "step_detect_hazard") throw new Exception("First event was not step_detect_hazard");
            if (events[1].StepId != "step_identify_hazard") throw new Exception("Second event was not step_identify_hazard");
            if (events[2].StepId != "step_raise_alarm") throw new Exception("Third event was not step_raise_alarm");

            // Verify rule IDs:
            if (events[0].GetPayloadValue("rule_id") != "rule_detect_hazard") throw new Exception("Rule 1 mismatch");
            if (events[1].GetPayloadValue("rule_id") != "rule_identify_hazard") throw new Exception("Rule 2 mismatch");
            if (events[2].GetPayloadValue("rule_id") != "rule_raise_alarm") throw new Exception("Rule 3 mismatch");

            // Verify outcomes:
            if (events[0].Outcome != "success" || events[1].Outcome != "success" || events[2].Outcome != "success")
                throw new Exception("All event outcomes should be success");
        }

        public static void Test_InvalidActionDoesNotAdvanceFlow()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AwaitingIdentification);

            // 1. Invalid hazard identification target
            bool s1 = workflow.SubmitHazardIdentification("wrong_hazard_chemical_spill", bus, out var e1);
            if (s1) throw new Exception("Wrong hazard target should return false");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingIdentification)
                throw new Exception("Stage should remain AwaitingIdentification after invalid identification");
            if (workflow.CurrentStepId != FireTrainingWorkflow.StepIdentifyHazard)
                throw new Exception("CurrentStepId should remain step_identify_hazard");

            // 2. Cannot raise alarm while in AwaitingIdentification
            bool s2 = workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out var e2);
            if (s2) throw new Exception("SubmitRaiseAlarm should return false when not in AwaitingAlarm stage");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingIdentification)
                throw new Exception("Stage should not change when raising alarm prematurely");

            // Advance to AwaitingAlarm with valid identification
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingAlarm)
                throw new Exception("Workflow failed to advance to AwaitingAlarm");

            // 3. Invalid raise alarm action ID
            bool s3 = workflow.SubmitRaiseAlarm("invalid_call_action", bus, out var e3);
            if (s3) throw new Exception("Invalid raise alarm action ID should return false");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingAlarm)
                throw new Exception("Stage should remain AwaitingAlarm after invalid action ID");
        }

        public static void Test_SuccessfulCO2ExtinguisherSelection()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AwaitingExtinguisherSelection);

            bool success = workflow.SubmitSelectExtinguisher(
                FireTrainingWorkflow.TargetExtinguisherCO2,
                bus,
                out var emittedEvent);

            if (!success) throw new Exception("CO2 selection should succeed");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingSafeDistance && workflow.CurrentStage != FireWorkflowStage.ExtinguisherSelected)
                throw new Exception($"Expected AwaitingSafeDistance stage, got {workflow.CurrentStage}");

            if (emittedEvent == null) throw new Exception("Emitted event was null");
            if (emittedEvent.ModuleId != "fire-explosion-response") throw new Exception("ModuleId mismatch");
            if (emittedEvent.ContentVersion != "1.0.0") throw new Exception("ContentVersion mismatch");
            if (emittedEvent.StepId != "step_select_extinguisher") throw new Exception("StepId mismatch");
            if (emittedEvent.EventType != "extinguisher_selected") throw new Exception("EventType mismatch");
            if (emittedEvent.ActionId != "select") throw new Exception("ActionId mismatch");
            if (emittedEvent.TargetId != "extinguisher_co2") throw new Exception("TargetId mismatch");
            if (emittedEvent.Outcome != "success") throw new Exception("Outcome mismatch");
            if (emittedEvent.GetPayloadValue("rule_id") != "rule_select_extinguisher") throw new Exception("rule_id mismatch");
            if (emittedEvent.GetPayloadValue("tool_class") != "co2_extinguisher") throw new Exception("tool_class mismatch");
        }

        public static void Test_IncorrectWaterExtinguisherSelection()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AwaitingExtinguisherSelection);

            bool success = workflow.SubmitSelectExtinguisher(
                FireTrainingWorkflow.TargetExtinguisherWater,
                bus,
                out var emittedEvent);

            if (success) throw new Exception("Water extinguisher selection should fail for electrical fire");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingExtinguisherSelection)
                throw new Exception("Workflow stage should not advance after incorrect water selection");

            if (emittedEvent == null) throw new Exception("Failure event should be emitted");
            if (emittedEvent.StepId != "step_select_extinguisher") throw new Exception("StepId mismatch");
            if (emittedEvent.EventType != "extinguisher_selected") throw new Exception("EventType mismatch");
            if (emittedEvent.TargetId != "extinguisher_water") throw new Exception("TargetId mismatch");
            if (emittedEvent.Outcome != "failure") throw new Exception("Outcome should be failure");
            if (emittedEvent.GetPayloadValue("rule_id") != "rule_select_extinguisher") throw new Exception("rule_id mismatch");
        }

        public static void Test_IncorrectFoamExtinguisherSelection()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AwaitingExtinguisherSelection);

            bool success = workflow.SubmitSelectExtinguisher(
                FireTrainingWorkflow.TargetExtinguisherFoam,
                bus,
                out var emittedEvent);

            if (success) throw new Exception("Foam extinguisher selection should fail for electrical fire");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingExtinguisherSelection)
                throw new Exception("Workflow stage should not advance after incorrect foam selection");

            if (emittedEvent == null) throw new Exception("Failure event should be emitted");
            if (emittedEvent.TargetId != "extinguisher_foam") throw new Exception("TargetId mismatch");
            if (emittedEvent.Outcome != "failure") throw new Exception("Outcome should be failure");
        }

        public static void Test_PrematureExtinguisherSelectionRejected()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();

            // 1. Cannot select extinguisher during AwaitingIdentification
            workflow.SetStage(FireWorkflowStage.AwaitingIdentification);
            bool s1 = workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out var e1);
            if (s1) throw new Exception("Premature extinguisher selection during AwaitingIdentification should return false");
            if (e1 != null) throw new Exception("No event should be emitted for premature action");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingIdentification) throw new Exception("Stage should remain AwaitingIdentification");

            // 2. Cannot select extinguisher during AwaitingAlarm
            workflow.SetStage(FireWorkflowStage.AwaitingAlarm);
            bool s2 = workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out var e2);
            if (s2) throw new Exception("Premature extinguisher selection during AwaitingAlarm should return false");
            if (e2 != null) throw new Exception("No event should be emitted for premature action");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingAlarm) throw new Exception("Stage should remain AwaitingAlarm");
        }

        public static void Test_Step3ToStep4EventOrdering()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            // Step 1: Detect Hazard
            bool s1 = workflow.ConfirmHazardDetected(bus, out var e1);
            if (!s1) throw new Exception("Step 1 detect hazard failed");

            // Step 2: Identify Hazard
            bool s2 = workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out var e2);
            if (!s2) throw new Exception("Step 2 identify hazard failed");

            // Step 3: Raise Alarm
            bool s3 = workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out var e3);
            if (!s3) throw new Exception("Step 3 raise alarm failed");

            // Step 4: Select Extinguisher
            bool s4 = workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out var e4);
            if (!s4) throw new Exception("Step 4 select extinguisher failed");

            var events = bus.DispatchedEvents;
            if (events.Count != 4) throw new Exception($"Expected 4 events, got {events.Count}");

            // Verify sequential ordering:
            if (events[0].StepId != "step_detect_hazard") throw new Exception("First event was not step_detect_hazard");
            if (events[1].StepId != "step_identify_hazard") throw new Exception("Second event was not step_identify_hazard");
            if (events[2].StepId != "step_raise_alarm") throw new Exception("Third event was not step_raise_alarm");
            if (events[3].StepId != "step_select_extinguisher") throw new Exception("Fourth event was not step_select_extinguisher");

            // Verify exact rule IDs:
            if (events[0].GetPayloadValue("rule_id") != "rule_detect_hazard") throw new Exception("Rule 1 mismatch");
            if (events[1].GetPayloadValue("rule_id") != "rule_identify_hazard") throw new Exception("Rule 2 mismatch");
            if (events[2].GetPayloadValue("rule_id") != "rule_raise_alarm") throw new Exception("Rule 3 mismatch");
            if (events[3].GetPayloadValue("rule_id") != "rule_select_extinguisher") throw new Exception("Rule 4 mismatch");

            // Verify all outcomes are success:
            for (int i = 0; i < 4; i++)
            {
                if (events[i].Outcome != "success") throw new Exception($"Event {i} outcome was not success");
            }
        }

        public static void Test_SuccessfulSafeDistanceAction()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AwaitingSafeDistance);

            // Standoff distance of 2.5m (>= 2.0m minimum safe distance)
            bool success = workflow.SubmitDistanceDecision(2.5f, bus, out var emittedEvent);

            if (!success) throw new Exception("Safe distance decision should succeed for 2.5m");
            if (workflow.CurrentStage != FireWorkflowStage.SafeDistanceMaintained)
                throw new Exception($"Expected SafeDistanceMaintained stage, got {workflow.CurrentStage}");
            if (workflow.CurrentStepId != "step_use_extinguisher")
                throw new Exception($"Expected next step step_use_extinguisher, got {workflow.CurrentStepId}");

            if (emittedEvent == null) throw new Exception("Emitted event was null");
            if (emittedEvent.ModuleId != "fire-explosion-response") throw new Exception("ModuleId mismatch");
            if (emittedEvent.ContentVersion != "1.0.0") throw new Exception("ContentVersion mismatch");
            if (emittedEvent.StepId != "step_maintain_distance") throw new Exception("StepId mismatch");
            if (emittedEvent.EventType != "decision_made") throw new Exception("EventType mismatch");
            if (emittedEvent.ActionId != "decide") throw new Exception("ActionId mismatch");
            if (emittedEvent.TargetId != "standoff_distance_2m_maintained") throw new Exception("TargetId mismatch");
            if (emittedEvent.Outcome != "success") throw new Exception("Outcome mismatch");
            if (emittedEvent.GetPayloadValue("rule_id") != "rule_maintain_distance") throw new Exception("rule_id mismatch");
            if (emittedEvent.GetPayloadValue("decision_id") != "standoff_distance_2m_maintained") throw new Exception("decision_id mismatch");
        }

        public static void Test_UnsafeTooCloseActionRejected()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AwaitingSafeDistance);

            // Standoff distance of 1.2m (< 2.0m minimum safe distance)
            bool success = workflow.SubmitDistanceDecision(1.2f, bus, out var emittedEvent);

            if (success) throw new Exception("Unsafe too-close action should be rejected");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingSafeDistance)
                throw new Exception("Workflow stage should not advance after unsafe distance decision");
            if (workflow.CurrentStepId != FireTrainingWorkflow.StepMaintainDistance)
                throw new Exception($"CurrentStepId should remain step_maintain_distance, got {workflow.CurrentStepId}");

            if (emittedEvent == null) throw new Exception("Failure event should be emitted");
            if (emittedEvent.StepId != "step_maintain_distance") throw new Exception("StepId mismatch");
            if (emittedEvent.EventType != "decision_made") throw new Exception("EventType mismatch");
            if (emittedEvent.TargetId != "standoff_distance_too_close") throw new Exception("TargetId mismatch");
            if (emittedEvent.Outcome != "failure") throw new Exception("Outcome should be failure");
            if (emittedEvent.GetPayloadValue("rule_id") != "rule_maintain_distance") throw new Exception("rule_id mismatch");
            if (emittedEvent.GetPayloadValue("decision_id") != "standoff_distance_too_close") throw new Exception("decision_id mismatch");
        }

        public static void Test_PrematureSafeDistanceActionRejected()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();

            // 1. Cannot submit distance decision during AwaitingIdentification
            workflow.SetStage(FireWorkflowStage.AwaitingIdentification);
            bool s1 = workflow.SubmitDistanceDecision(2.5f, bus, out var e1);
            if (s1) throw new Exception("Premature distance decision during AwaitingIdentification should return false");
            if (e1 != null) throw new Exception("No event should be emitted for premature action");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingIdentification)
                throw new Exception("Stage should remain AwaitingIdentification");

            // 2. Cannot submit distance decision during AwaitingAlarm
            workflow.SetStage(FireWorkflowStage.AwaitingAlarm);
            bool s2 = workflow.SubmitDistanceDecision(2.5f, bus, out var e2);
            if (s2) throw new Exception("Premature distance decision during AwaitingAlarm should return false");
            if (e2 != null) throw new Exception("No event should be emitted for premature action");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingAlarm)
                throw new Exception("Stage should remain AwaitingAlarm");

            // 3. Cannot submit distance decision during AwaitingExtinguisherSelection
            workflow.SetStage(FireWorkflowStage.AwaitingExtinguisherSelection);
            bool s3 = workflow.SubmitDistanceDecision(2.5f, bus, out var e3);
            if (s3) throw new Exception("Premature distance decision during AwaitingExtinguisherSelection should return false");
            if (e3 != null) throw new Exception("No event should be emitted for premature action");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingExtinguisherSelection)
                throw new Exception("Stage should remain AwaitingExtinguisherSelection");
        }

        public static void Test_Step4ToStep5EventOrdering()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            // Step 1: Detect Hazard
            bool s1 = workflow.ConfirmHazardDetected(bus, out var e1);
            if (!s1) throw new Exception("Step 1 detect hazard failed");

            // Step 2: Identify Hazard
            bool s2 = workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out var e2);
            if (!s2) throw new Exception("Step 2 identify hazard failed");

            // Step 3: Raise Alarm
            bool s3 = workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out var e3);
            if (!s3) throw new Exception("Step 3 raise alarm failed");

            // Step 4: Select Extinguisher
            bool s4 = workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out var e4);
            if (!s4) throw new Exception("Step 4 select extinguisher failed");

            // Step 5: Maintain Safe Distance
            bool s5 = workflow.SubmitDistanceDecision(2.5f, bus, out var e5);
            if (!s5) throw new Exception("Step 5 maintain safe distance failed");

            var events = bus.DispatchedEvents;
            if (events.Count != 5) throw new Exception($"Expected 5 events, got {events.Count}");

            // Verify sequential ordering of Step IDs across Steps 1 to 5:
            if (events[0].StepId != "step_detect_hazard") throw new Exception("First event was not step_detect_hazard");
            if (events[1].StepId != "step_identify_hazard") throw new Exception("Second event was not step_identify_hazard");
            if (events[2].StepId != "step_raise_alarm") throw new Exception("Third event was not step_raise_alarm");
            if (events[3].StepId != "step_select_extinguisher") throw new Exception("Fourth event was not step_select_extinguisher");
            if (events[4].StepId != "step_maintain_distance") throw new Exception("Fifth event was not step_maintain_distance");

            // Verify exact rubric Rule IDs across Steps 1 to 5:
            if (events[0].GetPayloadValue("rule_id") != "rule_detect_hazard") throw new Exception("Rule 1 mismatch");
            if (events[1].GetPayloadValue("rule_id") != "rule_identify_hazard") throw new Exception("Rule 2 mismatch");
            if (events[2].GetPayloadValue("rule_id") != "rule_raise_alarm") throw new Exception("Rule 3 mismatch");
            if (events[3].GetPayloadValue("rule_id") != "rule_select_extinguisher") throw new Exception("Rule 4 mismatch");
            if (events[4].GetPayloadValue("rule_id") != "rule_maintain_distance") throw new Exception("Rule 5 mismatch");

            // Verify all 5 outcomes are success:
            for (int i = 0; i < 5; i++)
            {
                if (events[i].Outcome != "success") throw new Exception($"Event {i} outcome was not success");
            }
        }

        public static void Test_Step6CannotBeginBeforeStep5()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();

            // 1. Cannot perform Step 6 at AwaitingIdentification
            workflow.SetStage(FireWorkflowStage.AwaitingIdentification);
            bool s1 = workflow.SubmitExtinguisherAction(FireTrainingWorkflow.ActionPullPin, bus, out var e1);
            if (s1) throw new Exception("Step 6 action should be rejected at AwaitingIdentification");
            if (e1 != null) throw new Exception("No event should be emitted for premature Step 6 action");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingIdentification)
                throw new Exception("Stage should remain AwaitingIdentification");

            // 2. Cannot perform Step 6 at AwaitingAlarm
            workflow.SetStage(FireWorkflowStage.AwaitingAlarm);
            bool s2 = workflow.SubmitExtinguisherAction(FireTrainingWorkflow.ActionPullPin, bus, out var e2);
            if (s2) throw new Exception("Step 6 action should be rejected at AwaitingAlarm");
            if (e2 != null) throw new Exception("No event should be emitted for premature Step 6 action");

            // 3. Cannot perform Step 6 at AwaitingExtinguisherSelection
            workflow.SetStage(FireWorkflowStage.AwaitingExtinguisherSelection);
            bool s3 = workflow.SubmitExtinguisherAction(FireTrainingWorkflow.ActionPullPin, bus, out var e3);
            if (s3) throw new Exception("Step 6 action should be rejected at AwaitingExtinguisherSelection");
            if (e3 != null) throw new Exception("No event should be emitted for premature Step 6 action");

            // 4. Cannot perform Step 6 at AwaitingSafeDistance (before safe distance confirmed)
            workflow.SetStage(FireWorkflowStage.AwaitingSafeDistance);
            bool s4 = workflow.SubmitExtinguisherAction(FireTrainingWorkflow.ActionPullPin, bus, out var e4);
            if (s4) throw new Exception("Step 6 action should be rejected at AwaitingSafeDistance before safe standoff");
            if (e4 != null) throw new Exception("No event should be emitted for premature Step 6 action");
        }

        public static void Test_PullPinAccepted()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.SafeDistanceMaintained);

            bool success = workflow.SubmitPullPin(bus, out var emittedEvent);

            if (!success) throw new Exception("Pull pin should succeed at SafeDistanceMaintained");
            if (workflow.CurrentStage != FireWorkflowStage.PinPulled)
                throw new Exception($"Expected PinPulled stage, got {workflow.CurrentStage}");
            if (workflow.CurrentStepId != FireTrainingWorkflow.StepUseExtinguisher)
                throw new Exception($"Expected StepId {FireTrainingWorkflow.StepUseExtinguisher}, got {workflow.CurrentStepId}");

            if (emittedEvent == null) throw new Exception("Emitted event was null");
            if (emittedEvent.StepId != "step_use_extinguisher") throw new Exception("StepId mismatch");
            if (emittedEvent.ActionId != "pull_pin") throw new Exception("ActionId mismatch");
            if (emittedEvent.Outcome != "success") throw new Exception("Outcome mismatch");
            if (emittedEvent.GetPayloadValue("rule_id") != "rule_use_extinguisher") throw new Exception("rule_id mismatch");
        }

        public static void Test_AimAcceptedOnlyAfterPin()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.SafeDistanceMaintained);

            // Cannot aim before pulling pin
            bool s1 = workflow.SubmitAim(bus, out var failEvent);
            if (s1) throw new Exception("Aim should fail before pin is pulled");
            if (workflow.CurrentStage != FireWorkflowStage.SafeDistanceMaintained)
                throw new Exception("Stage should not advance after premature aim");
            if (failEvent == null || failEvent.Outcome != "failure")
                throw new Exception("Failure event should be emitted for premature aim");

            // Pull pin first
            bool s2 = workflow.SubmitPullPin(bus, out _);
            if (!s2 || workflow.CurrentStage != FireWorkflowStage.PinPulled)
                throw new Exception("Pull pin failed to advance to PinPulled");

            // Now aim should succeed
            bool s3 = workflow.SubmitAim(bus, out var successEvent);
            if (!s3) throw new Exception("Aim should succeed after pin is pulled");
            if (workflow.CurrentStage != FireWorkflowStage.AimConfirmed)
                throw new Exception($"Expected AimConfirmed stage, got {workflow.CurrentStage}");
            if (successEvent == null || successEvent.Outcome != "success")
                throw new Exception("Success event should be emitted for valid aim");
            if (successEvent.ActionId != "aim") throw new Exception("ActionId should be aim");
        }

        public static void Test_SqueezeAcceptedOnlyAfterAim()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.PinPulled);

            // Cannot squeeze before aiming
            bool s1 = workflow.SubmitSqueeze(bus, out var failEvent);
            if (s1) throw new Exception("Squeeze should fail before aim");
            if (workflow.CurrentStage != FireWorkflowStage.PinPulled)
                throw new Exception("Stage should not advance after premature squeeze");

            // Aim first
            bool s2 = workflow.SubmitAim(bus, out _);
            if (!s2 || workflow.CurrentStage != FireWorkflowStage.AimConfirmed)
                throw new Exception("Aim failed to advance to AimConfirmed");

            // Now squeeze should succeed
            bool s3 = workflow.SubmitSqueeze(bus, out var successEvent);
            if (!s3) throw new Exception("Squeeze should succeed after aim");
            if (workflow.CurrentStage != FireWorkflowStage.HandleSqueezed)
                throw new Exception($"Expected HandleSqueezed stage, got {workflow.CurrentStage}");
            if (successEvent.ActionId != "squeeze") throw new Exception("ActionId should be squeeze");
            if (successEvent.Outcome != "success") throw new Exception("Outcome should be success");
        }

        public static void Test_SweepAcceptedOnlyAfterSqueeze()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AimConfirmed);

            // Cannot sweep before squeezing
            bool s1 = workflow.SubmitSweep(bus, out var failEvent);
            if (s1) throw new Exception("Sweep should fail before squeeze");
            if (workflow.CurrentStage != FireWorkflowStage.AimConfirmed)
                throw new Exception("Stage should not advance after premature sweep");

            // Squeeze first
            bool s2 = workflow.SubmitSqueeze(bus, out _);
            if (!s2 || workflow.CurrentStage != FireWorkflowStage.HandleSqueezed)
                throw new Exception("Squeeze failed to advance to HandleSqueezed");

            // Now sweep should succeed
            bool s3 = workflow.SubmitSweep(bus, out var successEvent);
            if (!s3) throw new Exception("Sweep should succeed after squeeze");
            if (workflow.CurrentStage != FireWorkflowStage.ExtinguisherDischarged)
                throw new Exception($"Expected ExtinguisherDischarged stage, got {workflow.CurrentStage}");
            if (workflow.CurrentStepId != "step_identify_exit")
                throw new Exception($"Expected next step step_identify_exit, got {workflow.CurrentStepId}");
            if (successEvent.ActionId != "sweep") throw new Exception("ActionId should be sweep");
            if (successEvent.Outcome != "success") throw new Exception("Outcome should be success");
        }

        public static void Test_WrongOutOfOrderActionRejected()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.SafeDistanceMaintained);

            // Attempting Squeeze while Pin is locked
            bool s1 = workflow.SubmitExtinguisherAction("squeeze", bus, out var e1);
            if (s1) throw new Exception("Out-of-order squeeze should be rejected");
            if (workflow.CurrentStage != FireWorkflowStage.SafeDistanceMaintained)
                throw new Exception("Stage should remain SafeDistanceMaintained");
            if (e1 == null || e1.Outcome != "failure")
                throw new Exception("Failure event should be emitted for out-of-order action");

            // Attempting completely invalid action
            bool s2 = workflow.SubmitExtinguisherAction("invalid_random_action", bus, out var e2);
            if (s2) throw new Exception("Invalid action should be rejected");
            if (workflow.CurrentStage != FireWorkflowStage.SafeDistanceMaintained)
                throw new Exception("Stage should remain SafeDistanceMaintained");
            if (e2 == null || e2.Outcome != "failure")
                throw new Exception("Failure event should be emitted for invalid action");

            // Valid pull pin
            workflow.SubmitPullPin(bus, out _);

            // Attempting sweep before aim/squeeze
            bool s3 = workflow.SubmitExtinguisherAction("sweep", bus, out var e3);
            if (s3) throw new Exception("Sweep should be rejected before aim/squeeze");
            if (workflow.CurrentStage != FireWorkflowStage.PinPulled)
                throw new Exception("Stage should remain PinPulled");
            if (e3 == null || e3.Outcome != "failure")
                throw new Exception("Failure event should be emitted");
        }

        public static void Test_FinalProcedureCompletedEventFields()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.SafeDistanceMaintained);

            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            bool success = workflow.SubmitSweep(bus, out var finalEvent);

            if (!success) throw new Exception("Sweep should succeed");
            if (finalEvent == null) throw new Exception("Final event was null");

            // Canonical Step 6 event requirements:
            if (finalEvent.StepId != "step_use_extinguisher") throw new Exception($"StepId mismatch: {finalEvent.StepId}");
            if (finalEvent.EventType != "procedure_completed") throw new Exception($"EventType mismatch: {finalEvent.EventType}");
            if (finalEvent.ActionId != "sweep") throw new Exception($"ActionId mismatch: {finalEvent.ActionId}");
            if (finalEvent.TargetId != "extinguisher_procedure") throw new Exception($"TargetId mismatch: {finalEvent.TargetId}");
            if (finalEvent.Outcome != "success") throw new Exception($"Outcome mismatch: {finalEvent.Outcome}");
            if (finalEvent.GetPayloadValue("rule_id") != "rule_use_extinguisher") throw new Exception("rule_id mismatch");
            if (finalEvent.ModuleId != "fire-explosion-response") throw new Exception("ModuleId mismatch");
            if (finalEvent.ContentVersion != "1.0.0") throw new Exception("ContentVersion mismatch");
        }

        public static void Test_Steps1To6EventOrdering()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            // Step 1: Detect Hazard
            bool s1 = workflow.ConfirmHazardDetected(bus, out var e1);
            if (!s1) throw new Exception("Step 1 detect hazard failed");

            // Step 2: Identify Hazard
            bool s2 = workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out var e2);
            if (!s2) throw new Exception("Step 2 identify hazard failed");

            // Step 3: Raise Alarm
            bool s3 = workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out var e3);
            if (!s3) throw new Exception("Step 3 raise alarm failed");

            // Step 4: Select Extinguisher
            bool s4 = workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out var e4);
            if (!s4) throw new Exception("Step 4 select extinguisher failed");

            // Step 5: Maintain Safe Distance
            bool s5 = workflow.SubmitDistanceDecision(2.5f, bus, out var e5);
            if (!s5) throw new Exception("Step 5 maintain safe distance failed");

            // Step 6: Use Extinguisher (PASS Procedure)
            bool s6a = workflow.SubmitPullPin(bus, out var e6a);
            if (!s6a) throw new Exception("Step 6a pull pin failed");

            bool s6b = workflow.SubmitAim(bus, out var e6b);
            if (!s6b) throw new Exception("Step 6b aim failed");

            bool s6c = workflow.SubmitSqueeze(bus, out var e6c);
            if (!s6c) throw new Exception("Step 6c squeeze failed");

            bool s6d = workflow.SubmitSweep(bus, out var e6d);
            if (!s6d) throw new Exception("Step 6d sweep failed");

            var events = bus.DispatchedEvents;
            if (events.Count != 9) throw new Exception($"Expected 9 dispatched events, got {events.Count}");

            // Verify sequential ordering of Step IDs across Steps 1 to 6:
            if (events[0].StepId != "step_detect_hazard") throw new Exception("Event 0 was not step_detect_hazard");
            if (events[1].StepId != "step_identify_hazard") throw new Exception("Event 1 was not step_identify_hazard");
            if (events[2].StepId != "step_raise_alarm") throw new Exception("Event 2 was not step_raise_alarm");
            if (events[3].StepId != "step_select_extinguisher") throw new Exception("Event 3 was not step_select_extinguisher");
            if (events[4].StepId != "step_maintain_distance") throw new Exception("Event 4 was not step_maintain_distance");
            if (events[5].StepId != "step_use_extinguisher") throw new Exception("Event 5 was not step_use_extinguisher");
            if (events[6].StepId != "step_use_extinguisher") throw new Exception("Event 6 was not step_use_extinguisher");
            if (events[7].StepId != "step_use_extinguisher") throw new Exception("Event 7 was not step_use_extinguisher");
            if (events[8].StepId != "step_use_extinguisher") throw new Exception("Event 8 was not step_use_extinguisher");

            // Verify final canonical Step 6 event:
            if (events[8].EventType != "procedure_completed") throw new Exception("Event 8 EventType mismatch");
            if (events[8].ActionId != "sweep") throw new Exception("Event 8 ActionId mismatch");
            if (events[8].TargetId != "extinguisher_procedure") throw new Exception("Event 8 TargetId mismatch");

            // Verify exact rubric Rule IDs across Steps 1 to 6:
            if (events[0].GetPayloadValue("rule_id") != "rule_detect_hazard") throw new Exception("Rule 1 mismatch");
            if (events[1].GetPayloadValue("rule_id") != "rule_identify_hazard") throw new Exception("Rule 2 mismatch");
            if (events[2].GetPayloadValue("rule_id") != "rule_raise_alarm") throw new Exception("Rule 3 mismatch");
            if (events[3].GetPayloadValue("rule_id") != "rule_select_extinguisher") throw new Exception("Rule 4 mismatch");
            if (events[4].GetPayloadValue("rule_id") != "rule_maintain_distance") throw new Exception("Rule 5 mismatch");
            if (events[8].GetPayloadValue("rule_id") != "rule_use_extinguisher") throw new Exception("Rule 6 mismatch");

            // Verify all events are success:
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Outcome != "success") throw new Exception($"Event {i} outcome was not success");
            }
        }

        public static void Test_Step7CannotBeginBeforeStep6()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.SafeDistanceMaintained);

            bool success = workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out var evt);

            if (success) throw new Exception("Step 7 identify exit should not succeed before Step 6 completes");
            if (evt != null) throw new Exception("Emitted event should be null for premature Step 7 action");
            if (bus.DispatchedEvents.Count != 0) throw new Exception("No events should be dispatched for premature action");
            if (workflow.CurrentStage != FireWorkflowStage.SafeDistanceMaintained)
                throw new Exception("Workflow stage should not change on premature Step 7 action");
        }

        public static void Test_SuccessfulEmergencyExitIdentification()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.ExtinguisherDischarged);

            bool success = workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out var evt);

            if (!success) throw new Exception("Emergency exit identification should succeed");
            if (workflow.CurrentStage != FireWorkflowStage.ExitIdentified)
                throw new Exception($"Expected ExitIdentified stage, got {workflow.CurrentStage}");
            if (workflow.CurrentStepId != "step_evacuate_route")
                throw new Exception($"Expected CurrentStepId step_evacuate_route, got {workflow.CurrentStepId}");

            if (evt == null) throw new Exception("Emitted event was null");
            if (evt.Outcome != "success") throw new Exception("Expected success outcome");
            if (bus.DispatchedEvents.Count != 1) throw new Exception($"Expected 1 dispatched event, got {bus.DispatchedEvents.Count}");

            // Duplicate call must be rejected and generate no duplicate completion event
            bool duplicate = workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out var dupEvt);
            if (duplicate) throw new Exception("Duplicate identification should be rejected");
            if (dupEvt != null) throw new Exception("Duplicate call should not emit an event");
            if (bus.DispatchedEvents.Count != 1) throw new Exception("Duplicate call should not add events to event bus");
        }

        public static void Test_IncorrectElevatorExitRejected()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AwaitingExitIdentification);

            string feedbackReceived = null;
            workflow.OnFeedbackChanged += (fb) => feedbackReceived = fb;

            bool success = workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitFreightElevator, bus, out var evt);

            if (success) throw new Exception("Freight elevator exit should be rejected");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingExitIdentification)
                throw new Exception($"Workflow should remain in AwaitingExitIdentification, got {workflow.CurrentStage}");
            if (workflow.CurrentStepId != "step_identify_exit")
                throw new Exception($"Expected CurrentStepId step_identify_exit, got {workflow.CurrentStepId}");

            if (evt == null) throw new Exception("Failure event should be emitted for incorrect choice");
            if (evt.Outcome != "failure") throw new Exception("Expected failure outcome");
            if (evt.TargetId != FireTrainingWorkflow.TargetExitFreightElevator)
                throw new Exception($"TargetId mismatch: expected {FireTrainingWorkflow.TargetExitFreightElevator}, got {evt.TargetId}");
            if (evt.StepId != "step_identify_exit") throw new Exception("StepId mismatch");
            if (evt.EventType != "exit_marked") throw new Exception("EventType mismatch");
            if (evt.GetPayloadValue("rule_id") != "rule_identify_exit") throw new Exception("rule_id mismatch");

            if (feedbackReceived == null || !feedbackReceived.Contains("CRITICAL HAZARD: Do NOT use elevators"))
                throw new Exception($"Expected elevator warning feedback, got: {feedbackReceived}");
        }

        public static void Test_IncorrectBlockedCorridorExitRejected()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AwaitingExitIdentification);

            string feedbackReceived = null;
            workflow.OnFeedbackChanged += (fb) => feedbackReceived = fb;

            bool success = workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitBlockedCorridor, bus, out var evt);

            if (success) throw new Exception("Blocked corridor exit should be rejected");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingExitIdentification)
                throw new Exception($"Workflow should remain in AwaitingExitIdentification, got {workflow.CurrentStage}");

            if (evt == null) throw new Exception("Failure event should be emitted for blocked corridor");
            if (evt.Outcome != "failure") throw new Exception("Expected failure outcome");
            if (evt.TargetId != FireTrainingWorkflow.TargetExitBlockedCorridor)
                throw new Exception($"TargetId mismatch: expected {FireTrainingWorkflow.TargetExitBlockedCorridor}, got {evt.TargetId}");

            if (feedbackReceived == null || !feedbackReceived.Contains("UNSAFE ROUTE"))
                throw new Exception($"Expected corridor warning feedback, got: {feedbackReceived}");
        }

        public static void Test_EmergencyExitEventMatchesRubricSchema()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.ExtinguisherDischarged);

            bool success = workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, "mark", bus, out var evt);

            if (!success) throw new Exception("SubmitIdentifyExit failed");
            if (evt.ModuleId != "fire-explosion-response") throw new Exception("ModuleId mismatch");
            if (evt.ContentVersion != "1.0.0") throw new Exception("ContentVersion mismatch");
            if (evt.StepId != "step_identify_exit") throw new Exception("StepId mismatch");
            if (evt.EventType != "exit_marked") throw new Exception("EventType mismatch");
            if (evt.ActionId != "mark") throw new Exception("ActionId mismatch");
            if (evt.TargetId != "exit_emergency_sector_b") throw new Exception("TargetId mismatch");
            if (evt.Outcome != "success") throw new Exception("Outcome mismatch");
            if (evt.GetPayloadValue("rule_id") != "rule_identify_exit") throw new Exception("rule_id mismatch");
            if (evt.GetPayloadValue("action_id") != "mark") throw new Exception("action_id mismatch");
            if (evt.GetPayloadValue("target_id") != "exit_emergency_sector_b") throw new Exception("target_id mismatch");
            if (evt.GetPayloadValue("location_type") != "emergency_exit") throw new Exception("location_type mismatch");
            if (evt.GetPayloadValue("outcome") != "success") throw new Exception("outcome mismatch");
        }

        public static void Test_Steps1To7EventOrdering()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            // Step 1: Detect Hazard
            bool s1 = workflow.ConfirmHazardDetected(bus, out _);
            if (!s1) throw new Exception("Step 1 detect hazard failed");

            // Step 2: Identify Hazard
            bool s2 = workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            if (!s2) throw new Exception("Step 2 identify hazard failed");

            // Step 3: Raise Alarm
            bool s3 = workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            if (!s3) throw new Exception("Step 3 raise alarm failed");

            // Step 4: Select Extinguisher
            bool s4 = workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            if (!s4) throw new Exception("Step 4 select extinguisher failed");

            // Step 5: Maintain Safe Distance
            bool s5 = workflow.SubmitDistanceDecision(2.5f, bus, out _);
            if (!s5) throw new Exception("Step 5 maintain safe distance failed");

            // Step 6: PASS Procedure
            bool s6a = workflow.SubmitPullPin(bus, out _);
            if (!s6a) throw new Exception("Step 6a pull pin failed");

            bool s6b = workflow.SubmitAim(bus, out _);
            if (!s6b) throw new Exception("Step 6b aim failed");

            bool s6c = workflow.SubmitSqueeze(bus, out _);
            if (!s6c) throw new Exception("Step 6c squeeze failed");

            bool s6d = workflow.SubmitSweep(bus, out _);
            if (!s6d) throw new Exception("Step 6d sweep failed");

            // Step 7: Identify Emergency Exit
            bool s7 = workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out var e7);
            if (!s7) throw new Exception("Step 7 identify emergency exit failed");

            var events = bus.DispatchedEvents;
            if (events.Count != 10) throw new Exception($"Expected 10 dispatched events, got {events.Count}");

            // Verify sequential ordering of Step IDs across Steps 1 to 7:
            if (events[0].StepId != "step_detect_hazard") throw new Exception("Event 0 was not step_detect_hazard");
            if (events[1].StepId != "step_identify_hazard") throw new Exception("Event 1 was not step_identify_hazard");
            if (events[2].StepId != "step_raise_alarm") throw new Exception("Event 2 was not step_raise_alarm");
            if (events[3].StepId != "step_select_extinguisher") throw new Exception("Event 3 was not step_select_extinguisher");
            if (events[4].StepId != "step_maintain_distance") throw new Exception("Event 4 was not step_maintain_distance");
            if (events[5].StepId != "step_use_extinguisher") throw new Exception("Event 5 was not step_use_extinguisher");
            if (events[6].StepId != "step_use_extinguisher") throw new Exception("Event 6 was not step_use_extinguisher");
            if (events[7].StepId != "step_use_extinguisher") throw new Exception("Event 7 was not step_use_extinguisher");
            if (events[8].StepId != "step_use_extinguisher") throw new Exception("Event 8 was not step_use_extinguisher");
            if (events[9].StepId != "step_identify_exit") throw new Exception("Event 9 was not step_identify_exit");

            // Verify final canonical Step 7 event:
            if (events[9].EventType != "exit_marked") throw new Exception("Event 9 EventType mismatch");
            if (events[9].ActionId != "mark") throw new Exception("Event 9 ActionId mismatch");
            if (events[9].TargetId != "exit_emergency_sector_b") throw new Exception("Event 9 TargetId mismatch");
            if (events[9].GetPayloadValue("rule_id") != "rule_identify_exit") throw new Exception("Event 9 rule_id mismatch");

            // Verify all events are success:
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Outcome != "success") throw new Exception($"Event {i} outcome was not success");
            }

            // Verify workflow state is ready for Step 8:
            if (workflow.CurrentStage != FireWorkflowStage.ExitIdentified)
                throw new Exception($"Expected ExitIdentified stage, got {workflow.CurrentStage}");
            if (workflow.CurrentStepId != "step_evacuate_route")
                throw new Exception($"Expected CurrentStepId step_evacuate_route, got {workflow.CurrentStepId}");
        }

        public static void Test_Step8CannotBeginBeforeStep7()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.ExtinguisherDischarged);

            bool success = workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out var evt);

            if (success) throw new Exception("Step 8 evacuation should not succeed before Step 7 completes");
            if (evt != null) throw new Exception("Emitted event should be null for premature Step 8 action");
            if (bus.DispatchedEvents.Count != 0) throw new Exception("No events should be dispatched for premature action");
            if (workflow.CurrentStage != FireWorkflowStage.ExtinguisherDischarged)
                throw new Exception("Workflow stage should not change on premature Step 8 action");
        }

        public static void Test_SuccessfulEvacuationSequence()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.ExitIdentified);

            // Waypoint 1: Main Corridor
            bool s1 = workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out var e1);
            if (!s1) throw new Exception("Waypoint 1 should succeed");
            if (workflow.CurrentStage != FireWorkflowStage.WaypointMainCorridorReached)
                throw new Exception($"Expected WaypointMainCorridorReached stage, got {workflow.CurrentStage}");

            // Waypoint 2: Bypass Crosscut
            bool s2 = workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out var e2);
            if (!s2) throw new Exception("Waypoint 2 should succeed");
            if (workflow.CurrentStage != FireWorkflowStage.WaypointBypassCrosscutReached)
                throw new Exception($"Expected WaypointBypassCrosscutReached stage, got {workflow.CurrentStage}");

            // Waypoint 3: Fire Door Exit
            bool s3 = workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out var e3);
            if (!s3) throw new Exception("Waypoint 3 should succeed");
            if (workflow.CurrentStage != FireWorkflowStage.RouteEvacuated)
                throw new Exception($"Expected RouteEvacuated stage, got {workflow.CurrentStage}");
            if (workflow.CurrentStepId != "step_reach_assembly")
                throw new Exception($"Expected CurrentStepId step_reach_assembly, got {workflow.CurrentStepId}");

            if (e3 == null) throw new Exception("Final completion event was null");
            if (e3.Outcome != "success") throw new Exception("Expected success outcome");
            if (e3.EventType != "evacuation_sequence_submitted")
                throw new Exception($"EventType mismatch: expected evacuation_sequence_submitted, got {e3.EventType}");

            // Verify skipping to fire door exit is rejected without bypass crosscut
            var workflow2 = new FireTrainingWorkflow();
            workflow2.SetStage(FireWorkflowStage.ExitIdentified);
            bool c1 = workflow2.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            if (!c1) throw new Exception("Canonical waypoint 1 should succeed");
            bool cSkip = workflow2.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out var eSkip);
            if (cSkip) throw new Exception("Skipping directly to fire door exit before bypass crosscut must fail");
            if (workflow2.CurrentStage != FireWorkflowStage.WaypointMainCorridorReached)
                throw new Exception($"Expected stage to remain WaypointMainCorridorReached, got {workflow2.CurrentStage}");
        }

        public static void Test_OutOfOrderWaypointRejected()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AwaitingEvacuationRoute);

            string feedbackReceived = null;
            workflow.OnFeedbackChanged += (fb) => feedbackReceived = fb;

            // Attempting waypoint 3 directly before waypoint 1
            bool success = workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out var evt);

            if (success) throw new Exception("Out-of-order waypoint must be rejected");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingEvacuationRoute)
                throw new Exception($"Workflow should remain in AwaitingEvacuationRoute, got {workflow.CurrentStage}");

            if (evt == null) throw new Exception("Failure event should be emitted for out-of-order action");
            if (evt.Outcome != "failure") throw new Exception("Expected failure outcome");
            if (evt.EventType != "evacuation_sequence_submitted") throw new Exception("EventType mismatch");
            if (evt.GetPayloadValue("rule_id") != "rule_evacuate_route") throw new Exception("rule_id mismatch");

            if (feedbackReceived == null || !feedbackReceived.Contains("SEQUENCE ERROR"))
                throw new Exception($"Expected sequence error feedback, got: {feedbackReceived}");
        }

        public static void Test_SmokeCorridorSelectionEmitsFailure()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AwaitingEvacuationRoute);

            string feedbackReceived = null;
            workflow.OnFeedbackChanged += (fb) => feedbackReceived = fb;

            bool success = workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.HazardSmokeCorridor, bus, out var evt);

            if (success) throw new Exception("Smoke corridor must be rejected");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingEvacuationRoute)
                throw new Exception($"Workflow should remain in AwaitingEvacuationRoute, got {workflow.CurrentStage}");

            if (evt == null) throw new Exception("Failure event should be emitted for smoke corridor hazard");
            if (evt.Outcome != "failure") throw new Exception("Expected failure outcome");
            if (evt.TargetId != FireTrainingWorkflow.HazardSmokeCorridor)
                throw new Exception($"TargetId mismatch: expected {FireTrainingWorkflow.HazardSmokeCorridor}, got {evt.TargetId}");
            if (evt.GetPayloadValue("hazard_warning") != FireTrainingWorkflow.DecisionAvoidSmoke)
                throw new Exception("hazard_warning payload mismatch");

            if (feedbackReceived == null || !feedbackReceived.Contains("CRITICAL HAZARD: Dense toxic smoke"))
                throw new Exception($"Expected smoke hazard feedback, got: {feedbackReceived}");
        }

        public static void Test_EvacuationRouteEventMatchesRubricSchema()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.WaypointBypassCrosscutReached);

            bool success = workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out var evt);

            if (!success) throw new Exception("SubmitEvacuationWaypoint failed");
            if (evt.ModuleId != "fire-explosion-response") throw new Exception("ModuleId mismatch");
            if (evt.ContentVersion != "1.0.0") throw new Exception("ContentVersion mismatch");
            if (evt.StepId != "step_evacuate_route") throw new Exception("StepId mismatch");
            if (evt.EventType != "evacuation_sequence_submitted") throw new Exception("EventType mismatch");
            if (evt.ActionId != "submit_sequence") throw new Exception("ActionId mismatch");
            if (evt.TargetId != "waypoint_fire_door_exit") throw new Exception("TargetId mismatch");
            if (evt.Outcome != "success") throw new Exception("Outcome mismatch");
            if (evt.GetPayloadValue("rule_id") != "rule_evacuate_route") throw new Exception("rule_id mismatch");
            if (evt.GetPayloadValue("action_id") != "submit_sequence") throw new Exception("action_id mismatch");
            if (evt.GetPayloadValue("target_id") != "waypoint_fire_door_exit") throw new Exception("target_id mismatch");
            if (evt.GetPayloadValue("ordered_ids") != "waypoint_main_corridor,waypoint_bypass_crosscut,waypoint_fire_door_exit")
                throw new Exception("ordered_ids payload mismatch");
            if (evt.GetPayloadValue("outcome") != "success") throw new Exception("outcome mismatch");
        }

        public static void Test_Steps1To8EventOrdering()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            // Step 1: Detect Hazard
            bool s1 = workflow.ConfirmHazardDetected(bus, out _);
            if (!s1) throw new Exception("Step 1 failed");

            // Step 2: Identify Hazard
            bool s2 = workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            if (!s2) throw new Exception("Step 2 failed");

            // Step 3: Raise Alarm
            bool s3 = workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            if (!s3) throw new Exception("Step 3 failed");

            // Step 4: Select Extinguisher
            bool s4 = workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            if (!s4) throw new Exception("Step 4 failed");

            // Step 5: Maintain Safe Distance
            bool s5 = workflow.SubmitDistanceDecision(2.5f, bus, out _);
            if (!s5) throw new Exception("Step 5 failed");

            // Step 6: PASS Procedure
            bool s6a = workflow.SubmitPullPin(bus, out _);
            if (!s6a) throw new Exception("Step 6a failed");

            bool s6b = workflow.SubmitAim(bus, out _);
            if (!s6b) throw new Exception("Step 6b failed");

            bool s6c = workflow.SubmitSqueeze(bus, out _);
            if (!s6c) throw new Exception("Step 6c failed");

            bool s6d = workflow.SubmitSweep(bus, out _);
            if (!s6d) throw new Exception("Step 6d failed");

            // Step 7: Identify Emergency Exit
            bool s7 = workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            if (!s7) throw new Exception("Step 7 failed");

            // Step 8: Evacuation Route (Sequential Waypoints)
            bool s8a = workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            if (!s8a) throw new Exception("Step 8a failed");

            bool s8b = workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            if (!s8b) throw new Exception("Step 8b failed");

            bool s8c = workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            if (!s8c) throw new Exception("Step 8c failed");

            var events = bus.DispatchedEvents;
            if (events.Count != 13) throw new Exception($"Expected 13 dispatched events, got {events.Count}");

            // Verify sequential ordering of Step IDs across Steps 1 to 8:
            if (events[0].StepId != "step_detect_hazard") throw new Exception("Event 0 was not step_detect_hazard");
            if (events[1].StepId != "step_identify_hazard") throw new Exception("Event 1 was not step_identify_hazard");
            if (events[2].StepId != "step_raise_alarm") throw new Exception("Event 2 was not step_raise_alarm");
            if (events[3].StepId != "step_select_extinguisher") throw new Exception("Event 3 was not step_select_extinguisher");
            if (events[4].StepId != "step_maintain_distance") throw new Exception("Event 4 was not step_maintain_distance");
            if (events[5].StepId != "step_use_extinguisher") throw new Exception("Event 5 was not step_use_extinguisher");
            if (events[6].StepId != "step_use_extinguisher") throw new Exception("Event 6 was not step_use_extinguisher");
            if (events[7].StepId != "step_use_extinguisher") throw new Exception("Event 7 was not step_use_extinguisher");
            if (events[8].StepId != "step_use_extinguisher") throw new Exception("Event 8 was not step_use_extinguisher");
            if (events[9].StepId != "step_identify_exit") throw new Exception("Event 9 was not step_identify_exit");
            if (events[10].StepId != "step_evacuate_route") throw new Exception("Event 10 was not step_evacuate_route");
            if (events[11].StepId != "step_evacuate_route") throw new Exception("Event 11 was not step_evacuate_route");
            if (events[12].StepId != "step_evacuate_route") throw new Exception("Event 12 was not step_evacuate_route");

            // Verify final Step 8 completion event:
            if (events[12].EventType != "evacuation_sequence_submitted") throw new Exception("Event 12 EventType mismatch");
            if (events[12].ActionId != "submit_sequence") throw new Exception("Event 12 ActionId mismatch");
            if (events[12].TargetId != "waypoint_fire_door_exit") throw new Exception("Event 12 TargetId mismatch");
            if (events[12].GetPayloadValue("rule_id") != "rule_evacuate_route") throw new Exception("Event 12 rule_id mismatch");

            // Verify all events are success:
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Outcome != "success") throw new Exception($"Event {i} outcome was not success");
            }

            // Verify workflow state is ready for Step 10:
            if (workflow.CurrentStage != FireWorkflowStage.RouteEvacuated)
                throw new Exception($"Expected RouteEvacuated stage, got {workflow.CurrentStage}");
            if (workflow.CurrentStepId != "step_reach_assembly")
                throw new Exception($"Expected CurrentStepId step_reach_assembly, got {workflow.CurrentStepId}");
        }

        public static void Test_DuplicateEvacuationCompletionPrevented()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.RouteEvacuated);

            bool success = workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out var evt);

            if (success) throw new Exception("Duplicate evacuation submission should be rejected");
            if (evt != null) throw new Exception("Duplicate call should not emit an event");
            if (bus.DispatchedEvents.Count != 0) throw new Exception("Duplicate call should not add events to bus");
        }

        public static void Test_Step9CannotBeginBeforeStep8()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AwaitingEvacuationRoute);

            bool success = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out var evt);

            if (success) throw new Exception("Assembly point identification should fail before Step 8 is completed");
            if (evt != null) throw new Exception("Emitted event should be null when premature");
            if (bus.DispatchedEvents.Count != 0) throw new Exception("No events should be dispatched to bus");
            if (workflow.CurrentStage != FireWorkflowStage.AwaitingEvacuationRoute)
                throw new Exception("Workflow stage should not change on rejected action");
        }

        public static void Test_SuccessfulAssemblyPointIdentification()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.RouteEvacuated);

            bool success = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out var evt);

            if (!success) throw new Exception("Expected SubmitReachAssemblyPoint to succeed");
            if (evt == null) throw new Exception("Emitted event should not be null");
            if (evt.StepId != FireTrainingWorkflow.StepReachAssembly) throw new Exception($"StepId mismatch: {evt.StepId}");
            if (evt.EventType != FireTrainingWorkflow.EventTypeAssemblyReached) throw new Exception($"EventType mismatch: {evt.EventType}");
            if (evt.ActionId != FireTrainingWorkflow.ActionCompleteStep) throw new Exception($"ActionId mismatch: {evt.ActionId}");
            if (evt.TargetId != FireTrainingWorkflow.TargetAssemblyMusterPoint) throw new Exception($"TargetId mismatch: {evt.TargetId}");
            if (evt.Outcome != "success") throw new Exception($"Outcome mismatch: {evt.Outcome}");
            if (workflow.CurrentStage != FireWorkflowStage.AssemblyPointReached)
                throw new Exception($"Expected stage AssemblyPointReached, got {workflow.CurrentStage}");
            if (bus.DispatchedEvents.Count != 1) throw new Exception("Bus should contain 1 dispatched event");
        }

        public static void Test_WrongAssemblyPointRejected()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.RouteEvacuated);

            bool success = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyPointBeta, bus, out var evt);

            if (success) throw new Exception("Selecting non-designated assembly point should return false");
            if (evt == null) throw new Exception("Failure event should be emitted for rubric grading");
            if (evt.Outcome != "failure") throw new Exception($"Expected outcome failure, got {evt.Outcome}");
            if (evt.TargetId != FireTrainingWorkflow.TargetAssemblyPointBeta) throw new Exception($"TargetId mismatch: {evt.TargetId}");
            if (evt.GetPayloadValue("error") != "wrong_assembly_point") throw new Exception("Payload error code mismatch");
            if (workflow.CurrentStage != FireWorkflowStage.RouteEvacuated)
                throw new Exception($"Stage should remain RouteEvacuated, got {workflow.CurrentStage}");
            if (bus.DispatchedEvents.Count != 1) throw new Exception("Failure event should be dispatched to bus");
        }

        public static void Test_AssemblyPointEventMatchesRubricSchema()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.RouteEvacuated);

            bool success = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out var evt);

            if (!success) throw new Exception("SubmitReachAssemblyPoint failed");
            if (evt.ModuleId != "fire-explosion-response") throw new Exception("ModuleId mismatch");
            if (evt.ContentVersion != "1.0.0") throw new Exception("ContentVersion mismatch");
            if (evt.StepId != "step_reach_assembly") throw new Exception("StepId mismatch");
            if (evt.EventType != "assembly_reached") throw new Exception("EventType mismatch");
            if (evt.ActionId != "complete_step") throw new Exception("ActionId mismatch");
            if (evt.TargetId != "assembly_muster_point_alpha") throw new Exception("TargetId mismatch");
            if (evt.Outcome != "success") throw new Exception("Outcome mismatch");
            if (evt.GetPayloadValue("rule_id") != "rule_reach_assembly") throw new Exception("rule_id mismatch");
            if (evt.GetPayloadValue("action_id") != "complete_step") throw new Exception("action_id mismatch");
            if (evt.GetPayloadValue("target_id") != "assembly_muster_point_alpha") throw new Exception("target_id mismatch");
            if (evt.GetPayloadValue("zone_type") != "emergency_assembly_area") throw new Exception("zone_type mismatch");
            if (evt.GetPayloadValue("outcome") != "success") throw new Exception("outcome mismatch");
        }

        public static void Test_Step8ToStep9EventOrdering()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            // Steps 1 to 8
            if (!workflow.ConfirmHazardDetected(bus, out _)) throw new Exception("Step 1 failed");
            if (!workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _)) throw new Exception("Step 2 failed");
            if (!workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _)) throw new Exception("Step 3 failed");
            if (!workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _)) throw new Exception("Step 4 failed");
            if (!workflow.SubmitDistanceDecision(2.5f, bus, out _)) throw new Exception("Step 5 failed");
            if (!workflow.SubmitPullPin(bus, out _)) throw new Exception("Step 6a failed");
            if (!workflow.SubmitAim(bus, out _)) throw new Exception("Step 6b failed");
            if (!workflow.SubmitSqueeze(bus, out _)) throw new Exception("Step 6c failed");
            if (!workflow.SubmitSweep(bus, out _)) throw new Exception("Step 6d failed");
            if (!workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _)) throw new Exception("Step 7 failed");
            if (!workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _)) throw new Exception("Step 8a failed");
            if (!workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _)) throw new Exception("Step 8b failed");
            if (!workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _)) throw new Exception("Step 8c failed");

            // Step 9: Reach Assembly Point
            bool s9 = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);
            if (!s9) throw new Exception("Step 9 failed");

            var events = bus.DispatchedEvents;
            if (events.Count != 14) throw new Exception($"Expected 14 dispatched events, got {events.Count}");

            // Verify sequential ordering of Step IDs across Steps 1 to 9:
            if (events[0].StepId != "step_detect_hazard") throw new Exception("Event 0 was not step_detect_hazard");
            if (events[1].StepId != "step_identify_hazard") throw new Exception("Event 1 was not step_identify_hazard");
            if (events[2].StepId != "step_raise_alarm") throw new Exception("Event 2 was not step_raise_alarm");
            if (events[3].StepId != "step_select_extinguisher") throw new Exception("Event 3 was not step_select_extinguisher");
            if (events[4].StepId != "step_maintain_distance") throw new Exception("Event 4 was not step_maintain_distance");
            if (events[5].StepId != "step_use_extinguisher") throw new Exception("Event 5 was not step_use_extinguisher");
            if (events[6].StepId != "step_use_extinguisher") throw new Exception("Event 6 was not step_use_extinguisher");
            if (events[7].StepId != "step_use_extinguisher") throw new Exception("Event 7 was not step_use_extinguisher");
            if (events[8].StepId != "step_use_extinguisher") throw new Exception("Event 8 was not step_use_extinguisher");
            if (events[9].StepId != "step_identify_exit") throw new Exception("Event 9 was not step_identify_exit");
            if (events[10].StepId != "step_evacuate_route") throw new Exception("Event 10 was not step_evacuate_route");
            if (events[11].StepId != "step_evacuate_route") throw new Exception("Event 11 was not step_evacuate_route");
            if (events[12].StepId != "step_evacuate_route") throw new Exception("Event 12 was not step_evacuate_route");
            if (events[13].StepId != "step_reach_assembly") throw new Exception("Event 13 was not step_reach_assembly");

            // Verify Step 9 event specifics:
            if (events[13].EventType != "assembly_reached") throw new Exception("Event 13 EventType mismatch");
            if (events[13].ActionId != "complete_step") throw new Exception("Event 13 ActionId mismatch");
            if (events[13].TargetId != "assembly_muster_point_alpha") throw new Exception("Event 13 TargetId mismatch");
            if (events[13].GetPayloadValue("rule_id") != "rule_reach_assembly") throw new Exception("Event 13 rule_id mismatch");

            // Verify all events have outcome == success
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Outcome != "success") throw new Exception($"Event {i} outcome was not success");
            }

            // Verify final terminal stage
            if (workflow.CurrentStage != FireWorkflowStage.AssemblyPointReached)
                throw new Exception($"Expected AssemblyPointReached stage, got {workflow.CurrentStage}");
        }

        public static void Test_DuplicateAssemblyCompletionPrevented()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.AssemblyPointReached);

            bool success = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out var evt);

            if (success) throw new Exception("Duplicate assembly point completion should be rejected");
            if (evt != null) throw new Exception("Duplicate call should not emit an event");
            if (bus.DispatchedEvents.Count != 0) throw new Exception("Duplicate call should not add events to bus");
        }

        public static void Test_PrematureAssemblyPointActionRejected()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.WaypointMainCorridorReached);

            bool success = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out var evt);

            if (success) throw new Exception("Premature assembly submission during mid-route should be rejected");
            if (evt != null) throw new Exception("Emitted event should be null on premature submission");
            if (bus.DispatchedEvents.Count != 0) throw new Exception("Bus should have 0 events");
            if (workflow.CurrentStage != FireWorkflowStage.WaypointMainCorridorReached)
                throw new Exception("Workflow stage should remain unchanged");
        }

        private static List<TrainingEvent> GenerateFlawlessStep1To9Events()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            return new List<TrainingEvent>(bus.DispatchedEvents);
        }

        public static void Test_Assessment_PerfectStep1To9Sequence_Scores100AndPasses()
        {
            var events = GenerateFlawlessStep1To9Events();
            var rubric = RubricDefinition.CreateFireExplosionRubric();

            var result = LocalAssessmentEngine.Evaluate(events, rubric);

            if (result.TotalAwarded != 100.00f)
                throw new Exception($"Expected TotalAwarded 100.00, got {result.TotalAwarded}");
            if (result.TotalPenalties != 0.00f)
                throw new Exception($"Expected TotalPenalties 0.00, got {result.TotalPenalties}");
            if (result.ClientScore != 100.00f)
                throw new Exception($"Expected ClientScore 100.00, got {result.ClientScore}");
            if (!result.Passed)
                throw new Exception("Expected Passed to be true for flawless run");
            if (result.RuleResults.Count != 9)
                throw new Exception($"Expected 9 rule results, got {result.RuleResults.Count}");

            foreach (var r in result.RuleResults)
            {
                if (!r.IsSatisfied)
                    throw new Exception($"Rule {r.RuleId} was not satisfied");
                if (r.TimesAwarded != 1)
                    throw new Exception($"Rule {r.RuleId} TimesAwarded was {r.TimesAwarded}, expected 1");
                if (r.PenaltyDeducted != 0f)
                    throw new Exception($"Rule {r.RuleId} had unexpected penalty: {r.PenaltyDeducted}");
            }

            var attempt = LocalAssessmentEngine.EvaluateAttempt(events, rubric, workerId: "test-worker-01");
            if (attempt.Status != TrainingAttempt.StatusCompleted)
                throw new Exception($"Expected status completed, got {attempt.Status}");
            if (attempt.ClientScore != 100.00f)
                throw new Exception($"Attempt ClientScore mismatch: {attempt.ClientScore}");
            if (!attempt.Passed)
                throw new Exception("Attempt Passed was false");
            if (attempt.Events.Count != 14)
                throw new Exception($"Expected 14 events in attempt, got {attempt.Events.Count}");
            if (string.IsNullOrEmpty(attempt.ClientAttemptId))
                throw new Exception("ClientAttemptId was null/empty");
        }

        public static void Test_Assessment_WrongHazardIdentification_Deducts5Penalty()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            workflow.ConfirmHazardDetected(bus, out _);
            // Incorrect identification => emits failure penalty event
            workflow.SubmitHazardIdentification("hazard_chemical_spill", bus, out _);
            // Correct identification => emits success award event
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            var rubric = RubricDefinition.CreateFireExplosionRubric();
            var result = LocalAssessmentEngine.Evaluate(bus.DispatchedEvents, rubric);

            var idRule = result.GetRuleResult("rule_identify_hazard");
            if (idRule == null) throw new Exception("rule_identify_hazard result not found");
            if (idRule.PointsAwarded != 15.00f) throw new Exception($"PointsAwarded was {idRule.PointsAwarded}, expected 15.00");
            if (idRule.PenaltyDeducted != 5.00f) throw new Exception($"PenaltyDeducted was {idRule.PenaltyDeducted}, expected 5.00");
            if (idRule.NetScore != 10.00f) throw new Exception($"NetScore was {idRule.NetScore}, expected 10.00");

            if (result.TotalPenalties != 5.00f) throw new Exception($"TotalPenalties was {result.TotalPenalties}, expected 5.00");
            if (result.ClientScore != 95.00f) throw new Exception($"ClientScore was {result.ClientScore}, expected 95.00");
            if (!result.Passed) throw new Exception("Expected Passed to be true (95 >= 70)");
        }

        public static void Test_Assessment_WrongExtinguisher_Deducts5Penalty()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            // Incorrect extinguisher => emits failure penalty event
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherWater, bus, out _);
            // Correct extinguisher => emits success award event
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            var rubric = RubricDefinition.CreateFireExplosionRubric();
            var result = LocalAssessmentEngine.Evaluate(bus.DispatchedEvents, rubric);

            var extRule = result.GetRuleResult("rule_select_extinguisher");
            if (extRule == null) throw new Exception("rule_select_extinguisher result not found");
            if (extRule.PointsAwarded != 15.00f) throw new Exception($"PointsAwarded was {extRule.PointsAwarded}, expected 15.00");
            if (extRule.PenaltyDeducted != 5.00f) throw new Exception($"PenaltyDeducted was {extRule.PenaltyDeducted}, expected 5.00");

            if (result.TotalPenalties != 5.00f) throw new Exception($"TotalPenalties was {result.TotalPenalties}, expected 5.00");
            if (result.ClientScore != 95.00f) throw new Exception($"ClientScore was {result.ClientScore}, expected 95.00");
            if (!result.Passed) throw new Exception("Expected Passed to be true");
        }

        public static void Test_Assessment_UnsafeSmokeCorridor_Deducts5Penalty()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            // Unsafe smoke corridor attempted => emits failure penalty event
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.HazardSmokeCorridor, bus, out _);
            // Valid route sequence
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            var rubric = RubricDefinition.CreateFireExplosionRubric();
            var result = LocalAssessmentEngine.Evaluate(bus.DispatchedEvents, rubric);

            var evacRule = result.GetRuleResult("rule_evacuate_route");
            if (evacRule == null) throw new Exception("rule_evacuate_route result not found");
            if (evacRule.PointsAwarded != 10.00f) throw new Exception($"PointsAwarded was {evacRule.PointsAwarded}, expected 10.00");
            if (evacRule.PenaltyDeducted != 5.00f) throw new Exception($"PenaltyDeducted was {evacRule.PenaltyDeducted}, expected 5.00");

            if (result.TotalPenalties != 5.00f) throw new Exception($"TotalPenalties was {result.TotalPenalties}, expected 5.00");
            if (result.ClientScore != 95.00f) throw new Exception($"ClientScore was {result.ClientScore}, expected 95.00");
            if (!result.Passed) throw new Exception("Expected Passed to be true");
        }

        public static void Test_Assessment_RepeatedAward_RespectsAwardLimit()
        {
            var events = new List<TrainingEvent>();
            for (int i = 0; i < 5; i++)
            {
                events.Add(new TrainingEvent
                {
                    StepId = "step_detect_hazard",
                    EventType = "step_completed",
                    ActionId = "detect_hazard_acknowledged",
                    Outcome = "success"
                });
            }

            var rubric = RubricDefinition.CreateFireExplosionRubric();
            var result = LocalAssessmentEngine.Evaluate(events, rubric);

            var detectRule = result.GetRuleResult("rule_detect_hazard");
            if (detectRule == null) throw new Exception("rule_detect_hazard not found");
            if (detectRule.TimesAwarded != 1)
                throw new Exception($"TimesAwarded was {detectRule.TimesAwarded}, expected 1");
            if (detectRule.PointsAwarded != 5.00f)
                throw new Exception($"PointsAwarded was {detectRule.PointsAwarded}, expected 5.00 (not 25.00)");
        }

        public static void Test_Assessment_ScoreBelow70_Fails()
        {
            // Worker only completes Step 1 (+5) and Step 2 (+15), missing all remaining steps
            var events = new List<TrainingEvent>
            {
                new TrainingEvent
                {
                    StepId = "step_detect_hazard",
                    EventType = "step_completed",
                    ActionId = "detect_hazard_acknowledged",
                    Outcome = "success"
                },
                new TrainingEvent
                {
                    StepId = "step_identify_hazard",
                    EventType = "hazard_identified",
                    TargetId = "hazard_electrical_conveyor_fire",
                    Outcome = "success"
                }
            };

            var rubric = RubricDefinition.CreateFireExplosionRubric();
            var result = LocalAssessmentEngine.Evaluate(events, rubric);

            if (result.ClientScore != 20.00f)
                throw new Exception($"Expected ClientScore 20.00, got {result.ClientScore}");
            if (result.Passed)
                throw new Exception("Score of 20% must FAIL (threshold is 70%)");
        }

        public static void Test_Assessment_PenaltiesCannotDriveScoreBelowZero()
        {
            // Events consist purely of penalty failures
            var events = new List<TrainingEvent>
            {
                new TrainingEvent
                {
                    StepId = "step_identify_hazard",
                    EventType = "hazard_identified",
                    Outcome = "failure"
                },
                new TrainingEvent
                {
                    StepId = "step_select_extinguisher",
                    EventType = "extinguisher_selected",
                    Outcome = "failure"
                },
                new TrainingEvent
                {
                    StepId = "step_evacuate_route",
                    EventType = "evacuation_sequence_submitted",
                    Outcome = "failure"
                }
            };

            var rubric = RubricDefinition.CreateFireExplosionRubric();
            var result = LocalAssessmentEngine.Evaluate(events, rubric);

            if (result.TotalPenalties != 15.00f)
                throw new Exception($"Expected TotalPenalties 15.00, got {result.TotalPenalties}");
            if (result.ClientScore != 0.00f)
                throw new Exception($"ClientScore must be clamped at 0.00, got {result.ClientScore}");
            if (result.Passed)
                throw new Exception("Expected Passed to be false");
        }

        public static void Test_Assessment_DeterministicRepeatedEvaluation_GivesIdenticalResult()
        {
            var events = GenerateFlawlessStep1To9Events();
            var rubric = RubricDefinition.CreateFireExplosionRubric();

            var firstResult = LocalAssessmentEngine.Evaluate(events, rubric);

            for (int i = 0; i < 10; i++)
            {
                var iterationResult = LocalAssessmentEngine.Evaluate(events, rubric);
                if (iterationResult.ClientScore != firstResult.ClientScore)
                    throw new Exception($"Drift detected in ClientScore on iteration {i}");
                if (iterationResult.TotalAwarded != firstResult.TotalAwarded)
                    throw new Exception($"Drift detected in TotalAwarded on iteration {i}");
                if (iterationResult.TotalPenalties != firstResult.TotalPenalties)
                    throw new Exception($"Drift detected in TotalPenalties on iteration {i}");
                if (iterationResult.Passed != firstResult.Passed)
                    throw new Exception($"Drift detected in Passed status on iteration {i}");
            }
        }

        public static void Test_Workflow_AssessmentStartsOnlyAfterFinalFireCompletion()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);

            if (workflow.LatestAttempt != null)
                throw new Exception("LatestAttempt should be null mid-training");
            if (workflow.IsAssessmentCompleted)
                throw new Exception("IsAssessmentCompleted should be false mid-training");

            var earlyEval = workflow.EvaluateAssessment(bus);
            if (earlyEval != null)
                throw new Exception("EvaluateAssessment should return null before terminal stage");
            if (workflow.LatestAttempt != null)
                throw new Exception("LatestAttempt should remain null after premature evaluation attempt");

            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);

            // Still at RouteEvacuated before Step 9 completion
            if (workflow.IsAssessmentCompleted)
                throw new Exception("Assessment should not be completed before reaching assembly point");

            // Complete Step 9
            bool s9 = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);
            if (!s9) throw new Exception("Step 9 failed");

            if (workflow.CurrentStage != FireWorkflowStage.AssemblyPointReached)
                throw new Exception($"Expected stage AssemblyPointReached, got {workflow.CurrentStage}");
            if (!workflow.IsAssessmentCompleted)
                throw new Exception("Assessment must be completed after reaching assembly point");
            if (workflow.LatestAttempt == null)
                throw new Exception("LatestAttempt must not be null after reaching assembly point");
            if (workflow.LatestAssessment == null)
                throw new Exception("LatestAssessment must not be null after reaching assembly point");
        }

        public static void Test_Workflow_FinalCompletionEvaluatesSteps1To9Events()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            workflow.WorkerId = "worker-fire-specialist-01";
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            var attempt = workflow.LatestAttempt;
            var assessment = workflow.LatestAssessment;

            if (attempt == null || assessment == null)
                throw new Exception("Attempt or Assessment was null");

            if (attempt.Events.Count != 14)
                throw new Exception($"Expected 14 events in attempt, got {attempt.Events.Count}");
            if (assessment.RuleResults.Count != 9)
                throw new Exception($"Expected 9 rule results, got {assessment.RuleResults.Count}");
            if (attempt.ClientScore != 100.00f)
                throw new Exception($"Expected 100.00 score, got {attempt.ClientScore}");
            if (!attempt.Passed)
                throw new Exception("Expected Passed to be true");
            if (attempt.Status != TrainingAttempt.StatusCompleted)
                throw new Exception($"Expected status completed, got {attempt.Status}");
            if (attempt.WorkerId != "worker-fire-specialist-01")
                throw new Exception($"WorkerId mismatch: {attempt.WorkerId}");
            if (attempt.ModuleId != "fire-explosion-response")
                throw new Exception($"ModuleId mismatch: {attempt.ModuleId}");
            if (string.IsNullOrEmpty(attempt.CompletedAt))
                throw new Exception("CompletedAt must be populated");
        }

        public static void Test_Workflow_ClientScoreAndPassedStatusCopiedFromEngine()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            workflow.ConfirmHazardDetected(bus, out _);
            // Incur identification penalty (-5)
            workflow.SubmitHazardIdentification("hazard_chemical_spill", bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            if (workflow.LatestAssessment.ClientScore != 95.00f)
                throw new Exception($"Assessment ClientScore was {workflow.LatestAssessment.ClientScore}, expected 95.00");
            if (workflow.LatestAttempt.ClientScore != workflow.LatestAssessment.ClientScore)
                throw new Exception("Attempt ClientScore must match Assessment ClientScore exactly");
            if (workflow.LatestAttempt.Passed != workflow.LatestAssessment.Passed)
                throw new Exception("Attempt Passed status must match Assessment Passed status exactly");
            if (!workflow.LatestAttempt.Passed)
                throw new Exception("Attempt with 95% must pass");
        }

        public static void Test_Workflow_DuplicateCompletionDoesNotCreateSecondAttempt()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            string initialAttemptId = workflow.LatestAttempt.ClientAttemptId;
            var initialAssessment = workflow.LatestAssessment;

            // Attempt duplicate completion call
            bool secondCall = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out var dupEvt);
            if (secondCall)
                throw new Exception("Duplicate SubmitReachAssemblyPoint must return false");
            if (dupEvt != null)
                throw new Exception("Duplicate call should not emit an event");

            // Evaluate assessment again
            var secondEval = workflow.EvaluateAssessment(bus);

            if (workflow.LatestAttempt.ClientAttemptId != initialAttemptId)
                throw new Exception("ClientAttemptId changed on duplicate evaluation!");
            if (!ReferenceEquals(workflow.LatestAssessment, initialAssessment))
                throw new Exception("LatestAssessment reference changed on duplicate evaluation!");
            if (!ReferenceEquals(secondEval, initialAssessment))
                throw new Exception("Returned assessment should be cached instance");
        }

        public static void Test_Workflow_PrematureCompletionCannotFinalizeAttempt()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            // Attempt Step 9 from HazardPlaced
            bool premature1 = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out var evt1);
            if (premature1) throw new Exception("Premature Step 9 from HazardPlaced must return false");
            if (evt1 != null) throw new Exception("Premature call must not emit event");
            if (workflow.LatestAttempt != null) throw new Exception("LatestAttempt must be null");
            if (workflow.IsAssessmentCompleted) throw new Exception("IsAssessmentCompleted must be false");

            // Advance to mid-transit
            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);

            // Attempt Step 9 during evacuation route
            bool premature2 = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out var evt2);
            if (premature2) throw new Exception("Premature Step 9 during evacuation route must return false");
            if (evt2 != null) throw new Exception("Premature call must not emit event");
            if (workflow.LatestAttempt != null) throw new Exception("LatestAttempt must be null");
            if (workflow.IsAssessmentCompleted) throw new Exception("IsAssessmentCompleted must be false");
        }

        public static void Test_SummaryViewModel_BuildsCorrectlyForPassingAttempt()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            workflow.WorkerId = "worker-fire-specialist-01";
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            var vm = AssessmentSummaryViewModel.Build(workflow.LatestAttempt, workflow.LatestAssessment);

            if (vm == null) throw new Exception("ViewModel was null");
            if (vm.ModuleTitle != "Fire & Explosion Response")
                throw new Exception($"Expected module title 'Fire & Explosion Response', got '{vm.ModuleTitle}'");
            if (!vm.Passed) throw new Exception("Expected Passed to be true");
            if (vm.ClientScore != 100.00f) throw new Exception($"Expected score 100.00, got {vm.ClientScore}");
            if (vm.ScoreDisplayText != "100.00 / 100")
                throw new Exception($"Expected ScoreDisplayText '100.00 / 100', got '{vm.ScoreDisplayText}'");
            if (vm.PassFailBadgeText != "PASS")
                throw new Exception($"Expected badge 'PASS', got '{vm.PassFailBadgeText}'");
            if (vm.PassFailColorHex != "#2ECC71")
                throw new Exception($"Expected pass color #2ECC71, got {vm.PassFailColorHex}");
            if (vm.StepSummaries.Count != 9)
                throw new Exception($"Expected 9 step summaries, got {vm.StepSummaries.Count}");
            if (vm.Penalties.Count != 0)
                throw new Exception($"Expected 0 penalties, got {vm.Penalties.Count}");
            if (string.IsNullOrEmpty(vm.SafetyFeedback))
                throw new Exception("Safety feedback should not be empty");
        }

        public static void Test_SummaryViewModel_BuildsCorrectlyForFailingAttempt()
        {
            var events = new List<TrainingEvent>
            {
                // Only detect hazard (+5) - total score 5% (fails < 70%)
                new TrainingEvent
                {
                    StepId = "step_detect_hazard",
                    EventType = "step_completed",
                    ActionId = "detect_hazard_acknowledged",
                    Outcome = "success"
                }
            };

            var rubric = RubricDefinition.CreateFireExplosionRubric();
            var assessment = LocalAssessmentEngine.Evaluate(events, rubric);
            var attempt = new TrainingAttempt("worker_failing_01", "fire-explosion-response");
            attempt.Complete(assessment.ClientScore, assessment.Passed);

            var vm = AssessmentSummaryViewModel.Build(attempt, assessment);

            if (vm == null) throw new Exception("ViewModel was null");
            if (vm.Passed) throw new Exception("Expected Passed to be false for 5% score");
            if (vm.PassFailBadgeText != "FAILED — RETAKE REQUIRED")
                throw new Exception($"Expected 'FAILED — RETAKE REQUIRED', got '{vm.PassFailBadgeText}'");
            if (vm.PassFailColorHex != "#E74C3C")
                throw new Exception($"Expected fail color #E74C3C, got {vm.PassFailColorHex}");
            if (!vm.SafetyFeedback.Contains("Standard Not Met"))
                throw new Exception("Safety feedback should mention Standard Not Met");
            if (!vm.SafetyFeedback.Contains("Retake training"))
                throw new Exception("Safety feedback should recommend retake");
        }

        public static void Test_SummaryViewModel_StepBreakdownHasAllNineSteps()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            var vm = AssessmentSummaryViewModel.Build(workflow.LatestAttempt, workflow.LatestAssessment);

            if (vm.StepSummaries.Count != 9)
                throw new Exception($"Expected 9 step breakdown rows, got {vm.StepSummaries.Count}");

            float totalMax = 0f;
            float totalAwarded = 0f;
            for (int i = 0; i < 9; i++)
            {
                var step = vm.StepSummaries[i];
                if (step.StepNumber != i + 1)
                    throw new Exception($"Step {i} has StepNumber {step.StepNumber}, expected {i + 1}");
                if (!step.IsSatisfied)
                    throw new Exception($"Step {step.StepNumber} should be satisfied");
                totalMax += step.MaxPoints;
                totalAwarded += step.NetScore;
            }

            if (totalMax != 100.00f)
                throw new Exception($"Sum of max points across 9 steps must be 100, got {totalMax}");
            if (totalAwarded != 100.00f)
                throw new Exception($"Sum of awarded points across 9 steps must be 100, got {totalAwarded}");
        }

        public static void Test_SummaryViewModel_CalculatesDurationCorrectly()
        {
            // Standard minutes + seconds
            string dur1 = AssessmentSummaryViewModel.FormatDuration("2026-09-16T08:00:00.0000000Z", "2026-09-16T08:01:45.0000000Z", out double secs1);
            if (dur1 != "01m 45s") throw new Exception($"Expected '01m 45s', got '{dur1}'");
            if (secs1 != 105.0) throw new Exception($"Expected 105 seconds, got {secs1}");

            // Seconds only
            string dur2 = AssessmentSummaryViewModel.FormatDuration("2026-09-16T08:00:00.0000000Z", "2026-09-16T08:00:25.0000000Z", out double secs2);
            if (dur2 != "25s") throw new Exception($"Expected '25s', got '{dur2}'");
            if (secs2 != 25.0) throw new Exception($"Expected 25 seconds, got {secs2}");

            // Invalid or missing values
            string dur3 = AssessmentSummaryViewModel.FormatDuration(null, null, out double secs3);
            if (dur3 != "--:--") throw new Exception($"Expected '--:--', got '{dur3}'");
            if (secs3 != 0.0) throw new Exception($"Expected 0 seconds, got {secs3}");
        }

        public static void Test_SummaryViewModel_CapturesPenaltiesCorrectly()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            workflow.ConfirmHazardDetected(bus, out _);
            // Incur Step 2 penalty (-5)
            workflow.SubmitHazardIdentification("hazard_chemical_spill", bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            // Incur Step 4 penalty (-5)
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherWater, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            var vm = AssessmentSummaryViewModel.Build(workflow.LatestAttempt, workflow.LatestAssessment);

            if (vm.ClientScore != 90.00f)
                throw new Exception($"Expected score 90.00 with two 5pt penalties, got {vm.ClientScore}");
            if (vm.Penalties.Count != 2)
                throw new Exception($"Expected 2 penalties captured, got {vm.Penalties.Count}");
            if (!vm.Passed)
                throw new Exception("Expected 90% attempt to pass");
            if (!vm.Penalties[0].Contains("Step 2: Identify Hazard"))
                throw new Exception($"Penalty 0 should mention Step 2: {vm.Penalties[0]}");
            if (!vm.Penalties[1].Contains("Step 4: Select Extinguisher"))
                throw new Exception($"Penalty 1 should mention Step 4: {vm.Penalties[1]}");
        }

        public static void Test_Workflow_RetakeCreatesNewUniqueAttemptId()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            // First run
            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            string firstAttemptId = workflow.LatestAttempt.ClientAttemptId;
            if (string.IsNullOrEmpty(firstAttemptId))
                throw new Exception("First attempt ID was null or empty");

            // Execute Retake / Reset
            workflow.Reset();
            bus.Clear();

            if (workflow.LatestAttempt != null)
                throw new Exception("LatestAttempt should be null after Reset()");
            if (workflow.LatestAssessment != null)
                throw new Exception("LatestAssessment should be null after Reset()");
            if (workflow.IsAssessmentCompleted)
                throw new Exception("IsAssessmentCompleted should be false after Reset()");

            // Second run
            workflow.SetStage(FireWorkflowStage.HazardPlaced);
            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            string secondAttemptId = workflow.LatestAttempt.ClientAttemptId;
            if (string.IsNullOrEmpty(secondAttemptId))
                throw new Exception("Second attempt ID was null or empty");

            if (firstAttemptId == secondAttemptId)
                throw new Exception($"Retake failed to create new unique attempt ID: {firstAttemptId} == {secondAttemptId}");

            // Verify both are valid non-empty GUIDs
            if (!Guid.TryParse(firstAttemptId, out _))
                throw new Exception($"First attempt ID was not a valid GUID: {firstAttemptId}");
            if (!Guid.TryParse(secondAttemptId, out _))
                throw new Exception($"Second attempt ID was not a valid GUID: {secondAttemptId}");
        }

        public static void Test_Assessment_RepeatedPenaltyCannotExceedRubricRules()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            workflow.ConfirmHazardDetected(bus, out _);

            // Incur repeated wrong hazard identification (3 distinct wrong attempts)
            workflow.SubmitHazardIdentification("hazard_chemical_spill", bus, out _);
            workflow.SubmitHazardIdentification("hazard_gas_leak", bus, out _);
            workflow.SubmitHazardIdentification("hazard_structural_collapse", bus, out _);
            // Now correct identification
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);

            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);

            // Incur repeated wrong extinguisher selection (3 distinct wrong attempts)
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherWater, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherFoam, bus, out _);
            workflow.SubmitSelectExtinguisher("extinguisher_wet_chemical", bus, out _);
            // Now correct extinguisher
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);

            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);

            // Incur repeated unsafe evacuation waypoint selection (3 smoke corridor attempts)
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.HazardSmokeCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.HazardSmokeCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.HazardSmokeCorridor, bus, out _);
            // Valid route sequence
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);

            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            var assessment = workflow.LatestAssessment;
            if (assessment == null) throw new Exception("Assessment was null");

            // Check Step 2 (Hazard Identification)
            var idRule = assessment.GetRuleResult("rule_identify_hazard");
            if (idRule == null) throw new Exception("rule_identify_hazard result not found");
            if (idRule.TimesAwarded != 1) throw new Exception($"rule_identify_hazard TimesAwarded was {idRule.TimesAwarded}, expected 1");
            if (idRule.PointsAwarded != 15.00f) throw new Exception($"rule_identify_hazard PointsAwarded was {idRule.PointsAwarded}, expected 15.00");
            if (idRule.TimesPenalized != 1) throw new Exception($"rule_identify_hazard TimesPenalized was {idRule.TimesPenalized}, expected 1 (capped at rubric AwardLimit)");
            if (idRule.PenaltyDeducted != 5.00f) throw new Exception($"rule_identify_hazard PenaltyDeducted was {idRule.PenaltyDeducted}, expected 5.00 (not 15.00)");
            if (idRule.NetScore != 10.00f) throw new Exception($"rule_identify_hazard NetScore was {idRule.NetScore}, expected 10.00");

            // Check Step 4 (Extinguisher Selection)
            var extRule = assessment.GetRuleResult("rule_select_extinguisher");
            if (extRule == null) throw new Exception("rule_select_extinguisher result not found");
            if (extRule.TimesAwarded != 1) throw new Exception($"rule_select_extinguisher TimesAwarded was {extRule.TimesAwarded}, expected 1");
            if (extRule.PointsAwarded != 15.00f) throw new Exception($"rule_select_extinguisher PointsAwarded was {extRule.PointsAwarded}, expected 15.00");
            if (extRule.TimesPenalized != 1) throw new Exception($"rule_select_extinguisher TimesPenalized was {extRule.TimesPenalized}, expected 1 (capped at rubric AwardLimit)");
            if (extRule.PenaltyDeducted != 5.00f) throw new Exception($"rule_select_extinguisher PenaltyDeducted was {extRule.PenaltyDeducted}, expected 5.00 (not 15.00)");
            if (extRule.NetScore != 10.00f) throw new Exception($"rule_select_extinguisher NetScore was {extRule.NetScore}, expected 10.00");

            // Check Step 8 (Evacuation Route)
            var evacRule = assessment.GetRuleResult("rule_evacuate_route");
            if (evacRule == null) throw new Exception("rule_evacuate_route result not found");
            if (evacRule.TimesAwarded != 1) throw new Exception($"rule_evacuate_route TimesAwarded was {evacRule.TimesAwarded}, expected 1");
            if (evacRule.PointsAwarded != 10.00f) throw new Exception($"rule_evacuate_route PointsAwarded was {evacRule.PointsAwarded}, expected 10.00");
            if (evacRule.TimesPenalized != 1) throw new Exception($"rule_evacuate_route TimesPenalized was {evacRule.TimesPenalized}, expected 1 (capped at rubric AwardLimit)");
            if (evacRule.PenaltyDeducted != 5.00f) throw new Exception($"rule_evacuate_route PenaltyDeducted was {evacRule.PenaltyDeducted}, expected 5.00 (not 15.00)");
            if (evacRule.NetScore != 5.00f) throw new Exception($"rule_evacuate_route NetScore was {evacRule.NetScore}, expected 5.00");

            // Overall totals
            if (assessment.TotalAwarded != 100.00f) throw new Exception($"Expected TotalAwarded 100.00, got {assessment.TotalAwarded}");
            if (assessment.TotalPenalties != 15.00f) throw new Exception($"Expected TotalPenalties 15.00 (5+5+5), got {assessment.TotalPenalties}");
            if (assessment.ClientScore != 85.00f) throw new Exception($"Expected ClientScore 85.00, got {assessment.ClientScore}");
            if (!assessment.Passed) throw new Exception("Expected Passed to be true (85 >= 70)");
        }

        public static void Test_Assessment_ScoreThresholdAndRequiredRuleCompliance()
        {
            var rubric = RubricDefinition.CreateFireExplosionRubric();

            // Case A: Near-threshold score of 65.00 (70 points awarded minus 5 penalty)
            // Steps 1 (+5), 2 (+15 with -5 penalty = 10), 3 (+15), 4 (+15), 5 (+10), 7 (+10) = 65.00
            var eventsNearFail = new List<TrainingEvent>
            {
                new TrainingEvent { StepId = "step_detect_hazard", EventType = "step_completed", ActionId = "detect_hazard_acknowledged", Outcome = "success" },
                new TrainingEvent { StepId = "step_identify_hazard", EventType = "hazard_identified", Outcome = "failure" }, // -5 penalty
                new TrainingEvent { StepId = "step_identify_hazard", EventType = "hazard_identified", TargetId = "hazard_electrical_conveyor_fire", Outcome = "success" }, // +15 award
                new TrainingEvent { StepId = "step_raise_alarm", EventType = "alarm_raised", ActionId = "manual_call_point_activated", Outcome = "success" }, // +15 award
                new TrainingEvent { StepId = "step_select_extinguisher", EventType = "extinguisher_selected", TargetId = "extinguisher_co2", Outcome = "success" }, // +15 award
                new TrainingEvent { StepId = "step_maintain_distance", EventType = "decision_made", TargetId = "standoff_distance_2m_maintained", Outcome = "success" }, // +10 award
                new TrainingEvent { StepId = "step_identify_exit", EventType = "exit_marked", TargetId = "exit_emergency_sector_b", Outcome = "success" } // +10 award
            };
            var evalA = LocalAssessmentEngine.Evaluate(eventsNearFail, rubric);
            if (evalA.ClientScore != 65.00f)
                throw new Exception($"Case A: Expected ClientScore 65.00, got {evalA.ClientScore}");
            if (evalA.Passed)
                throw new Exception("Case A: Score of 65.00% must FAIL (< 70% threshold)");

            // Case B: Boundary score of exactly 70.00% with required rules satisfied
            var boundaryRubric = new RubricDefinition
            {
                PassPercent = 70.00f,
                MaxScore = 100.00f,
                Rules = new List<RubricRule>
                {
                    new RubricRule { RuleId = "r1", Required = true, EventType = "step_completed", Points = 70.00f, Match = new RuleMatchCriteria { Outcome = "success" } },
                    new RubricRule { RuleId = "r2", Required = false, EventType = "step_completed", Points = 30.00f, Match = new RuleMatchCriteria { ActionId = "optional" } }
                }
            };
            var evalB = LocalAssessmentEngine.Evaluate(new List<TrainingEvent>
            {
                new TrainingEvent { EventType = "step_completed", Outcome = "success" }
            }, boundaryRubric);
            if (evalB.ClientScore != 70.00f)
                throw new Exception($"Case B: Expected ClientScore 70.00, got {evalB.ClientScore}");
            if (!evalB.Passed)
                throw new Exception("Case B: Score of exactly 70.00% with required rules satisfied must PASS (70 >= 70)");

            // Case C: Score >= 70.00 but a REQUIRED rule was not satisfied
            var reqRubric = new RubricDefinition
            {
                PassPercent = 70.00f,
                MaxScore = 100.00f,
                Rules = new List<RubricRule>
                {
                    new RubricRule { RuleId = "r1", Required = true, EventType = "step_completed", Points = 80.00f, Match = new RuleMatchCriteria { ActionId = "action_one" } },
                    new RubricRule { RuleId = "r2_mandatory", Required = true, EventType = "step_completed", Points = 20.00f, Match = new RuleMatchCriteria { ActionId = "action_mandatory" } }
                }
            };
            var evalC = LocalAssessmentEngine.Evaluate(new List<TrainingEvent>
            {
                new TrainingEvent { EventType = "step_completed", ActionId = "action_one" } // only r1, score 80
            }, reqRubric);
            if (evalC.ClientScore != 80.00f)
                throw new Exception($"Case C: Expected ClientScore 80.00, got {evalC.ClientScore}");
            if (evalC.Passed)
                throw new Exception("Case C: High score (80%) with missing required rule must strictly FAIL");
        }

        public static void Test_CompletedTrainingAttemptMatchesAttemptSchemaJson()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();
            string testWorkerGuid = Guid.NewGuid().ToString();
            workflow.WorkerId = testWorkerGuid;
            workflow.SetStage(FireWorkflowStage.HazardPlaced);

            // Execute full 9-step workflow
            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            var attempt = workflow.LatestAttempt;
            if (attempt == null) throw new Exception("LatestAttempt was null");

            // 1. schema_version: const "1.0.0"
            if (attempt.SchemaVersion != "1.0.0")
                throw new Exception($"schema_version must be '1.0.0', got '{attempt.SchemaVersion}'");

            // 2. client_attempt_id: format uuid (never empty, valid 36-char GUID)
            if (string.IsNullOrEmpty(attempt.ClientAttemptId))
                throw new Exception("client_attempt_id is missing or empty");
            if (!Guid.TryParse(attempt.ClientAttemptId, out var attemptGuid) || attemptGuid == Guid.Empty)
                throw new Exception($"client_attempt_id is not a valid non-empty UUID: '{attempt.ClientAttemptId}'");
            if (attempt.ClientAttemptId.Length != 36 || attempt.ClientAttemptId.Split('-').Length != 5)
                throw new Exception($"client_attempt_id is not canonical UUID format: '{attempt.ClientAttemptId}'");

            // 3. worker_id: format uuid
            if (string.IsNullOrEmpty(attempt.WorkerId))
                throw new Exception("worker_id is missing or empty");
            if (!Guid.TryParse(attempt.WorkerId, out var workerGuid) || workerGuid == Guid.Empty)
                throw new Exception($"worker_id is not a valid non-empty UUID: '{attempt.WorkerId}'");
            if (attempt.WorkerId != testWorkerGuid)
                throw new Exception($"worker_id mismatch: expected '{testWorkerGuid}', got '{attempt.WorkerId}'");

            // 4. module_id: pattern ^(fire-explosion-response|gas-confined-space|[a-z][a-z0-9-]{2,61})$
            if (attempt.ModuleId != "fire-explosion-response")
                throw new Exception($"module_id mismatch: '{attempt.ModuleId}'");
            if (attempt.ModuleId.Length > 64)
                throw new Exception($"module_id exceeds maxLength 64: length {attempt.ModuleId.Length}");

            // 5. content_version: pattern ^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$
            if (attempt.ContentVersion != "1.0.0")
                throw new Exception($"content_version mismatch: '{attempt.ContentVersion}'");
            if (attempt.ContentVersion.Length > 32)
                throw new Exception("content_version exceeds maxLength 32");

            // 6. started_at: format date-time
            if (string.IsNullOrEmpty(attempt.StartedAt))
                throw new Exception("started_at is missing or empty");
            if (!DateTime.TryParse(attempt.StartedAt, out var startedAt))
                throw new Exception($"started_at is not valid ISO date-time: '{attempt.StartedAt}'");

            // 7. completed_at: required when status is completed, format date-time
            if (string.IsNullOrEmpty(attempt.CompletedAt))
                throw new Exception("completed_at is required when status is completed");
            if (!DateTime.TryParse(attempt.CompletedAt, out var completedAt))
                throw new Exception($"completed_at is not valid ISO date-time: '{attempt.CompletedAt}'");
            if (completedAt < startedAt)
                throw new Exception($"completed_at ({completedAt}) cannot precede started_at ({startedAt})");

            // 8. status: enum ["in_progress", "completed", "abandoned"]
            if (attempt.Status != TrainingAttempt.StatusCompleted)
                throw new Exception($"status must be 'completed', got '{attempt.Status}'");

            // 9. client_score: number, minimum 0, maximum 10000, multipleOf 0.01
            if (attempt.ClientScore < 0.00f || attempt.ClientScore > 10000.00f)
                throw new Exception($"client_score {attempt.ClientScore} out of bounds [0, 10000]");
            if (Math.Abs(attempt.ClientScore - (float)Math.Round(attempt.ClientScore, 2)) > 0.001f)
                throw new Exception($"client_score {attempt.ClientScore} violates multipleOf 0.01");
            if (attempt.ClientScore != 100.00f)
                throw new Exception($"client_score was {attempt.ClientScore}, expected 100.00");

            // 10. passed: boolean
            if (!attempt.Passed)
                throw new Exception("passed must be true for 100 score");

            // 11. events: array, maxItems 10000
            if (attempt.Events == null || attempt.Events.Count == 0)
                throw new Exception("events list must not be null or empty");
            if (attempt.Events.Count > 10000)
                throw new Exception("events list exceeds maxItems 10000");
            if (attempt.Events.Count != 14)
                throw new Exception($"Expected 14 events in attempt, got {attempt.Events.Count}");

            // Verify enclosed attempt events against attempt-event.schema.json
            for (int i = 0; i < attempt.Events.Count; i++)
            {
                var evt = attempt.Events[i];
                if (string.IsNullOrEmpty(evt.EventId) || !Guid.TryParse(evt.EventId, out _))
                    throw new Exception($"Event {i} has invalid client_event_id (UUID): '{evt.EventId}'");
                if (string.IsNullOrEmpty(evt.StepId) || evt.StepId.Length > 64)
                    throw new Exception($"Event {i} has invalid step_id: '{evt.StepId}'");
                if (string.IsNullOrEmpty(evt.EventType))
                    throw new Exception($"Event {i} has empty event_type");
                if (evt.TimestampUnixMs <= 0)
                    throw new Exception($"Event {i} has invalid timestamp: {evt.TimestampUnixMs}");
                if (evt.Payload == null)
                    throw new Exception($"Event {i} has null payload");
            }

            // 12. Offline attempt must NOT require or populate server fields
            if (attempt.ServerAttemptId != null)
                throw new Exception("server_attempt_id must be null/absent for offline attempt");
            if (attempt.ServerScore.HasValue)
                throw new Exception("server_score must be null for offline attempt");
            if (attempt.ServerPassed.HasValue)
                throw new Exception("server_passed must be null for offline attempt");
            if (attempt.CertificatePublicId != null)
                throw new Exception("certificate_public_id must be null for offline attempt");
        }

        public static void Test_AssemblyPoint_PrematureAndDuplicateSubmissionsStrictlyRejected()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var workflow = new FireTrainingWorkflow();

            // Verify premature submission rejected across all initial and mid stages:
            var testStages = new FireWorkflowStage[]
            {
                FireWorkflowStage.NotStarted,
                FireWorkflowStage.WaitingForTracking,
                FireWorkflowStage.ReadyToPlace,
                FireWorkflowStage.HazardPlaced,
                FireWorkflowStage.HazardDetected,
                FireWorkflowStage.AwaitingIdentification,
                FireWorkflowStage.HazardIdentified,
                FireWorkflowStage.AwaitingAlarm,
                FireWorkflowStage.AlarmRaised,
                FireWorkflowStage.AwaitingExtinguisherSelection,
                FireWorkflowStage.ExtinguisherSelected,
                FireWorkflowStage.AwaitingSafeDistance,
                FireWorkflowStage.SafeDistanceMaintained,
                FireWorkflowStage.PinPulled,
                FireWorkflowStage.AimConfirmed,
                FireWorkflowStage.HandleSqueezed,
                FireWorkflowStage.ExtinguisherDischarged,
                FireWorkflowStage.AwaitingExitIdentification,
                FireWorkflowStage.ExitIdentified,
                FireWorkflowStage.AwaitingEvacuationRoute,
                FireWorkflowStage.WaypointMainCorridorReached,
                FireWorkflowStage.WaypointBypassCrosscutReached
            };

            foreach (var stage in testStages)
            {
                workflow.SetStage(stage);
                bus.Clear();

                bool premature = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out var evt);
                if (premature)
                    throw new Exception($"SubmitReachAssemblyPoint unexpectedly succeeded at premature stage '{stage}'");
                if (evt != null)
                    throw new Exception($"Premature call at stage '{stage}' emitted non-null event");
                if (bus.DispatchedEvents.Count != 0)
                    throw new Exception($"Premature call at stage '{stage}' dispatched events to bus");
                if (workflow.CurrentStage != stage)
                    throw new Exception($"Stage mutated from '{stage}' to '{workflow.CurrentStage}' on premature call");
                if (workflow.LatestAttempt != null)
                    throw new Exception($"LatestAttempt was created at premature stage '{stage}'");
                if (workflow.IsAssessmentCompleted)
                    throw new Exception($"IsAssessmentCompleted became true at premature stage '{stage}'");
            }

            // Now transition to RouteEvacuated (ready for assembly point)
            workflow.SetStage(FireWorkflowStage.RouteEvacuated);
            bus.Clear();

            // Test wrong target (e.g. TargetAssemblyPointBeta or invalid target)
            bool wrongTarget = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyPointBeta, bus, out var failEvt);
            if (wrongTarget)
                throw new Exception("SubmitReachAssemblyPoint with wrong target should return false");
            if (failEvt == null || failEvt.Outcome != "failure")
                throw new Exception("Wrong target submission should emit a failure event");
            if (workflow.CurrentStage != FireWorkflowStage.RouteEvacuated)
                throw new Exception("Stage should remain RouteEvacuated after wrong target submission");
            if (workflow.IsAssessmentCompleted)
                throw new Exception("Assessment must not be completed after wrong target submission");

            // Test null target
            bool nullTarget = workflow.SubmitReachAssemblyPoint(null, bus, out var nullEvt);
            if (nullTarget)
                throw new Exception("SubmitReachAssemblyPoint with null target should return false");

            // Now valid submission to TargetAssemblyMusterPoint
            bool validSub = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out var successEvt);
            if (!validSub)
                throw new Exception("Valid SubmitReachAssemblyPoint failed");
            if (successEvt == null || successEvt.Outcome != "success")
                throw new Exception("Valid submission should emit success event");
            if (workflow.CurrentStage != FireWorkflowStage.AssemblyPointReached)
                throw new Exception($"Expected AssemblyPointReached, got '{workflow.CurrentStage}'");
            if (!workflow.IsAssessmentCompleted)
                throw new Exception("IsAssessmentCompleted should be true after valid submission");

            string attemptId = workflow.LatestAttempt.ClientAttemptId;
            var assessment = workflow.LatestAssessment;
            int eventCountBeforeDup = bus.DispatchedEvents.Count;

            // Test duplicate submission when already reached
            bool dupSub = workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out var dupEvt);
            if (dupSub)
                throw new Exception("Duplicate SubmitReachAssemblyPoint must return false");
            if (dupEvt != null)
                throw new Exception("Duplicate submission must not emit an event");
            if (bus.DispatchedEvents.Count != eventCountBeforeDup)
                throw new Exception("Duplicate submission dispatched extra events to bus");
            if (workflow.LatestAttempt.ClientAttemptId != attemptId)
                throw new Exception("Duplicate submission modified client_attempt_id");
            if (!ReferenceEquals(workflow.LatestAssessment, assessment))
                throw new Exception("Duplicate submission modified assessment instance");

            // Test duplicate EvaluateAssessment call returns cached instance
            var reEval = workflow.EvaluateAssessment(bus);
            if (!ReferenceEquals(reEval, assessment))
                throw new Exception("EvaluateAssessment did not return cached assessment on duplicate call");
        }

        public static void Test_Retake_EnsuresZeroIdReuseAndCleanStateReset()
        {
            var bus = new TrainingEventBus();
            var workflow = new FireTrainingWorkflow();
            workflow.WorkerId = Guid.NewGuid().ToString();

            // RUN 1: Complete full scenario
            bus.Clear();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);
            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            string id1 = workflow.LatestAttempt.ClientAttemptId;
            float score1 = workflow.LatestAttempt.ClientScore;
            if (string.IsNullOrEmpty(id1)) throw new Exception("Run 1 attempt ID was empty");
            if (score1 != 100.00f) throw new Exception($"Run 1 score was {score1}, expected 100.00");

            // RETAKE 1: Reset workflow
            workflow.Reset();
            bus.Clear();

            if (workflow.CurrentStage != FireWorkflowStage.NotStarted)
                throw new Exception($"Reset failed to restore stage to NotStarted, was {workflow.CurrentStage}");
            if (workflow.LatestAttempt != null)
                throw new Exception("LatestAttempt must be null after Reset()");
            if (workflow.LatestAssessment != null)
                throw new Exception("LatestAssessment must be null after Reset()");
            if (workflow.IsAssessmentCompleted)
                throw new Exception("IsAssessmentCompleted must be false after Reset()");

            // RUN 2: Incur 1 penalty (-5 on hazard identification)
            workflow.SetStage(FireWorkflowStage.HazardPlaced);
            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification("hazard_chemical_spill", bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            string id2 = workflow.LatestAttempt.ClientAttemptId;
            float score2 = workflow.LatestAttempt.ClientScore;
            if (string.IsNullOrEmpty(id2)) throw new Exception("Run 2 attempt ID was empty");
            if (score2 != 95.00f) throw new Exception($"Run 2 score was {score2}, expected 95.00");
            if (id1 == id2) throw new Exception($"Attempt ID reused across retakes: id1 '{id1}' == id2 '{id2}'");

            // RETAKE 2: Reset workflow again
            workflow.Reset();
            bus.Clear();

            // RUN 3: Another run
            workflow.SetStage(FireWorkflowStage.HazardPlaced);
            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            string id3 = workflow.LatestAttempt.ClientAttemptId;
            if (string.IsNullOrEmpty(id3)) throw new Exception("Run 3 attempt ID was empty");
            if (id3 == id1 || id3 == id2)
                throw new Exception($"Attempt ID collision: id3 '{id3}' collided with id1 '{id1}' or id2 '{id2}'");

            // Verify all 3 IDs are unique and valid GUIDs
            var seenIds = new HashSet<string> { id1, id2, id3 };
            if (seenIds.Count != 3) throw new Exception("Expected 3 unique attempt IDs");
            foreach (var id in seenIds)
            {
                if (!Guid.TryParse(id, out var g) || g == Guid.Empty)
                    throw new Exception($"Attempt ID '{id}' is not a valid GUID");
            }
        }

        public static void Test_Workflow_DefaultWorkerId_IsValidNonEmptyUuid()
        {
            var workflow = new FireTrainingWorkflow();

            // 1. WorkerId must not be null or empty
            if (string.IsNullOrEmpty(workflow.WorkerId))
                throw new Exception("Default WorkerId must not be null or empty");

            // 2. WorkerId must be a valid non-empty GUID
            if (!Guid.TryParse(workflow.WorkerId, out var workerGuid) || workerGuid == Guid.Empty)
                throw new Exception($"Default WorkerId '{workflow.WorkerId}' is not a valid non-empty UUID");

            // 3. Must match DefaultOfflineWorkerId
            if (workflow.WorkerId != FireTrainingWorkflow.DefaultOfflineWorkerId)
                throw new Exception($"WorkerId '{workflow.WorkerId}' does not match DefaultOfflineWorkerId '{FireTrainingWorkflow.DefaultOfflineWorkerId}'");

            // 4. Must match attempt.schema.json format (36 chars, 4 hyphens, lowercase hex)
            if (workflow.WorkerId.Length != 36)
                throw new Exception($"WorkerId '{workflow.WorkerId}' must be exactly 36 characters");

            // 5. Complete an attempt and verify the generated TrainingAttempt retains the valid UUID
            var bus = new TrainingEventBus();
            bus.Clear();
            workflow.SetStage(FireWorkflowStage.HazardPlaced);
            workflow.ConfirmHazardDetected(bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            if (workflow.LatestAttempt == null)
                throw new Exception("Workflow failed to produce LatestAttempt");

            if (workflow.LatestAttempt.WorkerId != FireTrainingWorkflow.DefaultOfflineWorkerId)
                throw new Exception($"Attempt WorkerId '{workflow.LatestAttempt.WorkerId}' does not match DefaultOfflineWorkerId");

            if (!Guid.TryParse(workflow.LatestAttempt.WorkerId, out var attemptWorkerGuid) || attemptWorkerGuid == Guid.Empty)
                throw new Exception($"Attempt WorkerId '{workflow.LatestAttempt.WorkerId}' is not a valid UUID");
        }

        public static void Test_RubricLoader_LoadsBundledFireRubric()
        {
            // 1. Load via RubricLoader static method
            var rubric = RubricLoader.LoadFireExplosionRubric();
            if (rubric == null)
                throw new Exception("RubricLoader.LoadFireExplosionRubric() returned null");

            // 2. Load via IRubricProvider interface
            IRubricProvider provider = new BundledRubricProvider();
            var rubricFromProvider = provider.LoadRubric(RubricLoader.FireModuleId);
            if (rubricFromProvider == null)
                throw new Exception("BundledRubricProvider.LoadRubric() returned null");

            // 3. Test FromJson directly on bundled JSON string
            var fromJson = RubricDefinition.FromJson(RubricLoader.BundledFireRubricJson);
            if (fromJson == null)
                throw new Exception("RubricDefinition.FromJson(BundledFireRubricJson) returned null");

            // 4. Test loading from resolved file path if present on disk
            string resolvedPath = RubricLoader.ResolveRubricFilePath(RubricLoader.FireModuleId);
            if (!string.IsNullOrEmpty(resolvedPath) && System.IO.File.Exists(resolvedPath))
            {
                var fromFile = RubricLoader.LoadFromFile(resolvedPath);
                if (fromFile == null)
                    throw new Exception($"RubricLoader.LoadFromFile('{resolvedPath}') returned null");
                if (fromFile.ModuleId != "fire-explosion-response")
                    throw new Exception($"FromFile ModuleId mismatch: {fromFile.ModuleId}");
            }

            // 5. Test error handling for invalid/empty JSON
            try
            {
                RubricDefinition.FromJson("");
                throw new Exception("Expected ArgumentException on empty JSON, but none was thrown");
            }
            catch (ArgumentException)
            {
                // expected
            }
        }

        public static void Test_LoadedFireRubric_IdentityAndVersion()
        {
            var rubric = RubricLoader.LoadFireExplosionRubric();

            // 1. Schema version
            if (rubric.SchemaVersion != "1.0.0")
                throw new Exception($"SchemaVersion mismatch: expected '1.0.0', got '{rubric.SchemaVersion}'");

            // 2. Module identity
            if (rubric.ModuleId != "fire-explosion-response")
                throw new Exception($"ModuleId mismatch: expected 'fire-explosion-response', got '{rubric.ModuleId}'");

            // 3. Content version
            if (rubric.ContentVersion != "1.0.0")
                throw new Exception($"ContentVersion mismatch: expected '1.0.0', got '{rubric.ContentVersion}'");

            // 4. Pass percent
            if (Math.Abs(rubric.PassPercent - 70.00f) > 0.001f)
                throw new Exception($"PassPercent mismatch: expected 70.00, got {rubric.PassPercent}");

            // 5. Max score
            if (Math.Abs(rubric.MaxScore - 100.00f) > 0.001f)
                throw new Exception($"MaxScore mismatch: expected 100.00, got {rubric.MaxScore}");
        }

        public static void Test_LoadedFireRubric_AllNineRulesAvailableAndConfigured()
        {
            var rubric = RubricLoader.LoadFireExplosionRubric();

            if (rubric.Rules == null)
                throw new Exception("rubric.Rules must not be null");

            // Verify exactly 9 rules
            if (rubric.Rules.Count != 9)
                throw new Exception($"Expected 9 rules in rubric, found {rubric.Rules.Count}");

            // Expected 9 rules specification: (ruleId, stepId, required, eventType, points, penaltyPoints)
            var expectedRules = new (string ruleId, string stepId, bool required, string eventType, float points, float penaltyPoints)[]
            {
                ("rule_detect_hazard", "step_detect_hazard", true, "step_completed", 5.00f, 0.00f),
                ("rule_identify_hazard", "step_identify_hazard", true, "hazard_identified", 15.00f, 5.00f),
                ("rule_raise_alarm", "step_raise_alarm", true, "alarm_raised", 15.00f, 0.00f),
                ("rule_select_extinguisher", "step_select_extinguisher", true, "extinguisher_selected", 15.00f, 5.00f),
                ("rule_maintain_distance", "step_maintain_distance", true, "decision_made", 10.00f, 0.00f),
                ("rule_use_extinguisher", "step_use_extinguisher", true, "extinguisher_used", 15.00f, 5.00f),
                ("rule_identify_exit", "step_identify_exit", true, "exit_marked", 10.00f, 0.00f),
                ("rule_evacuate_route", "step_evacuate_route", true, "evacuation_sequence_submitted", 10.00f, 5.00f),
                ("rule_reach_assembly", "step_reach_assembly", true, "assembly_reached", 5.00f, 0.00f),
            };

            float totalAvailablePoints = 0f;

            for (int i = 0; i < expectedRules.Length; i++)
            {
                var exp = expectedRules[i];
                var rule = rubric.Rules.Find(r => r.RuleId == exp.ruleId);
                if (rule == null)
                    throw new Exception($"Rule '{exp.ruleId}' is missing from loaded rubric");

                if (rule.StepId != exp.stepId)
                    throw new Exception($"Rule '{exp.ruleId}' step_id mismatch: expected '{exp.stepId}', got '{rule.StepId}'");

                if (rule.Required != exp.required)
                    throw new Exception($"Rule '{exp.ruleId}' required mismatch: expected {exp.required}, got {rule.Required}");

                if (rule.EventType != exp.eventType)
                    throw new Exception($"Rule '{exp.ruleId}' event_type mismatch: expected '{exp.eventType}', got '{rule.EventType}'");

                if (Math.Abs(rule.Points - exp.points) > 0.001f)
                    throw new Exception($"Rule '{exp.ruleId}' points mismatch: expected {exp.points}, got {rule.Points}");

                if (Math.Abs(rule.PenaltyPoints - exp.penaltyPoints) > 0.001f)
                    throw new Exception($"Rule '{exp.ruleId}' penalty_points mismatch: expected {exp.penaltyPoints}, got {rule.PenaltyPoints}");

                if (rule.AwardLimit != 1)
                    throw new Exception($"Rule '{exp.ruleId}' award_limit mismatch: expected 1, got {rule.AwardLimit}");

                if (rule.Match == null)
                    throw new Exception($"Rule '{exp.ruleId}' match criteria must not be null");

                if (exp.penaltyPoints > 0f && rule.PenaltyMatch == null)
                    throw new Exception($"Rule '{exp.ruleId}' has penalties but penalty_match criteria is null");

                totalAvailablePoints += rule.Points;
            }

            if (Math.Abs(totalAvailablePoints - rubric.MaxScore) > 0.001f)
                throw new Exception($"Sum of rule points ({totalAvailablePoints}) does not equal MaxScore ({rubric.MaxScore})");
        }

        public static void Test_Workflow_UsesLoadedRubricForEvaluation()
        {
            // 1. Verify workflow automatically binds the loaded rubric upon construction
            var workflow = new FireTrainingWorkflow();
            if (workflow.BoundRubric == null)
                throw new Exception("Workflow.BoundRubric must be automatically bound on construction");

            if (workflow.BoundRubric.ModuleId != "fire-explosion-response")
                throw new Exception($"BoundRubric ModuleId mismatch: {workflow.BoundRubric.ModuleId}");

            if (workflow.BoundRubric.Rules.Count != 9)
                throw new Exception($"BoundRubric Rules count mismatch: {workflow.BoundRubric.Rules.Count}");

            // 2. Explicitly bind loaded rubric from provider
            var provider = new BundledRubricProvider();
            var loadedRubric = provider.LoadRubric("fire-explosion-response");
            workflow.BindRubric(loadedRubric);

            if (!object.ReferenceEquals(workflow.BoundRubric, loadedRubric))
                throw new Exception("BindRubric did not update BoundRubric reference");

            // 3. Execute scenario with 2 penalties to verify evaluation executes against loaded rubric
            var bus = new TrainingEventBus();
            bus.Clear();

            workflow.SetStage(FireWorkflowStage.HazardPlaced);
            workflow.ConfirmHazardDetected(bus, out _);
            // Incur penalty 1: wrong hazard identification (-5)
            workflow.SubmitHazardIdentification("hazard_chemical_spill", bus, out _);
            workflow.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            workflow.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            // Incur penalty 2: wrong extinguisher selection (-5)
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherWater, bus, out _);
            workflow.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            workflow.SubmitDistanceDecision(2.5f, bus, out _);
            workflow.SubmitPullPin(bus, out _);
            workflow.SubmitAim(bus, out _);
            workflow.SubmitSqueeze(bus, out _);
            workflow.SubmitSweep(bus, out _);
            workflow.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            workflow.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            workflow.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            // 4. Verify evaluation results match loaded rubric calculations
            var assessment = workflow.LatestAssessment;
            if (assessment == null)
                throw new Exception("AssessmentResult was not produced");

            if (assessment.RuleResults.Count != 9)
                throw new Exception($"Expected 9 rule evaluation results, got {assessment.RuleResults.Count}");

            // Net score should be 100 - 5 - 5 = 90.00
            if (Math.Abs(assessment.ClientScore - 90.00f) > 0.001f)
                throw new Exception($"ClientScore was {assessment.ClientScore}, expected 90.00");

            if (!assessment.Passed)
                throw new Exception("Assessment should be PASSED with 90% score");

            if (Math.Abs(assessment.TotalPenalties - 10.00f) > 0.001f)
                throw new Exception($"TotalPenalties was {assessment.TotalPenalties}, expected 10.00");

            // 5. Verify the generated TrainingAttempt reflects the bound rubric schema & content version
            var attempt = workflow.LatestAttempt;
            if (attempt == null)
                throw new Exception("LatestAttempt was not produced");

            if (attempt.SchemaVersion != loadedRubric.SchemaVersion)
                throw new Exception($"Attempt SchemaVersion '{attempt.SchemaVersion}' != rubric '{loadedRubric.SchemaVersion}'");

            if (attempt.ContentVersion != loadedRubric.ContentVersion)
                throw new Exception($"Attempt ContentVersion '{attempt.ContentVersion}' != rubric '{loadedRubric.ContentVersion}'");
        }

        public static void Test_DualInput_Step1ToStep9Equivalence()
        {
            var busA = new TrainingEventBus();
            var busB = new TrainingEventBus();

            var wfA = new FireTrainingWorkflow();
            var wfB = new FireTrainingWorkflow();

            // Step 1: Detect hazard
            // Path A: Physical AR Marker tap
            wfA.SetStage(FireWorkflowStage.HazardPlaced);
            bool okA1 = wfA.ConfirmHazardDetected(busA, out var evtA1);
            // Path B: UI Button tap
            wfB.SetStage(FireWorkflowStage.HazardPlaced);
            bool okB1 = wfB.ConfirmHazardDetected(busB, out var evtB1);

            if (!okA1 || !okB1) throw new Exception("Step 1 confirmation failed");
            AssertEventsEqual(evtA1, evtB1, "Step 1");

            // Step 2: Identify hazard
            // Path A: Physical AR Marker tap (hazard target ID)
            bool okA2 = wfA.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, busA, out var evtA2);
            // Path B: UI Button tap
            bool okB2 = wfB.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, busB, out var evtB2);

            if (!okA2 || !okB2) throw new Exception("Step 2 identification failed");
            AssertEventsEqual(evtA2, evtB2, "Step 2");

            // Step 3: Raise alarm
            // Path A: Physical AR Marker tap
            bool okA3 = wfA.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, busA, out var evtA3);
            // Path B: UI Button tap
            bool okB3 = wfB.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, busB, out var evtB3);

            if (!okA3 || !okB3) throw new Exception("Step 3 alarm failed");
            AssertEventsEqual(evtA3, evtB3, "Step 3");

            // Step 4: Select extinguisher
            // Path A: Physical AR Marker tap (CO2)
            bool okA4 = wfA.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, busA, out var evtA4);
            // Path B: UI Button tap
            bool okB4 = wfB.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, busB, out var evtB4);

            if (!okA4 || !okB4) throw new Exception("Step 4 extinguisher failed");
            AssertEventsEqual(evtA4, evtB4, "Step 4");

            // Step 5: Safe distance
            // Path A: Physical AR raycast ground distance (2.5m)
            bool okA5 = wfA.SubmitDistanceDecision(2.5f, busA, out var evtA5);
            // Path B: UI Button tap (DecisionSafeDistance2m)
            bool okB5 = wfB.SubmitDistanceDecision(FireTrainingWorkflow.DecisionSafeDistance2m, busB, out var evtB5);

            if (!okA5 || !okB5) throw new Exception("Step 5 safe distance failed");
            AssertEventsEqual(evtA5, evtB5, "Step 5");

            // Step 6: PASS procedure
            // 6.1 Pull Pin
            bool okA6_1 = wfA.SubmitPullPin(busA, out var evtA6_1);
            bool okB6_1 = wfB.SubmitPullPin(busB, out var evtB6_1);
            if (!okA6_1 || !okB6_1) throw new Exception("Step 6.1 pull pin failed");
            AssertEventsEqual(evtA6_1, evtB6_1, "Step 6.1");

            // 6.2 Aim
            bool okA6_2 = wfA.SubmitAim(busA, out var evtA6_2);
            bool okB6_2 = wfB.SubmitAim(busB, out var evtB6_2);
            if (!okA6_2 || !okB6_2) throw new Exception("Step 6.2 aim failed");
            AssertEventsEqual(evtA6_2, evtB6_2, "Step 6.2");

            // 6.3 Squeeze
            bool okA6_3 = wfA.SubmitSqueeze(busA, out var evtA6_3);
            bool okB6_3 = wfB.SubmitSqueeze(busB, out var evtB6_3);
            if (!okA6_3 || !okB6_3) throw new Exception("Step 6.3 squeeze failed");
            AssertEventsEqual(evtA6_3, evtB6_3, "Step 6.3");

            // 6.4 Sweep
            bool okA6_4 = wfA.SubmitSweep(busA, out var evtA6_4);
            bool okB6_4 = wfB.SubmitSweep(busB, out var evtB6_4);
            if (!okA6_4 || !okB6_4) throw new Exception("Step 6.4 sweep failed");
            AssertEventsEqual(evtA6_4, evtB6_4, "Step 6.4");

            // Step 7: Emergency exit
            // Path A: Physical AR Marker tap
            bool okA7 = wfA.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, busA, out var evtA7);
            // Path B: UI Button tap
            bool okB7 = wfB.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, busB, out var evtB7);

            if (!okA7 || !okB7) throw new Exception("Step 7 exit failed");
            AssertEventsEqual(evtA7, evtB7, "Step 7");

            // Step 8: Evacuation route waypoints
            // Waypoint 1
            bool okA8_1 = wfA.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, busA, out var evtA8_1);
            bool okB8_1 = wfB.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, busB, out var evtB8_1);
            if (!okA8_1 || !okB8_1) throw new Exception("Step 8.1 waypoint failed");
            AssertEventsEqual(evtA8_1, evtB8_1, "Step 8.1");

            // Waypoint 2
            bool okA8_2 = wfA.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, busA, out var evtA8_2);
            bool okB8_2 = wfB.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, busB, out var evtB8_2);
            if (!okA8_2 || !okB8_2) throw new Exception("Step 8.2 waypoint failed");
            AssertEventsEqual(evtA8_2, evtB8_2, "Step 8.2");

            // Waypoint 3
            bool okA8_3 = wfA.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, busA, out var evtA8_3);
            bool okB8_3 = wfB.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, busB, out var evtB8_3);
            if (!okA8_3 || !okB8_3) throw new Exception("Step 8.3 waypoint failed");
            AssertEventsEqual(evtA8_3, evtB8_3, "Step 8.3");

            // Step 9: Reach assembly point
            // Path A: Physical AR Marker tap
            bool okA9 = wfA.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, busA, out var evtA9);
            // Path B: UI Button tap
            bool okB9 = wfB.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, busB, out var evtB9);

            if (!okA9 || !okB9) throw new Exception("Step 9 assembly failed");
            AssertEventsEqual(evtA9, evtB9, "Step 9");

            // Verify both produced identical 100% PASS scores
            if (Math.Abs(wfA.LatestAssessment.ClientScore - wfB.LatestAssessment.ClientScore) > 0.001f)
                throw new Exception("Assessment scores between AR marker and UI path differ");

            if (wfA.LatestAssessment.Passed != wfB.LatestAssessment.Passed || !wfA.LatestAssessment.Passed)
                throw new Exception("Both paths must be PASSED");
        }

        private static void AssertEventsEqual(TrainingEvent a, TrainingEvent b, string stepName)
        {
            if (a == null || b == null) throw new Exception($"{stepName}: Event was null");
            if (a.StepId != b.StepId) throw new Exception($"{stepName}: StepId mismatch ({a.StepId} vs {b.StepId})");
            if (a.EventType != b.EventType) throw new Exception($"{stepName}: EventType mismatch ({a.EventType} vs {b.EventType})");
            if (a.ActionId != b.ActionId) throw new Exception($"{stepName}: ActionId mismatch ({a.ActionId} vs {b.ActionId})");
            if (a.TargetId != b.TargetId) throw new Exception($"{stepName}: TargetId mismatch ({a.TargetId} vs {b.TargetId})");
            if (a.Outcome != b.Outcome) throw new Exception($"{stepName}: Outcome mismatch ({a.Outcome} vs {b.Outcome})");
        }

        public static void Test_DualInput_InvalidInteractionsDoNotAdvanceWorkflow()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            // 1. ReadyToPlace: cannot identify hazard or confirm
            if (wf.ConfirmHazardDetected(bus, out _))
                throw new Exception("Cannot confirm hazard detection before hazard is placed");

            wf.SetStage(FireWorkflowStage.HazardPlaced);

            // 2. HazardPlaced: cannot jump to identification or alarm
            if (wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _))
                throw new Exception("Cannot identify hazard before detection is acknowledged");

            // Confirm detection to advance to AwaitingIdentification
            wf.ConfirmHazardDetected(bus, out _);
            if (wf.CurrentStage != FireWorkflowStage.AwaitingIdentification)
                throw new Exception("Expected AwaitingIdentification");

            // 3. AwaitingIdentification: wrong hazard does not advance
            int countBefore = bus.DispatchedEvents.Count;
            bool badIdent = wf.SubmitHazardIdentification("hazard_chemical_spill", bus, out _);
            if (badIdent) throw new Exception("Wrong hazard identification must return false");
            if (wf.CurrentStage != FireWorkflowStage.AwaitingIdentification)
                throw new Exception("Wrong hazard must NOT advance workflow stage");
            if (bus.DispatchedEvents.Count != countBefore + 1)
                throw new Exception("Wrong hazard must emit penalty failure event");

            // Advance correctly
            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);

            // 4. AwaitingAlarm: invalid action does not advance
            if (wf.SubmitRaiseAlarm("invalid_scream", bus, out _))
                throw new Exception("Invalid alarm action must return false");
            if (wf.CurrentStage != FireWorkflowStage.AwaitingAlarm)
                throw new Exception("Invalid alarm action must NOT advance workflow stage");

            wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);

            // 5. AwaitingExtinguisherSelection: wrong extinguishers do not advance
            bool badExt1 = wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherWater, bus, out _);
            if (badExt1) throw new Exception("Water extinguisher must return false");
            if (wf.CurrentStage != FireWorkflowStage.AwaitingExtinguisherSelection)
                throw new Exception("Wrong extinguisher must NOT advance stage");

            bool badExt2 = wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherFoam, bus, out _);
            if (badExt2) throw new Exception("Foam extinguisher must return false");

            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);

            // 6. AwaitingSafeDistance: unsafe distance (< 2.0m) does not advance
            bool badDist = wf.SubmitDistanceDecision(1.2f, bus, out _);
            if (badDist) throw new Exception("Distance < 2.0m must return false");
            if (wf.CurrentStage != FireWorkflowStage.AwaitingSafeDistance)
                throw new Exception("Unsafe distance must NOT advance stage");

            wf.SubmitDistanceDecision(2.5f, bus, out _);

            // 7. SafeDistanceMaintained: out-of-order PASS action does not advance
            bool badPass = wf.SubmitExtinguisherAction("squeeze", bus, out _);
            if (badPass) throw new Exception("Squeezing before pulling pin must return false");
            if (wf.CurrentStage != FireWorkflowStage.SafeDistanceMaintained)
                throw new Exception("Out-of-order PASS action must NOT advance stage");

            wf.SubmitPullPin(bus, out _);
            wf.SubmitAim(bus, out _);
            wf.SubmitSqueeze(bus, out _);
            wf.SubmitSweep(bus, out _);

            // 8. AwaitingExitIdentification: wrong exit does not advance
            bool badExit1 = wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitFreightElevator, bus, out _);
            if (badExit1) throw new Exception("Elevator exit must return false");
            if (wf.CurrentStage != FireWorkflowStage.AwaitingExitIdentification)
                throw new Exception("Wrong exit must NOT advance stage");

            bool badExit2 = wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitBlockedCorridor, bus, out _);
            if (badExit2) throw new Exception("Blocked corridor exit must return false");

            wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);

            // 9. AwaitingEvacuationRoute: smoke corridor or out-of-order waypoint does not advance
            bool badWp1 = wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.HazardSmokeCorridor, bus, out _);
            if (badWp1) throw new Exception("Smoke corridor must return false");

            bool badWp2 = wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            if (badWp2) throw new Exception("Waypoint 2 before Waypoint 1 must return false");

            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);

            // 10. AwaitingAssemblyPoint: wrong assembly point does not advance
            bool badAssembly = wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyPointBeta, bus, out _);
            if (badAssembly) throw new Exception("Wrong assembly point must return false");
            if (wf.CurrentStage != FireWorkflowStage.AwaitingAssemblyPoint && wf.CurrentStage != FireWorkflowStage.RouteEvacuated)
                throw new Exception("Wrong assembly point must NOT complete training");
        }

        public static void Test_DualInput_DuplicateTapsPreventDuplicateEvents()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            wf.SetStage(FireWorkflowStage.HazardPlaced);

            // Step 1
            if (!wf.ConfirmHazardDetected(bus, out _)) throw new Exception("Step 1 initial confirm failed");
            int count1 = bus.DispatchedEvents.Count;
            if (wf.ConfirmHazardDetected(bus, out _)) throw new Exception("Duplicate Step 1 confirm must return false");
            if (bus.DispatchedEvents.Count != count1) throw new Exception("Duplicate Step 1 emitted extra event");

            // Step 2
            if (!wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _)) throw new Exception("Step 2 failed");
            int count2 = bus.DispatchedEvents.Count;
            if (wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _)) throw new Exception("Duplicate Step 2 must return false");
            if (bus.DispatchedEvents.Count != count2) throw new Exception("Duplicate Step 2 emitted extra event");

            // Step 3
            if (!wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _)) throw new Exception("Step 3 failed");
            int count3 = bus.DispatchedEvents.Count;
            if (wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _)) throw new Exception("Duplicate Step 3 must return false");
            if (bus.DispatchedEvents.Count != count3) throw new Exception("Duplicate Step 3 emitted extra event");

            // Step 4
            if (!wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _)) throw new Exception("Step 4 failed");
            int count4 = bus.DispatchedEvents.Count;
            if (wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _)) throw new Exception("Duplicate Step 4 must return false");
            if (bus.DispatchedEvents.Count != count4) throw new Exception("Duplicate Step 4 emitted extra event");

            // Step 5
            if (!wf.SubmitDistanceDecision(2.5f, bus, out _)) throw new Exception("Step 5 failed");
            int count5 = bus.DispatchedEvents.Count;
            if (wf.SubmitDistanceDecision(2.5f, bus, out _)) throw new Exception("Duplicate Step 5 must return false");
            if (bus.DispatchedEvents.Count != count5) throw new Exception("Duplicate Step 5 emitted extra event");

            // Step 6: PASS
            wf.SubmitPullPin(bus, out _);
            wf.SubmitAim(bus, out _);
            wf.SubmitSqueeze(bus, out _);
            wf.SubmitSweep(bus, out _);
            int count6 = bus.DispatchedEvents.Count;
            if (wf.SubmitPullPin(bus, out _)) throw new Exception("Duplicate pull pin after PASS completed must return false");
            if (wf.SubmitAim(bus, out _)) throw new Exception("Duplicate aim after PASS completed must return false");
            if (wf.SubmitSqueeze(bus, out _)) throw new Exception("Duplicate squeeze after PASS completed must return false");
            if (wf.SubmitSweep(bus, out _)) throw new Exception("Duplicate sweep after PASS completed must return false");
            if (bus.DispatchedEvents.Count != count6) throw new Exception("Duplicate PASS actions emitted extra events");

            // Step 7
            if (!wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _)) throw new Exception("Step 7 failed");
            int count7 = bus.DispatchedEvents.Count;
            if (wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _)) throw new Exception("Duplicate Step 7 must return false");
            if (bus.DispatchedEvents.Count != count7) throw new Exception("Duplicate Step 7 emitted extra event");

            // Step 8
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            int count8 = bus.DispatchedEvents.Count;
            if (wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _)) throw new Exception("Duplicate Step 8 must return false");
            if (wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _)) throw new Exception("Duplicate Step 8 must return false");
            if (bus.DispatchedEvents.Count != count8) throw new Exception("Duplicate Step 8 emitted extra event");

            // Step 9
            if (!wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _)) throw new Exception("Step 9 failed");
            int count9 = bus.DispatchedEvents.Count;
            if (wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _)) throw new Exception("Duplicate Step 9 must return false");
            if (bus.DispatchedEvents.Count != count9) throw new Exception("Duplicate Step 9 emitted extra event");

            // Exactly 14 events across 9 steps (Step 1: 1, Step 2: 1, Step 3: 1, Step 4: 1, Step 5: 1, Step 6: 4, Step 7: 1, Step 8: 3, Step 9: 1 = 14)
            if (bus.DispatchedEvents.Count != 14)
                throw new Exception($"Expected exactly 14 events, got {bus.DispatchedEvents.Count}");
        }

        public static void Test_DualInput_FullScenarioMixedARAndUI_Scores100AndPasses()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            // Step 1: AR Marker tap -> ConfirmHazardDetected
            wf.SetStage(FireWorkflowStage.HazardPlaced);
            if (!wf.ConfirmHazardDetected(bus, out _)) throw new Exception("Step 1 failed");

            // Step 2: UI button -> SubmitHazardIdentification
            if (!wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _)) throw new Exception("Step 2 failed");

            // Step 3: AR Marker tap -> SubmitRaiseAlarm
            if (!wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _)) throw new Exception("Step 3 failed");

            // Step 4: UI button -> SubmitSelectExtinguisher
            if (!wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _)) throw new Exception("Step 4 failed");

            // Step 5: AR ground tap -> SubmitDistanceDecision
            if (!wf.SubmitDistanceDecision(2.8f, bus, out _)) throw new Exception("Step 5 failed");

            // Step 6: Mixed PASS inputs
            // P - AR Marker tap
            if (!wf.SubmitPullPin(bus, out _)) throw new Exception("Step 6 P failed");
            // A - UI button
            if (!wf.SubmitAim(bus, out _)) throw new Exception("Step 6 A failed");
            // S - AR Marker tap
            if (!wf.SubmitSqueeze(bus, out _)) throw new Exception("Step 6 S failed");
            // S - UI button
            if (!wf.SubmitSweep(bus, out _)) throw new Exception("Step 6 S2 failed");

            // Step 7: AR Exit Marker tap
            if (!wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _)) throw new Exception("Step 7 failed");

            // Step 8: Mixed Waypoint inputs
            // WP1 - UI button
            if (!wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _)) throw new Exception("Step 8 WP1 failed");
            // WP2 - AR Marker tap
            if (!wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _)) throw new Exception("Step 8 WP2 failed");
            // WP3 - AR Marker tap
            if (!wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _)) throw new Exception("Step 8 WP3 failed");

            // Step 9: UI button
            if (!wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _)) throw new Exception("Step 9 failed");

            // Verification
            if (!wf.IsAssessmentCompleted) throw new Exception("Assessment should be completed");
            var assessment = wf.LatestAssessment;
            if (assessment == null) throw new Exception("AssessmentResult is null");
            if (Math.Abs(assessment.ClientScore - 100.0f) > 0.001f)
                throw new Exception($"Score was {assessment.ClientScore}, expected 100.00");
            if (!assessment.Passed) throw new Exception("Assessment must be PASSED");
            if (assessment.TotalPenalties != 0f) throw new Exception($"TotalPenalties was {assessment.TotalPenalties}, expected 0");

            var attempt = wf.LatestAttempt;
            if (attempt == null) throw new Exception("TrainingAttempt is null");
            if (attempt.Passed != true) throw new Exception("Attempt Passed was false");
            if (attempt.ClientScore != 100.0f) throw new Exception($"Attempt ClientScore was {attempt.ClientScore}");
        }

        public static void Test_DualInput_InvalidMarker_IncursPenaltyAndAllowsRecovery()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            // Step 1: Confirm hazard
            wf.SetStage(FireWorkflowStage.HazardPlaced);
            wf.ConfirmHazardDetected(bus, out _);

            // Step 2: Invalid marker tap (chemical spill -> penalty -5)
            wf.SubmitHazardIdentification("hazard_chemical_spill", bus, out _);
            // Recover: tap correct marker or UI button
            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);

            // Step 3: Alarm
            wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);

            // Step 4: Invalid marker tap (water extinguisher -> penalty -5)
            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherWater, bus, out _);
            // Recover: tap correct CO2 marker or UI button
            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);

            // Step 5: Safe distance
            wf.SubmitDistanceDecision(2.5f, bus, out _);

            // Step 6: PASS
            wf.SubmitPullPin(bus, out _);
            wf.SubmitAim(bus, out _);
            wf.SubmitSqueeze(bus, out _);
            wf.SubmitSweep(bus, out _);

            // Step 7: Exit
            wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);

            // Step 8: Route
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);

            // Step 9: Assembly Point
            wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            // Verification: 100 - 5 - 5 = 90.00 => PASS
            var assessment = wf.LatestAssessment;
            if (assessment == null) throw new Exception("Assessment was null");
            if (Math.Abs(assessment.ClientScore - 90.00f) > 0.001f)
                throw new Exception($"Score was {assessment.ClientScore}, expected 90.00");
            if (!assessment.Passed) throw new Exception("Assessment must be PASSED with 90.00");
            if (Math.Abs(assessment.TotalPenalties - 10.00f) > 0.001f)
                throw new Exception($"Total penalties was {assessment.TotalPenalties}, expected 10.00");
        }

        public static void Test_Workflow_FinalizeAttempt_CompletedAttempt_EmitsEventAndExposesAttempt()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            // Run complete 9-step scenario
            wf.SetStage(FireWorkflowStage.HazardPlaced);
            wf.ConfirmHazardDetected(bus, out _);
            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            wf.SubmitDistanceDecision(2.5f, bus, out _);
            wf.SubmitPullPin(bus, out _);
            wf.SubmitAim(bus, out _);
            wf.SubmitSqueeze(bus, out _);
            wf.SubmitSweep(bus, out _);
            wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            if (!wf.IsAssessmentCompleted)
                throw new Exception("Assessment should be completed after Step 9");
            if (wf.IsAttemptFinalizedForOutbox)
                throw new Exception("IsAttemptFinalizedForOutbox should be false before finalization");

            // Subscribe to domain hook
            TrainingAttempt hookAttempt = null;
            int hookCallCount = 0;
            wf.OnAttemptFinalizedForOutbox += att =>
            {
                hookAttempt = att;
                hookCallCount++;
            };

            // Finalize attempt
            bool finalized = wf.FinalizeAttemptForOutbox(bus, out var finalizedAttempt);

            if (!finalized)
                throw new Exception("FinalizeAttemptForOutbox should succeed for completed attempt");
            if (finalizedAttempt == null)
                throw new Exception("Finalized attempt out parameter must not be null");
            if (finalizedAttempt != wf.LatestAttempt)
                throw new Exception("Finalized attempt must match wf.LatestAttempt exactly");
            if (hookAttempt != wf.LatestAttempt)
                throw new Exception("OnAttemptFinalizedForOutbox hook must receive wf.LatestAttempt");
            if (hookCallCount != 1)
                throw new Exception($"Hook call count was {hookCallCount}, expected 1");
            if (!wf.IsAttemptFinalizedForOutbox)
                throw new Exception("IsAttemptFinalizedForOutbox should be true after finalization");
            if (finalizedAttempt.Status != TrainingAttempt.StatusCompleted)
                throw new Exception($"Attempt status was '{finalizedAttempt.Status}', expected '{TrainingAttempt.StatusCompleted}'");
            if (string.IsNullOrEmpty(finalizedAttempt.CompletedAt))
                throw new Exception("CompletedAt timestamp should be populated");

            // Verify event dispatched to bus
            TrainingEvent outboxEvent = null;
            foreach (var evt in bus.DispatchedEvents)
            {
                if (evt.EventType == FireTrainingWorkflow.EventTypeAttemptFinalized)
                {
                    outboxEvent = evt;
                    break;
                }
            }

            if (outboxEvent == null)
                throw new Exception($"No event with EventType '{FireTrainingWorkflow.EventTypeAttemptFinalized}' was dispatched");
            if (outboxEvent.TargetId != finalizedAttempt.ClientAttemptId)
                throw new Exception($"Event TargetId was '{outboxEvent.TargetId}', expected client_attempt_id '{finalizedAttempt.ClientAttemptId}'");
            if (outboxEvent.ActionId != FireTrainingWorkflow.ActionFinalizeSession)
                throw new Exception($"Event ActionId was '{outboxEvent.ActionId}', expected '{FireTrainingWorkflow.ActionFinalizeSession}'");
            if (outboxEvent.GetPayloadValue("client_attempt_id") != finalizedAttempt.ClientAttemptId)
                throw new Exception("Event payload missing or mismatched client_attempt_id");
            if (outboxEvent.GetPayloadValue("worker_id") != finalizedAttempt.WorkerId)
                throw new Exception("Event payload missing or mismatched worker_id");
            if (outboxEvent.GetPayloadValue("score") != "100.00")
                throw new Exception($"Event payload score was '{outboxEvent.GetPayloadValue("score")}', expected '100.00'");
            if (outboxEvent.GetPayloadValue("passed") != "true")
                throw new Exception($"Event payload passed was '{outboxEvent.GetPayloadValue("passed")}', expected 'true'");
            if (outboxEvent.GetPayloadValue("status") != TrainingAttempt.StatusCompleted)
                throw new Exception($"Event payload status was '{outboxEvent.GetPayloadValue("status")}', expected '{TrainingAttempt.StatusCompleted}'");
        }

        public static void Test_Workflow_FinalizeAttempt_PrematureCall_Rejected()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            int hookCount = 0;
            wf.OnAttemptFinalizedForOutbox += _ => hookCount++;

            // 1. Premature at NotStarted stage
            bool res1 = wf.FinalizeAttemptForOutbox(bus, out var att1);
            if (res1) throw new Exception("FinalizeAttemptForOutbox should fail when training is NotStarted");
            if (att1 != null) throw new Exception("Attempt out parameter should be null on premature rejection");
            if (wf.IsAttemptFinalizedForOutbox) throw new Exception("IsAttemptFinalizedForOutbox must remain false");
            if (hookCount != 0) throw new Exception("Hook must not be invoked on premature rejection");

            // 2. Premature at intermediate stage (Step 3: AlarmRaised)
            wf.SetStage(FireWorkflowStage.HazardPlaced);
            wf.ConfirmHazardDetected(bus, out _);
            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);

            int eventsBefore = bus.DispatchedEvents.Count;
            bool res2 = wf.FinalizeAttemptForOutbox(bus, out var att2);
            if (res2) throw new Exception("FinalizeAttemptForOutbox should fail when training is at Step 3");
            if (att2 != null) throw new Exception("Attempt out parameter should be null at Step 3");
            if (wf.IsAttemptFinalizedForOutbox) throw new Exception("IsAttemptFinalizedForOutbox must remain false");
            if (hookCount != 0) throw new Exception("Hook must not be invoked");
            if (bus.DispatchedEvents.Count != eventsBefore)
                throw new Exception("No event should be dispatched on premature rejection");
        }

        public static void Test_Workflow_FinalizeAttempt_DuplicateCall_RejectedAndZeroDuplicateEvents()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            // Run to completion
            wf.SetStage(FireWorkflowStage.HazardPlaced);
            wf.ConfirmHazardDetected(bus, out _);
            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            wf.SubmitDistanceDecision(2.5f, bus, out _);
            wf.SubmitPullPin(bus, out _);
            wf.SubmitAim(bus, out _);
            wf.SubmitSqueeze(bus, out _);
            wf.SubmitSweep(bus, out _);
            wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            int hookCalls = 0;
            wf.OnAttemptFinalizedForOutbox += _ => hookCalls++;

            // Call 1: First finalization succeeds
            bool first = wf.FinalizeAttemptForOutbox(bus, out var firstAttempt);
            if (!first) throw new Exception("First finalization must succeed");
            if (firstAttempt == null) throw new Exception("First attempt should not be null");
            if (hookCalls != 1) throw new Exception($"Hook calls after call 1 was {hookCalls}, expected 1");

            // Count outbox events in bus
            int outboxEventsCount = 0;
            foreach (var e in bus.DispatchedEvents)
            {
                if (e.EventType == FireTrainingWorkflow.EventTypeAttemptFinalized) outboxEventsCount++;
            }
            if (outboxEventsCount != 1)
                throw new Exception($"Expected 1 outbox event in bus, got {outboxEventsCount}");

            // Call 2: Duplicate finalization must be rejected
            bool second = wf.FinalizeAttemptForOutbox(bus, out var secondAttempt);
            if (second) throw new Exception("Duplicate finalization (call 2) must return false");
            if (secondAttempt != null) throw new Exception("Duplicate finalization must output null attempt");
            if (hookCalls != 1) throw new Exception("Hook must NOT be called on duplicate finalization");

            // Call 3: Repeated duplicate finalization must also be rejected
            bool third = wf.FinalizeAttemptForOutbox(bus, out var thirdAttempt);
            if (third) throw new Exception("Duplicate finalization (call 3) must return false");
            if (thirdAttempt != null) throw new Exception("Duplicate finalization must output null attempt");
            if (hookCalls != 1) throw new Exception("Hook must NOT be called on repeated duplicate finalization");

            // Re-verify no duplicate outbox events were emitted to bus
            int finalOutboxEventsCount = 0;
            foreach (var e in bus.DispatchedEvents)
            {
                if (e.EventType == FireTrainingWorkflow.EventTypeAttemptFinalized) finalOutboxEventsCount++;
            }
            if (finalOutboxEventsCount != 1)
                throw new Exception($"Duplicate finalization emitted duplicate events! Expected 1, found {finalOutboxEventsCount}");
        }

        public static void Test_Workflow_FinalizeAttempt_NullOrResetAttempt_RejectedSafely()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            // Run to completion and finalize
            wf.SetStage(FireWorkflowStage.HazardPlaced);
            wf.ConfirmHazardDetected(bus, out _);
            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            wf.SubmitDistanceDecision(2.5f, bus, out _);
            wf.SubmitPullPin(bus, out _);
            wf.SubmitAim(bus, out _);
            wf.SubmitSqueeze(bus, out _);
            wf.SubmitSweep(bus, out _);
            wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);
            wf.FinalizeAttemptForOutbox(bus, out _);

            if (!wf.IsAttemptFinalizedForOutbox)
                throw new Exception("Expected IsAttemptFinalizedForOutbox to be true");

            // Reset workflow for retake
            wf.Reset();

            if (wf.LatestAttempt != null)
                throw new Exception("LatestAttempt should be null after Reset()");
            if (wf.LatestAssessment != null)
                throw new Exception("LatestAssessment should be null after Reset()");
            if (wf.IsAssessmentCompleted)
                throw new Exception("IsAssessmentCompleted should be false after Reset()");
            if (wf.IsAttemptFinalizedForOutbox)
                throw new Exception("IsAttemptFinalizedForOutbox must be reset to false after Reset()");

            // FinalizeAttempt on null / reset attempt must be safely rejected
            bool res = wf.FinalizeAttemptForOutbox(bus, out var att);
            if (res) throw new Exception("FinalizeAttemptForOutbox should fail on reset / null attempt");
            if (att != null) throw new Exception("Attempt out parameter should be null on failed call");

            // Null dispatcher test
            bool resNullDispatcher = wf.FinalizeAttemptForOutbox(null, out var attNull);
            if (resNullDispatcher) throw new Exception("Should fail when attempt is null even with null dispatcher");
            if (attNull != null) throw new Exception("Attempt out parameter should be null");
        }

        public static void Test_Workflow_FinalizeAttempt_PreservesScoreAndOutcomeUnchanged()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            // Scenario with deductions: Step 2 wrong hazard (-5), Step 4 wrong extinguisher (-5) => 90.00, PASS
            wf.SetStage(FireWorkflowStage.HazardPlaced);
            wf.ConfirmHazardDetected(bus, out _);
            wf.SubmitHazardIdentification("hazard_chemical_spill", bus, out _);
            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherWater, bus, out _);
            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            wf.SubmitDistanceDecision(2.5f, bus, out _);
            wf.SubmitPullPin(bus, out _);
            wf.SubmitAim(bus, out _);
            wf.SubmitSqueeze(bus, out _);
            wf.SubmitSweep(bus, out _);
            wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            float initialAttemptScore = wf.LatestAttempt.ClientScore;
            bool initialAttemptPass = wf.LatestAttempt.Passed;
            string initialAttemptStatus = wf.LatestAttempt.Status;
            string initialAttemptId = wf.LatestAttempt.ClientAttemptId;

            float initialAssessmentScore = wf.LatestAssessment.ClientScore;
            bool initialAssessmentPass = wf.LatestAssessment.Passed;

            if (Math.Abs(initialAttemptScore - 90.00f) > 0.001f)
                throw new Exception($"Expected initial attempt score 90.00, got {initialAttemptScore}");
            if (!initialAttemptPass)
                throw new Exception("Expected initial attempt to pass with 90.00");

            // Finalize
            bool success = wf.FinalizeAttemptForOutbox(bus, out var finalized);
            if (!success) throw new Exception("Finalization should succeed");

            // Verify completed-attempt state, score, pass/fail outcome are completely unchanged
            if (Math.Abs(finalized.ClientScore - initialAttemptScore) > 0.0001f)
                throw new Exception($"Finalized attempt score changed from {initialAttemptScore} to {finalized.ClientScore}");
            if (finalized.Passed != initialAttemptPass)
                throw new Exception($"Finalized attempt passed changed from {initialAttemptPass} to {finalized.Passed}");
            if (finalized.Status != initialAttemptStatus)
                throw new Exception($"Finalized attempt status changed from {initialAttemptStatus} to {finalized.Status}");
            if (finalized.ClientAttemptId != initialAttemptId)
                throw new Exception($"Finalized attempt ID changed from {initialAttemptId} to {finalized.ClientAttemptId}");

            // Verify LatestAttempt and LatestAssessment on workflow remain unchanged
            if (Math.Abs(wf.LatestAttempt.ClientScore - initialAttemptScore) > 0.0001f)
                throw new Exception("wf.LatestAttempt score changed");
            if (wf.LatestAttempt.Passed != initialAttemptPass)
                throw new Exception("wf.LatestAttempt passed changed");
            if (Math.Abs(wf.LatestAssessment.ClientScore - initialAssessmentScore) > 0.0001f)
                throw new Exception("wf.LatestAssessment score changed");
            if (wf.LatestAssessment.Passed != initialAssessmentPass)
                throw new Exception("wf.LatestAssessment passed changed");
        }

        public static void Test_SummaryViewModel_SyncPreparedFlag_UpdatesOnFinalization()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            wf.SetStage(FireWorkflowStage.HazardPlaced);
            wf.ConfirmHazardDetected(bus, out _);
            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            wf.SubmitDistanceDecision(2.5f, bus, out _);
            wf.SubmitPullPin(bus, out _);
            wf.SubmitAim(bus, out _);
            wf.SubmitSqueeze(bus, out _);
            wf.SubmitSweep(bus, out _);
            wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            var vm = AssessmentSummaryViewModel.Build(wf.LatestAttempt, wf.LatestAssessment);

            if (vm.SyncPrepared)
                throw new Exception("SyncPrepared should be false before finalization");
            if (vm.ClientScore != 100.00f)
                throw new Exception($"Score was {vm.ClientScore}, expected 100.00");
            if (!vm.Passed)
                throw new Exception("Attempt should be marked Passed");

            // Finalize workflow attempt
            bool finalized = wf.FinalizeAttemptForOutbox(bus, out var attempt);
            if (!finalized) throw new Exception("Finalization failed");

            // Mark view model sync prepared
            vm.SyncPrepared = true;

            if (!vm.SyncPrepared)
                throw new Exception("SyncPrepared should be true after finalization");
            if (vm.ClientScore != 100.00f)
                throw new Exception("Score must remain 100.00 after finalization");
            if (!vm.Passed)
                throw new Exception("Passed status must remain true after finalization");
            if (vm.StepSummaries.Count != 9)
                throw new Exception($"Step summaries count changed to {vm.StepSummaries.Count}");
        }

        public static void Test_RuntimeE2E_CompleteNineStepJourney_AllStagesAndEvents()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            // Initial state
            if (wf.CurrentStage != FireWorkflowStage.NotStarted)
                throw new Exception($"Expected NotStarted, got {wf.CurrentStage}");
            if (wf.IsAssessmentCompleted)
                throw new Exception("Assessment must not be completed at start");

            // Step 1: Place Hazard & Confirm Detection
            wf.SetStage(FireWorkflowStage.HazardPlaced);
            if (!wf.ConfirmHazardDetected(bus, out var evt1)) throw new Exception("Step 1 failed");
            if (wf.CurrentStage != FireWorkflowStage.HazardDetected && wf.CurrentStage != FireWorkflowStage.AwaitingIdentification)
                throw new Exception($"Expected AwaitingIdentification, got {wf.CurrentStage}");
            if (evt1.EventType != "step_completed" || evt1.ActionId != FireTrainingWorkflow.ActionDetectHazard)
                throw new Exception($"Unexpected evt1: {evt1}");

            // Step 2: Identify Hazard (Electrical Fire)
            if (!wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out var evt2))
                throw new Exception("Step 2 failed");
            if (wf.CurrentStage != FireWorkflowStage.HazardIdentified && wf.CurrentStage != FireWorkflowStage.AwaitingAlarm)
                throw new Exception($"Expected AwaitingAlarm, got {wf.CurrentStage}");
            if (evt2.EventType != "hazard_identified" || evt2.ActionId != FireTrainingWorkflow.ActionIdentify)
                throw new Exception($"Unexpected evt2: {evt2}");

            // Step 3: Raise Alarm
            if (!wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out var evt3))
                throw new Exception("Step 3 failed");
            if (wf.CurrentStage != FireWorkflowStage.AlarmRaised && wf.CurrentStage != FireWorkflowStage.AwaitingExtinguisherSelection)
                throw new Exception($"Expected AlarmRaised, got {wf.CurrentStage}");
            if (evt3.EventType != "alarm_raised")
                throw new Exception($"Unexpected evt3: {evt3}");

            // Step 4: Select Extinguisher (CO2)
            if (!wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out var evt4))
                throw new Exception("Step 4 failed");
            if (wf.CurrentStage != FireWorkflowStage.ExtinguisherSelected && wf.CurrentStage != FireWorkflowStage.AwaitingSafeDistance)
                throw new Exception($"Expected AwaitingSafeDistance, got {wf.CurrentStage}");
            if (evt4.EventType != "extinguisher_selected")
                throw new Exception($"Unexpected evt4: {evt4}");

            // Step 5: Safe Distance (2.5m)
            if (!wf.SubmitDistanceDecision(2.5f, bus, out var evt5))
                throw new Exception("Step 5 failed");
            if (wf.CurrentStage != FireWorkflowStage.SafeDistanceMaintained)
                throw new Exception($"Expected SafeDistanceMaintained, got {wf.CurrentStage}");
            if (evt5.EventType != "decision_made")
                throw new Exception($"Unexpected evt5: {evt5}");

            // Step 6: PASS procedure
            if (!wf.SubmitPullPin(bus, out _)) throw new Exception("Step 6 P failed");
            if (wf.CurrentStage != FireWorkflowStage.PinPulled) throw new Exception("Expected PinPulled");
            if (!wf.SubmitAim(bus, out _)) throw new Exception("Step 6 A failed");
            if (wf.CurrentStage != FireWorkflowStage.AimConfirmed) throw new Exception("Expected AimConfirmed");
            if (!wf.SubmitSqueeze(bus, out _)) throw new Exception("Step 6 S failed");
            if (wf.CurrentStage != FireWorkflowStage.HandleSqueezed) throw new Exception("Expected HandleSqueezed");
            if (!wf.SubmitSweep(bus, out var evt6)) throw new Exception("Step 6 S2 failed");
            if (wf.CurrentStage != FireWorkflowStage.ExtinguisherDischarged && wf.CurrentStage != FireWorkflowStage.AwaitingExitIdentification)
                throw new Exception($"Expected AwaitingExitIdentification, got {wf.CurrentStage}");
            if (evt6.EventType != "procedure_completed") throw new Exception($"Unexpected evt6: {evt6}");

            // Step 7: Identify Emergency Exit (Sector B)
            if (!wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out var evt7))
                throw new Exception("Step 7 failed");
            if (wf.CurrentStage != FireWorkflowStage.ExitIdentified && wf.CurrentStage != FireWorkflowStage.AwaitingEvacuationRoute)
                throw new Exception($"Expected AwaitingEvacuationRoute, got {wf.CurrentStage}");
            if (evt7.EventType != "exit_marked") throw new Exception($"Unexpected evt7: {evt7}");

            // Step 8: Evacuation route sequence (WP1 -> WP2 -> WP3)
            if (!wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _))
                throw new Exception("Step 8 WP1 failed");
            if (!wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _))
                throw new Exception("Step 8 WP2 failed");
            if (!wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out var evt8))
                throw new Exception("Step 8 WP3 failed");
            if (wf.CurrentStage != FireWorkflowStage.RouteEvacuated && wf.CurrentStage != FireWorkflowStage.AwaitingAssemblyPoint)
                throw new Exception($"Expected AwaitingAssemblyPoint, got {wf.CurrentStage}");
            if (evt8.EventType != "evacuation_sequence_submitted") throw new Exception($"Unexpected evt8: {evt8}");

            // Step 9: Reach Assembly Point (Muster Point Alpha)
            if (!wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out var evt9))
                throw new Exception("Step 9 failed");
            if (wf.CurrentStage != FireWorkflowStage.AssemblyPointReached)
                throw new Exception($"Expected AssemblyPointReached, got {wf.CurrentStage}");
            if (evt9.EventType != "assembly_reached") throw new Exception($"Unexpected evt9: {evt9}");

            // Assessment validation
            if (!wf.IsAssessmentCompleted) throw new Exception("Assessment should be completed");
            var attempt = wf.LatestAttempt;
            if (attempt == null) throw new Exception("LatestAttempt was null");
            if (attempt.ClientScore != 100.00f) throw new Exception($"Score was {attempt.ClientScore}, expected 100.00");
            if (!attempt.Passed) throw new Exception("Attempt was not marked Passed");
            if (attempt.Status != TrainingAttempt.StatusCompleted) throw new Exception($"Status was {attempt.Status}");
            if (string.IsNullOrEmpty(attempt.ClientAttemptId)) throw new Exception("ClientAttemptId is missing");
            if (string.IsNullOrEmpty(attempt.StartedAt)) throw new Exception("StartedAt is missing");
            if (string.IsNullOrEmpty(attempt.CompletedAt)) throw new Exception("CompletedAt is missing");
        }

        public static void Test_RuntimeE2E_InvalidActionsStrictlyRejectedAtEveryStep()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            // Step 1: Detect hazard
            wf.SetStage(FireWorkflowStage.HazardPlaced);
            // Attempt Step 2 before Step 1
            if (wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _))
                throw new Exception("Step 2 should be rejected before Step 1");
            wf.ConfirmHazardDetected(bus, out _);

            // Step 2: Invalid hazard identification
            if (wf.SubmitHazardIdentification("hazard_chemical_spill", bus, out _))
                throw new Exception("Chemical spill should not return true for Electrical fire");
            if (wf.CurrentStage == FireWorkflowStage.AlarmRaised)
                throw new Exception("Invalid hazard must not advance stage to AlarmRaised");
            // Recover
            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);

            // Step 3: Alarm - attempt wrong action or step 4 prematurely
            if (wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _))
                throw new Exception("Step 4 should be rejected before Step 3 alarm");
            wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);

            // Step 4: Invalid extinguishers
            if (wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherWater, bus, out _))
                throw new Exception("Water extinguisher should not return true");
            if (wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherFoam, bus, out _))
                throw new Exception("Foam extinguisher should not return true");
            if (wf.CurrentStage == FireWorkflowStage.SafeDistanceMaintained)
                throw new Exception("Invalid extinguisher must not advance stage");
            // Recover
            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);

            // Step 5: Unsafe distance
            if (wf.SubmitDistanceDecision(0.5f, bus, out _))
                throw new Exception("Unsafe distance 0.5m must be rejected");
            if (wf.CurrentStage == FireWorkflowStage.SafeDistanceMaintained)
                throw new Exception("Unsafe distance must not advance stage");
            // Recover
            wf.SubmitDistanceDecision(2.5f, bus, out _);

            // Step 6: Out-of-order PASS actions
            if (wf.SubmitAim(bus, out _)) throw new Exception("Aim before Pin must be rejected");
            if (wf.SubmitSqueeze(bus, out _)) throw new Exception("Squeeze before Pin must be rejected");
            if (wf.SubmitSweep(bus, out _)) throw new Exception("Sweep before Pin must be rejected");
            wf.SubmitPullPin(bus, out _);

            if (wf.SubmitSqueeze(bus, out _)) throw new Exception("Squeeze before Aim must be rejected");
            wf.SubmitAim(bus, out _);

            if (wf.SubmitSweep(bus, out _)) throw new Exception("Sweep before Squeeze must be rejected");
            wf.SubmitSqueeze(bus, out _);
            wf.SubmitSweep(bus, out _);

            // Step 7: Invalid exits
            if (wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitFreightElevator, bus, out _))
                throw new Exception("Freight elevator exit must be rejected");
            if (wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitBlockedCorridor, bus, out _))
                throw new Exception("Blocked corridor exit must be rejected");
            if (wf.CurrentStage == FireWorkflowStage.RouteEvacuated)
                throw new Exception("Invalid exit must not advance to route evacuated");
            // Recover
            wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);

            // Step 8: Out of order waypoints & smoke corridor
            if (wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _))
                throw new Exception("WP2 before WP1 must be rejected");
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            if (wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _))
                throw new Exception("WP3 before WP2 must be rejected");
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);

            // Step 9: Wrong assembly point
            if (wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyPointBeta, bus, out _))
                throw new Exception("Muster point Beta must be rejected");
            if (wf.IsAssessmentCompleted)
                throw new Exception("Wrong assembly point must not complete assessment");
            // Recover
            wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);
            if (!wf.IsAssessmentCompleted)
                throw new Exception("Correct assembly point must complete assessment");
        }

        public static void Test_RuntimeE2E_AlreadyCompletedMarkersCannotBeRepeated()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            wf.SetStage(FireWorkflowStage.HazardPlaced);
            if (!wf.ConfirmHazardDetected(bus, out _)) throw new Exception("Step 1 failed");
            // Repeated detection call
            if (wf.ConfirmHazardDetected(bus, out _))
                throw new Exception("Repeated ConfirmHazardDetected must return false");

            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            wf.SubmitDistanceDecision(2.5f, bus, out _);
            wf.SubmitPullPin(bus, out _);
            wf.SubmitAim(bus, out _);
            wf.SubmitSqueeze(bus, out _);
            wf.SubmitSweep(bus, out _);

            // Procedure completed; repeated PASS actions must return false
            if (wf.SubmitPullPin(bus, out _)) throw new Exception("Repeated PullPin must return false");
            if (wf.SubmitAim(bus, out _)) throw new Exception("Repeated Aim must return false");
            if (wf.SubmitSqueeze(bus, out _)) throw new Exception("Repeated Squeeze must return false");
            if (wf.SubmitSweep(bus, out _)) throw new Exception("Repeated Sweep must return false");

            // Step 7: identify exit
            if (!wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _))
                throw new Exception("Step 7 failed");
            if (wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _))
                throw new Exception("Repeated SubmitIdentifyExit must return false");

            // Step 8: route
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);

            // Step 9: assembly point
            if (!wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _))
                throw new Exception("Step 9 failed");
            if (wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _))
                throw new Exception("Repeated SubmitReachAssemblyPoint must return false");
        }

        public static void Test_RuntimeE2E_OutboxFinalizationWorkflowAndHookContract()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            wf.SetStage(FireWorkflowStage.HazardPlaced);
            wf.ConfirmHazardDetected(bus, out _);
            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            wf.SubmitDistanceDecision(2.5f, bus, out _);
            wf.SubmitPullPin(bus, out _);
            wf.SubmitAim(bus, out _);
            wf.SubmitSqueeze(bus, out _);
            wf.SubmitSweep(bus, out _);
            wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            TrainingAttempt hookReceivedAttempt = null;
            int hookInvocations = 0;
            wf.OnAttemptFinalizedForOutbox += att =>
            {
                hookReceivedAttempt = att;
                hookInvocations++;
            };

            bool res = wf.FinalizeAttemptForOutbox(bus, out var finalized);
            if (!res) throw new Exception("FinalizeAttemptForOutbox failed");
            if (finalized == null) throw new Exception("Finalized attempt was null");
            if (hookReceivedAttempt != finalized) throw new Exception("Hook received different attempt instance");
            if (hookInvocations != 1) throw new Exception($"Hook invocations: {hookInvocations}, expected 1");
            if (!wf.IsAttemptFinalizedForOutbox) throw new Exception("IsAttemptFinalizedForOutbox should be true");

            // Event emitted check
            int outboxEventCount = 0;
            TrainingEvent lastOutboxEvent = null;
            foreach (var evt in bus.DispatchedEvents)
            {
                if (evt.EventType == FireTrainingWorkflow.EventTypeAttemptFinalized)
                {
                    outboxEventCount++;
                    lastOutboxEvent = evt;
                }
            }
            if (outboxEventCount != 1) throw new Exception($"Outbox event count was {outboxEventCount}, expected 1");
            if (lastOutboxEvent.TargetId != finalized.ClientAttemptId)
                throw new Exception("Event TargetId mismatched attempt ID");

            // Duplicate finalization attempts
            for (int i = 0; i < 3; i++)
            {
                bool dupRes = wf.FinalizeAttemptForOutbox(bus, out var dupAttempt);
                if (dupRes) throw new Exception($"Duplicate finalization call {i + 1} succeeded");
                if (dupAttempt != null) throw new Exception("Duplicate finalization returned non-null attempt");
            }

            if (hookInvocations != 1)
                throw new Exception($"Hook invocations after duplicate calls: {hookInvocations}, expected 1");
        }

        public static void Test_RuntimeE2E_AssessmentSummaryUI_DisplaysAndFinalizes()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            wf.SetStage(FireWorkflowStage.HazardPlaced);
            wf.ConfirmHazardDetected(bus, out _);
            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            wf.SubmitDistanceDecision(2.5f, bus, out _);
            wf.SubmitPullPin(bus, out _);
            wf.SubmitAim(bus, out _);
            wf.SubmitSqueeze(bus, out _);
            wf.SubmitSweep(bus, out _);
            wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            var vm = AssessmentSummaryViewModel.Build(wf.LatestAttempt, wf.LatestAssessment);

            if (vm.ScoreDisplayText != "100.00 / 100")
                throw new Exception($"ScoreDisplayText was '{vm.ScoreDisplayText}', expected '100.00 / 100'");
            if (vm.PassFailBadgeText != "PASS")
                throw new Exception($"PassFailBadgeText was '{vm.PassFailBadgeText}', expected 'PASS'");
            if (vm.PassFailColorHex != "#2ECC71")
                throw new Exception($"PassFailColorHex was '{vm.PassFailColorHex}', expected '#2ECC71'");
            if (vm.StepSummaries.Count != 9)
                throw new Exception($"Expected 9 step breakdown entries, got {vm.StepSummaries.Count}");
            if (vm.SyncPrepared)
                throw new Exception("SyncPrepared must be false before finalize session click");

            // Simulate UI Click "Finish / Prepare Sync"
            bool finalized = wf.FinalizeAttemptForOutbox(bus, out _);
            if (!finalized) throw new Exception("Finalize session failed");
            vm.SyncPrepared = true;

            if (!vm.SyncPrepared) throw new Exception("SyncPrepared should be true");
            if (vm.ScoreDisplayText != "100.00 / 100") throw new Exception("Score altered after sync preparation");
            if (vm.PassFailBadgeText != "PASS") throw new Exception("Pass/Fail altered after sync preparation");
        }

        public static void Test_RuntimeE2E_Retake_ClearsAllFinalizationState_FreshAttemptId()
        {
            var bus = new TrainingEventBus();
            bus.Clear();
            var wf = new FireTrainingWorkflow();

            // First run
            wf.SetStage(FireWorkflowStage.HazardPlaced);
            wf.ConfirmHazardDetected(bus, out _);
            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            wf.SubmitDistanceDecision(2.5f, bus, out _);
            wf.SubmitPullPin(bus, out _);
            wf.SubmitAim(bus, out _);
            wf.SubmitSqueeze(bus, out _);
            wf.SubmitSweep(bus, out _);
            wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            wf.FinalizeAttemptForOutbox(bus, out var attempt1);
            if (attempt1 == null) throw new Exception("Attempt 1 was null");
            string id1 = attempt1.ClientAttemptId;
            if (!wf.IsAttemptFinalizedForOutbox) throw new Exception("IsAttemptFinalizedForOutbox should be true");

            // Retake / Reset
            wf.Reset();
            bus.Clear();

            if (wf.IsAttemptFinalizedForOutbox)
                throw new Exception("IsAttemptFinalizedForOutbox must be false after Reset()");
            if (wf.LatestAttempt != null)
                throw new Exception("LatestAttempt must be null after Reset()");
            if (wf.LatestAssessment != null)
                throw new Exception("LatestAssessment must be null after Reset()");
            if (wf.IsAssessmentCompleted)
                throw new Exception("IsAssessmentCompleted must be false after Reset()");

            // Second run
            wf.SetStage(FireWorkflowStage.HazardPlaced);
            wf.ConfirmHazardDetected(bus, out _);
            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);
            wf.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm, bus, out _);
            wf.SubmitSelectExtinguisher(FireTrainingWorkflow.TargetExtinguisherCO2, bus, out _);
            wf.SubmitDistanceDecision(2.5f, bus, out _);
            wf.SubmitPullPin(bus, out _);
            wf.SubmitAim(bus, out _);
            wf.SubmitSqueeze(bus, out _);
            wf.SubmitSweep(bus, out _);
            wf.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, bus, out _);

            if (!wf.IsAssessmentCompleted) throw new Exception("Second attempt should be completed");
            string id2 = wf.LatestAttempt.ClientAttemptId;
            if (string.Equals(id1, id2, StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Retake attempt ID '{id2}' matches first attempt ID '{id1}'! IDs must be unique.");

            bool res2 = wf.FinalizeAttemptForOutbox(bus, out var attempt2);
            if (!res2) throw new Exception("Finalizing second attempt failed");
            if (attempt2 == null || attempt2.ClientAttemptId != id2)
                throw new Exception("Attempt 2 finalization mismatched second attempt ID");
            if (!wf.IsAttemptFinalizedForOutbox)
                throw new Exception("IsAttemptFinalizedForOutbox should be true for attempt 2");
        }

        public static void Test_ArFloatingLabel_CreationAndScale()
        {
            var parent = new GameObject("TestParent");
            try
            {
                var label = ArFloatingLabel.Create(
                    parent,
                    Vector3.zero,
                    "TEST LABEL",
                    Color.yellow,
                    width: 0.38f,
                    height: 0.11f,
                    fontSize: 0.80f);

                if (label == null) throw new Exception("ArFloatingLabel was not created");
                if (label.Text != "TEST LABEL") throw new Exception($"Text mismatch: {label.Text}");
                if (label.LabelMesh == null) throw new Exception("LabelMesh is null");
                if (label.TextColor != Color.yellow) throw new Exception("TextColor mismatch");

                label.SetText("UPDATED");
                if (label.Text != "UPDATED") throw new Exception("Updated text mismatch");

                label.SetColor(Color.green);
                if (label.TextColor != Color.green) throw new Exception("Updated color mismatch");
            }
            finally
            {
                GameObject.DestroyImmediate(parent);
            }
        }

        public static void Test_ArFloatingLabel_OrientationAlignsWithCamera()
        {
            var camObj = new GameObject("TestCamera");
            var cam = camObj.AddComponent<Camera>();
            camObj.transform.position = new Vector3(0f, 1f, -2f);
            camObj.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

            var parent = new GameObject("TestParent");
            try
            {
                var label = ArFloatingLabel.Create(
                    parent,
                    Vector3.zero,
                    "BILLBOARD TEST",
                    Color.white,
                    width: 0.38f,
                    height: 0.11f,
                    fontSize: 0.80f,
                    ArBillboardMode.ScreenAligned);

                label.SetTargetCamera(cam);
                label.UpdateOrientation();

                float angle = Quaternion.Angle(label.transform.rotation, cam.transform.rotation);
                if (angle > 0.01f)
                    throw new Exception($"ScreenAligned orientation failed. Angle difference: {angle}");

                label.BillboardMode = ArBillboardMode.LookAtCamera;
                label.UpdateOrientation();
                Vector3 toViewer = label.transform.position - cam.transform.position;
                Quaternion expected = Quaternion.LookRotation(toViewer, cam.transform.up);
                float lookAngle = Quaternion.Angle(label.transform.rotation, expected);
                if (lookAngle > 0.01f)
                    throw new Exception($"LookAtCamera orientation failed. Angle difference: {lookAngle}");
            }
            finally
            {
                GameObject.DestroyImmediate(parent);
                GameObject.DestroyImmediate(camObj);
            }
        }

        public static void Test_FireHazardMarker_VisualStructureAndLabels()
        {
            var markerObj = new GameObject("TestHazardMarker");
            try
            {
                var marker = markerObj.AddComponent<FireHazardMarker>();
                marker.EnsureVisuals();

                var label = markerObj.GetComponentInChildren<ArFloatingLabel>(true);
                if (label == null) throw new Exception("FireHazardMarker must have an ArFloatingLabel component");

                marker.AcknowledgeDetection();
                if (!marker.IsDetected) throw new Exception("Hazard not detected");

                marker.MarkIdentified("class_e_electrical");
                if (!marker.IsIdentified) throw new Exception("Hazard not identified");

                marker.TriggerAlarmVisual();
                if (!marker.IsAlarmActive) throw new Exception("Alarm not active");

                marker.MarkSafeDistanceConfirmed();
                if (!marker.IsSafeDistanceConfirmed) throw new Exception("Safe distance not confirmed");

                marker.TriggerExtinguisherDischargeVisual();
                if (!marker.IsExtinguished) throw new Exception("Extinguished visual not set");
            }
            finally
            {
                GameObject.DestroyImmediate(markerObj);
            }
        }

        public static void Test_AllFireMarkers_HaveArFloatingLabels()
        {
            var extObj = new GameObject("TestExtinguisher");
            var exitObj = new GameObject("TestExit");
            var routeObj = new GameObject("TestRoute");
            var assemblyObj = new GameObject("TestAssembly");
            try
            {
                var ext = extObj.AddComponent<ExtinguisherMarker>();
                ext.EnsureVisuals();
                if (extObj.GetComponentInChildren<ArFloatingLabel>(true) == null)
                    throw new Exception("ExtinguisherMarker must have an ArFloatingLabel");

                var exit = exitObj.AddComponent<EmergencyExitMarker>();
                exit.EnsureVisuals();
                if (exitObj.GetComponentInChildren<ArFloatingLabel>(true) == null)
                    throw new Exception("EmergencyExitMarker must have an ArFloatingLabel");

                var route = routeObj.AddComponent<EvacuationRouteMarker>();
                route.EnsureVisuals();
                if (routeObj.GetComponentInChildren<ArFloatingLabel>(true) == null)
                    throw new Exception("EvacuationRouteMarker must have an ArFloatingLabel");

                var assembly = assemblyObj.AddComponent<AssemblyPointMarker>();
                assembly.EnsureVisuals();
                if (assemblyObj.GetComponentInChildren<ArFloatingLabel>(true) == null)
                    throw new Exception("AssemblyPointMarker must have an ArFloatingLabel");
            }
            finally
            {
                GameObject.DestroyImmediate(extObj);
                GameObject.DestroyImmediate(exitObj);
                GameObject.DestroyImmediate(routeObj);
                GameObject.DestroyImmediate(assemblyObj);
            }
        }

        public static void Test_TouchGestureFilter_StationaryTapAccepted()
        {
            Vector2 start = new Vector2(500f, 500f);
            Vector2 end = new Vector2(505f, 503f); // small displacement (5.8px)
            float maxMovement = 6f;
            float duration = 0.15f;
            bool ok = TouchGestureFilter.EvaluateTapParameters(start, end, maxMovement, duration, startedOverUI: false, endedOverUI: false, maxMovementThreshold: 25f);
            if (!ok) throw new Exception("Stationary tap within threshold should be accepted");
        }

        public static void Test_TouchGestureFilter_SwipeRejected()
        {
            Vector2 start = new Vector2(200f, 500f);
            Vector2 end = new Vector2(600f, 500f); // large displacement (400px swipe)
            float maxMovement = 400f;
            float duration = 0.20f;
            bool ok = TouchGestureFilter.EvaluateTapParameters(start, end, maxMovement, duration, startedOverUI: false, endedOverUI: false, maxMovementThreshold: 25f);
            if (ok) throw new Exception("Horizontal swipe (400px) must be rejected as tap");

            // Also test swipe that loops back to start position
            bool okLoop = TouchGestureFilter.EvaluateTapParameters(start, start, maxDisplacementDuringGesture: 120f, durationSeconds: 0.25f, startedOverUI: false, endedOverUI: false, maxMovementThreshold: 25f);
            if (okLoop) throw new Exception("Gesture with large intermediate displacement must be rejected as tap");
        }

        public static void Test_TouchGestureFilter_HoldDragRejected()
        {
            Vector2 start = new Vector2(500f, 500f);
            Vector2 end = new Vector2(502f, 502f);
            float duration = 0.85f; // > 0.45s tap duration
            bool ok = TouchGestureFilter.EvaluateTapParameters(start, end, maxDisplacementDuringGesture: 3f, durationSeconds: duration, startedOverUI: false, endedOverUI: false, maxMovementThreshold: 25f);
            if (ok) throw new Exception("Touch exceeding maximum tap duration must be rejected");
        }

        public static void Test_TouchGestureFilter_UiStartOrEndRejected()
        {
            Vector2 pos = new Vector2(500f, 500f);
            bool okStartUI = TouchGestureFilter.EvaluateTapParameters(pos, pos, maxDisplacementDuringGesture: 0f, durationSeconds: 0.1f, startedOverUI: true, endedOverUI: false, maxMovementThreshold: 25f);
            if (okStartUI) throw new Exception("Touch started over UI must not be accepted as AR tap");

            bool okEndUI = TouchGestureFilter.EvaluateTapParameters(pos, pos, maxDisplacementDuringGesture: 0f, durationSeconds: 0.1f, startedOverUI: false, endedOverUI: true, maxMovementThreshold: 25f);
            if (okEndUI) throw new Exception("Touch ended over UI must not be accepted as AR tap");
        }

        public static void Test_TapGatedButton_SwipeDoesNotTrigger()
        {
            var btnObj = new GameObject("TestButton");
            try
            {
                var rect = btnObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200, 80);
                var btn = btnObj.AddComponent<UnityEngine.UI.Button>();
                var tapGated = btnObj.AddComponent<TapGatedButton>();

                bool triggered = false;
                tapGated.Initialize(() => triggered = true);

                // Simulate programmatic trigger
                tapGated.TriggerTap();
                if (!triggered) throw new Exception("TapGatedButton TriggerTap should invoke callback");

                // Reset and simulate a swipe via pointer events
                triggered = false;
                var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                {
                    position = new Vector2(100f, 100f)
                };

                tapGated.OnPointerDown(eventData);

                // Drag far away (swipe)
                eventData.position = new Vector2(500f, 100f);
                tapGated.OnDrag(eventData);

                // Release
                tapGated.OnPointerUp(eventData);

                if (triggered) throw new Exception("TapGatedButton must not trigger action after a swipe gesture");
            }
            finally
            {
                GameObject.DestroyImmediate(btnObj);
            }
        }

        public static void Test_FireArInteractionController_Process3DMarkerHit_ActionGating()
        {
            var ctrlObj = new GameObject("TestCtrl");
            var hazardObj = new GameObject("TestHazard");
            try
            {
                var ctrl = ctrlObj.AddComponent<FireArInteractionController>();
                var hazard = hazardObj.AddComponent<FireHazardMarker>();
                hazard.EnsureVisuals();
                ctrl.SetActiveHazard(hazard);

                // Step 1: Placed -> hitting hazard confirms detection
                ctrl.Workflow.SetStage(FireWorkflowStage.HazardPlaced);
                bool hitStep1 = ctrl.Process3DMarkerHit(hazardObj);
                if (!hitStep1) throw new Exception("Step 1 intentional tap on hazard should confirm detection");

                // Step 2: Identification -> hitting hazard should NOT auto-solve classification
                ctrl.Workflow.SetStage(FireWorkflowStage.HazardDetected);
                bool hitStep2 = ctrl.Process3DMarkerHit(hazardObj);
                if (hitStep2) throw new Exception("Step 2 touching hazard must not auto-classify hazard");
                if (ctrl.WorkflowStage != FireWorkflowStage.HazardDetected)
                    throw new Exception("Step 2 stage must not advance from touching hazard");

                // Step 3: Alarm -> hitting hazard should NOT raise alarm
                ctrl.Workflow.SetStage(FireWorkflowStage.HazardIdentified);
                bool hitStep3 = ctrl.Process3DMarkerHit(hazardObj);
                if (hitStep3) throw new Exception("Step 3 touching burning hazard must not raise alarm");
                if (ctrl.WorkflowStage != FireWorkflowStage.HazardIdentified)
                    throw new Exception("Step 3 stage must not advance from touching hazard");

                // Step 4: Extinguisher -> hitting hazard should NOT select CO2
                ctrl.Workflow.SetStage(FireWorkflowStage.AlarmRaised);
                bool hitStep4 = ctrl.Process3DMarkerHit(hazardObj);
                if (hitStep4) throw new Exception("Step 4 touching burning hazard must not select extinguisher");
                if (ctrl.WorkflowStage != FireWorkflowStage.AlarmRaised)
                    throw new Exception("Step 4 stage must not advance from touching hazard");
            }
            finally
            {
                GameObject.DestroyImmediate(hazardObj);
                GameObject.DestroyImmediate(ctrlObj);
            }
        }

        public static void Test_Nav_Step3Alarm_ShowsNextButton()
        {
            var nav = new GuidedStepNavigator();
            nav.SetViewStep(3);

            if (nav.CurrentStepIndex != 3)
                throw new Exception($"Expected CurrentStepIndex 3, got {nav.CurrentStepIndex}");
            if (nav.IsStepCompleted(3))
                throw new Exception("Step 3 must not be completed initially");
            if (nav.CanGoNext)
                throw new Exception("CanGoNext must be false before required alarm action");

            bool completed = nav.CompleteStep(3, "✓ Emergency Alarm Activated! Siren sounding.");
            if (!completed) throw new Exception("CompleteStep(3) failed");
            if (!nav.IsStepCompleted(3)) throw new Exception("IsStepCompleted(3) should be true");
            if (!nav.CanGoNext) throw new Exception("CanGoNext must be true after alarm action");
            if (nav.CurrentNextLabel != "NEXT: SELECT EXTINGUISHER →")
                throw new Exception($"Expected 'NEXT: SELECT EXTINGUISHER →', got '{nav.CurrentNextLabel}'");
            if (nav.GetSuccessFeedback(3) != "✓ Emergency Alarm Activated! Siren sounding.")
                throw new Exception("Success feedback mismatch");
        }

        public static void Test_Nav_NextButton_AdvancesExactlyOneStep()
        {
            var nav = new GuidedStepNavigator();
            nav.SetViewStep(3);
            nav.CompleteStep(3, "✓ Emergency Alarm Activated! Siren sounding.");

            bool advanced = nav.GoNext();
            if (!advanced) throw new Exception("GoNext() returned false when CanGoNext was true");
            if (nav.CurrentStepIndex != 4)
                throw new Exception($"Expected CurrentStepIndex 4, got {nav.CurrentStepIndex}");
            if (nav.CurrentStepTitle != "SELECT EXTINGUISHER")
                throw new Exception($"Expected 'SELECT EXTINGUISHER', got '{nav.CurrentStepTitle}'");
            if (nav.IsStepCompleted(4))
                throw new Exception("Step 4 must be incomplete upon advancing");
            if (nav.CanGoNext)
                throw new Exception("CanGoNext on Step 4 must be false before extinguisher action");
        }

        public static void Test_Nav_BackButton_ReturnsExactlyOneCompletedStep()
        {
            var nav = new GuidedStepNavigator();
            nav.CompleteStep(1);
            nav.GoNext();
            nav.CompleteStep(2);
            nav.GoNext();
            nav.CompleteStep(3);
            nav.GoNext();

            if (nav.CurrentStepIndex != 4) throw new Exception($"Expected Step 4, got {nav.CurrentStepIndex}");
            if (!nav.CanGoBack) throw new Exception("CanGoBack must be true on Step 4");

            bool wentBack = nav.GoBack();
            if (!wentBack) throw new Exception("GoBack() returned false");
            if (nav.CurrentStepIndex != 3) throw new Exception($"Expected Step 3 after back, got {nav.CurrentStepIndex}");
            if (!nav.IsStepCompleted(3)) throw new Exception("Step 3 must remain completed");
            if (!nav.CanGoNext) throw new Exception("CanGoNext must remain true on completed Step 3");

            nav.GoNext();
            if (nav.CurrentStepIndex != 4) throw new Exception($"Expected Step 4 after next, got {nav.CurrentStepIndex}");
        }

        public static void Test_Nav_SwipeDoesNotAdvanceWorkflow()
        {
            var nav = new GuidedStepNavigator();
            nav.SetViewStep(3);

            // 1. Verify TouchGestureFilter rejects swipe gesture
            bool isTap = TouchGestureFilter.EvaluateTapParameters(
                new Vector2(100f, 100f),
                new Vector2(350f, 100f), // 250px swipe
                maxDisplacementDuringGesture: 250f,
                durationSeconds: 0.18f,
                startedOverUI: false,
                endedOverUI: false);

            if (isTap) throw new Exception("Swipe gesture must NOT be registered as an intentional tap");

            var go = new GameObject("TestBtn");
            try
            {
                var tapGated = go.AddComponent<TapGatedButton>();
                bool clicked = false;
                tapGated.Initialize(() => clicked = true);

                var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                {
                    position = new Vector2(100f, 100f),
                    pressPosition = new Vector2(100f, 100f)
                };

                tapGated.OnPointerDown(eventData);
                eventData.position = new Vector2(300f, 100f);
                tapGated.OnDrag(eventData);
                tapGated.OnPointerUp(eventData);

                if (clicked) throw new Exception("TapGatedButton must NOT trigger click on swipe/drag");
                if (nav.CurrentStepIndex != 3) throw new Exception("Step must remain unchanged during swipe");
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        public static void Test_Nav_StepLocking_NextUnavailableBeforeRequiredAction()
        {
            var nav = new GuidedStepNavigator();

            for (int step = 1; step <= 9; step++)
            {
                if (nav.CurrentStepIndex != step)
                    throw new Exception($"Expected step {step}, got {nav.CurrentStepIndex}");

                if (nav.CanGoNext)
                    throw new Exception($"CanGoNext must be false on step {step} before required action");

                bool advanced = nav.GoNext();
                if (advanced)
                    throw new Exception($"GoNext() must fail on step {step} when incomplete");

                if (nav.CurrentStepIndex != step)
                    throw new Exception($"Step advanced illegally from {step} to {nav.CurrentStepIndex}");

                nav.CompleteStep(step);
                if (!nav.CanGoNext)
                    throw new Exception($"CanGoNext must be true on step {step} after required action");

                if (step < 9)
                {
                    nav.GoNext();
                }
            }
        }

        public static void Test_Nav_IncorrectActionDoesNotAdvance()
        {
            var ctrlObj = new GameObject("TestCtrl");
            try
            {
                var ctrl = ctrlObj.AddComponent<FireArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);

                ctrl.Workflow.SetStage(FireWorkflowStage.AwaitingIdentification);
                ctrl.StepNavigator.SetViewStep(2);

                bool wrongId = ctrl.SubmitHazardIdentification("hazard_combustible_debris");
                if (wrongId) throw new Exception("Class A should be rejected");
                if (ctrl.StepNavigator.IsStepCompleted(2))
                    throw new Exception("Step 2 must NOT be marked completed on incorrect action");
                if (ctrl.StepNavigator.CanGoNext)
                    throw new Exception("Next button must be locked on incorrect action");
                if (ctrl.StepNavigator.CurrentStepIndex != 2)
                    throw new Exception("Worker must remain on Step 2");

                ctrl.Workflow.SetStage(FireWorkflowStage.AwaitingExtinguisherSelection);
                ctrl.StepNavigator.SetViewStep(4);

                bool wrongExt = ctrl.SubmitExtinguisherSelection(FireTrainingWorkflow.TargetExtinguisherWater);
                if (wrongExt) throw new Exception("Water extinguisher should be rejected");
                if (ctrl.StepNavigator.IsStepCompleted(4))
                    throw new Exception("Step 4 must NOT be marked completed on incorrect extinguisher");
                if (ctrl.StepNavigator.CanGoNext)
                    throw new Exception("Next button must be locked on incorrect extinguisher");
                if (ctrl.StepNavigator.CurrentStepIndex != 4)
                    throw new Exception("Worker must remain on Step 4");
            }
            finally
            {
                GameObject.DestroyImmediate(ctrlObj);
            }
        }

        public static void Test_Nav_CorrectActionCompletesStep()
        {
            var ctrlObj = new GameObject("TestCtrl");
            try
            {
                var ctrl = ctrlObj.AddComponent<FireArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);

                ctrl.Workflow.SetStage(FireWorkflowStage.AwaitingAlarm);
                ctrl.StepNavigator.SetViewStep(3);

                bool alarmOk = ctrl.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm);
                if (!alarmOk) throw new Exception("Alarm action failed");
                if (!ctrl.StepNavigator.IsStepCompleted(3))
                    throw new Exception("Step 3 must be marked complete on successful alarm");
                if (!ctrl.StepNavigator.CanGoNext)
                    throw new Exception("Next button must become available on successful alarm");
                if (ctrl.StepNavigator.CurrentNextLabel != "NEXT: SELECT EXTINGUISHER →")
                    throw new Exception($"Expected destination NEXT: SELECT EXTINGUISHER →, got {ctrl.StepNavigator.CurrentNextLabel}");
            }
            finally
            {
                GameObject.DestroyImmediate(ctrlObj);
            }
        }

        public static void Test_Nav_FullNineStepSequentialFlow()
        {
            var ctrlObj = new GameObject("TestCtrl");
            var hazardObj = new GameObject("HazardMarker");
            try
            {
                var ctrl = ctrlObj.AddComponent<FireArInteractionController>();
                var hazard = hazardObj.AddComponent<FireHazardMarker>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                ctrl.SetActiveHazard(hazard);

                ctrl.Workflow.SetStage(FireWorkflowStage.HazardPlaced);
                if (!ctrl.ConfirmHazardDetected()) throw new Exception("Step 1 failed");
                if (!ctrl.StepNavigator.IsStepCompleted(1)) throw new Exception("Step 1 incomplete");
                ctrl.StepNavigator.GoNext();

                if (ctrl.StepNavigator.CurrentStepIndex != 2) throw new Exception("Expected Step 2");
                if (!ctrl.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire)) throw new Exception("Step 2 failed");
                if (!ctrl.StepNavigator.IsStepCompleted(2)) throw new Exception("Step 2 incomplete");
                ctrl.StepNavigator.GoNext();

                if (ctrl.StepNavigator.CurrentStepIndex != 3) throw new Exception("Expected Step 3");
                if (!ctrl.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm)) throw new Exception("Step 3 failed");
                if (!ctrl.StepNavigator.IsStepCompleted(3)) throw new Exception("Step 3 incomplete");
                ctrl.StepNavigator.GoNext();

                if (ctrl.StepNavigator.CurrentStepIndex != 4) throw new Exception("Expected Step 4");
                if (!ctrl.SubmitExtinguisherSelection(FireTrainingWorkflow.TargetExtinguisherCO2)) throw new Exception("Step 4 failed");
                if (!ctrl.StepNavigator.IsStepCompleted(4)) throw new Exception("Step 4 incomplete");
                ctrl.StepNavigator.GoNext();

                if (ctrl.StepNavigator.CurrentStepIndex != 5) throw new Exception("Expected Step 5");
                if (!ctrl.SubmitDistanceDecision(2.5f)) throw new Exception("Step 5 failed");
                if (!ctrl.StepNavigator.IsStepCompleted(5)) throw new Exception("Step 5 incomplete");
                ctrl.StepNavigator.GoNext();

                if (ctrl.StepNavigator.CurrentStepIndex != 6) throw new Exception("Expected Step 6");
                ctrl.SubmitPullPin();
                ctrl.SubmitAim();
                ctrl.SubmitSqueeze();
                ctrl.SubmitSweep();
                if (!ctrl.StepNavigator.IsStepCompleted(6)) throw new Exception("Step 6 incomplete");
                ctrl.StepNavigator.GoNext();

                if (ctrl.StepNavigator.CurrentStepIndex != 7) throw new Exception("Expected Step 7");
                if (!ctrl.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB)) throw new Exception("Step 7 failed");
                if (!ctrl.StepNavigator.IsStepCompleted(7)) throw new Exception("Step 7 incomplete");
                ctrl.StepNavigator.GoNext();

                if (ctrl.StepNavigator.CurrentStepIndex != 8) throw new Exception("Expected Step 8");
                ctrl.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor);
                ctrl.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut);
                ctrl.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit);
                if (!ctrl.StepNavigator.IsStepCompleted(8)) throw new Exception("Step 8 incomplete");
                ctrl.StepNavigator.GoNext();

                if (ctrl.StepNavigator.CurrentStepIndex != 9) throw new Exception("Expected Step 9");
                if (!ctrl.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint)) throw new Exception("Step 9 failed");
                if (!ctrl.StepNavigator.IsStepCompleted(9)) throw new Exception("Step 9 incomplete");
                if (ctrl.StepNavigator.CurrentNextLabel != "VIEW ASSESSMENT →")
                    throw new Exception($"Expected 'VIEW ASSESSMENT →', got {ctrl.StepNavigator.CurrentNextLabel}");

                bool completedTrainingTriggered = false;
                ctrl.StepNavigator.OnCompleteTrainingRequested += () => completedTrainingTriggered = true;
                ctrl.StepNavigator.GoNext();

                if (!completedTrainingTriggered)
                    throw new Exception("Final step GoNext() must trigger OnCompleteTrainingRequested");
            }
            finally
            {
                GameObject.DestroyImmediate(hazardObj);
                GameObject.DestroyImmediate(ctrlObj);
            }
        }

        public static void Test_Nav_RetakeStartsFreshAttempt()
        {
            var ctrlObj = new GameObject("TestCtrl");
            var hazardObj = new GameObject("HazardMarker");
            try
            {
                var ctrl = ctrlObj.AddComponent<FireArInteractionController>();
                var hazard = hazardObj.AddComponent<FireHazardMarker>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                ctrl.SetActiveHazard(hazard);

                ctrl.Workflow.SetStage(FireWorkflowStage.HazardPlaced);
                if (!ctrl.ConfirmHazardDetected()) throw new Exception("ConfirmHazardDetected failed");
                ctrl.StepNavigator.GoNext();
                if (!ctrl.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire)) throw new Exception("Hazard identification failed");
                ctrl.StepNavigator.GoNext();

                if (ctrl.StepNavigator.CurrentStepIndex != 3)
                    throw new Exception("Setup failed");

                ctrl.RetakeTraining();

                if (ctrl.StepNavigator.CurrentStepIndex != 1)
                    throw new Exception($"Expected reset to step 1, got {ctrl.StepNavigator.CurrentStepIndex}");
                if (ctrl.StepNavigator.HighestCompletedStep != 0)
                    throw new Exception("HighestCompletedStep must be reset to 0");
                for (int i = 1; i <= 9; i++)
                {
                    if (ctrl.StepNavigator.IsStepCompleted(i))
                        throw new Exception($"Step {i} must be reset to incomplete");
                }
                if (ctrl.StepNavigator.CanGoBack)
                    throw new Exception("CanGoBack must be false after retake");
                if (ctrl.StepNavigator.CanGoNext)
                    throw new Exception("CanGoNext must be false after retake");
            }
            finally
            {
                GameObject.DestroyImmediate(hazardObj);
                GameObject.DestroyImmediate(ctrlObj);
            }
        }

        public static void Test_Nav_Idempotency_GoingBackDoesNotDuplicateScoringEvents()
        {
            var ctrlObj = new GameObject("TestCtrl");
            var hazardObj = new GameObject("HazardMarker");
            try
            {
                var ctrl = ctrlObj.AddComponent<FireArInteractionController>();
                var hazard = hazardObj.AddComponent<FireHazardMarker>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);
                ctrl.SetActiveHazard(hazard);

                ctrl.Workflow.SetStage(FireWorkflowStage.HazardPlaced);
                ctrl.ConfirmHazardDetected();
                ctrl.StepNavigator.GoNext();

                ctrl.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire);
                ctrl.StepNavigator.GoNext();

                ctrl.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm);
                ctrl.StepNavigator.GoNext();

                int eventCountBefore = bus.DispatchedEvents.Count;

                ctrl.StepNavigator.GoBack();
                ctrl.StepNavigator.GoBack();
                ctrl.StepNavigator.GoBack();

                if (ctrl.StepNavigator.CurrentStepIndex != 1)
                    throw new Exception($"Expected step 1 after going back, got {ctrl.StepNavigator.CurrentStepIndex}");

                ctrl.StepNavigator.GoNext();
                ctrl.StepNavigator.GoNext();
                ctrl.StepNavigator.GoNext();

                if (ctrl.StepNavigator.CurrentStepIndex != 4)
                    throw new Exception($"Expected step 4 after returning, got {ctrl.StepNavigator.CurrentStepIndex}");

                int eventCountAfter = bus.DispatchedEvents.Count;

                if (eventCountBefore != eventCountAfter)
                {
                    throw new Exception($"Event count changed from {eventCountBefore} to {eventCountAfter}. Back/Next navigation must NOT duplicate events!");
                }
            }
            finally
            {
                GameObject.DestroyImmediate(hazardObj);
                GameObject.DestroyImmediate(ctrlObj);
            }
        }

        // ====================================================================
        // STEP 8B Tests: Audio, Navigation, Interactions, Visuals, Assessment
        // ====================================================================

        public static void Test_FireAudioService_GeneratesSynthesizedClipsWithoutAssetFiles()
        {
            var audio = FireAudioService.Instance;
            if (audio == null) throw new Exception("FireAudioService.Instance is null");

            audio.PlayHazardDetected();
            audio.PlayCorrectAction();
            audio.PlayStepCompleted();

            if (audio.GetPlayCount(FireSoundType.HazardDetected) < 1)
                throw new Exception("PlayCount for HazardDetected should be >= 1");
            if (audio.GetPlayCount(FireSoundType.CorrectAction) < 1)
                throw new Exception("PlayCount for CorrectAction should be >= 1");
            if (audio.GetPlayCount(FireSoundType.StepCompleted) < 1)
                throw new Exception("PlayCount for StepCompleted should be >= 1");
        }

        public static void Test_FireAudioService_RespectsMuteAndVolumeSettings()
        {
            var audio = FireAudioService.Instance;
            if (audio == null) throw new Exception("FireAudioService.Instance is null");

            bool prevSound = audio.IsSoundEnabled;
            float prevVol = audio.EffectsVolume;

            try
            {
                audio.IsSoundEnabled = false;
                if (audio.IsSoundEnabled) throw new Exception("IsSoundEnabled should be false");

                audio.EffectsVolume = 0.42f;
                if (Mathf.Abs(audio.EffectsVolume - 0.42f) > 0.05f)
                    throw new Exception($"EffectsVolume should be ~0.42f, got {audio.EffectsVolume}");

                int prefVal = PlayerPrefs.GetInt("FireAudio_SoundEnabled", -1);
                if (prefVal != 0) throw new Exception($"PlayerPrefs FireAudio_SoundEnabled should be 0, got {prefVal}");
            }
            finally
            {
                audio.IsSoundEnabled = prevSound;
                audio.EffectsVolume = prevVol;
            }
        }

        public static void Test_FireAudioService_EmergencyAlarmAudioTriggered()
        {
            var audio = FireAudioService.Instance;
            if (audio == null) throw new Exception("FireAudioService.Instance is null");

            int beforeCount = audio.GetPlayCount(FireSoundType.EmergencyAlarm);
            audio.PlayEmergencyAlarm();
            int afterCount = audio.GetPlayCount(FireSoundType.EmergencyAlarm);

            if (afterCount != beforeCount + 1)
                throw new Exception($"EmergencyAlarm play count should increment by 1, was {beforeCount} -> {afterCount}");
        }

        public static void Test_EvacuationWorkflow_StrictSequentialProgression()
        {
            var bus = new TrainingEventBus();
            var wf = new FireTrainingWorkflow();
            wf.SetStage(FireWorkflowStage.ExitIdentified);

            bool s1 = wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            if (!s1 || wf.CurrentStage != FireWorkflowStage.WaypointMainCorridorReached)
                throw new Exception("Waypoint 1 should advance stage to WaypointMainCorridorReached");

            bool s3Fail = wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            if (s3Fail) throw new Exception("Waypoint 3 must fail before Waypoint 2 is reached");

            bool s2 = wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut, bus, out _);
            if (!s2 || wf.CurrentStage != FireWorkflowStage.WaypointBypassCrosscutReached)
                throw new Exception("Waypoint 2 should advance stage to WaypointBypassCrosscutReached");

            bool s3 = wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out _);
            if (!s3 || wf.CurrentStage != FireWorkflowStage.RouteEvacuated)
                throw new Exception("Waypoint 3 should advance stage to RouteEvacuated");
        }

        public static void Test_EvacuationWorkflow_DirectToExitFailsGracefully()
        {
            var bus = new TrainingEventBus();
            var wf = new FireTrainingWorkflow();
            wf.SetStage(FireWorkflowStage.ExitIdentified);

            bool s = wf.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit, bus, out var evt);
            if (s) throw new Exception("Direct jump to fire door exit must be rejected");
            if (wf.CurrentStage != FireWorkflowStage.ExitIdentified)
                throw new Exception("Workflow stage should remain ExitIdentified");
            if (evt == null || evt.Outcome != "failure")
                throw new Exception("Failure event should be recorded");

            bool sAssembly = wf.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint, FireTrainingWorkflow.ActionCompleteStep, bus, out _);
            if (sAssembly) throw new Exception("Jumping to assembly point before evacuation route completes must be rejected");
        }

        public static void Test_PassInteraction_TapButtonsTriggerCorrectSubActions()
        {
            var bus = new TrainingEventBus();
            var wf = new FireTrainingWorkflow();
            wf.SetStage(FireWorkflowStage.SafeDistanceMaintained);

            bool sPull = wf.SubmitExtinguisherAction(FireTrainingWorkflow.ActionPullPin, bus, out var ePull);
            if (!sPull || wf.CurrentStage != FireWorkflowStage.PinPulled) throw new Exception("Pull pin failed");
            if (ePull.ActionId != FireTrainingWorkflow.ActionPullPin) throw new Exception("Pull pin ActionId mismatch");

            bool sAim = wf.SubmitExtinguisherAction(FireTrainingWorkflow.ActionAim, bus, out var eAim);
            if (!sAim || wf.CurrentStage != FireWorkflowStage.AimConfirmed) throw new Exception("Aim failed");
            if (eAim.ActionId != FireTrainingWorkflow.ActionAim) throw new Exception("Aim ActionId mismatch");

            bool sSqueeze = wf.SubmitExtinguisherAction(FireTrainingWorkflow.ActionSqueeze, bus, out var eSqueeze);
            if (!sSqueeze || wf.CurrentStage != FireWorkflowStage.HandleSqueezed) throw new Exception("Squeeze failed");
            if (eSqueeze.ActionId != FireTrainingWorkflow.ActionSqueeze) throw new Exception("Squeeze ActionId mismatch");

            bool sSweep = wf.SubmitExtinguisherAction(FireTrainingWorkflow.ActionSweep, bus, out var eSweep);
            if (!sSweep || wf.CurrentStage != FireWorkflowStage.ExtinguisherDischarged) throw new Exception("Sweep failed");
            if (eSweep.ActionId != FireTrainingWorkflow.ActionSweep) throw new Exception("Sweep ActionId mismatch");
        }

        public static void Test_PassInteraction_InvalidGestureDoesNotProgress()
        {
            bool isSwipeTap = TouchGestureFilter.EvaluateTapParameters(
                new Vector2(100f, 100f),
                new Vector2(450f, 100f),
                350f,
                0.08f,
                false,
                false);

            if (isSwipeTap) throw new Exception("Swipe gesture must be strictly rejected as intentional tap");

            bool isRealTap = TouchGestureFilter.EvaluateTapParameters(
                new Vector2(100f, 100f),
                new Vector2(102f, 101f),
                2.2f,
                0.12f,
                false,
                false);

            if (!isRealTap) throw new Exception("Stationary tap within threshold must be accepted");

            var ctrlObj = new GameObject("TestCtrlSwipe");
            try
            {
                var ctrl = ctrlObj.AddComponent<FireArInteractionController>();
                ctrl.Workflow.SetStage(FireWorkflowStage.SafeDistanceMaintained);
                if (ctrl.Workflow.CurrentStage != FireWorkflowStage.SafeDistanceMaintained)
                    throw new Exception("Workflow should remain in SafeDistanceMaintained");
            }
            finally
            {
                GameObject.DestroyImmediate(ctrlObj);
            }
        }

        public static void Test_GuidedStepNavigator_StepActionTextsMatchInteraction()
        {
            LocaleService.Instance.SetLanguage(LocaleService.LangEnglish);
            var nav = new GuidedStepNavigator();
            for (int i = 1; i <= 9; i++)
            {
                string instruction = nav.GetStepInstruction(i);
                if (string.IsNullOrEmpty(instruction)) throw new Exception($"Step {i} instruction is empty");

                switch (i)
                {
                    case 1:
                        if (!instruction.Contains("ACKNOWLEDGE FIRE HAZARD"))
                            throw new Exception("Step 1 instruction must contain ACKNOWLEDGE FIRE HAZARD");
                        break;
                    case 2:
                        if (!instruction.Contains("CLASS E: ELECTRICAL FIRE"))
                            throw new Exception("Step 2 instruction must contain CLASS E: ELECTRICAL FIRE");
                        break;
                    case 3:
                        if (!instruction.Contains("ACTIVATE MANUAL CALL POINT"))
                            throw new Exception("Step 3 instruction must contain ACTIVATE MANUAL CALL POINT");
                        break;
                    case 4:
                        if (!instruction.Contains("CO2 EXTINGUISHER"))
                            throw new Exception("Step 4 instruction must contain CO2 EXTINGUISHER");
                        break;
                    case 5:
                        if (!instruction.Contains("STAND AT SAFE DISTANCE"))
                            throw new Exception("Step 5 instruction must contain STAND AT SAFE DISTANCE");
                        break;
                    case 6:
                        if (!instruction.Contains("PULL SAFETY PIN"))
                            throw new Exception("Step 6 instruction must contain PULL SAFETY PIN");
                        break;
                    case 7:
                        if (!instruction.Contains("SECTOR B EMERGENCY EXIT"))
                            throw new Exception("Step 7 instruction must contain SECTOR B EMERGENCY EXIT");
                        break;
                    case 8:
                        if (!instruction.Contains("WAYPOINT 1: MAIN CORRIDOR"))
                            throw new Exception("Step 8 instruction must contain WAYPOINT 1: MAIN CORRIDOR");
                        break;
                    case 9:
                        if (!instruction.Contains("REACH ASSEMBLY POINT"))
                            throw new Exception("Step 9 instruction must contain REACH ASSEMBLY POINT");
                        break;
                }
            }
        }

        public static void Test_FeedbackUI_SinglePrimaryActionButtonEnforced()
        {
            var root = new GameObject("TestSingleActionEnforced");
            try
            {
                var ctrl = root.AddComponent<FireArInteractionController>();
                var ui = root.AddComponent<FireInteractionFeedbackUI>();
                ui.Controller = ctrl;

                var hazardObj = new GameObject("HazardObj");
                var hazard = hazardObj.AddComponent<FireHazardMarker>();
                ctrl.SetActiveHazard(hazard);
                ctrl.Workflow.SetStage(FireWorkflowStage.HazardPlaced);

                ui.RefreshUI();

                if (ui.IsNextButtonVisible)
                    throw new Exception("Primary Next button must NOT be visible before action completion");

                ctrl.ConfirmHazardDetection(hazard);
                ui.RefreshUI();

                if (!ui.IsNextButtonVisible)
                    throw new Exception("Primary Next button MUST be visible after action completion");

                GameObject.DestroyImmediate(hazardObj);
            }
            finally
            {
                GameObject.DestroyImmediate(root);
            }
        }

        public static void Test_FeedbackUI_SoundSettingsTogglePersists()
        {
            var root = new GameObject("TestSoundToggle");
            try
            {
                var ui = root.AddComponent<FireInteractionFeedbackUI>();
                var audio = FireAudioService.Instance;

                bool initial = audio.IsSoundEnabled;
                ui.ToggleSoundEnabled();
                bool toggled = audio.IsSoundEnabled;
                if (toggled == initial) throw new Exception("Sound toggle should invert IsSoundEnabled");

                int pref = PlayerPrefs.GetInt("FireAudio_SoundEnabled", -1);
                if (pref != (toggled ? 1 : 0)) throw new Exception("PlayerPrefs should persist toggled sound state");

                ui.ToggleSoundEnabled();
                if (audio.IsSoundEnabled != initial) throw new Exception("Second toggle should restore sound state");
            }
            finally
            {
                GameObject.DestroyImmediate(root);
            }
        }

        public static void Test_AssessmentDeductionExplanation_HumanReadableFormat()
        {
            var bus = new TrainingEventBus();
            var wf = new FireTrainingWorkflow();
            wf.SetStage(FireWorkflowStage.HazardPlaced);
            wf.ConfirmHazardDetected(bus, out _);
            wf.SubmitHazardIdentification("hazard_chemical_spill", bus, out _);
            wf.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire, bus, out _);

            var rubric = RubricDefinition.CreateFireExplosionRubric();
            var attempt = new TrainingAttempt
            {
                ClientAttemptId = Guid.NewGuid().ToString(),
                ModuleId = FireTrainingWorkflow.ModuleId,
                StartedAt = DateTime.UtcNow.ToString("o"),
                CompletedAt = DateTime.UtcNow.ToString("o")
            };

            var assessment = LocalAssessmentEngine.Evaluate(bus.DispatchedEvents, rubric);
            if (assessment == null) throw new Exception("Assessment result was null");
            if (assessment.TotalPenalties <= 0f) throw new Exception("Expected penalty deduction");

            var vm = AssessmentSummaryViewModel.Build(attempt, assessment);
            if (vm.Penalties.Count == 0) throw new Exception("Expected at least one penalty entry in ViewModel");

            string penaltyStr = vm.Penalties[0];
            if (string.IsNullOrEmpty(penaltyStr)) throw new Exception("Penalty Explanation cannot be empty");
            if (!penaltyStr.Contains("(-") || !penaltyStr.Contains("pts)"))
                throw new Exception($"Penalty Explanation must include deduction points: {penaltyStr}");
        }

        public static void Test_ArVisual_BillboardLabelsOrientationTowardsCamera()
        {
            var camObj = new GameObject("TestCam");
            var labelObj = new GameObject("TestFloatingLabel");
            try
            {
                var cam = camObj.AddComponent<Camera>();
                camObj.transform.position = new Vector3(0, 1.5f, -3f);
                camObj.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1f));

                var label = labelObj.AddComponent<ArFloatingLabel>();
                labelObj.transform.position = new Vector3(0, 1f, 0);
                label.SetTargetCamera(cam);
                label.SetText("TEST BILLBOARD");

                label.SendMessage("UpdateOrientation", SendMessageOptions.DontRequireReceiver);

                Vector3 toCam = (cam.transform.position - labelObj.transform.position).normalized;
                float dot = Mathf.Abs(Vector3.Dot(labelObj.transform.forward, toCam));
                if (dot < 0.7f) throw new Exception($"Label should face camera, alignment dot is {dot}");
            }
            finally
            {
                GameObject.DestroyImmediate(camObj);
                GameObject.DestroyImmediate(labelObj);
            }
        }

        public static void Test_ArVisual_LabelScalesAreReadableAndNonOverlapping()
        {
            var hazardObj = new GameObject("TestHazardMarkerVisuals");
            try
            {
                var hazard = hazardObj.AddComponent<FireHazardMarker>();
                var label = hazard.FloatingLabel;
                if (label != null)
                {
                    Vector3 s = label.transform.localScale;
                    if (s.x > 0.05f || s.y > 0.05f)
                        throw new Exception($"Floating label scale too large ({s}), world-space text should be compact");
                }
            }
            finally
            {
                GameObject.DestroyImmediate(hazardObj);
            }
        }

        public static void Test_ProceduralFire_ClassEIndicatorActive()
        {
            var hazardObj = new GameObject("TestHazardClassE");
            try
            {
                var hazard = hazardObj.AddComponent<FireHazardMarker>();
                hazard.EnsureVisuals();
                if (hazard.HazardClass != FireTrainingWorkflow.HazardClassElectrical)
                    throw new Exception($"Expected hazard class electrical, got {hazard.HazardClass}");

                var tmps = hazardObj.GetComponentsInChildren<TextMeshPro>(true);
                bool hasElectricalIndication = false;
                foreach (var tmp in tmps)
                {
                    if (tmp.text.Contains("480V") || tmp.text.Contains("ELECTRICAL") || tmp.text.Contains("CLASS E"))
                    {
                        hasElectricalIndication = true;
                        break;
                    }
                }
                if (!hasElectricalIndication)
                    throw new Exception("Procedural fire hazard cabinet must display electrical hazard indication (480V / Class E)");
            }
            finally
            {
                GameObject.DestroyImmediate(hazardObj);
            }
        }

        public static void Test_AssessmentModal_OnlyOpensViaExplicitTap()
        {
            var root = new GameObject("TestModalExplicitTap");
            try
            {
                var ctrl = root.AddComponent<FireArInteractionController>();
                var ui = root.AddComponent<FireInteractionFeedbackUI>();
                var summary = root.AddComponent<FireAssessmentSummaryUI>();
                ui.Controller = ctrl;
                summary.Controller = ctrl;

                for (int i = 1; i <= 8; i++)
                {
                    ctrl.StepNavigator.CompleteStep(i);
                    ctrl.StepNavigator.GoNext();
                }

                ctrl.Workflow.SetStage(FireWorkflowStage.AwaitingAssemblyPoint);
                ctrl.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint);

                if (summary.IsSummaryVisible)
                    throw new Exception("Summary modal must NOT open automatically before worker explicitly taps View Assessment");

                ui.OnNextButtonClicked();

                if (!summary.IsSummaryVisible)
                    throw new Exception("Summary modal MUST be visible after tapping View Assessment");
            }
            finally
            {
                GameObject.DestroyImmediate(root);
            }
        }

        public static void Test_FullScenario_CompletesStep1Through9_PassScore()
        {
            var root = new GameObject("TestFullScenarioPass");
            try
            {
                var ctrl = root.AddComponent<FireArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);

                var hazardObj = new GameObject("HazardObj");
                var hazard = hazardObj.AddComponent<FireHazardMarker>();
                ctrl.SetActiveHazard(hazard);

                // Step 1: Detect
                ctrl.Workflow.SetStage(FireWorkflowStage.HazardPlaced);
                bool c1 = ctrl.ConfirmHazardDetection(hazard);
                if (!c1) throw new Exception("Step 1 failed");
                ctrl.StepNavigator.GoNext();

                // Step 2: Identify
                bool c2 = ctrl.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire);
                if (!c2) throw new Exception("Step 2 failed");
                ctrl.StepNavigator.GoNext();

                // Step 3: Alarm
                bool c3 = ctrl.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm);
                if (!c3) throw new Exception("Step 3 failed");
                ctrl.StepNavigator.GoNext();

                // Step 4: Extinguisher
                bool c4 = ctrl.SubmitExtinguisherSelection(FireTrainingWorkflow.TargetExtinguisherCO2);
                if (!c4) throw new Exception("Step 4 failed");
                ctrl.StepNavigator.GoNext();

                // Step 5: Distance
                bool c5 = ctrl.SubmitDistanceDecision(2.5f);
                if (!c5) throw new Exception("Step 5 failed");
                ctrl.StepNavigator.GoNext();

                // Step 6: PASS
                bool c6a = ctrl.SubmitPullPin();
                bool c6b = ctrl.SubmitAim();
                bool c6c = ctrl.SubmitSqueeze();
                bool c6d = ctrl.SubmitSweep();
                if (!c6a || !c6b || !c6c || !c6d) throw new Exception("Step 6 PASS failed");
                ctrl.StepNavigator.GoNext();

                // Step 7: Exit
                bool c7 = ctrl.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB);
                if (!c7) throw new Exception("Step 7 failed");
                ctrl.StepNavigator.GoNext();

                // Step 8: Evacuation Waypoints
                bool c8a = ctrl.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor);
                bool c8b = ctrl.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut);
                bool c8c = ctrl.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit);
                if (!c8a || !c8b || !c8c) throw new Exception("Step 8 waypoints failed");
                ctrl.StepNavigator.GoNext();

                // Step 9: Reach Assembly Point
                bool c9 = ctrl.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint);
                if (!c9) throw new Exception("Step 9 failed");

                if (ctrl.LatestAssessment == null)
                    throw new Exception("Assessment should be completed");
                if (ctrl.LatestAssessment.ClientScore < 80f)
                    throw new Exception($"Score should be >= 80, got {ctrl.LatestAssessment.ClientScore}");
                if (!ctrl.LatestAssessment.Passed)
                    throw new Exception("Assessment should be passed");

                GameObject.DestroyImmediate(hazardObj);
            }
            finally
            {
                GameObject.DestroyImmediate(root);
            }
        }

        public static void Test_FullScenario_UnsafeActionsResultInClearDeductions()
        {
            var root = new GameObject("TestFullScenarioDeductions");
            try
            {
                var ctrl = root.AddComponent<FireArInteractionController>();
                var bus = new TrainingEventBus();
                ctrl.SetEventDispatcher(bus);

                var hazardObj = new GameObject("HazardObj");
                var hazard = hazardObj.AddComponent<FireHazardMarker>();
                ctrl.SetActiveHazard(hazard);

                // Step 1: Detect
                ctrl.Workflow.SetStage(FireWorkflowStage.HazardPlaced);
                ctrl.ConfirmHazardDetection(hazard);
                ctrl.StepNavigator.GoNext();

                // Step 2: Identification with initial wrong attempt
                ctrl.SubmitHazardIdentification("hazard_combustible_debris");
                ctrl.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire);
                ctrl.StepNavigator.GoNext();

                // Step 3: Alarm
                ctrl.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm);
                ctrl.StepNavigator.GoNext();

                // Step 4: Extinguisher with wrong attempt
                ctrl.SubmitExtinguisherSelection(FireTrainingWorkflow.TargetExtinguisherWater);
                ctrl.SubmitExtinguisherSelection(FireTrainingWorkflow.TargetExtinguisherCO2);
                ctrl.StepNavigator.GoNext();

                // Step 5: Distance
                ctrl.SubmitDistanceDecision(2.5f);
                ctrl.StepNavigator.GoNext();

                // Step 6: PASS
                ctrl.SubmitPullPin();
                ctrl.SubmitAim();
                ctrl.SubmitSqueeze();
                ctrl.SubmitSweep();
                ctrl.StepNavigator.GoNext();

                // Step 7: Exit
                ctrl.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB);
                ctrl.StepNavigator.GoNext();

                // Step 8: Evacuation with unsafe smoke corridor attempt
                ctrl.SubmitEvacuationWaypoint(FireTrainingWorkflow.HazardSmokeCorridor);
                ctrl.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor);
                ctrl.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut);
                ctrl.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit);
                ctrl.StepNavigator.GoNext();

                // Step 9: Reach Assembly Point
                ctrl.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint);

                if (ctrl.LatestAssessment == null)
                    throw new Exception("Assessment must be completed");
                if (ctrl.LatestAssessment.TotalPenalties <= 0f)
                    throw new Exception("Expected penalties for unsafe actions");
                if (ctrl.LatestAssessment.ClientScore >= 100f)
                    throw new Exception($"Score should be deducted below 100, got {ctrl.LatestAssessment.ClientScore}");

                var vm = AssessmentSummaryViewModel.Build(ctrl.LatestAttempt, ctrl.LatestAssessment);
                if (vm.Penalties.Count == 0)
                    throw new Exception("Expected penalty explanation entries in ViewModel");

                foreach (var penalty in vm.Penalties)
                {
                    if (string.IsNullOrEmpty(penalty) || !penalty.Contains("(-") || !penalty.Contains("pts)"))
                        throw new Exception($"Penalty record must have clear deduction format: '{penalty}'");
                }

                GameObject.DestroyImmediate(hazardObj);
            }
            finally
            {
                GameObject.DestroyImmediate(root);
            }
        }

        // =========================================================================
        // COMMON WORKER APP FOUNDATION TESTS
        // =========================================================================

        public static void Test_WorkerHome_LoadsWithAppTitleAndProfile()
        {
            var go = new GameObject("TestHomeShell");
            try
            {
                var ctrl = go.AddComponent<WorkerHomeController>();
                if (!ctrl.IsHomeVisible) throw new Exception("Home screen must be visible on startup");
                if (ctrl.IsSettingsVisible) throw new Exception("Settings panel must be hidden on startup");

                if (string.IsNullOrEmpty(ctrl.WorkerName)) throw new Exception("Worker name must be initialized");
                if (string.IsNullOrEmpty(ctrl.WorkerId)) throw new Exception("Worker ID must be initialized");
                if (ctrl.WorkerId != FireTrainingWorkflow.DefaultOfflineWorkerId)
                    throw new Exception($"Expected DefaultOfflineWorkerId, got {ctrl.WorkerId}");
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        public static void Test_WorkerHome_FireModuleAvailable_GasModuleComingSoon()
        {
            var go = new GameObject("TestModuleCards");
            try
            {
                var ctrl = go.AddComponent<WorkerHomeController>();
                if (!ctrl.IsFireModuleAvailable) throw new Exception("Fire & Explosion module must be marked Available");
                if (ctrl.IsGasModuleEnabled) throw new Exception("Gas & Confined Space module must be disabled / Coming Soon");
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        public static void Test_WorkerHome_SettingsPanelOpensAndCloses()
        {
            var go = new GameObject("TestSettingsFlow");
            try
            {
                var ctrl = go.AddComponent<WorkerHomeController>();
                ctrl.OpenSettings();
                if (!ctrl.IsSettingsVisible) throw new Exception("Settings panel must be visible after OpenSettings()");
                if (ctrl.CurrentState != WorkerHomeController.WorkerAppScreenState.Settings)
                    throw new Exception("State must be Settings");

                ctrl.CloseSettings();
                if (ctrl.IsSettingsVisible) throw new Exception("Settings panel must be hidden after CloseSettings()");
                if (ctrl.CurrentState != WorkerHomeController.WorkerAppScreenState.Home)
                    throw new Exception("State must be Home");
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        public static void Test_WorkerHome_SoundSettingsPersistViaAudioService()
        {
            var audio = FireAudioService.Instance;
            bool origSound = audio.IsSoundEnabled;
            float origVol = audio.EffectsVolume;
            bool origAlarm = audio.IsEmergencyAlarmEnabled;

            try
            {
                audio.IsSoundEnabled = false;
                if (audio.IsSoundEnabled) throw new Exception("IsSoundEnabled should be false");
                if (PlayerPrefs.GetInt("FireAudio_SoundEnabled") != 0)
                    throw new Exception("PlayerPrefs for sound enabled not saved");

                audio.EffectsVolume = 0.45f;
                if (Mathf.Abs(audio.EffectsVolume - 0.45f) > 0.01f)
                    throw new Exception("EffectsVolume should be 0.45");

                audio.IsEmergencyAlarmEnabled = false;
                if (audio.IsEmergencyAlarmEnabled) throw new Exception("IsEmergencyAlarmEnabled should be false");

                audio.IsSoundEnabled = true;
                audio.IsEmergencyAlarmEnabled = true;
            }
            finally
            {
                audio.IsSoundEnabled = origSound;
                audio.EffectsVolume = origVol;
                audio.IsEmergencyAlarmEnabled = origAlarm;
            }
        }

        public static void Test_WorkerHome_LanguageSelectionPersistsLocales()
        {
            var loc = LocaleService.Instance;
            string origLang = loc.CurrentLanguage;

            try
            {
                loc.SetLanguage(LocaleService.LangHindi);
                if (loc.CurrentLanguage != LocaleService.LangHindi)
                    throw new Exception("Expected current language to be Hindi (hi)");
                if (PlayerPrefs.GetString(LocaleService.PrefLanguageKey) != LocaleService.LangHindi)
                    throw new Exception("PlayerPrefs for language should be 'hi'");

                string hiTitle = loc.Get("app_title");
                if (!hiTitle.Contains("सुरक्षा"))
                    throw new Exception($"Expected Hindi title, got '{hiTitle}'");

                loc.SetLanguage(LocaleService.LangSantali);
                if (loc.CurrentLanguage != LocaleService.LangSantali)
                    throw new Exception("Expected current language to be Santali (sat)");
                string satTitle = loc.Get("app_title");
                if (!satTitle.Contains("ᱥᱮᱯᱷᱴᱤ"))
                    throw new Exception($"Expected Santali title with Ol Chiki characters, got '{satTitle}'");

                loc.SetLanguage(LocaleService.LangEnglish);
                if (loc.CurrentLanguage != LocaleService.LangEnglish)
                    throw new Exception("Expected current language to be English (en)");
            }
            finally
            {
                loc.SetLanguage(origLang);
            }
        }

        public static void Test_WorkerHome_OfflineIndicatorReflectsNetworkStatus()
        {
            var go = new GameObject("TestOfflineBadge");
            try
            {
                var ctrl = go.AddComponent<WorkerHomeController>();
                ctrl.UpdateOfflineStatus();
                if (!ctrl.IsHomeVisible) throw new Exception("Home screen should remain visible");
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        public static void Test_WorkerHome_StartTrainingActivatesFireModule()
        {
            var homeObj = new GameObject("TestHome");
            var fireMgrObj = new GameObject("TestFireMgr");
            try
            {
                var homeCtrl = homeObj.AddComponent<WorkerHomeController>();
                var fireCtrl = fireMgrObj.AddComponent<FireArInteractionController>();
                var fireUI = fireMgrObj.AddComponent<FireInteractionFeedbackUI>();
                fireUI.Controller = fireCtrl;

                homeCtrl.StartFireTraining();

                if (homeCtrl.CurrentState != WorkerHomeController.WorkerAppScreenState.TrainingFire)
                    throw new Exception("State should transition to TrainingFire");
                if (homeCtrl.IsHomeVisible)
                    throw new Exception("Home screen should be hidden when training starts");
            }
            finally
            {
                GameObject.DestroyImmediate(homeObj);
                GameObject.DestroyImmediate(fireMgrObj);
            }
        }

        public static void Test_WorkerHome_BackNavigationReturnsToHome()
        {
            var homeObj = new GameObject("TestHomeNav");
            var fireMgrObj = new GameObject("TestFireNav");
            try
            {
                var homeCtrl = homeObj.AddComponent<WorkerHomeController>();
                var fireCtrl = fireMgrObj.AddComponent<FireArInteractionController>();
                var fireUI = fireMgrObj.AddComponent<FireInteractionFeedbackUI>();
                fireUI.Controller = fireCtrl;

                homeCtrl.StartFireTraining();
                if (homeCtrl.IsHomeVisible) throw new Exception("Home should be hidden");

                // Returning to home
                homeCtrl.ReturnToHome();
                if (homeCtrl.CurrentState != WorkerHomeController.WorkerAppScreenState.Home)
                    throw new Exception("State should be Home after ReturnToHome");
                if (!homeCtrl.IsHomeVisible)
                    throw new Exception("Home screen must be visible after return");
            }
            finally
            {
                GameObject.DestroyImmediate(homeObj);
                GameObject.DestroyImmediate(fireMgrObj);
            }
        }

        public static void Test_WorkerHome_VisibleInHomeState()
        {
            var go = new GameObject("TestHomeVis");
            try
            {
                var ctrl = go.AddComponent<WorkerHomeController>();
                ctrl.ShowHome();
                if (ctrl.CurrentState != WorkerHomeController.WorkerAppScreenState.Home)
                    throw new Exception($"Expected state Home, got {ctrl.CurrentState}");
                if (!ctrl.IsHomeVisible)
                    throw new Exception("HomeScreen must be visible in Home state");
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        public static void Test_WorkerHome_StartTrainingHidesHomeUI()
        {
            var go = new GameObject("TestHomeStart");
            try
            {
                var ctrl = go.AddComponent<WorkerHomeController>();
                ctrl.ShowHome();
                ctrl.StartFireTraining();
                if (ctrl.CurrentState != WorkerHomeController.WorkerAppScreenState.TrainingFire)
                    throw new Exception($"Expected state TrainingFire, got {ctrl.CurrentState}");
                if (ctrl.IsHomeVisible)
                    throw new Exception("HomeScreen must be inactive/hidden when training starts");
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        public static void Test_FireTraining_IsOnlyActiveTrainingUI()
        {
            var homeObj = new GameObject("TestHomeShell");
            var fireObj = new GameObject("FireTrainingFeedbackObj");
            try
            {
                var homeCtrl = homeObj.AddComponent<WorkerHomeController>();
                var fireUI = fireObj.AddComponent<FireInteractionFeedbackUI>();
                homeCtrl.ShowHome();
                if (fireUI.Canvas != null && fireUI.Canvas.gameObject.activeSelf)
                    throw new Exception("FireTrainingCanvas must NOT be active while on Home screen");

                homeCtrl.StartFireTraining();
                if (homeCtrl.IsHomeVisible)
                    throw new Exception("Worker Home UI must be inactive during Fire Training");
                if (fireUI.Canvas == null || !fireUI.Canvas.gameObject.activeSelf)
                    throw new Exception("FireTrainingCanvas must be active during Fire Training");
            }
            finally
            {
                GameObject.DestroyImmediate(homeObj);
                GameObject.DestroyImmediate(fireObj);
            }
        }

        public static void Test_WorkerHome_InputDisabledDuringTraining()
        {
            var homeObj = new GameObject("TestHomeInput");
            try
            {
                var homeCtrl = homeObj.AddComponent<WorkerHomeController>();
                homeCtrl.ShowHome();
                homeCtrl.StartFireTraining();
                if (homeCtrl.FireStartButton == null)
                    throw new Exception("FireStartButton not found");
                if (homeCtrl.FireStartButton.gameObject.activeInHierarchy)
                    throw new Exception("Home buttons must be inactive in hierarchy during training");
            }
            finally
            {
                GameObject.DestroyImmediate(homeObj);
            }
        }

        public static void Test_FireTraining_ExitRestoresHomeUI()
        {
            var homeObj = new GameObject("TestHomeExit");
            var fireObj = new GameObject("TestFireExit");
            try
            {
                var homeCtrl = homeObj.AddComponent<WorkerHomeController>();
                var fireUI = fireObj.AddComponent<FireInteractionFeedbackUI>();
                homeCtrl.ShowHome();
                homeCtrl.StartFireTraining();

                fireUI.ExitTrainingToHome();
                if (homeCtrl.CurrentState != WorkerHomeController.WorkerAppScreenState.Home)
                    throw new Exception($"Expected state Home, got {homeCtrl.CurrentState}");
                if (!homeCtrl.IsHomeVisible)
                    throw new Exception("HomeScreen must be restored after exit");
                if (fireUI.Canvas != null && fireUI.Canvas.gameObject.activeSelf)
                    throw new Exception("FireTrainingCanvas must be hidden after exit");
            }
            finally
            {
                GameObject.DestroyImmediate(homeObj);
                GameObject.DestroyImmediate(fireObj);
            }
        }

        public static void Test_WorkerHome_SettingsButtonOpensSettings()
        {
            var homeObj = new GameObject("TestHomeSettings");
            try
            {
                var homeCtrl = homeObj.AddComponent<WorkerHomeController>();
                homeCtrl.ShowHome();
                if (homeCtrl.HomeSettingsButton == null)
                    throw new Exception("HomeSettingsButton must exist on Home screen");

                homeCtrl.OpenSettings();
                if (homeCtrl.CurrentState != WorkerHomeController.WorkerAppScreenState.Settings)
                    throw new Exception($"Expected state Settings, got {homeCtrl.CurrentState}");
                if (!homeCtrl.IsSettingsVisible)
                    throw new Exception("Settings panel must be visible");
            }
            finally
            {
                GameObject.DestroyImmediate(homeObj);
            }
        }

        public static void Test_WorkerHome_SettingsClosesCorrectly()
        {
            var homeObj = new GameObject("TestHomeCloseSettings");
            try
            {
                var homeCtrl = homeObj.AddComponent<WorkerHomeController>();
                homeCtrl.ShowHome();
                homeCtrl.OpenSettings();
                homeCtrl.CloseSettings();
                if (homeCtrl.CurrentState != WorkerHomeController.WorkerAppScreenState.Home)
                    throw new Exception($"Expected state Home, got {homeCtrl.CurrentState}");
                if (homeCtrl.IsSettingsVisible)
                    throw new Exception("Settings panel must be hidden after closing");
                if (!homeCtrl.IsHomeVisible)
                    throw new Exception("Home screen must be restored after closing settings");
            }
            finally
            {
                GameObject.DestroyImmediate(homeObj);
            }
        }

        public static void Test_FireAudio_SoundEffectsSettingPersists()
        {
            var audio = FireAudioService.Instance;
            bool orig = audio.IsSoundEnabled;
            try
            {
                audio.IsSoundEnabled = false;
                if (PlayerPrefs.GetInt("FireAudio_SoundEnabled") != 0)
                    throw new Exception("PlayerPrefs for sound should be 0");
                audio.LoadSettings();
                if (audio.IsSoundEnabled != false)
                    throw new Exception("Expected sound setting to load as false");

                audio.IsSoundEnabled = true;
                if (PlayerPrefs.GetInt("FireAudio_SoundEnabled") != 1)
                    throw new Exception("PlayerPrefs for sound should be 1");
                audio.LoadSettings();
                if (audio.IsSoundEnabled != true)
                    throw new Exception("Expected sound setting to load as true");
            }
            finally
            {
                audio.IsSoundEnabled = orig;
            }
        }

        public static void Test_FireAudio_EmergencyAlarmSettingPersists()
        {
            var audio = FireAudioService.Instance;
            bool orig = audio.IsEmergencyAlarmEnabled;
            try
            {
                audio.IsEmergencyAlarmEnabled = false;
                if (PlayerPrefs.GetInt("FireAudio_AlarmEnabled") != 0)
                    throw new Exception("PlayerPrefs for alarm should be 0");
                audio.LoadSettings();
                if (audio.IsEmergencyAlarmEnabled != false)
                    throw new Exception("Expected alarm setting to load as false");

                audio.IsEmergencyAlarmEnabled = true;
                if (PlayerPrefs.GetInt("FireAudio_AlarmEnabled") != 1)
                    throw new Exception("PlayerPrefs for alarm should be 1");
                audio.LoadSettings();
                if (audio.IsEmergencyAlarmEnabled != true)
                    throw new Exception("Expected alarm setting to load as true");
            }
            finally
            {
                audio.IsEmergencyAlarmEnabled = orig;
            }
        }

        public static void Test_FireAudio_EmergencyAlarmOnStartsOrPermitsAlarm()
        {
            var audio = FireAudioService.Instance;
            bool origSound = audio.IsSoundEnabled;
            bool origAlarm = audio.IsEmergencyAlarmEnabled;
            try
            {
                audio.IsSoundEnabled = true;
                audio.IsEmergencyAlarmEnabled = true;
                audio.PlayEmergencyAlarm();
                if (!audio.IsAlarmActiveScenario)
                    throw new Exception("Scenario alarm must be marked active when emergency alarm is played");
            }
            finally
            {
                audio.StopEmergencyAlarm();
                audio.IsSoundEnabled = origSound;
                audio.IsEmergencyAlarmEnabled = origAlarm;
            }
        }

        public static void Test_FireAudio_EmergencyAlarmOffStopsActiveAlarm()
        {
            var audio = FireAudioService.Instance;
            bool origSound = audio.IsSoundEnabled;
            bool origAlarm = audio.IsEmergencyAlarmEnabled;
            try
            {
                audio.IsSoundEnabled = true;
                audio.IsEmergencyAlarmEnabled = true;
                audio.PlayEmergencyAlarm();

                // Disabling emergency alarm must immediately stop siren
                audio.IsEmergencyAlarmEnabled = false;
                if (audio.IsAlarmSirenPlaying)
                    throw new Exception("Alarm siren must stop immediately when IsEmergencyAlarmEnabled is set to false");
            }
            finally
            {
                audio.StopEmergencyAlarm();
                audio.IsSoundEnabled = origSound;
                audio.IsEmergencyAlarmEnabled = origAlarm;
            }
        }

        public static void Test_FireAudio_RepeatedAlarmToggleIdempotent()
        {
            var audio = FireAudioService.Instance;
            bool origSound = audio.IsSoundEnabled;
            bool origAlarm = audio.IsEmergencyAlarmEnabled;
            try
            {
                audio.IsSoundEnabled = true;
                for (int i = 0; i < 6; i++)
                {
                    audio.IsEmergencyAlarmEnabled = (i % 2 == 0);
                    if (audio.IsEmergencyAlarmEnabled != (i % 2 == 0))
                        throw new Exception($"Toggle failed on iteration {i}");
                }
            }
            finally
            {
                audio.StopEmergencyAlarm();
                audio.IsSoundEnabled = origSound;
                audio.IsEmergencyAlarmEnabled = origAlarm;
            }
        }

        public static void Test_FireAudio_LeavingFireStopsActiveAlarm()
        {
            var audio = FireAudioService.Instance;
            bool origSound = audio.IsSoundEnabled;
            bool origAlarm = audio.IsEmergencyAlarmEnabled;
            try
            {
                audio.IsSoundEnabled = true;
                audio.IsEmergencyAlarmEnabled = true;
                audio.PlayEmergencyAlarm();
                if (!audio.IsAlarmActiveScenario)
                    throw new Exception("Alarm should be active");

                audio.StopEmergencyAlarm();
                audio.StopAllAudio();
                if (audio.IsAlarmActiveScenario)
                    throw new Exception("Scenario alarm active flag must be false after StopEmergencyAlarm/StopAllAudio");
                if (audio.IsAlarmSirenPlaying)
                    throw new Exception("Alarm siren audio source must not be playing");
            }
            finally
            {
                audio.IsSoundEnabled = origSound;
                audio.IsEmergencyAlarmEnabled = origAlarm;
            }
        }

        public static void Test_FireAudio_RetakeDoesNotInheritPreviousAlarm()
        {
            var audio = FireAudioService.Instance;
            bool origSound = audio.IsSoundEnabled;
            bool origAlarm = audio.IsEmergencyAlarmEnabled;
            var summaryObj = new GameObject("TestSummaryRetake");
            try
            {
                audio.IsSoundEnabled = true;
                audio.IsEmergencyAlarmEnabled = true;
                audio.PlayEmergencyAlarm();

                var summary = summaryObj.AddComponent<FireAssessmentSummaryUI>();
                summary.OnRetakeTrainingClicked();

                if (audio.IsAlarmActiveScenario)
                    throw new Exception("Retake must reset scenario alarm active state");
                if (audio.IsAlarmSirenPlaying)
                    throw new Exception("Retake must stop any playing alarm siren");
            }
            finally
            {
                GameObject.DestroyImmediate(summaryObj);
                audio.StopEmergencyAlarm();
                audio.IsSoundEnabled = origSound;
                audio.IsEmergencyAlarmEnabled = origAlarm;
            }
        }

        public static void Test_FireTraining_HUDAlarmButtonTogglesAlarm()
        {
            var audio = FireAudioService.Instance;
            bool origAlarm = audio.IsEmergencyAlarmEnabled;
            var fireObj = new GameObject("TestHudAlarm");
            try
            {
                var feedbackUI = fireObj.AddComponent<FireInteractionFeedbackUI>();
                feedbackUI.ShowTrainingUI();
                if (feedbackUI.AlarmButton == null)
                    throw new Exception("Alarm button must exist on FireTraining HUD");

                bool before = audio.IsEmergencyAlarmEnabled;
                feedbackUI.ToggleEmergencyAlarmEnabled();
                if (audio.IsEmergencyAlarmEnabled == before)
                    throw new Exception("HUD alarm toggle must invert IsEmergencyAlarmEnabled");

                feedbackUI.ToggleEmergencyAlarmEnabled();
                if (audio.IsEmergencyAlarmEnabled != before)
                    throw new Exception("HUD alarm toggle must return to original state on second click");
            }
            finally
            {
                GameObject.DestroyImmediate(fireObj);
                audio.IsEmergencyAlarmEnabled = origAlarm;
            }
        }

        public static void Test_SettingsModal_BlocksRaycastsUnderneath()
        {
            var homeObj = new GameObject("TestRaycastModal");
            try
            {
                var homeCtrl = homeObj.AddComponent<WorkerHomeController>();
                homeCtrl.ShowHome();
                homeCtrl.OpenSettings();

                var modalObj = GameObject.Find("SettingsPanelModal");
                if (modalObj == null)
                    throw new Exception("SettingsPanelModal must exist");

                var img = modalObj.GetComponent<UnityEngine.UI.Image>();
                if (img == null)
                    throw new Exception("SettingsPanelModal root must have an Image component");
                if (!img.raycastTarget)
                    throw new Exception("SettingsPanelModal root image must have raycastTarget enabled to block touch bleed");

                var rect = modalObj.GetComponent<RectTransform>();
                if (rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one)
                    throw new Exception("SettingsPanelModal backdrop must be full screen (anchorMin=0, anchorMax=1)");
            }
            finally
            {
                GameObject.DestroyImmediate(homeObj);
            }
        }

        public static void Test_FireAssessmentSummary_ReturnToHomeExitsCleanly()
        {
            var homeObj = new GameObject("TestSummaryExitHome");
            var summaryObj = new GameObject("TestSummaryExit");
            try
            {
                var homeCtrl = homeObj.AddComponent<WorkerHomeController>();
                var summary = summaryObj.AddComponent<FireAssessmentSummaryUI>();
                homeCtrl.StartFireTraining();

                summary.ShowSummary(new AssessmentSummaryViewModel());
                if (!summary.IsSummaryVisible)
                    throw new Exception("Summary modal must be visible");

                summary.OnReturnToHomeClicked();
                if (summary.IsSummaryVisible)
                    throw new Exception("Summary modal must be hidden after return to home");
                if (homeCtrl.CurrentState != WorkerHomeController.WorkerAppScreenState.Home)
                    throw new Exception($"Expected state Home, got {homeCtrl.CurrentState}");
                if (!homeCtrl.IsHomeVisible)
                    throw new Exception("HomeScreen must be visible after return to home");
            }
            finally
            {
                GameObject.DestroyImmediate(homeObj);
                GameObject.DestroyImmediate(summaryObj);
            }
        }

        public static void Test_Localization_EnglishLocaleLoads()
        {
            var loc = LocaleService.Instance;
            loc.SetLanguage(LocaleService.LangEnglish);
            var catalog = loc.GetCatalogForLanguage(LocaleService.LangEnglish);
            if (catalog == null || catalog.Count == 0)
                throw new Exception("English catalog failed to load or is empty");

            if (!catalog.ContainsKey("app_title") || catalog["app_title"] != "Industrial Safety AR")
                throw new Exception("English catalog missing app_title");
        }

        public static void Test_Localization_HindiLocaleLoads()
        {
            var loc = LocaleService.Instance;
            var catalog = loc.GetCatalogForLanguage(LocaleService.LangHindi);
            if (catalog == null || catalog.Count == 0)
                throw new Exception("Hindi catalog failed to load or is empty");

            if (!catalog.ContainsKey("app_title") || string.IsNullOrEmpty(catalog["app_title"]))
                throw new Exception("Hindi catalog missing app_title");

            bool hasDevanagari = false;
            foreach (char c in catalog["app_title"])
            {
                if (c >= '\u0900' && c <= '\u097F') { hasDevanagari = true; break; }
            }
            if (!hasDevanagari)
                throw new Exception("Hindi catalog app_title does not contain Devanagari script characters");
        }

        public static void Test_Localization_SantaliLocaleLoads()
        {
            var loc = LocaleService.Instance;
            var catalog = loc.GetCatalogForLanguage(LocaleService.LangSantali);
            if (catalog == null || catalog.Count == 0)
                throw new Exception("Santali catalog failed to load or is empty");

            if (!catalog.ContainsKey("app_title") || string.IsNullOrEmpty(catalog["app_title"]))
                throw new Exception("Santali catalog missing app_title");

            bool hasOlChiki = false;
            foreach (char c in catalog["app_title"])
            {
                if (c >= '\u1C50' && c <= '\u1C7F') { hasOlChiki = true; break; }
            }
            if (!hasOlChiki)
                throw new Exception("Santali catalog app_title does not contain Ol Chiki script characters (U+1C50-U+1C7F)");
        }

        public static void Test_Localization_RequiredKeysExistInAllLocales()
        {
            var loc = LocaleService.Instance;
            string[] requiredKeys = new[]
            {
                "app_title", "app_subtitle", "btn_back", "btn_close", "btn_next",
                "btn_complete", "btn_retake", "btn_return_home", "state_on", "state_off",
                "offline_mode", "online_sync", "worker_profile", "worker_id_label",
                "worker_name_label", "modules_header", "module_fire_title", "module_fire_desc",
                "module_gas_title", "module_gas_desc", "status_available", "status_coming_soon",
                "btn_start_training", "settings_title", "sound_effects", "effects_volume",
                "emergency_alarm", "language_header", "fire_header_title", "step_badge_format",
                "ar_calibration_badge", "ar_calibration_prompt", "ar_calibration_feedback",
                "surface_detected_badge", "surface_detected_prompt", "surface_detected_feedback",
                "btn_place_hazard", "btn_alarm_on", "btn_alarm_off", "feedback_alarm_enabled",
                "feedback_alarm_muted", "feedback_sound_enabled", "feedback_sound_muted",
                "action_required_format", "step_completed_format", "step_review_format",
                "training_completed_notice", "default_success_feedback",
                "fire_step1_title", "fire_step1_prompt", "fire_step1_btn_ack",
                "fire_step2_title", "fire_step2_prompt", "fire_step2_opt1", "fire_step2_opt2", "fire_step2_opt3",
                "fire_step3_title", "fire_step3_prompt", "fire_step3_btn_alarm",
                "fire_step4_title", "fire_step4_prompt", "fire_step4_opt1", "fire_step4_opt2", "fire_step4_opt3",
                "fire_step5_title", "fire_step5_prompt", "fire_step5_opt1", "fire_step5_opt2",
                "fire_step6_title", "fire_step6_prompt_pull", "fire_step6_btn_pull",
                "fire_step7_title", "fire_step7_prompt", "fire_step7_opt1", "fire_step7_opt2", "fire_step7_opt3",
                "fire_step8_title", "fire_step8_prompt", "fire_step8_wp1", "fire_step8_wp2", "fire_step8_wp3", "fire_step8_unsafe_smoke",
                "fire_step9_title", "fire_step9_prompt", "fire_step9_opt1", "fire_step9_opt2",
                "assessment_title", "assessment_status_passed", "assessment_status_failed",
                "assessment_score_label", "assessment_duration_label", "assessment_sync_ready",
                "assessment_btn_finish", "assessment_btn_retake", "assessment_btn_breakdown"
            };

            string[] languages = new[] { LocaleService.LangEnglish, LocaleService.LangHindi, LocaleService.LangSantali };

            foreach (var lang in languages)
            {
                foreach (var key in requiredKeys)
                {
                    if (!loc.HasKey(lang, key))
                    {
                        throw new Exception($"Language '{lang}' is missing required key '{key}'");
                    }
                    string val = loc.GetCatalogForLanguage(lang)[key];
                    if (string.IsNullOrWhiteSpace(val))
                    {
                        throw new Exception($"Language '{lang}' has empty value for required key '{key}'");
                    }
                }
            }
        }

        public static void Test_Localization_RuntimeLocaleSwitching()
        {
            var loc = LocaleService.Instance;
            string recordedLang = null;
            Action<string> handler = lang => recordedLang = lang;

            try
            {
                loc.OnLanguageChanged += handler;

                loc.SetLanguage(LocaleService.LangHindi);
                if (loc.CurrentLanguage != LocaleService.LangHindi)
                    throw new Exception($"Expected CurrentLanguage to be 'hi', got {loc.CurrentLanguage}");
                if (recordedLang != LocaleService.LangHindi)
                    throw new Exception($"Expected OnLanguageChanged to emit 'hi', got {recordedLang}");

                string hindiTitle = loc.Get("app_title");
                if (hindiTitle != "औद्योगिक सुरक्षा एआर")
                    throw new Exception($"Expected Hindi app_title, got {hindiTitle}");

                loc.SetLanguage(LocaleService.LangSantali);
                if (loc.CurrentLanguage != LocaleService.LangSantali)
                    throw new Exception($"Expected CurrentLanguage to be 'sat', got {loc.CurrentLanguage}");
                if (recordedLang != LocaleService.LangSantali)
                    throw new Exception($"Expected OnLanguageChanged to emit 'sat', got {recordedLang}");

                string satTitle = loc.Get("app_title");
                if (!satTitle.Contains("ᱤᱱᱰᱟᱥᱴᱨᱤᱭᱟᱞ"))
                    throw new Exception($"Expected Santali app_title, got {satTitle}");
            }
            finally
            {
                loc.OnLanguageChanged -= handler;
                loc.SetLanguage(LocaleService.LangEnglish);
            }
        }

        public static void Test_Localization_SelectedLocalePersists()
        {
            var loc = LocaleService.Instance;
            try
            {
                loc.SetLanguage(LocaleService.LangSantali);
                string persisted = PlayerPrefs.GetString(LocaleService.PrefLanguageKey, "");
                if (persisted != LocaleService.LangSantali)
                    throw new Exception($"Expected PlayerPrefs '{LocaleService.PrefLanguageKey}' to be 'sat', got '{persisted}'");

                loc.SetLanguage(LocaleService.LangHindi);
                persisted = PlayerPrefs.GetString(LocaleService.PrefLanguageKey, "");
                if (persisted != LocaleService.LangHindi)
                    throw new Exception($"Expected PlayerPrefs '{LocaleService.PrefLanguageKey}' to be 'hi', got '{persisted}'");
            }
            finally
            {
                loc.SetLanguage(LocaleService.LangEnglish);
            }
        }

        public static void Test_Localization_MissingKeyDoesNotCrash()
        {
            var loc = LocaleService.Instance;
            loc.SetLanguage(LocaleService.LangHindi);

            string resultWithFallback = loc.Get("nonexistent_key_xyz123", "Default Fallback String");
            if (resultWithFallback != "Default Fallback String")
                throw new Exception($"Expected fallback string for missing key, got '{resultWithFallback}'");

            string resultWithoutFallback = loc.Get("nonexistent_key_xyz123");
            if (resultWithoutFallback != "nonexistent_key_xyz123")
                throw new Exception($"Expected key name when fallback is null, got '{resultWithoutFallback}'");

            loc.SetLanguage(LocaleService.LangEnglish);
        }

        public static void Test_Localization_EnglishFallbackWhenTranslationMissing()
        {
            var loc = LocaleService.Instance;
            try
            {
                loc.SetLanguage(LocaleService.LangSantali);
                string enVal = loc.GetCatalogForLanguage(LocaleService.LangEnglish)["app_title"];
                if (string.IsNullOrEmpty(enVal))
                    throw new Exception("English app_title should not be empty");

                string retrieved = loc.Get("app_title");
                if (string.IsNullOrEmpty(retrieved))
                    throw new Exception("Santali app_title should return valid text");
            }
            finally
            {
                loc.SetLanguage(LocaleService.LangEnglish);
            }
        }

        public static void Test_Localization_FontFallbackAssetsPresent()
        {
            LocaleService.EnsureFallbackFonts();
            var defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont == null)
            {
                defaultFont = TMP_Settings.defaultFontAsset;
            }
            if (defaultFont == null)
                throw new Exception("Default TextMeshPro font asset not found");

            var devFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansDevanagari SDF");
            if (devFont == null)
                throw new Exception("NotoSansDevanagari SDF font asset not found in Resources/Fonts & Materials");

            var olFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansOlChiki SDF");
            if (olFont == null)
                throw new Exception("NotoSansOlChiki SDF font asset not found in Resources/Fonts & Materials");

            bool hasDev = false;
            bool hasOl = false;
            if (defaultFont.fallbackFontAssetTable != null)
            {
                foreach (var fb in defaultFont.fallbackFontAssetTable)
                {
                    if (fb == null) continue;
                    if (fb.name.Contains("Devanagari")) hasDev = true;
                    if (fb.name.Contains("OlChiki")) hasOl = true;
                }
            }

            if (!hasDev || !hasOl)
            {
                if (TMP_Settings.fallbackFontAssets != null)
                {
                    foreach (var fb in TMP_Settings.fallbackFontAssets)
                    {
                        if (fb == null) continue;
                        if (fb.name.Contains("Devanagari")) hasDev = true;
                        if (fb.name.Contains("OlChiki")) hasOl = true;
                    }
                }
            }

            if (!hasDev)
                throw new Exception("Devanagari font asset is not linked in fallback font tables");
            if (!hasOl)
                throw new Exception("Ol Chiki font asset is not linked in fallback font tables");
        }
    }
}
