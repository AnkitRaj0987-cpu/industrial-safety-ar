// ArSessionFacade.cs
// Namespace : IndustrialSafetyAR.AR
//
// This component is the application's single boundary around AR Foundation's
// session state.  All other scripts that need to know whether AR is ready
// should read from this facade rather than importing AR Foundation types
// directly.  This keeps AR Foundation a detail of the AR layer, not a
// dependency that spreads across the whole codebase.
//
// Attach to any active GameObject in the AR scene alongside (or near) the
// AR Session component.  The facade is read-only from the outside; it does
// not start, stop, or reconfigure the session.
//
// What this script intentionally does NOT do:
//   - No plane spawning or raycasting.
//   - No training module logic.
//   - No UI, networking, localisation, assessment, or offline storage.

using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace IndustrialSafetyAR.AR
{
    /// <summary>
    /// Read-only facade over AR Foundation's global session state.
    /// Exposes only the minimum information the rest of the application needs:
    /// whether this component is initialised and whether AR is actively tracking.
    /// </summary>
    public sealed class ArSessionFacade : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Public state
        // ------------------------------------------------------------------

        /// <summary>
        /// True once this component has been enabled and has successfully
        /// confirmed that <c>ARSession</c> is present in the scene.
        /// Becomes false again if the component is disabled or destroyed.
        /// </summary>
        /// <remarks>
        /// This is deliberately lightweight: it does not mean the AR session
        /// has reached a usable tracking state — see <see cref="IsTrackingAvailable"/>.
        /// </remarks>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// True when the AR session is running and the device has successfully
        /// established its position and orientation in the world
        /// (<c>ARSessionState.SessionTracking</c>).
        /// False in all other states, including while initialising, when tracking
        /// is temporarily lost, or when AR is unsupported on the device.
        /// </summary>
        public bool IsTrackingAvailable => ARSession.state == ARSessionState.SessionTracking;

        // ------------------------------------------------------------------
        // Unity lifecycle
        // ------------------------------------------------------------------

        private void OnEnable()
        {
            // Subscribe to session state changes so any future logic added
            // here (e.g. events, logging) can react without polling.
            ARSession.stateChanged += OnSessionStateChanged;

            IsInitialized = true;

            Debug.Log(
                $"[ArSessionFacade] Initialised. " +
                $"Current AR session state: {ARSession.state}");
        }

        private void OnDisable()
        {
            ARSession.stateChanged -= OnSessionStateChanged;
            IsInitialized = false;

            Debug.Log("[ArSessionFacade] Disabled — facade deactivated.");
        }

        // ------------------------------------------------------------------
        // Session state callback
        // ------------------------------------------------------------------

        /// <summary>
        /// Called by AR Foundation whenever <c>ARSession.state</c> changes.
        /// Currently used only for diagnostic logging; extend here to raise
        /// application-level events (e.g. tracking gained/lost) in later steps.
        /// </summary>
        private void OnSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            Debug.Log($"[ArSessionFacade] AR session state → {args.state}");
        }
    }
}
