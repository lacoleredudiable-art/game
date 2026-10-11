using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// kural-motoru-v4.json "slice_vfx": dilim efekt ayarı (sayılar kodda gömülmez).
    /// Blok ya da alan yoksa <see cref="RuleEngineV4SliceVfxDefaults"/>.
    /// </summary>
    public sealed class RuleEngineV4SliceVfx
    {
        public RuleEngineV4BladeTrailFx BladeTrail { get; private set; } = new();
        public RuleEngineV4VfxAtlas Atlas { get; private set; } = new();
        public RuleEngineV4TargetRingFx TargetRing { get; private set; } = new();
        public RuleEngineV4NotCastFx NotCast { get; private set; } = new();

        public static RuleEngineV4SliceVfx FromJson(JsonValue root)
        {
            var v = new RuleEngineV4SliceVfx();
            JsonValue node = root["slice_vfx"];
            if (node.Kind != JsonKind.Object)
                return v;
            v.BladeTrail = RuleEngineV4BladeTrailFx.FromJson(node["blade_trail"]);
            v.Atlas = RuleEngineV4VfxAtlas.FromJson(node["atlas"]);
            v.TargetRing = RuleEngineV4TargetRingFx.FromJson(node["target_ring"]);
            v.NotCast = RuleEngineV4NotCastFx.FromJson(node["not_cast"]);
            return v;
        }

        public static RuleEngineV4SliceVfx FromJson(string json) => FromJson(MiniJson.Parse(json));
    }
}
