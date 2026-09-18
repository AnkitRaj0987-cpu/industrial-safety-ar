// FireHazardMarker.cs
// Namespace : IndustrialSafetyAR.Modules.FireExplosion
//
// Represents the 3D Fire Hazard placed in AR space.
// Provides procedural industrial electrical cabinet visuals with active fire flame tongues,
// crackling electrical sparks (Class E), emergency indicators, and camera-facing ArFloatingLabels.

using System;
using TMPro;
using UnityEngine;

namespace IndustrialSafetyAR.Modules.FireExplosion
{
    /// <summary>
    /// Interactive AR marker representing a Class E electrical fire hazard.
    /// Features recognizable procedural industrial cabinet modeling, animated flame and electrical arc,
    /// and dynamic camera-facing ArFloatingLabels with compact mobile AR scaling.
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

        private ArFloatingLabel _floatingLabel;
        private Material _beaconMaterial;
        private Material _cabinetMaterial;
        private Material _flameMaterial;
        private Material _sparkMaterial;

        private bool _isDetected;
        private float _pulseTimer;
        private float _flickerTimer;

        private static readonly Color ColorActiveHazard = new Color(1.0f, 0.32f, 0.05f); // Flame orange
        private static readonly Color ColorDetectedHazard = new Color(0.2f, 0.85f, 0.3f); // Confirmed green
        private static readonly Color ColorHousing = new Color(0.18f, 0.20f, 0.24f);     // Industrial dark steel
        private static readonly Color ColorBase = new Color(0.12f, 0.13f, 0.16f);        // Heavy steel skid
        private static readonly Color ColorCharred = new Color(0.09f, 0.09f, 0.10f);     // Scorch blackened

        public string HazardId => _hazardId;
        public string HazardClass => _hazardClass;
        public bool IsDetected => _isDetected;
        public ArFloatingLabel FloatingLabel => _floatingLabel;
        public TextMeshPro LabelMesh => _labelMesh;

        public event Action<IIdentifiableHazard> OnDetected;

        private void Awake()
        {
            EnsureVisuals();
        }

        private bool _isIdentified;
        private bool _isAlarmActive;
        private bool _isSafeDistanceConfirmed;
        private bool _isExtinguished;

        private GameObject _fireRoot;
        private Transform _flameTransform;
        private Transform _sparkTransform;

        private GameObject _dangerZoneRoot;
        private LineRenderer _dangerZoneLine;
        private ArFloatingLabel _dangerZoneLabelComp;

        private GameObject _aimTargetRoot;
        private ArFloatingLabel _aimLabelComp;
        private GameObject _dischargeCloudObj;

        public bool IsIdentified => _isIdentified;
        public bool IsAlarmActive => _isAlarmActive;
        public bool IsSafeDistanceConfirmed => _isSafeDistanceConfirmed;
        public bool IsExtinguished => _isExtinguished;

        private void Update()
        {
            if (_isExtinguished) return;

            _pulseTimer += Time.deltaTime * 3.5f;
            _flickerTimer += Time.deltaTime * 18f;

            // 1. Fire Flame & Electrical Arc Spark animation
            if (_flameTransform != null)
            {
                float flickerY = 0.15f + 0.035f * Mathf.Sin(_flickerTimer * 1.3f) + 0.02f * Mathf.Sin(_flickerTimer * 3.1f);
                float flickerXZ = 0.13f + 0.02f * Mathf.Cos(_flickerTimer * 2.1f);
                _flameTransform.localScale = new Vector3(flickerXZ, flickerY, flickerXZ);
            }

            if (_sparkTransform != null)
            {
                // Rapid high-frequency electrical arc flicker (Class E indication)
                bool sparkActive = Mathf.Sin(_flickerTimer * 4.7f) > -0.2f;
                _sparkTransform.gameObject.SetActive(sparkActive);
                if (sparkActive && _sparkMaterial != null)
                {
                    float intensity = 0.7f + 0.3f * Mathf.Sin(_flickerTimer * 6.5f);
                    _sparkMaterial.color = new Color(0.55f * intensity, 0.90f * intensity, 1.0f * intensity);
                }
            }

            // 2. Alarm or Pre-detection Warning Beacon Strobe
            if (_isAlarmActive)
            {
                if (_beaconMaterial != null)
                {
                    float strobe = 0.5f + 0.5f * Mathf.Sin(_pulseTimer * 4f);
                    _beaconMaterial.color = Color.Lerp(new Color(1f, 0.1f, 0.1f), new Color(1f, 0.85f, 0.1f), strobe);
                }
                return;
            }

            if (_isDetected) return;

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
            if (_beaconMaterial != null) Destroy(_beaconMaterial);
            if (_cabinetMaterial != null) Destroy(_cabinetMaterial);
            if (_flameMaterial != null) Destroy(_flameMaterial);
            if (_sparkMaterial != null) Destroy(_sparkMaterial);
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

            if (_floatingLabel != null)
            {
                _floatingLabel.SetText("✓ DETECTED: CLASS E");
                _floatingLabel.SetColor(ColorDetectedHazard);
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = "✓ DETECTED: CLASS E";
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

            Color identifiedColor = new Color(0.2f, 0.9f, 0.95f);

            if (_floatingLabel != null)
            {
                _floatingLabel.SetText("✓ CLASS E CONFIRMED");
                _floatingLabel.SetColor(identifiedColor);
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = "✓ CLASS E CONFIRMED";
                _labelMesh.color = identifiedColor;
            }

            if (_beaconMaterial != null)
            {
                _beaconMaterial.color = identifiedColor;
            }

            Debug.Log($"[FireHazardMarker] Hazard '{_hazardId}' identified as '{hazardClass}'.");
        }

        /// <summary>
        /// Triggers visual siren and alarm indicators on the hazard marker.
        /// </summary>
        public void TriggerAlarmVisual()
        {
            _isAlarmActive = true;

            Color alarmColor = new Color(1f, 0.25f, 0.2f);

            if (_floatingLabel != null)
            {
                _floatingLabel.SetText("🚨 ALARM ACTIVE");
                _floatingLabel.SetColor(alarmColor);
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = "🚨 ALARM ACTIVE";
                _labelMesh.color = alarmColor;
            }

            Debug.Log($"[FireHazardMarker] Emergency alarm visual activated on '{_hazardId}'.");
        }

        /// <summary>
        /// Updates the hazard marker visual when the worker selects the CO2 extinguisher.
        /// </summary>
        public void MarkExtinguisherSelected(string extinguisherId)
        {
            Color selectedColor = new Color(0.25f, 0.95f, 0.4f);

            if (_floatingLabel != null)
            {
                _floatingLabel.SetText("CO2 SELECTED (2m STANDOFF)");
                _floatingLabel.SetColor(selectedColor);
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = "CO2 SELECTED (2m STANDOFF)";
                _labelMesh.color = selectedColor;
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

            Color safeColor = new Color(0.2f, 0.88f, 0.35f);

            if (_dangerZoneLine != null && _dangerZoneLine.material != null)
            {
                _dangerZoneLine.material.color = new Color(0.2f, 0.88f, 0.35f, 0.95f);
            }

            if (_dangerZoneLabelComp != null)
            {
                _dangerZoneLabelComp.SetText("✓ 2.0m SAFE DISTANCE CONFIRMED");
                _dangerZoneLabelComp.SetColor(safeColor);
            }

            if (_floatingLabel != null)
            {
                _floatingLabel.SetText("✓ 2.0m SAFE DISTANCE");
                _floatingLabel.SetColor(safeColor);
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = "✓ 2.0m SAFE DISTANCE";
                _labelMesh.color = safeColor;
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

            if (_fireRoot != null)
            {
                _fireRoot.SetActive(false);
            }

            if (_cabinetMaterial != null)
            {
                _cabinetMaterial.color = ColorCharred;
            }

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
                _beaconMaterial.color = new Color(0.25f, 0.25f, 0.28f);
            }

            Color extinguishedColor = new Color(0.25f, 0.95f, 0.45f);

            if (_floatingLabel != null)
            {
                _floatingLabel.SetText("✓ FIRE SUPPRESSED");
                _floatingLabel.SetColor(extinguishedColor);
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = "✓ FIRE SUPPRESSED";
                _labelMesh.color = extinguishedColor;
            }

            Debug.Log($"[FireHazardMarker] Fire suppressed on '{_hazardId}' via CO2 discharge.");
        }

        /// <summary>
        /// Procedurally constructs standard industrial electrical fire visuals using primitives.
        /// </summary>
        public void EnsureVisuals()
        {
            if (transform.childCount > 0 && _beaconRenderer != null)
            {
                return;
            }

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard");

            // 1. Heavy Steel Equipment Skid Base
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "HazardSkidBase";
            baseObj.transform.SetParent(transform, false);
            baseObj.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            baseObj.transform.localScale = new Vector3(0.44f, 0.024f, 0.44f);
            var baseMat = new Material(litShader) { color = ColorBase };
            baseObj.GetComponent<Renderer>().material = baseMat;

            // Caution border perimeter ring on the floor
            GameObject cautionBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cautionBorder.name = "HazardCautionRim";
            cautionBorder.transform.SetParent(transform, false);
            cautionBorder.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            cautionBorder.transform.localScale = new Vector3(0.46f, 0.003f, 0.46f);
            var cautionMat = new Material(unlitShader) { color = new Color(0.95f, 0.75f, 0.10f) };
            cautionBorder.GetComponent<Renderer>().material = cautionMat;
            var cautionCol = cautionBorder.GetComponent<Collider>();
            if (cautionCol != null) DestroyImmediate(cautionCol);

            // 2. Electrical Control Cabinet (Conveyor Drive Unit)
            GameObject cabinetObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinetObj.name = "ElectricalCabinet";
            cabinetObj.transform.SetParent(transform, false);
            cabinetObj.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            cabinetObj.transform.localScale = new Vector3(0.34f, 0.36f, 0.24f);
            _cabinetMaterial = new Material(litShader) { color = ColorHousing };
            cabinetObj.GetComponent<Renderer>().material = _cabinetMaterial;

            // Cabinet front door panel detailing
            GameObject doorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorObj.name = "CabinetDoor";
            doorObj.transform.SetParent(cabinetObj.transform, false);
            doorObj.transform.localPosition = new Vector3(0f, 0f, 0.51f);
            doorObj.transform.localScale = new Vector3(0.90f, 0.90f, 0.04f);
            var doorMat = new Material(litShader) { color = new Color(0.24f, 0.26f, 0.30f) };
            doorObj.GetComponent<Renderer>().material = doorMat;
            var doorCol = doorObj.GetComponent<Collider>();
            if (doorCol != null) DestroyImmediate(doorCol);

            // High Voltage Danger Plaque on cabinet door
            GameObject signPlate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            signPlate.name = "HighVoltageDangerSign";
            signPlate.transform.SetParent(doorObj.transform, false);
            signPlate.transform.localPosition = new Vector3(0f, 0.18f, 0.52f);
            // Rotated 180 around Y so quad front face points outward along +Z (towards viewer)
            signPlate.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            signPlate.transform.localScale = new Vector3(0.52f, 0.20f, 1f);
            var signMat = new Material(unlitShader) { color = new Color(1.0f, 0.82f, 0.10f) }; // Caution yellow
            signPlate.GetComponent<Renderer>().material = signMat;
            var signCol = signPlate.GetComponent<Collider>();
            if (signCol != null) DestroyImmediate(signCol);

            // Danger Sign text
            GameObject signTextObj = new GameObject("SignText");
            signTextObj.transform.SetParent(signPlate.transform, false);
            signTextObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            var signTmp = signTextObj.AddComponent<TextMeshPro>();
            var defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset;
            if (defaultFont != null) signTmp.font = defaultFont;
            signTmp.text = "⚡ DANGER: 480V\nELECTRICAL HAZARD";
            signTmp.fontSize = 0.52f;
            signTmp.alignment = TextAlignmentOptions.Center;
            signTmp.color = Color.black;
            signTmp.rectTransform.sizeDelta = new Vector2(1.0f, 0.45f);

            // Side power conduit pipe
            GameObject conduit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            conduit.name = "PowerConduit";
            conduit.transform.SetParent(transform, false);
            conduit.transform.localPosition = new Vector3(0.18f, 0.12f, 0f);
            conduit.transform.localScale = new Vector3(0.035f, 0.12f, 0.035f);
            var conduitMat = new Material(litShader) { color = new Color(0.12f, 0.12f, 0.14f) };
            conduit.GetComponent<Renderer>().material = conduitMat;
            var conduitCol = conduit.GetComponent<Collider>();
            if (conduitCol != null) DestroyImmediate(conduitCol);

            // Emergency mushroom button on top
            GameObject eStop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            eStop.name = "EmergencyStopButton";
            eStop.transform.SetParent(transform, false);
            eStop.transform.localPosition = new Vector3(0.11f, 0.39f, 0.05f);
            eStop.transform.localScale = new Vector3(0.04f, 0.015f, 0.04f);
            var eStopMat = new Material(unlitShader) { color = new Color(0.9f, 0.1f, 0.1f) };
            eStop.GetComponent<Renderer>().material = eStopMat;
            var eStopCol = eStop.GetComponent<Collider>();
            if (eStopCol != null) DestroyImmediate(eStopCol);

            // 3. Flame Alert Beacon / Strobe on top corner
            GameObject beaconObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beaconObj.name = "HazardBeacon";
            beaconObj.transform.SetParent(transform, false);
            beaconObj.transform.localPosition = new Vector3(-0.11f, 0.40f, 0.05f);
            beaconObj.transform.localScale = new Vector3(0.05f, 0.025f, 0.05f);
            _beaconMaterial = new Material(litShader) { color = ColorActiveHazard };
            _beaconRenderer = beaconObj.GetComponent<Renderer>();
            _beaconRenderer.material = _beaconMaterial;
            var beaconCol = beaconObj.GetComponent<Collider>();
            if (beaconCol != null) DestroyImmediate(beaconCol);

            // 4. Fire Flame & Electrical Arc Spark Group
            _fireRoot = new GameObject("FireVisualEffect");
            _fireRoot.transform.SetParent(transform, false);
            _fireRoot.transform.localPosition = new Vector3(0f, 0.38f, 0f);

            // Main outer flame tongue
            GameObject flameObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flameObj.name = "FlameTongueOuter";
            flameObj.transform.SetParent(_fireRoot.transform, false);
            flameObj.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            flameObj.transform.localScale = new Vector3(0.13f, 0.15f, 0.13f);
            _flameMaterial = new Material(unlitShader) { color = new Color(1.0f, 0.35f, 0.02f, 0.95f) };
            flameObj.GetComponent<Renderer>().material = _flameMaterial;
            _flameTransform = flameObj.transform;
            var flameCol = flameObj.GetComponent<Collider>();
            if (flameCol != null) DestroyImmediate(flameCol);

            // Inner intense flame core
            GameObject flameInnerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flameInnerObj.name = "FlameCoreInner";
            flameInnerObj.transform.SetParent(_flameTransform, false);
            flameInnerObj.transform.localPosition = new Vector3(0f, 0f, 0f);
            flameInnerObj.transform.localScale = new Vector3(0.65f, 0.85f, 0.65f);
            var innerFlameMat = new Material(unlitShader) { color = new Color(1.0f, 0.82f, 0.15f, 0.98f) };
            flameInnerObj.GetComponent<Renderer>().material = innerFlameMat;
            var innerFlameCol = flameInnerObj.GetComponent<Collider>();
            if (innerFlameCol != null) DestroyImmediate(innerFlameCol);

            // Crackling electrical arc spark (Class E indicator)
            GameObject sparkObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sparkObj.name = "ElectricalArcSpark";
            sparkObj.transform.SetParent(_fireRoot.transform, false);
            sparkObj.transform.localPosition = new Vector3(0.04f, 0.04f, 0.02f);
            sparkObj.transform.localScale = new Vector3(0.055f, 0.055f, 0.055f);
            _sparkMaterial = new Material(unlitShader) { color = new Color(0.6f, 0.92f, 1.0f, 1.0f) };
            sparkObj.GetComponent<Renderer>().material = _sparkMaterial;
            _sparkTransform = sparkObj.transform;
            var sparkCol = sparkObj.GetComponent<Collider>();
            if (sparkCol != null) DestroyImmediate(sparkCol);

            // Dark smoldering smoke plume
            GameObject smokeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            smokeObj.name = "SmokePlume";
            smokeObj.transform.SetParent(_fireRoot.transform, false);
            smokeObj.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            smokeObj.transform.localScale = new Vector3(0.10f, 0.10f, 0.10f);
            var smokeMat = new Material(unlitShader) { color = new Color(0.18f, 0.18f, 0.20f, 0.55f) };
            smokeObj.GetComponent<Renderer>().material = smokeMat;
            var smokeCol = smokeObj.GetComponent<Collider>();
            if (smokeCol != null) DestroyImmediate(smokeCol);

            // Procedural Particle System for active flickering fire and smoke
            TryAddProceduralParticles(_fireRoot);

            // 5. Camera-Facing ArFloatingLabel (compact, elevated above flames)
            _floatingLabel = ArFloatingLabel.Create(
                gameObject,
                new Vector3(0f, 0.68f, 0f),
                "⚡ CLASS E: ELECTRICAL FIRE",
                ColorActiveHazard,
                width: 0.36f,
                height: 0.09f,
                fontSize: 0.72f,
                ArBillboardMode.ScreenAligned);
            _labelMesh = _floatingLabel.LabelMesh;

            // 6. Interaction BoxCollider (encompasses cabinet + flame)
            var boxCol = GetComponent<BoxCollider>();
            if (boxCol == null) boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, 0.24f, 0f);
            boxCol.size = new Vector3(0.48f, 0.50f, 0.48f);

            // 7. 2.0m Standoff Danger Zone Ring
            _dangerZoneRoot = new GameObject("DangerZoneRing");
            _dangerZoneRoot.transform.SetParent(transform, false);
            _dangerZoneRoot.transform.localPosition = Vector3.zero;

            _dangerZoneLine = _dangerZoneRoot.AddComponent<LineRenderer>();
            _dangerZoneLine.useWorldSpace = false;
            _dangerZoneLine.loop = true;
            _dangerZoneLine.positionCount = 48;
            _dangerZoneLine.startWidth = 0.04f;
            _dangerZoneLine.endWidth = 0.04f;
            _dangerZoneLine.material = new Material(unlitShader)
            {
                color = new Color(1.0f, 0.25f, 0.15f, 0.95f)
            };

            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2f / 48f;
                _dangerZoneLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * 2.0f, 0.02f, Mathf.Sin(angle) * 2.0f));
            }

            _dangerZoneLabelComp = ArFloatingLabel.Create(
                _dangerZoneRoot,
                new Vector3(0f, 0.14f, 2.02f),
                "2.0m DANGER BOUNDARY",
                new Color(1.0f, 0.35f, 0.2f),
                width: 0.36f,
                height: 0.085f,
                fontSize: 0.70f,
                ArBillboardMode.ScreenAligned);

            _dangerZoneRoot.SetActive(false);

            // 8. Base of Fire Aim Target
            _aimTargetRoot = new GameObject("AimBaseTarget");
            _aimTargetRoot.transform.SetParent(transform, false);
            _aimTargetRoot.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            var aimLine = _aimTargetRoot.AddComponent<LineRenderer>();
            aimLine.useWorldSpace = false;
            aimLine.loop = true;
            aimLine.positionCount = 32;
            aimLine.startWidth = 0.025f;
            aimLine.endWidth = 0.025f;
            aimLine.material = new Material(unlitShader)
            {
                color = new Color(0.0f, 0.9f, 1.0f, 0.95f)
            };
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2f / 32f;
                aimLine.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.28f, 0.01f, Mathf.Sin(a) * 0.28f));
            }

            _aimLabelComp = ArFloatingLabel.Create(
                _aimTargetRoot,
                new Vector3(0f, 0.10f, 0.32f),
                "BASE OF FIRE",
                new Color(0.0f, 0.9f, 1.0f),
                width: 0.28f,
                height: 0.075f,
                fontSize: 0.68f,
                ArBillboardMode.ScreenAligned);

            _aimTargetRoot.SetActive(false);

            // 9. Extinguisher CO2 discharge vapor cone
            _dischargeCloudObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _dischargeCloudObj.name = "CO2DischargeVapor";
            _dischargeCloudObj.transform.SetParent(transform, false);
            _dischargeCloudObj.transform.localPosition = new Vector3(0f, 0.24f, 0f);
            _dischargeCloudObj.transform.localScale = new Vector3(0.55f, 0.28f, 0.55f);
            var dischargeCol = _dischargeCloudObj.GetComponent<Collider>();
            if (dischargeCol != null) DestroyImmediate(dischargeCol);

            var vaporMat = new Material(litShader)
            {
                color = new Color(0.85f, 0.95f, 1.0f, 0.55f)
            };
            _dischargeCloudObj.GetComponent<Renderer>().material = vaporMat;
            _dischargeCloudObj.SetActive(false);
        }

        private void TryAddProceduralParticles(GameObject parent)
        {
            if (parent == null) return;
            try
            {
                var ps = parent.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.duration = 1.0f;
                main.loop = true;
                main.startLifetime = 0.45f;
                main.startSpeed = 0.35f;
                main.startSize = 0.08f;
                main.startColor = new Color(1.0f, 0.45f, 0.05f, 0.85f);
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.playOnAwake = true;

                var emission = ps.emission;
                emission.rateOverTime = 16f;

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 10f;
                shape.radius = 0.03f;
                shape.rotation = new Vector3(-90f, 0f, 0f);

                var renderer = parent.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")
                        ?? Shader.Find("Particles/Standard Unlit")
                        ?? Shader.Find("Sprites/Default")
                        ?? Shader.Find("Standard"));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FireHazardMarker] ParticleSystem fallback: {ex.Message}");
            }
        }
    }
}
