// ArFloatingLabel.cs
// Namespace : IndustrialSafetyAR.Modules.FireExplosion
//
// Reusable world-space AR label component with dynamic camera billboarding,
// compact phone-appropriate scaling, and high-contrast dark translucent backing plaque.
// Solves text mirroring, overscaled bounds, and visual UI interference in smartphone AR.

using System;
using TMPro;
using UnityEngine;

namespace IndustrialSafetyAR.Modules.FireExplosion
{
    /// <summary>
    /// Billboarding modes for AR world-space text badges.
    /// </summary>
    public enum ArBillboardMode
    {
        /// <summary>
        /// Screen-aligned: perfectly parallel to the camera view plane.
        /// Zero distortion, never mirrored, ideal for floating AR badges and HUD tags.
        /// </summary>
        ScreenAligned,

        /// <summary>
        /// Spherical look-at: faces the camera center position.
        /// </summary>
        LookAtCamera,

        /// <summary>
        /// Cylindrical look-at: rotates around Y-axis only, keeping vertical upright.
        /// </summary>
        YAxisOnly
    }

    /// <summary>
    /// Compact, high-contrast floating 3D label designed specifically for mobile AR scenes.
    /// Ensures text is always facing the camera (never mirrored), remains compact so as not
    /// to obstruct screen-space training UI, and stays legible on any physical background.
    /// </summary>
    [ExecuteAlways]
    [SelectionBase]
    public class ArFloatingLabel : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private ArBillboardMode _billboardMode = ArBillboardMode.ScreenAligned;

        [SerializeField]
        private float _width = 0.38f;

        [SerializeField]
        private float _height = 0.11f;

        [SerializeField]
        private float _fontSize = 0.80f;

        [Header("Components")]
        [SerializeField]
        private TextMeshPro _labelMesh;

        [SerializeField]
        private Renderer _backgroundRenderer;

        [SerializeField]
        private Renderer _accentRenderer;

        private Material _backgroundMaterial;
        private Material _accentMaterial;
        private Camera _targetCamera;

        public TextMeshPro LabelMesh => _labelMesh;
        public ArBillboardMode BillboardMode
        {
            get => _billboardMode;
            set => _billboardMode = value;
        }

        public string Text
        {
            get => _labelMesh != null ? _labelMesh.text : string.Empty;
            set => SetText(value);
        }

        public Color TextColor
        {
            get => _labelMesh != null ? _labelMesh.color : Color.white;
            set => SetColor(value);
        }

        private void Awake()
        {
            EnsureElements();
        }

        private void LateUpdate()
        {
            UpdateOrientation();
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (_backgroundMaterial != null) Destroy(_backgroundMaterial);
                if (_accentMaterial != null) Destroy(_accentMaterial);
            }
            else
            {
                if (_backgroundMaterial != null) DestroyImmediate(_backgroundMaterial);
                if (_accentMaterial != null) DestroyImmediate(_accentMaterial);
            }
        }

        /// <summary>
        /// Explicitly binds a specific camera for billboarding (defaults to Camera.main).
        /// </summary>
        public void SetTargetCamera(Camera cam)
        {
            _targetCamera = cam;
        }

        /// <summary>
        /// Updates the text displayed on the floating label.
        /// </summary>
        public void SetText(string text)
        {
            if (_labelMesh != null)
            {
                _labelMesh.text = text;
            }
        }

        /// <summary>
        /// Sets text color and optional accent border/underline color.
        /// </summary>
        public void SetColor(Color textColor, Color? accentColor = null)
        {
            if (_labelMesh != null)
            {
                _labelMesh.color = textColor;
            }

            Color accent = accentColor ?? textColor;
            if (_accentMaterial != null)
            {
                _accentMaterial.color = accent;
            }
        }

        /// <summary>
        /// Rotates the label to directly face the camera so text is never viewed from behind
        /// (which would cause horizontal mirroring) and remains readable from any angle.
        /// </summary>
        public void UpdateOrientation()
        {
            Camera cam = _targetCamera != null ? _targetCamera : Camera.main;
            if (cam == null) return;

            switch (_billboardMode)
            {
                case ArBillboardMode.ScreenAligned:
                    // Matches camera orientation exactly: text plane is flat to screen,
                    // horizontal axis matches screen right, never mirrored or skewed.
                    transform.rotation = cam.transform.rotation;
                    break;

                case ArBillboardMode.LookAtCamera:
                    // Text faces camera position. The forward vector points AWAY from camera
                    // because TMP reads normally along its local +Z axis.
                    Vector3 toViewer = transform.position - cam.transform.position;
                    if (toViewer.sqrMagnitude > 0.0001f)
                    {
                        transform.rotation = Quaternion.LookRotation(toViewer, cam.transform.up);
                    }
                    break;

                case ArBillboardMode.YAxisOnly:
                    // Cylindrical billboarding: rotates around world Y axis.
                    Vector3 yDir = transform.position - cam.transform.position;
                    yDir.y = 0f;
                    if (yDir.sqrMagnitude > 0.0001f)
                    {
                        transform.rotation = Quaternion.LookRotation(yDir, Vector3.up);
                    }
                    break;
            }
        }

        /// <summary>
        /// Procedurally constructs or links the TextMeshPro and dark backing plaque components.
        /// </summary>
        public void EnsureElements()
        {
            // 1. TextMeshPro label component
            if (_labelMesh == null)
            {
                _labelMesh = GetComponentInChildren<TextMeshPro>();
            }

            if (_labelMesh == null)
            {
                GameObject tmpObj = new GameObject("LabelText");
                tmpObj.transform.SetParent(transform, false);
                tmpObj.transform.localPosition = new Vector3(0f, 0.005f, -0.002f);
                _labelMesh = tmpObj.AddComponent<TextMeshPro>();
            }

            var defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
                ?? TMP_Settings.defaultFontAsset;
            if (defaultFont != null && _labelMesh.font == null)
            {
                _labelMesh.font = defaultFont;
            }

            _labelMesh.fontSize = _fontSize;
            _labelMesh.alignment = TextAlignmentOptions.Center;
            _labelMesh.rectTransform.sizeDelta = new Vector2(_width * 0.94f, _height * 0.85f);

            // 2. Translucent dark backing plaque
            if (_backgroundRenderer == null)
            {
                Transform bgTransform = transform.Find("BackgroundPlaque");
                if (bgTransform != null)
                {
                    _backgroundRenderer = bgTransform.GetComponent<Renderer>();
                }
            }

            if (_backgroundRenderer == null)
            {
                GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                bgObj.name = "BackgroundPlaque";
                bgObj.transform.SetParent(transform, false);
                // Positioned 4mm behind text plane (+Z is away from camera when camera looks along +Z)
                bgObj.transform.localPosition = new Vector3(0f, 0f, 0.004f);
                bgObj.transform.localRotation = Quaternion.identity;
                bgObj.transform.localScale = new Vector3(_width, _height, 1f);

                var col = bgObj.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);

                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Sprites/Default")
                    ?? Shader.Find("Standard");

                _backgroundMaterial = new Material(unlit)
                {
                    color = new Color(0.06f, 0.08f, 0.12f, 0.88f) // Dark industrial slate backing
                };
                _backgroundRenderer = bgObj.GetComponent<Renderer>();
                _backgroundRenderer.material = _backgroundMaterial;
            }

            // 3. Subtle bottom accent line
            if (_accentRenderer == null)
            {
                Transform accentTransform = transform.Find("AccentStripe");
                if (accentTransform != null)
                {
                    _accentRenderer = accentTransform.GetComponent<Renderer>();
                }
            }

            if (_accentRenderer == null)
            {
                GameObject accentObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                accentObj.name = "AccentStripe";
                accentObj.transform.SetParent(transform, false);
                accentObj.transform.localPosition = new Vector3(0f, -_height * 0.45f, 0.002f);
                accentObj.transform.localRotation = Quaternion.identity;
                accentObj.transform.localScale = new Vector3(_width * 0.90f, 0.006f, 1f);

                var col = accentObj.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);

                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Sprites/Default")
                    ?? Shader.Find("Standard");

                _accentMaterial = new Material(unlit)
                {
                    color = new Color(1.0f, 0.35f, 0.05f, 0.95f)
                };
                _accentRenderer = accentObj.GetComponent<Renderer>();
                _accentRenderer.material = _accentMaterial;
            }
        }

        /// <summary>
        /// Adjusts dimensions and updates internal quad transforms.
        /// </summary>
        public void SetDimensions(float width, float height, float fontSize)
        {
            _width = width;
            _height = height;
            _fontSize = fontSize;

            if (_labelMesh != null)
            {
                _labelMesh.fontSize = _fontSize;
                _labelMesh.rectTransform.sizeDelta = new Vector2(_width * 0.94f, _height * 0.85f);
            }

            if (_backgroundRenderer != null)
            {
                _backgroundRenderer.transform.localScale = new Vector3(_width, _height, 1f);
            }

            if (_accentRenderer != null)
            {
                _accentRenderer.transform.localPosition = new Vector3(0f, -_height * 0.45f, 0.002f);
                _accentRenderer.transform.localScale = new Vector3(_width * 0.90f, 0.006f, 1f);
            }
        }

        /// <summary>
        /// Static factory helper to quickly spawn an ArFloatingLabel child object.
        /// </summary>
        public static ArFloatingLabel Create(
            GameObject parent,
            Vector3 localPosition,
            string text,
            Color color,
            float width = 0.38f,
            float height = 0.11f,
            float fontSize = 0.80f,
            ArBillboardMode billboardMode = ArBillboardMode.ScreenAligned)
        {
            GameObject labelObj = new GameObject("ArFloatingLabel");
            labelObj.transform.SetParent(parent.transform, false);
            labelObj.transform.localPosition = localPosition;

            var comp = labelObj.AddComponent<ArFloatingLabel>();
            comp._width = width;
            comp._height = height;
            comp._fontSize = fontSize;
            comp._billboardMode = billboardMode;
            comp.EnsureElements();
            comp.SetText(text);
            comp.SetColor(color);

            return comp;
        }
    }
}
