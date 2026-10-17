using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
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
