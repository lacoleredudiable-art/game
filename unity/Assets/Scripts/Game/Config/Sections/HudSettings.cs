using UnityEngine;

namespace Dovus.Game.Config.Sections
{
    [System.Serializable]
    public sealed class HudSettings
    {
                // §8/T2 bedava kazanç: iptal penceresi dalganın yerinden okunur. Spec sayı vermiyor.
                [Header("İptal penceresi ipucu (T8, §8/T2)")]
                public float WindowCueUrgentRatio = 0.30f;
                public float WindowCuePulseHz = 2f;
                public float WindowCueUrgentHz = 8f;
                public float WindowCuePulseAmp = 0.45f;
                [Header("His katmanı (T8.1, CombatFeelDirector)")]
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
                // §6 gösterim: "sağ kenarda, parlak, büyük punto" + "hangi kenarda duracağı ayarlanabilir".
                // Punto/glow/bekleme/sönme FeelTuning.Readout* alanlarında (T1'de spec'ten kondu, T9
                // burada gerçekten kullanılıyor). Giriş vuruşunun (scale punch) sönme süresi spec'te
                // yok — uydurma, durum.md'ye T9 sapması olarak geçildi.
                [Header("Tepki yazısı (T9, ReactionReadoutHud)")]
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

                [Header("Build seçimi (v6 7b, BuildSelectHud)")]
                public bool SkipBuildSelectOnStart = false;

                [Header("Debug HUD")]
                public bool ShowSentenceDebugHud = false;
    }
}
