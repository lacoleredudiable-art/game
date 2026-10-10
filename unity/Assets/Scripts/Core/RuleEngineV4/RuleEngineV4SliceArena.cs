using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Dilim sahne arena düzeni (sayılar JSON'da).</summary>
    public sealed class RuleEngineV4SliceArena
    {
        RuleEngineV4SliceArena(
            IReadOnlyList<RuleEngineV4SliceArenaColumn> columns,
            IReadOnlyList<RuleEngineV4SliceArenaCocoon> cocoons,
            float outerWallCollisionRadiusM,
            float outerWallVisualInnerRadiusM,
            float outerWallVisualOuterRadiusM,
            int outerWallSegments,
            float outerWallHeightM)
        {
            Columns = columns;
            Cocoons = cocoons;
            OuterWallCollisionRadiusM = outerWallCollisionRadiusM;
            OuterWallVisualInnerRadiusM = outerWallVisualInnerRadiusM;
            OuterWallVisualOuterRadiusM = outerWallVisualOuterRadiusM;
            OuterWallSegments = outerWallSegments;
            OuterWallHeightM = outerWallHeightM;
        }

        public IReadOnlyList<RuleEngineV4SliceArenaColumn> Columns { get; }
        public IReadOnlyList<RuleEngineV4SliceArenaCocoon> Cocoons { get; }
        public float OuterWallCollisionRadiusM { get; }
        public float OuterWallVisualInnerRadiusM { get; }
        public float OuterWallVisualOuterRadiusM { get; }
        public int OuterWallSegments { get; }
        public float OuterWallHeightM { get; }

        public static RuleEngineV4SliceArena Default => BuildDefaults();

        public static RuleEngineV4SliceArena FromJson(JsonValue root)
        {
            JsonValue node = root["slice_arena"];
            if (node.IsNull)
                return BuildDefaults();

            var columns = new List<RuleEngineV4SliceArenaColumn>();
            JsonValue colNode = node["columns"];
            if (colNode.Kind == JsonKind.Array)
            {
                foreach (JsonValue c in colNode.AsArray())
                {
                    if (c.Kind != JsonKind.Object)
                        continue;
                    columns.Add(new RuleEngineV4SliceArenaColumn(
                        xM: F(c["x_m"], c["x"], 0f),
                        zM: F(c["z_m"], c["z"], 0f),
                        radiusM: F(c["radius_m"], null, RuleEngineV4SliceArenaDefaults.ColumnRadiusM),
                        heightM: F(c["height_m"], null, RuleEngineV4SliceArenaDefaults.ColumnHeightM)));
                }
            }

            var cocoons = new List<RuleEngineV4SliceArenaCocoon>();
            JsonValue cocNode = node["cocoons"];
            if (cocNode.Kind == JsonKind.Array)
            {
                foreach (JsonValue c in cocNode.AsArray())
                {
                    if (c.Kind != JsonKind.Object)
                        continue;
                    cocoons.Add(new RuleEngineV4SliceArenaCocoon(
                        xM: F(c["x_m"], c["x"], 0f),
                        zM: F(c["z_m"], c["z"], 0f),
                        radiusM: F(c["radius_m"], null, RuleEngineV4SliceArenaDefaults.CocoonRadiusM),
                        maxHp: c["max_hp"].AsInt(RuleEngineV4SliceArenaDefaults.CocoonMaxHp)));
                }
            }

            float collisionR = F(
                node["outer_wall_collision_radius_m"],
                null,
                RuleEngineV4SliceArenaDefaults.OuterWallCollisionRadiusM);
            float visualInner = F(
                node["outer_wall_visual_inner_radius_m"],
                node["outer_wall_collision_radius_m"],
                collisionR);
            float visualOuter = F(
                node["outer_wall_visual_outer_radius_m"],
                null,
                RuleEngineV4SliceArenaDefaults.OuterWallVisualOuterRadiusM);

            return new RuleEngineV4SliceArena(
                columns,
                cocoons,
                collisionR,
                visualInner,
                visualOuter,
                node["outer_wall_segments"].AsInt(RuleEngineV4SliceArenaDefaults.OuterWallSegments),
                F(node["outer_wall_height_m"], null, RuleEngineV4SliceArenaDefaults.OuterWallHeightM));
        }

        static RuleEngineV4SliceArena BuildDefaults()
        {
            return new RuleEngineV4SliceArena(
                new[]
                {
                    new RuleEngineV4SliceArenaColumn(
                        RuleEngineV4SliceArenaDefaults.Column1XM,
                        RuleEngineV4SliceArenaDefaults.Column1ZM,
                        RuleEngineV4SliceArenaDefaults.ColumnRadiusM,
                        RuleEngineV4SliceArenaDefaults.ColumnHeightM),
                    new RuleEngineV4SliceArenaColumn(
                        RuleEngineV4SliceArenaDefaults.Column2XM,
                        RuleEngineV4SliceArenaDefaults.Column2ZM,
                        RuleEngineV4SliceArenaDefaults.ColumnRadiusM,
                        RuleEngineV4SliceArenaDefaults.ColumnHeightM),
                    new RuleEngineV4SliceArenaColumn(
                        RuleEngineV4SliceArenaDefaults.Column3XM,
                        RuleEngineV4SliceArenaDefaults.Column3ZM,
                        RuleEngineV4SliceArenaDefaults.ColumnRadiusM,
                        RuleEngineV4SliceArenaDefaults.ColumnHeightM),
                },
                new[]
                {
                    new RuleEngineV4SliceArenaCocoon(
                        RuleEngineV4SliceArenaDefaults.Cocoon1XM,
                        RuleEngineV4SliceArenaDefaults.Cocoon1ZM,
                        RuleEngineV4SliceArenaDefaults.CocoonRadiusM,
                        RuleEngineV4SliceArenaDefaults.CocoonMaxHp),
                    new RuleEngineV4SliceArenaCocoon(
                        RuleEngineV4SliceArenaDefaults.Cocoon2XM,
                        RuleEngineV4SliceArenaDefaults.Cocoon2ZM,
                        RuleEngineV4SliceArenaDefaults.CocoonRadiusM,
                        RuleEngineV4SliceArenaDefaults.CocoonMaxHp),
                },
                RuleEngineV4SliceArenaDefaults.OuterWallCollisionRadiusM,
                RuleEngineV4SliceArenaDefaults.OuterWallCollisionRadiusM,
                RuleEngineV4SliceArenaDefaults.OuterWallVisualOuterRadiusM,
                RuleEngineV4SliceArenaDefaults.OuterWallSegments,
                RuleEngineV4SliceArenaDefaults.OuterWallHeightM);
        }

        static float F(JsonValue primary, JsonValue secondary, float fallback)
        {
            if (!primary.IsNull)
                return (float)primary.AsDouble(fallback);
            if (secondary != null && !secondary.IsNull)
                return (float)secondary.AsDouble(fallback);
            return fallback;
        }
    }
}
