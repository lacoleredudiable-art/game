namespace Dovus.Game.Boss
{
    /// <summary>Varsayılan boss saldırı telegraf ölçüleri — AttackTelegraph tek kaynak (PLAN 2B.11).</summary>
    public static class AttackTelegraphDefaults
    {
        public const float DefaultDurationSec = 0.7f;
        public const float DefaultRadiusM = 3.2f;
        public const float DefaultLengthM = 7f;
        public const float DefaultWidthM = 1.4f;
        public const float DefaultArcHalfDeg = 40f;

        public const float MinDurationSec = 0.05f;
        public const float MinShapeDimensionM = 0.2f;
        public const float ArcHalfMinDeg = 5f;
        public const float ArcHalfMaxDeg = 170f;

        public const float InitialFillScale = 0.02f;
        public const float FillProgressMin = 0.05f;

        public const float FadeDestroyDelaySec = 0.18f;

        public const float GlowAlphaDistanceInner = 0.70f;
        public const float GlowAlphaFloor = 0.55f;
        public const float GlowAlphaDistanceMid = 0.88f;
        public const float GlowAlphaMidSpan = 0.18f;
        public const float GlowAlphaTail = 0.1f;
        public const float GlowAlphaTailSpan = 0.12f;
    }

}
