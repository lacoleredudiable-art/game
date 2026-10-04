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
        /// <summary>S2: element-sistemi.json player_stats.dodge_distance_m (test eşitliği korur).</summary>
        public float DistanceM = 3.8f;
        public int DurationMs = 250;
        public float CurveExp = 3.6f;
        public int GlideTailMs = 120;
        // S2: CooldownMs (360) kaldırıldı — haklar her zaman var olduğu için hiç okunmuyordu.
        // JSON dodge_cooldown_sec 0.42 — 2 hak × 6 sn dolum (PLAN 1.3).

        // Dragon Nest tarzı hak + mükemmel sıyırma. Sayılar his varsayılanı;
        // element-sistemi.json'da dodge hak alanı yok (durum.md).
        public int MaxCharges = 2;
        public int ChargeRechargeMs = 6000;
        /// <summary>İlk basıştan sonra bu süre içinde ikinci basış birleşik dodge (his varsayılanı; spec'te yok).</summary>
        public int DoubleTapWindowMs = 200;
        /// <summary>Birleşik dodge i-frame süresi, ilk dodge başlangıcından (his varsayılanı; spec'te yok → 2×IframeMs).</summary>
        public int CombinedIframeMs = 520;
        /// <summary>Birleşik kaçış mesafe çarpanı (his varsayılanı; spec'te yok).</summary>
        public float CombinedDistanceMult = 1.6f;
        /// <summary>Birleşik kaçış süre çarpanı (his varsayılanı; spec'te yok).</summary>
        public float CombinedDurationMult = 1.4f;
        /// <summary>O2: TEK mükemmel pencere — PERFECT derecesi, yük iadesi ve sonraki vuruş bonusu (spec'te yok → 150).</summary>
        public int PerfectWindowMs = 150;
        public float PerfectNextHitMult = 1.3f;
        /// <summary>S1: mükemmel sıyırmanın ×PerfectNextHitMult bonusu bu süre içinde vurulmazsa düşer (his varsayılanı; spec'te yok).</summary>
        public int PerfectNextHitWindowMs = 2000;
        public float PerfectChargeRefund = 1f;
        public float PerfectFeelSec = 0.2f;
        public float EdgeGapM = 0.15f;
        public int TelegraphLeadMs = 700;

        // Merkez düğmenin sürükleme eşiği (aşan sürükleme çizimdir). O1: süre eşiği (TapMaxMs 180)
        // kaldırıldı — kaçış/silah basınca tetiklenir, uzun basış düşmez.
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
            TapMaxMoveDp = other.TapMaxMoveDp;
            MaxCharges = other.MaxCharges;
            ChargeRechargeMs = other.ChargeRechargeMs;
            DoubleTapWindowMs = other.DoubleTapWindowMs;
            CombinedIframeMs = other.CombinedIframeMs;
            CombinedDistanceMult = other.CombinedDistanceMult;
            CombinedDurationMult = other.CombinedDurationMult;
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
