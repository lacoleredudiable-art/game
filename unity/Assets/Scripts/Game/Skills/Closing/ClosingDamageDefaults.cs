namespace Dovus.Game.Skills.Closing
{
    /// <summary>Boss kapanış tepkisi ve iz ölçekleri — ClosingDamageResolver tek kaynak (PLAN 2B.11).</summary>
    public static class ClosingDamageDefaults
    {
        public const float ScarScaleBase = 0.7f;
        public const float ScarScalePerDot = 0.15f;
        public const float ScarCrackLerpScale = 0.85f;

        public const float BasicShakeMult = 0.35f;

        public const float SelfTargetKnockMult = 0.08f;
        public const float SelfTargetLiftM = 0.04f;

        public const float StrikeKnockBase = 1.85f;
        public const float StrikeKnockPerPierce = 0.4f;
        public const float StrikeLiftM = 0.05f;
        public const float StrikeShakeMult = 0.55f;

        public const float DisruptKnockMult = 0.12f;
        public const float DisruptLiftM = 0.08f;
        public const float DisruptShakeMult = 1.6f;
        public const float DisruptShakeFollowMult = 0.45f;
        public const float DisruptSideOffsetM = 0.35f;
        public const float DisruptSideKnockMult = 0.6f;
        public const float DisruptSideLiftMult = 0.5f;
        public const float DisruptSideShakeMult = 0.55f;

        public const float ControlPinSec = 0.7f;

        public const float ZoneKnockMult = 0.55f;
        public const float ZoneLiftBase = 1.15f;
        public const float ZoneLiftPerLift = 0.35f;
        public const float ZoneShakeMult = 0.9f;

        public const float MotionKnockMult = 0.9f;
        public const float MotionLiftM = 0.12f;
        public const float MotionShakeMult = 0.7f;

        public const float SpecialKnockMult = 0.25f;
        public const float SpecialLiftM = 0.2f;
        public const float SpecialShakeMult = 1.1f;

        public const float HavaPinSec = 0.55f;

        public const float ToprakKnockMult = 0.2f;
        public const float ToprakShakeMult = 0.7f;

        public const float PlanarEpsilonSqr = 0.0001f;
    }

}
