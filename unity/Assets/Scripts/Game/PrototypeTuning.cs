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
        public float ArenaHalfSizeM = 25f;
        /// <summary>Çevre duvar yüksekliği (tavansız salon).</summary>
        public float ArenaWallHeightM = 18f;
        /// <summary>Çevre duvar kalınlığı.</summary>
        public float ArenaWallThicknessM = 1.4f;
        /// <summary>Quaternius arena prefab ölçeği — daire salonda 1.</summary>
        public float ArenaVisualScale = 1.0f;

        [Header("Karakter görsel boyu (metre)")]
        /// <summary>Prefab kaynağından bağımsız, renderer bounds ile ölçülen oyuncu boyu.</summary>
        public float PlayerVisualHeightM = 1.78f;
        /// <summary>Oyuncunun yaklaşık 2.8 katı; renderer bounds ile ölçülen boss boyu.</summary>
        public float BossVisualHeightM = 5.0f;
        /// <summary>Animator hızı — locomotion 1; idle freeze blend tree’de.</summary>
        public float CharacterAnimSpeed = 1.0f;

        [Header("Oyuncu")]
        /// <summary>Tam çubuk koşu hızı; Axe run klibinin 5.52 m/sn doğal hızına yakın.</summary>
        public float WalkSpeedMps = 6.4f;
        public float MoveAccelMps2 = 52f;
        public float MoveDecelMps2 = 64f;
        public float TurnRateDegPerSec = 720f;
        /// <summary>Çubuk ölü bölgenin hemen üstündeyken hız oranı (yürüme); tam çubuk = 1 (koşu). Önerilen.</summary>
        public float MinStickSpeedFrac = 0.42f;
        /// <summary>Animator Speed parametresi sönümü. Önerilen.</summary>
        public float AnimSpeedDampSec = 0.08f;
        /// <summary>Koşu klibi doğal hızını aşınca en çok bu kat hızlanır (üstü ayak kayması kabul). Önerilen.</summary>
        public float LocoMaxPlaybackMult = 1.5f;
        /// <summary>Aksiyon state'ine giriş crossfade süresi (eskisi sert kesim). Önerilen.</summary>
        public float AnimCrossFadeSec = 0.06f;
        /// <summary>Düz vuruş görsel döngüsü (A→B→C) bu süre vuruşsuz geçince A'ya döner. Önerilen.</summary>
        public float BasicStrikeComboResetSec = 1.2f;
        /// <summary>Animator Speed bu eşiğin üstündeyken cast üst gövde katmanında oynar (bacaklar koşar). Önerilen.</summary>
        public float UpperBodyCastMinSpeed = 0.15f;
        /// <summary>Ayak sesi + tozu bu kadar yatay yolda bir (7.5 m/sn koşuda ~3.4 adım/sn). Önerilen.</summary>
        public float FootstepStrideM = 2.2f;
        /// <summary>Boss ağır adımı (ses + büyük toz). Önerilen.</summary>
        public float BossFootstepStrideM = 2.4f;

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
        /// <summary>
        /// Ekranın kısa kenarı bu dp'den azsa bütün savaş HUD'u orantılı küçülür (0 = kapalı).
        /// Yatay telefonda kısa kenar ~440 dp; tam ölçekte altıgen tepsisi yüksekliğin %84'ünü
        /// kaplıyordu (29 Eyl telefon). Önerilen.
        /// </summary>
        public float HudFitShortSideDp = 600f;
        // Sağ-alt köşeye yaslı: boss ekran ortasında görünür kalsın.
        public float HexagonCenterXNorm = 0.78f;
        // Sağ-alt; FittedRadiusPx alt rünleri safe içinde tutar.
        public float HexagonCenterYNorm = 0.30f;
        // Komşu rün kenar boşluğu ≈ radius − 2·dotR (≥40dp çizim koridoru).
        public float HexagonRadiusDp = 112f;
        public float DotHitRadiusDp = 34f;
        public float CenterHitRadiusDp = 38f;
        public bool MirrorForLeftHand = false;
        public float InkLingerSec = 0.40f;
        public float InkWidthDp = 3.5f;
        /// <summary>Ham çizim izi genişlik çarpanı (InkWidthDp tabanı). spec'te yok — varsayılan</summary>
        public float InkRawWidthScale = 0.9f;
        /// <summary>Ham iz baş ucunda cyan→beyaz parlaklık (1 = tam). spec'te yok — varsayılan</summary>
        public float InkRawGlow = 1f;

        // §2: dodge hex dışı (sağ-alt); offset ek kaydırma (varsayılan 0).
        [Header("Dodge düğmesi (§2, T6.2)")]
        public float DodgeButtonOffsetXDp = 0f;
        public float DodgeButtonOffsetYDp = 0f;
        public float DodgeButtonRadiusDp = 40f;
        public float DodgeButtonScreenMarginDp = 10f;
        /// <summary>Hex kenarı ile dodge yüzeyi arası (dp).</summary>
        public float DodgeClearanceDp = 40f;

        // weapon_skill_interaction.swap: dodge'un altıgene göre simetriği (sol-alt).
        [Header("Silah swap düğmesi (v6 swap)")]
        public float WeaponSwapButtonRadiusDp = 36f;
        public float WeaponSwapButtonOffsetXDp = 0f;
        public float WeaponSwapButtonOffsetYDp = 0f;
        [Range(0.4f, 1f), Tooltip("Swap yüzü içindeki silah ikonunun çap oranı.")]
        public float WeaponSwapIconScale = 0.72f;

        [Header("Lock-on düğmesi (mobil)")]
        public float LockOnButtonRadiusDp = 36f;
        public float LockOnButtonOffsetXDp = 0f;
        public float LockOnButtonOffsetYDp = 52f;
        public Color LockOnButtonColor = new Color(0.55f, 0.72f, 0.88f, 0.9f);
        public Color LockOnButtonActiveColor = new Color(0.95f, 0.82f, 0.45f, 0.95f);

        [Header("Element radial (v6.1.1)")]
        /// <summary>JSON yalnız 6 konum/300 ms verir; ekran yarıçapı için prototip varsayılanı.</summary>
        public float ElementMenuRadiusDp = 92f;
        public float ElementMenuChipWidthDp = 144f;
        public float ElementMenuChipHeightDp = 48f;
        public float ElementMenuItemWidthDp = 84f;
        public float ElementMenuItemHeightDp = 48f;
        // Sağ başparmak: altıgen tepsisinin üstü (sol yarı hareket çubuğunun).
        [Range(0f, 1f)] public float ElementMenuAnchorXNorm = 0.86f;
        [Range(0f, 1f)] public float ElementMenuAnchorYNorm = 0.72f;

        // §5: merkez bir kelime değil düğme; hangi fiille vurduğu veridir (prototipte 1/Ateş).
        [Header("Düz vuruş (§5, T6.2)")]
        public int BasicStrikeDot = 1;

        [Header("Altıgen ikon")]
        // 1.0 — görsel disk hit'ten şişmesin, komşu boşluğu yemesin.
        public float IconDisplayScale = 1.0f;
        [Tooltip("Altıgeni gruplayan cam tepsinin rune halkasına ek boşluğu.")]
        public float CombatTrayPaddingDp = 22f;
        public float CombatTrayHeaderHeightDp = 22f;
        public float CombatTrayCornerRadiusDp = 20f;
        public float CombatTrayLinkWidthDp = 1.5f;

        // Spec'te sayı yok — ayrık onay tıkırtısı (§2); Handheld.Vibrate ~500 ms üst üste biniyordu.
        [Header("Dokunsal (§2)")]
        public long DotVibrationMs = 30;

        [Header("Kamera — omuz üstü savaş")]
        public float FollowSmoothTimeSec = 0.12f;
        public float LookAheadM = 0.65f;
        /// <summary>Oyuncu köküne göre omuz pivotu; mesafe ayrıca geriye uygulanır.</summary>
        public Vector3 CameraShoulderOffset = new Vector3(0.42f, 1.12f, -0.28f);
        public float CameraDistanceM = 5.85f;
        public float CameraLookHeightM = 0.38f;
        public float CameraFovDeg = 54f;
        public float CameraAimDampingSec = 0.12f;
        public float CameraSoftLockRangeM = 20f;
        [Range(0f, 1f)] public float CameraSoftLockStrength = 0.58f;
        /// <summary>Soft-lock aktifken bakış: oyuncu→boss arası (0=oyuncu, 1=boss).</summary>
        [Range(0f, 1f)] public float CameraBossFramingWeight = 0.40f;
        public float CameraBossAimHeightM = 2.05f;
        public float CameraDefaultPitchDeg = 21f;
        public float CameraLockOnMinDistanceM = 5.2f;
        public float CameraLockOnMaxDistanceM = 8.8f;
        /// <summary>Boss mesafesi arttıkça mesafe artışı (m / m ayrım).</summary>
        public float CameraLockOnDistancePerSepM = 0.14f;
        public float CameraLockOnMaxExtraDistanceM = 3.1f;
        public float CameraLockOnDistanceSmoothSec = 0.22f;
        public float CameraWindupDistanceMul = 1.42f;
        public float CameraWindupExtraHeightM = 0.68f;
        public float CameraWindupSmoothSec = 0.28f;
        /// <summary>Slam dışı geniş telegraf (FireCone vb.) için yarıçap eşiği (m).</summary>
        public float CameraWindupMinRadiusM = 3.5f;
        public float CameraCollisionSphereRadiusM = 0.25f;
        public float CameraCollisionMarginM = 0.12f;
        public float CameraCollisionMinDistanceM = 1.2f;
        public float CameraCollisionPullInSmoothSec = 0.05f;
        public float CameraCollisionPullOutSmoothSec = 0.35f;
        /// <summary>Lock-on omuz üstü yatay ofset (m).</summary>
        public float CameraLockOnShoulderSideM = 1.1f;
        public float CameraLockOnShoulderFlipHysteresis = 0.12f;
        [Range(0.35f, 0.75f)] public float CameraLockOnLookBlendToBoss = 0.58f;

        // Ambiyans portu (PR #43 "deneme sahnesi"): gri bulutlu ışık, ~%35 doygunluk düşüşü,
        // açık gri sis — sert/sakin ama her şey görünür (karanlık değil, renkli değil). Sıcak
        // vurgu yalnız lav (LavaDecor/LavaCracks) ve VFX'te kalır. docs'ta sayı yok — PR #43'ün
        // kendi commit'lerinde kullandığı değerler (DenemeSahnesi_PostFX.asset) buraya taşındı.
        [Header("Arena atmosferi — mobil URP (ambiyans: PR #43 açık gri lav ovası)")]
        public Color AmbientSky = new Color(0.66f, 0.70f, 0.73f);
        public Color AmbientEquator = new Color(0.52f, 0.55f, 0.58f);
        public Color AmbientGround = new Color(0.30f, 0.30f, 0.31f);
        public Color FogColor = new Color(0.69f, 0.718f, 0.737f);
        public float FogDensity = 0.0032f;
        public Color KeyLightColor = new Color(0.86f, 0.89f, 0.92f);
        public float KeyLightIntensity = 0.85f;
        public Vector3 KeyLightEuler = new Vector3(52f, -30f, 0f);
        public float KeyShadowStrength = 0.38f;
        public Color RimLightColor = new Color(0.38f, 0.55f, 1f);
        public float RimLightIntensity = 0.18f;
        public Vector3 RimLightEuler = new Vector3(28f, 145f, 0f);
        public float BloomIntensity = 0.65f;
        public float BloomThreshold = 0.95f;
        public float BloomScatter = 0.55f;
        public Color BloomTint = new Color(1f, 0.86f, 0.72f);
        public float PostExposure = 0.15f;
        public float ColorContrast = -14f;
        /// <summary>Spec "~%35 doygunluk düşüşü" (task-ambience-fix); PR #43'ün gönderdiği -22
        /// değil, görevin kendi istediği oran — sahnede görünür olmalı.</summary>
        public float ColorSaturation = -35f;
        public Color ColorFilterTint = new Color(0.95f, 0.975f, 1f);
        public float PostVignetteIntensity = 0.12f;
        /// <summary>Ufuk siluet/kayaları görünür kalsın diye uzak kırpma düzlemi büyütüldü (eski 120m).</summary>
        public float CameraFarClipM = 320f;

        // Mevcut prototip paleti: nötr boss gövdesi, sıcak telegraf ve element renkleri.
        [Header("Renk dili (§10)")]
        public Color PlayerColor = new Color(0.373f, 0.941f, 1f);
        public Color BossColor = new Color(0.18f, 0.19f, 0.22f);
        public Color GroundColor = new Color(0.44f, 0.46f, 0.49f);
        public Color BackgroundColor = new Color(0.69f, 0.718f, 0.737f);
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

        [Header("Boss karşılaşma")]
        /// <summary>Resources/Bosses/{id}.json — varsayılan karadul (CI kapısı).</summary>
        public string ActiveBossId = "karadul";

        /// <summary><c>Resources.Load</c> yolu: <c>Bosses/</c> + id (<c>_</c> → <c>-</c>).</summary>
        public string ActiveBossResourcePath =>
            "Bosses/" + (string.IsNullOrWhiteSpace(ActiveBossId) ? "karadul" : ActiveBossId.Replace('_', '-'));

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
        public float VitalsBarWidthDp = 224f;
        public float VitalsBarHeightDp = 17f;
        public float VitalsBossBarHeightDp = 24f;
        public float VitalsBossBarWidthDp = 460f;
        public float VitalsBarSpacingDp = 6f;
        public float VitalsMarginDp = 16f;
        public float VitalsHeaderHeightDp = 18f;
        public float VitalsPanelPaddingDp = 8f;
        public Color BossVitalsColor = new Color(0.88f, 0.16f, 0.12f, 0.98f);

        [Header("Status ikon şeridi")]
        public float StatusIconSizeDp = 28f;
        public float StatusIconGapDp = 6f;

        [Header("Kamera orbit")]
        public float OrbitDegreesPerDp = 0.35f;
        /// <summary>
        /// Dikey sürükleme eğimi (+ = kamera yükselip aşağı bakar). Alt sınır kameranın zemine
        /// inmemesi için; üst sınır boss'u kadrajdan atmaması için. Önerilen.
        /// </summary>
        public float CameraPitchMinDeg = -8f;
        public float CameraPitchMaxDeg = 35f;
        [Tooltip("Açıkken parmak yukarı = kamera yükselir (aşağı bakar).")]
        public bool OrbitInvertPitch = false;

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
        public float SkillPreviewWidthDp = 324f;
        public float SkillPreviewHeightDp = 72f;
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

        const int CurrentVersion = 20;

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
