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

            Input.BasicStrikeDot = 1;
            Hud.ShowDamageNumbers = true;
            Visuals.ElementAir = new Color(0.50f, 0.70f, 0.62f);
            Hud.ShowSentenceDebugHud = false;
            if (Camera.SoftAimRangeM <= 0.01f) Camera.SoftAimRangeM = 8f;
            if (Camera.SoftAimConeDeg <= 0.01f) Camera.SoftAimConeDeg = 70f;
            if (Camera.OrbitDegreesPerDp <= 0.01f) Camera.OrbitDegreesPerDp = 0.35f;
            if (Hud.StatusIconSizeDp <= 0.01f) Hud.StatusIconSizeDp = 28f;
            if (Hud.VitalsBossBarWidthDp <= 0.01f) Hud.VitalsBossBarWidthDp = 460f;
            if (Hud.VitalsBossBarHeightDp <= 0.01f) Hud.VitalsBossBarHeightDp = 24f;
            if (Hud.VitalsHeaderHeightDp <= 0.01f) Hud.VitalsHeaderHeightDp = 18f;
            if (Hud.VitalsPanelPaddingDp <= 0.01f) Hud.VitalsPanelPaddingDp = 8f;
            if (Hud.DamageFloatFontDp <= 0.01f) Hud.DamageFloatFontDp = 28f;
            if (Input.HexagonRadiusDp <= 0.01f) Input.HexagonRadiusDp = 112f;
            if (Hud.SkillPreviewWidthDp <= 0.01f) Hud.SkillPreviewWidthDp = 324f;
            if (Hud.SkillPreviewHeightDp <= 0.01f) Hud.SkillPreviewHeightDp = 72f;
            if (Hud.SkillPreviewGapDp <= 0.01f) Hud.SkillPreviewGapDp = 10f;
            if (Hud.SkillPreviewHoldSec <= 0.01f) Hud.SkillPreviewHoldSec = 0.9f;
            if (Input.DotHitRadiusDp <= 0.01f) Input.DotHitRadiusDp = 34f;
            if (Input.CenterHitRadiusDp <= 0.01f) Input.CenterHitRadiusDp = 38f;
            if (Input.InkRawWidthScale <= 0f) Input.InkRawWidthScale = 0.9f;
            if (Input.InkRawGlow <= 0f) Input.InkRawGlow = 1f;
            if (string.IsNullOrWhiteSpace(Visuals.VfxTexFire)) Visuals.VfxTexFire = "flame_02";
            if (string.IsNullOrWhiteSpace(Visuals.VfxTexWater)) Visuals.VfxTexWater = "circle_03";
            if (string.IsNullOrWhiteSpace(Visuals.VfxTexAir)) Visuals.VfxTexAir = "twirl_01";
            if (string.IsNullOrWhiteSpace(Visuals.VfxTexEarth)) Visuals.VfxTexEarth = "dirt_01";
            if (string.IsNullOrWhiteSpace(Visuals.VfxTexLight)) Visuals.VfxTexLight = "star_04";
            if (string.IsNullOrWhiteSpace(Visuals.VfxTexDark)) Visuals.VfxTexDark = "magic_04";
            if (string.IsNullOrWhiteSpace(Visuals.VfxTexHit)) Visuals.VfxTexHit = "spark_05";
            if (string.IsNullOrWhiteSpace(Visuals.VfxTexInk)) Visuals.VfxTexInk = "light_01";
            if (Input.DodgeButtonRadiusDp <= 0.01f) Input.DodgeButtonRadiusDp = 40f;
            if (Input.WeaponSwapButtonRadiusDp <= 0.01f) Input.WeaponSwapButtonRadiusDp = 36f;
            if (Input.DodgeClearanceDp <= 0.01f) Input.DodgeClearanceDp = 40f;
            if (Input.IconDisplayScale <= 0.01f) Input.IconDisplayScale = 1.0f;
            if (Input.WeaponSwapIconScale <= 0.01f) Input.WeaponSwapIconScale = 0.72f;
            if (Input.CombatTrayPaddingDp <= 0.01f) Input.CombatTrayPaddingDp = 22f;
            if (Input.CombatTrayHeaderHeightDp <= 0.01f) Input.CombatTrayHeaderHeightDp = 22f;
            if (Input.CombatTrayCornerRadiusDp <= 0.01f) Input.CombatTrayCornerRadiusDp = 20f;
            if (Input.CombatTrayLinkWidthDp <= 0.01f) Input.CombatTrayLinkWidthDp = 1.5f;
            if (Input.ElementMenuChipWidthDp <= 0.01f) Input.ElementMenuChipWidthDp = 144f;
            if (Input.ElementMenuChipHeightDp <= 0.01f) Input.ElementMenuChipHeightDp = 48f;
            if (Input.ElementMenuItemWidthDp <= 0.01f) Input.ElementMenuItemWidthDp = 84f;
            if (Input.ElementMenuItemHeightDp <= 0.01f) Input.ElementMenuItemHeightDp = 48f;
            if (Input.ElementMenuAnchorXNorm <= 0.01f) Input.ElementMenuAnchorXNorm = 0.16f;
            if (Input.ElementMenuAnchorYNorm <= 0.01f) Input.ElementMenuAnchorYNorm = 0.27f;
            if (Input.HudFitShortSideDp < 0f) Input.HudFitShortSideDp = 600f;
            if (Camera.CameraPitchMaxDeg < Camera.CameraPitchMinDeg) { Camera.CameraPitchMinDeg = -8f; Camera.CameraPitchMaxDeg = 35f; }
            if (Player.PlayerVisualHeightM <= 0.01f) Player.PlayerVisualHeightM = 1.78f;
            if (Player.BossVisualHeightM <= 0.01f) Player.BossVisualHeightM = 5.0f;
            if (Player.CharacterAnimSpeed <= 0.01f) Player.CharacterAnimSpeed = 1.0f;
            if (Player.WalkSpeedMps <= 0.01f) Player.WalkSpeedMps = 6.4f;
            if (Player.MoveAccelMps2 <= 0.01f) Player.MoveAccelMps2 = 52f;
            if (Player.MoveDecelMps2 <= 0.01f) Player.MoveDecelMps2 = 64f;
            if (Player.TurnRateDegPerSec <= 0.01f) Player.TurnRateDegPerSec = 720f;
            if (Player.MinStickSpeedFrac <= 0.01f) Player.MinStickSpeedFrac = 0.42f;
            if (Player.AnimSpeedDampSec <= 0f) Player.AnimSpeedDampSec = 0.08f;
            if (Player.LocoMaxPlaybackMult < 1f) Player.LocoMaxPlaybackMult = 1.5f;
            if (Player.AnimCrossFadeSec <= 0f) Player.AnimCrossFadeSec = 0.06f;
            if (Player.BasicStrikeComboResetSec <= 0f) Player.BasicStrikeComboResetSec = 1.2f;
            if (Player.UpperBodyCastMinSpeed <= 0f) Player.UpperBodyCastMinSpeed = 0.15f;
            if (Player.FootstepStrideM <= 0.05f) Player.FootstepStrideM = 2.2f;
            if (Player.BossFootstepStrideM <= 0.05f) Player.BossFootstepStrideM = 2.4f;
            if (string.IsNullOrWhiteSpace(Boss.ActiveBossId)) Boss.ActiveBossId = "karadul";
            if (Boss.BossTurnRateDegPerSec <= 0.01f) Boss.BossTurnRateDegPerSec = 240f;
            if (Boss.BossSlamImpactNorm <= 0.01f) Boss.BossSlamImpactNorm = 0.42f;
            if (Boss.BossConeImpactNorm <= 0.01f) Boss.BossConeImpactNorm = 0.40f;
            if (Boss.BossStaggerMinGapSec <= 0f) Boss.BossStaggerMinGapSec = 0.6f;
            if (Boss.BossAnimCrossFadeSec <= 0f) Boss.BossAnimCrossFadeSec = 0.15f;
            if (Boss.BossWalkClipMps <= 0.01f) Boss.BossWalkClipMps = 1.4f;
            if (Arena.ArenaHalfSizeM <= 0.01f) Arena.ArenaHalfSizeM = 25f;
            if (Arena.ArenaWallHeightM <= 0.01f) Arena.ArenaWallHeightM = 18f;
            if (Arena.ArenaWallThicknessM <= 0.01f) Arena.ArenaWallThicknessM = 1.4f;
            if (Camera.CameraDistanceM <= 0.01f) Camera.CameraDistanceM = 5.85f;
            if (Camera.CameraFovDeg <= 1f) Camera.CameraFovDeg = 54f;
            if (Camera.CameraAimDampingSec <= 0f) Camera.CameraAimDampingSec = 0.12f;
            if (Camera.CameraSoftLockRangeM <= 0f) Camera.CameraSoftLockRangeM = 20f;
            if (Camera.CameraBossAimHeightM <= 0f) Camera.CameraBossAimHeightM = 1.45f;
            if (Camera.CameraLockOnMinDistanceM <= 0.01f) Camera.CameraLockOnMinDistanceM = 5.2f;
            if (Camera.CameraLockOnMaxDistanceM <= Camera.CameraLockOnMinDistanceM) Camera.CameraLockOnMaxDistanceM = 8.8f;
            if (Camera.CameraLockOnDistanceSmoothSec <= 0.01f) Camera.CameraLockOnDistanceSmoothSec = 0.22f;
            if (Camera.CameraWindupDistanceMul < 1f) Camera.CameraWindupDistanceMul = 1.42f;
            if (Camera.CameraWindupSmoothSec <= 0.01f) Camera.CameraWindupSmoothSec = 0.28f;
            if (Camera.CameraCollisionSphereRadiusM <= 0.01f) Camera.CameraCollisionSphereRadiusM = 0.25f;
            if (Camera.CameraCollisionMarginM < 0f) Camera.CameraCollisionMarginM = 0.12f;
            if (Camera.CameraCollisionMinDistanceM <= 0.1f) Camera.CameraCollisionMinDistanceM = 1.2f;
            if (Camera.CameraCollisionPullInSmoothSec <= 0.001f) Camera.CameraCollisionPullInSmoothSec = 0.05f;
            if (Camera.CameraCollisionPullOutSmoothSec <= 0.01f) Camera.CameraCollisionPullOutSmoothSec = 0.35f;
            if (Camera.CameraLockOnShoulderSideM <= 0.01f) Camera.CameraLockOnShoulderSideM = 1.1f;
            if (Camera.CameraLockOnShoulderFlipHysteresis <= 0f) Camera.CameraLockOnShoulderFlipHysteresis = 0.12f;
            if (Input.LockOnButtonRadiusDp <= 0.01f) Input.LockOnButtonRadiusDp = 36f;
            if (Arena.FogDensity <= 0f) Arena.FogDensity = 0.0032f;
            if (Arena.KeyLightIntensity <= 0f) Arena.KeyLightIntensity = 1.35f;
            if (Arena.BloomThreshold <= 0f) Arena.BloomThreshold = 1.05f;
        }

        void MigrateToCurrent()
        {
            var fresh = new PrototypeTuning();
            Player.PlayerMaxHp = fresh.Player.PlayerMaxHp;
            Hud.WindowCueUrgentRatio = fresh.Hud.WindowCueUrgentRatio;
            Hud.WindowCuePulseHz = fresh.Hud.WindowCuePulseHz;
            Hud.WindowCueUrgentHz = fresh.Hud.WindowCueUrgentHz;
            Hud.WindowCuePulseAmp = fresh.Hud.WindowCuePulseAmp;
            Visuals.TelegraphHot = fresh.Visuals.TelegraphHot;
            Visuals.TelegraphWarm = fresh.Visuals.TelegraphWarm;
            Player.DodgeGlideSpeedMps = fresh.Player.DodgeGlideSpeedMps;
            Boss.TelegraphStretch = fresh.Boss.TelegraphStretch;
            Boss.TelegraphSquash = fresh.Boss.TelegraphSquash;
            Boss.TelegraphSlamSquash = fresh.Boss.TelegraphSlamSquash;
            Boss.TelegraphTonePitchMin = fresh.Boss.TelegraphTonePitchMin;
            Boss.TelegraphTonePitchMax = fresh.Boss.TelegraphTonePitchMax;
            Boss.TelegraphToneVolumeMin = fresh.Boss.TelegraphToneVolumeMin;
            Boss.TelegraphToneVolumeMax = fresh.Boss.TelegraphToneVolumeMax;
            Boss.BossApproachStopPadM = fresh.Boss.BossApproachStopPadM;
            Hud.AfterimageAlpha = fresh.Hud.AfterimageAlpha;
            Hud.ImpactFadeSec = fresh.Hud.ImpactFadeSec;
            Hud.VignetteHoldSec = fresh.Hud.VignetteHoldSec;
            Hud.VignetteFadeSec = fresh.Hud.VignetteFadeSec;
            Hud.VignetteAlpha = fresh.Hud.VignetteAlpha;
            Hud.ThreatAlphaMax = fresh.Hud.ThreatAlphaMax;
            Hud.ThreatPulseHzMin = fresh.Hud.ThreatPulseHzMin;
            Hud.ThreatPulseHzMax = fresh.Hud.ThreatPulseHzMax;
            Hud.CameraShakePxToM = fresh.Hud.CameraShakePxToM;

            // T9: yeni alanlar, aynı "sürüm damgası bir kez yamalar" deseni (yukarısı).
            Hud.ReadoutAnchorRight = fresh.Hud.ReadoutAnchorRight;
            Hud.ReadoutPunchInSec = fresh.Hud.ReadoutPunchInSec;
            Hud.VitalsBarWidthDp = fresh.Hud.VitalsBarWidthDp;
            Hud.VitalsBarHeightDp = fresh.Hud.VitalsBarHeightDp;
            Hud.VitalsBarSpacingDp = fresh.Hud.VitalsBarSpacingDp;
            Hud.VitalsMarginDp = fresh.Hud.VitalsMarginDp;
            Hud.VitalsHeaderHeightDp = fresh.Hud.VitalsHeaderHeightDp;
            Hud.BossVitalsColor = fresh.Hud.BossVitalsColor;

            // T11.1: kilit HUD ölçüleri.
            Hud.RecoveryLockHeightDp = fresh.Hud.RecoveryLockHeightDp;
            Hud.RecoveryLockGapDp = fresh.Hud.RecoveryLockGapDp;

            // T11: aynı desen. Hud.TargetFrameRateHz 0 gelirse Application.targetFrameRate anlamsız
            // bir değere düşer, o yüzden bu yama ölçüm turu için kritik.
            Hud.FrameTimeSampleSec = fresh.Hud.FrameTimeSampleSec;
            Hud.TargetFrameRateHz = fresh.Hud.TargetFrameRateHz;

            // T12: ölüm pozu (0 gelirse squash görünmez / süre anlamsız).
            Boss.BossDeathCollapseSec = fresh.Boss.BossDeathCollapseSec;
            Boss.BossDeathSquashY = fresh.Boss.BossDeathSquashY;
            Boss.BossDeathSpreadXz = fresh.Boss.BossDeathSpreadXz;

            // T14: hareket karakteri ölçüleri (0 gelirse Zenitsu/sürü/halka/jab kaybolur).
            Visuals.EffectNeedleWindupLenMul = fresh.Visuals.EffectNeedleWindupLenMul;
            Visuals.EffectNeedleArrivalLenMul = fresh.Visuals.EffectNeedleArrivalLenMul;
            Visuals.EffectNeedleAfterimageAlpha = fresh.Visuals.EffectNeedleAfterimageAlpha;
            Visuals.EffectSwarmJitterM = fresh.Visuals.EffectSwarmJitterM;
            Visuals.EffectSwarmMinBlobs = fresh.Visuals.EffectSwarmMinBlobs;
            Visuals.EffectSarsintiGroundY = fresh.Visuals.EffectSarsintiGroundY;
            Visuals.EffectSarsintiMassWidthMul = fresh.Visuals.EffectSarsintiMassWidthMul;
            Visuals.EffectBasicStrikeLenM = fresh.Visuals.EffectBasicStrikeLenM;
            Visuals.EffectBasicStrikeThickM = fresh.Visuals.EffectBasicStrikeThickM;
            Visuals.EffectBasicStrikeHeightM = fresh.Visuals.EffectBasicStrikeHeightM;

            // v9: dodge sağ-alt (Toprak'tan uzak); ikon ölçeği.
            Input.DodgeButtonOffsetXDp = 100f;
            Input.DodgeButtonOffsetYDp = -100f;
            Input.DodgeButtonRadiusDp = 38f;
            Input.IconDisplayScale = 1.12f;
            Visuals.ElementFire = fresh.Visuals.ElementFire;
            Visuals.ElementWater = fresh.Visuals.ElementWater;
            Visuals.ElementAir = fresh.Visuals.ElementAir;
            Visuals.ElementEarth = fresh.Visuals.ElementEarth;
            Visuals.ElementLight = fresh.Visuals.ElementLight;
            Visuals.ElementDark = fresh.Visuals.ElementDark;

            // v10 HUD / orbit / float
            Hud.VitalsBarWidthDp = fresh.Hud.VitalsBarWidthDp;
            Hud.VitalsBarHeightDp = fresh.Hud.VitalsBarHeightDp;
            Hud.VitalsBossBarHeightDp = fresh.Hud.VitalsBossBarHeightDp;
            Hud.VitalsBossBarWidthDp = fresh.Hud.VitalsBossBarWidthDp;
            Hud.VitalsHeaderHeightDp = fresh.Hud.VitalsHeaderHeightDp;
            Hud.StatusIconSizeDp = fresh.Hud.StatusIconSizeDp;
            Hud.StatusIconGapDp = fresh.Hud.StatusIconGapDp;
            Camera.OrbitDegreesPerDp = fresh.Camera.OrbitDegreesPerDp;
            Camera.SoftAimRangeM = fresh.Camera.SoftAimRangeM;
            Camera.SoftAimConeDeg = fresh.Camera.SoftAimConeDeg;
            Hud.DamageFloatFontDp = fresh.Hud.DamageFloatFontDp;
            Hud.DamageFloatCritFontDp = fresh.Hud.DamageFloatCritFontDp;
            Hud.DamageFloatRisePx = fresh.Hud.DamageFloatRisePx;
            Hud.DamageFloatHoldSec = fresh.Hud.DamageFloatHoldSec;
            Hud.DamageFloatFadeSec = fresh.Hud.DamageFloatFadeSec;
            Hud.DamageFloatPunchScale = fresh.Hud.DamageFloatPunchScale;

            // v12: hex/dodge/vitals — telefon screenshot'ta alt rün kesiliyordu, dodge Hava üstündeydi.
            // v13: rünler arası + dodge çizim boşluğu.
            Input.HexagonCenterXNorm = fresh.Input.HexagonCenterXNorm;
            Input.HexagonCenterYNorm = fresh.Input.HexagonCenterYNorm;
            Input.HexagonRadiusDp = fresh.Input.HexagonRadiusDp;
            Input.CenterHitRadiusDp = fresh.Input.CenterHitRadiusDp;
            Input.DotHitRadiusDp = fresh.Input.DotHitRadiusDp;
            Input.DodgeButtonRadiusDp = fresh.Input.DodgeButtonRadiusDp;
            Input.DodgeButtonOffsetXDp = fresh.Input.DodgeButtonOffsetXDp;
            Input.DodgeButtonOffsetYDp = fresh.Input.DodgeButtonOffsetYDp;
            Input.DodgeClearanceDp = fresh.Input.DodgeClearanceDp;
            Input.WeaponSwapButtonRadiusDp = fresh.Input.WeaponSwapButtonRadiusDp;
            Input.WeaponSwapButtonOffsetXDp = fresh.Input.WeaponSwapButtonOffsetXDp;
            Input.WeaponSwapButtonOffsetYDp = fresh.Input.WeaponSwapButtonOffsetYDp;
            Input.IconDisplayScale = fresh.Input.IconDisplayScale;
            Hud.VitalsBarWidthDp = fresh.Hud.VitalsBarWidthDp;
            Hud.VitalsBarHeightDp = fresh.Hud.VitalsBarHeightDp;
            Hud.VitalsBossBarHeightDp = fresh.Hud.VitalsBossBarHeightDp;
            Hud.VitalsBossBarWidthDp = fresh.Hud.VitalsBossBarWidthDp;
            Hud.VitalsBarSpacingDp = fresh.Hud.VitalsBarSpacingDp;
            Hud.VitalsMarginDp = fresh.Hud.VitalsMarginDp;
            Hud.ShowSentenceDebugHud = false;

            // v14: 100 m çap daire salon. v15: yeni locomotion klipleri.
            // v16: insan ölçeği + büyük boss, omuz kamerası, mobil atmosfer ve okunur HUD.
            Arena.ArenaHalfSizeM = fresh.Arena.ArenaHalfSizeM;
            Arena.ArenaWallHeightM = fresh.Arena.ArenaWallHeightM;
            Arena.ArenaWallThicknessM = fresh.Arena.ArenaWallThicknessM;
            Arena.ArenaVisualScale = fresh.Arena.ArenaVisualScale;
            Player.PlayerVisualHeightM = fresh.Player.PlayerVisualHeightM;
            Player.BossVisualHeightM = fresh.Player.BossVisualHeightM;
            Player.CharacterAnimSpeed = fresh.Player.CharacterAnimSpeed;
            Player.WalkSpeedMps = fresh.Player.WalkSpeedMps;
            Player.MoveAccelMps2 = fresh.Player.MoveAccelMps2;
            Player.MoveDecelMps2 = fresh.Player.MoveDecelMps2;
            Player.TurnRateDegPerSec = fresh.Player.TurnRateDegPerSec;
            Player.MinStickSpeedFrac = fresh.Player.MinStickSpeedFrac;
            Player.DodgeGlideSpeedMps = fresh.Player.DodgeGlideSpeedMps;
            Input.HexagonRadiusDp = fresh.Input.HexagonRadiusDp;
            Input.DotHitRadiusDp = fresh.Input.DotHitRadiusDp;
            Input.CenterHitRadiusDp = fresh.Input.CenterHitRadiusDp;
            Input.DodgeButtonRadiusDp = fresh.Input.DodgeButtonRadiusDp;
            Hud.VitalsBarWidthDp = fresh.Hud.VitalsBarWidthDp;
            Hud.VitalsBarHeightDp = fresh.Hud.VitalsBarHeightDp;
            Hud.VitalsBossBarHeightDp = fresh.Hud.VitalsBossBarHeightDp;
            Hud.VitalsBossBarWidthDp = fresh.Hud.VitalsBossBarWidthDp;
            Hud.VitalsBarSpacingDp = fresh.Hud.VitalsBarSpacingDp;
            Hud.VitalsMarginDp = fresh.Hud.VitalsMarginDp;
            Hud.BossVitalsColor = fresh.Hud.BossVitalsColor;
            Hud.SkillPreviewWidthDp = fresh.Hud.SkillPreviewWidthDp;
            Hud.SkillPreviewHeightDp = fresh.Hud.SkillPreviewHeightDp;
            Camera.FollowSmoothTimeSec = fresh.Camera.FollowSmoothTimeSec;
            Camera.LookAheadM = fresh.Camera.LookAheadM;
            Camera.CameraShoulderOffset = fresh.Camera.CameraShoulderOffset;
            Camera.CameraDistanceM = fresh.Camera.CameraDistanceM;
            Camera.CameraLookHeightM = fresh.Camera.CameraLookHeightM;
            Camera.CameraFovDeg = fresh.Camera.CameraFovDeg;
            Camera.CameraAimDampingSec = fresh.Camera.CameraAimDampingSec;
            Camera.CameraSoftLockRangeM = fresh.Camera.CameraSoftLockRangeM;
            Camera.CameraSoftLockStrength = fresh.Camera.CameraSoftLockStrength;
            Camera.CameraBossFramingWeight = fresh.Camera.CameraBossFramingWeight;
            Camera.CameraBossAimHeightM = fresh.Camera.CameraBossAimHeightM;
            Camera.CameraDefaultPitchDeg = fresh.Camera.CameraDefaultPitchDeg;
            Camera.CameraLockOnMinDistanceM = fresh.Camera.CameraLockOnMinDistanceM;
            Camera.CameraLockOnMaxDistanceM = fresh.Camera.CameraLockOnMaxDistanceM;
            Camera.CameraLockOnDistancePerSepM = fresh.Camera.CameraLockOnDistancePerSepM;
            Camera.CameraLockOnMaxExtraDistanceM = fresh.Camera.CameraLockOnMaxExtraDistanceM;
            Camera.CameraLockOnDistanceSmoothSec = fresh.Camera.CameraLockOnDistanceSmoothSec;
            Camera.CameraWindupDistanceMul = fresh.Camera.CameraWindupDistanceMul;
            Camera.CameraWindupExtraHeightM = fresh.Camera.CameraWindupExtraHeightM;
            Camera.CameraWindupSmoothSec = fresh.Camera.CameraWindupSmoothSec;
            Camera.CameraWindupMinRadiusM = fresh.Camera.CameraWindupMinRadiusM;
            Camera.CameraCollisionSphereRadiusM = fresh.Camera.CameraCollisionSphereRadiusM;
            Camera.CameraCollisionMarginM = fresh.Camera.CameraCollisionMarginM;
            Camera.CameraCollisionMinDistanceM = fresh.Camera.CameraCollisionMinDistanceM;
            Camera.CameraCollisionPullInSmoothSec = fresh.Camera.CameraCollisionPullInSmoothSec;
            Camera.CameraCollisionPullOutSmoothSec = fresh.Camera.CameraCollisionPullOutSmoothSec;
            Camera.CameraLockOnShoulderSideM = fresh.Camera.CameraLockOnShoulderSideM;
            Camera.CameraLockOnShoulderFlipHysteresis = fresh.Camera.CameraLockOnShoulderFlipHysteresis;
            Camera.CameraLockOnLookBlendToBoss = fresh.Camera.CameraLockOnLookBlendToBoss;
            Input.LockOnButtonRadiusDp = fresh.Input.LockOnButtonRadiusDp;
            Input.LockOnButtonOffsetXDp = fresh.Input.LockOnButtonOffsetXDp;
            Input.LockOnButtonOffsetYDp = fresh.Input.LockOnButtonOffsetYDp;
            Input.LockOnButtonColor = fresh.Input.LockOnButtonColor;
            Input.LockOnButtonActiveColor = fresh.Input.LockOnButtonActiveColor;
            Arena.AmbientSky = fresh.Arena.AmbientSky;
            Arena.AmbientEquator = fresh.Arena.AmbientEquator;
            Arena.AmbientGround = fresh.Arena.AmbientGround;
            Arena.FogColor = fresh.Arena.FogColor;
            Arena.FogDensity = fresh.Arena.FogDensity;
            Arena.KeyLightColor = fresh.Arena.KeyLightColor;
            Arena.KeyLightIntensity = fresh.Arena.KeyLightIntensity;
            Arena.KeyLightEuler = fresh.Arena.KeyLightEuler;
            Arena.KeyShadowStrength = fresh.Arena.KeyShadowStrength;
            Arena.RimLightColor = fresh.Arena.RimLightColor;
            Arena.RimLightIntensity = fresh.Arena.RimLightIntensity;
            Arena.RimLightEuler = fresh.Arena.RimLightEuler;
            Arena.BloomIntensity = fresh.Arena.BloomIntensity;
            Arena.BloomThreshold = fresh.Arena.BloomThreshold;
            Arena.BloomScatter = fresh.Arena.BloomScatter;
            Arena.PostExposure = fresh.Arena.PostExposure;
            Arena.ColorContrast = fresh.Arena.ColorContrast;
            Arena.ColorSaturation = fresh.Arena.ColorSaturation;
            Arena.PostVignetteIntensity = fresh.Arena.PostVignetteIntensity;
            Arena.BloomTint = fresh.Arena.BloomTint;
            Arena.ColorFilterTint = fresh.Arena.ColorFilterTint;
            Arena.CameraFarClipM = fresh.Arena.CameraFarClipM;

            // v17: premium combat HUD — 44dp+ kontroller, ikonlu tepsi ve pasif yuvaları.
            Input.HexagonCenterXNorm = fresh.Input.HexagonCenterXNorm;
            Input.HexagonCenterYNorm = fresh.Input.HexagonCenterYNorm;
            Input.HexagonRadiusDp = fresh.Input.HexagonRadiusDp;
            Input.DotHitRadiusDp = fresh.Input.DotHitRadiusDp;
            Input.CenterHitRadiusDp = fresh.Input.CenterHitRadiusDp;
            Input.DodgeButtonRadiusDp = fresh.Input.DodgeButtonRadiusDp;
            Input.WeaponSwapButtonRadiusDp = fresh.Input.WeaponSwapButtonRadiusDp;
            Input.WeaponSwapIconScale = fresh.Input.WeaponSwapIconScale;
            Input.CombatTrayPaddingDp = fresh.Input.CombatTrayPaddingDp;
            Input.CombatTrayHeaderHeightDp = fresh.Input.CombatTrayHeaderHeightDp;
            Input.CombatTrayCornerRadiusDp = fresh.Input.CombatTrayCornerRadiusDp;
            Input.CombatTrayLinkWidthDp = fresh.Input.CombatTrayLinkWidthDp;
            Input.ElementMenuChipWidthDp = fresh.Input.ElementMenuChipWidthDp;
            Input.ElementMenuChipHeightDp = fresh.Input.ElementMenuChipHeightDp;
            Input.ElementMenuItemWidthDp = fresh.Input.ElementMenuItemWidthDp;
            Input.ElementMenuItemHeightDp = fresh.Input.ElementMenuItemHeightDp;
            Input.ElementMenuAnchorXNorm = fresh.Input.ElementMenuAnchorXNorm;
            Input.ElementMenuAnchorYNorm = fresh.Input.ElementMenuAnchorYNorm;
            Hud.VitalsBarWidthDp = fresh.Hud.VitalsBarWidthDp;
            Hud.VitalsBarHeightDp = fresh.Hud.VitalsBarHeightDp;
            Hud.VitalsBossBarHeightDp = fresh.Hud.VitalsBossBarHeightDp;
            Hud.VitalsBossBarWidthDp = fresh.Hud.VitalsBossBarWidthDp;
            Hud.VitalsHeaderHeightDp = fresh.Hud.VitalsHeaderHeightDp;
            Hud.VitalsPanelPaddingDp = fresh.Hud.VitalsPanelPaddingDp;
            Hud.SkillPreviewWidthDp = fresh.Hud.SkillPreviewWidthDp;
            Hud.SkillPreviewHeightDp = fresh.Hud.SkillPreviewHeightDp;

            // v18: telefon HUD'u — kısa kenara sığdırma, sağ element düğmesi, kamera eğimi.
            Input.HudFitShortSideDp = fresh.Input.HudFitShortSideDp;
            Input.HexagonCenterXNorm = fresh.Input.HexagonCenterXNorm;
            Input.ElementMenuAnchorXNorm = fresh.Input.ElementMenuAnchorXNorm;
            Input.ElementMenuAnchorYNorm = fresh.Input.ElementMenuAnchorYNorm;
            Camera.CameraPitchMinDeg = fresh.Camera.CameraPitchMinDeg;
            Camera.CameraPitchMaxDeg = fresh.Camera.CameraPitchMaxDeg;
            Camera.OrbitInvertPitch = fresh.Camera.OrbitInvertPitch;

            // v19 (task-ambience-fix): sahnede serileşmiş eski (gece/karanlık) atmosfer
            // değerleri v16'dan beri hiç kod varsayılanına çekilmemişti — Arena.AmbientSky/Arena.FogColor/
            // Visuals.BackgroundColor gibi alanlar zaten vardı, yalnızca değerleri değişti, bu yüzden
            // önceki migrate bloğu bir kez çalışıp sürümü kilitledikten sonra yeni gri/sis
            // varsayılanları hiçbir zaman uygulanmadı (sahne hâlâ koyu lacivert gökyüzü/sis
            // gösteriyordu). Bu blok atmosferi açıkça güncel koda zorlar.
            Arena.AmbientSky = fresh.Arena.AmbientSky;
            Arena.AmbientEquator = fresh.Arena.AmbientEquator;
            Arena.AmbientGround = fresh.Arena.AmbientGround;
            Arena.FogColor = fresh.Arena.FogColor;
            Arena.FogDensity = fresh.Arena.FogDensity;
            Arena.KeyLightColor = fresh.Arena.KeyLightColor;
            Arena.KeyLightIntensity = fresh.Arena.KeyLightIntensity;
            Arena.KeyLightEuler = fresh.Arena.KeyLightEuler;
            Arena.KeyShadowStrength = fresh.Arena.KeyShadowStrength;
            Arena.RimLightColor = fresh.Arena.RimLightColor;
            Arena.RimLightIntensity = fresh.Arena.RimLightIntensity;
            Arena.RimLightEuler = fresh.Arena.RimLightEuler;
            Arena.BloomIntensity = fresh.Arena.BloomIntensity;
            Arena.BloomThreshold = fresh.Arena.BloomThreshold;
            Arena.BloomScatter = fresh.Arena.BloomScatter;
            Arena.BloomTint = fresh.Arena.BloomTint;
            Arena.PostExposure = fresh.Arena.PostExposure;
            Arena.ColorContrast = fresh.Arena.ColorContrast;
            Arena.ColorSaturation = fresh.Arena.ColorSaturation;
            Arena.ColorFilterTint = fresh.Arena.ColorFilterTint;
            Arena.PostVignetteIntensity = fresh.Arena.PostVignetteIntensity;
            Arena.CameraFarClipM = fresh.Arena.CameraFarClipM;
            Visuals.GroundColor = fresh.Visuals.GroundColor;
            Visuals.BackgroundColor = fresh.Visuals.BackgroundColor;

            // v20: salon yarıçapı 50 m → 25 m. v19'da kilitlenmiş sahnelerde serileşmiş 50 kalabiliyordu;
            // bilinçli özelleştirmeyi korumak için yalnızca eski varsayılan ±0.01.
            if (Mathf.Abs(Arena.ArenaHalfSizeM - 50f) <= 0.01f)
                Arena.ArenaHalfSizeM = fresh.Arena.ArenaHalfSizeM;

            // v21: varsayılan boss karadul → aglarin_kralicesi; yalnız eski varsayılan taşınır.
            if (Boss.ActiveBossId == "karadul")
                Boss.ActiveBossId = fresh.Boss.ActiveBossId;

            Visuals.VfxTexFire = fresh.Visuals.VfxTexFire;
            Visuals.VfxTexWater = fresh.Visuals.VfxTexWater;
            Visuals.VfxTexAir = fresh.Visuals.VfxTexAir;
            Visuals.VfxTexEarth = fresh.Visuals.VfxTexEarth;
            Visuals.VfxTexLight = fresh.Visuals.VfxTexLight;
            Visuals.VfxTexDark = fresh.Visuals.VfxTexDark;
            Visuals.VfxTexHit = fresh.Visuals.VfxTexHit;
            Visuals.VfxTexInk = fresh.Visuals.VfxTexInk;

            TuningVersion = CurrentVersion;
        }

        /// <summary>Çekirdek rün → tezahür çizgi rengi.</summary>
        public Color ColorForRune(Dovus.Core.Grammar.Rune rune) => rune switch
        {
            Dovus.Core.Grammar.Rune.Ates => Visuals.ElementFire,
            Dovus.Core.Grammar.Rune.Su => Visuals.ElementWater,
            Dovus.Core.Grammar.Rune.Hava => Visuals.ElementAir,
            Dovus.Core.Grammar.Rune.Toprak => Visuals.ElementEarth,
            Dovus.Core.Grammar.Rune.Aydinlik => Visuals.ElementLight,
            Dovus.Core.Grammar.Rune.Karanlik => Visuals.ElementDark,
            _ => Visuals.InkCyan
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
            PlayerMaxHp = Player.PlayerMaxHp,
            DodgeGlideSpeedMps = Player.DodgeGlideSpeedMps,
            BossApproachStopPadM = Boss.BossApproachStopPadM,
            FollowSmoothTimeSec = Camera.FollowSmoothTimeSec,
            LookAheadM = Camera.LookAheadM,
            CameraShakePxToM = Hud.CameraShakePxToM,
            CameraDistanceM = Camera.CameraDistanceM,
            CameraLookHeightM = Camera.CameraLookHeightM,
            CameraDefaultPitchDeg = Camera.CameraDefaultPitchDeg,
            CameraBossAimHeightM = Camera.CameraBossAimHeightM,
            CameraLockOnMinDistanceM = Camera.CameraLockOnMinDistanceM,
            CameraLockOnMaxDistanceM = Camera.CameraLockOnMaxDistanceM,
            CameraLockOnDistancePerSepM = Camera.CameraLockOnDistancePerSepM,
            CameraLockOnMaxExtraDistanceM = Camera.CameraLockOnMaxExtraDistanceM,
            CameraWindupDistanceMul = Camera.CameraWindupDistanceMul,
            CameraWindupExtraHeightM = Camera.CameraWindupExtraHeightM,
            ReadoutAnchorRight = Hud.ReadoutAnchorRight,
            ReadoutPunchInSec = Hud.ReadoutPunchInSec,
            ShowFrameTimeHud = Hud.ShowFrameTimeHud,
            ShowDamageNumbers = Hud.ShowDamageNumbers,
        };

        public void ApplyPanelFields(PanelFields f)
        {
            if (f == null) return;
            Player.PlayerMaxHp = f.PlayerMaxHp;
            Player.DodgeGlideSpeedMps = f.DodgeGlideSpeedMps;
            Boss.BossApproachStopPadM = f.BossApproachStopPadM;
            Camera.FollowSmoothTimeSec = f.FollowSmoothTimeSec;
            Camera.LookAheadM = f.LookAheadM;
            Hud.CameraShakePxToM = f.CameraShakePxToM;
            Camera.CameraDistanceM = f.CameraDistanceM;
            Camera.CameraLookHeightM = f.CameraLookHeightM;
            Camera.CameraDefaultPitchDeg = f.CameraDefaultPitchDeg;
            Camera.CameraBossAimHeightM = f.CameraBossAimHeightM;
            Camera.CameraLockOnMinDistanceM = f.CameraLockOnMinDistanceM;
            Camera.CameraLockOnMaxDistanceM = f.CameraLockOnMaxDistanceM;
            Camera.CameraLockOnDistancePerSepM = f.CameraLockOnDistancePerSepM;
            Camera.CameraLockOnMaxExtraDistanceM = f.CameraLockOnMaxExtraDistanceM;
            Camera.CameraWindupDistanceMul = f.CameraWindupDistanceMul;
            Camera.CameraWindupExtraHeightM = f.CameraWindupExtraHeightM;
            Hud.ReadoutAnchorRight = f.ReadoutAnchorRight;
            Hud.ReadoutPunchInSec = f.ReadoutPunchInSec;
            Hud.ShowFrameTimeHud = f.ShowFrameTimeHud;
            Hud.ShowDamageNumbers = f.ShowDamageNumbers;
        }

        /// <summary>"Sıfırla": yalnızca panelin yönettiği alt küme spec varsayılanına döner.</summary>
        public void ResetPanelFields() => ApplyPanelFields(new PrototypeTuning().ToPanelFields());
    }
}
