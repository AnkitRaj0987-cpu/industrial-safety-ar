// ARTrackingStatusUI.cs
// Namespace : IndustrialSafetyAR.AR
//
// Reads tracking state from ArSessionFacade and displays a simple human-readable
// status message on screen.  Attach to the StatusText GameObject alongside a
// TextMeshProUGUI component; wire the ArSessionFacade reference in the Inspector.
//
// Three messages:
//   "Initializing AR..."              — facade not yet initialized
//   "Move your phone slowly to find a surface."  — initialized but not tracking
//   "AR Ready"                        — tracking is active
//
// Does NOT implement:  Fire/Gas modules, assessment, localization, networking,
// certificates, raycasting, plane interaction, or any other application feature.

using TMPro;
using UnityEngine;

namespace IndustrialSafetyAR.AR
{
    /// <summary>
    /// Polls <see cref="ArSessionFacade"/> every frame and updates a
    /// <see cref="TextMeshProUGUI"/> label with the current AR tracking status.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class ARTrackingStatusUI : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Inspector fields
        // ------------------------------------------------------------------

        [Tooltip("The ArSessionFacade on the AR Session GameObject.")]
        [SerializeField]
        private ArSessionFacade _facade;

        // ------------------------------------------------------------------
        // Private state
        // ------------------------------------------------------------------

        private TextMeshProUGUI _label;

        // Status strings — kept as constants so future localisation can
        // replace them in one place.
        private const string MsgInitializing = "Initializing AR...";
        private const string MsgSearching    = "Move your phone slowly to find a surface.";
        private const string MsgReady        = "AR Ready";

        // ------------------------------------------------------------------
        // Unity lifecycle
        // ------------------------------------------------------------------

        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (ARModeController.Instance != null && !ARModeController.Instance.IsARActive)
            {
                SetText(string.Empty);
                return;
            }

            if (_facade == null)
            {
                // Facade reference not set — show initializing and warn once in debug builds.
                SetText(MsgInitializing);
                return;
            }

            if (!_facade.IsInitialized)
            {
                SetText(MsgInitializing);
            }
            else if (!_facade.IsTrackingAvailable)
            {
                SetText(MsgSearching);
            }
            else
            {
                SetText(MsgReady);
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private void SetText(string message)
        {
            if (_label.text != message)
                _label.text = message;
        }
    }
}
