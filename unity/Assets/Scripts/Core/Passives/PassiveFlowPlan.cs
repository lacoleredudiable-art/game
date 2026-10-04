using System;
using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Passives
{
    public readonly struct PassiveFlowPlan
    {
        public PassiveFlowPlan(float durationSec, float tickSec, float tickDamage, int tickCount)
        {
            DurationSec = durationSec;
            TickSec = tickSec;
            TickDamage = tickDamage;
            TickCount = tickCount;
        }

        public float DurationSec { get; }
        public float TickSec { get; }
        public float TickDamage { get; }
        public int TickCount { get; }
        public float TotalDamage => TickDamage * TickCount;
    }
}
