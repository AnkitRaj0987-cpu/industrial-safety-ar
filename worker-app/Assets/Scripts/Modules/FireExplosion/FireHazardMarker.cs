// FireHazardMarker.cs
// Namespace : IndustrialSafetyAR.Modules.FireExplosion
//
// Represents the 3D Fire Hazard placed in AR space.
// Provides lightweight procedural placeholder visuals using standard Unity primitives
// and handles worker tap detection.

using System;
using TMPro;
using UnityEngine;

namespace IndustrialSafetyAR.Modules.FireExplosion
{
    /// <summary>
    /// Interactive AR marker representing a fire hazard.
    /// Handles physical raycast tapping, procedural placeholder rendering,
    /// and detection state transitions.
    /// </summary>
    [SelectionBase]
    public class FireHazardMarker : MonoBehaviour, IIdentifiableHazard
    {
        [Header("Hazard Metadata")]
        [Tooltip("Unique ID matching module.json / rubric.json")]
        [SerializeField]
        private string _hazardId = "hazard_electrical_conveyor_fire";

        [Tooltip("Hazard class identifier")]
        [SerializeField]
        private string _hazardClass = "class_e_electrical";

        [Header("Visual Elements")]
        [SerializeField]
        private Renderer _beaconRenderer;

        [SerializeField]
        private TextMeshPro _labelMesh;

        private Material _beaconMaterial;
        private bool _isDetected;
        private float _pulseTimer;

        private static readonly Color ColorActiveHazard = new Color(1.0f, 0.32f, 0.05f); // Flame orange
        private static readonly Color ColorDetectedHazard = new Color(0.2f, 0.85f, 0.3f); // Confirmed green
        private static readonly Color ColorHousing = new Color(0.32f, 0.34f, 0.38f);     // Industrial steel
        private static readonly Color ColorBase = new Color(0.15f, 0.15f, 0.18f);        // Heavy base

        public string HazardId => _hazardId;
        public string HazardClass => _hazardClass;
        public bool IsDetected => _isDetected;

        public event Action<IIdentifiableHazard> OnDetected;

        private void Awake()
        {
            EnsureVisuals();
        }

        private bool _isIdentified;
        private bool _isAlarmActive;

        public bool IsIdentified => _isIdentified;
        public bool IsAlarmActive => _isAlarmActive;

        private void Update()
        {
            _pulseTimer += Time.deltaTime * 3.5f;

            if (_isAlarmActive)
            {
                // Rapid emergency strobe flashing between red and yellow
                if (_beaconMaterial != null)
                {
                    float strobe = 0.5f + 0.5f * Mathf.Sin(_pulseTimer * 4f);
                    _beaconMaterial.color = Color.Lerp(new Color(1f, 0.1f, 0.1f), new Color(1f, 0.85f, 0.1f), strobe);
                }
                return;
            }

            if (_isDetected) return;

            // Subtle pulsing alert effect on the hazard beacon prior to detection
            if (_beaconMaterial != null)
            {
                float pulse = 0.85f + 0.15f * Mathf.Sin(_pulseTimer);
                Color currentColor = ColorActiveHazard * pulse;
                currentColor.a = 1f;
                _beaconMaterial.color = currentColor;
            }
        }

        private void OnDestroy()
        {
            if (_beaconMaterial != null)
            {
                Destroy(_beaconMaterial);
            }
        }

        /// <summary>
        /// Confirms detection by the worker, updating visuals and raising the detection event.
        /// </summary>
        public void AcknowledgeDetection()
        {
            if (_isDetected) return;

            _isDetected = true;

            if (_beaconMaterial != null)
            {
                _beaconMaterial.color = ColorDetectedHazard;
            }

            if (_labelMesh != null)
            {
                _labelMesh.text = "HAZARD DETECTED\n(Inspection Required)";
                _labelMesh.color = ColorDetectedHazard;
            }

            Debug.Log($"[FireHazardMarker] Hazard '{_hazardId}' detected & acknowledged by worker.");
            OnDetected?.Invoke(this);
        }

        /// <summary>
        /// Updates the hazard marker visual to indicate successful identification.
        /// </summary>
        public void MarkIdentified(string hazardClass)
        {
            _isIdentified = true;

            if (_labelMesh != null)
            {
                _labelMesh.text = "IDENTIFIED HAZARD\nClass E Electrical Fire";
                _labelMesh.color = new Color(0.2f, 0.9f, 0.95f);
            }

            if (_beaconMaterial != null)
            {
                _beaconMaterial.color = new Color(0.2f, 0.9f, 0.95f);
            }

            Debug.Log($"[FireHazardMarker] Hazard '{_hazardId}' identified as '{hazardClass}'.");
        }

        /// <summary>
        /// Triggers visual siren and alarm indicators on the hazard marker.
        /// </summary>
        public void TriggerAlarmVisual()
        {
            _isAlarmActive = true;

            if (_labelMesh != null)
            {
                _labelMesh.text = "ALARM ACTIVE\nClass E Conveyor Fire";
                _labelMesh.color = new Color(1f, 0.25f, 0.2f);
            }

            Debug.Log($"[FireHazardMarker] Emergency alarm visual activated on '{_hazardId}'.");
        }

        /// <summary>
        /// Procedurally constructs a recognizable industrial hazard marker if not created from a prefab.
        /// </summary>
        public void EnsureVisuals()
        {
            if (transform.childCount > 0 && _beaconRenderer != null)
            {
                return;
            }

            // 1. Heavy warning base (cylinder)
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "HazardBase";
            baseObj.transform.SetParent(transform, false);
            baseObj.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            baseObj.transform.localScale = new Vector3(0.48f, 0.015f, 0.48f);
            var baseMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = ColorBase
            };
            baseObj.GetComponent<Renderer>().material = baseMat;

            // 2. Equipment Housing (electrical box / conveyor simulator unit)
            GameObject housingObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housingObj.name = "EquipmentHousing";
            housingObj.transform.SetParent(transform, false);
            housingObj.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            housingObj.transform.localScale = new Vector3(0.32f, 0.26f, 0.32f);
            var housingMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = ColorHousing
            };
            housingObj.GetComponent<Renderer>().material = housingMat;

            // 3. Flame Alert Beacon / Hazard indicator
            GameObject beaconObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beaconObj.name = "HazardBeacon";
            beaconObj.transform.SetParent(transform, false);
            beaconObj.transform.localPosition = new Vector3(0f, 0.34f, 0f);
            beaconObj.transform.localScale = new Vector3(0.14f, 0.08f, 0.14f);
            _beaconMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = ColorActiveHazard
            };
            _beaconRenderer = beaconObj.GetComponent<Renderer>();
            _beaconRenderer.material = _beaconMaterial;

            // 4. Floating 3D Text Label
            GameObject labelObj = new GameObject("HazardLabel");
            labelObj.transform.SetParent(transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            _labelMesh = labelObj.AddComponent<TextMeshPro>();
            _labelMesh.text = "! FIRE HAZARD !\nClass E Electrical";
            _labelMesh.fontSize = 2.2f;
            _labelMesh.alignment = TextAlignmentOptions.Center;
            _labelMesh.color = ColorActiveHazard;
            _labelMesh.rectTransform.sizeDelta = new Vector2(2.5f, 1.0f);

            // 5. Ensure overall collider exists for interaction
            var boxCol = GetComponent<BoxCollider>();
            if (boxCol == null)
            {
                boxCol = gameObject.AddComponent<BoxCollider>();
            }
            boxCol.center = new Vector3(0f, 0.22f, 0f);
            boxCol.size = new Vector3(0.5f, 0.46f, 0.5f);
        }
    }
}
