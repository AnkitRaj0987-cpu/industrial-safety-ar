// EmergencyExitMarker.cs
// Namespace : IndustrialSafetyAR.Modules.FireExplosion
//
// Represents an interactive 3D Emergency Exit marker in AR space.
// Provides lightweight procedural placeholder visuals using standard Unity primitives,
// TextMeshPro 3D signage, ground halo directional indicators, and raycast detection.

using System;
using TMPro;
using UnityEngine;

namespace IndustrialSafetyAR.Modules.FireExplosion
{
    /// <summary>
    /// Interactive AR marker representing an emergency exit or evacuation alternative.
    /// Supports spatial selection via physics raycasting and visual feedback confirmation.
    /// </summary>
    [SelectionBase]
    public class EmergencyExitMarker : MonoBehaviour
    {
        [Header("Exit Metadata")]
        [Tooltip("Target ID matching module.json / rubric.json (e.g. exit_emergency_sector_b)")]
        [SerializeField]
        private string _exitId = FireTrainingWorkflow.TargetExitEmergencySectorB;

        [Tooltip("Human-readable title")]
        [SerializeField]
        private string _exitName = "Sector B Emergency Exit";

        [Tooltip("Whether this marker represents the safe designated emergency exit")]
        [SerializeField]
        private bool _isDesignatedSafeExit = true;

        [Header("Visual Components")]
        [SerializeField]
        private Renderer _signRenderer;

        [SerializeField]
        private TextMeshPro _labelMesh;

        [SerializeField]
        private Renderer _haloRenderer;

        private Material _signMaterial;
        private Material _haloMaterial;
        private bool _isIdentified;
        private float _pulseTimer;

        // Visual color palettes
        private static readonly Color ColorSafeExitGreen = new Color(0.12f, 0.78f, 0.32f); // ISO Exit Green
        private static readonly Color ColorProhibitedRed = new Color(0.88f, 0.18f, 0.18f); // Warning / Prohibited Red
        private static readonly Color ColorSmokeAmber = new Color(0.95f, 0.55f, 0.12f);    // Smoke Warning Amber
        private static readonly Color ColorConfirmedGreen = new Color(0.20f, 0.98f, 0.45f); // Confirmed glow

        public string ExitId => _exitId;
        public string ExitName => _exitName;
        public bool IsDesignatedSafeExit => _isDesignatedSafeExit;
        public bool IsIdentified => _isIdentified;

        public event Action<EmergencyExitMarker> OnExitTapped;

        private void Awake()
        {
            EnsureVisuals();
        }

        private void Update()
        {
            _pulseTimer += Time.deltaTime * 3.0f;

            if (_isIdentified)
            {
                // Steady luminous green pulse when identified
                if (_haloMaterial != null)
                {
                    float pulse = 0.85f + 0.15f * Mathf.Sin(_pulseTimer * 1.5f);
                    Color glow = ColorConfirmedGreen * pulse;
                    glow.a = 0.55f;
                    _haloMaterial.color = glow;
                }
                return;
            }

            // Subtle pulsing idle guide effect
            if (_haloMaterial != null)
            {
                float pulse = 0.80f + 0.20f * Mathf.Sin(_pulseTimer);
                Color baseCol = _isDesignatedSafeExit ? ColorSafeExitGreen : (_exitId.Contains("elevator") ? ColorProhibitedRed : ColorSmokeAmber);
                baseCol.a = 0.35f * pulse;
                _haloMaterial.color = baseCol;
            }
        }

        private void OnDestroy()
        {
            if (_signMaterial != null) Destroy(_signMaterial);
            if (_haloMaterial != null) Destroy(_haloMaterial);
        }

        /// <summary>
        /// Configures the exit marker metadata and visual theme.
        /// </summary>
        public void ConfigureExit(string exitId, string exitName, bool isDesignatedSafe)
        {
            _exitId = exitId;
            _exitName = exitName;
            _isDesignatedSafeExit = isDesignatedSafe;
            ApplyTheme();
        }

        /// <summary>
        /// Acknowledges correct identification of the emergency exit.
        /// </summary>
        public void AcknowledgeIdentification()
        {
            _isIdentified = true;

            if (_signMaterial != null)
            {
                _signMaterial.color = ColorConfirmedGreen;
            }

            if (_labelMesh != null)
            {
                _labelMesh.text = "EMERGENCY EXIT IDENTIFIED\nSector B — Egress Route Verified";
                _labelMesh.color = ColorConfirmedGreen;
            }

            Debug.Log($"[EmergencyExitMarker] Exit '{_exitId}' confirmed and acknowledged.");
            OnExitTapped?.Invoke(this);
        }

        private void ApplyTheme()
        {
            Color themeColor = _isDesignatedSafeExit
                ? ColorSafeExitGreen
                : (_exitId.Contains("elevator") ? ColorProhibitedRed : ColorSmokeAmber);

            if (_signMaterial != null)
            {
                _signMaterial.color = themeColor;
            }

            if (_labelMesh != null)
            {
                if (_isDesignatedSafeExit)
                {
                    _labelMesh.text = "EMERGENCY EXIT\nSector B";
                    _labelMesh.color = ColorSafeExitGreen;
                }
                else if (_exitId.Contains("elevator"))
                {
                    _labelMesh.text = "FREIGHT ELEVATOR\nDO NOT USE IN FIRE!";
                    _labelMesh.color = ColorProhibitedRed;
                }
                else
                {
                    _labelMesh.text = "CORRIDOR SECTOR A\nBLOCKED BY HEAVY SMOKE";
                    _labelMesh.color = ColorSmokeAmber;
                }
            }
        }

        /// <summary>
        /// Procedurally constructs standard industrial emergency exit signage and ground indicators.
        /// </summary>
        public void EnsureVisuals()
        {
            if (transform.childCount > 0 && _signRenderer != null)
            {
                return;
            }

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard");

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");

            Color themeColor = _isDesignatedSafeExit ? ColorSafeExitGreen : ColorProhibitedRed;

            // 1. Post / frame upright support
            GameObject postObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postObj.name = "SignPost";
            postObj.transform.SetParent(transform, false);
            postObj.transform.localPosition = new Vector3(0f, 0.90f, 0f);
            postObj.transform.localScale = new Vector3(0.06f, 0.90f, 0.06f);
            var postMat = new Material(litShader) { color = new Color(0.25f, 0.26f, 0.28f) };
            postObj.GetComponent<Renderer>().material = postMat;

            // 2. Signboard Header (illuminated green or warning red)
            GameObject signBoardObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signBoardObj.name = "SignBoard";
            signBoardObj.transform.SetParent(transform, false);
            signBoardObj.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            signBoardObj.transform.localScale = new Vector3(1.20f, 0.45f, 0.08f);
            _signMaterial = new Material(litShader) { color = themeColor };
            _signRenderer = signBoardObj.GetComponent<Renderer>();
            _signRenderer.material = _signMaterial;

            // 3. Ground Halo Disc / Directional indicator
            GameObject haloObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            haloObj.name = "ExitGroundHalo";
            haloObj.transform.SetParent(transform, false);
            haloObj.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            haloObj.transform.localScale = new Vector3(1.4f, 0.002f, 1.4f);
            var haloCol = haloObj.GetComponent<Collider>();
            if (haloCol != null) Destroy(haloCol);

            Color haloColor = themeColor;
            haloColor.a = 0.30f;
            _haloMaterial = new Material(litShader) { color = haloColor };
            _haloRenderer = haloObj.GetComponent<Renderer>();
            _haloRenderer.material = _haloMaterial;

            // 4. Floating 3D Text Label
            GameObject labelObj = new GameObject("ExitLabel");
            labelObj.transform.SetParent(transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 2.22f, 0f);
            _labelMesh = labelObj.AddComponent<TextMeshPro>();
            _labelMesh.fontSize = 2.2f;
            _labelMesh.alignment = TextAlignmentOptions.Center;
            _labelMesh.rectTransform.sizeDelta = new Vector2(3.0f, 1.0f);

            ApplyTheme();

            // 5. Ensure overall BoxCollider exists for screen raycast selection
            var boxCol = GetComponent<BoxCollider>();
            if (boxCol == null)
            {
                boxCol = gameObject.AddComponent<BoxCollider>();
            }
            boxCol.center = new Vector3(0f, 1.2f, 0f);
            boxCol.size = new Vector3(1.3f, 2.4f, 0.6f);
        }
    }
}
