// GasAttendantMarker.cs
// Namespace : IndustrialSafetyAR.Modules.GasConfinedSpace
//
// Represents the Outside Safety Attendant in AR space for Confined Space Safety.
// Provides a lightweight procedural industrial worker silhouette placed strictly outside
// the 3.0-meter danger zone perimeter, equipped with a high-vis safety vest, hard hat,
// shoulder-mounted two-way radio, and a camera-facing ArFloatingLabel.
//
// SAFETY RULE:
// The attendant MUST remain OUTSIDE the confined space hazard area at all times.
// Entry is prohibited; the attendant maintains two-way radio communication and monitors the perimeter.

using System;
using IndustrialSafetyAR.Modules.FireExplosion;
using TMPro;
using UnityEngine;

namespace IndustrialSafetyAR.Modules.GasConfinedSpace
{
    /// <summary>
    /// Interactive AR marker representing a designated Outside Safety Attendant / Buddy.
    /// Provides spatial raycast selection, visual equipment indicators, and camera-facing labels.
    /// </summary>
    [SelectionBase]
    public class GasAttendantMarker : MonoBehaviour
    {
        [Header("Attendant Metadata")]
        [SerializeField]
        private string _attendantId = "attendant_guard_outside";

        [SerializeField]
        private string _attendantName = "Safety Attendant Alpha (Outside)";

        [Header("Visual References")]
        [SerializeField]
        private Renderer _vestRenderer;

        [SerializeField]
        private Renderer _radioRenderer;

        [SerializeField]
        private TextMeshPro _labelMesh;

        private ArFloatingLabel _floatingLabel;
        private Material _vestMaterial;
        private Material _radioMaterial;
        private Material _baseMaterial;

        private bool _isAssigned;
        private bool _isCommunicationVerified;
        private float _pulseTimer;

        // Visual Colors
        private static readonly Color ColorVestOrange = new Color(1.0f, 0.45f, 0.05f);     // High-Vis Safety Orange
        private static readonly Color ColorVestAssigned = new Color(0.18f, 0.82f, 0.35f);   // Verified Green Glow
        private static readonly Color ColorRadioStandby = new Color(0.20f, 0.24f, 0.30f);   // Dark Radio Housing
        private static readonly Color ColorRadioActive = new Color(0.20f, 0.70f, 1.0f);     // Radio Transmit Blue
        private static readonly Color ColorHardHat = new Color(0.98f, 0.80f, 0.08f);       // Safety Yellow Hard Hat
        private static readonly Color ColorBasePedestal = new Color(0.15f, 0.18f, 0.22f);   // Industrial Standoff Base

        public string AttendantId => _attendantId;
        public string AttendantName => _attendantName;
        public bool IsAssigned => _isAssigned;
        public bool IsCommunicationVerified => _isCommunicationVerified;
        public ArFloatingLabel FloatingLabel => _floatingLabel;
        public TextMeshPro LabelMesh => _labelMesh;

        public event Action<GasAttendantMarker> OnAttendantTapped;

        private void Awake()
        {
            EnsureVisuals();
        }

        private void Update()
        {
            _pulseTimer += Time.deltaTime * 2.5f;

            if (_isAssigned && !_isCommunicationVerified && _vestMaterial != null)
            {
                // Subtle pulse to indicate awaiting radio check
                float glow = 0.85f + 0.15f * Mathf.Sin(_pulseTimer * 2f);
                _vestMaterial.color = ColorVestAssigned * glow;
            }
        }

        /// <summary>
        /// Validates that the attendant's position is strictly outside the hazardous danger perimeter.
        /// </summary>
        public bool IsPositionOutsideDangerZone(Vector3 hazardCenter, float dangerRadius = 3.0f)
        {
            Vector3 diff = transform.position - hazardCenter;
            diff.y = 0f; // Horizontal planar distance
            return diff.magnitude >= dangerRadius;
        }

        /// <summary>
        /// Sets the attendant status to Assigned.
        /// </summary>
        public void AcknowledgeAssigned()
        {
            _isAssigned = true;

            if (_vestMaterial != null)
            {
                _vestMaterial.color = ColorVestAssigned;
            }

            if (_floatingLabel != null)
            {
                _floatingLabel.SetText("OUTSIDE SAFETY ATTENDANT\n<size=75%><color=#2ECC71>✓ ASSIGNED (RADIO LINK READY)</color></size>");
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = "OUTSIDE SAFETY ATTENDANT\n<size=75%><color=#2ECC71>✓ ASSIGNED</color></size>";
            }
        }

        /// <summary>
        /// Sets the two-way radio communication to Verified.
        /// </summary>
        public void AcknowledgeCommunicationVerified()
        {
            _isCommunicationVerified = true;

            if (_radioMaterial != null)
            {
                _radioMaterial.color = ColorRadioActive;
            }

            if (_floatingLabel != null)
            {
                _floatingLabel.SetText("OUTSIDE SAFETY ATTENDANT\n<size=75%><color=#3498DB>✓ COMMUNICATION VERIFIED</color></size>");
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = "OUTSIDE SAFETY ATTENDANT\n<size=75%><color=#3498DB>✓ COMM VERIFIED</color></size>";
            }
        }

        /// <summary>
        /// Invoked when the attendant 3D collider is tapped in AR space.
        /// </summary>
        public void OnTap()
        {
            OnAttendantTapped?.Invoke(this);
        }

        /// <summary>
        /// Resets the attendant marker state.
        /// </summary>
        public void ResetMarker()
        {
            _isAssigned = false;
            _isCommunicationVerified = false;

            if (_vestMaterial != null)
            {
                _vestMaterial.color = ColorVestOrange;
            }

            if (_radioMaterial != null)
            {
                _radioMaterial.color = ColorRadioStandby;
            }

            if (_floatingLabel != null)
            {
                _floatingLabel.SetText("OUTSIDE SAFETY ATTENDANT\n<size=75%><color=#F39C12>● STANDBY (OUTSIDE)</color></size>");
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = "OUTSIDE SAFETY ATTENDANT\n<size=75%><color=#F39C12>● STANDBY</color></size>";
            }
        }

        /// <summary>
        /// Procedurally constructs the lightweight attendant silhouette, hard hat, radio, and label.
        /// </summary>
        public void EnsureVisuals()
        {
            var existingCollider = GetComponent<Collider>();
            if (existingCollider == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.85f, 0f);
                box.size = new Vector3(0.65f, 1.75f, 0.65f);
            }

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Mobile/Unlit (Supports Lightmap)")
                ?? Shader.Find("Unlit/Color");

            // 1. Base Stand Pedestal (Disc)
            var pedestal = transform.Find("AttendantPedestal");
            if (pedestal == null)
            {
                var pedObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pedObj.name = "AttendantPedestal";
                pedObj.transform.SetParent(transform, false);
                pedObj.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                pedObj.transform.localScale = new Vector3(0.55f, 0.02f, 0.55f);

                var coll = pedObj.GetComponent<Collider>();
                if (coll != null) DestroyImmediate(coll);

                var rend = pedObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    _baseMaterial = new Material(unlitShader) { color = ColorBasePedestal };
                    rend.sharedMaterial = _baseMaterial;
                }
            }

            // 2. Worker Torso with High-Vis Safety Vest
            var torso = transform.Find("AttendantTorso");
            if (torso == null)
            {
                var torsoObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                torsoObj.name = "AttendantTorso";
                torsoObj.transform.SetParent(transform, false);
                torsoObj.transform.localPosition = new Vector3(0f, 0.70f, 0f);
                torsoObj.transform.localScale = new Vector3(0.36f, 0.40f, 0.24f);

                var coll = torsoObj.GetComponent<Collider>();
                if (coll != null) DestroyImmediate(coll);

                _vestRenderer = torsoObj.GetComponent<Renderer>();
                if (_vestRenderer != null)
                {
                    _vestMaterial = new Material(unlitShader) { color = _isAssigned ? ColorVestAssigned : ColorVestOrange };
                    _vestRenderer.sharedMaterial = _vestMaterial;
                }
            }

            // 3. Reflective Stripe across Chest
            var stripe = transform.Find("ReflectiveStripe");
            if (stripe == null)
            {
                var stripeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stripeObj.name = "ReflectiveStripe";
                stripeObj.transform.SetParent(transform, false);
                stripeObj.transform.localPosition = new Vector3(0f, 0.72f, 0f);
                stripeObj.transform.localScale = new Vector3(0.365f, 0.05f, 0.245f);

                var coll = stripeObj.GetComponent<Collider>();
                if (coll != null) DestroyImmediate(coll);

                var rend = stripeObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.sharedMaterial = new Material(unlitShader) { color = new Color(0.92f, 0.95f, 0.98f) };
                }
            }

            // 4. Worker Head (Sphere)
            var head = transform.Find("AttendantHead");
            if (head == null)
            {
                var headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                headObj.name = "AttendantHead";
                headObj.transform.SetParent(transform, false);
                headObj.transform.localPosition = new Vector3(0f, 1.25f, 0f);
                headObj.transform.localScale = new Vector3(0.20f, 0.22f, 0.20f);

                var coll = headObj.GetComponent<Collider>();
                if (coll != null) DestroyImmediate(coll);

                var rend = headObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.sharedMaterial = new Material(unlitShader) { color = new Color(0.85f, 0.65f, 0.52f) };
                }
            }

            // 5. Hard Hat (Yellow Dome)
            var hardHat = transform.Find("HardHat");
            if (hardHat == null)
            {
                var hatObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hatObj.name = "HardHat";
                hatObj.transform.SetParent(transform, false);
                hatObj.transform.localPosition = new Vector3(0f, 1.34f, 0f);
                hatObj.transform.localScale = new Vector3(0.24f, 0.14f, 0.26f);

                var coll = hatObj.GetComponent<Collider>();
                if (coll != null) DestroyImmediate(coll);

                var rend = hatObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.sharedMaterial = new Material(unlitShader) { color = ColorHardHat };
                }
            }

            // 6. Two-Way Radio on Shoulder
            var radio = transform.Find("ShoulderRadio");
            if (radio == null)
            {
                var radioObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                radioObj.name = "ShoulderRadio";
                radioObj.transform.SetParent(transform, false);
                radioObj.transform.localPosition = new Vector3(0.16f, 0.96f, 0.08f);
                radioObj.transform.localScale = new Vector3(0.06f, 0.12f, 0.04f);

                var coll = radioObj.GetComponent<Collider>();
                if (coll != null) DestroyImmediate(coll);

                _radioRenderer = radioObj.GetComponent<Renderer>();
                if (_radioRenderer != null)
                {
                    _radioMaterial = new Material(unlitShader) { color = _isCommunicationVerified ? ColorRadioActive : ColorRadioStandby };
                    _radioRenderer.sharedMaterial = _radioMaterial;
                }
            }

            // 7. Camera-Facing ArFloatingLabel
            var labelObj = transform.Find("AttendantLabel");
            if (labelObj == null)
            {
                var go = new GameObject("AttendantLabel");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 1.70f, 0f);

                _floatingLabel = go.AddComponent<ArFloatingLabel>();
                _floatingLabel.SetText("OUTSIDE SAFETY ATTENDANT\n<size=75%><color=#F39C12>● STANDBY (OUTSIDE)</color></size>");
                _labelMesh = go.GetComponent<TextMeshPro>();
            }
            else
            {
                _floatingLabel = labelObj.GetComponent<ArFloatingLabel>();
                _labelMesh = labelObj.GetComponent<TextMeshPro>();
            }
        }

        private void OnDestroy()
        {
            if (_vestMaterial != null) DestroyImmediate(_vestMaterial);
            if (_radioMaterial != null) DestroyImmediate(_radioMaterial);
            if (_baseMaterial != null) DestroyImmediate(_baseMaterial);
        }
    }
}
