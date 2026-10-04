using System;
using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Passives
{
    public sealed class PassiveFlowRunner
    {
        readonly List<LiveFlow> _items = new();

        public int ActiveCount => _items.Count;

        public void Start(PassiveFlowPlan plan, double worldMs)
        {
            if (plan.TickCount <= 0 || plan.TickDamage <= 0f)
                return;
            _items.Add(new LiveFlow
            {
                NextMs = worldMs + plan.TickSec * Units.SecToMs,
                IntervalMs = plan.TickSec * Units.SecToMs,
                Left = plan.TickCount,
                TickDamage = plan.TickDamage
            });
        }

        public float Collect(double worldMs)
        {
            float sum = 0f;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                LiveFlow item = _items[i];
                int guard = 0;
                while (item.Left > 0 && worldMs + 0.001d >= item.NextMs && guard < SlotPassiveCombatDefaults.ChannelTickGuardMax)
                {
                    sum += item.TickDamage;
                    item.Left--;
                    item.NextMs += item.IntervalMs;
                    guard++;
                }
                if (item.Left <= 0)
                    _items.RemoveAt(i);
                else
                    _items[i] = item;
            }
            return sum;
        }

        struct LiveFlow
        {
            public double NextMs;
            public double IntervalMs;
            public int Left;
            public float TickDamage;
        }
    }
}
