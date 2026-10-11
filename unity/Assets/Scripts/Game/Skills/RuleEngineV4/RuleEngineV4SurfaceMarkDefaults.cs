namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Statik yüzey işaret decal'ı (halka) görünümü — spec'te yok; geçici görsel varsayılan.</summary>
    public static class RuleEngineV4SurfaceMarkDefaults
    {
        public const float DecalRadiusM = 0.35f;
        public const float DecalLineWidthM = 0.06f;
        /// <summary>Yüzeyle z-çakışmasını önlemek için normal boyunca kaydırma.</summary>
        public const float DecalSurfaceOffsetM = 0.02f;
        public const int DecalSegments = 32;
        public const string DecalShader = "Sprites/Default";
        /// <summary>Normal dikeye bu kadar yakınsa bakış "yukarı"sı ileri eksenden alınır.</summary>
        public const float VerticalNormalDot = 0.99f;
    }
}
