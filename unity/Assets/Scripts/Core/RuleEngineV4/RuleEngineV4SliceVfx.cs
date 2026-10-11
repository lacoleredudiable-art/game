using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// kural-motoru-v4.json "slice_vfx": dilim efekt ayarı (sayılar kodda gömülmez).
    /// Blok ya da alan yoksa <see cref="RuleEngineV4SliceVfxDefaults"/>.
    /// </summary>
    public sealed class RuleEngineV4SliceVfx
    {
        public RuleEngineV4BladeTrailFx BladeTrail { get; private set; } = new();
        public RuleEngineV4VfxAtlas Atlas { get; private set; } = new();
        public RuleEngineV4TargetRingFx TargetRing { get; private set; } = new();
        public RuleEngineV4NotCastFx NotCast { get; private set; } = new();

        public static RuleEngineV4SliceVfx FromJson(JsonValue root)
        {
            var v = new RuleEngineV4SliceVfx();
            JsonValue node = root["slice_vfx"];
            if (node.Kind != JsonKind.Object)
                return v;
            v.BladeTrail = RuleEngineV4BladeTrailFx.FromJson(node["blade_trail"]);
            v.Atlas = RuleEngineV4VfxAtlas.FromJson(node["atlas"]);
            v.TargetRing = RuleEngineV4TargetRingFx.FromJson(node["target_ring"]);
            v.NotCast = RuleEngineV4NotCastFx.FromJson(node["not_cast"]);
            return v;
        }

        public static RuleEngineV4SliceVfx FromJson(string json) => FromJson(MiniJson.Parse(json));
    }

    /// <summary>
    /// Kor izi: Vfx_Kabza↔Vfx_Tip şeridi. Örnek başına ömür; yaşlandıkça kabza tarafı uca doğru
    /// kapanır (iz kuyrukta incelir) ve söner.
    /// </summary>
    public sealed class RuleEngineV4BladeTrailFx
    {
        public float LifeSec { get; private set; } = RuleEngineV4SliceVfxDefaults.BladeTrailLifeSec;
        /// <summary>Ömür sonunda kalan kabza→uç genişliği (0 = uç çizgisine incelir).</summary>
        public float TailWidthFrac { get; private set; } = RuleEngineV4SliceVfxDefaults.BladeTrailTailWidthFrac;
        /// <summary>Kabza kenarının uç kenarına göre alfası.</summary>
        public float HiltAlpha { get; private set; } = RuleEngineV4SliceVfxDefaults.BladeTrailHiltAlpha;
        public float Intensity { get; private set; } = RuleEngineV4SliceVfxDefaults.BladeTrailIntensity;

        public static RuleEngineV4BladeTrailFx FromJson(JsonValue n)
        {
            var t = new RuleEngineV4BladeTrailFx();
            if (n.Kind != JsonKind.Object)
                return t;
            t.LifeSec = n["life_sec"].AsFloat(t.LifeSec);
            t.TailWidthFrac = n["tail_width_frac"].AsFloat(t.TailWidthFrac);
            t.HiltAlpha = n["hilt_alpha"].AsFloat(t.HiltAlpha);
            t.Intensity = n["intensity"].AsFloat(t.Intensity);
            return t;
        }

        /// <summary>Örnek yaşı → (genişlik çarpanı, alfa); u = yaş / ömür.</summary>
        public void Shape(float ageSec, out float widthFrac, out float alpha)
        {
            float u = LifeSec > 0f ? ageSec / LifeSec : 1f;
            if (u < 0f)
                u = 0f;
            if (u > 1f)
                u = 1f;
            widthFrac = 1f + (TailWidthFrac - 1f) * u;
            alpha = 1f - u;
        }
    }

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

    /// <summary>
    /// Hedef seçme halkası: gösterilirken <see cref="PulseHz"/> ile hafif nabız (ölçek + parlaklık),
    /// parmak bırakılınca <see cref="ReleaseFadeSec"/> içinde doğrusal söner.
    /// </summary>
    public sealed class RuleEngineV4TargetRingFx
    {
        const float TwoPi = (float)(2.0 * System.Math.PI);

        public float PulseHz { get; private set; } = RuleEngineV4SliceVfxDefaults.TargetRingPulseHz;
        /// <summary>Nabız tepesinde yarıçap artışı (oran).</summary>
        public float PulseScale { get; private set; } = RuleEngineV4SliceVfxDefaults.TargetRingPulseScale;
        /// <summary>Nabız dibinde parlaklık (tepe = 1).</summary>
        public float PulseMinBrightness { get; private set; } = RuleEngineV4SliceVfxDefaults.TargetRingPulseMinBrightness;
        public float ReleaseFadeSec { get; private set; } = RuleEngineV4SliceVfxDefaults.TargetRingReleaseFadeSec;

        public static RuleEngineV4TargetRingFx FromJson(JsonValue n)
        {
            var r = new RuleEngineV4TargetRingFx();
            if (n.Kind != JsonKind.Object)
                return r;
            r.PulseHz = n["pulse_hz"].AsFloat(r.PulseHz);
            r.PulseScale = n["pulse_scale"].AsFloat(r.PulseScale);
            r.PulseMinBrightness = n["pulse_min_brightness"].AsFloat(r.PulseMinBrightness);
            r.ReleaseFadeSec = n["release_fade_sec"].AsFloat(r.ReleaseFadeSec);
            return r;
        }

        /// <summary>Gösterildikten <paramref name="shownSec"/> sonra: 0 (dip) … 1 (tepe), t = 0'da dip.</summary>
        public float PulsePhase(float shownSec) =>
            0.5f - 0.5f * (float)System.Math.Cos(TwoPi * PulseHz * shownSec);

        public void Pulse(float shownSec, out float scale, out float brightness)
        {
            float p = PulsePhase(shownSec);
            scale = 1f + PulseScale * p;
            brightness = PulseMinBrightness + (1f - PulseMinBrightness) * p;
        }

        /// <summary>Bırakıldıktan <paramref name="sinceReleaseSec"/> sonra alfa (1 → 0).</summary>
        public float ReleaseAlpha(float sinceReleaseSec)
        {
            if (ReleaseFadeSec <= 0f)
                return 0f;
            float a = 1f - sinceReleaseSec / ReleaseFadeSec;
            return a < 0f ? 0f : a > 1f ? 1f : a;
        }
    }
}
