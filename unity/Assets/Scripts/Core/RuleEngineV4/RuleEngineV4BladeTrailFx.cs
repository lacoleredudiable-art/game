using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
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
}
