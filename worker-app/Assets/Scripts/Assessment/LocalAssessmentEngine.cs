// LocalAssessmentEngine.cs
// Namespace : IndustrialSafetyAR.Assessment
//
// Pure C# deterministic scoring evaluator for offline training assessment.
// Evaluates chronological domain events against rubric specifications matching rubric.schema.json.
// Completely decoupled from UnityEngine for isolated unit testing and offline-first execution.

using System;
using System.Collections.Generic;
using IndustrialSafetyAR.Core.Events;

namespace IndustrialSafetyAR.Assessment
{
    /// <summary>
    /// Pure C# scoring engine providing deterministic, synchronous offline assessment.
    /// Evaluates TrainingEvents against RubricDefinition rules and computes client score and pass status.
    /// </summary>
    public static class LocalAssessmentEngine
    {
        /// <summary>
        /// Evaluates a chronological sequence of training events against a rubric definition.
        /// </summary>
        /// <param name="events">Ordered list of events dispatched during the session.</param>
        /// <param name="rubric">Rubric specification containing rules and threshold.</param>
        /// <returns>A comprehensive AssessmentResult containing per-rule details and final scores.</returns>
        public static AssessmentResult Evaluate(IEnumerable<TrainingEvent> events, RubricDefinition rubric)
        {
            if (rubric == null)
            {
                rubric = RubricDefinition.CreateFireExplosionRubric();
            }

            var result = new AssessmentResult
            {
                MaxScore = rubric.MaxScore > 0f ? rubric.MaxScore : 100.00f,
                PassPercent = rubric.PassPercent > 0f ? rubric.PassPercent : 70.00f
            };

            var eventList = events != null ? new List<TrainingEvent>(events) : new List<TrainingEvent>();

            float totalAwarded = 0f;
            float totalPenalties = 0f;
            bool allRequiredSatisfied = true;

            var processedAwardKeys = new HashSet<string>();
            var processedPenaltyKeys = new HashSet<string>();

            foreach (var rule in rubric.Rules)
            {
                if (rule == null) continue;

                var ruleResult = new RuleEvaluationResult
                {
                    RuleId = rule.RuleId,
                    StepId = rule.StepId,
                    Required = rule.Required,
                    PointsAwarded = 0f,
                    PenaltyDeducted = 0f,
                    TimesAwarded = 0,
                    TimesPenalized = 0,
                    IsSatisfied = false
                };

                int maxAwardLimit = rule.AwardLimit > 0 ? rule.AwardLimit : 1;
                // Penalties are capped at rule's award limit to prevent duplicate penalty deductions
                int maxPenaltyLimit = maxAwardLimit;

                foreach (var evt in eventList)
                {
                    if (evt == null) continue;

                    // 1. Positive Award Evaluation
                    if (ruleResult.TimesAwarded < maxAwardLimit && rule.MatchesAwardEvent(evt))
                    {
                        string awardKey = $"{rule.RuleId}_{evt.EventId}";
                        if (string.IsNullOrEmpty(evt.EventId) || processedAwardKeys.Add(awardKey))
                        {
                            ruleResult.TimesAwarded++;
                            ruleResult.PointsAwarded += rule.Points;
                            ruleResult.IsSatisfied = true;
                            ruleResult.MatchedEvents.Add(evt);
                        }
                    }

                    // 2. Penalty Evaluation
                    if (rule.PenaltyPoints > 0f && ruleResult.TimesPenalized < maxPenaltyLimit && rule.MatchesPenaltyEvent(evt))
                    {
                        string penaltyKey = $"{rule.RuleId}_{evt.EventId}";
                        if (string.IsNullOrEmpty(evt.EventId) || processedPenaltyKeys.Add(penaltyKey))
                        {
                            ruleResult.TimesPenalized++;
                            ruleResult.PenaltyDeducted += rule.PenaltyPoints;
                            ruleResult.PenaltyEvents.Add(evt);
                        }
                    }
                }

                // Check required rule compliance
                if (rule.Required && !ruleResult.IsSatisfied)
                {
                    allRequiredSatisfied = false;
                }

                // Format diagnostic detail
                if (ruleResult.IsSatisfied && ruleResult.PenaltyDeducted == 0f)
                {
                    ruleResult.Details = $"Rule '{rule.RuleId}' satisfied: +{ruleResult.PointsAwarded:0.00} pts.";
                }
                else if (ruleResult.IsSatisfied && ruleResult.PenaltyDeducted > 0f)
                {
                    ruleResult.Details = $"Rule '{rule.RuleId}' completed with penalty: +{ruleResult.PointsAwarded:0.00} pts, -{ruleResult.PenaltyDeducted:0.00} pts.";
                }
                else if (ruleResult.PenaltyDeducted > 0f)
                {
                    ruleResult.Details = $"Rule '{rule.RuleId}' failed with penalty: -{ruleResult.PenaltyDeducted:0.00} pts.";
                }
                else
                {
                    ruleResult.Details = $"Rule '{rule.RuleId}' not completed: 0.00 pts.";
                }

                totalAwarded += ruleResult.PointsAwarded;
                totalPenalties += ruleResult.PenaltyDeducted;
                result.RuleResults.Add(ruleResult);
            }

            result.TotalAwarded = (float)Math.Round(totalAwarded, 2);
            result.TotalPenalties = (float)Math.Round(totalPenalties, 2);

            // Calculate net score clamped strictly between 0.00 and MaxScore
            float rawScore = totalAwarded - totalPenalties;
            float clampedScore = Math.Max(0.00f, Math.Min(result.MaxScore, rawScore));
            result.ClientScore = (float)Math.Round(clampedScore, 2);

            // Pass requirement: score percent >= pass_percent AND all required rules satisfied
            float scorePercent = result.MaxScore > 0f ? (result.ClientScore / result.MaxScore) * 100.00f : 0f;
            result.Passed = (scorePercent >= result.PassPercent) && allRequiredSatisfied;

            return result;
        }

        /// <summary>
        /// Evaluates a training session and generates a finalized, schema-compliant TrainingAttempt.
        /// </summary>
        public static TrainingAttempt EvaluateAttempt(
            IEnumerable<TrainingEvent> events,
            RubricDefinition rubric,
            string workerId = null,
            string clientAttemptId = null,
            string startedAt = null)
        {
            var eval = Evaluate(events, rubric);

            var attempt = new TrainingAttempt
            {
                SchemaVersion = rubric?.SchemaVersion ?? "1.0.0",
                ClientAttemptId = !string.IsNullOrEmpty(clientAttemptId) ? clientAttemptId : Guid.NewGuid().ToString(),
                WorkerId = !string.IsNullOrEmpty(workerId) ? workerId : Guid.Empty.ToString(),
                ModuleId = rubric?.ModuleId ?? "fire-explosion-response",
                ContentVersion = rubric?.ContentVersion ?? "1.0.0",
                StartedAt = !string.IsNullOrEmpty(startedAt) ? startedAt : DateTime.UtcNow.ToString("o"),
                CompletedAt = DateTime.UtcNow.ToString("o"),
                Status = TrainingAttempt.StatusCompleted,
                ClientScore = eval.ClientScore,
                Passed = eval.Passed,
                Events = events != null ? new List<TrainingEvent>(events) : new List<TrainingEvent>()
            };

            eval.Attempt = attempt;
            return attempt;
        }
    }
}
