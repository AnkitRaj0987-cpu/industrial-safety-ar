// WorkerHomeController.cs
// Namespace : IndustrialSafetyAR.UI
//
// Common Worker App Foundation shell:
// - Worker Home Screen with app branding, offline badge, and profile card.
// - Module Selection system (Fire: Available, Gas: Coming Soon).
// - Settings Panel with Sound controls (FireAudioService) and Language selection (LocaleService).
// - Predictable navigation between Home, Settings, and Module training.
// - Stationary tap gating on all interactive buttons.

using System;
using System.Collections.Generic;
using IndustrialSafetyAR.Core;
using IndustrialSafetyAR.Core.Audio;
using IndustrialSafetyAR.Modules.FireExplosion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IndustrialSafetyAR.UI
{
    [ExecuteAlways]
    public class WorkerHomeController : MonoBehaviour
    {
        public static WorkerHomeController Instance { get; private set; }

        public enum WorkerAppScreenState
        {
            Home,
            Settings,
            TrainingFire,
            Results
        }

        private WorkerAppScreenState _currentState = WorkerAppScreenState.Home;
        public WorkerAppScreenState CurrentState => _currentState;

        // Visual elements
        private Canvas _rootCanvas;
        private GameObject _homeRoot;
        private GameObject _settingsRoot;

        // Home View components
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subtitleText;
        private TextMeshProUGUI _offlineBadgeText;
        private Image _offlineBadgeBg;
        private TextMeshProUGUI _profileHeaderLabelText;
        private TextMeshProUGUI _profileNameText;
        private TextMeshProUGUI _profileIdText;
        private TextMeshProUGUI _profileDivisionText;
        private TextMeshProUGUI _modulesHeaderText;
        private Button _headerSettingsButton;
        private Button _homeSettingsButton;
        private TextMeshProUGUI _homeSettingsButtonText;

        // Module Card 1 (Fire)
        private TextMeshProUGUI _fireTitleText;
        private TextMeshProUGUI _fireDescText;
        private TextMeshProUGUI _fireStatusText;
        private Button _fireStartButton;
        private TextMeshProUGUI _fireStartButtonText;

        // Module Card 2 (Gas)
        private TextMeshProUGUI _gasTitleText;
        private TextMeshProUGUI _gasDescText;
        private TextMeshProUGUI _gasStatusText;
        private Button _gasStartButton;
        private TextMeshProUGUI _gasStartButtonText;

        // Settings View components
        private TextMeshProUGUI _settingsTitleText;
        private TextMeshProUGUI _soundToggleLabel;
        private Button _soundToggleButton;
        private TextMeshProUGUI _soundToggleButtonText;

        private TextMeshProUGUI _alarmToggleLabel;
        private Button _alarmToggleButton;
        private TextMeshProUGUI _alarmToggleButtonText;

        private TextMeshProUGUI _volumeLabel;
        private Slider _volumeSlider;
        private TextMeshProUGUI _volumeValueText;

        private TextMeshProUGUI _languageHeader;
        private Button _btnLangEnglish;
        private Button _btnLangHindi;
        private Button _btnLangSantali;
        private TextMeshProUGUI _btnLangEnglishText;
        private TextMeshProUGUI _btnLangHindiText;
        private TextMeshProUGUI _btnLangSantaliText;

        private Button _settingsCloseButton;
        private TextMeshProUGUI _settingsCloseButtonText;

        // Worker identity
        private string _workerName = "Operator Ramesh Kumar";
        private string _workerId = FireTrainingWorkflow.DefaultOfflineWorkerId;

        public string WorkerName
        {
            get => _workerName;
            set
            {
                _workerName = value;
                RefreshTexts();
            }
        }

        public string WorkerId
        {
            get => _workerId;
            set
            {
                _workerId = value;
                RefreshTexts();
            }
        }

        public bool IsHomeVisible
        {
            get
            {
                EnsureUIHierarchy();
                return _homeRoot != null && _homeRoot.activeSelf;
            }
        }

        public bool IsSettingsVisible
        {
            get
            {
                EnsureUIHierarchy();
                return _settingsRoot != null && _settingsRoot.activeSelf;
            }
        }

        public bool IsFireModuleAvailable => true;
        public bool IsGasModuleEnabled => false;

        public Button HomeSettingsButton => _homeSettingsButton;
        public Button HeaderSettingsButton => _headerSettingsButton;
        public Button SettingsCloseButton => _settingsCloseButton;
        public Button FireStartButton => _fireStartButton;
        public Button SoundToggleButton => _soundToggleButton;
        public Button AlarmToggleButton => _alarmToggleButton;

        public void SetState(WorkerAppScreenState state)
        {
            _currentState = state;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            EnsureUIHierarchy();
        }

        private void OnEnable()
        {
            if (LocaleService.Instance != null)
            {
                LocaleService.Instance.OnLanguageChanged -= HandleLanguageChanged;
                LocaleService.Instance.OnLanguageChanged += HandleLanguageChanged;
            }
            UpdateOfflineStatus();
            RefreshTexts();
        }

        private void OnDisable()
        {
            if (LocaleService.Instance != null)
            {
                LocaleService.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void OnDestroy()
        {
            if (_rootCanvas != null && _rootCanvas.gameObject != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_rootCanvas.gameObject);
                }
                else
                {
                    DestroyImmediate(_rootCanvas.gameObject);
                }
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void HandleLanguageChanged(string lang)
        {
            RefreshTexts();
        }

        /// <summary>
        /// Displays the Worker Home screen and deactivates any training UI or modals.
        /// </summary>
        public void ShowHome()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.Home;
            if (_homeRoot != null) _homeRoot.SetActive(true);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);

            // Explicitly hide Fire Training UI canvas
            var fireUI = FindAnyObjectByType<FireInteractionFeedbackUI>(FindObjectsInactive.Include);
            if (fireUI != null)
            {
                fireUI.HideTrainingUI();
            }

            // Explicitly hide Fire Assessment Summary dialog
            var summaryUI = FindAnyObjectByType<FireAssessmentSummaryUI>(FindObjectsInactive.Include);
            if (summaryUI != null)
            {
                summaryUI.HideSummary();
            }

            // Stop any active emergency siren or transient audio
            if (FireAudioService.Instance != null)
            {
                FireAudioService.Instance.StopEmergencyAlarm();
                FireAudioService.Instance.StopAllAudio();
            }

            var fireCtrl = FindAnyObjectByType<FireArInteractionController>(FindObjectsInactive.Include);
            if (fireCtrl != null)
            {
                fireCtrl.enabled = false;
            }

            UpdateOfflineStatus();
            RefreshTexts();
        }

        /// <summary>
        /// Opens the Settings panel.
        /// </summary>
        public void OpenSettings()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.Settings;
            if (_settingsRoot != null)
            {
                _settingsRoot.SetActive(true);
                _settingsRoot.transform.SetAsLastSibling();
            }
            UpdateSettingsControls();
            RefreshTexts();
        }

        /// <summary>
        /// Closes the Settings panel and returns to Home.
        /// </summary>
        public void CloseSettings()
        {
            ShowHome();
        }

        /// <summary>
        /// Initiates the Fire & Explosion Response training scenario.
        /// </summary>
        public void StartFireTraining()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.TrainingFire;
            if (_homeRoot != null) _homeRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);

            // Activate Fire AR Interaction UI and ensure canvas is visible
            var fireUI = FindAnyObjectByType<FireInteractionFeedbackUI>(FindObjectsInactive.Include);
            if (fireUI != null)
            {
                fireUI.gameObject.SetActive(true);
                fireUI.ShowTrainingUI();
            }

            var fireCtrl = FindAnyObjectByType<FireArInteractionController>(FindObjectsInactive.Include);
            if (fireCtrl != null)
            {
                fireCtrl.gameObject.SetActive(true);
                fireCtrl.enabled = true;
            }

            FireAudioService.Instance.PlayStepCompleted();
        }

        /// <summary>
        /// Returns from training scenario back to the Worker Home menu.
        /// </summary>
        public void ReturnToHome()
        {
            ShowHome();
        }

        /// <summary>
        /// Updates the informational offline status indicator.
        /// </summary>
        public void UpdateOfflineStatus()
        {
            EnsureUIHierarchy();
            if (_offlineBadgeText == null) return;

            bool isOffline = Application.internetReachability == NetworkReachability.NotReachable;
            if (isOffline)
            {
                _offlineBadgeText.text = "● " + LocaleService.Instance.Get("offline_mode", "OFFLINE MODE");
                if (_offlineBadgeBg != null) _offlineBadgeBg.color = new Color(0.11f, 0.42f, 0.18f, 0.94f); // Forest Green
            }
            else
            {
                _offlineBadgeText.text = "● " + LocaleService.Instance.Get("online_sync", "ONLINE • SYNC READY");
                if (_offlineBadgeBg != null) _offlineBadgeBg.color = new Color(0.10f, 0.35f, 0.65f, 0.94f); // Calming Blue
            }
        }

        /// <summary>
        /// Refreshes all UI text elements to match the active locale.
        /// </summary>
        public void RefreshTexts()
        {
            var loc = LocaleService.Instance;

            if (_titleText != null) _titleText.text = $"<b>{loc.Get("app_title", "Industrial Safety AR")}</b>";
            if (_subtitleText != null) _subtitleText.text = loc.Get("app_subtitle", "Vocational Training Simulator • Jharkhand Industry");

            if (_profileHeaderLabelText != null) _profileHeaderLabelText.text = $"<color=#90CAF9><b>👤 {loc.Get("worker_profile", "WORKER PROFILE")}</b></color>";
            if (_profileNameText != null) _profileNameText.text = $"<b>{_workerName}</b>";
            if (_profileIdText != null) _profileIdText.text = $"{loc.Get("worker_id_label", "Worker ID")}: <color=#90CAF9>{_workerId}</color>";
            if (_profileDivisionText != null) _profileDivisionText.text = loc.Get("worker_division", "Division: Mining & Material Handling");

            if (_modulesHeaderText != null) _modulesHeaderText.text = $"<b>{loc.Get("modules_header", "AVAILABLE MODULES")}</b>";

            // Fire Card
            if (_fireTitleText != null) _fireTitleText.text = $"🔥 <b>{loc.Get("module_fire_title", "Fire & Explosion Response")}</b>";
            if (_fireDescText != null) _fireDescText.text = loc.Get("module_fire_desc", "9-step industrial conveyor fire response: hazard detection, classification, P.A.S.S. extinguisher procedure, and emergency evacuation.");
            if (_fireStatusText != null) _fireStatusText.text = $"● {loc.Get("status_available", "AVAILABLE")}";
            if (_fireStartButtonText != null) _fireStartButtonText.text = $"<b>{loc.Get("btn_start_training", "START TRAINING →")}</b>";

            // Gas Card
            if (_gasTitleText != null) _gasTitleText.text = $"☣ <b>{loc.Get("module_gas_title", "Gas Leak & Confined Space Safety")}</b>";
            if (_gasDescText != null) _gasDescText.text = loc.Get("module_gas_desc", "Atmospheric monitoring, multi-gas detector calibration, forced air ventilation, and confined space entry rescue protocols.");
            if (_gasStatusText != null) _gasStatusText.text = $"○ {loc.Get("status_coming_soon", "COMING SOON")}";
            if (_gasStartButtonText != null) _gasStartButtonText.text = $"<b>{loc.Get("status_coming_soon", "COMING SOON")}</b>";

            // Prominent Home Settings Button
            if (_homeSettingsButtonText != null) _homeSettingsButtonText.text = $"<b>⚙  {loc.Get("settings_title", "APPLICATION SETTINGS")}</b>";

            // Settings View
            if (_settingsTitleText != null) _settingsTitleText.text = $"<b>{loc.Get("settings_title", "APPLICATION SETTINGS")}</b>";
            if (_soundToggleLabel != null) _soundToggleLabel.text = loc.Get("sound_effects", "Sound Effects");
            if (_alarmToggleLabel != null) _alarmToggleLabel.text = loc.Get("emergency_alarm", "Emergency Alarm Siren");
            if (_volumeLabel != null) _volumeLabel.text = loc.Get("effects_volume", "Effects Volume");
            if (_languageHeader != null) _languageHeader.text = loc.Get("language_header", "Language / भाषा / ᱯᱟᱹᱨᱥᱤ");
            if (_settingsCloseButtonText != null) _settingsCloseButtonText.text = $"<b>{loc.Get("btn_close", "CLOSE [X]")}</b>";

            UpdateSettingsControls();
        }

        private void UpdateSettingsControls()
        {
            var loc = LocaleService.Instance;
            var audio = FireAudioService.Instance;

            if (_soundToggleButtonText != null)
            {
                bool on = audio.IsSoundEnabled;
                _soundToggleButtonText.text = on ? $"<b>🔊 {loc.Get("state_on", "ON")}</b>" : $"<b>🔇 {loc.Get("state_off", "OFF")}</b>";
            }

            if (_alarmToggleButtonText != null)
            {
                bool on = audio.IsEmergencyAlarmEnabled;
                _alarmToggleButtonText.text = on ? $"<b>🚨 {loc.Get("state_on", "ON")}</b>" : $"<b>🔕 {loc.Get("state_off", "OFF")}</b>";
            }

            if (_volumeSlider != null)
            {
                _volumeSlider.value = audio.EffectsVolume;
            }

            if (_volumeValueText != null)
            {
                _volumeValueText.text = $"{(audio.EffectsVolume * 100f):0}%";
            }

            // Language button states
            string curLang = loc.CurrentLanguage;
            if (_btnLangEnglishText != null)
                _btnLangEnglishText.color = curLang == LocaleService.LangEnglish ? new Color(0.4f, 0.9f, 1f) : Color.white;
            if (_btnLangHindiText != null)
                _btnLangHindiText.color = curLang == LocaleService.LangHindi ? new Color(0.4f, 0.9f, 1f) : Color.white;
            if (_btnLangSantaliText != null)
                _btnLangSantaliText.color = curLang == LocaleService.LangSantali ? new Color(0.4f, 0.9f, 1f) : Color.white;
        }

        /// <summary>
        /// Procedurally constructs the complete Home and Settings UI hierarchies on a ScreenSpaceOverlay Canvas.
        /// </summary>
        public void EnsureUIHierarchy()
        {
            if (_homeRoot != null) return;

            // 1. Root Canvas
            var canvasObj = GameObject.Find("WorkerAppCanvas");
            if (canvasObj != null)
            {
                _rootCanvas = canvasObj.GetComponent<Canvas>();
            }

            if (_rootCanvas == null)
            {
                canvasObj = new GameObject("WorkerAppCanvas");
                _rootCanvas = canvasObj.AddComponent<Canvas>();
                _rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _rootCanvas.sortingOrder = 95; // Sits below modal dialogs

                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            var defaultFont = FireInteractionFeedbackUI.GetDefaultFont();

            // =========================================================================
            // 2. HOME SCREEN CONTAINER
            // =========================================================================
            _homeRoot = new GameObject("HomeScreen");
            _homeRoot.transform.SetParent(_rootCanvas.transform, false);

            var homeRect = _homeRoot.AddComponent<RectTransform>();
            homeRect.anchorMin = Vector2.zero;
            homeRect.anchorMax = Vector2.one;
            homeRect.offsetMin = Vector2.zero;
            homeRect.offsetMax = Vector2.zero;

            // Full background
            var homeBg = _homeRoot.AddComponent<Image>();
            homeBg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f); // Sleek dark industrial theme

            // -------------------------------------------------------------
            // Header Bar (y: 0.90 to 0.98)
            // -------------------------------------------------------------
            var headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(_homeRoot.transform, false);
            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.04f, 0.90f);
            headerRect.anchorMax = new Vector2(0.96f, 0.98f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            // App Title
            var titleObj = new GameObject("AppTitle");
            titleObj.transform.SetParent(headerObj.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.50f);
            titleRect.anchorMax = new Vector2(0.80f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            _titleText = titleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _titleText.font = defaultFont;
            _titleText.fontSize = 28;
            _titleText.alignment = TextAlignmentOptions.Left;
            _titleText.color = Color.white;

            // Subtitle
            var subObj = new GameObject("AppSubtitle");
            subObj.transform.SetParent(headerObj.transform, false);
            var subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0f);
            subRect.anchorMax = new Vector2(0.80f, 0.48f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;

            _subtitleText = subObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _subtitleText.font = defaultFont;
            _subtitleText.fontSize = 15;
            _subtitleText.alignment = TextAlignmentOptions.Left;
            _subtitleText.color = new Color(0.70f, 0.78f, 0.88f);

            // Header Settings Button (top-right quick access gear)
            var headerSettingsBtnObj = new GameObject("HeaderSettingsButton");
            headerSettingsBtnObj.transform.SetParent(headerObj.transform, false);
            var hsBtnRect = headerSettingsBtnObj.AddComponent<RectTransform>();
            hsBtnRect.anchorMin = new Vector2(0.82f, 0.12f);
            hsBtnRect.anchorMax = new Vector2(1.0f, 0.88f);
            hsBtnRect.offsetMin = Vector2.zero;
            hsBtnRect.offsetMax = Vector2.zero;

            var hsBtnImg = headerSettingsBtnObj.AddComponent<Image>();
            hsBtnImg.color = new Color(0.18f, 0.24f, 0.35f, 0.95f);
            _headerSettingsButton = headerSettingsBtnObj.AddComponent<Button>();
            var hsTapGated = headerSettingsBtnObj.AddComponent<TapGatedButton>();
            hsTapGated.Initialize(() => OpenSettings());

            var hsBtnTextObj = new GameObject("Text");
            hsBtnTextObj.transform.SetParent(headerSettingsBtnObj.transform, false);
            var hsBtnTextRect = hsBtnTextObj.AddComponent<RectTransform>();
            hsBtnTextRect.anchorMin = Vector2.zero;
            hsBtnTextRect.anchorMax = Vector2.one;
            hsBtnTextRect.offsetMin = Vector2.zero;
            hsBtnTextRect.offsetMax = Vector2.zero;

            var hsBtnText = hsBtnTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) hsBtnText.font = defaultFont;
            hsBtnText.text = "<b>⚙</b>";
            hsBtnText.fontSize = 20;
            hsBtnText.alignment = TextAlignmentOptions.Center;
            hsBtnText.color = Color.white;

            // -------------------------------------------------------------
            // Worker Profile Card (y: 0.77 to 0.88)
            // -------------------------------------------------------------
            var profileCardObj = new GameObject("WorkerProfileCard");
            profileCardObj.transform.SetParent(_homeRoot.transform, false);
            var pcRect = profileCardObj.AddComponent<RectTransform>();
            pcRect.anchorMin = new Vector2(0.04f, 0.77f);
            pcRect.anchorMax = new Vector2(0.96f, 0.88f);
            pcRect.offsetMin = Vector2.zero;
            pcRect.offsetMax = Vector2.zero;

            var pcBg = profileCardObj.AddComponent<Image>();
            pcBg.color = new Color(0.12f, 0.16f, 0.24f, 0.96f);

            // Profile Header Label
            var pHeaderObj = new GameObject("ProfileHeader");
            pHeaderObj.transform.SetParent(profileCardObj.transform, false);
            var phRect = pHeaderObj.AddComponent<RectTransform>();
            phRect.anchorMin = new Vector2(0.04f, 0.70f);
            phRect.anchorMax = new Vector2(0.96f, 0.95f);
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;

            _profileHeaderLabelText = pHeaderObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _profileHeaderLabelText.font = defaultFont;
            _profileHeaderLabelText.text = "<color=#90CAF9><b>👤 WORKER PROFILE</b></color>";
            _profileHeaderLabelText.fontSize = 15;
            _profileHeaderLabelText.alignment = TextAlignmentOptions.Left;

            // Worker Name
            var pNameObj = new GameObject("ProfileName");
            pNameObj.transform.SetParent(profileCardObj.transform, false);
            var pnRect = pNameObj.AddComponent<RectTransform>();
            pnRect.anchorMin = new Vector2(0.04f, 0.38f);
            pnRect.anchorMax = new Vector2(0.96f, 0.68f);
            pnRect.offsetMin = Vector2.zero;
            pnRect.offsetMax = Vector2.zero;

            _profileNameText = pNameObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _profileNameText.font = defaultFont;
            _profileNameText.text = $"<b>{_workerName}</b>";
            _profileNameText.fontSize = 20;
            _profileNameText.alignment = TextAlignmentOptions.Left;
            _profileNameText.color = Color.white;

            // Worker ID & Division
            var pIdObj = new GameObject("ProfileIdAndDivision");
            pIdObj.transform.SetParent(profileCardObj.transform, false);
            var pidRect = pIdObj.AddComponent<RectTransform>();
            pidRect.anchorMin = new Vector2(0.04f, 0.05f);
            pidRect.anchorMax = new Vector2(0.96f, 0.36f);
            pidRect.offsetMin = Vector2.zero;
            pidRect.offsetMax = Vector2.zero;

            _profileIdText = pIdObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _profileIdText.font = defaultFont;
            _profileIdText.text = $"Worker ID: <color=#90CAF9>{_workerId}</color>  |  Division: Mining & Material Handling";
            _profileIdText.fontSize = 13;
            _profileIdText.alignment = TextAlignmentOptions.Left;
            _profileIdText.color = new Color(0.75f, 0.82f, 0.90f);

            // -------------------------------------------------------------
            // Section Header: Modules (y: 0.71 to 0.75)
            // -------------------------------------------------------------
            var mHeaderObj = new GameObject("ModulesHeader");
            mHeaderObj.transform.SetParent(_homeRoot.transform, false);
            var mhRect = mHeaderObj.AddComponent<RectTransform>();
            mhRect.anchorMin = new Vector2(0.04f, 0.71f);
            mhRect.anchorMax = new Vector2(0.96f, 0.75f);
            mhRect.offsetMin = Vector2.zero;
            mhRect.offsetMax = Vector2.zero;

            _modulesHeaderText = mHeaderObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _modulesHeaderText.font = defaultFont;
            _modulesHeaderText.text = "<b>AVAILABLE MODULES</b>";
            _modulesHeaderText.fontSize = 17;
            _modulesHeaderText.alignment = TextAlignmentOptions.Left;
            _modulesHeaderText.color = new Color(0.85f, 0.90f, 0.96f);

            // -------------------------------------------------------------
            // Module Card 1: Fire & Explosion Response (AVAILABLE) (y: 0.45 to 0.69)
            // -------------------------------------------------------------
            var fireCardObj = new GameObject("ModuleCard_Fire");
            fireCardObj.transform.SetParent(_homeRoot.transform, false);
            var fcRect = fireCardObj.AddComponent<RectTransform>();
            fcRect.anchorMin = new Vector2(0.04f, 0.45f);
            fcRect.anchorMax = new Vector2(0.96f, 0.69f);
            fcRect.offsetMin = Vector2.zero;
            fcRect.offsetMax = Vector2.zero;

            var fcBg = fireCardObj.AddComponent<Image>();
            fcBg.color = new Color(0.12f, 0.17f, 0.25f, 0.96f);

            // Fire Title & Status
            var fireTitleObj = new GameObject("FireTitle");
            fireTitleObj.transform.SetParent(fireCardObj.transform, false);
            var ftRect = fireTitleObj.AddComponent<RectTransform>();
            ftRect.anchorMin = new Vector2(0.04f, 0.74f);
            ftRect.anchorMax = new Vector2(0.68f, 0.96f);
            ftRect.offsetMin = Vector2.zero;
            ftRect.offsetMax = Vector2.zero;

            _fireTitleText = fireTitleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireTitleText.font = defaultFont;
            _fireTitleText.text = "🔥 <b>Fire & Explosion Response</b>";
            _fireTitleText.fontSize = 19;
            _fireTitleText.alignment = TextAlignmentOptions.Left;
            _fireTitleText.color = Color.white;

            var fireStatusObj = new GameObject("FireStatusBadge");
            fireStatusObj.transform.SetParent(fireCardObj.transform, false);
            var fsbRect = fireStatusObj.AddComponent<RectTransform>();
            fsbRect.anchorMin = new Vector2(0.70f, 0.75f);
            fsbRect.anchorMax = new Vector2(0.96f, 0.95f);
            fsbRect.offsetMin = Vector2.zero;
            fsbRect.offsetMax = Vector2.zero;

            var fsbBg = fireStatusObj.AddComponent<Image>();
            fsbBg.color = new Color(0.12f, 0.55f, 0.28f, 0.96f);

            var fsbTextObj = new GameObject("Text");
            fsbTextObj.transform.SetParent(fireStatusObj.transform, false);
            var fsbTextRect = fsbTextObj.AddComponent<RectTransform>();
            fsbTextRect.anchorMin = Vector2.zero;
            fsbTextRect.anchorMax = Vector2.one;
            fsbTextRect.offsetMin = Vector2.zero;
            fsbTextRect.offsetMax = Vector2.zero;

            _fireStatusText = fsbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireStatusText.font = defaultFont;
            _fireStatusText.text = "● AVAILABLE";
            _fireStatusText.fontSize = 13;
            _fireStatusText.alignment = TextAlignmentOptions.Center;
            _fireStatusText.color = Color.white;

            // Fire Description
            var fireDescObj = new GameObject("FireDescription");
            fireDescObj.transform.SetParent(fireCardObj.transform, false);
            var fdRect = fireDescObj.AddComponent<RectTransform>();
            fdRect.anchorMin = new Vector2(0.04f, 0.32f);
            fdRect.anchorMax = new Vector2(0.96f, 0.70f);
            fdRect.offsetMin = Vector2.zero;
            fdRect.offsetMax = Vector2.zero;

            _fireDescText = fireDescObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireDescText.font = defaultFont;
            _fireDescText.text = "9-step industrial conveyor fire response: hazard detection, classification, P.A.S.S. extinguisher procedure, and emergency evacuation.";
            _fireDescText.fontSize = 14;
            _fireDescText.alignment = TextAlignmentOptions.Left;
            _fireDescText.color = new Color(0.78f, 0.84f, 0.92f);

            // Fire Start Button
            var fireBtnObj = new GameObject("StartFireButton");
            fireBtnObj.transform.SetParent(fireCardObj.transform, false);
            var fbRect = fireBtnObj.AddComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.04f, 0.05f);
            fbRect.anchorMax = new Vector2(0.96f, 0.28f);
            fbRect.offsetMin = Vector2.zero;
            fbRect.offsetMax = Vector2.zero;

            var fbImg = fireBtnObj.AddComponent<Image>();
            fbImg.color = new Color(0.15f, 0.65f, 0.35f);
            _fireStartButton = fireBtnObj.AddComponent<Button>();
            var fireTapGated = fireBtnObj.AddComponent<TapGatedButton>();
            fireTapGated.Initialize(() => StartFireTraining());

            var fbTextObj = new GameObject("Text");
            fbTextObj.transform.SetParent(fireBtnObj.transform, false);
            var fbTextRect = fbTextObj.AddComponent<RectTransform>();
            fbTextRect.anchorMin = Vector2.zero;
            fbTextRect.anchorMax = Vector2.one;
            fbTextRect.offsetMin = Vector2.zero;
            fbTextRect.offsetMax = Vector2.zero;

            _fireStartButtonText = fbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireStartButtonText.font = defaultFont;
            _fireStartButtonText.text = "<b>START TRAINING →</b>";
            _fireStartButtonText.fontSize = 18;
            _fireStartButtonText.alignment = TextAlignmentOptions.Center;
            _fireStartButtonText.color = Color.white;

            // -------------------------------------------------------------
            // Module Card 2: Gas Leak & Confined Space (COMING SOON) (y: 0.19 to 0.43)
            // -------------------------------------------------------------
            var gasCardObj = new GameObject("ModuleCard_Gas");
            gasCardObj.transform.SetParent(_homeRoot.transform, false);
            var gcRect = gasCardObj.AddComponent<RectTransform>();
            gcRect.anchorMin = new Vector2(0.04f, 0.19f);
            gcRect.anchorMax = new Vector2(0.96f, 0.43f);
            gcRect.offsetMin = Vector2.zero;
            gcRect.offsetMax = Vector2.zero;

            var gcBg = gasCardObj.AddComponent<Image>();
            gcBg.color = new Color(0.10f, 0.13f, 0.19f, 0.90f); // Muted card

            // Gas Title & Status
            var gasTitleObj = new GameObject("GasTitle");
            gasTitleObj.transform.SetParent(gasCardObj.transform, false);
            var gtRect = gasTitleObj.AddComponent<RectTransform>();
            gtRect.anchorMin = new Vector2(0.04f, 0.74f);
            gtRect.anchorMax = new Vector2(0.68f, 0.96f);
            gtRect.offsetMin = Vector2.zero;
            gtRect.offsetMax = Vector2.zero;

            _gasTitleText = gasTitleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasTitleText.font = defaultFont;
            _gasTitleText.text = "☣ <b>Gas Leak & Confined Space Safety</b>";
            _gasTitleText.fontSize = 19;
            _gasTitleText.alignment = TextAlignmentOptions.Left;
            _gasTitleText.color = new Color(0.65f, 0.70f, 0.78f);

            var gasStatusObj = new GameObject("GasStatusBadge");
            gasStatusObj.transform.SetParent(gasCardObj.transform, false);
            var gsbRect = gasStatusObj.AddComponent<RectTransform>();
            gsbRect.anchorMin = new Vector2(0.70f, 0.75f);
            gsbRect.anchorMax = new Vector2(0.96f, 0.95f);
            gsbRect.offsetMin = Vector2.zero;
            gsbRect.offsetMax = Vector2.zero;

            var gsbBg = gasStatusObj.AddComponent<Image>();
            gsbBg.color = new Color(0.50f, 0.32f, 0.12f, 0.96f); // Warm Orange/Bronze

            var gsbTextObj = new GameObject("Text");
            gsbTextObj.transform.SetParent(gasStatusObj.transform, false);
            var gsbTextRect = gsbTextObj.AddComponent<RectTransform>();
            gsbTextRect.anchorMin = Vector2.zero;
            gsbTextRect.anchorMax = Vector2.one;
            gsbTextRect.offsetMin = Vector2.zero;
            gsbTextRect.offsetMax = Vector2.zero;

            _gasStatusText = gsbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasStatusText.font = defaultFont;
            _gasStatusText.text = "○ COMING SOON";
            _gasStatusText.fontSize = 13;
            _gasStatusText.alignment = TextAlignmentOptions.Center;
            _gasStatusText.color = new Color(1f, 0.9f, 0.8f);

            // Gas Description
            var gasDescObj = new GameObject("GasDescription");
            gasDescObj.transform.SetParent(gasCardObj.transform, false);
            var gdRect = gasDescObj.AddComponent<RectTransform>();
            gdRect.anchorMin = new Vector2(0.04f, 0.32f);
            gdRect.anchorMax = new Vector2(0.96f, 0.70f);
            gdRect.offsetMin = Vector2.zero;
            gdRect.offsetMax = Vector2.zero;

            _gasDescText = gasDescObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasDescText.font = defaultFont;
            _gasDescText.text = "Atmospheric monitoring, multi-gas detector calibration, forced air ventilation, and confined space entry rescue protocols.";
            _gasDescText.fontSize = 14;
            _gasDescText.alignment = TextAlignmentOptions.Left;
            _gasDescText.color = new Color(0.55f, 0.60f, 0.68f);

            // Gas Button (Disabled)
            var gasBtnObj = new GameObject("DisabledGasButton");
            gasBtnObj.transform.SetParent(gasCardObj.transform, false);
            var gbRect = gasBtnObj.AddComponent<RectTransform>();
            gbRect.anchorMin = new Vector2(0.04f, 0.05f);
            gbRect.anchorMax = new Vector2(0.96f, 0.28f);
            gbRect.offsetMin = Vector2.zero;
            gbRect.offsetMax = Vector2.zero;

            var gbImg = gasBtnObj.AddComponent<Image>();
            gbImg.color = new Color(0.20f, 0.22f, 0.28f, 0.8f);
            _gasStartButton = gasBtnObj.AddComponent<Button>();
            _gasStartButton.interactable = false; // Strictly disabled

            var gbTextObj = new GameObject("Text");
            gbTextObj.transform.SetParent(gasBtnObj.transform, false);
            var gbTextRect = gbTextObj.AddComponent<RectTransform>();
            gbTextRect.anchorMin = Vector2.zero;
            gbTextRect.anchorMax = Vector2.one;
            gbTextRect.offsetMin = Vector2.zero;
            gbTextRect.offsetMax = Vector2.zero;

            _gasStartButtonText = gbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasStartButtonText.font = defaultFont;
            _gasStartButtonText.text = "<b>COMING SOON</b>";
            _gasStartButtonText.fontSize = 18;
            _gasStartButtonText.alignment = TextAlignmentOptions.Center;
            _gasStartButtonText.color = new Color(0.6f, 0.6f, 0.6f);

            // -------------------------------------------------------------
            // Prominent Settings Button (y: 0.09 to 0.165)
            // -------------------------------------------------------------
            var prominentSettingsBtnObj = new GameObject("ProminentSettingsButton");
            prominentSettingsBtnObj.transform.SetParent(_homeRoot.transform, false);
            var psbRect = prominentSettingsBtnObj.AddComponent<RectTransform>();
            psbRect.anchorMin = new Vector2(0.04f, 0.09f);
            psbRect.anchorMax = new Vector2(0.96f, 0.165f);
            psbRect.offsetMin = Vector2.zero;
            psbRect.offsetMax = Vector2.zero;

            var psbImg = prominentSettingsBtnObj.AddComponent<Image>();
            psbImg.color = new Color(0.18f, 0.25f, 0.38f, 0.98f);
            _homeSettingsButton = prominentSettingsBtnObj.AddComponent<Button>();
            var psbTapGated = prominentSettingsBtnObj.AddComponent<TapGatedButton>();
            psbTapGated.Initialize(() => OpenSettings());

            var psbTextObj = new GameObject("Text");
            psbTextObj.transform.SetParent(prominentSettingsBtnObj.transform, false);
            var psbTextRect = psbTextObj.AddComponent<RectTransform>();
            psbTextRect.anchorMin = Vector2.zero;
            psbTextRect.anchorMax = Vector2.one;
            psbTextRect.offsetMin = new Vector2(10, 0);
            psbTextRect.offsetMax = new Vector2(-10, 0);

            _homeSettingsButtonText = psbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _homeSettingsButtonText.font = defaultFont;
            _homeSettingsButtonText.text = "<b>⚙  APPLICATION SETTINGS</b>";
            _homeSettingsButtonText.fontSize = 17;
            _homeSettingsButtonText.alignment = TextAlignmentOptions.Center;
            _homeSettingsButtonText.color = Color.white;

            // -------------------------------------------------------------
            // Offline Status Bar (y: 0.02 to 0.07)
            // -------------------------------------------------------------
            var offlineBarObj = new GameObject("OfflineStatusBar");
            offlineBarObj.transform.SetParent(_homeRoot.transform, false);
            var obRect = offlineBarObj.AddComponent<RectTransform>();
            obRect.anchorMin = new Vector2(0.04f, 0.02f);
            obRect.anchorMax = new Vector2(0.96f, 0.07f);
            obRect.offsetMin = Vector2.zero;
            obRect.offsetMax = Vector2.zero;

            _offlineBadgeBg = offlineBarObj.AddComponent<Image>();
            _offlineBadgeBg.color = new Color(0.11f, 0.42f, 0.18f, 0.94f);

            var obTextObj = new GameObject("OfflineBadgeText");
            obTextObj.transform.SetParent(offlineBarObj.transform, false);
            var obTextRect = obTextObj.AddComponent<RectTransform>();
            obTextRect.anchorMin = Vector2.zero;
            obTextRect.anchorMax = Vector2.one;
            obTextRect.offsetMin = new Vector2(10, 0);
            obTextRect.offsetMax = new Vector2(-10, 0);

            _offlineBadgeText = obTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _offlineBadgeText.font = defaultFont;
            _offlineBadgeText.text = "● OFFLINE MODE";
            _offlineBadgeText.fontSize = 14;
            _offlineBadgeText.alignment = TextAlignmentOptions.Center;
            _offlineBadgeText.color = Color.white;

            // =========================================================================
            // 3. SETTINGS PANEL MODAL OVERLAY (Full-Screen Raycast Blocker)
            // =========================================================================
            _settingsRoot = new GameObject("SettingsPanelModal");
            _settingsRoot.transform.SetParent(_rootCanvas.transform, false);

            var spRect = _settingsRoot.AddComponent<RectTransform>();
            spRect.anchorMin = Vector2.zero;
            spRect.anchorMax = Vector2.one;
            spRect.offsetMin = Vector2.zero;
            spRect.offsetMax = Vector2.zero;

            // Fullscreen backdrop image blocks all touches from passing to HomeScreen
            var spBackdropImg = _settingsRoot.AddComponent<Image>();
            spBackdropImg.color = new Color(0.02f, 0.04f, 0.07f, 0.88f);
            spBackdropImg.raycastTarget = true;

            // Centered Settings Card inside the modal overlay
            var settingsCardObj = new GameObject("SettingsCard");
            settingsCardObj.transform.SetParent(_settingsRoot.transform, false);
            var scRect = settingsCardObj.AddComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0.04f, 0.08f);
            scRect.anchorMax = new Vector2(0.96f, 0.92f);
            scRect.offsetMin = Vector2.zero;
            scRect.offsetMax = Vector2.zero;

            var scBg = settingsCardObj.AddComponent<Image>();
            scBg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

            // Settings Title
            var stObj = new GameObject("SettingsTitle");
            stObj.transform.SetParent(settingsCardObj.transform, false);
            var stRect = stObj.AddComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0.05f, 0.90f);
            stRect.anchorMax = new Vector2(0.84f, 0.98f);
            stRect.offsetMin = Vector2.zero;
            stRect.offsetMax = Vector2.zero;

            _settingsTitleText = stObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _settingsTitleText.font = defaultFont;
            _settingsTitleText.text = "<b>APPLICATION SETTINGS</b>";
            _settingsTitleText.fontSize = 24;
            _settingsTitleText.alignment = TextAlignmentOptions.Center;
            _settingsTitleText.color = Color.white;

            // Top-right Quick Close [X] Icon
            var topCloseObj = new GameObject("TopCloseButton");
            topCloseObj.transform.SetParent(settingsCardObj.transform, false);
            var tcRect = topCloseObj.AddComponent<RectTransform>();
            tcRect.anchorMin = new Vector2(0.85f, 0.90f);
            tcRect.anchorMax = new Vector2(0.96f, 0.98f);
            tcRect.offsetMin = Vector2.zero;
            tcRect.offsetMax = Vector2.zero;

            var tcImg = topCloseObj.AddComponent<Image>();
            tcImg.color = new Color(0.24f, 0.18f, 0.22f, 0.95f);
            var tcBtn = topCloseObj.AddComponent<Button>();
            var tcTap = topCloseObj.AddComponent<TapGatedButton>();
            tcTap.Initialize(() => CloseSettings());

            var tcTextObj = new GameObject("Text");
            tcTextObj.transform.SetParent(topCloseObj.transform, false);
            var tctRect = tcTextObj.AddComponent<RectTransform>();
            tctRect.anchorMin = Vector2.zero;
            tctRect.anchorMax = Vector2.one;
            var tcTmp = tcTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) tcTmp.font = defaultFont;
            tcTmp.text = "<b>✕</b>";
            tcTmp.fontSize = 20;
            tcTmp.alignment = TextAlignmentOptions.Center;
            tcTmp.color = new Color(0.95f, 0.70f, 0.70f);

            // Sound Effects Row
            var seObj = new GameObject("SoundEffectsRow");
            seObj.transform.SetParent(settingsCardObj.transform, false);
            var seRect = seObj.AddComponent<RectTransform>();
            seRect.anchorMin = new Vector2(0.06f, 0.77f);
            seRect.anchorMax = new Vector2(0.94f, 0.87f);
            seRect.offsetMin = Vector2.zero;
            seRect.offsetMax = Vector2.zero;

            var seLabelObj = new GameObject("Label");
            seLabelObj.transform.SetParent(seObj.transform, false);
            var selRect = seLabelObj.AddComponent<RectTransform>();
            selRect.anchorMin = new Vector2(0f, 0f);
            selRect.anchorMax = new Vector2(0.65f, 1f);
            selRect.offsetMin = Vector2.zero;
            selRect.offsetMax = Vector2.zero;

            _soundToggleLabel = seLabelObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _soundToggleLabel.font = defaultFont;
            _soundToggleLabel.text = "Sound Effects";
            _soundToggleLabel.fontSize = 18;
            _soundToggleLabel.alignment = TextAlignmentOptions.Left;
            _soundToggleLabel.color = Color.white;

            var seBtnObj = new GameObject("SoundToggleButton");
            seBtnObj.transform.SetParent(seObj.transform, false);
            var sebRect = seBtnObj.AddComponent<RectTransform>();
            sebRect.anchorMin = new Vector2(0.68f, 0.1f);
            sebRect.anchorMax = new Vector2(1f, 0.9f);
            sebRect.offsetMin = Vector2.zero;
            sebRect.offsetMax = Vector2.zero;

            var sebImg = seBtnObj.AddComponent<Image>();
            sebImg.color = new Color(0.18f, 0.24f, 0.35f);
            _soundToggleButton = seBtnObj.AddComponent<Button>();
            var seTapGated = seBtnObj.AddComponent<TapGatedButton>();
            seTapGated.Initialize(() =>
            {
                var audio = FireAudioService.Instance;
                audio.IsSoundEnabled = !audio.IsSoundEnabled;
                UpdateSettingsControls();
            });

            var sebTextObj = new GameObject("Text");
            sebTextObj.transform.SetParent(seBtnObj.transform, false);
            var sebTextRect = sebTextObj.AddComponent<RectTransform>();
            sebTextRect.anchorMin = Vector2.zero;
            sebTextRect.anchorMax = Vector2.one;
            sebTextRect.offsetMin = Vector2.zero;
            sebTextRect.offsetMax = Vector2.zero;

            _soundToggleButtonText = sebTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _soundToggleButtonText.font = defaultFont;
            _soundToggleButtonText.text = "<b>ON</b>";
            _soundToggleButtonText.fontSize = 16;
            _soundToggleButtonText.alignment = TextAlignmentOptions.Center;
            _soundToggleButtonText.color = Color.white;

            // Emergency Alarm Row
            var eaObj = new GameObject("EmergencyAlarmRow");
            eaObj.transform.SetParent(settingsCardObj.transform, false);
            var eaRect = eaObj.AddComponent<RectTransform>();
            eaRect.anchorMin = new Vector2(0.06f, 0.65f);
            eaRect.anchorMax = new Vector2(0.94f, 0.75f);
            eaRect.offsetMin = Vector2.zero;
            eaRect.offsetMax = Vector2.zero;

            var eaLabelObj = new GameObject("Label");
            eaLabelObj.transform.SetParent(eaObj.transform, false);
            var ealRect = eaLabelObj.AddComponent<RectTransform>();
            ealRect.anchorMin = new Vector2(0f, 0f);
            ealRect.anchorMax = new Vector2(0.65f, 1f);
            ealRect.offsetMin = Vector2.zero;
            ealRect.offsetMax = Vector2.zero;

            _alarmToggleLabel = eaLabelObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _alarmToggleLabel.font = defaultFont;
            _alarmToggleLabel.text = "Emergency Alarm Siren";
            _alarmToggleLabel.fontSize = 18;
            _alarmToggleLabel.alignment = TextAlignmentOptions.Left;
            _alarmToggleLabel.color = Color.white;

            var eaBtnObj = new GameObject("AlarmToggleButton");
            eaBtnObj.transform.SetParent(eaObj.transform, false);
            var eabRect = eaBtnObj.AddComponent<RectTransform>();
            eabRect.anchorMin = new Vector2(0.68f, 0.1f);
            eabRect.anchorMax = new Vector2(1f, 0.9f);
            eabRect.offsetMin = Vector2.zero;
            eabRect.offsetMax = Vector2.zero;

            var eabImg = eaBtnObj.AddComponent<Image>();
            eabImg.color = new Color(0.18f, 0.24f, 0.35f);
            _alarmToggleButton = eaBtnObj.AddComponent<Button>();
            var eaTapGated = eaBtnObj.AddComponent<TapGatedButton>();
            eaTapGated.Initialize(() =>
            {
                var audio = FireAudioService.Instance;
                audio.IsEmergencyAlarmEnabled = !audio.IsEmergencyAlarmEnabled;
                UpdateSettingsControls();
            });

            var eabTextObj = new GameObject("Text");
            eabTextObj.transform.SetParent(eaBtnObj.transform, false);
            var eabTextRect = eabTextObj.AddComponent<RectTransform>();
            eabTextRect.anchorMin = Vector2.zero;
            eabTextRect.anchorMax = Vector2.one;
            eabTextRect.offsetMin = Vector2.zero;
            eabTextRect.offsetMax = Vector2.zero;

            _alarmToggleButtonText = eabTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _alarmToggleButtonText.font = defaultFont;
            _alarmToggleButtonText.text = "<b>ON</b>";
            _alarmToggleButtonText.fontSize = 16;
            _alarmToggleButtonText.alignment = TextAlignmentOptions.Center;
            _alarmToggleButtonText.color = Color.white;

            // Effects Volume Row
            var evObj = new GameObject("EffectsVolumeRow");
            evObj.transform.SetParent(settingsCardObj.transform, false);
            var evRect = evObj.AddComponent<RectTransform>();
            evRect.anchorMin = new Vector2(0.06f, 0.52f);
            evRect.anchorMax = new Vector2(0.94f, 0.62f);
            evRect.offsetMin = Vector2.zero;
            evRect.offsetMax = Vector2.zero;

            var evLabelObj = new GameObject("Label");
            evLabelObj.transform.SetParent(evObj.transform, false);
            var evlRect = evLabelObj.AddComponent<RectTransform>();
            evlRect.anchorMin = new Vector2(0f, 0f);
            evlRect.anchorMax = new Vector2(0.40f, 1f);
            evlRect.offsetMin = Vector2.zero;
            evlRect.offsetMax = Vector2.zero;

            _volumeLabel = evLabelObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _volumeLabel.font = defaultFont;
            _volumeLabel.text = "Effects Volume";
            _volumeLabel.fontSize = 18;
            _volumeLabel.alignment = TextAlignmentOptions.Left;
            _volumeLabel.color = Color.white;

            var evValObj = new GameObject("ValueText");
            evValObj.transform.SetParent(evObj.transform, false);
            var evvRect = evValObj.AddComponent<RectTransform>();
            evvRect.anchorMin = new Vector2(0.85f, 0f);
            evvRect.anchorMax = new Vector2(1.0f, 1f);
            evvRect.offsetMin = Vector2.zero;
            evvRect.offsetMax = Vector2.zero;

            _volumeValueText = evValObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _volumeValueText.font = defaultFont;
            _volumeValueText.text = "70%";
            _volumeValueText.fontSize = 16;
            _volumeValueText.alignment = TextAlignmentOptions.Right;
            _volumeValueText.color = new Color(0.9f, 0.9f, 0.9f);

            // Volume adjustment step buttons (- / +)
            var vMinusObj = new GameObject("VolMinus");
            vMinusObj.transform.SetParent(evObj.transform, false);
            var vmRect = vMinusObj.AddComponent<RectTransform>();
            vmRect.anchorMin = new Vector2(0.42f, 0.15f);
            vmRect.anchorMax = new Vector2(0.60f, 0.85f);
            vmRect.offsetMin = Vector2.zero;
            vmRect.offsetMax = Vector2.zero;
            vMinusObj.AddComponent<Image>().color = new Color(0.18f, 0.24f, 0.35f);
            var vMinusBtn = vMinusObj.AddComponent<Button>();
            var vmTap = vMinusObj.AddComponent<TapGatedButton>();
            vmTap.Initialize(() =>
            {
                var audio = FireAudioService.Instance;
                audio.EffectsVolume = Mathf.Max(0f, audio.EffectsVolume - 0.10f);
                UpdateSettingsControls();
            });
            var vMinusTextObj = new GameObject("Text");
            vMinusTextObj.transform.SetParent(vMinusObj.transform, false);
            var vmtRect = vMinusTextObj.AddComponent<RectTransform>();
            vmtRect.anchorMin = Vector2.zero;
            vmtRect.anchorMax = Vector2.one;
            var vmtTmp = vMinusTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) vmtTmp.font = defaultFont;
            vmtTmp.text = "<b>- 10%</b>";
            vmtTmp.fontSize = 14;
            vmtTmp.alignment = TextAlignmentOptions.Center;
            vmtTmp.color = Color.white;

            var vPlusObj = new GameObject("VolPlus");
            vPlusObj.transform.SetParent(evObj.transform, false);
            var vpRect = vPlusObj.AddComponent<RectTransform>();
            vpRect.anchorMin = new Vector2(0.63f, 0.15f);
            vpRect.anchorMax = new Vector2(0.81f, 0.85f);
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            vPlusObj.AddComponent<Image>().color = new Color(0.18f, 0.24f, 0.35f);
            var vPlusBtn = vPlusObj.AddComponent<Button>();
            var vpTap = vPlusObj.AddComponent<TapGatedButton>();
            vpTap.Initialize(() =>
            {
                var audio = FireAudioService.Instance;
                audio.EffectsVolume = Mathf.Min(1f, audio.EffectsVolume + 0.10f);
                UpdateSettingsControls();
            });
            var vPlusTextObj = new GameObject("Text");
            vPlusTextObj.transform.SetParent(vPlusObj.transform, false);
            var vptRect = vPlusTextObj.AddComponent<RectTransform>();
            vptRect.anchorMin = Vector2.zero;
            vptRect.anchorMax = Vector2.one;
            var vptTmp = vPlusTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) vptTmp.font = defaultFont;
            vptTmp.text = "<b>+ 10%</b>";
            vptTmp.fontSize = 14;
            vptTmp.alignment = TextAlignmentOptions.Center;
            vptTmp.color = Color.white;

            // Language Selection Section
            var langHeaderObj = new GameObject("LanguageHeader");
            langHeaderObj.transform.SetParent(settingsCardObj.transform, false);
            var lhRect = langHeaderObj.AddComponent<RectTransform>();
            lhRect.anchorMin = new Vector2(0.06f, 0.38f);
            lhRect.anchorMax = new Vector2(0.94f, 0.46f);
            lhRect.offsetMin = Vector2.zero;
            lhRect.offsetMax = Vector2.zero;

            _languageHeader = langHeaderObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _languageHeader.font = defaultFont;
            _languageHeader.text = "Language / भाषा / ᱯᱟᱹᱨᱥᱤ";
            _languageHeader.fontSize = 18;
            _languageHeader.alignment = TextAlignmentOptions.Left;
            _languageHeader.color = Color.white;

            // Language Option 1: English
            var lEnObj = new GameObject("LangBtn_English");
            lEnObj.transform.SetParent(settingsCardObj.transform, false);
            var lenRect = lEnObj.AddComponent<RectTransform>();
            lenRect.anchorMin = new Vector2(0.06f, 0.26f);
            lenRect.anchorMax = new Vector2(0.33f, 0.36f);
            lenRect.offsetMin = Vector2.zero;
            lenRect.offsetMax = Vector2.zero;
            lEnObj.AddComponent<Image>().color = new Color(0.18f, 0.24f, 0.35f);
            _btnLangEnglish = lEnObj.AddComponent<Button>();
            var enTap = lEnObj.AddComponent<TapGatedButton>();
            enTap.Initialize(() => LocaleService.Instance.SetLanguage(LocaleService.LangEnglish));

            var lenTextObj = new GameObject("Text");
            lenTextObj.transform.SetParent(lEnObj.transform, false);
            var lentRect = lenTextObj.AddComponent<RectTransform>();
            lentRect.anchorMin = Vector2.zero;
            lentRect.anchorMax = Vector2.one;
            _btnLangEnglishText = lenTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _btnLangEnglishText.font = defaultFont;
            _btnLangEnglishText.text = "<b>English</b>";
            _btnLangEnglishText.fontSize = 15;
            _btnLangEnglishText.alignment = TextAlignmentOptions.Center;
            _btnLangEnglishText.color = Color.white;

            // Language Option 2: Hindi
            var lHiObj = new GameObject("LangBtn_Hindi");
            lHiObj.transform.SetParent(settingsCardObj.transform, false);
            var lhiRect = lHiObj.AddComponent<RectTransform>();
            lhiRect.anchorMin = new Vector2(0.36f, 0.26f);
            lhiRect.anchorMax = new Vector2(0.63f, 0.36f);
            lhiRect.offsetMin = Vector2.zero;
            lhiRect.offsetMax = Vector2.zero;
            lHiObj.AddComponent<Image>().color = new Color(0.18f, 0.24f, 0.35f);
            _btnLangHindi = lHiObj.AddComponent<Button>();
            var hiTap = lHiObj.AddComponent<TapGatedButton>();
            hiTap.Initialize(() => LocaleService.Instance.SetLanguage(LocaleService.LangHindi));

            var lhiTextObj = new GameObject("Text");
            lhiTextObj.transform.SetParent(lHiObj.transform, false);
            var lhitRect = lhiTextObj.AddComponent<RectTransform>();
            lhitRect.anchorMin = Vector2.zero;
            lhitRect.anchorMax = Vector2.one;
            _btnLangHindiText = lhiTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _btnLangHindiText.font = defaultFont;
            _btnLangHindiText.text = "<b>हिन्दी</b>";
            _btnLangHindiText.fontSize = 15;
            _btnLangHindiText.alignment = TextAlignmentOptions.Center;
            _btnLangHindiText.color = Color.white;

            // Language Option 3: Santali
            var lSatObj = new GameObject("LangBtn_Santali");
            lSatObj.transform.SetParent(settingsCardObj.transform, false);
            var lsatRect = lSatObj.AddComponent<RectTransform>();
            lsatRect.anchorMin = new Vector2(0.66f, 0.26f);
            lsatRect.anchorMax = new Vector2(0.94f, 0.36f);
            lsatRect.offsetMin = Vector2.zero;
            lsatRect.offsetMax = Vector2.zero;
            lSatObj.AddComponent<Image>().color = new Color(0.18f, 0.24f, 0.35f);
            _btnLangSantali = lSatObj.AddComponent<Button>();
            var satTap = lSatObj.AddComponent<TapGatedButton>();
            satTap.Initialize(() => LocaleService.Instance.SetLanguage(LocaleService.LangSantali));

            var lsatTextObj = new GameObject("Text");
            lsatTextObj.transform.SetParent(lSatObj.transform, false);
            var lsattRect = lsatTextObj.AddComponent<RectTransform>();
            lsattRect.anchorMin = Vector2.zero;
            lsattRect.anchorMax = Vector2.one;
            _btnLangSantaliText = lsatTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _btnLangSantaliText.font = defaultFont;
            _btnLangSantaliText.text = "<b>ᱥᱟᱱᱛᱟᱲᱤ</b>";
            _btnLangSantaliText.fontSize = 15;
            _btnLangSantaliText.alignment = TextAlignmentOptions.Center;
            _btnLangSantaliText.color = Color.white;

            // Bottom Primary Close Settings Button
            var closeBtnObj = new GameObject("CloseSettingsButton");
            closeBtnObj.transform.SetParent(settingsCardObj.transform, false);
            var cbRect = closeBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.15f, 0.06f);
            cbRect.anchorMax = new Vector2(0.85f, 0.16f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;

            var cbImg = closeBtnObj.AddComponent<Image>();
            cbImg.color = new Color(0.22f, 0.32f, 0.48f, 0.98f);
            _settingsCloseButton = closeBtnObj.AddComponent<Button>();
            var cbTap = closeBtnObj.AddComponent<TapGatedButton>();
            cbTap.Initialize(() => CloseSettings());

            var cbTextObj = new GameObject("Text");
            cbTextObj.transform.SetParent(closeBtnObj.transform, false);
            var cbtRect = cbTextObj.AddComponent<RectTransform>();
            cbtRect.anchorMin = Vector2.zero;
            cbtRect.anchorMax = Vector2.one;
            cbtRect.offsetMin = Vector2.zero;
            cbtRect.offsetMax = Vector2.zero;

            _settingsCloseButtonText = cbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _settingsCloseButtonText.font = defaultFont;
            _settingsCloseButtonText.text = "<b>CLOSE [X]</b>";
            _settingsCloseButtonText.fontSize = 18;
            _settingsCloseButtonText.alignment = TextAlignmentOptions.Center;
            _settingsCloseButtonText.color = Color.white;

            // Default initial state
            _settingsRoot.SetActive(false);
            ShowHome();
        }

        /// <summary>
        /// Automatically bootstraps the Worker Home Screen in AR scenes after load.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static GameObject AutoBootstrap()
        {
            if (FindAnyObjectByType<WorkerHomeController>() == null)
            {
                var go = new GameObject("WorkerAppShell");
                var ctrl = go.AddComponent<WorkerHomeController>();
                Debug.Log("[WorkerHomeController] Auto-bootstrapped Worker App Shell.");
                return go;
            }
            return null;
        }
    }
}
