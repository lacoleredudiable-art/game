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
    /// Oyun kabuğu ayarları (renk, arena, girdi, HUD). Spec'te yürüme hızı yok; varsayılanlar durum.md'de kayıtlı.
    /// Sahnedeki tek örnek <see cref="Composition.GameBootstrap"/> üzerinden paylaşılır; kopya tutulmaz (canlı ayar paneli bunu bekler).
    /// Eski Unity serileştirme tip adı kaldırıldı (2B.12); script GUID aynı, alan adları aynı.
    /// </summary>
    [System.Serializable]
    public sealed partial class GameTuning
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
        /// <summary>Çekirdek rün → tezahür çizgi rengi.</summary>
        public Color ColorForRune(Dovus.Core.Element.Rune rune) => rune switch
        {
            Dovus.Core.Element.Rune.Ates => Visuals.ElementFire,
            Dovus.Core.Element.Rune.Su => Visuals.ElementWater,
            Dovus.Core.Element.Rune.Hava => Visuals.ElementAir,
            Dovus.Core.Element.Rune.Toprak => Visuals.ElementEarth,
            Dovus.Core.Element.Rune.Aydinlik => Visuals.ElementLight,
            Dovus.Core.Element.Rune.Karanlik => Visuals.ElementDark,
            _ => Visuals.InkCyan
        };

        public bool IsDotOpen(int dot) => true;

        /// <summary>
        /// T10: `GameTuning`'in tamamı (renkler, altıgen konumu, arena...) ayar paneline
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
        public void ResetPanelFields() => ApplyPanelFields(new GameTuning().ToPanelFields());
    }
}
