using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Doku UV dikdörtgeni; (X, Y) sol alt köşe (Unity UV'si alttan başlar).</summary>
    public readonly struct RuleEngineV4UvRect
    {
        public readonly float X;
        public readonly float Y;
        public readonly float W;
        public readonly float H;

        public RuleEngineV4UvRect(float x, float y, float w, float h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        public float XMax => X + W;
        public float YMax => Y + H;

        /// <summary>Sol yarı / sağ yarı (çatlakla bölünen harf).</summary>
        public RuleEngineV4UvRect LeftHalf => new(X, Y, W * 0.5f, H);
        public RuleEngineV4UvRect RightHalf => new(X + W * 0.5f, Y, W * 0.5f, H);

        /// <summary>Piksel bölgesi (üstten) → UV; <paramref name="atlasPx"/> kare atlas kenarı.</summary>
        public static RuleEngineV4UvRect FromTopPixels(int xPx, int yTopPx, int wPx, int hPx, int atlasPx)
        {
            float inv = atlasPx > 0 ? 1f / atlasPx : 0f;
            return new RuleEngineV4UvRect(xPx * inv, 1f - (yTopPx + hPx) * inv, wPx * inv, hPx * inv);
        }
    }

    /// <summary>Atlas içinde ızgara bölge; hücre 0 = sol üst, satır satır.</summary>
    public sealed class RuleEngineV4AtlasGrid
    {
        public int XPx { get; private set; }
        public int YPx { get; private set; }
        public int Cols { get; private set; }
        public int Rows { get; private set; }
        public int CellPx { get; private set; }
        public int Count => Cols * Rows;
        public int WidthPx => Cols * CellPx;
        public int HeightPx => Rows * CellPx;

        public RuleEngineV4AtlasGrid(int xPx, int yPx, int cols, int rows, int cellPx)
        {
            XPx = xPx;
            YPx = yPx;
            Cols = cols;
            Rows = rows;
            CellPx = cellPx;
        }

        public static RuleEngineV4AtlasGrid FromJson(JsonValue n, RuleEngineV4AtlasGrid fallback)
        {
            if (n.Kind != JsonKind.Object)
                return fallback;
            return new RuleEngineV4AtlasGrid(
                n["x_px"].AsInt(fallback.XPx),
                n["y_px"].AsInt(fallback.YPx),
                n["cols"].AsInt(fallback.Cols),
                n["rows"].AsInt(fallback.Rows),
                n["cell_px"].AsInt(fallback.CellPx));
        }

        /// <summary>Hücre dizini sınırlanır (0…Count−1).</summary>
        public RuleEngineV4UvRect Cell(int index, int atlasPx)
        {
            int last = Count - 1;
            int idx = index < 0 ? 0 : index > last ? last : index;
            int col = Cols > 0 ? idx % Cols : 0;
            int row = Cols > 0 ? idx / Cols : 0;
            return RuleEngineV4UvRect.FromTopPixels(XPx + col * CellPx, YPx + row * CellPx, CellPx, CellPx, atlasPx);
        }
    }

    /// <summary>Atlas içinde tek kare hücre (hedef halkası, kül tozu).</summary>
    public sealed class RuleEngineV4AtlasCell
    {
        public int XPx { get; private set; }
        public int YPx { get; private set; }
        public int SizePx { get; private set; }

        public RuleEngineV4AtlasCell(int xPx, int yPx, int sizePx)
        {
            XPx = xPx;
            YPx = yPx;
            SizePx = sizePx;
        }

        public static RuleEngineV4AtlasCell FromJson(JsonValue n, RuleEngineV4AtlasCell fallback)
        {
            if (n.Kind != JsonKind.Object)
                return fallback;
            return new RuleEngineV4AtlasCell(
                n["x_px"].AsInt(fallback.XPx),
                n["y_px"].AsInt(fallback.YPx),
                n["size_px"].AsInt(fallback.SizePx));
        }

        public RuleEngineV4UvRect Uv(int atlasPx) =>
            RuleEngineV4UvRect.FromTopPixels(XPx, YPx, SizePx, SizePx, atlasPx);
    }

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
