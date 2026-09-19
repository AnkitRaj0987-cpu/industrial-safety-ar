// FireInteractionFeedbackUI.cs
// Namespace : IndustrialSafetyAR.UI
//
// Worker-facing feedback and guided navigation UI for the Fire & Explosion Response module.
// Enforces explicit Step X/9 display, compact progress indicator (dots), explicit Action -> Success -> Next flow,
// large high-contrast primary Next button, Back button for reviewing completed steps, step locking,
// and phone-touch-friendly primary action buttons with TapGatedButton swipe rejection.

using System;
using IndustrialSafetyAR.Assessment;
using IndustrialSafetyAR.Modules.FireExplosion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IndustrialSafetyAR.UI
{
    /// <summary>
    /// Displays guided instructions, step progression, state feedback, and touch action buttons
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

        private TextMeshProUGUI _headerTitleText;
        private TextMeshProUGUI _stepBadgeText;
        private TextMeshProUGUI _progressText;
        private TextMeshProUGUI _feedbackText;
        private Canvas _canvas;
        private Image _bannerBg;
        private Image _feedbackBg;
        private GameObject _backButtonObj;
        private Button _backButton;
        private TextMeshProUGUI _backButtonText;
        private GameObject _actionContainer;
        private GameObject _optionsContainer;
        private GameObject _nextButtonObj;
        private Button _nextButton;
        private TextMeshProUGUI _nextButtonText;
        private GameObject _soundButtonObj;
        private Button _soundButton;
        private TextMeshProUGUI _soundButtonText;
        private GameObject _alarmButtonObj;
        private Button _alarmButton;
        private TextMeshProUGUI _alarmButtonText;

        private readonly GuidedStepNavigator _fallbackNavigator = new GuidedStepNavigator();
        private static TMP_FontAsset s_CachedFont;

        public Canvas Canvas => _canvas;

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

        public GuidedStepNavigator Navigator => _controller != null ? _controller.StepNavigator : _fallbackNavigator;

        public Button NextButton => _nextButton;
        public Button BackButton => _backButton;
        public Button SoundButton => _soundButton;
        public TextMeshProUGUI SoundButtonText => _soundButtonText;
        public Button AlarmButton => _alarmButton;
        public TextMeshProUGUI AlarmButtonText => _alarmButtonText;
        public bool IsNextButtonVisible => _nextButtonObj != null && _nextButtonObj.activeSelf;
        public bool IsBackButtonVisible => _backButtonObj != null && _backButtonObj.activeSelf;

        /// <summary>
        /// Retrieves the project default TMP font asset (LiberationSans SDF) from Resources.
        /// </summary>
        public static TMP_FontAsset GetDefaultFont()
        {
            if (s_CachedFont == null)
            {
                s_CachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
                    ?? TMP_Settings.defaultFontAsset;
            }
            return s_CachedFont;
        }

        private void Awake()
        {
            if (_promptText == null)
            {
                _promptText = GetComponent<TextMeshProUGUI>();
            }

            if (_controller == null)
            {
                _controller = FindAnyObjectByType<FireArInteractionController>(FindObjectsInactive.Include);
            }

            _canvas = GetOrCreateCanvas();
            EnsurePromptBanner();
            EnsureActionContainer();

            // Strictly hide training UI at startup if Worker Home is the current state
            if (WorkerHomeController.Instance == null || WorkerHomeController.Instance.CurrentState != WorkerHomeController.WorkerAppScreenState.TrainingFire)
            {
                HideTrainingUI();
            }
        }

        private Canvas GetOrCreateCanvas()
        {
            if (_canvas != null) return _canvas;

            var existingObj = GameObject.Find("FireTrainingCanvas");
            if (existingObj != null)
            {
                _canvas = existingObj.GetComponent<Canvas>();
                if (_canvas != null) return _canvas;
            }

            var canvasObj = new GameObject("FireTrainingCanvas");
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            return _canvas;
        }

        private void EnsurePromptBanner()
        {
            if (_promptText != null && _headerTitleText != null && _progressText != null && _backButton != null && _alarmButton != null) return;

            var canvas = GetOrCreateCanvas();
            if (canvas == null) return;

            var font = GetDefaultFont();

            // Main top card anchored inside mobile safe margins (compact ~260px banner, leaves AR view wide open)
            var bannerObj = new GameObject("FireTrainingHeaderBanner");
            bannerObj.transform.SetParent(canvas.transform, false);

            var rect = bannerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.850f);
            rect.anchorMax = new Vector2(0.96f, 0.985f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _bannerBg = bannerObj.AddComponent<Image>();
            _bannerBg.color = UITheme.CardBackground;

            var bannerOutline = bannerObj.AddComponent<Outline>();
            bannerOutline.effectColor = UITheme.BorderSubtle;
            bannerOutline.effectDistance = new Vector2(2, -2);

            // Top accent strip (Industrial Orange)
            var accentObj = new GameObject("TopAccentStrip");
            accentObj.transform.SetParent(bannerObj.transform, false);
            var accentRect = accentObj.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0.97f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.offsetMin = Vector2.zero;
            accentRect.offsetMax = Vector2.zero;
            var accentImg = accentObj.AddComponent<Image>();
            accentImg.color = UITheme.PrimaryAction;

            // Back button (top-left inside header)
            _backButtonObj = new GameObject("BackButton");
            _backButtonObj.transform.SetParent(bannerObj.transform, false);
            var backRect = _backButtonObj.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.02f, 0.74f);
            backRect.anchorMax = new Vector2(0.20f, 0.95f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;

            var backImg = _backButtonObj.AddComponent<Image>();
            backImg.color = UITheme.CardSecondary;
            _backButton = _backButtonObj.AddComponent<Button>();
            _backButton.targetGraphic = backImg;

            var backTapGated = _backButtonObj.AddComponent<TapGatedButton>();
            backTapGated.Initialize(() => OnBackButtonClicked());

            var backLabelObj = new GameObject("BackLabel");
            backLabelObj.transform.SetParent(_backButtonObj.transform, false);
            var backLabelRect = backLabelObj.AddComponent<RectTransform>();
            backLabelRect.anchorMin = Vector2.zero;
            backLabelRect.anchorMax = Vector2.one;
            backLabelRect.offsetMin = Vector2.zero;
            backLabelRect.offsetMax = Vector2.zero;
            _backButtonText = backLabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _backButtonText.font = font;
            _backButtonText.text = $"<b>{IndustrialSafetyAR.Core.LocaleService.Instance.Get("btn_back", "← BACK")}</b>";
            _backButtonText.fontSize = 17;
            _backButtonText.alignment = TextAlignmentOptions.Center;
            _backButtonText.color = UITheme.TextPrimary;

            _backButtonObj.SetActive(true);

            // Module Title Header
            var headerObj = new GameObject("HeaderTitle");
            headerObj.transform.SetParent(bannerObj.transform, false);
            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.21f, 0.74f);
            headerRect.anchorMax = new Vector2(0.60f, 0.95f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            _headerTitleText = headerObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _headerTitleText.font = font;
            _headerTitleText.text = "FIRE & EXPLOSION RESPONSE";
            _headerTitleText.fontSize = 18;
            _headerTitleText.enableAutoSizing = true;
            _headerTitleText.fontSizeMin = 13;
            _headerTitleText.fontSizeMax = 20;
            _headerTitleText.fontStyle = FontStyles.Bold;
            _headerTitleText.alignment = TextAlignmentOptions.Center;
            _headerTitleText.color = UITheme.TextPrimary;

            // Emergency Alarm Toggle Button (in header)
            _alarmButtonObj = new GameObject("AlarmButton");
            _alarmButtonObj.transform.SetParent(bannerObj.transform, false);
            var alarmRect = _alarmButtonObj.AddComponent<RectTransform>();
            alarmRect.anchorMin = new Vector2(0.61f, 0.74f);
            alarmRect.anchorMax = new Vector2(0.85f, 0.95f);
            alarmRect.offsetMin = Vector2.zero;
            alarmRect.offsetMax = Vector2.zero;

            var alarmImg = _alarmButtonObj.AddComponent<Image>();
            alarmImg.color = UITheme.CardSecondary;
            _alarmButton = _alarmButtonObj.AddComponent<Button>();
            _alarmButton.targetGraphic = alarmImg;

            var alarmTapGated = _alarmButtonObj.AddComponent<TapGatedButton>();
            alarmTapGated.Initialize(() => ToggleEmergencyAlarmEnabled());

            var alarmLabelObj = new GameObject("AlarmLabel");
            alarmLabelObj.transform.SetParent(_alarmButtonObj.transform, false);
            var alarmLabelRect = alarmLabelObj.AddComponent<RectTransform>();
            alarmLabelRect.anchorMin = Vector2.zero;
            alarmLabelRect.anchorMax = Vector2.one;
            alarmLabelRect.offsetMin = Vector2.zero;
            alarmLabelRect.offsetMax = Vector2.zero;
            _alarmButtonText = alarmLabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _alarmButtonText.font = font;
            _alarmButtonText.text = "<b>ALARM ON</b>";
            _alarmButtonText.fontSize = 14;
            _alarmButtonText.enableAutoSizing = true;
            _alarmButtonText.fontSizeMin = 11;
            _alarmButtonText.fontSizeMax = 16;
            _alarmButtonText.alignment = TextAlignmentOptions.Center;
            _alarmButtonText.color = UITheme.Danger;
            UpdateAlarmButtonVisual();

            // Sound Toggle Button (top-right inside header)
            _soundButtonObj = new GameObject("SoundButton");
            _soundButtonObj.transform.SetParent(bannerObj.transform, false);
            var soundRect = _soundButtonObj.AddComponent<RectTransform>();
            soundRect.anchorMin = new Vector2(0.86f, 0.74f);
            soundRect.anchorMax = new Vector2(0.98f, 0.95f);
            soundRect.offsetMin = Vector2.zero;
            soundRect.offsetMax = Vector2.zero;

            var soundImg = _soundButtonObj.AddComponent<Image>();
            soundImg.color = UITheme.CardSecondary;
            _soundButton = _soundButtonObj.AddComponent<Button>();
            _soundButton.targetGraphic = soundImg;

            var soundTapGated = _soundButtonObj.AddComponent<TapGatedButton>();
            soundTapGated.Initialize(() => ToggleSoundEnabled());

            var soundLabelObj = new GameObject("SoundLabel");
            soundLabelObj.transform.SetParent(_soundButtonObj.transform, false);
            var soundLabelRect = soundLabelObj.AddComponent<RectTransform>();
            soundLabelRect.anchorMin = Vector2.zero;
            soundLabelRect.anchorMax = Vector2.one;
            soundLabelRect.offsetMin = Vector2.zero;
            soundLabelRect.offsetMax = Vector2.zero;
            _soundButtonText = soundLabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _soundButtonText.font = font;
            _soundButtonText.text = "<b>SND</b>";
            _soundButtonText.fontSize = 15;
            _soundButtonText.alignment = TextAlignmentOptions.Center;
            _soundButtonText.color = UITheme.TextPrimary;
            UpdateSoundButtonVisual();

            // Step Badge (left side of row 2)
            var badgeObj = new GameObject("StepBadge");
            badgeObj.transform.SetParent(bannerObj.transform, false);
            var badgeRect = badgeObj.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.03f, 0.52f);
            badgeRect.anchorMax = new Vector2(0.60f, 0.72f);
            badgeRect.offsetMin = Vector2.zero;
            badgeRect.offsetMax = Vector2.zero;

            _stepBadgeText = badgeObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _stepBadgeText.font = font;
            _stepBadgeText.text = "<color=#EA580C><b>STEP 1/9</b></color> • DETECT HAZARD";
            _stepBadgeText.fontSize = 20;
            _stepBadgeText.fontStyle = FontStyles.Bold;
            _stepBadgeText.alignment = TextAlignmentOptions.MidlineLeft;
            _stepBadgeText.color = UITheme.TextPrimary;

            // Progress Indicator (right side of row 2)
            var progObj = new GameObject("ProgressIndicator");
            progObj.transform.SetParent(bannerObj.transform, false);
            var progRect = progObj.AddComponent<RectTransform>();
            progRect.anchorMin = new Vector2(0.62f, 0.52f);
            progRect.anchorMax = new Vector2(0.97f, 0.72f);
            progRect.offsetMin = Vector2.zero;
            progRect.offsetMax = Vector2.zero;

            _progressText = progObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _progressText.font = font;
            _progressText.text = "● ○ ○ ○ ○ ○ ○ ○ ○   1 / 9";
            _progressText.fontSize = 17;
            _progressText.alignment = TextAlignmentOptions.MidlineRight;
            _progressText.color = UITheme.TextSecondary;

            // Instruction Text (row 3)
            var textObj = new GameObject("PromptText");
            textObj.transform.SetParent(bannerObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.03f, 0.24f);
            textRect.anchorMax = new Vector2(0.97f, 0.50f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _promptText = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _promptText.font = font;
            _promptText.fontSize = 22;
            _promptText.enableAutoSizing = true;
            _promptText.fontSizeMin = 16;
            _promptText.fontSizeMax = 26;
            _promptText.alignment = TextAlignmentOptions.Center;
            _promptText.color = UITheme.TextPrimary;
            _promptText.text = "Initializing training module...";

            // State Feedback Sub-banner (row 4)
            var feedbackBoxObj = new GameObject("StateFeedbackBanner");
            feedbackBoxObj.transform.SetParent(bannerObj.transform, false);
            var fbBoxRect = feedbackBoxObj.AddComponent<RectTransform>();
            fbBoxRect.anchorMin = new Vector2(0.03f, 0.02f);
            fbBoxRect.anchorMax = new Vector2(0.97f, 0.22f);
            fbBoxRect.offsetMin = Vector2.zero;
            fbBoxRect.offsetMax = Vector2.zero;

            _feedbackBg = feedbackBoxObj.AddComponent<Image>();
            _feedbackBg.color = UITheme.CardSecondary;

            var feedbackTextObj = new GameObject("FeedbackText");
            feedbackTextObj.transform.SetParent(feedbackBoxObj.transform, false);
            var fbTextRect = feedbackTextObj.AddComponent<RectTransform>();
            fbTextRect.anchorMin = Vector2.zero;
            fbTextRect.anchorMax = Vector2.one;
            fbTextRect.offsetMin = new Vector2(10, 2);
            fbTextRect.offsetMax = new Vector2(-10, -2);

            _feedbackText = feedbackTextObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _feedbackText.font = font;
            _feedbackText.fontSize = 18;
            _feedbackText.enableAutoSizing = true;
            _feedbackText.fontSizeMin = 14;
            _feedbackText.fontSizeMax = 22;
            _feedbackText.alignment = TextAlignmentOptions.Center;
            _feedbackText.color = UITheme.TextSecondary;
            _feedbackText.text = "Scanning floor surfaces...";
        }

        private void EnsureActionContainer()
        {
            if (_actionContainer != null && _optionsContainer != null && _nextButtonObj != null) return;

            var canvas = GetOrCreateCanvas();
            if (canvas == null) return;

            // Compact bottom action container (height ~278px vs 460px)
            _actionContainer = new GameObject("TrainingActionContainer");
            _actionContainer.transform.SetParent(canvas.transform, false);

            var rect = _actionContainer.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.025f);
            rect.anchorMax = new Vector2(0.96f, 0.170f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Sub-container 1: Action Options
            _optionsContainer = new GameObject("OptionsContainer");
            _optionsContainer.transform.SetParent(_actionContainer.transform, false);
            var optRect = _optionsContainer.AddComponent<RectTransform>();
            optRect.anchorMin = Vector2.zero;
            optRect.anchorMax = Vector2.one;
            optRect.offsetMin = Vector2.zero;
            optRect.offsetMax = Vector2.zero;

            // Sub-container 2: Primary Next Button
            _nextButtonObj = new GameObject("PrimaryNextButton");
            _nextButtonObj.transform.SetParent(_actionContainer.transform, false);
            var nextRect = _nextButtonObj.AddComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(0f, 0.02f);
            nextRect.anchorMax = new Vector2(1f, 0.42f);
            nextRect.offsetMin = Vector2.zero;
            nextRect.offsetMax = Vector2.zero;

            var nextImg = _nextButtonObj.AddComponent<Image>();
            nextImg.color = UITheme.PrimaryAction; // Primary Orange

            _nextButton = _nextButtonObj.AddComponent<Button>();
            _nextButton.targetGraphic = nextImg;

            var nextTapGated = _nextButtonObj.AddComponent<TapGatedButton>();
            nextTapGated.Initialize(() => OnNextButtonClicked());

            var nextTextObj = new GameObject("NextLabel");
            nextTextObj.transform.SetParent(_nextButtonObj.transform, false);
            var nextTextRect = nextTextObj.AddComponent<RectTransform>();
            nextTextRect.anchorMin = Vector2.zero;
            nextTextRect.anchorMax = Vector2.one;
            nextTextRect.offsetMin = new Vector2(12, 4);
            nextTextRect.offsetMax = new Vector2(-12, -4);

            _nextButtonText = nextTextObj.AddComponent<TextMeshProUGUI>();
            var font = UITheme.GetFont();
            if (font != null) _nextButtonText.font = font;
            _nextButtonText.text = "NEXT STEP →";
            _nextButtonText.alignment = TextAlignmentOptions.Center;
            _nextButtonText.fontSize = 24;
            _nextButtonText.enableAutoSizing = true;
            _nextButtonText.fontSizeMin = 16;
            _nextButtonText.fontSizeMax = 28;
            _nextButtonText.fontStyle = FontStyles.Bold;
            _nextButtonText.color = Color.white;

            _nextButtonObj.SetActive(false);
        }

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
                _controller.OnFeedbackChanged += HandleFeedbackChanged;
                _controller.OnStateChanged += HandleStateChanged;
            }

            Navigator.OnStepChanged += HandleNavigatorStepChanged;
            Navigator.OnStepCompleted += HandleNavigatorStepCompleted;
            Navigator.OnCompleteTrainingRequested += HandleCompleteTrainingRequested;
            Navigator.OnFeedbackChanged += HandleFeedbackChanged;

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
                _controller.OnFeedbackChanged -= HandleFeedbackChanged;
                _controller.OnStateChanged -= HandleStateChanged;
            }

            Navigator.OnStepChanged -= HandleNavigatorStepChanged;
            Navigator.OnStepCompleted -= HandleNavigatorStepCompleted;
            Navigator.OnCompleteTrainingRequested -= HandleCompleteTrainingRequested;
            Navigator.OnFeedbackChanged -= HandleFeedbackChanged;

            if (IndustrialSafetyAR.Core.LocaleService.Instance != null)
            {
                IndustrialSafetyAR.Core.LocaleService.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void HandleLanguageChanged(string newLang)
        {
            RefreshUI();
        }

        private void Start()
        {
            if (_successBadge != null)
            {
                _successBadge.SetActive(false);
            }

            if (WorkerHomeController.Instance == null || WorkerHomeController.Instance.CurrentState != WorkerHomeController.WorkerAppScreenState.TrainingFire)
            {
                HideTrainingUI();
                return;
            }

            if (_controller != null)
            {
                HandleStateChanged(_controller.State);
            }
            else
            {
                RefreshUI();
            }
        }

        public void ShowTrainingUI()
        {
            EnsurePromptBanner();
            EnsureActionContainer();
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(true);
            }
            gameObject.SetActive(true);
            RefreshUI();
        }

        public void HideTrainingUI()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
            }
            ClearOptions();
        }

        public void ToggleSoundEnabled()
        {
            var audio = IndustrialSafetyAR.Core.Audio.FireAudioService.Instance;
            if (audio != null)
            {
                audio.IsSoundEnabled = !audio.IsSoundEnabled;
                UpdateSoundButtonVisual();
                var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
                string fb = audio.IsSoundEnabled ? loc.Get("feedback_sound_enabled", "Sound Alerts: Enabled") : loc.Get("feedback_sound_muted", "Sound Alerts: Muted");
                SetFeedback(fb);
            }
        }

        public void UpdateSoundButtonVisual()
        {
            if (_soundButtonText != null)
            {
                bool enabled = IndustrialSafetyAR.Core.Audio.FireAudioService.Instance == null ||
                               IndustrialSafetyAR.Core.Audio.FireAudioService.Instance.IsSoundEnabled;
                _soundButtonText.text = enabled ? "<b>SND</b>" : "<b>MUTE</b>";
            }
        }

        public void ToggleEmergencyAlarmEnabled()
        {
            var audio = IndustrialSafetyAR.Core.Audio.FireAudioService.Instance;
            if (audio != null)
            {
                audio.IsEmergencyAlarmEnabled = !audio.IsEmergencyAlarmEnabled;
                UpdateAlarmButtonVisual();
                var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
                string fb = audio.IsEmergencyAlarmEnabled ? loc.Get("feedback_alarm_enabled", "Emergency Alarm: Enabled") : loc.Get("feedback_alarm_muted", "Emergency Alarm: Muted");
                SetFeedback(fb);
            }
        }

        public void UpdateAlarmButtonVisual()
        {
            if (_alarmButtonText != null)
            {
                var audio = IndustrialSafetyAR.Core.Audio.FireAudioService.Instance;
                bool enabled = audio == null || audio.IsEmergencyAlarmEnabled;
                var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
                string text = enabled ? loc.Get("btn_alarm_on", "ALARM ON") : loc.Get("btn_alarm_off", "ALARM OFF");
                _alarmButtonText.text = $"<b>{text}</b>";
                _alarmButtonText.color = enabled ? new Color(1f, 0.5f, 0.5f) : new Color(0.7f, 0.7f, 0.7f);
            }
        }

        public void OnNextButtonClicked()
        {
            Navigator.GoNext();
        }

        public void OnBackButtonClicked()
        {
            if (Navigator.CanGoBack)
            {
                Navigator.GoBack();
            }
            else
            {
                ExitTrainingToHome();
            }
        }

        public void ExitTrainingToHome()
        {
            if (IndustrialSafetyAR.Core.Audio.FireAudioService.Instance != null)
            {
                IndustrialSafetyAR.Core.Audio.FireAudioService.Instance.StopEmergencyAlarm();
                IndustrialSafetyAR.Core.Audio.FireAudioService.Instance.StopAllAudio();
            }

            HideTrainingUI();

            if (_controller != null)
            {
                _controller.enabled = false;
            }

            if (WorkerHomeController.Instance != null)
            {
                WorkerHomeController.Instance.ReturnToHome();
            }
        }

        private void HandleNavigatorStepChanged(int stepIndex)
        {
            RefreshUI();
        }

        private void HandleNavigatorStepCompleted(int stepIndex, string feedback)
        {
            RefreshUI();
        }

        private void HandleCompleteTrainingRequested()
        {
            var summaryUI = GetComponent<FireAssessmentSummaryUI>() ?? FindAnyObjectByType<FireAssessmentSummaryUI>();
            if (summaryUI != null)
            {
                var vm = AssessmentSummaryViewModel.Build(_controller?.LatestAttempt, _controller?.LatestAssessment);
                summaryUI.ShowSummary(vm);
            }
        }

        private void HandleFeedbackChanged(string feedback)
        {
            if (string.IsNullOrEmpty(feedback)) return;

            bool isError = feedback.StartsWith("✗") || feedback.StartsWith("DANGER") || feedback.StartsWith("INCORRECT") || feedback.StartsWith("Unsafe");
            bool isSuccess = feedback.StartsWith("✓") || feedback.StartsWith("CORRECT");

            if (isError)
            {
                SetFeedback(feedback, UITheme.Danger, new Color(1f, 0.92f, 0.92f));
            }
            else if (isSuccess)
            {
                SetFeedback(feedback, UITheme.Success, new Color(0.92f, 0.98f, 0.94f));
            }
            else
            {
                SetFeedback(feedback, UITheme.TextPrimary, UITheme.CardSecondary);
            }
        }

        public void SetFeedback(string message, Color? textColor = null, Color? bgColor = null)
        {
            if (_feedbackText != null)
            {
                _feedbackText.text = message;
                _feedbackText.color = textColor ?? UITheme.TextPrimary;
            }
            if (_feedbackBg != null)
            {
                _feedbackBg.color = bgColor ?? UITheme.CardSecondary;
            }
        }

        private void HandleStateChanged(FireInteractionState state)
        {
            var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
            if (state == FireInteractionState.WaitingForTracking)
            {
                ClearOptions();
                if (_backButtonObj != null) _backButtonObj.SetActive(false);
                if (_nextButtonObj != null) _nextButtonObj.SetActive(false);
                if (_stepBadgeText != null) _stepBadgeText.text = $"<color=#E67E22><b>{loc.Get("ar_calibration_badge", "AR CALIBRATION")}</b></color>";
                if (_progressText != null) _progressText.text = "";
                if (_promptText != null) _promptText.text = loc.Get("ar_calibration_prompt", "Searching for surfaces... Move phone slowly.");
                SetFeedback(loc.Get("ar_calibration_feedback", "Keep camera pointed toward textured floor."));
                return;
            }

            if (state == FireInteractionState.ReadyToPlace)
            {
                ClearOptions();
                if (_backButtonObj != null) _backButtonObj.SetActive(false);
                if (_nextButtonObj != null) _nextButtonObj.SetActive(false);
                if (_stepBadgeText != null) _stepBadgeText.text = $"<color=#2ECC71><b>{loc.Get("surface_detected_badge", "SURFACE DETECTED")}</b></color>";
                if (_progressText != null) _progressText.text = "";
                if (_promptText != null) _promptText.text = loc.Get("surface_detected_prompt", "Surface detected! Tap anywhere on the floor to place the Fire Hazard.");
                SetFeedback(loc.Get("surface_detected_feedback", "Tap detected floor surface to spawn training scenario."));

                CreateOptionButton(loc.Get("btn_place_hazard", "PLACE FIRE HAZARD HERE →"), new Vector2(0f, 0.15f), new Vector2(1f, 0.85f),
                    new Color(0.18f, 0.55f, 0.30f, 0.96f), () =>
                {
                    _controller?.HandleTap(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
                });
                return;
            }

            RefreshUI();
        }

        /// <summary>
        /// Updates all UI components to reflect the Navigator's active step state.
        /// </summary>
        public void RefreshUI()
        {
            EnsurePromptBanner();
            EnsureActionContainer();

            var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
            int step = Navigator.CurrentStepIndex;
            string stepTitle = Navigator.GetStepTitle(step);
            string instruction = Navigator.GetStepInstruction(step);
            bool isCompleted = Navigator.IsStepCompleted(step);
            bool inReviewMode = isCompleted && (step < Navigator.HighestCompletedStep);

            // 1. Module Header Title & Buttons
            if (_headerTitleText != null)
            {
                _headerTitleText.text = loc.Get("fire_header_title", "FIRE & EXPLOSION RESPONSE");
            }
            if (_backButtonText != null)
            {
                _backButtonText.text = $"<b>{loc.Get("btn_back", "← BACK")}</b>";
            }
            UpdateSoundButtonVisual();
            UpdateAlarmButtonVisual();

            // 2. Step Badge (e.g. STEP 3/9 • RAISE ALARM)
            if (_stepBadgeText != null)
            {
                string badgeFmt = loc.Get("step_badge_format", "STEP {0}/{1}");
                string badgePrefix = string.Format(badgeFmt, step, Navigator.TotalSteps);
                _stepBadgeText.text = $"<color=#5DADE2><b>{badgePrefix}</b></color> • {stepTitle}";
            }

            // 3. Compact Progress Indicator (Dots)
            if (_progressText != null)
            {
                _progressText.text = Navigator.FormatProgressIndicator();
            }

            // 4. Clear Step Instruction
            if (_promptText != null)
            {
                _promptText.text = instruction;
                _promptText.color = UITheme.TextPrimary;
            }

            // 5. Back Button (always active during training: returns Home on Step 1, reviews completed steps on Steps 2-9)
            if (_backButtonObj != null)
            {
                _backButtonObj.SetActive(true);
            }

            // 6. Next Button & Options Container Dynamic Sizing
            if (_nextButtonObj != null)
            {
                _nextButtonObj.SetActive(isCompleted);
                if (_nextButtonText != null)
                {
                    _nextButtonText.text = Navigator.GetNextLabel(step);
                }
            }

            if (_optionsContainer != null)
            {
                var optRect = _optionsContainer.GetComponent<RectTransform>();
                if (optRect != null)
                {
                    if (isCompleted)
                    {
                        optRect.anchorMin = new Vector2(0f, 0.46f);
                        optRect.anchorMax = new Vector2(1f, 1f);
                    }
                    else
                    {
                        optRect.anchorMin = Vector2.zero;
                        optRect.anchorMax = Vector2.one;
                    }
                    optRect.offsetMin = Vector2.zero;
                    optRect.offsetMax = Vector2.zero;
                }
            }

            // 7. Dynamic Feedback
            if (isCompleted)
            {
                string successFb = Navigator.GetSuccessFeedback(step) ?? loc.Get("default_success_feedback", "✓ Action completed successfully.");
                SetFeedback(successFb, UITheme.Success, new Color(0.92f, 0.98f, 0.94f));
            }
            else
            {
                string actionReqFmt = loc.Get("action_required_format", "Action required for Step {0}: {1}");
                SetFeedback(string.Format(actionReqFmt, step, stepTitle), UITheme.TextPrimary, UITheme.CardSecondary);
            }

            // 8. Action Options
            ClearOptions();

            if (inReviewMode)
            {
                ShowReviewModeUI(step);
            }
            else if (isCompleted)
            {
                ShowStepCompletedUI(step);
            }
            else
            {
                ShowStepActiveUI(step);
            }
        }

        private void ClearOptions()
        {
            if (_optionsContainer == null) return;

            for (int i = _optionsContainer.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_optionsContainer.transform.GetChild(i).gameObject);
            }
        }

        private void ShowReviewModeUI(int step)
        {
            var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
            string reviewFmt = loc.Get("step_review_format", "✓ Step {0} Completed [Review Mode]");
            CreateStatusBadge(string.Format(reviewFmt, step), new Vector2(0f, 0.15f), new Vector2(1f, 0.85f),
                new Color(0.12f, 0.35f, 0.22f, 0.95f));
        }

        private void ShowStepCompletedUI(int step)
        {
            var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
            string completedText = step == 9
                ? loc.Get("training_completed_notice", "✓ Training Completed! Tap View Assessment Below")
                : string.Format(loc.Get("step_completed_format", "✓ Step {0} Completed! Tap Next Below"), step);

            CreateStatusBadge(completedText, new Vector2(0f, 0.15f), new Vector2(1f, 0.85f),
                new Color(0.12f, 0.45f, 0.25f, 0.95f));
        }

        private void ShowStepActiveUI(int step)
        {
            switch (step)
            {
                case 1:
                    ShowDetectHazardOptions();
                    break;
                case 2:
                    ShowIdentifyHazardOptions();
                    break;
                case 3:
                    ShowRaiseAlarmOptions();
                    break;
                case 4:
                    ShowSelectExtinguisherOptions();
                    break;
                case 5:
                    ShowSafeDistanceOptions();
                    break;
                case 6:
                    ShowPassProcedureOptions();
                    break;
                case 7:
                    ShowEmergencyExitOptions();
                    break;
                case 8:
                    ShowEvacuationRouteOptions();
                    break;
                case 9:
                    ShowAssemblyPointOptions();
                    break;
            }
        }

        // ====================================================================
        // Step 1: Detect / Acknowledge Hazard
        // ====================================================================
        private void ShowDetectHazardOptions()
        {
            var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
            CreateOptionButton(loc.Get("fire_step1_btn_ack", "ACKNOWLEDGE FIRE HAZARD →"), new Vector2(0f, 0.15f), new Vector2(1f, 0.85f),
                new Color(0.15f, 0.65f, 0.35f), () =>
            {
                _controller?.ConfirmHazardDetected();
            });
        }

        // ====================================================================
        // Step 2: Identify Hazard Classification
        // ====================================================================
        private void ShowIdentifyHazardOptions()
        {
            var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
            CreateOptionButton(loc.Get("fire_step2_opt1", "1. CLASS E: ELECTRICAL FIRE (480V) →"), new Vector2(0f, 0.68f), new Vector2(1f, 0.98f),
                new Color(0.14f, 0.24f, 0.38f, 0.96f), () =>
            {
                _controller?.SubmitHazardIdentification(FireTrainingWorkflow.TargetElectricalConveyorFire);
            });

            CreateOptionButton(loc.Get("fire_step2_opt2", "2. Class A: Ordinary Combustible Material"), new Vector2(0f, 0.35f), new Vector2(1f, 0.65f),
                new Color(0.14f, 0.18f, 0.26f, 0.96f), () =>
            {
                _controller?.SubmitHazardIdentification("hazard_combustible_debris");
                SetFeedback(loc.Get("fire_step2_err_classA", "✗ Incorrect: Conveyor is electrical machinery. Select Class E."), new Color(1f, 0.45f, 0.45f), new Color(0.35f, 0.12f, 0.12f, 0.95f));
            });

            CreateOptionButton(loc.Get("fire_step2_opt3", "3. Class B: Flammable Chemical / Solvent"), new Vector2(0f, 0.02f), new Vector2(1f, 0.32f),
                new Color(0.14f, 0.18f, 0.26f, 0.96f), () =>
            {
                _controller?.SubmitHazardIdentification("hazard_flammable_liquid");
                SetFeedback(loc.Get("fire_step2_err_classB", "✗ Incorrect: Class B is for flammable liquids. Select Class E."), new Color(1f, 0.45f, 0.45f), new Color(0.35f, 0.12f, 0.12f, 0.95f));
            });
        }

        // ====================================================================
        // Step 3: Raise Alarm (MCP)
        // ====================================================================
        private void ShowRaiseAlarmOptions()
        {
            var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
            CreateOptionButton(loc.Get("fire_step3_btn_alarm", "ACTIVATE MANUAL CALL POINT (ALARM) →"), new Vector2(0f, 0.15f), new Vector2(1f, 0.85f),
                new Color(0.85f, 0.18f, 0.18f), () =>
            {
                _controller?.SubmitRaiseAlarm(FireTrainingWorkflow.ActionRaiseAlarm);
            });
        }

        // ====================================================================
        // Step 4: Select Extinguisher
        // ====================================================================
        private void ShowSelectExtinguisherOptions()
        {
            var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
            CreateOptionButton(loc.Get("fire_step4_opt1", "1. CO2 EXTINGUISHER (CARBON DIOXIDE) →"), new Vector2(0f, 0.68f), new Vector2(1f, 0.98f),
                new Color(0.14f, 0.24f, 0.38f, 0.96f), () =>
            {
                _controller?.SubmitExtinguisherSelection(FireTrainingWorkflow.TargetExtinguisherCO2);
            });

            CreateOptionButton(loc.Get("fire_step4_opt2", "2. Water Extinguisher (H2O) [Electrical Shock Hazard]"), new Vector2(0f, 0.35f), new Vector2(1f, 0.65f),
                new Color(0.45f, 0.15f, 0.15f, 0.92f), () =>
            {
                _controller?.SubmitExtinguisherSelection(FireTrainingWorkflow.TargetExtinguisherWater);
                SetFeedback(loc.Get("fire_step4_err_water", "✗ Incorrect extinguisher! Water conducts electricity. Select CO2."), new Color(1f, 0.45f, 0.45f), new Color(0.35f, 0.12f, 0.12f, 0.95f));
            });

            CreateOptionButton(loc.Get("fire_step4_opt3", "3. Foam Extinguisher (AFFF) [Conductive Risk]"), new Vector2(0f, 0.02f), new Vector2(1f, 0.32f),
                new Color(0.45f, 0.15f, 0.15f, 0.92f), () =>
            {
                _controller?.SubmitExtinguisherSelection(FireTrainingWorkflow.TargetExtinguisherFoam);
                SetFeedback(loc.Get("fire_step4_err_foam", "✗ Incorrect extinguisher! Foam is water-based. Select CO2."), new Color(1f, 0.45f, 0.45f), new Color(0.35f, 0.12f, 0.12f, 0.95f));
            });
        }

        // ====================================================================
        // Step 5: Safe Distance Decision
        // ====================================================================
        private void ShowSafeDistanceOptions()
        {
            var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
            CreateOptionButton(loc.Get("fire_step5_opt1", "STAND AT SAFE DISTANCE (2.5m) →"), new Vector2(0f, 0.52f), new Vector2(1f, 0.96f),
                new Color(0.15f, 0.65f, 0.35f), () =>
            {
                _controller?.SubmitDistanceDecision(2.5f);
            });

            CreateOptionButton(loc.Get("fire_step5_opt2", "Approach Fire (1.2m - Danger Zone)"), new Vector2(0f, 0.04f), new Vector2(1f, 0.48f),
                new Color(0.45f, 0.15f, 0.15f, 0.92f), () =>
            {
                _controller?.SubmitDistanceDecision(1.2f);
                SetFeedback(loc.Get("fire_step5_err_danger", "✗ Unsafe distance! You entered the 2m danger zone. Move back."), new Color(1f, 0.45f, 0.45f), new Color(0.35f, 0.12f, 0.12f, 0.95f));
            });
        }

        // ====================================================================
        // Step 6: PASS Procedure (PULL, AIM, SQUEEZE, SWEEP)
        // ====================================================================
        private void ShowPassProcedureOptions()
        {
            var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
            var stage = _controller != null ? _controller.WorkflowStage : FireWorkflowStage.SafeDistanceMaintained;

            if (stage == FireWorkflowStage.SafeDistanceMaintained)
            {
                CreateOptionButton(loc.Get("fire_step6_btn_pull", "1. PULL SAFETY PIN →"), new Vector2(0f, 0.15f), new Vector2(1f, 0.85f),
                    new Color(0.18f, 0.35f, 0.60f), () =>
                {
                    _controller?.SubmitPullPin();
                    SetFeedback(loc.Get("fire_step6_fb_pin", "✓ Pin pulled! Extinguisher unlocked. Next: Aim nozzle."), new Color(0.4f, 1f, 0.5f), new Color(0.1f, 0.28f, 0.15f, 0.95f));
                });
            }
            else if (stage == FireWorkflowStage.PinPulled)
            {
                CreateOptionButton(loc.Get("fire_step6_btn_aim", "2. AIM NOZZLE AT BASE OF FIRE →"), new Vector2(0f, 0.15f), new Vector2(1f, 0.85f),
                    new Color(0.18f, 0.35f, 0.60f), () =>
                {
                    _controller?.SubmitAim();
                    SetFeedback(loc.Get("fire_step6_fb_aim", "✓ Nozzle aimed at fire base. Next: Squeeze handle."), new Color(0.4f, 1f, 0.5f), new Color(0.1f, 0.28f, 0.15f, 0.95f));
                });
            }
            else if (stage == FireWorkflowStage.AimConfirmed)
            {
                CreateOptionButton(loc.Get("fire_step6_btn_squeeze", "3. SQUEEZE OPERATING LEVER →"), new Vector2(0f, 0.15f), new Vector2(1f, 0.85f),
                    new Color(0.70f, 0.35f, 0.15f), () =>
                {
                    _controller?.SubmitSqueeze();
                    SetFeedback(loc.Get("fire_step6_fb_squeeze", "✓ CO2 gas discharging! Next: Sweep across fire base."), new Color(0.4f, 1f, 0.5f), new Color(0.1f, 0.28f, 0.15f, 0.95f));
                });
            }
            else if (stage == FireWorkflowStage.HandleSqueezed)
            {
                CreateOptionButton(loc.Get("fire_step6_btn_sweep", "4. SWEEP NOZZLE SIDE TO SIDE →"), new Vector2(0f, 0.15f), new Vector2(1f, 0.85f),
                    new Color(0.15f, 0.65f, 0.35f), () =>
                {
                    _controller?.SubmitSweep();
                });
            }
            else
            {
                ShowStepCompletedUI(6);
            }
        }

        // ====================================================================
        // Step 7: Emergency Exit Identification
        // ====================================================================
        private void ShowEmergencyExitOptions()
        {
            var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
            CreateOptionButton(loc.Get("fire_step7_opt1", "SECTOR B EMERGENCY EXIT →"), new Vector2(0f, 0.68f), new Vector2(1f, 0.98f),
                new Color(0.14f, 0.24f, 0.38f, 0.96f), () =>
            {
                _controller?.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitEmergencySectorB);
            });

            CreateOptionButton(loc.Get("fire_step7_opt2", "Freight Elevator (DO NOT USE IN FIRE)"), new Vector2(0f, 0.35f), new Vector2(1f, 0.65f),
                new Color(0.45f, 0.15f, 0.15f, 0.92f), () =>
            {
                _controller?.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitFreightElevator);
                SetFeedback(loc.Get("fire_step7_err_elevator", "✗ Unsafe! Never use elevators during fire evacuation. Select Sector B."), new Color(1f, 0.45f, 0.45f), new Color(0.35f, 0.12f, 0.12f, 0.95f));
            });

            CreateOptionButton(loc.Get("fire_step7_opt3", "Sector A Route (Smoke Blocked - Unsafe)"), new Vector2(0f, 0.02f), new Vector2(1f, 0.32f),
                new Color(0.45f, 0.15f, 0.15f, 0.92f), () =>
            {
                _controller?.SubmitIdentifyExit(FireTrainingWorkflow.TargetExitBlockedCorridor);
                SetFeedback(loc.Get("fire_step7_err_blocked", "✗ Unsafe! Sector A corridor is blocked by toxic smoke. Select Sector B."), new Color(1f, 0.45f, 0.45f), new Color(0.35f, 0.12f, 0.12f, 0.95f));
            });
        }

        // ====================================================================
        // Step 8: Evacuation Route Waypoints (Strict 1 -> 2 -> 3 Guided Flow)
        // ====================================================================
        private void ShowEvacuationRouteOptions()
        {
            var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
            var stage = _controller != null ? _controller.WorkflowStage : FireWorkflowStage.ExitIdentified;

            if (stage == FireWorkflowStage.ExitIdentified || stage == FireWorkflowStage.AwaitingEvacuationRoute)
            {
                CreateOptionButton(loc.Get("fire_step8_wp1", "WAYPOINT 1: MAIN CORRIDOR →"), new Vector2(0f, 0.52f), new Vector2(1f, 0.96f),
                    new Color(0.15f, 0.65f, 0.35f), () =>
                {
                    _controller?.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointMainCorridor);
                    SetFeedback(loc.Get("fire_step8_fb_wp1", "✓ Waypoint 1 reached! Proceed through Bypass Crosscut."), new Color(0.4f, 1f, 0.5f), new Color(0.1f, 0.28f, 0.15f, 0.95f));
                });

                CreateOptionButton(loc.Get("fire_step8_unsafe_smoke", "⚠ Sector A Smoke Corridor (Unsafe)"), new Vector2(0f, 0.04f), new Vector2(1f, 0.48f),
                    new Color(0.45f, 0.15f, 0.15f, 0.92f), () =>
                {
                    _controller?.SubmitEvacuationWaypoint(FireTrainingWorkflow.HazardSmokeCorridor);
                    SetFeedback(loc.Get("fire_step8_err_smoke", "✗ Toxic smoke hazard! Do not enter Sector A. Use Main Corridor."), new Color(1f, 0.45f, 0.45f), new Color(0.35f, 0.12f, 0.12f, 0.95f));
                });
            }
            else if (stage == FireWorkflowStage.WaypointMainCorridorReached)
            {
                CreateOptionButton(loc.Get("fire_step8_wp2", "WAYPOINT 2: BYPASS CROSSCUT →"), new Vector2(0f, 0.52f), new Vector2(1f, 0.96f),
                    new Color(0.15f, 0.65f, 0.35f), () =>
                {
                    _controller?.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointBypassCrosscut);
                    SetFeedback(loc.Get("fire_step8_fb_wp2", "✓ Waypoint 2 reached! Proceed to Fire Door Exit."), new Color(0.4f, 1f, 0.5f), new Color(0.1f, 0.28f, 0.15f, 0.95f));
                });

                CreateOptionButton(loc.Get("fire_step8_unsafe_smoke", "⚠ Sector A Smoke Corridor (Unsafe)"), new Vector2(0f, 0.04f), new Vector2(1f, 0.48f),
                    new Color(0.45f, 0.15f, 0.15f, 0.92f), () =>
                {
                    _controller?.SubmitEvacuationWaypoint(FireTrainingWorkflow.HazardSmokeCorridor);
                    SetFeedback(loc.Get("fire_step8_err_smoke", "✗ Toxic smoke hazard! Do not enter Sector A. Use Bypass Crosscut."), new Color(1f, 0.45f, 0.45f), new Color(0.35f, 0.12f, 0.12f, 0.95f));
                });
            }
            else if (stage == FireWorkflowStage.WaypointBypassCrosscutReached)
            {
                CreateOptionButton(loc.Get("fire_step8_wp3", "WAYPOINT 3: FIRE DOOR EXIT →"), new Vector2(0f, 0.52f), new Vector2(1f, 0.96f),
                    new Color(0.15f, 0.65f, 0.35f), () =>
                {
                    _controller?.SubmitEvacuationWaypoint(FireTrainingWorkflow.WaypointFireDoorExit);
                    SetFeedback(loc.Get("fire_step8_fb_wp3", "✓ Waypoint 3 reached! Route cleared safely."), new Color(0.4f, 1f, 0.5f), new Color(0.1f, 0.28f, 0.15f, 0.95f));
                });

                CreateOptionButton(loc.Get("fire_step8_unsafe_smoke", "⚠ Sector A Smoke Corridor (Unsafe)"), new Vector2(0f, 0.04f), new Vector2(1f, 0.48f),
                    new Color(0.45f, 0.15f, 0.15f, 0.92f), () =>
                {
                    _controller?.SubmitEvacuationWaypoint(FireTrainingWorkflow.HazardSmokeCorridor);
                    SetFeedback(loc.Get("fire_step8_err_smoke", "✗ Toxic smoke hazard! Do not enter Sector A. Use Fire Door Exit."), new Color(1f, 0.45f, 0.45f), new Color(0.35f, 0.12f, 0.12f, 0.95f));
                });
            }
            else
            {
                ShowStepCompletedUI(8);
            }
        }

        // ====================================================================
        // Step 9: Reach Assembly Point
        // ====================================================================
        private void ShowAssemblyPointOptions()
        {
            var loc = IndustrialSafetyAR.Core.LocaleService.Instance;
            CreateOptionButton(loc.Get("fire_step9_opt1", "REACH ASSEMBLY POINT (MUSTER ALPHA) →"), new Vector2(0f, 0.52f), new Vector2(1f, 0.96f),
                new Color(0.15f, 0.65f, 0.35f), () =>
            {
                _controller?.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyMusterPoint);
            });

            CreateOptionButton(loc.Get("fire_step9_opt2", "Perimeter Loading Gate (Unauthorized Area)"), new Vector2(0f, 0.04f), new Vector2(1f, 0.48f),
                new Color(0.45f, 0.15f, 0.15f, 0.92f), () =>
            {
                _controller?.SubmitReachAssemblyPoint(FireTrainingWorkflow.TargetAssemblyPointBeta);
                SetFeedback(loc.Get("fire_step9_err_downwind", "✗ Loading Gate is not an assembly area! Proceed to Muster Point Alpha."), new Color(1f, 0.45f, 0.45f), new Color(0.35f, 0.12f, 0.12f, 0.95f));
            });
        }

        private GameObject CreateStatusBadge(string text, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            var badgeObj = new GameObject("StatusBadge");
            badgeObj.transform.SetParent(_optionsContainer.transform, false);

            var rect = badgeObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(4, 2);
            rect.offsetMax = new Vector2(-4, -2);

            var img = badgeObj.AddComponent<Image>();
            img.color = bgColor;

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(badgeObj.transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12, 4);
            labelRect.offsetMax = new Vector2(-12, -4);

            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            var font = GetDefaultFont();
            if (font != null) tmp.font = font;
            tmp.text = $"<b>{text}</b>";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 20;
            tmp.color = Color.white;

            return badgeObj;
        }

        private Button CreateOptionButton(string label, Vector2 anchorMin, Vector2 anchorMax, Color bgColor, Action onClick)
        {
            var btnObj = new GameObject("OptionBtn_" + label);
            btnObj.transform.SetParent(_optionsContainer.transform, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(4, 2);
            rect.offsetMax = new Vector2(-4, -2);

            var img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            var tapGated = btnObj.AddComponent<TapGatedButton>();
            tapGated.Initialize(() => onClick?.Invoke());

            var textObj = new GameObject("Label");
            textObj.transform.SetParent(btnObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 4);
            textRect.offsetMax = new Vector2(-12, -4);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            var font = GetDefaultFont();
            if (font != null) tmp.font = font;
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 22;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14;
            tmp.fontSizeMax = 26;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;

            return btn;
        }
    }
}
