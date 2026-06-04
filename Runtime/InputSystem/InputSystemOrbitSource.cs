#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;

namespace VK.OrbitCamera.InputSystem
{
    public enum MouseButtonKind
    {
        Left,
        Right,
        Middle
    }

    [System.Serializable]
    public struct OrbitInputConfig
    {
        [Header("Mouse")] [Tooltip("Only orbit while a mouse button is held (so UI clicks don't spin the camera).")]
        public bool RequireHoldToOrbit;

        public MouseButtonKind OrbitButton;
        public float MouseZoomSensitivity; // multiplier per wheel notch
        public bool EnableMousePan;
        public MouseButtonKind PanButton;

        [Header("Touch")] public float PinchZoomSensitivity;
        public bool EnableTwoFingerPan;

        [Header("Gamepad")] public float GamepadTurnRate; // orbit units / sec at full right-stick
        public float GamepadZoomRate; // zoom notches / sec at full trigger

        public static OrbitInputConfig Default => new OrbitInputConfig
        {
            RequireHoldToOrbit = true,
            OrbitButton = MouseButtonKind.Left,
            MouseZoomSensitivity = 1f,
            EnableMousePan = false,
            PanButton = MouseButtonKind.Middle,
            PinchZoomSensitivity = 8f,
            EnableTwoFingerPan = false,
            GamepadTurnRate = 2.5f,
            GamepadZoomRate = 6f,
        };
    }

    /// <summary>
    /// IOrbitInputSource for the new Input System.
    ///   Mouse:   drag (held button) = orbit, wheel = zoom, optional middle-drag = pan.
    ///   Touch:   1 finger = orbit, pinch = zoom, optional 2-finger drag = pan.
    ///   Gamepad: right stick = orbit, triggers = zoom.
    /// Resolution-independent (deltas / screen height). Allocates nothing per frame.
    /// </summary>
    public sealed class InputSystemOrbitSource : IOrbitInputSource
    {
        readonly OrbitInputConfig _config;

        bool _hadTwoTouches;
        float _lastPinchDistance;
        Vector2 _lastTwoFingerMid;

        public InputSystemOrbitSource() : this(OrbitInputConfig.Default)
        {
        }

        public InputSystemOrbitSource(OrbitInputConfig config)
        {
            _config = config;
        }

        public OrbitInputFrame Read(float deltaTime)
        {
            float invH = Screen.height > 0 ? 1f / Screen.height : 0f;
            Vector2 orbit = Vector2.zero;
            Vector2 pan = Vector2.zero;
            float zoom = 0f;
            bool interacting = false;
            bool touchActive = false;

            // ---------- Touch ----------
            var ts = Touchscreen.current;
            if (ts != null)
            {
                int active = 0;
                Vector2 p0 = default, p1 = default, d0 = default;
                var touches = ts.touches;
                for (int i = 0; i < touches.Count && active < 2; i++)
                {
                    var tc = touches[i];
                    var phase = tc.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.Began ||
                        phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                        phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                    {
                        Vector2 pos = tc.position.ReadValue();
                        if (active == 0)
                        {
                            p0 = pos;
                            d0 = tc.delta.ReadValue();
                        }
                        else
                        {
                            p1 = pos;
                        }

                        active++;
                    }
                }

                if (active == 1)
                {
                    orbit += d0 * invH;
                    interacting = true;
                    touchActive = true;
                    _hadTwoTouches = false;
                }
                else if (active >= 2)
                {
                    interacting = true;
                    touchActive = true;
                    float pinch = Vector2.Distance(p0, p1);
                    Vector2 mid = (p0 + p1) * 0.5f;
                    if (_hadTwoTouches)
                    {
                        zoom += (pinch - _lastPinchDistance) * invH * _config.PinchZoomSensitivity;
                        if (_config.EnableTwoFingerPan) pan += (mid - _lastTwoFingerMid) * invH;
                    }

                    _lastPinchDistance = pinch;
                    _lastTwoFingerMid = mid;
                    _hadTwoTouches = true;
                }
                else
                {
                    _hadTwoTouches = false;
                }
            }

            // ---------- Mouse (skip while fingers are down to avoid double input) ----------
            var mouse = Mouse.current;
            if (mouse != null && !touchActive)
            {
                Vector2 mDelta = mouse.delta.ReadValue();
                bool orbitHeld = !_config.RequireHoldToOrbit || ButtonHeld(mouse, _config.OrbitButton);

                if (orbitHeld && (mDelta.x != 0f || mDelta.y != 0f))
                {
                    orbit += mDelta * invH;
                    interacting = true;
                }

                if (_config.EnableMousePan && ButtonHeld(mouse, _config.PanButton) &&
                    (mDelta.x != 0f || mDelta.y != 0f))
                {
                    pan += mDelta * invH;
                    interacting = true;
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (scroll != 0f)
                {
                    zoom += scroll / 120f * _config.MouseZoomSensitivity; // ~120 raw units per notch
                    interacting = true;
                }
            }

            // ---------- Gamepad ----------
            var pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.rightStick.ReadValue();
                if (stick.sqrMagnitude > 0.0001f)
                {
                    orbit += stick * (_config.GamepadTurnRate * deltaTime);
                    interacting = true;
                }

                float zoomAxis = pad.rightTrigger.ReadValue() - pad.leftTrigger.ReadValue();
                if (Mathf.Abs(zoomAxis) > 0.01f)
                {
                    zoom += zoomAxis * (_config.GamepadZoomRate * deltaTime);
                    interacting = true;
                }
            }

            return new OrbitInputFrame(orbit, zoom, pan, interacting);
        }

        static bool ButtonHeld(Mouse mouse, MouseButtonKind kind) => kind switch
        {
            MouseButtonKind.Left => mouse.leftButton.isPressed,
            MouseButtonKind.Right => mouse.rightButton.isPressed,
            MouseButtonKind.Middle => mouse.middleButton.isPressed,
            _ => false,
        };
    }
}
#endif