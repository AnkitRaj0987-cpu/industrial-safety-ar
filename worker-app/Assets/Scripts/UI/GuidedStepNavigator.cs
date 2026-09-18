// GuidedStepNavigator.cs
// Namespace : IndustrialSafetyAR.UI
//
// Reusable step navigation and state gating controller for guided AR safety training.
// Enforces explicit Step X/9 display, step-by-step progress tracking, forward/backward navigation,
// step locking (Next button only available upon successful action), and swipe-rejection compliance.
// Designed to be decoupled from Unity runtime so it can be 100% unit tested and reused across
// Fire & Explosion Response and Gas & Toxic Vapors modules.

using System;
using System.Collections.Generic;

namespace IndustrialSafetyAR.UI
{
    /// <summary>
    /// Configuration data for a single step in a guided training workflow.
    /// </summary>
    public class GuidedStepInfo
    {
        public int StepNumber { get; set; }
        public string StepId { get; set; }
        public string Title { get; set; }
        public string Instruction { get; set; }
        public string NextButtonLabel { get; set; }
        public string DefaultSuccessFeedback { get; set; }

        public GuidedStepInfo(int stepNumber, string stepId, string title, string instruction, string nextButtonLabel, string defaultSuccessFeedback = null)
        {
            StepNumber = stepNumber;
            StepId = stepId;
            Title = title;
            Instruction = instruction;
            NextButtonLabel = nextButtonLabel;
            DefaultSuccessFeedback = defaultSuccessFeedback;
        }
    }

    /// <summary>
    /// Pure C# navigation controller enforcing explicit step state, progress indicators,
    /// Back/Next transitions, and step completion gating.
    /// </summary>
    public class GuidedStepNavigator
    {
        public const int DefaultTotalSteps = 9;

        private readonly List<GuidedStepInfo> _steps = new List<GuidedStepInfo>();
        private readonly bool[] _stepCompleted;
        private readonly string[] _stepSuccessFeedback;

        public int TotalSteps => _steps.Count > 0 ? _steps.Count : DefaultTotalSteps;
        public int CurrentStepIndex { get; private set; } = 1;
        public int HighestCompletedStep { get; private set; } = 0;

        public bool CanGoBack => CurrentStepIndex > 1;
        public bool CanGoNext => IsStepCompleted(CurrentStepIndex);
        public bool IsAtFinalStep => CurrentStepIndex == TotalSteps;

        public event Action<int> OnStepChanged;
        public event Action<int, string> OnStepCompleted;
        public event Action OnCompleteTrainingRequested;
        public event Action<string> OnFeedbackChanged;

        public GuidedStepNavigator(int totalSteps = DefaultTotalSteps)
        {
            int capacity = Math.Max(totalSteps, DefaultTotalSteps) + 1;
            _stepCompleted = new bool[capacity];
            _stepSuccessFeedback = new string[capacity];

            InitializeDefaultFireSteps();
        }

        /// <summary>
        /// Populates the standard 9-step Fire &amp; Explosion Response curriculum.
        /// </summary>
        public void InitializeDefaultFireSteps()
        {
            _steps.Clear();
            _steps.Add(new GuidedStepInfo(1, "step_detect_hazard", "DETECT HAZARD",
                "Fire hazard located! Tap [ACKNOWLEDGE FIRE HAZARD →] below to confirm detection:",
                "NEXT: IDENTIFY HAZARD →", "✓ Hazard acknowledged. Next: Classify hazard."));

            _steps.Add(new GuidedStepInfo(2, "step_identify_hazard", "IDENTIFY HAZARD",
                "Hazard spotted! Tap [1. CLASS E: ELECTRICAL FIRE (480V) →] below:",
                "NEXT: RAISE ALARM →", "✓ Class E Electrical Hazard confirmed! Next: Raise alarm."));

            _steps.Add(new GuidedStepInfo(3, "step_raise_alarm", "RAISE ALARM",
                "Class E Hazard confirmed! Tap [ACTIVATE MANUAL CALL POINT (ALARM) →] below immediately:",
                "NEXT: SELECT EXTINGUISHER →", "✓ Emergency Alarm Activated! Siren sounding."));

            _steps.Add(new GuidedStepInfo(4, "step_select_extinguisher", "SELECT EXTINGUISHER",
                "Alarm active! Tap [1. CO2 EXTINGUISHER (CARBON DIOXIDE) →] below:",
                "NEXT: SAFE DISTANCE →", "✓ CO2 Extinguisher selected! Safe for electrical fires."));

            _steps.Add(new GuidedStepInfo(5, "step_maintain_distance", "SAFE DISTANCE",
                "Maintain safe distance! Tap [STAND AT SAFE DISTANCE (2.5m) →] below outside danger zone:",
                "NEXT: PASS PROCEDURE →", "✓ Safe 2.5m distance confirmed. Next: Begin PASS procedure."));

            _steps.Add(new GuidedStepInfo(6, "step_use_extinguisher", "PASS PROCEDURE",
                "Execute P.A.S.S. procedure! Tap [1. PULL SAFETY PIN →] below to begin:",
                "NEXT: EMERGENCY EXIT →", "✓ Fire extinguished! Conveyor fire suppressed."));

            _steps.Add(new GuidedStepInfo(7, "step_identify_exit", "EMERGENCY EXIT",
                "Fire suppressed! Tap [SECTOR B EMERGENCY EXIT →] below to mark designated exit:",
                "NEXT: EVACUATION ROUTE →", "✓ Emergency Exit Sector B verified! Next: Evacuate route."));

            _steps.Add(new GuidedStepInfo(8, "step_evacuate_route", "EVACUATION ROUTE",
                "Follow evacuation route! Tap [WAYPOINT 1: MAIN CORRIDOR →] below:",
                "NEXT: ASSEMBLY POINT →", "✓ Evacuation route completed safely! Next: Assemble at Muster Point Alpha."));

            _steps.Add(new GuidedStepInfo(9, "step_reach_assembly", "ASSEMBLY POINT",
                "Final safety action: Tap [REACH ASSEMBLY POINT (MUSTER ALPHA) →] below:",
                "VIEW ASSESSMENT →", "✓ Assembly Point reached! Worker accounted for."));
        }

        public string ModuleId { get; set; } = "fire-explosion-response";

        /// <summary>
        /// Populates the standard 9-step Gas Leak &amp; Confined Space curriculum.
        /// </summary>
        public void InitializeDefaultGasSteps()
        {
            ModuleId = "gas-confined-space";
            _steps.Clear();
            _steps.Add(new GuidedStepInfo(1, "step_gas_recognize_hazard", "RECOGNIZE HAZARD",
                "Confined space opening located! Tap [ACKNOWLEDGE GAS HAZARD →] below to confirm recognition:",
                "NEXT: DANGER ZONE →", "✓ Gas hazard acknowledged. Next: Mark danger zone."));

            _steps.Add(new GuidedStepInfo(2, "step_gas_danger_zone", "DANGER ZONE",
                "Hazard perimeter! Tap [MARK 3m DANGER PERIMETER →] below to establish boundary:",
                "NEXT: ATMOSPHERIC TEST →", "✓ Danger perimeter marked. Next: Atmospheric testing."));

            _steps.Add(new GuidedStepInfo(3, "step_gas_atmospheric_test", "ATMOSPHERIC TEST",
                "OSHA testing sequence: Tap [TEST ATMOSPHERE (O2 -> LEL -> H2S) →] below:",
                "NEXT: SELECT PPE →", "✓ Atmospheric test complete: UNSAFE conditions detected. Next: Select PPE."));

            _steps.Add(new GuidedStepInfo(4, "step_gas_select_ppe", "SELECT PPE",
                "Dangerous atmosphere detected! Tap [SELECT REQUIRED PPE KIT →] below:",
                "NEXT: VERIFY PPE →", "✓ PPE kit selected. Next: Inspect and verify PPE."));

            _steps.Add(new GuidedStepInfo(5, "step_gas_verify_ppe", "VERIFY PPE",
                "Verify PPE integrity! Tap [PERFORM FIT & SEAL CHECK →] below:",
                "NEXT: BUDDY SYSTEM →", "✓ PPE verified. Next: Establish buddy/attendant communication."));

            _steps.Add(new GuidedStepInfo(6, "step_gas_buddy_system", "BUDDY SYSTEM",
                "Outside attendant standby! Tap [ASSIGN ATTENDANT & TEST RADIO →] below:",
                "NEXT: ENTRY DECISION →", "✓ Attendant assigned outside. Next: Make entry decision."));

            _steps.Add(new GuidedStepInfo(7, "step_gas_entry_decision", "ENTRY DECISION",
                "Conditions UNSAFE! Tap [DO NOT ENTER (UNSAFE ATMOSPHERE) →] below:",
                "NEXT: EMERGENCY RESPONSE →", "✓ Safe decision: DO NOT ENTER. Next: Follow emergency response."));

            _steps.Add(new GuidedStepInfo(8, "step_gas_emergency_response", "EMERGENCY RESPONSE",
                "Gas alarm active! Tap [EVACUATE UPWIND & ALERT SUPERVISOR →] below:",
                "NEXT: SAFETY CHECK →", "✓ Emergency procedure completed. Next: Final safety check."));

            _steps.Add(new GuidedStepInfo(9, "step_gas_final_safety_check", "FINAL SAFETY CHECK",
                "Review compliance checklist: Tap [CONFIRM SAFETY CHECK & COMPLETE →] below:",
                "VIEW ASSESSMENT →", "✓ Confined space safety training completed."));
        }

        /// <summary>
        /// Allows configuring a custom step sequence (e.g. for Gas &amp; Toxic Vapors module).
        /// </summary>
        public void ConfigureSteps(IEnumerable<GuidedStepInfo> steps)
        {
            if (steps == null) return;
            _steps.Clear();
            _steps.AddRange(steps);
        }

        public GuidedStepInfo GetStepInfo(int stepNumber)
        {
            if (stepNumber >= 1 && stepNumber <= _steps.Count)
            {
                return _steps[stepNumber - 1];
            }
            return null;
        }

        public string GetStepTitle(int stepNumber)
        {
            var info = GetStepInfo(stepNumber);
            string fallback = info != null ? info.Title : $"STEP {stepNumber}";
            string prefix = string.Equals(ModuleId, "gas-confined-space", StringComparison.OrdinalIgnoreCase) ? "gas" : "fire";
            return IndustrialSafetyAR.Core.LocaleService.Instance.Get($"{prefix}_step{stepNumber}_title", fallback);
        }

        public string GetStepInstruction(int stepNumber)
        {
            var info = GetStepInfo(stepNumber);
            string fallback = info != null ? info.Instruction : string.Empty;
            string prefix = string.Equals(ModuleId, "gas-confined-space", StringComparison.OrdinalIgnoreCase) ? "gas" : "fire";
            return IndustrialSafetyAR.Core.LocaleService.Instance.Get($"{prefix}_step{stepNumber}_prompt", fallback);
        }

        public string GetNextLabel(int stepNumber)
        {
            var info = GetStepInfo(stepNumber);
            string fallback = info != null ? info.NextButtonLabel : "NEXT STEP →";
            string prefix = string.Equals(ModuleId, "gas-confined-space", StringComparison.OrdinalIgnoreCase) ? "gas" : "fire";
            return IndustrialSafetyAR.Core.LocaleService.Instance.Get($"{prefix}_step{stepNumber}_next", fallback);
        }

        public string CurrentStepTitle => GetStepTitle(CurrentStepIndex);
        public string CurrentInstruction => GetStepInstruction(CurrentStepIndex);
        public string CurrentNextLabel => GetNextLabel(CurrentStepIndex);
        public string CurrentSuccessFeedback => GetSuccessFeedback(CurrentStepIndex);

        public bool IsStepCompleted(int stepNumber)
        {
            if (stepNumber >= 1 && stepNumber < _stepCompleted.Length)
            {
                return _stepCompleted[stepNumber];
            }
            return false;
        }

        public string GetSuccessFeedback(int stepNumber)
        {
            var info = GetStepInfo(stepNumber);
            string defaultFeedback = info?.DefaultSuccessFeedback ?? "✓ Action completed successfully.";
            string recorded = (stepNumber >= 1 && stepNumber < _stepSuccessFeedback.Length)
                ? _stepSuccessFeedback[stepNumber]
                : null;
            string fallback = !string.IsNullOrEmpty(recorded) ? recorded : defaultFeedback;
            return IndustrialSafetyAR.Core.LocaleService.Instance.Get($"fire_step{stepNumber}_success", fallback);
        }

        /// <summary>
        /// Marks the specified step as successfully completed and stores its success feedback.
        /// Unlocks the Next button for this step.
        /// </summary>
        public bool CompleteStep(int stepNumber, string successFeedback = null)
        {
            if (stepNumber < 1 || stepNumber >= _stepCompleted.Length)
            {
                return false;
            }

            _stepCompleted[stepNumber] = true;
            if (stepNumber > HighestCompletedStep)
            {
                HighestCompletedStep = stepNumber;
            }

            string feedback = !string.IsNullOrEmpty(successFeedback)
                ? successFeedback
                : (GetStepInfo(stepNumber)?.DefaultSuccessFeedback ?? "✓ Action completed.");

            _stepSuccessFeedback[stepNumber] = feedback;
            OnStepCompleted?.Invoke(stepNumber, feedback);
            OnFeedbackChanged?.Invoke(feedback);
            return true;
        }

        /// <summary>
        /// Advances forward exactly one step if the current step is completed.
        /// If on the final step, requests assessment completion.
        /// </summary>
        public bool GoNext()
        {
            if (!CanGoNext)
            {
                // Step locking: cannot advance until current step action is completed
                return false;
            }

            if (IsAtFinalStep)
            {
                OnCompleteTrainingRequested?.Invoke();
                return true;
            }

            CurrentStepIndex++;
            OnStepChanged?.Invoke(CurrentStepIndex);
            return true;
        }

        /// <summary>
        /// Navigates backwards exactly one completed step in controlled review mode.
        /// Preserves all completed states and canonical assessment results; never emits duplicate scoring events.
        /// </summary>
        public bool GoBack()
        {
            if (!CanGoBack)
            {
                return false;
            }

            CurrentStepIndex--;
            OnStepChanged?.Invoke(CurrentStepIndex);
            return true;
        }

        /// <summary>
        /// Sets the active view step directly within valid step bounds (1..TotalSteps).
        /// </summary>
        public bool SetViewStep(int stepNumber)
        {
            if (stepNumber < 1 || stepNumber > TotalSteps) return false;

            CurrentStepIndex = stepNumber;
            OnStepChanged?.Invoke(CurrentStepIndex);
            return true;
        }

        /// <summary>
        /// Returns a compact, color-coded progress string, e.g. "● ● ● ○ ○ ○ ○ ○ ○  3 / 9"
        /// Completed steps = green (#2ECC71)
        /// Current step = cyan (#5DADE2)
        /// Upcoming steps = slate (#7F8C8D)
        /// </summary>
        public string FormatProgressIndicator()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 1; i <= TotalSteps; i++)
            {
                if (IsStepCompleted(i))
                {
                    sb.Append("<color=#2ECC71>●</color> ");
                }
                else if (i == CurrentStepIndex)
                {
                    sb.Append("<color=#5DADE2>●</color> ");
                }
                else
                {
                    sb.Append("<color=#7F8C8D>○</color> ");
                }
            }

            sb.Append($"  <color=#EAECEE><b>{CurrentStepIndex} / {TotalSteps}</b></color>");
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Resets navigation state for a fresh attempt (e.g. on Retake Training).
        /// </summary>
        public void Reset()
        {
            CurrentStepIndex = 1;
            HighestCompletedStep = 0;
            Array.Clear(_stepCompleted, 0, _stepCompleted.Length);
            Array.Clear(_stepSuccessFeedback, 0, _stepSuccessFeedback.Length);
            OnStepChanged?.Invoke(CurrentStepIndex);
        }

        /// <summary>
        /// Resets navigation state and sets active view to specified step.
        /// </summary>
        public void ResetToStep(int stepNumber = 1)
        {
            Reset();
            if (stepNumber > 1)
            {
                SetViewStep(stepNumber);
            }
        }
    }
}
