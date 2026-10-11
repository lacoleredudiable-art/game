using System;
using System.Collections.Generic;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Dilim PR1: aynı rün hem fiil hem sıfat (K 07:42).</summary>
    public static class RuleEngineV4Slice
    {
        static int[] _runes = Array.Empty<int>();

        public static bool IsSliceCombo(int verbRune, int adjectiveRune)
        {
            foreach (int r in _runes)
            {
                if (r != verbRune)
                    continue;
                foreach (int a in _runes)
                {
                    if (a == adjectiveRune)
                        return true;
                }
            }
            return false;
        }

        public static bool IsSliceWeapon(int weaponId)
        {
            foreach (int w in Weapons)
            {
                if (w == weaponId)
                    return true;
            }
            return false;
        }

        /// <summary>kural-motoru-v4.json slice_runes; katalog okununca bağlanır, öncesinde boş.</summary>
        public static IReadOnlyList<int> Runes => _runes;

        internal static void BindRunes(int[] runes) => _runes = runes ?? Array.Empty<int>();

        // Çekiç (6) ilk aşamadan çıktı; geri açmak: { 2, 4, 6 }
        public static readonly int[] Weapons = { 2, 4 };
    }
}
