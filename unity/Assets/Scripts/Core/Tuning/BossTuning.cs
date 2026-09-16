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
        /// <summary>Aynı saldırı TÜRÜnün (Slam/FireCone) üst üste tekrar tavanı — SlamVariant'la aynı desen.</summary>
        public int MaxSameAttackKindStreak = 2;

        public int ActiveMs = 90;
        public int RecoveryMs = 720;
        public int Damage = 0;
        public int IdleMinMs = 700;
        public int IdleMaxMs = 1500;
        public float ApproachSpeedMps = 2.2f;

        // Ölümden sonra tekrar dövüş — dovus-sistemi.md §11 (özet §4)
        public float RespawnMaxSec = 2.0f;

        // Boss can tavanı — dovus-sistemi.md §11
        public float MaxHp = 120f;

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
            ActiveMs = other.ActiveMs;
            RecoveryMs = other.RecoveryMs;
            Damage = other.Damage;
            IdleMinMs = other.IdleMinMs;
            IdleMaxMs = other.IdleMaxMs;
            ApproachSpeedMps = other.ApproachSpeedMps;
            RespawnMaxSec = other.RespawnMaxSec;
            MaxHp = other.MaxHp;
        }

        public void ResetToDefaults() => CopyFrom(new BossTuning());

        public int WindupMsFor(Dovus.Core.Combat.SlamVariant variant) => variant switch
        {
            Dovus.Core.Combat.SlamVariant.Gec => GecWindupMs,
            Dovus.Core.Combat.SlamVariant.Genis => GenisWindupMs,
            _ => WindupMs
        };

        public float RadiusMFor(Dovus.Core.Combat.SlamVariant variant) => variant switch
        {
            Dovus.Core.Combat.SlamVariant.Gec => GecRadiusM,
            Dovus.Core.Combat.SlamVariant.Genis => GenisRadiusM,
            _ => RadiusM
        };
    }
}
