namespace Dovus.Game.Diagnostics
{
    /// <summary>Composition/DevTools debug panel girdi durumu (release'de null).</summary>
    public static class DebugPanelInput
    {
        public static IDebugPanelInputState State { get; set; }
    }
}
