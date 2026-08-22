namespace Dovus.Core.Tuning
{
    /// <summary>Dodge hareketi — dovus-sistemi.md §6.</summary>
    [System.Serializable]
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

        /// <summary>T10: canlı panelin "Sıfırla" ve JSON yükleme yolu — alanları TEK TEK
        /// kopyalar, bu nesnenin kimliğini korur (DodgeState/DodgeMotion aynı referansı tutar).</summary>
        public void CopyFrom(DodgeTuning other)
        {
            StartupMs = other.StartupMs;
            IframeStartMs = other.IframeStartMs;
            IframeMs = other.IframeMs;
            DistanceM = other.DistanceM;
            DurationMs = other.DurationMs;
            CurveExp = other.CurveExp;
            GlideTailMs = other.GlideTailMs;
            CooldownMs = other.CooldownMs;
            TapMaxMs = other.TapMaxMs;
            TapMaxMoveDp = other.TapMaxMoveDp;
        }

        public void ResetToDefaults() => CopyFrom(new DodgeTuning());
    }
}
