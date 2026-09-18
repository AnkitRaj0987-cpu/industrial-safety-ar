// AppBootstrap.cs
// Namespace : IndustrialSafetyAR.Core
//
// Single entry-point that persists across scene loads for the duration of the
// application's lifetime.  Attach this MonoBehaviour to a dedicated GameObject
// in the first (bootstrap) scene.
//
// Responsibilities at this stage:
//   - Enforce a single instance (singleton guard).
//   - Survive scene transitions via DontDestroyOnLoad.
//   - Provide a stable hook for future sub-system initialisation (networking,
//     SQLite, localisation, sync queue, etc.) without coupling those systems
//     together at this point.
//
// What this script intentionally does NOT do yet:
//   - No networking / HTTP.
//   - No SQLite / offline database.
//   - No AR logic.
//   - No localisation.
//   - No assessment or scoring.
//   - No UI.

using UnityEngine;

namespace IndustrialSafetyAR.Core
{
    /// <summary>
    /// Root application bootstrap.  Persists across scene loads and ensures
    /// only one instance ever exists.  Future sub-systems should be initialised
    /// from <see cref="Initialise"/> so startup order is explicit and testable.
    /// </summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Singleton state
        // ------------------------------------------------------------------

        /// <summary>
        /// The single live instance.  Null until Awake has run on the first
        /// bootstrap object that reaches the scene.
        /// </summary>
        public static AppBootstrap Instance { get; private set; }

        // ------------------------------------------------------------------
        // Unity lifecycle
        // ------------------------------------------------------------------

        private void Awake()
        {
            // Duplicate-instance guard:
            //   If another AppBootstrap already registered itself as the
            //   canonical instance (e.g. the scene was reloaded or the prefab
            //   was accidentally placed twice), destroy *this* new arrival and
            //   return immediately so the existing instance is undisturbed.
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    "[AppBootstrap] Duplicate instance detected on " +
                    $"'{gameObject.name}' — destroying newcomer.");
                Destroy(gameObject);
                return;
            }

            // Register as the canonical instance and pin this GameObject so
            // it survives any future LoadScene calls.
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[AppBootstrap] Initialised — application bootstrap ready.");
            Initialise();
        }

        private void OnDestroy()
        {
            // Clear the static reference only when the authoritative instance
            // is actually being torn down (e.g. application quit), not when a
            // duplicate was rejected above.
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ------------------------------------------------------------------
        // Sub-system initialisation hook
        // ------------------------------------------------------------------

        /// <summary>
        /// Called once, immediately after this bootstrap object is promoted to
        /// the canonical instance.  Add ordered sub-system startup calls here
        /// as the application grows (e.g. config loading, SQLite open, sync
        /// queue init).  Keep each sub-system self-contained.
        /// </summary>
        private void Initialise()
        {
            LocaleService.Instance.LoadPersistedLanguage();
            Audio.FireAudioService.Instance.LoadSettings();
        }
    }
}
