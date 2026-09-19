// GasEvacuationMarker.cs
// Namespace : IndustrialSafetyAR.Modules.GasConfinedSpace
//
// Lightweight procedural AR evacuation waypoint marker for Gas Leak & Confined Space Safety training.
// Represents waypoints along the upwind evacuation route:
// - Waypoint 1: Move Away From Hazard (exit danger perimeter)
// - Waypoint 2: Move Upwind (cross-wind / upwind navigation)
// - Waypoint 3: Reach Safe Area (upwind muster point)
// Provides raycast selection, ground indicator halo, and camera-facing ArFloatingLabels.

using System;
using IndustrialSafetyAR.Modules.FireExplosion;
using TMPro;
using UnityEngine;

namespace IndustrialSafetyAR.Modules.GasConfinedSpace
{
    [SelectionBase]
    public class GasEvacuationMarker : MonoBehaviour
    {
        [Header("Waypoint Metadata")]
        [SerializeField]
        private int _waypointIndex = 1;

        [SerializeField]
        private string _waypointId = "gas_waypoint_1";

        [SerializeField]
        private string _waypointTitle = "MOVE AWAY FROM HAZARD";

        [Header("Visual References")]
        [SerializeField]
        private Renderer _markerRenderer;

        [SerializeField]
        private Renderer _haloRenderer;

        [SerializeField]
        private TextMeshPro _labelMesh;

        private ArFloatingLabel _floatingLabel;
        private Material _markerMaterial;
        private Material _haloMaterial;
        private bool _isTraversed;
        private bool _isActive;
        private float _pulseTimer;

        // Visual Colors
        private static readonly Color ColorActiveAmber = new Color(0.98f, 0.72f, 0.15f);
        private static readonly Color ColorActiveGreen = new Color(0.18f, 0.82f, 0.35f);
        private static readonly Color ColorTraversed = new Color(0.20f, 0.85f, 0.95f);
        private static readonly Color ColorPending = new Color(0.45f, 0.48f, 0.52f);

        public int WaypointIndex => _waypointIndex;
        public string WaypointId => _waypointId;
        public string WaypointTitle => _waypointTitle;
        public bool IsTraversed => _isTraversed;
        public bool IsActive => _isActive;
        public ArFloatingLabel FloatingLabel => _floatingLabel;

        public event Action<GasEvacuationMarker> OnWaypointTapped;
        public void OnTap() => OnWaypointTapped?.Invoke(this);

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
                    Color col = ColorTraversed;
                    col.a = 0.40f;
                    _haloMaterial.color = col;
                }
                return;
            }

            if (_haloMaterial != null)
            {
                float pulse = 0.75f + 0.25f * Mathf.Sin(_pulseTimer);
                Color baseCol = _isActive ? ColorActiveGreen : ColorPending;
                baseCol.a = (_isActive ? 0.60f : 0.25f) * pulse;
                _haloMaterial.color = baseCol;
            }
        }

        public void Initialize(int index, string id, string title)
        {
            _waypointIndex = index;
            _waypointId = id;
            _waypointTitle = title;
            EnsureVisuals();
            UpdateLabelText();
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            if (_floatingLabel != null)
            {
                _floatingLabel.SetColor(_isActive ? ColorActiveGreen : ColorPending);
            }
            if (_markerMaterial != null)
            {
                _markerMaterial.color = _isTraversed ? ColorTraversed : (_isActive ? ColorActiveGreen : ColorPending);
            }
        }

        public void AcknowledgeTraversed()
        {
            _isTraversed = true;
            _isActive = false;
            if (_markerMaterial != null)
            {
                _markerMaterial.color = ColorTraversed;
            }
            if (_floatingLabel != null)
            {
                _floatingLabel.SetText($"✓ {_waypointTitle}");
                _floatingLabel.SetColor(ColorTraversed);
            }
        }

        public void ResetMarker()
        {
            _isTraversed = false;
            _isActive = false;
            SetActive(false);
            UpdateLabelText();
        }

        public void EnsureVisuals()
        {
            // 1. Collider for physics raycasting
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var sc = gameObject.AddComponent<SphereCollider>();
                sc.radius = 0.35f;
                sc.center = new Vector3(0f, 0.25f, 0f);
            }

            // 2. Center Post / Waypoint Chevron
            var postTransform = transform.Find("WaypointPost");
            if (postTransform == null)
            {
                var postObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                postObj.name = "WaypointPost";
                postObj.transform.SetParent(transform, false);
                postObj.transform.localPosition = new Vector3(0f, 0.20f, 0f);
                postObj.transform.localScale = new Vector3(0.12f, 0.20f, 0.12f);

                var postCol = postObj.GetComponent<Collider>();
                if (postCol != null) DestroyImmediate(postCol);

                _markerRenderer = postObj.GetComponent<Renderer>();
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Mobile/Diffuse")
                    ?? Shader.Find("Standard");
                _markerMaterial = new Material(unlit);
                _markerMaterial.color = ColorPending;
                _markerRenderer.sharedMaterial = _markerMaterial;
            }
            else
            {
                _markerRenderer = postTransform.GetComponent<Renderer>();
                if (_markerRenderer != null) _markerMaterial = _markerRenderer.material;
            }

            // 3. Ground Halo Ring
            var haloTransform = transform.Find("GroundHalo");
            if (haloTransform == null)
            {
                var haloObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                haloObj.name = "GroundHalo";
                haloObj.transform.SetParent(transform, false);
                haloObj.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                haloObj.transform.localScale = new Vector3(0.70f, 0.01f, 0.70f);

                var haloCol = haloObj.GetComponent<Collider>();
                if (haloCol != null) DestroyImmediate(haloCol);

                _haloRenderer = haloObj.GetComponent<Renderer>();
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Mobile/Diffuse")
                    ?? Shader.Find("Standard");
                _haloMaterial = new Material(unlit);
                _haloMaterial.color = new Color(ColorPending.r, ColorPending.g, ColorPending.b, 0.35f);
                _haloRenderer.sharedMaterial = _haloMaterial;
            }
            else
            {
                _haloRenderer = haloTransform.GetComponent<Renderer>();
                if (_haloRenderer != null) _haloMaterial = _haloRenderer.material;
            }

            // 4. Camera-facing Floating Label
            var labelTransform = transform.Find("FloatingLabel");
            if (labelTransform == null)
            {
                var labelObj = new GameObject("FloatingLabel");
                labelObj.transform.SetParent(transform, false);
                labelObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);

                _floatingLabel = labelObj.AddComponent<ArFloatingLabel>();
                _floatingLabel.EnsureElements();
                _floatingLabel.BillboardMode = ArBillboardMode.ScreenAligned;
                UpdateLabelText();
            }
            else
            {
                _floatingLabel = labelTransform.GetComponent<ArFloatingLabel>();
            }
        }

        private void UpdateLabelText()
        {
            if (_floatingLabel != null)
            {
                string prefix = _isTraversed ? "✓ " : $"{_waypointIndex}. ";
                _floatingLabel.SetText($"{prefix}{_waypointTitle}");
                _floatingLabel.SetColor(_isTraversed ? ColorTraversed : (_isActive ? ColorActiveGreen : ColorPending));
            }
        }

        public void HandleTap()
        {
            OnWaypointTapped?.Invoke(this);
        }
    }
}
