using Dovus.Core.Shared;
namespace Dovus.Core.Tuning
{
    /// <summary>Prototip boss (YERE ÇAKMA) — dovus-sistemi.md §11.</summary>
    [System.Serializable]
    public class BossTuning
    {
        // YAKIN — temel ritim (§11 tablosu). WindupMs/RadiusM bu varyantın alanlarıdır.
        public int WindupMs = 640;
        public float RadiusM = 5.4f;

        // GEÇ — aynı yarıçap, daha uzun hazırlık
        public int GecWindupMs = 900;
        public float GecRadiusM = 5.4f;

        // GENİŞ — aynı windup, daha büyük etki hacmi
        public int GenisWindupMs = 640;
        public float GenisRadiusM = 8.0f;

        /// <summary>
        /// Aynı varyantın üst üste en fazla kaç kez seçilebileceği.
        /// 2 → üçüncü tekrar engellenir (T13: üç kez gelirse desen yok sanılır).
        /// </summary>
        public int MaxSameVariantStreak = 2;

        // 16 Eylül: ikinci saldırı — docs/bosses/karadul.json "fire_cone" (Cehennem Nefesi,
        // can_yakma fiili). windup_ms=800, damage=18 spec'ten (uydurma yok); radius/arc spec
        // vermiyor — varsayılan, docs/durum.md'ye sapma olarak geçildi. (Core saf C# — Header
        // attribute yok, bkz. AGENTS.md kural 1.)
        public int FireConeWindupMs = 800;
        public int FireConeDamage = 18;
        public float FireConeRadiusM = 6.0f;
        public float FireConeArcHalfAngleDeg = 40f;
        /// <summary>Aynı saldırı TÜRÜnün (Slam/FireCone/Volley) üst üste tekrar tavanı — SlamVariant'la aynı desen.</summary>
        public int MaxSameAttackKindStreak = 2;

        // Zehir Tükürüğü — karadul.json "volley" (BossEncounterData yükler; bunlar dosyayla aynı
        // varsayılanlar). Oyuncu canı 22: mermi 6 → tam salvo ≈ %80 can.
        public int VolleyWindupMs = 700;
        public int VolleyCount = 3;
        public int VolleyCountEnraged = 5;
        /// <summary>Yelpazenin TAM açısı (derece).</summary>
        public float VolleySpreadDeg = 30f;
        public float VolleySpeedMps = 7f;
        public int VolleyDamage = 6;
        public float VolleyRadiusM = 0.35f;
        public float VolleyLifeSec = 3f;
        /// <summary>Telegraf yayının boyu (yalnız çizim; vuruş hacmi yok). Spec'te yok, his değeri.</summary>
        public float VolleyTelegraphRangeM = 8f;

        // Ağ Örme / Sıçrayış — spec'te yok — docs/design/aglarin-kralicesi.md
        public int WebFieldWindupMs = 900;
        public float WebFieldRadiusM = 3.0f;
        public float WebFieldLifeSec = 8f;
        public int WebFieldMaxCount = 3;
        public float WebFieldRefreshSec = 0.25f;
        public float WebFieldMinCenterDistM = 4f;
        public int PounceWindupMs = 1000;
        public int PounceDamage = 14;
        public float PounceLandRadiusM = 3.0f;
        public float PounceMinRangeM = 4f;
        public float PounceMaxRangeM = 12f;
        public float PounceAirSec = 0.45f;
        public float PounceWallMarginM = 2f;

        public int ActiveMs = 90;
        public int RecoveryMs = 720;
        /// <summary>
        /// Yere çakma hasarı. docs/bosses/karadul.json tuning.maps_to BossTuning.Damage = 22.
        /// Eski kayıt 0 ise BossDamageMigration bir kez 22'ye çeker; güncel sürüm bilinçli 0'ı korur.
        /// </summary>
        public int Damage = 22;
        public int IdleMinMs = 700;
        public int IdleMaxMs = 1500;
        public float ApproachSpeedMps = 2.2f;

        // Ölümden sonra tekrar dövüş — dovus-sistemi.md §11 (özet §4)
        public float RespawnMaxSec = 2.0f;

        // Boss can tavanı — dovus-sistemi.md §11
        public float MaxHp = 120f;

        /// <summary>
        /// Poise tavanı. Spec bir tavan yazmıyor; base_poise 15–20 ve silah poise_mult
        /// (çekiç 1.8, swap 2) ile birkaç ağır vuruş kırsın diye 100. docs/durum.md sapma.
        /// </summary>
        public float PoiseMax = 100f;

        /// <summary>
        /// Poise 0 olunca sersemlik süresi. karadul.json vitals.stagger_duration_sec 0.4 yazar;
        /// bu prototip turu 1.5 sn istiyor. Ayar değeri, koda gömülü his değil. docs/durum.md.
        /// </summary>
        public float StaggerDurationSec = 1.5f;

        /// <summary>
        /// ters_kontrol süresi, etki kendi süresini taşımıyorsa. Spec'te yok: gramer bugün her
        /// ters_kontrol etkisine 1.5 sn veriyor; aynı değer yedek olarak burada.
        /// </summary>
        public float ReverseFallbackSec = 1.5f;

        public void CopyFrom(BossTuning other)
        {
            WindupMs = other.WindupMs;
            RadiusM = other.RadiusM;
            GecWindupMs = other.GecWindupMs;
            GecRadiusM = other.GecRadiusM;
            GenisWindupMs = other.GenisWindupMs;
            GenisRadiusM = other.GenisRadiusM;
            MaxSameVariantStreak = other.MaxSameVariantStreak;
            FireConeWindupMs = other.FireConeWindupMs;
            FireConeDamage = other.FireConeDamage;
            FireConeRadiusM = other.FireConeRadiusM;
            FireConeArcHalfAngleDeg = other.FireConeArcHalfAngleDeg;
            MaxSameAttackKindStreak = other.MaxSameAttackKindStreak;
            VolleyWindupMs = other.VolleyWindupMs;
            VolleyCount = other.VolleyCount;
            VolleyCountEnraged = other.VolleyCountEnraged;
            VolleySpreadDeg = other.VolleySpreadDeg;
            VolleySpeedMps = other.VolleySpeedMps;
            VolleyDamage = other.VolleyDamage;
            VolleyRadiusM = other.VolleyRadiusM;
            VolleyLifeSec = other.VolleyLifeSec;
            VolleyTelegraphRangeM = other.VolleyTelegraphRangeM;
            WebFieldWindupMs = other.WebFieldWindupMs;
            WebFieldRadiusM = other.WebFieldRadiusM;
            WebFieldLifeSec = other.WebFieldLifeSec;
            WebFieldMaxCount = other.WebFieldMaxCount;
            WebFieldRefreshSec = other.WebFieldRefreshSec;
            WebFieldMinCenterDistM = other.WebFieldMinCenterDistM;
            PounceWindupMs = other.PounceWindupMs;
            PounceDamage = other.PounceDamage;
            PounceLandRadiusM = other.PounceLandRadiusM;
            PounceMinRangeM = other.PounceMinRangeM;
            PounceMaxRangeM = other.PounceMaxRangeM;
            PounceAirSec = other.PounceAirSec;
            PounceWallMarginM = other.PounceWallMarginM;
            ActiveMs = other.ActiveMs;
            RecoveryMs = other.RecoveryMs;
            Damage = other.Damage;
            IdleMinMs = other.IdleMinMs;
            IdleMaxMs = other.IdleMaxMs;
            ApproachSpeedMps = other.ApproachSpeedMps;
            RespawnMaxSec = other.RespawnMaxSec;
            MaxHp = other.MaxHp;
            PoiseMax = other.PoiseMax;
            StaggerDurationSec = other.StaggerDurationSec;
            ReverseFallbackSec = other.ReverseFallbackSec;
        }

        public void ResetToDefaults() => CopyFrom(new BossTuning());

        public int WindupMsFor(SlamVariant variant) => variant switch
        {
            SlamVariant.Gec => GecWindupMs,
            SlamVariant.Genis => GenisWindupMs,
            _ => WindupMs
        };

        public float RadiusMFor(SlamVariant variant) => variant switch
        {
            SlamVariant.Gec => GecRadiusM,
            SlamVariant.Genis => GenisRadiusM,
            _ => RadiusM
        };
    }
}
