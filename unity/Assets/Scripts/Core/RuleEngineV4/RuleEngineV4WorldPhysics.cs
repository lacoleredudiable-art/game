namespace Dovus.Core.RuleEngineV4
{
    /// <summary>dunya-fizigi.md D-1, §5–§7; kural-motoru-v4.json world_physics.</summary>
    public sealed class RuleEngineV4WorldPhysics
    {
        public static RuleEngineV4WorldPhysics Default => new()
        {
            BodyRadiusPlayerM = RuleEngineV4CatalogDefaults.BodyRadiusPlayerM,
            BodyRadiusBossM = RuleEngineV4CatalogDefaults.BodyRadiusBossM,
            BodyRadiusCreatureM = RuleEngineV4CatalogDefaults.BodyRadiusCreatureM,
            BodyRadiusStructureM = RuleEngineV4CatalogDefaults.BodyRadiusStructureM,
            DashSpeedMps = RuleEngineV4CatalogDefaults.DashSpeedMps,
            ApproachSpeedMps = RuleEngineV4CatalogDefaults.ApproachSpeedMps,
            MotionCarryRatio = RuleEngineV4CatalogDefaults.MotionCarryRatio,
            MotionSpeedMaxMult = RuleEngineV4CatalogDefaults.MotionSpeedMaxMult,
            ProjectileRadiusM = RuleEngineV4CatalogDefaults.ProjectileRadiusM,
            DefaultMissileSpeedMps = RuleEngineV4CatalogDefaults.DefaultMissileSpeedMps,
            ProtectionInterceptWidthM = RuleEngineV4CatalogDefaults.ProtectionInterceptWidthM,
            StructureMaxPerPlayer = RuleEngineV4CatalogDefaults.StructureMaxPerPlayer,
            StructureMaxGlobal = RuleEngineV4CatalogDefaults.StructureMaxGlobal,
            BounceAngleConeDeg = RuleEngineV4CatalogDefaults.BounceAngleConeDeg,
            DiminishMultSecond = RuleEngineV4CatalogDefaults.DiminishMultSecond,
            DiminishMultThird = RuleEngineV4CatalogDefaults.DiminishMultThird,
            ProjectileColumnHalfHeightM = RuleEngineV4CatalogDefaults.ProjectileColumnHalfHeightM,
        };

        public float BodyRadiusPlayerM { get; init; }
        public float BodyRadiusBossM { get; init; }
        public float BodyRadiusCreatureM { get; init; }
        public float BodyRadiusStructureM { get; init; }
        public float DashSpeedMps { get; init; }
        public float ApproachSpeedMps { get; init; }
        public float MotionCarryRatio { get; init; }
        public float MotionSpeedMaxMult { get; init; }
        public float ProjectileRadiusM { get; init; }
        public float DefaultMissileSpeedMps { get; init; }
        public float ProtectionInterceptWidthM { get; init; }
        public int StructureMaxPerPlayer { get; init; }
        public int StructureMaxGlobal { get; init; }
        public float BounceAngleConeDeg { get; init; }
        public float DiminishMultSecond { get; init; }
        public float DiminishMultThird { get; init; }
        public float ProjectileColumnHalfHeightM { get; init; }
    }
}
