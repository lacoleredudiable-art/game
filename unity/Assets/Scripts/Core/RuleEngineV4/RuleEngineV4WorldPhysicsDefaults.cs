namespace Dovus.Core.RuleEngineV4
{
    /// <summary>dunya-fizigi.md — değerler JSON world_physics (RuleEngineV4WorldPhysicsRuntime).</summary>
    public static class RuleEngineV4WorldPhysicsDefaults
    {
        public static float DashSpeedMps => RuleEngineV4WorldPhysicsRuntime.Active.DashSpeedMps;
        public static float ApproachSpeedMps => RuleEngineV4WorldPhysicsRuntime.Active.ApproachSpeedMps;
        public static float MotionCarryRatio => RuleEngineV4WorldPhysicsRuntime.Active.MotionCarryRatio;
        public static float MotionSpeedMaxMult => RuleEngineV4WorldPhysicsRuntime.Active.MotionSpeedMaxMult;
        public static int StructureMaxPerPlayer => RuleEngineV4WorldPhysicsRuntime.Active.StructureMaxPerPlayer;
        public static int StructureMaxGlobal => RuleEngineV4WorldPhysicsRuntime.Active.StructureMaxGlobal;
        public static float BounceAngleConeDeg => RuleEngineV4WorldPhysicsRuntime.Active.BounceAngleConeDeg;
        public static float ProtectionInterceptWidthM => RuleEngineV4WorldPhysicsRuntime.Active.ProtectionInterceptWidthM;
        public static float ProjectileRadiusM => RuleEngineV4WorldPhysicsRuntime.Active.ProjectileRadiusM;
        public static float ProjectileColumnHalfHeightM =>
            RuleEngineV4WorldPhysicsRuntime.Active.ProjectileColumnHalfHeightM;
    }
}
