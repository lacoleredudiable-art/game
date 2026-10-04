using UnityEngine;

namespace Dovus.Game.Config
{
    public sealed partial class PrototypeTuning
    {
        // Unity sahne YAML düz anahtarları — runtime okuma bölümlerde.
        [Header("Arena")]
        [SerializeField, HideInInspector]
        private float ArenaHalfSizeM = 25f;
        [SerializeField, HideInInspector]
        private float ArenaWallHeightM = 18f;
        [SerializeField, HideInInspector]
        private float ArenaWallThicknessM = 1.4f;
        [SerializeField, HideInInspector]
        private float ArenaVisualScale = 1.0f;
        [Header("Arena atmosferi — mobil URP (ambiyans: PR #43 açık gri lav ovası)")]
        [SerializeField, HideInInspector]
        private Color AmbientSky = new Color(0.66f, 0.70f, 0.73f);
        [SerializeField, HideInInspector]
        private Color AmbientEquator = new Color(0.52f, 0.55f, 0.58f);
        [SerializeField, HideInInspector]
        private Color AmbientGround = new Color(0.30f, 0.30f, 0.31f);
        [SerializeField, HideInInspector]
        private Color FogColor = new Color(0.69f, 0.718f, 0.737f);
        [SerializeField, HideInInspector]
        private float FogDensity = 0.0032f;
        [SerializeField, HideInInspector]
        private Color KeyLightColor = new Color(0.86f, 0.89f, 0.92f);
        [SerializeField, HideInInspector]
        private float KeyLightIntensity = 0.85f;
        [SerializeField, HideInInspector]
        private Vector3 KeyLightEuler = new Vector3(52f, -30f, 0f);
        [SerializeField, HideInInspector]
        private float KeyShadowStrength = 0.38f;
        [SerializeField, HideInInspector]
        private Color RimLightColor = new Color(0.38f, 0.55f, 1f);
        [SerializeField, HideInInspector]
        private float RimLightIntensity = 0.18f;
        [SerializeField, HideInInspector]
        private Vector3 RimLightEuler = new Vector3(28f, 145f, 0f);
        [SerializeField, HideInInspector]
        private float BloomIntensity = 0.65f;
        [SerializeField, HideInInspector]
        private float BloomThreshold = 0.95f;
        [SerializeField, HideInInspector]
        private float BloomScatter = 0.55f;
        [SerializeField, HideInInspector]
        private Color BloomTint = new Color(1f, 0.86f, 0.72f);
        [SerializeField, HideInInspector]
        private float PostExposure = 0.15f;
        [SerializeField, HideInInspector]
        private float ColorContrast = -14f;
        [SerializeField, HideInInspector]
        private float ColorSaturation = -35f;
        [SerializeField, HideInInspector]
        private Color ColorFilterTint = new Color(0.95f, 0.975f, 1f);
        [SerializeField, HideInInspector]
        private float PostVignetteIntensity = 0.12f;
        [SerializeField, HideInInspector]
        private float CameraFarClipM = 320f;
        [Header("Boss telegrafı (T8.1, §11)")]
        [SerializeField, HideInInspector]
        private float TelegraphStretch = 0.28f;
        [SerializeField, HideInInspector]
        private float TelegraphSquash = 0.18f;
        [SerializeField, HideInInspector]
        private float TelegraphSlamSquash = 0.22f;
        [SerializeField, HideInInspector]
        private float TelegraphTonePitchMin = 0.55f;
        [SerializeField, HideInInspector]
        private float TelegraphTonePitchMax = 1.8f;
        [SerializeField, HideInInspector]
        private float TelegraphToneVolumeMin = 0.12f;
        [SerializeField, HideInInspector]
        private float TelegraphToneVolumeMax = 0.40f;
        [SerializeField, HideInInspector]
        private float BossApproachStopPadM = 0.35f;
        [Header("Boss tepki fiziği (T7.2, BossReactor)")]
        [SerializeField, HideInInspector]
        private float BossGravityMps2 = 22f;
        [SerializeField, HideInInspector]
        private float BossRecoilEaseDecayPerSec = 3.2f;
        [SerializeField, HideInInspector]
        private float BossShakeAmpBaseM = 0.12f;
        [SerializeField, HideInInspector]
        private float BossShakeAmpPerKnockbackM = 0.05f;
        [SerializeField, HideInInspector]
        private float BossPinShakeAmpM = 0.04f;
        [SerializeField, HideInInspector]
        private float BossLiftVelocityPerM = 4.5f;
        [Header("Boss karşılaşma")]
        [SerializeField, HideInInspector]
        private string ActiveBossId = "aglarin_kralicesi";
        [Header("Boss animasyon (his turu)")]
        [SerializeField, HideInInspector]
        private float BossTurnRateDegPerSec = 240f;
        [SerializeField, HideInInspector]
        private float BossSlamImpactNorm = 0.42f;
        [SerializeField, HideInInspector]
        private float BossConeImpactNorm = 0.40f;
        [SerializeField, HideInInspector]
        private float BossStaggerMinGapSec = 0.6f;
        [SerializeField, HideInInspector]
        private float BossAnimCrossFadeSec = 0.15f;
        [SerializeField, HideInInspector]
        private float BossWalkClipMps = 1.4f;
        [Header("Boss ölüm pozu (T12, §11)")]
        [SerializeField, HideInInspector]
        private float BossDeathCollapseSec = 0.85f;
        [SerializeField, HideInInspector]
        private float BossDeathSquashY = 0.28f;
        [SerializeField, HideInInspector]
        private float BossDeathSpreadXz = 1.35f;
        [Header("Kamera — omuz üstü savaş")]
        [SerializeField, HideInInspector]
        private float FollowSmoothTimeSec = 0.12f;
        [SerializeField, HideInInspector]
        private float LookAheadM = 0.65f;
        [SerializeField, HideInInspector]
        private Vector3 CameraShoulderOffset = new Vector3(0.42f, 1.12f, -0.28f);
        [SerializeField, HideInInspector]
        private float CameraDistanceM = 5.85f;
        [SerializeField, HideInInspector]
        private float CameraLookHeightM = 0.38f;
        [SerializeField, HideInInspector]
        private float CameraFovDeg = 54f;
        [SerializeField, HideInInspector]
        private float CameraAimDampingSec = 0.12f;
        [SerializeField, HideInInspector]
        private float CameraSoftLockRangeM = 20f;
        [SerializeField, HideInInspector]
        private float CameraSoftLockStrength = 0.58f;
        [SerializeField, HideInInspector]
        private float CameraBossFramingWeight = 0.40f;
        [SerializeField, HideInInspector]
        private float CameraBossAimHeightM = 2.05f;
        [SerializeField, HideInInspector]
        private float CameraDefaultPitchDeg = 21f;
        [SerializeField, HideInInspector]
        private float CameraLockOnMinDistanceM = 5.2f;
        [SerializeField, HideInInspector]
        private float CameraLockOnMaxDistanceM = 8.8f;
        [SerializeField, HideInInspector]
        private float CameraLockOnDistancePerSepM = 0.14f;
        [SerializeField, HideInInspector]
        private float CameraLockOnMaxExtraDistanceM = 3.1f;
        [SerializeField, HideInInspector]
        private float CameraLockOnDistanceSmoothSec = 0.22f;
        [SerializeField, HideInInspector]
        private float CameraWindupDistanceMul = 1.42f;
        [SerializeField, HideInInspector]
        private float CameraWindupExtraHeightM = 0.68f;
        [SerializeField, HideInInspector]
        private float CameraWindupSmoothSec = 0.28f;
        [SerializeField, HideInInspector]
        private float CameraWindupMinRadiusM = 3.5f;
        [SerializeField, HideInInspector]
        private float CameraCollisionSphereRadiusM = 0.25f;
        [SerializeField, HideInInspector]
        private float CameraCollisionMarginM = 0.12f;
        [SerializeField, HideInInspector]
        private float CameraCollisionMinDistanceM = 1.2f;
        [SerializeField, HideInInspector]
        private float CameraCollisionPullInSmoothSec = 0.05f;
        [SerializeField, HideInInspector]
        private float CameraCollisionPullOutSmoothSec = 0.35f;
        [SerializeField, HideInInspector]
        private float CameraLockOnShoulderSideM = 1.1f;
        [SerializeField, HideInInspector]
        private float CameraLockOnShoulderFlipHysteresis = 0.12f;
        [SerializeField, HideInInspector]
        private float CameraLockOnLookBlendToBoss = 0.58f;
        [Header("Kamera orbit")]
        [SerializeField, HideInInspector]
        private float OrbitDegreesPerDp = 0.35f;
        [SerializeField, HideInInspector]
        private float CameraPitchMinDeg = -8f;
        [SerializeField, HideInInspector]
        private float CameraPitchMaxDeg = 35f;
        [Tooltip("Açıkken parmak yukarı = kamera yükselir (aşağı bakar).")]
        [SerializeField, HideInInspector]
        private bool OrbitInvertPitch = false;
        [Header("Soft aim / menzil")]
        [SerializeField, HideInInspector]
        private float SoftAimRangeM = 8f;
        [SerializeField, HideInInspector]
        private float SoftAimConeDeg = 70f;
        [Header("İptal penceresi ipucu (T8, §8/T2)")]
        [SerializeField, HideInInspector]
        private float WindowCueUrgentRatio = 0.30f;
        [SerializeField, HideInInspector]
        private float WindowCuePulseHz = 2f;
        [SerializeField, HideInInspector]
        private float WindowCueUrgentHz = 8f;
        [SerializeField, HideInInspector]
        private float WindowCuePulseAmp = 0.45f;
        [Header("His katmanı (T8.1, CombatFeel)")]
        [SerializeField, HideInInspector]
        private float AfterimageAlpha = 0.55f;
        [SerializeField, HideInInspector]
        private float ImpactFadeSec = 0.04f;
        [SerializeField, HideInInspector]
        private float VignetteHoldSec = 0.85f;
        [SerializeField, HideInInspector]
        private float VignetteFadeSec = 0.45f;
        [SerializeField, HideInInspector]
        private float VignetteAlpha = 0.55f;
        [SerializeField, HideInInspector]
        private float ThreatAlphaMax = 0.35f;
        [SerializeField, HideInInspector]
        private float ThreatPulseHzMin = 4f;
        [SerializeField, HideInInspector]
        private float ThreatPulseHzMax = 14f;
        [SerializeField, HideInInspector]
        private float CameraShakePxToM = 0.01f;
        [Header("Tepki yazısı (T9, ReactionReadout)")]
        [SerializeField, HideInInspector]
        private bool ReadoutAnchorRight = true;
        [SerializeField, HideInInspector]
        private float ReadoutPunchInSec = 0.12f;
        [Header("Can göstergesi (T9, VitalsHud)")]
        [SerializeField, HideInInspector]
        private float VitalsBarWidthDp = 224f;
        [SerializeField, HideInInspector]
        private float VitalsBarHeightDp = 17f;
        [SerializeField, HideInInspector]
        private float VitalsBossBarHeightDp = 24f;
        [SerializeField, HideInInspector]
        private float VitalsBossBarWidthDp = 460f;
        [SerializeField, HideInInspector]
        private float VitalsBarSpacingDp = 6f;
        [SerializeField, HideInInspector]
        private float VitalsMarginDp = 16f;
        [SerializeField, HideInInspector]
        private float VitalsHeaderHeightDp = 18f;
        [SerializeField, HideInInspector]
        private float VitalsPanelPaddingDp = 8f;
        [SerializeField, HideInInspector]
        private Color BossVitalsColor = new Color(0.88f, 0.16f, 0.12f, 0.98f);
        [Header("Status ikon şeridi")]
        [SerializeField, HideInInspector]
        private float StatusIconSizeDp = 28f;
        [SerializeField, HideInInspector]
        private float StatusIconGapDp = 6f;
        [Header("Floating hasar")]
        [SerializeField, HideInInspector]
        private float DamageFloatFontDp = 28f;
        [SerializeField, HideInInspector]
        private float DamageFloatCritFontDp = 36f;
        [SerializeField, HideInInspector]
        private float DamageFloatRisePx = 64f;
        [SerializeField, HideInInspector]
        private float DamageFloatHoldSec = 0.35f;
        [SerializeField, HideInInspector]
        private float DamageFloatFadeSec = 0.45f;
        [SerializeField, HideInInspector]
        private float DamageFloatPunchScale = 1.25f;
        [Header("Toparlanma kilidi HUD (T11.1, §5)")]
        [SerializeField, HideInInspector]
        private float RecoveryLockHeightDp = 10f;
        [SerializeField, HideInInspector]
        private float RecoveryLockGapDp = 8f;
        [Header("Kare süresi göstergesi (T11)")]
        [SerializeField, HideInInspector]
        private bool ShowFrameTimeHud = false;
        [SerializeField, HideInInspector]
        private float FrameTimeSampleSec = 0.5f;
        [SerializeField, HideInInspector]
        private int TargetFrameRateHz = 60;
        [Header("Hasar göstergesi (T12)")]
        [SerializeField, HideInInspector]
        private bool ShowDamageNumbers = true;
        [Header("Skill önizleme (v6 7a, SkillPreviewHud)")]
        [SerializeField, HideInInspector]
        private float SkillPreviewWidthDp = 324f;
        [SerializeField, HideInInspector]
        private float SkillPreviewHeightDp = 72f;
        [SerializeField, HideInInspector]
        private float SkillPreviewGapDp = 10f;
        [SerializeField, HideInInspector]
        private float SkillPreviewHoldSec = 0.9f;
        [Header("Build seçimi (v6 7b, BuildSelectScreen)")]
        [SerializeField, HideInInspector]
        private bool SkipBuildSelectOnStart = false;
        [Header("Debug HUD")]
        [SerializeField, HideInInspector]
        private bool ShowSentenceDebugHud = false;
        [Header("Sanal çubuk")]
        [SerializeField, HideInInspector]
        private float JoystickMaxRadiusDp = 72f;
        [SerializeField, HideInInspector]
        private float JoystickDeadZone = 0.12f;
        [Header("Altıgen (§2)")]
        [SerializeField, HideInInspector]
        private float HudFitShortSideDp = 600f;
        [SerializeField, HideInInspector]
        private float HexagonCenterXNorm = 0.78f;
        [SerializeField, HideInInspector]
        private float HexagonCenterYNorm = 0.30f;
        [SerializeField, HideInInspector]
        private float HexagonRadiusDp = 112f;
        [SerializeField, HideInInspector]
        private float DotHitRadiusDp = 34f;
        [SerializeField, HideInInspector]
        private float CenterHitRadiusDp = 38f;
        [SerializeField, HideInInspector]
        private bool MirrorForLeftHand = false;
        [SerializeField, HideInInspector]
        private float InkLingerSec = 0.40f;
        [SerializeField, HideInInspector]
        private float InkWidthDp = 3.5f;
        [SerializeField, HideInInspector]
        private float InkRawWidthScale = 0.9f;
        [SerializeField, HideInInspector]
        private float InkRawGlow = 1f;
        [Header("Dodge düğmesi (§2, T6.2)")]
        [SerializeField, HideInInspector]
        private float DodgeButtonOffsetXDp = 0f;
        [SerializeField, HideInInspector]
        private float DodgeButtonOffsetYDp = 0f;
        [SerializeField, HideInInspector]
        private float DodgeButtonRadiusDp = 40f;
        [SerializeField, HideInInspector]
        private float DodgeButtonScreenMarginDp = 10f;
        [SerializeField, HideInInspector]
        private float DodgeClearanceDp = 40f;
        [Header("Silah swap düğmesi (v6 swap)")]
        [SerializeField, HideInInspector]
        private float WeaponSwapButtonRadiusDp = 36f;
        [SerializeField, HideInInspector]
        private float WeaponSwapButtonOffsetXDp = 0f;
        [SerializeField, HideInInspector]
        private float WeaponSwapButtonOffsetYDp = 0f;
        [Range(0.4f, 1f), Tooltip("Swap yüzü içindeki silah ikonunun çap oranı.")]
        [SerializeField, HideInInspector]
        private float WeaponSwapIconScale = 0.72f;
        [Header("Lock-on düğmesi (mobil)")]
        [SerializeField, HideInInspector]
        private float LockOnButtonRadiusDp = 36f;
        [SerializeField, HideInInspector]
        private float LockOnButtonOffsetXDp = 0f;
        [SerializeField, HideInInspector]
        private float LockOnButtonOffsetYDp = 52f;
        [SerializeField, HideInInspector]
        private Color LockOnButtonColor = new Color(0.55f, 0.72f, 0.88f, 0.9f);
        [SerializeField, HideInInspector]
        private Color LockOnButtonActiveColor = new Color(0.95f, 0.82f, 0.45f, 0.95f);
        [Header("Element radial (v6.1.1)")]
        [SerializeField, HideInInspector]
        private float ElementMenuRadiusDp = 92f;
        [SerializeField, HideInInspector]
        private float ElementMenuChipWidthDp = 144f;
        [SerializeField, HideInInspector]
        private float ElementMenuChipHeightDp = 48f;
        [SerializeField, HideInInspector]
        private float ElementMenuItemWidthDp = 84f;
        [SerializeField, HideInInspector]
        private float ElementMenuItemHeightDp = 48f;
        [SerializeField, HideInInspector]
        private float ElementMenuAnchorXNorm = 0.86f;
        [SerializeField, HideInInspector]
        private float ElementMenuAnchorYNorm = 0.72f;
        [Header("Düz vuruş (§5, T6.2)")]
        [SerializeField, HideInInspector]
        private int BasicStrikeDot = 1;
        [Header("Altıgen ikon")]
        [SerializeField, HideInInspector]
        private float IconDisplayScale = 1.0f;
        [Tooltip("Altıgeni gruplayan cam tepsinin rune halkasına ek boşluğu.")]
        [SerializeField, HideInInspector]
        private float CombatTrayPaddingDp = 22f;
        [SerializeField, HideInInspector]
        private float CombatTrayHeaderHeightDp = 22f;
        [SerializeField, HideInInspector]
        private float CombatTrayCornerRadiusDp = 20f;
        [SerializeField, HideInInspector]
        private float CombatTrayLinkWidthDp = 1.5f;
        [Header("Dokunsal (§2)")]
        [SerializeField, HideInInspector]
        private long DotVibrationMs = 30;
        [Header("Karakter görsel boyu (metre)")]
        [SerializeField, HideInInspector]
        private float PlayerVisualHeightM = 1.78f;
        [SerializeField, HideInInspector]
        private float BossVisualHeightM = 5.0f;
        [SerializeField, HideInInspector]
        private float CharacterAnimSpeed = 1.0f;
        [Header("Oyuncu")]
        [SerializeField, HideInInspector]
        private float WalkSpeedMps = 6.4f;
        [SerializeField, HideInInspector]
        private float MoveAccelMps2 = 52f;
        [SerializeField, HideInInspector]
        private float MoveDecelMps2 = 64f;
        [SerializeField, HideInInspector]
        private float TurnRateDegPerSec = 720f;
        [SerializeField, HideInInspector]
        private float MinStickSpeedFrac = 0.42f;
        [SerializeField, HideInInspector]
        private float AnimSpeedDampSec = 0.08f;
        [SerializeField, HideInInspector]
        private float LocoMaxPlaybackMult = 1.5f;
        [SerializeField, HideInInspector]
        private float AnimCrossFadeSec = 0.06f;
        [SerializeField, HideInInspector]
        private float BasicStrikeComboResetSec = 1.2f;
        [SerializeField, HideInInspector]
        private float UpperBodyCastMinSpeed = 0.15f;
        [SerializeField, HideInInspector]
        private float FootstepStrideM = 2.2f;
        [SerializeField, HideInInspector]
        private float BossFootstepStrideM = 2.4f;
        [Header("Oyuncu can (T8)")]
        [SerializeField, HideInInspector]
        private int PlayerMaxHp = 22;
        [Header("Dodge kayma kuyruğu (T8.1, §6)")]
        [SerializeField, HideInInspector]
        private float DodgeGlideSpeedMps = 3.5f;
        [Header("Renk dili (§10)")]
        [SerializeField, HideInInspector]
        private Color PlayerColor = new Color(0.373f, 0.941f, 1f);
        [SerializeField, HideInInspector]
        private Color BossColor = new Color(0.18f, 0.19f, 0.22f);
        [SerializeField, HideInInspector]
        private Color GroundColor = new Color(0.44f, 0.46f, 0.49f);
        [SerializeField, HideInInspector]
        private Color BackgroundColor = new Color(0.69f, 0.718f, 0.737f);
        [SerializeField, HideInInspector]
        private Color InkPurple = new Color(0.725f, 0.549f, 1f);   // #B98CFF
        [SerializeField, HideInInspector]
        private Color InkCyan = new Color(0.373f, 0.941f, 1f);     // #5FF0FF
        [SerializeField, HideInInspector]
        private Color AcidGreen = new Color(0.608f, 0.910f, 0.235f); // #9BE83C — §10 zehir birikintisi
        [Header("Element renkleri (çizgi/tezahür)")]
        [SerializeField, HideInInspector]
        private Color ElementFire = new Color(1f, 0.42f, 0.62f);       // Ateş — sıcak magenta
        [SerializeField, HideInInspector]
        private Color ElementWater = new Color(0.28f, 0.72f, 1f);      // Su
        [SerializeField, HideInInspector]
        private Color ElementAir = new Color(0.50f, 0.70f, 0.62f);     // Hava — muted teal (prezentasyon)
        [SerializeField, HideInInspector]
        private Color ElementEarth = new Color(0.62f, 0.78f, 0.42f);   // Toprak
        [SerializeField, HideInInspector]
        private Color ElementLight = new Color(1f, 0.96f, 0.82f);      // Aydınlık
        [SerializeField, HideInInspector]
        private Color ElementDark = new Color(0.48f, 0.28f, 0.78f);    // Karanlık
        [Header("Kenney parçacık dokuları (Resources/Vfx/Kenney, uzantı yok)")]
        [SerializeField, HideInInspector]
        private string VfxTexFire = "flame_02";
        [SerializeField, HideInInspector]
        private string VfxTexWater = "circle_03";
        [SerializeField, HideInInspector]
        private string VfxTexAir = "twirl_01";
        [SerializeField, HideInInspector]
        private string VfxTexEarth = "dirt_01";
        [SerializeField, HideInInspector]
        private string VfxTexLight = "star_04";
        [SerializeField, HideInInspector]
        private string VfxTexDark = "magic_04";
        [SerializeField, HideInInspector]
        private string VfxTexHit = "spark_05";
        [SerializeField, HideInInspector]
        private string VfxTexInk = "light_01";
        [SerializeField, HideInInspector]
        private Color HexagonDotColor = new Color(0.55f, 0.62f, 0.72f, 0.85f);
        [SerializeField, HideInInspector]
        private Color HexagonCenterColor = new Color(0.373f, 0.941f, 1f, 0.9f);
        [SerializeField, HideInInspector]
        private Color DodgeButtonColor = new Color(0.725f, 0.549f, 1f, 0.9f);
        [SerializeField, HideInInspector]
        private Color TelegraphHot = new Color(1f, 0.302f, 0.141f);   // #FF4D24
        [SerializeField, HideInInspector]
        private Color TelegraphWarm = new Color(1f, 0.604f, 0.235f);  // #FF9A3C
        [Header("Tezahür çizgisi (T7.2, LivingEffectView)")]
        [SerializeField, HideInInspector]
        private float EffectLineWidthDefaultM = 0.28f;
        [SerializeField, HideInInspector]
        private float EffectLineWidthWideM = 0.55f;
        [SerializeField, HideInInspector]
        private float EffectLineWidthNarrowM = 0.16f;
        [SerializeField, HideInInspector]
        private float EffectSarsintiWidthWideM = 0.22f;
        [SerializeField, HideInInspector]
        private float EffectSarsintiWidthNarrowM = 0.1f;
        [Header("Tezahür silüet eşikleri (T7.2, LivingEffectView)")]
        [SerializeField, HideInInspector]
        private float EffectShowMinFocus = 0.2f;
        [SerializeField, HideInInspector]
        private float EffectIgneShowMinSpread = 0.2f;
        [SerializeField, HideInInspector]
        private float EffectFocusRingMax = 0.35f;
        [SerializeField, HideInInspector]
        private float EffectFocusArcMax = 0.75f;
        [SerializeField, HideInInspector]
        private float EffectPierceNeedleShowMin = 0.45f;
        [SerializeField, HideInInspector]
        private float EffectFocusSwarmAlongLineMin = 0.45f;
        [Header("Tezahür şekil ölçekleri (T7.2, LivingEffectView)")]
        [SerializeField, HideInInspector]
        private float EffectBlobScaleBaseM = 0.38f;
        [SerializeField, HideInInspector]
        private float EffectBlobScalePerSpreadM = 0.18f;
        [SerializeField, HideInInspector]
        private float EffectNeedleThickWideM = 0.35f;
        [SerializeField, HideInInspector]
        private float EffectNeedleThickNarrowM = 0.14f;
        [SerializeField, HideInInspector]
        private float EffectNeedleLenBaseM = 0.7f;
        [SerializeField, HideInInspector]
        private float EffectNeedleLenPerPierceM = 0.5f;
        [Header("Tezahür hareket (T14, LivingEffectView)")]
        [SerializeField, HideInInspector]
        private float EffectNeedleWindupLenMul = 1.55f;
        [SerializeField, HideInInspector]
        private float EffectNeedleArrivalLenMul = 0.72f;
        [SerializeField, HideInInspector]
        private float EffectNeedleAfterimageAlpha = 0.35f;
        [SerializeField, HideInInspector]
        private float EffectSwarmJitterM = 0.65f;
        [SerializeField, HideInInspector]
        private float EffectSwarmMinBlobs = 4f;
        [SerializeField, HideInInspector]
        private float EffectSarsintiGroundY = 0.02f;
        [SerializeField, HideInInspector]
        private float EffectSarsintiMassWidthMul = 1.35f;
        [SerializeField, HideInInspector]
        private float EffectBasicStrikeLenM = 0.9f;
        [SerializeField, HideInInspector]
        private float EffectBasicStrikeThickM = 0.16f;
        [SerializeField, HideInInspector]
        private float EffectBasicStrikeHeightM = 0.55f;
        [Header("Aktör poz (T7.2, ActorPose)")]
        [SerializeField, HideInInspector]
        private float ActorPoseDurationMs = 180f;
        [SerializeField, HideInInspector]
        private Vector3 PoseIgne = new Vector3(0.78f, 0.88f, 1.35f);
        [SerializeField, HideInInspector]
        private Vector3 PoseSuru = new Vector3(1.35f, 0.9f, 1.1f);
        [SerializeField, HideInInspector]
        private Vector3 PoseSarsinti = new Vector3(1.2f, 0.55f, 1.2f);
        [SerializeField, HideInInspector]
        private Vector3 PoseKabuk = new Vector3(1.15f, 1.05f, 1.15f);
        [SerializeField, HideInInspector]
        private Vector3 PoseZehir = new Vector3(1.05f, 0.95f, 1.25f);
        [Header("Kalıcı iz tavanı (T7.2, GroundScarField)")]
        [SerializeField, HideInInspector]
        private int GroundScarCapCount = 60;
    }
}
