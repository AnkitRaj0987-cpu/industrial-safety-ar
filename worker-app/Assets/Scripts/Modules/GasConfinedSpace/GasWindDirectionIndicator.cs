// GasWindDirectionIndicator.cs
// Namespace : IndustrialSafetyAR.Modules.GasConfinedSpace
//
// Lightweight procedural visual component indicating simulated wind direction in AR space.
// Provides a directional windsock/arrow with a camera-facing ArFloatingLabel.
// Clearly communicates that wind direction is a simulated training condition.

using System;
using IndustrialSafetyAR.Modules.FireExplosion;
using TMPro;
using UnityEngine;

namespace IndustrialSafetyAR.Modules.GasConfinedSpace
{
    [SelectionBase]
    public class GasWindDirectionIndicator : MonoBehaviour
    {
        [Header("Wind Direction")]
        [SerializeField]
        private Vector3 _windDirection = new Vector3(0.707f, 0f, 0.707f);

        [SerializeField]
        private string _windLabel = "SIMULATED WIND: UPWIND →";

        private ArFloatingLabel _floatingLabel;
        private Transform _arrowTransform;
        private Material _arrowMaterial;
        private float _swayTimer;

        public Vector3 WindDirection => _windDirection;
        public string DirectionText => _windLabel;

        private void Awake()
        {
            EnsureVisuals();
        }

        private void Update()
        {
            _swayTimer += Time.deltaTime * 2.0f;

            // Subtle wind oscillation (simulated wind gust effect)
            if (_arrowTransform != null)
            {
                float sway = Mathf.Sin(_swayTimer) * 4.0f;
                Quaternion baseRot = Quaternion.LookRotation(_windDirection);
                _arrowTransform.rotation = baseRot * Quaternion.Euler(0f, sway, 0f);
            }
        }

        public void SetWindDirection(Vector3 direction, string label = null)
        {
            if (direction != Vector3.zero)
            {
                direction.y = 0f;
                _windDirection = direction.normalized;
            }

            if (!string.IsNullOrEmpty(label))
            {
                _windLabel = label;
            }

            if (_floatingLabel != null)
            {
                _floatingLabel.SetText(_windLabel);
            }

            if (_arrowTransform != null)
            {
                _arrowTransform.rotation = Quaternion.LookRotation(_windDirection);
            }
        }

        public void EnsureVisuals()
        {
            var arrowObj = transform.Find("ArrowVisual");
            if (arrowObj == null)
            {
                var arrow = new GameObject("ArrowVisual");
                arrow.transform.SetParent(transform, false);
                arrow.transform.localPosition = new Vector3(0f, 0.40f, 0f);
                _arrowTransform = arrow.transform;

                // Shaft
                var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.name = "Shaft";
                shaft.transform.SetParent(arrow.transform, false);
                shaft.transform.localPosition = new Vector3(0f, 0f, 0.20f);
                shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                shaft.transform.localScale = new Vector3(0.04f, 0.25f, 0.04f);

                var shaftCol = shaft.GetComponent<Collider>();
                if (shaftCol != null) DestroyImmediate(shaftCol);

                // Cone head
                var head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                head.name = "ConeHead";
                head.transform.SetParent(arrow.transform, false);
                head.transform.localPosition = new Vector3(0f, 0f, 0.50f);
                head.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                head.transform.localScale = new Vector3(0.12f, 0.10f, 0.12f);

                var headCol = head.GetComponent<Collider>();
                if (headCol != null) DestroyImmediate(headCol);

                // Material
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Mobile/Diffuse")
                    ?? Shader.Find("Standard");
                _arrowMaterial = new Material(unlit);
                _arrowMaterial.color = new Color(0.20f, 0.75f, 1.0f); // Aero Cyan
                shaft.GetComponent<Renderer>().sharedMaterial = _arrowMaterial;
                head.GetComponent<Renderer>().sharedMaterial = _arrowMaterial;

                _arrowTransform.rotation = Quaternion.LookRotation(_windDirection);
            }
            else
            {
                _arrowTransform = arrowObj;
            }

            // Camera-facing label
            var labelTransform = transform.Find("FloatingLabel");
            if (labelTransform == null)
            {
                var labelObj = new GameObject("FloatingLabel");
                labelObj.transform.SetParent(transform, false);
                labelObj.transform.localPosition = new Vector3(0f, 0.75f, 0f);

                _floatingLabel = labelObj.AddComponent<ArFloatingLabel>();
                _floatingLabel.EnsureElements();
                _floatingLabel.BillboardMode = ArBillboardMode.ScreenAligned;
                _floatingLabel.SetText(_windLabel);
                _floatingLabel.SetColor(new Color(0.20f, 0.75f, 1.0f));
            }
            else
            {
                _floatingLabel = labelTransform.GetComponent<ArFloatingLabel>();
            }
        }
    }
}
