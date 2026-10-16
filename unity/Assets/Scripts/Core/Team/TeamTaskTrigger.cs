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
}
