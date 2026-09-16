// AssemblyPointMarker.cs
// Namespace : IndustrialSafetyAR.Modules.FireExplosion
//
// Represents an interactive 3D Emergency Assembly Point marker in AR space.
// Provides lightweight procedural placeholder visuals using standard Unity primitives,
// TextMeshPro 3D signage, ground halo directional indicators, and raycast detection.

using System;
using TMPro;
using UnityEngine;

namespace IndustrialSafetyAR.Modules.FireExplosion
{
    /// <summary>
    /// Interactive AR marker representing an emergency assembly muster point.
    /// Supports spatial selection via physics raycasting and visual feedback confirmation.
    /// </summary>
    [SelectionBase]
    public class AssemblyPointMarker : MonoBehaviour
    {
        [Header("Assembly Point Metadata")]
        [Tooltip("Target ID matching module.json / rubric.json (e.g. assembly_muster_point_alpha)")]
        [SerializeField]
        private string _pointId = FireTrainingWorkflow.TargetAssemblyMusterPoint;

        [Tooltip("Human-readable title")]
        [SerializeField]
        private string _pointName = "Assembly Muster Point Alpha";

        [Tooltip("Whether this marker represents the safe designated emergency assembly area")]
        [SerializeField]
        private bool _isDesignated = true;

        [Header("Visual Components")]
        [SerializeField]
        private Renderer _signRenderer;

        [SerializeField]
        private TextMeshPro _labelMesh;

        [SerializeField]
        private Renderer _haloRenderer;

        private Material _signMaterial;
        private Material _haloMaterial;
        private bool _isReached;
        private float _pulseTimer;

        // Visual color palettes
        private static readonly Color ColorSafeGreen = new Color(0.12f, 0.78f, 0.32f);       // ISO Safety Green
        private static readonly Color ColorActiveCyan = new Color(0.20f, 0.95f, 0.55f);      // Confirmed Glow
        private static readonly Color ColorUnauthorizedRed = new Color(0.88f, 0.22f, 0.22f); // Unauthorized area red

        public string PointId => _pointId;
        public string PointName => _pointName;
        public bool IsDesignated => _isDesignated;
        public bool IsReached => _isReached;

        public event Action<AssemblyPointMarker> OnAssemblyPointTapped;

        private void Awake()
        {
            EnsureVisuals();
        }

        private void Update()
        {
            _pulseTimer += Time.deltaTime * 3.0f;

            if (_isReached)
            {
                if (_haloMaterial != null)
                {
                    float pulse = 0.85f + 0.15f * Mathf.Sin(_pulseTimer * 1.5f);
                    Color glow = ColorActiveCyan * pulse;
                    glow.a = 0.60f;
                    _haloMaterial.color = glow;
                }
                return;
            }

            if (_haloMaterial != null)
            {
                float pulse = 0.80f + 0.20f * Mathf.Sin(_pulseTimer);
                Color baseCol = _isDesignated ? ColorSafeGreen : ColorUnauthorizedRed;
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
        /// Configures assembly point metadata and visual presentation.
        /// </summary>
        public void ConfigureAssemblyPoint(string pointId, string pointName, bool isDesignated)
        {
            _pointId = pointId;
            _pointName = pointName;
            _isDesignated = isDesignated;
            ApplyTheme();
        }

        /// <summary>
        /// Visually acknowledges successful identification and arrival at this assembly point.
        /// </summary>
        public void AcknowledgeReached()
        {
            _isReached = true;

            if (_signMaterial != null)
            {
                _signMaterial.color = ColorActiveCyan;
            }

            if (_labelMesh != null)
            {
                _labelMesh.text = $"<b>{_pointName}</b>\n<color=#00FF88>[REACHED - EVACUATION COMPLETED]</color>";
                _labelMesh.color = ColorActiveCyan;
            }

            Debug.Log($"[AssemblyPointMarker] Assembly Point '{_pointId}' reached and confirmed.");
            OnAssemblyPointTapped?.Invoke(this);
        }

        private void ApplyTheme()
        {
            Color themeColor = _isDesignated ? ColorSafeGreen : ColorUnauthorizedRed;

            if (_signMaterial != null)
            {
                _signMaterial.color = themeColor;
            }

            if (_labelMesh != null)
            {
                if (_isReached)
                {
                    _labelMesh.text = $"<b>{_pointName}</b>\n<color=#00FF88>[REACHED - EVACUATION COMPLETED]</color>";
                    _labelMesh.color = ColorActiveCyan;
                }
                else if (_isDesignated)
                {
                    _labelMesh.text = $"<b>EMERGENCY ASSEMBLY POINT</b>\n{_pointName}";
                    _labelMesh.color = ColorSafeGreen;
                }
                else
                {
                    _labelMesh.text = $"<b>UNAUTHORIZED AREA</b>\n{_pointName}\n(DO NOT ASSEMBLE)";
                    _labelMesh.color = ColorUnauthorizedRed;
                }
            }
        }

        /// <summary>
        /// Procedurally constructs standard industrial assembly point signage and ground beacons.
        /// </summary>
        public void EnsureVisuals()
        {
            if (transform.childCount > 0 && _signRenderer != null)
            {
                return;
            }

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");

            Color themeColor = _isDesignated ? ColorSafeGreen : ColorUnauthorizedRed;

            // 1. Ground Circular Halo Ring
            GameObject haloObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            haloObj.name = "AssemblyHalo";
            haloObj.transform.SetParent(transform, false);
            haloObj.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            haloObj.transform.localScale = new Vector3(2.2f, 0.005f, 2.2f);
            var haloCol = haloObj.GetComponent<Collider>();
            if (haloCol != null) Destroy(haloCol);

            Color haloColVal = themeColor;
            haloColVal.a = 0.35f;
            _haloMaterial = new Material(litShader) { color = haloColVal };
            _haloRenderer = haloObj.GetComponent<Renderer>();
            _haloRenderer.material = _haloMaterial;

            // 2. Base Disc
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "AssemblyBase";
            baseObj.transform.SetParent(transform, false);
            baseObj.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            baseObj.transform.localScale = new Vector3(1.6f, 0.03f, 1.6f);
            var baseCol = baseObj.GetComponent<Collider>();
            if (baseCol != null) Destroy(baseCol);
            var baseMat = new Material(litShader) { color = new Color(0.18f, 0.20f, 0.24f) };
            baseObj.GetComponent<Renderer>().material = baseMat;

            // 3. Central Beacon Post
            GameObject postObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postObj.name = "AssemblyPost";
            postObj.transform.SetParent(transform, false);
            postObj.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            postObj.transform.localScale = new Vector3(0.08f, 0.9f, 0.08f);
            var postCol = postObj.GetComponent<Collider>();
            if (postCol != null) Destroy(postCol);
            var postMat = new Material(litShader) { color = new Color(0.28f, 0.30f, 0.35f) };
            postObj.GetComponent<Renderer>().material = postMat;

            // 4. Sign Board (Square ISO Assembly Sign)
            GameObject signObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signObj.name = "SignBoard";
            signObj.transform.SetParent(transform, false);
            signObj.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            signObj.transform.localScale = new Vector3(1.1f, 0.7f, 0.05f);
            var signCol = signObj.GetComponent<Collider>();
            if (signCol != null) Destroy(signCol);

            _signMaterial = new Material(litShader) { color = themeColor };
            _signRenderer = signObj.GetComponent<Renderer>();
            _signRenderer.material = _signMaterial;

            // 5. Floating 3D TextMeshPro Label
            GameObject labelObj = new GameObject("SignLabel");
            labelObj.transform.SetParent(transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 1.9f, -0.04f);
            _labelMesh = labelObj.AddComponent<TextMeshPro>();
            _labelMesh.fontSize = 2.4f;
            _labelMesh.alignment = TextAlignmentOptions.Center;
            _labelMesh.rectTransform.sizeDelta = new Vector2(3.5f, 1.4f);

            ApplyTheme();

            // 6. BoxCollider for screen tap Physics.Raycast interaction
            var boxCol = GetComponent<BoxCollider>();
            if (boxCol == null)
            {
                boxCol = gameObject.AddComponent<BoxCollider>();
            }
            boxCol.center = new Vector3(0f, 1.0f, 0f);
            boxCol.size = new Vector3(1.8f, 2.0f, 1.8f);
        }
    }
}
