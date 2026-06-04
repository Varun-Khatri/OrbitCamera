using VK.OrbitCamera.Examples;
using UnityEngine;

namespace VK.OrbitCamera.Samples
{
    /// <summary>
    /// Basic Showcase demo. Drop this on an empty GameObject in a fresh scene and press Play —
    /// it builds everything at runtime (light, ground, a few objects to orbit, a configured camera,
    /// and click-to-refocus), so there is no inspector wiring to get wrong.
    ///
    /// Read it top-to-bottom as a recipe for setting up your own scene by hand (see the sample
    /// README for the manual steps and how to save this as a reusable .unity scene).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Orbit Camera/Samples/Orbit Showcase Bootstrap")]
    public sealed class OrbitShowcaseBootstrap : MonoBehaviour
    {
        [Tooltip("How many primitives to spawn in a row as focus targets.")]
        [SerializeField] int _objectCount = 3;

        [Tooltip("Spacing between spawned objects, in metres.")]
        [SerializeField] float _spacing = 2.5f;

        static readonly PrimitiveType[] Shapes =
            { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Capsule, PrimitiveType.Cylinder };

        void Start()
        {
            EnsureLight();

            var cam = ResolveCamera();

            // --- Spawn focus targets in a centered row ---
            Transform first = null;
            float startX = -(_objectCount - 1) * 0.5f * _spacing;
            for (int i = 0; i < Mathf.Max(1, _objectCount); i++)
            {
                var go = GameObject.CreatePrimitive(Shapes[i % Shapes.Length]);
                go.name = $"Showcase {i + 1}";
                go.transform.position = new Vector3(startX + i * _spacing, 0.5f, 0f);
                if (first == null) first = go.transform;
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = Vector3.one * 2f;

            // --- Configure the orbit camera ---
            var orbit = cam.GetComponent<OrbitCameraBehaviour>();
            if (orbit == null) orbit = cam.gameObject.AddComponent<OrbitCameraBehaviour>();

            // Auto-selects InputSystemOrbitSource when the Input System package is present,
            // otherwise a push-based ManualOrbitSource.
            orbit.Initialize(OrbitInputSourceFactory.CreateDefault());
            orbit.SetFocus(first);

            // Click / tap any object to smoothly refocus and auto-frame it.
            if (cam.GetComponent<OrbitFocusClickExample>() == null)
                cam.gameObject.AddComponent<OrbitFocusClickExample>();
        }

        static void EnsureLight()
        {
            if (FindAnyLight() != null) return;
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        static Light FindAnyLight()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<Light>();
#else
            return Object.FindObjectOfType<Light>();
#endif
        }

        static Camera ResolveCamera()
        {
            var cam = Camera.main;
            if (cam != null) return cam;

            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
            return cam;
        }
    }
}
