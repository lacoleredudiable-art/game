namespace Dovus.Game.Boss
{
    /// <summary>BossReactor çöküş ve sarsıntı (PLAN 2B.11e).</summary>
    public static class BossReactorDefaults
    {
        public const int KnockupIntegrateHoldMs = 140;
        public const float PullMoveEpsilonM = 0.02f;
        public const float MinCollapseDurationSec = 0.05f;
        public const float DeathShakeNoiseTimeScale = 0.05f;
        public const float DeathShakeNoiseOffsetY = 0.1f;
        public const float DeathShakeNoiseOffsetX = 0.3f;
        public const float MinPullDurationSec = 0.01f;
    }
}
