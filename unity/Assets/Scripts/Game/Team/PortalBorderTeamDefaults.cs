namespace Dovus.Game.Team
{
    /// <summary>Takım sahne host oynanış ölçüleri — PortalBorderTeamHost tek kaynak (PLAN 2B.11).</summary>
    public static class PortalBorderTeamDefaults
    {
        public const float AllySpawnBaseX = -1.6f;
        public const float AllySpawnStepX = 0.8f;
        public const float AllySpawnZ = -1.2f;
        public const float ActorGroundY = 1f;

        public const float AllyDummyHpRatio = 0.7f;
        public const float TeamActorRadiusM = 0.5f;

        public const float HpPercentScale = 100f;
        public const float NearBossDistSqr = 0.01f;

        public const float MineBurstHeightY = 0.4f;
        public const float PortalStrikeMarkerY = 1.1f;

        public const float BossBodyRadiusFallbackM = 0.85f;
        public const float BossBodyRadiusMinM = 0.1f;

        public const float StunStatusStrength = 1f;
        public const float MineMultActiveThreshold = 1f;

        public const float BorderAuraLocalY = 0.05f;
        public const float BorderAuraScaleXZ = 1.6f;
        public const float BorderAuraScaleY = 0.02f;

        public const float GateMarkerY = 0.04f;
        public const float GateThicknessY = 0.03f;

        public const float WorldMarkerY = 0.35f;
        public const float WorldMarkerScale = 0.45f;

        public const float BurstFxScale = 0.7f;
        public const float BurstFxLifetimeSec = 0.6f;
    }

}
