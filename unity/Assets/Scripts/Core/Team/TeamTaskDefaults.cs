namespace Dovus.Core.Team
{
    /// <summary>
    /// Takım görevi sayıları. Kaynak: docs/tasarim/18-kul-ejderi.md (KE-5.8..5.13, KE-6.8..6.13, KE-7.6..7.11, KE-9.3, KE-10.2/10.3).
    /// "(tahmin)" yazanların kaynakta değeri yok; oynanarak bulunur.
    /// </summary>
    public static class TeamTaskDefaults
    {
        // Uyku Mührü (KE-5.9, KE-5.10)
        public const double SleepSealDurationSec = 15.0;
        public const double SleepSealHoldSec = 2.0;
        public const float StoneCircleRadiusM = 2f;

        // Kor Mührü (KE-7.7, KE-7.8)
        public const double HeartSealDurationSec = 10.0;
        public const double HeartSealHoldSec = 3.0;
        // KE-7.6 sabit döngü: boss'un faz 3 döngüsüyle tek kaynak
        public const double HeartSealFirstAtPhaseSec = Dovus.Core.Boss.KulEjderiDefaults.Phase3CycleStartMs / 1000.0;
        public const double HeartSealRepeatSec = Dovus.Core.Boss.KulEjderiDefaults.Phase3CycleMs / 1000.0;

        // Kül Yağmuru (KE-6.9, KE-6.10, KE-6.11)
        public const double AshRainDurationSec = 8.0;
        public const float ShadowStripWidthM = 3f;
        public const float ShadowStripLengthM = 6f;

        // Görev başlama eşikleri (boss can oranı; KE-5.8, KE-6.8)
        public const float SleepSealHpFirst = 0.85f;
        public const float SleepSealHpSecond = 0.70f;
        public const float AshRainHpFirst = 0.50f;
        public const float AshRainHpSecond = 0.35f;
        public const float PhaseThreeHpRatio = 0.25f;

        // Ceza (KE-9.3)
        public const float PenaltyNormalHpFraction = 0.40f;
        public const float PenaltyHardHpFraction = 0.70f;
        public const float PenaltyHardHeartSealBossHpRatio = 0.25f;

        // Yapay zekâ dost (KE-10.2, KE-10.3)
        public const double AllyReactionDelaySec = 0.3;
        public const int AllyLateEveryNth = 5; // her 5 ön işaretten 1'i
        /// <summary>(tahmin) Geç kalan ön işarette ek bekleme; kaynakta yok.</summary>
        public const double AllyLateExtraDelaySec = 0.6;
        /// <summary>(tahmin) Alanın dışına çıkış payı; kaynakta yok.</summary>
        public const float AllyExitMarginM = 0.5f;
        /// <summary>(tahmin) Duyurulu saldırıda dodge, vuruşa bu kadar kala basılır; kaynakta yok (dodge hasarsızlığı 0,26 s, O-4.6).</summary>
        public const double AllyDodgeLeadSec = 0.15;
        /// <summary>(tahmin) Oyuncuya bu mesafeden uzaksa yaklaşır; kaynakta yok.</summary>
        public const float AllyFollowDistanceM = 6f;
    }
}
