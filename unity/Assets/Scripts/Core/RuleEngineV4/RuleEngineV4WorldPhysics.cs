namespace Dovus.Core.RuleEngineV4
{
    /// <summary>dunya-fizigi.md D-1, §5–§7; kural-motoru-v4.json world_physics.</summary>
    public sealed class RuleEngineV4WorldPhysics
    {
        public static RuleEngineV4WorldPhysics Default => new()
        {
            BodyRadiusPlayerM = 0.35f,
            BodyRadiusBossM = 2.5f,
            BodyRadiusCreatureM = 0.5f,
            BodyRadiusStructureM = 1f,
            DashSpeedMps = 14f,
            ApproachSpeedMps = 8f,
            MotionCarryRatio = 0.5f,
            MotionSpeedMaxMult = 1.5f,
            ProjectileRadiusM = 0.25f,
            DefaultMissileSpeedMps = 30f,
            ProtectionInterceptWidthM = 1.2f,
            StructureMaxPerPlayer = 5,
            StructureMaxGlobal = 25,
            BounceAngleConeDeg = 35f,
            DiminishMultSecond = 0.5f,
            DiminishMultThird = 0.25f,
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
    }
}
