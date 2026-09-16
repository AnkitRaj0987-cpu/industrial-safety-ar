// EvacuationRouteMarker.cs
// Namespace : IndustrialSafetyAR.Modules.FireExplosion
//
// Represents an interactive 3D Evacuation Waypoint marker in AR space.
// Provides lightweight procedural placeholder visuals using standard Unity primitives,
// TextMeshPro 3D signage, ground chevron / halo directional indicators, and raycast detection.

using System;
using TMPro;
using UnityEngine;

namespace IndustrialSafetyAR.Modules.FireExplosion
{
    /// <summary>
    /// Interactive AR marker representing a waypoint along the designated emergency evacuation route.
    /// Supports spatial selection via physics raycasting and dynamic state transitions.
    /// </summary>
    [SelectionBase]
    public class EvacuationRouteMarker : MonoBehaviour
    {
        [Header("Waypoint Metadata")]
        [Tooltip("Waypoint ID matching module.json (e.g. waypoint_main_corridor)")]
        [SerializeField]
        private string _waypointId = FireTrainingWorkflow.WaypointMainCorridor;

        [Tooltip("Human-readable title")]
        [SerializeField]
        private string _waypointName = "Waypoint 1: Main Corridor";

        [Tooltip("Step sequence index along the route (1, 2, 3)")]
        [SerializeField]
        private int _sequenceOrder = 1;

        [Tooltip("Whether this marker represents a hazardous corridor to avoid")]
        [SerializeField]
        private bool _isHazardousAlternative = false;

        [Header("Visual Components")]
        [SerializeField]
        private Renderer _markerRenderer;

        [SerializeField]
        private TextMeshPro _labelMesh;

        [SerializeField]
        private Renderer _haloRenderer;

        private Material _markerMaterial;
        private Material _haloMaterial;
        private bool _isTraversed;
        private bool _isActive;
        private float _pulseTimer;

        // Visual color palettes
        private static readonly Color ColorSafeGreen = new Color(0.12f, 0.78f, 0.32f);       // ISO Safety Green
        private static readonly Color ColorActiveAmber = new Color(0.98f, 0.72f, 0.15f);     // Active Guidance Amber
        private static readonly Color ColorCompletedCyan = new Color(0.20f, 0.85f, 0.95f);   // Traversed Cyan/Green
        private static readonly Color ColorHazardRed = new Color(0.90f, 0.15f, 0.15f);       // Smoke Hazard Red

        public string WaypointId => _waypointId;
        public string WaypointName => _waypointName;
        public int SequenceOrder => _sequenceOrder;
        public bool IsHazardousAlternative => _isHazardousAlternative;
        public bool IsTraversed => _isTraversed;
        public bool IsActive => _isActive;

        public event Action<EvacuationRouteMarker> OnWaypointTapped;

        private void Awake()
        {
            EnsureVisuals();
        }

        private void Update()
        {
            _pulseTimer += Time.deltaTime * 3.0f;

            if (_isTraversed)
            {
                if (_haloMaterial != null)
                {
                    Color col = ColorCompletedCyan;
                    col.a = 0.40f;
                    _haloMaterial.color = col;
                }
                return;
            }

            if (_haloMaterial != null)
            {
                float pulse = 0.75f + 0.25f * Mathf.Sin(_pulseTimer);
                Color baseCol = _isHazardousAlternative
                    ? ColorHazardRed
                    : (_isActive ? ColorActiveAmber : ColorSafeGreen);
                baseCol.a = (_isActive ? 0.50f : 0.25f) * pulse;
                _haloMaterial.color = baseCol;
            }
        }

        private void OnDestroy()
        {
            if (_markerMaterial != null) Destroy(_markerMaterial);
            if (_haloMaterial != null) Destroy(_haloMaterial);
        }

        /// <summary>
        /// Configures waypoint metadata and initializes visual presentation.
        /// </summary>
        public void ConfigureWaypoint(string waypointId, string waypointName, int sequenceOrder, bool isHazardous)
        {
            _waypointId = waypointId;
            _waypointName = waypointName;
            _sequenceOrder = sequenceOrder;
            _isHazardousAlternative = isHazardous;
            _isActive = sequenceOrder == 1 && !isHazardous;
            ApplyTheme();
        }

        /// <summary>
        /// Sets this waypoint as the currently active expected target along the route.
        /// </summary>
        public void SetActiveTarget(bool active)
        {
            _isActive = active;
            ApplyTheme();
        }

        /// <summary>
        /// Acknowledges traversal of this waypoint along the evacuation path.
        /// </summary>
        public void MarkTraversed()
        {
            _isTraversed = true;
            _isActive = false;

            if (_markerMaterial != null)
            {
                _markerMaterial.color = ColorCompletedCyan;
            }

            if (_labelMesh != null)
            {
                _labelMesh.text = $"{_waypointName}\n[PASSED]";
                _labelMesh.color = ColorCompletedCyan;
            }

            Debug.Log($"[EvacuationRouteMarker] Waypoint '{_waypointId}' marked traversed.");
            OnWaypointTapped?.Invoke(this);
        }

        private void ApplyTheme()
        {
            Color themeColor = _isHazardousAlternative
                ? ColorHazardRed
                : (_isTraversed ? ColorCompletedCyan : (_isActive ? ColorActiveAmber : ColorSafeGreen));

            if (_markerMaterial != null)
            {
                _markerMaterial.color = themeColor;
            }

            if (_labelMesh != null)
            {
                _labelMesh.text = _isHazardousAlternative
                    ? $"{_waypointName}\nDO NOT ENTER (SMOKE HAZARD)"
                    : (_isTraversed ? $"{_waypointName}\n[PASSED]" : _waypointName);
                _labelMesh.color = themeColor;
            }
        }

        /// <summary>
        /// Procedurally constructs standard industrial evacuation route waypoint graphics.
        /// </summary>
        public void EnsureVisuals()
        {
            if (transform.childCount > 0 && _markerRenderer != null)
            {
                return;
            }

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");

            Color themeColor = _isHazardousAlternative ? ColorHazardRed : ColorSafeGreen;

            // 1. Ground Chevron / Disc Base
            GameObject discObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            discObj.name = "WaypointBase";
            discObj.transform.SetParent(transform, false);
            discObj.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            discObj.transform.localScale = new Vector3(0.9f, 0.02f, 0.9f);
            _markerMaterial = new Material(litShader) { color = themeColor };
            _markerRenderer = discObj.GetComponent<Renderer>();
            _markerRenderer.material = _markerMaterial;

            // Remove default mesh collider from primitive cylinder
            var meshCol = discObj.GetComponent<Collider>();
            if (meshCol != null) Destroy(meshCol);

            // 2. Upright Guide Beacon / Marker Post
            GameObject beaconObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beaconObj.name = "BeaconPost";
            beaconObj.transform.SetParent(transform, false);
            beaconObj.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            beaconObj.transform.localScale = new Vector3(0.05f, 0.45f, 0.05f);
            var beaconMat = new Material(litShader) { color = new Color(0.25f, 0.26f, 0.28f) };
            beaconObj.GetComponent<Renderer>().material = beaconMat;

            var beaconCol = beaconObj.GetComponent<Collider>();
            if (beaconCol != null) Destroy(beaconCol);

            // 3. Ground Halo Ring
            GameObject haloObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            haloObj.name = "WaypointHalo";
            haloObj.transform.SetParent(transform, false);
            haloObj.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            haloObj.transform.localScale = new Vector3(1.3f, 0.002f, 1.3f);
            var haloCol = haloObj.GetComponent<Collider>();
            if (haloCol != null) Destroy(haloCol);

            Color haloColVal = themeColor;
            haloColVal.a = 0.35f;
            _haloMaterial = new Material(litShader) { color = haloColVal };
            _haloRenderer = haloObj.GetComponent<Renderer>();
            _haloRenderer.material = _haloMaterial;

            // 4. Floating 3D TextMeshPro Label
            GameObject labelObj = new GameObject("WaypointLabel");
            labelObj.transform.SetParent(transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            _labelMesh = labelObj.AddComponent<TextMeshPro>();
            _labelMesh.fontSize = 2.0f;
            _labelMesh.alignment = TextAlignmentOptions.Center;
            _labelMesh.rectTransform.sizeDelta = new Vector2(3.0f, 1.0f);

            ApplyTheme();

            // 5. BoxCollider for screen tap Physics.Raycast interaction
            var boxCol = GetComponent<BoxCollider>();
            if (boxCol == null)
            {
                boxCol = gameObject.AddComponent<BoxCollider>();
            }
            boxCol.center = new Vector3(0f, 0.6f, 0f);
            boxCol.size = new Vector3(1.2f, 1.3f, 1.2f);
        }
    }
}
