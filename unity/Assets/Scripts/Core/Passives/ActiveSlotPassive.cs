using System;
using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Passives
{
    public readonly struct ActiveSlotPassive
    {
        public ActiveSlotPassive(
            int runeId,
            string name,
            double sinceMs,
            double untilMs,
            JsonValue modifiers,
            int excludedCastId = 0)
        {
            RuneId = runeId;
            Name = name ?? string.Empty;
            SinceMs = sinceMs;
            UntilMs = untilMs;
            Modifiers = modifiers;
            ExcludedCastId = excludedCastId;
        }

        public int RuneId { get; }
        public string Name { get; }
        public double SinceMs { get; }
        public double UntilMs { get; }
        public JsonValue Modifiers { get; }
        public int ExcludedCastId { get; }
        public float RemainingSec(double worldMs) =>
            (float)(Math.Max(0.0, UntilMs - worldMs) / Units.SecToMs);
    }
}
