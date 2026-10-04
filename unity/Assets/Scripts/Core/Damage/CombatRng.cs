using System;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Damage
{
    /// <summary>
    /// O7: oturum ve tarama tohumu (<see cref="SessionSeed"/>, <see cref="SweepSeed"/>). Tek ve kalıcı savaş zarı
    /// (kritik + ±%5 sapma). Ardışık tohumla her vuruşa yeni Random açmak ilişkili ilk değerler üretiyordu.
    /// IRng sarmalayıcısı (Shared/SeededRng) ile karıştırılmaz.
    /// </summary>
    public sealed class CombatRng
    {
        /// <summary>Test ve tarama tohumu (deterministik).</summary>
        public const int SweepSeed = 20261001;

        Random _rng;

        public CombatRng(int seed)
        {
            Reseed(seed);
        }

        public int Seed { get; private set; }

        public void Reseed(int seed)
        {
            Seed = seed;
            _rng = new Random(seed);
        }

        public float NextRoll01() => (float)_rng.NextDouble();

        /// <summary>Oyun oturumu için gerçek rastgele tohum.</summary>
        public static int SessionSeed() => Guid.NewGuid().GetHashCode() ^ Environment.TickCount;
    }
}
