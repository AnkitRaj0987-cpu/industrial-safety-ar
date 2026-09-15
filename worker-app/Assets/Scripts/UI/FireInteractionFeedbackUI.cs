// FireInteractionFeedbackUI.cs
// Namespace : IndustrialSafetyAR.UI
//
// Minimal worker-facing feedback UI displaying instructions and success confirmation
// for Fire Hazard AR interactions without cluttering the screen with debug diagnostics.

using IndustrialSafetyAR.Modules.FireExplosion;
using TMPro;
using UnityEngine;

namespace IndustrialSafetyAR.UI
{
    /// <summary>
    /// Displays guidance and success feedback to the worker during AR hazard detection.
    /// </summary>
    public class FireInteractionFeedbackUI : MonoBehaviour
    {
        [Tooltip("The interaction controller to listen to. Discovered automatically if null.")]
        [SerializeField]
        private FireArInteractionController _controller;

        [Tooltip("TextMeshProUGUI component displaying current guidance/instruction.")]
        [SerializeField]
        private TextMeshProUGUI _promptText;

        [Tooltip("Optional panel or graphic highlighting success state.")]
        [SerializeField]
        private GameObject _successBadge;

        private void Awake()
        {
            if (_promptText == null)
            {
                _promptText = GetComponent<TextMeshProUGUI>();
            }

            if (_controller == null)
            {
                _controller = FindAnyObjectByType<FireArInteractionController>();
            }

            EnsurePromptBanner();
        }

        private void EnsurePromptBanner()
        {
            if (_promptText != null) return;

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var bannerObj = new GameObject("TrainingPromptBanner");
            bannerObj.transform.SetParent(canvas.transform, false);

            var rect = bannerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.06f, 0.88f);
            rect.anchorMax = new Vector2(0.94f, 0.96f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bgImg = bannerObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.10f, 0.12f, 0.16f, 0.88f);

            var textObj = new GameObject("PromptText");
            textObj.transform.SetParent(bannerObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16, 4);
            textRect.offsetMax = new Vector2(-16, -4);

            _promptText = textObj.AddComponent<TextMeshProUGUI>();
            _promptText.fontSize = 32;
            _promptText.enableAutoSizing = true;
            _promptText.fontSizeMin = 20;
            _promptText.fontSizeMax = 38;
            _promptText.alignment = TextAlignmentOptions.Center;
            _promptText.color = new Color(0.95f, 0.95f, 0.95f);
            _promptText.text = "Initializing training module...";
        }

        private void OnEnable()
        {
            if (_controller != null)
            {
                _controller.OnFeedbackChanged += HandleFeedbackChanged;
                _controller.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.OnFeedbackChanged -= HandleFeedbackChanged;
                _controller.OnStateChanged -= HandleStateChanged;
            }
        }

        private void Start()
        {
            if (_successBadge != null)
            {
                _successBadge.SetActive(false);
            }
        }

        private void HandleFeedbackChanged(string feedback)
        {
            if (_promptText != null && !string.IsNullOrEmpty(feedback))
            {
                _promptText.text = feedback;
            }
        }

        private void HandleStateChanged(FireInteractionState state)
        {
            if (state == FireInteractionState.HazardDetected)
            {
                if (_promptText != null)
                {
                    _promptText.text = "Fire Hazard Detected!\n<size=80%>Electrical Conveyor Fire (Acknowledged)</size>";
                    _promptText.color = new Color(0.3f, 0.9f, 0.4f); // Crisp safety green
                }

                if (_successBadge != null)
                {
                    _successBadge.SetActive(true);
                }
            }
        }
    }
}
