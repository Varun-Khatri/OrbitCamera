using LitMotion;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VK.OrbitCamera.Examples
{
    /// <summary>
    /// Click / tap a collider to refocus the orbit camera on it, auto-framing by the object's
    /// renderer bounds. Demonstrates the "change the focused object" feature for showcases /
    /// configurators / real-estate scenes. Drop on any GameObject and assign the camera.
    /// </summary>
    public sealed class OrbitFocusClickExample : MonoBehaviour
    {
        [SerializeField] OrbitCameraBehaviour _orbitCamera;
        [SerializeField] Camera _raycastCamera;
        [SerializeField] LayerMask _focusableLayers = ~0;
        [SerializeField] float _maxRayDistance = 500f;

        [Header("Framing")]
        [SerializeField] bool _autoFrameByBounds = true;
        [SerializeField] float _fillRatio = 0.85f;
        [SerializeField] float _pitch = 15f;
        [SerializeField] float _transitionDuration = 0.6f;
        [SerializeField] Ease _ease = Ease.OutExpo;

        void Awake()
        {
            if (_raycastCamera == null) _raycastCamera = _orbitCamera != null
                ? _orbitCamera.GetComponent<Camera>() : Camera.main;
        }

        void Update()
        {
            if (_orbitCamera == null || _raycastCamera == null) return;
            if (!TryGetClick(out Vector2 screenPos)) return;

            Ray ray = _raycastCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance, _focusableLayers)) return;

            Transform target = hit.transform;
            var profile = OrbitFocusProfile.Default;
            profile.Reframe = true;
            profile.Pitch = _pitch;
            profile.Yaw = _orbitCamera.Controller.Yaw; // keep the current heading

            if (_autoFrameByBounds && TryGetRadius(target, out Vector3 center, out float radius))
            {
                profile.PivotOffset = center - target.position;
                profile.Distance = OrbitFraming.DistanceForRadius(radius, _raycastCamera.fieldOfView, _fillRatio);
            }

            _orbitCamera.SetFocus(target, profile, OrbitFocusTransition.Smooth(_transitionDuration, _ease));
        }

        static bool TryGetRadius(Transform target, out Vector3 center, out float radius)
        {
            center = target.position;
            radius = 0f;
            var renderers = target.GetComponentsInChildren<Renderer>(); // editor/setup path; fine for click events
            if (renderers.Length == 0) return false;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            center = b.center;
            radius = b.extents.magnitude;
            return radius > 0f;
        }

        static bool TryGetClick(out Vector2 screenPos)
        {
#if ENABLE_INPUT_SYSTEM
            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = ts.primaryTouch.position.ReadValue();
                return true;
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPos = mouse.position.ReadValue();
                return true;
            }
            screenPos = default;
            return false;
#else
            if (Input.GetMouseButtonDown(0))
            {
                screenPos = Input.mousePosition;
                return true;
            }
            screenPos = default;
            return false;
#endif
        }
    }
}
