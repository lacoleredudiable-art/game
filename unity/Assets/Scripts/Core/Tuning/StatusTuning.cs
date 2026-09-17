namespace Dovus.Core.Tuning
{
    /// <summary>
    /// Status süreleri / büyüklükleri. element-sistemi.json süre vermiyor —
    /// varsayılanlar; his turunda ayarlanır (durum.md).
    /// </summary>
    [System.Serializable]
    public sealed class StatusTuning
    {
        public int StunMs = 800;
        public int RootMs = 1200;
        public int SilenceMs = 1000;
        public int SlowMs = 1500;
        public float SlowSpeedMult = 0.55f;
        public int BlindMs = 1400;
        public int FearMs = 900;
        public int DisarmMs = 1000;
        public int TauntMs = 1200;
        public int StasisMs = 700;
        /// <summary>global_rules.status_durations.stealth duration_sec=4.</summary>
        public int StealthMs = 4000;

        public int BurnMs = 2400;
        public float BurnDamagePerSec = 6f;
        public int ArmorBreakMs = 3000;
        public float ArmorBreakDamageTakenMult = 1.2f;
        public int WeakenMs = 2500;
        public float WeakenOutgoingMult = 0.85f;
        public int GrievousMs = 2500;
        // 16 Eylül: magnitude eskiden hep 1f (kullanılmıyordu) — artık gelen heal'i çarpıyor
        // (bkz. StatusBoard.HealEffectivenessMult). "Kavurucu Yara" (grievous+burn) tepkisi
        // bu değeri hedefler.
        public float GrievousHealMult = 0.5f;

        // 16 Eylül: element-sistemi.json mechanics'te vardı, StatusKind'ta yoktu (bilinen açık).
        public int PoisonMs = 3000;
        public float PoisonDamagePerSec = 4f;

        public int ShieldMs = 5000;
        public float ShieldAbsorb = 50f;
        public int HasteMs = 2000;
        public float HasteSpeedMult = 1.35f;
        public int DamageReductionMs = 2000;
        public float DamageReductionMult = 0.7f;
        public int RegenMs = 3000;
        public float RegenPerSec = 5f;

        /// <summary>Knockback anlık; süre yerine BossReactor metresi.</summary>
        public float KnockbackMeters = 1.4f;
        public float KnockbackLiftM = 0.35f;
        public float KnockbackShakeSec = 0.18f;

        // 16 Eylül: durum etkileşim tablosu (docs/element-sistemi.json status_interaction_table)
        // — genellenemeyen üç özel kombinasyon. Sayılar sahibinin verdiği spec'ten (uydurma yok).
        /// <summary>"Zehirli Ateş": burn + poison aynı anda → ekstra tick hasarı.</summary>
        public float BurnPoisonComboBonusPerSec = 2f;
        /// <summary>"Yanan Kalkan": burn tick'i kalkanı da bu oranla aşındırır.</summary>
        public float ShieldBurnDrainRatio = 0.5f;
        /// <summary>"Savrulma Sersemliği": aynı vuruşta stun+knockback birlikteyse stun süresi uzar.</summary>
        public int StunKnockbackDurationAddMs = 1000;

        public void CopyFrom(StatusTuning other)
        {
            if (other == null) return;
            StunMs = other.StunMs;
            RootMs = other.RootMs;
            SilenceMs = other.SilenceMs;
            SlowMs = other.SlowMs;
            SlowSpeedMult = other.SlowSpeedMult;
            BlindMs = other.BlindMs;
            FearMs = other.FearMs;
            DisarmMs = other.DisarmMs;
            TauntMs = other.TauntMs;
            StasisMs = other.StasisMs;
            StealthMs = other.StealthMs;
            BurnMs = other.BurnMs;
            BurnDamagePerSec = other.BurnDamagePerSec;
            ArmorBreakMs = other.ArmorBreakMs;
            ArmorBreakDamageTakenMult = other.ArmorBreakDamageTakenMult;
            WeakenMs = other.WeakenMs;
            WeakenOutgoingMult = other.WeakenOutgoingMult;
            GrievousMs = other.GrievousMs;
            GrievousHealMult = other.GrievousHealMult;
            PoisonMs = other.PoisonMs;
            PoisonDamagePerSec = other.PoisonDamagePerSec;
            ShieldMs = other.ShieldMs;
            ShieldAbsorb = other.ShieldAbsorb;
            HasteMs = other.HasteMs;
            HasteSpeedMult = other.HasteSpeedMult;
            DamageReductionMs = other.DamageReductionMs;
            DamageReductionMult = other.DamageReductionMult;
            RegenMs = other.RegenMs;
            RegenPerSec = other.RegenPerSec;
            KnockbackMeters = other.KnockbackMeters;
            KnockbackLiftM = other.KnockbackLiftM;
            KnockbackShakeSec = other.KnockbackShakeSec;
            BurnPoisonComboBonusPerSec = other.BurnPoisonComboBonusPerSec;
            ShieldBurnDrainRatio = other.ShieldBurnDrainRatio;
            StunKnockbackDurationAddMs = other.StunKnockbackDurationAddMs;
        }

        public void ResetToDefaults() => CopyFrom(new StatusTuning());
    }
}
