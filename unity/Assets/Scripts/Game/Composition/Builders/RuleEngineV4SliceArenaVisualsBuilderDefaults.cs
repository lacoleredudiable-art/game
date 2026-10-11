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
        /// <summary>Ortam_DitherLit _HALF_CLIP eşiği (nesne uzayı x); global, Properties bloğunda yok.</summary>
        public const string HalfClipShaderProperty = "_DovusHalfClipX";
        /// <summary>Yarım düzeni kapalıyken kesim yok: eşik mesh'in çok dışında.</summary>
        public const float HalfClipOff = -1e6f;
        public const string HalfRightName = "HalfR";
        public const string HalfLeftName = "HalfL";
    }
}
