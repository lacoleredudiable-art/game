using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// T5 prototip kabuğu ayarları. Spec'te yürüme hızı yok; varsayılanlar durum.md'de kayıtlı.
    /// Sahnedeki tek örnek Bootstrap'ten paylaşılır; kopya tutulmaz (T10 canlı ayarı bunu bekliyor).
    /// </summary>
    [System.Serializable]
    public sealed class PrototypeTuning
    {
        [Header("Arena")]
        /// <summary>Daire salon yarıçapı (çap = 2×). Eski kare yarım-kenar adı korundu.</summary>
        public float ArenaHalfSizeM = 50f;
        /// <summary>Çevre duvar yüksekliği (tavansız salon).</summary>
        public float ArenaWallHeightM = 18f;
        /// <summary>Çevre duvar kalınlığı.</summary>
        public float ArenaWallThicknessM = 1.4f;
        /// <summary>Quaternius arena prefab ölçeği — daire salonda 1.</summary>
        public float ArenaVisualScale = 1.0f;

        [Header("Karakter görsel ölçek (Synty)")]
        /// <summary>Player/Ally mesh ölçeği — önceki 1.75’in %50 büyüğü.</summary>
        public float PlayerVisualScale = 2.625f;
        /// <summary>Boss mesh ölçeği — önceki 2.2’nin %50 büyüğü.</summary>
        public float BossVisualScale = 3.3f;
        /// <summary>Animator hızı — locomotion 1; idle freeze blend tree’de.</summary>
        public float CharacterAnimSpeed = 1.0f;

        [Header("Oyuncu")]
        public float WalkSpeedMps = 7.5f;
        public float MoveAccelMps2 = 40f;
        public float MoveDecelMps2 = 50f;
        public float TurnRateDegPerSec = 720f;
        /// <summary>Çubuk ölü bölgenin hemen üstündeyken hız oranı (yürüme); tam çubuk = 1 (koşu). Önerilen.</summary>
        public float MinStickSpeedFrac = 0.4f;
        /// <summary>Animator Speed parametresi sönümü. Önerilen.</summary>
        public float AnimSpeedDampSec = 0.08f;
        /// <summary>Aksiyon state'ine giriş crossfade süresi (eskisi sert kesim). Önerilen.</summary>
        public float AnimCrossFadeSec = 0.06f;

        // Spec §11 hasar 22; oyuncu tavanı belgede yok. Bir çakma = ölüm — respawn ≤2 sn
        // (§11) döngüsü böyle denenebiliyor. T11 his turunda ayarlanacak.
        [Header("Oyuncu can (T8)")]
        public int PlayerMaxHp = 22;

        [Header("Sanal çubuk")]
        public float JoystickMaxRadiusDp = 72f;
        public float JoystickDeadZone = 0.12f;

        // Altıgen ekrana sabit (§2). Yarıçap/konum spec'te sayı yok — varsayılan; durum.md'ye geçildi.
        // T6.2: merkez X, dodge düğmesi sağ kenardan taşmasın diye 0.78'den içeri alındı.
        [Header("Altıgen (§2)")]
        // Biraz içeri: sağda dodge + çizim boşluğu kalsın.
        public float HexagonCenterXNorm = 0.68f;
        // Sağ-alt; FittedRadiusPx alt rünleri safe içinde tutar.
        public float HexagonCenterYNorm = 0.32f;
        // Komşu rün kenar boşluğu ≈ radius − 2·dotR (≥40dp çizim koridoru).
        public float HexagonRadiusDp = 98f;
        public float DotHitRadiusDp = 26f;
        public float CenterHitRadiusDp = 28f;
        public bool MirrorForLeftHand = false;
        public float InkLingerSec = 0.40f;
        public float InkWidthDp = 3.5f;

        // §2: dodge hex dışı (sağ-alt); offset ek kaydırma (varsayılan 0).
        [Header("Dodge düğmesi (§2, T6.2)")]
        public float DodgeButtonOffsetXDp = 0f;
        public float DodgeButtonOffsetYDp = 0f;
        public float DodgeButtonRadiusDp = 32f;
        public float DodgeButtonScreenMarginDp = 10f;
        /// <summary>Hex kenarı ile dodge yüzeyi arası (dp).</summary>
        public float DodgeClearanceDp = 40f;

        // weapon_skill_interaction.swap: dodge'un altıgene göre simetriği (sol-alt).
        [Header("Silah swap düğmesi (v6 swap)")]
        public float WeaponSwapButtonRadiusDp = 28f;
        public float WeaponSwapButtonOffsetXDp = 0f;
        public float WeaponSwapButtonOffsetYDp = 0f;

        [Header("Element radial (v6.1.1)")]
        /// <summary>JSON yalnız 6 konum/300 ms verir; ekran yarıçapı için prototip varsayılanı.</summary>
        public float ElementMenuRadiusDp = 92f;
        public float ElementMenuChipWidthDp = 126f;
        public float ElementMenuChipHeightDp = 44f;

        // §5: merkez bir kelime değil düğme; hangi fiille vurduğu veridir (prototipte 1/Ateş).
        [Header("Düz vuruş (§5, T6.2)")]
        public int BasicStrikeDot = 1;

        // Altı element rünü — hepsi açık (element-sistemi.json).
        [Header("Açık rünler (§4)")]
        public bool OpenDot1 = true;
        public bool OpenDot2 = true;
        public bool OpenDot3 = true;
        public bool OpenDot4 = true;
        public bool OpenDot5 = true;
        public bool OpenDot6 = true;

        [Header("Altıgen ikon")]
        // 1.0 — görsel disk hit'ten şişmesin, komşu boşluğu yemesin.
        public float IconDisplayScale = 1.0f;

        // Spec'te sayı yok — ayrık onay tıkırtısı (§2); Handheld.Vibrate ~500 ms üst üste biniyordu.
        [Header("Dokunsal (§2)")]
        public long DotVibrationMs = 30;

        [Header("Kamera")]
        public float FollowSmoothTimeSec = 0.18f;
        public float LookAheadM = 1.4f;
        public Vector3 CameraOffset = new Vector3(0f, 7f, -6f);

        // Mevcut prototip paleti: nötr boss gövdesi, sıcak telegraf ve element renkleri.
        [Header("Renk dili (§10)")]
        public Color PlayerColor = new Color(0.373f, 0.941f, 1f);
        public Color BossColor = new Color(0.18f, 0.19f, 0.22f);
        public Color GroundColor = new Color(0.38f, 0.4f, 0.44f);
        public Color BackgroundColor = new Color(0.14f, 0.13f, 0.125f);
        public Color InkPurple = new Color(0.725f, 0.549f, 1f);   // #B98CFF
        public Color InkCyan = new Color(0.373f, 0.941f, 1f);     // #5FF0FF
        public Color AcidGreen = new Color(0.608f, 0.910f, 0.235f); // #9BE83C — §10 zehir birikintisi

        // Altı çekirdek için mevcut prototip renkleri. Ateş sıcak magenta.
        [Header("Element renkleri (çizgi/tezahür)")]
        public Color ElementFire = new Color(1f, 0.42f, 0.62f);       // Ateş — sıcak magenta
        public Color ElementWater = new Color(0.28f, 0.72f, 1f);      // Su
        public Color ElementAir = new Color(0.50f, 0.70f, 0.62f);     // Hava — muted teal (prezentasyon)
        public Color ElementEarth = new Color(0.62f, 0.78f, 0.42f);   // Toprak
        public Color ElementLight = new Color(1f, 0.96f, 0.82f);      // Aydınlık
        public Color ElementDark = new Color(0.48f, 0.28f, 0.78f);    // Karanlık

        public Color HexagonDotColor = new Color(0.55f, 0.62f, 0.72f, 0.85f);
        // T6.2: merkez artık "vur" demek — oyuncu rengine çekildi (§10 camgöbeği).
        public Color HexagonCenterColor = new Color(0.373f, 0.941f, 1f, 0.9f);
        // Dodge diski mevcut mor oyuncu vurgusunu kullanır.
        public Color DodgeButtonColor = new Color(0.725f, 0.549f, 1f, 0.9f);
        // Boss telegrafının mevcut sıcak renkleri.
        public Color TelegraphHot = new Color(1f, 0.302f, 0.141f);   // #FF4D24
        public Color TelegraphWarm = new Color(1f, 0.604f, 0.235f);  // #FF9A3C

        // §8/T2 bedava kazanç: iptal penceresi dalganın yerinden okunur. Spec sayı vermiyor.
        [Header("İptal penceresi ipucu (T8, §8/T2)")]
        public float WindowCueUrgentRatio = 0.30f;
        public float WindowCuePulseHz = 2f;
        public float WindowCueUrgentHz = 8f;
        public float WindowCuePulseAmp = 0.45f;

        // §6 "sönen artık hız". Spec büyüklük vermiyor; T8'de ana hareketin ORTALAMA hızı
        // (3.8/0.26 = 14.6 m/s) kullanılıyordu — eğri u=1'de hızı sıfıra indirdiği için bu
        // ikinci bir atılım gibi okunuyordu. Yürüme hızı mertebesi seçildi (T8.1).
        [Header("Dodge kayma kuyruğu (T8.1, §6)")]
        public float DodgeGlideSpeedMps = 3.5f;

        // Telegraf silüeti: sayı değil poz. Spec §11 pozu tarif ediyor, oran vermiyor.
        [Header("Boss telegrafı (T8.1, §11)")]
        public float TelegraphStretch = 0.28f;
        public float TelegraphSquash = 0.18f;
        public float TelegraphSlamSquash = 0.22f;
        public float TelegraphTonePitchMin = 0.55f;
        public float TelegraphTonePitchMax = 1.8f;
        public float TelegraphToneVolumeMin = 0.12f;
        public float TelegraphToneVolumeMax = 0.40f;
        // Bossun oyuncuya yaklaşırken bıraktığı boşluk. Etki yarıçapı (5.4 m) ile birlikte
        // hangi derecelerin erişilebilir olduğunu BU sayı belirliyor — bkz. durum.md T8.1.
        public float BossApproachStopPadM = 0.35f;

        [Header("His katmanı (T8.1, CombatFeel)")]
        public float AfterimageAlpha = 0.55f;
        public float ImpactFadeSec = 0.04f;
        public float VignetteHoldSec = 0.85f;
        public float VignetteFadeSec = 0.45f;
        public float VignetteAlpha = 0.55f;
        public float ThreatAlphaMax = 0.35f;
        public float ThreatPulseHzMin = 4f;
        public float ThreatPulseHzMax = 14f;
        // §8 sarsıntı PİKSEL veriyor, kamera METRE ile sarsılıyor. Dönüşüm spec'te yok.
        public float CameraShakePxToM = 0.01f;

        // T7.2: LivingEffectView'a gömülü his sayıları (AGENTS kural 3). Değerler T7'den
        // AYNI taşındı, yalnızca yeri değişti — dovus-sistemi.md'de sayı yok, sapma T7
        // durum.md'sinde kayıtlı.
        [Header("Tezahür çizgisi (T7.2, LivingEffectView)")]
        public float EffectLineWidthDefaultM = 0.28f;
        public float EffectLineWidthWideM = 0.55f;
        public float EffectLineWidthNarrowM = 0.16f;
        public float EffectSarsintiWidthWideM = 0.22f;
        public float EffectSarsintiWidthNarrowM = 0.1f;

        [Header("Tezahür silüet eşikleri (T7.2, LivingEffectView)")]
        public float EffectShowMinFocus = 0.2f;
        public float EffectIgneShowMinSpread = 0.2f;
        public float EffectFocusRingMax = 0.35f;
        public float EffectFocusArcMax = 0.75f;
        public float EffectPierceNeedleShowMin = 0.45f;
        public float EffectFocusSwarmAlongLineMin = 0.45f;

        [Header("Tezahür şekil ölçekleri (T7.2, LivingEffectView)")]
        public float EffectBlobScaleBaseM = 0.38f;
        public float EffectBlobScalePerSpreadM = 0.18f;
        public float EffectNeedleThickWideM = 0.35f;
        public float EffectNeedleThickNarrowM = 0.14f;
        public float EffectNeedleLenBaseM = 0.7f;
        public float EffectNeedleLenPerPierceM = 0.5f;

        // T14 — hareket karakteri görsel ölçüleri (spec yok; durum.md T14 sapmaları).
        [Header("Tezahür hareket (T14, LivingEffectView)")]
        public float EffectNeedleWindupLenMul = 1.55f;
        public float EffectNeedleArrivalLenMul = 0.72f;
        public float EffectNeedleAfterimageAlpha = 0.35f;
        public float EffectSwarmJitterM = 0.65f;
        public float EffectSwarmMinBlobs = 4f;
        public float EffectSarsintiGroundY = 0.02f;
        public float EffectSarsintiMassWidthMul = 1.35f;
        public float EffectBasicStrikeLenM = 0.9f;
        public float EffectBasicStrikeThickM = 0.16f;
        public float EffectBasicStrikeHeightM = 0.55f;

        // Rün başına squash/stretch + poz süresi (T1/T5) — değer aynı, yeri ActorPose'dan taşındı.
        [Header("Aktör poz (T7.2, ActorPose)")]
        public float ActorPoseDurationMs = 180f;
        public Vector3 PoseIgne = new Vector3(0.78f, 0.88f, 1.35f);
        public Vector3 PoseSuru = new Vector3(1.35f, 0.9f, 1.1f);
        public Vector3 PoseSarsinti = new Vector3(1.2f, 0.55f, 1.2f);
        public Vector3 PoseKabuk = new Vector3(1.15f, 1.05f, 1.15f);
        public Vector3 PoseZehir = new Vector3(1.05f, 0.95f, 1.25f);

        // Değer aynı, yeri BossReactor'dan taşındı.
        [Header("Boss tepki fiziği (T7.2, BossReactor)")]
        public float BossGravityMps2 = 22f;
        public float BossRecoilEaseDecayPerSec = 3.2f;
        public float BossShakeAmpBaseM = 0.12f;
        public float BossShakeAmpPerKnockbackM = 0.05f;
        public float BossPinShakeAmpM = 0.04f;
        public float BossLiftVelocityPerM = 4.5f;

        // Boss animasyon sunumu (his turu Faz 1). Hepsi önerilen — klip değişince göz kontrolü.
        [Header("Boss animasyon (his turu)")]
        public float BossTurnRateDegPerSec = 240f;
        /// <summary>Saldırı klibinde darbe karesinin normalize zamanı; windup sonuna hizalanır.</summary>
        public float BossSlamImpactNorm = 0.42f;
        public float BossConeImpactNorm = 0.40f;
        public float BossStaggerMinGapSec = 0.6f;
        public float BossAnimCrossFadeSec = 0.15f;
        /// <summary>Yürüme klibinin kök hızı bulunamazsa (in-place klip) ölçek-1 adım hızı.</summary>
        public float BossWalkClipMps = 1.4f;

        // Boss ölümündeki çökme pozu; süre mevcut his varsayılanıdır.
        [Header("Boss ölüm pozu (T12, §11)")]
        public float BossDeathCollapseSec = 0.85f;
        public float BossDeathSquashY = 0.28f;
        public float BossDeathSpreadXz = 1.35f;

        // Spec'te tavan sayısı yok (uydurma) — T11 kare bütçesi için icat edildi; gerekçe
        // docs/durum.md T7.2 sapmalarına yazıldı. İz kalıcıdır (§8/T4), süreye bağlı silinmez;
        // tavan dolunca en eski iz DÖNÜŞTÜRÜLÜR (yok edilip yeniden yaratılmaz).
        [Header("Kalıcı iz tavanı (T7.2, GroundScarField)")]
        public int GroundScarCapCount = 60;

        // §6 gösterim: "sağ kenarda, parlak, büyük punto" + "hangi kenarda duracağı ayarlanabilir".
        // Punto/glow/bekleme/sönme FeelTuning.Readout* alanlarında (T1'de spec'ten kondu, T9
        // burada gerçekten kullanılıyor). Giriş vuruşunun (scale punch) sönme süresi spec'te
        // yok — uydurma, durum.md'ye T9 sapması olarak geçildi.
        [Header("Tepki yazısı (T9, ReactionReadout)")]
        public bool ReadoutAnchorRight = true;
        public float ReadoutPunchInSec = 0.12f;

        // §6/§11 "boss ve oyuncu can göstergesi, sade". Ölçüler dp (altıgen/dodge diskiyle
        // aynı yol: HexagonLayoutScreen.DpToPixels). Boss barı T12'den beri BossVitals okur.
        [Header("Can göstergesi (T9, VitalsHud)")]
        public float VitalsBarWidthDp = 168f;
        public float VitalsBarHeightDp = 11f;
        public float VitalsBossBarHeightDp = 12f;
        public float VitalsBossBarWidthDp = 280f;
        public float VitalsBarSpacingDp = 5f;
        public float VitalsMarginDp = 14f;
        public Color BossVitalsColor = new Color(0.78f, 0.22f, 0.18f, 0.98f);

        [Header("Status ikon şeridi")]
        public float StatusIconSizeDp = 28f;
        public float StatusIconGapDp = 6f;

        [Header("Kamera orbit")]
        public float OrbitDegreesPerDp = 0.35f;

        [Header("Soft aim / menzil")]
        public float SoftAimRangeM = 8f;
        // Yarım açı: boss bakış yönünün bu kadar dışındaysa kilit yok (arkası dönük vurmaz).
        public float SoftAimConeDeg = 70f;

        [Header("Floating hasar")]
        public float DamageFloatFontDp = 28f;
        public float DamageFloatCritFontDp = 36f;
        public float DamageFloatRisePx = 64f;
        public float DamageFloatHoldSec = 0.35f;
        public float DamageFloatFadeSec = 0.45f;
        public float DamageFloatPunchScale = 1.25f;

        // T11.1: toparlanma kilidi kalıcı HUD (§5 beceri ekseni). Yükseklik/gap spec'te yok —
        // can barıyla aynı dilde dp; gerekçe docs/durum.md T11.1 sapmaları.
        [Header("Toparlanma kilidi HUD (T11.1, §5)")]
        public float RecoveryLockHeightDp = 10f;
        public float RecoveryLockGapDp = 8f;

        // T11 ölçüm turu. Hedef 60 fps görev metninden; örnekleme penceresi spec'te yok —
        // uydurma. Pencere hem yazının tazelenme aralığı hem de "en kötü kare"nin arandığı
        // aralık: kısalırsa yazı titrer, uzarsa takılma gözden kaçar.
        [Header("Kare süresi göstergesi (T11)")]
        public bool ShowFrameTimeHud = false;
        public float FrameTimeSampleSec = 0.5f;
        public int TargetFrameRateHz = 60;

        // T12: kapanış hasarı ölçüm aracı (§5 / §12). Varsayılan KAPALI — his kanalı değil
        // kumpas. Eski tuning.json'da alan yoksa JsonUtility false verir (= güvenli).
        [Header("Hasar göstergesi (T12)")]
        public bool ShowDamageNumbers = true;

        // v6 7a: önizleme altıgenin hemen üstünde durur, ekran ortasını kapatmaz. Genişlik/boşluk
        // spec'te yok (durum.md); bekleme FeelTuning.ReadoutHoldMs (900) varsayılanından.
        [Header("Skill önizleme (v6 7a, SkillPreviewHud)")]
        public float SkillPreviewWidthDp = 240f;
        public float SkillPreviewGapDp = 10f;
        public float SkillPreviewHoldSec = 0.9f;

        [Header("Build seçimi (v6 7b, BuildSelectScreen)")]
        public bool SkipBuildSelectOnStart = false;

        [Header("Debug HUD")]
        public bool ShowSentenceDebugHud = false;

        // Sahneye serileşmiş eski kopyada yeni alanlar 0/siyah gelir (C# initializer
        // deserialize'da uygulanmaz). Sürüm numarası da 0 geldiği için tek seferlik yama
        // ÇALIŞIR; sahne bir kez yeniden kaydedildikten sonra bu blok hiç girmez ve
        // tasarımcının bilinçli 0'ı (ör. nabzı kapatmak) artık ezilmez (T8.1).
        [HideInInspector] public int TuningVersion = CurrentVersion;

        const int CurrentVersion = 15;

        /// <summary>Sürümü geçmiş serileşmiş kopyayı bu sürümün varsayılanlarına çeker.</summary>
        public void EnsureRuntimeDefaults()
        {
            if (TuningVersion < CurrentVersion)
                MigrateToCurrent();

            // Prototip: altı çekirdek her zaman açık — eski sahne false'larını her açılışta ezer.
            OpenDot1 = true;
            OpenDot2 = true;
            OpenDot3 = true;
            OpenDot4 = true;
            OpenDot5 = true;
            OpenDot6 = true;

            BasicStrikeDot = 1;
            ShowDamageNumbers = true;
            ElementAir = new Color(0.50f, 0.70f, 0.62f);
            ShowSentenceDebugHud = false;
            if (SoftAimRangeM <= 0.01f) SoftAimRangeM = 8f;
            if (SoftAimConeDeg <= 0.01f) SoftAimConeDeg = 70f;
            if (OrbitDegreesPerDp <= 0.01f) OrbitDegreesPerDp = 0.35f;
            if (StatusIconSizeDp <= 0.01f) StatusIconSizeDp = 28f;
            if (VitalsBossBarWidthDp <= 0.01f) VitalsBossBarWidthDp = 280f;
            if (VitalsBossBarHeightDp <= 0.01f) VitalsBossBarHeightDp = 12f;
            if (DamageFloatFontDp <= 0.01f) DamageFloatFontDp = 28f;
            if (HexagonRadiusDp <= 0.01f) HexagonRadiusDp = 98f;
            if (SkillPreviewWidthDp <= 0.01f) SkillPreviewWidthDp = 240f;
            if (SkillPreviewGapDp <= 0.01f) SkillPreviewGapDp = 10f;
            if (SkillPreviewHoldSec <= 0.01f) SkillPreviewHoldSec = 0.9f;
            if (DotHitRadiusDp <= 0.01f) DotHitRadiusDp = 26f;
            if (CenterHitRadiusDp <= 0.01f) CenterHitRadiusDp = 28f;
            if (DodgeButtonRadiusDp <= 0.01f) DodgeButtonRadiusDp = 32f;
            if (WeaponSwapButtonRadiusDp <= 0.01f) WeaponSwapButtonRadiusDp = 28f;
            if (DodgeClearanceDp <= 0.01f) DodgeClearanceDp = 40f;
            if (IconDisplayScale <= 0.01f) IconDisplayScale = 1.0f;
            if (PlayerVisualScale <= 0.01f) PlayerVisualScale = 2.625f;
            if (BossVisualScale <= 0.01f) BossVisualScale = 3.3f;
            if (CharacterAnimSpeed <= 0.01f) CharacterAnimSpeed = 1.0f;
            if (WalkSpeedMps <= 0.01f) WalkSpeedMps = 7.5f;
            if (MoveAccelMps2 <= 0.01f) MoveAccelMps2 = 40f;
            if (MoveDecelMps2 <= 0.01f) MoveDecelMps2 = 50f;
            if (TurnRateDegPerSec <= 0.01f) TurnRateDegPerSec = 720f;
            if (MinStickSpeedFrac <= 0.01f) MinStickSpeedFrac = 0.4f;
            if (AnimSpeedDampSec <= 0f) AnimSpeedDampSec = 0.08f;
            if (AnimCrossFadeSec <= 0f) AnimCrossFadeSec = 0.06f;
            if (BossTurnRateDegPerSec <= 0.01f) BossTurnRateDegPerSec = 240f;
            if (BossSlamImpactNorm <= 0.01f) BossSlamImpactNorm = 0.42f;
            if (BossConeImpactNorm <= 0.01f) BossConeImpactNorm = 0.40f;
            if (BossStaggerMinGapSec <= 0f) BossStaggerMinGapSec = 0.6f;
            if (BossAnimCrossFadeSec <= 0f) BossAnimCrossFadeSec = 0.15f;
            if (BossWalkClipMps <= 0.01f) BossWalkClipMps = 1.4f;
            if (ArenaHalfSizeM <= 0.01f) ArenaHalfSizeM = 50f;
            if (ArenaWallHeightM <= 0.01f) ArenaWallHeightM = 18f;
            if (ArenaWallThicknessM <= 0.01f) ArenaWallThicknessM = 1.4f;
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

            // v14: 100 m çap daire salon + %50 karakter + ağır hareket.
            // v15: walk snappy 5.5; anim 1.0; idle fidget freeze (controller).
            ArenaHalfSizeM = fresh.ArenaHalfSizeM;
            ArenaWallHeightM = fresh.ArenaWallHeightM;
            ArenaWallThicknessM = fresh.ArenaWallThicknessM;
            ArenaVisualScale = fresh.ArenaVisualScale;
            PlayerVisualScale = fresh.PlayerVisualScale;
            BossVisualScale = fresh.BossVisualScale;
            CharacterAnimSpeed = fresh.CharacterAnimSpeed;
            WalkSpeedMps = fresh.WalkSpeedMps;
            MoveAccelMps2 = fresh.MoveAccelMps2;
            MoveDecelMps2 = fresh.MoveDecelMps2;
            TurnRateDegPerSec = fresh.TurnRateDegPerSec;
            DodgeGlideSpeedMps = fresh.DodgeGlideSpeedMps;

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

        public bool IsDotOpen(int dot) => dot switch
        {
            1 => OpenDot1,
            2 => OpenDot2,
            3 => OpenDot3,
            4 => OpenDot4,
            5 => OpenDot5,
            6 => OpenDot6,
            _ => false
        };

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
            ReadoutAnchorRight = f.ReadoutAnchorRight;
            ReadoutPunchInSec = f.ReadoutPunchInSec;
            ShowFrameTimeHud = f.ShowFrameTimeHud;
            ShowDamageNumbers = f.ShowDamageNumbers;
        }

        /// <summary>"Sıfırla": yalnızca panelin yönettiği alt küme spec varsayılanına döner.</summary>
        public void ResetPanelFields() => ApplyPanelFields(new PrototypeTuning().ToPanelFields());
    }
}
