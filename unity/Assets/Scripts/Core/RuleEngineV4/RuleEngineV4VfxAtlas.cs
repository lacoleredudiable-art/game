using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// "slice_vfx.atlas": kuyruk silueti, harf flaşı, hedef halkası ve kül tozu tek dokuda
    /// (tek VFX_Kor materyali → tek batch). Üstte harfler, altta siluetler, boş yerde halka + toz.
    /// Doku <c>tools/pack-vfx-atlas.py</c> ile bu yerleşimden üretilir.
    /// </summary>
    public sealed class RuleEngineV4VfxAtlas
    {
        public string Resource { get; private set; } = RuleEngineV4SliceVfxDefaults.AtlasResource;
        public string Material { get; private set; } = RuleEngineV4SliceVfxDefaults.AtlasMaterial;
        public int SizePx { get; private set; } = RuleEngineV4SliceVfxDefaults.AtlasSizePx;
        public RuleEngineV4AtlasGrid Glyphs { get; private set; } = DefaultGlyphs();
        public RuleEngineV4AtlasGrid Silhouettes { get; private set; } = DefaultSilhouettes();
        public RuleEngineV4AtlasCell Ring { get; private set; } = DefaultRing();
        public RuleEngineV4AtlasCell Dust { get; private set; } = DefaultDust();
        /// <summary>Halka orta çizgisi yarıçapı / hücre yarı kenarı.</summary>
        public float RingRadiusFrac { get; private set; } = RuleEngineV4SliceVfxDefaults.AtlasRingRadiusFrac;
        /// <summary>Halka çizgi kalınlığı / hücre yarı kenarı.</summary>
        public float RingLineFrac { get; private set; } = RuleEngineV4SliceVfxDefaults.AtlasRingLineFrac;

        public static RuleEngineV4VfxAtlas FromJson(JsonValue n)
        {
            var a = new RuleEngineV4VfxAtlas();
            if (n.Kind != JsonKind.Object)
                return a;
            a.Resource = n["resource"].AsString(a.Resource);
            a.Material = n["material"].AsString(a.Material);
            a.SizePx = n["size_px"].AsInt(a.SizePx);
            a.Glyphs = RuleEngineV4AtlasGrid.FromJson(n["glyphs"], a.Glyphs);
            a.Silhouettes = RuleEngineV4AtlasGrid.FromJson(n["silhouettes"], a.Silhouettes);
            JsonValue ring = n["ring"];
            a.Ring = RuleEngineV4AtlasCell.FromJson(ring, a.Ring);
            if (ring.Kind == JsonKind.Object)
            {
                a.RingRadiusFrac = ring["radius_frac"].AsFloat(a.RingRadiusFrac);
                a.RingLineFrac = ring["line_frac"].AsFloat(a.RingLineFrac);
            }
            a.Dust = RuleEngineV4AtlasCell.FromJson(n["dust"], a.Dust);
            return a;
        }

        public RuleEngineV4UvRect GlyphUv(int cell) => Glyphs.Cell(cell, SizePx);
        public RuleEngineV4UvRect SilhouetteUv(int cell) => Silhouettes.Cell(cell, SizePx);
        public RuleEngineV4UvRect RingUv => Ring.Uv(SizePx);
        public RuleEngineV4UvRect DustUv => Dust.Uv(SizePx);

        /// <summary>Halka orta çizgisi <paramref name="radiusM"/> olacak dörtgenin yarı kenarı.</summary>
        public float RingQuadHalfM(float radiusM) => RingRadiusFrac > 0f ? radiusM / RingRadiusFrac : radiusM;

        static RuleEngineV4AtlasGrid DefaultGlyphs() => new(
            RuleEngineV4SliceVfxDefaults.AtlasGlyphXPx,
            RuleEngineV4SliceVfxDefaults.AtlasGlyphYPx,
            RuleEngineV4SliceVfxDefaults.AtlasGlyphCols,
            RuleEngineV4SliceVfxDefaults.AtlasGlyphRows,
            RuleEngineV4SliceVfxDefaults.AtlasCellPx);

        static RuleEngineV4AtlasGrid DefaultSilhouettes() => new(
            RuleEngineV4SliceVfxDefaults.AtlasSilhouetteXPx,
            RuleEngineV4SliceVfxDefaults.AtlasSilhouetteYPx,
            RuleEngineV4SliceVfxDefaults.AtlasSilhouetteCols,
            RuleEngineV4SliceVfxDefaults.AtlasSilhouetteRows,
            RuleEngineV4SliceVfxDefaults.AtlasCellPx);

        static RuleEngineV4AtlasCell DefaultRing() => new(
            RuleEngineV4SliceVfxDefaults.AtlasRingXPx,
            RuleEngineV4SliceVfxDefaults.AtlasRingYPx,
            RuleEngineV4SliceVfxDefaults.AtlasRingSizePx);

        static RuleEngineV4AtlasCell DefaultDust() => new(
            RuleEngineV4SliceVfxDefaults.AtlasDustXPx,
            RuleEngineV4SliceVfxDefaults.AtlasDustYPx,
            RuleEngineV4SliceVfxDefaults.AtlasDustSizePx);
    }
}
