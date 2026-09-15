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
        private bool _isSafeDistanceConfirmed;
        private bool _isExtinguished;

        private GameObject _dangerZoneRoot;
        private LineRenderer _dangerZoneLine;
        private Renderer _dangerZoneDiscRenderer;
        private TextMeshPro _dangerZoneLabel;

        private GameObject _aimTargetRoot;
        private GameObject _dischargeCloudObj;

        public bool IsIdentified => _isIdentified;
        public bool IsAlarmActive => _isAlarmActive;
        public bool IsSafeDistanceConfirmed => _isSafeDistanceConfirmed;
        public bool IsExtinguished => _isExtinguished;

        private void Update()
        {
            if (_isExtinguished) return;

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
        /// Updates the hazard marker visual when the worker selects the CO2 extinguisher.
        /// </summary>
        public void MarkExtinguisherSelected(string extinguisherId)
        {
            if (_labelMesh != null)
            {
                _labelMesh.text = "CO2 EXTINGUISHER SELECTED\nMaintain 2m Distance";
                _labelMesh.color = new Color(0.25f, 0.95f, 0.4f);
            }

            Debug.Log($"[FireHazardMarker] Extinguisher '{extinguisherId}' selected for '{_hazardId}'.");
        }

        /// <summary>
        /// Shows or hides the 2.0m standoff distance ring in AR space.
        /// </summary>
        public void ShowDistanceZoneRing(bool show)
        {
            if (_dangerZoneRoot != null)
            {
                _dangerZoneRoot.SetActive(show);
            }
        }

        /// <summary>
        /// Updates the danger zone visuals to confirmed safe standoff status (turns green).
        /// </summary>
        public void MarkSafeDistanceConfirmed()
        {
            _isSafeDistanceConfirmed = true;

            if (_dangerZoneLine != null && _dangerZoneLine.material != null)
            {
                _dangerZoneLine.material.color = new Color(0.2f, 0.88f, 0.35f, 0.95f);
            }

            if (_dangerZoneDiscRenderer != null && _dangerZoneDiscRenderer.material != null)
            {
                _dangerZoneDiscRenderer.material.color = new Color(0.2f, 0.88f, 0.35f, 0.15f);
            }

            if (_dangerZoneLabel != null)
            {
                _dangerZoneLabel.text = "SAFE STANDOFF MAINTAINED\n(>= 2.0m Verified)";
                _dangerZoneLabel.color = new Color(0.2f, 0.88f, 0.35f);
            }

            if (_labelMesh != null)
            {
                _labelMesh.text = "SAFE DISTANCE CONFIRMED\nReady to Extinguish";
                _labelMesh.color = new Color(0.2f, 0.88f, 0.35f);
            }

            Debug.Log($"[FireHazardMarker] Safe distance confirmed on '{_hazardId}'.");
        }

        /// <summary>
        /// Shows or hides the AR aim target indicator at the base of the fire.
        /// </summary>
        public void ShowAimTarget(bool show)
        {
            if (_aimTargetRoot != null)
            {
                _aimTargetRoot.SetActive(show);
            }
        }

        /// <summary>
        /// Displays the CO2 discharge visual effect and marks the fire extinguished.
        /// </summary>
        public void TriggerExtinguisherDischargeVisual()
        {
            _isExtinguished = true;

            if (_aimTargetRoot != null)
            {
                _aimTargetRoot.SetActive(false);
            }

            if (_dischargeCloudObj != null)
            {
                _dischargeCloudObj.SetActive(true);
            }

            if (_beaconMaterial != null)
            {
                _beaconMaterial.color = new Color(0.25f, 0.25f, 0.28f); // Extinguished charred/smoke state
            }

            if (_labelMesh != null)
            {
                _labelMesh.text = "FIRE EXTINGUISHED\nClass E Conveyor Suppressed";
                _labelMesh.color = new Color(0.25f, 0.95f, 0.45f);
            }

            Debug.Log($"[FireHazardMarker] Fire suppressed on '{_hazardId}' via CO2 discharge.");
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

            // 6. 2.0m Standoff Danger Zone Ring
            _dangerZoneRoot = new GameObject("DangerZoneRing");
            _dangerZoneRoot.transform.SetParent(transform, false);
            _dangerZoneRoot.transform.localPosition = Vector3.zero;

            _dangerZoneLine = _dangerZoneRoot.AddComponent<LineRenderer>();
            _dangerZoneLine.useWorldSpace = false;
            _dangerZoneLine.loop = true;
            _dangerZoneLine.positionCount = 48;
            _dangerZoneLine.startWidth = 0.04f;
            _dangerZoneLine.endWidth = 0.04f;
            var lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard"))
            {
                color = new Color(1.0f, 0.22f, 0.18f, 0.9f)
            };
            _dangerZoneLine.material = lineMat;

            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2f / 48f;
                _dangerZoneLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * 2.0f, 0.015f, Mathf.Sin(angle) * 2.0f));
            }

            GameObject discObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            discObj.name = "DangerZoneDisc";
            discObj.transform.SetParent(_dangerZoneRoot.transform, false);
            discObj.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            discObj.transform.localScale = new Vector3(4.0f, 0.002f, 4.0f);
            var col = discObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var discMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(1.0f, 0.2f, 0.1f, 0.15f)
            };
            _dangerZoneDiscRenderer = discObj.GetComponent<Renderer>();
            _dangerZoneDiscRenderer.material = discMat;

            GameObject zoneLabelObj = new GameObject("ZoneLabel");
            zoneLabelObj.transform.SetParent(_dangerZoneRoot.transform, false);
            zoneLabelObj.transform.localPosition = new Vector3(0f, 0.2f, 2.05f);
            _dangerZoneLabel = zoneLabelObj.AddComponent<TextMeshPro>();
            _dangerZoneLabel.text = "2.0m DANGER ZONE BOUNDARY\n(Do Not Approach)";
            _dangerZoneLabel.fontSize = 1.8f;
            _dangerZoneLabel.alignment = TextAlignmentOptions.Center;
            _dangerZoneLabel.color = new Color(1.0f, 0.3f, 0.2f);
            _dangerZoneLabel.rectTransform.sizeDelta = new Vector2(3f, 1f);

            _dangerZoneRoot.SetActive(false);

            // 7. Base of Fire Aim Target
            _aimTargetRoot = new GameObject("AimBaseTarget");
            _aimTargetRoot.transform.SetParent(transform, false);
            _aimTargetRoot.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            var aimLine = _aimTargetRoot.AddComponent<LineRenderer>();
            aimLine.useWorldSpace = false;
            aimLine.loop = true;
            aimLine.positionCount = 32;
            aimLine.startWidth = 0.025f;
            aimLine.endWidth = 0.025f;
            aimLine.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard"))
            {
                color = new Color(0.0f, 0.9f, 1.0f, 0.95f) // Bright cyan target
            };
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2f / 32f;
                aimLine.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.28f, 0.01f, Mathf.Sin(a) * 0.28f));
            }

            GameObject aimLabelObj = new GameObject("AimLabel");
            aimLabelObj.transform.SetParent(_aimTargetRoot.transform, false);
            aimLabelObj.transform.localPosition = new Vector3(0f, 0.12f, 0.35f);
            var aimLabel = aimLabelObj.AddComponent<TextMeshPro>();
            aimLabel.text = "AIM AT BASE OF FIRE";
            aimLabel.fontSize = 1.5f;
            aimLabel.alignment = TextAlignmentOptions.Center;
            aimLabel.color = new Color(0.0f, 0.9f, 1.0f);
            aimLabel.rectTransform.sizeDelta = new Vector2(2.5f, 0.8f);

            _aimTargetRoot.SetActive(false);

            // 8. Extinguisher CO2 discharge vapor cone
            _dischargeCloudObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _dischargeCloudObj.name = "CO2DischargeVapor";
            _dischargeCloudObj.transform.SetParent(transform, false);
            _dischargeCloudObj.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            _dischargeCloudObj.transform.localScale = new Vector3(0.55f, 0.28f, 0.55f);
            var dischargeCol = _dischargeCloudObj.GetComponent<Collider>();
            if (dischargeCol != null) Destroy(dischargeCol);

            var vaporMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(0.85f, 0.95f, 1.0f, 0.45f) // Frosted white CO2 cloud
            };
            _dischargeCloudObj.GetComponent<Renderer>().material = vaporMat;
            _dischargeCloudObj.SetActive(false);
        }
    }
}
