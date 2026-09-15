// FireInteractionFeedbackUI.cs
// Namespace : IndustrialSafetyAR.UI
//
// Minimal worker-facing feedback UI displaying instructions, identification choices,
// and emergency alarm trigger without cluttering the screen with debug diagnostics.

using IndustrialSafetyAR.Modules.FireExplosion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IndustrialSafetyAR.UI
{
    /// <summary>
    /// Displays guidance, hazard classification buttons, and the emergency alarm trigger
    /// to the worker during AR Fire training.
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

        private Image _bannerBg;
        private GameObject _actionContainer;

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
            EnsureActionContainer();
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

            _bannerBg = bannerObj.AddComponent<Image>();
            _bannerBg.color = new Color(0.10f, 0.12f, 0.16f, 0.90f);

            var textObj = new GameObject("PromptText");
            textObj.transform.SetParent(bannerObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16, 4);
            textRect.offsetMax = new Vector2(-16, -4);

            _promptText = textObj.AddComponent<TextMeshProUGUI>();
            _promptText.fontSize = 30;
            _promptText.enableAutoSizing = true;
            _promptText.fontSizeMin = 18;
            _promptText.fontSizeMax = 36;
            _promptText.alignment = TextAlignmentOptions.Center;
            _promptText.color = new Color(0.95f, 0.95f, 0.95f);
            _promptText.text = "Initializing training module...";
        }

        private void EnsureActionContainer()
        {
            if (_actionContainer != null) return;

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            _actionContainer = new GameObject("TrainingActionContainer");
            _actionContainer.transform.SetParent(canvas.transform, false);

            var rect = _actionContainer.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.03f);
            rect.anchorMax = new Vector2(0.92f, 0.26f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
            switch (state)
            {
                case FireInteractionState.AwaitingIdentification:
                    ShowIdentificationUI();
                    break;

                case FireInteractionState.AwaitingAlarm:
                    ShowRaiseAlarmUI();
                    break;

                case FireInteractionState.AlarmRaised:
                    ShowAlarmActivatedUI();
                    break;

                case FireInteractionState.AwaitingExtinguisherSelection:
                    ShowExtinguisherSelectionUI();
                    break;

                case FireInteractionState.ExtinguisherSelected:
                    ShowExtinguisherSelectedUI();
                    break;
            }
        }

        private void ClearActionButtons()
        {
            if (_actionContainer == null) return;

            for (int i = _actionContainer.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_actionContainer.transform.GetChild(i).gameObject);
            }
        }

        private void ShowIdentificationUI()
        {
            ClearActionButtons();
            if (_actionContainer == null) return;

            if (_promptText != null)
            {
                _promptText.text = "Hazard Spotted: Select the correct hazard classification:";
                _promptText.color = new Color(1.0f, 0.85f, 0.3f); // Amber prompt
            }

            // Create 3 option buttons vertically stacked
            CreateOptionButton("1. Class E: Electrical Conveyor Fire", new Vector2(0f, 0.68f), new Vector2(1f, 0.98f), () =>
            {
                _controller?.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire);
            });

            CreateOptionButton("2. Class A: Ordinary Combustible Material", new Vector2(0f, 0.35f), new Vector2(1f, 0.65f), () =>
            {
                _controller?.SubmitHazardIdentification("hazard_combustible_debris");
            });

            CreateOptionButton("3. Class B: Flammable Chemical / Solvent", new Vector2(0f, 0.02f), new Vector2(1f, 0.32f), () =>
            {
                _controller?.SubmitHazardIdentification("hazard_flammable_liquid");
            });
        }

        private void ShowRaiseAlarmUI()
        {
            ClearActionButtons();
            if (_actionContainer == null) return;

            if (_promptText != null)
            {
                _promptText.text = "<color=#4CAF50>Class E Electrical Hazard Confirmed!</color>\nActivate Emergency Alarm Call Point immediately.";
                _promptText.color = new Color(0.95f, 0.95f, 0.95f);
            }

            // Create prominent emergency alarm trigger button
            var alarmBtn = CreateActionButton("PULL / PRESS EMERGENCY ALARM (MCP)", new Vector2(0f, 0.15f), new Vector2(1f, 0.85f),
                new Color(0.85f, 0.15f, 0.15f), () =>
            {
                _controller?.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm);
            });

            var btnText = alarmBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.fontSize = 32;
                btnText.fontStyle = FontStyles.Bold;
            }
        }

        private void ShowAlarmActivatedUI()
        {
            ClearActionButtons();

            if (_promptText != null)
            {
                _promptText.text = "EMERGENCY ALARM ACTIVATED!\n<size=80%>Manual Call Point (MCP) Triggered • Siren Sounding</size>";
                _promptText.color = new Color(1f, 0.3f, 0.2f);
            }

            if (_bannerBg != null)
            {
                _bannerBg.color = new Color(0.4f, 0.08f, 0.08f, 0.94f);
            }
        }

        private void ShowExtinguisherSelectionUI()
        {
            ClearActionButtons();
            if (_actionContainer == null) return;

            if (_promptText != null)
            {
                _promptText.text = "Alarm Active! Select extinguisher for Class E electrical fire:";
                _promptText.color = new Color(1.0f, 0.85f, 0.3f);
            }

            // Option 1: CO2 Extinguisher (Correct)
            CreateOptionButton("1. CO2 Extinguisher (Carbon Dioxide) - Class E", new Vector2(0f, 0.68f), new Vector2(1f, 0.98f), () =>
            {
                _controller?.SubmitExtinguisherSelection(FireTrainingWorkflow.TargetExtinguisherCO2);
            });

            // Option 2: Water Extinguisher (Incorrect)
            CreateOptionButton("2. Water Extinguisher (H2O)", new Vector2(0f, 0.35f), new Vector2(1f, 0.65f), () =>
            {
                _controller?.SubmitExtinguisherSelection(FireTrainingWorkflow.TargetExtinguisherWater);
            });

            // Option 3: Foam Extinguisher (Incorrect)
            CreateOptionButton("3. Foam Extinguisher (AFFF)", new Vector2(0f, 0.02f), new Vector2(1f, 0.32f), () =>
            {
                _controller?.SubmitExtinguisherSelection(FireTrainingWorkflow.TargetExtinguisherFoam);
            });
        }

        private void ShowExtinguisherSelectedUI()
        {
            ClearActionButtons();

            if (_promptText != null)
            {
                _promptText.text = "CORRECT: CO2 Extinguisher Selected!\n<size=80%>Non-conductive agent safe for energized electrical fires</size>";
                _promptText.color = new Color(0.25f, 0.95f, 0.4f);
            }

            if (_bannerBg != null)
            {
                _bannerBg.color = new Color(0.12f, 0.28f, 0.16f, 0.94f);
            }
        }

        private Button CreateOptionButton(string label, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
        {
            return CreateActionButton(label, anchorMin, anchorMax, new Color(0.18f, 0.22f, 0.28f, 0.92f), onClick);
        }

        private Button CreateActionButton(string label, Vector2 anchorMin, Vector2 anchorMax, Color bgColor, System.Action onClick)
        {
            var btnObj = new GameObject("ActionButton_" + label);
            btnObj.transform.SetParent(_actionContainer.transform, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var textObj = new GameObject("Label");
            textObj.transform.SetParent(btnObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 4);
            textRect.offsetMax = new Vector2(-10, -4);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 24;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14;
            tmp.fontSizeMax = 28;
            tmp.color = Color.white;

            return btn;
        }
    }
}
