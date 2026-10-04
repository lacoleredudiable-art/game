using System;
using System.Collections.Generic;
using Dovus.Core;

namespace Dovus.Core.Motion
{
    /// <summary>Gramerin oyuncuyu oynatan bir konum adımı. Mesafe, kalıpta yoksa yedek veridir.</summary>
    public readonly struct GrammarPositionStep
    {
        public GrammarPositionStep(string stat, double amount)
        {
            Stat = stat ?? string.Empty;
            Amount = amount;
        }

        public string Stat { get; }
        public double Amount { get; }
    }
}
