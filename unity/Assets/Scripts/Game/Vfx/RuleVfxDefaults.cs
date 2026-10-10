namespace Dovus.Game.Vfx
{
    /// <summary>efekt-motoru dilim oynatma sabitleri (Core VfxPlanDefaults ile hizalı).</summary>
    public static class RuleVfxDefaults
    {
        public const string KorShaderName = "Dovus/Vfx/VFX_Kor";
        public const string ParticlesUnlit = "Universal Render Pipeline/Particles/Unlit";

        public const float TrailWidthM = 0.12f;
        public const float TrailHeightM = 1.1f;
        public const float LightningJagM = 0.18f;
        public const int LightningSegments = 14;

        public const float SilhouetteWidthM = 2.4f;
        public const float SilhouetteHeightM = 1.1f;
        public const float SilhouetteLiftM = 0.55f;

        public const float SlashRadiusM = 0.85f;
        public const float SlashWidthM = 0.08f;
        public const float ClawLengthM = 0.7f;
        public const float ClawGapM = 0.12f;

        public const float RuneLetterSizePx = 72f;
        public const float RuneLetterGapPx = 16f;

        public const float WeaponTipLocalY = 0.9f;
        public const float WeaponGuardLocalY = 0.15f;

        public const int MeshTrailSamples = 20;
        public const float TrailFlowTurnsPerSec = 2f;
        public const float FootSparkLiftM = 0.05f;

        public const float AwakenGlowLocalX = 0.15f;
        public const float AwakenGlowLocalZ = 0.35f;
        public const float AwakenGlowScaleX = 0.12f;
        public const float AwakenGlowScaleY = 0.9f;
        public const float AwakenGlowScaleZ = 0.12f;
        public const float AwakenIdleEpsilon = 0.01f;

        public const float SilhouetteMinLenM = 1.2f;
        public const float SlashArcLifePadSec = 0.05f;
        public const float ClawDestroyPadSec = 0.05f;
        public const float ClawMatDestroyPadSec = 0.1f;
        public const float FootWingSideM = 0.35f;
        public const float BladeTipForwardM = 0.55f;
        public const float BladeGuardForwardM = 0.15f;
        public const float DashForeshadowM = 2.5f;
        public const float PathSampleEpsSq = 0.0001f;
        public const float FootSparkMoveEpsSq = 0.0025f;
    }
}
