using System.Runtime.InteropServices;
using UnityEngine;

internal static class WebGLKeyboardGuard
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void InstallWebGLKeyboardGuard();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        WebGLInput.captureAllKeyboardInput = true;
        InstallWebGLKeyboardGuard();
    }
#endif
}
