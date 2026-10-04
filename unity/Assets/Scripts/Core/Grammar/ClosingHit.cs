using System.Collections.Generic;
using Dovus.Core.Input;
using Dovus.Core.Element;

namespace Dovus.Core.Grammar
{
    /// <summary>Başarılı kapanış — tür son rüne, ödül uzunluğa bağlı (§5).</summary>
    public readonly struct ClosingHit
    {
        public ClosingHit(Rune type, float totalEffect, int dotCount)
        {
            Type = type;
            TotalEffect = totalEffect;
            DotCount = dotCount;
        }

        public Rune Type { get; }
        public float TotalEffect { get; }
        public int DotCount { get; }
    }
}
