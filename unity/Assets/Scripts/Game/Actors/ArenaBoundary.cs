using Dovus.Core.RuleEngineV4;
using Dovus.Core.Tuning;
using Dovus.Game.Config;
using Dovus.Game.Skills.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// Arena yarıçapının tek okuma yeri (O-7.2, O-4.11). Dilimde (<see cref="RuleEngineV4Feature.Enabled"/>)
    /// dış duvarın iç yüzü: kural-motoru-v4.json <c>slice_arena</c> → <see cref="RuleEngineV4SliceArenaWall"/>;
    /// sahnedeki <c>ArenaHalfSizeM</c> okunmaz. Bayrak kapalıyken eski değer.
    /// </summary>
    public static class ArenaBoundary
    {
        static float? _sliceRadiusM;

        public static float RadiusM(GameTuning tuning) =>
            RuleEngineV4Feature.Enabled ? SliceRadiusM() : tuning.Arena.ArenaHalfSizeM;

        public static float RadiusM(SkillMotionTuning motion) =>
            RuleEngineV4Feature.Enabled ? SliceRadiusM() : motion.ArenaHalfSizeM;

        static float SliceRadiusM()
        {
            if (_sliceRadiusM.HasValue)
                return _sliceRadiusM.Value;
            RuleEngineV4SliceArena arena = new RuleEngineV4CatalogAccess().Catalog.SliceArena;
            if (!arena.HasOuterWallGeometry)
                Debug.LogError("[V4-FALLBACK] ArenaBoundary: kural-motoru-v4.json slice_arena.outer_wall_collision_radius_m "
                    + "veya outer_wall_segments yok; duvar ve sınır RuleEngineV4SliceArenaDefaults ile kuruldu");
            _sliceRadiusM = new RuleEngineV4SliceArenaWall(arena).InnerFaceRadiusM;
            return _sliceRadiusM.Value;
        }
    }
}
