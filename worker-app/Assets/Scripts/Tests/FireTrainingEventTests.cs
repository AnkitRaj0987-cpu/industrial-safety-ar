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
    }
}
