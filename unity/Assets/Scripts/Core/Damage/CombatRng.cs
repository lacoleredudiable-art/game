using System;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Damage
{
    /// <summary>
    /// O7: tek ve kalıcı savaş zarı (kritik + ±%5 sapma). Ardışık tohumla her vuruşa yeni Random
    /// açmak ilişkili ilk değerler üretiyordu (ilk kritik hep 15. vuruş). Oyunda oturum başına gerçek
    /// rastgele tohum; test ve taramalarda sabit tohum (<see cref="SweepSeed"/>).
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
