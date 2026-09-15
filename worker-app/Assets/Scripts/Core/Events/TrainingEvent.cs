// TrainingEvent.cs
// Namespace : IndustrialSafetyAR.Core.Events
//
// Pure C# domain model representing an event emitted during training.
// Decoupled from Unity runtime so it can be instantiated, serialized,
// and unit-tested in isolation.

using System;
using System.Collections.Generic;

namespace IndustrialSafetyAR.Core.Events
{
    /// <summary>
    /// Domain-level training event emitted when a worker performs an action
    /// or completes a step during an AR training scenario.
    /// Designed to match module.json criteria and rubric.json scoring rules.
    /// </summary>
    [Serializable]
    public class TrainingEvent
    {
        /// <summary>Unique identifier for this event instance.</summary>
        public string EventId { get; set; }

        /// <summary>The identifier of the active training module (e.g. "fire-explosion-response").</summary>
        public string ModuleId { get; set; }

        /// <summary>Content version of the active module (e.g. "1.0.0").</summary>
        public string ContentVersion { get; set; }

        /// <summary>Current step identifier in the module sequence (e.g. "step_detect_hazard").</summary>
        public string StepId { get; set; }

        /// <summary>Semantic event type matching rubric criteria (e.g. "step_completed", "hazard_identified").</summary>
        public string EventType { get; set; }

        /// <summary>Action identifier associated with the event (e.g. "detect_hazard_acknowledged").</summary>
        public string ActionId { get; set; }

        /// <summary>Target entity identifier if applicable (e.g. "hazard_electrical_conveyor_fire").</summary>
        public string TargetId { get; set; }

        /// <summary>Outcome of the action ("success", "failure", etc.).</summary>
        public string Outcome { get; set; }

        /// <summary>Epoch timestamp in milliseconds when the event occurred.</summary>
        public long TimestampUnixMs { get; set; }

        /// <summary>Arbitrary payload metadata matching module success_criteria and rubric rules.</summary>
        public Dictionary<string, string> Payload { get; set; }

        public TrainingEvent()
        {
            EventId = Guid.NewGuid().ToString();
            TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Payload = new Dictionary<string, string>();
            Outcome = "success";
        }

        public string GetPayloadValue(string key)
        {
            if (Payload != null && Payload.TryGetValue(key, out var value))
            {
                return value;
            }
            return null;
        }

        public override string ToString()
        {
            return $"[TrainingEvent] {ModuleId} | Step: {StepId} | EventType: {EventType} | Action: {ActionId} | Target: {TargetId} | Outcome: {Outcome}";
        }
    }
}
