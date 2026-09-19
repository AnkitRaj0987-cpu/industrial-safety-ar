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
        private TextMeshProUGUI _reviewButtonText;
        private TextMeshProUGUI _retakeButtonText;
        private TextMeshProUGUI _returnHomeButtonText;

        private AssessmentSummaryViewModel _currentViewModel;

        public FireArInteractionController Controller
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

        private void Awake()
        {
            if (_controller == null)
            {
                _controller = GetComponent<FireArInteractionController>() ?? FindAnyObjectByType<FireArInteractionController>();
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

            if (IndustrialSafetyAR.Core.LocaleService.Instance != null)
            {
                IndustrialSafetyAR.Core.LocaleService.Instance.OnLanguageChanged -= HandleLanguageChanged;
                IndustrialSafetyAR.Core.LocaleService.Instance.OnLanguageChanged += HandleLanguageChanged;
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

            if (IndustrialSafetyAR.Core.LocaleService.Instance != null)
            {
                IndustrialSafetyAR.Core.LocaleService.Instance.OnLanguageChanged -= HandleLanguageChanged;
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

            var loc = IndustrialSafetyAR.Core.LocaleService.Instance;

            if (_titleText != null)
            {
                string modTitle = loc.Get("module_fire_title", _currentViewModel.ModuleTitle ?? "Fire & Explosion Response").ToUpper();
                string summarySub = $"{loc.Get("assessment_title", "Assessment Summary")} • {loc.Get("app_title", "Industrial Safety AR")}";
                _titleText.text = $"<b>{modTitle}</b>\n<size=70%>{summarySub}</size>";
            }

            if (_badgeBackground != null)
            {
                _badgeBackground.color = _currentViewModel.Passed
                    ? UITheme.Success
                    : UITheme.Danger;
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
                    sb.AppendLine($"\n<color=#DC2626><b>{loc.Get("assessment_deductions_label", "Deductions & Identified Risks:")}</b></color>");
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
                            ? (step.PenaltyDeducted > 0 ? "<color=#B45309>" : "<color=#15803D>")
                            : "<color=#B91C1C>";

                        sb.AppendLine($"{colorTag}{mark} {step.Title}</color> : <b>{step.NetScore:0.00} / {step.MaxPoints:0} pts</b> ({step.StatusText})");
                    }
                }

                _breakdownText.text = sb.ToString();
            }

            if (_syncStatusText != null)
            {
                string syncReady = loc.Get("assessment_sync_ready", "Prepared for Outbox Sync");
                _syncStatusText.text = _currentViewModel.SyncPrepared
                    ? $"<color=#15803D>● {syncReady} (Attempt: {_currentViewModel.ClientAttemptId?.Substring(0, Math.Min(8, _currentViewModel.ClientAttemptId.Length))}...)</color>"
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

        private void EnsureSummaryModal()
        {
            if (_modalRoot != null) return;

            var canvasObj = GameObject.Find("FireTrainingCanvas");
            var canvas = canvasObj != null ? canvasObj.GetComponent<Canvas>() : null;
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
                if (canvas == null)
                {
                    canvasObj = new GameObject("AssessmentCanvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 110;
                    var scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080, 1920);
                    scaler.matchWidthOrHeight = 0.5f;
                    canvasObj.AddComponent<GraphicRaycaster>();
                }
            }

            var defaultFont = UITheme.GetFont();

            // Modal Background Overlay (Light Card on Backdrop)
            _modalRoot = new GameObject("AssessmentSummaryModal");
            _modalRoot.transform.SetParent(canvas.transform, false);

            var rootRect = _modalRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.04f, 0.04f);
            rootRect.anchorMax = new Vector2(0.96f, 0.96f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var rootBg = _modalRoot.AddComponent<Image>();
            rootBg.color = UITheme.CardBackground;

            var outline = _modalRoot.AddComponent<Outline>();
            outline.effectColor = UITheme.BorderSubtle;
            outline.effectDistance = new Vector2(2, -2);

            // Title Banner
            var titleObj = new GameObject("ModalTitle");
            titleObj.transform.SetParent(_modalRoot.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.90f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            _titleText = titleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _titleText.font = defaultFont;
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.fontSize = 28;
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = UITheme.TextPrimary;

            // Score & Status Badge Card
            var badgeObj = new GameObject("ScoreBadgeCard");
            badgeObj.transform.SetParent(_modalRoot.transform, false);
            var badgeRect = badgeObj.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.06f, 0.74f);
            badgeRect.anchorMax = new Vector2(0.94f, 0.89f);
            badgeRect.offsetMin = Vector2.zero;
            badgeRect.offsetMax = Vector2.zero;

            _badgeBackground = badgeObj.AddComponent<Image>();
            _badgeBackground.color = UITheme.Success;

            var scoreTextObj = new GameObject("ScoreText");
            scoreTextObj.transform.SetParent(badgeObj.transform, false);
            var scoreRect = scoreTextObj.AddComponent<RectTransform>();
            scoreRect.anchorMin = Vector2.zero;
            scoreRect.anchorMax = Vector2.one;
            scoreRect.offsetMin = new Vector2(12, 4);
            scoreRect.offsetMax = new Vector2(-12, -4);

            _scoreBadgeText = scoreTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _scoreBadgeText.font = defaultFont;
            _scoreBadgeText.alignment = TextAlignmentOptions.Center;
            _scoreBadgeText.fontSize = 28;
            _scoreBadgeText.color = Color.white;

            // Metadata Row
            var metaObj = new GameObject("MetadataRow");
            metaObj.transform.SetParent(_modalRoot.transform, false);
            var metaRect = metaObj.AddComponent<RectTransform>();
            metaRect.anchorMin = new Vector2(0.06f, 0.68f);
            metaRect.anchorMax = new Vector2(0.94f, 0.73f);
            metaRect.offsetMin = Vector2.zero;
            metaRect.offsetMax = Vector2.zero;

            _metaText = metaObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _metaText.font = defaultFont;
            _metaText.alignment = TextAlignmentOptions.Center;
            _metaText.fontSize = 18;
            _metaText.color = UITheme.TextSecondary;

            // Safety Feedback Box (Light secondary card)
            var feedbackObj = new GameObject("SafetyFeedbackBox");
            feedbackObj.transform.SetParent(_modalRoot.transform, false);
            var feedbackRect = feedbackObj.AddComponent<RectTransform>();
            feedbackRect.anchorMin = new Vector2(0.06f, 0.44f);
            feedbackRect.anchorMax = new Vector2(0.94f, 0.67f);
            feedbackRect.offsetMin = Vector2.zero;
            feedbackRect.offsetMax = Vector2.zero;

            var feedbackBg = feedbackObj.AddComponent<Image>();
            feedbackBg.color = UITheme.CardSecondary;

            var fbOutline = feedbackObj.AddComponent<Outline>();
            fbOutline.effectColor = UITheme.BorderSubtle;
            fbOutline.effectDistance = new Vector2(1, -1);

            var feedbackTextObj = new GameObject("FeedbackText");
            feedbackTextObj.transform.SetParent(feedbackObj.transform, false);
            var fbTextRect = feedbackTextObj.AddComponent<RectTransform>();
            fbTextRect.anchorMin = Vector2.zero;
            fbTextRect.anchorMax = Vector2.one;
            fbTextRect.offsetMin = new Vector2(16, 8);
            fbTextRect.offsetMax = new Vector2(-16, -8);

            _safetyFeedbackText = feedbackTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _safetyFeedbackText.font = defaultFont;
            _safetyFeedbackText.alignment = TextAlignmentOptions.TopLeft;
            _safetyFeedbackText.fontSize = 18;
            _safetyFeedbackText.color = UITheme.TextPrimary;

            // Step Breakdown Container (collapsible / toggleable)
            _breakdownContainer = new GameObject("StepBreakdownContainer");
            _breakdownContainer.transform.SetParent(_modalRoot.transform, false);
            var bdRect = _breakdownContainer.AddComponent<RectTransform>();
            bdRect.anchorMin = new Vector2(0.06f, 0.22f);
            bdRect.anchorMax = new Vector2(0.94f, 0.43f);
            bdRect.offsetMin = Vector2.zero;
            bdRect.offsetMax = Vector2.zero;

            var bdBg = _breakdownContainer.AddComponent<Image>();
            bdBg.color = UITheme.CardSecondary;

            var bdOutline = _breakdownContainer.AddComponent<Outline>();
            bdOutline.effectColor = UITheme.BorderSubtle;
            bdOutline.effectDistance = new Vector2(1, -1);

            var bdTextObj = new GameObject("BreakdownText");
            bdTextObj.transform.SetParent(_breakdownContainer.transform, false);
            var bdTextRect = bdTextObj.AddComponent<RectTransform>();
            bdTextRect.anchorMin = Vector2.zero;
            bdTextRect.anchorMax = Vector2.one;
            bdTextRect.offsetMin = new Vector2(14, 6);
            bdTextRect.offsetMax = new Vector2(-14, -6);

            _breakdownText = bdTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _breakdownText.font = defaultFont;
            _breakdownText.alignment = TextAlignmentOptions.TopLeft;
            _breakdownText.fontSize = 17;
            _breakdownText.color = UITheme.TextPrimary;

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
            if (defaultFont != null) _syncStatusText.font = defaultFont;
            _syncStatusText.alignment = TextAlignmentOptions.Center;
            _syncStatusText.fontSize = 16;
            _syncStatusText.color = UITheme.TextSecondary;

            // Button 1: Review Performance (toggle breakdown)
            var btn1 = CreateButton("1. Review Performance", new Vector2(0.06f, 0.11f), new Vector2(0.34f, 0.16f), new Color(0.18f, 0.35f, 0.65f), () =>
            {
                OnReviewPerformanceClicked();
            });
            _reviewButtonText = btn1.GetComponentInChildren<TextMeshProUGUI>();

            // Button 2: Finish Session / Prepare Outbox Sync (Primary Orange)
            _finishButton = CreateButton("2. Finish / Prepare Sync", new Vector2(0.36f, 0.11f), new Vector2(0.64f, 0.16f), UITheme.PrimaryAction, () =>
            {
                OnFinishSessionClicked();
            });
            _finishButtonText = _finishButton.GetComponentInChildren<TextMeshProUGUI>();

            // Button 3: Retake Training (Amber)
            var btn3 = CreateButton("3. Retake Training", new Vector2(0.66f, 0.11f), new Vector2(0.94f, 0.16f), new Color(0.85f, 0.45f, 0.15f), () =>
            {
                OnRetakeTrainingClicked();
            });
            _retakeButtonText = btn3.GetComponentInChildren<TextMeshProUGUI>();

            // Button 4: Return to Home (Slate)
            var btn4 = CreateButton("← Return to Home Menu", new Vector2(0.15f, 0.04f), new Vector2(0.85f, 0.095f), new Color(0.25f, 0.30f, 0.38f), () =>
            {
                OnReturnToHomeClicked();
            });
            _returnHomeButtonText = btn4.GetComponentInChildren<TextMeshProUGUI>();
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
                        _finishButtonText.text = "Finalized for Sync";
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
                    _finishButtonText.text = "Finalized for Sync";
                }
                UpdateUIContents();
                Debug.Log($"[FireAssessmentSummaryUI] Finish Session (standalone): Attempt {_currentViewModel.ClientAttemptId} prepared for offline outbox sync.");
            }
        }

        /// <summary>
        /// Compatibility wrapper for Retake action used by tests and UI bindings.
        /// </summary>
        public void OnRetakeClicked()
        {
            OnRetakeTrainingClicked();
        }

        public void OnRetakeTrainingClicked()
        {
            HideSummary();
            _isSummaryRequested = false;
            _currentViewModel = null;
            if (_finishButton != null)
            {
                _finishButton.interactable = true;
            }
            if (_finishButtonText != null)
            {
                _finishButtonText.text = "2. Finish / Prepare Sync";
            }
            if (IndustrialSafetyAR.Core.Audio.FireAudioService.Instance != null)
            {
                IndustrialSafetyAR.Core.Audio.FireAudioService.Instance.StopEmergencyAlarm();
                IndustrialSafetyAR.Core.Audio.FireAudioService.Instance.StopAllAudio();
            }

            if (IndustrialSafetyAR.AR.ARModeController.Instance != null)
            {
                IndustrialSafetyAR.AR.ARModeController.Instance.EnableAR();
            }

            if (_controller != null)
            {
                _controller.RetakeTraining();
            }
        }

        public void OnReturnToHomeClicked()
        {
            HideSummary();
            _isSummaryRequested = false;
            _currentViewModel = null;

            if (IndustrialSafetyAR.Core.Audio.FireAudioService.Instance != null)
            {
                IndustrialSafetyAR.Core.Audio.FireAudioService.Instance.StopEmergencyAlarm();
                IndustrialSafetyAR.Core.Audio.FireAudioService.Instance.StopAllAudio();
            }

            if (IndustrialSafetyAR.AR.ARModeController.Instance != null)
            {
                IndustrialSafetyAR.AR.ARModeController.Instance.DisableAR();
            }

            var fireUI = FindAnyObjectByType<FireInteractionFeedbackUI>(FindObjectsInactive.Include);
            if (fireUI != null)
            {
                fireUI.HideTrainingUI();
            }

            if (_controller != null)
            {
                _controller.enabled = false;
            }

            if (WorkerHomeController.Instance != null)
            {
                WorkerHomeController.Instance.ReturnToHome();
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
            var defaultFont = FireInteractionFeedbackUI.GetDefaultFont();
            if (defaultFont != null) tmp.font = defaultFont;
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
