#if ENABLE_INPUT_SYSTEM
using UnityEngine;

namespace VK.OrbitCamera.InputSystem
{
    internal static class InputSystemOrbitRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Install()
        {
            if (OrbitInputSourceFactory.Override == null)
                OrbitInputSourceFactory.Override = () => new InputSystemOrbitSource(OrbitInputConfig.Default);
        }
    }
}
#endif