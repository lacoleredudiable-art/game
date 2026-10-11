using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// "Atılmadı" geri bildirimi: kombo tanındı ama atılmadı (yalnız mana yetmedi / bekleme sürüyor;
    /// ıska ve boşa gitmede yok). Harf kül grisi ve sönük belirir, <see cref="DurationSec"/> içinde
    /// <see cref="Shakes"/> kez titreyip dağılır; mana yetmiyorsa altından kül tozu düşer,
    /// beklemedeyse harf çatlakla ikiye bölünür. Uzunluklar harf yerleşimi pikseli (referans genişlik).
    /// </summary>
    public sealed class RuleEngineV4NotCastFx
    {
        const float TwoPi = (float)(2.0 * System.Math.PI);

        public float DurationSec { get; private set; } = RuleEngineV4SliceVfxDefaults.NotCastDurationSec;
        public int Shakes { get; private set; } = RuleEngineV4SliceVfxDefaults.NotCastShakes;
        /// <summary>Titreme süresi / toplam süre; kalan kısımda harf dağılır.</summary>
        public float ShakeUntilFrac { get; private set; } = RuleEngineV4SliceVfxDefaults.NotCastShakeUntilFrac;
        public float ShakePx { get; private set; } = RuleEngineV4SliceVfxDefaults.NotCastShakePx;
        public float AshR { get; private set; } = RuleEngineV4SliceVfxDefaults.NotCastAshR;
        public float AshG { get; private set; } = RuleEngineV4SliceVfxDefaults.NotCastAshG;
        public float AshB { get; private set; } = RuleEngineV4SliceVfxDefaults.NotCastAshB;
        /// <summary>Sönük harf alfası (normal flaş = 1).</summary>
        public float Alpha { get; private set; } = RuleEngineV4SliceVfxDefaults.NotCastAlpha;
        public int DustCount { get; private set; } = RuleEngineV4SliceVfxDefaults.NotCastDustCount;
        public float DustFallPx { get; private set; } = RuleEngineV4SliceVfxDefaults.NotCastDustFallPx;
        public float DustSizePx { get; private set; } = RuleEngineV4SliceVfxDefaults.NotCastDustSizePx;
        public float DustSpreadPx { get; private set; } = RuleEngineV4SliceVfxDefaults.NotCastDustSpreadPx;
        public float CrackGapPx { get; private set; } = RuleEngineV4SliceVfxDefaults.NotCastCrackGapPx;

        public static RuleEngineV4NotCastFx FromJson(JsonValue n)
        {
            var f = new RuleEngineV4NotCastFx();
            if (n.Kind != JsonKind.Object)
                return f;
            f.DurationSec = n["duration_sec"].AsFloat(f.DurationSec);
            f.Shakes = n["shakes"].AsInt(f.Shakes);
            f.ShakeUntilFrac = n["shake_until_frac"].AsFloat(f.ShakeUntilFrac);
            f.ShakePx = n["shake_px"].AsFloat(f.ShakePx);
            var ash = n["ash_rgb"].AsArray();
            if (ash.Count >= 3)
            {
                f.AshR = ash[0].AsFloat(f.AshR);
                f.AshG = ash[1].AsFloat(f.AshG);
                f.AshB = ash[2].AsFloat(f.AshB);
            }
            f.Alpha = n["alpha"].AsFloat(f.Alpha);
            f.DustCount = n["dust_count"].AsInt(f.DustCount);
            f.DustFallPx = n["dust_fall_px"].AsFloat(f.DustFallPx);
            f.DustSizePx = n["dust_size_px"].AsFloat(f.DustSizePx);
            f.DustSpreadPx = n["dust_spread_px"].AsFloat(f.DustSpreadPx);
            f.CrackGapPx = n["crack_gap_px"].AsFloat(f.CrackGapPx);
            return f;
        }

        /// <summary>Yalnız mana yetmedi / bekleme; diğer her sonuç (ıska, boşa gitme, atıldı) geri bildirim almaz.</summary>
        public static bool Applies(RuleEngineV4CastDenial denial) =>
            denial == RuleEngineV4CastDenial.Mana || denial == RuleEngineV4CastDenial.Cooldown;

        public bool Done(float ageSec) => ageSec >= DurationSec;

        float U(float ageSec)
        {
            float u = DurationSec > 0f ? ageSec / DurationSec : 1f;
            return u < 0f ? 0f : u > 1f ? 1f : u;
        }

        /// <summary>
        /// Yaş → yatay titreme (px), alfa, dağılma (0…1, dissolve). Titreme penceresinde
        /// <see cref="Shakes"/> tam salınım; sonra harf dağılarak söner.
        /// </summary>
        public void Sample(float ageSec, out float shakePx, out float alpha, out float scatter)
        {
            float u = U(ageSec);
            float until = ShakeUntilFrac > 0f ? ShakeUntilFrac : 1f;
            if (u < until)
            {
                shakePx = ShakePx * (float)System.Math.Sin(TwoPi * Shakes * (u / until));
                scatter = 0f;
            }
            else
            {
                shakePx = 0f;
                scatter = until < 1f ? (u - until) / (1f - until) : 1f;
            }
            alpha = Alpha * (1f - scatter);
        }

        /// <summary>Kül tozunun harf altından düşüş mesafesi (px).</summary>
        public float DustFall(float ageSec) => DustFallPx * U(ageSec);

        /// <summary>Toz tanesi <paramref name="i"/> yatay konumu (px, harf merkezine göre).</summary>
        public float DustX(int i) =>
            DustCount > 1 ? DustSpreadPx * ((float)i / (DustCount - 1) - 0.5f) : 0f;

        /// <summary>Çatlak aralığı (px): titreme penceresinde açılır, sonra sabit.</summary>
        public float CrackGap(float ageSec)
        {
            float until = ShakeUntilFrac > 0f ? ShakeUntilFrac : 1f;
            float k = U(ageSec) / until;
            return CrackGapPx * (k > 1f ? 1f : k);
        }
    }
}
