// GasAssessmentSummaryUI.cs
// Namespace : IndustrialSafetyAR.UI
//
// Worker-facing Assessment Summary UI displaying Gas Leak & Confined Space Safety module completion results:
// - Duration, final score / 100, PASS / FAIL visual indicators
// - Per-step scoring breakdown (Steps 1–9)
// - Penalty feedback (e.g. unsafe atmospheric entry attempt -15 penalty)
// - Interactive control buttons (Review Breakdown, Finalize / Sync, Retake Training, Return to Home).

using System;
using System.Text;
using IndustrialSafetyAR.Assessment;
using IndustrialSafetyAR.Core;
using IndustrialSafetyAR.Modules.GasConfinedSpace;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IndustrialSafetyAR.UI
{
    public class GasAssessmentSummaryUI : MonoBehaviour
    {
        [SerializeField]
        private GasArInteractionController _controller;

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
        private TextMeshProUGUI _reviewButtonText;
        private TextMeshProUGUI _retakeButtonText;
        private TextMeshProUGUI _returnHomeButtonText;

        private AssessmentSummaryViewModel _currentViewModel;

        public GasArInteractionController Controller
        {
            get => _controller;
            set
            {
                if (_controller != value)
                {
                    UnsubscribeEvents();
                    _controller = value;
                    SubscribeEvents();
                }
            }
        }

        public AssessmentSummaryViewModel CurrentViewModel => _currentViewModel;
        public bool IsSummaryVisible => _modalRoot != null && _modalRoot.activeSelf;
        public Button FinishButton => _finishButton;

        private void Awake()
        {
            if (_controller == null)
            {
                _controller = GetComponent<GasArInteractionController>() ?? FindAnyObjectByType<GasArInteractionController>();
            }
        }

        private bool _isSummaryRequested;

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (_controller != null)
            {
                _controller.OnAssessmentCompleted += HandleAssessmentCompleted;
                _controller.OnAttemptFinalizedForOutbox += HandleAttemptFinalizedForOutbox;
                if (_controller.StepNavigator != null)
                {
                    _controller.StepNavigator.OnCompleteTrainingRequested += HandleCompleteTrainingRequested;
                }
            }

            if (LocaleService.Instance != null)
            {
                LocaleService.Instance.OnLanguageChanged -= HandleLanguageChanged;
                LocaleService.Instance.OnLanguageChanged += HandleLanguageChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_controller != null)
            {
                _controller.OnAssessmentCompleted -= HandleAssessmentCompleted;
                _controller.OnAttemptFinalizedForOutbox -= HandleAttemptFinalizedForOutbox;
                if (_controller.StepNavigator != null)
                {
                    _controller.StepNavigator.OnCompleteTrainingRequested -= HandleCompleteTrainingRequested;
                }
            }

            if (LocaleService.Instance != null)
            {
                LocaleService.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void HandleLanguageChanged(string newLang)
        {
            if (IsSummaryVisible)
            {
                UpdateUIContents();
            }
        }

        public void HandleCompleteTrainingRequested()
        {
            _isSummaryRequested = true;
            if (_currentViewModel != null)
            {
                ShowSummary(_currentViewModel);
            }
            else if (_controller != null && _controller.LatestAttempt != null && _controller.LatestAssessment != null)
            {
                ShowSummary(AssessmentSummaryViewModel.Build(_controller.LatestAttempt, _controller.LatestAssessment));
            }
            else
            {
                ShowSummary(new AssessmentSummaryViewModel());
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
                _finishButtonText.text = "Finalized for Sync";
            }
        }

        public void HandleAssessmentCompleted(TrainingAttempt attempt, AssessmentResult assessment)
        {
            _currentViewModel = AssessmentSummaryViewModel.Build(attempt, assessment);
            if (_isSummaryRequested || _controller == null || _controller.StepNavigator == null)
            {
                ShowSummary(_currentViewModel);
            }
        }

        public void ShowSummary(AssessmentSummaryViewModel viewModel)
        {
            _currentViewModel = viewModel ?? new AssessmentSummaryViewModel();
            EnsureSummaryModal();

            bool isFinalized = (_controller != null && _controller.IsAttemptFinalizedForOutbox) || _currentViewModel.SyncPrepared;
            if (isFinalized)
            {
                _currentViewModel.SyncPrepared = true;
                if (_finishButton != null) _finishButton.interactable = false;
                if (_finishButtonText != null) _finishButtonText.text = "Finalized for Sync";
            }
            else
            {
                if (_finishButton != null) _finishButton.interactable = true;
                if (_finishButtonText != null) _finishButtonText.text = "2. Finish / Prepare Sync";
            }

            if (_modalRoot != null)
            {
                if (_modalRoot.transform.parent != null && !_modalRoot.transform.parent.gameObject.activeSelf)
                {
                    _modalRoot.transform.parent.gameObject.SetActive(true);
                }
                _modalRoot.SetActive(true);
                _modalRoot.transform.SetAsLastSibling();
            }

            if (WorkerHomeController.Instance != null)
            {
                WorkerHomeController.Instance.SetState(WorkerHomeController.WorkerAppScreenState.Results);
            }

            if (IndustrialSafetyAR.AR.ARModeController.Instance != null)
            {
                IndustrialSafetyAR.AR.ARModeController.Instance.DisableAR();
            }

            UpdateUIContents();
        }

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

            var loc = LocaleService.Instance;

            if (_titleText != null)
            {
                string modTitle = loc.Get("module_gas_title", "Gas Leak & Confined Space Safety").ToUpper();
                string summarySub = $"{loc.Get("assessment_title", "Assessment Summary")} • {loc.Get("app_title", "Industrial Safety AR")}";
                _titleText.text = $"<b>{modTitle}</b>\n<size=70%>{summarySub}</size>";
            }

            if (_badgeBackground != null)
            {
                _badgeBackground.color = _currentViewModel.Passed
                    ? new Color(0.12f, 0.55f, 0.28f, 0.96f) // Vibrant Green
                    : new Color(0.70f, 0.18f, 0.18f, 0.96f); // Crimson Red
            }

            if (_scoreBadgeText != null)
            {
                string statusText = _currentViewModel.Passed ? loc.Get("assessment_status_passed", "PASS") : loc.Get("assessment_status_failed", "FAILED — RETAKE REQUIRED");
                _scoreBadgeText.text = $"<size=120%><b>{_currentViewModel.ScoreDisplayText}</b></size>\n<size=85%><b>{statusText}</b></size>";
            }

            if (_metaText != null)
            {
                string durLabel = loc.Get("assessment_duration_label", "Duration");
                string workerLabel = loc.Get("worker_id_label", "Worker ID");
                _metaText.text = $"{durLabel}: <b>{_currentViewModel.DurationText}</b>   |   {workerLabel}: <b>{_currentViewModel.WorkerId ?? "offline"}</b>   |   Pass: <b>{_currentViewModel.PassPercent:0}%</b>";
            }

            if (_safetyFeedbackText != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"<b>{loc.Get("assessment_breakdown_header", "Safety Compliance Assessment:")}</b>");
                sb.AppendLine(_currentViewModel.SafetyFeedback);

                if (_currentViewModel.Penalties != null && _currentViewModel.Penalties.Count > 0)
                {
                    sb.AppendLine($"\n<color=#FF7043><b>{loc.Get("assessment_deductions_label", "Deductions & Identified Risks:")}</b></color>");
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
                sb.AppendLine($"<b>{loc.Get("assessment_breakdown_header", "Step-by-Step Scoring Breakdown:")}</b>");
                sb.AppendLine("--------------------------------------------------");

                if (_currentViewModel.StepSummaries != null)
                {
                    foreach (var step in _currentViewModel.StepSummaries)
                    {
                        string mark = step.IsSatisfied ? (step.PenaltyDeducted > 0 ? "[!]" : "[OK]") : "[X]";
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
                string syncReady = loc.Get("assessment_sync_ready", "Prepared for Outbox Sync");
                _syncStatusText.text = _currentViewModel.SyncPrepared
                    ? $"<color=#81C784>● {syncReady} (Attempt: {_currentViewModel.ClientAttemptId?.Substring(0, Math.Min(8, _currentViewModel.ClientAttemptId.Length))}...)</color>"
                    : loc.Get("assessment_sync_pending", "Offline session stored locally. Ready for sync confirmation.");
            }

            if (_reviewButtonText != null) _reviewButtonText.text = $"1. {loc.Get("assessment_btn_breakdown", "Review Performance")}";
            if (_retakeButtonText != null) _retakeButtonText.text = $"3. {loc.Get("assessment_btn_retake", "Retake Training")}";
            if (_returnHomeButtonText != null) _returnHomeButtonText.text = $"← {loc.Get("assessment_btn_finish", "Return to Home Menu")}";
            if (_finishButtonText != null)
            {
                if (_currentViewModel.SyncPrepared)
                {
                    _finishButtonText.text = loc.Get("assessment_outbox_prepared", "Finalized for Sync");
                }
                else
                {
                    _finishButtonText.text = $"2. {loc.Get("assessment_outbox_prepared", "Finish / Prepare Sync")}";
                }
            }
        }

        public void EnsureSummaryModal()
        {
            if (_modalRoot != null) return;

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("GasAssessmentSummaryCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            var font = GasInteractionFeedbackUI.GetDefaultFont();

            // Backdrop
            _modalRoot = new GameObject("GasAssessmentSummaryModal");
            _modalRoot.transform.SetParent(canvas.transform, false);
            var rootRect = _modalRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var bgImg = _modalRoot.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.07f, 0.12f, 0.96f);

            // Dialog Panel
            var panelObj = new GameObject("CardPanel");
            panelObj.transform.SetParent(_modalRoot.transform, false);
            var panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.04f, 0.04f);
            panelRect.anchorMax = new Vector2(0.96f, 0.96f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImg = panelObj.AddComponent<Image>();
            panelImg.color = new Color(0.09f, 0.13f, 0.20f, 0.98f);

            // Title
            var titleObj = new GameObject("SummaryTitle");
            titleObj.transform.SetParent(panelObj.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.88f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            _titleText = titleObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _titleText.font = font;
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.fontSize = 24;
            _titleText.color = Color.white;

            // Score Badge
            var badgeObj = new GameObject("ScoreBadge");
            badgeObj.transform.SetParent(panelObj.transform, false);
            var badgeRect = badgeObj.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.12f, 0.73f);
            badgeRect.anchorMax = new Vector2(0.88f, 0.87f);
            badgeRect.offsetMin = Vector2.zero;
            badgeRect.offsetMax = Vector2.zero;

            _badgeBackground = badgeObj.AddComponent<Image>();
            _badgeBackground.color = new Color(0.12f, 0.55f, 0.28f, 0.96f);

            var scoreTextObj = new GameObject("ScoreText");
            scoreTextObj.transform.SetParent(badgeObj.transform, false);
            var stRect = scoreTextObj.AddComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.offsetMin = Vector2.zero;
            stRect.offsetMax = Vector2.zero;

            _scoreBadgeText = scoreTextObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _scoreBadgeText.font = font;
            _scoreBadgeText.alignment = TextAlignmentOptions.Center;
            _scoreBadgeText.fontSize = 22;
            _scoreBadgeText.color = Color.white;

            // Meta info (duration, worker ID)
            var metaObj = new GameObject("MetaText");
            metaObj.transform.SetParent(panelObj.transform, false);
            var metaRect = metaObj.AddComponent<RectTransform>();
            metaRect.anchorMin = new Vector2(0.05f, 0.67f);
            metaRect.anchorMax = new Vector2(0.95f, 0.72f);
            metaRect.offsetMin = Vector2.zero;
            metaRect.offsetMax = Vector2.zero;

            _metaText = metaObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _metaText.font = font;
            _metaText.alignment = TextAlignmentOptions.Center;
            _metaText.fontSize = 15;
            _metaText.color = new Color(0.70f, 0.78f, 0.88f);

            // Scrollable Content Area for Feedback & Breakdown
            var scrollAreaObj = new GameObject("ScrollArea");
            scrollAreaObj.transform.SetParent(panelObj.transform, false);
            var saRect = scrollAreaObj.AddComponent<RectTransform>();
            saRect.anchorMin = new Vector2(0.05f, 0.28f);
            saRect.anchorMax = new Vector2(0.95f, 0.66f);
            saRect.offsetMin = Vector2.zero;
            saRect.offsetMax = Vector2.zero;

            var saImg = scrollAreaObj.AddComponent<Image>();
            saImg.color = new Color(0.06f, 0.09f, 0.14f, 0.85f);

            // Safety Feedback Text
            var feedbackObj = new GameObject("SafetyFeedbackText");
            feedbackObj.transform.SetParent(scrollAreaObj.transform, false);
            var fbRect = feedbackObj.AddComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.04f, 0.04f);
            fbRect.anchorMax = new Vector2(0.96f, 0.96f);
            fbRect.offsetMin = Vector2.zero;
            fbRect.offsetMax = Vector2.zero;

            _safetyFeedbackText = feedbackObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _safetyFeedbackText.font = font;
            _safetyFeedbackText.alignment = TextAlignmentOptions.TopLeft;
            _safetyFeedbackText.fontSize = 15;
            _safetyFeedbackText.color = new Color(0.90f, 0.93f, 0.96f);

            // Step Breakdown Container (initially hidden, toggled via Review Performance)
            _breakdownContainer = new GameObject("BreakdownContainer");
            _breakdownContainer.transform.SetParent(scrollAreaObj.transform, false);
            var bdRect = _breakdownContainer.AddComponent<RectTransform>();
            bdRect.anchorMin = new Vector2(0.04f, 0.04f);
            bdRect.anchorMax = new Vector2(0.96f, 0.96f);
            bdRect.offsetMin = Vector2.zero;
            bdRect.offsetMax = Vector2.zero;

            _breakdownText = _breakdownContainer.AddComponent<TextMeshProUGUI>();
            if (font != null) _breakdownText.font = font;
            _breakdownText.alignment = TextAlignmentOptions.TopLeft;
            _breakdownText.fontSize = 14;
            _breakdownText.color = new Color(0.90f, 0.93f, 0.96f);
            _breakdownContainer.SetActive(false);

            // Sync Status
            var syncObj = new GameObject("SyncStatus");
            syncObj.transform.SetParent(panelObj.transform, false);
            var syncRect = syncObj.AddComponent<RectTransform>();
            syncRect.anchorMin = new Vector2(0.05f, 0.22f);
            syncRect.anchorMax = new Vector2(0.95f, 0.27f);
            syncRect.offsetMin = Vector2.zero;
            syncRect.offsetMax = Vector2.zero;

            _syncStatusText = syncObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _syncStatusText.font = font;
            _syncStatusText.alignment = TextAlignmentOptions.Center;
            _syncStatusText.fontSize = 14;
            _syncStatusText.color = new Color(0.55f, 0.65f, 0.78f);

            // Action Buttons Container (Bottom Row)
            var btnContainerObj = new GameObject("ButtonsContainer");
            btnContainerObj.transform.SetParent(panelObj.transform, false);
            var bcRect = btnContainerObj.AddComponent<RectTransform>();
            bcRect.anchorMin = new Vector2(0.04f, 0.02f);
            bcRect.anchorMax = new Vector2(0.96f, 0.21f);
            bcRect.offsetMin = Vector2.zero;
            bcRect.offsetMax = Vector2.zero;

            // Button 1: Review Breakdown
            var revBtnObj = CreateButton(btnContainerObj, "ReviewButton", new Vector2(0.0f, 0.52f), new Vector2(0.48f, 0.98f),
                new Color(0.18f, 0.35f, 0.65f), font, out _reviewButtonText);
            var revTap = revBtnObj.AddComponent<TapGatedButton>();
            revTap.Initialize(ToggleBreakdownView);

            // Button 2: Finish & Prepare Sync
            var finBtnObj = CreateButton(btnContainerObj, "FinishSyncButton", new Vector2(0.52f, 0.52f), new Vector2(1.0f, 0.98f),
                new Color(0.12f, 0.55f, 0.35f), font, out _finishButtonText);
            _finishButton = finBtnObj.GetComponent<Button>();
            var finTap = finBtnObj.AddComponent<TapGatedButton>();
            finTap.Initialize(OnFinishSyncClicked);

            // Button 3: Retake Training
            var retakeBtnObj = CreateButton(btnContainerObj, "RetakeButton", new Vector2(0.0f, 0.02f), new Vector2(0.48f, 0.48f),
                new Color(0.65f, 0.40f, 0.12f), font, out _retakeButtonText);
            var retakeTap = retakeBtnObj.AddComponent<TapGatedButton>();
            retakeTap.Initialize(OnRetakeClicked);

            // Button 4: Return to Home
            var homeBtnObj = CreateButton(btnContainerObj, "ReturnHomeButton", new Vector2(0.52f, 0.02f), new Vector2(1.0f, 0.48f),
                new Color(0.25f, 0.28f, 0.35f), font, out _returnHomeButtonText);
            var homeTap = homeBtnObj.AddComponent<TapGatedButton>();
            homeTap.Initialize(OnReturnHomeClicked);
        }

        private GameObject CreateButton(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color bgColor, TMP_FontAsset font, out TextMeshProUGUI label)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent.transform, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = btnObj.AddComponent<Image>();
            img.color = bgColor;
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            var textObj = new GameObject("Label");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4, 2);
            textRect.offsetMax = new Vector2(-4, -2);

            label = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 15;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            return btnObj;
        }

        private void ToggleBreakdownView()
        {
            _isBreakdownVisible = !_isBreakdownVisible;
            if (_breakdownContainer != null) _breakdownContainer.SetActive(_isBreakdownVisible);
            if (_safetyFeedbackText != null) _safetyFeedbackText.gameObject.SetActive(!_isBreakdownVisible);
        }

        private void OnFinishSyncClicked()
        {
            if (_controller != null)
            {
                _controller.FinalizeAttemptForOutbox(out var attempt);
            }
        }

        private void OnRetakeClicked()
        {
            HideSummary();
            _isSummaryRequested = false;

            if (_controller != null)
            {
                _controller.ResetScenario();
            }

            if (IndustrialSafetyAR.AR.ARModeController.Instance != null)
            {
                IndustrialSafetyAR.AR.ARModeController.Instance.EnableAR();
            }

            var gasUI = FindAnyObjectByType<GasInteractionFeedbackUI>(FindObjectsInactive.Include);
            if (gasUI != null)
            {
                gasUI.ShowTrainingUI();
            }
        }

        private void OnReturnHomeClicked()
        {
            HideSummary();
            _isSummaryRequested = false;

            if (WorkerHomeController.Instance != null)
            {
                WorkerHomeController.Instance.ReturnToHome();
            }
        }
    }
}
