using System;
using System.Collections.Generic;

namespace Dovus.App.Sweep
{
    /// <summary>Play Sweep vaka listesi sırası (144 kombo × silah); Unity'siz.</summary>
    public static class SweepComboCatalog
    {
        public static readonly int[][] RuneGroups =
        {
            new[] { 1, 2, 3 }, new[] { 4, 5, 6 }, new[] { 7, 8, 9 }, new[] { 10, 11, 12 },
        };

        public static int BuildKey(int v, int a)
        {
            int gv = (v - 1) / 3;
            int ga = (a - 1) / 3;
            if (gv == ga)
                ga = (gv + 1) % 4;
            int lo = Math.Min(gv, ga);
            int hi = Math.Max(gv, ga);
            return lo * 4 + hi;
        }

        public static List<SweepComboEntry> OrderedCombos(string weapon, string secondWeapon = "", float startDistM = SweepDefaults.DefaultStartDistM)
        {
            var list = new List<SweepComboEntry>();
            foreach (string w in new[] { weapon, secondWeapon })
            {
                if (string.IsNullOrEmpty(w))
                    continue;
                for (int v = 1; v <= 12; v++)
                for (int a = 1; a <= 12; a++)
                    list.Add(new SweepComboEntry(v, a, w, startDistM));
            }

            list.Sort((a, b) =>
            {
                int cmp = string.Equals(a.Weapon, weapon, StringComparison.Ordinal) ? 0 : 1;
                int cmpB = string.Equals(b.Weapon, weapon, StringComparison.Ordinal) ? 0 : 1;
                if (cmp != cmpB)
                    return cmp.CompareTo(cmpB);
                cmp = BuildKey(a.Verb, a.Adj).CompareTo(BuildKey(b.Verb, b.Adj));
                if (cmp != 0)
                    return cmp;
                cmp = a.Verb.CompareTo(b.Verb);
                if (cmp != 0)
                    return cmp;
                return a.Adj.CompareTo(b.Adj);
            });
            return list;
        }
    }
}
