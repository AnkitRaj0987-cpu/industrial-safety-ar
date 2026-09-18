// ExtinguisherMarker.cs
// Namespace : IndustrialSafetyAR.Modules.FireExplosion
//
// Represents an interactive 3D Fire Extinguisher prop/marker in AR space.
// Supports physics raycast tapping, configuration of extinguisher type (CO2, Water, Foam),
// procedural visual rendering, and camera-facing ArFloatingLabels.

using System;
using TMPro;
using UnityEngine;

namespace IndustrialSafetyAR.Modules.FireExplosion
{
    /// <summary>
    /// Interactive AR marker representing a fire extinguisher option or equipment station.
    /// Supports spatial selection via physics raycasting and visual feedback.
    /// </summary>
    [SelectionBase]
    public class ExtinguisherMarker : MonoBehaviour
    {
        [Header("Extinguisher Metadata")]
        [Tooltip("Extinguisher target ID matching rubric (e.g. extinguisher_co2, extinguisher_water, extinguisher_foam)")]
        [SerializeField]
        private string _extinguisherId = FireTrainingWorkflow.TargetExtinguisherCO2;

        [Tooltip("Human-readable title")]
        [SerializeField]
        private string _extinguisherName = "CO2 Fire Extinguisher";

        [Tooltip("Whether this is the correct extinguisher for the electrical fire hazard")]
        [SerializeField]
        private bool _isCorrectForHazard = true;

        [Header("Visual Components")]
        [SerializeField]
        private Renderer _bodyRenderer;

        [SerializeField]
        private TextMeshPro _labelMesh;

        private ArFloatingLabel _floatingLabel;
        private Material _bodyMaterial;
        private bool _isSelected;

        public string ExtinguisherId => _extinguisherId;
        public string ExtinguisherName => _extinguisherName;
        public bool IsCorrectForHazard => _isCorrectForHazard;
        public bool IsSelected => _isSelected;

        public event Action<ExtinguisherMarker> OnExtinguisherTapped;

        private void Awake()
        {
            EnsureVisuals();
        }

        private void OnDestroy()
        {
            if (_bodyMaterial != null) Destroy(_bodyMaterial);
        }

        public void ConfigureExtinguisher(string extinguisherId, string extinguisherName, bool isCorrect)
        {
            _extinguisherId = extinguisherId;
            _extinguisherName = extinguisherName;
            _isCorrectForHazard = isCorrect;
            ApplyTheme();
        }

        public void MarkSelected()
        {
            _isSelected = true;
            if (_bodyMaterial != null)
            {
                _bodyMaterial.color = new Color(0.2f, 0.9f, 0.4f);
            }

            Color selectColor = new Color(0.2f, 0.9f, 0.4f);
            if (_floatingLabel != null)
            {
                _floatingLabel.SetText($"{_extinguisherName}\n[SELECTED]");
                _floatingLabel.SetColor(selectColor);
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = $"{_extinguisherName}\n[SELECTED]";
                _labelMesh.color = selectColor;
            }

            OnExtinguisherTapped?.Invoke(this);
        }

        private void ApplyTheme()
        {
            Color themeColor;
            if (_extinguisherId.IndexOf("co2", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                themeColor = new Color(0.2f, 0.2f, 0.22f); // Black cylinder for CO2
            }
            else if (_extinguisherId.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                themeColor = new Color(0.85f, 0.15f, 0.15f); // Red body for water
            }
            else
            {
                themeColor = new Color(0.95f, 0.85f, 0.6f); // Cream/buff for foam
            }

            if (_bodyMaterial != null)
            {
                _bodyMaterial.color = themeColor;
            }

            Color textColor = _isCorrectForHazard ? new Color(0.2f, 0.9f, 0.4f) : new Color(1f, 0.8f, 0.2f);
            if (_floatingLabel != null)
            {
                _floatingLabel.SetText(_extinguisherName);
                _floatingLabel.SetColor(textColor);
            }
            else if (_labelMesh != null)
            {
                _labelMesh.text = _extinguisherName;
                _labelMesh.color = textColor;
            }
        }

        public void EnsureVisuals()
        {
            if (transform.childCount > 0 && _bodyRenderer != null) return;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Cylinder body
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "ExtinguisherBody";
            cylinder.transform.SetParent(transform, false);
            cylinder.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            cylinder.transform.localScale = new Vector3(0.18f, 0.35f, 0.18f);

            var col = cylinder.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            _bodyMaterial = new Material(litShader) { color = new Color(0.2f, 0.2f, 0.22f) };
            _bodyRenderer = cylinder.GetComponent<Renderer>();
            _bodyRenderer.material = _bodyMaterial;

            // Camera-facing compact ArFloatingLabel (replaces giant static 3D label)
            _floatingLabel = ArFloatingLabel.Create(
                gameObject,
                new Vector3(0f, 0.78f, 0f),
                _extinguisherName,
                _isCorrectForHazard ? new Color(0.2f, 0.9f, 0.4f) : new Color(1f, 0.8f, 0.2f),
                width: 0.36f,
                height: 0.10f,
                fontSize: 0.76f,
                ArBillboardMode.ScreenAligned);
            _labelMesh = _floatingLabel.LabelMesh;

            ApplyTheme();

            // Box collider for raycast hit
            var boxCol = GetComponent<BoxCollider>();
            if (boxCol == null) boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, 0.4f, 0f);
            boxCol.size = new Vector3(0.35f, 0.85f, 0.35f);
        }
    }
}
