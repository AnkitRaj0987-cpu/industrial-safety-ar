// GasInteractionFeedbackUI.cs
// Namespace : IndustrialSafetyAR.UI
//
// Worker-facing feedback and guided navigation UI for the Gas Leak & Confined Space module.
// Enforces explicit Step X/9 display, compact progress indicator (dots), explicit Action -> Success -> Next flow,
// large high-contrast primary Next button, Back button for reviewing completed steps,
// multi-gas atmospheric detector representation with OSHA sequence enforcement,
// and phone-touch-friendly primary action buttons with TapGatedButton swipe rejection.

using System;
using IndustrialSafetyAR.Core;
using IndustrialSafetyAR.Core.Audio;
using IndustrialSafetyAR.Modules.GasConfinedSpace;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IndustrialSafetyAR.UI
{
    /// <summary>
    /// Displays guided instructions, step progression, handheld multi-gas detector representation,
    /// state feedback, and touch action buttons to the worker during AR Gas training.
    /// </summary>
    public class GasInteractionFeedbackUI : MonoBehaviour
    {
        [Tooltip("The interaction controller to listen to. Discovered automatically if null.")]
        [SerializeField]
        private GasArInteractionController _controller;

        [Tooltip("TextMeshProUGUI component displaying current guidance/instruction.")]
        [SerializeField]
        private TextMeshProUGUI _promptText;

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

        private GameObject _soundButtonObj;
        private Button _soundButton;
        private TextMeshProUGUI _soundButtonText;

        private GameObject _alarmButtonObj;
        private Button _alarmButton;
        private TextMeshProUGUI _alarmButtonText;

        private GameObject _actionContainer;
        private GameObject _optionsContainer;

        // Step 1 button
        private GameObject _step1ActionBtnObj;
        private Button _step1ActionBtn;
        private TextMeshProUGUI _step1ActionBtnText;

        // Step 2 button
        private GameObject _step2ActionBtnObj;
        private Button _step2ActionBtn;
        private TextMeshProUGUI _step2ActionBtnText;

        // Step 3 Detector UI Representation
        private GameObject _detectorRootObj;
        private TextMeshProUGUI _detectorHeaderStatus;

        private TextMeshProUGUI _o2ReadoutText;
        private Button _btnTestO2;
        private TextMeshProUGUI _btnTestO2Text;
        private Image _btnTestO2Bg;

        private TextMeshProUGUI _lelReadoutText;
        private Button _btnTestLel;
        private TextMeshProUGUI _btnTestLelText;
        private Image _btnTestLelBg;

        private TextMeshProUGUI _h2sReadoutText;
        private Button _btnTestH2s;
        private TextMeshProUGUI _btnTestH2sText;
        private Image _btnTestH2sBg;

        // Next Button
        private GameObject _nextButtonObj;
        private Button _nextButton;
        private TextMeshProUGUI _nextButtonText;

        private readonly GuidedStepNavigator _fallbackNavigator = new GuidedStepNavigator();
        private static TMP_FontAsset s_CachedFont;

        public Canvas Canvas => _canvas;

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

        public GuidedStepNavigator Navigator => _controller != null ? _controller.StepNavigator : _fallbackNavigator;

        public Button NextButton => _nextButton;
        public Button BackButton => _backButton;
        public Button SoundButton => _soundButton;
        public Button AlarmButton => _alarmButton;
        public Button Step1ActionButton => _step1ActionBtn;
        public Button Step2ActionButton => _step2ActionBtn;
        public Button BtnTestO2 => _btnTestO2;
        public Button BtnTestLel => _btnTestLel;
        public Button BtnTestH2s => _btnTestH2s;
        public TextMeshProUGUI DetectorHeaderStatus => _detectorHeaderStatus;
        public bool IsNextButtonVisible => _nextButtonObj != null && _nextButtonObj.activeSelf;
        public bool IsBackButtonVisible => _backButtonObj != null && _backButtonObj.activeSelf;

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
            if (_controller == null)
            {
                _controller = FindAnyObjectByType<GasArInteractionController>(FindObjectsInactive.Include);
            }

            _canvas = GetOrCreateCanvas();
            EnsurePromptBanner();
            EnsureActionContainer();

            if (WorkerHomeController.Instance == null || WorkerHomeController.Instance.CurrentState != WorkerHomeController.WorkerAppScreenState.TrainingGas)
            {
                HideTrainingUI();
            }
        }

        private Canvas GetOrCreateCanvas()
        {
            if (_canvas != null) return _canvas;

            var existingObj = GameObject.Find("GasTrainingCanvas");
            if (existingObj != null)
            {
                _canvas = existingObj.GetComponent<Canvas>();
                if (_canvas != null) return _canvas;
            }

            var canvasObj = new GameObject("GasTrainingCanvas");
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

            // Main top card inside safe area
            var bannerObj = new GameObject("GasTrainingHeaderBanner");
            bannerObj.transform.SetParent(canvas.transform, false);

            var rect = bannerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.74f);
            rect.anchorMax = new Vector2(0.96f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _bannerBg = bannerObj.AddComponent<Image>();
            _bannerBg.color = new Color(0.07f, 0.09f, 0.13f, 0.97f);

            // Top accent strip (Gas Warning Amber / Gold)
            var accentObj = new GameObject("TopAccentStrip");
            accentObj.transform.SetParent(bannerObj.transform, false);
            var accentRect = accentObj.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0.98f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.offsetMin = Vector2.zero;
            accentRect.offsetMax = Vector2.zero;
            var accentImg = accentObj.AddComponent<Image>();
            accentImg.color = new Color(0.95f, 0.65f, 0.12f, 1.0f);

            // Back button (top-left inside header)
            _backButtonObj = new GameObject("BackButton");
            _backButtonObj.transform.SetParent(bannerObj.transform, false);
            var backRect = _backButtonObj.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.02f, 0.80f);
            backRect.anchorMax = new Vector2(0.20f, 0.96f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;

            var backImg = _backButtonObj.AddComponent<Image>();
            backImg.color = new Color(0.16f, 0.20f, 0.28f, 0.98f);
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
            _backButtonText.text = $"<b>{LocaleService.Instance.Get("btn_back", "← BACK")}</b>";
            _backButtonText.fontSize = 17;
            _backButtonText.alignment = TextAlignmentOptions.Center;
            _backButtonText.color = new Color(0.96f, 0.72f, 0.20f);

            // Module Title Header
            var headerObj = new GameObject("HeaderTitle");
            headerObj.transform.SetParent(bannerObj.transform, false);
            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.21f, 0.80f);
            headerRect.anchorMax = new Vector2(0.60f, 0.96f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            _headerTitleText = headerObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _headerTitleText.font = font;
            _headerTitleText.text = LocaleService.Instance.Get("gas_header_title", "GAS LEAK & CONFINED SPACE SAFETY");
            _headerTitleText.fontSize = 18;
            _headerTitleText.enableAutoSizing = true;
            _headerTitleText.fontSizeMin = 13;
            _headerTitleText.fontSizeMax = 20;
            _headerTitleText.fontStyle = FontStyles.Bold;
            _headerTitleText.alignment = TextAlignmentOptions.Center;
            _headerTitleText.color = new Color(0.96f, 0.68f, 0.15f);

            // Alarm Toggle Button (in header)
            _alarmButtonObj = new GameObject("AlarmButton");
            _alarmButtonObj.transform.SetParent(bannerObj.transform, false);
            var alarmRect = _alarmButtonObj.AddComponent<RectTransform>();
            alarmRect.anchorMin = new Vector2(0.61f, 0.80f);
            alarmRect.anchorMax = new Vector2(0.85f, 0.96f);
            alarmRect.offsetMin = Vector2.zero;
            alarmRect.offsetMax = Vector2.zero;

            var alarmImg = _alarmButtonObj.AddComponent<Image>();
            alarmImg.color = new Color(0.20f, 0.16f, 0.22f, 0.98f);
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
            _alarmButtonText.fontSize = 13;
            _alarmButtonText.enableAutoSizing = true;
            _alarmButtonText.fontSizeMin = 10;
            _alarmButtonText.fontSizeMax = 15;
            _alarmButtonText.alignment = TextAlignmentOptions.Center;
            _alarmButtonText.color = new Color(1f, 0.5f, 0.5f);
            UpdateAlarmButtonVisual();

            // Sound Toggle Button (top-right inside header)
            _soundButtonObj = new GameObject("SoundButton");
            _soundButtonObj.transform.SetParent(bannerObj.transform, false);
            var soundRect = _soundButtonObj.AddComponent<RectTransform>();
            soundRect.anchorMin = new Vector2(0.86f, 0.80f);
            soundRect.anchorMax = new Vector2(0.98f, 0.96f);
            soundRect.offsetMin = Vector2.zero;
            soundRect.offsetMax = Vector2.zero;

            var soundImg = _soundButtonObj.AddComponent<Image>();
            soundImg.color = new Color(0.16f, 0.20f, 0.28f, 0.98f);
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
            _soundButtonText.fontSize = 14;
            _soundButtonText.alignment = TextAlignmentOptions.Center;
            _soundButtonText.color = new Color(0.96f, 0.82f, 0.25f);
            UpdateSoundButtonVisual();

            // Step Badge (e.g. STEP 1/9 • RECOGNIZE GAS HAZARD)
            var badgeObj = new GameObject("StepBadge");
            badgeObj.transform.SetParent(bannerObj.transform, false);
            var badgeRect = badgeObj.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.04f, 0.63f);
            badgeRect.anchorMax = new Vector2(0.96f, 0.79f);
            badgeRect.offsetMin = Vector2.zero;
            badgeRect.offsetMax = Vector2.zero;

            _stepBadgeText = badgeObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _stepBadgeText.font = font;
            _stepBadgeText.text = "<color=#F39C12><b>STEP 1/9</b></color> • RECOGNIZE GAS HAZARD";
            _stepBadgeText.fontSize = 20;
            _stepBadgeText.fontStyle = FontStyles.Bold;
            _stepBadgeText.alignment = TextAlignmentOptions.Center;
            _stepBadgeText.color = new Color(0.88f, 0.92f, 0.98f);

            // Progress Indicator (e.g. ● ○ ○ ○ ○ ○ ○ ○ ○   1 / 9)
            var progObj = new GameObject("ProgressIndicator");
            progObj.transform.SetParent(bannerObj.transform, false);
            var progRect = progObj.AddComponent<RectTransform>();
            progRect.anchorMin = new Vector2(0.04f, 0.49f);
            progRect.anchorMax = new Vector2(0.96f, 0.62f);
            progRect.offsetMin = Vector2.zero;
            progRect.offsetMax = Vector2.zero;

            _progressText = progObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _progressText.font = font;
            _progressText.text = "● ○ ○ ○ ○ ○ ○ ○ ○   1 / 9";
            _progressText.fontSize = 17;
            _progressText.alignment = TextAlignmentOptions.Center;
            _progressText.color = new Color(0.80f, 0.84f, 0.90f);

            // Instruction Prompt Text
            var textObj = new GameObject("PromptText");
            textObj.transform.SetParent(bannerObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.04f, 0.23f);
            textRect.anchorMax = new Vector2(0.96f, 0.48f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _promptText = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _promptText.font = font;
            _promptText.fontSize = 21;
            _promptText.enableAutoSizing = true;
            _promptText.fontSizeMin = 15;
            _promptText.fontSizeMax = 25;
            _promptText.alignment = TextAlignmentOptions.Center;
            _promptText.color = Color.white;
            _promptText.text = "Initializing Gas module...";

            // State Feedback Sub-banner
            var feedbackBoxObj = new GameObject("StateFeedbackBanner");
            feedbackBoxObj.transform.SetParent(bannerObj.transform, false);
            var fbBoxRect = feedbackBoxObj.AddComponent<RectTransform>();
            fbBoxRect.anchorMin = new Vector2(0.03f, 0.03f);
            fbBoxRect.anchorMax = new Vector2(0.97f, 0.21f);
            fbBoxRect.offsetMin = Vector2.zero;
            fbBoxRect.offsetMax = Vector2.zero;

            _feedbackBg = feedbackBoxObj.AddComponent<Image>();
            _feedbackBg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

            var feedbackTextObj = new GameObject("FeedbackText");
            feedbackTextObj.transform.SetParent(feedbackBoxObj.transform, false);
            var fbTextRect = feedbackTextObj.AddComponent<RectTransform>();
            fbTextRect.anchorMin = Vector2.zero;
            fbTextRect.anchorMax = Vector2.one;
            fbTextRect.offsetMin = new Vector2(10, 2);
            fbTextRect.offsetMax = new Vector2(-10, -2);

            _feedbackText = feedbackTextObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _feedbackText.font = font;
            _feedbackText.fontSize = 17;
            _feedbackText.enableAutoSizing = true;
            _feedbackText.fontSizeMin = 13;
            _feedbackText.fontSizeMax = 20;
            _feedbackText.alignment = TextAlignmentOptions.Center;
            _feedbackText.color = new Color(0.85f, 0.88f, 0.92f);
            _feedbackText.text = "Scanning floor surfaces...";
        }

        private void EnsureActionContainer()
        {
            if (_actionContainer != null && _optionsContainer != null && _nextButtonObj != null) return;

            var canvas = GetOrCreateCanvas();
            if (canvas == null) return;

            var font = GetDefaultFont();

            // Action container inside bottom safe area
            _actionContainer = new GameObject("TrainingActionContainer");
            _actionContainer.transform.SetParent(canvas.transform, false);

            var rect = _actionContainer.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.02f);
            rect.anchorMax = new Vector2(0.96f, 0.38f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Sub-container for contextual step actions
            _optionsContainer = new GameObject("OptionsContainer");
            _optionsContainer.transform.SetParent(_actionContainer.transform, false);
            var optRect = _optionsContainer.AddComponent<RectTransform>();
            optRect.anchorMin = new Vector2(0f, 0.24f);
            optRect.anchorMax = new Vector2(1f, 1f);
            optRect.offsetMin = Vector2.zero;
            optRect.offsetMax = Vector2.zero;

            // 1. Step 1 Action Button
            _step1ActionBtnObj = new GameObject("Step1HazardActionButton");
            _step1ActionBtnObj.transform.SetParent(_optionsContainer.transform, false);
            var s1Rect = _step1ActionBtnObj.AddComponent<RectTransform>();
            s1Rect.anchorMin = new Vector2(0.05f, 0.15f);
            s1Rect.anchorMax = new Vector2(0.95f, 0.85f);
            s1Rect.offsetMin = Vector2.zero;
            s1Rect.offsetMax = Vector2.zero;

            var s1Img = _step1ActionBtnObj.AddComponent<Image>();
            s1Img.color = new Color(0.85f, 0.52f, 0.10f, 0.98f);
            _step1ActionBtn = _step1ActionBtnObj.AddComponent<Button>();
            _step1ActionBtn.targetGraphic = s1Img;

            var s1Tap = _step1ActionBtnObj.AddComponent<TapGatedButton>();
            s1Tap.Initialize(() => { if (_controller != null) _controller.ProcessHazardTap(); });

            var s1LabelObj = new GameObject("Label");
            s1LabelObj.transform.SetParent(_step1ActionBtnObj.transform, false);
            var s1LabelRect = s1LabelObj.AddComponent<RectTransform>();
            s1LabelRect.anchorMin = Vector2.zero;
            s1LabelRect.anchorMax = Vector2.one;
            _step1ActionBtnText = s1LabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _step1ActionBtnText.font = font;
            _step1ActionBtnText.text = "<b>IDENTIFY GAS HAZARD →</b>";
            _step1ActionBtnText.alignment = TextAlignmentOptions.Center;
            _step1ActionBtnText.fontSize = 20;
            _step1ActionBtnText.color = Color.white;

            // 2. Step 2 Action Button
            _step2ActionBtnObj = new GameObject("Step2DangerZoneActionButton");
            _step2ActionBtnObj.transform.SetParent(_optionsContainer.transform, false);
            var s2Rect = _step2ActionBtnObj.AddComponent<RectTransform>();
            s2Rect.anchorMin = new Vector2(0.05f, 0.15f);
            s2Rect.anchorMax = new Vector2(0.95f, 0.85f);
            s2Rect.offsetMin = Vector2.zero;
            s2Rect.offsetMax = Vector2.zero;

            var s2Img = _step2ActionBtnObj.AddComponent<Image>();
            s2Img.color = new Color(0.85f, 0.35f, 0.12f, 0.98f);
            _step2ActionBtn = _step2ActionBtnObj.AddComponent<Button>();
            _step2ActionBtn.targetGraphic = s2Img;

            var s2Tap = _step2ActionBtnObj.AddComponent<TapGatedButton>();
            s2Tap.Initialize(() => { if (_controller != null) _controller.ProcessDangerZonePerimeterTap(); });

            var s2LabelObj = new GameObject("Label");
            s2LabelObj.transform.SetParent(_step2ActionBtnObj.transform, false);
            var s2LabelRect = s2LabelObj.AddComponent<RectTransform>();
            s2LabelRect.anchorMin = Vector2.zero;
            s2LabelRect.anchorMax = Vector2.one;
            _step2ActionBtnText = s2LabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _step2ActionBtnText.font = font;
            _step2ActionBtnText.text = "<b>MARK 3m DANGER PERIMETER →</b>";
            _step2ActionBtnText.alignment = TextAlignmentOptions.Center;
            _step2ActionBtnText.fontSize = 20;
            _step2ActionBtnText.color = Color.white;
            _step2ActionBtnObj.SetActive(false);

            // 3. Step 3 Handheld Multi-Gas Detector Representation
            BuildAtmosphericDetectorUI(_optionsContainer, font);

            // Primary Next Button
            _nextButtonObj = new GameObject("PrimaryNextButton");
            _nextButtonObj.transform.SetParent(_actionContainer.transform, false);
            var nextRect = _nextButtonObj.AddComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(0f, 0.02f);
            nextRect.anchorMax = new Vector2(1f, 0.22f);
            nextRect.offsetMin = Vector2.zero;
            nextRect.offsetMax = Vector2.zero;

            var nextImg = _nextButtonObj.AddComponent<Image>();
            nextImg.color = new Color(0.12f, 0.58f, 0.28f, 0.98f); // Emerald Green

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

        private void BuildAtmosphericDetectorUI(GameObject parent, TMP_FontAsset font)
        {
            _detectorRootObj = new GameObject("AtmosphericDetectorUI");
            _detectorRootObj.transform.SetParent(parent.transform, false);
            var dRect = _detectorRootObj.AddComponent<RectTransform>();
            dRect.anchorMin = Vector2.zero;
            dRect.anchorMax = Vector2.one;
            dRect.offsetMin = Vector2.zero;
            dRect.offsetMax = Vector2.zero;

            var dBg = _detectorRootObj.AddComponent<Image>();
            dBg.color = new Color(0.10f, 0.12f, 0.16f, 0.98f); // Ruggedized detector housing

            // Status bar on top of detector
            var statusObj = new GameObject("DetectorStatusBar");
            statusObj.transform.SetParent(_detectorRootObj.transform, false);
            var sRect = statusObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.02f, 0.78f);
            sRect.anchorMax = new Vector2(0.98f, 0.98f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;

            _detectorHeaderStatus = statusObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _detectorHeaderStatus.font = font;
            _detectorHeaderStatus.text = "<b>MULTI-GAS DETECTOR • READY (SEQUENCE: O2 -> LEL -> H2S)</b>";
            _detectorHeaderStatus.fontSize = 15;
            _detectorHeaderStatus.alignment = TextAlignmentOptions.Center;
            _detectorHeaderStatus.color = new Color(0.95f, 0.75f, 0.20f);

            // Row 1: Oxygen (O2)
            BuildSensorRow(_detectorRootObj, font, 0.52f, 0.76f, "O2 (Oxygen)", "--- % Vol",
                out _o2ReadoutText, out _btnTestO2, out _btnTestO2Text, out _btnTestO2Bg,
                () => PerformTest(GasSensorType.Oxygen));

            // Row 2: Combustible Gas (LEL)
            BuildSensorRow(_detectorRootObj, font, 0.27f, 0.51f, "LEL (Combustible)", "🔒 LOCKED",
                out _lelReadoutText, out _btnTestLel, out _btnTestLelText, out _btnTestLelBg,
                () => PerformTest(GasSensorType.Flammable));

            // Row 3: Toxic Contaminant (H2S)
            BuildSensorRow(_detectorRootObj, font, 0.02f, 0.26f, "H2S (Toxic Gas)", "🔒 LOCKED",
                out _h2sReadoutText, out _btnTestH2s, out _btnTestH2sText, out _btnTestH2sBg,
                () => PerformTest(GasSensorType.Toxic));

            _detectorRootObj.SetActive(false);
        }

        private void BuildSensorRow(GameObject parent, TMP_FontAsset font, float yMin, float yMax,
            string sensorLabel, string initialReadout,
            out TextMeshProUGUI readoutText, out Button testBtn, out TextMeshProUGUI btnText, out Image btnBg,
            Action onTestClicked)
        {
            var rowObj = new GameObject($"Row_{sensorLabel}");
            rowObj.transform.SetParent(parent.transform, false);
            var rRect = rowObj.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0.02f, yMin);
            rRect.anchorMax = new Vector2(0.98f, yMax);
            rRect.offsetMin = Vector2.zero;
            rRect.offsetMax = Vector2.zero;

            var rBg = rowObj.AddComponent<Image>();
            rBg.color = new Color(0.14f, 0.18f, 0.25f, 0.95f);

            // Readout label (left 60%)
            var labelObj = new GameObject("ReadoutLabel");
            labelObj.transform.SetParent(rowObj.transform, false);
            var lRect = labelObj.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.03f, 0.05f);
            lRect.anchorMax = new Vector2(0.62f, 0.95f);
            lRect.offsetMin = Vector2.zero;
            lRect.offsetMax = Vector2.zero;

            readoutText = labelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) readoutText.font = font;
            readoutText.text = $"<b>{sensorLabel}</b>: {initialReadout}";
            readoutText.fontSize = 15;
            readoutText.alignment = TextAlignmentOptions.MidlineLeft;
            readoutText.color = Color.white;

            // Action button (right 35%)
            var btnObj = new GameObject("TestButton");
            btnObj.transform.SetParent(rowObj.transform, false);
            var bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.65f, 0.10f);
            bRect.anchorMax = new Vector2(0.97f, 0.90f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;

            btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.20f, 0.50f, 0.85f, 0.98f);
            testBtn = btnObj.AddComponent<Button>();
            testBtn.targetGraphic = btnBg;

            var tapGated = btnObj.AddComponent<TapGatedButton>();
            tapGated.Initialize(onTestClicked);

            var btnLblObj = new GameObject("BtnLabel");
            btnLblObj.transform.SetParent(btnObj.transform, false);
            var blRect = btnLblObj.AddComponent<RectTransform>();
            blRect.anchorMin = Vector2.zero;
            blRect.anchorMax = Vector2.one;
            btnText = btnLblObj.AddComponent<TextMeshProUGUI>();
            if (font != null) btnText.font = font;
            btnText.text = "<b>TEST</b>";
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontSize = 15;
            btnText.color = Color.white;
        }

        private void PerformTest(GasSensorType sensorType)
        {
            if (_controller == null) return;

            if (_controller.TestSensor(sensorType, out string error))
            {
                UpdateDetectorVisuals();
            }
            else
            {
                UpdateDetectorVisuals();
            }
        }

        public void UpdateDetectorVisuals()
        {
            if (_controller == null) return;
            var sim = _controller.Workflow.AtmosphericSimulator;

            // O2
            if (sim.IsOxygenTested)
            {
                _o2ReadoutText.text = $"<b>O2 (Oxygen)</b>: <color=#E74C3C><b>19.1 % Vol (DEFICIENT)</b></color> ✓";
                _btnTestO2.interactable = false;
                _btnTestO2Bg.color = new Color(0.25f, 0.30f, 0.38f);
                _btnTestO2Text.text = "DONE";
            }
            else
            {
                _o2ReadoutText.text = "<b>O2 (Oxygen)</b>: [ READY ] →";
                _btnTestO2.interactable = true;
                _btnTestO2Bg.color = new Color(0.20f, 0.55f, 0.90f);
                _btnTestO2Text.text = "TEST O2";
            }

            // LEL
            if (sim.IsFlammableTested)
            {
                _lelReadoutText.text = $"<b>LEL (Combustible)</b>: <color=#E74C3C><b>18.0 % LEL (HAZARDOUS)</b></color> ✓";
                _btnTestLel.interactable = false;
                _btnTestLelBg.color = new Color(0.25f, 0.30f, 0.38f);
                _btnTestLelText.text = "DONE";
            }
            else if (sim.IsOxygenTested)
            {
                _lelReadoutText.text = "<b>LEL (Combustible)</b>: [ READY ] →";
                _btnTestLel.interactable = true;
                _btnTestLelBg.color = new Color(0.85f, 0.55f, 0.15f);
                _btnTestLelText.text = "TEST LEL";
            }
            else
            {
                _lelReadoutText.text = "<b>LEL (Combustible)</b>: 🔒 LOCKED";
                _btnTestLel.interactable = false;
                _btnTestLelBg.color = new Color(0.25f, 0.28f, 0.32f);
                _btnTestLelText.text = "LOCKED";
            }

            // H2S
            if (sim.IsToxicTested)
            {
                _h2sReadoutText.text = $"<b>H2S (Toxic)</b>: <color=#E74C3C><b>35.0 ppm (LETHAL DANGER)</b></color> ✓";
                _btnTestH2s.interactable = false;
                _btnTestH2sBg.color = new Color(0.25f, 0.30f, 0.38f);
                _btnTestH2sText.text = "DONE";
            }
            else if (sim.IsFlammableTested)
            {
                _h2sReadoutText.text = "<b>H2S (Toxic)</b>: [ READY ] →";
                _btnTestH2s.interactable = true;
                _btnTestH2sBg.color = new Color(0.85f, 0.25f, 0.20f);
                _btnTestH2sText.text = "TEST H2S";
            }
            else
            {
                _h2sReadoutText.text = "<b>H2S (Toxic)</b>: 🔒 LOCKED";
                _btnTestH2s.interactable = false;
                _btnTestH2sBg.color = new Color(0.25f, 0.28f, 0.32f);
                _btnTestH2sText.text = "LOCKED";
            }

            // Assessment summary
            if (sim.IsAssessmentCompleted)
            {
                _detectorHeaderStatus.text = "<b><color=#E74C3C>ATMOSPHERE: UNSAFE • DO NOT ENTER</color></b>";
            }
            else if (sim.IsFlammableTested)
            {
                _detectorHeaderStatus.text = "<b>SEQUENCE: 3/3 TOXIC CONTAMINANT (H2S)</b>";
            }
            else if (sim.IsOxygenTested)
            {
                _detectorHeaderStatus.text = "<b>SEQUENCE: 2/3 FLAMMABLE ATMOSPHERE (LEL)</b>";
            }
            else
            {
                _detectorHeaderStatus.text = "<b>SEQUENCE: 1/3 OXYGEN CONTENT (O2)</b>";
            }
        }

        private void OnEnable()
        {
            SubscribeEvents();
            if (LocaleService.Instance != null)
            {
                LocaleService.Instance.OnLanguageChanged -= HandleLanguageChanged;
                LocaleService.Instance.OnLanguageChanged += HandleLanguageChanged;
            }
            RefreshUI();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            if (LocaleService.Instance != null)
            {
                LocaleService.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void SubscribeEvents()
        {
            if (_controller != null)
            {
                _controller.OnStateChanged += HandleStateChanged;
                _controller.OnFeedbackChanged += HandleFeedbackChanged;
                _controller.StepNavigator.OnStepChanged += HandleStepChanged;
                _controller.StepNavigator.OnStepCompleted += HandleStepCompleted;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_controller != null)
            {
                _controller.OnStateChanged -= HandleStateChanged;
                _controller.OnFeedbackChanged -= HandleFeedbackChanged;
                _controller.StepNavigator.OnStepChanged -= HandleStepChanged;
                _controller.StepNavigator.OnStepCompleted -= HandleStepCompleted;
            }
        }

        private void HandleLanguageChanged(string newLang)
        {
            RefreshUI();
        }

        private void HandleStateChanged(GasInteractionState state)
        {
            RefreshUI();
        }

        private void HandleFeedbackChanged(string feedback)
        {
            if (_feedbackText != null)
            {
                _feedbackText.text = feedback;
            }
        }

        private void HandleStepChanged(int stepIndex)
        {
            RefreshUI();
        }

        private void HandleStepCompleted(int stepIndex, string feedback)
        {
            RefreshUI();
        }

        public void ShowTrainingUI()
        {
            EnsurePromptBanner();
            EnsureActionContainer();
            if (_canvas != null) _canvas.gameObject.SetActive(true);
            RefreshUI();
        }

        public void HideTrainingUI()
        {
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        public void RefreshUI()
        {
            EnsurePromptBanner();
            EnsureActionContainer();

            var nav = Navigator;
            int step = nav.CurrentStepIndex;
            var loc = LocaleService.Instance;

            // Header title
            if (_headerTitleText != null)
            {
                _headerTitleText.text = loc.Get("gas_header_title", "GAS LEAK & CONFINED SPACE SAFETY");
            }

            // Step Badge & Progress
            if (_stepBadgeText != null)
            {
                string stepTitle = loc.Get($"gas_step{step}_title", nav.GetStepInfo(step)?.Title ?? $"STEP {step}");
                _stepBadgeText.text = $"<color=#F39C12><b>STEP {step}/9</b></color> • {stepTitle.ToUpperInvariant()}";
            }

            if (_progressText != null)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 1; i <= nav.TotalSteps; i++)
                {
                    sb.Append(i <= nav.HighestCompletedStep || nav.IsStepCompleted(i) ? "● " : "○ ");
                }
                sb.Append($"  {step} / {nav.TotalSteps}");
                _progressText.text = sb.ToString();
            }

            // Prompt text
            if (_promptText != null)
            {
                string prompt = loc.Get($"gas_step{step}_prompt", nav.GetStepInfo(step)?.Instruction ?? "Follow on-screen guidance.");
                _promptText.text = prompt;
            }

            // Toggle step-specific contextual action containers
            if (_step1ActionBtnObj != null) _step1ActionBtnObj.SetActive(step == 1);
            if (_step2ActionBtnObj != null) _step2ActionBtnObj.SetActive(step == 2);
            if (_detectorRootObj != null)
            {
                _detectorRootObj.SetActive(step == 3);
                if (step == 3)
                {
                    UpdateDetectorVisuals();
                }
            }

            // Primary Next button visibility
            if (_nextButtonObj != null)
            {
                bool canNext = nav.CanGoNext;
                _nextButtonObj.SetActive(canNext);
                if (canNext && _nextButtonText != null)
                {
                    string nextLabel = loc.Get($"gas_step{step}_next", nav.GetStepInfo(step)?.NextButtonLabel ?? "NEXT STEP →");
                    _nextButtonText.text = $"<b>{nextLabel}</b>";
                }
            }

            // Back button text
            if (_backButtonText != null)
            {
                _backButtonText.text = $"<b>{loc.Get("btn_back", "← BACK")}</b>";
            }

            UpdateSoundButtonVisual();
            UpdateAlarmButtonVisual();
        }

        private void OnNextButtonClicked()
        {
            if (_controller != null)
            {
                _controller.AdvanceToNextStep();
            }
        }

        private void OnBackButtonClicked()
        {
            var nav = Navigator;
            if (nav.CanGoBack)
            {
                _controller?.ReturnToPreviousStep();
            }
            else
            {
                // Return to home menu
                WorkerHomeController.Instance?.ReturnToHome();
            }
        }

        private void ToggleSoundEnabled()
        {
            if (FireAudioService.Instance != null)
            {
                FireAudioService.Instance.IsSoundEnabled = !FireAudioService.Instance.IsSoundEnabled;
                UpdateSoundButtonVisual();
            }
        }

        private void ToggleEmergencyAlarmEnabled()
        {
            if (FireAudioService.Instance != null)
            {
                FireAudioService.Instance.IsEmergencyAlarmEnabled = !FireAudioService.Instance.IsEmergencyAlarmEnabled;
                UpdateAlarmButtonVisual();
            }
        }

        private void UpdateSoundButtonVisual()
        {
            if (_soundButtonText == null) return;
            bool snd = FireAudioService.Instance == null || FireAudioService.Instance.IsSoundEnabled;
            _soundButtonText.text = snd ? "<b>SND ON</b>" : "<b>SND OFF</b>";
            _soundButtonText.color = snd ? new Color(0.96f, 0.82f, 0.25f) : new Color(0.6f, 0.65f, 0.7f);
        }

        private void UpdateAlarmButtonVisual()
        {
            if (_alarmButtonText == null) return;
            bool alm = FireAudioService.Instance == null || FireAudioService.Instance.IsEmergencyAlarmEnabled;
            _alarmButtonText.text = alm ? "<b>ALARM ON</b>" : "<b>ALARM OFF</b>";
            _alarmButtonText.color = alm ? new Color(1f, 0.45f, 0.45f) : new Color(0.6f, 0.65f, 0.7f);
        }
    }
}
