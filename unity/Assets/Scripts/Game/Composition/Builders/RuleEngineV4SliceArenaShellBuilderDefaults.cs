namespace Dovus.Game.Composition.Builders
{
    public static class RuleEngineV4SliceArenaShellBuilderDefaults
    {
        public const string WallName = "OuterWallVisual";
        public const string GroundName = "BeyondWallGround";
        /// <summary>"Düz koyu taş" (O-7.3): parlama yok.</summary>
        public const float WallSmoothness = 0f;
        public const float GroundSmoothness = 0f;
        /// <summary>Duvar ötesi zemin, arena zemininin altında kalır (z-fighting payı).</summary>
        public const float GroundDropM = 0.03f;
        /// <summary>spec'te yok: 300 m diskte kenar başına ~20 m kiriş; kenar sisin içinde.</summary>
        public const int GroundSegments = 96;
        public const string LitShader = "Universal Render Pipeline/Lit";
        public const string BaseColorProperty = "_BaseColor";
        public const string SmoothnessProperty = "_Smoothness";
        public const string BaseMapProperty = "_BaseMap";
        public const string EmissionColorProperty = "_EmissionColor";
        public const string EmissionMapProperty = "_EmissionMap";
        public const string EmissionKeyword = "_EMISSION";
        /// <summary>Zemin solma rampası: iki teksel (baş, son), arası çift doğrusal.</summary>
        public const int RampTexels = 2;
    }
}
