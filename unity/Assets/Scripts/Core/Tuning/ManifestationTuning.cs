namespace Dovus.Core.Tuning
{
    /// <summary>
    /// Tezahür (yaşayan etki) sayıları. Spec §8'de hız/yarıçap yok — varsayılanlar;
    /// telefonda T11 his turunda ayarlanacak. Değişiklik durum.md'ye geçilir.
    /// T10 paneli bunu KAPSAMIYOR (henüz düzenlenebilir yüzeye alınmadı); [Serializable]
    /// yalnızca CombatTuning JSON'a yazılırken hata vermemesi için.
    /// </summary>
    [System.Serializable]
    public class ManifestationTuning
    {
        // Seyahat — fiil dünyada yaşasın diye (T2)
        // T14: hızlar hareket karakterini ayırır (spec sayı yok — durum.md T14 sapmaları).
        public float WaveSpeedMps = 4.8f;
        public float WaveMaxRadiusM = 8.5f;
        public float NeedleSpeedMps = 28f;
        public float NeedleMaxRangeM = 12f;
        public float SwarmSpeedMps = 5.2f;
        public float SwarmMaxRadiusM = 7.5f;

        // T14 — İĞNE Zenitsu küçük hâli: gerilme → 2–3 kare gidiş → donmuş varış.
        public float NeedleWindupSec = 0.12f;
        public float NeedleDashSec = 0.05f;

        // T14 — SÜRÜ kademeli üşüşme (görünüm + tempo; hasar değil).
        public float SwarmStaggerSec = 0.055f;

        // T14 — SARSINTI yerden yükselme yüksekliği (görünüm; Lift ile çarpılır).
        public float WaveRiseHeightM = 0.55f;

        // T14 — düz vuruş: kısa/dar jab; cümle SARSINTI halkasından ayrı (bilinen açık).
        public float BasicStrikeRangeM = 2.4f;
        public float BasicStrikeSpeedMps = 14f;
        public float BasicStrikeBangSec = 0.22f;
        public float BasicStrikeFadeSec = 0.18f;
        public float BasicStrikeScarScaleM = 0.7f;

        // Silüet eksenleri — sıfat sayıyı değil şekli değiştirir (T3)
        public float FocusPerIgne = 0.78f;
        public float PiercePerIgne = 0.28f;
        public float SpreadPerSuru = 0.85f;
        public float LiftPerSarsinti = 0.45f;
        public float DwellStackScale = 0.35f;

        // SilhouetteBuilder sıfat sabitleri (T7.1: Core'a gömülüydü, yeri değişti — değer aynı)
        public float SuruFocusReduceThreshold = 0.35f;
        public float SuruFocusReduceAmount = 0.05f;
        public float KabukSpreadMultiplier = 0.55f;
        public float KabukFocusAdd = 0.12f;
        public float ZehirSpreadAdd = 0.2f;

        // Görünür toplama: hedef silüete saniyede ne kadar yaklaşır
        public float MorphLerpPerSec = 3.5f;

        // Seyahat hızı / kapanış zamanlaması (T7.1: LivingEffect'e gömülüydü, yeri değişti)
        public float PierceSpeedBonus = 0.25f;
        public float FadeDurationSec = 0.35f;
        public float BangDurationSec = 0.45f;
        public float WaveCorridorHalfWidthWideM = 2.2f;
        public float WaveCorridorHalfWidthNarrowM = 0.35f;

        /// <summary>
        /// Menzilini bitiren etki cümle kapanmadan sönmemeli (§5/T2: fiil dünyada yaşamalı).
        /// Bu, o beklemenin üst güvenlik payı — normal oyunda hiç dokunulmaz, motor her cümleyi
        /// er ya da geç kapatır (Abort ya da ResolveWithClosing). Spec'te sayı yok, uydurma;
        /// gerekçe docs/durum.md T7.1 sapmalarına yazıldı.
        /// </summary>
        public float MaxHoldPastRangeSec = 6f;

        // Temas / kapanış (hasar sayısı YOK — yalnızca fiziksel tepki)
        public float TravelHitRadiusM = 1.15f;
        public float ClosingBangRadiusM = 3.6f;
        public float BossKnockbackM = 1.35f;
        public float BossLiftM = 1.1f;
        public float BossShakeSec = 0.28f;

        // Kalıcı iz boyutu
        public float ScarScaleM = 1.4f;
        public int MaxSwarmBlobs = 10;

        /// <summary>Alan kopyası — düz vuruş profili gibi geçici override için.</summary>
        public ManifestationTuning Clone()
        {
            return (ManifestationTuning)MemberwiseClone();
        }

        /// <summary>
        /// Düz vuruşun kısa menzil/tempo profili. Gramer aynı (BasicStrikeDot fiili);
        /// yalnızca seyahat ölçüleri cümleden ayrılır.
        /// </summary>
        public ManifestationTuning WithBasicStrikeProfile()
        {
            ManifestationTuning c = Clone();
            c.WaveSpeedMps = BasicStrikeSpeedMps;
            c.WaveMaxRadiusM = BasicStrikeRangeM;
            c.BangDurationSec = BasicStrikeBangSec;
            c.FadeDurationSec = BasicStrikeFadeSec;
            c.ScarScaleM = BasicStrikeScarScaleM;
            return c;
        }
    }
}
