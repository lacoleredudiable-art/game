namespace Dovus.Game.Hud
{
    /// <summary>PLAN 2B.11g / O-10.3: gömülü oynanış/ayar sayıları.</summary>
    public static class DamageNumberHudDefaults
    {
        public const float PunchDurationSec = 0.22f;
        /// <summary>Doğuş anındaki ekstra tepe (DamageFloatPunchScale üstü).</summary>
        public const float PunchPeakMultiplier = 1.12f;
        public const float PunchBirthScale = 0.55f;
        public const float RiseEaseOutPower = 2.35f;
        public const float FallbackCamForwardM = 6f;
        public const float FallbackScreenHeightFrac = 0.7f;
        public const float JitterX = 36f;
        public const float FadeDurationMinSec = 0.01f;
        public const float DamageTextColorLerp = 0.35f;
        public const float OutlineEffectDistancePx = 2.5f;
        public const float ShadowEffectDistancePx = 2f;
        public const float FallbackBossHeightOffsetM = 2.2f;
        /// <summary>Renderer yoksa tepe yüksekliği: gövde yarıçapı × bu katsayı.</summary>
        public const float BossHeadFallbackHeightMul = 3f;
        /// <summary>Bounds tepe noktasının hemen üstü (m).</summary>
        public const float BossHeadTopExtraOffsetM = 0.12f;
        /// <summary>Vuruş yönünde hafif yatay kayma: BodyRadiusM ile çarpılır.</summary>
        public const float BossHitHorizontalBodyRadiusFrac = 0.18f;
        public const float BossHitHorizontalOffsetMaxM = 0.45f;
    }
}
