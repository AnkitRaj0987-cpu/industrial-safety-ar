// TrainingEventBus.cs
// Namespace : IndustrialSafetyAR.Core.Events
//
// In-memory event bus implementing ITrainingEventDispatcher.
// Decoupled, testable, and provides a default singleton hook while allowing
// explicit instance creation for isolated unit tests.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndustrialSafetyAR.Core.Events
{
    /// <summary>
    /// Thread-safe in-memory dispatcher and repository of training events.
    /// Captures emitted domain events so the Assessment Engine, UI feedback,
    /// and offline sync queue can process them independently.
    /// </summary>
    public class TrainingEventBus : ITrainingEventDispatcher
    {
        private static TrainingEventBus s_Instance;
        private static readonly object s_Lock = new object();

        /// <summary>
        /// Shared application-level dispatcher instance.
        /// </summary>
        public static TrainingEventBus Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    lock (s_Lock)
                    {
                        s_Instance ??= new TrainingEventBus();
                    }
                }
                return s_Instance;
            }
        }

        private readonly List<TrainingEvent> _dispatchedEvents = new List<TrainingEvent>();
        private readonly object _listLock = new object();

        /// <summary>
        /// Fired whenever a training event is dispatched through this bus.
        /// </summary>
        public event Action<TrainingEvent> OnEventDispatched;

        /// <summary>
        /// Read-only snapshot of all events dispatched during the current session.
        /// </summary>
        public IReadOnlyList<TrainingEvent> DispatchedEvents
        {
            get
            {
                lock (_listLock)
                {
                    return _dispatchedEvents.ToArray();
                }
            }
        }

        /// <summary>
        /// Emits a domain training event to all registered listeners and records it.
        /// </summary>
        public void Dispatch(TrainingEvent trainingEvent)
        {
            if (trainingEvent == null)
            {
                Debug.LogWarning("[TrainingEventBus] Attempted to dispatch a null TrainingEvent.");
                return;
            }

            lock (_listLock)
            {
                _dispatchedEvents.Add(trainingEvent);
            }

            Debug.Log($"[TrainingEventBus] Dispatched: {trainingEvent}");

            try
            {
                OnEventDispatched?.Invoke(trainingEvent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TrainingEventBus] Exception while invoking listener for event {trainingEvent.EventId}: {ex}");
            }
        }

        /// <summary>
        /// Clears recorded events — primarily for test fixtures and session resets.
        /// </summary>
        public void Clear()
        {
            lock (_listLock)
            {
                _dispatchedEvents.Clear();
            }
        }
    }
}
