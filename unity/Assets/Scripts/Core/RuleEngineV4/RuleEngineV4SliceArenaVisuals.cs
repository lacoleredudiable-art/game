using System.Collections.Generic;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// kural-motoru-v4.json "slice_arena_visuals": yalnız görsel ortam (kaburga kemerleri, köy duvarları,
    /// ocak, rün taşı, sütun/koza modelleri). Çarpışma/hedefleme slice_arena'da kalır.
    /// Blok yoksa <see cref="Enabled"/> false: görsel kurulmaz.
    /// </summary>
    public sealed class RuleEngineV4SliceArenaVisuals
    {
        public bool Enabled { get; private set; }
        public string ColumnModel { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.ColumnModel;
        public string CocoonModel { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.CocoonModel;
        public RuleEngineV4SliceArenaRibs Ribs { get; private set; } = RuleEngineV4SliceArenaRibs.Default;
        public string WallModel { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.WallModel;
        public IReadOnlyList<RuleEngineV4SliceArenaPlacement> Walls { get; private set; } = new RuleEngineV4SliceArenaPlacement[0];
        public RuleEngineV4SliceArenaHearth Hearth { get; private set; } = RuleEngineV4SliceArenaHearth.FromJson(JsonValue.Null);
        public bool HasRuneStone { get; private set; }
        public string RuneStoneModel { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RuneStoneModel;
        public RuleEngineV4SliceArenaPlacement RuneStone { get; private set; } = new(
            RuleEngineV4SliceArenaVisualsDefaults.RuneStoneXM, RuleEngineV4SliceArenaVisualsDefaults.RuneStoneZM, 0f, 1f);
        public bool RuneStoneCollider { get; private set; }
        public bool HasFloorGlow { get; private set; }
        public float FloorGlowRadiusM { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.FloorGlowRadiusM;
        public float FloorGlowLiftM { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.FloorGlowLiftM;

        public static RuleEngineV4SliceArenaVisuals Disabled => new();

        public static RuleEngineV4SliceArenaVisuals FromJson(JsonValue root)
        {
            var v = new RuleEngineV4SliceArenaVisuals();
            JsonValue node = root["slice_arena_visuals"];
            if (node.Kind != JsonKind.Object)
                return v;
            v.Enabled = true;
            v.ColumnModel = node["column_model"].AsString(v.ColumnModel);
            v.CocoonModel = node["cocoon_model"].AsString(v.CocoonModel);
            v.Ribs = RuleEngineV4SliceArenaRibs.FromJson(node["ribs"]);

            JsonValue village = node["village"];
            v.WallModel = village["wall_model"].AsString(v.WallModel);
            var walls = new List<RuleEngineV4SliceArenaPlacement>();
            foreach (JsonValue w in village["walls"].AsArray())
            {
                if (w.Kind == JsonKind.Object)
                    walls.Add(Placement(w));
            }
            v.Walls = walls;
            v.Hearth = RuleEngineV4SliceArenaHearth.FromJson(village["hearth"]);

            JsonValue rune = node["rune_stone"];
            if (rune.Kind == JsonKind.Object)
            {
                v.HasRuneStone = true;
                v.RuneStoneModel = rune["model"].AsString(v.RuneStoneModel);
                v.RuneStone = Placement(rune, RuleEngineV4SliceArenaVisualsDefaults.RuneStoneXM, RuleEngineV4SliceArenaVisualsDefaults.RuneStoneZM);
                v.RuneStoneCollider = rune["collider"].AsBool(false);
            }

            JsonValue floor = node["floor_glow"];
            if (floor.Kind == JsonKind.Object)
            {
                v.HasFloorGlow = true;
                v.FloorGlowRadiusM = floor["radius_m"].AsFloat(v.FloorGlowRadiusM);
                v.FloorGlowLiftM = floor["lift_m"].AsFloat(v.FloorGlowLiftM);
            }
            return v;
        }

        static RuleEngineV4SliceArenaPlacement Placement(JsonValue n, float x = 0f, float z = 0f) =>
            new(n["x_m"].AsFloat(x), n["z_m"].AsFloat(z), n["yaw_deg"].AsFloat(0f), n["scale"].AsFloat(1f));
    }
}
