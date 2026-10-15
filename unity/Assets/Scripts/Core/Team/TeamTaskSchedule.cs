using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    /// <summary>Ne zaman hangi görev başlar: boss can eşiği ya da faz süresi (KE-5.8, KE-6.8, KE-7.6).</summary>
    public sealed class TeamTaskTrigger
    {
        TeamTaskTrigger(TeamTaskKind kind, float hpBelow, double firstAtPhaseSec, double repeatSec)
        {
            Kind = kind; HpBelow = hpBelow; FirstAtPhaseSec = firstAtPhaseSec; RepeatSec = repeatSec;
        }

        public TeamTaskKind Kind { get; }
        public float HpBelow { get; }
        public double FirstAtPhaseSec { get; }
        public double RepeatSec { get; }
        internal bool Fired { get; set; }
        internal double NextAtPhaseSec { get; set; }

        public static TeamTaskTrigger AtHp(TeamTaskKind kind, float hpBelow) => new TeamTaskTrigger(kind, hpBelow, -1.0, 0.0);

        public static TeamTaskTrigger EveryPhaseSec(TeamTaskKind kind, double firstAtSec, double repeatSec) =>
            new TeamTaskTrigger(kind, 0f, firstAtSec, repeatSec) { NextAtPhaseSec = firstAtSec };
    }

    /// <summary>Görev zamanlayıcısı. Bir görev sürerken yenisi başlamaz; kaçan tetik görev bitince başlar.</summary>
    public sealed class TeamTaskSchedule
    {
        readonly List<TeamTaskTrigger> _triggers;

        public TeamTaskSchedule(IEnumerable<TeamTaskTrigger> triggers)
        {
            _triggers = new List<TeamTaskTrigger>(triggers ?? Array.Empty<TeamTaskTrigger>());
        }

        /// <summary>Kül Ejderi: Uyku Mührü %85/%70, Kül Yağmuru %50/%35, Kor Mührü faz 3'te 5. sn sonra her 40 sn.</summary>
        public static TeamTaskSchedule KulEjderi() => new TeamTaskSchedule(new[]
        {
            TeamTaskTrigger.AtHp(TeamTaskKind.SleepSeal, TeamTaskDefaults.SleepSealHpFirst),
            TeamTaskTrigger.AtHp(TeamTaskKind.SleepSeal, TeamTaskDefaults.SleepSealHpSecond),
            TeamTaskTrigger.AtHp(TeamTaskKind.AshRain, TeamTaskDefaults.AshRainHpFirst),
            TeamTaskTrigger.AtHp(TeamTaskKind.AshRain, TeamTaskDefaults.AshRainHpSecond),
            TeamTaskTrigger.EveryPhaseSec(TeamTaskKind.HeartSeal, TeamTaskDefaults.HeartSealFirstAtPhaseSec, TeamTaskDefaults.HeartSealRepeatSec),
        });

        /// <param name="hpRatio">Boss can oranı 0..1.</param>
        /// <param name="phaseSec">Faz 3'ün başından beri geçen süre; faz 3'te değilse eksi.</param>
        /// <param name="busy">Bir görev sürüyor ya da boss büyük saldırı duyuruyor mu.</param>
        public TeamTaskKind? Poll(float hpRatio, double phaseSec, bool busy)
        {
            if (busy) return null;
            for (int i = 0; i < _triggers.Count; i++)
            {
                var t = _triggers[i];
                if (t.FirstAtPhaseSec >= 0.0)
                {
                    if (phaseSec < 0.0 || phaseSec < t.NextAtPhaseSec) continue;
                    t.NextAtPhaseSec = phaseSec + t.RepeatSec; // tetik ne zaman işlerse, süre oradan sayılır
                    return t.Kind;
                }
                if (t.Fired || hpRatio > t.HpBelow) continue;
                t.Fired = true;
                return t.Kind;
            }
            return null;
        }
    }
}
