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
using IndustrialSafetyAR.Core.Events;
using IndustrialSafetyAR.Modules.FireExplosion;
using UnityEngine;

namespace IndustrialSafetyAR.Tests
{
    public static class FireTrainingEventTests
    {
        public static bool RunAllTests(out List<string> logMessages)
        {
            logMessages = new List<string>();
            bool allPassed = true;

            allPassed &= RunTest("EventCreationMatchesSchema", Test_EventCreationMatchesSchema, logMessages);
            allPassed &= RunTest("EventBusRecordsAndNotifies", Test_EventBusRecordsAndNotifies, logMessages);
            allPassed &= RunTest("EventPayloadIntegrity", Test_EventPayloadIntegrity, logMessages);
            allPassed &= RunTest("SuccessfulHazardIdentificationEvent", Test_SuccessfulHazardIdentificationEvent, logMessages);
            allPassed &= RunTest("SuccessfulRaiseAlarmEvent", Test_SuccessfulRaiseAlarmEvent, logMessages);
            allPassed &= RunTest("CorrectEventOrdering", Test_CorrectEventOrdering, logMessages);
            allPassed &= RunTest("InvalidActionDoesNotAdvanceFlow", Test_InvalidActionDoesNotAdvanceFlow, logMessages);
            allPassed &= RunTest("SuccessfulCO2ExtinguisherSelection", Test_SuccessfulCO2ExtinguisherSelection, logMessages);
            allPassed &= RunTest("IncorrectWaterExtinguisherSelection", Test_IncorrectWaterExtinguisherSelection, logMessages);
            allPassed &= RunTest("IncorrectFoamExtinguisherSelection", Test_IncorrectFoamExtinguisherSelection, logMessages);
            allPassed &= RunTest("PrematureExtinguisherSelectionRejected", Test_PrematureExtinguisherSelectionRejected, logMessages);
            allPassed &= RunTest("Step3ToStep4EventOrdering", Test_Step3ToStep4EventOrdering, logMessages);
            allPassed &= RunTest("SuccessfulSafeDistanceAction", Test_SuccessfulSafeDistanceAction, logMessages);
            allPassed &= RunTest("UnsafeTooCloseActionRejected", Test_UnsafeTooCloseActionRejected, logMessages);
            allPassed &= RunTest("PrematureSafeDistanceActionRejected", Test_PrematureSafeDistanceActionRejected, logMessages);
            allPassed &= RunTest("Step4ToStep5EventOrdering", Test_Step4ToStep5EventOrdering, logMessages);
            allPassed &= RunTest("Step6CannotBeginBeforeStep5", Test_Step6CannotBeginBeforeStep5, logMessages);
            allPassed &= RunTest("PullPinAccepted", Test_PullPinAccepted, logMessages);
            allPassed &= RunTest("AimAcceptedOnlyAfterPin", Test_AimAcceptedOnlyAfterPin, logMessages);
            allPassed &= RunTest("SqueezeAcceptedOnlyAfterAim", Test_SqueezeAcceptedOnlyAfterAim, logMessages);
            allPassed &= RunTest("SweepAcceptedOnlyAfterSqueeze", Test_SweepAcceptedOnlyAfterSqueeze, logMessages);
            allPassed &= RunTest("WrongOutOfOrderActionRejected", Test_WrongOutOfOrderActionRejected, logMessages);
            allPassed &= RunTest("FinalProcedureCompletedEventFields", Test_FinalProcedureCompletedEventFields, logMessages);
            allPassed &= RunTest("Steps1To6EventOrdering", Test_Steps1To6EventOrdering, logMessages);
            allPassed &= RunTest("Step7CannotBeginBeforeStep6", Test_Step7CannotBeginBeforeStep6, logMessages);
            allPassed &= RunTest("SuccessfulEmergencyExitIdentification", Test_SuccessfulEmergencyExitIdentification, logMessages);
            allPassed &= RunTest("IncorrectElevatorExitRejected", Test_IncorrectElevatorExitRejected, logMessages);
            allPassed &= RunTest("IncorrectBlockedCorridorExitRejected", Test_IncorrectBlockedCorridorExitRejected, logMessages);
            allPassed &= RunTest("EmergencyExitEventMatchesRubricSchema", Test_EmergencyExitEventMatchesRubricSchema, logMessages);
            allPassed &= RunTest("Steps1To7EventOrdering", Test_Steps1To7EventOrdering, logMessages);
            allPassed &= RunTest("Step8CannotBeginBeforeStep7", Test_Step8CannotBeginBeforeStep7, logMessages);
            allPassed &= RunTest("SuccessfulEvacuationSequence", Test_SuccessfulEvacuationSequence, logMessages);
            allPassed &= RunTest("OutOfOrderWaypointRejected", Test_OutOfOrderWaypointRejected, logMessages);
            allPassed &= RunTest("SmokeCorridorSelectionEmitsFailure", Test_SmokeCorridorSelectionEmitsFailure, logMessages);
            allPassed &= RunTest("EvacuationRouteEventMatchesRubricSchema", Test_EvacuationRouteEventMatchesRubricSchema, logMessages);
            allPassed &= RunTest("Steps1To8EventOrdering", Test_Steps1To8EventOrdering, logMessages);
            allPassed &= RunTest("DuplicateEvacuationCompletionPrevented", Test_DuplicateEvacuationCompletionPrevented, logMessages);
            allPassed &= RunTest("Step9CannotBeginBeforeStep8", Test_Step9CannotBeginBeforeStep8, logMessages);
            allPassed &= RunTest("SuccessfulAssemblyPointIdentification", Test_SuccessfulAssemblyPointIdentification, logMessages);
            allPassed &= RunTest("WrongAssemblyPointRejected", Test_WrongAssemblyPointRejected, logMessages);
            allPassed &= RunTest("AssemblyPointEventMatchesRubricSchema", Test_AssemblyPointEventMatchesRubricSchema, logMessages);
            allPassed &= RunTest("Step8ToStep9EventOrdering", Test_Step8ToStep9EventOrdering, logMessages);
            allPassed &= RunTest("DuplicateAssemblyCompletionPrevented", Test_DuplicateAssemblyCompletionPrevented, logMessages);
            allPassed &= RunTest("PrematureAssemblyPointActionRejected", Test_PrematureAssemblyPointActionRejected, logMessages);

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

            // Verify canonical 2-step route (waypoint_main_corridor -> waypoint_by_exit -> step_reach_assembly)
            var workflow2 = new FireTrainingWorkflow();
            workflow2.SetStage(FireWorkflowStage.ExitIdentified);
            bool c1 = workflow2.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor, bus, out _);
            if (!c1) throw new Exception("Canonical waypoint 1 should succeed");
            bool c2 = workflow2.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointByExit, bus, out var ec2);
            if (!c2) throw new Exception("Canonical waypoint_by_exit should succeed");
            if (workflow2.CurrentStage != FireWorkflowStage.RouteEvacuated)
                throw new Exception($"Expected RouteEvacuated stage, got {workflow2.CurrentStage}");
            if (workflow2.CurrentStepId != "step_reach_assembly")
                throw new Exception($"Expected CurrentStepId step_reach_assembly, got {workflow2.CurrentStepId}");
            if (ec2 == null || ec2.Outcome != "success")
                throw new Exception("Expected successful completion event for canonical route");
            if (ec2.TargetId != FireTrainingWorkflow.WaypointByExit)
                throw new Exception("Expected target_id waypoint_by_exit");
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
    }
}
