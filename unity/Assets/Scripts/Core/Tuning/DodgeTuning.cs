namespace Dovus.Core.Tuning
{
    /// <summary>Ayrı dodge kontrolünün hareket ve tap ayarları.</summary>
    [System.Serializable]
    public class DodgeTuning
    {
        public int StartupMs = 10;
        public int IframeStartMs = 0;
        public int IframeMs = 220;
        public float DistanceM = 3.2f;
        public int DurationMs = 190;
        public float CurveExp = 3.6f;
        public int GlideTailMs = 120;
        public int CooldownMs = 360;

        // Altıgen dışındaki ayrı dodge düğmesinin tap eşikleri.
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
