// GasArInteractionController.cs
// Namespace : IndustrialSafetyAR.Modules.GasConfinedSpace
//
// Orchestrates the Gas Leak & Confined Space Safety AR interactions:
// 1. Plane raycasting to place the Confined Space Portal in the AR environment.
// 2. Physics / screen raycasting to detect worker interactions on the portal and danger perimeter.
// 3. OSHA-compliant Atmospheric multi-gas detector test sequencing (O2 -> LEL -> Toxic H2S).
// 4. Emitting domain TrainingEvents for:
//    - step_gas_recognize_hazard / rule_hazard_recognition
//    - step_gas_danger_zone / rule_danger_zone
//    - step_gas_atmospheric_test / rule_atmospheric_test
// 5. Audio and haptic feedback coordination via FireAudioService.

using System;
using IndustrialSafetyAR.AR;
using IndustrialSafetyAR.Assessment;
using IndustrialSafetyAR.Core.Audio;
using IndustrialSafetyAR.Core.Events;
using IndustrialSafetyAR.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IndustrialSafetyAR.Modules.GasConfinedSpace
{
    public enum GasInteractionState
    {
        WaitingForTracking,
        ReadyToPlace,
        HazardPlaced,
        AwaitingHazardRecognition,
        HazardRecognized,
        AwaitingDangerZone,
        DangerZoneRecognized,
        AwaitingAtmosphericTest,
        AtmosphericTestO2Completed,
        AtmosphericTestLelCompleted,
        AtmosphericTestH2sCompleted,
        AtmosphericAssessmentCompleted,
        AwaitingPpeSelection,
        PpeSelected,
        AwaitingPpeVerification,
        PpeVerified,
        AwaitingBuddySystem,
        AttendantAssigned,
        CommunicationChecked,
        StepCompleted
    }

    /// <summary>
    /// Coordinates AR surface placement, hazard recognition, danger perimeter establishing,
    /// and atmospheric detector sequencing for the Gas Leak &amp; Confined Space module.
    /// </summary>
    public class GasArInteractionController : MonoBehaviour
    {
        [Header("Dependencies")]
        [Tooltip("Facade exposing AR tracking readiness. Discovered automatically if null.")]
        [SerializeField]
        private ArSessionFacade _arSessionFacade;

        [Tooltip("AR plane raycasting service. Discovered automatically if null.")]
        [SerializeField]
        private ArPlaneRaycastService _raycastService;

        [Tooltip("Main AR Camera used for raycasting. Defaults to Camera.main if null.")]
        [SerializeField]
        private Camera _arCamera;

        [Tooltip("Optional prefab for the gas portal marker. If null, generated procedurally.")]
        [SerializeField]
        private GasHazardMarker _hazardPrefab;

        // Domain workflow state machine
        private readonly GasTrainingWorkflow _workflow = new GasTrainingWorkflow();

        // Step Navigator
        private readonly GuidedStepNavigator _stepNavigator = new GuidedStepNavigator();
        public GuidedStepNavigator StepNavigator => _stepNavigator;

        // Runtime state
        private GasInteractionState _state = GasInteractionState.WaitingForTracking;
        private GasHazardMarker _activeHazard;
        private GasAttendantMarker _activeAttendant;
        private ITrainingEventDispatcher _eventDispatcher;

        // Step 4: PPE Selection state
        private readonly System.Collections.Generic.HashSet<string> _selectedPpeItems =
            new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        // Step 5: PPE Verification state
        private bool _isSealCheckPassed;
        private bool _isHarnessFitPassed;
        private bool _isCylinderPressurePassed;

        public GasInteractionState State => _state;
        public GasWorkflowStage WorkflowStage => _workflow.CurrentStage;
        public GasTrainingWorkflow Workflow => _workflow;
        public GasHazardMarker ActiveHazard => _activeHazard;
        public GasAttendantMarker ActiveAttendant => _activeAttendant;
        public string CurrentStepId => _workflow.CurrentStepId;

        public System.Collections.Generic.IReadOnlyCollection<string> SelectedPpeItems => _selectedPpeItems;
        public bool IsSealCheckPassed => _isSealCheckPassed;
        public bool IsHarnessFitPassed => _isHarnessFitPassed;
        public bool IsCylinderPressurePassed => _isCylinderPressurePassed;
        public bool IsAttendantAssigned => _workflow.IsAttendantAssigned;
        public bool IsCommunicationChecked => _workflow.IsCommunicationChecked;

        public event Action<GasInteractionState> OnStateChanged;
        public event Action<GasHazardMarker> OnHazardPlaced;
        public event Action<GasAttendantMarker> OnAttendantPlaced;
        public event Action<TrainingEvent> OnHazardRecognized;
        public event Action<TrainingEvent> OnDangerZoneRecognized;
        public event Action<TrainingEvent> OnUnsafeZoneEntry;
        public event Action<TrainingEvent> OnAtmosphericTestStepCompleted;
        public event Action<TrainingEvent> OnAtmosphericAssessmentCompleted;
        public event Action<TrainingEvent> OnPpeSelected;
        public event Action<TrainingEvent> OnPpeSelectionIncorrect;
        public event Action<TrainingEvent> OnPpeVerified;
        public event Action<TrainingEvent> OnPpeVerificationFailed;
        public event Action<TrainingEvent> OnAttendantAssigned;
        public event Action<TrainingEvent> OnCommunicationChecked;
        public event Action<string> OnFeedbackChanged;

        private void Awake()
        {
            if (_arSessionFacade == null)
            {
                _arSessionFacade = FindAnyObjectByType<ArSessionFacade>();
            }

            if (_raycastService == null)
            {
                _raycastService = GetComponent<ArPlaneRaycastService>() ?? FindAnyObjectByType<ArPlaneRaycastService>();
                if (_raycastService == null)
                {
                    _raycastService = gameObject.AddComponent<ArPlaneRaycastService>();
                }
            }

            if (_arCamera == null)
            {
                _arCamera = Camera.main;
            }

            _stepNavigator.InitializeDefaultGasSteps();
            _workflow.StartWorkflow();
        }

        private void Start()
        {
            SetState(GasInteractionState.WaitingForTracking);
            _workflow.OnFeedbackChanged += msg => OnFeedbackChanged?.Invoke(msg);
        }

        private void Update()
        {
            // Transition from WaitingForTracking to ReadyToPlace once AR tracking is ready
            if (_state == GasInteractionState.WaitingForTracking)
            {
                if (_arSessionFacade == null || _arSessionFacade.IsTrackingAvailable)
                {
                    SetState(GasInteractionState.ReadyToPlace);
                    SetFeedback("Surfaces detected. Tap floor to place Confined Space Portal.");
                }
            }

            // Detect screen touches for placement or hazard interaction
            HandleInput();
        }

        public void SetEventDispatcher(ITrainingEventDispatcher dispatcher)
        {
            _eventDispatcher = dispatcher;
        }

        public void SetState(GasInteractionState newState)
        {
            _state = newState;
            OnStateChanged?.Invoke(_state);
        }

        public void SetFeedback(string feedback)
        {
            OnFeedbackChanged?.Invoke(feedback);
        }

        /// <summary>
        /// Instantiates or procedurally constructs the GasHazardMarker in AR space.
        /// </summary>
        public GasHazardMarker SpawnHazardMarker(Vector3 position, Quaternion rotation)
        {
            if (_activeHazard != null)
            {
                _activeHazard.transform.position = position;
                _activeHazard.transform.rotation = rotation;
                return _activeHazard;
            }

            if (_hazardPrefab != null)
            {
                _activeHazard = Instantiate(_hazardPrefab, position, rotation);
            }
            else
            {
                var go = new GameObject("ConfinedSpaceGasPortal");
                go.transform.position = position;
                go.transform.rotation = rotation;
                _activeHazard = go.AddComponent<GasHazardMarker>();
                _activeHazard.EnsureVisuals();
            }

            SetState(GasInteractionState.HazardPlaced);
            SetState(GasInteractionState.AwaitingHazardRecognition);
            OnHazardPlaced?.Invoke(_activeHazard);
            SetFeedback("Confined space portal placed. Identify the gas accumulation hazard.");
            return _activeHazard;
        }

        private void HandleInput()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                ProcessScreenTap(Input.mousePosition);
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        return;
                    }
                    ProcessScreenTap(touch.position);
                }
            }
#endif
        }

        /// <summary>
        /// Processes 2D screen coordinate input for placement or AR object interaction.
        /// </summary>
        public void ProcessScreenTap(Vector2 screenPosition)
        {
            // 1. Placement phase
            if (_state == GasInteractionState.ReadyToPlace)
            {
                if (_raycastService != null && _raycastService.TryRaycastPlane(screenPosition, out Pose hitPose))
                {
                    SpawnHazardMarker(hitPose.position, hitPose.rotation);
                    TriggerHapticLight();
                    PlayAudioCorrect();
                    return;
                }
                else if (Application.isEditor || _arSessionFacade == null || !_arSessionFacade.IsTrackingAvailable)
                {
                    // Fallback placement 1.5m ahead of camera in Editor or non-AR environment
                    Vector3 forwardPos = _arCamera != null
                        ? _arCamera.transform.position + _arCamera.transform.forward * 1.5f + Vector3.down * 0.4f
                        : new Vector3(0f, 0f, 1.5f);
                    SpawnHazardMarker(forwardPos, Quaternion.identity);
                    TriggerHapticLight();
                    PlayAudioCorrect();
                    return;
                }
            }

            // 2. Interaction phase with placed hazard
            if (_activeHazard != null)
            {
                Ray ray = _arCamera != null
                    ? _arCamera.ScreenPointToRay(screenPosition)
                    : new Ray(new Vector3(0, 1, -2), Vector3.forward);

                if (Physics.Raycast(ray, out RaycastHit hit, 20f))
                {
                    ProcessHit(hit);
                }
                else
                {
                    // If tap missed 3D collider, check proximity or handle rejection
                    ProcessMissedTap();
                }
            }
        }

        private void ProcessHit(RaycastHit hit)
        {
            var attendantMarker = hit.collider.GetComponentInParent<GasAttendantMarker>();
            if (attendantMarker != null)
            {
                if (_stepNavigator.CurrentStepIndex == 6)
                {
                    AssignAttendant();
                    return;
                }
            }

            var hazardMarker = hit.collider.GetComponentInParent<GasHazardMarker>();
            if (hazardMarker != null)
            {
                if (_stepNavigator.CurrentStepIndex == 1)
                {
                    ProcessHazardTap();
                }
                else if (_stepNavigator.CurrentStepIndex == 2)
                {
                    // Tapping directly onto the portal opening while in Step 2 is an unsafe zone entry!
                    ProcessUnsafeZoneTap();
                }
            }
            else
            {
                ProcessMissedTap();
            }
        }

        private void ProcessMissedTap()
        {
            if (_stepNavigator.CurrentStepIndex == 1 && !_activeHazard.IsHazardRecognized)
            {
                PlayAudioIncorrect();
                TriggerHapticWarning();
                SetFeedback("Tap the confined-space opening with gas accumulation to identify the hazard.");
            }
        }

        // =============================================================
        // STEP 1: RECOGNIZE GAS HAZARD
        // =============================================================
        /// <summary>
        /// Executes recognition of the gas accumulation hazard at the confined-space portal.
        /// Emits gas_hazard_recognized event on success.
        /// </summary>
        public bool ProcessHazardTap()
        {
            if (_stepNavigator.CurrentStepIndex != 1) return false;

            if (_workflow.RecognizeHazard(_eventDispatcher, out TrainingEvent emittedEvent))
            {
                if (_activeHazard != null)
                {
                    _activeHazard.AcknowledgeHazard();
                }

                SetState(GasInteractionState.HazardRecognized);
                _stepNavigator.CompleteStep(1, "✓ Gas hazard recognized. Remain clear of opening.");
                PlayAudioCorrect();
                TriggerHapticLight();
                SetFeedback("✓ Gas hazard identified! Establish a 3.0-meter danger perimeter.");
                OnHazardRecognized?.Invoke(emittedEvent);
                return true;
            }

            PlayAudioIncorrect();
            TriggerHapticWarning();
            return false;
        }

        // =============================================================
        // STEP 2: RECOGNIZE DANGER ZONE
        // =============================================================
        /// <summary>
        /// Called when the worker navigates to Step 2, revealing the 3.0m danger-zone ring.
        /// </summary>
        public void PrepareStep2DangerZone()
        {
            if (_activeHazard != null)
            {
                _activeHazard.ShowDangerZoneRing(true);
            }
            SetState(GasInteractionState.AwaitingDangerZone);
            SetFeedback("Identify the 3.0-meter safety perimeter and remain outside it.");
        }

        /// <summary>
        /// Executes recognition of the 3.0m danger-zone boundary.
        /// Emits danger_zone_recognized event on success.
        /// </summary>
        public bool ProcessDangerZonePerimeterTap()
        {
            if (_stepNavigator.CurrentStepIndex != 2) return false;

            if (_workflow.MarkDangerZone(_eventDispatcher, out TrainingEvent emittedEvent))
            {
                if (_activeHazard != null)
                {
                    _activeHazard.MarkDangerZoneEstablished();
                }

                SetState(GasInteractionState.DangerZoneRecognized);
                _stepNavigator.CompleteStep(2, "✓ 3.0m Danger perimeter marked. Standoff maintained.");
                PlayAudioCorrect();
                TriggerHapticLight();
                SetFeedback("✓ Danger perimeter established! Atmospheric testing is required before approach.");
                OnDangerZoneRecognized?.Invoke(emittedEvent);
                return true;
            }

            PlayAudioIncorrect();
            TriggerHapticWarning();
            return false;
        }

        /// <summary>
        /// Records an unsafe breach into the confined space or danger interior before testing.
        /// Emits unsafe_zone_entry event with rubric penalty (-5 points).
        /// </summary>
        public bool ProcessUnsafeZoneTap()
        {
            if (_stepNavigator.CurrentStepIndex != 2) return false;

            if (_workflow.RecordUnsafeZoneEntry(_eventDispatcher, out TrainingEvent emittedEvent))
            {
                PlayAudioIncorrect();
                TriggerHapticWarning();
                SetFeedback("DANGER: Stay outside the hazardous perimeter! Atmospheric testing required before approach.");
                OnUnsafeZoneEntry?.Invoke(emittedEvent);
                return true;
            }

            return false;
        }

        // =============================================================
        // STEP 3: ATMOSPHERIC TESTING (OSHA DETERMINISTIC SEQUENCE)
        // =============================================================
        /// <summary>
        /// Prepares the multi-gas detector for Step 3 atmospheric testing.
        /// </summary>
        public bool PrepareStep3AtmosphericTest()
        {
            SetState(GasInteractionState.AwaitingAtmosphericTest);
            SetFeedback("OSHA Sequence: Test Oxygen (O2) first, then Combustible (LEL), then Toxic (H2S).");
            return _workflow.StartAtmosphericTest(_eventDispatcher, out _);
        }

        /// <summary>
        /// Executes a single sensor test in the OSHA deterministic sequence:
        /// 1. Oxygen (O2 = 19.1%)
        /// 2. Flammable (LEL = 18.0%)
        /// 3. Toxic (H2S = 35.0 ppm)
        /// Emits atmosphere_test_step_completed on success.
        /// Emits atmosphere_assessment_completed once all three are complete.
        /// </summary>
        public bool TestSensor(GasSensorType sensorType, out string error)
        {
            error = null;
            if (_stepNavigator.CurrentStepIndex != 3)
            {
                error = "Not at atmospheric testing step.";
                return false;
            }

            if (!_workflow.AtmosphericSimulator.IsTestStarted)
            {
                _workflow.StartAtmosphericTest(_eventDispatcher, out _);
            }

            if (_workflow.TestSensor(sensorType, _eventDispatcher, out TrainingEvent stepEvent, out error))
            {
                PlayAudioCorrect();
                TriggerHapticLight();
                OnAtmosphericTestStepCompleted?.Invoke(stepEvent);

                if (sensorType == GasSensorType.Oxygen)
                {
                    SetState(GasInteractionState.AtmosphericTestO2Completed);
                    SetFeedback("✓ Oxygen tested: 19.1% Vol (DEFICIENT). Next: Test Combustible Gas (LEL).");
                }
                else if (sensorType == GasSensorType.Flammable)
                {
                    SetState(GasInteractionState.AtmosphericTestLelCompleted);
                    SetFeedback("✓ Flammable tested: 18.0% LEL (HAZARDOUS). Next: Test Toxic Contaminant (H2S).");
                }
                else if (sensorType == GasSensorType.Toxic)
                {
                    SetState(GasInteractionState.AtmosphericTestH2sCompleted);
                    SetFeedback("✓ Toxic tested: 35.0 ppm (LETHAL DANGER). All 3 sensors recorded.");
                }

                // If all 3 sensors are tested, complete assessment
                if (_workflow.AtmosphericSimulator.IsAssessmentCompleted)
                {
                    CompleteAtmosphericAssessment();
                }

                return true;
            }

            // Sequence violation or error
            PlayAudioIncorrect();
            TriggerHapticWarning();
            SetFeedback(error);
            return false;
        }

        /// <summary>
        /// Concludes atmospheric assessment, emitting atmosphere_assessment_completed
        /// with overall_status = 'unsafe' and triggering emergency alert audio.
        /// </summary>
        public bool CompleteAtmosphericAssessment()
        {
            if (_workflow.CompleteAtmosphericAssessment(_eventDispatcher, out TrainingEvent assessmentEvent))
            {
                SetState(GasInteractionState.AtmosphericAssessmentCompleted);
                _stepNavigator.CompleteStep(3, "✓ Atmosphere tested: UNSAFE conditions. DO NOT ENTER.");
                PlayEmergencyAlarmWarning();
                TriggerHapticWarning();
                SetFeedback("ATMOSPHERE: UNSAFE (Deficient O2, Flammable LEL, Toxic H2S). DO NOT ENTER!");
                OnAtmosphericAssessmentCompleted?.Invoke(assessmentEvent);
                return true;
            }

            return false;
        }

        // =============================================================
        // STEP 4: PPE SELECTION (SAFETY PROTOCOL - ENTRY PROHIBITED)
        // =============================================================
        public void PrepareStep4PpeSelection()
        {
            SetState(GasInteractionState.AwaitingPpeSelection);
            SetFeedback("ATMOSPHERE: UNSAFE. Select required PPE kit. NOTE: PPE does NOT make an unsafe atmosphere safe!");
        }

        public bool TogglePpeItem(string itemId)
        {
            if (_selectedPpeItems.Contains(itemId))
            {
                _selectedPpeItems.Remove(itemId);
                PlayAudioCorrect();
                TriggerHapticLight();
                return false;
            }
            else
            {
                _selectedPpeItems.Add(itemId);
                return SelectPpeItem(itemId);
            }
        }

        public bool SelectPpeItem(string itemId)
        {
            if (_stepNavigator.CurrentStepIndex != 4) return false;

            if (!_selectedPpeItems.Contains(itemId))
            {
                _selectedPpeItems.Add(itemId);
            }

            // Immediately catch dangerous respiratory distractors
            if (itemId == GasPpeSystem.ItemDustMask || itemId == GasPpeSystem.ItemClothMask)
            {
                _workflow.SubmitPpeSelection(_selectedPpeItems, _eventDispatcher, out TrainingEvent failureEvent, out string rejectionMsg);
                PlayAudioIncorrect();
                TriggerHapticWarning();
                SetFeedback(rejectionMsg ?? "CRITICAL: Dust and surgical masks provide ZERO protection against toxic gas or oxygen deficiency!");
                if (failureEvent != null)
                {
                    OnPpeSelectionIncorrect?.Invoke(failureEvent);
                }
                return false;
            }

            PlayAudioCorrect();
            TriggerHapticLight();
            return true;
        }

        public bool IsPpeItemSelected(string itemId)
        {
            return _selectedPpeItems.Contains(itemId);
        }

        public bool SubmitPpeSelection()
        {
            if (_stepNavigator.CurrentStepIndex != 4) return false;

            if (_workflow.SubmitPpeSelection(_selectedPpeItems, _eventDispatcher, out TrainingEvent emittedEvent, out string feedback))
            {
                SetState(GasInteractionState.PpeSelected);
                _stepNavigator.CompleteStep(4, "✓ Complete PPE kit selected. Next: Inspect and verify PPE.");
                PlayAudioCorrect();
                TriggerHapticLight();
                SetFeedback(feedback);
                OnPpeSelected?.Invoke(emittedEvent);
                return true;
            }
            else
            {
                PlayAudioIncorrect();
                TriggerHapticWarning();
                SetFeedback(feedback ?? "Incomplete selection: Safety Helmet, Harness, Gloves, Boots, and SCBA required.");
                if (emittedEvent != null && emittedEvent.EventType == "ppe_selection_incorrect")
                {
                    OnPpeSelectionIncorrect?.Invoke(emittedEvent);
                }
                return false;
            }
        }

        // =============================================================
        // STEP 5: PPE VERIFICATION (EQUIPMENT INTEGRITY CHECKS)
        // =============================================================
        public void PrepareStep5PpeVerification()
        {
            SetState(GasInteractionState.AwaitingPpeVerification);
            SetFeedback("Verify PPE equipment readiness: Face seal check, harness inspection, and cylinder pressure.");
        }

        public bool VerifyScbaSeal()
        {
            if (_stepNavigator.CurrentStepIndex != 5) return false;
            _isSealCheckPassed = true;
            PlayAudioCorrect();
            TriggerHapticLight();
            SetFeedback("✓ SCBA Face seal verified: Positive-pressure hermetic seal intact.");
            CheckAutoPpeVerification();
            return true;
        }

        public bool VerifyHarnessFit()
        {
            if (_stepNavigator.CurrentStepIndex != 5) return false;
            _isHarnessFitPassed = true;
            PlayAudioCorrect();
            TriggerHapticLight();
            SetFeedback("✓ Harness fit verified: Straps, buckles, and dorsal D-ring fully inspected.");
            CheckAutoPpeVerification();
            return true;
        }

        public bool CheckCylinderPressure()
        {
            if (_stepNavigator.CurrentStepIndex != 5) return false;
            _isCylinderPressurePassed = true;
            PlayAudioCorrect();
            TriggerHapticLight();
            SetFeedback("✓ Cylinder pressure verified: 300 Bar (4500 PSI) full charge confirmed.");
            CheckAutoPpeVerification();
            return true;
        }

        private void CheckAutoPpeVerification()
        {
            if (_isSealCheckPassed && _isHarnessFitPassed && _isCylinderPressurePassed)
            {
                SubmitPpeVerification();
            }
        }

        public bool SubmitPpeVerification(bool submitPrematurely = false)
        {
            if (_stepNavigator.CurrentStepIndex != 5) return false;

            if (_workflow.VerifyPpe(_isSealCheckPassed, _isHarnessFitPassed, _isCylinderPressurePassed, _eventDispatcher, out TrainingEvent emittedEvent, out string feedback))
            {
                SetState(GasInteractionState.PpeVerified);
                _stepNavigator.CompleteStep(5, "✓ PPE verified: SCBA seal, harness, cylinder confirmed.");
                PlayAudioCorrect();
                TriggerHapticLight();
                SetFeedback(feedback);
                OnPpeVerified?.Invoke(emittedEvent);
                return true;
            }
            else
            {
                PlayAudioIncorrect();
                TriggerHapticWarning();
                SetFeedback(feedback ?? "PPE verification failed: Complete all 3 verification checks before proceeding.");
                if (emittedEvent != null)
                {
                    OnPpeVerificationFailed?.Invoke(emittedEvent);
                }
                return false;
            }
        }

        // =============================================================
        // STEP 6: BUDDY / ATTENDANT SYSTEM (STAYS OUTSIDE)
        // =============================================================
        public bool PrepareStep6BuddySystem()
        {
            SetState(GasInteractionState.AwaitingBuddySystem);
            SetFeedback("RULE: Outside attendant must remain OUTSIDE the confined space. Assign attendant & check radio.");

            if (_activeAttendant == null && _activeHazard != null)
            {
                Vector3 forwardDir = _activeHazard.transform.forward;
                if (forwardDir == Vector3.zero) forwardDir = Vector3.forward;
                Vector3 spawnPos = _activeHazard.transform.position + forwardDir * 3.4f;
                SpawnAttendantMarker(spawnPos, Quaternion.LookRotation(-forwardDir));
            }
            return true;
        }

        public GasAttendantMarker SpawnAttendantMarker(Vector3 position, Quaternion rotation)
        {
            if (_activeAttendant != null)
            {
                _activeAttendant.transform.position = position;
                _activeAttendant.transform.rotation = rotation;
                return _activeAttendant;
            }

            var go = new GameObject("OutsideSafetyAttendant");
            go.transform.position = position;
            go.transform.rotation = rotation;
            _activeAttendant = go.AddComponent<GasAttendantMarker>();
            _activeAttendant.EnsureVisuals();
            _activeAttendant.OnAttendantTapped += _ => AssignAttendant();
            OnAttendantPlaced?.Invoke(_activeAttendant);
            return _activeAttendant;
        }

        public bool AssignAttendant(string attendantId = "attendant_guard_outside")
        {
            if (_stepNavigator.CurrentStepIndex != 6) return false;

            // Validate attendant position is strictly outside danger zone (>= 3.0m)
            if (_activeAttendant != null && _activeHazard != null)
            {
                if (!_activeAttendant.IsPositionOutsideDangerZone(_activeHazard.transform.position, 3.0f))
                {
                    SetFeedback("CRITICAL ERROR: Attendant must remain OUTSIDE the 3.0m danger perimeter!");
                    PlayAudioIncorrect();
                    TriggerHapticWarning();
                    return false;
                }
            }

            if (_workflow.AssignAttendant(attendantId, _eventDispatcher, out TrainingEvent emittedEvent))
            {
                if (_activeAttendant != null)
                {
                    _activeAttendant.AcknowledgeAssigned();
                }

                SetState(GasInteractionState.AttendantAssigned);
                PlayAudioCorrect();
                TriggerHapticLight();
                SetFeedback("✓ Attendant assigned OUTSIDE the confined space. Now verify two-way radio communication.");
                OnAttendantAssigned?.Invoke(emittedEvent);
                return true;
            }

            PlayAudioIncorrect();
            TriggerHapticWarning();
            return false;
        }

        public bool CheckCommunication(string protocol = "intrinsically_safe_two_way_radio")
        {
            if (_stepNavigator.CurrentStepIndex != 6) return false;

            if (!_workflow.IsAttendantAssigned)
            {
                SetFeedback("Assign outside attendant before testing radio communication.");
                PlayAudioIncorrect();
                TriggerHapticWarning();
                return false;
            }

            if (_workflow.CheckCommunication(protocol, _eventDispatcher, out TrainingEvent emittedEvent))
            {
                if (_activeAttendant != null)
                {
                    _activeAttendant.AcknowledgeCommunicationVerified();
                }

                SetState(GasInteractionState.CommunicationChecked);
                _stepNavigator.CompleteStep(6, "✓ Attendant assigned outside. Radio communication verified.");
                PlayRadioCommunicationAudio();
                TriggerHapticLight();
                SetFeedback("✓ Communication verified: Two-way intrinsically safe radio link active.");
                OnCommunicationChecked?.Invoke(emittedEvent);
                return true;
            }

            PlayAudioIncorrect();
            TriggerHapticWarning();
            return false;
        }

        // =============================================================
        // RETAKE / RESET
        // =============================================================
        public void ResetScenario()
        {
            _selectedPpeItems.Clear();
            _isSealCheckPassed = false;
            _isHarnessFitPassed = false;
            _isCylinderPressurePassed = false;

            if (_activeAttendant != null)
            {
                _activeAttendant.ResetMarker();
            }

            if (_activeHazard != null)
            {
                _activeHazard.ResetMarker();
            }

            _workflow.ResetWorkflow();
            _stepNavigator.ResetToStep(1);

            SetState(GasInteractionState.HazardPlaced);
            SetState(GasInteractionState.AwaitingHazardRecognition);
            SetFeedback("Training reset. Identify the gas accumulation hazard at the opening.");
        }

        // =============================================================
        // STEP NAVIGATION HOOKS
        // =============================================================
        public void AdvanceToNextStep()
        {
            if (_stepNavigator.CanGoNext)
            {
                int nextStep = _stepNavigator.CurrentStepIndex + 1;
                _stepNavigator.SetViewStep(nextStep);

                if (nextStep == 2)
                {
                    PrepareStep2DangerZone();
                }
                else if (nextStep == 3)
                {
                    PrepareStep3AtmosphericTest();
                }
                else if (nextStep == 4)
                {
                    PrepareStep4PpeSelection();
                }
                else if (nextStep == 5)
                {
                    PrepareStep5PpeVerification();
                }
                else if (nextStep == 6)
                {
                    PrepareStep6BuddySystem();
                }
            }
        }

        public void ReturnToPreviousStep()
        {
            if (_stepNavigator.CanGoBack)
            {
                int prevStep = _stepNavigator.CurrentStepIndex - 1;
                _stepNavigator.SetViewStep(prevStep);
            }
        }

        // =============================================================
        // AUDIO & HAPTIC HELPERS
        // =============================================================
        private void PlayAudioCorrect()
        {
            if (FireAudioService.Instance != null)
            {
                FireAudioService.Instance.PlayCorrectAction();
            }
        }

        private void PlayAudioIncorrect()
        {
            if (FireAudioService.Instance != null)
            {
                FireAudioService.Instance.PlayIncorrectAction();
            }
        }

        private void PlayEmergencyAlarmWarning()
        {
            if (FireAudioService.Instance != null)
            {
                FireAudioService.Instance.PlayEmergencyAlarm();
            }
        }

        private void PlayRadioCommunicationAudio()
        {
            if (FireAudioService.Instance != null)
            {
                FireAudioService.Instance.PlaySound(FireSoundType.PassProcedureAction);
            }
        }

        private void TriggerHapticLight()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { Handheld.Vibrate(); } catch {}
#endif
        }

        private void TriggerHapticWarning()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { Handheld.Vibrate(); } catch {}
#endif
        }
    }
}
