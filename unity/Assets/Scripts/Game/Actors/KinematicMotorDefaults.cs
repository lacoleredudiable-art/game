namespace Dovus.Game.Actors
{
    /// <summary>KinematicMotor fizik ve locomotion yedekleri (PLAN 2B.11e).</summary>
    public static class KinematicMotorDefaults
    {
        public const float FallbackWalkSpeedMps = 6.4f;
        public const float FallbackAnimSpeedDampSec = 0.08f;
        public const float FallbackLocoMaxPlaybackMult = 1.5f;
        public const float CapsuleRadiusFloorM = 0.05f;
        public const float CapsuleRadiusBodyScale = 0.92f;
        public const float CapsuleProbeLiftM = 0.05f;
        public const float CapsuleTopLiftM = 1.6f;
        public const float MoveStopInsetM = 0.02f;
        public const float GroundProbeLiftM = 0.9f;
        public const float SeparationInsetM = 0.02f;
    }
}
