#if UNITY_EDITOR || DOVUS_DEBUG
using Dovus.Game.DevTools;
using Dovus.Game.Diagnostics;

namespace Dovus.Game.Composition
{
    /// <summary>Composition kökünde debug arayüz köprüleri (release'de derlenmez).</summary>
    public static class DevToolsRuntimeWiring
    {
        public static void Apply(GameSceneRuntime runtime)
        {
            if (runtime == null)
                return;
            runtime.DebugPanelInput = new DevToolsPanelInputState();
            runtime.DebugPanelsChrome = new DevToolsDebugPanelsChromeSink();
        }
    }
}
#endif
