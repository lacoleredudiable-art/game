using Dovus.Game.Actors;
using Dovus.Game.Arena;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Feel;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Config
{
    /// <summary>
    /// T5 prototip kabuğu ayarları. Spec'te yürüme hızı yok; varsayılanlar durum.md'de kayıtlı.
    /// Sahnedeki tek örnek Bootstrap'ten paylaşılır; kopya tutulmaz (T10 canlı ayarı bunu bekliyor).
    /// </summary>
    [System.Serializable]
    public sealed partial class PrototypeTuning
    {
        // Sahneye serileşmiş eski kopyada yeni alanlar 0/siyah gelir (C# initializer
        // deserialize'da uygulanmaz). Sürüm numarası da 0 geldiği için tek seferlik yama
        // ÇALIŞIR; sahne bir kez yeniden kaydedildikten sonra bu blok hiç girmez ve
        // tasarımcının bilinçli 0'ı (ör. nabzı kapatmak) artık ezilmez (T8.1).
        [HideInInspector] public int TuningVersion = CurrentVersion;

        const int CurrentVersion = 22;

        /// <summary>Sürümü geçmiş serileşmiş kopyayı bu sürümün varsayılanlarına çeker.</summary>
        public void EnsureRuntimeDefaults()
        {
            if (TuningVersion < CurrentVersion)
                MigrateToCurrent();

            BasicStrikeDot = 1;
            ShowDamageNumbers = true;
            ElementAir = new Color(0.50f, 0.70f, 0.62f);
            ShowSentenceDebugHud = false;
            if (SoftAimRangeM <= 0.01f) SoftAimRangeM = 8f;
            if (SoftAimConeDeg <= 0.01f) SoftAimConeDeg = 70f;
            if (OrbitDegreesPerDp <= 0.01f) OrbitDegreesPerDp = 0.35f;
            if (StatusIconSizeDp <= 0.01f) StatusIconSizeDp = 28f;
            if (VitalsBossBarWidthDp <= 0.01f) VitalsBossBarWidthDp = 460f;
            if (VitalsBossBarHeightDp <= 0.01f) VitalsBossBarHeightDp = 24f;
            if (VitalsHeaderHeightDp <= 0.01f) VitalsHeaderHeightDp = 18f;
            if (VitalsPanelPaddingDp <= 0.01f) VitalsPanelPaddingDp = 8f;
            if (DamageFloatFontDp <= 0.01f) DamageFloatFontDp = 28f;
            if (HexagonRadiusDp <= 0.01f) HexagonRadiusDp = 112f;
            if (SkillPreviewWidthDp <= 0.01f) SkillPreviewWidthDp = 324f;
            if (SkillPreviewHeightDp <= 0.01f) SkillPreviewHeightDp = 72f;
            if (SkillPreviewGapDp <= 0.01f) SkillPreviewGapDp = 10f;
            if (SkillPreviewHoldSec <= 0.01f) SkillPreviewHoldSec = 0.9f;
            if (DotHitRadiusDp <= 0.01f) DotHitRadiusDp = 34f;
            if (CenterHitRadiusDp <= 0.01f) CenterHitRadiusDp = 38f;
            if (InkRawWidthScale <= 0f) InkRawWidthScale = 0.9f;
            if (InkRawGlow <= 0f) InkRawGlow = 1f;
            if (string.IsNullOrWhiteSpace(VfxTexFire)) VfxTexFire = "flame_02";
            if (string.IsNullOrWhiteSpace(VfxTexWater)) VfxTexWater = "circle_03";
            if (string.IsNullOrWhiteSpace(VfxTexAir)) VfxTexAir = "twirl_01";
            if (string.IsNullOrWhiteSpace(VfxTexEarth)) VfxTexEarth = "dirt_01";
            if (string.IsNullOrWhiteSpace(VfxTexLight)) VfxTexLight = "star_04";
            if (string.IsNullOrWhiteSpace(VfxTexDark)) VfxTexDark = "magic_04";
            if (string.IsNullOrWhiteSpace(VfxTexHit)) VfxTexHit = "spark_05";
            if (string.IsNullOrWhiteSpace(VfxTexInk)) VfxTexInk = "light_01";
            if (DodgeButtonRadiusDp <= 0.01f) DodgeButtonRadiusDp = 40f;
            if (WeaponSwapButtonRadiusDp <= 0.01f) WeaponSwapButtonRadiusDp = 36f;
            if (DodgeClearanceDp <= 0.01f) DodgeClearanceDp = 40f;
            if (IconDisplayScale <= 0.01f) IconDisplayScale = 1.0f;
            if (WeaponSwapIconScale <= 0.01f) WeaponSwapIconScale = 0.72f;
            if (CombatTrayPaddingDp <= 0.01f) CombatTrayPaddingDp = 22f;
            if (CombatTrayHeaderHeightDp <= 0.01f) CombatTrayHeaderHeightDp = 22f;
            if (CombatTrayCornerRadiusDp <= 0.01f) CombatTrayCornerRadiusDp = 20f;
            if (CombatTrayLinkWidthDp <= 0.01f) CombatTrayLinkWidthDp = 1.5f;
            if (ElementMenuChipWidthDp <= 0.01f) ElementMenuChipWidthDp = 144f;
            if (ElementMenuChipHeightDp <= 0.01f) ElementMenuChipHeightDp = 48f;
            if (ElementMenuItemWidthDp <= 0.01f) ElementMenuItemWidthDp = 84f;
            if (ElementMenuItemHeightDp <= 0.01f) ElementMenuItemHeightDp = 48f;
            if (ElementMenuAnchorXNorm <= 0.01f) ElementMenuAnchorXNorm = 0.16f;
            if (ElementMenuAnchorYNorm <= 0.01f) ElementMenuAnchorYNorm = 0.27f;
            if (HudFitShortSideDp < 0f) HudFitShortSideDp = 600f;
            if (CameraPitchMaxDeg < CameraPitchMinDeg) { CameraPitchMinDeg = -8f; CameraPitchMaxDeg = 35f; }
            if (PlayerVisualHeightM <= 0.01f) PlayerVisualHeightM = 1.78f;
            if (BossVisualHeightM <= 0.01f) BossVisualHeightM = 5.0f;
            if (CharacterAnimSpeed <= 0.01f) CharacterAnimSpeed = 1.0f;
            if (WalkSpeedMps <= 0.01f) WalkSpeedMps = 6.4f;
            if (MoveAccelMps2 <= 0.01f) MoveAccelMps2 = 52f;
            if (MoveDecelMps2 <= 0.01f) MoveDecelMps2 = 64f;
            if (TurnRateDegPerSec <= 0.01f) TurnRateDegPerSec = 720f;
            if (MinStickSpeedFrac <= 0.01f) MinStickSpeedFrac = 0.42f;
            if (AnimSpeedDampSec <= 0f) AnimSpeedDampSec = 0.08f;
            if (LocoMaxPlaybackMult < 1f) LocoMaxPlaybackMult = 1.5f;
            if (AnimCrossFadeSec <= 0f) AnimCrossFadeSec = 0.06f;
            if (BasicStrikeComboResetSec <= 0f) BasicStrikeComboResetSec = 1.2f;
            if (UpperBodyCastMinSpeed <= 0f) UpperBodyCastMinSpeed = 0.15f;
            if (FootstepStrideM <= 0.05f) FootstepStrideM = 2.2f;
            if (BossFootstepStrideM <= 0.05f) BossFootstepStrideM = 2.4f;
            if (string.IsNullOrWhiteSpace(ActiveBossId)) ActiveBossId = "karadul";
            if (BossTurnRateDegPerSec <= 0.01f) BossTurnRateDegPerSec = 240f;
            if (BossSlamImpactNorm <= 0.01f) BossSlamImpactNorm = 0.42f;
            if (BossConeImpactNorm <= 0.01f) BossConeImpactNorm = 0.40f;
            if (BossStaggerMinGapSec <= 0f) BossStaggerMinGapSec = 0.6f;
            if (BossAnimCrossFadeSec <= 0f) BossAnimCrossFadeSec = 0.15f;
            if (BossWalkClipMps <= 0.01f) BossWalkClipMps = 1.4f;
            if (ArenaHalfSizeM <= 0.01f) ArenaHalfSizeM = 25f;
            if (ArenaWallHeightM <= 0.01f) ArenaWallHeightM = 18f;
            if (ArenaWallThicknessM <= 0.01f) ArenaWallThicknessM = 1.4f;
            if (CameraDistanceM <= 0.01f) CameraDistanceM = 5.85f;
            if (CameraFovDeg <= 1f) CameraFovDeg = 54f;
            if (CameraAimDampingSec <= 0f) CameraAimDampingSec = 0.12f;
            if (CameraSoftLockRangeM <= 0f) CameraSoftLockRangeM = 20f;
            if (CameraBossAimHeightM <= 0f) CameraBossAimHeightM = 1.45f;
            if (CameraLockOnMinDistanceM <= 0.01f) CameraLockOnMinDistanceM = 5.2f;
            if (CameraLockOnMaxDistanceM <= CameraLockOnMinDistanceM) CameraLockOnMaxDistanceM = 8.8f;
            if (CameraLockOnDistanceSmoothSec <= 0.01f) CameraLockOnDistanceSmoothSec = 0.22f;
            if (CameraWindupDistanceMul < 1f) CameraWindupDistanceMul = 1.42f;
            if (CameraWindupSmoothSec <= 0.01f) CameraWindupSmoothSec = 0.28f;
            if (CameraCollisionSphereRadiusM <= 0.01f) CameraCollisionSphereRadiusM = 0.25f;
            if (CameraCollisionMarginM < 0f) CameraCollisionMarginM = 0.12f;
            if (CameraCollisionMinDistanceM <= 0.1f) CameraCollisionMinDistanceM = 1.2f;
            if (CameraCollisionPullInSmoothSec <= 0.001f) CameraCollisionPullInSmoothSec = 0.05f;
            if (CameraCollisionPullOutSmoothSec <= 0.01f) CameraCollisionPullOutSmoothSec = 0.35f;
            if (CameraLockOnShoulderSideM <= 0.01f) CameraLockOnShoulderSideM = 1.1f;
            if (CameraLockOnShoulderFlipHysteresis <= 0f) CameraLockOnShoulderFlipHysteresis = 0.12f;
            if (LockOnButtonRadiusDp <= 0.01f) LockOnButtonRadiusDp = 36f;
            if (FogDensity <= 0f) FogDensity = 0.0032f;
            if (KeyLightIntensity <= 0f) KeyLightIntensity = 1.35f;
            if (BloomThreshold <= 0f) BloomThreshold = 1.05f;
        }

        void MigrateToCurrent()
        {
            var fresh = new PrototypeTuning();
            PlayerMaxHp = fresh.PlayerMaxHp;
            WindowCueUrgentRatio = fresh.WindowCueUrgentRatio;
            WindowCuePulseHz = fresh.WindowCuePulseHz;
            WindowCueUrgentHz = fresh.WindowCueUrgentHz;
            WindowCuePulseAmp = fresh.WindowCuePulseAmp;
            TelegraphHot = fresh.TelegraphHot;
            TelegraphWarm = fresh.TelegraphWarm;
            DodgeGlideSpeedMps = fresh.DodgeGlideSpeedMps;
            TelegraphStretch = fresh.TelegraphStretch;
            TelegraphSquash = fresh.TelegraphSquash;
            TelegraphSlamSquash = fresh.TelegraphSlamSquash;
            TelegraphTonePitchMin = fresh.TelegraphTonePitchMin;
            TelegraphTonePitchMax = fresh.TelegraphTonePitchMax;
            TelegraphToneVolumeMin = fresh.TelegraphToneVolumeMin;
            TelegraphToneVolumeMax = fresh.TelegraphToneVolumeMax;
            BossApproachStopPadM = fresh.BossApproachStopPadM;
            AfterimageAlpha = fresh.AfterimageAlpha;
            ImpactFadeSec = fresh.ImpactFadeSec;
            VignetteHoldSec = fresh.VignetteHoldSec;
            VignetteFadeSec = fresh.VignetteFadeSec;
            VignetteAlpha = fresh.VignetteAlpha;
            ThreatAlphaMax = fresh.ThreatAlphaMax;
            ThreatPulseHzMin = fresh.ThreatPulseHzMin;
            ThreatPulseHzMax = fresh.ThreatPulseHzMax;
            CameraShakePxToM = fresh.CameraShakePxToM;

            // T9: yeni alanlar, aynı "sürüm damgası bir kez yamalar" deseni (yukarısı).
            ReadoutAnchorRight = fresh.ReadoutAnchorRight;
            ReadoutPunchInSec = fresh.ReadoutPunchInSec;
            VitalsBarWidthDp = fresh.VitalsBarWidthDp;
            VitalsBarHeightDp = fresh.VitalsBarHeightDp;
            VitalsBarSpacingDp = fresh.VitalsBarSpacingDp;
            VitalsMarginDp = fresh.VitalsMarginDp;
            VitalsHeaderHeightDp = fresh.VitalsHeaderHeightDp;
            BossVitalsColor = fresh.BossVitalsColor;

            // T11.1: kilit HUD ölçüleri.
            RecoveryLockHeightDp = fresh.RecoveryLockHeightDp;
            RecoveryLockGapDp = fresh.RecoveryLockGapDp;

            // T11: aynı desen. TargetFrameRateHz 0 gelirse Application.targetFrameRate anlamsız
            // bir değere düşer, o yüzden bu yama ölçüm turu için kritik.
            FrameTimeSampleSec = fresh.FrameTimeSampleSec;
            TargetFrameRateHz = fresh.TargetFrameRateHz;

            // T12: ölüm pozu (0 gelirse squash görünmez / süre anlamsız).
            BossDeathCollapseSec = fresh.BossDeathCollapseSec;
            BossDeathSquashY = fresh.BossDeathSquashY;
            BossDeathSpreadXz = fresh.BossDeathSpreadXz;

            // T14: hareket karakteri ölçüleri (0 gelirse Zenitsu/sürü/halka/jab kaybolur).
            EffectNeedleWindupLenMul = fresh.EffectNeedleWindupLenMul;
            EffectNeedleArrivalLenMul = fresh.EffectNeedleArrivalLenMul;
            EffectNeedleAfterimageAlpha = fresh.EffectNeedleAfterimageAlpha;
            EffectSwarmJitterM = fresh.EffectSwarmJitterM;
            EffectSwarmMinBlobs = fresh.EffectSwarmMinBlobs;
            EffectSarsintiGroundY = fresh.EffectSarsintiGroundY;
            EffectSarsintiMassWidthMul = fresh.EffectSarsintiMassWidthMul;
            EffectBasicStrikeLenM = fresh.EffectBasicStrikeLenM;
            EffectBasicStrikeThickM = fresh.EffectBasicStrikeThickM;
            EffectBasicStrikeHeightM = fresh.EffectBasicStrikeHeightM;

            // v9: dodge sağ-alt (Toprak'tan uzak); ikon ölçeği.
            DodgeButtonOffsetXDp = 100f;
            DodgeButtonOffsetYDp = -100f;
            DodgeButtonRadiusDp = 38f;
            IconDisplayScale = 1.12f;
            ElementFire = fresh.ElementFire;
            ElementWater = fresh.ElementWater;
            ElementAir = fresh.ElementAir;
            ElementEarth = fresh.ElementEarth;
            ElementLight = fresh.ElementLight;
            ElementDark = fresh.ElementDark;

            // v10 HUD / orbit / float
            VitalsBarWidthDp = fresh.VitalsBarWidthDp;
            VitalsBarHeightDp = fresh.VitalsBarHeightDp;
            VitalsBossBarHeightDp = fresh.VitalsBossBarHeightDp;
            VitalsBossBarWidthDp = fresh.VitalsBossBarWidthDp;
            VitalsHeaderHeightDp = fresh.VitalsHeaderHeightDp;
            StatusIconSizeDp = fresh.StatusIconSizeDp;
            StatusIconGapDp = fresh.StatusIconGapDp;
            OrbitDegreesPerDp = fresh.OrbitDegreesPerDp;
            SoftAimRangeM = fresh.SoftAimRangeM;
            SoftAimConeDeg = fresh.SoftAimConeDeg;
            DamageFloatFontDp = fresh.DamageFloatFontDp;
            DamageFloatCritFontDp = fresh.DamageFloatCritFontDp;
            DamageFloatRisePx = fresh.DamageFloatRisePx;
            DamageFloatHoldSec = fresh.DamageFloatHoldSec;
            DamageFloatFadeSec = fresh.DamageFloatFadeSec;
            DamageFloatPunchScale = fresh.DamageFloatPunchScale;

            // v12: hex/dodge/vitals — telefon screenshot'ta alt rün kesiliyordu, dodge Hava üstündeydi.
            // v13: rünler arası + dodge çizim boşluğu.
            HexagonCenterXNorm = fresh.HexagonCenterXNorm;
            HexagonCenterYNorm = fresh.HexagonCenterYNorm;
            HexagonRadiusDp = fresh.HexagonRadiusDp;
            CenterHitRadiusDp = fresh.CenterHitRadiusDp;
            DotHitRadiusDp = fresh.DotHitRadiusDp;
            DodgeButtonRadiusDp = fresh.DodgeButtonRadiusDp;
            DodgeButtonOffsetXDp = fresh.DodgeButtonOffsetXDp;
            DodgeButtonOffsetYDp = fresh.DodgeButtonOffsetYDp;
            DodgeClearanceDp = fresh.DodgeClearanceDp;
            WeaponSwapButtonRadiusDp = fresh.WeaponSwapButtonRadiusDp;
            WeaponSwapButtonOffsetXDp = fresh.WeaponSwapButtonOffsetXDp;
            WeaponSwapButtonOffsetYDp = fresh.WeaponSwapButtonOffsetYDp;
            IconDisplayScale = fresh.IconDisplayScale;
            VitalsBarWidthDp = fresh.VitalsBarWidthDp;
            VitalsBarHeightDp = fresh.VitalsBarHeightDp;
            VitalsBossBarHeightDp = fresh.VitalsBossBarHeightDp;
            VitalsBossBarWidthDp = fresh.VitalsBossBarWidthDp;
            VitalsBarSpacingDp = fresh.VitalsBarSpacingDp;
            VitalsMarginDp = fresh.VitalsMarginDp;
            ShowSentenceDebugHud = false;

            // v14: 100 m çap daire salon. v15: yeni locomotion klipleri.
            // v16: insan ölçeği + büyük boss, omuz kamerası, mobil atmosfer ve okunur HUD.
            ArenaHalfSizeM = fresh.ArenaHalfSizeM;
            ArenaWallHeightM = fresh.ArenaWallHeightM;
            ArenaWallThicknessM = fresh.ArenaWallThicknessM;
            ArenaVisualScale = fresh.ArenaVisualScale;
            PlayerVisualHeightM = fresh.PlayerVisualHeightM;
            BossVisualHeightM = fresh.BossVisualHeightM;
            CharacterAnimSpeed = fresh.CharacterAnimSpeed;
            WalkSpeedMps = fresh.WalkSpeedMps;
            MoveAccelMps2 = fresh.MoveAccelMps2;
            MoveDecelMps2 = fresh.MoveDecelMps2;
            TurnRateDegPerSec = fresh.TurnRateDegPerSec;
            MinStickSpeedFrac = fresh.MinStickSpeedFrac;
            DodgeGlideSpeedMps = fresh.DodgeGlideSpeedMps;
            HexagonRadiusDp = fresh.HexagonRadiusDp;
            DotHitRadiusDp = fresh.DotHitRadiusDp;
            CenterHitRadiusDp = fresh.CenterHitRadiusDp;
            DodgeButtonRadiusDp = fresh.DodgeButtonRadiusDp;
            VitalsBarWidthDp = fresh.VitalsBarWidthDp;
            VitalsBarHeightDp = fresh.VitalsBarHeightDp;
            VitalsBossBarHeightDp = fresh.VitalsBossBarHeightDp;
            VitalsBossBarWidthDp = fresh.VitalsBossBarWidthDp;
            VitalsBarSpacingDp = fresh.VitalsBarSpacingDp;
            VitalsMarginDp = fresh.VitalsMarginDp;
            BossVitalsColor = fresh.BossVitalsColor;
            SkillPreviewWidthDp = fresh.SkillPreviewWidthDp;
            SkillPreviewHeightDp = fresh.SkillPreviewHeightDp;
            FollowSmoothTimeSec = fresh.FollowSmoothTimeSec;
            LookAheadM = fresh.LookAheadM;
            CameraShoulderOffset = fresh.CameraShoulderOffset;
            CameraDistanceM = fresh.CameraDistanceM;
            CameraLookHeightM = fresh.CameraLookHeightM;
            CameraFovDeg = fresh.CameraFovDeg;
            CameraAimDampingSec = fresh.CameraAimDampingSec;
            CameraSoftLockRangeM = fresh.CameraSoftLockRangeM;
            CameraSoftLockStrength = fresh.CameraSoftLockStrength;
            CameraBossFramingWeight = fresh.CameraBossFramingWeight;
            CameraBossAimHeightM = fresh.CameraBossAimHeightM;
            CameraDefaultPitchDeg = fresh.CameraDefaultPitchDeg;
            CameraLockOnMinDistanceM = fresh.CameraLockOnMinDistanceM;
            CameraLockOnMaxDistanceM = fresh.CameraLockOnMaxDistanceM;
            CameraLockOnDistancePerSepM = fresh.CameraLockOnDistancePerSepM;
            CameraLockOnMaxExtraDistanceM = fresh.CameraLockOnMaxExtraDistanceM;
            CameraLockOnDistanceSmoothSec = fresh.CameraLockOnDistanceSmoothSec;
            CameraWindupDistanceMul = fresh.CameraWindupDistanceMul;
            CameraWindupExtraHeightM = fresh.CameraWindupExtraHeightM;
            CameraWindupSmoothSec = fresh.CameraWindupSmoothSec;
            CameraWindupMinRadiusM = fresh.CameraWindupMinRadiusM;
            CameraCollisionSphereRadiusM = fresh.CameraCollisionSphereRadiusM;
            CameraCollisionMarginM = fresh.CameraCollisionMarginM;
            CameraCollisionMinDistanceM = fresh.CameraCollisionMinDistanceM;
            CameraCollisionPullInSmoothSec = fresh.CameraCollisionPullInSmoothSec;
            CameraCollisionPullOutSmoothSec = fresh.CameraCollisionPullOutSmoothSec;
            CameraLockOnShoulderSideM = fresh.CameraLockOnShoulderSideM;
            CameraLockOnShoulderFlipHysteresis = fresh.CameraLockOnShoulderFlipHysteresis;
            CameraLockOnLookBlendToBoss = fresh.CameraLockOnLookBlendToBoss;
            LockOnButtonRadiusDp = fresh.LockOnButtonRadiusDp;
            LockOnButtonOffsetXDp = fresh.LockOnButtonOffsetXDp;
            LockOnButtonOffsetYDp = fresh.LockOnButtonOffsetYDp;
            LockOnButtonColor = fresh.LockOnButtonColor;
            LockOnButtonActiveColor = fresh.LockOnButtonActiveColor;
            AmbientSky = fresh.AmbientSky;
            AmbientEquator = fresh.AmbientEquator;
            AmbientGround = fresh.AmbientGround;
            FogColor = fresh.FogColor;
            FogDensity = fresh.FogDensity;
            KeyLightColor = fresh.KeyLightColor;
            KeyLightIntensity = fresh.KeyLightIntensity;
            KeyLightEuler = fresh.KeyLightEuler;
            KeyShadowStrength = fresh.KeyShadowStrength;
            RimLightColor = fresh.RimLightColor;
            RimLightIntensity = fresh.RimLightIntensity;
            RimLightEuler = fresh.RimLightEuler;
            BloomIntensity = fresh.BloomIntensity;
            BloomThreshold = fresh.BloomThreshold;
            BloomScatter = fresh.BloomScatter;
            PostExposure = fresh.PostExposure;
            ColorContrast = fresh.ColorContrast;
            ColorSaturation = fresh.ColorSaturation;
            PostVignetteIntensity = fresh.PostVignetteIntensity;
            BloomTint = fresh.BloomTint;
            ColorFilterTint = fresh.ColorFilterTint;
            CameraFarClipM = fresh.CameraFarClipM;

            // v17: premium combat HUD — 44dp+ kontroller, ikonlu tepsi ve pasif yuvaları.
            HexagonCenterXNorm = fresh.HexagonCenterXNorm;
            HexagonCenterYNorm = fresh.HexagonCenterYNorm;
            HexagonRadiusDp = fresh.HexagonRadiusDp;
            DotHitRadiusDp = fresh.DotHitRadiusDp;
            CenterHitRadiusDp = fresh.CenterHitRadiusDp;
            DodgeButtonRadiusDp = fresh.DodgeButtonRadiusDp;
            WeaponSwapButtonRadiusDp = fresh.WeaponSwapButtonRadiusDp;
            WeaponSwapIconScale = fresh.WeaponSwapIconScale;
            CombatTrayPaddingDp = fresh.CombatTrayPaddingDp;
            CombatTrayHeaderHeightDp = fresh.CombatTrayHeaderHeightDp;
            CombatTrayCornerRadiusDp = fresh.CombatTrayCornerRadiusDp;
            CombatTrayLinkWidthDp = fresh.CombatTrayLinkWidthDp;
            ElementMenuChipWidthDp = fresh.ElementMenuChipWidthDp;
            ElementMenuChipHeightDp = fresh.ElementMenuChipHeightDp;
            ElementMenuItemWidthDp = fresh.ElementMenuItemWidthDp;
            ElementMenuItemHeightDp = fresh.ElementMenuItemHeightDp;
            ElementMenuAnchorXNorm = fresh.ElementMenuAnchorXNorm;
            ElementMenuAnchorYNorm = fresh.ElementMenuAnchorYNorm;
            VitalsBarWidthDp = fresh.VitalsBarWidthDp;
            VitalsBarHeightDp = fresh.VitalsBarHeightDp;
            VitalsBossBarHeightDp = fresh.VitalsBossBarHeightDp;
            VitalsBossBarWidthDp = fresh.VitalsBossBarWidthDp;
            VitalsHeaderHeightDp = fresh.VitalsHeaderHeightDp;
            VitalsPanelPaddingDp = fresh.VitalsPanelPaddingDp;
            SkillPreviewWidthDp = fresh.SkillPreviewWidthDp;
            SkillPreviewHeightDp = fresh.SkillPreviewHeightDp;

            // v18: telefon HUD'u — kısa kenara sığdırma, sağ element düğmesi, kamera eğimi.
            HudFitShortSideDp = fresh.HudFitShortSideDp;
            HexagonCenterXNorm = fresh.HexagonCenterXNorm;
            ElementMenuAnchorXNorm = fresh.ElementMenuAnchorXNorm;
            ElementMenuAnchorYNorm = fresh.ElementMenuAnchorYNorm;
            CameraPitchMinDeg = fresh.CameraPitchMinDeg;
            CameraPitchMaxDeg = fresh.CameraPitchMaxDeg;
            OrbitInvertPitch = fresh.OrbitInvertPitch;

            // v19 (task-ambience-fix): sahnede serileşmiş eski (gece/karanlık) atmosfer
            // değerleri v16'dan beri hiç kod varsayılanına çekilmemişti — AmbientSky/FogColor/
            // BackgroundColor gibi alanlar zaten vardı, yalnızca değerleri değişti, bu yüzden
            // önceki migrate bloğu bir kez çalışıp sürümü kilitledikten sonra yeni gri/sis
            // varsayılanları hiçbir zaman uygulanmadı (sahne hâlâ koyu lacivert gökyüzü/sis
            // gösteriyordu). Bu blok atmosferi açıkça güncel koda zorlar.
            AmbientSky = fresh.AmbientSky;
            AmbientEquator = fresh.AmbientEquator;
            AmbientGround = fresh.AmbientGround;
            FogColor = fresh.FogColor;
            FogDensity = fresh.FogDensity;
            KeyLightColor = fresh.KeyLightColor;
            KeyLightIntensity = fresh.KeyLightIntensity;
            KeyLightEuler = fresh.KeyLightEuler;
            KeyShadowStrength = fresh.KeyShadowStrength;
            RimLightColor = fresh.RimLightColor;
            RimLightIntensity = fresh.RimLightIntensity;
            RimLightEuler = fresh.RimLightEuler;
            BloomIntensity = fresh.BloomIntensity;
            BloomThreshold = fresh.BloomThreshold;
            BloomScatter = fresh.BloomScatter;
            BloomTint = fresh.BloomTint;
            PostExposure = fresh.PostExposure;
            ColorContrast = fresh.ColorContrast;
            ColorSaturation = fresh.ColorSaturation;
            ColorFilterTint = fresh.ColorFilterTint;
            PostVignetteIntensity = fresh.PostVignetteIntensity;
            CameraFarClipM = fresh.CameraFarClipM;
            GroundColor = fresh.GroundColor;
            BackgroundColor = fresh.BackgroundColor;

            // v20: salon yarıçapı 50 m → 25 m. v19'da kilitlenmiş sahnelerde serileşmiş 50 kalabiliyordu;
            // bilinçli özelleştirmeyi korumak için yalnızca eski varsayılan ±0.01.
            if (Mathf.Abs(ArenaHalfSizeM - 50f) <= 0.01f)
                ArenaHalfSizeM = fresh.ArenaHalfSizeM;

            // v21: varsayılan boss karadul → aglarin_kralicesi; yalnız eski varsayılan taşınır.
            if (ActiveBossId == "karadul")
                ActiveBossId = fresh.ActiveBossId;

            VfxTexFire = fresh.VfxTexFire;
            VfxTexWater = fresh.VfxTexWater;
            VfxTexAir = fresh.VfxTexAir;
            VfxTexEarth = fresh.VfxTexEarth;
            VfxTexLight = fresh.VfxTexLight;
            VfxTexDark = fresh.VfxTexDark;
            VfxTexHit = fresh.VfxTexHit;
            VfxTexInk = fresh.VfxTexInk;

            TuningVersion = CurrentVersion;
        }

        /// <summary>Çekirdek rün → tezahür çizgi rengi.</summary>
        public Color ColorForRune(Dovus.Core.Grammar.Rune rune) => rune switch
        {
            Dovus.Core.Grammar.Rune.Ates => ElementFire,
            Dovus.Core.Grammar.Rune.Su => ElementWater,
            Dovus.Core.Grammar.Rune.Hava => ElementAir,
            Dovus.Core.Grammar.Rune.Toprak => ElementEarth,
            Dovus.Core.Grammar.Rune.Aydinlik => ElementLight,
            Dovus.Core.Grammar.Rune.Karanlik => ElementDark,
            _ => InkCyan
        };

        public bool IsDotOpen(int dot) => true;

        /// <summary>
        /// T10: `PrototypeTuning`'in tamamı (renkler, altıgen konumu, arena...) ayar paneline
        /// AÇILMIYOR — yalnızca bu alt küme (dodge kayma hızı, boss yaklaşımı, kamera takibi,
        /// tepki yazısı zamanlaması). Tam nesneyi JSON'a yazsaydık panelin hiç dokunmadığı
        /// renk/yerleşim alanları da diske kilitlenir, ileride Inspector'dan elle ayarlanan bir
        /// değeri sessizce ezerdi. Bu yüzden ayrı, küçük bir DTO — CombatTuning'in tamamı JSON'a
        /// yazılabiliyor çünkü onun HİÇBİR alanı panel dışı değil (bkz. CombatTuning.CopyFrom).
        /// </summary>
        [System.Serializable]
        public sealed class PanelFields
        {
            public int PlayerMaxHp;
            public float DodgeGlideSpeedMps;
            public float BossApproachStopPadM;
            public float FollowSmoothTimeSec;
            public float LookAheadM;
            public float CameraShakePxToM;
            public float CameraDistanceM;
            public float CameraLookHeightM;
            public float CameraDefaultPitchDeg;
            public float CameraBossAimHeightM;
            public float CameraLockOnMinDistanceM;
            public float CameraLockOnMaxDistanceM;
            public float CameraLockOnDistancePerSepM;
            public float CameraLockOnMaxExtraDistanceM;
            public float CameraWindupDistanceMul;
            public float CameraWindupExtraHeightM;
            public bool ReadoutAnchorRight;
            public float ReadoutPunchInSec;
            // T11: telefonda panelden açılıp kapanır ve kapatılınca öyle kalır. Eski bir
            // tuning.json'da bu alan yok — JsonUtility false verir, o da zaten varsayılan.
            public bool ShowFrameTimeHud;
            // T12: aynı bool deseni — eski JSON'da yoksa false (= kapalı, §5/§12 güvenli).
            public bool ShowDamageNumbers;
        }

        public PanelFields ToPanelFields() => new PanelFields
        {
            PlayerMaxHp = PlayerMaxHp,
            DodgeGlideSpeedMps = DodgeGlideSpeedMps,
            BossApproachStopPadM = BossApproachStopPadM,
            FollowSmoothTimeSec = FollowSmoothTimeSec,
            LookAheadM = LookAheadM,
            CameraShakePxToM = CameraShakePxToM,
            CameraDistanceM = CameraDistanceM,
            CameraLookHeightM = CameraLookHeightM,
            CameraDefaultPitchDeg = CameraDefaultPitchDeg,
            CameraBossAimHeightM = CameraBossAimHeightM,
            CameraLockOnMinDistanceM = CameraLockOnMinDistanceM,
            CameraLockOnMaxDistanceM = CameraLockOnMaxDistanceM,
            CameraLockOnDistancePerSepM = CameraLockOnDistancePerSepM,
            CameraLockOnMaxExtraDistanceM = CameraLockOnMaxExtraDistanceM,
            CameraWindupDistanceMul = CameraWindupDistanceMul,
            CameraWindupExtraHeightM = CameraWindupExtraHeightM,
            ReadoutAnchorRight = ReadoutAnchorRight,
            ReadoutPunchInSec = ReadoutPunchInSec,
            ShowFrameTimeHud = ShowFrameTimeHud,
            ShowDamageNumbers = ShowDamageNumbers,
        };

        public void ApplyPanelFields(PanelFields f)
        {
            if (f == null) return;
            PlayerMaxHp = f.PlayerMaxHp;
            DodgeGlideSpeedMps = f.DodgeGlideSpeedMps;
            BossApproachStopPadM = f.BossApproachStopPadM;
            FollowSmoothTimeSec = f.FollowSmoothTimeSec;
            LookAheadM = f.LookAheadM;
            CameraShakePxToM = f.CameraShakePxToM;
            CameraDistanceM = f.CameraDistanceM;
            CameraLookHeightM = f.CameraLookHeightM;
            CameraDefaultPitchDeg = f.CameraDefaultPitchDeg;
            CameraBossAimHeightM = f.CameraBossAimHeightM;
            CameraLockOnMinDistanceM = f.CameraLockOnMinDistanceM;
            CameraLockOnMaxDistanceM = f.CameraLockOnMaxDistanceM;
            CameraLockOnDistancePerSepM = f.CameraLockOnDistancePerSepM;
            CameraLockOnMaxExtraDistanceM = f.CameraLockOnMaxExtraDistanceM;
            CameraWindupDistanceMul = f.CameraWindupDistanceMul;
            CameraWindupExtraHeightM = f.CameraWindupExtraHeightM;
            ReadoutAnchorRight = f.ReadoutAnchorRight;
            ReadoutPunchInSec = f.ReadoutPunchInSec;
            ShowFrameTimeHud = f.ShowFrameTimeHud;
            ShowDamageNumbers = f.ShowDamageNumbers;
        }

        /// <summary>"Sıfırla": yalnızca panelin yönettiği alt küme spec varsayılanına döner.</summary>
        public void ResetPanelFields() => ApplyPanelFields(new PrototypeTuning().ToPanelFields());
    }
}
