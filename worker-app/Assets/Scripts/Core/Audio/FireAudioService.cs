// FireAudioService.cs
// Namespace : IndustrialSafetyAR.Core.Audio
//
// Centralized event-based sound alert service for the Fire & Explosion Response module.
// Uses lightweight procedural waveform synthesis via Unity AudioClip.Create to eliminate
// external audio asset bloat, prevent missing-clip errors, and guarantee non-blocking,
// fail-safe sound playback with local PlayerPrefs settings persistence.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndustrialSafetyAR.Core.Audio
{
    /// <summary>
    /// Supported sound feedback events for the Fire training curriculum.
    /// </summary>
    public enum FireSoundType
    {
        HazardDetected,
        CorrectAction,
        IncorrectAction,
        EmergencyAlarm,
        PassProcedureAction,
        WaypointReached,
        StepCompleted,
        FinalPass,
        FinalFail
    }

    /// <summary>
    /// Centralized singleton service handling training sound alerts and settings.
    /// </summary>
    [DisallowMultipleComponent]
    public class FireAudioService : MonoBehaviour
    {
        private const string PrefSoundEnabled = "FireAudio_SoundEnabled";
        private const string PrefEffectsVolume = "FireAudio_EffectsVolume";
        private const string PrefAlarmEnabled = "FireAudio_AlarmEnabled";

        private static FireAudioService s_Instance;
        private AudioSource _audioSource;
        private AudioSource _alarmAudioSource;
        private readonly Dictionary<FireSoundType, AudioClip> _clipCache = new Dictionary<FireSoundType, AudioClip>();
        private readonly Dictionary<FireSoundType, int> _playCounts = new Dictionary<FireSoundType, int>();

        private bool _isSoundEnabled = true;
        private float _effectsVolume = 0.70f;
        private bool _isEmergencyAlarmEnabled = true;
        private bool _isAlarmActiveScenario = false;

        public static FireAudioService Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindAnyObjectByType<FireAudioService>();
                    if (s_Instance == null)
                    {
                        var go = new GameObject("FireAudioService");
                        s_Instance = go.AddComponent<FireAudioService>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return s_Instance;
            }
        }

        public bool IsSoundEnabled
        {
            get => _isSoundEnabled;
            set
            {
                if (_isSoundEnabled == value) return;
                _isSoundEnabled = value;
                SaveSettings();
                if (!_isSoundEnabled)
                {
                    if (_alarmAudioSource != null && _alarmAudioSource.isPlaying)
                    {
                        _alarmAudioSource.Stop();
                    }
                }
                else if (_isAlarmActiveScenario && _isEmergencyAlarmEnabled)
                {
                    ResumeEmergencyAlarmSiren();
                }
            }
        }

        public float EffectsVolume
        {
            get => _effectsVolume;
            set
            {
                _effectsVolume = Mathf.Clamp01(value);
                SaveSettings();
                if (_audioSource != null) _audioSource.volume = _effectsVolume;
                if (_alarmAudioSource != null) _alarmAudioSource.volume = Mathf.Clamp01(_effectsVolume * 0.85f);
            }
        }

        public bool IsEmergencyAlarmEnabled
        {
            get => _isEmergencyAlarmEnabled;
            set
            {
                if (_isEmergencyAlarmEnabled == value) return;
                _isEmergencyAlarmEnabled = value;
                SaveSettings();
                if (!_isEmergencyAlarmEnabled)
                {
                    if (_alarmAudioSource != null && _alarmAudioSource.isPlaying)
                    {
                        _alarmAudioSource.Stop();
                    }
                }
                else if (_isAlarmActiveScenario && _isSoundEnabled)
                {
                    ResumeEmergencyAlarmSiren();
                }
            }
        }

        public bool IsAlarmActiveScenario => _isAlarmActiveScenario;
        public bool IsAlarmSirenPlaying => _alarmAudioSource != null && _alarmAudioSource.isPlaying;

        public FireSoundType? LastPlayedSound { get; private set; }
        public bool SimulateAudioFailure { get; set; }

        public event Action<FireSoundType> OnSoundPlayed;

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            if (Application.isPlaying && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            LoadSettings();
            EnsureAudioSource();
            SynthesizeAllClips();
        }

        public void EnsureAudioSource()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (_audioSource != null)
            {
                _audioSource.playOnAwake = false;
                _audioSource.loop = false;
                _audioSource.spatialBlend = 0f; // 2D flat sound for mobile speaker clarity
                _audioSource.volume = _effectsVolume;
            }

            if (_alarmAudioSource == null)
            {
                var alarmChild = transform.Find("AlarmAudioSource");
                if (alarmChild != null)
                {
                    _alarmAudioSource = alarmChild.GetComponent<AudioSource>();
                }
                if (_alarmAudioSource == null)
                {
                    var alarmObj = new GameObject("AlarmAudioSource");
                    alarmObj.transform.SetParent(transform, false);
                    _alarmAudioSource = alarmObj.AddComponent<AudioSource>();
                }
            }

            if (_alarmAudioSource != null)
            {
                _alarmAudioSource.playOnAwake = false;
                _alarmAudioSource.loop = true; // Sirens loop until explicitly stopped or alarm disabled
                _alarmAudioSource.spatialBlend = 0f;
                _alarmAudioSource.volume = Mathf.Clamp01(_effectsVolume * 0.85f);
            }
        }

        public void LoadSettings()
        {
            try
            {
                _isSoundEnabled = PlayerPrefs.GetInt(PrefSoundEnabled, 1) == 1;
                _effectsVolume = PlayerPrefs.GetFloat(PrefEffectsVolume, 0.70f);
                _isEmergencyAlarmEnabled = PlayerPrefs.GetInt(PrefAlarmEnabled, 1) == 1;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FireAudioService] Could not load PlayerPrefs audio settings: {ex.Message}");
                _isSoundEnabled = true;
                _effectsVolume = 0.70f;
                _isEmergencyAlarmEnabled = true;
            }
        }

        public void SaveSettings()
        {
            try
            {
                PlayerPrefs.SetInt(PrefSoundEnabled, _isSoundEnabled ? 1 : 0);
                PlayerPrefs.SetFloat(PrefEffectsVolume, _effectsVolume);
                PlayerPrefs.SetInt(PrefAlarmEnabled, _isEmergencyAlarmEnabled ? 1 : 0);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FireAudioService] Could not save PlayerPrefs audio settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Plays the specified sound event with non-blocking, exception-safe execution.
        /// </summary>
        public bool PlaySound(FireSoundType soundType)
        {
            try
            {
                EnsureAudioSource();

                // Track play counts and notify listeners even when muted, allowing deterministic testing
                if (!_playCounts.ContainsKey(soundType))
                {
                    _playCounts[soundType] = 0;
                }
                _playCounts[soundType]++;
                LastPlayedSound = soundType;
                OnSoundPlayed?.Invoke(soundType);

                if (SimulateAudioFailure)
                {
                    throw new InvalidOperationException("Simulated hardware audio subsystem failure");
                }

                if (soundType == FireSoundType.EmergencyAlarm)
                {
                    _isAlarmActiveScenario = true;

                    if (!_isSoundEnabled || !_isEmergencyAlarmEnabled)
                    {
                        if (_alarmAudioSource != null && _alarmAudioSource.isPlaying)
                        {
                            _alarmAudioSource.Stop();
                        }
                        return true;
                    }

                    if (!_clipCache.TryGetValue(soundType, out var alarmClip) || alarmClip == null)
                    {
                        alarmClip = SynthesizeClip(soundType);
                        if (alarmClip != null)
                        {
                            _clipCache[soundType] = alarmClip;
                        }
                    }

                    if (alarmClip != null && _alarmAudioSource != null)
                    {
                        _alarmAudioSource.clip = alarmClip;
                        _alarmAudioSource.volume = Mathf.Clamp01(_effectsVolume * 0.85f);
                        if (!_alarmAudioSource.isPlaying)
                        {
                            _alarmAudioSource.Play();
                        }
                    }
                    return true;
                }

                if (!_isSoundEnabled)
                {
                    return true;
                }

                if (!_clipCache.TryGetValue(soundType, out var clip) || clip == null)
                {
                    clip = SynthesizeClip(soundType);
                    if (clip != null)
                    {
                        _clipCache[soundType] = clip;
                    }
                }

                if (clip != null && _audioSource != null)
                {
                    _audioSource.PlayOneShot(clip, _effectsVolume);
                }

                return true;
            }
            catch (Exception ex)
            {
                // Audio failure must NEVER block or break training workflow
                Debug.LogWarning($"[FireAudioService] Audio playback encountered non-fatal error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resumes the emergency alarm siren looping if the scenario alarm is active and settings permit.
        /// </summary>
        private void ResumeEmergencyAlarmSiren()
        {
            if (!_isSoundEnabled || !_isEmergencyAlarmEnabled || !_isAlarmActiveScenario) return;

            EnsureAudioSource();
            if (!_clipCache.TryGetValue(FireSoundType.EmergencyAlarm, out var clip) || clip == null)
            {
                clip = SynthesizeClip(FireSoundType.EmergencyAlarm);
                if (clip != null)
                {
                    _clipCache[FireSoundType.EmergencyAlarm] = clip;
                }
            }

            if (clip != null && _alarmAudioSource != null)
            {
                _alarmAudioSource.clip = clip;
                _alarmAudioSource.volume = Mathf.Clamp01(_effectsVolume * 0.85f);
                if (!_alarmAudioSource.isPlaying)
                {
                    _alarmAudioSource.Play();
                }
            }
        }

        /// <summary>
        /// Stops the emergency alarm siren and clears the scenario alarm active state.
        /// </summary>
        public void StopEmergencyAlarm()
        {
            _isAlarmActiveScenario = false;
            if (_alarmAudioSource != null && _alarmAudioSource.isPlaying)
            {
                _alarmAudioSource.Stop();
            }
        }

        /// <summary>
        /// Stops all active sound sources (one-shot and looping siren) and resets scenario alarm active state.
        /// </summary>
        public void StopAllAudio()
        {
            _isAlarmActiveScenario = false;
            if (_alarmAudioSource != null && _alarmAudioSource.isPlaying)
            {
                _alarmAudioSource.Stop();
            }
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }

        public void PlayHazardDetected() => PlaySound(FireSoundType.HazardDetected);
        public void PlayCorrectAction() => PlaySound(FireSoundType.CorrectAction);
        public void PlayIncorrectAction() => PlaySound(FireSoundType.IncorrectAction);
        public void PlayEmergencyAlarm() => PlaySound(FireSoundType.EmergencyAlarm);
        public void PlayPassAction() => PlaySound(FireSoundType.PassProcedureAction);
        public void PlayPassProcedureAction() => PlaySound(FireSoundType.PassProcedureAction);
        public void PlayWaypointReached() => PlaySound(FireSoundType.WaypointReached);
        public void PlayStepCompleted() => PlaySound(FireSoundType.StepCompleted);
        public void PlayFinalPass() => PlaySound(FireSoundType.FinalPass);
        public void PlayFinalFail() => PlaySound(FireSoundType.FinalFail);

        public int GetSoundPlayCount(FireSoundType soundType)
        {
            return _playCounts.TryGetValue(soundType, out int count) ? count : 0;
        }

        public int GetPlayCount(FireSoundType soundType) => GetSoundPlayCount(soundType);

        public void ResetPlayCounts()
        {
            _playCounts.Clear();
            LastPlayedSound = null;
        }

        private void SynthesizeAllClips()
        {
            foreach (FireSoundType soundType in Enum.GetValues(typeof(FireSoundType)))
            {
                if (!_clipCache.ContainsKey(soundType))
                {
                    var clip = SynthesizeClip(soundType);
                    if (clip != null)
                    {
                        _clipCache[soundType] = clip;
                    }
                }
            }
        }

        private AudioClip SynthesizeClip(FireSoundType soundType)
        {
            try
            {
                switch (soundType)
                {
                    case FireSoundType.HazardDetected:
                        // Short warning double beep (880 Hz, 0.20s total)
                        return GenerateWaveClip("snd_hazard_detected", 0.20f, t =>
                        {
                            if (t < 0.08f) return 0.5f * Mathf.Sin(2f * Mathf.PI * 880f * t);
                            if (t >= 0.12f && t < 0.20f) return 0.5f * Mathf.Sin(2f * Mathf.PI * 880f * t);
                            return 0f;
                        });

                    case FireSoundType.CorrectAction:
                        // Pleasant rising chime (C5 523Hz -> E5 659Hz, 0.24s)
                        return GenerateWaveClip("snd_correct_action", 0.24f, t =>
                        {
                            float freq = t < 0.12f ? 523.25f : 659.25f;
                            float fade = 1f - (t % 0.12f) / 0.12f;
                            return 0.45f * fade * Mathf.Sin(2f * Mathf.PI * freq * t);
                        });

                    case FireSoundType.IncorrectAction:
                        // Short error buzz (220 Hz low buzz, 0.18s)
                        return GenerateWaveClip("snd_incorrect_action", 0.18f, t =>
                        {
                            float saw = (t * 220f) % 1.0f - 0.5f;
                            float fade = 1f - (t / 0.18f);
                            return 0.40f * fade * saw;
                        });

                    case FireSoundType.EmergencyAlarm:
                        // 2-tone industrial siren warble (~800Hz / 1000Hz, exactly 0.8s, non-looping)
                        return GenerateWaveClip("snd_emergency_alarm", 0.80f, t =>
                        {
                            float freq = 900f + 140f * Mathf.Sin(2f * Mathf.PI * 5f * t);
                            float fade = t > 0.70f ? (0.80f - t) / 0.10f : 1f;
                            return 0.45f * fade * Mathf.Sin(2f * Mathf.PI * freq * t);
                        });

                    case FireSoundType.PassProcedureAction:
                        // Crisp subtle action confirm click/tone (620 Hz, 0.09s)
                        return GenerateWaveClip("snd_pass_action", 0.09f, t =>
                        {
                            float fade = 1f - (t / 0.09f);
                            return 0.40f * fade * Mathf.Sin(2f * Mathf.PI * 620f * t);
                        });

                    case FireSoundType.WaypointReached:
                        // Cheerful high ping (1046 Hz, 0.12s)
                        return GenerateWaveClip("snd_waypoint_reached", 0.12f, t =>
                        {
                            float fade = 1f - (t / 0.12f);
                            return 0.40f * fade * Mathf.Sin(2f * Mathf.PI * 1046.5f * t);
                        });

                    case FireSoundType.StepCompleted:
                        // Positive completion chord (523Hz -> 659Hz -> 784Hz, 0.28s)
                        return GenerateWaveClip("snd_step_completed", 0.28f, t =>
                        {
                            float freq = t < 0.09f ? 523.25f : (t < 0.18f ? 659.25f : 783.99f);
                            float localT = t < 0.09f ? t : (t < 0.18f ? t - 0.09f : t - 0.18f);
                            float fade = 1f - (localT / 0.10f);
                            return 0.45f * fade * Mathf.Sin(2f * Mathf.PI * freq * t);
                        });

                    case FireSoundType.FinalPass:
                        // Triumphant 4-tone fanfare (C5 -> E5 -> G5 -> C6, 0.40s)
                        return GenerateWaveClip("snd_final_pass", 0.40f, t =>
                        {
                            float freq = t < 0.10f ? 523.25f : (t < 0.20f ? 659.25f : (t < 0.30f ? 783.99f : 1046.50f));
                            float fade = t > 0.30f ? 1f - ((t - 0.30f) / 0.10f) : 1f;
                            return 0.50f * fade * Mathf.Sin(2f * Mathf.PI * freq * t);
                        });

                    case FireSoundType.FinalFail:
                        // Neutral/descending notification (440Hz -> 330Hz, 0.30s)
                        return GenerateWaveClip("snd_final_fail", 0.30f, t =>
                        {
                            float freq = t < 0.15f ? 440f : 330f;
                            float fade = 1f - (t / 0.30f);
                            return 0.40f * fade * Mathf.Sin(2f * Mathf.PI * freq * t);
                        });

                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FireAudioService] Synthesis failed for {soundType}: {ex.Message}");
                return null;
            }
        }

        private AudioClip GenerateWaveClip(string clipName, float durationSeconds, Func<float, float> waveFunc, int sampleRate = 22050)
        {
            int totalSamples = Mathf.Max(1, (int)(sampleRate * durationSeconds));
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                samples[i] = Mathf.Clamp(waveFunc(t), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
