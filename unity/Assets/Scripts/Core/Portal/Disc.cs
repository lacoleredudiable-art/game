using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core.Motion;
using Dovus.Core.Status;

namespace Dovus.Core.Portal
{
    public readonly struct Disc
    {
        public Disc(bool present, float x, float z, float radius, float gap)
        {
            Present = present;
            X = x;
            Z = z;
            Radius = radius > 0f ? radius : 0f;
            Gap = gap > 0f ? gap : 0f;
        }

        public bool Present { get; }
        public float X { get; }
        public float Z { get; }
        public float Radius { get; }
        public float Gap { get; }

        public static Disc None => new Disc(false, 0f, 0f, 0f, 0f);
    }
}
