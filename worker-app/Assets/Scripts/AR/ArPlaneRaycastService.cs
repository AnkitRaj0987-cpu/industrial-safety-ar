// ArPlaneRaycastService.cs
// Namespace : IndustrialSafetyAR.AR
//
// Concrete implementation of IArRaycastService using AR Foundation's ARRaycastManager.
// Attached to the AR scene to provide reusable, clean surface hit testing.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace IndustrialSafetyAR.AR
{
    /// <summary>
    /// Wraps <see cref="ARRaycastManager"/> to provide clean, robust plane hit-testing
    /// for AR object placement and spatial interactions.
    /// </summary>
    public class ArPlaneRaycastService : MonoBehaviour, IArRaycastService
    {
        [Tooltip("Optional direct reference to ARRaycastManager. If null, discovered automatically.")]
        [SerializeField]
        private ARRaycastManager _raycastManager;

        [Tooltip("The trackable types considered valid surface hits for placement.")]
        [SerializeField]
        private TrackableType _raycastMask = TrackableType.PlaneWithinPolygon | TrackableType.PlaneWithinBounds;

        private static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        private void Awake()
        {
            if (_raycastManager == null)
            {
                _raycastManager = FindAnyObjectByType<ARRaycastManager>();
            }
        }

        /// <summary>
        /// Attempts to raycast against detected AR planes from a screen pixel point.
        /// </summary>
        public bool TryRaycastPlane(Vector2 screenPosition, out Pose hitPose)
        {
            hitPose = default;

            if (_raycastManager == null)
            {
                _raycastManager = FindAnyObjectByType<ARRaycastManager>();
                if (_raycastManager == null)
                {
                    Debug.LogWarning("[ArPlaneRaycastService] ARRaycastManager not found in active scene.");
                    return false;
                }
            }

            s_Hits.Clear();
            if (_raycastManager.Raycast(screenPosition, s_Hits, _raycastMask) && s_Hits.Count > 0)
            {
                hitPose = s_Hits[0].pose;
                return true;
            }

            return false;
        }
    }
}
