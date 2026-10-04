using System;

namespace Dovus.Core.Shared
{
    /// <summary>
    /// <see cref="System.Random"/> sarmalayıcı (<see cref="IRng"/>). Oturum/tarama tohumu Damage/CombatRng içinde;
    /// bu tip yalnız paylaşımlı veya sabit tohumlu sapma içindir.
    /// </summary>
    public sealed class SeededRng : IRng
    {
        readonly Random _rng;

        SeededRng(Random rng) => _rng = rng ?? throw new ArgumentNullException(nameof(rng));

        public static SeededRng Seeded(int seed) => new SeededRng(new Random(seed));

        public double NextDouble() => _rng.NextDouble();
    }
}
