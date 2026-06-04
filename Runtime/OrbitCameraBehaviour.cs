using System;
using LitMotion;
using UnityEngine;

namespace VK.OrbitCamera
{
    /// <summary>How a focus change is animated. Uses LitMotion easing.</summary>
    public struct OrbitFocusTransition
    {
        public float Duration;
        public Ease Ease;

        public static OrbitFocusTransition Instant => new OrbitFocusTransition { Duration = 0f, Ease = Ease.Linear };

        public static OrbitFocusTransition Smooth(float duration = 0.6f, Ease ease = Ease.OutExpo)
            => new OrbitFocusTransition { Duration = duration, Ease = ease };
    }

    /// <summary>
    /// The single MonoBehaviour in the system. It bridges Unity's lifecycle and LitMotion focus
    /// transitions to the pure <see cref="OrbitController"/>. Attach it to the camera (or assign one).
    ///
    /// Everything else — controller, input source, events — is plain C# behind interfaces, so the
    /// per-frame path is allocation-free and the whole thing is trivially testable / DI-friendly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OrbitCameraBehaviour : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        Camera _camera;

        [SerializeField] Transform _initialFocus;
        [SerializeField] OrbitFocusProfile _initialFocusProfile = OrbitFocusProfile.Default;

        [Header("Config")] [SerializeField] OrbitSettings _settings = new OrbitSettings();

        [Header("Initial state")] [SerializeField]
        float _initialYaw = 0f;

        [SerializeField] float _initialPitch = 15f;
        [SerializeField] float _initialDistance = 6f;

        [Header("Fallbacks (used only if nothing is injected)")]
        [Tooltip("If no IOrbitInputSource is injected, build one for the current input backend.")]
        [SerializeField]
        bool _createDefaultInputSource = true;

        OrbitController _controller;
        IOrbitInputSource _source;
        IOrbitEventPublisher _events;
        Transform _cameraTransform;

        IOrbitFocus _focus;
        IOrbitFocus _previousFocus;

        // Focus-transition state (LitMotion drives _focusBlend; LateUpdate reads it).
        MotionHandle _focusMotion;
        bool _transitioning;
        float _focusBlend;
        float _fromYaw, _fromPitch, _fromDistance;
        Vector3 _fromPivot;
        float _toYaw, _toPitch, _toDistance;
        bool _reframe;

        Action _onFocusComplete; // cached delegate -> zero alloc per transition
        bool _initialized;

        public OrbitController Controller
        {
            get
            {
                EnsureCore();
                return _controller;
            }
        }

        public OrbitSettings Settings => _settings;
        public IOrbitFocus CurrentFocus => _focus;
        public bool IsTransitioning => _transitioning;

        // ---------------------------------------------------------------- DI / wiring

        /// <summary>
        /// Inject dependencies before the first frame (e.g. from a Reflex bootstrap).
        /// Optional: defaults are created in Awake/LateUpdate if you don't.
        /// </summary>
        public void Initialize(IOrbitInputSource source, IOrbitEventPublisher events = null)
        {
            EnsureCore();
            if (source != null) _source = source;
            if (events != null) _events = events;
        }

        public void SetInputSource(IOrbitInputSource source)
        {
            if (source != null) _source = source;
        }

        // ---------------------------------------------------------------- lifecycle

        void Awake()
        {
            _onFocusComplete = OnFocusComplete;
            EnsureCore();
        }

        void EnsureCore()
        {
            if (_initialized) return;
            _initialized = true;

            if (_camera == null) _camera = GetComponent<Camera>();
            _cameraTransform = _camera != null ? _camera.transform : transform;

            Vector3 pivot = _initialFocus != null
                ? _initialFocus.position + _initialFocusProfile.PivotOffset
                : _cameraTransform.position + _cameraTransform.forward * _initialDistance;

            _controller = new OrbitController(_settings,
                new OrbitState(_initialYaw, _initialPitch, _initialDistance, pivot));

            _events ??= NullOrbitEventPublisher.Instance;
            if (_source == null && _createDefaultInputSource)
                _source = OrbitInputSourceFactory.CreateDefault();

            if (_initialFocus != null)
                _focus = new TransformOrbitFocus(_initialFocus, _initialFocusProfile);
        }

        void LateUpdate()
        {
            EnsureCore();

            float dt = _settings.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 pivot = ResolvePivot();

            if (_transitioning)
            {
                // LitMotion has already advanced _focusBlend this frame (Update runs before LateUpdate).
                float t = _focusBlend;
                float yaw = _reframe ? Mathf.LerpAngle(_fromYaw, _toYaw, t) : _fromYaw;
                float pitch = _reframe ? Mathf.Lerp(_fromPitch, _toPitch, t) : _fromPitch;
                float dist = _reframe ? Mathf.Lerp(_fromDistance, _toDistance, t) : _fromDistance;
                Vector3 p = Vector3.LerpUnclamped(_fromPivot, pivot, t); // tracks moving targets mid-transition
                _controller.SetCurrent(yaw, pitch, dist, p);
            }
            else
            {
                OrbitInputFrame input = _source != null ? _source.Read(dt) : OrbitInputFrame.None;
                _controller.TargetPivot = pivot; // follow moving focus targets
                _controller.ProcessInput(input, dt);
                _controller.Smooth(dt);
            }

            CameraPose pose = _controller.GetPose();
            _cameraTransform.SetPositionAndRotation(pose.Position, pose.Rotation);
        }

        void OnDisable()
        {
            if (_focusMotion.IsActive()) _focusMotion.Cancel();
            _transitioning = false;
        }

        Vector3 ResolvePivot()
            => _focus != null && _focus.IsValid ? _focus.GetPivot() : _controller.TargetPivot;

        // ---------------------------------------------------------------- focus API

        /// <summary>Focus a transform with default framing and a smooth transition.</summary>
        public void SetFocus(Transform target)
            => SetFocus(target, OrbitFocusProfile.Default, OrbitFocusTransition.Smooth());

        public void SetFocus(Transform target, OrbitFocusProfile profile, OrbitFocusTransition transition)
        {
            if (target == null) return;
            SetFocus(new TransformOrbitFocus(target, profile), transition);
        }

        /// <summary>Focus any IOrbitFocus. Cancels any in-flight transition and animates to the new target.</summary>
        public void SetFocus(IOrbitFocus focus, OrbitFocusTransition transition)
        {
            if (focus == null) return;
            EnsureCore();

            if (_focusMotion.IsActive()) _focusMotion.Cancel();

            _previousFocus = _focus;
            _focus = focus;
            OrbitFocusProfile profile = focus.Profile;

            // Snapshot where we are now (transition source).
            _fromYaw = _controller.Yaw;
            _fromPitch = _controller.Pitch;
            _fromDistance = _controller.Distance;
            _fromPivot = _controller.Pivot;

            // Destination framing (pivot resolves live each frame so moving targets still work).
            _reframe = profile.Reframe;
            _toYaw = profile.Yaw;
            _toPitch = Mathf.Clamp(profile.Pitch, _settings.MinPitch, _settings.MaxPitch);
            _toDistance = Mathf.Clamp(profile.Distance, _settings.MinDistance, _settings.MaxDistance);

            _events.Publish(new OrbitFocusEvent(_focus, _previousFocus, OrbitFocusPhase.Started));

            if (transition.Duration <= 0f)
            {
                FinishFocus();
                return;
            }

            _transitioning = true;
            _focusBlend = 0f;

            var builder = LMotion.Create(0f, 1f, transition.Duration).WithEase(transition.Ease);
            if (_settings.UseUnscaledTime)
                builder = builder.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);

            // Zero-alloc bind: static lambda (compiler-cached) + 'this' as state. No closure captured.
            _focusMotion = builder
                .WithOnComplete(_onFocusComplete)
                .Bind(this, static (t, self) => self._focusBlend = t);
        }

        void OnFocusComplete()
        {
            _transitioning = false;
            FinishFocus();
        }

        /// <summary>Land on the destination and hand control back to the user without a jump.</summary>
        void FinishFocus()
        {
            Vector3 pivot = ResolvePivot();
            float yaw = _reframe ? _toYaw : _controller.Yaw;
            float pitch = _reframe ? _toPitch : _controller.Pitch;
            float dist = _reframe ? _toDistance : _controller.Distance;

            _controller.SetCurrent(yaw, pitch, dist, pivot);
            _controller.SetTargets(yaw, pitch, dist, pivot);
            _controller.ResetVelocities();

            _events.Publish(new OrbitFocusEvent(_focus, _previousFocus, OrbitFocusPhase.Completed));
        }
    }
}