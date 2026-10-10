namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Dilim PR1: aynı rün hem fiil hem sıfat (K 07:42).</summary>
    public static class RuleEngineV4Slice
    {
        public static bool IsSliceCombo(int verbRune, int adjectiveRune)
        {
            foreach (int r in Runes)
            {
                if (r != verbRune)
                    continue;
                foreach (int a in Runes)
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

        public static readonly int[] Runes = { 1, 2, 3, 4, 6, 9 };
        // Çekiç (6) ilk aşamadan çıktı; geri açmak: { 2, 4, 6 }
        public static readonly int[] Weapons = { 2, 4 };
    }
}
