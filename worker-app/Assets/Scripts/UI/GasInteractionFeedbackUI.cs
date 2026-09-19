// GasInteractionFeedbackUI.cs
// Namespace : IndustrialSafetyAR.UI
//
// Worker-facing feedback and guided navigation UI for the Gas Leak & Confined Space module.
// Enforces explicit Step X/9 display, compact progress indicator (dots), explicit Action -> Success -> Next flow,
// large high-contrast primary Next button, Back button for reviewing completed steps,
// multi-gas atmospheric detector representation with OSHA sequence enforcement,
// and phone-touch-friendly primary action buttons with TapGatedButton swipe rejection.

using System;
using IndustrialSafetyAR.Assessment;
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
        private Image _nextButtonBg;

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

        // Step 7: Entry Decision UI
        private GameObject _entryDecisionRootObj;
        private TextMeshProUGUI _entryHeaderWarningText;
        private TextMeshProUGUI _entryDecisionPromptText;
        private Button _btnDoNotEnter;
        private TextMeshProUGUI _btnDoNotEnterText;
        private Image _btnDoNotEnterBg;
        private Button _btnEnterConfinedSpace;
        private TextMeshProUGUI _btnEnterConfinedSpaceText;
        private Image _btnEnterConfinedSpaceBg;
        private TextMeshProUGUI _entryDecisionFeedbackText;

        // Step 8: Emergency Response UI
        private GameObject _emergencyResponseRootObj;
        private TextMeshProUGUI _emergencyHeaderTitleText;
        private GameObject _gasAlarmSubPanel;
        private Button _btnAcknowledgeAlarm;
        private TextMeshProUGUI _btnAcknowledgeAlarmText;
        private Image _btnAcknowledgeAlarmBg;
        private GameObject _stopWorkSubPanel;
        private Button _btnAcknowledgeStopWork;
        private TextMeshProUGUI _btnAcknowledgeStopWorkText;
        private Image _btnAcknowledgeStopWorkBg;
        private GameObject _supervisorAlertSubPanel;
        private Button _btnAlertSupervisor;
        private TextMeshProUGUI _btnAlertSupervisorText;
        private Image _btnAlertSupervisorBg;
        private GameObject _evacuationWaypointsSubPanel;
        private TextMeshProUGUI _evacuationProgressText;
        private Button _btnWaypointAdvance;
        private TextMeshProUGUI _btnWaypointAdvanceText;
        private Image _btnWaypointAdvanceBg;
        private GameObject _trainedRescueSubPanel;
        private TextMeshProUGUI _rescueChecklistText;
        private Button _btnConfirmRescue;
        private TextMeshProUGUI _btnConfirmRescueText;
        private Image _btnConfirmRescueBg;

        // Step 9: Final Safety Check UI
        private GameObject _finalSafetyCheckRootObj;
        private TextMeshProUGUI _finalHeaderTitleText;
        private TextMeshProUGUI _finalChecklistText;
        private TextMeshProUGUI _finalAtmosphereRuleText;
        private Button _btnCompleteTraining;
        private TextMeshProUGUI _btnCompleteTrainingText;
        private Image _btnCompleteTrainingBg;

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

        // Step 7-9 Getters for Testing & Automation
        public GameObject EntryDecisionRootObj => _entryDecisionRootObj;
        public GameObject EmergencyResponseRootObj => _emergencyResponseRootObj;
        public GameObject FinalSafetyCheckRootObj => _finalSafetyCheckRootObj;
        public Button BtnDoNotEnter => _btnDoNotEnter;
        public Button BtnEnterConfinedSpace => _btnEnterConfinedSpace;
        public Button BtnAcknowledgeAlarm => _btnAcknowledgeAlarm;
        public Button BtnAcknowledgeStopWork => _btnAcknowledgeStopWork;
        public Button BtnAlertSupervisor => _btnAlertSupervisor;
        public Button BtnWaypointAdvance => _btnWaypointAdvance;
        public Button BtnConfirmRescue => _btnConfirmRescue;
        public Button BtnCompleteTraining => _btnCompleteTraining;
        public TextMeshProUGUI EntryDecisionFeedbackText => _entryDecisionFeedbackText;
        public TextMeshProUGUI FinalChecklistText => _finalChecklistText;

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
            _bannerBg.color = UITheme.ArHudBackground;

            // Top accent strip (Safety Orange CTA color)
            var accentObj = new GameObject("TopAccentStrip");
            accentObj.transform.SetParent(bannerObj.transform, false);
            var accentRect = accentObj.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0.98f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.offsetMin = Vector2.zero;
            accentRect.offsetMax = Vector2.zero;
            var accentImg = accentObj.AddComponent<Image>();
            accentImg.color = UITheme.PrimaryOrange;

            // Back button (top-left inside header)
            _backButtonObj = new GameObject("BackButton");
            _backButtonObj.transform.SetParent(bannerObj.transform, false);
            var backRect = _backButtonObj.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.02f, 0.80f);
            backRect.anchorMax = new Vector2(0.20f, 0.96f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;

            var backImg = _backButtonObj.AddComponent<Image>();
            backImg.color = UITheme.CardSecondaryBg;
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
            _backButtonText.fontSize = 20;
            _backButtonText.alignment = TextAlignmentOptions.Center;
            _backButtonText.color = UITheme.TextPrimary;

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
            _headerTitleText.fontSize = 24;
            _headerTitleText.enableAutoSizing = true;
            _headerTitleText.fontSizeMin = 18;
            _headerTitleText.fontSizeMax = 28;
            _headerTitleText.fontStyle = FontStyles.Bold;
            _headerTitleText.alignment = TextAlignmentOptions.Center;
            _headerTitleText.color = UITheme.TextPrimary;

            // Alarm Toggle Button (in header)
            _alarmButtonObj = new GameObject("AlarmButton");
            _alarmButtonObj.transform.SetParent(bannerObj.transform, false);
            var alarmRect = _alarmButtonObj.AddComponent<RectTransform>();
            alarmRect.anchorMin = new Vector2(0.61f, 0.80f);
            alarmRect.anchorMax = new Vector2(0.85f, 0.96f);
            alarmRect.offsetMin = Vector2.zero;
            alarmRect.offsetMax = Vector2.zero;

            var alarmImg = _alarmButtonObj.AddComponent<Image>();
            alarmImg.color = UITheme.DangerSurface;
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
            _alarmButtonText.fontSize = 18;
            _alarmButtonText.enableAutoSizing = true;
            _alarmButtonText.fontSizeMin = 14;
            _alarmButtonText.fontSizeMax = 22;
            _alarmButtonText.alignment = TextAlignmentOptions.Center;
            _alarmButtonText.color = UITheme.DangerText;
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
            soundImg.color = UITheme.CardSecondaryBg;
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
            _soundButtonText.fontSize = 18;
            _soundButtonText.alignment = TextAlignmentOptions.Center;
            _soundButtonText.color = UITheme.TextSecondary;
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
            _stepBadgeText.text = "<color=#EA580C><b>STEP 1/9</b></color> • RECOGNIZE GAS HAZARD";
            _stepBadgeText.fontSize = 26;
            _stepBadgeText.fontStyle = FontStyles.Bold;
            _stepBadgeText.alignment = TextAlignmentOptions.Center;
            _stepBadgeText.color = UITheme.TextPrimary;

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
            _progressText.fontSize = 22;
            _progressText.alignment = TextAlignmentOptions.Center;
            _progressText.color = UITheme.TextSecondary;

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
            _promptText.fontSize = 28;
            _promptText.enableAutoSizing = true;
            _promptText.fontSizeMin = 20;
            _promptText.fontSizeMax = 32;
            _promptText.alignment = TextAlignmentOptions.Center;
            _promptText.color = UITheme.TextPrimary;
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
            _feedbackBg.color = UITheme.CardSecondaryBg;

            var feedbackTextObj = new GameObject("FeedbackText");
            feedbackTextObj.transform.SetParent(feedbackBoxObj.transform, false);
            var fbTextRect = feedbackTextObj.AddComponent<RectTransform>();
            fbTextRect.anchorMin = Vector2.zero;
            fbTextRect.anchorMax = Vector2.one;
            fbTextRect.offsetMin = new Vector2(10, 2);
            fbTextRect.offsetMax = new Vector2(-10, -2);

            _feedbackText = feedbackTextObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _feedbackText.font = font;
            _feedbackText.fontSize = 22;
            _feedbackText.enableAutoSizing = true;
            _feedbackText.fontSizeMin = 18;
            _feedbackText.fontSizeMax = 26;
            _feedbackText.alignment = TextAlignmentOptions.Center;
            _feedbackText.color = UITheme.TextPrimary;
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
            s1Img.color = UITheme.PrimaryOrange;
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
            _step1ActionBtnText.fontSize = 28;
            _step1ActionBtnText.color = UITheme.TextLightOnDark;

            // 2. Step 2 Action Button
            _step2ActionBtnObj = new GameObject("Step2DangerZoneActionButton");
            _step2ActionBtnObj.transform.SetParent(_optionsContainer.transform, false);
            var s2Rect = _step2ActionBtnObj.AddComponent<RectTransform>();
            s2Rect.anchorMin = new Vector2(0.05f, 0.15f);
            s2Rect.anchorMax = new Vector2(0.95f, 0.85f);
            s2Rect.offsetMin = Vector2.zero;
            s2Rect.offsetMax = Vector2.zero;

            var s2Img = _step2ActionBtnObj.AddComponent<Image>();
            s2Img.color = UITheme.PrimaryOrange;
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
            _step2ActionBtnText.fontSize = 28;
            _step2ActionBtnText.color = UITheme.TextLightOnDark;
            _step2ActionBtnObj.SetActive(false);

            // 3. Step 3 Handheld Multi-Gas Detector Representation
            BuildAtmosphericDetectorUI(_optionsContainer, font);

            // 4. Step 4 PPE Selection Palette
            BuildPpePaletteUI(_optionsContainer, font);

            // 5. Step 5 PPE Verification Inspection
            BuildPpeVerificationUI(_optionsContainer, font);

            // 6. Step 6 Buddy / Outside Attendant System
            BuildBuddySystemUI(_optionsContainer, font);

            // 7. Step 7 Safe Entry Decision UI
            BuildEntryDecisionUI(_optionsContainer, font);

            // 8. Step 8 Emergency Response & Evacuation UI
            BuildEmergencyResponseUI(_optionsContainer, font);

            // 9. Step 9 Final Safety Check UI
            BuildFinalSafetyCheckUI(_optionsContainer, font);

            // Primary Next Button
            _nextButtonObj = new GameObject("PrimaryNextButton");
            _nextButtonObj.transform.SetParent(_actionContainer.transform, false);
            var nextRect = _nextButtonObj.AddComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(0f, 0.02f);
            nextRect.anchorMax = new Vector2(1f, 0.22f);
            nextRect.offsetMin = Vector2.zero;
            nextRect.offsetMax = Vector2.zero;

            _nextButtonBg = _nextButtonObj.AddComponent<Image>();
            _nextButtonBg.color = UITheme.PrimaryOrange;

            _nextButton = _nextButtonObj.AddComponent<Button>();
            _nextButton.targetGraphic = _nextButtonBg;

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
            _nextButtonText.fontSize = 28;
            _nextButtonText.enableAutoSizing = true;
            _nextButtonText.fontSizeMin = 20;
            _nextButtonText.fontSizeMax = 32;
            _nextButtonText.fontStyle = FontStyles.Bold;
            _nextButtonText.color = UITheme.TextLightOnDark;

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
            dBg.color = UITheme.CardBackground; // Clean light card detector housing

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
            _detectorHeaderStatus.fontSize = 22;
            _detectorHeaderStatus.alignment = TextAlignmentOptions.Center;
            _detectorHeaderStatus.color = UITheme.TextPrimary;

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
            rBg.color = UITheme.CardSecondaryBg;

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
            readoutText.fontSize = 24;
            readoutText.alignment = TextAlignmentOptions.MidlineLeft;
            readoutText.color = UITheme.TextPrimary;

            // Action button (right 35%)
            var btnObj = new GameObject("TestButton");
            btnObj.transform.SetParent(rowObj.transform, false);
            var bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.65f, 0.10f);
            bRect.anchorMax = new Vector2(0.97f, 0.90f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;

            btnBg = btnObj.AddComponent<Image>();
            btnBg.color = UITheme.PrimaryOrange;
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
            btnText.fontSize = 22;
            btnText.color = UITheme.TextLightOnDark;
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
                _o2ReadoutText.text = $"<b>O2 (Oxygen)</b>: <color=#DC2626><b>19.1 % Vol (DEFICIENT)</b></color> ✓";
                _btnTestO2.interactable = false;
                _btnTestO2Bg.color = UITheme.SuccessSurface;
                _btnTestO2Text.text = "DONE ✓";
                _btnTestO2Text.color = UITheme.SuccessText;
            }
            else
            {
                _o2ReadoutText.text = "<b>O2 (Oxygen)</b>: [ READY ] →";
                _btnTestO2.interactable = true;
                _btnTestO2Bg.color = UITheme.PrimaryOrange;
                _btnTestO2Text.text = "TEST O2";
                _btnTestO2Text.color = UITheme.TextLightOnDark;
            }

            // LEL
            if (sim.IsFlammableTested)
            {
                _lelReadoutText.text = $"<b>LEL (Combustible)</b>: <color=#DC2626><b>18.0 % LEL (HAZARDOUS)</b></color> ✓";
                _btnTestLel.interactable = false;
                _btnTestLelBg.color = UITheme.SuccessSurface;
                _btnTestLelText.text = "DONE ✓";
                _btnTestLelText.color = UITheme.SuccessText;
            }
            else if (sim.IsOxygenTested)
            {
                _lelReadoutText.text = "<b>LEL (Combustible)</b>: [ READY ] →";
                _btnTestLel.interactable = true;
                _btnTestLelBg.color = UITheme.PrimaryOrange;
                _btnTestLelText.text = "TEST LEL";
                _btnTestLelText.color = UITheme.TextLightOnDark;
            }
            else
            {
                _lelReadoutText.text = "<b>LEL (Combustible)</b>: 🔒 LOCKED";
                _btnTestLel.interactable = false;
                _btnTestLelBg.color = UITheme.CardBorder;
                _btnTestLelText.text = "LOCKED";
                _btnTestLelText.color = UITheme.TextMuted;
            }

            // H2S
            if (sim.IsToxicTested)
            {
                _h2sReadoutText.text = $"<b>H2S (Toxic)</b>: <color=#DC2626><b>35.0 ppm (LETHAL DANGER)</b></color> ✓";
                _btnTestH2s.interactable = false;
                _btnTestH2sBg.color = UITheme.SuccessSurface;
                _btnTestH2sText.text = "DONE ✓";
                _btnTestH2sText.color = UITheme.SuccessText;
            }
            else if (sim.IsFlammableTested)
            {
                _h2sReadoutText.text = "<b>H2S (Toxic)</b>: [ READY ] →";
                _btnTestH2s.interactable = true;
                _btnTestH2sBg.color = UITheme.PrimaryOrange;
                _btnTestH2sText.text = "TEST H2S";
                _btnTestH2sText.color = UITheme.TextLightOnDark;
            }
            else
            {
                _h2sReadoutText.text = "<b>H2S (Toxic)</b>: 🔒 LOCKED";
                _btnTestH2s.interactable = false;
                _btnTestH2sBg.color = UITheme.CardBorder;
                _btnTestH2sText.text = "LOCKED";
                _btnTestH2sText.color = UITheme.TextMuted;
            }

            // Assessment summary
            if (sim.IsAssessmentCompleted)
            {
                _detectorHeaderStatus.text = "<b><color=#DC2626>ATMOSPHERE: UNSAFE • DO NOT ENTER</color></b>";
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
            pBg.color = UITheme.CardBackground;

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
            _ppeHeaderWarningText.text = "<b><color=#DC2626>ATMOSPHERE: UNSAFE</color> • PPE PREPARATION REQUIRED (ENTRY PROHIBITED)</b>";
            _ppeHeaderWarningText.fontSize = 20;
            _ppeHeaderWarningText.alignment = TextAlignmentOptions.Center;
            _ppeHeaderWarningText.enableAutoSizing = true;
            _ppeHeaderWarningText.fontSizeMin = 15;
            _ppeHeaderWarningText.fontSizeMax = 22;

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
            _btnConfirmPpeBg.color = UITheme.PrimaryDisabled;
            _btnConfirmPpe = confirmBtnObj.AddComponent<Button>();
            _btnConfirmPpe.targetGraphic = _btnConfirmPpeBg;
            _btnConfirmPpe.interactable = false;

            var cbTap = confirmBtnObj.AddComponent<TapGatedButton>();
            cbTap.Initialize(() => {
                if (_controller != null && _controller.HasValidPpeSelection())
                {
                    _controller.SubmitPpeSelectionAndAdvance();
                }
            });

            var cLblObj = new GameObject("Label");
            cLblObj.transform.SetParent(confirmBtnObj.transform, false);
            var clRect = cLblObj.AddComponent<RectTransform>();
            clRect.anchorMin = Vector2.zero;
            clRect.anchorMax = Vector2.one;
            _btnConfirmPpeText = cLblObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _btnConfirmPpeText.font = font;
            _btnConfirmPpeText.text = "<b>CONFIRM PPE →</b>";
            _btnConfirmPpeText.alignment = TextAlignmentOptions.Center;
            _btnConfirmPpeText.fontSize = 22;
            _btnConfirmPpeText.color = UITheme.PrimaryDisabledText;

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
            bg.color = UITheme.CardSecondaryBg;
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
            tRect.offsetMin = new Vector2(10, 2);
            tRect.offsetMax = new Vector2(-10, -2);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = $"[  ] {defaultLabel}";
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.fontSize = 22;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 16;
            tmp.fontSizeMax = 24;
            tmp.color = UITheme.TextPrimary;

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
                _ppeHeaderWarningText.text = $"<b><color=#DC2626>{loc.Get("status_unsafe_atmosphere", "ATMOSPHERE: UNSAFE")}</color> • {loc.Get("ppe_unsafe_warning", "PPE DOES NOT MAKE AN UNSAFE ATMOSPHERE SAFE. DO NOT ENTER.")}</b>";
            }

            foreach (var kvp in _ppeButtons)
            {
                string itemId = kvp.Key;
                bool isSelected = _controller.IsPpeItemSelected(itemId);
                string itemTitle = loc.Get($"ppe_item_{itemId.Replace("ppe_", "")}", itemId);

                if (_ppeLabels.TryGetValue(itemId, out var label))
                {
                    label.text = isSelected ? $"<b><color=#16A34A>[✓]</color></b> {itemTitle}" : $"[  ] {itemTitle}";
                    label.color = isSelected ? UITheme.SuccessText : UITheme.TextPrimary;
                }

                if (_ppeItemBgs.TryGetValue(itemId, out var bg))
                {
                    bg.color = isSelected ? UITheme.SuccessSurface : UITheme.CardSecondaryBg;
                }
            }

            bool isPpeValid = _controller.HasValidPpeSelection();

            // Synchronize Primary Next button at bottom of screen
            if (_nextButtonObj != null && Navigator.CurrentStepIndex == 4)
            {
                _nextButtonObj.SetActive(true);
                if (_nextButton != null) _nextButton.interactable = isPpeValid;
                if (_nextButtonBg != null) _nextButtonBg.color = isPpeValid ? UITheme.PrimaryOrange : UITheme.PrimaryDisabled;
                if (_nextButtonText != null)
                {
                    string confirmLabel = loc.Get("ppe_btn_confirm", "CONFIRM PPE SELECTION →");
                    _nextButtonText.text = $"<b>{confirmLabel}</b>";
                    _nextButtonText.color = isPpeValid ? UITheme.TextLightOnDark : UITheme.PrimaryDisabledText;
                }
            }

            // Also synchronize palette Confirm button
            if (_btnConfirmPpe != null)
            {
                _btnConfirmPpe.interactable = isPpeValid;
            }
            if (_btnConfirmPpeBg != null)
            {
                _btnConfirmPpeBg.color = isPpeValid ? UITheme.PrimaryOrange : UITheme.PrimaryDisabled;
            }
            if (_btnConfirmPpeText != null)
            {
                string confirmLabel = loc.Get("ppe_btn_confirm", "CONFIRM PPE SELECTION →");
                _btnConfirmPpeText.text = $"<b>{confirmLabel}</b>";
                _btnConfirmPpeText.color = isPpeValid ? UITheme.TextLightOnDark : UITheme.PrimaryDisabledText;
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
            vBg.color = UITheme.CardBackground;

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
            _verifyHeaderWarningText.text = "<b><color=#D97706>EQUIPMENT READINESS INSPECTION</color>\n<size=80%>(PPE does NOT make an unsafe atmosphere safe • Entry prohibited)</size></b>";
            _verifyHeaderWarningText.fontSize = 20;
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
            rBg.color = UITheme.CardSecondaryBg;

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
            statusText.fontSize = 22;
            statusText.alignment = TextAlignmentOptions.MidlineLeft;
            statusText.color = UITheme.TextPrimary;

            var btnObj = new GameObject("VerifyButton");
            btnObj.transform.SetParent(rowObj.transform, false);
            var bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.66f, 0.10f);
            bRect.anchorMax = new Vector2(0.97f, 0.90f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;

            btnBg = btnObj.AddComponent<Image>();
            btnBg.color = UITheme.PrimaryOrange;
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
            btnText.fontSize = 20;
            btnText.color = UITheme.TextLightOnDark;
        }

        private void UpdatePpeVerificationVisuals()
        {
            if (_controller == null) return;
            var loc = LocaleService.Instance;

            if (_verifyHeaderWarningText != null)
            {
                _verifyHeaderWarningText.text = $"<b><color=#D97706>{loc.Get("verify_title", "PPE INSPECTION & VERIFICATION")}</color>\n<size=80%>({loc.Get("verify_warning", "PPE does not make atmosphere safe • Entry prohibited")})</size></b>";
            }

            // Seal
            bool sealOk = _controller.IsSealCheckPassed;
            if (_sealStatusText != null)
            {
                _sealStatusText.text = $"<b>{loc.Get("verify_scba_seal", "SCBA Seal")}</b>: {(sealOk ? "<color=#16A34A>VERIFIED ✓</color>" : "Pending")}";
            }
            if (_btnVerifySeal != null)
            {
                _btnVerifySeal.interactable = !sealOk;
                if (_btnVerifySealBg != null) _btnVerifySealBg.color = sealOk ? UITheme.SuccessSurface : UITheme.PrimaryOrange;
                if (_btnVerifySealText != null)
                {
                    _btnVerifySealText.text = sealOk ? "DONE ✓" : loc.Get("verify_btn_action", "VERIFY");
                    _btnVerifySealText.color = sealOk ? UITheme.SuccessText : UITheme.TextLightOnDark;
                }
            }

            // Harness
            bool harnessOk = _controller.IsHarnessFitPassed;
            if (_harnessStatusText != null)
            {
                _harnessStatusText.text = $"<b>{loc.Get("verify_harness_fit", "Harness Fit")}</b>: {(harnessOk ? "<color=#16A34A>VERIFIED ✓</color>" : "Pending")}";
            }
            if (_btnVerifyHarness != null)
            {
                _btnVerifyHarness.interactable = !harnessOk;
                if (_btnVerifyHarnessBg != null) _btnVerifyHarnessBg.color = harnessOk ? UITheme.SuccessSurface : UITheme.PrimaryOrange;
                if (_btnVerifyHarnessText != null)
                {
                    _btnVerifyHarnessText.text = harnessOk ? "DONE ✓" : loc.Get("verify_btn_action", "VERIFY");
                    _btnVerifyHarnessText.color = harnessOk ? UITheme.SuccessText : UITheme.TextLightOnDark;
                }
            }

            // Cylinder
            bool cylOk = _controller.IsCylinderPressurePassed;
            if (_pressureStatusText != null)
            {
                _pressureStatusText.text = $"<b>{loc.Get("verify_cylinder_pressure", "Cylinder Pressure")}</b>: {(cylOk ? "<color=#16A34A>300 BAR (OK) ✓</color>" : "Pending")}";
            }
            if (_btnCheckPressure != null)
            {
                _btnCheckPressure.interactable = !cylOk;
                if (_btnCheckPressureBg != null) _btnCheckPressureBg.color = cylOk ? UITheme.SuccessSurface : UITheme.PrimaryOrange;
                if (_btnCheckPressureText != null)
                {
                    _btnCheckPressureText.text = cylOk ? "DONE ✓" : loc.Get("verify_btn_action", "VERIFY");
                    _btnCheckPressureText.color = cylOk ? UITheme.SuccessText : UITheme.TextLightOnDark;
                }
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
            bBg.color = UITheme.CardBackground;

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
            _buddyHeaderRuleText.text = "<b><color=#D97706>SAFETY ATTENDANT MUST REMAIN OUTSIDE</color>\n<size=80%>Stationed outside danger perimeter • Maintain radio contact</size></b>";
            _buddyHeaderRuleText.fontSize = 22;
            _buddyHeaderRuleText.alignment = TextAlignmentOptions.Center;

            // Row 1: Assign Outside Attendant (y: 0.38 to 0.72)
            var attRowObj = new GameObject("Row_AssignAttendant");
            attRowObj.transform.SetParent(_buddySystemRootObj.transform, false);
            var arRect = attRowObj.AddComponent<RectTransform>();
            arRect.anchorMin = new Vector2(0.02f, 0.38f);
            arRect.anchorMax = new Vector2(0.98f, 0.72f);
            arRect.offsetMin = Vector2.zero;
            arRect.offsetMax = Vector2.zero;
            attRowObj.AddComponent<Image>().color = UITheme.CardSecondaryBg;

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
            _attendantStatusText.fontSize = 22;
            _attendantStatusText.alignment = TextAlignmentOptions.MidlineLeft;
            _attendantStatusText.color = UITheme.TextPrimary;

            var attBtnObj = new GameObject("AssignAttendantButton");
            attBtnObj.transform.SetParent(attRowObj.transform, false);
            var abRect = attBtnObj.AddComponent<RectTransform>();
            abRect.anchorMin = new Vector2(0.62f, 0.10f);
            abRect.anchorMax = new Vector2(0.98f, 0.90f);
            abRect.offsetMin = Vector2.zero;
            abRect.offsetMax = Vector2.zero;

            _btnAssignAttendantBg = attBtnObj.AddComponent<Image>();
            _btnAssignAttendantBg.color = UITheme.PrimaryOrange;
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
            _btnAssignAttendantText.fontSize = 22;
            _btnAssignAttendantText.alignment = TextAlignmentOptions.Center;
            _btnAssignAttendantText.color = UITheme.TextLightOnDark;

            // Row 2: Check Two-Way Radio (y: 0.04 to 0.36)
            var commRowObj = new GameObject("Row_CheckRadio");
            commRowObj.transform.SetParent(_buddySystemRootObj.transform, false);
            var crRect = commRowObj.AddComponent<RectTransform>();
            crRect.anchorMin = new Vector2(0.02f, 0.04f);
            crRect.anchorMax = new Vector2(0.98f, 0.36f);
            crRect.offsetMin = Vector2.zero;
            crRect.offsetMax = Vector2.zero;
            commRowObj.AddComponent<Image>().color = UITheme.CardSecondaryBg;

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
            _commStatusText.fontSize = 22;
            _commStatusText.alignment = TextAlignmentOptions.MidlineLeft;
            _commStatusText.color = UITheme.TextPrimary;

            var commBtnObj = new GameObject("CheckCommButton");
            commBtnObj.transform.SetParent(commRowObj.transform, false);
            var cbRect = commBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.62f, 0.10f);
            cbRect.anchorMax = new Vector2(0.98f, 0.90f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;

            _btnCheckCommunicationBg = commBtnObj.AddComponent<Image>();
            _btnCheckCommunicationBg.color = UITheme.PrimaryDisabled;
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
            _btnCheckCommunicationText.fontSize = 22;
            _btnCheckCommunicationText.alignment = TextAlignmentOptions.Center;
            _btnCheckCommunicationText.color = UITheme.PrimaryDisabledText;

            _buddySystemRootObj.SetActive(false);
        }

        private void UpdateBuddySystemVisuals()
        {
            if (_controller == null) return;
            var loc = LocaleService.Instance;

            if (_buddyHeaderRuleText != null)
            {
                _buddyHeaderRuleText.text = $"<b><color=#D97706>{loc.Get("attendant_title", "SAFETY ATTENDANT MUST REMAIN OUTSIDE")}</color>\n<size=80%>{loc.Get("attendant_rule", "Stationed outside danger perimeter • Maintain radio contact")}</size></b>";
            }

            bool attAssigned = _controller.IsAttendantAssigned;
            if (_attendantStatusText != null)
            {
                _attendantStatusText.text = $"<b>{loc.Get("attendant_title", "Attendant")}</b>: {(attAssigned ? $"<color=#16A34A>{loc.Get("attendant_status_assigned", "ASSIGNED (OUTSIDE) ✓")}</color>" : loc.Get("attendant_outside", "Standby outside (3.4m)"))}";
            }
            if (_btnAssignAttendant != null)
            {
                _btnAssignAttendant.interactable = !attAssigned;
                if (_btnAssignAttendantBg != null) _btnAssignAttendantBg.color = attAssigned ? UITheme.SuccessSurface : UITheme.PrimaryOrange;
                if (_btnAssignAttendantText != null)
                {
                    _btnAssignAttendantText.text = attAssigned ? "DONE ✓" : loc.Get("attendant_btn_assign", "ASSIGN");
                    _btnAssignAttendantText.color = attAssigned ? UITheme.SuccessText : UITheme.TextLightOnDark;
                }
            }

            bool commOk = _controller.IsCommunicationChecked;
            if (_commStatusText != null)
            {
                _commStatusText.text = $"<b>{loc.Get("comm_title", "Radio Link")}</b>: {(commOk ? $"<color=#16A34A>{loc.Get("comm_status_verified", "RADIO LINK VERIFIED ✓")}</color>" : (attAssigned ? "Ready to test" : "Waiting for attendant"))}";
            }
            if (_btnCheckCommunication != null)
            {
                _btnCheckCommunication.interactable = attAssigned && !commOk;
                if (_btnCheckCommunicationBg != null)
                {
                    _btnCheckCommunicationBg.color = commOk ? UITheme.SuccessSurface : (attAssigned ? UITheme.PrimaryOrange : UITheme.PrimaryDisabled);
                }
                if (_btnCheckCommunicationText != null)
                {
                    _btnCheckCommunicationText.text = commOk ? "VERIFIED ✓" : loc.Get("comm_btn_check", "CHECK RADIO");
                    _btnCheckCommunicationText.color = commOk ? UITheme.SuccessText : (attAssigned ? UITheme.TextLightOnDark : UITheme.PrimaryDisabledText);
                }
            }
        }

        private void BuildEntryDecisionUI(GameObject parent, TMP_FontAsset font)
        {
            _entryDecisionRootObj = new GameObject("EntryDecisionUI");
            _entryDecisionRootObj.transform.SetParent(parent.transform, false);
            var rect = _entryDecisionRootObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = _entryDecisionRootObj.AddComponent<Image>();
            bg.color = UITheme.CardBackground;

            var headerObj = new GameObject("EntryHeaderWarning");
            headerObj.transform.SetParent(_entryDecisionRootObj.transform, false);
            var hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.02f, 0.76f);
            hRect.anchorMax = new Vector2(0.98f, 0.98f);
            hRect.offsetMin = Vector2.zero;
            hRect.offsetMax = Vector2.zero;
            _entryHeaderWarningText = headerObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _entryHeaderWarningText.font = font;
            _entryHeaderWarningText.fontSize = 24;
            _entryHeaderWarningText.alignment = TextAlignmentOptions.Center;
            _entryHeaderWarningText.color = UITheme.DangerText;

            var promptObj = new GameObject("DecisionPrompt");
            promptObj.transform.SetParent(_entryDecisionRootObj.transform, false);
            var pRect = promptObj.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.02f, 0.48f);
            pRect.anchorMax = new Vector2(0.98f, 0.75f);
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;
            _entryDecisionPromptText = promptObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _entryDecisionPromptText.font = font;
            _entryDecisionPromptText.fontSize = 22;
            _entryDecisionPromptText.alignment = TextAlignmentOptions.Center;
            _entryDecisionPromptText.color = UITheme.TextPrimary;

            // Choice 1: DO NOT ENTER (SAFE)
            var doNotEnterObj = new GameObject("BtnDoNotEnter");
            doNotEnterObj.transform.SetParent(_entryDecisionRootObj.transform, false);
            var dneRect = doNotEnterObj.AddComponent<RectTransform>();
            dneRect.anchorMin = new Vector2(0.03f, 0.24f);
            dneRect.anchorMax = new Vector2(0.48f, 0.46f);
            dneRect.offsetMin = Vector2.zero;
            dneRect.offsetMax = Vector2.zero;
            _btnDoNotEnterBg = doNotEnterObj.AddComponent<Image>();
            _btnDoNotEnterBg.color = UITheme.PrimaryOrange;
            _btnDoNotEnter = doNotEnterObj.AddComponent<Button>();
            _btnDoNotEnter.targetGraphic = _btnDoNotEnterBg;
            Action doNotEnterAction = () => {
                if (_controller != null)
                {
                    _controller.SubmitEntryDecision(false);
                    UpdateEntryDecisionVisuals();
                }
            };
            _btnDoNotEnter.onClick.AddListener(() => doNotEnterAction());
            var dneTap = doNotEnterObj.AddComponent<TapGatedButton>();
            dneTap.Initialize(doNotEnterAction);
            var dneLabelObj = new GameObject("Label");
            dneLabelObj.transform.SetParent(doNotEnterObj.transform, false);
            var dneLabelRect = dneLabelObj.AddComponent<RectTransform>();
            dneLabelRect.anchorMin = Vector2.zero;
            dneLabelRect.anchorMax = Vector2.one;
            _btnDoNotEnterText = dneLabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _btnDoNotEnterText.font = font;
            _btnDoNotEnterText.text = "<b>DO NOT ENTER\n<size=80%>[CORRECT PROTOCOL]</size></b>";
            _btnDoNotEnterText.alignment = TextAlignmentOptions.Center;
            _btnDoNotEnterText.fontSize = 22;
            _btnDoNotEnterText.color = UITheme.TextLightOnDark;

            // Choice 2: ENTER CONFINED SPACE (UNSAFE)
            var enterObj = new GameObject("BtnEnterConfinedSpace");
            enterObj.transform.SetParent(_entryDecisionRootObj.transform, false);
            var eRect = enterObj.AddComponent<RectTransform>();
            eRect.anchorMin = new Vector2(0.52f, 0.24f);
            eRect.anchorMax = new Vector2(0.97f, 0.46f);
            eRect.offsetMin = Vector2.zero;
            eRect.offsetMax = Vector2.zero;
            _btnEnterConfinedSpaceBg = enterObj.AddComponent<Image>();
            _btnEnterConfinedSpaceBg.color = UITheme.CardSecondaryBg;
            _btnEnterConfinedSpace = enterObj.AddComponent<Button>();
            _btnEnterConfinedSpace.targetGraphic = _btnEnterConfinedSpaceBg;
            Action enterAction = () => {
                if (_controller != null)
                {
                    _controller.SubmitEntryDecision(true);
                    UpdateEntryDecisionVisuals();
                }
            };
            _btnEnterConfinedSpace.onClick.AddListener(() => enterAction());
            var eTap = enterObj.AddComponent<TapGatedButton>();
            eTap.Initialize(enterAction);
            var eLabelObj = new GameObject("Label");
            eLabelObj.transform.SetParent(enterObj.transform, false);
            var eLabelRect = eLabelObj.AddComponent<RectTransform>();
            eLabelRect.anchorMin = Vector2.zero;
            eLabelRect.anchorMax = Vector2.one;
            _btnEnterConfinedSpaceText = eLabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _btnEnterConfinedSpaceText.font = font;
            _btnEnterConfinedSpaceText.text = "<b>ENTER CONFINED\n<size=80%>SPACE [RISK]</size></b>";
            _btnEnterConfinedSpaceText.alignment = TextAlignmentOptions.Center;
            _btnEnterConfinedSpaceText.fontSize = 22;
            _btnEnterConfinedSpaceText.color = UITheme.TextPrimary;

            var fbObj = new GameObject("EntryDecisionFeedback");
            fbObj.transform.SetParent(_entryDecisionRootObj.transform, false);
            var fbRect = fbObj.AddComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.02f, 0.02f);
            fbRect.anchorMax = new Vector2(0.98f, 0.22f);
            fbRect.offsetMin = Vector2.zero;
            fbRect.offsetMax = Vector2.zero;
            _entryDecisionFeedbackText = fbObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _entryDecisionFeedbackText.font = font;
            _entryDecisionFeedbackText.fontSize = 20;
            _entryDecisionFeedbackText.alignment = TextAlignmentOptions.Center;
            _entryDecisionFeedbackText.color = UITheme.TextSecondary;

            _entryDecisionRootObj.SetActive(false);
        }

        private void UpdateEntryDecisionVisuals()
        {
            if (_controller == null) return;
            var loc = LocaleService.Instance;

            if (_entryHeaderWarningText != null)
            {
                _entryHeaderWarningText.text = $"<b><color=#DC2626>{loc.Get("entry_decision_header", "ATMOSPHERIC HAZARD DETECTED • SAFE ENTRY EVALUATION")}</color></b>";
            }

            if (_entryDecisionPromptText != null)
            {
                _entryDecisionPromptText.text = loc.Get("entry_decision_prompt", "O2: 19.1% (Low) | LEL: 18.0% (Risk) | H2S: 35 ppm (Lethal)\n<b>ATMOSPHERE IS UNSAFE. SELECT WORKER ACTION:</b>");
            }

            if (_btnDoNotEnterText != null)
            {
                _btnDoNotEnterText.text = $"<b>{loc.Get("btn_do_not_enter", "DO NOT ENTER — UNSAFE")}</b>";
            }

            if (_btnEnterConfinedSpaceText != null)
            {
                _btnEnterConfinedSpaceText.text = $"<b>{loc.Get("btn_enter_confined_space", "ENTER CONFINED SPACE")}</b>";
            }

            if (_controller.IsEntryDecisionMade)
            {
                if (string.Equals(_controller.EntryDecisionResult, "do_not_enter", StringComparison.OrdinalIgnoreCase))
                {
                    if (_btnDoNotEnterBg != null) _btnDoNotEnterBg.color = UITheme.SuccessSurface;
                    if (_btnDoNotEnterText != null) _btnDoNotEnterText.color = UITheme.SuccessText;
                    if (_btnEnterConfinedSpace != null) _btnEnterConfinedSpace.interactable = false;
                    if (_btnEnterConfinedSpaceBg != null) _btnEnterConfinedSpaceBg.color = UITheme.CardSecondaryBg;
                    if (_entryDecisionFeedbackText != null)
                    {
                        _entryDecisionFeedbackText.text = $"<b><color=#16A34A>{loc.Get("entry_decision_safe_success", "CORRECT DECISION: Confined space entry prohibited under unsafe atmospheric conditions. Proceed to emergency response.")}</color></b>";
                    }
                }
                else
                {
                    if (_btnEnterConfinedSpaceBg != null) _btnEnterConfinedSpaceBg.color = UITheme.DangerSurface;
                    if (_btnEnterConfinedSpaceText != null) _btnEnterConfinedSpaceText.color = UITheme.DangerText;
                    if (_entryDecisionFeedbackText != null)
                    {
                        _entryDecisionFeedbackText.text = $"<b><color=#DC2626>{loc.Get("entry_decision_unsafe_warning", "CRITICAL SAFETY VIOLATION: Atmosphere is lethal and flammable! Never enter an unsafe confined space. Correct your decision now.")}</color></b>";
                    }
                }
            }
            else
            {
                if (_btnDoNotEnterBg != null) _btnDoNotEnterBg.color = UITheme.PrimaryOrange;
                if (_btnDoNotEnterText != null) _btnDoNotEnterText.color = UITheme.TextLightOnDark;
                if (_btnEnterConfinedSpaceBg != null) _btnEnterConfinedSpaceBg.color = UITheme.CardSecondaryBg;
                if (_btnEnterConfinedSpaceText != null) _btnEnterConfinedSpaceText.color = UITheme.TextPrimary;
                if (_btnEnterConfinedSpace != null) _btnEnterConfinedSpace.interactable = true;
                if (_entryDecisionFeedbackText != null)
                {
                    _entryDecisionFeedbackText.text = loc.Get("entry_decision_hint", "Evaluate multi-gas detector readings before making entry decision.");
                }
            }
        }

        private void BuildEmergencyResponseUI(GameObject parent, TMP_FontAsset font)
        {
            _emergencyResponseRootObj = new GameObject("EmergencyResponseUI");
            _emergencyResponseRootObj.transform.SetParent(parent.transform, false);
            var rect = _emergencyResponseRootObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = _emergencyResponseRootObj.AddComponent<Image>();
            bg.color = UITheme.CardBackground;

            var titleObj = new GameObject("EmergencyHeader");
            titleObj.transform.SetParent(_emergencyResponseRootObj.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.02f, 0.82f);
            tRect.anchorMax = new Vector2(0.98f, 0.98f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            _emergencyHeaderTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _emergencyHeaderTitleText.font = font;
            _emergencyHeaderTitleText.fontSize = 24;
            _emergencyHeaderTitleText.alignment = TextAlignmentOptions.Center;
            _emergencyHeaderTitleText.color = UITheme.DangerText;

            // Sub-panel 1: Gas Alarm (8A)
            _gasAlarmSubPanel = new GameObject("SubPanel_Alarm");
            _gasAlarmSubPanel.transform.SetParent(_emergencyResponseRootObj.transform, false);
            var aRect = _gasAlarmSubPanel.AddComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0.02f, 0.04f);
            aRect.anchorMax = new Vector2(0.98f, 0.80f);
            aRect.offsetMin = Vector2.zero;
            aRect.offsetMax = Vector2.zero;

            var alarmMsgObj = new GameObject("AlarmMessage");
            alarmMsgObj.transform.SetParent(_gasAlarmSubPanel.transform, false);
            var amRect = alarmMsgObj.AddComponent<RectTransform>();
            amRect.anchorMin = new Vector2(0.04f, 0.45f);
            amRect.anchorMax = new Vector2(0.96f, 0.95f);
            amRect.offsetMin = Vector2.zero;
            amRect.offsetMax = Vector2.zero;
            var alarmMsg = alarmMsgObj.AddComponent<TextMeshProUGUI>();
            if (font != null) alarmMsg.font = font;
            alarmMsg.text = "<b><color=#DC2626>GAS ALARM SOUNDING!</color></b>\n<size=85%>Audible & visual alarms activated. Multi-gas detector threshold exceeded!</size>";
            alarmMsg.alignment = TextAlignmentOptions.Center;
            alarmMsg.fontSize = 22;
            alarmMsg.color = UITheme.TextPrimary;

            var ackBtnObj = new GameObject("BtnAcknowledgeAlarm");
            ackBtnObj.transform.SetParent(_gasAlarmSubPanel.transform, false);
            var abRect = ackBtnObj.AddComponent<RectTransform>();
            abRect.anchorMin = new Vector2(0.05f, 0.08f);
            abRect.anchorMax = new Vector2(0.95f, 0.40f);
            abRect.offsetMin = Vector2.zero;
            abRect.offsetMax = Vector2.zero;
            _btnAcknowledgeAlarmBg = ackBtnObj.AddComponent<Image>();
            _btnAcknowledgeAlarmBg.color = UITheme.PrimaryOrange;
            _btnAcknowledgeAlarm = ackBtnObj.AddComponent<Button>();
            _btnAcknowledgeAlarm.targetGraphic = _btnAcknowledgeAlarmBg;
            Action ackAction = () => {
                if (_controller != null)
                {
                    _controller.AcknowledgeGasAlarm();
                    UpdateEmergencyResponseVisuals();
                }
            };
            _btnAcknowledgeAlarm.onClick.AddListener(() => ackAction());
            var ackTap = ackBtnObj.AddComponent<TapGatedButton>();
            ackTap.Initialize(ackAction);
            var ackLabelObj = new GameObject("Label");
            ackLabelObj.transform.SetParent(ackBtnObj.transform, false);
            var alRect = ackLabelObj.AddComponent<RectTransform>();
            alRect.anchorMin = Vector2.zero;
            alRect.anchorMax = Vector2.one;
            _btnAcknowledgeAlarmText = ackLabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _btnAcknowledgeAlarmText.font = font;
            _btnAcknowledgeAlarmText.text = "<b>ACKNOWLEDGE ALARM →</b>";
            _btnAcknowledgeAlarmText.alignment = TextAlignmentOptions.Center;
            _btnAcknowledgeAlarmText.fontSize = 26;
            _btnAcknowledgeAlarmText.color = UITheme.TextLightOnDark;

            // Sub-panel 2: Stop Work (8B)
            _stopWorkSubPanel = new GameObject("SubPanel_StopWork");
            _stopWorkSubPanel.transform.SetParent(_emergencyResponseRootObj.transform, false);
            var swRect = _stopWorkSubPanel.AddComponent<RectTransform>();
            swRect.anchorMin = new Vector2(0.02f, 0.04f);
            swRect.anchorMax = new Vector2(0.98f, 0.80f);
            swRect.offsetMin = Vector2.zero;
            swRect.offsetMax = Vector2.zero;

            var swMsgObj = new GameObject("StopWorkMessage");
            swMsgObj.transform.SetParent(_stopWorkSubPanel.transform, false);
            var swmRect = swMsgObj.AddComponent<RectTransform>();
            swmRect.anchorMin = new Vector2(0.04f, 0.45f);
            swmRect.anchorMax = new Vector2(0.96f, 0.95f);
            swmRect.offsetMin = Vector2.zero;
            swmRect.offsetMax = Vector2.zero;
            var swMsg = swMsgObj.AddComponent<TextMeshProUGUI>();
            if (font != null) swMsg.font = font;
            swMsg.text = "<b><color=#D97706>STOP WORK ORDER ISSUED</color></b>\n<size=85%>Halt all hot work & entry. Keep all personnel outside danger perimeter!</size>";
            swMsg.alignment = TextAlignmentOptions.Center;
            swMsg.fontSize = 22;
            swMsg.color = UITheme.TextPrimary;

            var swBtnObj = new GameObject("BtnAcknowledgeStopWork");
            swBtnObj.transform.SetParent(_stopWorkSubPanel.transform, false);
            var swbRect = swBtnObj.AddComponent<RectTransform>();
            swbRect.anchorMin = new Vector2(0.05f, 0.08f);
            swbRect.anchorMax = new Vector2(0.95f, 0.40f);
            swbRect.offsetMin = Vector2.zero;
            swbRect.offsetMax = Vector2.zero;
            _btnAcknowledgeStopWorkBg = swBtnObj.AddComponent<Image>();
            _btnAcknowledgeStopWorkBg.color = UITheme.PrimaryOrange;
            _btnAcknowledgeStopWork = swBtnObj.AddComponent<Button>();
            _btnAcknowledgeStopWork.targetGraphic = _btnAcknowledgeStopWorkBg;
            Action swAction = () => {
                if (_controller != null)
                {
                    _controller.AcknowledgeStopWork();
                    UpdateEmergencyResponseVisuals();
                }
            };
            _btnAcknowledgeStopWork.onClick.AddListener(() => swAction());
            var swTap = swBtnObj.AddComponent<TapGatedButton>();
            swTap.Initialize(swAction);
            var swLabelObj = new GameObject("Label");
            swLabelObj.transform.SetParent(swBtnObj.transform, false);
            var swlRect = swLabelObj.AddComponent<RectTransform>();
            swlRect.anchorMin = Vector2.zero;
            swlRect.anchorMax = Vector2.one;
            _btnAcknowledgeStopWorkText = swLabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _btnAcknowledgeStopWorkText.font = font;
            _btnAcknowledgeStopWorkText.text = "<b>STOP WORK & KEEP OUT →</b>";
            _btnAcknowledgeStopWorkText.alignment = TextAlignmentOptions.Center;
            _btnAcknowledgeStopWorkText.fontSize = 26;
            _btnAcknowledgeStopWorkText.color = UITheme.TextLightOnDark;

            // Sub-panel 3: Supervisor Alert (8C)
            _supervisorAlertSubPanel = new GameObject("SubPanel_SupervisorAlert");
            _supervisorAlertSubPanel.transform.SetParent(_emergencyResponseRootObj.transform, false);
            var saRect = _supervisorAlertSubPanel.AddComponent<RectTransform>();
            saRect.anchorMin = new Vector2(0.02f, 0.04f);
            saRect.anchorMax = new Vector2(0.98f, 0.80f);
            saRect.offsetMin = Vector2.zero;
            saRect.offsetMax = Vector2.zero;

            var saMsgObj = new GameObject("SupervisorAlertMessage");
            saMsgObj.transform.SetParent(_supervisorAlertSubPanel.transform, false);
            var samRect = saMsgObj.AddComponent<RectTransform>();
            samRect.anchorMin = new Vector2(0.04f, 0.45f);
            samRect.anchorMax = new Vector2(0.96f, 0.95f);
            samRect.offsetMin = Vector2.zero;
            samRect.offsetMax = Vector2.zero;
            var saMsg = saMsgObj.AddComponent<TextMeshProUGUI>();
            if (font != null) saMsg.font = font;
            saMsg.text = "<b><color=#D97706>EMERGENCY NOTIFICATION</color></b>\n<size=85%>Inform outside attendant & call safety control room / emergency services.</size>";
            saMsg.alignment = TextAlignmentOptions.Center;
            saMsg.fontSize = 22;
            saMsg.color = UITheme.TextPrimary;

            var saBtnObj = new GameObject("BtnAlertSupervisor");
            saBtnObj.transform.SetParent(_supervisorAlertSubPanel.transform, false);
            var sabRect = saBtnObj.AddComponent<RectTransform>();
            sabRect.anchorMin = new Vector2(0.05f, 0.08f);
            sabRect.anchorMax = new Vector2(0.95f, 0.40f);
            sabRect.offsetMin = Vector2.zero;
            sabRect.offsetMax = Vector2.zero;
            _btnAlertSupervisorBg = saBtnObj.AddComponent<Image>();
            _btnAlertSupervisorBg.color = UITheme.PrimaryOrange;
            _btnAlertSupervisor = saBtnObj.AddComponent<Button>();
            _btnAlertSupervisor.targetGraphic = _btnAlertSupervisorBg;
            Action saAction = () => {
                if (_controller != null)
                {
                    _controller.AlertEmergencySupervisor();
                    UpdateEmergencyResponseVisuals();
                }
            };
            _btnAlertSupervisor.onClick.AddListener(() => saAction());
            var saTap = saBtnObj.AddComponent<TapGatedButton>();
            saTap.Initialize(saAction);
            var saLabelObj = new GameObject("Label");
            saLabelObj.transform.SetParent(saBtnObj.transform, false);
            var salRect = saLabelObj.AddComponent<RectTransform>();
            salRect.anchorMin = Vector2.zero;
            salRect.anchorMax = Vector2.one;
            _btnAlertSupervisorText = saLabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _btnAlertSupervisorText.font = font;
            _btnAlertSupervisorText.text = "<b>ALERT SUPERVISOR & ATTENDANT →</b>";
            _btnAlertSupervisorText.alignment = TextAlignmentOptions.Center;
            _btnAlertSupervisorText.fontSize = 24;
            _btnAlertSupervisorText.color = UITheme.TextLightOnDark;

            // Sub-panel 4: Upwind Evacuation Waypoints (8D)
            _evacuationWaypointsSubPanel = new GameObject("SubPanel_EvacuationWaypoints");
            _evacuationWaypointsSubPanel.transform.SetParent(_emergencyResponseRootObj.transform, false);
            var ewRect = _evacuationWaypointsSubPanel.AddComponent<RectTransform>();
            ewRect.anchorMin = new Vector2(0.02f, 0.04f);
            ewRect.anchorMax = new Vector2(0.98f, 0.80f);
            ewRect.offsetMin = Vector2.zero;
            ewRect.offsetMax = Vector2.zero;

            var epObj = new GameObject("EvacuationProgressText");
            epObj.transform.SetParent(_evacuationWaypointsSubPanel.transform, false);
            var epRect = epObj.AddComponent<RectTransform>();
            epRect.anchorMin = new Vector2(0.04f, 0.45f);
            epRect.anchorMax = new Vector2(0.96f, 0.95f);
            epRect.offsetMin = Vector2.zero;
            epRect.offsetMax = Vector2.zero;
            _evacuationProgressText = epObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _evacuationProgressText.font = font;
            _evacuationProgressText.text = "<b>EVACUATE UPWIND</b>\n<size=85%>Follow green waypoint markers in 3D AR or tap button below.</size>";
            _evacuationProgressText.alignment = TextAlignmentOptions.Center;
            _evacuationProgressText.fontSize = 22;
            _evacuationProgressText.color = UITheme.TextPrimary;

            var wpBtnObj = new GameObject("BtnWaypointAdvance");
            wpBtnObj.transform.SetParent(_evacuationWaypointsSubPanel.transform, false);
            var wpbRect = wpBtnObj.AddComponent<RectTransform>();
            wpbRect.anchorMin = new Vector2(0.05f, 0.08f);
            wpbRect.anchorMax = new Vector2(0.95f, 0.40f);
            wpbRect.offsetMin = Vector2.zero;
            wpbRect.offsetMax = Vector2.zero;
            _btnWaypointAdvanceBg = wpBtnObj.AddComponent<Image>();
            _btnWaypointAdvanceBg.color = UITheme.PrimaryOrange;
            _btnWaypointAdvance = wpBtnObj.AddComponent<Button>();
            _btnWaypointAdvance.targetGraphic = _btnWaypointAdvanceBg;
            Action wpAction = () => {
                if (_controller != null)
                {
                    _controller.ProcessEvacuationWaypointTap(_controller.CurrentWaypointIndex);
                    UpdateEmergencyResponseVisuals();
                }
            };
            _btnWaypointAdvance.onClick.AddListener(() => wpAction());
            var wpTap = wpBtnObj.AddComponent<TapGatedButton>();
            wpTap.Initialize(wpAction);
            var wpLabelObj = new GameObject("Label");
            wpLabelObj.transform.SetParent(wpBtnObj.transform, false);
            var wplRect = wpLabelObj.AddComponent<RectTransform>();
            wplRect.anchorMin = Vector2.zero;
            wplRect.anchorMax = Vector2.one;
            _btnWaypointAdvanceText = wpLabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _btnWaypointAdvanceText.font = font;
            _btnWaypointAdvanceText.text = "<b>REACH WAYPOINT 1 →</b>";
            _btnWaypointAdvanceText.alignment = TextAlignmentOptions.Center;
            _btnWaypointAdvanceText.fontSize = 26;
            _btnWaypointAdvanceText.color = UITheme.TextLightOnDark;

            // Sub-panel 5: Trained Rescue Confirmation (8E)
            _trainedRescueSubPanel = new GameObject("SubPanel_TrainedRescue");
            _trainedRescueSubPanel.transform.SetParent(_emergencyResponseRootObj.transform, false);
            var trRect = _trainedRescueSubPanel.AddComponent<RectTransform>();
            trRect.anchorMin = new Vector2(0.02f, 0.04f);
            trRect.anchorMax = new Vector2(0.98f, 0.80f);
            trRect.offsetMin = Vector2.zero;
            trRect.offsetMax = Vector2.zero;

            var rcObj = new GameObject("RescueChecklistText");
            rcObj.transform.SetParent(_trainedRescueSubPanel.transform, false);
            var rcRect = rcObj.AddComponent<RectTransform>();
            rcRect.anchorMin = new Vector2(0.04f, 0.45f);
            rcRect.anchorMax = new Vector2(0.96f, 0.95f);
            rcRect.offsetMin = Vector2.zero;
            rcRect.offsetMax = Vector2.zero;
            _rescueChecklistText = rcObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _rescueChecklistText.font = font;
            _rescueChecklistText.text = "<b><color=#D97706>STRICT RULE: NO IMPROVISED RESCUE</color></b>\n<size=85%>Never enter confined space without breathing equipment to attempt rescue. Wait for certified emergency rescue team!</size>";
            _rescueChecklistText.alignment = TextAlignmentOptions.Center;
            _rescueChecklistText.fontSize = 22;
            _rescueChecklistText.color = UITheme.TextPrimary;

            var crBtnObj = new GameObject("BtnConfirmRescue");
            crBtnObj.transform.SetParent(_trainedRescueSubPanel.transform, false);
            var crbRect = crBtnObj.AddComponent<RectTransform>();
            crbRect.anchorMin = new Vector2(0.05f, 0.08f);
            crbRect.anchorMax = new Vector2(0.95f, 0.40f);
            crbRect.offsetMin = Vector2.zero;
            crbRect.offsetMax = Vector2.zero;
            _btnConfirmRescueBg = crBtnObj.AddComponent<Image>();
            _btnConfirmRescueBg.color = UITheme.PrimaryOrange;
            _btnConfirmRescue = crBtnObj.AddComponent<Button>();
            _btnConfirmRescue.targetGraphic = _btnConfirmRescueBg;
            Action crAction = () => {
                if (_controller != null)
                {
                    _controller.ConfirmTrainedRescueResponse();
                    UpdateEmergencyResponseVisuals();
                }
            };
            _btnConfirmRescue.onClick.AddListener(() => crAction());
            var crTap = crBtnObj.AddComponent<TapGatedButton>();
            crTap.Initialize(crAction);
            var crLabelObj = new GameObject("Label");
            crLabelObj.transform.SetParent(crBtnObj.transform, false);
            var crlRect = crLabelObj.AddComponent<RectTransform>();
            crlRect.anchorMin = Vector2.zero;
            crlRect.anchorMax = Vector2.one;
            _btnConfirmRescueText = crLabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _btnConfirmRescueText.font = font;
            _btnConfirmRescueText.text = "<b>CONFIRM TRAINED RESCUE PROTOCOL →</b>";
            _btnConfirmRescueText.alignment = TextAlignmentOptions.Center;
            _btnConfirmRescueText.fontSize = 24;
            _btnConfirmRescueText.color = UITheme.TextLightOnDark;

            _emergencyResponseRootObj.SetActive(false);
        }

        private void UpdateEmergencyResponseVisuals()
        {
            if (_controller == null) return;
            var loc = LocaleService.Instance;

            if (_emergencyHeaderTitleText != null)
            {
                _emergencyHeaderTitleText.text = $"<b><color=#DC2626>{loc.Get("emergency_title", "STEP 8: EMERGENCY RESPONSE & EVACUATION")}</color></b>";
            }

            bool alarmAck = _controller.IsGasAlarmAcknowledged;
            bool stopWork = _controller.IsStopWorkAcknowledged;
            bool alertSuper = _controller.IsSupervisorAlerted;
            bool safeArea = _controller.IsSafeAreaReached;
            bool emergencyDone = _controller.IsEmergencyProcedureCompleted;

            if (_gasAlarmSubPanel != null) _gasAlarmSubPanel.SetActive(!alarmAck);
            if (_stopWorkSubPanel != null) _stopWorkSubPanel.SetActive(alarmAck && !stopWork);
            if (_supervisorAlertSubPanel != null) _supervisorAlertSubPanel.SetActive(stopWork && !alertSuper);
            if (_evacuationWaypointsSubPanel != null) _evacuationWaypointsSubPanel.SetActive(alertSuper && !safeArea);
            if (_trainedRescueSubPanel != null) _trainedRescueSubPanel.SetActive(safeArea);

            if (_evacuationWaypointsSubPanel != null && _evacuationWaypointsSubPanel.activeSelf)
            {
                int currentWp = _controller.CurrentWaypointIndex;
                if (_evacuationProgressText != null)
                {
                    _evacuationProgressText.text = $"<b>{loc.Get("evac_title", "EVACUATE UPWIND ALONG DESIGNATED ROUTE")}</b>\n<size=85%>{loc.Get("evac_wp_prompt", "Waypoint")} {Mathf.Clamp(currentWp, 1, 3)} / 3 • {loc.Get("evac_wind_dir", "Wind: UPWIND (Follow Green Arrow)")}</size>";
                }
                if (_btnWaypointAdvanceText != null)
                {
                    string wpName = currentWp == 1 ? loc.Get("evac_wp1", "Exit Danger Perimeter") :
                                    currentWp == 2 ? loc.Get("evac_wp2", "Upwind Cross-Path") :
                                    loc.Get("evac_wp3", "Safe Assembly Area");
                    _btnWaypointAdvanceText.text = $"<b>{loc.Get("evac_btn_advance", "REACH")} {wpName} →</b>";
                }
            }

            if (_trainedRescueSubPanel != null && _trainedRescueSubPanel.activeSelf)
            {
                if (_rescueChecklistText != null)
                {
                    _rescueChecklistText.text = emergencyDone ?
                        $"<b><color=#16A34A>{loc.Get("rescue_confirmed", "TRAINED RESCUE PROTOCOL CONFIRMED ✓\nAll personnel accounted for at safe assembly point.")}</color></b>" :
                        $"<b><color=#D97706>{loc.Get("rescue_rule_title", "CRITICAL RULE: NO IMPROVISED RESCUE")}</color></b>\n<size=85%>{loc.Get("rescue_rule_desc", "Never re-enter without certified rescue team & breathing apparatus.")}</size>";
                }
                if (_btnConfirmRescue != null)
                {
                    _btnConfirmRescue.interactable = !emergencyDone;
                    if (_btnConfirmRescueBg != null) _btnConfirmRescueBg.color = emergencyDone ? UITheme.SuccessSurface : UITheme.PrimaryOrange;
                    if (_btnConfirmRescueText != null)
                    {
                        _btnConfirmRescueText.text = emergencyDone ? "COMPLETED ✓" : loc.Get("rescue_btn_confirm", "CONFIRM TRAINED RESCUE PROTOCOL");
                        _btnConfirmRescueText.color = emergencyDone ? UITheme.SuccessText : UITheme.TextLightOnDark;
                    }
                }
            }
        }

        private void BuildFinalSafetyCheckUI(GameObject parent, TMP_FontAsset font)
        {
            _finalSafetyCheckRootObj = new GameObject("FinalSafetyCheckUI");
            _finalSafetyCheckRootObj.transform.SetParent(parent.transform, false);
            var rect = _finalSafetyCheckRootObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = _finalSafetyCheckRootObj.AddComponent<Image>();
            bg.color = UITheme.CardBackground;

            var titleObj = new GameObject("FinalHeaderTitle");
            titleObj.transform.SetParent(_finalSafetyCheckRootObj.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.02f, 0.85f);
            tRect.anchorMax = new Vector2(0.98f, 0.98f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            _finalHeaderTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _finalHeaderTitleText.font = font;
            _finalHeaderTitleText.fontSize = 24;
            _finalHeaderTitleText.alignment = TextAlignmentOptions.Center;
            _finalHeaderTitleText.color = UITheme.SuccessText;

            var clObj = new GameObject("FinalChecklistText");
            clObj.transform.SetParent(_finalSafetyCheckRootObj.transform, false);
            var clRect = clObj.AddComponent<RectTransform>();
            clRect.anchorMin = new Vector2(0.04f, 0.36f);
            clRect.anchorMax = new Vector2(0.96f, 0.84f);
            clRect.offsetMin = Vector2.zero;
            clRect.offsetMax = Vector2.zero;
            _finalChecklistText = clObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _finalChecklistText.font = font;
            _finalChecklistText.fontSize = 20;
            _finalChecklistText.lineSpacing = 1.15f;
            _finalChecklistText.alignment = TextAlignmentOptions.TopLeft;
            _finalChecklistText.color = UITheme.TextPrimary;
            var sbInit = new System.Text.StringBuilder();
            sbInit.AppendLine("✓ 1. Gas Hazard Identified (Methane & Toxic Plume)");
            sbInit.AppendLine("✓ 2. 3m Safety Danger Perimeter Established");
            sbInit.AppendLine("✓ 3. Handheld Atmospheric Testing Completed (O2 -> LEL -> H2S)");
            sbInit.AppendLine("✓ 4. Mandatory PPE Selected (SCBA, Chem Suit, Harness, Monitor, Radio)");
            sbInit.AppendLine("✓ 5. PPE Verification Verified (Face Seal, Harness, Cylinder 300 Bar)");
            sbInit.AppendLine("✓ 6. Outside Attendant Stationed & Radio Link Operational");
            sbInit.AppendLine("✓ 7. Entry Decision Evaluated: DO NOT ENTER (Atmosphere UNSAFE)");
            sbInit.AppendLine("✓ 8. Gas Alarm Acknowledged & Stop-Work Order Enforced");
            sbInit.AppendLine("✓ 9. Attendant & Supervisor Alerted via Emergency Radio");
            sbInit.AppendLine("✓ 10. Upwind Evacuation Completed (Waypoints 1-3) & Rescue Confirmed");
            _finalChecklistText.text = sbInit.ToString();

            var ruleObj = new GameObject("FinalAtmosphereRule");
            ruleObj.transform.SetParent(_finalSafetyCheckRootObj.transform, false);
            var rRect = ruleObj.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0.04f, 0.22f);
            rRect.anchorMax = new Vector2(0.96f, 0.35f);
            rRect.offsetMin = Vector2.zero;
            rRect.offsetMax = Vector2.zero;
            _finalAtmosphereRuleText = ruleObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _finalAtmosphereRuleText.font = font;
            _finalAtmosphereRuleText.fontSize = 20;
            _finalAtmosphereRuleText.alignment = TextAlignmentOptions.Center;
            _finalAtmosphereRuleText.color = UITheme.DangerText;
            _finalAtmosphereRuleText.text = "<b>FINAL ATMOSPHERE STATUS: UNSAFE (LEL 18.0%, H2S 35 ppm)\nCONFINED SPACE ENTRY PROHIBITED • 10/10 PROTOCOLS VERIFIED</b>";

            var completeBtnObj = new GameObject("BtnCompleteTraining");
            completeBtnObj.transform.SetParent(_finalSafetyCheckRootObj.transform, false);
            var cbRect = completeBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.05f, 0.03f);
            cbRect.anchorMax = new Vector2(0.95f, 0.20f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;
            _btnCompleteTrainingBg = completeBtnObj.AddComponent<Image>();
            _btnCompleteTrainingBg.color = UITheme.PrimaryOrange;
            _btnCompleteTraining = completeBtnObj.AddComponent<Button>();
            _btnCompleteTraining.targetGraphic = _btnCompleteTrainingBg;
            Action compAction = () => {
                if (_controller != null)
                {
                    _controller.CompleteGasTraining();
                    ShowGasAssessmentSummary();
                }
            };
            _btnCompleteTraining.onClick.AddListener(() => compAction());
            var compTap = completeBtnObj.AddComponent<TapGatedButton>();
            compTap.Initialize(compAction);
            var cbLabelObj = new GameObject("Label");
            cbLabelObj.transform.SetParent(completeBtnObj.transform, false);
            var cblRect = cbLabelObj.AddComponent<RectTransform>();
            cblRect.anchorMin = Vector2.zero;
            cblRect.anchorMax = Vector2.one;
            _btnCompleteTrainingText = cbLabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _btnCompleteTrainingText.font = font;
            _btnCompleteTrainingText.text = "<b>COMPLETE TRAINING & VIEW ASSESSMENT →</b>";
            _btnCompleteTrainingText.alignment = TextAlignmentOptions.Center;
            _btnCompleteTrainingText.fontSize = 24;
            _btnCompleteTrainingText.color = UITheme.TextLightOnDark;

            _finalSafetyCheckRootObj.SetActive(false);
        }

        private void UpdateFinalSafetyCheckVisuals()
        {
            var loc = LocaleService.Instance;

            if (_finalHeaderTitleText != null)
            {
                _finalHeaderTitleText.text = $"<b>{loc.Get("final_check_title", "CONFINED SPACE SAFETY AUDIT & COMPLIANCE SUMMARY")}</b>";
            }

            if (_finalChecklistText != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("✓ 1. Gas Hazard Identified (Methane & Toxic Plume)");
                sb.AppendLine("✓ 2. 3m Safety Danger Perimeter Established");
                sb.AppendLine("✓ 3. Handheld Atmospheric Testing Completed (O2 -> LEL -> H2S)");
                sb.AppendLine("✓ 4. Mandatory PPE Selected (SCBA, Chem Suit, Harness, Monitor, Radio)");
                sb.AppendLine("✓ 5. PPE Verification Verified (Face Seal, Harness, Cylinder 300 Bar)");
                sb.AppendLine("✓ 6. Outside Attendant Stationed & Radio Link Operational");
                sb.AppendLine("✓ 7. Entry Decision Evaluated: DO NOT ENTER (Atmosphere UNSAFE)");
                sb.AppendLine("✓ 8. Gas Alarm Acknowledged & Stop-Work Order Enforced");
                sb.AppendLine("✓ 9. Attendant & Supervisor Alerted via Emergency Radio");
                sb.AppendLine("✓ 10. Upwind Evacuation Completed (Waypoints 1-3) & Rescue Confirmed");
                _finalChecklistText.text = sb.ToString();
            }

            if (_finalAtmosphereRuleText != null)
            {
                _finalAtmosphereRuleText.text = $"<b><color=#DC2626>{loc.Get("final_rule_unsafe", "FINAL ATMOSPHERE STATUS: UNSAFE (LEL 18.0%, H2S 35 ppm)\nCONFINED SPACE ENTRY PROHIBITED • 10/10 PROTOCOLS VERIFIED")}</color></b>";
            }

            if (_btnCompleteTrainingText != null)
            {
                bool isDone = _controller != null && _controller.IsAssessmentCompleted;
                _btnCompleteTrainingText.text = isDone ?
                    $"<b>{loc.Get("btn_view_summary", "VIEW ASSESSMENT SUMMARY →")}</b>" :
                    $"<b>{loc.Get("btn_complete_training", "COMPLETE TRAINING & VIEW SUMMARY →")}</b>";
            }
        }

        public void ShowGasAssessmentSummary()
        {
            var summaryUI = FindAnyObjectByType<GasAssessmentSummaryUI>(FindObjectsInactive.Include);
            if (summaryUI == null)
            {
                var canvas = GetOrCreateCanvas();
                var summaryObj = new GameObject("GasAssessmentSummaryUI");
                summaryObj.transform.SetParent(canvas.transform, false);
                summaryUI = summaryObj.AddComponent<GasAssessmentSummaryUI>();
            }

            if (_controller != null)
            {
                summaryUI.Controller = _controller;
                if (_controller.LatestAttempt != null && _controller.LatestAssessment != null)
                {
                    summaryUI.ShowSummary(AssessmentSummaryViewModel.Build(_controller.LatestAttempt, _controller.LatestAssessment));
                }
                else
                {
                    summaryUI.HandleCompleteTrainingRequested();
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
                _stepBadgeText.text = $"<color=#F97316><b>STEP {step}/9</b></color> • {stepTitle.ToUpperInvariant()}";
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
            if (_entryDecisionRootObj != null)
            {
                _entryDecisionRootObj.SetActive(step == 7);
                if (step == 7)
                {
                    UpdateEntryDecisionVisuals();
                }
            }
            if (_emergencyResponseRootObj != null)
            {
                _emergencyResponseRootObj.SetActive(step == 8);
                if (step == 8)
                {
                    UpdateEmergencyResponseVisuals();
                }
            }
            if (_finalSafetyCheckRootObj != null)
            {
                _finalSafetyCheckRootObj.SetActive(step == 9);
                if (step == 9)
                {
                    UpdateFinalSafetyCheckVisuals();
                }
            }

            // Primary Next button visibility & Step 4 synchronization
            if (_nextButtonObj != null)
            {
                if (step == 4)
                {
                    bool isPpeValid = _controller != null && _controller.HasValidPpeSelection();
                    _nextButtonObj.SetActive(true);
                    if (_nextButton != null) _nextButton.interactable = isPpeValid;
                    if (_nextButtonBg != null) _nextButtonBg.color = isPpeValid ? UITheme.PrimaryOrange : UITheme.PrimaryDisabled;
                    if (_nextButtonText != null)
                    {
                        string confirmLabel = loc.Get("ppe_btn_confirm", "CONFIRM PPE SELECTION →");
                        _nextButtonText.text = $"<b>{confirmLabel}</b>";
                        _nextButtonText.color = isPpeValid ? UITheme.TextLightOnDark : UITheme.PrimaryDisabledText;
                    }
                }
                else
                {
                    bool canNext = nav.CanGoNext;
                    _nextButtonObj.SetActive(canNext);
                    if (_nextButton != null) _nextButton.interactable = canNext;
                    if (_nextButtonBg != null) _nextButtonBg.color = canNext ? UITheme.PrimaryOrange : UITheme.PrimaryDisabled;
                    if (canNext && _nextButtonText != null)
                    {
                        string nextLabel = loc.Get($"gas_step{step}_next", nav.GetStepInfo(step)?.NextButtonLabel ?? "NEXT STEP →");
                        _nextButtonText.text = $"<b>{nextLabel}</b>";
                        _nextButtonText.color = UITheme.TextLightOnDark;
                    }
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
            var nav = Navigator;
            if (nav.CurrentStepIndex == 4 && _controller != null)
            {
                if (_controller.HasValidPpeSelection())
                {
                    _controller.SubmitPpeSelectionAndAdvance();
                }
                return;
            }

            if (nav.CurrentStepIndex == 9 && _controller != null && _controller.IsAssessmentCompleted)
            {
                ShowGasAssessmentSummary();
                return;
            }

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
