using Dovus.Core.RuleEngineV4;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public static class RuleEngineV4PhysicsDefaults
    {
        public static float ProjectileRadiusM => RuleEngineV4WorldPhysicsDefaults.ProjectileRadiusM;
        public static float ProjectileColumnHalfHeightM =>
            RuleEngineV4WorldPhysicsDefaults.ProjectileColumnHalfHeightM;
        public static float MeleeArcRadiusScale => RuleEngineV4UnitySceneDefaults.MeleeArcRadiusScale;
        public static float MinBodyRadiusM => RuleEngineV4UnitySceneDefaults.MinBodyRadiusM;
        public static float StrikeChestOffsetM => RuleEngineV4UnitySceneDefaults.StrikeChestOffsetM;
        public static float ProjectileOriginHeightM => RuleEngineV4UnitySceneDefaults.ProjectileOriginHeightM;
        public static float SliceMinionFallbackRadiusM => RuleEngineV4UnitySceneDefaults.SliceMinionFallbackRadiusM;
    }
}
