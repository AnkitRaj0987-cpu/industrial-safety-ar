// TapGatedButton.cs
// Namespace : IndustrialSafetyAR.UI
//
// Ensures screen-space UI action buttons only execute from intentional, stationary taps.
// Swipes, drags, and accidental sliding across button rects are strictly rejected.

using System;
using IndustrialSafetyAR.Modules.FireExplosion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IndustrialSafetyAR.UI
{
    /// <summary>
    /// Event handler attached to UI buttons that enforces tap-vs-swipe gating.
    /// Rejects click invocation if the pointer moved beyond the tap threshold or exceeded tap duration.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TapGatedButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private Vector2 _pressPos;
        private float _pressTime;
        private bool _isSwipe;
        private Action _onTap;
        private Button _button;

        public void Initialize(Action onTap)
        {
            _button = GetComponent<Button>();
            _onTap = onTap;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressPos = eventData.position;
            _pressTime = Time.unscaledTime;
            _isSwipe = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            float threshold = TouchGestureFilter.GetTapMovementThreshold();
            if (Vector2.Distance(eventData.position, _pressPos) > threshold)
            {
                _isSwipe = true;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            float threshold = TouchGestureFilter.GetTapMovementThreshold();
            float dist = Vector2.Distance(eventData.position, _pressPos);
            float duration = Time.unscaledTime - _pressTime;

            if (_isSwipe || dist > threshold || duration > TouchGestureFilter.DefaultMaxTapDurationSeconds)
            {
                // Discard swipe/drag gesture
                return;
            }

            // Ensure release occurred inside the button's screen rect
            if (transform is RectTransform rect &&
                !RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, eventData.pressEventCamera))
            {
                return;
            }

            // Valid intentional button tap
            _onTap?.Invoke();
        }

        /// <summary>
        /// Programmatically triggers the button action (useful for automated testing).
        /// </summary>
        public void TriggerTap()
        {
            _onTap?.Invoke();
        }
    }
}
