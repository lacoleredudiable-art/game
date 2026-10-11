namespace Dovus.Game.Vfx
{
    /// <summary>
    /// Efekt görünüm sabitleri (efekt-motoru dilim). Atlas yerleşimi kural-motoru-v4.json slice_vfx.atlas.
    /// </summary>
    public static class RuleVfxArtDefaults
    {
        public const float RuneAnchorY = 120f;
        public const float RuneFromOffsetX = 80f;
        public const float RuneFromOffsetY = 40f;
        public const float RuneAdjFromY = 30f;
        public const float RuneAdjDelay = 0.15f;
        public const float RuneAdjDelayInv = 0.85f;
        public const float RuneHueSat = 0.35f;

        public const float LightningTipLift = 0.35f;
        public const float LightningEndLift = 0.25f;
        public const float LightningJagLift = 0.4f;
        public const float LightningEndAlpha = 0.2f;
        public const float LightningWidthMin = 0.35f;
        public const float LightningIntensityBase = 1.2f;
        public const float CoreHdrMult = 2f;
        public const float CoreHdrHot = 2.5f;
        public const float CoreHdrHit = 2.8f;
        public const float IntensityHit = 2.5f;
        public const float IntensityClaw = 3f;
        public const float IntensityRune = 2.2f;
        public const float IntensityRuneMin = 0.6f;
        public const float IntensityRuneSpan = 1.6f;
        public const float HitHeightM = 1.05f;
        public const float HitHeightClawM = 1.0f;
        public const float HitHeightSparkM = 1.1f;
        public const float SlashArcDeg = 50f;
        public const float SlashArcYScale = 0.35f;
        public const float SlashArcZ = 0.05f;
        public const float ClawWidthM = 0.05f;
        public const float ClawStartFrac = 0.35f;
        public const float ClawEndFrac = 0.65f;
        public const float ClawUpStartM = 0.25f;
        public const float ClawUpEndM = 0.15f;
        public const float ClawSideStart = 0.05f;
        public const float ClawSideEnd = 0.08f;
        public const float ClawRowLift = 0.05f;
        public const float EdgeSparkSpeed = 2.5f;
        public const float EdgeSparkSize = 0.08f;
        public const float EdgeSparkRadius = 0.2f;
        public const float EdgeSparkLift = 0.4f;
        public const float EdgeDestroyPad = 0.15f;
        public const float MeshLifeMin = 0.05f;
        public const float HitForwardPad = 0.35f;
        public const float HitForwardMin = 0.2f;
        public const float DashStartLift = 0.3f;
        public const float SilhouettePathLift = 0.4f;
        public const float TrailRebuildAlpha = 0.15f;
        public const int LightningJagHashMul = 37;
        public const int LightningJagMod = 7;
        public const float LightningJagDiv = 6f;
        public const int GlyphCellMaxIndex = 23;
        public const int GlyphHueMod = 12;
        public const float SmoothstepThree = 3f;
        public const float SmoothstepTwo = 2f;
        public const int SlashArcSegments = 10;
        public const float SlashArcLastIndex = 9f;
    }
}
