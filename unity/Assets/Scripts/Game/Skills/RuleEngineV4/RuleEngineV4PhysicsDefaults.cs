using Dovus.Core.RuleEngineV4;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public static class RuleEngineV4PhysicsDefaults
    {
        public static float ProjectileRadiusM => RuleEngineV4WorldPhysicsDefaults.ProjectileRadiusM;
        public const float MeleeArcRadiusScale = 0.6f;
        public const float MinBodyRadiusM = 0.1f;
        public const float StrikeChestOffsetM = 0.9f;
        public const float ProjectileOriginHeightM = 1f;
        /// <summary>Spec'te yok: mermi sorgusunun dikey yarı boyu; sahnedeki her gövdeyi kapsayacak kadar büyük.</summary>
        public const float ProjectileColumnHalfHeightM = 50f;
        public const float SliceMinionFallbackRadiusM = 0.5f;
    }
}
