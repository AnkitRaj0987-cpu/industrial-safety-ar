// LocaleService.cs
// Namespace : IndustrialSafetyAR.Core
//
// Centralized localization service for the Worker AR Training App.
// Supports English (en), Hindi (hi), and Santali (sat) in Ol Chiki script.
// Persists selection locally via PlayerPrefs ("WorkerApp_Language").
// Provides dictionary lookup with fallback, and notifies UI on change.
//
// NOTE: Safety-critical Santali translations in Ol Chiki script require native-speaker review
// for formal industrial deployment.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndustrialSafetyAR.Core
{
    public sealed class LocaleService
    {
        public const string LangEnglish = "en";
        public const string LangHindi = "hi";
        public const string LangSantali = "sat";

        public const string PrefLanguageKey = "WorkerApp_Language";

        private static LocaleService s_Instance;
        private string _currentLanguage = LangEnglish;

        public event Action<string> OnLanguageChanged;

        public static LocaleService Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new LocaleService();
                    s_Instance.LoadPersistedLanguage();
                }
                EnsureFallbackFonts();
                return s_Instance;
            }
        }

        public string CurrentLanguage => _currentLanguage;

        public string CurrentLanguageDisplayName
        {
            get
            {
                switch (_currentLanguage)
                {
                    case LangHindi: return "हिन्दी (Hindi)";
                    case LangSantali: return "ᱥᱟᱱᱛᱟᱲᱤ (Santali)";
                    default: return "English";
                }
            }
        }

        public readonly IReadOnlyList<string> SupportedLanguages = new[]
        {
            LangEnglish,
            LangHindi,
            LangSantali
        };

        private readonly Dictionary<string, Dictionary<string, string>> _catalog =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public LocaleService()
        {
            PopulateCatalog();
            EnsureFallbackFonts();
        }

        /// <summary>
        /// Ensures TextMeshPro default font asset has Noto Sans Devanagari and Noto Sans Ol Chiki
        /// registered in its runtime fallback table so Devanagari and Ol Chiki glyphs render cleanly.
        /// </summary>
        public static void EnsureFallbackFonts()
        {
            try
            {
                var liberation = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
                    ?? TMPro.TMP_Settings.defaultFontAsset;
                if (liberation == null) return;

                if (liberation.fallbackFontAssetTable == null)
                {
                    liberation.fallbackFontAssetTable = new List<TMPro.TMP_FontAsset>();
                }

                var deva = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/NotoSansDevanagari SDF");
                var olChiki = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/NotoSansOlChiki SDF");

                if (deva != null && !liberation.fallbackFontAssetTable.Contains(deva))
                {
                    liberation.fallbackFontAssetTable.Add(deva);
                }
                if (olChiki != null && !liberation.fallbackFontAssetTable.Contains(olChiki))
                {
                    liberation.fallbackFontAssetTable.Add(olChiki);
                }

                if (TMPro.TMP_Settings.fallbackFontAssets != null)
                {
                    if (deva != null && !TMPro.TMP_Settings.fallbackFontAssets.Contains(deva))
                        TMPro.TMP_Settings.fallbackFontAssets.Add(deva);
                    if (olChiki != null && !TMPro.TMP_Settings.fallbackFontAssets.Contains(olChiki))
                        TMPro.TMP_Settings.fallbackFontAssets.Add(olChiki);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocaleService] EnsureFallbackFonts warning: {ex.Message}");
            }
        }

        public void SetLanguage(string langCode)
        {
            if (string.IsNullOrEmpty(langCode)) return;

            string normalized = langCode.Trim().ToLowerInvariant();
            if (normalized != LangEnglish && normalized != LangHindi && normalized != LangSantali)
            {
                normalized = LangEnglish;
            }

            if (_currentLanguage != normalized)
            {
                _currentLanguage = normalized;
                PlayerPrefs.SetString(PrefLanguageKey, _currentLanguage);
                PlayerPrefs.Save();
                EnsureFallbackFonts();
                OnLanguageChanged?.Invoke(_currentLanguage);
            }
        }

        public void LoadPersistedLanguage()
        {
            string saved = PlayerPrefs.GetString(PrefLanguageKey, LangEnglish);
            SetLanguage(saved);
        }

        public string Get(string key, string fallback = null)
        {
            if (string.IsNullOrEmpty(key)) return fallback ?? string.Empty;

            if (_catalog.TryGetValue(_currentLanguage, out var dict) && dict.TryGetValue(key, out var val))
            {
                return val;
            }

            // Fallback to English
            if (_currentLanguage != LangEnglish &&
                _catalog.TryGetValue(LangEnglish, out var enDict) &&
                enDict.TryGetValue(key, out var enVal))
            {
                return enVal;
            }

            return fallback ?? key;
        }

        public bool HasKey(string langCode, string key)
        {
            if (string.IsNullOrEmpty(langCode) || string.IsNullOrEmpty(key)) return false;
            return _catalog.TryGetValue(langCode, out var dict) && dict.ContainsKey(key);
        }

        public IReadOnlyDictionary<string, string> GetCatalogForLanguage(string langCode)
        {
            if (_catalog.TryGetValue(langCode, out var dict))
            {
                return dict;
            }
            return null;
        }

        private void PopulateCatalog()
        {
            // =============================================================
            // 1. English (en)
            // =============================================================
            var en = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // App Branding & Navigation
                { "app_title", "Industrial Safety AR" },
                { "app_subtitle", "Vocational Training Simulator • Jharkhand Industry" },
                { "btn_back", "← BACK" },
                { "btn_close", "CLOSE [X]" },
                { "btn_next", "NEXT STEP →" },
                { "btn_complete", "COMPLETE TRAINING" },
                { "btn_retake", "RETAKE" },
                { "btn_return_home", "RETURN TO HOME" },
                { "state_on", "ON" },
                { "state_off", "OFF" },
                { "offline_mode", "OFFLINE MODE" },
                { "online_sync", "ONLINE • SYNC READY" },
                { "exit_training_confirm", "Exit to Home Menu?" },

                // Worker Profile & Modules (Home)
                { "worker_profile", "WORKER PROFILE" },
                { "worker_id_label", "Worker ID" },
                { "worker_name_label", "Worker Name" },
                { "worker_default_name", "Operator Ramesh Kumar" },
                { "worker_division", "Division: Mining & Material Handling" },
                { "modules_header", "AVAILABLE MODULES" },
                { "module_fire_title", "Fire & Explosion Response" },
                { "module_fire_desc", "9-step industrial conveyor fire response: hazard detection, classification, P.A.S.S. extinguisher procedure, and emergency evacuation." },
                { "module_gas_title", "Gas Leak & Confined Space Safety" },
                { "module_gas_desc", "Atmospheric monitoring, multi-gas detector calibration, forced air ventilation, and confined space entry rescue protocols." },
                { "status_available", "AVAILABLE" },
                { "status_coming_soon", "COMING SOON" },
                { "btn_start_training", "START TRAINING →" },

                // Application Settings
                { "settings_title", "APPLICATION SETTINGS" },
                { "sound_effects", "Sound Effects" },
                { "effects_volume", "Effects Volume" },
                { "emergency_alarm", "Emergency Alarm Siren" },
                { "language_header", "Language / भाषा / ᱯᱟᱹᱨᱥᱤ" },
                { "lang_english", "English" },
                { "lang_hindi", "हिन्दी" },
                { "lang_santali", "ᱥᱟᱱᱛᱟᱲᱤ" },

                // Fire Training HUD & Common AR States
                { "fire_header_title", "FIRE & EXPLOSION RESPONSE" },
                { "step_badge_format", "STEP {0}/{1}" },
                { "ar_calibration_badge", "AR CALIBRATION" },
                { "ar_calibration_prompt", "Searching for surfaces... Move phone slowly." },
                { "ar_calibration_feedback", "Keep camera pointed toward textured floor." },
                { "surface_detected_badge", "SURFACE DETECTED" },
                { "surface_detected_prompt", "Surface detected! Tap anywhere on the floor to place the Fire Hazard." },
                { "surface_detected_feedback", "Tap detected floor surface to spawn training scenario." },
                { "btn_place_hazard", "PLACE FIRE HAZARD HERE →" },
                { "btn_alarm_on", "🔊 ALARM ON" },
                { "btn_alarm_off", "🔇 ALARM OFF" },
                { "feedback_alarm_enabled", "Emergency Alarm: Enabled 🔊" },
                { "feedback_alarm_muted", "Emergency Alarm: Muted 🔇" },
                { "feedback_sound_enabled", "Sound Alerts: Enabled 🔊" },
                { "feedback_sound_muted", "Sound Alerts: Muted 🔇" },
                { "action_required_format", "Action required for Step {0}: {1}" },
                { "step_completed_format", "✓ Step {0} Completed! Tap Next Below" },
                { "step_review_format", "✓ Step {0} Completed [Review Mode]" },
                { "training_completed_notice", "✓ Training Completed! Tap View Assessment Below" },
                { "default_success_feedback", "✓ Action completed successfully." },

                // Step 1: Detect Hazard
                { "fire_step1_title", "DETECT HAZARD" },
                { "fire_step1_prompt", "Fire hazard located! Tap [ACKNOWLEDGE FIRE HAZARD →] below to confirm detection:" },
                { "fire_step1_next", "NEXT: IDENTIFY HAZARD →" },
                { "fire_step1_btn_ack", "ACKNOWLEDGE FIRE HAZARD →" },
                { "fire_step1_success", "✓ Hazard acknowledged. Next: Classify hazard." },

                // Step 2: Identify Hazard
                { "fire_step2_title", "IDENTIFY HAZARD" },
                { "fire_step2_prompt", "Hazard spotted! Tap [1. CLASS E: ELECTRICAL FIRE (480V) →] below:" },
                { "fire_step2_next", "NEXT: RAISE ALARM →" },
                { "fire_step2_opt1", "1. CLASS E: ELECTRICAL FIRE (480V) →" },
                { "fire_step2_opt2", "2. Class A: Ordinary Combustible Material" },
                { "fire_step2_opt3", "3. Class B: Flammable Chemical / Solvent" },
                { "fire_step2_success", "✓ Class E Electrical Hazard confirmed! Next: Raise alarm." },
                { "fire_step2_err_classA", "✗ Incorrect: Conveyor is electrical machinery. Select Class E." },
                { "fire_step2_err_classB", "✗ Incorrect: Class B is for flammable liquids. Select Class E." },

                // Step 3: Raise Alarm
                { "fire_step3_title", "RAISE ALARM" },
                { "fire_step3_prompt", "Class E Hazard confirmed! Tap [ACTIVATE MANUAL CALL POINT (ALARM) →] below immediately:" },
                { "fire_step3_next", "NEXT: SELECT EXTINGUISHER →" },
                { "fire_step3_btn_alarm", "ACTIVATE MANUAL CALL POINT (ALARM) →" },
                { "fire_step3_success", "✓ Emergency Alarm Activated! Siren sounding." },

                // Step 4: Select Extinguisher
                { "fire_step4_title", "SELECT EXTINGUISHER" },
                { "fire_step4_prompt", "Alarm active! Tap [1. CO2 EXTINGUISHER (CARBON DIOXIDE) →] below:" },
                { "fire_step4_next", "NEXT: SAFE DISTANCE →" },
                { "fire_step4_opt1", "1. CO2 EXTINGUISHER (CARBON DIOXIDE) →" },
                { "fire_step4_opt2", "2. Water Extinguisher (H2O) [Electrical Shock Hazard]" },
                { "fire_step4_opt3", "3. Foam Extinguisher (AFFF) [Conductive Risk]" },
                { "fire_step4_success", "✓ CO2 Extinguisher selected! Safe for electrical fires." },
                { "fire_step4_err_water", "✗ Incorrect extinguisher! Water conducts electricity. Select CO2." },
                { "fire_step4_err_foam", "✗ Incorrect extinguisher! Foam is water-based. Select CO2." },

                // Step 5: Safe Distance
                { "fire_step5_title", "SAFE DISTANCE" },
                { "fire_step5_prompt", "Maintain safe distance! Tap [STAND AT SAFE DISTANCE (2.5m) →] below outside danger zone:" },
                { "fire_step5_next", "NEXT: PASS PROCEDURE →" },
                { "fire_step5_opt1", "STAND AT SAFE DISTANCE (2.5m) →" },
                { "fire_step5_opt2", "Approach Fire (1.2m - Danger Zone)" },
                { "fire_step5_success", "✓ Safe 2.5m distance confirmed. Next: Begin PASS procedure." },
                { "fire_step5_err_danger", "✗ Unsafe distance! You entered the 2m danger zone. Move back." },

                // Step 6: PASS Procedure
                { "fire_step6_title", "PASS PROCEDURE" },
                { "fire_step6_prompt_pull", "Execute P.A.S.S. Step 1: Tap [1. PULL SAFETY PIN →] below to break the tamper seal:" },
                { "fire_step6_prompt_aim", "Execute P.A.S.S. Step 2: Pin removed! Tap [2. AIM AT BASE OF FIRE →] below:" },
                { "fire_step6_prompt_squeeze", "Execute P.A.S.S. Step 3: Base aimed! Tap [3. SQUEEZE OPERATING LEVER →] below:" },
                { "fire_step6_prompt_sweep", "Execute P.A.S.S. Step 4: Discharging CO2! Tap [4. SWEEP NOZZLE SIDE TO SIDE →] below to smother flames:" },
                { "fire_step6_next", "NEXT: EMERGENCY EXIT →" },
                { "fire_step6_btn_pull", "1. PULL SAFETY PIN →" },
                { "fire_step6_btn_aim", "2. AIM NOZZLE AT BASE OF FIRE →" },
                { "fire_step6_btn_squeeze", "3. SQUEEZE OPERATING LEVER →" },
                { "fire_step6_btn_sweep", "4. SWEEP NOZZLE SIDE TO SIDE →" },
                { "fire_step6_success", "✓ Fire extinguished! Conveyor fire suppressed." },
                { "fire_step6_fb_pin", "✓ Pin pulled! Extinguisher unlocked. Next: Aim nozzle." },
                { "fire_step6_fb_aim", "✓ Nozzle aimed at fire base. Next: Squeeze handle." },
                { "fire_step6_fb_squeeze", "✓ CO2 gas discharging! Next: Sweep across fire base." },
                { "fire_step6_fb_sweep", "✓ Side-to-side sweeping motion applied." },

                // Step 7: Emergency Exit
                { "fire_step7_title", "EMERGENCY EXIT" },
                { "fire_step7_prompt", "Fire suppressed! Tap [SECTOR B EMERGENCY EXIT →] below to mark designated exit:" },
                { "fire_step7_next", "NEXT: EVACUATION ROUTE →" },
                { "fire_step7_opt1", "SECTOR B EMERGENCY EXIT →" },
                { "fire_step7_opt2", "Freight Elevator (DO NOT USE IN FIRE)" },
                { "fire_step7_opt3", "Sector A Route (Smoke Blocked - Unsafe)" },
                { "fire_step7_success", "✓ Emergency Exit Sector B verified! Next: Evacuate route." },
                { "fire_step7_err_elevator", "✗ Unsafe! Never use elevators during fire evacuation. Select Sector B." },
                { "fire_step7_err_blocked", "✗ Unsafe! Sector A corridor is blocked by toxic smoke. Select Sector B." },

                // Step 8: Evacuation Route
                { "fire_step8_title", "EVACUATION ROUTE" },
                { "fire_step8_prompt", "Follow evacuation route! Tap [WAYPOINT 1: MAIN CORRIDOR →] below:" },
                { "fire_step8_prompt_wp1", "Evacuate: Tap [WAYPOINT 1: MAIN CORRIDOR →] below:" },
                { "fire_step8_prompt_wp2", "Evacuate: Tap [WAYPOINT 2: BYPASS CROSSCUT →] below:" },
                { "fire_step8_prompt_wp3", "Evacuate: Tap [WAYPOINT 3: FIRE DOOR EXIT →] below:" },
                { "fire_step8_next", "NEXT: ASSEMBLY POINT →" },
                { "fire_step8_wp1", "WAYPOINT 1: MAIN CORRIDOR →" },
                { "fire_step8_wp2", "WAYPOINT 2: BYPASS CROSSCUT →" },
                { "fire_step8_wp3", "WAYPOINT 3: FIRE DOOR EXIT →" },
                { "fire_step8_unsafe_smoke", "⚠ Sector A Smoke Corridor (Unsafe)" },
                { "fire_step8_success", "✓ Evacuation route completed safely! Next: Assemble at Muster Point Alpha." },
                { "fire_step8_fb_wp1", "✓ Waypoint 1 reached! Proceed through Bypass Crosscut." },
                { "fire_step8_fb_wp2", "✓ Waypoint 2 reached! Proceed to Fire Door Exit." },
                { "fire_step8_fb_wp3", "✓ Waypoint 3 reached! Route cleared safely." },
                { "fire_step8_err_smoke", "✗ Toxic smoke hazard! Do not enter Sector A. Use Main Corridor." },

                // Step 9: Assembly Point
                { "fire_step9_title", "ASSEMBLY POINT" },
                { "fire_step9_prompt", "Final safety action: Tap [REACH ASSEMBLY POINT (MUSTER ALPHA) →] below:" },
                { "fire_step9_next", "VIEW ASSESSMENT →" },
                { "fire_step9_opt1", "REACH ASSEMBLY POINT (MUSTER ALPHA) →" },
                { "fire_step9_opt2", "Perimeter Loading Gate (Unauthorized Area)" },
                { "fire_step9_success", "✓ Assembly Point reached! Worker accounted for." },
                { "fire_step9_err_downwind", "✗ Loading Gate is not an assembly area! Proceed to Muster Point Alpha." },

                // Assessment Summary
                { "assessment_title", "ASSESSMENT SUMMARY" },
                { "assessment_subtitle", "Industrial Safety AR • Jharkhand Mining Division" },
                { "assessment_status_passed", "PASSED" },
                { "assessment_status_failed", "FAILED" },
                { "assessment_score_label", "FINAL SCORE" },
                { "assessment_duration_label", "Duration" },
                { "assessment_deductions_label", "Deductions" },
                { "assessment_sync_ready", "SYNC READY" },
                { "assessment_sync_pending", "SYNC PENDING" },
                { "assessment_outbox_prepared", "RECORD PREPARED FOR SYNC" },
                { "assessment_outbox_not_final", "ATTEMPT INCOMPLETE" },
                { "assessment_breakdown_header", "STEP-BY-STEP BREAKDOWN" },
                { "assessment_deduction_reason_hazard", "-5: Selected incorrect hazard classification" },
                { "assessment_deduction_reason_extinguisher", "-5: Selected incorrect/conductive extinguisher" },
                { "assessment_deduction_reason_smoke", "-5: Entered toxic smoke corridor during evacuation" },
                { "assessment_btn_finish", "RETURN TO HOME" },
                { "assessment_btn_retake", "RETAKE SCENARIO" },
                { "assessment_btn_breakdown", "VIEW BREAKDOWN" }
            };
            _catalog[LangEnglish] = en;

            // =============================================================
            // 2. Hindi (hi)
            // =============================================================
            var hi = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // App Branding & Navigation
                { "app_title", "औद्योगिक सुरक्षा एआर" },
                { "app_subtitle", "व्यावसायिक प्रशिक्षण सिम्युलेटर • झारखंड उद्योग" },
                { "btn_back", "← वापस" },
                { "btn_close", "बंद करें [X]" },
                { "btn_next", "अगला चरण →" },
                { "btn_complete", "प्रशिक्षण पूर्ण करें" },
                { "btn_retake", "पुनः प्रयास करें" },
                { "btn_return_home", "मुख्य मेनू पर वापस जाएं" },
                { "state_on", "चालू" },
                { "state_off", "बंद" },
                { "offline_mode", "ऑफलाइन मोड" },
                { "online_sync", "ऑनलाइन • सिंक तैयार" },
                { "exit_training_confirm", "मुख्य मेनू पर वापस जाएं?" },

                // Worker Profile & Modules (Home)
                { "worker_profile", "श्रमिक प्रोफ़ाइल" },
                { "worker_id_label", "श्रमिक आईडी" },
                { "worker_name_label", "श्रमिक नाम" },
                { "worker_default_name", "ऑपरेटर रमेश कुमार" },
                { "worker_division", "विभाग: खनन एवं सामग्री प्रबंधन" },
                { "modules_header", "उपलब्ध मॉड्यूल" },
                { "module_fire_title", "आग एवं विस्फोट प्रतिक्रिया" },
                { "module_fire_desc", "9-चरणीय औद्योगिक कन्वेयर आग प्रतिक्रिया: खतरे की पहचान, वर्गीकरण, P.A.S.S. अग्निशामक प्रक्रिया और आपातकालीन निकासी।" },
                { "module_gas_title", "गैस रिसाव एवं सीमित स्थान सुरक्षा" },
                { "module_gas_desc", "वायुमंडलीय निगरानी, बहु-गैस डिटेक्टर अंशांकन, मजबूर वायु वेंटिलेशन और सीमित स्थान बचाव प्रोटोकॉल।" },
                { "status_available", "उपलब्ध" },
                { "status_coming_soon", "शीघ्र उपलब्ध" },
                { "btn_start_training", "प्रशिक्षण शुरू करें →" },

                // Application Settings
                { "settings_title", "एप्लिकेशन सेटिंग्स" },
                { "sound_effects", "ध्वनि प्रभाव" },
                { "effects_volume", "ध्वनि वॉल्यूम" },
                { "emergency_alarm", "आपातकालीन सायरन" },
                { "language_header", "भाषा / Language / ᱯᱟᱹᱨᱥᱤ" },
                { "lang_english", "English" },
                { "lang_hindi", "हिन्दी" },
                { "lang_santali", "ᱥᱟᱱᱛᱟᱲᱤ" },

                // Fire Training HUD & Common AR States
                { "fire_header_title", "आग एवं विस्फोट प्रतिक्रिया" },
                { "step_badge_format", "चरण {0}/{1}" },
                { "ar_calibration_badge", "एआर कैलिब्रेशन" },
                { "ar_calibration_prompt", "सतह खोजी जा रही है... फोन को धीरे-धीरे घुमाएं।" },
                { "ar_calibration_feedback", "कैमरे को फर्श की ओर केंद्रित रखें।" },
                { "surface_detected_badge", "सतह मिल गई" },
                { "surface_detected_prompt", "सतह मिल गई! आग का खतरा स्थापित करने के लिए फर्श पर टैप करें।" },
                { "surface_detected_feedback", "प्रशिक्षण परिदृश्य शुरू करने के लिए फर्श पर टैप करें।" },
                { "btn_place_hazard", "यहाँ आग का खतरा स्थापित करें →" },
                { "btn_alarm_on", "🔊 सायरन चालू" },
                { "btn_alarm_off", "🔇 सायरन बंद" },
                { "feedback_alarm_enabled", "आपातकालीन सायरन: सक्षम 🔊" },
                { "feedback_alarm_muted", "आपातकालीन सायरन: म्यूट 🔇" },
                { "feedback_sound_enabled", "ध्वनि चेतावनी: सक्षम 🔊" },
                { "feedback_sound_muted", "ध्वनि चेतावनी: म्यूट 🔇" },
                { "action_required_format", "चरण {0} के लिए कार्रवाई आवश्यक: {1}" },
                { "step_completed_format", "✓ चरण {0} पूर्ण! नीचे 'अगला' टैप करें" },
                { "step_review_format", "✓ चरण {0} पूर्ण [समीक्षा मोड]" },
                { "training_completed_notice", "✓ प्रशिक्षण पूर्ण! नीचे परिणाम देखें" },
                { "default_success_feedback", "✓ कार्रवाई सफलतापूर्वक पूर्ण हुई।" },

                // Step 1: Detect Hazard
                { "fire_step1_title", "खतरे का पता लगाना" },
                { "fire_step1_prompt", "आग का खतरा देखा गया! पुष्टि करने के लिए नीचे टैप करें:" },
                { "fire_step1_next", "अगला: खतरे की पहचान →" },
                { "fire_step1_btn_ack", "आग के खतरे की पुष्टि करें →" },
                { "fire_step1_success", "✓ खतरे की पुष्टि हुई। अगला: खतरे का वर्गीकरण करें।" },

                // Step 2: Identify Hazard
                { "fire_step2_title", "खतरे की पहचान" },
                { "fire_step2_prompt", "खतरा पहचाना गया! नीचे [1. क्लास ई: विद्युत आग] चुनें:" },
                { "fire_step2_next", "अगला: अलार्म बजाएं →" },
                { "fire_step2_opt1", "1. क्लास ई: विद्युत आग (480V) →" },
                { "fire_step2_opt2", "2. क्लास ए: सामान्य ज्वलनशील ठोस सामग्री" },
                { "fire_step2_opt3", "3. क्लास बी: ज्वलनशील रासायनिक तरल / विलायक" },
                { "fire_step2_success", "✓ क्लास ई विद्युत खतरा पुष्ट! अगला: सायरन बजाएं।" },
                { "fire_step2_err_classA", "✗ गलत: कन्वेयर विद्युत मशीनरी है। क्लास ई चुनें।" },
                { "fire_step2_err_classB", "✗ गलत: क्लास बी तरल पदार्थों के लिए है। क्लास ई चुनें।" },

                // Step 3: Raise Alarm
                { "fire_step3_title", "अलार्म बजाएं" },
                { "fire_step3_prompt", "विद्युत आग पुष्ट! तुरंत नीचे मैनुअल कॉल पॉइंट सक्रिय करें:" },
                { "fire_step3_next", "अगला: अग्निशामक चुनें →" },
                { "fire_step3_btn_alarm", "मैनुअल कॉल पॉइंट (अलार्म) सक्रिय करें →" },
                { "fire_step3_success", "✓ आपातकालीन अलार्म चालू! सायरन बज रहा है।" },

                // Step 4: Select Extinguisher
                { "fire_step4_title", "अग्निशामक चुनें" },
                { "fire_step4_prompt", "अलार्म सक्रिय! नीचे [1. CO2 अग्निशामक] चुनें:" },
                { "fire_step4_next", "अगला: सुरक्षित दूरी →" },
                { "fire_step4_opt1", "1. CO2 अग्निशामक (कार्बन डाइऑक्साइड) →" },
                { "fire_step4_opt2", "2. पानी अग्निशामक (H2O) [विद्युत झटका जोखिम]" },
                { "fire_step4_opt3", "3. फोम अग्निशामक (AFFF) [विद्युत चालक जोखिम]" },
                { "fire_step4_success", "✓ CO2 अग्निशामक चयनित! विद्युत आग के लिए सुरक्षित।" },
                { "fire_step4_err_water", "✗ गलत अग्निशामक! पानी में बिजली फैलती है। CO2 चुनें।" },
                { "fire_step4_err_foam", "✗ गलत अग्निशामक! फोम में पानी होता है। CO2 चुनें।" },

                // Step 5: Safe Distance
                { "fire_step5_title", "सुरक्षित दूरी" },
                { "fire_step5_prompt", "सुरक्षित दूरी बनाए रखें! खतरे के क्षेत्र से बाहर (2.5 मी) खड़े हों:" },
                { "fire_step5_next", "अगला: PASS प्रक्रिया →" },
                { "fire_step5_opt1", "सुरक्षित दूरी (2.5 मी) पर खड़े हों →" },
                { "fire_step5_opt2", "आग के पास जाएं (1.2 मी - खतरा क्षेत्र)" },
                { "fire_step5_success", "✓ सुरक्षित 2.5 मी दूरी पुष्ट। अगला: PASS प्रक्रिया शुरू करें।" },
                { "fire_step5_err_danger", "✗ असुरक्षित दूरी! आप खतरे के क्षेत्र में हैं। पीछे हटें।" },

                // Step 6: PASS Procedure
                { "fire_step6_title", "PASS प्रक्रिया" },
                { "fire_step6_prompt_pull", "PASS चरण 1: सुरक्षा सील तोड़ने के लिए पिन खींचें:" },
                { "fire_step6_prompt_aim", "PASS चरण 2: नोजल को आग के आधार की ओर लक्षित करें:" },
                { "fire_step6_prompt_squeeze", "PASS चरण 3: गैस छोड़ने के लिए ऑपरेटिंग लीवर दबाएं:" },
                { "fire_step6_prompt_sweep", "PASS चरण 4: आग बुझाने के लिए नोजल को दाएं-बाएं घुमाएं:" },
                { "fire_step6_next", "अगला: आपातकालीन निकास →" },
                { "fire_step6_btn_pull", "1. सुरक्षा पिन खींचें (PULL) →" },
                { "fire_step6_btn_aim", "2. आग के आधार पर निशाना साधें (AIM) →" },
                { "fire_step6_btn_squeeze", "3. ऑपरेटिंग लीवर दबाएं (SQUEEZE) →" },
                { "fire_step6_btn_sweep", "4. नोजल को दाएं-बाएं घुमाएं (SWEEP) →" },
                { "fire_step6_success", "✓ आग बुझ गई! कन्वेयर आग सफलतापूर्वक शांत।" },
                { "fire_step6_fb_pin", "✓ पिन खींच ली गई! अग्निशामक अनलॉक। अगला: नोजल लक्षित करें।" },
                { "fire_step6_fb_aim", "✓ नोजल आग के आधार पर केंद्रित। अगला: हैंडल दबाएं।" },
                { "fire_step6_fb_squeeze", "✓ CO2 गैस निकल रही है! अगला: आधार पर दाएं-बाएं घुमाएं।" },
                { "fire_step6_fb_sweep", "✓ दाएं-बाएं छिड़काव किया गया।" },

                // Step 7: Emergency Exit
                { "fire_step7_title", "आपातकालीन निकास" },
                { "fire_step7_prompt", "आग बुझाई गई! निर्धारित आपातकालीन निकास की पुष्टि करें:" },
                { "fire_step7_next", "अगला: निकासी मार्ग →" },
                { "fire_step7_opt1", "सेक्टर बी आपातकालीन निकास →" },
                { "fire_step7_opt2", "मालवाहक लिफ्ट (आग में उपयोग न करें)" },
                { "fire_step7_opt3", "सेक्टर ए मार्ग (धुएं से अवरुद्ध - असुरक्षित)" },
                { "fire_step7_success", "✓ आपातकालीन निकास सेक्टर बी पुष्ट! अगला: निकासी मार्ग।" },
                { "fire_step7_err_elevator", "✗ असुरक्षित! निकासी के दौरान लिफ्ट का उपयोग कभी न करें। सेक्टर बी चुनें।" },
                { "fire_step7_err_blocked", "✗ असुरक्षित! सेक्टर ए गलियारा विषाक्त धुएं से अवरुद्ध है। सेक्टर बी चुनें।" },

                // Step 8: Evacuation Route
                { "fire_step8_title", "निकासी मार्ग" },
                { "fire_step8_prompt", "निकासी मार्ग का पालन करें! नीचे [वेपॉइंट 1: मुख्य गलियारा →] चुनें:" },
                { "fire_step8_prompt_wp1", "निकासी: नीचे [वेपॉइंट 1: मुख्य गलियारा →] चुनें:" },
                { "fire_step8_prompt_wp2", "निकासी: नीचे [वेपॉइंट 2: बाईपास क्रॉसकट →] चुनें:" },
                { "fire_step8_prompt_wp3", "निकासी: नीचे [वेपॉइंट 3: फायर डोर निकास →] चुनें:" },
                { "fire_step8_next", "अगला: सुरक्षित सभा स्थल →" },
                { "fire_step8_wp1", "वेपॉइंट 1: मुख्य गलियारा →" },
                { "fire_step8_wp2", "वेपॉइंट 2: बाईपास क्रॉसकट →" },
                { "fire_step8_wp3", "वेपॉइंट 3: फायर डोर निकास →" },
                { "fire_step8_unsafe_smoke", "⚠ सेक्टर ए धुआं गलियारा (असुरक्षित)" },
                { "fire_step8_success", "✓ निकासी मार्ग सुरक्षित रूप से पूर्ण! अगला: मस्टर पॉइंट अल्फा।" },
                { "fire_step8_fb_wp1", "✓ वेपॉइंट 1 पहुंचे! बाईपास क्रॉसकट से आगे बढ़ें।" },
                { "fire_step8_fb_wp2", "✓ वेपॉइंट 2 पहुंचे! फायर डोर निकास की ओर बढ़ें।" },
                { "fire_step8_fb_wp3", "✓ वेपॉइंट 3 पहुंचे! मार्ग सुरक्षित रूप से पार हुआ।" },
                { "fire_step8_err_smoke", "✗ जहरीले धुएं का खतरा! सेक्टर ए में प्रवेश न करें। मुख्य गलियारा लें।" },

                // Step 9: Assembly Point
                { "fire_step9_title", "सुरक्षित सभा स्थल" },
                { "fire_step9_prompt", "अंतिम सुरक्षा कार्रवाई: मस्टर पॉइंट अल्फा पर एकत्रित हों:" },
                { "fire_step9_next", "मूल्यांकन परिणाम देखें →" },
                { "fire_step9_opt1", "सभा स्थल (मस्टर अल्फा) पहुंचें →" },
                { "fire_step9_opt2", "परिधि लोडिंग गेट (अनधिकृत क्षेत्र)" },
                { "fire_step9_success", "✓ सभा स्थल पहुंचे! श्रमिक उपस्थिति दर्ज हुई।" },
                { "fire_step9_err_downwind", "✗ लोडिंग गेट सभा स्थल नहीं है! मस्टर पॉइंट अल्फा की ओर बढ़ें।" },

                // Assessment Summary
                { "assessment_title", "मूल्यांकन सारांश" },
                { "assessment_subtitle", "औद्योगिक सुरक्षा एआर • झारखंड खनन प्रभाग" },
                { "assessment_status_passed", "उत्तीर्ण (PASS)" },
                { "assessment_status_failed", "अनुत्तीर्ण (FAIL)" },
                { "assessment_score_label", "अंतिम स्कोर" },
                { "assessment_duration_label", "समय अवधि" },
                { "assessment_deductions_label", "कटौतियां" },
                { "assessment_sync_ready", "सिंक तैयार" },
                { "assessment_sync_pending", "सिंक लंबित" },
                { "assessment_outbox_prepared", "रिकॉर्ड सिंक के लिए तैयार" },
                { "assessment_outbox_not_final", "प्रयास अधूरा" },
                { "assessment_breakdown_header", "चरण-दर-चरण विवरण" },
                { "assessment_deduction_reason_hazard", "-5: गलत खतरे के वर्गीकरण का चयन" },
                { "assessment_deduction_reason_extinguisher", "-5: गलत/चालक अग्निशामक का चयन" },
                { "assessment_deduction_reason_smoke", "-5: निकासी के दौरान जहरीले धुएं में प्रवेश" },
                { "assessment_btn_finish", "मुख्य मेनू पर वापस जाएं" },
                { "assessment_btn_retake", "पुनः प्रयास करें" },
                { "assessment_btn_breakdown", "विवरण देखें" }
            };
            _catalog[LangHindi] = hi;

            // =============================================================
            // 3. Santali (sat) in Ol Chiki script
            // =============================================================
            var sat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // App Branding & Navigation
                { "app_title", "ᱤᱱᱰᱟᱥᱴᱨᱤᱭᱟᱞ ᱥᱮᱯᱷᱴᱤ ᱮ.ᱟᱨ." },
                { "app_subtitle", "ᱵᱮᱵᱚᱥᱟᱭ ᱴᱨᱮᱱᱤᱝ ᱥᱤᱢᱩᱞᱮᱴᱚᱨ • ᱡᱷᱟᱨᱠᱷᱚᱸᱰ ᱠᱟᱹᱨᱜᱟᱲ" },
                { "btn_back", "← ᱨᱩᱣᱟᱹᱲ" },
                { "btn_close", "ᱵᱚᱸᱫᱽ [X]" },
                { "btn_next", "ᱞᱟᱦᱟ ᱫᱷᱟᱯ →" },
                { "btn_complete", "ᱴᱨᱮᱱᱤᱝ ᱯᱩᱨᱟᱹᱣ" },
                { "btn_retake", "ᱫᱚᱦᱲᱟ ᱮᱦᱚᱵ" },
                { "btn_return_home", "ᱢᱩᱬᱩᱛ ᱢᱮᱱᱩ ᱨᱩᱣᱟᱹᱲ" },
                { "state_on", "ᱪᱟᱹᱞᱩ" },
                { "state_off", "ᱵᱚᱸᱫᱽ" },
                { "offline_mode", "ᱚᱯᱷᱞᱟᱭᱤᱱ ᱢᱳᱰ" },
                { "online_sync", "ᱚᱱᱞᱟᱭᱤᱱ • ᱥᱤᱝᱠ ᱛᱮᱭᱟᱨ" },
                { "exit_training_confirm", "ᱢᱩᱬᱩᱛ ᱢᱮᱱᱩ ᱨᱩᱣᱟᱹᱲ?" },

                // Worker Profile & Modules (Home)
                { "worker_profile", "ᱠᱟᱹᱢᱤᱭᱟᱹ ᱯᱨᱳᱯᱷᱟᱭᱤᱞ" },
                { "worker_id_label", "ᱠᱟᱹᱢᱤᱭᱟᱹ ᱟᱭᱰᱤ" },
                { "worker_name_label", "ᱠᱟᱹᱢᱤᱭᱟᱹ ᱧᱩᱛᱩᱢ" },
                { "worker_default_name", "ᱚᱯᱟᱨᱮᱴᱚᱨ ᱨᱚᱢᱮᱥ ᱠᱩᱢᱟᱨ" },
                { "worker_division", "ᱦᱟᱹᱴᱤᱧ: ᱠᱷᱟᱫᱟᱱ ᱟᱨ ᱥᱟᱢᱟᱱ ᱥᱟᱵ" },
                { "modules_header", "ᱢᱮᱱᱟᱜ ᱢᱚᱰᱩᱞᱠᱚ" },
                { "module_fire_title", "ᱥᱮᱸᱜᱮᱞ ᱟᱨ ᱵᱤᱥᱯᱷᱚᱴ ᱨᱩᱠᱷᱤᱭᱟᱹ" },
                { "module_fire_desc", "9-ᱫᱷᱟᱯ ᱠᱟᱹᱨᱜᱟᱲ ᱠᱚᱱᱵᱷᱮᱭᱟᱨ ᱥᱮᱸᱜᱮᱞ ᱱᱤᱭᱟᱹᱢ: ᱵᱚᱛᱚᱨ ᱧᱟᱢ, ᱵᱷᱮᱜᱟᱨ, P.A.S.S. ᱥᱮᱸᱜᱮᱞ ᱤᱬᱤᱡ ᱟᱨ ᱚᱰᱚᱠ ᱦᱚᱨ᱾" },
                { "module_gas_title", "ᱜᱮᱥ ᱞᱤᱠ ᱟᱨ ᱥᱤᱢᱤᱛ ᱴᱷᱟᱶ ᱨᱩᱠᱷᱤᱭᱟᱹ" },
                { "module_gas_desc", "ᱦᱚᱭ-ᱦᱤᱥᱤᱫ ᱧᱮᱞ, ᱢᱟᱞᱴᱤ-ᱜᱮᱥ ᱰᱤᱴᱮᱠᱴᱚᱨ ᱴᱷᱟᱹᱣᱠᱟᱹ, ᱦᱚᱭ ᱵᱚᱞᱚᱱ ᱟᱨ ᱥᱤᱢᱤᱛ ᱴᱷᱟᱶ ᱵᱟᱧᱪᱟᱣ ᱱᱤᱭᱟᱹᱢ᱾" },
                { "status_available", "ᱢᱮᱱᱟᱜ-ᱟ" },
                { "status_coming_soon", "ᱞᱚᱜᱚᱱ ᱦᱤᱡᱩᱜ-ᱟ" },
                { "btn_start_training", "ᱴᱨᱮᱱᱤᱝ ᱮᱦᱚᱵ ᱢᱮ →" },

                // Application Settings
                { "settings_title", "ᱮᱯᱞᱤᱠᱮᱥᱚᱱ ᱥᱮᱴᱤᱝ" },
                { "sound_effects", "ᱥᱟᱰᱮ ᱤᱯᱷᱮᱠᱴ" },
                { "effects_volume", "ᱥᱟᱰᱮ ᱵᱷᱚᱞᱤᱭᱩᱢ" },
                { "emergency_alarm", "ᱞᱟᱹᱠᱛᱤᱭᱟᱱ ᱟᱞᱟᱨᱢ" },
                { "language_header", "ᱯᱟᱹᱨᱥᱤ / Language / भाषा" },
                { "lang_english", "English" },
                { "lang_hindi", "हिन्दी" },
                { "lang_santali", "ᱥᱟᱱᱛᱟᱲᱤ" },

                // Fire Training HUD & Common AR States
                { "fire_header_title", "ᱥᱮᱸᱜᱮᱞ ᱟᱨ ᱵᱤᱥᱯᱷᱚᱴ ᱨᱩᱠᱷᱤᱭᱟᱹ" },
                { "step_badge_format", "ᱫᱷᱟᱯ {0}/{1}" },
                { "ar_calibration_badge", "ᱮ.ᱟᱨ. ᱥᱟᱡᱟᱣ" },
                { "ar_calibration_prompt", "ᱚᱛ ᱧᱟᱢᱚᱜ ᱠᱟᱱᱟ... ᱯᱷᱳᱱ ᱵᱟᱹᱭ-ᱵᱟᱹᱭ ᱛᱮ ᱦᱤᱞᱟᱹᱣ ᱢᱮ᱾" },
                { "ar_calibration_feedback", "ᱠᱮᱢᱨᱟ ᱚᱛ ᱥᱮᱫ ᱥᱟᱢᱟᱝ ᱫᱚᱦᱚᱭ ᱢᱮ᱾" },
                { "surface_detected_badge", "ᱚᱛ ᱧᱟᱢ ᱮᱱᱟ" },
                { "surface_detected_prompt", "ᱚᱛ ᱧᱟᱢ ᱮᱱᱟ! ᱥᱮᱸᱜᱮᱞ ᱵᱚᱛᱚᱨ ᱵᱟᱹᱭᱥᱟᱹᱣ ᱞᱟᱹᱜᱤᱫ ᱚᱛ ᱨᱮ ᱴᱤᱯᱟᱹᱣ ᱢᱮ᱾" },
                { "surface_detected_feedback", "ᱴᱨᱮᱱᱤᱝ ᱮᱦᱚᱵ ᱞᱟᱹᱜᱤᱫ ᱚᱛ ᱨᱮ ᱴᱤᱯᱟᱹᱣ ᱢᱮ᱾" },
                { "btn_place_hazard", "ᱱᱚᱸᱰᱮ ᱥᱮᱸᱜᱮᱞ ᱵᱟᱹᱭᱥᱟᱹᱣ ᱢᱮ →" },
                { "btn_alarm_on", "🔊 ᱟᱞᱟᱨᱢ ᱪᱟᱹᱞᱩ" },
                { "btn_alarm_off", "🔇 ᱟᱞᱟᱨᱢ ᱵᱚᱸᱫᱽ" },
                { "feedback_alarm_enabled", "ᱞᱟᱹᱠᱛᱤᱭᱟᱱ ᱟᱞᱟᱨᱢ: ᱪᱟᱹᱞᱩ 🔊" },
                { "feedback_alarm_muted", "ᱞᱟᱹᱠᱛᱤᱭᱟᱱ ᱟᱞᱟᱨᱢ: ᱵᱚᱸᱫᱽ 🔇" },
                { "feedback_sound_enabled", "ᱥᱟᱰᱮ: ᱪᱟᱹᱞᱩ 🔊" },
                { "feedback_sound_muted", "ᱥᱟᱰᱮ: ᱵᱚᱸᱫᱽ 🔇" },
                { "action_required_format", "ᱫᱷᱟᱯ {0} ᱞᱟᱹᱜᱤᱫ ᱠᱟᱹᱢᱤ: {1}" },
                { "step_completed_format", "✓ ᱫᱷᱟᱯ {0} ᱯᱩᱨᱟᱹᱣ ᱮᱱᱟ! ᱞᱟᱛᱟᱨ ᱨᱮ ᱞᱟᱦᱟ ᱴᱤᱯᱟᱹᱣ ᱢᱮ" },
                { "step_review_format", "✓ ᱫᱷᱟᱯ {0} ᱯᱩᱨᱟᱹᱣ [ᱧᱮᱞ ᱨᱩᱣᱟᱹᱲ ᱢᱳᱰ]" },
                { "training_completed_notice", "✓ ᱴᱨᱮᱱᱤᱝ ᱯᱩᱨᱟᱹᱣ ᱮᱱᱟ! ᱞᱟᱛᱟᱨ ᱨᱮ ᱧᱮᱞ ᱢᱮ" },
                { "default_success_feedback", "✓ ᱠᱟᱹᱢᱤ ᱱᱟᱯᱟᱭ ᱛᱮ ᱯᱩᱨᱟᱹᱣ ᱮᱱᱟ᱾" },

                // Step 1: Detect Hazard
                { "fire_step1_title", "ᱵᱚᱛᱚᱨ ᱧᱟᱢ" },
                { "fire_step1_prompt", "ᱥᱮᱸᱜᱮᱞ ᱵᱚᱛᱚᱨ ᱧᱟᱢ ᱮᱱᱟ! ᱴᱷᱟᱹᱣᱠᱟᱹ ᱞᱟᱹᱜᱤᱫ ᱞᱟᱛᱟᱨ ᱨᱮ ᱴᱤᱯᱟᱹᱣ ᱢᱮ:" },
                { "fire_step1_next", "ᱞᱟᱦᱟ: ᱵᱚᱛᱚᱨ ᱪᱤᱱᱦᱟᱹᱣ →" },
                { "fire_step1_btn_ack", "ᱥᱮᱸᱜᱮᱞ ᱵᱚᱛᱚᱨ ᱴᱷᱟᱹᱣᱠᱟᱹᱭ ᱢᱮ →" },
                { "fire_step1_success", "✓ ᱵᱚᱛᱚᱨ ᱴᱷᱟᱹᱣᱠᱟᱹ ᱮᱱᱟ᱾ ᱞᱟᱦᱟ: ᱦᱟᱹᱴᱤᱧ ᱪᱤᱱᱦᱟᱹᱣ ᱢᱮ᱾" },

                // Step 2: Identify Hazard
                { "fire_step2_title", "ᱵᱚᱛᱚᱨ ᱪᱤᱱᱦᱟᱹᱣ" },
                { "fire_step2_prompt", "ᱵᱚᱛᱚᱨ ᱧᱮᱞ ᱮᱱᱟ! [1. ᱠᱞᱟᱥ ᱤ: ᱵᱤᱡᱽᱞᱤ ᱥᱮᱸᱜᱮᱞ] ᱵᱟᱪᱷᱟᱣ ᱢᱮ:" },
                { "fire_step2_next", "ᱞᱟᱦᱟ: ᱟᱞᱟᱨᱢ ᱥᱟᱰᱮ →" },
                { "fire_step2_opt1", "1. ᱠᱞᱟᱥ ᱤ: ᱵᱤᱡᱽᱞᱤ ᱥᱮᱸᱜᱮᱞ (480V) →" },
                { "fire_step2_opt2", "2. ᱠᱞᱟᱥ ᱮ: ᱥᱟᱫᱷᱟᱨᱚᱱ ᱨᱚᱠᱚᱢ ᱡᱩᱞᱩᱜ ᱥᱟᱢᱟᱱ" },
                { "fire_step2_opt3", "3. ᱠᱞᱟᱥ ᱵᱤ: ᱡᱩᱞᱩᱜ ᱠᱮᱢᱤᱠᱟᱞ ᱫᱟᱜ / ᱫᱨᱟᱵᱚᱠ" },
                { "fire_step2_success", "✓ ᱠᱞᱟᱥ ᱤ ᱵᱤᱡᱽᱞᱤ ᱵᱚᱛᱚᱨ ᱴᱷᱟᱹᱣᱠᱟᱹ ᱮᱱᱟ! ᱞᱟᱦᱟ: ᱟᱞᱟᱨᱢ ᱥᱟᱰᱮ ᱢᱮ᱾" },
                { "fire_step2_err_classA", "✗ ᱵᱷᱩᱞ: ᱠᱚᱱᱵᱷᱮᱭᱟᱨ ᱫᱚ ᱵᱤᱡᱽᱞᱤ ᱢᱮᱥᱤᱱ ᱠᱟᱱᱟ᱾ ᱠᱞᱟᱥ ᱤ ᱵᱟᱪᱷᱟᱣ ᱢᱮ᱾" },
                { "fire_step2_err_classB", "✗ ᱵᱷᱩᱞ: ᱠᱞᱟᱥ ᱵᱤ ᱫᱚ ᱞᱤᱠᱩᱭᱤᱰ ᱞᱟᱹᱜᱤᱫ ᱠᱟᱱᱟ᱾ ᱠᱞᱟᱥ ᱤ ᱵᱟᱪᱷᱟᱣ ᱢᱮ᱾" },

                // Step 3: Raise Alarm
                { "fire_step3_title", "ᱟᱞᱟᱨᱢ ᱥᱟᱰᱮ" },
                { "fire_step3_prompt", "ᱵᱤᱡᱽᱞᱤ ᱥᱮᱸᱜᱮᱞ ᱴᱷᱟᱹᱣᱠᱟᱹ ᱮᱱᱟ! ᱞᱚᱜᱚᱱ ᱢᱮᱱᱩᱣᱟᱞ ᱠᱚᱞ ᱯᱚᱭᱮᱸᱴ ᱪᱟᱹᱞᱩᱭ ᱢᱮ:" },
                { "fire_step3_next", "ᱞᱟᱦᱟ: ᱤᱬᱤᱡᱤᱡ ᱵᱟᱪᱷᱟᱣ →" },
                { "fire_step3_btn_alarm", "ᱢᱮᱱᱩᱣᱟᱞ ᱠᱚᱞ ᱯᱚᱭᱮᱸᱴ (ᱟᱞᱟᱨᱢ) ᱪᱟᱹᱞᱩᱭ ᱢᱮ →" },
                { "fire_step3_success", "✓ ᱞᱟᱹᱠᱛᱤᱭᱟᱱ ᱟᱞᱟᱨᱢ ᱪᱟᱹᱞᱩ ᱮᱱᱟ! ᱥᱟᱰᱮ ᱠᱟᱱᱟ᱾" },

                // Step 4: Select Extinguisher
                { "fire_step4_title", "ᱤᱬᱤᱡᱤᱡ ᱵᱟᱪᱷᱟᱣ" },
                { "fire_step4_prompt", "ᱟᱞᱟᱨᱢ ᱪᱟᱹᱞᱩ! ᱞᱟᱛᱟᱨ ᱨᱮ [1. CO2 ᱤᱬᱤᱡᱤᱡ] ᱵᱟᱪᱷᱟᱣ ᱢᱮ:" },
                { "fire_step4_next", "ᱞᱟᱦᱟ: ᱥᱟᱺᱜᱤᱧ ᱛᱟᱦᱮᱸᱱ →" },
                { "fire_step4_opt1", "1. CO2 ᱥᱮᱸᱜᱮᱞ ᱤᱬᱤᱡᱤᱡ (ᱠᱟᱨᱵᱚᱱ ᱰᱟᱭᱚᱠᱥᱟᱭᱤᱰ) →" },
                { "fire_step4_opt2", "2. ᱫᱟᱜ ᱤᱬᱤᱡᱤᱡ (H2O) [ᱵᱤᱡᱽᱞᱤ ᱥᱚᱠ ᱵᱚᱛᱚᱨ]" },
                { "fire_step4_opt3", "3. ᱯᱷᱳᱢ ᱤᱬᱤᱡᱤᱡ (AFFF) [ᱵᱤᱡᱽᱞᱤ ᱯᱟᱥᱱᱟᱣ ᱵᱚᱛᱚᱨ]" },
                { "fire_step4_success", "✓ CO2 ᱤᱬᱤᱡᱤᱡ ᱵᱟᱪᱷᱟᱣ ᱮᱱᱟ! ᱵᱤᱡᱽᱞᱤ ᱥᱮᱸᱜᱮᱞ ᱞᱟᱹᱜᱤᱫ ᱱᱟᱯᱟᱭ᱾" },
                { "fire_step4_err_water", "✗ ᱵᱷᱩᱞ ᱤᱬᱤᱡᱤᱡ! ᱫᱟᱜ ᱨᱮ ᱵᱤᱡᱽᱞᱤ ᱯᱟᱥᱱᱟᱣᱜ-ᱟ᱾ CO2 ᱵᱟᱪᱷᱟᱣ ᱢᱮ᱾" },
                { "fire_step4_err_foam", "✗ ᱵᱷᱩᱞ ᱤᱬᱤᱡᱤᱡ! ᱯᱷᱳᱢ ᱨᱮ ᱫᱟᱜ ᱛᱟᱦᱮᱸᱱᱟ᱾ CO2 ᱵᱟᱪᱷᱟᱣ ᱢᱮ᱾" },

                // Step 5: Safe Distance
                { "fire_step5_title", "ᱥᱟᱺᱜᱤᱧ ᱛᱟᱦᱮᱸᱱ" },
                { "fire_step5_prompt", "ᱵᱚᱛᱚᱨ ᱴᱷᱟᱶ ᱠᱷᱚᱱ ᱥᱟᱺᱜᱤᱧ (2.5 ᱢᱤᱴᱟᱨ) ᱨᱮ ᱛᱤᱸᱜᱩᱱ ᱢᱮ:" },
                { "fire_step5_next", "ᱞᱟᱦᱟ: PASS ᱱᱤᱭᱟᱹᱢ →" },
                { "fire_step5_opt1", "ᱥᱟᱺᱜᱤᱧ (2.5 ᱢᱤᱴᱟᱨ) ᱨᱮ ᱛᱤᱸᱜᱩᱱ ᱢᱮ →" },
                { "fire_step5_opt2", "ᱥᱮᱸᱜᱮᱞ ᱥᱩᱨ (1.2 ᱢᱤᱴᱟᱨ - ᱵᱚᱛᱚᱨ ᱴᱷᱟᱶ)" },
                { "fire_step5_success", "✓ 2.5 ᱢᱤᱴᱟᱨ ᱥᱟᱺᱜᱤᱧ ᱴᱷᱟᱹᱣᱠᱟᱹ ᱮᱱᱟ᱾ ᱞᱟᱦᱟ: PASS ᱮᱦᱚᱵ ᱢᱮ᱾" },
                { "fire_step5_err_danger", "✗ ᱵᱚᱛᱚᱨ ᱥᱩᱨ! ᱟᱢ ᱵᱚᱛᱚᱨ ᱴᱷᱟᱶ ᱨᱮ ᱢᱮᱱᱟᱢᱟ᱾ ᱛᱟᱭᱚᱢ ᱢᱮ᱾" },

                // Step 6: PASS Procedure
                { "fire_step6_title", "PASS ᱱᱤᱭᱟᱹᱢ" },
                { "fire_step6_prompt_pull", "PASS ᱫᱷᱟᱯ 1: ᱥᱤᱞ ᱨᱟᱹᱯᱩᱫ ᱞᱟᱹᱜᱤᱫ ᱯᱤᱱ ᱚᱨ ᱢᱮ (PULL):" },
                { "fire_step6_prompt_aim", "PASS ᱫᱷᱟᱯ 2: ᱥᱮᱸᱜᱮᱞ ᱯᱷᱮᱰᱟᱛ ᱥᱮᱫ ᱢᱩᱸᱦᱰᱟᱹᱭ ᱢᱮ (AIM):" },
                { "fire_step6_prompt_squeeze", "PASS ᱫᱷᱟᱯ 3: CO2 ᱚᱰᱚᱠ ᱞᱟᱹᱜᱤᱫ ᱪᱟᱞᱟᱣ ᱞᱤᱵᱷᱟᱨ ᱞᱤᱵᱤᱫ ᱢᱮ:" },
                { "fire_step6_prompt_sweep", "PASS ᱫᱷᱟᱯ 4: ᱥᱮᱸᱜᱮᱞ ᱤᱬᱤᱡ ᱞᱟᱹᱜᱤᱫ ᱱᱚᱡᱚᱞ ᱞᱮᱸᱜᱟ-ᱡᱚᱡᱚᱢ ᱦᱤᱞᱟᱹᱣ ᱢᱮ:" },
                { "fire_step6_next", "ᱞᱟᱦᱟ: ᱚᱰᱚᱠ ᱦᱚᱨ →" },
                { "fire_step6_btn_pull", "1. ᱥᱩᱨᱚᱠᱷᱟ ᱯᱤᱱ ᱚᱨ ᱢᱮ (PULL) →" },
                { "fire_step6_btn_aim", "2. ᱯᱷᱮᱰᱟᱛ ᱥᱮᱫ ᱢᱩᱸᱦᱰᱟᱹᱭ ᱢᱮ (AIM) →" },
                { "fire_step6_btn_squeeze", "3. ᱪᱟᱞᱟᱣ ᱞᱤᱵᱷᱟᱨ ᱞᱤᱵᱤᱫ ᱢᱮ →" },
                { "fire_step6_btn_sweep", "4. ᱱᱚᱡᱚᱞ ᱞᱮᱸᱜᱟ-ᱡᱚᱡᱚᱢ ᱦᱤᱞᱟᱹᱣ ᱢᱮ →" },
                { "fire_step6_success", "✓ ᱥᱮᱸᱜᱮᱞ ᱤᱬᱤᱡ ᱮᱱᱟ! ᱠᱚᱱᱵᱷᱮᱭᱟᱨ ᱥᱮᱸᱜᱮᱞ ᱪᱟᱵᱟ ᱮᱱᱟ᱾" },
                { "fire_step6_fb_pin", "✓ ᱯᱤᱱ ᱚᱨ ᱮᱱᱟ! ᱤᱬᱤᱡᱤᱡ ᱡᱷᱤᱡ ᱮᱱᱟ᱾ ᱞᱟᱦᱟ: ᱢᱩᱸᱦᱰᱟᱹᱭ ᱢᱮ᱾" },
                { "fire_step6_fb_aim", "✓ ᱢᱩᱸᱦᱰᱟᱹ ᱯᱷᱮᱰᱟᱛ ᱥᱮᱫ ᱥᱟᱢᱟᱝ ᱮᱱᱟ᱾ ᱞᱟᱦᱟ: ᱞᱤᱵᱤᱫ ᱢᱮ᱾" },
                { "fire_step6_fb_squeeze", "✓ CO2 ᱜᱮᱥ ᱚᱰᱚᱠᱚᱜ ᱠᱟᱱᱟ! ᱞᱟᱦᱟ: ᱦᱤᱞᱟᱹᱣ ᱢᱮ᱾" },
                { "fire_step6_fb_sweep", "✓ ᱞᱮᱸᱜᱟ-ᱡᱚᱡᱚᱢ ᱦᱤᱞᱟᱹᱣ ᱮᱱᱟ᱾" },

                // Step 7: Emergency Exit
                { "fire_step7_title", "ᱚᱰᱚᱠ ᱦᱚᱨ" },
                { "fire_step7_prompt", "ᱥᱮᱸᱜᱮᱞ ᱤᱬᱤᱡ ᱮᱱᱟ! ᱴᱷᱟᱹᱣᱠᱟᱹ ᱚᱰᱚᱠ ᱦᱚᱨ ᱪᱤᱱᱦᱟᱹᱣ ᱢᱮ:" },
                { "fire_step7_next", "ᱞᱟᱦᱟ: ᱫᱟᱹᱲ ᱦᱚᱨ →" },
                { "fire_step7_opt1", "ᱥᱮᱠᱴᱚᱨ ᱵᱤ ᱚᱰᱚᱠ ᱦᱚᱨ →" },
                { "fire_step7_opt2", "ᱞᱤᱯᱷᱴ (ᱥᱮᱸᱜᱮᱞ ᱚᱠᱛᱚ ᱟᱞᱚᱢ ᱵᱮᱵᱷᱟᱨᱟ)" },
                { "fire_step7_opt3", "ᱥᱮᱠᱴᱚᱨ ᱮ ᱦᱚᱨ (ᱫᱷᱩᱶᱟᱹ ᱛᱮ ᱮᱥᱮᱫ - ᱵᱚᱛᱚᱨ)" },
                { "fire_step7_success", "✓ ᱚᱰᱚᱠ ᱦᱚᱨ ᱥᱮᱠᱴᱚᱨ ᱵᱤ ᱴᱷᱟᱹᱣᱠᱟᱹ ᱮᱱᱟ! ᱞᱟᱦᱟ: ᱫᱟᱹᱲ ᱦᱚᱨ᱾" },
                { "fire_step7_err_elevator", "✗ ᱵᱚᱛᱚᱨ! ᱥᱮᱸᱜᱮᱞ ᱚᱠᱛᱚ ᱞᱤᱯᱷᱴ ᱟᱞᱚᱢ ᱵᱮᱵᱷᱟᱨᱟ᱾ ᱥᱮᱠᱴᱚᱨ ᱵᱤ ᱵᱟᱪᱷᱟᱣ ᱢᱮ᱾" },
                { "fire_step7_err_blocked", "✗ ᱵᱚᱛᱚᱨ! ᱥᱮᱠᱴᱚᱨ ᱮ ᱦᱚᱨ ᱫᱷᱩᱶᱟᱹ ᱛᱮ ᱮᱥᱮᱫ ᱜᱮᱭᱟ᱾ ᱥᱮᱠᱴᱚᱨ ᱵᱤ ᱵᱟᱪᱷᱟᱣ ᱢᱮ᱾" },

                // Step 8: Evacuation Route
                { "fire_step8_title", "ᱫᱟᱹᱲ ᱦᱚᱨ" },
                { "fire_step8_prompt", "ᱫᱟᱹᱲ ᱦᱚᱨ ᱯᱟᱸᱡᱟᱭ ᱢᱮ! ᱞᱟᱛᱟᱨ ᱨᱮ [ᱣᱮᱯᱚᱭᱮᱸᱴ 1: ᱢᱩᱬᱩᱛ ᱦᱚᱨ →] ᱵᱟᱪᱷᱟᱣ ᱢᱮ:" },
                { "fire_step8_prompt_wp1", "ᱫᱟᱹᱲ: ᱞᱟᱛᱟᱨ ᱨᱮ [ᱣᱮᱯᱚᱭᱮᱸᱴ 1: ᱢᱩᱬᱩᱛ ᱦᱚᱨ →] ᱵᱟᱪᱷᱟᱣ ᱢᱮ:" },
                { "fire_step8_prompt_wp2", "ᱫᱟᱹᱲ: ᱞᱟᱛᱟᱨ ᱨᱮ [ᱣᱮᱯᱚᱭᱮᱸᱴ 2: ᱵᱟᱭᱯᱟᱥ ᱠᱨᱚᱥᱠᱟᱴ →] ᱵᱟᱪᱷᱟᱣ ᱢᱮ:" },
                { "fire_step8_prompt_wp3", "ᱫᱟᱹᱲ: ᱞᱟᱛᱟᱨ ᱨᱮ [ᱣᱮᱯᱚᱭᱮᱸᱴ 3: ᱯᱷᱟᱭᱟᱨ ᱰᱳᱨ ᱚᱰᱚᱠ ᱦᱚᱨ →] ᱵᱟᱪᱷᱟᱣ ᱢᱮ:" },
                { "fire_step8_next", "ᱞᱟᱦᱟ: ᱡᱟᱣᱨᱟ ᱴᱷᱟᱶ →" },
                { "fire_step8_wp1", "ᱣᱮᱯᱚᱭᱮᱸᱴ 1: ᱢᱩᱬᱩᱛ ᱦᱚᱨ →" },
                { "fire_step8_wp2", "ᱣᱮᱯᱚᱭᱮᱸᱴ 2: ᱵᱟᱭᱯᱟᱥ ᱠᱨᱚᱥᱠᱟᱴ →" },
                { "fire_step8_wp3", "ᱣᱮᱯᱚᱭᱮᱸᱴ 3: ᱯᱷᱟᱭᱟᱨ ᱰᱳᱨ ᱚᱰᱚᱠ ᱦᱚᱨ →" },
                { "fire_step8_unsafe_smoke", "⚠ ᱥᱮᱠᱴᱚᱨ ᱮ ᱫᱷᱩᱶᱟᱹ ᱦᱚᱨ (ᱵᱚᱛᱚᱨ)" },
                { "fire_step8_success", "✓ ᱫᱟᱹᱲ ᱦᱚᱨ ᱱᱟᱯᱟᱭ ᱛᱮ ᱯᱩᱨᱟᱹᱣ ᱮᱱᱟ! ᱞᱟᱦᱟ: ᱢᱟᱥᱴᱚᱨ ᱯᱚᱭᱮᱸᱴ ᱟᱞᱯᱷᱟ᱾" },
                { "fire_step8_fb_wp1", "✓ ᱣᱮᱯᱚᱭᱮᱸᱴ 1 ᱥᱮᱴᱮᱨ ᱮᱱᱟ! ᱵᱟᱭᱯᱟᱥ ᱠᱨᱚᱥᱠᱟᱴ ᱥᱮᱫ ᱪᱟᱞᱟᱜ ᱢᱮ᱾" },
                { "fire_step8_fb_wp2", "✓ ᱣᱮᱯᱚᱭᱮᱸᱴ 2 ᱥᱮᱴᱮᱨ ᱮᱱᱟ! ᱯᱷᱟᱭᱟᱨ ᱰᱳᱨ ᱚᱰᱚᱠ ᱥᱮᱫ ᱪᱟᱞᱟᱜ ᱢᱮ᱾" },
                { "fire_step8_fb_wp3", "✓ ᱣᱮᱯᱚᱭᱮᱸᱴ 3 ᱥᱮᱴᱮᱨ ᱮᱱᱟ! ᱦᱚᱨ ᱱᱟᱯᱟᱭ ᱛᱮ ᱯᱟᱨᱚᱢ ᱮᱱᱟ᱾" },
                { "fire_step8_err_smoke", "✗ ᱵᱤᱥ ᱫᱷᱩᱶᱟᱹ ᱵᱚᱛᱚᱨ! ᱥᱮᱠᱴᱚᱨ ᱮ ᱨᱮ ᱟᱞᱚᱢ ᱵᱚᱞᱚᱱᱟ᱾ ᱢᱩᱬᱩᱛ ᱦᱚᱨ ᱵᱮᱵᱷᱟᱨ ᱢᱮ᱾" },

                // Step 9: Assembly Point
                { "fire_step9_title", "ᱡᱟᱣᱨᱟ ᱴᱷᱟᱶ" },
                { "fire_step9_prompt", "ᱢᱩᱪᱟᱹᱫ ᱠᱟᱹᱢᱤ: ᱢᱟᱥᱴᱚᱨ ᱯᱚᱭᱮᱸᱴ ᱟᱞᱯᱷᱟ ᱨᱮ ᱡᱟᱣᱨᱟᱜ ᱢᱮ:" },
                { "fire_step9_next", "ᱚᱨᱡᱚ ᱧᱮᱞ ᱢᱮ →" },
                { "fire_step9_opt1", "ᱡᱟᱣᱨᱟ ᱴᱷᱟᱶ (ᱢᱟᱥᱴᱚᱨ ᱟᱞᱯᱷᱟ) ᱥᱮᱴᱮᱨᱚᱜ ᱢᱮ →" },
                { "fire_step9_opt2", "ᱞᱳᱰᱤᱝ ᱜᱮᱴ (ᱵᱟᱝ ᱴᱷᱟᱹᱣᱠᱟᱹ ᱴᱷᱟᱶ)" },
                { "fire_step9_success", "✓ ᱡᱟᱣᱨᱟ ᱴᱷᱟᱶ ᱥᱮᱴᱮᱨ ᱮᱱᱟ! ᱠᱟᱹᱢᱤᱭᱟᱹ ᱞᱮᱠᱷᱟ ᱮᱱᱟᱭ᱾" },
                { "fire_step9_err_downwind", "✗ ᱞᱳᱰᱤᱝ ᱜᱮᱴ ᱫᱚ ᱡᱟᱣᱨᱟ ᱴᱷᱟᱶ ᱵᱟᱝ ᱠᱟᱱᱟ! ᱢᱟᱥᱴᱚᱨ ᱟᱞᱯᱷᱟ ᱥᱮᱫ ᱪᱟᱞᱟᱜ ᱢᱮ᱾" },

                // Assessment Summary
                { "assessment_title", "ᱵᱤᱱᱤᱰ ᱥᱟᱨᱟᱝᱥᱚ" },
                { "assessment_subtitle", "ᱤᱱᱰᱟᱥᱴᱨᱤᱭᱟᱞ ᱥᱮᱯᱷᱴᱤ ᱮ.ᱟᱨ. • ᱡᱷᱟᱨᱠᱷᱚᱸᱰ ᱠᱷᱟᱫᱟᱱ ᱦᱟᱹᱴᱤᱧ" },
                { "assessment_status_passed", "ᱯᱟᱥ (PASSED)" },
                { "assessment_status_failed", "ᱯᱷᱮᱞ (FAILED)" },
                { "assessment_score_label", "ᱢᱩᱪᱟᱹᱫ ᱱᱚᱢᱵᱚᱨ" },
                { "assessment_duration_label", "ᱚᱠᱛᱚ" },
                { "assessment_deductions_label", "ᱠᱟᱴᱟᱣ ᱱᱚᱢᱵᱚᱨ" },
                { "assessment_sync_ready", "ᱥᱤᱝᱠ ᱛᱮᱭᱟᱨ" },
                { "assessment_sync_pending", "ᱥᱤᱝᱠ ᱵᱟᱹᱠᱤ" },
                { "assessment_outbox_prepared", "ᱥᱤᱝᱠ ᱞᱟᱹᱜᱤᱫ ᱨᱮᱠᱚᱨᱰ ᱛᱮᱭᱟᱨ" },
                { "assessment_outbox_not_final", "ᱠᱟᱹᱢᱤ ᱵᱟᱝ ᱯᱩᱨᱟᱹᱣ" },
                { "assessment_breakdown_header", "ᱫᱷᱟᱯ-ᱫᱷᱟᱯ ᱛᱮ ᱵᱤᱵᱚᱨᱚᱬ" },
                { "assessment_deduction_reason_hazard", "-5: ᱵᱷᱩᱞ ᱵᱚᱛᱚᱨ ᱦᱟᱹᱴᱤᱧ ᱵᱟᱪᱷᱟᱣ" },
                { "assessment_deduction_reason_extinguisher", "-5: ᱵᱷᱩᱞ ᱤᱬᱤᱡᱤᱡ ᱵᱟᱪᱷᱟᱣ" },
                { "assessment_deduction_reason_smoke", "-5: ᱫᱟᱹᱲ ᱚᱠᱛᱚ ᱵᱤᱥ ᱫᱷᱩᱶᱟᱹ ᱨᱮ ᱵᱚᱞᱚᱱ" },
                { "assessment_btn_finish", "ᱢᱩᱬᱩᱛ ᱢᱮᱱᱩ ᱨᱩᱣᱟᱹᱲ" },
                { "assessment_btn_retake", "ᱫᱚᱦᱲᱟ ᱮᱦᱚᱵ" },
                { "assessment_btn_breakdown", "ᱵᱤᱵᱚᱨᱚᱬ ᱧᱮᱞ" }
            };
            _catalog[LangSantali] = sat;
        }
    }
}
