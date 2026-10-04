using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core.Motion;
using Dovus.Core.Status;

namespace Dovus.Core.Portal
{
    public readonly struct BackStrike
    {
        public BackStrike(bool active, float x, float z, float dirX, float dirZ)
        {
            Active = active;
            X = x;
            Z = z;
            DirX = dirX;
            DirZ = dirZ;
        }

        public bool Active { get; }
        public float X { get; }
        public float Z { get; }
        public float DirX { get; }
        public float DirZ { get; }
    }
}
