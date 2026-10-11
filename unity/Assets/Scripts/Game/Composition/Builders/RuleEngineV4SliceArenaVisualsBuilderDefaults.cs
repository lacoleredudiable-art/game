namespace Dovus.Game.Composition.Builders
{
    public static class RuleEngineV4SliceArenaVisualsBuilderDefaults
    {
        public const string RootName = "ArenaVisuals";
        public const string RegistryResource = "Arena/ArenaVisualRegistry";
        public const string VisualChildPrefix = "Visual_";
        /// <summary>Yerleşik "Ignore Raycast": Physics.DefaultRaycastLayers dışı; görseller zaten collider'sız.</summary>
        public const int VisualLayer = 2;
        /// <summary>Ocak kor diski zeminden kalkış (z-fighting payı).</summary>
        public const float HearthEmberLiftM = 0.03f;
        /// <summary>Quad'ı XZ düzlemine yatırır (yüz +Y).</summary>
        public const float FlatQuadPitchDeg = 90f;
    }
}
