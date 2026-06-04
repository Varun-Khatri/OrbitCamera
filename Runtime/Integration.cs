using System;
using UnityEngine;

namespace VK.OrbitCamera
{
    /// <summary>
    /// Push-based input source. Drive it from YOUR event system: in your input event handlers call
    /// AddOrbit / AddZoom / AddPan; the controller drains the accumulated input each frame.
    ///
    /// Example (pseudo) wiring to an event bus:
    ///   _bus.Subscribe&lt;DragEvent&gt;(e =&gt; _orbitSource.AddOrbit(e.Delta / Screen.height));
    ///   _bus.Subscribe&lt;ZoomEvent&gt;(e =&gt; _orbitSource.AddZoom(e.Amount));
    ///
    /// No Unity Input dependency, zero per-frame allocation.
    /// </summary>
    public sealed class ManualOrbitSource : IOrbitInputSource
    {
        Vector2 _orbit;
        float _zoom;
        Vector2 _pan;
        bool _interacting;

        /// <summary>Feed orbit delta in normalized units (≈ pixels / Screen.height).</summary>
        public void AddOrbit(Vector2 delta)
        {
            _orbit += delta;
            _interacting = true;
        }

        /// <summary>Feed zoom delta (+ = zoom in), ≈ wheel notches.</summary>
        public void AddZoom(float delta)
        {
            _zoom += delta;
            _interacting = true;
        }

        /// <summary>Feed pan delta in normalized units.</summary>
        public void AddPan(Vector2 delta)
        {
            _pan += delta;
            _interacting = true;
        }

        public OrbitInputFrame Read(float deltaTime)
        {
            var frame = new OrbitInputFrame(_orbit, _zoom, _pan, _interacting);
            _orbit = Vector2.zero;
            _zoom = 0f;
            _pan = Vector2.zero;
            _interacting = false;
            return frame;
        }
    }

    /// <summary>
    /// Forwards orbit focus events to your own event system via a delegate.
    /// Wire it once at startup:
    ///   var publisher = new CallbackOrbitEventPublisher(e =&gt; myBus.Publish(e));
    ///   orbitCamera.Initialize(inputSource, publisher);
    /// </summary>
    public sealed class CallbackOrbitEventPublisher : IOrbitEventPublisher
    {
        readonly Action<OrbitFocusEvent> _onFocusEvent;
        public CallbackOrbitEventPublisher(Action<OrbitFocusEvent> onFocusEvent) => _onFocusEvent = onFocusEvent;
        public void Publish(in OrbitFocusEvent evt) => _onFocusEvent?.Invoke(evt);
    }
}