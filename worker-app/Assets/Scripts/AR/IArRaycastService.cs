// IArRaycastService.cs
// Namespace : IndustrialSafetyAR.AR
//
// Abstraction over AR plane raycasting to keep interaction logic decoupled
// from concrete AR Foundation classes and support unit testing.

using UnityEngine;

namespace IndustrialSafetyAR.AR
{
    /// <summary>
    /// Service contract for querying whether a screen coordinate hits a detected AR plane.
    /// </summary>
    public interface IArRaycastService
    {
        /// <summary>
        /// Attempts to raycast from a 2D screen coordinate against detected AR planes.
        /// </summary>
        /// <param name="screenPosition">Screen pixel coordinates (e.g. from touch or pointer).</param>
        /// <param name="hitPose">The world pose (position and rotation) on the detected plane surface.</param>
        /// <returns>True if a plane was hit; false otherwise.</returns>
        bool TryRaycastPlane(Vector2 screenPosition, out Pose hitPose);
    }
}
