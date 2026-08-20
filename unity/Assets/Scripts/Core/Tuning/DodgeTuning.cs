namespace Dovus.Core.Tuning
{
    /// <summary>Dodge hareketi — dovus-sistemi.md §6.</summary>
    public class DodgeTuning
    {
        public int StartupMs = 20;
        public int IframeStartMs = 0;
        public int IframeMs = 260;
        public float DistanceM = 3.8f;
        public int DurationMs = 260;
        public float CurveExp = 3.2f;
        public int GlideTailMs = 220;
        public int CooldownMs = 420;

        // Beşgen merkezi kısa dokunma — dovus-sistemi.md §2
        public int TapMaxMs = 180;
        public int TapMaxMoveDp = 12;
    }
}
