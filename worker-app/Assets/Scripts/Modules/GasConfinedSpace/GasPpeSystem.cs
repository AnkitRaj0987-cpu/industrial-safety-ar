// GasPpeSystem.cs
// Namespace : IndustrialSafetyAR.Modules.GasConfinedSpace
//
// Pure C# PPE selection and verification system for Confined Space Safety.
// Decoupled from Unity runtime so it can be instantiated, verified, and unit-tested in isolation.
//
// SAFETY PRINCIPLE:
// PPE is vital equipment for emergency egress and hazardous space entry preparation, but:
// "PPE DOES NOT MAKE AN UNSAFE ATMOSPHERE SAFE."
// Selection and verification must be rigorous; dust masks/surgical masks are strictly rejected.

using System;
using System.Collections.Generic;
using System.Linq;
using IndustrialSafetyAR.Core.Events;

namespace IndustrialSafetyAR.Modules.GasConfinedSpace
{
    public class GasPpeItem
    {
        public string ItemId { get; set; }
        public string DisplayName { get; set; }
        public bool IsRequiredForScenario { get; set; }
        public bool IsDangerousDistractor { get; set; }
        public string RejectionReason { get; set; }
    }

    public class GasPpeSystem
    {
        public const string StepSelectPpe = "step_gas_select_ppe";
        public const string RuleSelectPpe = "rule_select_ppe";

        public const string StepVerifyPpe = "step_gas_verify_ppe";
        public const string RuleVerifyPpe = "rule_verify_ppe";

        // Standard PPE identifiers
        public const string ItemHelmet = "ppe_helmet";
        public const string ItemHarness = "ppe_harness";
        public const string ItemGloves = "ppe_gloves";
        public const string ItemBoots = "ppe_boots";
        public const string ItemScba = "ppe_scba";
        public const string ItemDustMask = "ppe_dust_mask";
        public const string ItemClothMask = "ppe_cloth_mask";

        private readonly Dictionary<string, GasPpeItem> _palette = new Dictionary<string, GasPpeItem>(StringComparer.OrdinalIgnoreCase)
        {
            {
                ItemHelmet,
                new GasPpeItem
                {
                    ItemId = ItemHelmet,
                    DisplayName = "Industrial Safety Helmet (Hard Hat)",
                    IsRequiredForScenario = true,
                    IsDangerousDistractor = false
                }
            },
            {
                ItemHarness,
                new GasPpeItem
                {
                    ItemId = ItemHarness,
                    DisplayName = "Full-Body Retrieval Harness (Confined Space)",
                    IsRequiredForScenario = true,
                    IsDangerousDistractor = false
                }
            },
            {
                ItemGloves,
                new GasPpeItem
                {
                    ItemId = ItemGloves,
                    DisplayName = "Chemical & Abrasion Resistant Safety Gloves",
                    IsRequiredForScenario = true,
                    IsDangerousDistractor = false
                }
            },
            {
                ItemBoots,
                new GasPpeItem
                {
                    ItemId = ItemBoots,
                    DisplayName = "Steel-Toe Anti-Static Safety Boots",
                    IsRequiredForScenario = true,
                    IsDangerousDistractor = false
                }
            },
            {
                ItemScba,
                new GasPpeItem
                {
                    ItemId = ItemScba,
                    DisplayName = "Self-Contained Breathing Apparatus (SCBA)",
                    IsRequiredForScenario = true,
                    IsDangerousDistractor = false
                }
            },
            {
                ItemDustMask,
                new GasPpeItem
                {
                    ItemId = ItemDustMask,
                    DisplayName = "Particulate Dust Mask",
                    IsRequiredForScenario = false,
                    IsDangerousDistractor = true,
                    RejectionReason = "CRITICAL PPE ERROR: Dust masks provide ZERO protection against toxic gases or oxygen deficiency!"
                }
            },
            {
                ItemClothMask,
                new GasPpeItem
                {
                    ItemId = ItemClothMask,
                    DisplayName = "Standard Cloth / Surgical Mask",
                    IsRequiredForScenario = false,
                    IsDangerousDistractor = true,
                    RejectionReason = "CRITICAL PPE ERROR: Surgical masks provide no gas filtration or oxygen supply!"
                }
            }
        };

        private readonly HashSet<string> _selectedItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool IsPpeSelected { get; private set; }
        public bool IsPpeVerified { get; private set; }

        public IReadOnlyCollection<string> SelectedItems => _selectedItems;
        public IReadOnlyCollection<string> RequiredItemIds => _palette.Values.Where(v => v.IsRequiredForScenario).Select(v => v.ItemId).ToList();

        /// <summary>
        /// Pure predicate checking if the current candidate PPE selection satisfies all requirements
        /// and does not contain any dangerous distractors.
        /// </summary>
        public bool HasValidSelection(IEnumerable<string> itemIds)
        {
            if (itemIds == null) return false;
            var selectionList = itemIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var id in selectionList)
            {
                if (_palette.TryGetValue(id, out var item) && item.IsDangerousDistractor)
                {
                    return false;
                }
            }
            var required = RequiredItemIds;
            return !required.Any(req => !selectionList.Contains(req, StringComparer.OrdinalIgnoreCase));
        }

        public bool SubmitPpeSelection(IEnumerable<string> itemIds, string moduleId, string contentVersion, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent, out string feedback)
        {
            emittedEvent = null;
            feedback = null;

            if (itemIds == null)
            {
                feedback = "No PPE items selected.";
                return false;
            }

            var selectionList = itemIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Check if any dangerous distractor was included
            foreach (var id in selectionList)
            {
                if (_palette.TryGetValue(id, out var item) && item.IsDangerousDistractor)
                {
                    feedback = item.RejectionReason ?? "Inappropriate PPE item selected for hazardous gas scenario.";
                    emittedEvent = new TrainingEvent
                    {
                        ModuleId = moduleId,
                        ContentVersion = contentVersion,
                        StepId = StepSelectPpe,
                        EventType = "ppe_selection_incorrect",
                        ActionId = "select_ppe",
                        TargetId = id,
                        Outcome = "failure",
                        Payload =
                        {
                            { "rule_id", RuleSelectPpe },
                            { "action_id", "select_ppe" },
                            { "target_id", id },
                            { "error_reason", feedback },
                            { "outcome", "failure" }
                        }
                    };
                    dispatcher?.Dispatch(emittedEvent);
                    return false;
                }
            }

            // Verify all required items are present
            var required = RequiredItemIds;
            bool missingAny = required.Any(req => !selectionList.Contains(req, StringComparer.OrdinalIgnoreCase));
            if (missingAny)
            {
                feedback = "Incomplete PPE selection: All required items (Helmet, Harness, Gloves, Boots, SCBA) must be selected.";
                return false;
            }

            _selectedItems.Clear();
            foreach (var id in selectionList)
            {
                _selectedItems.Add(id);
            }

            IsPpeSelected = true;
            feedback = "✓ Complete PPE selected: Helmet, Harness, Gloves, Boots, and SCBA confirmed.";

            emittedEvent = new TrainingEvent
            {
                ModuleId = moduleId,
                ContentVersion = contentVersion,
                StepId = StepSelectPpe,
                EventType = "ppe_selected",
                ActionId = "select_ppe",
                TargetId = "confined_space_ppe_kit",
                Outcome = "success",
                Payload =
                {
                    { "rule_id", RuleSelectPpe },
                    { "action_id", "select_ppe" },
                    { "selection_count", _selectedItems.Count.ToString() },
                    { "selected_items", string.Join(",", _selectedItems) },
                    { "outcome", "success" }
                }
            };

            dispatcher?.Dispatch(emittedEvent);
            return true;
        }

        public bool VerifyPpe(bool sealCheckPassed, bool harnessFitPassed, bool cylinderPressurePassed, string moduleId, string contentVersion, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent, out string feedback)
        {
            emittedEvent = null;
            feedback = null;

            if (!IsPpeSelected)
            {
                feedback = "PPE must be selected before verification can occur.";
                return false;
            }

            bool allPassed = sealCheckPassed && harnessFitPassed && cylinderPressurePassed;

            if (allPassed)
            {
                IsPpeVerified = true;
                feedback = "✓ PPE Verified: SCBA seal check, harness fit inspection, and air pressure confirmed.";

                emittedEvent = new TrainingEvent
                {
                    ModuleId = moduleId,
                    ContentVersion = contentVersion,
                    StepId = StepVerifyPpe,
                    EventType = "ppe_verified",
                    ActionId = "verify_ppe",
                    TargetId = "ppe_inspection_complete",
                    Outcome = "success",
                    Payload =
                    {
                        { "rule_id", RuleVerifyPpe },
                        { "action_id", "verify_ppe" },
                        { "verification_status", "verified" },
                        { "seal_check", "passed" },
                        { "harness_fit", "passed" },
                        { "cylinder_pressure", "passed" },
                        { "outcome", "success" }
                    }
                };

                dispatcher?.Dispatch(emittedEvent);
                return true;
            }
            else
            {
                feedback = "PPE verification check failed. SCBA seal, harness fit, and cylinder pressure must all pass inspection.";

                emittedEvent = new TrainingEvent
                {
                    ModuleId = moduleId,
                    ContentVersion = contentVersion,
                    StepId = StepVerifyPpe,
                    EventType = "ppe_verification_failed",
                    ActionId = "verify_ppe",
                    TargetId = "ppe_inspection_incomplete",
                    Outcome = "failure",
                    Payload =
                    {
                        { "rule_id", RuleVerifyPpe },
                        { "action_id", "verify_ppe" },
                        { "verification_status", "failed" },
                        { "seal_check", sealCheckPassed ? "passed" : "failed" },
                        { "harness_fit", harnessFitPassed ? "passed" : "failed" },
                        { "cylinder_pressure", cylinderPressurePassed ? "passed" : "failed" },
                        { "outcome", "failure" }
                    }
                };

                dispatcher?.Dispatch(emittedEvent);
                return false;
            }
        }

        public void Reset()
        {
            IsPpeSelected = false;
            IsPpeVerified = false;
            _selectedItems.Clear();
        }
    }
}
