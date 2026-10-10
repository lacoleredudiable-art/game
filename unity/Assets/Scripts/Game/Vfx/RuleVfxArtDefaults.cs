using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// Yer tutucu atlas / glif üretim sabitleri ve builder'lar (efekt-motoru dilim).
    /// Final sanat gelince bu dosya küçülür; sayılar Defaults'ta kalır.
    /// </summary>
    public static class RuleVfxArtDefaults
    {
        /// <summary>Yer tutucu: gerçek atlasla aynı 4×2 düzen, 64² hücre.</summary>
        public const int SilhouetteAtlasW = 256;
        public const int SilhouetteAtlasH = 128;
        public const int SilhouetteTailCell = 5;
        public const float SilhouetteArcAmp = 0.15f;
        public const float SilhouetteArcFreq = 3.2f;
        public const float SilhouetteArcSharp = 18f;
        public const float SilhouetteLenFalloff = 1.2f;
        public const float SilhouetteBlobFalloff = 3.5f;

        public const int GlyphCellPx = 64;
        public const int GlyphCols = 8;
        public const int GlyphRows = 3;
        public const float GlyphStrokeA = 40f;
        public const float GlyphStrokeB = 55f;
        public const float GlyphStrokeC = 50f;
        public const float GlyphStrokeNy = 8f;
        public const float GlyphStrokeNx = 10f;
        public const float GlyphDiag = 0.6f;
        public const float GlyphBarX = 0.35f;
        public const float GlyphBarY = 0.45f;
        public const float GlyphBarX2 = 0.2f;
        public const float GlyphPpu = 100f;

        public const float RuneAnchorY = 120f;
        public const float RuneFromOffsetX = 80f;
        public const float RuneFromOffsetY = 40f;
        public const float RuneAdjFromY = 30f;
        public const float RuneAdjDelay = 0.15f;
        public const float RuneAdjDelayInv = 0.85f;
        public const float RuneHueSat = 0.35f;

        public const float TrailAlphaGuard = 0.55f;
        public const float TrailAlphaTail = 0.15f;
        public const float TrailIntensityMin = 0.4f;
        public const float TrailIntensityMax = 2.2f;
        public const float LightningTipLift = 0.35f;
        public const float LightningEndLift = 0.25f;
        public const float LightningJagLift = 0.4f;
        public const float LightningEndAlpha = 0.2f;
        public const float LightningWidthMin = 0.35f;
        public const float LightningIntensityBase = 1.2f;
        public const float CoreHdrMult = 2f;
        public const float CoreHdrHot = 2.5f;
        public const float CoreHdrHit = 2.8f;
        public const float IntensityHit = 2.5f;
        public const float IntensityClaw = 3f;
        public const float IntensityRune = 2.2f;
        public const float IntensityRuneMin = 0.6f;
        public const float IntensityRuneSpan = 1.6f;
        public const float HitHeightM = 1.05f;
        public const float HitHeightClawM = 1.0f;
        public const float HitHeightSparkM = 1.1f;
        public const float SlashArcDeg = 50f;
        public const float SlashArcYScale = 0.35f;
        public const float SlashArcZ = 0.05f;
        public const float ClawWidthM = 0.05f;
        public const float ClawStartFrac = 0.35f;
        public const float ClawEndFrac = 0.65f;
        public const float ClawUpStartM = 0.25f;
        public const float ClawUpEndM = 0.15f;
        public const float ClawSideStart = 0.05f;
        public const float ClawSideEnd = 0.08f;
        public const float ClawRowLift = 0.05f;
        public const float EdgeSparkSpeed = 2.5f;
        public const float EdgeSparkSize = 0.08f;
        public const float EdgeSparkRadius = 0.2f;
        public const float EdgeSparkLift = 0.4f;
        public const float EdgeDestroyPad = 0.15f;
        public const float MeshLifeMin = 0.05f;
        public const float HitForwardPad = 0.35f;
        public const float HitForwardMin = 0.2f;
        public const float DashStartLift = 0.3f;
        public const float SilhouettePathLift = 0.4f;
        public const float AwakenAlphaBias = 0.5f;
        public const float TrailRebuildAlpha = 0.15f;
        public const int LightningJagHashMul = 37;
        public const int LightningJagMod = 7;
        public const float LightningJagDiv = 6f;
        public const int GlyphCellMaxIndex = 23;
        public const int GlyphHueMod = 12;
        public const float SmoothstepThree = 3f;
        public const float SmoothstepTwo = 2f;
        public const int SlashArcSegments = 10;
        public const float SlashArcLastIndex = 9f;

        public static Texture2D BuildSilhouetteAtlas()
        {
            int w = SilhouetteAtlasW;
            int h = SilhouetteAtlasH;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "DragonSilhouetteAtlas_Placeholder",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[w * h];
            int cols = RuleVfxDefaults.SilhouetteAtlasCols;
            int rows = RuleVfxDefaults.SilhouetteAtlasRows;
            int cw = w / cols;
            int ch = h / rows;
            for (int cell = 0; cell < cols * rows; cell++)
            {
                int x0 = cell % cols * cw;
                int y0 = (rows - 1 - cell / cols) * ch;
                for (int y = y0; y < y0 + ch; y++)
                for (int x = x0; x < x0 + cw; x++)
                {
                    float nx = (x - x0) / (float)cw * 2f - 1f;
                    float ny = (y - y0) / (float)ch * 2f - 1f;
                    float arc = cell == SilhouetteTailCell
                        ? Mathf.Exp(-Mathf.Pow(ny - SilhouetteArcAmp * Mathf.Sin(nx * SilhouetteArcFreq), 2f) * SilhouetteArcSharp)
                          * Mathf.Exp(-nx * nx * SilhouetteLenFalloff)
                        : Mathf.Exp(-(nx * nx + ny * ny) * SilhouetteBlobFalloff);
                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(arc * 255f), 0, 255);
                    pixels[y * w + x] = new Color32(255, 200, 120, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        public static Texture2D BuildGlyphAtlas()
        {
            int cell = GlyphCellPx;
            int cols = GlyphCols;
            int rows = GlyphRows;
            var tex = new Texture2D(cell * cols, cell * rows, TextureFormat.RGBA32, false)
            {
                name = "DragonScriptGlyphs_Placeholder",
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[tex.width * tex.height];
            for (int i = 0; i < cols * rows; i++)
            {
                int c = i % cols;
                int r = i / cols;
                int x0 = c * cell;
                int y0 = (rows - 1 - r) * cell;
                for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                {
                    float nx = x / (float)cell * 2f - 1f;
                    float ny = y / (float)cell * 2f - 1f;
                    float stroke = 0f;
                    stroke = Mathf.Max(stroke, Mathf.Exp(-Mathf.Pow(ny - GlyphDiag * nx, 2f) * GlyphStrokeA));
                    stroke = Mathf.Max(stroke, Mathf.Exp(-Mathf.Pow(nx + GlyphBarX, 2f) * GlyphStrokeB) * Mathf.Exp(-ny * ny * GlyphStrokeNy));
                    stroke = Mathf.Max(stroke, Mathf.Exp(-Mathf.Pow(ny + GlyphBarY, 2f) * GlyphStrokeC) * Mathf.Exp(-(nx - GlyphBarX2) * (nx - GlyphBarX2) * GlyphStrokeNx));
                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(stroke * 255f), 0, 255);
                    pixels[(y0 + y) * tex.width + (x0 + x)] = new Color32(255, 220, 140, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
