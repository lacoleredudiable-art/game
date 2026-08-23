using System;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Idle'da varyant seçimi. Aynı ritmin üst üste MaxSameVariantStreak kez gelmesi
    /// engellenir — aksi halde oyuncu tek desen sanır (§11 / T13).
    /// </summary>
    public static class SlamVariantPicker
    {
        static readonly SlamVariant[] All =
        {
            SlamVariant.Yakin,
            SlamVariant.Gec,
            SlamVariant.Genis
        };

        public static SlamVariant Pick(
            SlamVariant? last,
            int currentStreak,
            int maxSameStreak,
            Random rng)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            int cap = Math.Max(1, maxSameStreak);
            Span<SlamVariant> buf = stackalloc SlamVariant[3];
            int n = 0;

            for (int i = 0; i < All.Length; i++)
            {
                SlamVariant v = All[i];
                if (last.HasValue && v == last.Value && currentStreak >= cap)
                    continue;
                buf[n++] = v;
            }

            if (n == 0)
            {
                // Cap 0 veya bozulmuş streak: yine de birini seç.
                return All[rng.Next(All.Length)];
            }

            return buf[rng.Next(n)];
        }

        public static int NextStreak(SlamVariant? last, int currentStreak, SlamVariant picked) =>
            last.HasValue && last.Value == picked ? currentStreak + 1 : 1;
    }
}
