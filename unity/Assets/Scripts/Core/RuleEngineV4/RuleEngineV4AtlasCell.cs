using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
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
}
