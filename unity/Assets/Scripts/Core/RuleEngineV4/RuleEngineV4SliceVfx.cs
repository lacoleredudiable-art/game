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

        public static RuleEngineV4SliceVfx FromJson(JsonValue root)
        {
            var v = new RuleEngineV4SliceVfx();
            JsonValue node = root["slice_vfx"];
            if (node.Kind != JsonKind.Object)
                return v;
            v.BladeTrail = RuleEngineV4BladeTrailFx.FromJson(node["blade_trail"]);
            v.Atlas = RuleEngineV4VfxAtlas.FromJson(node["atlas"]);
            v.TargetRing = RuleEngineV4TargetRingFx.FromJson(node["target_ring"]);
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
