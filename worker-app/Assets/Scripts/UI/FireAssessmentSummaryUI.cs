// FireAssessmentSummaryUI.cs
// Namespace : IndustrialSafetyAR.UI
//
// Worker-facing Assessment Summary UI displaying module completion results,
// duration, final score / 100, PASS / FAILED visual indicators, per-step breakdown (Steps 1–9),
// penalty feedback, and interactive control buttons (Review Performance, Finish Session, Retake).

using System;
using System.Text;
using IndustrialSafetyAR.Assessment;
using IndustrialSafetyAR.Modules.FireExplosion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IndustrialSafetyAR.UI
{
    /// <summary>
    /// Displays the finalized assessment summary dialog to the worker upon completing
    /// the Fire &amp; Explosion Response training scenario.
    /// </summary>
    public class FireAssessmentSummaryUI : MonoBehaviour
    {
        [SerializeField]
        private FireArInteractionController _controller;

        private GameObject _modalRoot;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _scoreBadgeText;
        private TextMeshProUGUI _metaText;
        private TextMeshProUGUI _safetyFeedbackText;
        private TextMeshProUGUI _breakdownText;
        private TextMeshProUGUI _syncStatusText;
        private Image _badgeBackground;
        private GameObject _breakdownContainer;
        private bool _isBreakdownVisible;
        private Button _finishButton;
        private TextMeshProUGUI _finishButtonText;

        private AssessmentSummaryViewModel _currentViewModel;

        public FireArInteractionController Controller
        {
            get => _controller;
            set => _controller = value;
        }

        public AssessmentSummaryViewModel CurrentViewModel => _currentViewModel;
        public bool IsSummaryVisible => _modalRoot != null && _modalRoot.activeSelf;

        private void Awake()
        {
            if (_controller == null)
            {
                _controller = GetComponent<FireArInteractionController>() ?? FindAnyObjectByType<FireArInteractionController>();
            }
        }

        private void OnEnable()
        {
            if (_controller != null)
            {
                _controller.OnAssessmentCompleted += HandleAssessmentCompleted;
                _controller.OnAttemptFinalizedForOutbox += HandleAttemptFinalizedForOutbox;
            }
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.OnAssessmentCompleted -= HandleAssessmentCompleted;
                _controller.OnAttemptFinalizedForOutbox -= HandleAttemptFinalizedForOutbox;
            }
        }

        public void HandleAttemptFinalizedForOutbox(TrainingAttempt attempt)
        {
            if (_currentViewModel != null)
            {
                _currentViewModel.SyncPrepared = true;
                UpdateUIContents();
            }

            if (_finishButton != null)
            {
                _finishButton.interactable = false;
            }
            if (_finishButtonText != null)
            {
                _finishButtonText.text = "✓ Finalized for Sync";
            }
        }

        public void HandleAssessmentCompleted(TrainingAttempt attempt, AssessmentResult assessment)
        {
            var vm = AssessmentSummaryViewModel.Build(attempt, assessment);
            ShowSummary(vm);
        }

        /// <summary>
        /// Renders the assessment summary on screen using the provided view model.
        /// </summary>
        public void ShowSummary(AssessmentSummaryViewModel viewModel)
        {
            _currentViewModel = viewModel ?? new AssessmentSummaryViewModel();
            EnsureSummaryModal();

            bool isFinalized = (_controller != null && _controller.IsAttemptFinalizedForOutbox) || _currentViewModel.SyncPrepared;
            if (isFinalized)
            {
                _currentViewModel.SyncPrepared = true;
                if (_finishButton != null) _finishButton.interactable = false;
                if (_finishButtonText != null) _finishButtonText.text = "✓ Finalized for Sync";
            }
            else
            {
                if (_finishButton != null) _finishButton.interactable = true;
                if (_finishButtonText != null) _finishButtonText.text = "2. Finish / Prepare Sync";
            }

            if (_modalRoot != null)
            {
                _modalRoot.SetActive(true);
            }

            UpdateUIContents();
        }

        /// <summary>
        /// Hides the assessment summary dialog.
        /// </summary>
        public void HideSummary()
        {
            if (_modalRoot != null)
            {
                _modalRoot.SetActive(false);
            }
        }

        private void UpdateUIContents()
        {
            if (_currentViewModel == null) return;

            if (_titleText != null)
            {
                _titleText.text = $"<b>{_currentViewModel.ModuleTitle.ToUpper()}</b>\n<size=70%>Assessment Summary • Industrial Safety AR</size>";
            }

            if (_badgeBackground != null)
            {
                _badgeBackground.color = _currentViewModel.Passed
                    ? new Color(0.12f, 0.55f, 0.28f, 0.96f) // Vibrant Green
                    : new Color(0.70f, 0.18f, 0.18f, 0.96f); // Crimson Red
            }

            if (_scoreBadgeText != null)
            {
                string statusText = _currentViewModel.Passed ? "PASS" : "FAILED — RETAKE REQUIRED";
                _scoreBadgeText.text = $"<size=120%><b>{_currentViewModel.ScoreDisplayText}</b></size>\n<size=85%><b>{statusText}</b></size>";
            }

            if (_metaText != null)
            {
                _metaText.text = $"Duration: <b>{_currentViewModel.DurationText}</b>   |   Worker: <b>{_currentViewModel.WorkerId ?? "offline"}</b>   |   Pass Threshold: <b>{_currentViewModel.PassPercent:0}%</b>";
            }

            if (_safetyFeedbackText != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"<b>Safety Compliance Assessment:</b>");
                sb.AppendLine(_currentViewModel.SafetyFeedback);

                if (_currentViewModel.Penalties != null && _currentViewModel.Penalties.Count > 0)
                {
                    sb.AppendLine("\n<color=#FF7043><b>Deductions & Identified Risks:</b></color>");
                    foreach (var pen in _currentViewModel.Penalties)
                    {
                        sb.AppendLine($"• {pen}");
                    }
                }

                _safetyFeedbackText.text = sb.ToString();
            }

            if (_breakdownText != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("<b>Step-by-Step Scoring Breakdown:</b>");
                sb.AppendLine("--------------------------------------------------");

                if (_currentViewModel.StepSummaries != null)
                {
                    foreach (var step in _currentViewModel.StepSummaries)
                    {
                        string mark = step.IsSatisfied ? (step.PenaltyDeducted > 0 ? "[!]" : "[✓]") : "[X]";
                        string colorTag = step.IsSatisfied
                            ? (step.PenaltyDeducted > 0 ? "<color=#FFB74D>" : "<color=#81C784>")
                            : "<color=#E57373>";

                        sb.AppendLine($"{colorTag}{mark} {step.Title}</color> : <b>{step.NetScore:0.00} / {step.MaxPoints:0} pts</b> ({step.StatusText})");
                    }
                }

                _breakdownText.text = sb.ToString();
            }

            if (_syncStatusText != null)
            {
                _syncStatusText.text = _currentViewModel.SyncPrepared
                    ? $"<color=#81C784>✓ Prepared for Outbox Sync (Attempt: {_currentViewModel.ClientAttemptId?.Substring(0, Math.Min(8, _currentViewModel.ClientAttemptId.Length))}...)</color>"
                    : "Offline session stored locally. Ready for sync confirmation.";
            }
        }

        private void EnsureSummaryModal()
        {
            if (_modalRoot != null) return;

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("AssessmentCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Modal Background Overlay
            _modalRoot = new GameObject("AssessmentSummaryModal");
            _modalRoot.transform.SetParent(canvas.transform, false);

            var rootRect = _modalRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.04f, 0.04f);
            rootRect.anchorMax = new Vector2(0.96f, 0.96f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var rootBg = _modalRoot.AddComponent<Image>();
            rootBg.color = new Color(0.07f, 0.09f, 0.13f, 0.97f);

            // Title Banner
            var titleObj = new GameObject("ModalTitle");
            titleObj.transform.SetParent(_modalRoot.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.90f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            _titleText = titleObj.AddComponent<TextMeshProUGUI>();
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.fontSize = 28;
            _titleText.enableAutoSizing = true;
            _titleText.fontSizeMin = 18;
            _titleText.fontSizeMax = 32;
            _titleText.color = Color.white;

            // Score & Status Badge Card
            var badgeObj = new GameObject("ScoreBadgeCard");
            badgeObj.transform.SetParent(_modalRoot.transform, false);
            var badgeRect = badgeObj.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.06f, 0.73f);
            badgeRect.anchorMax = new Vector2(0.94f, 0.89f);
            badgeRect.offsetMin = Vector2.zero;
            badgeRect.offsetMax = Vector2.zero;

            _badgeBackground = badgeObj.AddComponent<Image>();
            _badgeBackground.color = new Color(0.12f, 0.55f, 0.28f, 0.96f);

            var scoreTextObj = new GameObject("ScoreText");
            scoreTextObj.transform.SetParent(badgeObj.transform, false);
            var scoreRect = scoreTextObj.AddComponent<RectTransform>();
            scoreRect.anchorMin = Vector2.zero;
            scoreRect.anchorMax = Vector2.one;
            scoreRect.offsetMin = new Vector2(12, 4);
            scoreRect.offsetMax = new Vector2(-12, -4);

            _scoreBadgeText = scoreTextObj.AddComponent<TextMeshProUGUI>();
            _scoreBadgeText.alignment = TextAlignmentOptions.Center;
            _scoreBadgeText.fontSize = 32;
            _scoreBadgeText.enableAutoSizing = true;
            _scoreBadgeText.fontSizeMin = 18;
            _scoreBadgeText.fontSizeMax = 40;
            _scoreBadgeText.color = Color.white;

            // Metadata Row
            var metaObj = new GameObject("MetadataRow");
            metaObj.transform.SetParent(_modalRoot.transform, false);
            var metaRect = metaObj.AddComponent<RectTransform>();
            metaRect.anchorMin = new Vector2(0.06f, 0.68f);
            metaRect.anchorMax = new Vector2(0.94f, 0.72f);
            metaRect.offsetMin = Vector2.zero;
            metaRect.offsetMax = Vector2.zero;

            _metaText = metaObj.AddComponent<TextMeshProUGUI>();
            _metaText.alignment = TextAlignmentOptions.Center;
            _metaText.fontSize = 18;
            _metaText.enableAutoSizing = true;
            _metaText.fontSizeMin = 12;
            _metaText.fontSizeMax = 22;
            _metaText.color = new Color(0.85f, 0.88f, 0.92f);

            // Safety Feedback Box
            var feedbackObj = new GameObject("SafetyFeedbackBox");
            feedbackObj.transform.SetParent(_modalRoot.transform, false);
            var feedbackRect = feedbackObj.AddComponent<RectTransform>();
            feedbackRect.anchorMin = new Vector2(0.06f, 0.44f);
            feedbackRect.anchorMax = new Vector2(0.94f, 0.67f);
            feedbackRect.offsetMin = Vector2.zero;
            feedbackRect.offsetMax = Vector2.zero;

            var feedbackBg = feedbackObj.AddComponent<Image>();
            feedbackBg.color = new Color(0.12f, 0.15f, 0.20f, 0.90f);

            var feedbackTextObj = new GameObject("FeedbackText");
            feedbackTextObj.transform.SetParent(feedbackObj.transform, false);
            var fbTextRect = feedbackTextObj.AddComponent<RectTransform>();
            fbTextRect.anchorMin = Vector2.zero;
            fbTextRect.anchorMax = Vector2.one;
            fbTextRect.offsetMin = new Vector2(16, 8);
            fbTextRect.offsetMax = new Vector2(-16, -8);

            _safetyFeedbackText = feedbackTextObj.AddComponent<TextMeshProUGUI>();
            _safetyFeedbackText.alignment = TextAlignmentOptions.TopLeft;
            _safetyFeedbackText.fontSize = 18;
            _safetyFeedbackText.enableAutoSizing = true;
            _safetyFeedbackText.fontSizeMin = 12;
            _safetyFeedbackText.fontSizeMax = 22;
            _safetyFeedbackText.color = new Color(0.92f, 0.94f, 0.96f);

            // Step Breakdown Container (collapsible / toggleable)
            _breakdownContainer = new GameObject("StepBreakdownContainer");
            _breakdownContainer.transform.SetParent(_modalRoot.transform, false);
            var bdRect = _breakdownContainer.AddComponent<RectTransform>();
            bdRect.anchorMin = new Vector2(0.06f, 0.22f);
            bdRect.anchorMax = new Vector2(0.94f, 0.43f);
            bdRect.offsetMin = Vector2.zero;
            bdRect.offsetMax = Vector2.zero;

            var bdBg = _breakdownContainer.AddComponent<Image>();
            bdBg.color = new Color(0.10f, 0.12f, 0.16f, 0.90f);

            var bdTextObj = new GameObject("BreakdownText");
            bdTextObj.transform.SetParent(_breakdownContainer.transform, false);
            var bdTextRect = bdTextObj.AddComponent<RectTransform>();
            bdTextRect.anchorMin = Vector2.zero;
            bdTextRect.anchorMax = Vector2.one;
            bdTextRect.offsetMin = new Vector2(14, 6);
            bdTextRect.offsetMax = new Vector2(-14, -6);

            _breakdownText = bdTextObj.AddComponent<TextMeshProUGUI>();
            _breakdownText.alignment = TextAlignmentOptions.TopLeft;
            _breakdownText.fontSize = 16;
            _breakdownText.enableAutoSizing = true;
            _breakdownText.fontSizeMin = 11;
            _breakdownText.fontSizeMax = 20;
            _breakdownText.color = new Color(0.88f, 0.90f, 0.94f);

            _isBreakdownVisible = true;

            // Sync Status Text
            var syncObj = new GameObject("SyncStatusRow");
            syncObj.transform.SetParent(_modalRoot.transform, false);
            var syncRect = syncObj.AddComponent<RectTransform>();
            syncRect.anchorMin = new Vector2(0.06f, 0.17f);
            syncRect.anchorMax = new Vector2(0.94f, 0.21f);
            syncRect.offsetMin = Vector2.zero;
            syncRect.offsetMax = Vector2.zero;

            _syncStatusText = syncObj.AddComponent<TextMeshProUGUI>();
            _syncStatusText.alignment = TextAlignmentOptions.Center;
            _syncStatusText.fontSize = 16;
            _syncStatusText.enableAutoSizing = true;
            _syncStatusText.fontSizeMin = 12;
            _syncStatusText.fontSizeMax = 20;
            _syncStatusText.color = new Color(0.70f, 0.75f, 0.82f);

            // Button 1: Review Performance (toggle breakdown)
            CreateButton("1. Review Performance", new Vector2(0.06f, 0.11f), new Vector2(0.34f, 0.16f), new Color(0.20f, 0.35f, 0.55f, 0.95f), () =>
            {
                OnReviewPerformanceClicked();
            });

            // Button 2: Finish Session / Prepare Outbox Sync
            _finishButton = CreateButton("2. Finish / Prepare Sync", new Vector2(0.36f, 0.11f), new Vector2(0.64f, 0.16f), new Color(0.22f, 0.48f, 0.30f, 0.95f), () =>
            {
                OnFinishSessionClicked();
            });
            _finishButtonText = _finishButton.GetComponentInChildren<TextMeshProUGUI>();

            // Button 3: Retake Training
            CreateButton("3. Retake Training", new Vector2(0.66f, 0.11f), new Vector2(0.94f, 0.16f), new Color(0.65f, 0.25f, 0.20f, 0.95f), () =>
            {
                OnRetakeTrainingClicked();
            });
        }

        public void OnReviewPerformanceClicked()
        {
            _isBreakdownVisible = !_isBreakdownVisible;
            if (_breakdownContainer != null)
            {
                _breakdownContainer.SetActive(_isBreakdownVisible);
            }
        }

        public void OnFinishSessionClicked()
        {
            if (_controller != null)
            {
                if (_controller.IsAttemptFinalizedForOutbox)
                {
                    Debug.LogWarning("[FireAssessmentSummaryUI] Attempt is already finalized for outbox sync.");
                    return;
                }

                bool success = _controller.FinalizeAttemptForOutbox(out var attempt);
                if (success)
                {
                    if (_currentViewModel != null)
                    {
                        _currentViewModel.SyncPrepared = true;
                    }
                    if (_finishButton != null)
                    {
                        _finishButton.interactable = false;
                    }
                    if (_finishButtonText != null)
                    {
                        _finishButtonText.text = "✓ Finalized for Sync";
                    }
                    UpdateUIContents();
                    Debug.Log($"[FireAssessmentSummaryUI] Finish Session: Attempt {attempt?.ClientAttemptId} finalized and prepared for offline outbox sync.");
                }
                else
                {
                    Debug.LogWarning("[FireAssessmentSummaryUI] Finalization rejected: attempt already finalized, null, or training incomplete.");
                }
            }
            else if (_currentViewModel != null && !_currentViewModel.SyncPrepared)
            {
                _currentViewModel.SyncPrepared = true;
                if (_finishButton != null)
                {
                    _finishButton.interactable = false;
                }
                if (_finishButtonText != null)
                {
                    _finishButtonText.text = "✓ Finalized for Sync";
                }
                UpdateUIContents();
                Debug.Log($"[FireAssessmentSummaryUI] Finish Session (standalone): Attempt {_currentViewModel.ClientAttemptId} prepared for offline outbox sync.");
            }
        }

        public void OnRetakeTrainingClicked()
        {
            HideSummary();
            if (_finishButton != null)
            {
                _finishButton.interactable = true;
            }
            if (_finishButtonText != null)
            {
                _finishButtonText.text = "2. Finish / Prepare Sync";
            }
            if (_controller != null)
            {
                _controller.RetakeTraining();
            }
        }

        private Button CreateButton(string label, Vector2 anchorMin, Vector2 anchorMax, Color bgColor, Action onClick)
        {
            var btnObj = new GameObject("Btn_" + label);
            btnObj.transform.SetParent(_modalRoot.transform, false);

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

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4, 2);
            textRect.offsetMax = new Vector2(-4, -2);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 18;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 11;
            tmp.fontSizeMax = 22;
            tmp.color = Color.white;

            return btn;
        }
    }
}
