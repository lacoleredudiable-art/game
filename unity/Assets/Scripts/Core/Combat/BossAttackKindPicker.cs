using System;
using System.Collections.Generic;
using Dovus.Core.Tuning;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Idle'da hangi saldırı seçilir. SlamVariantPicker ile aynı desen: aynısı üst üste
    /// maxSameStreak'i geçemez (§11/T13'ün "tek desen sanılır" dersi, saldırı TÜRÜ için de).
    /// İzinli küme çağırandan gelir: faz 1 = {Slam, Volley}, öfke = {Slam, FireCone, Volley}
    /// (karadul.json fazları); CC kapısının kestiği türler çağıranda zaten elenir.
    /// </summary>
    public static class BossAttackKindPicker
    {
        static readonly BossAttackKind[] Calm = { BossAttackKind.Slam, BossAttackKind.Volley };
        static readonly BossAttackKind[] Enraged = { BossAttackKind.Slam, BossAttackKind.FireCone, BossAttackKind.Volley };

        /// <summary>karadul.json faz listesi: faz 1 Slam + Volley, faz 2 (öfke) FireCone da açılır.</summary>
        public static ReadOnlySpan<BossAttackKind> AllowedFor(bool enraged) => enraged ? Enraged : Calm;

        public static BossAttackKind Pick(
            BossAttackKind? last,
            int currentStreak,
            int maxSameStreak,
            Random rng,
            ReadOnlySpan<BossAttackKind> allowed)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));
            if (allowed.IsEmpty)
                throw new ArgumentException("allowed set is empty", nameof(allowed));

            int cap = Math.Max(1, maxSameStreak);
            Span<BossAttackKind> buf = stackalloc BossAttackKind[allowed.Length];
            int n = 0;

            for (int i = 0; i < allowed.Length; i++)
            {
                BossAttackKind k = allowed[i];
                if (last.HasValue && k == last.Value && currentStreak >= cap)
                    continue;
                buf[n++] = k;
            }

            if (n == 0)
                return allowed[rng.Next(allowed.Length)];

            return buf[rng.Next(n)];
        }

        public static int NextStreak(BossAttackKind? last, int currentStreak, BossAttackKind picked) =>
            last.HasValue && last.Value == picked ? currentStreak + 1 : 1;

        /// <summary>Boss JSON saldırı id'lerini enum'a çevirir; bilinmeyen id atlanır.</summary>
        public static BossAttackKind[] FromIds(IReadOnlyList<string> kinds)
        {
            if (kinds == null || kinds.Count == 0)
                return Array.Empty<BossAttackKind>();

            var buf = new List<BossAttackKind>(kinds.Count);
            for (int i = 0; i < kinds.Count; i++)
            {
                if (TryMapId(kinds[i], out BossAttackKind k))
                    buf.Add(k);
            }
            return buf.ToArray();
        }

        static bool TryMapId(string id, out BossAttackKind kind)
        {
            switch (id)
            {
                case "slam":
                    kind = BossAttackKind.Slam;
                    return true;
                case "fire_cone":
                    kind = BossAttackKind.FireCone;
                    return true;
                case "volley":
                    kind = BossAttackKind.Volley;
                    return true;
                case "web_field":
                    kind = BossAttackKind.WebField;
                    return true;
                case "pounce":
                    kind = BossAttackKind.Pounce;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        /// <summary>Sıçrayış seçimi: hedef boss'tan PounceMinRangeM–PounceMaxRangeM arasında mı?</summary>
        public static bool PounceInRange(float distM, BossTuning t) =>
            t != null && distM >= t.PounceMinRangeM && distM <= t.PounceMaxRangeM;
    }
}
