using Dovus.Game.Config.Sections;
using UnityEngine;

namespace Dovus.Game.Config
{
    public sealed partial class PrototypeTuning
#if !SWEEP_HEADLESS
        : ISerializationCallbackReceiver
#endif
    {
        public ArenaSettings Arena = new ArenaSettings();
        public BossSettings Boss = new BossSettings();
        public CameraSettings Camera = new CameraSettings();
        public HudSettings Hud = new HudSettings();
        public InputSettings Input = new InputSettings();
        public PlayerSettings Player = new PlayerSettings();
        public VisualSettings Visuals = new VisualSettings();

        [SerializeField, HideInInspector]
        int SectionsVersion;

#if !SWEEP_HEADLESS
        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (SectionsVersion >= 1)
                return;
            CopyLegacyFlatFieldsToSections();
            SectionsVersion = 1;
        }
#endif

        /// <summary>SweepV2 sahne YAML düz anahtarları legacy'ye yazar; bölümlere aktar.</summary>
        public void SyncSectionsFromLegacyFlatFields()
        {
            CopyLegacyFlatFieldsToSections();
        }

                void CopyLegacyFlatFieldsToSections()
        {
            Visuals.AcidGreen = AcidGreen;
            Boss.ActiveBossId = ActiveBossId;
            Visuals.ActorPoseDurationMs = ActorPoseDurationMs;
            Hud.AfterimageAlpha = AfterimageAlpha;
            Arena.AmbientEquator = AmbientEquator;
            Arena.AmbientGround = AmbientGround;
            Arena.AmbientSky = AmbientSky;
            Player.AnimCrossFadeSec = AnimCrossFadeSec;
            Player.AnimSpeedDampSec = AnimSpeedDampSec;
            Arena.ArenaHalfSizeM = ArenaHalfSizeM;
            Arena.ArenaVisualScale = ArenaVisualScale;
            Arena.ArenaWallHeightM = ArenaWallHeightM;
            Arena.ArenaWallThicknessM = ArenaWallThicknessM;
            Visuals.BackgroundColor = BackgroundColor;
            Player.BasicStrikeComboResetSec = BasicStrikeComboResetSec;
            Input.BasicStrikeDot = BasicStrikeDot;
            Arena.BloomIntensity = BloomIntensity;
            Arena.BloomScatter = BloomScatter;
            Arena.BloomThreshold = BloomThreshold;
            Arena.BloomTint = BloomTint;
            Boss.BossAnimCrossFadeSec = BossAnimCrossFadeSec;
            Boss.BossApproachStopPadM = BossApproachStopPadM;
            Visuals.BossColor = BossColor;
            Boss.BossConeImpactNorm = BossConeImpactNorm;
            Boss.BossDeathCollapseSec = BossDeathCollapseSec;
            Boss.BossDeathSpreadXz = BossDeathSpreadXz;
            Boss.BossDeathSquashY = BossDeathSquashY;
            Player.BossFootstepStrideM = BossFootstepStrideM;
            Boss.BossGravityMps2 = BossGravityMps2;
            Boss.BossLiftVelocityPerM = BossLiftVelocityPerM;
            Boss.BossPinShakeAmpM = BossPinShakeAmpM;
            Boss.BossRecoilEaseDecayPerSec = BossRecoilEaseDecayPerSec;
            Boss.BossShakeAmpBaseM = BossShakeAmpBaseM;
            Boss.BossShakeAmpPerKnockbackM = BossShakeAmpPerKnockbackM;
            Boss.BossSlamImpactNorm = BossSlamImpactNorm;
            Boss.BossStaggerMinGapSec = BossStaggerMinGapSec;
            Boss.BossTurnRateDegPerSec = BossTurnRateDegPerSec;
            Player.BossVisualHeightM = BossVisualHeightM;
            Hud.BossVitalsColor = BossVitalsColor;
            Boss.BossWalkClipMps = BossWalkClipMps;
            Camera.CameraAimDampingSec = CameraAimDampingSec;
            Camera.CameraBossAimHeightM = CameraBossAimHeightM;
            Camera.CameraCollisionMarginM = CameraCollisionMarginM;
            Camera.CameraCollisionMinDistanceM = CameraCollisionMinDistanceM;
            Camera.CameraCollisionPullInSmoothSec = CameraCollisionPullInSmoothSec;
            Camera.CameraCollisionPullOutSmoothSec = CameraCollisionPullOutSmoothSec;
            Camera.CameraCollisionSphereRadiusM = CameraCollisionSphereRadiusM;
            Camera.CameraDefaultPitchDeg = CameraDefaultPitchDeg;
            Camera.CameraDistanceM = CameraDistanceM;
            Arena.CameraFarClipM = CameraFarClipM;
            Camera.CameraFovDeg = CameraFovDeg;
            Camera.CameraLockOnDistancePerSepM = CameraLockOnDistancePerSepM;
            Camera.CameraLockOnDistanceSmoothSec = CameraLockOnDistanceSmoothSec;
            Camera.CameraLockOnMaxDistanceM = CameraLockOnMaxDistanceM;
            Camera.CameraLockOnMaxExtraDistanceM = CameraLockOnMaxExtraDistanceM;
            Camera.CameraLockOnMinDistanceM = CameraLockOnMinDistanceM;
            Camera.CameraLockOnShoulderFlipHysteresis = CameraLockOnShoulderFlipHysteresis;
            Camera.CameraLockOnShoulderSideM = CameraLockOnShoulderSideM;
            Camera.CameraLookHeightM = CameraLookHeightM;
            Camera.CameraPitchMaxDeg = CameraPitchMaxDeg;
            Camera.CameraPitchMinDeg = CameraPitchMinDeg;
            Hud.CameraShakePxToM = CameraShakePxToM;
            Camera.CameraShoulderOffset = CameraShoulderOffset;
            Camera.CameraSoftLockRangeM = CameraSoftLockRangeM;
            Camera.CameraSoftLockStrength = CameraSoftLockStrength;
            Camera.CameraBossFramingWeight = CameraBossFramingWeight;
            Camera.CameraLockOnLookBlendToBoss = CameraLockOnLookBlendToBoss;
            Input.ElementMenuAnchorXNorm = ElementMenuAnchorXNorm;
            Input.ElementMenuAnchorYNorm = ElementMenuAnchorYNorm;
            Camera.CameraWindupDistanceMul = CameraWindupDistanceMul;
            Camera.CameraWindupExtraHeightM = CameraWindupExtraHeightM;
            Camera.CameraWindupMinRadiusM = CameraWindupMinRadiusM;
            Camera.CameraWindupSmoothSec = CameraWindupSmoothSec;
            Input.CenterHitRadiusDp = CenterHitRadiusDp;
            Player.CharacterAnimSpeed = CharacterAnimSpeed;
            Arena.ColorContrast = ColorContrast;
            Arena.ColorFilterTint = ColorFilterTint;
            Arena.ColorSaturation = ColorSaturation;
            Input.CombatTrayCornerRadiusDp = CombatTrayCornerRadiusDp;
            Input.CombatTrayHeaderHeightDp = CombatTrayHeaderHeightDp;
            Input.CombatTrayLinkWidthDp = CombatTrayLinkWidthDp;
            Input.CombatTrayPaddingDp = CombatTrayPaddingDp;
            Hud.DamageFloatCritFontDp = DamageFloatCritFontDp;
            Hud.DamageFloatFadeSec = DamageFloatFadeSec;
            Hud.DamageFloatFontDp = DamageFloatFontDp;
            Hud.DamageFloatHoldSec = DamageFloatHoldSec;
            Hud.DamageFloatPunchScale = DamageFloatPunchScale;
            Hud.DamageFloatRisePx = DamageFloatRisePx;
            Visuals.DodgeButtonColor = DodgeButtonColor;
            Input.DodgeButtonOffsetXDp = DodgeButtonOffsetXDp;
            Input.DodgeButtonOffsetYDp = DodgeButtonOffsetYDp;
            Input.DodgeButtonRadiusDp = DodgeButtonRadiusDp;
            Input.DodgeButtonScreenMarginDp = DodgeButtonScreenMarginDp;
            Input.DodgeClearanceDp = DodgeClearanceDp;
            Player.DodgeGlideSpeedMps = DodgeGlideSpeedMps;
            Input.DotHitRadiusDp = DotHitRadiusDp;
            Input.DotVibrationMs = DotVibrationMs;
            Visuals.EffectBasicStrikeHeightM = EffectBasicStrikeHeightM;
            Visuals.EffectBasicStrikeLenM = EffectBasicStrikeLenM;
            Visuals.EffectBasicStrikeThickM = EffectBasicStrikeThickM;
            Visuals.EffectBlobScaleBaseM = EffectBlobScaleBaseM;
            Visuals.EffectBlobScalePerSpreadM = EffectBlobScalePerSpreadM;
            Visuals.EffectFocusArcMax = EffectFocusArcMax;
            Visuals.EffectFocusRingMax = EffectFocusRingMax;
            Visuals.EffectFocusSwarmAlongLineMin = EffectFocusSwarmAlongLineMin;
            Visuals.EffectIgneShowMinSpread = EffectIgneShowMinSpread;
            Visuals.EffectLineWidthDefaultM = EffectLineWidthDefaultM;
            Visuals.EffectLineWidthNarrowM = EffectLineWidthNarrowM;
            Visuals.EffectLineWidthWideM = EffectLineWidthWideM;
            Visuals.EffectNeedleAfterimageAlpha = EffectNeedleAfterimageAlpha;
            Visuals.EffectNeedleArrivalLenMul = EffectNeedleArrivalLenMul;
            Visuals.EffectNeedleLenBaseM = EffectNeedleLenBaseM;
            Visuals.EffectNeedleLenPerPierceM = EffectNeedleLenPerPierceM;
            Visuals.EffectNeedleThickNarrowM = EffectNeedleThickNarrowM;
            Visuals.EffectNeedleThickWideM = EffectNeedleThickWideM;
            Visuals.EffectNeedleWindupLenMul = EffectNeedleWindupLenMul;
            Visuals.EffectPierceNeedleShowMin = EffectPierceNeedleShowMin;
            Visuals.EffectSarsintiGroundY = EffectSarsintiGroundY;
            Visuals.EffectSarsintiMassWidthMul = EffectSarsintiMassWidthMul;
            Visuals.EffectSarsintiWidthNarrowM = EffectSarsintiWidthNarrowM;
            Visuals.EffectSarsintiWidthWideM = EffectSarsintiWidthWideM;
            Visuals.EffectShowMinFocus = EffectShowMinFocus;
            Visuals.EffectSwarmJitterM = EffectSwarmJitterM;
            Visuals.EffectSwarmMinBlobs = EffectSwarmMinBlobs;
            Visuals.ElementAir = ElementAir;
            Visuals.ElementDark = ElementDark;
            Visuals.ElementEarth = ElementEarth;
            Visuals.ElementFire = ElementFire;
            Visuals.ElementLight = ElementLight;
            Input.ElementMenuChipHeightDp = ElementMenuChipHeightDp;
            Input.ElementMenuChipWidthDp = ElementMenuChipWidthDp;
            Input.ElementMenuItemHeightDp = ElementMenuItemHeightDp;
            Input.ElementMenuItemWidthDp = ElementMenuItemWidthDp;
            Input.ElementMenuRadiusDp = ElementMenuRadiusDp;
            Visuals.ElementWater = ElementWater;
            Arena.FogColor = FogColor;
            Arena.FogDensity = FogDensity;
            Camera.FollowSmoothTimeSec = FollowSmoothTimeSec;
            Player.FootstepStrideM = FootstepStrideM;
            Hud.FrameTimeSampleSec = FrameTimeSampleSec;
            Visuals.GroundColor = GroundColor;
            Visuals.GroundScarCapCount = GroundScarCapCount;
            Visuals.HexagonCenterColor = HexagonCenterColor;
            Input.HexagonCenterXNorm = HexagonCenterXNorm;
            Input.HexagonCenterYNorm = HexagonCenterYNorm;
            Visuals.HexagonDotColor = HexagonDotColor;
            Input.HexagonRadiusDp = HexagonRadiusDp;
            Input.HudFitShortSideDp = HudFitShortSideDp;
            Input.IconDisplayScale = IconDisplayScale;
            Hud.ImpactFadeSec = ImpactFadeSec;
            Visuals.InkCyan = InkCyan;
            Input.InkLingerSec = InkLingerSec;
            Visuals.InkPurple = InkPurple;
            Input.InkRawGlow = InkRawGlow;
            Input.InkRawWidthScale = InkRawWidthScale;
            Input.InkWidthDp = InkWidthDp;
            Input.JoystickDeadZone = JoystickDeadZone;
            Input.JoystickMaxRadiusDp = JoystickMaxRadiusDp;
            Arena.KeyLightColor = KeyLightColor;
            Arena.KeyLightEuler = KeyLightEuler;
            Arena.KeyLightIntensity = KeyLightIntensity;
            Arena.KeyShadowStrength = KeyShadowStrength;
            Input.LockOnButtonActiveColor = LockOnButtonActiveColor;
            Input.LockOnButtonColor = LockOnButtonColor;
            Input.LockOnButtonOffsetXDp = LockOnButtonOffsetXDp;
            Input.LockOnButtonOffsetYDp = LockOnButtonOffsetYDp;
            Input.LockOnButtonRadiusDp = LockOnButtonRadiusDp;
            Player.LocoMaxPlaybackMult = LocoMaxPlaybackMult;
            Camera.LookAheadM = LookAheadM;
            Player.MinStickSpeedFrac = MinStickSpeedFrac;
            Input.MirrorForLeftHand = MirrorForLeftHand;
            Player.MoveAccelMps2 = MoveAccelMps2;
            Player.MoveDecelMps2 = MoveDecelMps2;
            Camera.OrbitDegreesPerDp = OrbitDegreesPerDp;
            Camera.OrbitInvertPitch = OrbitInvertPitch;
            Visuals.PlayerColor = PlayerColor;
            Player.PlayerMaxHp = PlayerMaxHp;
            Player.PlayerVisualHeightM = PlayerVisualHeightM;
            Visuals.PoseIgne = PoseIgne;
            Visuals.PoseKabuk = PoseKabuk;
            Visuals.PoseSarsinti = PoseSarsinti;
            Visuals.PoseSuru = PoseSuru;
            Visuals.PoseZehir = PoseZehir;
            Arena.PostExposure = PostExposure;
            Arena.PostVignetteIntensity = PostVignetteIntensity;
            Hud.ReadoutAnchorRight = ReadoutAnchorRight;
            Hud.ReadoutPunchInSec = ReadoutPunchInSec;
            Hud.RecoveryLockGapDp = RecoveryLockGapDp;
            Hud.RecoveryLockHeightDp = RecoveryLockHeightDp;
            Arena.RimLightColor = RimLightColor;
            Arena.RimLightEuler = RimLightEuler;
            Arena.RimLightIntensity = RimLightIntensity;
            Hud.ShowDamageNumbers = ShowDamageNumbers;
            Hud.ShowFrameTimeHud = ShowFrameTimeHud;
            Hud.ShowSentenceDebugHud = ShowSentenceDebugHud;
            Hud.SkillPreviewGapDp = SkillPreviewGapDp;
            Hud.SkillPreviewHeightDp = SkillPreviewHeightDp;
            Hud.SkillPreviewHoldSec = SkillPreviewHoldSec;
            Hud.SkillPreviewWidthDp = SkillPreviewWidthDp;
            Hud.SkipBuildSelectOnStart = SkipBuildSelectOnStart;
            Camera.SoftAimConeDeg = SoftAimConeDeg;
            Camera.SoftAimRangeM = SoftAimRangeM;
            Hud.StatusIconGapDp = StatusIconGapDp;
            Hud.StatusIconSizeDp = StatusIconSizeDp;
            Hud.TargetFrameRateHz = TargetFrameRateHz;
            Visuals.TelegraphHot = TelegraphHot;
            Boss.TelegraphSlamSquash = TelegraphSlamSquash;
            Boss.TelegraphSquash = TelegraphSquash;
            Boss.TelegraphStretch = TelegraphStretch;
            Boss.TelegraphTonePitchMax = TelegraphTonePitchMax;
            Boss.TelegraphTonePitchMin = TelegraphTonePitchMin;
            Boss.TelegraphToneVolumeMax = TelegraphToneVolumeMax;
            Boss.TelegraphToneVolumeMin = TelegraphToneVolumeMin;
            Visuals.TelegraphWarm = TelegraphWarm;
            Hud.ThreatAlphaMax = ThreatAlphaMax;
            Hud.ThreatPulseHzMax = ThreatPulseHzMax;
            Hud.ThreatPulseHzMin = ThreatPulseHzMin;
            Player.TurnRateDegPerSec = TurnRateDegPerSec;
            Player.UpperBodyCastMinSpeed = UpperBodyCastMinSpeed;
            Visuals.VfxTexAir = VfxTexAir;
            Visuals.VfxTexDark = VfxTexDark;
            Visuals.VfxTexEarth = VfxTexEarth;
            Visuals.VfxTexFire = VfxTexFire;
            Visuals.VfxTexHit = VfxTexHit;
            Visuals.VfxTexInk = VfxTexInk;
            Visuals.VfxTexLight = VfxTexLight;
            Visuals.VfxTexWater = VfxTexWater;
            Hud.VignetteAlpha = VignetteAlpha;
            Hud.VignetteFadeSec = VignetteFadeSec;
            Hud.VignetteHoldSec = VignetteHoldSec;
            Hud.VitalsBarHeightDp = VitalsBarHeightDp;
            Hud.VitalsBarSpacingDp = VitalsBarSpacingDp;
            Hud.VitalsBarWidthDp = VitalsBarWidthDp;
            Hud.VitalsBossBarHeightDp = VitalsBossBarHeightDp;
            Hud.VitalsBossBarWidthDp = VitalsBossBarWidthDp;
            Hud.VitalsHeaderHeightDp = VitalsHeaderHeightDp;
            Hud.VitalsMarginDp = VitalsMarginDp;
            Hud.VitalsPanelPaddingDp = VitalsPanelPaddingDp;
            Player.WalkSpeedMps = WalkSpeedMps;
            Input.WeaponSwapButtonOffsetXDp = WeaponSwapButtonOffsetXDp;
            Input.WeaponSwapButtonOffsetYDp = WeaponSwapButtonOffsetYDp;
            Input.WeaponSwapButtonRadiusDp = WeaponSwapButtonRadiusDp;
            Input.WeaponSwapIconScale = WeaponSwapIconScale;
            Hud.WindowCuePulseAmp = WindowCuePulseAmp;
            Hud.WindowCuePulseHz = WindowCuePulseHz;
            Hud.WindowCueUrgentHz = WindowCueUrgentHz;
            Hud.WindowCueUrgentRatio = WindowCueUrgentRatio;
        }
    }
}
