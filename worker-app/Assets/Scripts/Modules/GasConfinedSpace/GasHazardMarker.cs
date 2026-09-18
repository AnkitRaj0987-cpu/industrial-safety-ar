// GasHazardMarker.cs
// Namespace : IndustrialSafetyAR.Modules.GasConfinedSpace
//
// Represents the 3D Gas Hazard & Confined Space Portal placed in AR space.
// Provides procedural industrial manhole/vault visuals with dark interior void,
// semi-transparent volumetric gas accumulation haze, camera-facing ArFloatingLabels,
// and 3.0-meter conceptual danger-zone perimeter ring.

using System;
using IndustrialSafetyAR.Modules.FireExplosion;
using TMPro;
using UnityEngine;

namespace IndustrialSafetyAR.Modules.GasConfinedSpace
{
    /// <summary>
    /// Interactive AR marker representing a Confined Space Entry Portal with hazardous gas accumulation.
    /// Features procedural industrial manhole opening, toxic gas haze, camera-facing ArFloatingLabel,
    /// and a 3.0-meter safety perimeter ring.
    /// </summary>
    [SelectionBase]
    public class GasHazardMarker : MonoBehaviour
    {
        [Header("Hazard Metadata")]
        [SerializeField]
        private string _hazardId = "hazard_gas_confined_space";

        [SerializeField]
        private string _hazardClass = "toxic_combustible_confined_space";

        [Header("Visual References")]
        [SerializeField]
        private Renderer _collarRenderer;

        [SerializeField]
        private TextMeshPro _labelMesh;

        private ArFloatingLabel _floatingLabel;
        private Material _collarMaterial;
        private Material _hazeMaterial;
        private Material _innerVoidMaterial;

        private bool _isHazardRecognized;
        private bool _isDangerZoneMarked;
        private float _pulseTimer;
        private float _hazeTimer;

        private static readonly Color ColorActiveHazard = new Color(1.0f, 0.55f, 0.10f);   // Caution Gas Amber
        private static readonly Color ColorRecognized = new Color(0.20f, 0.85f, 0.35f);    // Confirmed Green
        private static readonly Color ColorPortalRim = new Color(0.18f, 0.20f, 0.23f);     // Heavy Cast Steel
        private static readonly Color ColorHazardStripes = new Color(0.95f, 0.75f, 0.10f); // Safety Yellow
        private static readonly Color ColorVoid = new Color(0.04f, 0.04f, 0.05f);          // Deep Interior Void
        private static readonly Color ColorHaze = new Color(0.85f, 0.80f, 0.25f, 0.35f);   // Volumetric Toxic Haze

        public string HazardId => _hazardId;
        public string HazardClass => _hazardClass;
        public bool IsHazardRecognized => _isHazardRecognized;
        public bool IsDangerZoneMarked => _isDangerZoneMarked;
        public ArFloatingLabel FloatingLabel => _floatingLabel;
        public TextMeshPro LabelMesh => _labelMesh;

        private GameObject _gasHazeRoot;
        private Transform _hazeCoreTransform;
        private Transform _hazeCloudTransform;
        private ParticleSystem _hazeParticles;

        private GameObject _dangerZoneRoot;
        private LineRenderer _dangerZoneLine;
        private ArFloatingLabel _dangerZoneLabelComp;

        public event Action<GasHazardMarker> OnHazardRecognized;
        public event Action<GasHazardMarker> OnDangerZoneMarked;

        private void Awake()
        {
            EnsureVisuals();
        }

        private void Update()
        {
            _pulseTimer += Time.deltaTime * 3.0f;
            _hazeTimer += Time.deltaTime * 1.5f;

            // Animate procedural gas accumulation haze (swirling volumetric pulse)
            if (_hazeCoreTransform != null)
            {
                float pulseScale = 0.45f + 0.06f * Mathf.Sin(_hazeTimer * 2.2f) + 0.03f * Mathf.Cos(_hazeTimer * 3.7f);
                _hazeCoreTransform.localScale = new Vector3(pulseScale * 1.1f, 0.28f + 0.04f * Mathf.Sin(_hazeTimer * 1.8f), pulseScale * 1.1f);
                _hazeCoreTransform.localRotation = Quaternion.Euler(0f, _hazeTimer * 20f, 0f);
            }

            if (_hazeCloudTransform != null)
            {
                float cloudScale = 0.60f + 0.08f * Mathf.Cos(_hazeTimer * 1.6f);
                _hazeCloudTransform.localScale = new Vector3(cloudScale, 0.35f + 0.05f * Mathf.Sin(_hazeTimer * 2.4f), cloudScale);
                _hazeCloudTransform.localRotation = Quaternion.Euler(0f, -_hazeTimer * 14f, 0f);
            }

            // Subtle pulsing of hazard warning beacon if not yet recognized
            if (!_isHazardRecognized && _collarMaterial != null)
            {
                float pulse = 0.88f + 0.12f * Mathf.Sin(_pulseTimer);
                _collarMaterial.color = Color.Lerp(ColorPortalRim, ColorActiveHazard * 0.5f, (pulse - 0.76f) * 4f);
            }
        }

        private void OnDestroy()
        {
            if (_collarMaterial != null) Destroy(_collarMaterial);
            if (_hazeMaterial != null) Destroy(_hazeMaterial);
            if (_innerVoidMaterial != null) Destroy(_innerVoidMaterial);
        }

        /// <summary>
        /// Acknowledges recognition of the gas hazard by the worker (Step 1).
        /// </summary>
        public void AcknowledgeHazard()
        {
            if (_isHazardRecognized) return;

            _isHazardRecognized = true;

            if (_floatingLabel != null)
            {
                _floatingLabel.SetText("✓ GAS HAZARD IDENTIFIED");
                _floatingLabel.SetColor(ColorRecognized);
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = "✓ GAS HAZARD IDENTIFIED";
                _labelMesh.color = ColorRecognized;
            }

            if (_collarMaterial != null)
            {
                _collarMaterial.color = ColorPortalRim;
            }

            Debug.Log($"[GasHazardMarker] Gas Hazard '{_hazardId}' recognized by worker.");
            OnHazardRecognized?.Invoke(this);
        }

        /// <summary>
        /// Shows or hides the 3.0m danger-zone safety perimeter ring (Step 2).
        /// </summary>
        public void ShowDangerZoneRing(bool show)
        {
            if (_dangerZoneRoot != null)
            {
                _dangerZoneRoot.SetActive(show);
            }
        }

        /// <summary>
        /// Marks the 3.0m safety perimeter as established by the worker (Step 2).
        /// </summary>
        public void MarkDangerZoneEstablished()
        {
            _isDangerZoneMarked = true;

            if (_dangerZoneLine != null && _dangerZoneLine.material != null)
            {
                _dangerZoneLine.material.color = new Color(0.20f, 0.88f, 0.35f, 0.95f);
            }

            if (_dangerZoneLabelComp != null)
            {
                _dangerZoneLabelComp.SetText("✓ 3.0m DANGER ZONE ESTABLISHED");
                _dangerZoneLabelComp.SetColor(ColorRecognized);
            }

            Debug.Log($"[GasHazardMarker] 3.0m Danger Zone perimeter established around '{_hazardId}'.");
            OnDangerZoneMarked?.Invoke(this);
        }

        /// <summary>
        /// Resets the hazard marker state for scenario retakes.
        /// </summary>
        public void ResetMarker()
        {
            _isHazardRecognized = false;
            _isDangerZoneMarked = false;
            ShowDangerZoneRing(false);

            if (_floatingLabel != null)
            {
                _floatingLabel.SetText("DANGER: CONFINED SPACE\n<size=80%><color=#FFC300>TAP TO IDENTIFY HAZARD</color></size>");
                _floatingLabel.SetColor(ColorActiveHazard);
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = "DANGER: CONFINED SPACE";
                _labelMesh.color = ColorActiveHazard;
            }

            if (_dangerZoneLabelComp != null)
            {
                _dangerZoneLabelComp.SetText("! 3.0m DANGER PERIMETER\n<size=75%><color=#FF8C00>STANDOFF REQUIRED</color></size>");
                _dangerZoneLabelComp.SetColor(new Color(1f, 0.45f, 0.05f, 0.95f));
            }
        }

        /// <summary>
        /// Procedurally constructs standard industrial confined-space portal, gas haze, and danger ring.
        /// </summary>
        public void EnsureVisuals()
        {
            if (transform.childCount > 0 && _floatingLabel != null)
            {
                return;
            }

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard");

            // 1. Heavy Industrial Floor Plate / Portal Surround (Skid base)
            GameObject floorPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorPlate.name = "PortalFloorPlate";
            floorPlate.transform.SetParent(transform, false);
            floorPlate.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            floorPlate.transform.localScale = new Vector3(0.95f, 0.024f, 0.95f);
            var plateMat = new Material(litShader) { color = new Color(0.12f, 0.13f, 0.15f) };
            floorPlate.GetComponent<Renderer>().material = plateMat;

            // Caution border rim on the floor plate
            GameObject cautionBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cautionBorder.name = "PortalCautionBorder";
            cautionBorder.transform.SetParent(transform, false);
            cautionBorder.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            cautionBorder.transform.localScale = new Vector3(0.98f, 0.003f, 0.98f);
            var cautionMat = new Material(unlitShader) { color = ColorHazardStripes };
            cautionBorder.GetComponent<Renderer>().material = cautionMat;
            var cautionCol = cautionBorder.GetComponent<Collider>();
            if (cautionCol != null) DestroyImmediate(cautionCol);

            // 2. Confined Space Manhole / Portal Lip (Raised Cylindrical Collar)
            GameObject collarObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            collarObj.name = "PortalCollar";
            collarObj.transform.SetParent(transform, false);
            collarObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            collarObj.transform.localScale = new Vector3(0.72f, 0.07f, 0.72f);
            _collarMaterial = new Material(litShader) { color = ColorPortalRim };
            _collarRenderer = collarObj.GetComponent<Renderer>();
            _collarRenderer.material = _collarMaterial;
            var collarCol = collarObj.GetComponent<Collider>();
            if (collarCol != null) DestroyImmediate(collarCol);

            // Raised Portal Rim Ring (Hatch flange)
            GameObject rimRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rimRing.name = "PortalRimFlange";
            rimRing.transform.SetParent(collarObj.transform, false);
            rimRing.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            rimRing.transform.localScale = new Vector3(1.08f, 0.18f, 1.08f);
            var rimMat = new Material(litShader) { color = new Color(0.24f, 0.26f, 0.30f) };
            rimRing.GetComponent<Renderer>().material = rimMat;
            var rimCol = rimRing.GetComponent<Collider>();
            if (rimCol != null) DestroyImmediate(rimCol);

            // 3. Dark Interior Void (Representing Subterranean Enclosed Space)
            GameObject voidObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            voidObj.name = "ConfinedSpaceInteriorVoid";
            voidObj.transform.SetParent(collarObj.transform, false);
            voidObj.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            voidObj.transform.localScale = new Vector3(0.82f, 0.22f, 0.82f);
            _innerVoidMaterial = new Material(unlitShader) { color = ColorVoid };
            voidObj.GetComponent<Renderer>().material = _innerVoidMaterial;
            var voidCol = voidObj.GetComponent<Collider>();
            if (voidCol != null) DestroyImmediate(voidCol);

            // Hatch Hinge Detailing
            GameObject hingeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hingeObj.name = "PortalHatchHinge";
            hingeObj.transform.SetParent(transform, false);
            hingeObj.transform.localPosition = new Vector3(0f, 0.16f, 0.38f);
            hingeObj.transform.localScale = new Vector3(0.18f, 0.06f, 0.08f);
            var hingeMat = new Material(litShader) { color = new Color(0.30f, 0.32f, 0.36f) };
            hingeObj.GetComponent<Renderer>().material = hingeMat;
            var hingeCol = hingeObj.GetComponent<Collider>();
            if (hingeCol != null) DestroyImmediate(hingeCol);

            // 4. Gas Accumulation Haze (Volumetric Gas Effect)
            _gasHazeRoot = new GameObject("GasAccumulationHaze");
            _gasHazeRoot.transform.SetParent(transform, false);
            _gasHazeRoot.transform.localPosition = new Vector3(0f, 0.20f, 0f);

            // Inner volumetric haze core (pulsing cylinder)
            GameObject hazeCore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hazeCore.name = "HazeCoreVolumetric";
            hazeCore.transform.SetParent(_gasHazeRoot.transform, false);
            hazeCore.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            hazeCore.transform.localScale = new Vector3(0.48f, 0.26f, 0.48f);
            _hazeMaterial = new Material(unlitShader) { color = ColorHaze };
            hazeCore.GetComponent<Renderer>().material = _hazeMaterial;
            _hazeCoreTransform = hazeCore.transform;
            var hazeCol = hazeCore.GetComponent<Collider>();
            if (hazeCol != null) DestroyImmediate(hazeCol);

            // Outer volumetric haze cloud (semi-transparent sphere)
            GameObject hazeCloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hazeCloud.name = "HazeCloudOuter";
            hazeCloud.transform.SetParent(_gasHazeRoot.transform, false);
            hazeCloud.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            hazeCloud.transform.localScale = new Vector3(0.62f, 0.34f, 0.62f);
            var outerHazeMat = new Material(unlitShader) { color = new Color(0.85f, 0.80f, 0.20f, 0.25f) };
            hazeCloud.GetComponent<Renderer>().material = outerHazeMat;
            _hazeCloudTransform = hazeCloud.transform;
            var hazeCloudCol = hazeCloud.GetComponent<Collider>();
            if (hazeCloudCol != null) DestroyImmediate(hazeCloudCol);

            // Lightweight procedural particle system
            TryAddProceduralGasParticles(_gasHazeRoot);

            // 5. Camera-Facing ArFloatingLabel (Elevated above portal opening)
            _floatingLabel = ArFloatingLabel.Create(
                gameObject,
                new Vector3(0f, 0.68f, 0f),
                "⚠ CONFINED SPACE • GAS HAZARD",
                ColorActiveHazard,
                width: 0.44f,
                height: 0.10f,
                fontSize: 0.74f,
                ArBillboardMode.ScreenAligned);
            _labelMesh = _floatingLabel.LabelMesh;

            // 6. Interaction Collider encompassing portal opening and haze
            var boxCol = GetComponent<BoxCollider>();
            if (boxCol == null) boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, 0.24f, 0f);
            boxCol.size = new Vector3(0.92f, 0.52f, 0.92f);

            // 7. 3.0m Conceptual Danger Zone Safety Perimeter Ring
            _dangerZoneRoot = new GameObject("DangerZoneRing");
            _dangerZoneRoot.transform.SetParent(transform, false);
            _dangerZoneRoot.transform.localPosition = Vector3.zero;

            _dangerZoneLine = _dangerZoneRoot.AddComponent<LineRenderer>();
            _dangerZoneLine.useWorldSpace = false;
            _dangerZoneLine.loop = true;
            _dangerZoneLine.positionCount = 48;
            _dangerZoneLine.startWidth = 0.045f;
            _dangerZoneLine.endWidth = 0.045f;
            _dangerZoneLine.material = new Material(unlitShader)
            {
                color = new Color(1.0f, 0.32f, 0.15f, 0.95f) // Initial warning red/amber
            };

            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2f / 48f;
                _dangerZoneLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * 3.0f, 0.02f, Mathf.Sin(angle) * 3.0f));
            }

            _dangerZoneLabelComp = ArFloatingLabel.Create(
                _dangerZoneRoot,
                new Vector3(0f, 0.15f, 3.04f),
                "3.0m DANGER PERIMETER (REMAIN OUTSIDE)",
                new Color(1.0f, 0.35f, 0.20f),
                width: 0.46f,
                height: 0.09f,
                fontSize: 0.70f,
                ArBillboardMode.ScreenAligned);

            _dangerZoneRoot.SetActive(false);
        }

        private void TryAddProceduralGasParticles(GameObject parent)
        {
            try
            {
                _hazeParticles = parent.GetComponent<ParticleSystem>();
                if (_hazeParticles == null)
                {
                    _hazeParticles = parent.AddComponent<ParticleSystem>();
                }

                var main = _hazeParticles.main;
                main.maxParticles = 18;
                main.loop = true;
                main.startLifetime = 2.0f;
                main.startSpeed = 0.12f;
                main.startSize = 0.25f;
                main.startColor = new Color(0.85f, 0.82f, 0.25f, 0.45f);
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.playOnAwake = true;

                var emission = _hazeParticles.emission;
                emission.rateOverTime = 8f;

                var shape = _hazeParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.24f;
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
                Debug.LogWarning($"[GasHazardMarker] ParticleSystem fallback: {ex.Message}");
            }
        }
    }
}
