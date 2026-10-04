using Dovus.Game.Diagnostics;

namespace Dovus.Game.DevTools
{
    /// <summary>Composition kökünde debug arayüz köprüleri (release'de derlenmez).</summary>
    public static class DevToolsCompositionWiring
    {
        public static void Apply()
        {
            DebugPanelInput.State = new DevToolsPanelInputState();
            DebugPanelsChromeAccess.Sink = new DevToolsDebugPanelsChromeSink();
        }
    }
}
