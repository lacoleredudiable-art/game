using System;

namespace Dovus.Core.Shared
{
    /// <summary><see cref="System.Random"/> sarmalayıcı; paylaşılan veya tohumlu hasar sapması için.</summary>
    public sealed class SeededRng : IRng
    {
        readonly Random _rng;

        SeededRng(Random rng) => _rng = rng ?? throw new ArgumentNullException(nameof(rng));

        public static SeededRng Unseeded() => new SeededRng(new Random());

        public static SeededRng Seeded(int seed) => new SeededRng(new Random(seed));

        public double NextDouble() => _rng.NextDouble();
    }
}
