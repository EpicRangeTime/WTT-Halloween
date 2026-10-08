using UnityEngine;

namespace WTTHalloweenClient
{
    /// <summary>Attach to an eye pivot under an EyeTrackingPatch controller.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("WTT Halloween/Eye Tracking Target")]
    public sealed class EyeTrackingTarget : MonoBehaviour
    {
        [Tooltip("Direction the eye faces in this pivot's local coordinates at its neutral pose.")]
        public Vector3 forward = Vector3.forward;
        [Tooltip("Local up direction at the neutral pose. Must not be parallel to Forward.")]
        public Vector3 up = Vector3.up;
        [Tooltip("Maximum left rotation from this eye's neutral direction, in degrees.")]
        [Range(0f, 89f)] public float leftLimit = 35f;
        [Tooltip("Maximum right rotation from this eye's neutral direction, in degrees.")]
        [Range(0f, 89f)] public float rightLimit = 35f;
        [Tooltip("Maximum upward rotation from this eye's neutral direction, in degrees.")]
        [Range(0f, 89f)] public float upLimit = 20f;
        [Tooltip("Maximum downward rotation from this eye's neutral direction, in degrees.")]
        [Range(0f, 89f)] public float downLimit = 20f;

        private Quaternion _neutral;
        private bool _initialized;
        private bool _warnedInvalidAxes;

        private void OnEnable()
        {
            CaptureNeutral();
        }

        private void CaptureNeutral()
        {
            if (_initialized) return;
            _neutral = transform.localRotation;
            _initialized = true;
        }

        internal void Track(Vector3 cameraPosition, float blend)
        {
            CaptureNeutral();
            if (forward.sqrMagnitude < 0.0001f
                || Vector3.Cross(forward, up).sqrMagnitude < 0.0001f)
            {
                if (!_warnedInvalidAxes)
                {
                    Debug.LogWarning("[WTT Halloween] Eye Forward and Up must be nonzero and nonparallel.", this);
                    _warnedInvalidAxes = true;
                }
                return;
            }
            _warnedInvalidAxes = false;

            // Limits follow this eye's own neutral orientation, regardless of where it faces on the mask.
            Quaternion basis = Quaternion.LookRotation(forward.normalized, up.normalized);
            Quaternion parentRotation = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
            Quaternion neutralWorld = parentRotation * _neutral * basis;
            Vector3 direction = Quaternion.Inverse(neutralWorld) * (cameraPosition - transform.position);
            if (direction.sqrMagnitude < 0.000001f) return;

            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Atan2(direction.y,
                Mathf.Sqrt(direction.x * direction.x + direction.z * direction.z)) * Mathf.Rad2Deg;
            yaw = Mathf.Clamp(yaw, -Mathf.Clamp(leftLimit, 0f, 89f), Mathf.Clamp(rightLimit, 0f, 89f));
            pitch = Mathf.Clamp(pitch, -Mathf.Clamp(downLimit, 0f, 89f), Mathf.Clamp(upLimit, 0f, 89f));
            Quaternion desired = _neutral * basis
                * Quaternion.Euler(-pitch, yaw, 0f) * Quaternion.Inverse(basis);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, desired, blend);
        }

        private void OnDisable()
        {
            RestoreNeutral();
        }

        internal void RestoreNeutral()
        {
            if (_initialized) transform.localRotation = _neutral;
        }
    }
}