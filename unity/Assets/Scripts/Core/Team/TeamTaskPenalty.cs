using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    /// <summary>Başarısız görevin cezası (KE-9.3, A2 kararı: ceza zorluğa göre).</summary>
    public readonly struct TeamTaskPenaltyResult
    {
        public TeamTaskPenaltyResult(bool wipe, float teamDamageFractionOfMax, float bossHpResetRatio, bool exposedDie, string message)
        {
            Wipe = wipe;
            TeamDamageFractionOfMax = teamDamageFractionOfMax;
            BossHpResetRatio = bossHpResetRatio;
            ExposedDie = exposedDie;
            Message = message;
        }

        /// <summary>Tur yanar: bütün takım ölür (Cehennem).</summary>
        public bool Wipe { get; }

        /// <summary>Bütün takımın azami canından yiyeceği hasar oranı (0 = yok).</summary>
        public float TeamDamageFractionOfMax { get; }

        /// <summary>&gt;0 ise boss canı bu orana döner (Zor, Kor Mührü: 0,25). 0 = dokunma.</summary>
        public float BossHpResetRatio { get; }

        /// <summary>Kül Yağmuru'nda şerit dışındakiler ölür mü (Antrenman'da can 1'de durur, KE-6.13).</summary>
        public bool ExposedDie { get; }

        public string Message { get; }
    }

    public static class TeamTaskPenalty
    {
        public static TeamTaskPenaltyResult Resolve(TeamTaskKind kind, TeamDifficulty difficulty)
        {
            bool exposedDie = kind == TeamTaskKind.AshRain && difficulty != TeamDifficulty.Training;
            switch (difficulty)
            {
                case TeamDifficulty.Training:
                    return new TeamTaskPenaltyResult(false, 0f, 0f, false, "Görev kaçtı — " + Hint(kind));
                case TeamDifficulty.Normal:
                    return new TeamTaskPenaltyResult(false, TeamTaskDefaults.PenaltyNormalHpFraction, 0f, exposedDie, "Görev kaçtı");
                case TeamDifficulty.Hard:
                    float reset = kind == TeamTaskKind.HeartSeal ? TeamTaskDefaults.PenaltyHardHeartSealBossHpRatio : 0f;
                    return new TeamTaskPenaltyResult(false, TeamTaskDefaults.PenaltyHardHpFraction, reset, exposedDie, "Görev kaçtı");
                default:
                    return new TeamTaskPenaltyResult(true, 1f, 0f, exposedDie, "Tur yandı");
            }
        }

        /// <summary>Takım hasarı: azami can × oran, yuvarlanır. Antrenman'da (oran 0) hasar yok.</summary>
        public static int TeamDamage(int maxHp, TeamTaskPenaltyResult p)
        {
            if (p.TeamDamageFractionOfMax <= 0f) return 0;
            return (int)Math.Round(Math.Max(0, maxHp) * p.TeamDamageFractionOfMax, MidpointRounding.AwayFromZero);
        }

        static string Hint(TeamTaskKind kind)
        {
            switch (kind)
            {
                case TeamTaskKind.SleepSeal: return "her taşta 2 saniye durup yakmalıydınız";
                case TeamTaskKind.HeartSeal: return "üç taşta aynı anda durmalıydınız";
                default: return "bir taşın arkasına geçmeliydiniz";
            }
        }
    }
}
