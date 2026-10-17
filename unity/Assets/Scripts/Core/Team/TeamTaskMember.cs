using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    /// <summary>Görevin her tick'te gördüğü kişi (oyuncu ya da dost; ağ oyuncusu da aynı kapı).</summary>
    public readonly struct TeamTaskMember
    {
        public TeamTaskMember(int id, float x, float z, bool alive)
        {
            Id = id; X = x; Z = z; Alive = alive;
        }

        public int Id { get; }
        public float X { get; }
        public float Z { get; }
        public bool Alive { get; }
    }
}
