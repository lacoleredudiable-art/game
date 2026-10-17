using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    /// <summary>Boss'un duyurduğu tehlike alanı (daire). Konilere dünya, kapsayan daireyle yaklaşır.</summary>
    public readonly struct AllyDanger
    {
        public AllyDanger(int id, float x, float z, float radius, double hitInSec, bool arenaWide)
        {
            Id = id; X = x; Z = z; Radius = radius; HitInSec = hitInSec; ArenaWide = arenaWide;
        }

        public int Id { get; }
        public float X { get; }
        public float Z { get; }
        public float Radius { get; }
        /// <summary>Vuruşa kalan süre (sn).</summary>
        public double HitInSec { get; }
        /// <summary>Bütün arena (Kor Dalgası gibi): dışarı çıkılamaz; taş gölgesi ya da dodge.</summary>
        public bool ArenaWide { get; }
    }
}
