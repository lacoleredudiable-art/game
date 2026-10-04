using System.Collections.Generic;
using Dovus.Core.Input;
using Dovus.Core.Element;

namespace Dovus.Core.Grammar
{
    /// <summary>Cümle içindeki bir kelime (fiil veya sıfat) + dwell yoğunluğu.</summary>
    public readonly struct SentenceWord
    {
        public SentenceWord(Rune rune, JumpKind jumpFromPrevious, int intensityStacks)
            : this((int)rune, rune, jumpFromPrevious, intensityStacks)
        {
        }

        public SentenceWord(int slot, Rune rune, JumpKind jumpFromPrevious, int intensityStacks)
        {
            Slot = slot;
            Rune = rune;
            JumpFromPrevious = jumpFromPrevious;
            IntensityStacks = intensityStacks;
        }

        /// <summary>Altıgen ekran slotu (1..6).</summary>
        public int Slot { get; }
        public Rune Rune { get; }
        /// <summary>Geriye uyumlu ad: artık rün id'si değil ekran slotudur.</summary>
        public int Dot => Slot;
        public JumpKind JumpFromPrevious { get; }
        public int IntensityStacks { get; }
    }
}
