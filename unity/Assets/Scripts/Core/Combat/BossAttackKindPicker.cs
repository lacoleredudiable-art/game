using System;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Idle'da hangi saldırı (Slam / FireCone) seçilir. SlamVariantPicker ile aynı desen:
    /// aynısı üst üste maxSameStreak'i geçemez (§11/T13'ün "tek desen sanılır" dersi, artık
    /// saldırı TÜRÜ için de geçerli — 16 Eylül).
    /// </summary>
    public static class BossAttackKindPicker
    {
        static readonly BossAttackKind[] All = { BossAttackKind.Slam, BossAttackKind.FireCone };

        public static BossAttackKind Pick(
            BossAttackKind? last,
            int currentStreak,
            int maxSameStreak,
            Random rng)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            int cap = Math.Max(1, maxSameStreak);
            Span<BossAttackKind> buf = stackalloc BossAttackKind[2];
            int n = 0;

            for (int i = 0; i < All.Length; i++)
            {
                BossAttackKind k = All[i];
                if (last.HasValue && k == last.Value && currentStreak >= cap)
                    continue;
                buf[n++] = k;
            }

            if (n == 0)
                return All[rng.Next(All.Length)];

            return buf[rng.Next(n)];
        }

        public static int NextStreak(BossAttackKind? last, int currentStreak, BossAttackKind picked) =>
            last.HasValue && last.Value == picked ? currentStreak + 1 : 1;
    }
}
