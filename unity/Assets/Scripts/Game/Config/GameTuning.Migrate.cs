using UnityEngine;

namespace Dovus.Game.Config
{
    public sealed partial class GameTuning
    {
        void MigrateToCurrent()
        {
            var fresh = new GameTuning();
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
    }
}
