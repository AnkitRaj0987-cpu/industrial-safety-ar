// WorkerHomeController.cs
// Namespace : IndustrialSafetyAR.UI
//
// Common Worker App Foundation shell:
// - Worker Home Screen with app branding, offline badge, and profile card.
// - Module Selection system (Fire: Available/In Progress/Completed, Gas: Coming Soon).
// - Controlled AR Launch Screen with explicit Camera ON/OFF toggle.
// - 4-Tab Bottom Navigation (Home, AR, Certificates, Profile).
// - Clean Screen State Isolation (Home, AR, Settings, TrainingFire, Results, Certificates, Profile).
// - Authoritative AR camera lifecycle enforcement (Camera OFF on launch, Home, Settings, Certificates, Profile).
// - Settings Panel with Sound controls (FireAudioService) and Language selection (LocaleService).
// - Stationary tap gating on all interactive buttons.

using System;
using System.Collections.Generic;
using IndustrialSafetyAR.AR;
using IndustrialSafetyAR.Core;
using IndustrialSafetyAR.Core.Audio;
using IndustrialSafetyAR.Modules.FireExplosion;
using IndustrialSafetyAR.Modules.GasConfinedSpace;
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
            AR,
            Settings,
            TrainingFire,
            TrainingGas,
            Results,
            Certificates,
            Profile
        }

        private WorkerAppScreenState _currentState = WorkerAppScreenState.Home;
        private WorkerAppScreenState _previousNavState = WorkerAppScreenState.Home;
        public WorkerAppScreenState CurrentState => _currentState;

        // Visual elements
        private Canvas _rootCanvas;
        private GameObject _homeRoot;
        private GameObject _settingsRoot;

        // Content Views inside HomeRoot
        private GameObject _homeContentRoot;
        private GameObject _arContentRoot;
        private GameObject _certificatesContentRoot;
        private GameObject _profileContentRoot;

        // Top Header components
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _welcomeBackText;
        private TextMeshProUGUI _subtitleText;
        private Button _headerSettingsButton;

        // Home View components
        private TextMeshProUGUI _profileHeaderLabelText;
        private TextMeshProUGUI _profileNameText;
        private TextMeshProUGUI _profileIdText;
        private TextMeshProUGUI _modulesHeaderText;
        private Button _homeSettingsButton;
        private TextMeshProUGUI _homeSettingsButtonText;

        // Module Card 1 (Fire)
        private TextMeshProUGUI _fireTitleText;
        private TextMeshProUGUI _fireArBadgeText;
        private TextMeshProUGUI _fireDescText;
        private TextMeshProUGUI _fireStatusText;
        private Image _fireStatusBg;
        private TextMeshProUGUI _fireOfflineTagText;
        private Button _fireStartButton;
        private TextMeshProUGUI _fireStartButtonText;

        // Module Card 2 (Gas)
        private TextMeshProUGUI _gasTitleText;
        private TextMeshProUGUI _gasArBadgeText;
        private TextMeshProUGUI _gasDescText;
        private TextMeshProUGUI _gasStatusText;
        private Button _gasStartButton;
        private TextMeshProUGUI _gasStartButtonText;

        // AR View components
        private TextMeshProUGUI _arTitleText;
        private TextMeshProUGUI _arSubtitleText;
        private TextMeshProUGUI _arDescText;
        private TextMeshProUGUI _arStatusText;
        private Image _arStatusBg;
        private Button _arToggleButton;
        private TextMeshProUGUI _arToggleButtonText;
        private Image _arToggleBtnBg;
        private Button _arStartFireShortcutBtn;
        private TextMeshProUGUI _arStartFireShortcutBtnText;

        // Certificates View components
        private TextMeshProUGUI _certTitleText;
        private TextMeshProUGUI _certIconText;
        private TextMeshProUGUI _certDescText;
        private TextMeshProUGUI _certSubText;

        // Profile View components
        private TextMeshProUGUI _profileViewTitleText;
        private TextMeshProUGUI _pvNameLabel;
        private TextMeshProUGUI _pvNameVal;
        private TextMeshProUGUI _pvIdLabel;
        private TextMeshProUGUI _pvIdVal;
        private TextMeshProUGUI _pvDivLabel;
        private TextMeshProUGUI _pvDivVal;
        private TextMeshProUGUI _pvLangLabel;
        private TextMeshProUGUI _pvLangVal;
        private Button _pvSettingsBtn;
        private TextMeshProUGUI _pvSettingsBtnText;

        // Bottom Navigation Bar components
        private GameObject _bottomNavBar;
        private Button _navHomeBtn;
        private TextMeshProUGUI _navHomeText;
        private Image _navHomeBg;
        private Button _navArBtn;
        private TextMeshProUGUI _navArText;
        private Image _navArBg;
        private Button _navCertBtn;
        private TextMeshProUGUI _navCertText;
        private Image _navCertBg;
        private Button _navProfBtn;
        private TextMeshProUGUI _navProfText;
        private Image _navProfBg;

        // Offline Status Bar components
        private TextMeshProUGUI _offlineBadgeText;
        private Image _offlineBadgeBg;

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
                return _homeRoot != null && _homeRoot.activeSelf && _homeContentRoot != null && _homeContentRoot.activeSelf;
            }
        }

        public bool IsArVisible
        {
            get
            {
                EnsureUIHierarchy();
                return _homeRoot != null && _homeRoot.activeSelf && _arContentRoot != null && _arContentRoot.activeSelf;
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

        public bool IsCertificatesVisible
        {
            get
            {
                EnsureUIHierarchy();
                return _homeRoot != null && _homeRoot.activeSelf && _certificatesContentRoot != null && _certificatesContentRoot.activeSelf;
            }
        }

        public bool IsProfileVisible
        {
            get
            {
                EnsureUIHierarchy();
                return _homeRoot != null && _homeRoot.activeSelf && _profileContentRoot != null && _profileContentRoot.activeSelf;
            }
        }

        public bool IsFireModuleAvailable => true;
        public bool IsGasModuleEnabled => false;

        public Button HomeSettingsButton => _homeSettingsButton;
        public Button HeaderSettingsButton => _headerSettingsButton;
        public Button SettingsCloseButton => _settingsCloseButton;
        public Button FireStartButton => _fireStartButton;
        public Button GasStartButton => _gasStartButton;
        public Button SoundToggleButton => _soundToggleButton;
        public Button AlarmToggleButton => _alarmToggleButton;
        public Button NavHomeButton => _navHomeBtn;
        public Button NavArButton => _navArBtn;
        public Button NavCertificatesButton => _navCertBtn;
        public Button NavProfileButton => _navProfBtn;
        public Button ArToggleButton => _arToggleButton;

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

            // Strictly enforce AR OFF on application launch
            if (ARModeController.Instance != null)
            {
                ARModeController.Instance.DisableAR();
            }
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
        /// Displays the Worker Home screen (modules list) and authoritatively deactivates AR camera.
        /// </summary>
        public void ShowHome()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.Home;
            _previousNavState = WorkerAppScreenState.Home;

            if (_homeRoot != null) _homeRoot.SetActive(true);
            if (_homeContentRoot != null) _homeContentRoot.SetActive(true);
            if (_arContentRoot != null) _arContentRoot.SetActive(false);
            if (_certificatesContentRoot != null) _certificatesContentRoot.SetActive(false);
            if (_profileContentRoot != null) _profileContentRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);

            UpdateNavHighlight();

            // Authoritatively disable camera and AR on Home
            if (ARModeController.Instance != null)
            {
                ARModeController.Instance.DisableAR();
            }

            // Explicitly hide Fire Training UI canvas
            var fireUIs = FindObjectsByType<FireInteractionFeedbackUI>(FindObjectsInactive.Include);
            if (fireUIs != null)
            {
                foreach (var fui in fireUIs)
                {
                    if (fui != null) fui.HideTrainingUI();
                }
            }

            // Explicitly hide Gas Training UI canvas
            var gasUIs = FindObjectsByType<GasInteractionFeedbackUI>(FindObjectsInactive.Include);
            if (gasUIs != null)
            {
                foreach (var gui in gasUIs)
                {
                    if (gui != null) gui.HideTrainingUI();
                }
            }

            // Explicitly hide Fire Assessment Summary dialog
            var summaryUIs = FindObjectsByType<FireAssessmentSummaryUI>(FindObjectsInactive.Include);
            if (summaryUIs != null)
            {
                foreach (var sui in summaryUIs)
                {
                    if (sui != null) sui.HideSummary();
                }
            }

            var gasSummaryUIs = FindObjectsByType<GasAssessmentSummaryUI>(FindObjectsInactive.Include);
            if (gasSummaryUIs != null)
            {
                foreach (var gsui in gasSummaryUIs)
                {
                    if (gsui != null) gsui.HideSummary();
                }
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

            var gasCtrl = FindAnyObjectByType<GasArInteractionController>(FindObjectsInactive.Include);
            if (gasCtrl != null)
            {
                gasCtrl.enabled = false;
            }

            var gasUI = FindAnyObjectByType<GasInteractionFeedbackUI>(FindObjectsInactive.Include);
            if (gasUI != null)
            {
                gasUI.HideTrainingUI();
            }

            UpdateOfflineStatus();
            RefreshTexts();
        }

        /// <summary>
        /// Displays the controlled AR launch screen. Camera remains OFF until explicitly enabled by user.
        /// </summary>
        public void ShowAR()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.AR;
            _previousNavState = WorkerAppScreenState.AR;

            if (_homeRoot != null) _homeRoot.SetActive(true);
            if (_homeContentRoot != null) _homeContentRoot.SetActive(false);
            if (_arContentRoot != null) _arContentRoot.SetActive(true);
            if (_certificatesContentRoot != null) _certificatesContentRoot.SetActive(false);
            if (_profileContentRoot != null) _profileContentRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);

            UpdateNavHighlight();
            UpdateArScreenUI();

            var fireUI = FindAnyObjectByType<FireInteractionFeedbackUI>(FindObjectsInactive.Include);
            if (fireUI != null) fireUI.HideTrainingUI();

            var summaryUI = FindAnyObjectByType<FireAssessmentSummaryUI>(FindObjectsInactive.Include);
            if (summaryUI != null) summaryUI.HideSummary();

            var gasSummaryUI = FindAnyObjectByType<GasAssessmentSummaryUI>(FindObjectsInactive.Include);
            if (gasSummaryUI != null) gasSummaryUI.HideSummary();

            if (FireAudioService.Instance != null)
            {
                FireAudioService.Instance.StopEmergencyAlarm();
                FireAudioService.Instance.StopAllAudio();
            }

            UpdateOfflineStatus();
            RefreshTexts();
        }

        /// <summary>
        /// Explicit user action to toggle AR Camera on or off from the AR screen.
        /// </summary>
        public void ToggleARCamera()
        {
            if (ARModeController.Instance != null)
            {
                if (ARModeController.Instance.IsARActive)
                {
                    ARModeController.Instance.DisableAR();
                }
                else
                {
                    ARModeController.Instance.EnableAR();
                }
            }
            UpdateArScreenUI();
        }

        /// <summary>
        /// Displays the Certificates screen (safe empty state without fake certificates).
        /// </summary>
        public void ShowCertificates()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.Certificates;
            _previousNavState = WorkerAppScreenState.Certificates;

            if (_homeRoot != null) _homeRoot.SetActive(true);
            if (_homeContentRoot != null) _homeContentRoot.SetActive(false);
            if (_arContentRoot != null) _arContentRoot.SetActive(false);
            if (_certificatesContentRoot != null) _certificatesContentRoot.SetActive(true);
            if (_profileContentRoot != null) _profileContentRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);

            UpdateNavHighlight();

            if (ARModeController.Instance != null)
            {
                ARModeController.Instance.DisableAR();
            }

            var fireUI = FindAnyObjectByType<FireInteractionFeedbackUI>(FindObjectsInactive.Include);
            if (fireUI != null) fireUI.HideTrainingUI();

            var summaryUI = FindAnyObjectByType<FireAssessmentSummaryUI>(FindObjectsInactive.Include);
            if (summaryUI != null) summaryUI.HideSummary();

            var gasSummaryUI = FindAnyObjectByType<GasAssessmentSummaryUI>(FindObjectsInactive.Include);
            if (gasSummaryUI != null) gasSummaryUI.HideSummary();

            if (FireAudioService.Instance != null)
            {
                FireAudioService.Instance.StopEmergencyAlarm();
                FireAudioService.Instance.StopAllAudio();
            }

            UpdateOfflineStatus();
            RefreshTexts();
        }

        /// <summary>
        /// Displays the Worker Profile view.
        /// </summary>
        public void ShowProfile()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.Profile;
            _previousNavState = WorkerAppScreenState.Profile;

            if (_homeRoot != null) _homeRoot.SetActive(true);
            if (_homeContentRoot != null) _homeContentRoot.SetActive(false);
            if (_arContentRoot != null) _arContentRoot.SetActive(false);
            if (_certificatesContentRoot != null) _certificatesContentRoot.SetActive(false);
            if (_profileContentRoot != null) _profileContentRoot.SetActive(true);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);

            UpdateNavHighlight();

            if (ARModeController.Instance != null)
            {
                ARModeController.Instance.DisableAR();
            }

            var fireUI = FindAnyObjectByType<FireInteractionFeedbackUI>(FindObjectsInactive.Include);
            if (fireUI != null) fireUI.HideTrainingUI();

            var summaryUI = FindAnyObjectByType<FireAssessmentSummaryUI>(FindObjectsInactive.Include);
            if (summaryUI != null) summaryUI.HideSummary();

            var gasSummaryUI = FindAnyObjectByType<GasAssessmentSummaryUI>(FindObjectsInactive.Include);
            if (gasSummaryUI != null) gasSummaryUI.HideSummary();

            if (FireAudioService.Instance != null)
            {
                FireAudioService.Instance.StopEmergencyAlarm();
                FireAudioService.Instance.StopAllAudio();
            }

            UpdateOfflineStatus();
            RefreshTexts();
        }

        private void UpdateNavHighlight()
        {
            Color activeBg = UITheme.PrimaryOrangeSurface;
            Color activeText = UITheme.PrimaryOrange;
            Color inactiveBg = UITheme.CardSecondaryBg;
            Color inactiveText = UITheme.TextSecondary;

            if (_navHomeBg != null) _navHomeBg.color = _currentState == WorkerAppScreenState.Home ? activeBg : inactiveBg;
            if (_navHomeText != null) _navHomeText.color = _currentState == WorkerAppScreenState.Home ? activeText : inactiveText;

            if (_navArBg != null) _navArBg.color = _currentState == WorkerAppScreenState.AR ? activeBg : inactiveBg;
            if (_navArText != null) _navArText.color = _currentState == WorkerAppScreenState.AR ? activeText : inactiveText;

            if (_navCertBg != null) _navCertBg.color = _currentState == WorkerAppScreenState.Certificates ? activeBg : inactiveBg;
            if (_navCertText != null) _navCertText.color = _currentState == WorkerAppScreenState.Certificates ? activeText : inactiveText;

            if (_navProfBg != null) _navProfBg.color = _currentState == WorkerAppScreenState.Profile ? activeBg : inactiveBg;
            if (_navProfText != null) _navProfText.color = _currentState == WorkerAppScreenState.Profile ? activeText : inactiveText;
        }

        /// <summary>
        /// Opens the Settings panel overlay.
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
        /// Closes the Settings panel and restores the previous view.
        /// </summary>
        public void CloseSettings()
        {
            EnsureUIHierarchy();
            if (_settingsRoot != null)
            {
                _settingsRoot.SetActive(false);
            }

            switch (_previousNavState)
            {
                case WorkerAppScreenState.AR:
                    ShowAR();
                    break;
                case WorkerAppScreenState.Certificates:
                    ShowCertificates();
                    break;
                case WorkerAppScreenState.Profile:
                    ShowProfile();
                    break;
                case WorkerAppScreenState.Home:
                default:
                    ShowHome();
                    break;
            }
        }

        /// <summary>
        /// Initiates the Fire & Explosion Response training scenario and authoritatively enables AR camera.
        /// </summary>
        public void StartFireTraining()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.TrainingFire;
            if (_homeRoot != null) _homeRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);

            // Authoritatively enable AR subsystem and camera
            if (ARModeController.Instance != null)
            {
                ARModeController.Instance.EnableAR();
            }

            // Activate Fire Interaction UI and ensure canvas is visible
            var fireUIs = FindObjectsByType<FireInteractionFeedbackUI>(FindObjectsInactive.Include);
            if (fireUIs == null || fireUIs.Length == 0)
            {
                var uiObj = new GameObject("FireInteractionFeedbackUI");
                var createdUI = uiObj.AddComponent<FireInteractionFeedbackUI>();
                createdUI.gameObject.SetActive(true);
                createdUI.ShowTrainingUI();
            }
            else
            {
                foreach (var fui in fireUIs)
                {
                    if (fui != null)
                    {
                        fui.gameObject.SetActive(true);
                        fui.ShowTrainingUI();
                    }
                }
            }

            var fireCtrl = FindAnyObjectByType<FireArInteractionController>(FindObjectsInactive.Include);
            if (fireCtrl != null)
            {
                fireCtrl.gameObject.SetActive(true);
                fireCtrl.enabled = true;
            }

            if (FireAudioService.Instance != null)
            {
                FireAudioService.Instance.PlayStepCompleted();
            }
        }

        /// <summary>
        /// Initiates the Gas Leak & Confined Space training scenario and authoritatively enables AR camera.
        /// </summary>
        public void StartGasTraining()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.TrainingGas;
            if (_homeRoot != null) _homeRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);

            // Authoritatively enable AR subsystem and camera
            if (ARModeController.Instance != null)
            {
                ARModeController.Instance.EnableAR();
            }

            // Activate Gas AR Interaction UI and ensure canvas is visible
            var gasUIs = FindObjectsByType<GasInteractionFeedbackUI>(FindObjectsInactive.Include);
            GasInteractionFeedbackUI primaryGasUI = null;
            if (gasUIs == null || gasUIs.Length == 0)
            {
                var uiObj = new GameObject("GasInteractionFeedbackUI");
                primaryGasUI = uiObj.AddComponent<GasInteractionFeedbackUI>();
                primaryGasUI.gameObject.SetActive(true);
                primaryGasUI.ShowTrainingUI();
            }
            else
            {
                foreach (var gui in gasUIs)
                {
                    if (gui != null)
                    {
                        gui.gameObject.SetActive(true);
                        gui.ShowTrainingUI();
                        if (primaryGasUI == null) primaryGasUI = gui;
                    }
                }
            }

            var gasCtrl = FindAnyObjectByType<GasArInteractionController>(FindObjectsInactive.Include);
            if (gasCtrl == null)
            {
                var ctrlObj = new GameObject("GasArInteractionController");
                gasCtrl = ctrlObj.AddComponent<GasArInteractionController>();
            }
            if (gasCtrl != null)
            {
                gasCtrl.gameObject.SetActive(true);
                gasCtrl.enabled = true;
                if (primaryGasUI != null)
                {
                    primaryGasUI.Controller = gasCtrl;
                }
            }

            if (FireAudioService.Instance != null)
            {
                FireAudioService.Instance.PlayStepCompleted();
            }
        }

        /// <summary>
        /// Returns from training scenario back to the Worker Home menu, disabling AR camera.
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

            var loc = LocaleService.Instance;
            bool isOffline = Application.internetReachability == NetworkReachability.NotReachable;
            if (isOffline)
            {
                string offlineText = loc.Get("status_offline", "OFFLINE");
                string savedLocally = loc.Get("status_saved_locally", "SAVED LOCALLY");
                _offlineBadgeText.text = $"● {offlineText} • {savedLocally}";
                _offlineBadgeText.color = UITheme.SuccessText;
                if (_offlineBadgeBg != null) _offlineBadgeBg.color = UITheme.SuccessSurface;
            }
            else
            {
                string onlineText = loc.Get("status_online", "ONLINE");
                string syncReady = loc.Get("status_sync_ready", "SYNC READY");
                _offlineBadgeText.text = $"● {onlineText} • {syncReady}";
                _offlineBadgeText.color = UITheme.PrimaryOrange;
                if (_offlineBadgeBg != null) _offlineBadgeBg.color = UITheme.PrimaryOrangeSurface;
            }
        }

        public void UpdateArScreenUI()
        {
            var loc = LocaleService.Instance;
            bool isArActive = ARModeController.Instance != null && ARModeController.Instance.IsARActive;

            if (_arTitleText != null)
            {
                _arTitleText.text = $"<b>{loc.Get("ar_screen_title", "AR SAFETY TRAINING")}</b>";
                _arTitleText.color = UITheme.TextPrimary;
            }
            if (_arSubtitleText != null)
            {
                _arSubtitleText.text = loc.Get("ar_screen_subtitle", "Interactive Industrial Safety Training");
                _arSubtitleText.color = UITheme.TextSecondary;
            }
            if (_arDescText != null)
            {
                _arDescText.text = loc.Get("ar_screen_desc", "Use your phone camera to enter an interactive safety scenario.");
                _arDescText.color = UITheme.TextPrimary;
            }

            if (_arStatusText != null)
            {
                _arStatusText.text = isArActive
                    ? $"● {loc.Get("ar_camera_active", "AR Camera is active. Look for floor surfaces.")}"
                    : $"○ {loc.Get("ar_camera_inactive", "Camera is currently inactive.")}";
                _arStatusText.color = isArActive ? UITheme.SuccessText : UITheme.TextSecondary;
            }

            if (_arStatusBg != null)
            {
                _arStatusBg.color = isArActive ? UITheme.SuccessSurface : UITheme.CardSecondaryBg;
            }

            if (_arToggleButtonText != null)
            {
                _arToggleButtonText.text = isArActive
                    ? $"<b>{loc.Get("btn_exit_ar", "EXIT AR")}</b>"
                    : $"<b>{loc.Get("btn_enable_ar", "ENABLE AR CAMERA")}</b>";
                _arToggleButtonText.color = UITheme.TextLightOnDark;
            }

            if (_arToggleBtnBg != null)
            {
                _arToggleBtnBg.color = isArActive
                    ? UITheme.DangerText // Crimson / exit
                    : UITheme.PrimaryOrange; // Primary action
            }

            if (_arStartFireShortcutBtn != null)
            {
                _arStartFireShortcutBtn.gameObject.SetActive(isArActive);
            }

            if (_arStartFireShortcutBtnText != null)
            {
                _arStartFireShortcutBtnText.text = $"<b>{loc.Get("btn_start_training", "START TRAINING →")}</b>";
                _arStartFireShortcutBtnText.color = UITheme.TextLightOnDark;
            }

            if (_navArText != null) _navArText.text = $"<b>{loc.Get("nav_ar", "AR")}</b>";
        }

        /// <summary>
        /// Refreshes all UI text elements to match the active locale and current state.
        /// </summary>
        public void RefreshTexts()
        {
            var loc = LocaleService.Instance;

            // Top Header
            if (_titleText != null)
            {
                _titleText.text = $"<b>{loc.Get("app_title", "Industrial Safety AR")}</b>";
                _titleText.color = UITheme.TextPrimary;
            }
            if (_welcomeBackText != null)
            {
                _welcomeBackText.text = $"{loc.Get("welcome_back", "Welcome back")}, <color=#F97316><b>{_workerName}</b></color>";
                _welcomeBackText.color = UITheme.TextSecondary;
            }
            if (_subtitleText != null)
            {
                _subtitleText.text = $"● {loc.Get("safety_training", "Safety Training")} • {loc.Get("app_subtitle", "Vocational Training Simulator • Jharkhand Industry")}";
                _subtitleText.color = UITheme.TextMuted;
            }

            // Profile Card (in Home view)
            if (_profileHeaderLabelText != null)
            {
                _profileHeaderLabelText.text = $"<color=#F97316><b>● {loc.Get("worker_profile", "WORKER PROFILE")}</b></color>";
            }
            if (_profileNameText != null)
            {
                _profileNameText.text = $"<b>{_workerName}</b>";
                _profileNameText.color = UITheme.TextPrimary;
            }
            if (_profileIdText != null)
            {
                _profileIdText.text = $"{loc.Get("worker_id_label", "Worker ID")}: <color=#F97316>{_workerId}</color>  |  {loc.Get("worker_division", "Division: Mining & Material Handling")}";
                _profileIdText.color = UITheme.TextSecondary;
            }

            // Modules Section Header
            if (_modulesHeaderText != null)
            {
                _modulesHeaderText.text = $"<b>{loc.Get("modules_header", "AVAILABLE MODULES")}</b>";
                _modulesHeaderText.color = UITheme.TextPrimary;
            }

            // Determine Fire Module Status based on actual state
            string fireStatusKey = "status_available";
            string fireStatusDefault = "AVAILABLE";
            string fireBtnKey = "btn_start_training";
            string fireBtnDefault = "START TRAINING →";
            Color fireBadgeCol = UITheme.SuccessSurface;
            Color fireTextCol = UITheme.SuccessText;

            var fireCtrl = FindAnyObjectByType<FireArInteractionController>(FindObjectsInactive.Include);
            if (fireCtrl != null)
            {
                if (fireCtrl.LatestAssessment != null)
                {
                    fireStatusKey = "status_completed";
                    fireStatusDefault = "COMPLETED";
                    fireBtnKey = "btn_retake";
                    fireBtnDefault = "RETAKE TRAINING →";
                    fireBadgeCol = UITheme.SuccessSurface;
                    fireTextCol = UITheme.SuccessText;
                }
                else if (fireCtrl.StepNavigator != null && fireCtrl.StepNavigator.CurrentStepIndex > 0)
                {
                    fireStatusKey = "status_in_progress";
                    fireStatusDefault = "IN PROGRESS";
                    fireBtnKey = "btn_continue_training";
                    fireBtnDefault = "CONTINUE TRAINING →";
                    fireBadgeCol = UITheme.PrimaryOrangeSurface;
                    fireTextCol = UITheme.PrimaryOrange;
                }
            }

            // Fire Card
            if (_fireTitleText != null)
            {
                _fireTitleText.text = $"<b>{loc.Get("module_fire_title", "Fire & Explosion Response")}</b>";
                _fireTitleText.color = UITheme.TextPrimary;
            }
            if (_fireArBadgeText != null)
            {
                _fireArBadgeText.text = $"<b>{loc.Get("badge_ar", "AR")}</b>";
                _fireArBadgeText.color = UITheme.TextLightOnDark;
            }
            if (_fireDescText != null)
            {
                _fireDescText.text = loc.Get("module_fire_desc", "9-step industrial conveyor fire response: hazard detection, classification, P.A.S.S. extinguisher procedure, and emergency evacuation.");
                _fireDescText.color = UITheme.TextSecondary;
            }
            if (_fireStatusText != null)
            {
                _fireStatusText.text = $"● {loc.Get(fireStatusKey, fireStatusDefault)}";
                _fireStatusText.color = fireTextCol;
            }
            if (_fireStatusBg != null) _fireStatusBg.color = fireBadgeCol;
            if (_fireOfflineTagText != null)
            {
                _fireOfflineTagText.text = $"● {loc.Get("available_offline", "Available Offline")}";
                _fireOfflineTagText.color = UITheme.SuccessText;
            }
            if (_fireStartButtonText != null)
            {
                _fireStartButtonText.text = $"<b>{loc.Get(fireBtnKey, fireBtnDefault)}</b>";
                _fireStartButtonText.color = UITheme.TextLightOnDark;
            }

            // Gas Card
            if (_gasTitleText != null)
            {
                _gasTitleText.text = $"<b>{loc.Get("module_gas_title", "Gas Leak & Confined Space Safety")}</b>";
                _gasTitleText.color = UITheme.TextPrimary;
            }
            if (_gasArBadgeText != null)
            {
                _gasArBadgeText.text = $"<b>{loc.Get("badge_ar", "AR")}</b>";
                _gasArBadgeText.color = UITheme.TextLightOnDark;
            }
            if (_gasDescText != null)
            {
                _gasDescText.text = loc.Get("module_gas_desc", "Atmospheric monitoring, multi-gas detector calibration, forced air ventilation, and confined space entry rescue protocols.");
                _gasDescText.color = UITheme.TextSecondary;
            }
            if (_gasStatusText != null)
            {
                _gasStatusText.text = $"● {loc.Get("status_available", "AVAILABLE")}";
                _gasStatusText.color = UITheme.SuccessText;
            }
            if (_gasStartButtonText != null)
            {
                _gasStartButtonText.text = $"<b>{loc.Get("btn_start_training", "START TRAINING →")}</b>";
                _gasStartButtonText.color = UITheme.TextLightOnDark;
            }

            // Home Prominent Settings Button
            if (_homeSettingsButtonText != null)
            {
                _homeSettingsButtonText.text = $"<b>{loc.Get("settings_title", "APPLICATION SETTINGS")}</b>";
                _homeSettingsButtonText.color = UITheme.TextPrimary;
            }

            // AR Screen
            UpdateArScreenUI();

            // Certificates View
            if (_certTitleText != null)
            {
                _certTitleText.text = $"<b>{loc.Get("certificates_title", "Certificates")}</b>";
                _certTitleText.color = UITheme.TextPrimary;
            }
            if (_certIconText != null) _certIconText.text = "";
            if (_certDescText != null)
            {
                _certDescText.text = loc.Get("certificates_empty_desc", "Training certificates will appear here after successful training and synchronization.");
                _certDescText.color = UITheme.TextSecondary;
            }
            if (_certSubText != null)
            {
                _certSubText.text = loc.Get("certificates_empty_sub", "No official certificates issued yet.");
                _certSubText.color = UITheme.TextMuted;
            }

            // Profile View
            if (_profileViewTitleText != null)
            {
                _profileViewTitleText.text = $"<b>{loc.Get("worker_profile", "WORKER PROFILE")}</b>";
                _profileViewTitleText.color = UITheme.TextPrimary;
            }
            if (_pvNameLabel != null)
            {
                _pvNameLabel.text = loc.Get("worker_name_label", "Worker Name");
                _pvNameLabel.color = UITheme.TextSecondary;
            }
            if (_pvNameVal != null)
            {
                _pvNameVal.text = $"<b>{_workerName}</b>";
                _pvNameVal.color = UITheme.TextPrimary;
            }
            if (_pvIdLabel != null)
            {
                _pvIdLabel.text = loc.Get("worker_id_label", "Worker ID");
                _pvIdLabel.color = UITheme.TextSecondary;
            }
            if (_pvIdVal != null)
            {
                _pvIdVal.text = $"<color=#F97316>{_workerId}</color>";
                _pvIdVal.color = UITheme.PrimaryOrange;
            }
            if (_pvDivLabel != null)
            {
                _pvDivLabel.text = loc.Get("worker_division", "Division: Mining & Material Handling");
                _pvDivLabel.color = UITheme.TextPrimary;
            }
            if (_pvLangLabel != null)
            {
                _pvLangLabel.text = loc.Get("preferred_language_label", "Preferred Language");
                _pvLangLabel.color = UITheme.TextSecondary;
            }
            if (_pvLangVal != null)
            {
                _pvLangVal.text = $"<b>{loc.CurrentLanguageDisplayName}</b>";
                _pvLangVal.color = UITheme.PrimaryOrange;
            }
            if (_pvSettingsBtnText != null)
            {
                _pvSettingsBtnText.text = $"<b>{loc.Get("settings_title", "APPLICATION SETTINGS")}</b>";
                _pvSettingsBtnText.color = UITheme.TextPrimary;
            }

            // Bottom Navigation Bar
            if (_navHomeText != null) _navHomeText.text = $"<b>{loc.Get("nav_home", "HOME")}</b>";
            if (_navArText != null) _navArText.text = $"<b>{loc.Get("nav_ar", "AR")}</b>";
            if (_navCertText != null) _navCertText.text = $"<b>{loc.Get("nav_certificates", "CERTIFICATES")}</b>";
            if (_navProfText != null) _navProfText.text = $"<b>{loc.Get("nav_profile", "PROFILE")}</b>";
            UpdateNavHighlight();

            // Settings View
            if (_settingsTitleText != null)
            {
                _settingsTitleText.text = $"<b>{loc.Get("settings_title", "APPLICATION SETTINGS")}</b>";
                _settingsTitleText.color = UITheme.TextPrimary;
            }
            if (_soundToggleLabel != null)
            {
                _soundToggleLabel.text = loc.Get("sound_effects", "Sound Effects");
                _soundToggleLabel.color = UITheme.TextPrimary;
            }
            if (_alarmToggleLabel != null)
            {
                _alarmToggleLabel.text = loc.Get("emergency_alarm", "Emergency Alarm Siren");
                _alarmToggleLabel.color = UITheme.TextPrimary;
            }
            if (_volumeLabel != null)
            {
                _volumeLabel.text = loc.Get("effects_volume", "Effects Volume");
                _volumeLabel.color = UITheme.TextPrimary;
            }
            if (_languageHeader != null)
            {
                _languageHeader.text = loc.Get("language_header", "Language / भाषा / ᱯᱟᱹᱨᱥᱤ");
                _languageHeader.color = UITheme.TextPrimary;
            }
            if (_settingsCloseButtonText != null)
            {
                _settingsCloseButtonText.text = $"<b>{loc.Get("btn_close", "CLOSE [X]")}</b>";
                _settingsCloseButtonText.color = UITheme.TextLightOnDark;
            }

            UpdateSettingsControls();
        }

        private void UpdateSettingsControls()
        {
            var loc = LocaleService.Instance;
            var audio = FireAudioService.Instance;

            if (_soundToggleButtonText != null)
            {
                bool on = audio.IsSoundEnabled;
                _soundToggleButtonText.text = on ? $"<b>{loc.Get("state_on", "ON")}</b>" : $"<b>{loc.Get("state_off", "OFF")}</b>";
            }

            if (_alarmToggleButtonText != null)
            {
                bool on = audio.IsEmergencyAlarmEnabled;
                _alarmToggleButtonText.text = on ? $"<b>{loc.Get("state_on", "ON")}</b>" : $"<b>{loc.Get("state_off", "OFF")}</b>";
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
        /// Procedurally constructs the complete Home, AR, Certificates, Profile, and Settings UI hierarchies on a ScreenSpaceOverlay Canvas.
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
            // 2. HOME SCREEN SHELL CONTAINER
            // =========================================================================
            _homeRoot = new GameObject("HomeScreen");
            _homeRoot.transform.SetParent(_rootCanvas.transform, false);

            var homeRect = _homeRoot.AddComponent<RectTransform>();
            homeRect.anchorMin = Vector2.zero;
            homeRect.anchorMax = Vector2.one;
            homeRect.offsetMin = Vector2.zero;
            homeRect.offsetMax = Vector2.zero;

            // Full light background
            var homeBg = _homeRoot.AddComponent<Image>();
            homeBg.color = UITheme.ScreenBackground;

            // -------------------------------------------------------------
            // Top Header Bar (y: 0.89 to 0.99)
            // -------------------------------------------------------------
            var headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(_homeRoot.transform, false);
            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.04f, 0.89f);
            headerRect.anchorMax = new Vector2(0.96f, 0.99f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            // App Title
            var titleObj = new GameObject("AppTitle");
            titleObj.transform.SetParent(headerObj.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.62f);
            titleRect.anchorMax = new Vector2(0.80f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            _titleText = titleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _titleText.font = defaultFont;
            _titleText.fontSize = 30;
            _titleText.alignment = TextAlignmentOptions.Left;
            _titleText.color = UITheme.TextPrimary;

            // Welcome back line
            var welcomeObj = new GameObject("WelcomeBack");
            welcomeObj.transform.SetParent(headerObj.transform, false);
            var welcomeRect = welcomeObj.AddComponent<RectTransform>();
            welcomeRect.anchorMin = new Vector2(0f, 0.32f);
            welcomeRect.anchorMax = new Vector2(0.80f, 0.62f);
            welcomeRect.offsetMin = Vector2.zero;
            welcomeRect.offsetMax = Vector2.zero;

            _welcomeBackText = welcomeObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _welcomeBackText.font = defaultFont;
            _welcomeBackText.fontSize = 22;
            _welcomeBackText.alignment = TextAlignmentOptions.Left;
            _welcomeBackText.color = UITheme.TextSecondary;

            // Subtitle / Safety Training tag
            var subObj = new GameObject("AppSubtitle");
            subObj.transform.SetParent(headerObj.transform, false);
            var subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0f);
            subRect.anchorMax = new Vector2(0.80f, 0.32f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;

            _subtitleText = subObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _subtitleText.font = defaultFont;
            _subtitleText.fontSize = 18;
            _subtitleText.alignment = TextAlignmentOptions.Left;
            _subtitleText.color = UITheme.TextMuted;

            // Header Settings Button (touch target >= 44dp)
            var headerSettingsBtnObj = new GameObject("HeaderSettingsButton");
            headerSettingsBtnObj.transform.SetParent(headerObj.transform, false);
            var hsBtnRect = headerSettingsBtnObj.AddComponent<RectTransform>();
            hsBtnRect.anchorMin = new Vector2(0.72f, 0.16f);
            hsBtnRect.anchorMax = new Vector2(1.0f, 0.84f);
            hsBtnRect.offsetMin = Vector2.zero;
            hsBtnRect.offsetMax = Vector2.zero;

            var hsBtnImg = headerSettingsBtnObj.AddComponent<Image>();
            hsBtnImg.color = UITheme.CardSecondaryBg;
            _headerSettingsButton = headerSettingsBtnObj.AddComponent<Button>();
            var hsTapGated = headerSettingsBtnObj.AddComponent<TapGatedButton>();
            hsTapGated.Initialize(() => OpenSettings());

            var hsBtnTextObj = new GameObject("Text");
            hsBtnTextObj.transform.SetParent(headerSettingsBtnObj.transform, false);
            var hsBtnTextRect = hsBtnTextObj.AddComponent<RectTransform>();
            hsBtnTextRect.anchorMin = Vector2.zero;
            hsBtnTextRect.anchorMax = Vector2.one;

            var hsBtnText = hsBtnTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) hsBtnText.font = defaultFont;
            hsBtnText.text = "<b>SETTINGS</b>";
            hsBtnText.fontSize = 18;
            hsBtnText.alignment = TextAlignmentOptions.Center;
            hsBtnText.color = UITheme.TextPrimary;

            // -------------------------------------------------------------
            // Main Content Area (y: 0.12 to 0.88)
            // -------------------------------------------------------------
            var contentContainer = new GameObject("ContentContainer");
            contentContainer.transform.SetParent(_homeRoot.transform, false);
            var ccRect = contentContainer.AddComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(0.04f, 0.12f);
            ccRect.anchorMax = new Vector2(0.96f, 0.88f);
            ccRect.offsetMin = Vector2.zero;
            ccRect.offsetMax = Vector2.zero;

            // =============================================================
            // TAB 1: HOME CONTENT ROOT (Modules + Profile Card)
            // =============================================================
            _homeContentRoot = new GameObject("HomeContentRoot");
            _homeContentRoot.transform.SetParent(contentContainer.transform, false);
            var hcrRect = _homeContentRoot.AddComponent<RectTransform>();
            hcrRect.anchorMin = Vector2.zero;
            hcrRect.anchorMax = Vector2.one;
            hcrRect.offsetMin = Vector2.zero;
            hcrRect.offsetMax = Vector2.zero;

            // Worker Profile Card (in Home view) (y: 0.84 to 0.98 of container)
            var profileCardObj = new GameObject("WorkerProfileCard");
            profileCardObj.transform.SetParent(_homeContentRoot.transform, false);
            var pcRect = profileCardObj.AddComponent<RectTransform>();
            pcRect.anchorMin = new Vector2(0f, 0.84f);
            pcRect.anchorMax = new Vector2(1f, 0.98f);
            pcRect.offsetMin = Vector2.zero;
            pcRect.offsetMax = Vector2.zero;

            var pcBg = profileCardObj.AddComponent<Image>();
            pcBg.color = UITheme.CardBackground;

            var pHeaderObj = new GameObject("ProfileHeader");
            pHeaderObj.transform.SetParent(profileCardObj.transform, false);
            var phRect = pHeaderObj.AddComponent<RectTransform>();
            phRect.anchorMin = new Vector2(0.04f, 0.65f);
            phRect.anchorMax = new Vector2(0.96f, 0.95f);
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;

            _profileHeaderLabelText = pHeaderObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _profileHeaderLabelText.font = defaultFont;
            _profileHeaderLabelText.text = "<color=#F97316><b>● WORKER PROFILE</b></color>";
            _profileHeaderLabelText.fontSize = 18;
            _profileHeaderLabelText.alignment = TextAlignmentOptions.Left;

            var pNameObj = new GameObject("ProfileName");
            pNameObj.transform.SetParent(profileCardObj.transform, false);
            var pnRect = pNameObj.AddComponent<RectTransform>();
            pnRect.anchorMin = new Vector2(0.04f, 0.32f);
            pnRect.anchorMax = new Vector2(0.96f, 0.65f);
            pnRect.offsetMin = Vector2.zero;
            pnRect.offsetMax = Vector2.zero;

            _profileNameText = pNameObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _profileNameText.font = defaultFont;
            _profileNameText.text = $"<b>{_workerName}</b>";
            _profileNameText.fontSize = 26;
            _profileNameText.alignment = TextAlignmentOptions.Left;
            _profileNameText.color = UITheme.TextPrimary;

            var pIdObj = new GameObject("ProfileIdAndDivision");
            pIdObj.transform.SetParent(profileCardObj.transform, false);
            var pidRect = pIdObj.AddComponent<RectTransform>();
            pidRect.anchorMin = new Vector2(0.04f, 0.05f);
            pidRect.anchorMax = new Vector2(0.96f, 0.32f);
            pidRect.offsetMin = Vector2.zero;
            pidRect.offsetMax = Vector2.zero;

            _profileIdText = pIdObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _profileIdText.font = defaultFont;
            _profileIdText.text = $"Worker ID: <color=#F97316>{_workerId}</color>  |  Division: Mining & Material Handling";
            _profileIdText.fontSize = 20;
            _profileIdText.alignment = TextAlignmentOptions.Left;
            _profileIdText.color = UITheme.TextSecondary;

            // Section Header: Modules (y: 0.77 to 0.82 of container)
            var mHeaderObj = new GameObject("ModulesHeader");
            mHeaderObj.transform.SetParent(_homeContentRoot.transform, false);
            var mhRect = mHeaderObj.AddComponent<RectTransform>();
            mhRect.anchorMin = new Vector2(0f, 0.77f);
            mhRect.anchorMax = new Vector2(1f, 0.82f);
            mhRect.offsetMin = Vector2.zero;
            mhRect.offsetMax = Vector2.zero;

            _modulesHeaderText = mHeaderObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _modulesHeaderText.font = defaultFont;
            _modulesHeaderText.text = "<b>AVAILABLE MODULES</b>";
            _modulesHeaderText.fontSize = 24;
            _modulesHeaderText.alignment = TextAlignmentOptions.Left;
            _modulesHeaderText.color = UITheme.TextPrimary;

            // Module Card 1: Fire & Explosion Response (y: 0.44 to 0.75 of container)
            var fireCardObj = new GameObject("ModuleCard_Fire");
            fireCardObj.transform.SetParent(_homeContentRoot.transform, false);
            var fcRect = fireCardObj.AddComponent<RectTransform>();
            fcRect.anchorMin = new Vector2(0f, 0.44f);
            fcRect.anchorMax = new Vector2(1f, 0.75f);
            fcRect.offsetMin = Vector2.zero;
            fcRect.offsetMax = Vector2.zero;

            var fcBg = fireCardObj.AddComponent<Image>();
            fcBg.color = UITheme.CardBackground;

            // Fire AR Badge
            var fireArBadgeObj = new GameObject("FireArBadge");
            fireArBadgeObj.transform.SetParent(fireCardObj.transform, false);
            var fabRect = fireArBadgeObj.AddComponent<RectTransform>();
            fabRect.anchorMin = new Vector2(0.04f, 0.78f);
            fabRect.anchorMax = new Vector2(0.14f, 0.94f);
            fabRect.offsetMin = Vector2.zero;
            fabRect.offsetMax = Vector2.zero;

            var fabBg = fireArBadgeObj.AddComponent<Image>();
            fabBg.color = UITheme.PrimaryOrange;

            var fabTextObj = new GameObject("Text");
            fabTextObj.transform.SetParent(fireArBadgeObj.transform, false);
            var fabtRect = fabTextObj.AddComponent<RectTransform>();
            fabtRect.anchorMin = Vector2.zero;
            fabtRect.anchorMax = Vector2.one;

            _fireArBadgeText = fabTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireArBadgeText.font = defaultFont;
            _fireArBadgeText.text = "<b>AR</b>";
            _fireArBadgeText.fontSize = 16;
            _fireArBadgeText.alignment = TextAlignmentOptions.Center;
            _fireArBadgeText.color = UITheme.TextLightOnDark;

            // Fire Title
            var fireTitleObj = new GameObject("FireTitle");
            fireTitleObj.transform.SetParent(fireCardObj.transform, false);
            var ftRect = fireTitleObj.AddComponent<RectTransform>();
            ftRect.anchorMin = new Vector2(0.16f, 0.75f);
            ftRect.anchorMax = new Vector2(0.68f, 0.96f);
            ftRect.offsetMin = Vector2.zero;
            ftRect.offsetMax = Vector2.zero;

            _fireTitleText = fireTitleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireTitleText.font = defaultFont;
            _fireTitleText.text = "<b>Fire & Explosion Response</b>";
            _fireTitleText.fontSize = 24;
            _fireTitleText.alignment = TextAlignmentOptions.Left;
            _fireTitleText.color = UITheme.TextPrimary;

            // Fire Status Badge
            var fireStatusObj = new GameObject("FireStatusBadge");
            fireStatusObj.transform.SetParent(fireCardObj.transform, false);
            var fsbRect = fireStatusObj.AddComponent<RectTransform>();
            fsbRect.anchorMin = new Vector2(0.70f, 0.76f);
            fsbRect.anchorMax = new Vector2(0.96f, 0.94f);
            fsbRect.offsetMin = Vector2.zero;
            fsbRect.offsetMax = Vector2.zero;

            _fireStatusBg = fireStatusObj.AddComponent<Image>();
            _fireStatusBg.color = UITheme.SuccessSurface;

            var fsbTextObj = new GameObject("Text");
            fsbTextObj.transform.SetParent(fireStatusObj.transform, false);
            var fsbtRect = fsbTextObj.AddComponent<RectTransform>();
            fsbtRect.anchorMin = Vector2.zero;
            fsbtRect.anchorMax = Vector2.one;

            _fireStatusText = fsbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireStatusText.font = defaultFont;
            _fireStatusText.text = "● AVAILABLE";
            _fireStatusText.fontSize = 18;
            _fireStatusText.alignment = TextAlignmentOptions.Center;
            _fireStatusText.color = UITheme.SuccessText;

            // Fire Description
            var fireDescObj = new GameObject("FireDescription");
            fireDescObj.transform.SetParent(fireCardObj.transform, false);
            var fdRect = fireDescObj.AddComponent<RectTransform>();
            fdRect.anchorMin = new Vector2(0.04f, 0.36f);
            fdRect.anchorMax = new Vector2(0.96f, 0.72f);
            fdRect.offsetMin = Vector2.zero;
            fdRect.offsetMax = Vector2.zero;

            _fireDescText = fireDescObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireDescText.font = defaultFont;
            _fireDescText.text = "9-step industrial conveyor fire response: hazard detection, classification, P.A.S.S. extinguisher procedure, and emergency evacuation.";
            _fireDescText.fontSize = 20;
            _fireDescText.alignment = TextAlignmentOptions.Left;
            _fireDescText.color = UITheme.TextSecondary;

            // Fire Available Offline tag
            var fireOfflineTagObj = new GameObject("FireOfflineTag");
            fireOfflineTagObj.transform.SetParent(fireCardObj.transform, false);
            var fotRect = fireOfflineTagObj.AddComponent<RectTransform>();
            fotRect.anchorMin = new Vector2(0.04f, 0.22f);
            fotRect.anchorMax = new Vector2(0.96f, 0.34f);
            fotRect.offsetMin = Vector2.zero;
            fotRect.offsetMax = Vector2.zero;

            _fireOfflineTagText = fireOfflineTagObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireOfflineTagText.font = defaultFont;
            _fireOfflineTagText.text = "● Available Offline";
            _fireOfflineTagText.fontSize = 18;
            _fireOfflineTagText.alignment = TextAlignmentOptions.Left;
            _fireOfflineTagText.color = UITheme.SuccessText;

            // Fire Start Button (touch target >= 48dp)
            var fireBtnObj = new GameObject("StartFireButton");
            fireBtnObj.transform.SetParent(fireCardObj.transform, false);
            var fbRect = fireBtnObj.AddComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.04f, 0.04f);
            fbRect.anchorMax = new Vector2(0.96f, 0.20f);
            fbRect.offsetMin = Vector2.zero;
            fbRect.offsetMax = Vector2.zero;

            var fbImg = fireBtnObj.AddComponent<Image>();
            fbImg.color = UITheme.PrimaryOrange;
            _fireStartButton = fireBtnObj.AddComponent<Button>();
            var fireTapGated = fireBtnObj.AddComponent<TapGatedButton>();
            fireTapGated.Initialize(() => StartFireTraining());

            var fbTextObj = new GameObject("Text");
            fbTextObj.transform.SetParent(fireBtnObj.transform, false);
            var fbtRect = fbTextObj.AddComponent<RectTransform>();
            fbtRect.anchorMin = Vector2.zero;
            fbtRect.anchorMax = Vector2.one;

            _fireStartButtonText = fbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireStartButtonText.font = defaultFont;
            _fireStartButtonText.text = "<b>START TRAINING →</b>";
            _fireStartButtonText.fontSize = 24;
            _fireStartButtonText.alignment = TextAlignmentOptions.Center;
            _fireStartButtonText.color = UITheme.TextLightOnDark;

            // Module Card 2: Gas Leak & Confined Space (y: 0.16 to 0.42 of container)
            var gasCardObj = new GameObject("ModuleCard_Gas");
            gasCardObj.transform.SetParent(_homeContentRoot.transform, false);
            var gcRect = gasCardObj.AddComponent<RectTransform>();
            gcRect.anchorMin = new Vector2(0f, 0.16f);
            gcRect.anchorMax = new Vector2(1f, 0.42f);
            gcRect.offsetMin = Vector2.zero;
            gcRect.offsetMax = Vector2.zero;

            var gcBg = gasCardObj.AddComponent<Image>();
            gcBg.color = UITheme.CardBackground;

            // Gas AR Badge
            var gasArBadgeObj = new GameObject("GasArBadge");
            gasArBadgeObj.transform.SetParent(gasCardObj.transform, false);
            var gabRect = gasArBadgeObj.AddComponent<RectTransform>();
            gabRect.anchorMin = new Vector2(0.04f, 0.78f);
            gabRect.anchorMax = new Vector2(0.14f, 0.94f);
            gabRect.offsetMin = Vector2.zero;
            gabRect.offsetMax = Vector2.zero;

            var gabBg = gasArBadgeObj.AddComponent<Image>();
            gabBg.color = UITheme.PrimaryOrange;

            var gabTextObj = new GameObject("Text");
            gabTextObj.transform.SetParent(gasArBadgeObj.transform, false);
            var gabtRect = gabTextObj.AddComponent<RectTransform>();
            gabtRect.anchorMin = Vector2.zero;
            gabtRect.anchorMax = Vector2.one;

            _gasArBadgeText = gabTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasArBadgeText.font = defaultFont;
            _gasArBadgeText.text = "<b>AR</b>";
            _gasArBadgeText.fontSize = 16;
            _gasArBadgeText.alignment = TextAlignmentOptions.Center;
            _gasArBadgeText.color = UITheme.TextLightOnDark;

            // Gas Title
            var gasTitleObj = new GameObject("GasTitle");
            gasTitleObj.transform.SetParent(gasCardObj.transform, false);
            var gtRect = gasTitleObj.AddComponent<RectTransform>();
            gtRect.anchorMin = new Vector2(0.16f, 0.75f);
            gtRect.anchorMax = new Vector2(0.68f, 0.96f);
            gtRect.offsetMin = Vector2.zero;
            gtRect.offsetMax = Vector2.zero;

            _gasTitleText = gasTitleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasTitleText.font = defaultFont;
            _gasTitleText.text = "<b>Gas Leak & Confined Space Safety</b>";
            _gasTitleText.fontSize = 24;
            _gasTitleText.alignment = TextAlignmentOptions.Left;
            _gasTitleText.color = UITheme.TextPrimary;

            // Gas Status Badge
            var gasStatusObj = new GameObject("GasStatusBadge");
            gasStatusObj.transform.SetParent(gasCardObj.transform, false);
            var gsbRect = gasStatusObj.AddComponent<RectTransform>();
            gsbRect.anchorMin = new Vector2(0.70f, 0.76f);
            gsbRect.anchorMax = new Vector2(0.96f, 0.94f);
            gsbRect.offsetMin = Vector2.zero;
            gsbRect.offsetMax = Vector2.zero;

            var gsbBg = gasStatusObj.AddComponent<Image>();
            gsbBg.color = UITheme.SuccessSurface;

            var gsbTextObj = new GameObject("Text");
            gsbTextObj.transform.SetParent(gasStatusObj.transform, false);
            var gsbtRect = gsbTextObj.AddComponent<RectTransform>();
            gsbtRect.anchorMin = Vector2.zero;
            gsbtRect.anchorMax = Vector2.one;

            _gasStatusText = gsbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasStatusText.font = defaultFont;
            _gasStatusText.text = "● AVAILABLE";
            _gasStatusText.fontSize = 18;
            _gasStatusText.alignment = TextAlignmentOptions.Center;
            _gasStatusText.color = UITheme.SuccessText;

            // Gas Description
            var gasDescObj = new GameObject("GasDescription");
            gasDescObj.transform.SetParent(gasCardObj.transform, false);
            var gdRect = gasDescObj.AddComponent<RectTransform>();
            gdRect.anchorMin = new Vector2(0.04f, 0.30f);
            gdRect.anchorMax = new Vector2(0.96f, 0.72f);
            gdRect.offsetMin = Vector2.zero;
            gdRect.offsetMax = Vector2.zero;

            _gasDescText = gasDescObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasDescText.font = defaultFont;
            _gasDescText.text = "Atmospheric monitoring, multi-gas detector calibration, forced air ventilation, and confined space entry rescue protocols.";
            _gasDescText.fontSize = 20;
            _gasDescText.alignment = TextAlignmentOptions.Left;
            _gasDescText.color = UITheme.TextSecondary;

            // Gas Button (Active)
            var gasBtnObj = new GameObject("GasStartButton");
            gasBtnObj.transform.SetParent(gasCardObj.transform, false);
            var gbRect = gasBtnObj.AddComponent<RectTransform>();
            gbRect.anchorMin = new Vector2(0.04f, 0.04f);
            gbRect.anchorMax = new Vector2(0.96f, 0.24f);
            gbRect.offsetMin = Vector2.zero;
            gbRect.offsetMax = Vector2.zero;

            var gbImg = gasBtnObj.AddComponent<Image>();
            gbImg.color = UITheme.PrimaryOrange;
            _gasStartButton = gasBtnObj.AddComponent<Button>();
            _gasStartButton.targetGraphic = gbImg;
            _gasStartButton.interactable = true;
            var gbTap = gasBtnObj.AddComponent<TapGatedButton>();
            gbTap.Initialize(() => StartGasTraining());

            var gbTextObj = new GameObject("Text");
            gbTextObj.transform.SetParent(gasBtnObj.transform, false);
            var gbtRect = gbTextObj.AddComponent<RectTransform>();
            gbtRect.anchorMin = Vector2.zero;
            gbtRect.anchorMax = Vector2.one;

            _gasStartButtonText = gbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasStartButtonText.font = defaultFont;
            _gasStartButtonText.text = "<b>START TRAINING →</b>";
            _gasStartButtonText.fontSize = 24;
            _gasStartButtonText.alignment = TextAlignmentOptions.Center;
            _gasStartButtonText.color = UITheme.TextLightOnDark;

            // Prominent Settings Button in Home view (y: 0.02 to 0.12 of container)
            var prominentSettingsBtnObj = new GameObject("ProminentSettingsButton");
            prominentSettingsBtnObj.transform.SetParent(_homeContentRoot.transform, false);
            var psbRect = prominentSettingsBtnObj.AddComponent<RectTransform>();
            psbRect.anchorMin = new Vector2(0f, 0.02f);
            psbRect.anchorMax = new Vector2(1f, 0.12f);
            psbRect.offsetMin = Vector2.zero;
            psbRect.offsetMax = Vector2.zero;

            var psbImg = prominentSettingsBtnObj.AddComponent<Image>();
            psbImg.color = UITheme.CardSecondaryBg;
            _homeSettingsButton = prominentSettingsBtnObj.AddComponent<Button>();
            var psbTapGated = prominentSettingsBtnObj.AddComponent<TapGatedButton>();
            psbTapGated.Initialize(() => OpenSettings());

            var psbTextObj = new GameObject("Text");
            psbTextObj.transform.SetParent(prominentSettingsBtnObj.transform, false);
            var psbtRect = psbTextObj.AddComponent<RectTransform>();
            psbtRect.anchorMin = Vector2.zero;
            psbtRect.anchorMax = Vector2.one;

            _homeSettingsButtonText = psbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _homeSettingsButtonText.font = defaultFont;
            _homeSettingsButtonText.text = "<b>APPLICATION SETTINGS</b>";
            _homeSettingsButtonText.fontSize = 20;
            _homeSettingsButtonText.alignment = TextAlignmentOptions.Center;
            _homeSettingsButtonText.color = UITheme.TextPrimary;

            // =============================================================
            // TAB 2: AR CONTENT ROOT (Controlled AR Entry & Camera Toggle)
            // =============================================================
            _arContentRoot = new GameObject("ArContentRoot");
            _arContentRoot.transform.SetParent(contentContainer.transform, false);
            var arcrRect = _arContentRoot.AddComponent<RectTransform>();
            arcrRect.anchorMin = Vector2.zero;
            arcrRect.anchorMax = Vector2.one;
            arcrRect.offsetMin = Vector2.zero;
            arcrRect.offsetMax = Vector2.zero;

            var arCardObj = new GameObject("ArLaunchCard");
            arCardObj.transform.SetParent(_arContentRoot.transform, false);
            var arCardRect = arCardObj.AddComponent<RectTransform>();
            arCardRect.anchorMin = new Vector2(0.04f, 0.10f);
            arCardRect.anchorMax = new Vector2(0.96f, 0.90f);
            arCardRect.offsetMin = Vector2.zero;
            arCardRect.offsetMax = Vector2.zero;

            var arCardBg = arCardObj.AddComponent<Image>();
            arCardBg.color = UITheme.CardBackground;

            // AR Screen Title
            var arTitleObj = new GameObject("ArTitle");
            arTitleObj.transform.SetParent(arCardObj.transform, false);
            var atRect = arTitleObj.AddComponent<RectTransform>();
            atRect.anchorMin = new Vector2(0.06f, 0.82f);
            atRect.anchorMax = new Vector2(0.94f, 0.94f);
            atRect.offsetMin = Vector2.zero;
            atRect.offsetMax = Vector2.zero;

            _arTitleText = arTitleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _arTitleText.font = defaultFont;
            _arTitleText.text = "<b>AR SAFETY TRAINING</b>";
            _arTitleText.fontSize = 28;
            _arTitleText.alignment = TextAlignmentOptions.Center;
            _arTitleText.color = UITheme.TextPrimary;

            // AR Screen Subtitle
            var arSubObj = new GameObject("ArSubtitle");
            arSubObj.transform.SetParent(arCardObj.transform, false);
            var asRect = arSubObj.AddComponent<RectTransform>();
            asRect.anchorMin = new Vector2(0.06f, 0.74f);
            asRect.anchorMax = new Vector2(0.94f, 0.82f);
            asRect.offsetMin = Vector2.zero;
            asRect.offsetMax = Vector2.zero;

            _arSubtitleText = arSubObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _arSubtitleText.font = defaultFont;
            _arSubtitleText.text = "Interactive Industrial Safety Training";
            _arSubtitleText.fontSize = 20;
            _arSubtitleText.alignment = TextAlignmentOptions.Center;
            _arSubtitleText.color = UITheme.TextSecondary;

            // AR Description Body
            var arDescObj = new GameObject("ArDesc");
            arDescObj.transform.SetParent(arCardObj.transform, false);
            var adRect = arDescObj.AddComponent<RectTransform>();
            adRect.anchorMin = new Vector2(0.08f, 0.50f);
            adRect.anchorMax = new Vector2(0.92f, 0.70f);
            adRect.offsetMin = Vector2.zero;
            adRect.offsetMax = Vector2.zero;

            _arDescText = arDescObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _arDescText.font = defaultFont;
            _arDescText.text = "Use your phone camera to enter an interactive safety scenario.";
            _arDescText.fontSize = 22;
            _arDescText.alignment = TextAlignmentOptions.Center;
            _arDescText.color = UITheme.TextPrimary;

            // AR Camera Status Indicator
            var arStatusBoxObj = new GameObject("ArStatusBox");
            arStatusBoxObj.transform.SetParent(arCardObj.transform, false);
            var asbRect = arStatusBoxObj.AddComponent<RectTransform>();
            asbRect.anchorMin = new Vector2(0.08f, 0.36f);
            asbRect.anchorMax = new Vector2(0.92f, 0.46f);
            asbRect.offsetMin = Vector2.zero;
            asbRect.offsetMax = Vector2.zero;

            _arStatusBg = arStatusBoxObj.AddComponent<Image>();
            _arStatusBg.color = UITheme.CardSecondaryBg;

            var arStatusTextObj = new GameObject("Text");
            arStatusTextObj.transform.SetParent(arStatusBoxObj.transform, false);
            var astRect = arStatusTextObj.AddComponent<RectTransform>();
            astRect.anchorMin = Vector2.zero;
            astRect.anchorMax = Vector2.one;

            _arStatusText = arStatusTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _arStatusText.font = defaultFont;
            _arStatusText.text = "○ Camera is currently inactive.";
            _arStatusText.fontSize = 20;
            _arStatusText.alignment = TextAlignmentOptions.Center;
            _arStatusText.color = UITheme.TextSecondary;

            // Primary Toggle Button (ENABLE AR CAMERA / EXIT AR)
            var arToggleBtnObj = new GameObject("ArToggleButton");
            arToggleBtnObj.transform.SetParent(arCardObj.transform, false);
            var atbRect = arToggleBtnObj.AddComponent<RectTransform>();
            atbRect.anchorMin = new Vector2(0.08f, 0.20f);
            atbRect.anchorMax = new Vector2(0.92f, 0.32f);
            atbRect.offsetMin = Vector2.zero;
            atbRect.offsetMax = Vector2.zero;

            _arToggleBtnBg = arToggleBtnObj.AddComponent<Image>();
            _arToggleBtnBg.color = UITheme.PrimaryOrange;
            _arToggleButton = arToggleBtnObj.AddComponent<Button>();
            var atbTap = arToggleBtnObj.AddComponent<TapGatedButton>();
            atbTap.Initialize(() => ToggleARCamera());

            var atbTextObj = new GameObject("Text");
            atbTextObj.transform.SetParent(arToggleBtnObj.transform, false);
            var atbtRect = atbTextObj.AddComponent<RectTransform>();
            atbtRect.anchorMin = Vector2.zero;
            atbtRect.anchorMax = Vector2.one;

            _arToggleButtonText = atbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _arToggleButtonText.font = defaultFont;
            _arToggleButtonText.text = "<b>ENABLE AR CAMERA</b>";
            _arToggleButtonText.fontSize = 24;
            _arToggleButtonText.alignment = TextAlignmentOptions.Center;
            _arToggleButtonText.color = UITheme.TextLightOnDark;

            // Fire Shortcut button (visible when AR camera is active)
            var arFireBtnObj = new GameObject("ArStartFireShortcut");
            arFireBtnObj.transform.SetParent(arCardObj.transform, false);
            var afbRect = arFireBtnObj.AddComponent<RectTransform>();
            afbRect.anchorMin = new Vector2(0.08f, 0.06f);
            afbRect.anchorMax = new Vector2(0.92f, 0.17f);
            afbRect.offsetMin = Vector2.zero;
            afbRect.offsetMax = Vector2.zero;

            var afbImg = arFireBtnObj.AddComponent<Image>();
            afbImg.color = UITheme.SuccessSurface;
            _arStartFireShortcutBtn = arFireBtnObj.AddComponent<Button>();
            var afbTap = arFireBtnObj.AddComponent<TapGatedButton>();
            afbTap.Initialize(() => StartFireTraining());

            var afbTextObj = new GameObject("Text");
            afbTextObj.transform.SetParent(arFireBtnObj.transform, false);
            var afbtRect = afbTextObj.AddComponent<RectTransform>();
            afbtRect.anchorMin = Vector2.zero;
            afbtRect.anchorMax = Vector2.one;

            _arStartFireShortcutBtnText = afbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _arStartFireShortcutBtnText.font = defaultFont;
            _arStartFireShortcutBtnText.text = "<b>START TRAINING →</b>";
            _arStartFireShortcutBtnText.fontSize = 24;
            _arStartFireShortcutBtnText.alignment = TextAlignmentOptions.Center;
            _arStartFireShortcutBtnText.color = UITheme.SuccessText;
            arFireBtnObj.SetActive(false);

            _arContentRoot.SetActive(false);

            // =============================================================
            // TAB 3: CERTIFICATES CONTENT ROOT (Safe Empty State)
            // =============================================================
            _certificatesContentRoot = new GameObject("CertificatesContentRoot");
            _certificatesContentRoot.transform.SetParent(contentContainer.transform, false);
            var ccrRect = _certificatesContentRoot.AddComponent<RectTransform>();
            ccrRect.anchorMin = Vector2.zero;
            ccrRect.anchorMax = Vector2.one;
            ccrRect.offsetMin = Vector2.zero;
            ccrRect.offsetMax = Vector2.zero;

            var certCardObj = new GameObject("CertificatesCard");
            certCardObj.transform.SetParent(_certificatesContentRoot.transform, false);
            var ccardRect = certCardObj.AddComponent<RectTransform>();
            ccardRect.anchorMin = new Vector2(0.04f, 0.15f);
            ccardRect.anchorMax = new Vector2(0.96f, 0.85f);
            ccardRect.offsetMin = Vector2.zero;
            ccardRect.offsetMax = Vector2.zero;

            var ccardBg = certCardObj.AddComponent<Image>();
            ccardBg.color = UITheme.CardBackground;

            var certTitleObj = new GameObject("CertTitle");
            certTitleObj.transform.SetParent(certCardObj.transform, false);
            var ctitRect = certTitleObj.AddComponent<RectTransform>();
            ctitRect.anchorMin = new Vector2(0.05f, 0.82f);
            ctitRect.anchorMax = new Vector2(0.95f, 0.95f);
            ctitRect.offsetMin = Vector2.zero;
            ctitRect.offsetMax = Vector2.zero;

            _certTitleText = certTitleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _certTitleText.font = defaultFont;
            _certTitleText.text = "<b>Certificates</b>";
            _certTitleText.fontSize = 28;
            _certTitleText.alignment = TextAlignmentOptions.Center;
            _certTitleText.color = UITheme.TextPrimary;

            var certIconObj = new GameObject("CertIcon");
            certIconObj.transform.SetParent(certCardObj.transform, false);
            var cicoRect = certIconObj.AddComponent<RectTransform>();
            cicoRect.anchorMin = new Vector2(0.35f, 0.45f);
            cicoRect.anchorMax = new Vector2(0.65f, 0.78f);
            cicoRect.offsetMin = Vector2.zero;
            cicoRect.offsetMax = Vector2.zero;

            _certIconText = certIconObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _certIconText.font = defaultFont;
            _certIconText.text = "";
            _certIconText.fontSize = 54;
            _certIconText.alignment = TextAlignmentOptions.Center;

            var certDescObj = new GameObject("CertDesc");
            certDescObj.transform.SetParent(certCardObj.transform, false);
            var cdescRect = certDescObj.AddComponent<RectTransform>();
            cdescRect.anchorMin = new Vector2(0.08f, 0.25f);
            cdescRect.anchorMax = new Vector2(0.92f, 0.44f);
            cdescRect.offsetMin = Vector2.zero;
            cdescRect.offsetMax = Vector2.zero;

            _certDescText = certDescObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _certDescText.font = defaultFont;
            _certDescText.text = "Training certificates will appear here after successful training and synchronization.";
            _certDescText.fontSize = 22;
            _certDescText.alignment = TextAlignmentOptions.Center;
            _certDescText.color = UITheme.TextSecondary;

            var certSubObj = new GameObject("CertSub");
            certSubObj.transform.SetParent(certCardObj.transform, false);
            var csubRect = certSubObj.AddComponent<RectTransform>();
            csubRect.anchorMin = new Vector2(0.08f, 0.10f);
            csubRect.anchorMax = new Vector2(0.92f, 0.22f);
            csubRect.offsetMin = Vector2.zero;
            csubRect.offsetMax = Vector2.zero;

            _certSubText = certSubObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _certSubText.font = defaultFont;
            _certSubText.text = "No official certificates issued yet.";
            _certSubText.fontSize = 20;
            _certSubText.alignment = TextAlignmentOptions.Center;
            _certSubText.color = UITheme.TextMuted;

            _certificatesContentRoot.SetActive(false);

            // =============================================================
            // TAB 4: PROFILE CONTENT ROOT (Worker Identity & Preferences)
            // =============================================================
            _profileContentRoot = new GameObject("ProfileContentRoot");
            _profileContentRoot.transform.SetParent(contentContainer.transform, false);
            var pcrRect = _profileContentRoot.AddComponent<RectTransform>();
            pcrRect.anchorMin = Vector2.zero;
            pcrRect.anchorMax = Vector2.one;
            pcrRect.offsetMin = Vector2.zero;
            pcrRect.offsetMax = Vector2.zero;

            var pCardObj = new GameObject("ProfileDetailsCard");
            pCardObj.transform.SetParent(_profileContentRoot.transform, false);
            var pdcRect = pCardObj.AddComponent<RectTransform>();
            pdcRect.anchorMin = new Vector2(0.04f, 0.10f);
            pdcRect.anchorMax = new Vector2(0.96f, 0.90f);
            pdcRect.offsetMin = Vector2.zero;
            pdcRect.offsetMax = Vector2.zero;

            var pdcBg = pCardObj.AddComponent<Image>();
            pdcBg.color = UITheme.CardBackground;

            var pvtObj = new GameObject("ProfileTitle");
            pvtObj.transform.SetParent(pCardObj.transform, false);
            var pvtRect = pvtObj.AddComponent<RectTransform>();
            pvtRect.anchorMin = new Vector2(0.05f, 0.88f);
            pvtRect.anchorMax = new Vector2(0.95f, 0.96f);
            pvtRect.offsetMin = Vector2.zero;
            pvtRect.offsetMax = Vector2.zero;

            _profileViewTitleText = pvtObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _profileViewTitleText.font = defaultFont;
            _profileViewTitleText.text = "<b>WORKER PROFILE</b>";
            _profileViewTitleText.fontSize = 28;
            _profileViewTitleText.alignment = TextAlignmentOptions.Center;
            _profileViewTitleText.color = UITheme.TextPrimary;

            // Name Field
            var pvNameObj = new GameObject("NameField");
            pvNameObj.transform.SetParent(pCardObj.transform, false);
            var pvnRect = pvNameObj.AddComponent<RectTransform>();
            pvnRect.anchorMin = new Vector2(0.08f, 0.72f);
            pvnRect.anchorMax = new Vector2(0.92f, 0.84f);
            pvnRect.offsetMin = Vector2.zero;
            pvnRect.offsetMax = Vector2.zero;

            _pvNameLabel = pvNameObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvNameLabel.font = defaultFont;
            _pvNameLabel.text = "Worker Name";
            _pvNameLabel.fontSize = 18;
            _pvNameLabel.color = UITheme.TextSecondary;

            var pvNameValObj = new GameObject("Val");
            pvNameValObj.transform.SetParent(pvNameObj.transform, false);
            var pvnvRect = pvNameValObj.AddComponent<RectTransform>();
            pvnvRect.anchorMin = new Vector2(0f, 0f);
            pvnvRect.anchorMax = new Vector2(1f, 0.55f);
            pvnvRect.offsetMin = Vector2.zero;
            pvnvRect.offsetMax = Vector2.zero;

            _pvNameVal = pvNameValObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvNameVal.font = defaultFont;
            _pvNameVal.text = $"<b>{_workerName}</b>";
            _pvNameVal.fontSize = 24;
            _pvNameVal.color = UITheme.TextPrimary;

            // Worker ID Field
            var pvIdObj = new GameObject("IdField");
            pvIdObj.transform.SetParent(pCardObj.transform, false);
            var pviRect = pvIdObj.AddComponent<RectTransform>();
            pviRect.anchorMin = new Vector2(0.08f, 0.56f);
            pviRect.anchorMax = new Vector2(0.92f, 0.68f);
            pviRect.offsetMin = Vector2.zero;
            pviRect.offsetMax = Vector2.zero;

            _pvIdLabel = pvIdObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvIdLabel.font = defaultFont;
            _pvIdLabel.text = "Worker ID";
            _pvIdLabel.fontSize = 18;
            _pvIdLabel.color = UITheme.TextSecondary;

            var pvIdValObj = new GameObject("Val");
            pvIdValObj.transform.SetParent(pvIdObj.transform, false);
            var pvivRect = pvIdValObj.AddComponent<RectTransform>();
            pvivRect.anchorMin = new Vector2(0f, 0f);
            pvivRect.anchorMax = new Vector2(1f, 0.55f);
            pvivRect.offsetMin = Vector2.zero;
            pvivRect.offsetMax = Vector2.zero;

            _pvIdVal = pvIdValObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvIdVal.font = defaultFont;
            _pvIdVal.text = $"<color=#F97316>{_workerId}</color>";
            _pvIdVal.fontSize = 22;

            // Division Field
            var pvDivObj = new GameObject("DivField");
            pvDivObj.transform.SetParent(pCardObj.transform, false);
            var pvdRect = pvDivObj.AddComponent<RectTransform>();
            pvdRect.anchorMin = new Vector2(0.08f, 0.42f);
            pvdRect.anchorMax = new Vector2(0.92f, 0.52f);
            pvdRect.offsetMin = Vector2.zero;
            pvdRect.offsetMax = Vector2.zero;

            _pvDivLabel = pvDivObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvDivLabel.font = defaultFont;
            _pvDivLabel.text = "Division: Mining & Material Handling";
            _pvDivLabel.fontSize = 20;
            _pvDivLabel.color = UITheme.TextPrimary;

            // Preferred Language Field
            var pvLangObj = new GameObject("LangField");
            pvLangObj.transform.SetParent(pCardObj.transform, false);
            var pvlRect = pvLangObj.AddComponent<RectTransform>();
            pvlRect.anchorMin = new Vector2(0.08f, 0.26f);
            pvlRect.anchorMax = new Vector2(0.92f, 0.38f);
            pvlRect.offsetMin = Vector2.zero;
            pvlRect.offsetMax = Vector2.zero;

            _pvLangLabel = pvLangObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvLangLabel.font = defaultFont;
            _pvLangLabel.text = "Preferred Language";
            _pvLangLabel.fontSize = 18;
            _pvLangLabel.color = UITheme.TextSecondary;

            var pvLangValObj = new GameObject("Val");
            pvLangValObj.transform.SetParent(pvLangObj.transform, false);
            var pvlvRect = pvLangValObj.AddComponent<RectTransform>();
            pvlvRect.anchorMin = new Vector2(0f, 0f);
            pvlvRect.anchorMax = new Vector2(1f, 0.55f);
            pvlvRect.offsetMin = Vector2.zero;
            pvlvRect.offsetMax = Vector2.zero;

            _pvLangVal = pvLangValObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvLangVal.font = defaultFont;
            _pvLangVal.text = "<b>English</b>";
            _pvLangVal.fontSize = 22;
            _pvLangVal.color = UITheme.PrimaryOrange;

            // Profile Settings button
            var pvSetBtnObj = new GameObject("ProfileSettingsBtn");
            pvSetBtnObj.transform.SetParent(pCardObj.transform, false);
            var pvsRect = pvSetBtnObj.AddComponent<RectTransform>();
            pvsRect.anchorMin = new Vector2(0.10f, 0.06f);
            pvsRect.anchorMax = new Vector2(0.90f, 0.18f);
            pvsRect.offsetMin = Vector2.zero;
            pvsRect.offsetMax = Vector2.zero;

            var pvsImg = pvSetBtnObj.AddComponent<Image>();
            pvsImg.color = UITheme.CardSecondaryBg;
            _pvSettingsBtn = pvSetBtnObj.AddComponent<Button>();
            var pvsTap = pvSetBtnObj.AddComponent<TapGatedButton>();
            pvsTap.Initialize(() => OpenSettings());

            var pvsTextObj = new GameObject("Text");
            pvsTextObj.transform.SetParent(pvSetBtnObj.transform, false);
            var pvstRect = pvsTextObj.AddComponent<RectTransform>();
            pvstRect.anchorMin = Vector2.zero;
            pvstRect.anchorMax = Vector2.one;

            _pvSettingsBtnText = pvsTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvSettingsBtnText.font = defaultFont;
            _pvSettingsBtnText.text = "<b>APPLICATION SETTINGS</b>";
            _pvSettingsBtnText.fontSize = 20;
            _pvSettingsBtnText.alignment = TextAlignmentOptions.Center;
            _pvSettingsBtnText.color = UITheme.TextPrimary;

            _profileContentRoot.SetActive(false);

            // -------------------------------------------------------------
            // Offline Status Bar (y: 0.075 to 0.115)
            // -------------------------------------------------------------
            var offlineBarObj = new GameObject("OfflineStatusBar");
            offlineBarObj.transform.SetParent(_homeRoot.transform, false);
            var obRect = offlineBarObj.AddComponent<RectTransform>();
            obRect.anchorMin = new Vector2(0.04f, 0.075f);
            obRect.anchorMax = new Vector2(0.96f, 0.115f);
            obRect.offsetMin = Vector2.zero;
            obRect.offsetMax = Vector2.zero;

            _offlineBadgeBg = offlineBarObj.AddComponent<Image>();
            _offlineBadgeBg.color = UITheme.SuccessSurface;

            var obTextObj = new GameObject("OfflineBadgeText");
            obTextObj.transform.SetParent(offlineBarObj.transform, false);
            var obTextRect = obTextObj.AddComponent<RectTransform>();
            obTextRect.anchorMin = Vector2.zero;
            obTextRect.anchorMax = Vector2.one;
            obTextRect.offsetMin = new Vector2(10, 0);
            obTextRect.offsetMax = new Vector2(-10, 0);

            _offlineBadgeText = obTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _offlineBadgeText.font = defaultFont;
            _offlineBadgeText.text = "● OFFLINE • SAVED LOCALLY";
            _offlineBadgeText.fontSize = 18;
            _offlineBadgeText.alignment = TextAlignmentOptions.Center;
            _offlineBadgeText.color = UITheme.SuccessText;

            // -------------------------------------------------------------
            // Bottom Navigation Bar (4 TABS: HOME, AR, CERTIFICATES, PROFILE)
            // -------------------------------------------------------------
            _bottomNavBar = new GameObject("BottomNavBar");
            _bottomNavBar.transform.SetParent(_homeRoot.transform, false);
            var bnbRect = _bottomNavBar.AddComponent<RectTransform>();
            bnbRect.anchorMin = new Vector2(0f, 0f);
            bnbRect.anchorMax = new Vector2(1f, 0.07f);
            bnbRect.offsetMin = Vector2.zero;
            bnbRect.offsetMax = Vector2.zero;

            var bnbBg = _bottomNavBar.AddComponent<Image>();
            bnbBg.color = UITheme.CardBackground;

            // Tab 1: HOME (0.02 to 0.245)
            var navHomeObj = new GameObject("NavTab_Home");
            navHomeObj.transform.SetParent(_bottomNavBar.transform, false);
            var nhRect = navHomeObj.AddComponent<RectTransform>();
            nhRect.anchorMin = new Vector2(0.02f, 0.08f);
            nhRect.anchorMax = new Vector2(0.245f, 0.92f);
            nhRect.offsetMin = Vector2.zero;
            nhRect.offsetMax = Vector2.zero;

            _navHomeBg = navHomeObj.AddComponent<Image>();
            _navHomeBg.color = UITheme.PrimaryOrangeSurface;
            _navHomeBtn = navHomeObj.AddComponent<Button>();
            var nhTap = navHomeObj.AddComponent<TapGatedButton>();
            nhTap.Initialize(() => ShowHome());

            var nhTextObj = new GameObject("Text");
            nhTextObj.transform.SetParent(navHomeObj.transform, false);
            var nhtRect = nhTextObj.AddComponent<RectTransform>();
            nhtRect.anchorMin = Vector2.zero;
            nhtRect.anchorMax = Vector2.one;

            _navHomeText = nhTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _navHomeText.font = defaultFont;
            _navHomeText.text = "<b>HOME</b>";
            _navHomeText.fontSize = 18;
            _navHomeText.alignment = TextAlignmentOptions.Center;
            _navHomeText.color = UITheme.PrimaryOrange;

            // Tab 2: AR (0.265 to 0.490)
            var navArObj = new GameObject("NavTab_AR");
            navArObj.transform.SetParent(_bottomNavBar.transform, false);
            var naRect = navArObj.AddComponent<RectTransform>();
            naRect.anchorMin = new Vector2(0.265f, 0.08f);
            naRect.anchorMax = new Vector2(0.490f, 0.92f);
            naRect.offsetMin = Vector2.zero;
            naRect.offsetMax = Vector2.zero;

            _navArBg = navArObj.AddComponent<Image>();
            _navArBg.color = UITheme.CardSecondaryBg;
            _navArBtn = navArObj.AddComponent<Button>();
            var naTap = navArObj.AddComponent<TapGatedButton>();
            naTap.Initialize(() => ShowAR());

            var naTextObj = new GameObject("Text");
            naTextObj.transform.SetParent(navArObj.transform, false);
            var natRect = naTextObj.AddComponent<RectTransform>();
            natRect.anchorMin = Vector2.zero;
            natRect.anchorMax = Vector2.one;

            _navArText = naTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _navArText.font = defaultFont;
            _navArText.text = "<b>AR</b>";
            _navArText.fontSize = 18;
            _navArText.alignment = TextAlignmentOptions.Center;
            _navArText.color = UITheme.TextSecondary;

            // Tab 3: CERTIFICATES (0.510 to 0.735)
            var navCertObj = new GameObject("NavTab_Certificates");
            navCertObj.transform.SetParent(_bottomNavBar.transform, false);
            var ncRect = navCertObj.AddComponent<RectTransform>();
            ncRect.anchorMin = new Vector2(0.510f, 0.08f);
            ncRect.anchorMax = new Vector2(0.735f, 0.92f);
            ncRect.offsetMin = Vector2.zero;
            ncRect.offsetMax = Vector2.zero;

            _navCertBg = navCertObj.AddComponent<Image>();
            _navCertBg.color = UITheme.CardSecondaryBg;
            _navCertBtn = navCertObj.AddComponent<Button>();
            var ncTap = navCertObj.AddComponent<TapGatedButton>();
            ncTap.Initialize(() => ShowCertificates());

            var ncTextObj = new GameObject("Text");
            ncTextObj.transform.SetParent(navCertObj.transform, false);
            var nctRect = ncTextObj.AddComponent<RectTransform>();
            nctRect.anchorMin = Vector2.zero;
            nctRect.anchorMax = Vector2.one;

            _navCertText = ncTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _navCertText.font = defaultFont;
            _navCertText.text = "<b>CERTIFICATES</b>";
            _navCertText.fontSize = 17;
            _navCertText.alignment = TextAlignmentOptions.Center;
            _navCertText.color = UITheme.TextSecondary;

            // Tab 4: PROFILE (0.755 to 0.980)
            var navProfObj = new GameObject("NavTab_Profile");
            navProfObj.transform.SetParent(_bottomNavBar.transform, false);
            var npRect = navProfObj.AddComponent<RectTransform>();
            npRect.anchorMin = new Vector2(0.755f, 0.08f);
            npRect.anchorMax = new Vector2(0.980f, 0.92f);
            npRect.offsetMin = Vector2.zero;
            npRect.offsetMax = Vector2.zero;

            _navProfBg = navProfObj.AddComponent<Image>();
            _navProfBg.color = UITheme.CardSecondaryBg;
            _navProfBtn = navProfObj.AddComponent<Button>();
            var npTap = navProfObj.AddComponent<TapGatedButton>();
            npTap.Initialize(() => ShowProfile());

            var npTextObj = new GameObject("Text");
            npTextObj.transform.SetParent(navProfObj.transform, false);
            var nptRect = npTextObj.AddComponent<RectTransform>();
            nptRect.anchorMin = Vector2.zero;
            nptRect.anchorMax = Vector2.one;

            _navProfText = npTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _navProfText.font = defaultFont;
            _navProfText.text = "<b>PROFILE</b>";
            _navProfText.fontSize = 18;
            _navProfText.alignment = TextAlignmentOptions.Center;
            _navProfText.color = UITheme.TextSecondary;

            // =============================================================
            // 3. SETTINGS PANEL MODAL OVERLAY (Full-Screen Raycast Blocker)
            // =============================================================
            _settingsRoot = new GameObject("SettingsPanelModal");
            _settingsRoot.transform.SetParent(_rootCanvas.transform, false);

            var spRect = _settingsRoot.AddComponent<RectTransform>();
            spRect.anchorMin = Vector2.zero;
            spRect.anchorMax = Vector2.one;
            spRect.offsetMin = Vector2.zero;
            spRect.offsetMax = Vector2.zero;

            var spBackdropImg = _settingsRoot.AddComponent<Image>();
            spBackdropImg.color = new Color(0.05f, 0.08f, 0.12f, 0.75f);
            spBackdropImg.raycastTarget = true;

            var settingsCardObj = new GameObject("SettingsCard");
            settingsCardObj.transform.SetParent(_settingsRoot.transform, false);
            var scRect = settingsCardObj.AddComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0.04f, 0.08f);
            scRect.anchorMax = new Vector2(0.96f, 0.92f);
            scRect.offsetMin = Vector2.zero;
            scRect.offsetMax = Vector2.zero;

            var scBg = settingsCardObj.AddComponent<Image>();
            scBg.color = UITheme.CardBackground;

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
            _settingsTitleText.fontSize = 26;
            _settingsTitleText.alignment = TextAlignmentOptions.Center;
            _settingsTitleText.color = UITheme.TextPrimary;

            // Top-right Quick Close [X] Icon
            var topCloseObj = new GameObject("TopCloseButton");
            topCloseObj.transform.SetParent(settingsCardObj.transform, false);
            var tcRect = topCloseObj.AddComponent<RectTransform>();
            tcRect.anchorMin = new Vector2(0.85f, 0.90f);
            tcRect.anchorMax = new Vector2(0.96f, 0.98f);
            tcRect.offsetMin = Vector2.zero;
            tcRect.offsetMax = Vector2.zero;

            var tcImg = topCloseObj.AddComponent<Image>();
            tcImg.color = UITheme.CardSecondaryBg;
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
            tcTmp.text = "<b>X</b>";
            tcTmp.fontSize = 20;
            tcTmp.alignment = TextAlignmentOptions.Center;
            tcTmp.color = UITheme.TextSecondary;

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
            _soundToggleLabel.fontSize = 22;
            _soundToggleLabel.alignment = TextAlignmentOptions.Left;
            _soundToggleLabel.color = UITheme.TextPrimary;

            var seBtnObj = new GameObject("SoundToggleButton");
            seBtnObj.transform.SetParent(seObj.transform, false);
            var sebRect = seBtnObj.AddComponent<RectTransform>();
            sebRect.anchorMin = new Vector2(0.68f, 0.1f);
            sebRect.anchorMax = new Vector2(1f, 0.9f);
            sebRect.offsetMin = Vector2.zero;
            sebRect.offsetMax = Vector2.zero;

            var sebImg = seBtnObj.AddComponent<Image>();
            sebImg.color = UITheme.CardSecondaryBg;
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

            _soundToggleButtonText = sebTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _soundToggleButtonText.font = defaultFont;
            _soundToggleButtonText.text = "<b>ON</b>";
            _soundToggleButtonText.fontSize = 20;
            _soundToggleButtonText.alignment = TextAlignmentOptions.Center;
            _soundToggleButtonText.color = UITheme.TextPrimary;

            // Emergency Alarm Row
            var eaObj = new GameObject("AlarmRow");
            eaObj.transform.SetParent(settingsCardObj.transform, false);
            var eaRect = eaObj.AddComponent<RectTransform>();
            eaRect.anchorMin = new Vector2(0.06f, 0.64f);
            eaRect.anchorMax = new Vector2(0.94f, 0.74f);
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
            _alarmToggleLabel.fontSize = 22;
            _alarmToggleLabel.alignment = TextAlignmentOptions.Left;
            _alarmToggleLabel.color = UITheme.TextPrimary;

            var eaBtnObj = new GameObject("AlarmToggleButton");
            eaBtnObj.transform.SetParent(eaObj.transform, false);
            var eabRect = eaBtnObj.AddComponent<RectTransform>();
            eabRect.anchorMin = new Vector2(0.68f, 0.1f);
            eabRect.anchorMax = new Vector2(1f, 0.9f);
            eabRect.offsetMin = Vector2.zero;
            eabRect.offsetMax = Vector2.zero;

            var eabImg = eaBtnObj.AddComponent<Image>();
            eabImg.color = UITheme.CardSecondaryBg;
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
            var eabtRect = eabTextObj.AddComponent<RectTransform>();
            eabtRect.anchorMin = Vector2.zero;
            eabtRect.anchorMax = Vector2.one;

            _alarmToggleButtonText = eabTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _alarmToggleButtonText.font = defaultFont;
            _alarmToggleButtonText.text = "<b>ON</b>";
            _alarmToggleButtonText.fontSize = 20;
            _alarmToggleButtonText.alignment = TextAlignmentOptions.Center;
            _alarmToggleButtonText.color = UITheme.TextPrimary;

            // Volume Section
            var volObj = new GameObject("VolumeRow");
            volObj.transform.SetParent(settingsCardObj.transform, false);
            var volRect = volObj.AddComponent<RectTransform>();
            volRect.anchorMin = new Vector2(0.06f, 0.50f);
            volRect.anchorMax = new Vector2(0.94f, 0.60f);
            volRect.offsetMin = Vector2.zero;
            volRect.offsetMax = Vector2.zero;

            var vlObj = new GameObject("Label");
            vlObj.transform.SetParent(volObj.transform, false);
            var vlRect = vlObj.AddComponent<RectTransform>();
            vlRect.anchorMin = new Vector2(0f, 0.55f);
            vlRect.anchorMax = new Vector2(0.60f, 1f);
            vlRect.offsetMin = Vector2.zero;
            vlRect.offsetMax = Vector2.zero;

            _volumeLabel = vlObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _volumeLabel.font = defaultFont;
            _volumeLabel.text = "Effects Volume";
            _volumeLabel.fontSize = 22;
            _volumeLabel.alignment = TextAlignmentOptions.Left;
            _volumeLabel.color = UITheme.TextPrimary;

            var vvObj = new GameObject("ValueText");
            vvObj.transform.SetParent(volObj.transform, false);
            var vvRect = vvObj.AddComponent<RectTransform>();
            vvRect.anchorMin = new Vector2(0.62f, 0.55f);
            vvRect.anchorMax = new Vector2(1f, 1f);
            vvRect.offsetMin = Vector2.zero;
            vvRect.offsetMax = Vector2.zero;

            _volumeValueText = vvObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _volumeValueText.font = defaultFont;
            _volumeValueText.text = "100%";
            _volumeValueText.fontSize = 20;
            _volumeValueText.alignment = TextAlignmentOptions.Right;
            _volumeValueText.color = UITheme.PrimaryOrange;

            // Volume Step - Button
            var vmBtnObj = new GameObject("VolMinusBtn");
            vmBtnObj.transform.SetParent(volObj.transform, false);
            var vmbRect = vmBtnObj.AddComponent<RectTransform>();
            vmbRect.anchorMin = new Vector2(0f, 0f);
            vmbRect.anchorMax = new Vector2(0.46f, 0.50f);
            vmbRect.offsetMin = Vector2.zero;
            vmbRect.offsetMax = Vector2.zero;

            vmBtnObj.AddComponent<Image>().color = UITheme.CardSecondaryBg;
            var vmBtn = vmBtnObj.AddComponent<Button>();
            var vmTap = vmBtnObj.AddComponent<TapGatedButton>();
            vmTap.Initialize(() =>
            {
                var audio = FireAudioService.Instance;
                audio.EffectsVolume = Mathf.Clamp01(audio.EffectsVolume - 0.10f);
                UpdateSettingsControls();
            });

            var vmtObj = new GameObject("Text");
            vmtObj.transform.SetParent(vmBtnObj.transform, false);
            var vmtrRect = vmtObj.AddComponent<RectTransform>();
            vmtrRect.anchorMin = Vector2.zero;
            vmtrRect.anchorMax = Vector2.one;
            var vmtTmp = vmtObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) vmtTmp.font = defaultFont;
            vmtTmp.text = "<b>- 10%</b>";
            vmtTmp.fontSize = 20;
            vmtTmp.alignment = TextAlignmentOptions.Center;
            vmtTmp.color = UITheme.TextPrimary;

            // Volume Step + Button
            var vpBtnObj = new GameObject("VolPlusBtn");
            vpBtnObj.transform.SetParent(volObj.transform, false);
            var vpbRect = vpBtnObj.AddComponent<RectTransform>();
            vpbRect.anchorMin = new Vector2(0.54f, 0f);
            vpbRect.anchorMax = new Vector2(1f, 0.50f);
            vpbRect.offsetMin = Vector2.zero;
            vpbRect.offsetMax = Vector2.zero;

            vpBtnObj.AddComponent<Image>().color = UITheme.CardSecondaryBg;
            var vpBtn = vpBtnObj.AddComponent<Button>();
            var vpTap = vpBtnObj.AddComponent<TapGatedButton>();
            vpTap.Initialize(() =>
            {
                var audio = FireAudioService.Instance;
                audio.EffectsVolume = Mathf.Clamp01(audio.EffectsVolume + 0.10f);
                UpdateSettingsControls();
            });

            var vptObj = new GameObject("Text");
            vptObj.transform.SetParent(vpBtnObj.transform, false);
            var vptrRect = vptObj.AddComponent<RectTransform>();
            vptrRect.anchorMin = Vector2.zero;
            vptrRect.anchorMax = Vector2.one;
            var vptTmp = vptObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) vptTmp.font = defaultFont;
            vptTmp.text = "<b>+ 10%</b>";
            vptTmp.fontSize = 20;
            vptTmp.alignment = TextAlignmentOptions.Center;
            vptTmp.color = UITheme.TextPrimary;

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
            _languageHeader.fontSize = 22;
            _languageHeader.alignment = TextAlignmentOptions.Left;
            _languageHeader.color = UITheme.TextPrimary;

            // Language Option 1: English
            var lEnObj = new GameObject("LangBtn_English");
            lEnObj.transform.SetParent(settingsCardObj.transform, false);
            var lenRect = lEnObj.AddComponent<RectTransform>();
            lenRect.anchorMin = new Vector2(0.06f, 0.26f);
            lenRect.anchorMax = new Vector2(0.33f, 0.36f);
            lenRect.offsetMin = Vector2.zero;
            lenRect.offsetMax = Vector2.zero;
            lEnObj.AddComponent<Image>().color = UITheme.CardSecondaryBg;
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
            _btnLangEnglishText.fontSize = 20;
            _btnLangEnglishText.alignment = TextAlignmentOptions.Center;
            _btnLangEnglishText.color = UITheme.TextPrimary;

            // Language Option 2: Hindi
            var lHiObj = new GameObject("LangBtn_Hindi");
            lHiObj.transform.SetParent(settingsCardObj.transform, false);
            var lhiRect = lHiObj.AddComponent<RectTransform>();
            lhiRect.anchorMin = new Vector2(0.36f, 0.26f);
            lhiRect.anchorMax = new Vector2(0.63f, 0.36f);
            lhiRect.offsetMin = Vector2.zero;
            lhiRect.offsetMax = Vector2.zero;
            lHiObj.AddComponent<Image>().color = UITheme.CardSecondaryBg;
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
            _btnLangHindiText.fontSize = 20;
            _btnLangHindiText.alignment = TextAlignmentOptions.Center;
            _btnLangHindiText.color = UITheme.TextPrimary;

            // Language Option 3: Santali
            var lSatObj = new GameObject("LangBtn_Santali");
            lSatObj.transform.SetParent(settingsCardObj.transform, false);
            var lsatRect = lSatObj.AddComponent<RectTransform>();
            lsatRect.anchorMin = new Vector2(0.66f, 0.26f);
            lsatRect.anchorMax = new Vector2(0.94f, 0.36f);
            lsatRect.offsetMin = Vector2.zero;
            lsatRect.offsetMax = Vector2.zero;
            lSatObj.AddComponent<Image>().color = UITheme.CardSecondaryBg;
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
            _btnLangSantaliText.fontSize = 20;
            _btnLangSantaliText.alignment = TextAlignmentOptions.Center;
            _btnLangSantaliText.color = UITheme.TextPrimary;

            // Bottom Primary Close Settings Button
            var closeBtnObj = new GameObject("CloseSettingsButton");
            closeBtnObj.transform.SetParent(settingsCardObj.transform, false);
            var cbRect = closeBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.15f, 0.06f);
            cbRect.anchorMax = new Vector2(0.85f, 0.16f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;

            var cbImg = closeBtnObj.AddComponent<Image>();
            cbImg.color = UITheme.PrimaryOrange;
            _settingsCloseButton = closeBtnObj.AddComponent<Button>();
            var cbTap = closeBtnObj.AddComponent<TapGatedButton>();
            cbTap.Initialize(() => CloseSettings());

            var cbTextObj = new GameObject("Text");
            cbTextObj.transform.SetParent(closeBtnObj.transform, false);
            var cbtRect = cbTextObj.AddComponent<RectTransform>();
            cbtRect.anchorMin = Vector2.zero;
            cbtRect.anchorMax = Vector2.one;

            _settingsCloseButtonText = cbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _settingsCloseButtonText.font = defaultFont;
            _settingsCloseButtonText.text = "<b>CLOSE [X]</b>";
            _settingsCloseButtonText.fontSize = 24;
            _settingsCloseButtonText.alignment = TextAlignmentOptions.Center;
            _settingsCloseButtonText.color = UITheme.TextLightOnDark;

            // Initial State: Show Home
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
