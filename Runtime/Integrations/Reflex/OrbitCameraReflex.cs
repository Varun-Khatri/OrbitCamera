using VK.OrbitCamera;
using Reflex.Attributes;
using Reflex.Core;
using UnityEngine;

namespace VK.OrbitCamera.Reflex
{
    /// <summary>
    /// Example Reflex installer. Add it to a SceneScope (or ProjectScope) GameObject.
    /// Registers the orbit services so <see cref="OrbitCameraBootstrap"/> can resolve them.
    /// Adapt freely — e.g. bind your own IOrbitEventPublisher that forwards to your event bus.
    /// </summary>
    public sealed class OrbitCameraInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] OrbitSettings _settings = new OrbitSettings();

        public void InstallBindings(ContainerBuilder builder)
        {
            // Reflex API note: RegisterValue registers a user-created instance against the given
            // contracts and always behaves as a singleton (it replaces the old AddSingleton(value, ...)).

            // Shared config.
            builder.RegisterValue(_settings, new[] { typeof(OrbitSettings) });

            // Input source for the active backend. CreateDefault() returns an InputSystemOrbitSource
            // when the Input System package is present, otherwise a ManualOrbitSource you can drive
            // from your own event system. Built here so we can register it as a value.
            builder.RegisterValue(OrbitInputSourceFactory.CreateDefault(), new[] { typeof(IOrbitInputSource) });

            // Replace NullOrbitEventPublisher with your own adapter that forwards to your event bus, e.g.:
            //   builder.RegisterValue(new CallbackOrbitEventPublisher(myBus.Publish), typeof(IOrbitEventPublisher));
            // (If you need the bus resolved from the container, register a type/factory instead and
            //  build the publisher from the resolved bus.)
            builder.RegisterValue(NullOrbitEventPublisher.Instance, new[] { typeof(IOrbitEventPublisher) });
        }
    }

    /// <summary>
    /// Bridges Reflex-resolved services into the dependency-free <see cref="OrbitCameraBehaviour"/>.
    /// Put it on the camera GameObject inside a SceneScope hierarchy; Reflex calls Construct via
    /// method injection, keeping the camera itself free of any DI references.
    /// </summary>
    [RequireComponent(typeof(OrbitCameraBehaviour))]
    public sealed class OrbitCameraBootstrap : MonoBehaviour
    {
        [SerializeField] OrbitCameraBehaviour _camera;

        [Inject]
        void Construct(IOrbitInputSource source, IOrbitEventPublisher events)
        {
            if (_camera == null) _camera = GetComponent<OrbitCameraBehaviour>();
            _camera.Initialize(source, events);
        }
    }
}