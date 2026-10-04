namespace Dovus.Game.Actors
{
    /// <summary>ActorVisual locomotion ve anim eşikleri (PLAN 2B.11e).</summary>
    public static class ActorVisualDefaults
    {
        public const float StrikeComboResetSec = 1.2f;
        public const float UpperBodyMinSpeed = 0.15f;
        public const float IdleSpeedCutoff = 0.08f;
        public const float MinPositive = 0.01f;
        public const float LocoRunSpeedRatioFloor = 0.08f;
        public const float MinPlaybackRate = 0.05f;
        public const float SidestepMirrorStrafeMin = 0.15f;
        public const float FallbackLocoRunMps = 2.24f;
        public const float BackwardForwardThreshold = -0.35f;
        public const float BackwardSpeedMinMps = 0.2f;
    }
}
