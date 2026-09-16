// AssessmentModels.cs
// Namespace : IndustrialSafetyAR.Assessment
//
// Pure C# domain models for offline evaluation, rubric rules, and attempt persistence.
// Strictly matches docs/contracts/attempt.schema.json and docs/contracts/rubric.schema.json.
// Completely decoupled from UnityEngine for isolated unit testing and cross-platform verification.

using System;
using System.Collections.Generic;
using IndustrialSafetyAR.Core.Events;

namespace IndustrialSafetyAR.Assessment
{
    /// <summary>
    /// Represents one worker attempt of a training module.
    /// Conforms strictly to docs/contracts/attempt.schema.json.
    /// </summary>
    [Serializable]
    public class TrainingAttempt
    {
        public const string StatusInProgress = "in_progress";
        public const string StatusCompleted = "completed";
        public const string StatusAbandoned = "abandoned";

        /// <summary>JSON contract version (e.g. "1.0.0").</summary>
        public string SchemaVersion { get; set; } = "1.0.0";

        /// <summary>Unique client-generated UUID for sync idempotency.</summary>
        public string ClientAttemptId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Worker identifier known on-device.</summary>
        public string WorkerId { get; set; }

        /// <summary>Module identifier (e.g. "fire-explosion-response").</summary>
        public string ModuleId { get; set; }

        /// <summary>Authored module/rubric pack version (e.g. "1.0.0").</summary>
        public string ContentVersion { get; set; } = "1.0.0";

        /// <summary>ISO 8601 timestamp when training commenced.</summary>
        public string StartedAt { get; set; } = DateTime.UtcNow.ToString("o");

        /// <summary>ISO 8601 timestamp when training finished (required when status is completed).</summary>
        public string CompletedAt { get; set; }

        /// <summary>Attempt lifecycle status ("in_progress", "completed", "abandoned").</summary>
        public string Status { get; set; } = StatusInProgress;

        /// <summary>Device-computed score from the bundled rubric (0.00 to 10000.00).</summary>
        public float ClientScore { get; set; }

        /// <summary>Device-computed pass flag based on module pass_percent.</summary>
        public bool Passed { get; set; }

        /// <summary>Ordered list of training events dispatched during this attempt.</summary>
        public List<TrainingEvent> Events { get; set; } = new List<TrainingEvent>();

        // Optional server-side post-sync enrichments
        public string ServerAttemptId { get; set; }
        public float? ServerScore { get; set; }
        public bool? ServerPassed { get; set; }
        public string CertificatePublicId { get; set; }

        public TrainingAttempt()
        {
        }

        public TrainingAttempt(string workerId, string moduleId, string contentVersion = "1.0.0")
        {
            WorkerId = workerId;
            ModuleId = moduleId;
            ContentVersion = contentVersion;
            ClientAttemptId = Guid.NewGuid().ToString();
            StartedAt = DateTime.UtcNow.ToString("o");
            Status = StatusInProgress;
        }

        /// <summary>
        /// Finalizes the attempt with score, pass status, and completed_at timestamp.
        /// </summary>
        public void Complete(float score, bool passed)
        {
            ClientScore = (float)Math.Round(score, 2);
            Passed = passed;
            Status = StatusCompleted;
            CompletedAt = DateTime.UtcNow.ToString("o");
        }
    }

    /// <summary>
    /// Encapsulates the complete scoring rubric specification for a training module.
    /// Conforms strictly to docs/contracts/rubric.schema.json.
    /// </summary>
    [Serializable]
    public class RubricDefinition
    {
        public string SchemaVersion { get; set; } = "1.0.0";
        public string ModuleId { get; set; }
        public string ContentVersion { get; set; } = "1.0.0";
        public float PassPercent { get; set; } = 70.00f;
        public float MaxScore { get; set; } = 100.00f;
        public List<RubricRule> Rules { get; set; } = new List<RubricRule>();

        /// <summary>
        /// Factory creating the canonical Fire &amp; Explosion Response scoring rubric matching rubric.json.
        /// </summary>
        public static RubricDefinition CreateFireExplosionRubric()
        {
            return new RubricDefinition
            {
                SchemaVersion = "1.0.0",
                ModuleId = "fire-explosion-response",
                ContentVersion = "1.0.0",
                PassPercent = 70.00f,
                MaxScore = 100.00f,
                Rules = new List<RubricRule>
                {
                    // Step 1: Detect Hazard (+5)
                    new RubricRule
                    {
                        RuleId = "rule_detect_hazard",
                        StepId = "step_detect_hazard",
                        Required = true,
                        EventType = "step_completed",
                        Match = new RuleMatchCriteria { ActionId = "detect_hazard_acknowledged", Outcome = "success" },
                        Points = 5.00f,
                        AwardLimit = 1,
                        PenaltyPoints = 0.00f
                    },
                    // Step 2: Identify Hazard (+15, -5 on failure)
                    new RubricRule
                    {
                        RuleId = "rule_identify_hazard",
                        StepId = "step_identify_hazard",
                        Required = true,
                        EventType = "hazard_identified",
                        Match = new RuleMatchCriteria { TargetId = "hazard_electrical_conveyor_fire", Outcome = "success" },
                        Points = 15.00f,
                        AwardLimit = 1,
                        PenaltyPoints = 5.00f,
                        PenaltyEventType = "hazard_identified",
                        PenaltyMatch = new RuleMatchCriteria { Outcome = "failure" }
                    },
                    // Step 3: Raise Alarm (+15)
                    new RubricRule
                    {
                        RuleId = "rule_raise_alarm",
                        StepId = "step_raise_alarm",
                        Required = true,
                        EventType = "alarm_raised",
                        Match = new RuleMatchCriteria { ActionId = "manual_call_point_activated", Outcome = "success" },
                        Points = 15.00f,
                        AwardLimit = 1,
                        PenaltyPoints = 0.00f
                    },
                    // Step 4: Select Extinguisher (+15, -5 on failure)
                    new RubricRule
                    {
                        RuleId = "rule_select_extinguisher",
                        StepId = "step_select_extinguisher",
                        Required = true,
                        EventType = "extinguisher_selected",
                        Match = new RuleMatchCriteria { TargetId = "extinguisher_co2", Outcome = "success" },
                        Points = 15.00f,
                        AwardLimit = 1,
                        PenaltyPoints = 5.00f,
                        PenaltyEventType = "extinguisher_selected",
                        PenaltyMatch = new RuleMatchCriteria { Outcome = "failure" }
                    },
                    // Step 5: Maintain Safe Distance (+10)
                    new RubricRule
                    {
                        RuleId = "rule_maintain_distance",
                        StepId = "step_maintain_distance",
                        Required = true,
                        EventType = "decision_made",
                        Match = new RuleMatchCriteria { DecisionId = "standoff_distance_2m_maintained", Outcome = "success" },
                        Points = 10.00f,
                        AwardLimit = 1,
                        PenaltyPoints = 0.00f
                    },
                    // Step 6: Use Extinguisher / PASS (+15, -5 on failure)
                    new RubricRule
                    {
                        RuleId = "rule_use_extinguisher",
                        StepId = "step_use_extinguisher",
                        Required = true,
                        EventType = "extinguisher_used",
                        Match = new RuleMatchCriteria { ActionId = "pass_procedure_completed", Outcome = "success" },
                        Points = 15.00f,
                        AwardLimit = 1,
                        PenaltyPoints = 5.00f,
                        PenaltyEventType = "extinguisher_used",
                        PenaltyMatch = new RuleMatchCriteria { Outcome = "failure" }
                    },
                    // Step 7: Identify Emergency Exit (+10)
                    new RubricRule
                    {
                        RuleId = "rule_identify_exit",
                        StepId = "step_identify_exit",
                        Required = true,
                        EventType = "exit_marked",
                        Match = new RuleMatchCriteria { TargetId = "exit_emergency_sector_b", Outcome = "success" },
                        Points = 10.00f,
                        AwardLimit = 1,
                        PenaltyPoints = 0.00f
                    },
                    // Step 8: Evacuation Route (+10, -5 on failure)
                    new RubricRule
                    {
                        RuleId = "rule_evacuate_route",
                        StepId = "step_evacuate_route",
                        Required = true,
                        EventType = "evacuation_sequence_submitted",
                        Match = new RuleMatchCriteria { Outcome = "success" },
                        Points = 10.00f,
                        AwardLimit = 1,
                        PenaltyPoints = 5.00f,
                        PenaltyEventType = "evacuation_sequence_submitted",
                        PenaltyMatch = new RuleMatchCriteria { Outcome = "failure" }
                    },
                    // Step 9: Reach Assembly Point (+5)
                    new RubricRule
                    {
                        RuleId = "rule_reach_assembly",
                        StepId = "step_reach_assembly",
                        Required = true,
                        EventType = "assembly_reached",
                        Match = new RuleMatchCriteria { TargetId = "assembly_muster_point_alpha", Outcome = "success" },
                        Points = 5.00f,
                        AwardLimit = 1,
                        PenaltyPoints = 0.00f
                    }
                }
            };
        }
    }

    /// <summary>
    /// Deterministic rule definition within a scoring rubric.
    /// </summary>
    [Serializable]
    public class RubricRule
    {
        public string RuleId { get; set; }
        public string StepId { get; set; }
        public bool Required { get; set; } = true;
        public string EventType { get; set; }
        public RuleMatchCriteria Match { get; set; }
        public float Points { get; set; }
        public int AwardLimit { get; set; } = 1;
        public float PenaltyPoints { get; set; } = 0.00f;
        public string PenaltyEventType { get; set; }
        public RuleMatchCriteria PenaltyMatch { get; set; }

        /// <summary>
        /// Evaluates whether an event qualifies for positive point award under this rule.
        /// </summary>
        public bool MatchesAwardEvent(TrainingEvent evt)
        {
            if (evt == null) return false;
            if (!string.Equals(evt.EventType, EventType, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrEmpty(StepId) && !string.Equals(evt.StepId, StepId, StringComparison.OrdinalIgnoreCase))
                return false;

            return Match == null || Match.Matches(evt);
        }

        /// <summary>
        /// Evaluates whether an event qualifies as a penalty under this rule.
        /// </summary>
        public bool MatchesPenaltyEvent(TrainingEvent evt)
        {
            if (evt == null || PenaltyPoints <= 0f) return false;

            string penaltyType = !string.IsNullOrEmpty(PenaltyEventType) ? PenaltyEventType : EventType;
            if (!string.Equals(evt.EventType, penaltyType, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrEmpty(StepId) && !string.Equals(evt.StepId, StepId, StringComparison.OrdinalIgnoreCase))
                return false;

            return PenaltyMatch == null || PenaltyMatch.Matches(evt);
        }
    }

    /// <summary>
    /// Predicates for matching training events during rubric scoring.
    /// </summary>
    [Serializable]
    public class RuleMatchCriteria
    {
        public string ActionId { get; set; }
        public string TargetId { get; set; }
        public string DecisionId { get; set; }
        public string Outcome { get; set; }
        public List<string> SelectionIds { get; set; }
        public List<string> OrderedIds { get; set; }
        public bool? BooleanValue { get; set; }

        public bool Matches(TrainingEvent evt)
        {
            if (evt == null) return false;

            if (!string.IsNullOrEmpty(ActionId))
            {
                string act = evt.ActionId ?? evt.GetPayloadValue("action_id");
                if (!string.Equals(act, ActionId, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrEmpty(TargetId))
            {
                string tgt = evt.TargetId ?? evt.GetPayloadValue("target_id");
                if (!string.Equals(tgt, TargetId, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrEmpty(DecisionId))
            {
                string dec = evt.GetPayloadValue("decision_id") ?? evt.TargetId;
                if (!string.Equals(dec, DecisionId, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrEmpty(Outcome))
            {
                string outc = evt.Outcome ?? evt.GetPayloadValue("outcome");
                if (!string.Equals(outc, Outcome, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (OrderedIds != null && OrderedIds.Count > 0)
            {
                string orderedPayload = evt.GetPayloadValue("ordered_ids");
                if (string.IsNullOrEmpty(orderedPayload))
                    return false;

                string expectedCsv = string.Join(",", OrderedIds);
                if (!string.Equals(orderedPayload, expectedCsv, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Granular outcome and diagnostic details from evaluating a single RubricRule.
    /// </summary>
    [Serializable]
    public class RuleEvaluationResult
    {
        public string RuleId { get; set; }
        public string StepId { get; set; }
        public bool Required { get; set; }
        public bool IsSatisfied { get; set; }
        public int TimesAwarded { get; set; }
        public float PointsAwarded { get; set; }
        public int TimesPenalized { get; set; }
        public float PenaltyDeducted { get; set; }
        public float NetScore => PointsAwarded - PenaltyDeducted;
        public List<TrainingEvent> MatchedEvents { get; set; } = new List<TrainingEvent>();
        public List<TrainingEvent> PenaltyEvents { get; set; } = new List<TrainingEvent>();
        public string Details { get; set; }
    }
}
