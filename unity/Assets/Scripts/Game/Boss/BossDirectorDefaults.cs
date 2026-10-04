namespace Dovus.Game.Boss
{
    /// <summary>BossDirector yaklaşma ve tempo (PLAN 2B.11e).</summary>
    public static class BossDirectorDefaults
    {
        public const float FallbackApproachStopPadM = 0.35f;
        public const float FallbackTurnRateDegPerSec = 240f;
        public const float PlanarDirEpsilonSqr = 0.01f;
        public const float PouncePlanMarginSec = 0.05f;
        public const float MinPhaseSpeed = 0.05f;
    }
}
