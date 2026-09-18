// GasAtmosphericSimulator.cs
// Namespace : IndustrialSafetyAR.Modules.GasConfinedSpace
//
// Pure C# deterministic atmospheric testing simulator for Confined Space Entry.
// Decoupled from Unity runtime so it can be instantiated, verified, and unit-tested in isolation.
//
// SAFETY PRINCIPLE:
// Workers cannot reliably determine atmospheric safety using human senses alone (NIOSH guidance).
// Enforces the deterministic OSHA 29 CFR 1910.146 testing sequence:
//   1. Oxygen content (O2)
//   2. Flammable gases and vapors (LEL)
//   3. Toxic air contaminants (H2S / CO)
//
// Sequence violation (e.g. testing toxic or flammable before oxygen) is rejected.

using System;
using System.Collections.Generic;
using IndustrialSafetyAR.Core.Events;

namespace IndustrialSafetyAR.Modules.GasConfinedSpace
{
    public enum GasSensorType
    {
        Oxygen,
        Flammable,
        Toxic
    }

    public class GasSensorReading
    {
        public GasSensorType SensorType { get; set; }
        public string GasName { get; set; }
        public float Value { get; set; }
        public string Unit { get; set; }
        public bool IsSafe { get; set; }
        public string StatusDescription { get; set; }
    }

    public class GasAtmosphericSimulator
    {
        public const string StepId = "step_gas_atmospheric_test";
        public const string RuleAtmosphericTest = "rule_atmospheric_test";

        // Predefined deterministic scenario readings (Unsafe Confined Space Scenario)
        // 1. Oxygen: 19.1% Vol (< 19.5% safe baseline -> Oxygen Deficient / Unsafe)
        public static readonly GasSensorReading DefaultOxygenReading = new GasSensorReading
        {
            SensorType = GasSensorType.Oxygen,
            GasName = "Oxygen (O2)",
            Value = 19.1f,
            Unit = "% Vol",
            IsSafe = false,
            StatusDescription = "DEFICIENT (< 19.5% Vol) - Atmospheric hazard"
        };

        // 2. Flammable: 18% LEL (> 10% LEL permissible entry limit -> Flammable Hazard / Unsafe)
        public static readonly GasSensorReading DefaultFlammableReading = new GasSensorReading
        {
            SensorType = GasSensorType.Flammable,
            GasName = "Combustible Gas (LEL)",
            Value = 18.0f,
            Unit = "% LEL",
            IsSafe = false,
            StatusDescription = "HAZARDOUS (> 10% LEL) - Explosion hazard"
        };

        // 3. Toxic: 35 ppm H2S (> 10 ppm permissible exposure ceiling -> Toxic Hazard / Lethal)
        public static readonly GasSensorReading DefaultToxicReading = new GasSensorReading
        {
            SensorType = GasSensorType.Toxic,
            GasName = "Hydrogen Sulfide (H2S)",
            Value = 35.0f,
            Unit = "ppm",
            IsSafe = false,
            StatusDescription = "DANGER (> 10 ppm) - Lethal toxic contaminant"
        };

        public bool IsTestStarted { get; private set; }
        public bool IsOxygenTested { get; private set; }
        public bool IsFlammableTested { get; private set; }
        public bool IsToxicTested { get; private set; }
        public bool IsAssessmentCompleted => IsOxygenTested && IsFlammableTested && IsToxicTested;

        public GasSensorReading OxygenReading { get; set; } = DefaultOxygenReading;
        public GasSensorReading FlammableReading { get; set; } = DefaultFlammableReading;
        public GasSensorReading ToxicReading { get; set; } = DefaultToxicReading;

        public bool OverallAtmosphereSafe => OxygenReading.IsSafe && FlammableReading.IsSafe && ToxicReading.IsSafe;

        public event Action<GasSensorReading> OnSensorReadingAvailable;
        public event Action<bool> OnAssessmentFinished;

        public bool StartTest(string moduleId, string contentVersion, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (IsTestStarted)
            {
                return false;
            }

            IsTestStarted = true;
            emittedEvent = new TrainingEvent
            {
                ModuleId = moduleId,
                ContentVersion = contentVersion,
                StepId = StepId,
                EventType = "atmosphere_test_started",
                ActionId = "initiate_multi_gas_detector",
                TargetId = "multi_gas_detector_calibrated",
                Outcome = "success",
                Payload =
                {
                    { "rule_id", RuleAtmosphericTest },
                    { "standard", "OSHA_29CFR_1910_146" },
                    { "action_id", "initiate_multi_gas_detector" },
                    { "outcome", "success" }
                }
            };

            dispatcher?.Dispatch(emittedEvent);
            return true;
        }

        public bool TestSensor(GasSensorType sensorType, string moduleId, string contentVersion, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent, out string rejectionReason)
        {
            emittedEvent = null;
            rejectionReason = null;

            if (!IsTestStarted)
            {
                rejectionReason = "Atmospheric test has not been initiated. Turn on multi-gas detector first.";
                return false;
            }

            // Enforce OSHA sequence: 1. O2 -> 2. LEL -> 3. Toxic
            switch (sensorType)
            {
                case GasSensorType.Oxygen:
                    if (IsOxygenTested)
                    {
                        rejectionReason = "Oxygen level has already been measured.";
                        return false;
                    }
                    IsOxygenTested = true;
                    emittedEvent = CreateStepEvent(moduleId, contentVersion, OxygenReading, "step_1_oxygen");
                    dispatcher?.Dispatch(emittedEvent);
                    OnSensorReadingAvailable?.Invoke(OxygenReading);
                    return true;

                case GasSensorType.Flammable:
                    if (!IsOxygenTested)
                    {
                        rejectionReason = "OSHA SEQUENCE VIOLATION: Oxygen content must be verified before flammable gas testing.";
                        return false;
                    }
                    if (IsFlammableTested)
                    {
                        rejectionReason = "Flammable gas level has already been measured.";
                        return false;
                    }
                    IsFlammableTested = true;
                    emittedEvent = CreateStepEvent(moduleId, contentVersion, FlammableReading, "step_2_flammable");
                    dispatcher?.Dispatch(emittedEvent);
                    OnSensorReadingAvailable?.Invoke(FlammableReading);
                    return true;

                case GasSensorType.Toxic:
                    if (!IsOxygenTested)
                    {
                        rejectionReason = "OSHA SEQUENCE VIOLATION: Oxygen content must be verified first.";
                        return false;
                    }
                    if (!IsFlammableTested)
                    {
                        rejectionReason = "OSHA SEQUENCE VIOLATION: Flammable gas (LEL) must be verified before toxic contaminants.";
                        return false;
                    }
                    if (IsToxicTested)
                    {
                        rejectionReason = "Toxic contaminant level has already been measured.";
                        return false;
                    }
                    IsToxicTested = true;
                    emittedEvent = CreateStepEvent(moduleId, contentVersion, ToxicReading, "step_3_toxic");
                    dispatcher?.Dispatch(emittedEvent);
                    OnSensorReadingAvailable?.Invoke(ToxicReading);
                    return true;

                default:
                    rejectionReason = "Unknown sensor type.";
                    return false;
            }
        }

        public bool CompleteAssessment(string moduleId, string contentVersion, ITrainingEventDispatcher dispatcher, out TrainingEvent emittedEvent)
        {
            emittedEvent = null;
            if (!IsAssessmentCompleted)
            {
                return false;
            }

            string overallStatus = OverallAtmosphereSafe ? "safe" : "unsafe";
            emittedEvent = new TrainingEvent
            {
                ModuleId = moduleId,
                ContentVersion = contentVersion,
                StepId = StepId,
                EventType = "atmosphere_assessment_completed",
                ActionId = "evaluate_atmospheric_condition",
                TargetId = "confined_space_atmosphere",
                Outcome = "success",
                Payload =
                {
                    { "rule_id", RuleAtmosphericTest },
                    { "action_id", "evaluate_atmospheric_condition" },
                    { "overall_status", overallStatus },
                    { "hazard_detected", OverallAtmosphereSafe ? "false" : "true" },
                    { "o2_value", OxygenReading.Value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) },
                    { "lel_value", FlammableReading.Value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) },
                    { "toxic_value", ToxicReading.Value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) },
                    { "outcome", "success" }
                }
            };

            dispatcher?.Dispatch(emittedEvent);
            OnAssessmentFinished?.Invoke(OverallAtmosphereSafe);
            return true;
        }

        public void Reset()
        {
            IsTestStarted = false;
            IsOxygenTested = false;
            IsFlammableTested = false;
            IsToxicTested = false;
        }

        private TrainingEvent CreateStepEvent(string moduleId, string contentVersion, GasSensorReading reading, string sequenceKey)
        {
            return new TrainingEvent
            {
                ModuleId = moduleId,
                ContentVersion = contentVersion,
                StepId = StepId,
                EventType = "atmosphere_test_step_completed",
                ActionId = "measure_sensor_level",
                TargetId = reading.GasName,
                Outcome = "success",
                Payload =
                {
                    { "rule_id", RuleAtmosphericTest },
                    { "action_id", "measure_sensor_level" },
                    { "sequence_step", sequenceKey },
                    { "gas_type", reading.SensorType.ToString().ToLowerInvariant() },
                    { "gas_name", reading.GasName },
                    { "reading_value", reading.Value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) },
                    { "unit", reading.Unit },
                    { "status", reading.IsSafe ? "safe" : "unsafe" },
                    { "status_description", reading.StatusDescription },
                    { "outcome", "success" }
                }
            };
        }
    }
}
