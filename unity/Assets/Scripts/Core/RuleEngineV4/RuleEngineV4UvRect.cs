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
}
