// FireTrainingEventTests.cs
// Namespace : IndustrialSafetyAR.Tests
//
// Standalone unit tests validating the decoupled training event architecture,
// event bus dispatching, and hazard detection contracts without requiring AR hardware.

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

            return allPassed;
        }

        private static bool RunTest(string testName, System.Action testAction, List<string> log)
        {
            try
            {
                testAction();
                log.Add($"[PASS] {testName}");
                return true;
            }
            catch (System.Exception ex)
            {
                log.Add($"[FAIL] {testName}: {ex.Message}");
                return false;
            }
        }

        public static void Test_EventCreationMatchesSchema()
        {
            var evt = new TrainingEvent
            {
                ModuleId = "fire-explosion-response",
                ContentVersion = "1.0.0",
                StepId = "step_detect_hazard",
                EventType = "step_completed",
                ActionId = "detect_hazard_acknowledged",
                TargetId = "hazard_electrical_conveyor_fire",
                Outcome = "success"
            };

            if (evt.ModuleId != "fire-explosion-response") throw new System.Exception("ModuleId mismatch");
            if (evt.StepId != "step_detect_hazard") throw new System.Exception("StepId mismatch");
            if (evt.EventType != "step_completed") throw new System.Exception("EventType mismatch");
            if (evt.ActionId != "detect_hazard_acknowledged") throw new System.Exception("ActionId mismatch");
            if (evt.Outcome != "success") throw new System.Exception("Outcome mismatch");
            if (string.IsNullOrEmpty(evt.EventId)) throw new System.Exception("EventId was not generated");
        }

        public static void Test_EventBusRecordsAndNotifies()
        {
            var bus = new TrainingEventBus();
            bus.Clear();

            TrainingEvent received = null;
            bus.OnEventDispatched += (e) => received = e;

            var evt = new TrainingEvent
            {
                ModuleId = "fire-explosion-response",
                StepId = "step_detect_hazard",
                EventType = "step_completed",
                ActionId = "detect_hazard_acknowledged"
            };

            bus.Dispatch(evt);

            if (received == null) throw new System.Exception("Subscriber was not invoked");
            if (received.EventId != evt.EventId) throw new System.Exception("Received incorrect event");
            if (bus.DispatchedEvents.Count != 1) throw new System.Exception("Event was not stored in history");
        }

        public static void Test_EventPayloadIntegrity()
        {
            var evt = new TrainingEvent
            {
                Payload = new Dictionary<string, string>
                {
                    { "rule_id", "rule_detect_hazard" },
                    { "action_id", "detect_hazard_acknowledged" }
                }
            };

            if (evt.GetPayloadValue("rule_id") != "rule_detect_hazard")
                throw new System.Exception("Payload rule_id missing or invalid");

            if (evt.GetPayloadValue("nonexistent") != null)
                throw new System.Exception("Payload returned value for nonexistent key");
        }
    }
}
