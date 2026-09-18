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

        // Step 4: PPE Palette UI
        private GameObject _ppePaletteRootObj;
        private TextMeshProUGUI _ppeHeaderWarningText;
        private Button _btnConfirmPpe;
        private TextMeshProUGUI _btnConfirmPpeText;
        private Image _btnConfirmPpeBg;
        private readonly System.Collections.Generic.Dictionary<string, Button> _ppeButtons =
            new System.Collections.Generic.Dictionary<string, Button>(System.StringComparer.OrdinalIgnoreCase);
        private readonly System.Collections.Generic.Dictionary<string, TextMeshProUGUI> _ppeLabels =
            new System.Collections.Generic.Dictionary<string, TextMeshProUGUI>(System.StringComparer.OrdinalIgnoreCase);
        private readonly System.Collections.Generic.Dictionary<string, Image> _ppeItemBgs =
            new System.Collections.Generic.Dictionary<string, Image>(System.StringComparer.OrdinalIgnoreCase);

        // Step 5: PPE Verification UI
        private GameObject _ppeVerificationRootObj;
        private TextMeshProUGUI _verifyHeaderWarningText;
        private Button _btnVerifySeal;
        private TextMeshProUGUI _btnVerifySealText;
        private Image _btnVerifySealBg;
        private TextMeshProUGUI _sealStatusText;
        private Button _btnVerifyHarness;
        private TextMeshProUGUI _btnVerifyHarnessText;
        private Image _btnVerifyHarnessBg;
        private TextMeshProUGUI _harnessStatusText;
        private Button _btnCheckPressure;
        private TextMeshProUGUI _btnCheckPressureText;
        private Image _btnCheckPressureBg;
        private TextMeshProUGUI _pressureStatusText;

        // Step 6: Buddy / Outside Attendant UI
        private GameObject _buddySystemRootObj;
        private TextMeshProUGUI _buddyHeaderRuleText;
        private Button _btnAssignAttendant;
        private TextMeshProUGUI _btnAssignAttendantText;
        private Image _btnAssignAttendantBg;
        private TextMeshProUGUI _attendantStatusText;
        private Button _btnCheckCommunication;
        private TextMeshProUGUI _btnCheckCommunicationText;
        private Image _btnCheckCommunicationBg;
        private TextMeshProUGUI _commStatusText;

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

        // Step 4-6 Getters for Testing & Automation
        public GameObject PpePaletteRootObj => _ppePaletteRootObj;
        public GameObject PpeVerificationRootObj => _ppeVerificationRootObj;
        public GameObject BuddySystemRootObj => _buddySystemRootObj;
        public Button BtnConfirmPpe => _btnConfirmPpe;
        public Button BtnVerifySeal => _btnVerifySeal;
        public Button BtnVerifyHarness => _btnVerifyHarness;
        public Button BtnCheckPressure => _btnCheckPressure;
        public Button BtnAssignAttendant => _btnAssignAttendant;
        public Button BtnCheckCommunication => _btnCheckCommunication;
        public Button GetPpeButton(string itemId) => _ppeButtons.TryGetValue(itemId, out var btn) ? btn : null;

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

            // 4. Step 4 PPE Selection Palette
            BuildPpePaletteUI(_optionsContainer, font);

            // 5. Step 5 PPE Verification Inspection
            BuildPpeVerificationUI(_optionsContainer, font);

            // 6. Step 6 Buddy / Outside Attendant System
            BuildBuddySystemUI(_optionsContainer, font);

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

        // =============================================================
        // STEP 4: PPE PALETTE BUILDER & UPDATER
        // =============================================================
        private void BuildPpePaletteUI(GameObject parent, TMP_FontAsset font)
        {
            _ppePaletteRootObj = new GameObject("PpePaletteUI");
            _ppePaletteRootObj.transform.SetParent(parent.transform, false);
            var pRect = _ppePaletteRootObj.AddComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;

            var pBg = _ppePaletteRootObj.AddComponent<Image>();
            pBg.color = new Color(0.08f, 0.10f, 0.14f, 0.98f);

            // Warning Header Banner
            var warnObj = new GameObject("PpeWarningBanner");
            warnObj.transform.SetParent(_ppePaletteRootObj.transform, false);
            var wRect = warnObj.AddComponent<RectTransform>();
            wRect.anchorMin = new Vector2(0.02f, 0.82f);
            wRect.anchorMax = new Vector2(0.98f, 0.98f);
            wRect.offsetMin = Vector2.zero;
            wRect.offsetMax = Vector2.zero;

            _ppeHeaderWarningText = warnObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _ppeHeaderWarningText.font = font;
            _ppeHeaderWarningText.text = "<b><color=#E74C3C>ATMOSPHERE: UNSAFE</color> • PPE PREPARATION REQUIRED (ENTRY PROHIBITED)</b>";
            _ppeHeaderWarningText.fontSize = 12;
            _ppeHeaderWarningText.alignment = TextAlignmentOptions.Center;
            _ppeHeaderWarningText.enableAutoSizing = true;
            _ppeHeaderWarningText.fontSizeMin = 9;
            _ppeHeaderWarningText.fontSizeMax = 14;

            // 2-Column layout for PPE items:
            // Left Column (x: 0.02 to 0.49):
            BuildPpeItemButton(_ppePaletteRootObj, font, 0.02f, 0.49f, 0.62f, 0.80f, GasPpeSystem.ItemHelmet, "Safety Helmet");
            BuildPpeItemButton(_ppePaletteRootObj, font, 0.02f, 0.49f, 0.42f, 0.60f, GasPpeSystem.ItemHarness, "Full-Body Harness");
            BuildPpeItemButton(_ppePaletteRootObj, font, 0.02f, 0.49f, 0.22f, 0.40f, GasPpeSystem.ItemGloves, "Protective Gloves");
            BuildPpeItemButton(_ppePaletteRootObj, font, 0.02f, 0.49f, 0.02f, 0.20f, GasPpeSystem.ItemBoots, "Safety Boots");

            // Right Column (x: 0.51 to 0.98):
            BuildPpeItemButton(_ppePaletteRootObj, font, 0.51f, 0.98f, 0.62f, 0.80f, GasPpeSystem.ItemScba, "SCBA Apparatus");
            BuildPpeItemButton(_ppePaletteRootObj, font, 0.51f, 0.98f, 0.42f, 0.60f, GasPpeSystem.ItemDustMask, "Dust Mask [X]");
            BuildPpeItemButton(_ppePaletteRootObj, font, 0.51f, 0.98f, 0.22f, 0.40f, GasPpeSystem.ItemClothMask, "Surgical Mask [X]");

            // Confirm Button (Right col, row 4)
            var confirmBtnObj = new GameObject("ConfirmPpeButton");
            confirmBtnObj.transform.SetParent(_ppePaletteRootObj.transform, false);
            var cbRect = confirmBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.51f, 0.02f);
            cbRect.anchorMax = new Vector2(0.98f, 0.20f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;

            _btnConfirmPpeBg = confirmBtnObj.AddComponent<Image>();
            _btnConfirmPpeBg.color = new Color(0.18f, 0.62f, 0.32f, 0.98f);
            _btnConfirmPpe = confirmBtnObj.AddComponent<Button>();
            _btnConfirmPpe.targetGraphic = _btnConfirmPpeBg;

            var cbTap = confirmBtnObj.AddComponent<TapGatedButton>();
            cbTap.Initialize(() => {
                if (_controller != null)
                {
                    _controller.SubmitPpeSelection();
                    UpdatePpePaletteVisuals();
                }
            });

            var cLblObj = new GameObject("Label");
            cLblObj.transform.SetParent(confirmBtnObj.transform, false);
            var clRect = cLblObj.AddComponent<RectTransform>();
            clRect.anchorMin = Vector2.zero;
            clRect.anchorMax = Vector2.one;
            _btnConfirmPpeText = cLblObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _btnConfirmPpeText.font = font;
            _btnConfirmPpeText.text = "<b>CONFIRM PPE ✓</b>";
            _btnConfirmPpeText.alignment = TextAlignmentOptions.Center;
            _btnConfirmPpeText.fontSize = 13;
            _btnConfirmPpeText.color = Color.white;

            _ppePaletteRootObj.SetActive(false);
        }

        private void BuildPpeItemButton(GameObject parent, TMP_FontAsset font, float xMin, float xMax, float yMin, float yMax, string itemId, string defaultLabel)
        {
            var btnObj = new GameObject($"PpeBtn_{itemId}");
            btnObj.transform.SetParent(parent.transform, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.16f, 0.20f, 0.28f, 0.95f);
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            var tapGated = btnObj.AddComponent<TapGatedButton>();
            tapGated.Initialize(() => {
                if (_controller != null)
                {
                    _controller.TogglePpeItem(itemId);
                    UpdatePpePaletteVisuals();
                }
            });

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(6, 2);
            tRect.offsetMax = new Vector2(-6, -2);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = $"[  ] {defaultLabel}";
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.fontSize = 12;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 9;
            tmp.fontSizeMax = 13;
            tmp.color = Color.white;

            _ppeButtons[itemId] = btn;
            _ppeLabels[itemId] = tmp;
            _ppeItemBgs[itemId] = bg;
        }

        private void UpdatePpePaletteVisuals()
        {
            if (_controller == null) return;
            var loc = LocaleService.Instance;

            if (_ppeHeaderWarningText != null)
            {
                _ppeHeaderWarningText.text = $"<b><color=#E74C3C>{loc.Get("status_unsafe_atmosphere", "ATMOSPHERE: UNSAFE")}</color> • {loc.Get("ppe_unsafe_warning", "PPE DOES NOT MAKE AN UNSAFE ATMOSPHERE SAFE. DO NOT ENTER.")}</b>";
            }

            foreach (var kvp in _ppeButtons)
            {
                string itemId = kvp.Key;
                bool isSelected = _controller.IsPpeItemSelected(itemId);
                string itemTitle = loc.Get($"ppe_item_{itemId.Replace("ppe_", "")}", itemId);

                if (_ppeLabels.TryGetValue(itemId, out var label))
                {
                    label.text = isSelected ? $"<b><color=#2ECC71>[✓]</color></b> {itemTitle}" : $"[  ] {itemTitle}";
                    label.color = isSelected ? new Color(0.3f, 0.9f, 0.4f) : Color.white;
                }

                if (_ppeItemBgs.TryGetValue(itemId, out var bg))
                {
                    bg.color = isSelected ? new Color(0.10f, 0.35f, 0.18f, 0.98f) : new Color(0.16f, 0.20f, 0.28f, 0.95f);
                }
            }

            if (_btnConfirmPpeText != null)
            {
                _btnConfirmPpeText.text = $"<b>{loc.Get("ppe_btn_confirm", "CONFIRM PPE ✓")}</b>";
            }
        }

        // =============================================================
        // STEP 5: PPE VERIFICATION BUILDER & UPDATER
        // =============================================================
        private void BuildPpeVerificationUI(GameObject parent, TMP_FontAsset font)
        {
            _ppeVerificationRootObj = new GameObject("PpeVerificationUI");
            _ppeVerificationRootObj.transform.SetParent(parent.transform, false);
            var vRect = _ppeVerificationRootObj.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;
            vRect.offsetMin = Vector2.zero;
            vRect.offsetMax = Vector2.zero;

            var vBg = _ppeVerificationRootObj.AddComponent<Image>();
            vBg.color = new Color(0.08f, 0.10f, 0.14f, 0.98f);

            // Header Warning
            var warnObj = new GameObject("VerifyWarningBanner");
            warnObj.transform.SetParent(_ppeVerificationRootObj.transform, false);
            var wRect = warnObj.AddComponent<RectTransform>();
            wRect.anchorMin = new Vector2(0.02f, 0.78f);
            wRect.anchorMax = new Vector2(0.98f, 0.98f);
            wRect.offsetMin = Vector2.zero;
            wRect.offsetMax = Vector2.zero;

            _verifyHeaderWarningText = warnObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _verifyHeaderWarningText.font = font;
            _verifyHeaderWarningText.text = "<b><color=#F39C12>EQUIPMENT READINESS INSPECTION</color>\n<size=80%>(PPE does NOT make an unsafe atmosphere safe • Entry prohibited)</size></b>";
            _verifyHeaderWarningText.fontSize = 12;
            _verifyHeaderWarningText.alignment = TextAlignmentOptions.Center;

            // Row 1: SCBA Face Seal Check (y: 0.52 to 0.76)
            BuildVerificationRow(_ppeVerificationRootObj, font, 0.52f, 0.76f, "SCBA Seal Check",
                out _sealStatusText, out _btnVerifySeal, out _btnVerifySealText, out _btnVerifySealBg,
                () => {
                    if (_controller != null) {
                        _controller.VerifyScbaSeal();
                        UpdatePpeVerificationVisuals();
                    }
                });

            // Row 2: Harness Fit Inspection (y: 0.27 to 0.51)
            BuildVerificationRow(_ppeVerificationRootObj, font, 0.27f, 0.51f, "Harness Fit & D-Ring",
                out _harnessStatusText, out _btnVerifyHarness, out _btnVerifyHarnessText, out _btnVerifyHarnessBg,
                () => {
                    if (_controller != null) {
                        _controller.VerifyHarnessFit();
                        UpdatePpeVerificationVisuals();
                    }
                });

            // Row 3: Cylinder Pressure Check (y: 0.02 to 0.26)
            BuildVerificationRow(_ppeVerificationRootObj, font, 0.02f, 0.26f, "Cylinder Pressure (300 Bar)",
                out _pressureStatusText, out _btnCheckPressure, out _btnCheckPressureText, out _btnCheckPressureBg,
                () => {
                    if (_controller != null) {
                        _controller.CheckCylinderPressure();
                        UpdatePpeVerificationVisuals();
                    }
                });

            _ppeVerificationRootObj.SetActive(false);
        }

        private void BuildVerificationRow(GameObject parent, TMP_FontAsset font, float yMin, float yMax,
            string rowLabel, out TextMeshProUGUI statusText, out Button actionBtn, out TextMeshProUGUI btnText, out Image btnBg,
            Action onActionClicked)
        {
            var rowObj = new GameObject($"Row_{rowLabel}");
            rowObj.transform.SetParent(parent.transform, false);
            var rRect = rowObj.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0.02f, yMin);
            rRect.anchorMax = new Vector2(0.98f, yMax);
            rRect.offsetMin = Vector2.zero;
            rRect.offsetMax = Vector2.zero;

            var rBg = rowObj.AddComponent<Image>();
            rBg.color = new Color(0.14f, 0.18f, 0.25f, 0.95f);

            var labelObj = new GameObject("StatusLabel");
            labelObj.transform.SetParent(rowObj.transform, false);
            var lRect = labelObj.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.03f, 0.05f);
            lRect.anchorMax = new Vector2(0.64f, 0.95f);
            lRect.offsetMin = Vector2.zero;
            lRect.offsetMax = Vector2.zero;

            statusText = labelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) statusText.font = font;
            statusText.text = $"<b>{rowLabel}</b>: Pending";
            statusText.fontSize = 14;
            statusText.alignment = TextAlignmentOptions.MidlineLeft;
            statusText.color = Color.white;

            var btnObj = new GameObject("VerifyButton");
            btnObj.transform.SetParent(rowObj.transform, false);
            var bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.66f, 0.10f);
            bRect.anchorMax = new Vector2(0.97f, 0.90f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;

            btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.20f, 0.50f, 0.85f, 0.98f);
            actionBtn = btnObj.AddComponent<Button>();
            actionBtn.targetGraphic = btnBg;

            var tapGated = btnObj.AddComponent<TapGatedButton>();
            tapGated.Initialize(onActionClicked);

            var btnLblObj = new GameObject("BtnLabel");
            btnLblObj.transform.SetParent(btnObj.transform, false);
            var blRect = btnLblObj.AddComponent<RectTransform>();
            blRect.anchorMin = Vector2.zero;
            blRect.anchorMax = Vector2.one;
            btnText = btnLblObj.AddComponent<TextMeshProUGUI>();
            if (font != null) btnText.font = font;
            btnText.text = "<b>VERIFY</b>";
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontSize = 14;
            btnText.color = Color.white;
        }

        private void UpdatePpeVerificationVisuals()
        {
            if (_controller == null) return;
            var loc = LocaleService.Instance;

            if (_verifyHeaderWarningText != null)
            {
                _verifyHeaderWarningText.text = $"<b><color=#F39C12>{loc.Get("verify_title", "PPE INSPECTION & VERIFICATION")}</color>\n<size=80%>({loc.Get("verify_warning", "PPE does not make atmosphere safe • Entry prohibited")})</size></b>";
            }

            // Seal
            bool sealOk = _controller.IsSealCheckPassed;
            if (_sealStatusText != null)
            {
                _sealStatusText.text = $"<b>{loc.Get("verify_scba_seal", "SCBA Seal")}</b>: {(sealOk ? "<color=#2ECC71>VERIFIED ✓</color>" : "Pending")}";
            }
            if (_btnVerifySeal != null)
            {
                _btnVerifySeal.interactable = !sealOk;
                if (_btnVerifySealBg != null) _btnVerifySealBg.color = sealOk ? new Color(0.15f, 0.45f, 0.22f) : new Color(0.20f, 0.50f, 0.85f);
                if (_btnVerifySealText != null) _btnVerifySealText.text = sealOk ? "DONE ✓" : loc.Get("verify_btn_action", "VERIFY");
            }

            // Harness
            bool harnessOk = _controller.IsHarnessFitPassed;
            if (_harnessStatusText != null)
            {
                _harnessStatusText.text = $"<b>{loc.Get("verify_harness_fit", "Harness Fit")}</b>: {(harnessOk ? "<color=#2ECC71>VERIFIED ✓</color>" : "Pending")}";
            }
            if (_btnVerifyHarness != null)
            {
                _btnVerifyHarness.interactable = !harnessOk;
                if (_btnVerifyHarnessBg != null) _btnVerifyHarnessBg.color = harnessOk ? new Color(0.15f, 0.45f, 0.22f) : new Color(0.20f, 0.50f, 0.85f);
                if (_btnVerifyHarnessText != null) _btnVerifyHarnessText.text = harnessOk ? "DONE ✓" : loc.Get("verify_btn_action", "VERIFY");
            }

            // Cylinder
            bool cylOk = _controller.IsCylinderPressurePassed;
            if (_pressureStatusText != null)
            {
                _pressureStatusText.text = $"<b>{loc.Get("verify_cylinder_pressure", "Cylinder Pressure")}</b>: {(cylOk ? "<color=#2ECC71>300 BAR (OK) ✓</color>" : "Pending")}";
            }
            if (_btnCheckPressure != null)
            {
                _btnCheckPressure.interactable = !cylOk;
                if (_btnCheckPressureBg != null) _btnCheckPressureBg.color = cylOk ? new Color(0.15f, 0.45f, 0.22f) : new Color(0.20f, 0.50f, 0.85f);
                if (_btnCheckPressureText != null) _btnCheckPressureText.text = cylOk ? "DONE ✓" : loc.Get("verify_btn_action", "VERIFY");
            }
        }

        // =============================================================
        // STEP 6: BUDDY SYSTEM BUILDER & UPDATER
        // =============================================================
        private void BuildBuddySystemUI(GameObject parent, TMP_FontAsset font)
        {
            _buddySystemRootObj = new GameObject("BuddySystemUI");
            _buddySystemRootObj.transform.SetParent(parent.transform, false);
            var bRect = _buddySystemRootObj.AddComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;

            var bBg = _buddySystemRootObj.AddComponent<Image>();
            bBg.color = new Color(0.08f, 0.10f, 0.14f, 0.98f);

            // Rule Warning Header
            var ruleObj = new GameObject("BuddyRuleBanner");
            ruleObj.transform.SetParent(_buddySystemRootObj.transform, false);
            var rRect = ruleObj.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0.02f, 0.74f);
            rRect.anchorMax = new Vector2(0.98f, 0.98f);
            rRect.offsetMin = Vector2.zero;
            rRect.offsetMax = Vector2.zero;

            _buddyHeaderRuleText = ruleObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _buddyHeaderRuleText.font = font;
            _buddyHeaderRuleText.text = "<b><color=#E67E22>SAFETY ATTENDANT MUST REMAIN OUTSIDE</color>\n<size=80%>Stationed outside danger perimeter • Maintain radio contact</size></b>";
            _buddyHeaderRuleText.fontSize = 12;
            _buddyHeaderRuleText.alignment = TextAlignmentOptions.Center;

            // Row 1: Assign Outside Attendant (y: 0.38 to 0.72)
            var attRowObj = new GameObject("Row_AssignAttendant");
            attRowObj.transform.SetParent(_buddySystemRootObj.transform, false);
            var arRect = attRowObj.AddComponent<RectTransform>();
            arRect.anchorMin = new Vector2(0.02f, 0.38f);
            arRect.anchorMax = new Vector2(0.98f, 0.72f);
            arRect.offsetMin = Vector2.zero;
            arRect.offsetMax = Vector2.zero;
            attRowObj.AddComponent<Image>().color = new Color(0.14f, 0.18f, 0.25f, 0.95f);

            var attLblObj = new GameObject("AttendantStatusLabel");
            attLblObj.transform.SetParent(attRowObj.transform, false);
            var alRect = attLblObj.AddComponent<RectTransform>();
            alRect.anchorMin = new Vector2(0.03f, 0.05f);
            alRect.anchorMax = new Vector2(0.60f, 0.95f);
            alRect.offsetMin = Vector2.zero;
            alRect.offsetMax = Vector2.zero;

            _attendantStatusText = attLblObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _attendantStatusText.font = font;
            _attendantStatusText.text = "<b>Attendant</b>: Standby outside (3.4m)";
            _attendantStatusText.fontSize = 13;
            _attendantStatusText.alignment = TextAlignmentOptions.MidlineLeft;
            _attendantStatusText.color = Color.white;

            var attBtnObj = new GameObject("AssignAttendantButton");
            attBtnObj.transform.SetParent(attRowObj.transform, false);
            var abRect = attBtnObj.AddComponent<RectTransform>();
            abRect.anchorMin = new Vector2(0.62f, 0.10f);
            abRect.anchorMax = new Vector2(0.98f, 0.90f);
            abRect.offsetMin = Vector2.zero;
            abRect.offsetMax = Vector2.zero;

            _btnAssignAttendantBg = attBtnObj.AddComponent<Image>();
            _btnAssignAttendantBg.color = new Color(0.20f, 0.50f, 0.85f, 0.98f);
            _btnAssignAttendant = attBtnObj.AddComponent<Button>();
            _btnAssignAttendant.targetGraphic = _btnAssignAttendantBg;
            var attTap = attBtnObj.AddComponent<TapGatedButton>();
            attTap.Initialize(() => {
                if (_controller != null) {
                    _controller.AssignAttendant();
                    UpdateBuddySystemVisuals();
                }
            });

            var ablObj = new GameObject("Label");
            ablObj.transform.SetParent(attBtnObj.transform, false);
            var ablRect = ablObj.AddComponent<RectTransform>();
            ablRect.anchorMin = Vector2.zero;
            ablRect.anchorMax = Vector2.one;
            _btnAssignAttendantText = ablObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _btnAssignAttendantText.font = font;
            _btnAssignAttendantText.text = "<b>ASSIGN</b>";
            _btnAssignAttendantText.fontSize = 14;
            _btnAssignAttendantText.alignment = TextAlignmentOptions.Center;
            _btnAssignAttendantText.color = Color.white;

            // Row 2: Check Two-Way Radio (y: 0.04 to 0.36)
            var commRowObj = new GameObject("Row_CheckRadio");
            commRowObj.transform.SetParent(_buddySystemRootObj.transform, false);
            var crRect = commRowObj.AddComponent<RectTransform>();
            crRect.anchorMin = new Vector2(0.02f, 0.04f);
            crRect.anchorMax = new Vector2(0.98f, 0.36f);
            crRect.offsetMin = Vector2.zero;
            crRect.offsetMax = Vector2.zero;
            commRowObj.AddComponent<Image>().color = new Color(0.14f, 0.18f, 0.25f, 0.95f);

            var commLblObj = new GameObject("CommStatusLabel");
            commLblObj.transform.SetParent(commRowObj.transform, false);
            var clRect = commLblObj.AddComponent<RectTransform>();
            clRect.anchorMin = new Vector2(0.03f, 0.05f);
            clRect.anchorMax = new Vector2(0.60f, 0.95f);
            clRect.offsetMin = Vector2.zero;
            clRect.offsetMax = Vector2.zero;

            _commStatusText = commLblObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _commStatusText.font = font;
            _commStatusText.text = "<b>Radio</b>: Waiting for attendant";
            _commStatusText.fontSize = 13;
            _commStatusText.alignment = TextAlignmentOptions.MidlineLeft;
            _commStatusText.color = Color.white;

            var commBtnObj = new GameObject("CheckCommButton");
            commBtnObj.transform.SetParent(commRowObj.transform, false);
            var cbRect = commBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.62f, 0.10f);
            cbRect.anchorMax = new Vector2(0.98f, 0.90f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;

            _btnCheckCommunicationBg = commBtnObj.AddComponent<Image>();
            _btnCheckCommunicationBg.color = new Color(0.25f, 0.28f, 0.32f, 0.98f);
            _btnCheckCommunication = commBtnObj.AddComponent<Button>();
            _btnCheckCommunication.targetGraphic = _btnCheckCommunicationBg;
            _btnCheckCommunication.interactable = false;

            var commTap = commBtnObj.AddComponent<TapGatedButton>();
            commTap.Initialize(() => {
                if (_controller != null) {
                    _controller.CheckCommunication();
                    UpdateBuddySystemVisuals();
                }
            });

            var cblObj = new GameObject("Label");
            cblObj.transform.SetParent(commBtnObj.transform, false);
            var cblRect = cblObj.AddComponent<RectTransform>();
            cblRect.anchorMin = Vector2.zero;
            cblRect.anchorMax = Vector2.one;
            _btnCheckCommunicationText = cblObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _btnCheckCommunicationText.font = font;
            _btnCheckCommunicationText.text = "<b>RADIO</b>";
            _btnCheckCommunicationText.fontSize = 14;
            _btnCheckCommunicationText.alignment = TextAlignmentOptions.Center;
            _btnCheckCommunicationText.color = Color.white;

            _buddySystemRootObj.SetActive(false);
        }

        private void UpdateBuddySystemVisuals()
        {
            if (_controller == null) return;
            var loc = LocaleService.Instance;

            if (_buddyHeaderRuleText != null)
            {
                _buddyHeaderRuleText.text = $"<b><color=#E67E22>{loc.Get("attendant_title", "SAFETY ATTENDANT MUST REMAIN OUTSIDE")}</color>\n<size=80%>{loc.Get("attendant_rule", "Stationed outside danger perimeter • Maintain radio contact")}</size></b>";
            }

            bool attAssigned = _controller.IsAttendantAssigned;
            if (_attendantStatusText != null)
            {
                _attendantStatusText.text = $"<b>{loc.Get("attendant_title", "Attendant")}</b>: {(attAssigned ? $"<color=#2ECC71>{loc.Get("attendant_status_assigned", "ASSIGNED (OUTSIDE) ✓")}</color>" : loc.Get("attendant_outside", "Standby outside (3.4m)"))}";
            }
            if (_btnAssignAttendant != null)
            {
                _btnAssignAttendant.interactable = !attAssigned;
                if (_btnAssignAttendantBg != null) _btnAssignAttendantBg.color = attAssigned ? new Color(0.15f, 0.45f, 0.22f) : new Color(0.20f, 0.50f, 0.85f);
                if (_btnAssignAttendantText != null) _btnAssignAttendantText.text = attAssigned ? "DONE ✓" : loc.Get("attendant_btn_assign", "ASSIGN");
            }

            bool commOk = _controller.IsCommunicationChecked;
            if (_commStatusText != null)
            {
                _commStatusText.text = $"<b>{loc.Get("comm_title", "Radio Link")}</b>: {(commOk ? $"<color=#2ECC71>{loc.Get("comm_status_verified", "RADIO LINK VERIFIED ✓")}</color>" : (attAssigned ? "Ready to test" : "Waiting for attendant"))}";
            }
            if (_btnCheckCommunication != null)
            {
                _btnCheckCommunication.interactable = attAssigned && !commOk;
                if (_btnCheckCommunicationBg != null)
                {
                    _btnCheckCommunicationBg.color = commOk ? new Color(0.15f, 0.45f, 0.22f) : (attAssigned ? new Color(0.20f, 0.50f, 0.85f) : new Color(0.25f, 0.28f, 0.32f));
                }
                if (_btnCheckCommunicationText != null)
                {
                    _btnCheckCommunicationText.text = commOk ? "VERIFIED ✓" : loc.Get("comm_btn_check", "CHECK RADIO");
                }
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
            if (_ppePaletteRootObj != null)
            {
                _ppePaletteRootObj.SetActive(step == 4);
                if (step == 4)
                {
                    UpdatePpePaletteVisuals();
                }
            }
            if (_ppeVerificationRootObj != null)
            {
                _ppeVerificationRootObj.SetActive(step == 5);
                if (step == 5)
                {
                    UpdatePpeVerificationVisuals();
                }
            }
            if (_buddySystemRootObj != null)
            {
                _buddySystemRootObj.SetActive(step == 6);
                if (step == 6)
                {
                    UpdateBuddySystemVisuals();
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
