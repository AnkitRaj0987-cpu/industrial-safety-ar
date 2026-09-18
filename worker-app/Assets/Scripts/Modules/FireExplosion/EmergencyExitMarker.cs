// EmergencyExitMarker.cs
// Namespace : IndustrialSafetyAR.Modules.FireExplosion
//
// Represents an interactive 3D Emergency Exit marker in AR space.
// Provides procedural signage, ground halo directional indicators, raycast detection,
// and camera-facing ArFloatingLabels with compact mobile AR scaling.

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

        private ArFloatingLabel _floatingLabel;
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
                if (_haloMaterial != null)
                {
                    float pulse = 0.85f + 0.15f * Mathf.Sin(_pulseTimer * 1.5f);
                    Color glow = ColorConfirmedGreen * pulse;
                    glow.a = 0.55f;
                    _haloMaterial.color = glow;
                }
                return;
            }

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

            string text = "EMERGENCY EXIT IDENTIFIED\nSector B — Egress Route Verified";
            if (_floatingLabel != null)
            {
                _floatingLabel.SetText(text);
                _floatingLabel.SetColor(ColorConfirmedGreen);
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = text;
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

            string text;
            if (_isIdentified)
            {
                text = "✓ EXIT MARKED (SECTOR B)";
            }
            else if (_isDesignatedSafeExit)
            {
                text = "EMERGENCY EXIT (SECTOR B)";
            }
            else if (_exitId.Contains("elevator"))
            {
                text = "⚠ NO EXIT: FREIGHT ELEVATOR";
            }
            else
            {
                text = "⚠ BLOCKED: SMOKE HAZARD";
            }

            if (_floatingLabel != null)
            {
                _floatingLabel.SetText(text);
                _floatingLabel.SetColor(themeColor);
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = text;
                _labelMesh.color = themeColor;
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

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Color themeColor = _isDesignatedSafeExit
                ? ColorSafeExitGreen
                : (_exitId.Contains("elevator") ? ColorProhibitedRed : ColorSmokeAmber);

            // 1. Sign Post Pole
            GameObject poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poleObj.name = "SignPole";
            poleObj.transform.SetParent(transform, false);
            poleObj.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            poleObj.transform.localScale = new Vector3(0.06f, 0.95f, 0.06f);
            var poleMat = new Material(litShader) { color = new Color(0.22f, 0.24f, 0.28f) };
            poleObj.GetComponent<Renderer>().material = poleMat;
            var poleCol = poleObj.GetComponent<Collider>();
            if (poleCol != null) DestroyImmediate(poleCol);

            // 2. Sign Plaque Header (ISO Running Man / Green Exit Plate)
            GameObject plateObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plateObj.name = "SignPlate";
            plateObj.transform.SetParent(transform, false);
            plateObj.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            plateObj.transform.localScale = new Vector3(0.95f, 0.45f, 0.04f);
            _signMaterial = new Material(litShader) { color = themeColor };
            _signRenderer = plateObj.GetComponent<Renderer>();
            _signRenderer.material = _signMaterial;

            var plateCol = plateObj.GetComponent<Collider>();
            if (plateCol != null) DestroyImmediate(plateCol);

            // 3. Ground Directional Halo
            GameObject haloObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            haloObj.name = "ExitHalo";
            haloObj.transform.SetParent(transform, false);
            haloObj.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            haloObj.transform.localScale = new Vector3(1.6f, 0.005f, 1.6f);
            var haloCol = haloObj.GetComponent<Collider>();
            if (haloCol != null) DestroyImmediate(haloCol);

            Color haloColor = themeColor;
            haloColor.a = 0.30f;
            _haloMaterial = new Material(litShader) { color = haloColor };
            _haloRenderer = haloObj.GetComponent<Renderer>();
            _haloRenderer.material = _haloMaterial;

            // 4. Camera-Facing ArFloatingLabel (compact billboard badge)
            _floatingLabel = ArFloatingLabel.Create(
                gameObject,
                new Vector3(0f, 2.18f, 0f),
                "EMERGENCY EXIT (SECTOR B)",
                themeColor,
                width: 0.40f,
                height: 0.09f,
                fontSize: 0.72f,
                ArBillboardMode.ScreenAligned);
            _labelMesh = _floatingLabel.LabelMesh;

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
