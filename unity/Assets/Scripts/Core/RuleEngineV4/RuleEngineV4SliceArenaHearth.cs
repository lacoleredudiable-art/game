using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Köy ocağı: kor diski + çevresinde küçük taş halkası (görsel, çarpışmasız).</summary>
    public sealed class RuleEngineV4SliceArenaHearth
    {
        public bool Enabled { get; private set; }
        public float XM { get; private set; }
        public float ZM { get; private set; }
        public float EmberRadiusM { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.HearthEmberRadiusM;
        public string StoneModel { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.WallModel;
        public int StoneCount { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.HearthStoneCount;
        public float StoneRingM { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.HearthStoneRingM;
        public float StoneScale { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.HearthStoneScale;

        public static RuleEngineV4SliceArenaHearth FromJson(JsonValue node)
        {
            var h = new RuleEngineV4SliceArenaHearth();
            if (node.IsNull)
                return h;
            h.Enabled = true;
            h.XM = node["x_m"].AsFloat(0f);
            h.ZM = node["z_m"].AsFloat(0f);
            h.EmberRadiusM = node["ember_radius_m"].AsFloat(h.EmberRadiusM);
            h.StoneModel = node["stone_model"].AsString(h.StoneModel);
            h.StoneCount = node["stone_count"].AsInt(h.StoneCount);
            h.StoneRingM = node["stone_ring_m"].AsFloat(h.StoneRingM);
            h.StoneScale = node["stone_scale"].AsFloat(h.StoneScale);
            return h;
        }
    }
}
