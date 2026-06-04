using UnityEngine;

namespace VK.OrbitCamera
{
    /// <summary>
    /// Pure C# orbit brain. No MonoBehaviour, no allocations on the per-frame path.
    /// Drive it: ProcessInput() -> Smooth() -> GetPose() each frame, feeding the current pivot.
    /// Transitions and external systems can override state directly via the Set* helpers.
    /// </summary>
    public sealed class OrbitController
    {
        readonly OrbitSettings _settings;

        // Rendered (smoothed) state.
        public float Yaw, Pitch, Distance;
        public Vector3 Pivot;

        // Desired state the smoothing chases.
        public float TargetYaw, TargetPitch, TargetDistance;
        public Vector3 TargetPivot;

        // SmoothDamp velocities (kept as fields so smoothing allocates nothing).
        float _yawVel, _pitchVel, _distVel;
        Vector3 _pivotVel;
        float _idleTimer;

        public OrbitSettings Settings => _settings;
        public bool IsIdle => _idleTimer >= _settings.AutoRotateDelay;

        public OrbitController(OrbitSettings settings, in OrbitState initial)
        {
            _settings = settings;
            Yaw = TargetYaw = initial.Yaw;
            Pitch = TargetPitch = ClampPitch(initial.Pitch);
            Distance = TargetDistance = ClampDistance(initial.Distance);
            Pivot = TargetPivot = initial.Pivot;
        }

        /// <summary>Apply normalized input to the target state. Returns true if the user moved the camera.</summary>
        public bool ProcessInput(in OrbitInputFrame input, float deltaTime)
        {
            var s = _settings;
            bool moved = input.HasMovement;

            if (input.OrbitDelta.x != 0f || input.OrbitDelta.y != 0f)
            {
                TargetYaw += input.OrbitDelta.x * s.OrbitSpeed;
                TargetPitch += input.OrbitDelta.y * s.OrbitSpeed * (s.InvertY ? 1f : -1f);
            }

            if (input.ZoomDelta != 0f)
            {
                // Multiplicative: zoom feels consistent at any distance (slower as you get closer).
                float factor = Mathf.Clamp(1f - input.ZoomDelta * s.ZoomSpeed, 0.1f, 4f);
                TargetDistance *= factor;
            }

            if (s.EnablePan && (input.PanDelta.x != 0f || input.PanDelta.y != 0f))
            {
                Quaternion rot = Quaternion.Euler(TargetPitch, TargetYaw, 0f);
                Vector3 right = rot * Vector3.right;
                Vector3 up = rot * Vector3.up;
                // Scale by distance so a swipe pans the same screen amount whether near or far.
                TargetPivot += (right * -input.PanDelta.x + up * -input.PanDelta.y) * (s.PanSpeed * TargetDistance);
            }

            TargetPitch = ClampPitch(TargetPitch);
            TargetDistance = ClampDistance(TargetDistance);
            if (s.ClampYaw) TargetYaw = Mathf.Clamp(TargetYaw, s.MinYaw, s.MaxYaw);

            if (moved)
            {
                _idleTimer = 0f;
            }
            else
            {
                _idleTimer += deltaTime;
                if (s.AutoRotate && _idleTimer >= s.AutoRotateDelay)
                    TargetYaw += s.AutoRotateSpeed * deltaTime;
            }

            return moved;
        }

        /// <summary>Critically-damped smoothing of current toward target. Frame-rate independent, zero-alloc.</summary>
        public void Smooth(float deltaTime)
        {
            var s = _settings;
            Yaw = Mathf.SmoothDampAngle(Yaw, TargetYaw, ref _yawVel, s.RotationSmoothTime, Mathf.Infinity, deltaTime);
            Pitch = Mathf.SmoothDamp(Pitch, TargetPitch, ref _pitchVel, s.RotationSmoothTime, Mathf.Infinity,
                deltaTime);
            Distance = Mathf.SmoothDamp(Distance, TargetDistance, ref _distVel, s.ZoomSmoothTime, Mathf.Infinity,
                deltaTime);
            Pivot = Vector3.SmoothDamp(Pivot, TargetPivot, ref _pivotVel, s.PivotSmoothTime, Mathf.Infinity, deltaTime);
        }

        /// <summary>Spherical-to-cartesian: camera sits 'Distance' behind the pivot, looking at it.</summary>
        public CameraPose GetPose()
        {
            Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 position = Pivot - (rot * Vector3.forward) * Distance;
            return new CameraPose(position, rot);
        }

        // --- external control (used by focus transitions) ---

        public void SetCurrent(float yaw, float pitch, float distance, in Vector3 pivot)
        {
            Yaw = yaw;
            Pitch = pitch;
            Distance = distance;
            Pivot = pivot;
        }

        public void SetTargets(float yaw, float pitch, float distance, in Vector3 pivot)
        {
            TargetYaw = yaw;
            TargetPitch = ClampPitch(pitch);
            TargetDistance = ClampDistance(distance);
            TargetPivot = pivot;
        }

        public void SyncTargetsToCurrent()
        {
            TargetYaw = Yaw;
            TargetPitch = Pitch;
            TargetDistance = Distance;
            TargetPivot = Pivot;
            ResetVelocities();
        }

        public void ResetVelocities()
        {
            _yawVel = _pitchVel = _distVel = 0f;
            _pivotVel = Vector3.zero;
            _idleTimer = 0f;
        }

        float ClampPitch(float p) => Mathf.Clamp(p, _settings.MinPitch, _settings.MaxPitch);
        float ClampDistance(float d) => Mathf.Clamp(d, _settings.MinDistance, _settings.MaxDistance);
    }
}