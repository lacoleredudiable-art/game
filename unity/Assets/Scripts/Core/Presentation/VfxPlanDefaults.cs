namespace Dovus.Core.Presentation
{
    /// <summary>
    /// efekt-motoru v4 dilim sabitleri (§1, §1.1, §3.1, §3.4, §4, §5.5, §7).
    /// Const geçici taşımadır; nihai ayar JSON/tuning.
    /// </summary>
    public static class VfxPlanDefaults
    {
        // §1 fiil renkleri (lineer RGB) + yoğunluk
        public const float ZararR = 1f, ZararG = 0.28f, ZararB = 0.06f, ZararIntensity = 3f;
        public const float HareketR = 1f, HareketG = 0.55f, HareketB = 0.20f, HareketIntensity = 1.5f;
        public const float ZayiflatmaR = 0.55f, ZayiflatmaG = 0.16f, ZayiflatmaB = 0.10f, ZayiflatmaIntensity = 1.5f;
        public const float CagirmaR = 1f, CagirmaG = 0.42f, CagirmaB = 0.10f, CagirmaIntensity = 2.5f;
        public const float KorumaR = 1f, KorumaG = 0.85f, KorumaB = 0.55f, KorumaIntensity = 2f;
        public const float YansimaR = 1f, YansimaG = 0.95f, YansimaB = 0.80f, YansimaIntensity = 2.5f;
        public const float KontrolR = 0.9f, KontrolG = 0.6f, KontrolB = 0.25f, KontrolIntensity = 2f;
        public const float KuvvetR = 1f, KuvvetG = 0.75f, KuvvetB = 0.40f, KuvvetIntensity = 2f;
        public const float SifaR = 0.55f, SifaG = 1f, SifaB = 0.35f, SifaIntensity = 2f;
        public const float GuclendirmeR = 1f, GuclendirmeG = 0.80f, GuclendirmeB = 0.25f, GuclendirmeIntensity = 2.5f;
        public const float ArindirmaR = 0.95f, ArindirmaG = 1f, ArindirmaB = 0.70f, ArindirmaIntensity = 2.5f;
        public const float ZamanR = 0.75f, ZamanG = 0.9f, ZamanB = 0.55f, ZamanIntensity = 1.5f;

        // §1.1 rün → Ejderha Dili
        public const float RuneSnapSec = 0.2f;
        public const float RuneGlowSec = 0.3f;

        // §3.1 Kılıç iz
        public const float KilicIzAtilOmurSec = 0.25f;
        public const float KilicIzVurOmurSec = 0.18f;
        public const float KilicIzAkisTurPerSec = 2f;
        public const int KilicIzOrnek = 20;
        public const int KilicIzAraNokta = 2;

        // §3.4 ejderha silüeti
        public const float EjderKilicAtilOmurSec = 0.25f;
        public const float EjderDissolveInSec = 0.05f;
        public const float EjderDissolveOutFrac = 0.4f;
        public const int EjderSiluetEkranMax = 2;
        public const float EjderSiluetUzakM = 12f;
        public const int EjderAtlasCellF = 5; // kuyruk yayı (0-index A=0 … F=5)

        // §4 skill uyanışı
        public const float KorIdleIntensity = 1f;
        public const float KorAwakenPeak = 1.6f;
        public const float KorAwakenRiseSec = 0.1f;
        public const float KorAwakenFadeSec = 0.25f;

        // §5.5 kenar freni kıvılcımı
        public const int EdgeStopSparkCount = 10;
        public const float EdgeStopSparkSec = 0.2f;

        // §2 değme flaşı
        public const float DegmeFlashSec = 0.15f;

        // Hit slash (Kılıç taşıyıcı)
        public const float SlashArcSec = 0.12f;
        public const float ZararClawSec = 0.18f;
        public const int ZararClawCount = 3;

        // Hareket ayak kıvılcımı
        public const int FootWingSparkCount = 8;
        public const float FootWingSparkSec = 0.2f;

        public const string WeaponKeyKilic = "kilic";
        public const string ActionDash = "dash";
    }
}
