using System;

namespace Dovus.Core.Shared
{
    /// <summary><see cref="System.Random"/> sarmalayıcı; paylaşılan veya tohumlu hasar sapması için.</summary>
    public sealed class CombatRng : IRng
    {
        readonly Random _rng;

        CombatRng(Random rng) => _rng = rng ?? throw new ArgumentNullException(nameof(rng));

        public static CombatRng Unseeded() => new CombatRng(new Random());

        public static CombatRng Seeded(int seed) => new CombatRng(new Random(seed));

        public double NextDouble() => _rng.NextDouble();
    }
}
