// TouchGestureFilter.cs
// Namespace : IndustrialSafetyAR.Modules.FireExplosion
//
// Provides robust, device-independent discrimination between intentional screen taps
// and swipes/drags/touch movement. Ensures accidental gestures never trigger AR actions
// or advance the safety training workflow.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IndustrialSafetyAR.Modules.FireExplosion
{
    /// <summary>
    /// Tracks touch and pointer lifecycles to guarantee that only stationary, deliberate taps
    /// within a strict movement distance threshold and time window register as interactions.
    /// Rejects swipes, drags, long presses, and touches originating over UI.
    /// </summary>
    public class TouchGestureFilter
    {
        public const float DefaultMaxTapDurationSeconds = 0.45f;

        private bool _isTrackingTouch;
        private int _trackingFingerId = -1;
        private Vector2 _touchStartPos;
        private float _touchStartTime;
        private float _touchMaxMovement;
        private bool _touchStartedOverUI;

        private bool _isTrackingMouse;
        private Vector2 _mouseStartPos;
        private float _mouseStartTime;
        private float _mouseMaxMovement;
        private bool _mouseStartedOverUI;

        /// <summary>
        /// Calculates the maximum allowed screen displacement (in pixels) for an intentional tap,
        /// dynamically scaled against the device display DPI (~3.8mm of physical movement).
        /// </summary>
        public static float GetTapMovementThreshold()
        {
            float dpi = Screen.dpi;
            if (dpi <= 0f) dpi = 160f; // Default baseline Android dpi
            float threshold = dpi * 0.15f; // ~3.8mm
            return Mathf.Clamp(threshold, 20f, 45f);
        }

        /// <summary>
        /// Pure evaluation helper for validating tap vs. swipe gesture parameters.
        /// Useful for unit testing and deterministic verification.
        /// </summary>
        public static bool EvaluateTapParameters(
            Vector2 startPosition,
            Vector2 endPosition,
            float maxDisplacementDuringGesture,
            float durationSeconds,
            bool startedOverUI,
            bool endedOverUI,
            float maxMovementThreshold = -1f,
            float maxDuration = DefaultMaxTapDurationSeconds)
        {
            if (startedOverUI || endedOverUI)
            {
                return false;
            }

            if (durationSeconds <= 0f || durationSeconds > maxDuration)
            {
                return false;
            }

            float threshold = maxMovementThreshold > 0f ? maxMovementThreshold : GetTapMovementThreshold();
            float netDisplacement = Vector2.Distance(startPosition, endPosition);

            if (netDisplacement > threshold)
            {
                return false;
            }

            if (maxDisplacementDuringGesture > threshold)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Authoritatively checks whether a given screen coordinate lies over an interactive UI element,
        /// performing direct PointerEventData GraphicRaycasts across active UI canvases.
        /// </summary>
        public static bool IsPointerOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;

            // 1. Check legacy/input system pointer ID if active
            try
            {
                if (EventSystem.current.IsPointerOverGameObject()) return true;

#if !ENABLE_INPUT_SYSTEM
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                    {
                        return true;
                    }
                }
#endif
            }
            catch (InvalidOperationException) {}

            // 2. Full graphic raycast on the coordinate
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            return results.Count > 0;
        }

        /// <summary>
        /// Polls current device input (mobile touch or mouse) and returns true only on the frame
        /// where a verified intentional tap concludes (on finger lift / button release).
        /// </summary>
        /// <param name="tapPosition">The validated screen position where the tap occurred.</param>
        /// <returns>True if an intentional tap completed this frame; false if swiping, dragging, or idle.</returns>
        public bool PollIntentionalTap(out Vector2 tapPosition)
        {
            tapPosition = Vector2.zero;
            float threshold = GetTapMovementThreshold();
            float now = Time.unscaledTime;

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                var primary = Touchscreen.current.primaryTouch;
                if (primary.press.wasPressedThisFrame)
                {
                    _isTrackingTouch = true;
                    _touchStartPos = primary.position.ReadValue();
                    _touchStartTime = now;
                    _touchMaxMovement = 0f;
                    _touchStartedOverUI = IsPointerOverUI(_touchStartPos);
                }
                else if (_isTrackingTouch && primary.press.isPressed)
                {
                    Vector2 cur = primary.position.ReadValue();
                    float dist = Vector2.Distance(cur, _touchStartPos);
                    if (dist > _touchMaxMovement) _touchMaxMovement = dist;
                }
                else if (_isTrackingTouch && primary.press.wasReleasedThisFrame)
                {
                    Vector2 endPos = primary.position.ReadValue();
                    float duration = now - _touchStartTime;
                    bool endedOverUI = IsPointerOverUI(endPos);
                    bool isValidTap = EvaluateTapParameters(
                        _touchStartPos,
                        endPos,
                        _touchMaxMovement,
                        duration,
                        _touchStartedOverUI,
                        endedOverUI,
                        threshold);

                    _isTrackingTouch = false;

                    if (isValidTap)
                    {
                        tapPosition = endPos;
                        return true;
                    }
                }
            }

            // Mouse fallback for Editor/Desktop
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    _isTrackingMouse = true;
                    _mouseStartPos = Mouse.current.position.ReadValue();
                    _mouseStartTime = now;
                    _mouseMaxMovement = 0f;
                    _mouseStartedOverUI = IsPointerOverUI(_mouseStartPos);
                }
                else if (_isTrackingMouse && Mouse.current.leftButton.isPressed)
                {
                    Vector2 cur = Mouse.current.position.ReadValue();
                    float dist = Vector2.Distance(cur, _mouseStartPos);
                    if (dist > _mouseMaxMovement) _mouseMaxMovement = dist;
                }
                else if (_isTrackingMouse && Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    Vector2 endPos = Mouse.current.position.ReadValue();
                    float duration = now - _mouseStartTime;
                    bool endedOverUI = IsPointerOverUI(endPos);
                    bool isValidClick = EvaluateTapParameters(
                        _mouseStartPos,
                        endPos,
                        _mouseMaxMovement,
                        duration,
                        _mouseStartedOverUI,
                        endedOverUI,
                        threshold);

                    _isTrackingMouse = false;

                    if (isValidClick)
                    {
                        tapPosition = endPos;
                        return true;
                    }
                }
            }
#endif

            // Unity Standard Touch Input
            if (Input.touchCount > 0)
            {
                UnityEngine.Touch t = Input.GetTouch(0);

                if (t.phase == UnityEngine.TouchPhase.Began)
                {
                    _isTrackingTouch = true;
                    _trackingFingerId = t.fingerId;
                    _touchStartPos = t.position;
                    _touchStartTime = now;
                    _touchMaxMovement = 0f;
                    _touchStartedOverUI = IsPointerOverUI(t.position);
                }
                else if (_isTrackingTouch && t.fingerId == _trackingFingerId)
                {
                    if (t.phase == UnityEngine.TouchPhase.Moved || t.phase == UnityEngine.TouchPhase.Stationary)
                    {
                        float dist = Vector2.Distance(t.position, _touchStartPos);
                        if (dist > _touchMaxMovement) _touchMaxMovement = dist;
                    }
                    else if (t.phase == UnityEngine.TouchPhase.Ended)
                    {
                        Vector2 endPos = t.position;
                        float duration = now - _touchStartTime;
                        bool endedOverUI = IsPointerOverUI(endPos);
                        bool isValidTap = EvaluateTapParameters(
                            _touchStartPos,
                            endPos,
                            _touchMaxMovement,
                            duration,
                            _touchStartedOverUI,
                            endedOverUI,
                            threshold);

                        _isTrackingTouch = false;
                        _trackingFingerId = -1;

                        if (isValidTap)
                        {
                            tapPosition = endPos;
                            return true;
                        }
                    }
                    else if (t.phase == UnityEngine.TouchPhase.Canceled)
                    {
                        _isTrackingTouch = false;
                        _trackingFingerId = -1;
                    }
                }
            }
            // Unity Standard Mouse Input (Editor / Desktop)
            else if (Input.GetMouseButtonDown(0))
            {
                _isTrackingMouse = true;
                _mouseStartPos = Input.mousePosition;
                _mouseStartTime = now;
                _mouseMaxMovement = 0f;
                _mouseStartedOverUI = IsPointerOverUI(Input.mousePosition);
            }
            else if (_isTrackingMouse)
            {
                if (Input.GetMouseButton(0))
                {
                    float dist = Vector2.Distance((Vector2)Input.mousePosition, _mouseStartPos);
                    if (dist > _mouseMaxMovement) _mouseMaxMovement = dist;
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    Vector2 endPos = Input.mousePosition;
                    float duration = now - _mouseStartTime;
                    bool endedOverUI = IsPointerOverUI(endPos);
                    bool isValidClick = EvaluateTapParameters(
                        _mouseStartPos,
                        endPos,
                        _mouseMaxMovement,
                        duration,
                        _mouseStartedOverUI,
                        endedOverUI,
                        threshold);

                    _isTrackingMouse = false;

                    if (isValidClick)
                    {
                        tapPosition = endPos;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Resets all active gesture tracking state.
        /// </summary>
        public void Reset()
        {
            _isTrackingTouch = false;
            _trackingFingerId = -1;
            _touchMaxMovement = 0f;
            _touchStartedOverUI = false;

            _isTrackingMouse = false;
            _mouseMaxMovement = 0f;
            _mouseStartedOverUI = false;
        }
    }
}
