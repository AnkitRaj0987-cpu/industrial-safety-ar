// ITrainingEventDispatcher.cs
// Namespace : IndustrialSafetyAR.Core.Events
//
// Clean interface through which training interactions publish domain events
// without coupling to concrete consumers (such as AssessmentEngine or storage).

using System;

namespace IndustrialSafetyAR.Core.Events
{
    /// <summary>
    /// Contract for dispatching training domain events.
    /// Allows the assessment engine, analytics, and local feedback systems
    /// to observe training progress without coupling to AR interaction components.
    /// </summary>
    public interface ITrainingEventDispatcher
    {
        /// <summary>
        /// Emits a domain training event into the event pipeline.
        /// </summary>
        /// <param name="trainingEvent">The event to dispatch.</param>
        void Dispatch(TrainingEvent trainingEvent);

        /// <summary>
        /// Subscribe to receive events dispatched through this dispatcher.
        /// </summary>
        event Action<TrainingEvent> OnEventDispatched;
    }
}
