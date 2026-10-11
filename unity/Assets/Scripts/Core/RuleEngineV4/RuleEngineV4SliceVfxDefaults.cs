namespace Dovus.Core.RuleEngineV4
{
    /// <summary>"slice_vfx" yoksa geçici değerler; nihai ayar kural-motoru-v4.json.</summary>
    public static class RuleEngineV4SliceVfxDefaults
    {
        /// <summary>efekt-motoru §3.1: kor izi ömrü ~0,25 s.</summary>
        public const float BladeTrailLifeSec = 0.25f;
        /// <summary>spec'te yok: "uçta incelir" → kuyrukta uç çizgisine kapanır.</summary>
        public const float BladeTrailTailWidthFrac = 0f;
        /// <summary>spec'te yok: kabza kenarı uç kenarından sönük.</summary>
        public const float BladeTrailHiltAlpha = 0.55f;
        /// <summary>spec'te yok.</summary>
        public const float BladeTrailIntensity = 2.2f;
    }
}
