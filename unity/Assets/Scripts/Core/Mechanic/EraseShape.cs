using System;
using System.Linq;

namespace Dovus.Core.Mechanic
{
    public enum EraseShape : byte
    {
        None,
        /// <summary>Hacim diski (merkez sabit).</summary>
        Disk,
        /// <summary>Oyuncu↔dost bağ şeridi (bag_hatti).</summary>
        Segment,
        /// <summary>Oyuncudan nişan boyunca delici silme hattı (delici).</summary>
        Line,
        /// <summary>Merkez her kare oyuncuyu izler (surekli_perde).</summary>
        Follow
    }
}
