// IIdentifiableHazard.cs
// Namespace : IndustrialSafetyAR.Modules.FireExplosion
//
// Common contract for training hazards that workers must locate, inspect,
// and identify in AR space.

using System;

namespace IndustrialSafetyAR.Modules.FireExplosion
{
    /// <summary>
    /// Contract for an interactive AR hazard object that can be detected/identified by the worker.
    /// </summary>
    public interface IIdentifiableHazard
    {
        /// <summary>Unique target ID matching the training content (e.g. "hazard_electrical_conveyor_fire").</summary>
        string HazardId { get; }

        /// <summary>Hazard classification (e.g. "class_e_electrical").</summary>
        string HazardClass { get; }

        /// <summary>Whether this hazard has already been detected and confirmed by the worker.</summary>
        bool IsDetected { get; }

        /// <summary>Marks the hazard as detected, triggering visual feedback and state change.</summary>
        void AcknowledgeDetection();

        /// <summary>Event raised when the hazard has been acknowledged/identified.</summary>
        event Action<IIdentifiableHazard> OnDetected;
    }
}
