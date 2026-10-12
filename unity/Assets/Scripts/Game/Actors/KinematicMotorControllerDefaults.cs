namespace Dovus.Game.Actors
{
    /// <summary>KinematicMotorController fizik ve locomotion yedekleri (PLAN 2B.11e).</summary>
    public static class KinematicMotorControllerDefaults
    {
        public const float FallbackWalkSpeedMps = 6.4f;
        public const float FallbackAnimSpeedDampSec = 0.08f;
        public const float FallbackLocoMaxPlaybackMult = 1.5f;
        public const float CapsuleRadiusFloorM = 0.05f;
        public const float CapsuleProbeLiftM = 0.05f;
        public const float CapsuleTopLiftM = 1.6f;
        /// <summary>O-1.13: gövde kapsülü ayak tabanından tepeye 1,80 m (merkez 0,90 m).</summary>
        public const float BodyHeightM = 1.8f;
        public const float MoveStopInsetM = 0.02f;
        public const float SeparationInsetM = 0.02f;
    }
}
