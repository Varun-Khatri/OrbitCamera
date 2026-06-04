# Orbit Camera

A generic, near-zero-allocation camera rig for orbiting a focus object with **mouse, touch, or
gamepad**, plus zoom and **runtime focus-swapping** with smooth eased transitions. Built for
vehicle configurators, product showcases, and real-estate walkarounds.

The core is **dependency-free pure C#** with a **single MonoBehaviour**. Third-party integrations
(LitMotion, the Input System, Reflex) live in thin, optional assemblies that are auto-detected and
compiled in only when their package is present.

---

## Requirements

| Dependency | Required? | Used by |
|---|---|---|
| **LitMotion v2** (`com.annulusgames.lit-motion`) | **Yes** | Focus transitions in `OrbitCameraBehaviour` |
| **Input System** (`com.unity.inputsystem`) | Optional | `OrbitCamera.InputSystem` assembly |
| **Reflex** (`com.gustavopsantos.reflex`) | Optional | `OrbitCamera.Reflex` assembly |

Unity **2021.3+**.

> LitMotion is distributed via Git/OpenUPM, and Unity does **not** resolve Git dependencies
> transitively. So this package does not list it in `package.json` `dependencies` — **install
> LitMotion yourself first** (see below), or add the OpenUPM scoped registry so it resolves
> automatically.

---

## Installation

Install LitMotion first, then this package. In **Window > Package Manager > + > Add package from git URL**:

```
https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion
https://github.com/your-name/orbit-camera.git
```

Or add both to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.annulusgames.lit-motion": "https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion",
    "com.example.orbit-camera": "https://github.com/your-name/orbit-camera.git"
  }
}
```

Pin a release by appending `#v0.1.0` to the orbit-camera URL.

---

## Quick start

1. Add `OrbitCameraBehaviour` to your camera.
2. Assign **Initial Focus** (the Transform to orbit) and tune **Settings**.
3. Press Play: drag/swipe to orbit, wheel/pinch/triggers to zoom.

Change focus at runtime:

```csharp
behaviour.SetFocus(newTransform);                                       // smooth default blend
behaviour.SetFocus(newTransform, profile, OrbitFocusTransition.Smooth(0.6f, Ease.OutExpo));
behaviour.SetFocus(focus, OrbitFocusTransition.Instant);                // snap, no tween
```

Try the **Basic Showcase** sample (Package Manager > this package > Samples > Import).

---

## Assembly layout

```
OrbitCamera                      Runtime/                 core (refs LitMotion only)
OrbitCamera.InputSystem          Runtime/InputSystem/     optional; compiles iff Input System present
OrbitCamera.Reflex               Integrations/Reflex/     optional; compiles iff Reflex present
OrbitCamera.Samples.BasicShowcase Samples~/BasicShowcase/ sample
```

The optional assemblies use `versionDefines` + `defineConstraints`, so they are skipped entirely
when their package is absent — no broken references, no `#if` soup in your project.

`OrbitInputSourceFactory.CreateDefault()` returns an `InputSystemOrbitSource` when the Input System
assembly is compiled in (it self-registers at startup), otherwise a push-based `ManualOrbitSource`.

---

## Wiring your event system

```csharp
// Outbound: forward focus Started/Completed events to your bus.
var publisher = new CallbackOrbitEventPublisher(evt => myBus.Publish(evt));
behaviour.Initialize(inputSource, publisher);

// Inbound: drive orbit/zoom from your own input instead of the Input System.
var src = new ManualOrbitSource();
behaviour.Initialize(src);
src.AddOrbit(new Vector2(dx, dy));   // pixel-ish deltas; normalized internally
src.AddZoom(scrollOrPinchDelta);
src.AddPan(panDelta);
```

## Wiring with Reflex

Add `OrbitCameraInstaller` to a scope (registers settings, input source, event publisher) and put
`OrbitCameraBootstrap` next to `OrbitCameraBehaviour`. The bootstrap receives dependencies via
`[Inject]` and calls `Initialize(...)`, so the camera component itself stays DI-free.

---

## Performance

- **Per-frame orbit/zoom/pan is zero-alloc** (pooled input structs, `SmoothDamp` velocity fields, no LINQ/boxing).
- **Focus transitions are near-zero-alloc** (static LitMotion bind lambda + cached completion delegate).
- Input is **resolution-independent**; smoothing is **frame-rate independent** and honors `UseUnscaledTime`.
