# Basic Showcase (sample)

Two ways to use this sample.

## A. Zero-wiring runtime demo (fastest)

1. Create a new empty scene.
2. Create an empty GameObject and add **Orbit Showcase Bootstrap**
   (`Add Component > Orbit Camera/Samples/Orbit Showcase Bootstrap`).
3. Press **Play**.

`OrbitShowcaseBootstrap` builds the whole scene at runtime: a light, a ground plane, a row of
primitives to orbit, a camera with `OrbitCameraBehaviour`, and click-to-refocus. Drag/swipe to
orbit, wheel/pinch/triggers to zoom, click an object to smoothly refocus and auto-frame it.

> This sample assumes the **Unity Input System** package is installed (it reads pointer position
> for the click raycast). If you don't use the Input System, delete this sample's asmdef constraint
> or drive a `ManualOrbitSource` from your own input instead.

## B. Build a real scene by hand (what to ship)

Use the bootstrap as a recipe:

1. Add an empty scene with a Camera and a Directional Light.
2. Add `OrbitCameraBehaviour` to the Camera. In the inspector:
   - leave **Camera** empty (it auto-uses the Camera on the same GameObject), or assign one;
   - set **Initial Focus** to the Transform you want to orbit;
   - tune **Settings** (orbit speed, pitch/zoom clamps, smoothing).
3. Leave **Create Default Input Source** on, or wire input yourself (see the package README).
4. Optionally add `OrbitFocusClickExample` to the Camera for click-to-refocus.
5. **File > Save As** to store it as e.g. `Demo.unity`, and commit it under the sample folder if you
   want it versioned (note: anything under `Samples~` is hidden from the Editor until imported via
   the Package Manager).
