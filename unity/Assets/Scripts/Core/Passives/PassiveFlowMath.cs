using System;
using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Passives
{
    /// <summary>
    /// Akış: vuruş channel_sec boyunca tik bırakır. Oran mechanic_grammar.params.flow_tick_fraction.
    /// </summary>
    public static class PassiveFlowMath
    {
        public const float DefaultTickFraction = 0.33f;

        public static bool TryPlan(
            float channelSec,
            float tickRateMult,
            float dealtDamage,
            float baseTickSec,
            float tickFraction,
            out PassiveFlowPlan plan)
        {
            plan = default;
            if (channelSec <= 0f || dealtDamage <= 0f)
                return false;
            float rate = tickRateMult > SlotPassiveCombatDefaults.MinTickSec ? tickRateMult : 1f;
            float tickSec = (baseTickSec > SlotPassiveCombatDefaults.MinTickSec ? baseTickSec : 1f) / rate;
            if (tickSec < SlotPassiveCombatDefaults.MinTickSec)
                tickSec = SlotPassiveCombatDefaults.MinTickSec;
            int count = 0;
            double t = tickSec;
            while (t < channelSec - 0.0001d && count < SlotPassiveCombatDefaults.ChannelTickGuardMax)
            {
                count++;
                t += tickSec;
            }
            if (count <= 0)
                return false;
            float fraction = tickFraction > 0f ? tickFraction : DefaultTickFraction;
            float tickDamage = dealtDamage * fraction;
            if (tickDamage <= 0f)
                return false;
            plan = new PassiveFlowPlan(channelSec, tickSec, tickDamage, count);
            return true;
        }
    }
}
