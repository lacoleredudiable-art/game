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

        /// <summary>Ortak efekt atlası 2048² (harf 2048×768 üstte, siluet 1024×512 altında).</summary>
        /// <summary>Hedef halkası nabzı 2 Hz; bırakınca 0,1 s'de söner.</summary>
        public const float TargetRingPulseHz = 2f;
        public const float TargetRingReleaseFadeSec = 0.1f;
        /// <summary>spec'te yok: "hafifçe" → yarıçap %6, parlaklık dipte %70.</summary>
        public const float TargetRingPulseScale = 0.06f;
        public const float TargetRingPulseMinBrightness = 0.7f;

        public const string AtlasResource = "Vfx/Ejder/Ejder_Vfx_Atlas";
        public const string AtlasMaterial = "Vfx/Ejder/VFX_Kor_Ejder";
        public const int AtlasSizePx = 2048;
        public const int AtlasCellPx = 256;
        public const int AtlasGlyphXPx = 0;
        public const int AtlasGlyphYPx = 0;
        public const int AtlasGlyphCols = 8;
        public const int AtlasGlyphRows = 3;
        public const int AtlasSilhouetteXPx = 0;
        public const int AtlasSilhouetteYPx = 768;
        public const int AtlasSilhouetteCols = 4;
        public const int AtlasSilhouetteRows = 2;
        /// <summary>spec'te yok: siluetlerin sağındaki boşluk.</summary>
        public const int AtlasRingXPx = 1024;
        public const int AtlasRingYPx = 768;
        public const int AtlasRingSizePx = 512;
        /// <summary>spec'te yok: eski işaret decal'ı oranı (0,06 m çizgi / 0,35 m yarıçap).</summary>
        public const float AtlasRingRadiusFrac = 0.84f;
        public const float AtlasRingLineFrac = 0.144f;
        /// <summary>spec'te yok: kül tozu noktası, halkanın sağında.</summary>
        public const int AtlasDustXPx = 1536;
        public const int AtlasDustYPx = 768;
        public const int AtlasDustSizePx = 128;
    }
}
