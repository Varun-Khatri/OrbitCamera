using System;
using UnityEngine;

namespace VK.OrbitCamera
{
    /// <summary>
    /// One frame of normalized orbit input. Every source (mouse, touch, gamepad, your event bus)
    /// produces this same struct so the controller never cares where input came from.
    /// Deltas are resolution-independent (sources divide pixel motion by screen height).
    /// </summary>
    public readonly struct OrbitInputFrame
    {
        /// <summary>x = yaw, y = pitch. ~1.0 == a full-screen-height swipe.</summary>
        public readonly Vector2 OrbitDelta;

        /// <summary>+ = zoom in. ~1.0 == one mouse-wheel notch.</summary>
        public readonly float ZoomDelta;

        /// <summary>Screen-plane pan in the same normalized units as OrbitDelta.</summary>
        public readonly Vector2 PanDelta;

        /// <summary>True when the user is actively manipulating the camera this frame (gates auto-rotate).</summary>
        public readonly bool Interacting;

        public OrbitInputFrame(Vector2 orbitDelta, float zoomDelta, Vector2 panDelta, bool interacting)
        {
            OrbitDelta = orbitDelta;
            ZoomDelta = zoomDelta;
            PanDelta = panDelta;
            Interacting = interacting;
        }

        public bool HasMovement =>
            OrbitDelta.x != 0f || OrbitDelta.y != 0f || ZoomDelta != 0f || PanDelta.x != 0f || PanDelta.y != 0f;

        public static readonly OrbitInputFrame None = default;
    }

    /// <summary>Pose the controller produces each frame. Pure value type.</summary>
    public readonly struct CameraPose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public CameraPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }

    /// <summary>Initial spherical state for the controller.</summary>
    public readonly struct OrbitState
    {
        public readonly float Yaw, Pitch, Distance;
        public readonly Vector3 Pivot;

        public OrbitState(float yaw, float pitch, float distance, Vector3 pivot)
        {
            Yaw = yaw;
            Pitch = pitch;
            Distance = distance;
            Pivot = pivot;
        }
    }

    /// <summary>Optional framing applied when the camera focuses on a target.</summary>
    [Serializable]
    public struct OrbitFocusProfile
    {
        [Tooltip("World-space offset added to the focus position to form the orbit pivot.")]
        public Vector3 PivotOffset;

        [Tooltip("If true, the camera re-aims to the yaw/pitch/distance below while focusing. " +
                 "If false, only the pivot moves and the user's current angle/zoom is kept.")]
        public bool Reframe;

        public float Yaw;
        public float Pitch;
        public float Distance;

        public static OrbitFocusProfile Default => new OrbitFocusProfile
        {
            PivotOffset = Vector3.zero,
            Reframe = false,
            Yaw = 0f,
            Pitch = 15f,
            Distance = 6f,
        };
    }

    /// <summary>Anything the camera can orbit around. Implement to expose your own focus objects.</summary>
    public interface IOrbitFocus
    {
        Vector3 GetPivot();
        OrbitFocusProfile Profile { get; }
        bool IsValid { get; }
    }

    /// <summary>Default focus that tracks a Transform. Handles moving objects (e.g. driving vehicles).</summary>
    public sealed class TransformOrbitFocus : IOrbitFocus
    {
        Transform _transform;
        OrbitFocusProfile _profile;
        Vector3 _lastPivot;

        public TransformOrbitFocus(Transform transform, OrbitFocusProfile profile)
        {
            _transform = transform;
            _profile = profile;
            if (transform != null) _lastPivot = transform.position + profile.PivotOffset;
        }

        public Transform Transform
        {
            get => _transform;
            set => _transform = value;
        }

        public OrbitFocusProfile Profile => _profile;
        public bool IsValid => _transform != null;

        public Vector3 GetPivot()
        {
            if (_transform != null) _lastPivot = _transform.position + _profile.PivotOffset;
            return _lastPivot; // graceful fallback if the target was destroyed mid-orbit
        }
    }

    /// <summary>Produces normalized input each frame. Back it with the Input System, your event bus, replay data, etc.</summary>
    public interface IOrbitInputSource
    {
        OrbitInputFrame Read(float deltaTime);
    }

    public enum OrbitFocusPhase
    {
        Started,
        Completed
    }

    /// <summary>Payload published when the focus target changes. Small value type — passed by 'in'.</summary>
    public readonly struct OrbitFocusEvent
    {
        public readonly IOrbitFocus Focus;
        public readonly IOrbitFocus Previous;
        public readonly OrbitFocusPhase Phase;

        public OrbitFocusEvent(IOrbitFocus focus, IOrbitFocus previous, OrbitFocusPhase phase)
        {
            Focus = focus;
            Previous = previous;
            Phase = phase;
        }
    }

    /// <summary>Hook for your event system. Implement and forward to your bus.</summary>
    public interface IOrbitEventPublisher
    {
        void Publish(in OrbitFocusEvent evt);
    }

    /// <summary>No-op publisher used when no event system is wired.</summary>
    public sealed class NullOrbitEventPublisher : IOrbitEventPublisher
    {
        public static readonly NullOrbitEventPublisher Instance = new NullOrbitEventPublisher();

        NullOrbitEventPublisher()
        {
        }

        public void Publish(in OrbitFocusEvent evt)
        {
        }
    }

    /// <summary>Helpers for auto-framing focus targets by size.</summary>
    public static class OrbitFraming
    {
        /// <summary>Distance needed for a sphere of the given radius to fill 'fill' of the vertical view.</summary>
        public static float DistanceForRadius(float radius, float verticalFovDeg, float fill = 0.85f)
        {
            float half = verticalFovDeg * 0.5f * Mathf.Deg2Rad;
            return radius / Mathf.Max(0.05f, fill) / Mathf.Tan(half);
        }
    }

    /// <summary>Creates a sensible default input source for the current input backend.</summary>
    public static class OrbitInputSourceFactory
    {
        // Installed by the InputSystem assembly at startup (if present). Keeps core decoupled.
        public static Func<IOrbitInputSource> Override;

        public static IOrbitInputSource CreateDefault()
            => Override != null ? Override() : new ManualOrbitSource();
    }
}