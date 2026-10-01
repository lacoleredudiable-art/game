namespace Dovus.Core.Tuning
{
    /// <summary>Ayrı dodge kontrolünün hareket ve tap ayarları.</summary>
    [System.Serializable]
    public class DodgeTuning
    {
        public int StartupMs = 10;
        public int IframeStartMs = 0;
        /// <summary>element-sistemi.json player_stats.i_frame_on_dodge_ms (karadul.json maps_to aynı).</summary>
        public int IframeMs = 260;
        public float DistanceM = 4f;
        public int DurationMs = 250;
        public float CurveExp = 3.6f;
        public int GlideTailMs = 120;
        public int CooldownMs = 360;

        // Dragon Nest tarzı hak + mükemmel sıyırma. Sayılar his varsayılanı;
        // element-sistemi.json'da dodge hak alanı yok (durum.md).
        public int MaxCharges = 2;
        public int ChargeRechargeMs = 4000;
        public int PerfectWindowMs = 150;
        public float PerfectNextHitMult = 1.3f;
        /// <summary>S1: mükemmel sıyırmanın ×PerfectNextHitMult bonusu bu süre içinde vurulmazsa düşer (his varsayılanı; spec'te yok).</summary>
        public int PerfectNextHitWindowMs = 2000;
        public float PerfectChargeRefund = 1f;
        public float PerfectFeelSec = 0.2f;
        public float EdgeGapM = 0.15f;
        public int TelegraphLeadMs = 700;

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
            MaxCharges = other.MaxCharges;
            ChargeRechargeMs = other.ChargeRechargeMs;
            PerfectWindowMs = other.PerfectWindowMs;
            PerfectNextHitMult = other.PerfectNextHitMult;
            PerfectNextHitWindowMs = other.PerfectNextHitWindowMs;
            PerfectChargeRefund = other.PerfectChargeRefund;
            PerfectFeelSec = other.PerfectFeelSec;
            EdgeGapM = other.EdgeGapM;
            TelegraphLeadMs = other.TelegraphLeadMs;
        }

        public void ResetToDefaults() => CopyFrom(new DodgeTuning());
    }
}
