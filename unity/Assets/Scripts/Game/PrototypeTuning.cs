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
        public float ArenaHalfSizeM = 12f;

        [Header("Oyuncu")]
        public float WalkSpeedMps = 4.5f;

        // Spec §11 hasar 22; oyuncu tavanı belgede yok. Bir çakma = ölüm — respawn ≤2 sn
        // (§11) döngüsü böyle denenebiliyor. T11 his turunda ayarlanacak.
        [Header("Oyuncu can (T8)")]
        public int PlayerMaxHp = 22;

        [Header("Sanal çubuk")]
        public float JoystickMaxRadiusDp = 72f;
        public float JoystickDeadZone = 0.12f;

        // Beşgen ekrana sabit (§2). Yarıçap/konum spec'te sayı yok — varsayılan; durum.md'ye geçildi.
        // T6.2: merkez X, dodge düğmesi sağ kenardan taşmasın diye 0.78'den içeri alındı.
        [Header("Beşgen (§2)")]
        public float PentagonCenterXNorm = 0.72f;
        public float PentagonCenterYNorm = 0.40f;
        public float PentagonRadiusDp = 100f;
        public float DotHitRadiusDp = 30f;
        public float CenterHitRadiusDp = 24f;
        public bool MirrorForLeftHand = false;
        public float InkLingerSec = 0.40f;
        public float InkWidthDp = 3.5f;

        // §2: dodge beşgenin dışında, ekrana sabit ayrı düğme (sol çubuk dinamik olduğu için
        // yanına konamaz). Ofset/yarıçap spec'te yok — varsayılan; durum.md'ye geçildi.
        // Ofset beşgen merkezinden dp cinsinden; MirrorForLeftHand X'i çevirir.
        [Header("Dodge düğmesi (§2, T6.2)")]
        public float DodgeButtonOffsetXDp = 80f;
        public float DodgeButtonOffsetYDp = -140f;
        public float DodgeButtonRadiusDp = 34f;
        public float DodgeButtonScreenMarginDp = 8f;

        // §5: merkez bir kelime değil düğme; hangi fiille vurduğu veridir (prototipte 5/SARSINTI).
        [Header("Düz vuruş (§5, T6.2)")]
        public int BasicStrikeDot = 5;

        // §4 ilk tur: yalnızca 1 (İĞNE), 2 (SÜRÜ), 5 (SARSINTI). Kombo tablosu değil — açık/kapalı bayrak.
        [Header("Açık rünler (§4)")]
        public bool OpenDot1 = true;
        public bool OpenDot2 = true;
        public bool OpenDot3 = false;
        public bool OpenDot4 = false;
        public bool OpenDot5 = true;

        // Spec'te sayı yok — ayrık onay tıkırtısı (§2); Handheld.Vibrate ~500 ms üst üste biniyordu.
        [Header("Dokunsal (§2)")]
        public long DotVibrationMs = 30;

        [Header("Kamera")]
        public float FollowSmoothTimeSec = 0.18f;
        public float LookAheadM = 1.4f;
        public Vector3 CameraOffset = new Vector3(0f, 7f, -6f);

        // Renkler dovus-sistemi.md §10'dan. Kırmızı-turuncu bossun TEHDİDİNE ayrılı olduğu için
        // bossun gövdesi nötr: telegraf (T8) yandığında kontrast kalsın.
        [Header("Renk dili (§10)")]
        public Color PlayerColor = new Color(0.373f, 0.941f, 1f);
        public Color BossColor = new Color(0.18f, 0.19f, 0.22f);
        public Color GroundColor = new Color(0.38f, 0.4f, 0.44f);
        public Color BackgroundColor = new Color(0.12f, 0.14f, 0.18f);
        public Color InkPurple = new Color(0.725f, 0.549f, 1f);   // #B98CFF
        public Color InkCyan = new Color(0.373f, 0.941f, 1f);     // #5FF0FF
        public Color AcidGreen = new Color(0.608f, 0.910f, 0.235f); // #9BE83C — §10 zehir birikintisi
        public Color PentagonDotColor = new Color(0.55f, 0.62f, 0.72f, 0.85f);
        // T6.2: merkez artık "vur" demek — oyuncu rengine çekildi (§10 camgöbeği).
        public Color PentagonCenterColor = new Color(0.373f, 0.941f, 1f, 0.9f);
        // Dodge diski §10 moru: kırmızı-turuncu OLAMAZ, o renk yalnızca boss tehdidi.
        public Color DodgeButtonColor = new Color(0.725f, 0.549f, 1f, 0.9f);
        // §10: kırmızı-turuncu YALNIZCA boss tehdidi.
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
        // "Filtre yok" değeri; yavaş çekimde SlowmoTuning.AudioLowpassHz devralır.
        public float AudioBaseCutoffHz = 22000f;

        // T7.2: LivingEffectView'a gömülü his sayıları (AGENTS kural 3). Değerler T7'den
        // AYNI taşındı, yalnızca yeri değişti — dovus-sistemi.md'de sayı yok, sapma T7
        // durum.md'sinde kayıtlı.
        [Header("Tezahür çizgisi (T7.2, LivingEffectView)")]
        public float EffectLineWidthDefaultM = 0.18f;
        public float EffectLineWidthWideM = 0.35f;
        public float EffectLineWidthNarrowM = 0.12f;
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
        public float EffectBlobScaleBaseM = 0.28f;
        public float EffectBlobScalePerSpreadM = 0.12f;
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

        // §11 boss ölümü: "kısa yavaş çekim + çökme pozu" — süre spec'te yok (uydurma).
        // Yavaş çekim TimeDirector.TriggerSlowmo (SlowmoTuning); çökme süresi dünya saati.
        // Gerekçe docs/durum.md T12 sapmaları.
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

        // §6/§11 "boss ve oyuncu can göstergesi, sade". Ölçüler dp (beşgen/dodge diskiyle
        // aynı yol: PentagonLayoutScreen.DpToPixels). Boss barı T12'den beri BossVitals okur.
        [Header("Can göstergesi (T9, VitalsHud)")]
        public float VitalsBarWidthDp = 220f;
        public float VitalsBarHeightDp = 16f;
        public float VitalsBarSpacingDp = 6f;
        public float VitalsMarginDp = 18f;
        public Color BossVitalsColor = new Color(0.70f, 0.74f, 0.80f, 0.85f);

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
        public bool ShowDamageNumbers = false;

        // Sahneye serileşmiş eski kopyada yeni alanlar 0/siyah gelir (C# initializer
        // deserialize'da uygulanmaz). Sürüm numarası da 0 geldiği için tek seferlik yama
        // ÇALIŞIR; sahne bir kez yeniden kaydedildikten sonra bu blok hiç girmez ve
        // tasarımcının bilinçli 0'ı (ör. nabzı kapatmak) artık ezilmez (T8.1).
        [HideInInspector] public int TuningVersion = CurrentVersion;

        const int CurrentVersion = 7;

        /// <summary>Sürümü geçmiş serileşmiş kopyayı bu sürümün varsayılanlarına çeker.</summary>
        public void EnsureRuntimeDefaults()
        {
            if (TuningVersion >= CurrentVersion)
                return;

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
            AudioBaseCutoffHz = fresh.AudioBaseCutoffHz;

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

            TuningVersion = CurrentVersion;
        }

        public bool IsDotOpen(int dot) => dot switch
        {
            1 => OpenDot1,
            2 => OpenDot2,
            3 => OpenDot3,
            4 => OpenDot4,
            5 => OpenDot5,
            _ => false
        };

        /// <summary>
        /// T10: `PrototypeTuning`'in tamamı (renkler, beşgen konumu, arena...) ayar paneline
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
