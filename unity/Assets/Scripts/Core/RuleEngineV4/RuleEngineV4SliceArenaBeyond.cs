using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// slice_arena_visuals.beyond_wall (O-7.7, O-8.7): duvarın ötesinde düz zemin ve üstel sis. Uzak zemin kenarı
    /// sisin içinde kaybolur; kamera arka planı sis rengidir.
    /// </summary>
    public sealed class RuleEngineV4SliceArenaBeyond
    {
        /// <summary>JSON'da <c>beyond_wall</c> bloğu var. Yoksa değerler <see cref="RuleEngineV4SliceArenaVisualsDefaults"/>'tandır.</summary>
        public bool HasBlock { get; private set; }
        public float GroundRadiusM { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.BeyondGroundRadiusM;
        public string GroundColorHex { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.BeyondGroundColorHex;
        public float FogDensity { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.BeyondFogDensity;
        public string FogColorHex { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.BeyondFogColorHex;

        public static RuleEngineV4SliceArenaBeyond FromJson(JsonValue node)
        {
            var b = new RuleEngineV4SliceArenaBeyond();
            if (node.Kind != JsonKind.Object)
                return b;
            b.HasBlock = true;
            b.GroundRadiusM = node["ground_radius_m"].AsFloat(b.GroundRadiusM);
            b.GroundColorHex = node["ground_color"].AsString(b.GroundColorHex);
            b.FogDensity = node["fog_density"].AsFloat(b.FogDensity);
            b.FogColorHex = node["fog_color"].AsString(b.FogColorHex);
            return b;
        }
    }
}
