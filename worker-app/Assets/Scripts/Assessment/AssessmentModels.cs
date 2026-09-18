// AssessmentModels.cs
// Namespace : IndustrialSafetyAR.Assessment
//
// Pure C# domain models for offline evaluation, rubric rules, and attempt persistence.
// Strictly matches docs/contracts/attempt.schema.json and docs/contracts/rubric.schema.json.
// Completely decoupled from UnityEngine for isolated unit testing and cross-platform verification.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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
        /// Parses a RubricDefinition from a raw JSON string adhering to docs/contracts/rubric.schema.json.
        /// Pure C# standard library implementation decoupled from UnityEngine.
        /// </summary>
        public static RubricDefinition FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Rubric JSON string cannot be null or empty.", nameof(json));

            object parsed = SimpleJsonParser.Parse(json);
            if (!(parsed is Dictionary<string, object> dict))
                throw new FormatException("Rubric JSON root must be an object.");

            var rubric = new RubricDefinition
            {
                SchemaVersion = GetString(dict, "schema_version", "1.0.0"),
                ModuleId = GetString(dict, "module_id", null),
                ContentVersion = GetString(dict, "content_version", "1.0.0"),
                PassPercent = (float)GetDouble(dict, "pass_percent", 70.0),
                MaxScore = (float)GetDouble(dict, "max_score", 100.0)
            };

            if (dict.TryGetValue("rules", out var rulesObj) && rulesObj is List<object> rulesList)
            {
                foreach (var item in rulesList)
                {
                    if (item is Dictionary<string, object> ruleDict)
                    {
                        var rule = ParseRule(ruleDict);
                        if (rule != null)
                        {
                            rubric.Rules.Add(rule);
                        }
                    }
                }
            }

            return rubric;
        }

        private static RubricRule ParseRule(Dictionary<string, object> dict)
        {
            var rule = new RubricRule
            {
                RuleId = GetString(dict, "rule_id", null),
                StepId = GetString(dict, "step_id", null),
                Required = GetBool(dict, "required", true),
                EventType = GetString(dict, "event_type", null),
                Points = (float)GetDouble(dict, "points", 0.0),
                AwardLimit = (int)GetDouble(dict, "award_limit", 1.0),
                PenaltyPoints = (float)GetDouble(dict, "penalty_points", 0.0),
                PenaltyEventType = GetString(dict, "penalty_event_type", null)
            };

            if (dict.TryGetValue("match", out var matchObj) && matchObj is Dictionary<string, object> matchDict)
            {
                rule.Match = ParseMatchCriteria(matchDict);
            }

            if (dict.TryGetValue("penalty_match", out var penaltyMatchObj) && penaltyMatchObj is Dictionary<string, object> pMatchDict)
            {
                rule.PenaltyMatch = ParseMatchCriteria(pMatchDict);
            }

            return rule;
        }

        private static RuleMatchCriteria ParseMatchCriteria(Dictionary<string, object> dict)
        {
            var criteria = new RuleMatchCriteria
            {
                ActionId = GetString(dict, "action_id", null),
                TargetId = GetString(dict, "target_id", null),
                DecisionId = GetString(dict, "decision_id", null),
                Outcome = GetString(dict, "outcome", null)
            };

            if (dict.TryGetValue("boolean_value", out var boolVal) && boolVal is bool b)
            {
                criteria.BooleanValue = b;
            }

            if (dict.TryGetValue("selection_ids", out var selObj) && selObj is List<object> selList)
            {
                criteria.SelectionIds = new List<string>();
                foreach (var s in selList) if (s != null) criteria.SelectionIds.Add(s.ToString());
            }

            if (dict.TryGetValue("ordered_ids", out var ordObj) && ordObj is List<object> ordList)
            {
                criteria.OrderedIds = new List<string>();
                foreach (var o in ordList) if (o != null) criteria.OrderedIds.Add(o.ToString());
            }

            return criteria;
        }

        private static string GetString(Dictionary<string, object> dict, string key, string fallback = null)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
                return val.ToString();
            return fallback;
        }

        private static double GetDouble(Dictionary<string, object> dict, string key, double fallback = 0.0)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (val is double d) return d;
                if (val is int i) return i;
                if (val is long l) return l;
                if (double.TryParse(val.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
                    return parsed;
            }
            return fallback;
        }

        private static bool GetBool(Dictionary<string, object> dict, string key, bool fallback = false)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (val is bool b) return b;
                if (bool.TryParse(val.ToString(), out bool parsed)) return parsed;
            }
            return fallback;
        }

        /// <summary>
        /// Factory returning the Fire &amp; Explosion Response scoring rubric loaded from bundled assets.
        /// </summary>
        public static RubricDefinition CreateFireExplosionRubric()
        {
            return RubricLoader.LoadFireExplosionRubric();
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

            if (!string.IsNullOrEmpty(StepId) && !string.Equals(evt.StepId, StepId, StringComparison.OrdinalIgnoreCase))
                return false;

            bool eventTypeMatches = string.Equals(evt.EventType, EventType, StringComparison.OrdinalIgnoreCase)
                || (string.Equals(EventType, "extinguisher_used", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(evt.EventType, "procedure_completed", StringComparison.OrdinalIgnoreCase));

            if (!eventTypeMatches)
                return false;

            return Match == null || Match.Matches(evt);
        }

        /// <summary>
        /// Evaluates whether an event qualifies as a penalty under this rule.
        /// </summary>
        public bool MatchesPenaltyEvent(TrainingEvent evt)
        {
            if (evt == null || PenaltyPoints <= 0f) return false;

            if (!string.IsNullOrEmpty(StepId) && !string.Equals(evt.StepId, StepId, StringComparison.OrdinalIgnoreCase))
                return false;

            string penaltyType = !string.IsNullOrEmpty(PenaltyEventType) ? PenaltyEventType : EventType;
            bool eventTypeMatches = string.Equals(evt.EventType, penaltyType, StringComparison.OrdinalIgnoreCase)
                || (string.Equals(penaltyType, "extinguisher_used", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(evt.EventType, "procedure_failed", StringComparison.OrdinalIgnoreCase));

            if (!eventTypeMatches)
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
                bool actionMatches = string.Equals(act, ActionId, StringComparison.OrdinalIgnoreCase)
                    || (string.Equals(ActionId, "pass_procedure_completed", StringComparison.OrdinalIgnoreCase) &&
                        (string.Equals(act, "sweep", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(evt.TargetId, "extinguisher_procedure", StringComparison.OrdinalIgnoreCase)));

                if (!actionMatches)
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

    /// <summary>
    /// Complete evaluation report produced by LocalAssessmentEngine.
    /// </summary>
    [Serializable]
    public class AssessmentResult
    {
        public float ClientScore { get; set; }
        public float MaxScore { get; set; } = 100.00f;
        public float PassPercent { get; set; } = 70.00f;
        public bool Passed { get; set; }
        public float TotalAwarded { get; set; }
        public float TotalPenalties { get; set; }
        public List<RuleEvaluationResult> RuleResults { get; set; } = new List<RuleEvaluationResult>();
        public TrainingAttempt Attempt { get; set; }

        public RuleEvaluationResult GetRuleResult(string ruleId)
        {
            if (RuleResults == null) return null;
            return RuleResults.Find(r => string.Equals(r.RuleId, ruleId, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Step-by-step score line item for worker assessment summary display.
    /// Pure C# model with no UnityEngine dependency.
    /// </summary>
    [Serializable]
    public class StepScoreSummary
    {
        public int StepNumber { get; set; }
        public string StepId { get; set; }
        public string RuleId { get; set; }
        public string Title { get; set; }
        public float MaxPoints { get; set; }
        public float PointsAwarded { get; set; }
        public float PenaltyDeducted { get; set; }
        public float NetScore => (float)Math.Round(PointsAwarded - PenaltyDeducted, 2);
        public bool IsSatisfied { get; set; }
        public string StatusText { get; set; }
        public string Details { get; set; }
    }

    /// <summary>
    /// Presentation model providing pre-formatted data for the Assessment Summary UI.
    /// Decoupled from UnityEngine so formatting and calculation can be verified in unit tests.
    /// </summary>
    [Serializable]
    public class AssessmentSummaryViewModel
    {
        public const string DefaultModuleTitle = "Fire & Explosion Response";
        public const string StatusPassText = "PASS";
        public const string StatusFailText = "FAILED — RETAKE REQUIRED";
        public const string ColorPassHex = "#2ECC71"; // Emerald Green
        public const string ColorFailHex = "#E74C3C"; // Crimson Red

        public string ModuleTitle { get; set; } = DefaultModuleTitle;
        public string ModuleId { get; set; } = "fire-explosion-response";
        public string ContentVersion { get; set; } = "1.0.0";
        public string ClientAttemptId { get; set; }
        public string WorkerId { get; set; }
        public string StartedAt { get; set; }
        public string CompletedAt { get; set; }
        public string DurationText { get; set; } = "--:--";
        public double DurationSeconds { get; set; }
        public float ClientScore { get; set; }
        public float MaxScore { get; set; } = 100.00f;
        public float PassPercent { get; set; } = 70.00f;
        public bool Passed { get; set; }
        public string ScoreDisplayText => $"{ClientScore:0.00} / {MaxScore:0}";
        public string PassFailBadgeText => Passed ? StatusPassText : StatusFailText;
        public string PassFailColorHex => Passed ? ColorPassHex : ColorFailHex;
        public List<StepScoreSummary> StepSummaries { get; set; } = new List<StepScoreSummary>();
        public List<string> Penalties { get; set; } = new List<string>();
        public string SafetyFeedback { get; set; }
        public bool SyncPrepared { get; set; }

        public static string FormatDuration(string startedAt, string completedAt, out double totalSeconds)
        {
            totalSeconds = 0;
            if (!string.IsNullOrEmpty(startedAt) && !string.IsNullOrEmpty(completedAt))
            {
                if (DateTime.TryParse(startedAt, out var start) && DateTime.TryParse(completedAt, out var end))
                {
                    var span = end - start;
                    if (span.TotalSeconds < 0) span = TimeSpan.Zero;
                    totalSeconds = span.TotalSeconds;
                    int minutes = (int)span.TotalMinutes;
                    int seconds = span.Seconds;
                    if (minutes > 0)
                    {
                        return $"{minutes:D2}m {seconds:D2}s";
                    }
                    return $"{seconds}s";
                }
            }
            return "--:--";
        }

        public static AssessmentSummaryViewModel Build(TrainingAttempt attempt, AssessmentResult assessment)
        {
            var vm = new AssessmentSummaryViewModel();

            if (attempt != null)
            {
                vm.ClientAttemptId = attempt.ClientAttemptId;
                vm.WorkerId = attempt.WorkerId;
                vm.ModuleId = !string.IsNullOrEmpty(attempt.ModuleId) ? attempt.ModuleId : "fire-explosion-response";
                vm.ContentVersion = attempt.ContentVersion ?? "1.0.0";
                vm.StartedAt = attempt.StartedAt;
                vm.CompletedAt = attempt.CompletedAt;
                vm.DurationText = FormatDuration(attempt.StartedAt, attempt.CompletedAt, out double secs);
                vm.DurationSeconds = secs;
            }

            if (assessment != null)
            {
                vm.ClientScore = assessment.ClientScore;
                vm.MaxScore = assessment.MaxScore;
                vm.PassPercent = assessment.PassPercent;
                vm.Passed = assessment.Passed;

                // Step 1 to 9 rule mappings
                var stepDefs = new (int num, string ruleId, string stepId, string title, float maxPts)[]
                {
                    (1, "rule_detect_hazard", "step_detect_hazard", "Step 1: Detect Hazard", 5f),
                    (2, "rule_identify_hazard", "step_identify_hazard", "Step 2: Identify Hazard", 15f),
                    (3, "rule_raise_alarm", "step_raise_alarm", "Step 3: Raise Alarm", 15f),
                    (4, "rule_select_extinguisher", "step_select_extinguisher", "Step 4: Select Extinguisher", 15f),
                    (5, "rule_maintain_distance", "step_maintain_distance", "Step 5: Maintain Safe Distance", 10f),
                    (6, "rule_use_extinguisher", "step_use_extinguisher", "Step 6: PASS Extinguisher Procedure", 15f),
                    (7, "rule_identify_exit", "step_identify_exit", "Step 7: Identify Emergency Exit", 10f),
                    (8, "rule_evacuate_route", "step_evacuate_route", "Step 8: Evacuate Designated Route", 10f),
                    (9, "rule_reach_assembly", "step_reach_assembly", "Step 9: Reach Assembly Point", 5f),
                };

                foreach (var def in stepDefs)
                {
                    var ruleRes = assessment.GetRuleResult(def.ruleId);
                    float awarded = ruleRes != null ? ruleRes.PointsAwarded : 0f;
                    float penalty = ruleRes != null ? ruleRes.PenaltyDeducted : 0f;
                    bool satisfied = ruleRes != null && ruleRes.IsSatisfied;

                    string status = satisfied ? (penalty > 0 ? "PENALIZED" : "PASSED") : "INCOMPLETE";

                    vm.StepSummaries.Add(new StepScoreSummary
                    {
                        StepNumber = def.num,
                        RuleId = def.ruleId,
                        StepId = def.stepId,
                        Title = def.title,
                        MaxPoints = def.maxPts,
                        PointsAwarded = awarded,
                        PenaltyDeducted = penalty,
                        IsSatisfied = satisfied,
                        StatusText = status,
                        Details = ruleRes?.Details ?? string.Empty
                    });

                    if (penalty > 0)
                    {
                        string reason;
                        if (def.ruleId == "rule_evacuate_route")
                        {
                            reason = "Evacuation route penalty: Unsafe route selected before reaching the safe route.";
                        }
                        else if (def.ruleId == "rule_identify_hazard")
                        {
                            reason = "Hazard identification penalty: Incorrect classification selected before Class E electrical confirmation.";
                        }
                        else if (def.ruleId == "rule_select_extinguisher")
                        {
                            reason = "Extinguisher selection penalty: Inappropriate extinguisher type selected before CO2.";
                        }
                        else if (def.ruleId == "rule_use_extinguisher")
                        {
                            reason = "P.A.S.S. procedure penalty: Out-of-order action attempted during extinguisher discharge sequence.";
                        }
                        else
                        {
                            reason = ruleRes?.Details ?? "Procedure penalty deduction incurred.";
                        }

                        vm.Penalties.Add($"{def.title}: (-{penalty:0.00} pts) {reason}");
                    }
                }
            }

            // Generate safety feedback
            if (vm.Passed)
            {
                if (vm.Penalties.Count == 0 && vm.ClientScore >= 100f)
                {
                    vm.SafetyFeedback = "Flawless emergency performance! All 9 industrial fire response protocols were executed with 100% compliance.";
                }
                else
                {
                    vm.SafetyFeedback = $"Certified Compliant ({vm.ClientScore:0.00}%). Note: Review the {vm.Penalties.Count} penalty area(s) to maintain zero-incident standard.";
                }
            }
            else
            {
                vm.SafetyFeedback = $"Standard Not Met ({vm.ClientScore:0.00}% < {vm.PassPercent:0}% threshold). Retake training to master safety compliance before field authorization.";
            }

            return vm;
        }
    }

    /// <summary>
    /// Contract for loading training module scoring rubrics from bundled or external assets.
    /// Pure C# interface decoupled from UnityEngine.
    /// </summary>
    public interface IRubricProvider
    {
        RubricDefinition LoadRubric(string moduleId = RubricLoader.FireModuleId);
    }

    /// <summary>
    /// Clean runtime loader and resolver for module rubrics.
    /// Handles disk loading from bundled assets and provides an offline-safe fallback
    /// with zero network requirements and zero UnityEngine dependencies.
    /// </summary>
    public static class RubricLoader
    {
        public const string FireModuleId = "fire-explosion-response";

        /// <summary>
        /// Optional override path for testing or dynamic loading.
        /// </summary>
        public static string CustomRubricPath { get; set; }

        /// <summary>
        /// Loads the Fire &amp; Explosion rubric from local bundled assets.
        /// </summary>
        public static RubricDefinition LoadFireExplosionRubric()
        {
            return LoadRubric(FireModuleId);
        }

        /// <summary>
        /// Loads a rubric by module identifier from local bundled assets.
        /// </summary>
        public static RubricDefinition LoadRubric(string moduleId = FireModuleId)
        {
            string filePath = ResolveRubricFilePath(moduleId);
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    return RubricDefinition.FromJson(json);
                }
                catch
                {
                    // Fall back to bundled JSON on disk read failure
                }
            }

            if (string.Equals(moduleId, FireModuleId, StringComparison.OrdinalIgnoreCase))
            {
                return RubricDefinition.FromJson(BundledFireRubricJson);
            }

            throw new FileNotFoundException($"Could not load rubric for module '{moduleId}' from assets or bundled fallback.");
        }

        /// <summary>
        /// Loads a rubric directly from an explicit file path.
        /// </summary>
        public static RubricDefinition LoadFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Rubric file not found at: {filePath}", filePath);

            string json = File.ReadAllText(filePath);
            return RubricDefinition.FromJson(json);
        }

        /// <summary>
        /// Resolves the filesystem path to the bundled rubric file for a given module.
        /// Searches project root, current directory, AppDomain base, and parent directories.
        /// </summary>
        public static string ResolveRubricFilePath(string moduleId = FireModuleId)
        {
            if (!string.IsNullOrEmpty(CustomRubricPath) && File.Exists(CustomRubricPath))
            {
                return CustomRubricPath;
            }

            string relPath1 = Path.Combine("Assets", "Content", "Modules", moduleId, "rubric.json");
            string relPath2 = Path.Combine("worker-app", "Assets", "Content", "Modules", moduleId, "rubric.json");

            // Check current working directory
            string cwd = Directory.GetCurrentDirectory();
            if (!string.IsNullOrEmpty(cwd))
            {
                string p1 = Path.Combine(cwd, relPath1);
                if (File.Exists(p1)) return p1;
                string p2 = Path.Combine(cwd, relPath2);
                if (File.Exists(p2)) return p2;

                var dir = new DirectoryInfo(cwd);
                for (int i = 0; i < 6 && dir != null; i++)
                {
                    string check1 = Path.Combine(dir.FullName, relPath1);
                    if (File.Exists(check1)) return check1;
                    string check2 = Path.Combine(dir.FullName, relPath2);
                    if (File.Exists(check2)) return check2;
                    dir = dir.Parent;
                }
            }

            // Check AppDomain BaseDirectory
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
            {
                string b1 = Path.Combine(baseDir, relPath1);
                if (File.Exists(b1)) return b1;
                string b2 = Path.Combine(baseDir, relPath2);
                if (File.Exists(b2)) return b2;

                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 6 && dir != null; i++)
                {
                    string check1 = Path.Combine(dir.FullName, relPath1);
                    if (File.Exists(check1)) return check1;
                    string check2 = Path.Combine(dir.FullName, relPath2);
                    if (File.Exists(check2)) return check2;
                    dir = dir.Parent;
                }
            }

            return null;
        }

        /// <summary>
        /// Offline-bundled exact copy of worker-app/Assets/Content/Modules/fire-explosion-response/rubric.json.
        /// Guarantees offline availability when filesystem access is restricted (e.g. mobile player sandbox).
        /// </summary>
        public const string BundledFireRubricJson = @"{
  ""schema_version"": ""1.0.0"",
  ""module_id"": ""fire-explosion-response"",
  ""content_version"": ""1.0.0"",
  ""pass_percent"": 70.00,
  ""max_score"": 100.00,
  ""rules"": [
    {
      ""rule_id"": ""rule_detect_hazard"",
      ""step_id"": ""step_detect_hazard"",
      ""required"": true,
      ""event_type"": ""step_completed"",
      ""match"": {
        ""action_id"": ""detect_hazard_acknowledged"",
        ""outcome"": ""success""
      },
      ""points"": 5.00,
      ""award_limit"": 1,
      ""penalty_points"": 0.00
    },
    {
      ""rule_id"": ""rule_identify_hazard"",
      ""step_id"": ""step_identify_hazard"",
      ""required"": true,
      ""event_type"": ""hazard_identified"",
      ""match"": {
        ""target_id"": ""hazard_electrical_conveyor_fire"",
        ""outcome"": ""success""
      },
      ""points"": 15.00,
      ""award_limit"": 1,
      ""penalty_points"": 5.00,
      ""penalty_event_type"": ""hazard_identified"",
      ""penalty_match"": {
        ""outcome"": ""failure""
      }
    },
    {
      ""rule_id"": ""rule_raise_alarm"",
      ""step_id"": ""step_raise_alarm"",
      ""required"": true,
      ""event_type"": ""alarm_raised"",
      ""match"": {
        ""action_id"": ""manual_call_point_activated"",
        ""outcome"": ""success""
      },
      ""points"": 15.00,
      ""award_limit"": 1,
      ""penalty_points"": 0.00
    },
    {
      ""rule_id"": ""rule_select_extinguisher"",
      ""step_id"": ""step_select_extinguisher"",
      ""required"": true,
      ""event_type"": ""extinguisher_selected"",
      ""match"": {
        ""target_id"": ""extinguisher_co2"",
        ""outcome"": ""success""
      },
      ""points"": 15.00,
      ""award_limit"": 1,
      ""penalty_points"": 5.00,
      ""penalty_event_type"": ""extinguisher_selected"",
      ""penalty_match"": {
        ""outcome"": ""failure""
      }
    },
    {
      ""rule_id"": ""rule_maintain_distance"",
      ""step_id"": ""step_maintain_distance"",
      ""required"": true,
      ""event_type"": ""decision_made"",
      ""match"": {
        ""decision_id"": ""standoff_distance_2m_maintained"",
        ""outcome"": ""success""
      },
      ""points"": 10.00,
      ""award_limit"": 1,
      ""penalty_points"": 0.00
    },
    {
      ""rule_id"": ""rule_use_extinguisher"",
      ""step_id"": ""step_use_extinguisher"",
      ""required"": true,
      ""event_type"": ""extinguisher_used"",
      ""match"": {
        ""action_id"": ""pass_procedure_completed"",
        ""outcome"": ""success""
      },
      ""points"": 15.00,
      ""award_limit"": 1,
      ""penalty_points"": 5.00,
      ""penalty_event_type"": ""extinguisher_used"",
      ""penalty_match"": {
        ""outcome"": ""failure""
      }
    },
    {
      ""rule_id"": ""rule_identify_exit"",
      ""step_id"": ""step_identify_exit"",
      ""required"": true,
      ""event_type"": ""exit_marked"",
      ""match"": {
        ""target_id"": ""exit_emergency_sector_b"",
        ""outcome"": ""success""
      },
      ""points"": 10.00,
      ""award_limit"": 1,
      ""penalty_points"": 0.00
    },
    {
      ""rule_id"": ""rule_evacuate_route"",
      ""step_id"": ""step_evacuate_route"",
      ""required"": true,
      ""event_type"": ""evacuation_sequence_submitted"",
      ""match"": {
        ""outcome"": ""success""
      },
      ""points"": 10.00,
      ""award_limit"": 1,
      ""penalty_points"": 5.00,
      ""penalty_event_type"": ""evacuation_sequence_submitted"",
      ""penalty_match"": {
        ""outcome"": ""failure""
      }
    },
    {
      ""rule_id"": ""rule_reach_assembly"",
      ""step_id"": ""step_reach_assembly"",
      ""required"": true,
      ""event_type"": ""assembly_reached"",
      ""match"": {
        ""target_id"": ""assembly_muster_point_alpha"",
        ""outcome"": ""success""
      },
      ""points"": 5.00,
      ""award_limit"": 1,
      ""penalty_points"": 0.00
    }
  ]
}";
    }

    /// <summary>
    /// Default implementation of IRubricProvider that loads bundled offline rubrics.
    /// </summary>
    public class BundledRubricProvider : IRubricProvider
    {
        public RubricDefinition LoadRubric(string moduleId = RubricLoader.FireModuleId)
        {
            return RubricLoader.LoadRubric(moduleId);
        }
    }

    /// <summary>
    /// Minimal, zero-dependency JSON parser for rubric definitions.
    /// Pure C# standard library only, completely independent of UnityEngine,
    /// Newtonsoft.Json, or System.Text.Json.
    /// </summary>
    internal static class SimpleJsonParser
    {
        public static object Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            int idx = 0;
            return ParseValue(json, ref idx);
        }

        private static void SkipWhitespace(string s, ref int idx)
        {
            while (idx < s.Length && char.IsWhiteSpace(s[idx]))
                idx++;
        }

        private static object ParseValue(string s, ref int idx)
        {
            SkipWhitespace(s, ref idx);
            if (idx >= s.Length) return null;

            char c = s[idx];
            if (c == '{') return ParseObject(s, ref idx);
            if (c == '[') return ParseArray(s, ref idx);
            if (c == '"') return ParseString(s, ref idx);
            if (c == 't' || c == 'T' || c == 'f' || c == 'F') return ParseBool(s, ref idx);
            if (c == 'n' || c == 'N') return ParseNull(s, ref idx);
            if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber(s, ref idx);

            throw new FormatException($"Unexpected character '{c}' at position {idx}");
        }

        private static Dictionary<string, object> ParseObject(string s, ref int idx)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            idx++; // consume '{'

            while (idx < s.Length)
            {
                SkipWhitespace(s, ref idx);
                if (idx >= s.Length) throw new FormatException("Unterminated object in JSON");
                if (s[idx] == '}')
                {
                    idx++;
                    return dict;
                }

                if (s[idx] != '"')
                    throw new FormatException($"Expected string key at position {idx}, got '{s[idx]}'");

                string key = ParseString(s, ref idx);

                SkipWhitespace(s, ref idx);
                if (idx >= s.Length || s[idx] != ':')
                    throw new FormatException($"Expected ':' at position {idx}");
                idx++; // consume ':'

                object val = ParseValue(s, ref idx);
                dict[key] = val;

                SkipWhitespace(s, ref idx);
                if (idx >= s.Length) throw new FormatException("Unterminated object in JSON");
                if (s[idx] == ',')
                {
                    idx++;
                }
                else if (s[idx] == '}')
                {
                    idx++;
                    return dict;
                }
                else
                {
                    throw new FormatException($"Expected ',' or '}}' at position {idx}, got '{s[idx]}'");
                }
            }

            throw new FormatException("Unterminated object in JSON");
        }

        private static List<object> ParseArray(string s, ref int idx)
        {
            var list = new List<object>();
            idx++; // consume '['

            while (idx < s.Length)
            {
                SkipWhitespace(s, ref idx);
                if (idx >= s.Length) throw new FormatException("Unterminated array in JSON");
                if (s[idx] == ']')
                {
                    idx++;
                    return list;
                }

                object val = ParseValue(s, ref idx);
                list.Add(val);

                SkipWhitespace(s, ref idx);
                if (idx >= s.Length) throw new FormatException("Unterminated array in JSON");
                if (s[idx] == ',')
                {
                    idx++;
                }
                else if (s[idx] == ']')
                {
                    idx++;
                    return list;
                }
                else
                {
                    throw new FormatException($"Expected ',' or ']' at position {idx}, got '{s[idx]}'");
                }
            }

            throw new FormatException("Unterminated array in JSON");
        }

        private static string ParseString(string s, ref int idx)
        {
            idx++; // consume opening '"'
            var sb = new StringBuilder();

            while (idx < s.Length)
            {
                char c = s[idx++];
                if (c == '"')
                {
                    return sb.ToString();
                }
                if (c == '\\')
                {
                    if (idx >= s.Length) throw new FormatException("Incomplete escape sequence in JSON string");
                    char esc = s[idx++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (idx + 4 > s.Length) throw new FormatException("Incomplete unicode escape in JSON string");
                            string hex = s.Substring(idx, 4);
                            sb.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            idx += 4;
                            break;
                        default:
                            sb.Append(esc);
                            break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            throw new FormatException("Unterminated string in JSON");
        }

        private static bool ParseBool(string s, ref int idx)
        {
            if (idx + 4 <= s.Length && string.Equals(s.Substring(idx, 4), "true", StringComparison.OrdinalIgnoreCase))
            {
                idx += 4;
                return true;
            }
            if (idx + 5 <= s.Length && string.Equals(s.Substring(idx, 5), "false", StringComparison.OrdinalIgnoreCase))
            {
                idx += 5;
                return false;
            }
            throw new FormatException($"Invalid boolean at position {idx}");
        }

        private static object ParseNull(string s, ref int idx)
        {
            if (idx + 4 <= s.Length && string.Equals(s.Substring(idx, 4), "null", StringComparison.OrdinalIgnoreCase))
            {
                idx += 4;
                return null;
            }
            throw new FormatException($"Invalid null token at position {idx}");
        }

        private static double ParseNumber(string s, ref int idx)
        {
            int start = idx;
            if (s[idx] == '-') idx++;
            while (idx < s.Length && ((s[idx] >= '0' && s[idx] <= '9') || s[idx] == '.' || s[idx] == 'e' || s[idx] == 'E' || s[idx] == '+' || s[idx] == '-'))
            {
                if ((s[idx] == '+' || s[idx] == '-') && idx > start && s[idx - 1] != 'e' && s[idx - 1] != 'E')
                    break;
                idx++;
            }

            string numStr = s.Substring(start, idx - start);
            if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            throw new FormatException($"Invalid number '{numStr}' at position {start}");
        }
    }
}
