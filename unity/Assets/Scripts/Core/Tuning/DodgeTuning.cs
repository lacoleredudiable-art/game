namespace Dovus.Core.Tuning
{
    /// <summary>Ayrı dodge kontrolünün hareket ve tap ayarları.</summary>
    [System.Serializable]
    public class DodgeTuning
    {
        public int StartupMs = 10;
        public int IframeStartMs = 0;
        /// <summary>T_tek: tek dodge hasarsızlık penceresi. 15-dodge-tasarimi §3.4: TEST DEĞERİ 0,5 s, kesin değer dengelemede (Aşama 8).</summary>
        public int IframeMs = 500;
        /// <summary>S2: element-sistemi.json player_stats.dodge_distance_m (test eşitliği korur).</summary>
        public float DistanceM = 3.8f;
        public int DurationMs = 250;
        public float CurveExp = 3.6f;
        /// <summary>O-4.4a süzülme fazı 0,40 s. Hareket (10+250+400) birleşme penceresini (500 ms) örter; kısa kuyrukta 380-500 ms arası basış takla olmazdı.</summary>
        public int GlideTailMs = 400;
        // S2: CooldownMs (360) kaldırıldı — haklar her zaman var olduğu için hiç okunmuyordu.
        // JSON dodge_cooldown_sec 0.42 — 2 hak × 6 sn dolum (PLAN 1.3).

        // Dragon Nest tarzı hak + mükemmel sıyırma. Sayılar his varsayılanı;
        // element-sistemi.json'da dodge hak alanı yok (durum.md).
        public int MaxCharges = 2;
        public int ChargeRechargeMs = 6000;
        /// <summary>Birleşme penceresi = T_tek (15-dodge-tasarimi §3.4, D-4). İlk basıştan sonra bu süre dolmadan ikinci basış zincirdir. Ayrı ayarlanabilir.</summary>
        public int DoubleTapWindowMs = 500;
        /// <summary>T_zincir: birleşik dodge hasarsızlık süresi, İLK basıştan itibaren sabit (Model A). TEST DEĞERİ 1,3 s, kesin değer dengelemede.</summary>
        public int CombinedIframeMs = 1300;
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

        // O-4.9 engelde kayma. Hepsi (ölçüm) test başlangıç değeri, tahmin; telefonda ayarlanır.
        /// <summary>Kayma bütçesi tabanı: bütçe = kalan yol × max(t, bu).</summary>
        public float SlideMinRatio = 0.5f;
        /// <summary>t bu eşikten büyükse taraf çubuğun teğet işaretinden gelir; değilse motor seçer.</summary>
        public float EngineChoiceT = 0.17f;
        /// <summary>Motorun taraf seçerken ileri taradığı mesafe (m).</summary>
        public float SlideScanPadM = 0.5f;
        /// <summary>Kayma adımı başına en çok iterasyon.</summary>
        public int SlideMaxIter = 4;
        /// <summary>Adımda gerçek ilerleme beklenenin bu oranından azsa (köşe) kayma durur.</summary>
        public float SlideStallRatio = 0.1f;

        // Merkez düğmenin sürükleme eşiği (aşan sürükleme çizimdir). O1: süre eşiği (TapMaxMs 180)
        // kaldırıldı — kaçış/silah basınca tetiklenir, uzun basış düşmez.
        public int TapMaxMoveDp = 12;

        /// <summary>T10: canlı panelin "Sıfırla" ve JSON yükleme yolu — alanları TEK TEK
        /// kopyalar, bu nesnenin kimliğini korur (DodgeState/DodgeMotionController aynı referansı tutar).</summary>
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
            SlideMinRatio = other.SlideMinRatio;
            EngineChoiceT = other.EngineChoiceT;
            SlideScanPadM = other.SlideScanPadM;
            SlideMaxIter = other.SlideMaxIter;
            SlideStallRatio = other.SlideStallRatio;
        }

        public void ResetToDefaults() => CopyFrom(new DodgeTuning());
    }
}
