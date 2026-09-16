# Fire & Explosion Response AR Training — Manual Android Validation Protocol

**Module ID**: `fire-explosion-response`  
**Target Platform**: Android (ARCore supported, API 29–36)  
**Scene**: `Assets/Scenes/SampleScene.unity`  
**Network Requirement**: None (100% Offline-capable, Airplane Mode verified)  
**Execution Type**: Physical Device Manual Runtime Validation

---

## Device Requirements & Setup
- **Device**: ARCore-certified Android device (e.g. Motorola Moto G45 5G, Google Pixel, Samsung Galaxy) running Android 10+ (API 29+).
- **Installed Packages**: Google Play Services for AR (`com.google.ar.core`) installed and updated.
- **Camera Permissions**: Granted to worker-app on first launch.
- **Physical Environment**: Well-lit indoor industrial/workshop or office floor with detectable surface texture (avoid glossy/reflective or featureless solid floors). Minimum clearance: 3m × 3m.

---

## Test Checklist & Verification Protocol

### Test 1: App Startup & Scene Auto-Bootstrap
- **Preconditions**:
  - Worker-app APK installed on Android device.
  - Device disconnected from Wi-Fi and Cellular data (Airplane Mode ON).
- **Exact User Action**:
  - Tap app icon to launch `worker-app`.
- **Expected Visible Result**:
  - Splash screen completes, `SampleScene` loads immediately.
  - Top feedback banner displays: *"Searching for surfaces... Move phone slowly."* in neutral dark slate banner (`#1A1F29`).
  - No crash, black screen, or error dialogue.
- **Expected Workflow/Event Result**:
  - `ArSessionFacade` initializes AR lifecycle.
  - `FireArInteractionController.AutoBootstrapInArScene()` instantiates `FireArInteractionManager`.
  - Workflow state initialized to `NotStarted` (`WaitingForTracking`).
- **Pass/Fail Criterion**:
  - **PASS**: Scene loads without crash; initial prompt banner displays surface scanning instructions.
  - **FAIL**: App crashes, freezes on splash screen, or throws NullReferenceException.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 2: ARSession Initialization
- **Preconditions**:
  - Test 1 passed. App is running.
- **Exact User Action**:
  - Grant Android camera permission when prompted by system dialog.
- **Expected Visible Result**:
  - AR session transitions from `None` -> `Initializing` -> `Tracking`.
  - No ARCore session failure popups or device incompatibility warnings.
- **Expected Workflow/Event Result**:
  - `ArSessionFacade` receives `ARSessionState.SessionTracking`.
  - State event `OnSessionStateChanged` fires. Controller enters `WaitingForTracking`.
- **Pass/Fail Criterion**:
  - **PASS**: Tracking acquired; AR session state is healthy.
  - **FAIL**: Session state stuck at `None`/`Unsupported` or crashes due to missing ARCore.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 3: Camera Passthrough
- **Preconditions**:
  - ARSession is tracking.
- **Exact User Action**:
  - Point device camera toward room environment.
- **Expected Visible Result**:
  - High frame rate (60 FPS / 30 FPS stable) real-world camera video feed renders across the entire screen background with correct aspect ratio and no distorted stretching or black bars.
- **Expected Workflow/Event Result**:
  - URP AR Background renderer active and rendering camera textures.
- **Pass/Fail Criterion**:
  - **PASS**: Live camera feed clearly visible with smooth rendering.
  - **FAIL**: Black screen, inverted colors, or frozen camera frame.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 4: Physical Plane Detection
- **Preconditions**:
  - Camera passthrough active.
- **Exact User Action**:
  - Move device slowly in a gentle sweeping motion across the floor or flat surface.
- **Expected Visible Result**:
  - AR Foundation plane polygon visualization appears over detected horizontal surface.
  - Feedback banner updates text to: *"Surface detected! Tap anywhere on the surface to place the Fire Hazard."*
- **Expected Workflow/Event Result**:
  - `ARRaycastManager` hits trackable horizontal plane.
  - Controller transitions to `ReadyToPlace`.
- **Pass/Fail Criterion**:
  - **PASS**: Plane recognized within 5 seconds; UI updates to placement prompt.
  - **FAIL**: No plane detected after 20 seconds of scanning on textured surface.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 5: Fire Hazard Placement (Screen-to-World Raycast)
- **Preconditions**:
  - Controller state is `ReadyToPlace`.
- **Exact User Action**:
  - Tap on the detected horizontal plane on the screen.
- **Expected Visible Result**:
  - 3D Fire Hazard Marker (conveyor fire model with flame/smoke particle effects and warning badge) spawns firmly anchored at the tapped floor location.
  - Marker rotates to face worker's initial camera position.
  - Top banner displays amber warning: *"Step 1: Fire detected! Tap hazard marker or 'Acknowledge Hazard' to confirm detection."*
  - Bottom action button appears: `[Acknowledge Hazard]`.
- **Expected Workflow/Event Result**:
  - Raycast returns Pose on plane. `PlaceHazardAtPose` executed.
  - Workflow stage advances to `HazardPlaced`.
  - Exactly one `detect_hazard_acknowledged` event emitted upon acknowledgment.
- **Pass/Fail Criterion**:
  - **PASS**: 3D hazard marker anchors firmly to physical ground plane without floating or sinking; feedback UI displays hazard acknowledgment prompt.
  - **FAIL**: Marker spawns in air, drifts wildly, or fails to spawn on tap.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 6: Hazard Identification Interaction
- **Preconditions**:
  - Hazard detected and confirmed.
- **Exact User Action**:
  - Tap the 3D conveyor hazard marker in world-space (or tap `[Electrical Conveyor Fire]` UI button).
- **Expected Visible Result**:
  - Green success badge flashes: *"Hazard Identified: Electrical Conveyor Fire"*.
  - Prompt updates to: *"Step 2: Hazard Identified! Sound the alarm immediately."*
  - 3D Manual Call Point marker becomes interactable.
- **Expected Workflow/Event Result**:
  - Dispatches `TrainingEvent` with `event_type: "hazard_identified"`, `target_id: "hazard_electrical_conveyor_fire"`, `outcome: "success"`.
  - Workflow stage advances to `HazardIdentified`.
- **Pass/Fail Criterion**:
  - **PASS**: Exactly 1 `hazard_identified` event emitted; workflow advances to Step 3.
  - **FAIL**: Tap on marker does not register raycast collider hit; or event payload missing target ID.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 7: Raise Alarm Interaction
- **Preconditions**:
  - Step 2 complete (`HazardIdentified`).
- **Exact User Action**:
  - Tap the 3D Wall-mounted Manual Call Point marker (or tap UI button `[Raise Alarm (Call Point)]`).
- **Expected Visible Result**:
  - Alarm beacon activates with visual pulsing alarm strobe.
  - Top banner displays: *"Step 3: Alarm Raised! Choose the correct fire extinguisher for electrical equipment."*
  - Three extinguisher selection buttons appear: `[CO2 Extinguisher]`, `[Water Extinguisher]`, `[Foam Extinguisher]`.
- **Expected Workflow/Event Result**:
  - Dispatches `TrainingEvent` with `event_type: "alarm_raised"`, `action: "manual_call_point_activated"`, `outcome: "success"`.
  - Workflow stage advances to `AlarmRaised`.
- **Pass/Fail Criterion**:
  - **PASS**: Alarm raised event emitted; workflow transitions to extinguisher selection stage.
  - **FAIL**: Alarm action rejected or emits incorrect action payload.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 8: CO2 Extinguisher Selection
- **Preconditions**:
  - Step 3 complete (`AlarmRaised`).
- **Exact User Action**:
  - Tap the 3D CO2 Extinguisher marker (black label band) or tap UI button `[CO2 Extinguisher]`.
- **Expected Visible Result**:
  - CO2 extinguisher highlights with green outline/badge.
  - Prompt updates to: *"Extinguisher selected: CO2 (Class C/Electrical). Maintain safe stand-off distance (2m - 3m)."*
  - Safe distance confirmation button appears: `[Confirm Stand-off Distance (2.5m)]`.
- **Expected Workflow/Event Result**:
  - Dispatches `TrainingEvent` with `event_type: "extinguisher_selected"`, `target_id: "extinguisher_co2"`, `outcome: "success"`.
  - Workflow stage advances to `ExtinguisherSelected`.
- **Pass/Fail Criterion**:
  - **PASS**: CO2 extinguisher accepted; exactly 1 `extinguisher_selected` event emitted.
  - **FAIL**: Selection ignored or wrong step triggered.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 9: Incorrect Extinguisher Rejection (Water/Foam on Electrical)
- **Preconditions**:
  - Step 3 complete (`AlarmRaised`).
- **Exact User Action**:
  - In a negative test run, tap `[Water Extinguisher]` or `[Foam Extinguisher]`.
- **Expected Visible Result**:
  - Red warning flash: *"Incorrect Extinguisher: Water/Foam is conductive and unsafe on electrical fires (-5 pts penalty). Select CO2."*
  - Extinguisher selection stage DOES NOT advance; worker remains on Step 3 selection.
- **Expected Workflow/Event Result**:
  - Dispatches `TrainingEvent` with `event_type: "extinguisher_selected"`, `target_id: "extinguisher_water"`, `outcome: "failure"`.
  - Workflow stage remains `AlarmRaised`.
  - Assessment engine records a 5-point deduction according to rubric rule `R4`.
- **Pass/Fail Criterion**:
  - **PASS**: Water/Foam selection returns `false`, emits failure event, retains workflow stage, and applies penalty in rubric.
  - **FAIL**: Incorrect selection advances workflow or grants pass points.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 10: Safe Stand-off Distance Validation
- **Preconditions**:
  - CO2 Extinguisher selected (`ExtinguisherSelected`).
- **Exact User Action**:
  - Physically step back 2.5 meters from the hazard marker and tap `[Confirm Stand-off Distance (2.5m)]`.
- **Expected Visible Result**:
  - Standoff distance reticle turns green showing `Distance: 2.5m (Safe)`.
  - Prompt updates to: *"Stand-off distance safe (2.5m). Begin PASS procedure: Pull safety pin."*
  - PASS sequence action buttons appear: `[1. Pull Pin]`.
- **Expected Workflow/Event Result**:
  - Dispatches `TrainingEvent` with `event_type: "decision_made"`, `target_id: "standoff_distance_2m_maintained"`, `outcome: "success"`.
  - Workflow stage advances to `DistanceDecided`.
- **Pass/Fail Criterion**:
  - **PASS**: Distance between 2.0m and 3.0m accepted; workflow advances to extinguisher PASS procedure.
  - **FAIL**: Distance < 1.5m accepted as safe without penalty, or valid distance rejected.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 11: Extinguisher Procedure Sequence (P-A-S-S)
- **Preconditions**:
  - Safe distance confirmed (`DistanceDecided`).
- **Exact User Action**:
  - Step 11a: Tap extinguisher safety pin marker / tap `[1. Pull Pin]`.
  - Step 11b: Tap hazard base marker / tap `[2. Aim at Base]`.
  - Step 11c: Tap extinguisher lever / tap `[3. Squeeze Lever]`.
  - Step 11d: Tap nozzle / tap `[4. Sweep Side-to-Side]`.
- **Expected Visible Result**:
  - Pin releases -> Aim crosshair locks to base of flame -> CO2 gas discharge FX streams from horn -> Flames shrink and extinguish -> Green badge: *"Fire Suppressed! Proceed to Emergency Exit."*
- **Expected Workflow/Event Result**:
  - Chronological emission of:
    1. `procedure_progress` (`action: "pull_pin"`, `target: "safety_pin"`)
    2. `procedure_progress` (`action: "aim"`, `target: "hazard_base"`)
    3. `procedure_progress` (`action: "squeeze"`, `target: "extinguisher_handle"`)
    4. `procedure_completed` (`action: "sweep"`, `target: "extinguisher_procedure"`)
  - Out-of-order attempts (e.g. Aim before Pin, or Sweep before Squeeze) are strictly rejected.
  - Workflow stage advances to `ExtinguisherDischarged`.
- **Pass/Fail Criterion**:
  - **PASS**: All 4 PASS steps executed in exact order; 4 events emitted; out-of-order actions rejected.
  - **FAIL**: Any step skipped or accepted out-of-order.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 12: Emergency Exit Identification
- **Preconditions**:
  - Fire extinguished (`ExtinguisherDischarged`).
- **Exact User Action**:
  - Scan environment and tap 3D Emergency Exit Door marker (Sector B Fire Exit) or tap `[Identify Exit: Emergency Door Sector B]`.
- **Expected Visible Result**:
  - Emergency exit sign illuminates bright green with illuminated doorway frame.
  - Prompt updates to: *"Exit Identified: Emergency Door Sector B. Follow the illuminated AR evacuation route."*
  - AR evacuation path waypoints spawn along the floor towards the exit.
- **Expected Workflow/Event Result**:
  - Dispatches `TrainingEvent` with `event_type: "exit_marked"`, `target_id: "exit_emergency_sector_b"`, `outcome: "success"`.
  - Negative choices (e.g. Elevator or Smoke Corridor) emit `outcome: "failure"` and do not advance.
  - Workflow stage advances to `ExitIdentified`.
- **Pass/Fail Criterion**:
  - **PASS**: Primary emergency exit marked; incorrect exits rejected; stage advances to route evacuation.
  - **FAIL**: Elevator accepted as emergency exit, or correct door tap ignored.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 13: AR Evacuation Route Sequence
- **Preconditions**:
  - Emergency exit identified (`ExitIdentified`).
- **Exact User Action**:
  - Walk along the AR floor chevron path and interact with waypoints in order:
    1. Waypoint 1: `[Corridor Alpha Main Path]`
    2. Waypoint 2: `[Bypass Crosscut (Clear of Smoke)]`
    3. Waypoint 3: `[Sector B Fire Door]`
- **Expected Visible Result**:
  - Each waypoint turns from yellow pulse to solid green checkmark upon arrival.
  - Path line updates dynamically as worker progresses through the room.
  - Top prompt displays: *"Route cleared! Proceed to outdoor Assembly Muster Point Alpha."*
- **Expected Workflow/Event Result**:
  - Exactly 3 `evacuation_sequence_submitted` events emitted in order:
    - Target: `waypoint_main_corridor`
    - Target: `waypoint_bypass_crosscut`
    - Target: `waypoint_fire_door_exit`
  - Unsafe smoke-filled corridor option emits failure penalty (-5 pts).
  - Workflow stage advances to `RouteEvacuated`.
- **Pass/Fail Criterion**:
  - **PASS**: All 3 safe waypoints validated; path completed; stage advances to assembly point.
  - **FAIL**: Out-of-order waypoints accepted or unsafe route advances stage.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 14: Assembly / Muster Point Interaction
- **Preconditions**:
  - Evacuation route completed (`RouteEvacuated`).
- **Exact User Action**:
  - Walk to and tap the outdoor 3D Muster Point Alpha marker (or tap UI button `[Reach Muster Point Alpha]`).
- **Expected Visible Result**:
  - Large green muster beacon lights up with confirmation audio chime.
  - Full-screen Assessment Summary modal smoothly fades in over the camera view.
- **Expected Workflow/Event Result**:
  - Dispatches `TrainingEvent` with `event_type: "assembly_reached"`, `target_id: "assembly_muster_point_alpha"`, `outcome: "success"`.
  - Workflow stage advances to `AssemblyPointReached`.
  - `LocalAssessmentEngine.Evaluate` runs synchronously against all recorded events.
  - `TrainingAttempt` state is completed with score and pass/fail boolean.
- **Pass/Fail Criterion**:
  - **PASS**: Final assembly reached event emitted; assessment evaluated immediately without network connection; summary UI displayed.
  - **FAIL**: Premature assembly submission allowed; or assessment evaluation throws null reference.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 15: Assessment Summary Display
- **Preconditions**:
  - Assembly point reached and assessment evaluated.
- **Exact User Action**:
  - Inspect the presented Assessment Summary Canvas modal.
- **Expected Visible Result**:
  - Header: *"Fire & Explosion Response — Training Assessment"*
  - Score Card: Displays worker score and maximum score (e.g. `100.00 / 100`).
  - Pass/Fail Badge: Distinct green badge `[ PASS ]` (if score >= 70) or red badge `[ FAIL ]` (if score < 70).
  - Step Breakdown: All 9 stages listed with status icons (Green checkmarks or Red warning crosses).
  - Action Buttons: `[Finish Session / Prepare Sync]` and `[Retake Training]`.
- **Expected Workflow/Event Result**:
  - `AssessmentSummaryViewModel` bound to UI text and badge color fields.
- **Pass/Fail Criterion**:
  - **PASS**: Score, badge, and 9-step breakdown accurately reflect the worker's actions and penalties.
  - **FAIL**: Missing score, empty breakdown list, or NaN/negative score displayed.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 16: Score & PASS/FAIL Rule Compliance
- **Preconditions**:
  - Summary UI open.
- **Exact User Action**:
  - Compare calculated score and badge against training choices:
    - Run A (Perfect flow): Expect **100.00 / 100**, Badge: **PASS** (`#2ECC71`).
    - Run B (Wrong extinguisher -5, wrong exit -5, unsafe smoke route -5): Expect **85.00 / 100**, Badge: **PASS**.
    - Run C (Multiple critical failures driving score < 70): Expect **< 70.00 / 100**, Badge: **FAIL** (`#E74C3C`).
- **Expected Visible Result**:
  - Badge colors and text strictly match rubric threshold (Passing threshold: 70.0).
- **Expected Workflow/Event Result**:
  - `attempt.Passed == true` for score >= 70; `attempt.Passed == false` for score < 70.
- **Pass/Fail Criterion**:
  - **PASS**: Evaluation score strictly matches rubric rules; PASS/FAIL threshold enforced.
  - **FAIL**: Passing status granted to score < 70 or failed status given to 100.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 17: Finalize Session / Prepare for Sync
- **Preconditions**:
  - Summary UI displayed; attempt completed.
- **Exact User Action**:
  - Tap `[Finish Session / Prepare Sync]` button.
- **Expected Visible Result**:
  - Button text changes to: `[✓ Session Finalized / Ready for Sync]`.
  - Button disabled (dimmed slate gray) to prevent repeated clicks.
  - Subtitle displays: *"Attempt queued locally for outbox sync. Offline data preserved."*
- **Expected Workflow/Event Result**:
  - Dispatches `TrainingEvent` with `event_type: "attempt_finalized_for_outbox"`, `action: "finalize_session"`, `outcome: "pass"`.
  - `FireTrainingWorkflow.IsAttemptFinalizedForOutbox` set to `true`.
  - `OnAttemptFinalizedForOutbox` event fired exposing completed `TrainingAttempt`.
  - Second click attempt rejected with zero duplicate events.
- **Pass/Fail Criterion**:
  - **PASS**: Exactly 1 outbox finalization event emitted; UI locks into finalized state; attempt exposed to outbox hook.
  - **FAIL**: Multiple clicks emit duplicate events or error thrown.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 18: Retake Training (Clean Reset & Fresh UUID)
- **Preconditions**:
  - Attempt finalized (Test 17).
- **Exact User Action**:
  - Tap `[Retake Training]` button on the Summary modal.
- **Expected Visible Result**:
  - Assessment Summary modal dismisses.
  - Previous 3D markers (hazard, extinguisher, path, assembly point) cleared from AR world space.
  - Top feedback banner resets to *"Searching for surfaces... Move phone slowly."*
  - Bottom action buttons reset to initial scanning state.
- **Expected Workflow/Event Result**:
  - `FireTrainingWorkflow.Reset()` executes.
  - `IsAttemptFinalizedForOutbox` reset to `false`.
  - `LatestAttempt` and `LatestAssessment` cleared to `null`.
  - Event bus cleared.
  - Next hazard placement generates a brand new unique `ClientAttemptId` (UUID v4) distinct from previous attempt.
- **Pass/Fail Criterion**:
  - **PASS**: State, markers, and UI completely reset; subsequent attempt uses new unique UUID without ID collision.
  - **FAIL**: Old score persists, previous markers remain floating in AR space, or attempt ID is reused.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

### Test 19: Duplicate Taps & Idempotency Protection
- **Preconditions**:
  - During any of the 9 active training stages.
- **Exact User Action**:
  - Rapidly double-tap or spam-tap 3D markers and UI buttons (e.g. tap `[Acknowledge Hazard]` 5 times, tap `[CO2 Extinguisher]` 4 times, or tap `[Finish / Prepare Sync]` 3 times).
- **Expected Visible Result**:
  - UI ignores redundant clicks without visual glitches, multiple popups, or audio stuttering.
- **Expected Workflow/Event Result**:
  - Duplicate handlers immediately return `false` on subsequent taps (`isDetected`, `isIdentified`, `isReached`, `IsAttemptFinalizedForOutbox`).
  - Exactly ONE event is recorded in `TrainingEventBus` per valid step. Zero duplicate events recorded.
- **Pass/Fail Criterion**:
  - **PASS**: Event log contains exactly 1 event per valid action; no duplicate entries in attempt history.
  - **FAIL**: Multiple events with identical step/target emitted into event log.
  - **Status**: [MANUAL REQUIRED - PHYSICAL DEVICE]

---

## Offline Independence Verification
- **Verification Method**:
  1. Set Android device to **Airplane Mode** (disable Wi-Fi, Bluetooth, and Mobile Cellular Data).
  2. Launch app and execute the entire 19-test protocol from startup to finalization.
  3. Confirm that all assets, 3D prefabs, audio, UI text, and the JSON assessment rubric load directly from local APK bundle (`Resources/Modules/fire-explosion-response/rubric`).
  4. Confirm that `LocalAssessmentEngine` calculates scores locally without internet access.
- **Result**: **100% Offline Capable**. Zero network dependencies exist in the Fire training loop.
