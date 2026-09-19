// WorkerHomeController.cs
// Namespace : IndustrialSafetyAR.UI
//
// Real-User Industrial Safety AR Worker App Shell:
// - Content-First Mobile UI with large readable typography (20-34sp) and compact rich cards.
// - 5-Tab Bottom Navigation (HOME, AR, RECORDS, CERTIFICATES, PROFILE).
// - Interactive Checklists and duration badges for Fire & Gas curriculum cards.
// - Local Training Records screen reading from LocalStorageService (survives app restarts, 100% offline).
// - Official Certificates screen with canonical Jharkhand QR verification link modal.
// - Real Online & Offline-First Worker Authentication via Fastify backend & cached salted PIN hash.
// - Real-time Outbox synchronization trigger adhering to sync schema contracts.
// - Strict mutual screen state isolation & AR camera lifecycle enforcement.

using System;
using System.Collections;
using System.Collections.Generic;
using IndustrialSafetyAR.AR;
using IndustrialSafetyAR.Core;
using IndustrialSafetyAR.Core.Audio;
using IndustrialSafetyAR.Core.Events;
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
            Records,
            Certificates,
            Profile,
            Login
        }

        private WorkerAppScreenState _currentState = WorkerAppScreenState.Home;
        private WorkerAppScreenState _previousNavState = WorkerAppScreenState.Home;
        public WorkerAppScreenState CurrentState => _currentState;

        // Visual elements & Shell roots
        private Canvas _rootCanvas;
        private GameObject _homeRoot;
        private GameObject _settingsRoot;
        private GameObject _loginRoot;
        private GameObject _recordDetailModalRoot;
        private GameObject _qrModalRoot;

        // Content Views inside HomeRoot
        private GameObject _homeContentRoot;
        private GameObject _arContentRoot;
        private GameObject _recordsContentRoot;
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
        private TextMeshProUGUI _profileSessionStatusText;
        private TextMeshProUGUI _modulesHeaderText;
        private Button _homeSettingsButton;
        private TextMeshProUGUI _homeSettingsButtonText;

        // Module Card 1 (Fire)
        private TextMeshProUGUI _fireTitleText;
        private TextMeshProUGUI _fireArBadgeText;
        private TextMeshProUGUI _fireStatusText;
        private Image _fireStatusBg;
        private TextMeshProUGUI _fireOfflineTagText;
        private TextMeshProUGUI _fireDurationTagText;
        private TextMeshProUGUI _fireCheck1Text;
        private TextMeshProUGUI _fireCheck2Text;
        private TextMeshProUGUI _fireCheck3Text;
        private Button _fireStartButton;
        private TextMeshProUGUI _fireStartButtonText;

        // Module Card 2 (Gas)
        private TextMeshProUGUI _gasTitleText;
        private TextMeshProUGUI _gasArBadgeText;
        private TextMeshProUGUI _gasStatusText;
        private Image _gasStatusBg;
        private TextMeshProUGUI _gasOfflineTagText;
        private TextMeshProUGUI _gasDurationTagText;
        private TextMeshProUGUI _gasCheck1Text;
        private TextMeshProUGUI _gasCheck2Text;
        private TextMeshProUGUI _gasCheck3Text;
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

        // Records View components
        private TextMeshProUGUI _recordsTitleText;
        private TextMeshProUGUI _recordsSubText;
        private GameObject _recordsListContainer;
        private Button _recordsSyncBtn;
        private TextMeshProUGUI _recordsSyncBtnText;

        // Record Detail Modal components
        private TextMeshProUGUI _rdTitleText;
        private TextMeshProUGUI _rdSubtitleText;
        private TextMeshProUGUI _rdScoreText;
        private TextMeshProUGUI _rdStatusBadgeText;
        private Image _rdStatusBadgeBg;
        private TextMeshProUGUI _rdStepsBodyText;
        private TextMeshProUGUI _rdMetaText;

        // Certificates View components
        private TextMeshProUGUI _certTitleText;
        private TextMeshProUGUI _certIconText;
        private TextMeshProUGUI _certDescText;
        private TextMeshProUGUI _certSubText;
        private GameObject _certsListContainer;

        // QR Verification Modal components
        private TextMeshProUGUI _qrTitleText;
        private TextMeshProUGUI _qrSubText;
        private TextMeshProUGUI _qrUrlText;
        private TextMeshProUGUI _qrInstructionsText;

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
        private TextMeshProUGUI _pvSyncCountText;
        private TextMeshProUGUI _pvLastSyncText;
        private Button _syncNowBtn;
        private TextMeshProUGUI _syncNowBtnText;
        private Button _logoutBtn;
        private TextMeshProUGUI _logoutBtnText;
        private Button _pvSettingsBtn;
        private TextMeshProUGUI _pvSettingsBtnText;

        // Login View components
        private TextMeshProUGUI _loginAppTitleText;
        private TextMeshProUGUI _loginSubTitleText;
        private TextMeshProUGUI _loginCardTitleText;
        private TextMeshProUGUI _loginStatusText;
        private TextMeshProUGUI _loginIdDisplay;
        private TextMeshProUGUI _loginPinDisplay;
        private string _inputWorkerCode = "DEMO-001";
        private string _inputPin = "1234";

        // Bottom Navigation Bar components (5 TABS)
        private GameObject _bottomNavBar;
        private Button _navHomeBtn;
        private TextMeshProUGUI _navHomeText;
        private Image _navHomeBg;

        private Button _navArBtn;
        private TextMeshProUGUI _navArText;
        private Image _navArBg;

        private Button _navRecordsBtn;
        private TextMeshProUGUI _navRecordsText;
        private Image _navRecordsBg;

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
        private Image _soundToggleBg;
        private TextMeshProUGUI _soundToggleButtonText;

        private TextMeshProUGUI _alarmToggleLabel;
        private Button _alarmToggleButton;
        private Image _alarmToggleBg;
        private TextMeshProUGUI _alarmToggleButtonText;

        private TextMeshProUGUI _volumeLabel;
        private Slider _volumeSlider;
        private TextMeshProUGUI _volumeValueText;

        private TextMeshProUGUI _languageHeader;
        private Button _btnLangEnglish;
        private Image _btnLangEnglishBg;
        private Button _btnLangHindi;
        private Image _btnLangHindiBg;
        private Button _btnLangSantali;
        private Image _btnLangSantaliBg;
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

        public bool IsRecordsVisible
        {
            get
            {
                EnsureUIHierarchy();
                return _homeRoot != null && _homeRoot.activeSelf && _recordsContentRoot != null && _recordsContentRoot.activeSelf;
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

        public bool IsSettingsVisible
        {
            get
            {
                EnsureUIHierarchy();
                return _settingsRoot != null && _settingsRoot.activeSelf;
            }
        }

        public bool IsLoginVisible
        {
            get
            {
                EnsureUIHierarchy();
                return _loginRoot != null && _loginRoot.activeSelf;
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
        public Button NavRecordsButton => _navRecordsBtn;
        public Button NavCertificatesButton => _navCertBtn;
        public Button NavProfileButton => _navProfBtn;
        public Button ArToggleButton => _arToggleButton;
        public Button LogoutButton => _logoutBtn;
        public Button SyncNowButton => _syncNowBtn;

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

            // Sync with session service if available
            try
            {
                var sessionService = WorkerSessionService.Instance;
                if (sessionService != null && !string.IsNullOrEmpty(sessionService.DisplayName))
                {
                    _workerName = sessionService.DisplayName;
                    _workerId = sessionService.WorkerId;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WorkerHomeController] WorkerSessionService init note: {ex.Message}");
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

        // =========================================================================
        // NAVIGATION HANDLERS
        // =========================================================================

        public void ShowHome()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.Home;
            _previousNavState = WorkerAppScreenState.Home;

            if (_homeRoot != null) _homeRoot.SetActive(true);
            if (_homeContentRoot != null) _homeContentRoot.SetActive(true);
            if (_arContentRoot != null) _arContentRoot.SetActive(false);
            if (_recordsContentRoot != null) _recordsContentRoot.SetActive(false);
            if (_certificatesContentRoot != null) _certificatesContentRoot.SetActive(false);
            if (_profileContentRoot != null) _profileContentRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);
            if (_loginRoot != null) _loginRoot.SetActive(false);
            if (_recordDetailModalRoot != null) _recordDetailModalRoot.SetActive(false);
            if (_qrModalRoot != null) _qrModalRoot.SetActive(false);

            UpdateNavHighlight();

            // Authoritatively disable camera and AR on Home
            if (ARModeController.Instance != null)
            {
                ARModeController.Instance.DisableAR();
            }

            HideSubsystemOverlays();
            UpdateOfflineStatus();
            RefreshTexts();
        }

        public void ShowAR()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.AR;
            _previousNavState = WorkerAppScreenState.AR;

            if (_homeRoot != null) _homeRoot.SetActive(true);
            if (_homeContentRoot != null) _homeContentRoot.SetActive(false);
            if (_arContentRoot != null) _arContentRoot.SetActive(true);
            if (_recordsContentRoot != null) _recordsContentRoot.SetActive(false);
            if (_certificatesContentRoot != null) _certificatesContentRoot.SetActive(false);
            if (_profileContentRoot != null) _profileContentRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);
            if (_loginRoot != null) _loginRoot.SetActive(false);
            if (_recordDetailModalRoot != null) _recordDetailModalRoot.SetActive(false);
            if (_qrModalRoot != null) _qrModalRoot.SetActive(false);

            UpdateNavHighlight();
            UpdateArScreenUI();
            HideSubsystemOverlays();
            UpdateOfflineStatus();
            RefreshTexts();
        }

        public void ShowRecords()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.Records;
            _previousNavState = WorkerAppScreenState.Records;

            if (_homeRoot != null) _homeRoot.SetActive(true);
            if (_homeContentRoot != null) _homeContentRoot.SetActive(false);
            if (_arContentRoot != null) _arContentRoot.SetActive(false);
            if (_recordsContentRoot != null) _recordsContentRoot.SetActive(true);
            if (_certificatesContentRoot != null) _certificatesContentRoot.SetActive(false);
            if (_profileContentRoot != null) _profileContentRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);
            if (_loginRoot != null) _loginRoot.SetActive(false);
            if (_recordDetailModalRoot != null) _recordDetailModalRoot.SetActive(false);
            if (_qrModalRoot != null) _qrModalRoot.SetActive(false);

            UpdateNavHighlight();

            if (ARModeController.Instance != null)
            {
                ARModeController.Instance.DisableAR();
            }

            HideSubsystemOverlays();
            PopulateRecordsList();
            UpdateOfflineStatus();
            RefreshTexts();
        }

        public void ShowCertificates()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.Certificates;
            _previousNavState = WorkerAppScreenState.Certificates;

            if (_homeRoot != null) _homeRoot.SetActive(true);
            if (_homeContentRoot != null) _homeContentRoot.SetActive(false);
            if (_arContentRoot != null) _arContentRoot.SetActive(false);
            if (_recordsContentRoot != null) _recordsContentRoot.SetActive(false);
            if (_certificatesContentRoot != null) _certificatesContentRoot.SetActive(true);
            if (_profileContentRoot != null) _profileContentRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);
            if (_loginRoot != null) _loginRoot.SetActive(false);
            if (_recordDetailModalRoot != null) _recordDetailModalRoot.SetActive(false);
            if (_qrModalRoot != null) _qrModalRoot.SetActive(false);

            UpdateNavHighlight();

            if (ARModeController.Instance != null)
            {
                ARModeController.Instance.DisableAR();
            }

            HideSubsystemOverlays();
            PopulateCertificatesList();
            UpdateOfflineStatus();
            RefreshTexts();
        }

        public void ShowProfile()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.Profile;
            _previousNavState = WorkerAppScreenState.Profile;

            if (_homeRoot != null) _homeRoot.SetActive(true);
            if (_homeContentRoot != null) _homeContentRoot.SetActive(false);
            if (_arContentRoot != null) _arContentRoot.SetActive(false);
            if (_recordsContentRoot != null) _recordsContentRoot.SetActive(false);
            if (_certificatesContentRoot != null) _certificatesContentRoot.SetActive(false);
            if (_profileContentRoot != null) _profileContentRoot.SetActive(true);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);
            if (_loginRoot != null) _loginRoot.SetActive(false);
            if (_recordDetailModalRoot != null) _recordDetailModalRoot.SetActive(false);
            if (_qrModalRoot != null) _qrModalRoot.SetActive(false);

            UpdateNavHighlight();

            if (ARModeController.Instance != null)
            {
                ARModeController.Instance.DisableAR();
            }

            HideSubsystemOverlays();
            UpdateProfileView();
            UpdateOfflineStatus();
            RefreshTexts();
        }

        public void ShowLogin()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.Login;

            if (_homeRoot != null) _homeRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);
            if (_recordDetailModalRoot != null) _recordDetailModalRoot.SetActive(false);
            if (_qrModalRoot != null) _qrModalRoot.SetActive(false);

            if (_loginRoot != null)
            {
                _loginRoot.SetActive(true);
                _loginRoot.transform.SetAsLastSibling();
            }

            if (ARModeController.Instance != null)
            {
                ARModeController.Instance.DisableAR();
            }

            UpdateLoginView();
        }

        public void HideLogin()
        {
            if (_loginRoot != null) _loginRoot.SetActive(false);
            ShowHome();
        }

        private void HideSubsystemOverlays()
        {
            var fireUIs = FindObjectsByType<FireInteractionFeedbackUI>(FindObjectsInactive.Include);
            if (fireUIs != null)
            {
                foreach (var fui in fireUIs) if (fui != null) fui.HideTrainingUI();
            }

            var gasUIs = FindObjectsByType<GasInteractionFeedbackUI>(FindObjectsInactive.Include);
            if (gasUIs != null)
            {
                foreach (var gui in gasUIs) if (gui != null) gui.HideTrainingUI();
            }

            var summaryUIs = FindObjectsByType<FireAssessmentSummaryUI>(FindObjectsInactive.Include);
            if (summaryUIs != null)
            {
                foreach (var sui in summaryUIs) if (sui != null) sui.HideSummary();
            }

            var gasSummaryUIs = FindObjectsByType<GasAssessmentSummaryUI>(FindObjectsInactive.Include);
            if (gasSummaryUIs != null)
            {
                foreach (var gsui in gasSummaryUIs) if (gsui != null) gsui.HideSummary();
            }

            if (FireAudioService.Instance != null)
            {
                FireAudioService.Instance.StopEmergencyAlarm();
                FireAudioService.Instance.StopAllAudio();
            }

            var fireCtrl = FindAnyObjectByType<FireArInteractionController>(FindObjectsInactive.Include);
            if (fireCtrl != null) fireCtrl.enabled = false;

            var gasCtrl = FindAnyObjectByType<GasArInteractionController>(FindObjectsInactive.Include);
            if (gasCtrl != null) gasCtrl.enabled = false;
        }

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
                case WorkerAppScreenState.Records:
                    ShowRecords();
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

        public void StartFireTraining()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.TrainingFire;
            if (_homeRoot != null) _homeRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);
            if (_loginRoot != null) _loginRoot.SetActive(false);

            if (ARModeController.Instance != null)
            {
                ARModeController.Instance.EnableAR();
            }

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
                fireCtrl.SetEventDispatcher(TrainingEventBus.Instance);
            }

            if (FireAudioService.Instance != null)
            {
                FireAudioService.Instance.PlayStepCompleted();
            }
        }

        public void StartGasTraining()
        {
            EnsureUIHierarchy();
            _currentState = WorkerAppScreenState.TrainingGas;
            if (_homeRoot != null) _homeRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);
            if (_loginRoot != null) _loginRoot.SetActive(false);

            if (ARModeController.Instance != null)
            {
                ARModeController.Instance.EnableAR();
            }

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
                gasCtrl.SetEventDispatcher(TrainingEventBus.Instance);
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

        public void ReturnToHome()
        {
            ShowHome();
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

            if (_navRecordsBg != null) _navRecordsBg.color = _currentState == WorkerAppScreenState.Records ? activeBg : inactiveBg;
            if (_navRecordsText != null) _navRecordsText.color = _currentState == WorkerAppScreenState.Records ? activeText : inactiveText;

            if (_navCertBg != null) _navCertBg.color = _currentState == WorkerAppScreenState.Certificates ? activeBg : inactiveBg;
            if (_navCertText != null) _navCertText.color = _currentState == WorkerAppScreenState.Certificates ? activeText : inactiveText;

            if (_navProfBg != null) _navProfBg.color = _currentState == WorkerAppScreenState.Profile ? activeBg : inactiveBg;
            if (_navProfText != null) _navProfText.color = _currentState == WorkerAppScreenState.Profile ? activeText : inactiveText;
        }

        public void UpdateOfflineStatus()
        {
            EnsureUIHierarchy();
            if (_offlineBadgeText == null) return;

            var loc = LocaleService.Instance;
            bool isOffline = Application.internetReachability == NetworkReachability.NotReachable;
            if (isOffline)
            {
                string offlineText = loc.Get("status_offline", "OFFLINE");
                string savedText = loc.Get("status_saved_locally", "SAVED LOCALLY");
                _offlineBadgeText.text = $"● {offlineText} • {savedText}";
                _offlineBadgeText.color = UITheme.SuccessText;
                if (_offlineBadgeBg != null) _offlineBadgeBg.color = UITheme.SuccessSurface;
            }
            else
            {
                string onlineText = loc.Get("status_online", "ONLINE");
                string syncReadyText = loc.Get("status_sync_ready", "SYNC READY");
                _offlineBadgeText.text = $"● {onlineText} • {syncReadyText}";
                _offlineBadgeText.color = UITheme.PrimaryOrange;
                if (_offlineBadgeBg != null) _offlineBadgeBg.color = UITheme.PrimaryOrangeSurface;
            }
        }

        private void UpdateArScreenUI()
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
                    ? $"<color=#10B981>● {loc.Get("ar_camera_active", "AR Camera is active. Look for floor surfaces.")}</color>"
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
                _arToggleBtnBg.color = isArActive ? UITheme.DangerRed : UITheme.PrimaryOrange;
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
        }

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

            // Profile Card (Home view)
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
            if (_profileSessionStatusText != null)
            {
                _profileSessionStatusText.text = "● Active Session (Works 100% Offline)";
                _profileSessionStatusText.color = UITheme.SuccessText;
            }

            // Modules Section Header
            if (_modulesHeaderText != null)
            {
                _modulesHeaderText.text = $"<b>{loc.Get("modules_header", "AVAILABLE MODULES")}</b>";
                _modulesHeaderText.color = UITheme.TextPrimary;
            }

            // Fire Module Status
            string fireStatusText = "● IN PROGRESS 60%";
            Color fireBadgeCol = UITheme.PrimaryOrangeSurface;
            Color fireTextCol = UITheme.PrimaryOrange;
            string fireBtnText = "CONTINUE TRAINING →";

            var fireCtrl = FindAnyObjectByType<FireArInteractionController>(FindObjectsInactive.Include);
            if (fireCtrl != null && fireCtrl.LatestAssessment != null)
            {
                fireStatusText = "● COMPLETED";
                fireBadgeCol = UITheme.SuccessSurface;
                fireTextCol = UITheme.SuccessText;
                fireBtnText = "RETAKE TRAINING →";
            }

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
            if (_fireStatusText != null)
            {
                _fireStatusText.text = fireStatusText;
                _fireStatusText.color = fireTextCol;
            }
            if (_fireStatusBg != null) _fireStatusBg.color = fireBadgeCol;
            if (_fireOfflineTagText != null)
            {
                _fireOfflineTagText.text = $"● {loc.Get("available_offline", "Available Offline")}";
                _fireOfflineTagText.color = UITheme.SuccessText;
            }
            if (_fireDurationTagText != null)
            {
                _fireDurationTagText.text = "~15 min";
                _fireDurationTagText.color = UITheme.TextSecondary;
            }
            if (_fireCheck1Text != null) _fireCheck1Text.text = "<color=#10B981>[OK]</color> Hazard identification";
            if (_fireCheck2Text != null) _fireCheck2Text.text = "<color=#10B981>[OK]</color> Extinguisher procedure (P.A.S.S.)";
            if (_fireCheck3Text != null) _fireCheck3Text.text = "<color=#10B981>[OK]</color> Emergency evacuation route";
            if (_fireStartButtonText != null)
            {
                _fireStartButtonText.text = $"<b>{fireBtnText}</b>";
                _fireStartButtonText.color = UITheme.TextLightOnDark;
            }

            // Gas Module Card
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
            if (_gasStatusText != null)
            {
                _gasStatusText.text = "● AVAILABLE";
                _gasStatusText.color = UITheme.SuccessText;
            }
            if (_gasStatusBg != null) _gasStatusBg.color = UITheme.SuccessSurface;
            if (_gasOfflineTagText != null)
            {
                _gasOfflineTagText.text = $"● {loc.Get("available_offline", "Available Offline")}";
                _gasOfflineTagText.color = UITheme.SuccessText;
            }
            if (_gasDurationTagText != null)
            {
                _gasDurationTagText.text = "~10 min";
                _gasDurationTagText.color = UITheme.TextSecondary;
            }
            if (_gasCheck1Text != null) _gasCheck1Text.text = "<color=#10B981>[OK]</color> Atmospheric multi-gas test";
            if (_gasCheck2Text != null) _gasCheck2Text.text = "<color=#10B981>[OK]</color> Level-A Hazmat PPE kit";
            if (_gasCheck3Text != null) _gasCheck3Text.text = "<color=#10B981>[OK]</color> Forced air ventilation & standby";
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

            // Records View
            if (_recordsTitleText != null)
            {
                _recordsTitleText.text = $"<b>{loc.Get("records_title", "TRAINING RECORDS")}</b>";
                _recordsTitleText.color = UITheme.TextPrimary;
            }
            if (_recordsSubText != null)
            {
                _recordsSubText.text = "Survives App Restarts • Stored 100% Offline";
                _recordsSubText.color = UITheme.TextSecondary;
            }
            if (_recordsSyncBtnText != null)
            {
                _recordsSyncBtnText.text = $"<b>{loc.Get("btn_sync_now", "SYNC NOW")}</b>";
                _recordsSyncBtnText.color = UITheme.TextLightOnDark;
            }

            // Certificates View
            if (_certTitleText != null)
            {
                _certTitleText.text = $"<b>{loc.Get("certificates_title", "Certificates")}</b>";
                _certTitleText.color = UITheme.TextPrimary;
            }
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
                _profileViewTitleText.text = $"<b>{loc.Get("worker_profile", "WORKER PROFILE")} & DATA SYNC</b>";
                _profileViewTitleText.color = UITheme.TextPrimary;
            }
            if (_pvNameLabel != null) _pvNameLabel.text = loc.Get("worker_name_label", "Worker Name");
            if (_pvNameVal != null) _pvNameVal.text = $"<b>{_workerName}</b>";
            if (_pvIdLabel != null) _pvIdLabel.text = loc.Get("worker_id_label", "Worker ID");
            if (_pvIdVal != null) _pvIdVal.text = $"<color=#F97316>{_workerId}</color>";
            if (_pvDivLabel != null) _pvDivLabel.text = loc.Get("worker_division", "Division: Mining & Material Handling");
            if (_pvLangLabel != null) _pvLangLabel.text = loc.Get("preferred_language_label", "Preferred Language");
            if (_pvLangVal != null) _pvLangVal.text = $"<b>{loc.CurrentLanguageDisplayName}</b>";
            if (_logoutBtnText != null) _logoutBtnText.text = $"<b>{loc.Get("btn_logout", "LOGOUT")}</b>";
            if (_syncNowBtnText != null) _syncNowBtnText.text = $"<b>{loc.Get("btn_sync_now", "↻ SYNC NOW")}</b>";
            if (_pvSettingsBtnText != null) _pvSettingsBtnText.text = $"<b>{loc.Get("settings_title", "APPLICATION SETTINGS")}</b>";

            // Bottom Navigation Bar
            if (_navHomeText != null) _navHomeText.text = $"<b>{loc.Get("nav_home", "HOME")}</b>";
            if (_navArText != null) _navArText.text = $"<b>{loc.Get("nav_ar", "AR")}</b>";
            if (_navRecordsText != null) _navRecordsText.text = $"<b>{loc.Get("nav_records", "RECORDS")}</b>";
            if (_navCertText != null) _navCertText.text = $"<b>{loc.Get("nav_certificates", "CERTIFICATES")}</b>";
            if (_navProfText != null) _navProfText.text = $"<b>{loc.Get("nav_profile", "PROFILE")}</b>";
            UpdateNavHighlight();

            // Settings View
            if (_settingsTitleText != null)
            {
                _settingsTitleText.text = $"<b>{loc.Get("settings_title", "APPLICATION SETTINGS")}</b>";
                _settingsTitleText.color = UITheme.TextPrimary;
            }
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
                _soundToggleButtonText.text = on ? $"<b>{loc.Get("state_on", "ON")}</b>" : $"<b>{loc.Get("state_off", "OFF")}</b>";
                _soundToggleButtonText.color = on ? UITheme.TextLightOnDark : UITheme.TextPrimary;
                if (_soundToggleBg != null) _soundToggleBg.color = on ? UITheme.PrimaryOrange : UITheme.CardSecondaryBg;
            }

            if (_alarmToggleButtonText != null)
            {
                bool on = audio.IsEmergencyAlarmEnabled;
                _alarmToggleButtonText.text = on ? $"<b>{loc.Get("state_on", "ON")}</b>" : $"<b>{loc.Get("state_off", "OFF")}</b>";
                _alarmToggleButtonText.color = on ? UITheme.TextLightOnDark : UITheme.TextPrimary;
                if (_alarmToggleBg != null) _alarmToggleBg.color = on ? UITheme.DangerRed : UITheme.CardSecondaryBg;
            }

            if (_volumeSlider != null) _volumeSlider.value = audio.EffectsVolume;
            if (_volumeValueText != null) _volumeValueText.text = $"{(audio.EffectsVolume * 100f):0}%";

            string curLang = loc != null ? loc.CurrentLanguage : LocaleService.LangEnglish;
            UITheme.ApplyLanguageButton(_btnLangEnglish, _btnLangEnglishBg, _btnLangEnglishText, curLang == LocaleService.LangEnglish);
            UITheme.ApplyLanguageButton(_btnLangHindi, _btnLangHindiBg, _btnLangHindiText, curLang == LocaleService.LangHindi);
            UITheme.ApplyLanguageButton(_btnLangSantali, _btnLangSantaliBg, _btnLangSantaliText, curLang == LocaleService.LangSantali);
        }

        // =========================================================================
        // DYNAMIC CONTENT REBUILDERS (RECORDS, CERTIFICATES, PROFILE, LOGIN)
        // =========================================================================

        private void PopulateRecordsList()
        {
            if (_recordsListContainer == null) return;

            // Clear previous cards
            for (int i = _recordsListContainer.transform.childCount - 1; i >= 0; i--)
            {
                var child = _recordsListContainer.transform.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            var records = LocalStorageService.Instance.GetRecordsForWorker(_workerId);
            if (records == null || records.Count == 0)
            {
                SeedInitialDemoRecords();
                records = LocalStorageService.Instance.GetRecordsForWorker(_workerId);
            }

            var font = FireInteractionFeedbackUI.GetDefaultFont();
            int count = Mathf.Min(records.Count, 3);

            for (int i = 0; i < count; i++)
            {
                var rec = records[i];
                float yTop = 0.98f - i * 0.31f;
                float yBot = yTop - 0.28f;

                var cardObj = new GameObject($"RecordCard_{i}");
                cardObj.transform.SetParent(_recordsListContainer.transform, false);
                var cRect = cardObj.AddComponent<RectTransform>();
                cRect.anchorMin = new Vector2(0f, yBot);
                cRect.anchorMax = new Vector2(1f, yTop);
                cRect.offsetMin = Vector2.zero;
                cRect.offsetMax = Vector2.zero;

                var cBg = cardObj.AddComponent<Image>();
                cBg.color = UITheme.CardBackground;

                // Card Button to open details
                var cBtn = cardObj.AddComponent<Button>();
                var cTap = cardObj.AddComponent<TapGatedButton>();
                var recordRef = rec;
                cTap.Initialize(() => OpenRecordDetail(recordRef));

                // Title Line
                var tObj = new GameObject("Title");
                tObj.transform.SetParent(cardObj.transform, false);
                var tRect = tObj.AddComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0.04f, 0.72f);
                tRect.anchorMax = new Vector2(0.68f, 0.96f);
                tRect.offsetMin = Vector2.zero;
                tRect.offsetMax = Vector2.zero;
                var tText = tObj.AddComponent<TextMeshProUGUI>();
                if (font != null) tText.font = font;
                tText.text = $"<b>{rec.moduleTitle}</b> • Att #{rec.attemptNumber}";
                tText.fontSize = 21;
                tText.fontStyle = FontStyles.Bold;
                tText.color = UITheme.TextPrimary;

                // Score Badge
                var bObj = new GameObject("ScoreBadge");
                bObj.transform.SetParent(cardObj.transform, false);
                var bRect = bObj.AddComponent<RectTransform>();
                bRect.anchorMin = new Vector2(0.70f, 0.70f);
                bRect.anchorMax = new Vector2(0.96f, 0.96f);
                bRect.offsetMin = Vector2.zero;
                bRect.offsetMax = Vector2.zero;
                var bBg = bObj.AddComponent<Image>();
                bBg.color = rec.passed ? UITheme.SuccessSurface : UITheme.DangerSurface;
                var btObj = new GameObject("Text");
                btObj.transform.SetParent(bObj.transform, false);
                var btRect = btObj.AddComponent<RectTransform>();
                btRect.anchorMin = Vector2.zero;
                btRect.anchorMax = Vector2.one;
                var btText = btObj.AddComponent<TextMeshProUGUI>();
                if (font != null) btText.font = font;
                btText.text = rec.passed ? $"<b>{(int)rec.score}% PASS</b>" : $"<b>{(int)rec.score}% FAIL</b>";
                btText.fontSize = 17;
                btText.fontStyle = FontStyles.Bold;
                btText.alignment = TextAlignmentOptions.Center;
                btText.color = rec.passed ? UITheme.SuccessText : UITheme.DangerText;

                // Date & Time
                var dObj = new GameObject("Date");
                dObj.transform.SetParent(cardObj.transform, false);
                var dRect = dObj.AddComponent<RectTransform>();
                dRect.anchorMin = new Vector2(0.04f, 0.44f);
                dRect.anchorMax = new Vector2(0.96f, 0.68f);
                dRect.offsetMin = Vector2.zero;
                dRect.offsetMax = Vector2.zero;
                var dText = dObj.AddComponent<TextMeshProUGUI>();
                if (font != null) dText.font = font;
                string dateStr = !string.IsNullOrEmpty(rec.completedAt) ? rec.completedAt.Substring(0, Mathf.Min(16, rec.completedAt.Length)).Replace("T", " ") : "2026-09-19 09:30";
                dText.text = $"Date: {dateStr}  |  Duration: ~8 min";
                dText.fontSize = 17;
                dText.color = UITheme.TextSecondary;

                // Status row: Certificate + Sync
                var sObj = new GameObject("StatusRow");
                sObj.transform.SetParent(cardObj.transform, false);
                var sRect = sObj.AddComponent<RectTransform>();
                sRect.anchorMin = new Vector2(0.04f, 0.10f);
                sRect.anchorMax = new Vector2(0.96f, 0.38f);
                sRect.offsetMin = Vector2.zero;
                sRect.offsetMax = Vector2.zero;
                var sText = sObj.AddComponent<TextMeshProUGUI>();
                if (font != null) sText.font = font;
                string certTag = rec.certificateStatus == "issued" ? "<color=#10B981>[CERT ISSUED]</color>" : "<color=#F97316>[PENDING SYNC]</color>";
                string syncTag = rec.syncStatus == "synced" ? "<color=#10B981>[SYNCED]</color>" : "<color=#F97316>[LOCAL ONLY]</color>";
                sText.text = $"{certTag}  •  {syncTag}  •  <color=#F97316><b>[ VIEW DETAILS ]</b></color>";
                sText.fontSize = 16;
                sText.fontStyle = FontStyles.Bold;
                sText.color = UITheme.TextPrimary;
            }
        }

        private void PopulateCertificatesList()
        {
            if (_certsListContainer == null) return;

            for (int i = _certsListContainer.transform.childCount - 1; i >= 0; i--)
            {
                var child = _certsListContainer.transform.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            var records = LocalStorageService.Instance.GetRecordsForWorker(_workerId);
            if (records == null || records.Count == 0)
            {
                SeedInitialDemoRecords();
                records = LocalStorageService.Instance.GetRecordsForWorker(_workerId);
            }

            var passedRecords = records.FindAll(r => r.passed);
            var font = FireInteractionFeedbackUI.GetDefaultFont();

            if (passedRecords.Count == 0)
            {
                // Show empty state
                if (_certTitleText != null) _certTitleText.gameObject.SetActive(true);
                if (_certDescText != null) _certDescText.gameObject.SetActive(true);
                if (_certSubText != null) _certSubText.gameObject.SetActive(true);
                return;
            }

            // Hide empty state text if displaying rich certificates
            if (_certDescText != null) _certDescText.gameObject.SetActive(false);
            if (_certSubText != null) _certSubText.gameObject.SetActive(false);

            int count = Mathf.Min(passedRecords.Count, 2);
            for (int i = 0; i < count; i++)
            {
                var rec = passedRecords[i];
                float yTop = 0.98f - i * 0.48f;
                float yBot = yTop - 0.44f;

                var certCardObj = new GameObject($"CertCard_{i}");
                certCardObj.transform.SetParent(_certsListContainer.transform, false);
                var cRect = certCardObj.AddComponent<RectTransform>();
                cRect.anchorMin = new Vector2(0f, yBot);
                cRect.anchorMax = new Vector2(1f, yTop);
                cRect.offsetMin = Vector2.zero;
                cRect.offsetMax = Vector2.zero;

                var cBg = certCardObj.AddComponent<Image>();
                cBg.color = UITheme.CardBackground;

                // Gold top banner
                var gBanner = new GameObject("Banner");
                gBanner.transform.SetParent(certCardObj.transform, false);
                var gbRect = gBanner.AddComponent<RectTransform>();
                gbRect.anchorMin = new Vector2(0f, 0.82f);
                gbRect.anchorMax = new Vector2(1f, 1f);
                gbRect.offsetMin = Vector2.zero;
                gbRect.offsetMax = Vector2.zero;
                var gbImg = gBanner.AddComponent<Image>();
                gbImg.color = UITheme.PrimaryOrange;

                var gbtObj = new GameObject("Text");
                gbtObj.transform.SetParent(gBanner.transform, false);
                var gbtRect = gbtObj.AddComponent<RectTransform>();
                gbtRect.anchorMin = Vector2.zero;
                gbtRect.anchorMax = Vector2.one;
                var gbtText = gbtObj.AddComponent<TextMeshProUGUI>();
                if (font != null) gbtText.font = font;
                gbtText.text = "<b>GOVERNMENT OF JHARKHAND • VOCATIONAL SAFETY CERTIFICATE</b>";
                gbtText.fontSize = 15;
                gbtText.fontStyle = FontStyles.Bold;
                gbtText.alignment = TextAlignmentOptions.Center;
                gbtText.color = UITheme.TextLightOnDark;

                // Module Title
                var mtObj = new GameObject("ModuleTitle");
                mtObj.transform.SetParent(certCardObj.transform, false);
                var mtRect = mtObj.AddComponent<RectTransform>();
                mtRect.anchorMin = new Vector2(0.04f, 0.60f);
                mtRect.anchorMax = new Vector2(0.96f, 0.80f);
                mtRect.offsetMin = Vector2.zero;
                mtRect.offsetMax = Vector2.zero;
                var mtText = mtObj.AddComponent<TextMeshProUGUI>();
                if (font != null) mtText.font = font;
                mtText.text = $"<b>{rec.moduleTitle}</b>";
                mtText.fontSize = 24;
                mtText.fontStyle = FontStyles.Bold;
                mtText.color = UITheme.TextPrimary;

                // Recipient
                var rcObj = new GameObject("Recipient");
                rcObj.transform.SetParent(certCardObj.transform, false);
                var rcRect = rcObj.AddComponent<RectTransform>();
                rcRect.anchorMin = new Vector2(0.04f, 0.38f);
                rcRect.anchorMax = new Vector2(0.96f, 0.58f);
                rcRect.offsetMin = Vector2.zero;
                rcRect.offsetMax = Vector2.zero;
                var rcText = rcObj.AddComponent<TextMeshProUGUI>();
                if (font != null) rcText.font = font;
                rcText.text = $"Awarded to: <b>{_workerName}</b> ({_workerId})\nScore: <color=#10B981><b>{(int)rec.score}% PASSED</b></color>  |  Status: <b>{rec.certificateStatus.ToUpper()}</b>";
                rcText.fontSize = 17;
                rcText.color = UITheme.TextSecondary;

                // QR / Verification Button
                var qrBtnObj = new GameObject("ViewQrButton");
                qrBtnObj.transform.SetParent(certCardObj.transform, false);
                var qrbRect = qrBtnObj.AddComponent<RectTransform>();
                qrbRect.anchorMin = new Vector2(0.04f, 0.06f);
                qrbRect.anchorMax = new Vector2(0.96f, 0.32f);
                qrbRect.offsetMin = Vector2.zero;
                qrbRect.offsetMax = Vector2.zero;

                var qrbImg = qrBtnObj.AddComponent<Image>();
                qrbImg.color = UITheme.CardSecondaryBg;
                var qrBtn = qrBtnObj.AddComponent<Button>();
                var qrTap = qrBtnObj.AddComponent<TapGatedButton>();
                string certId = rec.certificateId ?? "CERT-DEMO-001";
                string verifyUrl = rec.verificationUrl ?? $"https://industrial-safety.jharkhand.gov.in/verify?cert={certId}";
                qrTap.Initialize(() => OpenQrModal(certId, verifyUrl));

                var qrbtObj = new GameObject("Text");
                qrbtObj.transform.SetParent(qrBtnObj.transform, false);
                var qrbtRect = qrbtObj.AddComponent<RectTransform>();
                qrbtRect.anchorMin = Vector2.zero;
                qrbtRect.anchorMax = Vector2.one;
                var qrbtText = qrbtObj.AddComponent<TextMeshProUGUI>();
                if (font != null) qrbtText.font = font;
                qrbtText.text = "<b>VIEW QR & OFFICIAL VERIFICATION LINK -></b>";
                qrbtText.fontSize = 18;
                qrbtText.fontStyle = FontStyles.Bold;
                qrbtText.alignment = TextAlignmentOptions.Center;
                qrbtText.color = UITheme.PrimaryOrange;
            }
        }

        private void UpdateProfileView()
        {
            var pending = LocalStorageService.Instance.GetPendingOutbox();
            int pendingCount = pending != null ? pending.Count : 0;
            var records = LocalStorageService.Instance.GetRecordsForWorker(_workerId);
            int totalCount = records != null ? records.Count : 0;

            if (_pvSyncCountText != null)
            {
                _pvSyncCountText.text = $"Total Attempts: <b>{totalCount}</b>  |  Pending Cloud Sync: <color=#F97316><b>{pendingCount}</b></color>";
                _pvSyncCountText.color = UITheme.TextPrimary;
            }

            if (_pvLastSyncText != null)
            {
                string syncStatus = SyncService.Instance.LastSyncResult;
                _pvLastSyncText.text = $"Sync Engine Status: <b>{syncStatus}</b>";
                _pvLastSyncText.color = UITheme.TextSecondary;
            }
        }

        private void UpdateLoginView()
        {
            if (_loginIdDisplay != null) _loginIdDisplay.text = $"Worker ID: <b>{_inputWorkerCode}</b>";
            if (_loginPinDisplay != null) _loginPinDisplay.text = $"PIN: <b>{_inputPin}</b> (Default Demo: 1234)";
            if (_loginStatusText != null)
            {
                _loginStatusText.text = "Ready to authenticate online or offline.";
                _loginStatusText.color = UITheme.TextSecondary;
            }
        }

        // =========================================================================
        // SEEDING DEMO RECORDS (IDEMPOTENT)
        // =========================================================================

        private void SeedInitialDemoRecords()
        {
            var fireAttempt = new StoredTrainingRecord
            {
                clientAttemptId = "local_att_fire_001",
                workerId = _workerId,
                workerCode = "DEMO-001",
                workerName = "Operator Ramesh Kumar",
                moduleId = "fire-explosion-response",
                moduleTitle = "Fire & Explosion Response",
                score = 85.0f,
                passed = true,
                attemptNumber = 1,
                completedAt = DateTime.UtcNow.AddMinutes(-30).ToString("o"),
                syncStatus = "synced",
                certificatePublicId = "CERT-FIRE-2026-001",
                certificateStatus = "issued",
                verificationUrl = "https://industrial-safety.jharkhand.gov.in/verify?cert=CERT-FIRE-2026-001",
                breakdownItems = new List<string>
                {
                    "[PASS] Step 1: Hazard Detection — 10/10 pts",
                    "[PASS] Step 2: Hazard Classification — 10/10 pts",
                    "[PASS] Step 3: Extinguisher Selection — 15/15 pts",
                    "[PASS] Step 4: P.A.S.S. Pull Pin — 15/15 pts",
                    "[PASS] Step 5: P.A.S.S. Aim Base — 10/15 pts",
                    "[PASS] Step 6: P.A.S.S. Squeeze Lever — 10/10 pts",
                    "[PASS] Step 7: P.A.S.S. Sweep Side-to-Side — 5/10 pts",
                    "[PASS] Step 8: Evacuation Route — 10/10 pts",
                    "[PASS] Step 9: Assembly & Headcount — 5/5 pts"
                }
            };
            LocalStorageService.Instance.SaveDirectRecord(fireAttempt);

            var gasAttempt = new StoredTrainingRecord
            {
                clientAttemptId = "local_att_gas_001",
                workerId = _workerId,
                workerCode = "DEMO-001",
                workerName = "Operator Ramesh Kumar",
                moduleId = "gas-confined-space",
                moduleTitle = "Gas Leak & Confined Space Safety",
                score = 90.0f,
                passed = true,
                attemptNumber = 1,
                completedAt = DateTime.UtcNow.AddMinutes(-5).ToString("o"),
                syncStatus = "saved_on_device",
                certificatePublicId = "DRAFT-GAS-2026-002",
                certificateStatus = "pending_sync",
                verificationUrl = "https://industrial-safety.jharkhand.gov.in/verify?cert=DRAFT-GAS-2026-002",
                breakdownItems = new List<string>
                {
                    "[PASS] Step 1: Atmospheric Testing — 20/20 pts",
                    "[PASS] Step 2: Detector Calibration — 20/20 pts",
                    "[PASS] Step 3: Forced Air Ventilation — 15/20 pts",
                    "[PASS] Step 4: PPE Verification — 20/20 pts",
                    "[PASS] Step 5: Emergency Standby Protocol — 15/20 pts"
                }
            };
            LocalStorageService.Instance.SaveDirectRecord(gasAttempt);
        }

        // =========================================================================
        // MODALS (RECORD DETAIL & QR LINK)
        // =========================================================================

        public void OpenRecordDetail(StoredTrainingRecord record)
        {
            if (record == null) return;
            EnsureUIHierarchy();

            if (_rdTitleText != null) _rdTitleText.text = $"<b>{record.moduleTitle}</b>";
            if (_rdSubtitleText != null) _rdSubtitleText.text = $"Attempt #{record.attemptNumber} • Completed: {record.completedAt?.Substring(0, Mathf.Min(16, record.completedAt.Length)).Replace("T", " ")}";
            if (_rdScoreText != null) _rdScoreText.text = $"{(int)record.score}%";
            if (_rdStatusBadgeText != null)
            {
                _rdStatusBadgeText.text = record.passed ? "● PASSED" : "● FAILED";
                _rdStatusBadgeText.color = record.passed ? UITheme.SuccessText : UITheme.DangerText;
            }
            if (_rdStatusBadgeBg != null) _rdStatusBadgeBg.color = record.passed ? UITheme.SuccessSurface : UITheme.DangerSurface;

            if (_rdStepsBodyText != null)
            {
                var sb = new System.Text.StringBuilder();
                if (record.breakdownItems != null && record.breakdownItems.Count > 0)
                {
                    foreach (var s in record.breakdownItems)
                    {
                        sb.AppendLine(s);
                    }
                }
                else
                {
                    sb.AppendLine("● All curriculum procedures evaluated and recorded.");
                }
                _rdStepsBodyText.text = sb.ToString();
            }

            if (_rdMetaText != null)
            {
                string cert = record.certificatePublicId ?? "Pending issuance";
                _rdMetaText.text = $"Certificate ID: <color=#F97316>{cert}</color>  |  Sync Status: <b>{record.syncStatus}</b>";
            }

            if (_recordDetailModalRoot != null)
            {
                _recordDetailModalRoot.SetActive(true);
                _recordDetailModalRoot.transform.SetAsLastSibling();
            }
        }

        public void CloseRecordDetail()
        {
            if (_recordDetailModalRoot != null) _recordDetailModalRoot.SetActive(false);
        }

        public void OpenQrModal(string certId, string verifyUrl)
        {
            EnsureUIHierarchy();
            if (_qrTitleText != null) _qrTitleText.text = $"<b>OFFICIAL VERIFICATION</b>";
            if (_qrSubText != null) _qrSubText.text = $"Certificate ID: <color=#F97316><b>{certId}</b></color>";
            if (_qrUrlText != null) _qrUrlText.text = $"<b>{verifyUrl}</b>";
            if (_qrInstructionsText != null)
            {
                _qrInstructionsText.text = "This canonical URL is generated in compliance with the Government of Jharkhand Industrial Safety Portal. It validates certificate authenticity against the backend database.";
            }

            if (_qrModalRoot != null)
            {
                _qrModalRoot.SetActive(true);
                _qrModalRoot.transform.SetAsLastSibling();
            }
        }

        public void CloseQrModal()
        {
            if (_qrModalRoot != null) _qrModalRoot.SetActive(false);
        }

        // =========================================================================
        // MANUAL SYNC & AUTH ACTIONS
        // =========================================================================

        public void TriggerManualSync()
        {
            if (_syncNowBtnText != null) _syncNowBtnText.text = "<b>SYNCING...</b>";
            if (_recordsSyncBtnText != null) _recordsSyncBtnText.text = "<b>SYNCING...</b>";

            StartCoroutine(SyncService.Instance.RoutineSyncNow((success, msg) =>
            {
                if (_syncNowBtnText != null) _syncNowBtnText.text = "<b>↻ SYNC NOW</b>";
                if (_recordsSyncBtnText != null) _recordsSyncBtnText.text = "<b>↻ SYNC NOW</b>";
                UpdateProfileView();
                PopulateRecordsList();
                PopulateCertificatesList();
                UpdateOfflineStatus();
            }));
        }

        public void PerformOnlineLogin()
        {
            if (_loginStatusText != null)
            {
                _loginStatusText.text = "Contacting Fastify backend...";
                _loginStatusText.color = UITheme.PrimaryOrange;
            }

            StartCoroutine(WorkerSessionService.Instance.RoutineLoginOnline(_inputWorkerCode, _inputPin, (success, msg) =>
            {
                if (success)
                {
                    _workerName = WorkerSessionService.Instance.DisplayName;
                    _workerId = WorkerSessionService.Instance.WorkerId;
                    if (_loginStatusText != null)
                    {
                        _loginStatusText.text = $"<color=#10B981>Login successful! Welcome, {_workerName}.</color>";
                    }
                    HideLogin();
                }
                else
                {
                    if (_loginStatusText != null)
                    {
                        _loginStatusText.text = $"<color=#EF4444>Online login failed: {msg}\nTip: Use Offline Login or verify backend server.</color>";
                    }
                }
            }));
        }

        public void PerformOfflineLogin()
        {
            string err;
            bool ok = WorkerSessionService.Instance.LoginOffline(_inputWorkerCode, _inputPin, out err);
            if (ok)
            {
                _workerName = WorkerSessionService.Instance.DisplayName;
                _workerId = WorkerSessionService.Instance.WorkerId;
                if (_loginStatusText != null)
                {
                    _loginStatusText.text = $"<color=#10B981>Offline PIN verified! Welcome, {_workerName}.</color>";
                }
                HideLogin();
            }
            else
            {
                if (_loginStatusText != null)
                {
                    _loginStatusText.text = $"<color=#EF4444>{err}</color>";
                }
            }
        }

        // =========================================================================
        // PROCEDURAL UI HIERARCHY BUILDER
        // =========================================================================

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
                _rootCanvas.sortingOrder = 95;

                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            var defaultFont = FireInteractionFeedbackUI.GetDefaultFont();

            // =============================================================
            // 2. HOME SCREEN SHELL CONTAINER
            // =============================================================
            _homeRoot = new GameObject("HomeScreen");
            _homeRoot.transform.SetParent(_rootCanvas.transform, false);

            var homeRect = _homeRoot.AddComponent<RectTransform>();
            homeRect.anchorMin = Vector2.zero;
            homeRect.anchorMax = Vector2.one;
            homeRect.offsetMin = Vector2.zero;
            homeRect.offsetMax = Vector2.zero;

            // -------------------------------------------------------------
            // Top Header Bar (y: 0.905 to 0.975)
            // -------------------------------------------------------------
            var headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(_homeRoot.transform, false);
            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.04f, 0.905f);
            headerRect.anchorMax = new Vector2(0.96f, 0.975f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            // App Title
            var titleObj = new GameObject("AppTitle");
            titleObj.transform.SetParent(headerObj.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.60f);
            titleRect.anchorMax = new Vector2(0.74f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            _titleText = titleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _titleText.font = defaultFont;
            _titleText.fontSize = UITheme.DisplayTitleSize;
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.alignment = TextAlignmentOptions.Left;
            _titleText.color = UITheme.TextPrimary;

            // Welcome back line
            var welcomeObj = new GameObject("WelcomeBack");
            welcomeObj.transform.SetParent(headerObj.transform, false);
            var welcomeRect = welcomeObj.AddComponent<RectTransform>();
            welcomeRect.anchorMin = new Vector2(0f, 0.30f);
            welcomeRect.anchorMax = new Vector2(0.74f, 0.60f);
            welcomeRect.offsetMin = Vector2.zero;
            welcomeRect.offsetMax = Vector2.zero;

            _welcomeBackText = welcomeObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _welcomeBackText.font = defaultFont;
            _welcomeBackText.fontSize = 20;
            _welcomeBackText.alignment = TextAlignmentOptions.Left;
            _welcomeBackText.color = UITheme.TextSecondary;

            // Subtitle Tagline
            var subObj = new GameObject("SubtitleTagline");
            subObj.transform.SetParent(headerObj.transform, false);
            var subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0f);
            subRect.anchorMax = new Vector2(0.74f, 0.30f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;

            _subtitleText = subObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _subtitleText.font = defaultFont;
            _subtitleText.fontSize = 15;
            _subtitleText.color = UITheme.TextMuted;

            // Header Settings Button
            var setBtnObj = new GameObject("HeaderSettingsButton");
            setBtnObj.transform.SetParent(headerObj.transform, false);
            var setBtnRect = setBtnObj.AddComponent<RectTransform>();
            setBtnRect.anchorMin = new Vector2(0.76f, 0.20f);
            setBtnRect.anchorMax = new Vector2(1f, 0.85f);
            setBtnRect.offsetMin = Vector2.zero;
            setBtnRect.offsetMax = Vector2.zero;

            var setBtnImg = setBtnObj.AddComponent<Image>();
            setBtnImg.color = UITheme.CardSecondaryBg;
            _headerSettingsButton = setBtnObj.AddComponent<Button>();
            var sbTap = setBtnObj.AddComponent<TapGatedButton>();
            sbTap.Initialize(() => OpenSettings());

            var setBtnTextObj = new GameObject("Text");
            setBtnTextObj.transform.SetParent(setBtnObj.transform, false);
            var sbtr = setBtnTextObj.AddComponent<RectTransform>();
            sbtr.anchorMin = Vector2.zero;
            sbtr.anchorMax = Vector2.one;

            var sbTmp = setBtnTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) sbTmp.font = defaultFont;
            sbTmp.text = "SETTINGS";
            sbTmp.fontSize = 17;
            sbTmp.fontStyle = FontStyles.Bold;
            sbTmp.alignment = TextAlignmentOptions.Center;
            sbTmp.color = UITheme.TextPrimary;

            // -------------------------------------------------------------
            // Main Content Container (Tabs swap inside here)
            // -------------------------------------------------------------
            var contentContainer = new GameObject("ContentContainer");
            contentContainer.transform.SetParent(_homeRoot.transform, false);
            var ccRect = contentContainer.AddComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(0.04f, 0.128f);
            ccRect.anchorMax = new Vector2(0.96f, 0.895f);
            ccRect.offsetMin = Vector2.zero;
            ccRect.offsetMax = Vector2.zero;

            // =============================================================
            // TAB 1: HOME CONTENT ROOT
            // =============================================================
            _homeContentRoot = new GameObject("HomeContentRoot");
            _homeContentRoot.transform.SetParent(contentContainer.transform, false);
            var hcrRect = _homeContentRoot.AddComponent<RectTransform>();
            hcrRect.anchorMin = Vector2.zero;
            hcrRect.anchorMax = Vector2.one;
            hcrRect.offsetMin = Vector2.zero;
            hcrRect.offsetMax = Vector2.zero;

            // Worker Profile Card (Home)
            var profileCardObj = new GameObject("WorkerProfileCard");
            profileCardObj.transform.SetParent(_homeContentRoot.transform, false);
            var pcRect = profileCardObj.AddComponent<RectTransform>();
            pcRect.anchorMin = new Vector2(0f, 0.875f);
            pcRect.anchorMax = new Vector2(1f, 1f);
            pcRect.offsetMin = Vector2.zero;
            pcRect.offsetMax = Vector2.zero;

            var pcBg = profileCardObj.AddComponent<Image>();
            pcBg.color = UITheme.CardBackground;

            var pHeaderObj = new GameObject("ProfileHeader");
            pHeaderObj.transform.SetParent(profileCardObj.transform, false);
            var phRect = pHeaderObj.AddComponent<RectTransform>();
            phRect.anchorMin = new Vector2(0.04f, 0.68f);
            phRect.anchorMax = new Vector2(0.96f, 0.94f);
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;

            _profileHeaderLabelText = pHeaderObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _profileHeaderLabelText.font = defaultFont;
            _profileHeaderLabelText.text = "<color=#F97316><b>● WORKER PROFILE</b></color>";
            _profileHeaderLabelText.fontSize = 16;
            _profileHeaderLabelText.fontStyle = FontStyles.Bold;
            _profileHeaderLabelText.alignment = TextAlignmentOptions.Left;

            var pNameObj = new GameObject("ProfileName");
            pNameObj.transform.SetParent(profileCardObj.transform, false);
            var pnRect = pNameObj.AddComponent<RectTransform>();
            pnRect.anchorMin = new Vector2(0.04f, 0.36f);
            pnRect.anchorMax = new Vector2(0.96f, 0.68f);
            pnRect.offsetMin = Vector2.zero;
            pnRect.offsetMax = Vector2.zero;

            _profileNameText = pNameObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _profileNameText.font = defaultFont;
            _profileNameText.text = $"<b>{_workerName}</b>";
            _profileNameText.fontSize = 24;
            _profileNameText.fontStyle = FontStyles.Bold;
            _profileNameText.alignment = TextAlignmentOptions.Left;
            _profileNameText.color = UITheme.TextPrimary;

            var pIdObj = new GameObject("ProfileIdAndDivision");
            pIdObj.transform.SetParent(profileCardObj.transform, false);
            var pidRect = pIdObj.AddComponent<RectTransform>();
            pidRect.anchorMin = new Vector2(0.04f, 0.06f);
            pidRect.anchorMax = new Vector2(0.96f, 0.36f);
            pidRect.offsetMin = Vector2.zero;
            pidRect.offsetMax = Vector2.zero;

            _profileIdText = pIdObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _profileIdText.font = defaultFont;
            _profileIdText.text = $"Worker ID: <color=#F97316>{_workerId}</color>  |  Division: Mining & Material Handling";
            _profileIdText.fontSize = 17;
            _profileIdText.alignment = TextAlignmentOptions.Left;
            _profileIdText.color = UITheme.TextSecondary;

            // Section Header: Modules
            var mHeaderObj = new GameObject("ModulesHeader");
            mHeaderObj.transform.SetParent(_homeContentRoot.transform, false);
            var mhRect = mHeaderObj.AddComponent<RectTransform>();
            mhRect.anchorMin = new Vector2(0f, 0.835f);
            mhRect.anchorMax = new Vector2(1f, 0.865f);
            mhRect.offsetMin = Vector2.zero;
            mhRect.offsetMax = Vector2.zero;

            _modulesHeaderText = mHeaderObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _modulesHeaderText.font = defaultFont;
            _modulesHeaderText.text = "<b>AVAILABLE MODULES</b>";
            _modulesHeaderText.fontSize = UITheme.SectionHeadingSize;
            _modulesHeaderText.fontStyle = FontStyles.Bold;
            _modulesHeaderText.alignment = TextAlignmentOptions.Left;
            _modulesHeaderText.color = UITheme.TextPrimary;

            // Module Card 1: Fire & Explosion Response
            var fireCardObj = new GameObject("ModuleCard_Fire");
            fireCardObj.transform.SetParent(_homeContentRoot.transform, false);
            var fcRect = fireCardObj.AddComponent<RectTransform>();
            fcRect.anchorMin = new Vector2(0f, 0.470f);
            fcRect.anchorMax = new Vector2(1f, 0.825f);
            fcRect.offsetMin = Vector2.zero;
            fcRect.offsetMax = Vector2.zero;

            var fcBg = fireCardObj.AddComponent<Image>();
            fcBg.color = UITheme.CardBackground;

            // Fire AR Badge
            var fireArBadgeObj = new GameObject("FireArBadge");
            fireArBadgeObj.transform.SetParent(fireCardObj.transform, false);
            var fabRect = fireArBadgeObj.AddComponent<RectTransform>();
            fabRect.anchorMin = new Vector2(0.04f, 0.84f);
            fabRect.anchorMax = new Vector2(0.13f, 0.96f);
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
            UITheme.ApplyModuleBadge(fabBg, _fireArBadgeText);

            // Fire Title
            var fireTitleObj = new GameObject("FireTitle");
            fireTitleObj.transform.SetParent(fireCardObj.transform, false);
            var ftRect = fireTitleObj.AddComponent<RectTransform>();
            ftRect.anchorMin = new Vector2(0.15f, 0.82f);
            ftRect.anchorMax = new Vector2(0.68f, 0.98f);
            ftRect.offsetMin = Vector2.zero;
            ftRect.offsetMax = Vector2.zero;

            _fireTitleText = fireTitleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireTitleText.font = defaultFont;
            _fireTitleText.text = "<b>Fire & Explosion Response</b>";
            _fireTitleText.fontSize = 24;
            _fireTitleText.fontStyle = FontStyles.Bold;
            _fireTitleText.alignment = TextAlignmentOptions.Left;
            _fireTitleText.color = UITheme.TextPrimary;

            // Fire Status Badge
            var fireStatusObj = new GameObject("FireStatusBadge");
            fireStatusObj.transform.SetParent(fireCardObj.transform, false);
            var fsbRect = fireStatusObj.AddComponent<RectTransform>();
            fsbRect.anchorMin = new Vector2(0.70f, 0.84f);
            fsbRect.anchorMax = new Vector2(0.96f, 0.96f);
            fsbRect.offsetMin = Vector2.zero;
            fsbRect.offsetMax = Vector2.zero;

            _fireStatusBg = fireStatusObj.AddComponent<Image>();
            _fireStatusBg.color = UITheme.PrimaryOrangeSurface;

            var fsbTextObj = new GameObject("Text");
            fsbTextObj.transform.SetParent(fireStatusObj.transform, false);
            var fsbtRect = fsbTextObj.AddComponent<RectTransform>();
            fsbtRect.anchorMin = Vector2.zero;
            fsbtRect.anchorMax = Vector2.one;

            _fireStatusText = fsbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireStatusText.font = defaultFont;
            _fireStatusText.text = "● IN PROGRESS 60%";
            _fireStatusText.fontSize = 16;
            _fireStatusText.fontStyle = FontStyles.Bold;
            _fireStatusText.alignment = TextAlignmentOptions.Center;
            _fireStatusText.color = UITheme.PrimaryOrange;

            // Fire 3-Point Checklist Container
            var fCheckObj = new GameObject("FireChecklist");
            fCheckObj.transform.SetParent(fireCardObj.transform, false);
            var fchkRect = fCheckObj.AddComponent<RectTransform>();
            fchkRect.anchorMin = new Vector2(0.04f, 0.36f);
            fchkRect.anchorMax = new Vector2(0.96f, 0.78f);
            fchkRect.offsetMin = Vector2.zero;
            fchkRect.offsetMax = Vector2.zero;

            var fc1 = new GameObject("Item1");
            fc1.transform.SetParent(fCheckObj.transform, false);
            var fc1R = fc1.AddComponent<RectTransform>();
            fc1R.anchorMin = new Vector2(0f, 0.68f);
            fc1R.anchorMax = new Vector2(1f, 1f);
            fc1R.offsetMin = Vector2.zero;
            fc1R.offsetMax = Vector2.zero;
            _fireCheck1Text = fc1.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireCheck1Text.font = defaultFont;
            _fireCheck1Text.text = "<color=#10B981>[OK]</color> Hazard identification & detection";
            _fireCheck1Text.fontSize = 19;
            _fireCheck1Text.color = UITheme.TextPrimary;

            var fc2 = new GameObject("Item2");
            fc2.transform.SetParent(fCheckObj.transform, false);
            var fc2R = fc2.AddComponent<RectTransform>();
            fc2R.anchorMin = new Vector2(0f, 0.34f);
            fc2R.anchorMax = new Vector2(1f, 0.66f);
            fc2R.offsetMin = Vector2.zero;
            fc2R.offsetMax = Vector2.zero;
            _fireCheck2Text = fc2.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireCheck2Text.font = defaultFont;
            _fireCheck2Text.text = "<color=#10B981>[OK]</color> Extinguisher procedure (P.A.S.S.)";
            _fireCheck2Text.fontSize = 19;
            _fireCheck2Text.color = UITheme.TextPrimary;

            var fc3 = new GameObject("Item3");
            fc3.transform.SetParent(fCheckObj.transform, false);
            var fc3R = fc3.AddComponent<RectTransform>();
            fc3R.anchorMin = new Vector2(0f, 0f);
            fc3R.anchorMax = new Vector2(1f, 0.32f);
            fc3R.offsetMin = Vector2.zero;
            fc3R.offsetMax = Vector2.zero;
            _fireCheck3Text = fc3.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireCheck3Text.font = defaultFont;
            _fireCheck3Text.text = "<color=#10B981>[OK]</color> Emergency evacuation route";
            _fireCheck3Text.fontSize = 19;
            _fireCheck3Text.color = UITheme.TextPrimary;

            // Fire Info Row (Offline Tag + Duration)
            var fInfoObj = new GameObject("FireInfoRow");
            fInfoObj.transform.SetParent(fireCardObj.transform, false);
            var fiRect = fInfoObj.AddComponent<RectTransform>();
            fiRect.anchorMin = new Vector2(0.04f, 0.22f);
            fiRect.anchorMax = new Vector2(0.96f, 0.34f);
            fiRect.offsetMin = Vector2.zero;
            fiRect.offsetMax = Vector2.zero;

            var fotObj = new GameObject("OfflineTag");
            fotObj.transform.SetParent(fInfoObj.transform, false);
            var fotR = fotObj.AddComponent<RectTransform>();
            fotR.anchorMin = new Vector2(0f, 0f);
            fotR.anchorMax = new Vector2(0.60f, 1f);
            fotR.offsetMin = Vector2.zero;
            fotR.offsetMax = Vector2.zero;
            _fireOfflineTagText = fotObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireOfflineTagText.font = defaultFont;
            _fireOfflineTagText.text = "● Available Offline";
            _fireOfflineTagText.fontSize = 16;
            _fireOfflineTagText.fontStyle = FontStyles.Bold;
            _fireOfflineTagText.color = UITheme.SuccessText;

            var fdtObj = new GameObject("DurationTag");
            fdtObj.transform.SetParent(fInfoObj.transform, false);
            var fdtR = fdtObj.AddComponent<RectTransform>();
            fdtR.anchorMin = new Vector2(0.62f, 0f);
            fdtR.anchorMax = new Vector2(1f, 1f);
            fdtR.offsetMin = Vector2.zero;
            fdtR.offsetMax = Vector2.zero;
            _fireDurationTagText = fdtObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _fireDurationTagText.font = defaultFont;
            _fireDurationTagText.text = "~15 min";
            _fireDurationTagText.fontSize = 16;
            _fireDurationTagText.alignment = TextAlignmentOptions.Right;
            _fireDurationTagText.color = UITheme.TextSecondary;

            // Fire Start Button
            var fireBtnObj = new GameObject("StartFireButton");
            fireBtnObj.transform.SetParent(fireCardObj.transform, false);
            var fbRect = fireBtnObj.AddComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.04f, 0.04f);
            fbRect.anchorMax = new Vector2(0.96f, 0.19f);
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
            _fireStartButtonText.text = "<b>CONTINUE TRAINING →</b>";
            _fireStartButtonText.fontSize = 24;
            _fireStartButtonText.fontStyle = FontStyles.Bold;
            _fireStartButtonText.alignment = TextAlignmentOptions.Center;
            _fireStartButtonText.color = UITheme.TextLightOnDark;

            // Module Card 2: Gas Leak & Confined Space
            var gasCardObj = new GameObject("ModuleCard_Gas");
            gasCardObj.transform.SetParent(_homeContentRoot.transform, false);
            var gcRect = gasCardObj.AddComponent<RectTransform>();
            gcRect.anchorMin = new Vector2(0f, 0.105f);
            gcRect.anchorMax = new Vector2(1f, 0.460f);
            gcRect.offsetMin = Vector2.zero;
            gcRect.offsetMax = Vector2.zero;

            var gcBg = gasCardObj.AddComponent<Image>();
            gcBg.color = UITheme.CardBackground;

            // Gas AR Badge
            var gasArBadgeObj = new GameObject("GasArBadge");
            gasArBadgeObj.transform.SetParent(gasCardObj.transform, false);
            var gabRect = gasArBadgeObj.AddComponent<RectTransform>();
            gabRect.anchorMin = new Vector2(0.04f, 0.84f);
            gabRect.anchorMax = new Vector2(0.13f, 0.96f);
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
            UITheme.ApplyModuleBadge(gabBg, _gasArBadgeText);

            // Gas Title
            var gasTitleObj = new GameObject("GasTitle");
            gasTitleObj.transform.SetParent(gasCardObj.transform, false);
            var gtRect = gasTitleObj.AddComponent<RectTransform>();
            gtRect.anchorMin = new Vector2(0.15f, 0.82f);
            gtRect.anchorMax = new Vector2(0.68f, 0.98f);
            gtRect.offsetMin = Vector2.zero;
            gtRect.offsetMax = Vector2.zero;

            _gasTitleText = gasTitleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasTitleText.font = defaultFont;
            _gasTitleText.text = "<b>Gas Leak & Confined Space Safety</b>";
            _gasTitleText.fontSize = 24;
            _gasTitleText.fontStyle = FontStyles.Bold;
            _gasTitleText.alignment = TextAlignmentOptions.Left;
            _gasTitleText.color = UITheme.TextPrimary;

            // Gas Status Badge
            var gasStatusObj = new GameObject("GasStatusBadge");
            gasStatusObj.transform.SetParent(gasCardObj.transform, false);
            var gsbRect = gasStatusObj.AddComponent<RectTransform>();
            gsbRect.anchorMin = new Vector2(0.70f, 0.84f);
            gsbRect.anchorMax = new Vector2(0.96f, 0.96f);
            gsbRect.offsetMin = Vector2.zero;
            gsbRect.offsetMax = Vector2.zero;

            _gasStatusBg = gasStatusObj.AddComponent<Image>();
            _gasStatusBg.color = UITheme.SuccessSurface;

            var gsbTextObj = new GameObject("Text");
            gsbTextObj.transform.SetParent(gasStatusObj.transform, false);
            var gsbtRect = gsbTextObj.AddComponent<RectTransform>();
            gsbtRect.anchorMin = Vector2.zero;
            gsbtRect.anchorMax = Vector2.one;

            _gasStatusText = gsbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasStatusText.font = defaultFont;
            _gasStatusText.text = "● AVAILABLE";
            _gasStatusText.fontSize = 16;
            _gasStatusText.fontStyle = FontStyles.Bold;
            _gasStatusText.alignment = TextAlignmentOptions.Center;
            _gasStatusText.color = UITheme.SuccessText;

            // Gas 3-Point Checklist
            var gCheckObj = new GameObject("GasChecklist");
            gCheckObj.transform.SetParent(gasCardObj.transform, false);
            var gchkRect = gCheckObj.AddComponent<RectTransform>();
            gchkRect.anchorMin = new Vector2(0.04f, 0.36f);
            gchkRect.anchorMax = new Vector2(0.96f, 0.78f);
            gchkRect.offsetMin = Vector2.zero;
            gchkRect.offsetMax = Vector2.zero;

            var gc1 = new GameObject("Item1");
            gc1.transform.SetParent(gCheckObj.transform, false);
            var gc1R = gc1.AddComponent<RectTransform>();
            gc1R.anchorMin = new Vector2(0f, 0.68f);
            gc1R.anchorMax = new Vector2(1f, 1f);
            gc1R.offsetMin = Vector2.zero;
            gc1R.offsetMax = Vector2.zero;
            _gasCheck1Text = gc1.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasCheck1Text.font = defaultFont;
            _gasCheck1Text.text = "<color=#10B981>[OK]</color> Atmospheric multi-gas test";
            _gasCheck1Text.fontSize = 19;
            _gasCheck1Text.color = UITheme.TextPrimary;

            var gc2 = new GameObject("Item2");
            gc2.transform.SetParent(gCheckObj.transform, false);
            var gc2R = gc2.AddComponent<RectTransform>();
            gc2R.anchorMin = new Vector2(0f, 0.34f);
            gc2R.anchorMax = new Vector2(1f, 0.66f);
            gc2R.offsetMin = Vector2.zero;
            gc2R.offsetMax = Vector2.zero;
            _gasCheck2Text = gc2.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasCheck2Text.font = defaultFont;
            _gasCheck2Text.text = "<color=#10B981>[OK]</color> Level-A Hazmat PPE kit";
            _gasCheck2Text.fontSize = 19;
            _gasCheck2Text.color = UITheme.TextPrimary;

            var gc3 = new GameObject("Item3");
            gc3.transform.SetParent(gCheckObj.transform, false);
            var gc3R = gc3.AddComponent<RectTransform>();
            gc3R.anchorMin = new Vector2(0f, 0f);
            gc3R.anchorMax = new Vector2(1f, 0.32f);
            gc3R.offsetMin = Vector2.zero;
            gc3R.offsetMax = Vector2.zero;
            _gasCheck3Text = gc3.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasCheck3Text.font = defaultFont;
            _gasCheck3Text.text = "<color=#10B981>[OK]</color> Forced air ventilation & standby";
            _gasCheck3Text.fontSize = 19;
            _gasCheck3Text.color = UITheme.TextPrimary;

            // Gas Info Row
            var gInfoObj = new GameObject("GasInfoRow");
            gInfoObj.transform.SetParent(gasCardObj.transform, false);
            var giRect = gInfoObj.AddComponent<RectTransform>();
            giRect.anchorMin = new Vector2(0.04f, 0.22f);
            giRect.anchorMax = new Vector2(0.96f, 0.34f);
            giRect.offsetMin = Vector2.zero;
            giRect.offsetMax = Vector2.zero;

            var gotObj = new GameObject("OfflineTag");
            gotObj.transform.SetParent(gInfoObj.transform, false);
            var gotR = gotObj.AddComponent<RectTransform>();
            gotR.anchorMin = new Vector2(0f, 0f);
            gotR.anchorMax = new Vector2(0.60f, 1f);
            gotR.offsetMin = Vector2.zero;
            gotR.offsetMax = Vector2.zero;
            _gasOfflineTagText = gotObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasOfflineTagText.font = defaultFont;
            _gasOfflineTagText.text = "● Available Offline";
            _gasOfflineTagText.fontSize = 16;
            _gasOfflineTagText.fontStyle = FontStyles.Bold;
            _gasOfflineTagText.color = UITheme.SuccessText;

            var gdtObj = new GameObject("DurationTag");
            gdtObj.transform.SetParent(gInfoObj.transform, false);
            var gdtR = gdtObj.AddComponent<RectTransform>();
            gdtR.anchorMin = new Vector2(0.62f, 0f);
            gdtR.anchorMax = new Vector2(1f, 1f);
            gdtR.offsetMin = Vector2.zero;
            gdtR.offsetMax = Vector2.zero;
            _gasDurationTagText = gdtObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _gasDurationTagText.font = defaultFont;
            _gasDurationTagText.text = "~10 min";
            _gasDurationTagText.fontSize = 16;
            _gasDurationTagText.alignment = TextAlignmentOptions.Right;
            _gasDurationTagText.color = UITheme.TextSecondary;

            // Gas Start Button
            var gasBtnObj = new GameObject("GasStartButton");
            gasBtnObj.transform.SetParent(gasCardObj.transform, false);
            var gbRect = gasBtnObj.AddComponent<RectTransform>();
            gbRect.anchorMin = new Vector2(0.04f, 0.04f);
            gbRect.anchorMax = new Vector2(0.96f, 0.19f);
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
            _gasStartButtonText.fontStyle = FontStyles.Bold;
            _gasStartButtonText.alignment = TextAlignmentOptions.Center;
            _gasStartButtonText.color = UITheme.TextLightOnDark;

            // Application Settings Button in Home
            var prominentSettingsBtnObj = new GameObject("ProminentSettingsButton");
            prominentSettingsBtnObj.transform.SetParent(_homeContentRoot.transform, false);
            var psbRect = prominentSettingsBtnObj.AddComponent<RectTransform>();
            psbRect.anchorMin = new Vector2(0f, 0.010f);
            psbRect.anchorMax = new Vector2(1f, 0.095f);
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
            _homeSettingsButtonText.fontStyle = FontStyles.Bold;
            _homeSettingsButtonText.alignment = TextAlignmentOptions.Center;
            _homeSettingsButtonText.color = UITheme.TextPrimary;

            // =============================================================
            // TAB 2: AR CONTENT ROOT
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
            arCardRect.anchorMin = new Vector2(0.06f, 0.30f);
            arCardRect.anchorMax = new Vector2(0.94f, 0.70f);
            arCardRect.offsetMin = Vector2.zero;
            arCardRect.offsetMax = Vector2.zero;

            var arCardBg = arCardObj.AddComponent<Image>();
            arCardBg.color = UITheme.CardBackground;

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
            _arTitleText.fontStyle = FontStyles.Bold;
            _arTitleText.alignment = TextAlignmentOptions.Center;
            _arTitleText.color = UITheme.TextPrimary;

            var arSubObj = new GameObject("ArSubtitle");
            arSubObj.transform.SetParent(arCardObj.transform, false);
            var asRect = arSubObj.AddComponent<RectTransform>();
            asRect.anchorMin = new Vector2(0.06f, 0.70f);
            asRect.anchorMax = new Vector2(0.94f, 0.80f);
            asRect.offsetMin = Vector2.zero;
            asRect.offsetMax = Vector2.zero;

            _arSubtitleText = arSubObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _arSubtitleText.font = defaultFont;
            _arSubtitleText.text = "Interactive Industrial Safety Training";
            _arSubtitleText.fontSize = 20;
            _arSubtitleText.alignment = TextAlignmentOptions.Center;
            _arSubtitleText.color = UITheme.TextSecondary;

            var arDescObj = new GameObject("ArDesc");
            arDescObj.transform.SetParent(arCardObj.transform, false);
            var adRect = arDescObj.AddComponent<RectTransform>();
            adRect.anchorMin = new Vector2(0.06f, 0.50f);
            adRect.anchorMax = new Vector2(0.94f, 0.68f);
            adRect.offsetMin = Vector2.zero;
            adRect.offsetMax = Vector2.zero;

            _arDescText = arDescObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _arDescText.font = defaultFont;
            _arDescText.text = "Use your phone camera to enter an interactive safety scenario.";
            _arDescText.fontSize = 21;
            _arDescText.alignment = TextAlignmentOptions.Center;
            _arDescText.color = UITheme.TextPrimary;

            var arStatusBoxObj = new GameObject("ArStatusBox");
            arStatusBoxObj.transform.SetParent(arCardObj.transform, false);
            var asbRect = arStatusBoxObj.AddComponent<RectTransform>();
            asbRect.anchorMin = new Vector2(0.06f, 0.32f);
            asbRect.anchorMax = new Vector2(0.94f, 0.46f);
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

            var arToggleBtnObj = new GameObject("ArToggleButton");
            arToggleBtnObj.transform.SetParent(arCardObj.transform, false);
            var atbRect = arToggleBtnObj.AddComponent<RectTransform>();
            atbRect.anchorMin = new Vector2(0.06f, 0.14f);
            atbRect.anchorMax = new Vector2(0.94f, 0.28f);
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
            _arToggleButtonText.fontStyle = FontStyles.Bold;
            _arToggleButtonText.alignment = TextAlignmentOptions.Center;
            _arToggleButtonText.color = UITheme.TextLightOnDark;

            var arFireBtnObj = new GameObject("ArStartFireShortcut");
            arFireBtnObj.transform.SetParent(arCardObj.transform, false);
            var afbRect = arFireBtnObj.AddComponent<RectTransform>();
            afbRect.anchorMin = new Vector2(0.06f, 0.02f);
            afbRect.anchorMax = new Vector2(0.94f, 0.12f);
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
            _arStartFireShortcutBtnText.fontSize = 22;
            _arStartFireShortcutBtnText.fontStyle = FontStyles.Bold;
            _arStartFireShortcutBtnText.alignment = TextAlignmentOptions.Center;
            _arStartFireShortcutBtnText.color = UITheme.SuccessText;
            arFireBtnObj.SetActive(false);

            _arContentRoot.SetActive(false);

            // =============================================================
            // TAB 3: RECORDS CONTENT ROOT (MY TRAINING RECORDS)
            // =============================================================
            _recordsContentRoot = new GameObject("RecordsContentRoot");
            _recordsContentRoot.transform.SetParent(contentContainer.transform, false);
            var rcrRect = _recordsContentRoot.AddComponent<RectTransform>();
            rcrRect.anchorMin = Vector2.zero;
            rcrRect.anchorMax = Vector2.one;
            rcrRect.offsetMin = Vector2.zero;
            rcrRect.offsetMax = Vector2.zero;

            // Top Header Line
            var rHeaderObj = new GameObject("RecordsHeader");
            rHeaderObj.transform.SetParent(_recordsContentRoot.transform, false);
            var rhRect = rHeaderObj.AddComponent<RectTransform>();
            rhRect.anchorMin = new Vector2(0f, 0.93f);
            rhRect.anchorMax = new Vector2(1f, 1f);
            rhRect.offsetMin = Vector2.zero;
            rhRect.offsetMax = Vector2.zero;

            _recordsTitleText = rHeaderObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _recordsTitleText.font = defaultFont;
            _recordsTitleText.text = "<b>TRAINING RECORDS</b>";
            _recordsTitleText.fontSize = 28;
            _recordsTitleText.fontStyle = FontStyles.Bold;
            _recordsTitleText.color = UITheme.TextPrimary;

            var rSubObj = new GameObject("RecordsSubtitle");
            rSubObj.transform.SetParent(_recordsContentRoot.transform, false);
            var rsRect = rSubObj.AddComponent<RectTransform>();
            rsRect.anchorMin = new Vector2(0f, 0.885f);
            rsRect.anchorMax = new Vector2(1f, 0.925f);
            rsRect.offsetMin = Vector2.zero;
            rsRect.offsetMax = Vector2.zero;

            _recordsSubText = rSubObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _recordsSubText.font = defaultFont;
            _recordsSubText.text = "Survives App Restarts • Stored 100% Offline";
            _recordsSubText.fontSize = 18;
            _recordsSubText.color = UITheme.TextSecondary;

            // Dynamic Records List Container
            _recordsListContainer = new GameObject("RecordsListContainer");
            _recordsListContainer.transform.SetParent(_recordsContentRoot.transform, false);
            var rlcRect = _recordsListContainer.AddComponent<RectTransform>();
            rlcRect.anchorMin = new Vector2(0f, 0.12f);
            rlcRect.anchorMax = new Vector2(1f, 0.875f);
            rlcRect.offsetMin = Vector2.zero;
            rlcRect.offsetMax = Vector2.zero;

            // Bottom Manual Sync Button
            var rSyncBtnObj = new GameObject("RecordsSyncNowButton");
            rSyncBtnObj.transform.SetParent(_recordsContentRoot.transform, false);
            var rsbRect = rSyncBtnObj.AddComponent<RectTransform>();
            rsbRect.anchorMin = new Vector2(0.04f, 0.015f);
            rsbRect.anchorMax = new Vector2(0.96f, 0.095f);
            rsbRect.offsetMin = Vector2.zero;
            rsbRect.offsetMax = Vector2.zero;

            var rsbImg = rSyncBtnObj.AddComponent<Image>();
            rsbImg.color = UITheme.PrimaryOrange;
            _recordsSyncBtn = rSyncBtnObj.AddComponent<Button>();
            var rsbTap = rSyncBtnObj.AddComponent<TapGatedButton>();
            rsbTap.Initialize(() => TriggerManualSync());

            var rsbtObj = new GameObject("Text");
            rsbtObj.transform.SetParent(rSyncBtnObj.transform, false);
            var rsbtRect = rsbtObj.AddComponent<RectTransform>();
            rsbtRect.anchorMin = Vector2.zero;
            rsbtRect.anchorMax = Vector2.one;

            _recordsSyncBtnText = rsbtObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _recordsSyncBtnText.font = defaultFont;
            _recordsSyncBtnText.text = "<b>SYNC NOW</b>";
            _recordsSyncBtnText.fontSize = 22;
            _recordsSyncBtnText.fontStyle = FontStyles.Bold;
            _recordsSyncBtnText.alignment = TextAlignmentOptions.Center;
            _recordsSyncBtnText.color = UITheme.TextLightOnDark;

            _recordsContentRoot.SetActive(false);

            // =============================================================
            // TAB 4: CERTIFICATES CONTENT ROOT
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
            ccardRect.anchorMin = new Vector2(0f, 0.88f);
            ccardRect.anchorMax = new Vector2(1f, 1f);
            ccardRect.offsetMin = Vector2.zero;
            ccardRect.offsetMax = Vector2.zero;

            var ctitRect = certCardObj.GetComponent<RectTransform>();
            _certTitleText = certCardObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _certTitleText.font = defaultFont;
            _certTitleText.text = "<b>Certificates</b>";
            _certTitleText.fontSize = 28;
            _certTitleText.fontStyle = FontStyles.Bold;
            _certTitleText.alignment = TextAlignmentOptions.Left;
            _certTitleText.color = UITheme.TextPrimary;

            // Empty state texts
            var certSubObj = new GameObject("CertSub");
            certSubObj.transform.SetParent(_certificatesContentRoot.transform, false);
            var csubRect = certSubObj.AddComponent<RectTransform>();
            csubRect.anchorMin = new Vector2(0.06f, 0.46f);
            csubRect.anchorMax = new Vector2(0.94f, 0.58f);
            csubRect.offsetMin = Vector2.zero;
            csubRect.offsetMax = Vector2.zero;

            _certSubText = certSubObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _certSubText.font = defaultFont;
            _certSubText.text = "<b>No official certificates issued yet.</b>";
            _certSubText.fontSize = 22;
            _certSubText.alignment = TextAlignmentOptions.Center;
            _certSubText.color = UITheme.TextPrimary;

            var certDescObj = new GameObject("CertDesc");
            certDescObj.transform.SetParent(_certificatesContentRoot.transform, false);
            var cdescRect = certDescObj.AddComponent<RectTransform>();
            cdescRect.anchorMin = new Vector2(0.06f, 0.28f);
            cdescRect.anchorMax = new Vector2(0.94f, 0.44f);
            cdescRect.offsetMin = Vector2.zero;
            cdescRect.offsetMax = Vector2.zero;

            _certDescText = certDescObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _certDescText.font = defaultFont;
            _certDescText.text = "Training certificates will appear here after successful training and synchronization.";
            _certDescText.fontSize = 20;
            _certDescText.alignment = TextAlignmentOptions.Center;
            _certDescText.color = UITheme.TextSecondary;

            _certsListContainer = new GameObject("CertsListContainer");
            _certsListContainer.transform.SetParent(_certificatesContentRoot.transform, false);
            var clcRect = _certsListContainer.AddComponent<RectTransform>();
            clcRect.anchorMin = new Vector2(0f, 0.04f);
            clcRect.anchorMax = new Vector2(1f, 0.86f);
            clcRect.offsetMin = Vector2.zero;
            clcRect.offsetMax = Vector2.zero;

            _certificatesContentRoot.SetActive(false);

            // =============================================================
            // TAB 5: PROFILE CONTENT ROOT
            // =============================================================
            _profileContentRoot = new GameObject("ProfileContentRoot");
            _profileContentRoot.transform.SetParent(contentContainer.transform, false);
            var pcrRect = _profileContentRoot.AddComponent<RectTransform>();
            pcrRect.anchorMin = Vector2.zero;
            pcrRect.anchorMax = Vector2.one;
            pcrRect.offsetMin = Vector2.zero;
            pcrRect.offsetMax = Vector2.zero;

            // Worker Info Card
            var pCardObj = new GameObject("ProfileDetailsCard");
            pCardObj.transform.SetParent(_profileContentRoot.transform, false);
            var pdcRect = pCardObj.AddComponent<RectTransform>();
            pdcRect.anchorMin = new Vector2(0f, 0.52f);
            pdcRect.anchorMax = new Vector2(1f, 1f);
            pdcRect.offsetMin = Vector2.zero;
            pdcRect.offsetMax = Vector2.zero;

            var pdcBg = pCardObj.AddComponent<Image>();
            pdcBg.color = UITheme.CardBackground;

            var pvtObj = new GameObject("ProfileTitle");
            pvtObj.transform.SetParent(pCardObj.transform, false);
            var pvtRect = pvtObj.AddComponent<RectTransform>();
            pvtRect.anchorMin = new Vector2(0.05f, 0.88f);
            pvtRect.anchorMax = new Vector2(0.95f, 0.98f);
            pvtRect.offsetMin = Vector2.zero;
            pvtRect.offsetMax = Vector2.zero;

            _profileViewTitleText = pvtObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _profileViewTitleText.font = defaultFont;
            _profileViewTitleText.text = "<b>WORKER PROFILE & DATA SYNC</b>";
            _profileViewTitleText.fontSize = 26;
            _profileViewTitleText.fontStyle = FontStyles.Bold;
            _profileViewTitleText.alignment = TextAlignmentOptions.Center;
            _profileViewTitleText.color = UITheme.TextPrimary;

            // Name Field
            var pvNameObj = new GameObject("NameField");
            pvNameObj.transform.SetParent(pCardObj.transform, false);
            var pvnRect = pvNameObj.AddComponent<RectTransform>();
            pvnRect.anchorMin = new Vector2(0.06f, 0.68f);
            pvnRect.anchorMax = new Vector2(0.94f, 0.84f);
            pvnRect.offsetMin = Vector2.zero;
            pvnRect.offsetMax = Vector2.zero;

            _pvNameLabel = pvNameObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvNameLabel.font = defaultFont;
            _pvNameLabel.text = "Worker Name";
            _pvNameLabel.fontSize = 17;
            _pvNameLabel.color = UITheme.TextSecondary;

            var pvNameValObj = new GameObject("Val");
            pvNameValObj.transform.SetParent(pvNameObj.transform, false);
            var pvnvRect = pvNameValObj.AddComponent<RectTransform>();
            pvnvRect.anchorMin = Vector2.zero;
            pvnvRect.anchorMax = new Vector2(1f, 0.55f);
            pvnvRect.offsetMin = Vector2.zero;
            pvnvRect.offsetMax = Vector2.zero;
            _pvNameVal = pvNameValObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvNameVal.font = defaultFont;
            _pvNameVal.text = $"<b>{_workerName}</b>";
            _pvNameVal.fontSize = 24;
            _pvNameVal.fontStyle = FontStyles.Bold;
            _pvNameVal.color = UITheme.TextPrimary;

            // Worker ID Field
            var pvIdObj = new GameObject("IdField");
            pvIdObj.transform.SetParent(pCardObj.transform, false);
            var pviRect = pvIdObj.AddComponent<RectTransform>();
            pviRect.anchorMin = new Vector2(0.06f, 0.48f);
            pviRect.anchorMax = new Vector2(0.94f, 0.64f);
            pviRect.offsetMin = Vector2.zero;
            pviRect.offsetMax = Vector2.zero;

            _pvIdLabel = pvIdObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvIdLabel.font = defaultFont;
            _pvIdLabel.text = "Worker ID";
            _pvIdLabel.fontSize = 17;
            _pvIdLabel.color = UITheme.TextSecondary;

            var pvIdValObj = new GameObject("Val");
            pvIdValObj.transform.SetParent(pvIdObj.transform, false);
            var pvivRect = pvIdValObj.AddComponent<RectTransform>();
            pvivRect.anchorMin = Vector2.zero;
            pvivRect.anchorMax = new Vector2(1f, 0.55f);
            pvivRect.offsetMin = Vector2.zero;
            pvivRect.offsetMax = Vector2.zero;
            _pvIdVal = pvIdValObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvIdVal.font = defaultFont;
            _pvIdVal.text = $"<color=#F97316>{_workerId}</color>";
            _pvIdVal.fontSize = 22;
            _pvIdVal.fontStyle = FontStyles.Bold;

            // Division Field
            var pvDivObj = new GameObject("DivField");
            pvDivObj.transform.SetParent(pCardObj.transform, false);
            var pvdRect = pvDivObj.AddComponent<RectTransform>();
            pvdRect.anchorMin = new Vector2(0.06f, 0.30f);
            pvdRect.anchorMax = new Vector2(0.94f, 0.44f);
            pvdRect.offsetMin = Vector2.zero;
            pvdRect.offsetMax = Vector2.zero;

            _pvDivLabel = pvDivObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvDivLabel.font = defaultFont;
            _pvDivLabel.text = "Division: Mining & Material Handling";
            _pvDivLabel.fontSize = 19;
            _pvDivLabel.color = UITheme.TextPrimary;

            // Language Field
            var pvLangObj = new GameObject("LangField");
            pvLangObj.transform.SetParent(pCardObj.transform, false);
            var pvlRect = pvLangObj.AddComponent<RectTransform>();
            pvlRect.anchorMin = new Vector2(0.06f, 0.12f);
            pvlRect.anchorMax = new Vector2(0.94f, 0.26f);
            pvlRect.offsetMin = Vector2.zero;
            pvlRect.offsetMax = Vector2.zero;

            _pvLangLabel = pvLangObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvLangLabel.font = defaultFont;
            _pvLangLabel.text = "Preferred Language: <b>English</b>";
            _pvLangLabel.fontSize = 18;
            _pvLangLabel.color = UITheme.TextSecondary;

            // Sync Status Card
            var syncCardObj = new GameObject("SyncStatusCard");
            syncCardObj.transform.SetParent(_profileContentRoot.transform, false);
            var scRect = syncCardObj.AddComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0f, 0.22f);
            scRect.anchorMax = new Vector2(1f, 0.50f);
            scRect.offsetMin = Vector2.zero;
            scRect.offsetMax = Vector2.zero;

            var scBg = syncCardObj.AddComponent<Image>();
            scBg.color = UITheme.CardBackground;

            var sctObj = new GameObject("SyncTitle");
            sctObj.transform.SetParent(syncCardObj.transform, false);
            var sctRect = sctObj.AddComponent<RectTransform>();
            sctRect.anchorMin = new Vector2(0.06f, 0.72f);
            sctRect.anchorMax = new Vector2(0.94f, 0.94f);
            sctRect.offsetMin = Vector2.zero;
            sctRect.offsetMax = Vector2.zero;
            var sct = sctObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) sct.font = defaultFont;
            sct.text = "<b>OFFLINE-FIRST DATA SYNC</b>";
            sct.fontSize = 20;
            sct.fontStyle = FontStyles.Bold;
            sct.color = UITheme.TextPrimary;

            var sccObj = new GameObject("SyncCounts");
            sccObj.transform.SetParent(syncCardObj.transform, false);
            var sccRect = sccObj.AddComponent<RectTransform>();
            sccRect.anchorMin = new Vector2(0.06f, 0.44f);
            sccRect.anchorMax = new Vector2(0.94f, 0.68f);
            sccRect.offsetMin = Vector2.zero;
            sccRect.offsetMax = Vector2.zero;
            _pvSyncCountText = sccObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _pvSyncCountText.font = defaultFont;
            _pvSyncCountText.text = "Total Attempts: 2  |  Pending Cloud Sync: 1";
            _pvSyncCountText.fontSize = 18;

            var syncBtnObj = new GameObject("SyncNowButton");
            syncBtnObj.transform.SetParent(syncCardObj.transform, false);
            var snbRect = syncBtnObj.AddComponent<RectTransform>();
            snbRect.anchorMin = new Vector2(0.06f, 0.08f);
            snbRect.anchorMax = new Vector2(0.94f, 0.38f);
            snbRect.offsetMin = Vector2.zero;
            snbRect.offsetMax = Vector2.zero;

            var snbImg = syncBtnObj.AddComponent<Image>();
            snbImg.color = UITheme.PrimaryOrange;
            _syncNowBtn = syncBtnObj.AddComponent<Button>();
            var snbTap = syncBtnObj.AddComponent<TapGatedButton>();
            snbTap.Initialize(() => TriggerManualSync());

            var snbtObj = new GameObject("Text");
            snbtObj.transform.SetParent(syncBtnObj.transform, false);
            var snbtRect = snbtObj.AddComponent<RectTransform>();
            snbtRect.anchorMin = Vector2.zero;
            snbtRect.anchorMax = Vector2.one;

            _syncNowBtnText = snbtObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _syncNowBtnText.font = defaultFont;
            _syncNowBtnText.text = "<b>SYNC NOW</b>";
            _syncNowBtnText.fontSize = 22;
            _syncNowBtnText.fontStyle = FontStyles.Bold;
            _syncNowBtnText.alignment = TextAlignmentOptions.Center;
            _syncNowBtnText.color = UITheme.TextLightOnDark;

            // Session Logout Button & Application Settings
            var logoutBtnObj = new GameObject("LogoutButton");
            logoutBtnObj.transform.SetParent(_profileContentRoot.transform, false);
            var lobRect = logoutBtnObj.AddComponent<RectTransform>();
            lobRect.anchorMin = new Vector2(0f, 0.11f);
            lobRect.anchorMax = new Vector2(1f, 0.20f);
            lobRect.offsetMin = Vector2.zero;
            lobRect.offsetMax = Vector2.zero;

            var lobImg = logoutBtnObj.AddComponent<Image>();
            lobImg.color = UITheme.DangerSurface;
            _logoutBtn = logoutBtnObj.AddComponent<Button>();
            var lobTap = logoutBtnObj.AddComponent<TapGatedButton>();
            lobTap.Initialize(() =>
            {
                WorkerSessionService.Instance.Logout();
                ShowLogin();
            });

            var lobtObj = new GameObject("Text");
            lobtObj.transform.SetParent(logoutBtnObj.transform, false);
            var lobtRect = lobtObj.AddComponent<RectTransform>();
            lobtRect.anchorMin = Vector2.zero;
            lobtRect.anchorMax = Vector2.one;

            _logoutBtnText = lobtObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _logoutBtnText.font = defaultFont;
            _logoutBtnText.text = "<b>LOGOUT (SWITCH WORKER)</b>";
            _logoutBtnText.fontSize = 20;
            _logoutBtnText.fontStyle = FontStyles.Bold;
            _logoutBtnText.alignment = TextAlignmentOptions.Center;
            _logoutBtnText.color = UITheme.DangerText;

            var pvSetBtnObj = new GameObject("ProfileSettingsBtn");
            pvSetBtnObj.transform.SetParent(_profileContentRoot.transform, false);
            var pvsRect = pvSetBtnObj.AddComponent<RectTransform>();
            pvsRect.anchorMin = new Vector2(0f, 0.010f);
            pvsRect.anchorMax = new Vector2(1f, 0.095f);
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
            _pvSettingsBtnText.fontStyle = FontStyles.Bold;
            _pvSettingsBtnText.alignment = TextAlignmentOptions.Center;
            _pvSettingsBtnText.color = UITheme.TextPrimary;

            _profileContentRoot.SetActive(false);

            // -------------------------------------------------------------
            // Offline Status Bar (y: 0.082 to 0.118)
            // -------------------------------------------------------------
            var offlineBarObj = new GameObject("OfflineStatusBar");
            offlineBarObj.transform.SetParent(_homeRoot.transform, false);
            var obRect = offlineBarObj.AddComponent<RectTransform>();
            obRect.anchorMin = new Vector2(0.04f, 0.082f);
            obRect.anchorMax = new Vector2(0.96f, 0.118f);
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
            _offlineBadgeText.fontSize = 17;
            _offlineBadgeText.fontStyle = FontStyles.Bold;
            _offlineBadgeText.alignment = TextAlignmentOptions.Center;
            _offlineBadgeText.color = UITheme.SuccessText;

            // -------------------------------------------------------------
            // Bottom Navigation Bar (5 TABS: HOME, AR, RECORDS, CERTIFICATES, PROFILE)
            // -------------------------------------------------------------
            _bottomNavBar = new GameObject("BottomNavBar");
            _bottomNavBar.transform.SetParent(_homeRoot.transform, false);
            var bnbRect = _bottomNavBar.AddComponent<RectTransform>();
            bnbRect.anchorMin = new Vector2(0f, 0.012f);
            bnbRect.anchorMax = new Vector2(1f, 0.078f);
            bnbRect.offsetMin = Vector2.zero;
            bnbRect.offsetMax = Vector2.zero;

            var bnbBg = _bottomNavBar.AddComponent<Image>();
            bnbBg.color = UITheme.CardBackground;

            // Tab 1: HOME (0.020 to 0.196)
            var navHomeObj = new GameObject("NavTab_Home");
            navHomeObj.transform.SetParent(_bottomNavBar.transform, false);
            var nhRect = navHomeObj.AddComponent<RectTransform>();
            nhRect.anchorMin = new Vector2(0.020f, 0.08f);
            nhRect.anchorMax = new Vector2(0.196f, 0.92f);
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
            _navHomeText.fontSize = 16;
            _navHomeText.fontStyle = FontStyles.Bold;
            _navHomeText.alignment = TextAlignmentOptions.Center;
            _navHomeText.color = UITheme.PrimaryOrange;

            // Tab 2: AR (0.216 to 0.392)
            var navArObj = new GameObject("NavTab_AR");
            navArObj.transform.SetParent(_bottomNavBar.transform, false);
            var naRect = navArObj.AddComponent<RectTransform>();
            naRect.anchorMin = new Vector2(0.216f, 0.08f);
            naRect.anchorMax = new Vector2(0.392f, 0.92f);
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
            _navArText.fontSize = 16;
            _navArText.fontStyle = FontStyles.Bold;
            _navArText.alignment = TextAlignmentOptions.Center;
            _navArText.color = UITheme.TextSecondary;

            // Tab 3: RECORDS (0.412 to 0.588)
            var navRecObj = new GameObject("NavTab_Records");
            navRecObj.transform.SetParent(_bottomNavBar.transform, false);
            var nrRect = navRecObj.AddComponent<RectTransform>();
            nrRect.anchorMin = new Vector2(0.412f, 0.08f);
            nrRect.anchorMax = new Vector2(0.588f, 0.92f);
            nrRect.offsetMin = Vector2.zero;
            nrRect.offsetMax = Vector2.zero;

            _navRecordsBg = navRecObj.AddComponent<Image>();
            _navRecordsBg.color = UITheme.CardSecondaryBg;
            _navRecordsBtn = navRecObj.AddComponent<Button>();
            var nrTap = navRecObj.AddComponent<TapGatedButton>();
            nrTap.Initialize(() => ShowRecords());

            var nrTextObj = new GameObject("Text");
            nrTextObj.transform.SetParent(navRecObj.transform, false);
            var nrtRect = nrTextObj.AddComponent<RectTransform>();
            nrtRect.anchorMin = Vector2.zero;
            nrtRect.anchorMax = Vector2.one;

            _navRecordsText = nrTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _navRecordsText.font = defaultFont;
            _navRecordsText.text = "<b>RECORDS</b>";
            _navRecordsText.fontSize = 15;
            _navRecordsText.fontStyle = FontStyles.Bold;
            _navRecordsText.alignment = TextAlignmentOptions.Center;
            _navRecordsText.color = UITheme.TextSecondary;

            // Tab 4: CERTIFICATES (0.608 to 0.784)
            var navCertObj = new GameObject("NavTab_Certificates");
            navCertObj.transform.SetParent(_bottomNavBar.transform, false);
            var ncRect = navCertObj.AddComponent<RectTransform>();
            ncRect.anchorMin = new Vector2(0.608f, 0.08f);
            ncRect.anchorMax = new Vector2(0.784f, 0.92f);
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
            _navCertText.text = "<b>CERTS</b>";
            _navCertText.fontSize = 15;
            _navCertText.fontStyle = FontStyles.Bold;
            _navCertText.alignment = TextAlignmentOptions.Center;
            _navCertText.color = UITheme.TextSecondary;

            // Tab 5: PROFILE (0.804 to 0.980)
            var navProfObj = new GameObject("NavTab_Profile");
            navProfObj.transform.SetParent(_bottomNavBar.transform, false);
            var npRect = navProfObj.AddComponent<RectTransform>();
            npRect.anchorMin = new Vector2(0.804f, 0.08f);
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
            _navProfText.fontSize = 15;
            _navProfText.fontStyle = FontStyles.Bold;
            _navProfText.alignment = TextAlignmentOptions.Center;
            _navProfText.color = UITheme.TextSecondary;

            // =============================================================
            // 3. SETTINGS PANEL MODAL OVERLAY
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
            var scCardRect = settingsCardObj.AddComponent<RectTransform>();
            scCardRect.anchorMin = new Vector2(0.05f, 0.23f);
            scCardRect.anchorMax = new Vector2(0.95f, 0.77f);
            scCardRect.offsetMin = Vector2.zero;
            scCardRect.offsetMax = Vector2.zero;

            var settingsCardBg = settingsCardObj.AddComponent<Image>();
            settingsCardBg.color = UITheme.CardBackground;

            // Settings Title
            var stObj = new GameObject("SettingsTitle");
            stObj.transform.SetParent(settingsCardObj.transform, false);
            var stRect = stObj.AddComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0.05f, 0.88f);
            stRect.anchorMax = new Vector2(0.84f, 0.96f);
            stRect.offsetMin = Vector2.zero;
            stRect.offsetMax = Vector2.zero;

            _settingsTitleText = stObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _settingsTitleText.font = defaultFont;
            _settingsTitleText.text = "<b>APPLICATION SETTINGS</b>";
            _settingsTitleText.fontSize = 26;
            _settingsTitleText.fontStyle = FontStyles.Bold;
            _settingsTitleText.alignment = TextAlignmentOptions.Center;
            _settingsTitleText.color = UITheme.TextPrimary;

            // Top-right Quick Close [X] Icon
            var topCloseObj = new GameObject("TopCloseButton");
            topCloseObj.transform.SetParent(settingsCardObj.transform, false);
            var tcRect = topCloseObj.AddComponent<RectTransform>();
            tcRect.anchorMin = new Vector2(0.85f, 0.88f);
            tcRect.anchorMax = new Vector2(0.95f, 0.96f);
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
            tcTmp.fontStyle = FontStyles.Bold;
            tcTmp.alignment = TextAlignmentOptions.Center;
            tcTmp.color = UITheme.TextSecondary;

            // Sound Effects Row
            var seObj = new GameObject("SoundEffectsRow");
            seObj.transform.SetParent(settingsCardObj.transform, false);
            var seRect = seObj.AddComponent<RectTransform>();
            seRect.anchorMin = new Vector2(0.06f, 0.74f);
            seRect.anchorMax = new Vector2(0.94f, 0.84f);
            seRect.offsetMin = Vector2.zero;
            seRect.offsetMax = Vector2.zero;

            var seLabelObj = new GameObject("Label");
            seLabelObj.transform.SetParent(seObj.transform, false);
            var selRect = seLabelObj.AddComponent<RectTransform>();
            selRect.anchorMin = Vector2.zero;
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

            _soundToggleBg = seBtnObj.AddComponent<Image>();
            _soundToggleBg.color = UITheme.CardSecondaryBg;
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
            _soundToggleButtonText.fontSize = 20;
            _soundToggleButtonText.fontStyle = FontStyles.Bold;
            _soundToggleButtonText.alignment = TextAlignmentOptions.Center;
            _soundToggleButtonText.color = UITheme.TextPrimary;

            // Emergency Alarm Row
            var eaObj = new GameObject("AlarmRow");
            eaObj.transform.SetParent(settingsCardObj.transform, false);
            var eaRect = eaObj.AddComponent<RectTransform>();
            eaRect.anchorMin = new Vector2(0.06f, 0.61f);
            eaRect.anchorMax = new Vector2(0.94f, 0.71f);
            eaRect.offsetMin = Vector2.zero;
            eaRect.offsetMax = Vector2.zero;

            var eaLabelObj = new GameObject("Label");
            eaLabelObj.transform.SetParent(eaObj.transform, false);
            var ealRect = eaLabelObj.AddComponent<RectTransform>();
            ealRect.anchorMin = Vector2.zero;
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

            _alarmToggleBg = eaBtnObj.AddComponent<Image>();
            _alarmToggleBg.color = UITheme.CardSecondaryBg;
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
            eabtRect.offsetMin = Vector2.zero;
            eabtRect.offsetMax = Vector2.zero;

            _alarmToggleButtonText = eabTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _alarmToggleButtonText.font = defaultFont;
            _alarmToggleButtonText.text = "<b>ON</b>";
            _alarmToggleButtonText.fontSize = 20;
            _alarmToggleButtonText.fontStyle = FontStyles.Bold;
            _alarmToggleButtonText.alignment = TextAlignmentOptions.Center;
            _alarmToggleButtonText.color = UITheme.TextPrimary;

            // Volume Section
            var volObj = new GameObject("VolumeRow");
            volObj.transform.SetParent(settingsCardObj.transform, false);
            var volRect = volObj.AddComponent<RectTransform>();
            volRect.anchorMin = new Vector2(0.06f, 0.46f);
            volRect.anchorMax = new Vector2(0.94f, 0.58f);
            volRect.offsetMin = Vector2.zero;
            volRect.offsetMax = Vector2.zero;

            var vlObj = new GameObject("Label");
            vlObj.transform.SetParent(volObj.transform, false);
            var vlRect = vlObj.AddComponent<RectTransform>();
            vlRect.anchorMin = new Vector2(0f, 0.52f);
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
            vvRect.anchorMin = new Vector2(0.62f, 0.52f);
            vvRect.anchorMax = new Vector2(1f, 1f);
            vvRect.offsetMin = Vector2.zero;
            vvRect.offsetMax = Vector2.zero;

            _volumeValueText = vvObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _volumeValueText.font = defaultFont;
            _volumeValueText.text = "100%";
            _volumeValueText.fontSize = 22;
            _volumeValueText.fontStyle = FontStyles.Bold;
            _volumeValueText.alignment = TextAlignmentOptions.Right;
            _volumeValueText.color = UITheme.PrimaryOrange;

            var vmBtnObj = new GameObject("VolMinusBtn");
            vmBtnObj.transform.SetParent(volObj.transform, false);
            var vmbRect = vmBtnObj.AddComponent<RectTransform>();
            vmbRect.anchorMin = Vector2.zero;
            vmbRect.anchorMax = new Vector2(0.46f, 0.48f);
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
            vmtrRect.offsetMin = Vector2.zero;
            vmtrRect.offsetMax = Vector2.zero;
            var vmtTmp = vmtObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) vmtTmp.font = defaultFont;
            vmtTmp.text = "<b>- 10%</b>";
            vmtTmp.fontSize = 20;
            vmtTmp.fontStyle = FontStyles.Bold;
            vmtTmp.alignment = TextAlignmentOptions.Center;
            vmtTmp.color = UITheme.TextPrimary;

            var vpBtnObj = new GameObject("VolPlusBtn");
            vpBtnObj.transform.SetParent(volObj.transform, false);
            var vpbRect = vpBtnObj.AddComponent<RectTransform>();
            vpbRect.anchorMin = new Vector2(0.54f, 0f);
            vpbRect.anchorMax = new Vector2(1f, 0.48f);
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
            vptrRect.offsetMin = Vector2.zero;
            vptrRect.offsetMax = Vector2.zero;
            var vptTmp = vptObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) vptTmp.font = defaultFont;
            vptTmp.text = "<b>+ 10%</b>";
            vptTmp.fontSize = 20;
            vptTmp.fontStyle = FontStyles.Bold;
            vptTmp.alignment = TextAlignmentOptions.Center;
            vptTmp.color = UITheme.TextPrimary;

            // Language Selection Section
            var langHeaderObj = new GameObject("LanguageHeader");
            langHeaderObj.transform.SetParent(settingsCardObj.transform, false);
            var lhRect = langHeaderObj.AddComponent<RectTransform>();
            lhRect.anchorMin = new Vector2(0.06f, 0.33f);
            lhRect.anchorMax = new Vector2(0.94f, 0.42f);
            lhRect.offsetMin = Vector2.zero;
            lhRect.offsetMax = Vector2.zero;

            _languageHeader = langHeaderObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _languageHeader.font = defaultFont;
            _languageHeader.text = "Language / भाषा / ᱯᱟᱹᱨᱥᱤ";
            _languageHeader.fontSize = 22;
            _languageHeader.fontStyle = FontStyles.Bold;
            _languageHeader.alignment = TextAlignmentOptions.Left;
            _languageHeader.color = UITheme.TextPrimary;

            var lEnObj = new GameObject("LangBtn_English");
            lEnObj.transform.SetParent(settingsCardObj.transform, false);
            var lenRect = lEnObj.AddComponent<RectTransform>();
            lenRect.anchorMin = new Vector2(0.06f, 0.19f);
            lenRect.anchorMax = new Vector2(0.33f, 0.31f);
            lenRect.offsetMin = Vector2.zero;
            lenRect.offsetMax = Vector2.zero;
            _btnLangEnglishBg = lEnObj.AddComponent<Image>();
            _btnLangEnglishBg.color = UITheme.CardSecondaryBg;
            _btnLangEnglish = lEnObj.AddComponent<Button>();
            var enTap = lEnObj.AddComponent<TapGatedButton>();
            enTap.Initialize(() => LocaleService.Instance.SetLanguage(LocaleService.LangEnglish));

            var lenTextObj = new GameObject("Text");
            lenTextObj.transform.SetParent(lEnObj.transform, false);
            var lentRect = lenTextObj.AddComponent<RectTransform>();
            lentRect.anchorMin = Vector2.zero;
            lentRect.anchorMax = Vector2.one;
            lentRect.offsetMin = Vector2.zero;
            lentRect.offsetMax = Vector2.zero;
            _btnLangEnglishText = lenTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _btnLangEnglishText.font = defaultFont;
            _btnLangEnglishText.text = "<b>English</b>";
            _btnLangEnglishText.fontSize = 20;
            _btnLangEnglishText.alignment = TextAlignmentOptions.Center;
            _btnLangEnglishText.color = UITheme.TextPrimary;

            var lHiObj = new GameObject("LangBtn_Hindi");
            lHiObj.transform.SetParent(settingsCardObj.transform, false);
            var lhiRect = lHiObj.AddComponent<RectTransform>();
            lhiRect.anchorMin = new Vector2(0.36f, 0.19f);
            lhiRect.anchorMax = new Vector2(0.63f, 0.31f);
            lhiRect.offsetMin = Vector2.zero;
            lhiRect.offsetMax = Vector2.zero;
            _btnLangHindiBg = lHiObj.AddComponent<Image>();
            _btnLangHindiBg.color = UITheme.CardSecondaryBg;
            _btnLangHindi = lHiObj.AddComponent<Button>();
            var hiTap = lHiObj.AddComponent<TapGatedButton>();
            hiTap.Initialize(() => LocaleService.Instance.SetLanguage(LocaleService.LangHindi));

            var lhiTextObj = new GameObject("Text");
            lhiTextObj.transform.SetParent(lHiObj.transform, false);
            var lhitRect = lhiTextObj.AddComponent<RectTransform>();
            lhitRect.anchorMin = Vector2.zero;
            lhitRect.anchorMax = Vector2.one;
            lhitRect.offsetMin = Vector2.zero;
            lhitRect.offsetMax = Vector2.zero;
            _btnLangHindiText = lhiTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _btnLangHindiText.font = defaultFont;
            _btnLangHindiText.text = "<b>हिन्दी</b>";
            _btnLangHindiText.fontSize = 20;
            _btnLangHindiText.alignment = TextAlignmentOptions.Center;
            _btnLangHindiText.color = UITheme.TextPrimary;

            var lSatObj = new GameObject("LangBtn_Santali");
            lSatObj.transform.SetParent(settingsCardObj.transform, false);
            var lsatRect = lSatObj.AddComponent<RectTransform>();
            lsatRect.anchorMin = new Vector2(0.66f, 0.19f);
            lsatRect.anchorMax = new Vector2(0.94f, 0.31f);
            lsatRect.offsetMin = Vector2.zero;
            lsatRect.offsetMax = Vector2.zero;
            _btnLangSantaliBg = lSatObj.AddComponent<Image>();
            _btnLangSantaliBg.color = UITheme.CardSecondaryBg;
            _btnLangSantali = lSatObj.AddComponent<Button>();
            var satTap = lSatObj.AddComponent<TapGatedButton>();
            satTap.Initialize(() => LocaleService.Instance.SetLanguage(LocaleService.LangSantali));

            var lsatTextObj = new GameObject("Text");
            lsatTextObj.transform.SetParent(lSatObj.transform, false);
            var lsattRect = lsatTextObj.AddComponent<RectTransform>();
            lsattRect.anchorMin = Vector2.zero;
            lsattRect.anchorMax = Vector2.one;
            lsattRect.offsetMin = Vector2.zero;
            lsattRect.offsetMax = Vector2.zero;
            _btnLangSantaliText = lsatTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _btnLangSantaliText.font = defaultFont;
            _btnLangSantaliText.text = "<b>ᱥᱟᱱᱛᱟᱲᱤ</b>";
            _btnLangSantaliText.fontSize = 20;
            _btnLangSantaliText.alignment = TextAlignmentOptions.Center;
            _btnLangSantaliText.color = UITheme.TextPrimary;

            var closeBtnObj = new GameObject("CloseSettingsButton");
            closeBtnObj.transform.SetParent(settingsCardObj.transform, false);
            var cbRect = closeBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.12f, 0.05f);
            cbRect.anchorMax = new Vector2(0.88f, 0.15f);
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
            cbtRect.offsetMin = Vector2.zero;
            cbtRect.offsetMax = Vector2.zero;

            _settingsCloseButtonText = cbTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _settingsCloseButtonText.font = defaultFont;
            _settingsCloseButtonText.text = "<b>CLOSE [X]</b>";
            _settingsCloseButtonText.fontSize = 24;
            _settingsCloseButtonText.fontStyle = FontStyles.Bold;
            _settingsCloseButtonText.alignment = TextAlignmentOptions.Center;
            _settingsCloseButtonText.color = UITheme.TextLightOnDark;

            _settingsRoot.SetActive(false);

            // =============================================================
            // 4. RECORD DETAIL MODAL OVERLAY
            // =============================================================
            _recordDetailModalRoot = new GameObject("RecordDetailModal");
            _recordDetailModalRoot.transform.SetParent(_rootCanvas.transform, false);
            var rdmRect = _recordDetailModalRoot.AddComponent<RectTransform>();
            rdmRect.anchorMin = Vector2.zero;
            rdmRect.anchorMax = Vector2.one;
            rdmRect.offsetMin = Vector2.zero;
            rdmRect.offsetMax = Vector2.zero;

            var rdmBg = _recordDetailModalRoot.AddComponent<Image>();
            rdmBg.color = new Color(0.05f, 0.08f, 0.12f, 0.75f);
            rdmBg.raycastTarget = true;

            var rdmCard = new GameObject("Card");
            rdmCard.transform.SetParent(_recordDetailModalRoot.transform, false);
            var rdcRect = rdmCard.AddComponent<RectTransform>();
            rdcRect.anchorMin = new Vector2(0.05f, 0.15f);
            rdcRect.anchorMax = new Vector2(0.95f, 0.85f);
            rdcRect.offsetMin = Vector2.zero;
            rdcRect.offsetMax = Vector2.zero;
            var rdcImg = rdmCard.AddComponent<Image>();
            rdcImg.color = UITheme.CardBackground;

            var rdtObj = new GameObject("Title");
            rdtObj.transform.SetParent(rdmCard.transform, false);
            var rdtr = rdtObj.AddComponent<RectTransform>();
            rdtr.anchorMin = new Vector2(0.06f, 0.88f);
            rdtr.anchorMax = new Vector2(0.94f, 0.98f);
            rdtr.offsetMin = Vector2.zero;
            rdtr.offsetMax = Vector2.zero;
            _rdTitleText = rdtObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _rdTitleText.font = defaultFont;
            _rdTitleText.text = "<b>Attempt Score Breakdown</b>";
            _rdTitleText.fontSize = 24;
            _rdTitleText.fontStyle = FontStyles.Bold;
            _rdTitleText.color = UITheme.TextPrimary;

            var rdsObj = new GameObject("Subtitle");
            rdsObj.transform.SetParent(rdmCard.transform, false);
            var rdsr = rdsObj.AddComponent<RectTransform>();
            rdsr.anchorMin = new Vector2(0.06f, 0.82f);
            rdsr.anchorMax = new Vector2(0.94f, 0.88f);
            rdsr.offsetMin = Vector2.zero;
            rdsr.offsetMax = Vector2.zero;
            _rdSubtitleText = rdsObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _rdSubtitleText.font = defaultFont;
            _rdSubtitleText.fontSize = 17;
            _rdSubtitleText.color = UITheme.TextSecondary;

            // Score Banner Box
            var sbox = new GameObject("ScoreBox");
            sbox.transform.SetParent(rdmCard.transform, false);
            var sbr = sbox.AddComponent<RectTransform>();
            sbr.anchorMin = new Vector2(0.06f, 0.68f);
            sbr.anchorMax = new Vector2(0.94f, 0.80f);
            sbr.offsetMin = Vector2.zero;
            sbr.offsetMax = Vector2.zero;
            _rdStatusBadgeBg = sbox.AddComponent<Image>();
            _rdStatusBadgeBg.color = UITheme.SuccessSurface;

            var sbText = new GameObject("Score");
            sbText.transform.SetParent(sbox.transform, false);
            var sbrScoreRect = sbText.AddComponent<RectTransform>();
            sbrScoreRect.anchorMin = new Vector2(0.04f, 0f);
            sbrScoreRect.anchorMax = new Vector2(0.40f, 1f);
            sbrScoreRect.offsetMin = Vector2.zero;
            sbrScoreRect.offsetMax = Vector2.zero;
            _rdScoreText = sbText.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _rdScoreText.font = defaultFont;
            _rdScoreText.fontSize = 32;
            _rdScoreText.fontStyle = FontStyles.Bold;
            _rdScoreText.color = UITheme.SuccessText;

            var sbtText = new GameObject("Badge");
            sbtText.transform.SetParent(sbox.transform, false);
            var sbtr2 = sbtText.AddComponent<RectTransform>();
            sbtr2.anchorMin = new Vector2(0.42f, 0f);
            sbtr2.anchorMax = new Vector2(0.96f, 1f);
            sbtr2.offsetMin = Vector2.zero;
            sbtr2.offsetMax = Vector2.zero;
            _rdStatusBadgeText = sbtText.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _rdStatusBadgeText.font = defaultFont;
            _rdStatusBadgeText.fontSize = 22;
            _rdStatusBadgeText.fontStyle = FontStyles.Bold;
            _rdStatusBadgeText.alignment = TextAlignmentOptions.Right;
            _rdStatusBadgeText.color = UITheme.SuccessText;

            // Steps text body
            var stepsObj = new GameObject("StepsBody");
            stepsObj.transform.SetParent(rdmCard.transform, false);
            var stepr = stepsObj.AddComponent<RectTransform>();
            stepr.anchorMin = new Vector2(0.06f, 0.22f);
            stepr.anchorMax = new Vector2(0.94f, 0.66f);
            stepr.offsetMin = Vector2.zero;
            stepr.offsetMax = Vector2.zero;
            _rdStepsBodyText = stepsObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _rdStepsBodyText.font = defaultFont;
            _rdStepsBodyText.fontSize = 18;
            _rdStepsBodyText.lineSpacing = 10;
            _rdStepsBodyText.color = UITheme.TextPrimary;

            // Meta text
            var metaObj = new GameObject("Meta");
            metaObj.transform.SetParent(rdmCard.transform, false);
            var mr = metaObj.AddComponent<RectTransform>();
            mr.anchorMin = new Vector2(0.06f, 0.13f);
            mr.anchorMax = new Vector2(0.94f, 0.20f);
            mr.offsetMin = Vector2.zero;
            mr.offsetMax = Vector2.zero;
            _rdMetaText = metaObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _rdMetaText.font = defaultFont;
            _rdMetaText.fontSize = 16;
            _rdMetaText.color = UITheme.TextSecondary;

            // Close button
            var rdcCloseBtnObj = new GameObject("CloseBtn");
            rdcCloseBtnObj.transform.SetParent(rdmCard.transform, false);
            var rdcbr = rdcCloseBtnObj.AddComponent<RectTransform>();
            rdcbr.anchorMin = new Vector2(0.15f, 0.03f);
            rdcbr.anchorMax = new Vector2(0.85f, 0.11f);
            rdcbr.offsetMin = Vector2.zero;
            rdcbr.offsetMax = Vector2.zero;
            rdcCloseBtnObj.AddComponent<Image>().color = UITheme.PrimaryOrange;
            var rdcBtn = rdcCloseBtnObj.AddComponent<Button>();
            var rdcTap = rdcCloseBtnObj.AddComponent<TapGatedButton>();
            rdcTap.Initialize(() => CloseRecordDetail());

            var rdct = new GameObject("Text");
            rdct.transform.SetParent(rdcCloseBtnObj.transform, false);
            var rdctr = rdct.AddComponent<RectTransform>();
            rdctr.anchorMin = Vector2.zero;
            rdctr.anchorMax = Vector2.one;
            rdctr.offsetMin = Vector2.zero;
            rdctr.offsetMax = Vector2.zero;
            var rdctTmp = rdct.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) rdctTmp.font = defaultFont;
            rdctTmp.text = "<b>CLOSE [X]</b>";
            rdctTmp.fontSize = 22;
            rdctTmp.fontStyle = FontStyles.Bold;
            rdctTmp.alignment = TextAlignmentOptions.Center;
            rdctTmp.color = UITheme.TextLightOnDark;

            _recordDetailModalRoot.SetActive(false);

            // =============================================================
            // 5. QR VERIFICATION LINK MODAL OVERLAY
            // =============================================================
            _qrModalRoot = new GameObject("QrVerificationModal");
            _qrModalRoot.transform.SetParent(_rootCanvas.transform, false);
            var qrmRect = _qrModalRoot.AddComponent<RectTransform>();
            qrmRect.anchorMin = Vector2.zero;
            qrmRect.anchorMax = Vector2.one;
            qrmRect.offsetMin = Vector2.zero;
            qrmRect.offsetMax = Vector2.zero;

            var qrmBg = _qrModalRoot.AddComponent<Image>();
            qrmBg.color = new Color(0.05f, 0.08f, 0.12f, 0.75f);
            qrmBg.raycastTarget = true;

            var qrmCard = new GameObject("Card");
            qrmCard.transform.SetParent(_qrModalRoot.transform, false);
            var qrmcRect = qrmCard.AddComponent<RectTransform>();
            qrmcRect.anchorMin = new Vector2(0.06f, 0.28f);
            qrmcRect.anchorMax = new Vector2(0.94f, 0.72f);
            qrmcRect.offsetMin = Vector2.zero;
            qrmcRect.offsetMax = Vector2.zero;
            var qrmcImg = qrmCard.AddComponent<Image>();
            qrmcImg.color = UITheme.CardBackground;

            var qrtObj = new GameObject("Title");
            qrtObj.transform.SetParent(qrmCard.transform, false);
            var qrtr = qrtObj.AddComponent<RectTransform>();
            qrtr.anchorMin = new Vector2(0.06f, 0.84f);
            qrtr.anchorMax = new Vector2(0.94f, 0.96f);
            qrtr.offsetMin = Vector2.zero;
            qrtr.offsetMax = Vector2.zero;
            _qrTitleText = qrtObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _qrTitleText.font = defaultFont;
            _qrTitleText.text = "<b>OFFICIAL VERIFICATION PORTAL</b>";
            _qrTitleText.fontSize = 24;
            _qrTitleText.fontStyle = FontStyles.Bold;
            _qrTitleText.alignment = TextAlignmentOptions.Center;
            _qrTitleText.color = UITheme.TextPrimary;

            var qrsObj = new GameObject("Sub");
            qrsObj.transform.SetParent(qrmCard.transform, false);
            var qrsr = qrsObj.AddComponent<RectTransform>();
            qrsr.anchorMin = new Vector2(0.06f, 0.72f);
            qrsr.anchorMax = new Vector2(0.94f, 0.82f);
            qrsr.offsetMin = Vector2.zero;
            qrsr.offsetMax = Vector2.zero;
            _qrSubText = qrsObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _qrSubText.font = defaultFont;
            _qrSubText.fontSize = 18;
            _qrSubText.alignment = TextAlignmentOptions.Center;
            _qrSubText.color = UITheme.TextSecondary;

            // URL Box
            var urlBox = new GameObject("UrlBox");
            urlBox.transform.SetParent(qrmCard.transform, false);
            var urlr = urlBox.AddComponent<RectTransform>();
            urlr.anchorMin = new Vector2(0.06f, 0.44f);
            urlr.anchorMax = new Vector2(0.94f, 0.68f);
            urlr.offsetMin = Vector2.zero;
            urlr.offsetMax = Vector2.zero;
            var urlImg = urlBox.AddComponent<Image>();
            urlImg.color = UITheme.CardSecondaryBg;

            var urlt = new GameObject("Text");
            urlt.transform.SetParent(urlBox.transform, false);
            var urltr = urlt.AddComponent<RectTransform>();
            urltr.anchorMin = new Vector2(0.04f, 0.04f);
            urltr.anchorMax = new Vector2(0.96f, 0.96f);
            urltr.offsetMin = Vector2.zero;
            urltr.offsetMax = Vector2.zero;
            _qrUrlText = urlt.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _qrUrlText.font = defaultFont;
            _qrUrlText.fontSize = 17;
            _qrUrlText.alignment = TextAlignmentOptions.Center;
            _qrUrlText.color = UITheme.PrimaryOrange;

            var qriObj = new GameObject("Instructions");
            qriObj.transform.SetParent(qrmCard.transform, false);
            var qrir = qriObj.AddComponent<RectTransform>();
            qrir.anchorMin = new Vector2(0.06f, 0.22f);
            qrir.anchorMax = new Vector2(0.94f, 0.40f);
            qrir.offsetMin = Vector2.zero;
            qrir.offsetMax = Vector2.zero;
            _qrInstructionsText = qriObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _qrInstructionsText.font = defaultFont;
            _qrInstructionsText.fontSize = 16;
            _qrInstructionsText.alignment = TextAlignmentOptions.Center;
            _qrInstructionsText.color = UITheme.TextSecondary;

            var qrCloseBtnObj = new GameObject("CloseBtn");
            qrCloseBtnObj.transform.SetParent(qrmCard.transform, false);
            var qrcr = qrCloseBtnObj.AddComponent<RectTransform>();
            qrcr.anchorMin = new Vector2(0.15f, 0.05f);
            qrcr.anchorMax = new Vector2(0.85f, 0.18f);
            qrcr.offsetMin = Vector2.zero;
            qrcr.offsetMax = Vector2.zero;
            qrCloseBtnObj.AddComponent<Image>().color = UITheme.PrimaryOrange;
            var qrcBtn = qrCloseBtnObj.AddComponent<Button>();
            var qrcTap = qrCloseBtnObj.AddComponent<TapGatedButton>();
            qrcTap.Initialize(() => CloseQrModal());

            var qrct = new GameObject("Text");
            qrct.transform.SetParent(qrCloseBtnObj.transform, false);
            var qrctr = qrct.AddComponent<RectTransform>();
            qrctr.anchorMin = Vector2.zero;
            qrctr.anchorMax = Vector2.one;
            qrctr.offsetMin = Vector2.zero;
            qrctr.offsetMax = Vector2.zero;
            var qrctTmp = qrct.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) qrctTmp.font = defaultFont;
            qrctTmp.text = "<b>CLOSE [X]</b>";
            qrctTmp.fontSize = 22;
            qrctTmp.fontStyle = FontStyles.Bold;
            qrctTmp.alignment = TextAlignmentOptions.Center;
            qrctTmp.color = UITheme.TextLightOnDark;

            _qrModalRoot.SetActive(false);

            // =============================================================
            // 6. LOGIN SCREEN OVERLAY (AUTHENTICATION SYSTEM)
            // =============================================================
            _loginRoot = new GameObject("LoginScreenModal");
            _loginRoot.transform.SetParent(_rootCanvas.transform, false);
            var lmRect = _loginRoot.AddComponent<RectTransform>();
            lmRect.anchorMin = Vector2.zero;
            lmRect.anchorMax = Vector2.one;
            lmRect.offsetMin = Vector2.zero;
            lmRect.offsetMax = Vector2.zero;

            var lmBg = _loginRoot.AddComponent<Image>();
            lmBg.color = UITheme.ScreenBackground;

            // Brand Header
            var lhbObj = new GameObject("LoginHeaderBrand");
            lhbObj.transform.SetParent(_loginRoot.transform, false);
            var lhbr = lhbObj.AddComponent<RectTransform>();
            lhbr.anchorMin = new Vector2(0.06f, 0.88f);
            lhbr.anchorMax = new Vector2(0.94f, 0.97f);
            lhbr.offsetMin = Vector2.zero;
            lhbr.offsetMax = Vector2.zero;
            _loginAppTitleText = lhbObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _loginAppTitleText.font = defaultFont;
            _loginAppTitleText.text = "<b>INDUSTRIAL SAFETY AR</b>";
            _loginAppTitleText.fontSize = 32;
            _loginAppTitleText.fontStyle = FontStyles.Bold;
            _loginAppTitleText.alignment = TextAlignmentOptions.Center;
            _loginAppTitleText.color = UITheme.PrimaryOrange;

            // Card Container
            var lCard = new GameObject("LoginCard");
            lCard.transform.SetParent(_loginRoot.transform, false);
            var lcRect = lCard.AddComponent<RectTransform>();
            lcRect.anchorMin = new Vector2(0.06f, 0.12f);
            lcRect.anchorMax = new Vector2(0.94f, 0.86f);
            lcRect.offsetMin = Vector2.zero;
            lcRect.offsetMax = Vector2.zero;
            var lcImg = lCard.AddComponent<Image>();
            lcImg.color = UITheme.CardBackground;

            // Card Title
            var lctObj = new GameObject("CardTitle");
            lctObj.transform.SetParent(lCard.transform, false);
            var lctr = lctObj.AddComponent<RectTransform>();
            lctr.anchorMin = new Vector2(0.06f, 0.88f);
            lctr.anchorMax = new Vector2(0.94f, 0.98f);
            lctr.offsetMin = Vector2.zero;
            lctr.offsetMax = Vector2.zero;
            _loginCardTitleText = lctObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _loginCardTitleText.font = defaultFont;
            _loginCardTitleText.text = "<b>WORKER AUTHENTICATION</b>";
            _loginCardTitleText.fontSize = 26;
            _loginCardTitleText.fontStyle = FontStyles.Bold;
            _loginCardTitleText.alignment = TextAlignmentOptions.Center;
            _loginCardTitleText.color = UITheme.TextPrimary;

            // Worker ID Box Display
            var widBox = new GameObject("WorkerIdBox");
            widBox.transform.SetParent(lCard.transform, false);
            var widr = widBox.AddComponent<RectTransform>();
            widr.anchorMin = new Vector2(0.08f, 0.74f);
            widr.anchorMax = new Vector2(0.92f, 0.85f);
            widr.offsetMin = Vector2.zero;
            widr.offsetMax = Vector2.zero;
            widBox.AddComponent<Image>().color = UITheme.CardSecondaryBg;

            var widText = new GameObject("Text");
            widText.transform.SetParent(widBox.transform, false);
            var widtr = widText.AddComponent<RectTransform>();
            widtr.anchorMin = new Vector2(0.04f, 0f);
            widtr.anchorMax = new Vector2(0.96f, 1f);
            widtr.offsetMin = Vector2.zero;
            widtr.offsetMax = Vector2.zero;
            _loginIdDisplay = widText.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _loginIdDisplay.font = defaultFont;
            _loginIdDisplay.text = "Worker ID: <b>DEMO-001</b>";
            _loginIdDisplay.fontSize = 20;
            _loginIdDisplay.alignment = TextAlignmentOptions.MidlineLeft;
            _loginIdDisplay.color = UITheme.TextPrimary;

            // PIN Box Display
            var pinBox = new GameObject("PinBox");
            pinBox.transform.SetParent(lCard.transform, false);
            var pinr = pinBox.AddComponent<RectTransform>();
            pinr.anchorMin = new Vector2(0.08f, 0.60f);
            pinr.anchorMax = new Vector2(0.92f, 0.71f);
            pinr.offsetMin = Vector2.zero;
            pinr.offsetMax = Vector2.zero;
            pinBox.AddComponent<Image>().color = UITheme.CardSecondaryBg;

            var pinText = new GameObject("Text");
            pinText.transform.SetParent(pinBox.transform, false);
            var pintr = pinText.AddComponent<RectTransform>();
            pintr.anchorMin = new Vector2(0.04f, 0f);
            pintr.anchorMax = new Vector2(0.96f, 1f);
            pintr.offsetMin = Vector2.zero;
            pintr.offsetMax = Vector2.zero;
            _loginPinDisplay = pinText.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _loginPinDisplay.font = defaultFont;
            _loginPinDisplay.text = "PIN: <b>1234</b> (Default Demo)";
            _loginPinDisplay.fontSize = 20;
            _loginPinDisplay.alignment = TextAlignmentOptions.MidlineLeft;
            _loginPinDisplay.color = UITheme.TextPrimary;

            // Status message
            var statObj = new GameObject("StatusMsg");
            statObj.transform.SetParent(lCard.transform, false);
            var statr = statObj.AddComponent<RectTransform>();
            statr.anchorMin = new Vector2(0.08f, 0.50f);
            statr.anchorMax = new Vector2(0.92f, 0.58f);
            statr.offsetMin = Vector2.zero;
            statr.offsetMax = Vector2.zero;
            _loginStatusText = statObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) _loginStatusText.font = defaultFont;
            _loginStatusText.fontSize = 17;
            _loginStatusText.alignment = TextAlignmentOptions.Center;
            _loginStatusText.color = UITheme.TextSecondary;

            // Button 1: Online Login
            var onlBtnObj = new GameObject("OnlineLoginBtn");
            onlBtnObj.transform.SetParent(lCard.transform, false);
            var onlr = onlBtnObj.AddComponent<RectTransform>();
            onlr.anchorMin = new Vector2(0.08f, 0.36f);
            onlr.anchorMax = new Vector2(0.92f, 0.48f);
            onlr.offsetMin = Vector2.zero;
            onlr.offsetMax = Vector2.zero;
            onlBtnObj.AddComponent<Image>().color = UITheme.PrimaryOrange;
            var onlBtn = onlBtnObj.AddComponent<Button>();
            var onlTap = onlBtnObj.AddComponent<TapGatedButton>();
            onlTap.Initialize(() => PerformOnlineLogin());

            var onlt = new GameObject("Text");
            onlt.transform.SetParent(onlBtnObj.transform, false);
            var onltr = onlt.AddComponent<RectTransform>();
            onltr.anchorMin = Vector2.zero;
            onltr.anchorMax = Vector2.one;
            onltr.offsetMin = Vector2.zero;
            onltr.offsetMax = Vector2.zero;
            var onltTmp = onlt.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) onltTmp.font = defaultFont;
            onltTmp.text = "<b>ONLINE LOGIN (FASTIFY API)</b>";
            onltTmp.fontSize = 22;
            onltTmp.fontStyle = FontStyles.Bold;
            onltTmp.alignment = TextAlignmentOptions.Center;
            onltTmp.color = UITheme.TextLightOnDark;

            // Button 2: Offline Login
            var offBtnObj = new GameObject("OfflineLoginBtn");
            offBtnObj.transform.SetParent(lCard.transform, false);
            var offr = offBtnObj.AddComponent<RectTransform>();
            offr.anchorMin = new Vector2(0.08f, 0.22f);
            offr.anchorMax = new Vector2(0.92f, 0.34f);
            offr.offsetMin = Vector2.zero;
            offr.offsetMax = Vector2.zero;
            offBtnObj.AddComponent<Image>().color = UITheme.SuccessSurface;
            var offBtn = offBtnObj.AddComponent<Button>();
            var offTap = offBtnObj.AddComponent<TapGatedButton>();
            offTap.Initialize(() => PerformOfflineLogin());

            var offt = new GameObject("Text");
            offt.transform.SetParent(offBtnObj.transform, false);
            var offtr = offt.AddComponent<RectTransform>();
            offtr.anchorMin = Vector2.zero;
            offtr.anchorMax = Vector2.one;
            offtr.offsetMin = Vector2.zero;
            offtr.offsetMax = Vector2.zero;
            var offtTmp = offt.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) offtTmp.font = defaultFont;
            offtTmp.text = "<b>OFFLINE LOGIN (SALTED PIN)</b>";
            offtTmp.fontSize = 22;
            offtTmp.fontStyle = FontStyles.Bold;
            offtTmp.alignment = TextAlignmentOptions.Center;
            offtTmp.color = UITheme.SuccessText;

            // Button 3: Cancel / Return as Ramesh
            var retBtnObj = new GameObject("CancelBtn");
            retBtnObj.transform.SetParent(lCard.transform, false);
            var retr = retBtnObj.AddComponent<RectTransform>();
            retr.anchorMin = new Vector2(0.08f, 0.08f);
            retr.anchorMax = new Vector2(0.92f, 0.19f);
            retr.offsetMin = Vector2.zero;
            retr.offsetMax = Vector2.zero;
            retBtnObj.AddComponent<Image>().color = UITheme.CardSecondaryBg;
            var retBtn = retBtnObj.AddComponent<Button>();
            var retTap = retBtnObj.AddComponent<TapGatedButton>();
            retTap.Initialize(() => HideLogin());

            var rett = new GameObject("Text");
            rett.transform.SetParent(retBtnObj.transform, false);
            var rettr = rett.AddComponent<RectTransform>();
            rettr.anchorMin = Vector2.zero;
            rettr.anchorMax = Vector2.one;
            rettr.offsetMin = Vector2.zero;
            rettr.offsetMax = Vector2.zero;
            var rettTmp = rett.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) rettTmp.font = defaultFont;
            rettTmp.text = "<b><- CONTINUE AS OPERATOR RAMESH</b>";
            rettTmp.fontSize = 20;
            rettTmp.fontStyle = FontStyles.Bold;
            rettTmp.alignment = TextAlignmentOptions.Center;
            rettTmp.color = UITheme.TextPrimary;

            _loginRoot.SetActive(false);

            // Initial view state: Show Home
            ShowHome();
        }

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
