using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    public readonly struct AllyTarget
    {
        public AllyTarget(int id, float x, float z) { Id = id; X = x; Z = z; }
        public int Id { get; }
        public float X { get; }
        public float Z { get; }
    }
}
