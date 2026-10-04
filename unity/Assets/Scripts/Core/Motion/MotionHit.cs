using System;
using System.Collections.Generic;
using Dovus.Core;

namespace Dovus.Core.Motion
{
    public readonly struct MotionHit
    {
        public MotionHit(
            string phase,
            string shape,
            string anchor,
            string payload,
            float lengthM,
            float radiusM,
            float share,
            float originX,
            float originY,
            float originZ,
            float dirX,
            float dirZ,
            float timeSec)
        {
            Phase = phase ?? string.Empty;
            Shape = shape ?? string.Empty;
            Anchor = anchor ?? string.Empty;
            Payload = payload ?? string.Empty;
            LengthM = lengthM;
            RadiusM = radiusM;
            Share = share;
            OriginX = originX;
            OriginY = originY;
            OriginZ = originZ;
            DirX = dirX;
            DirZ = dirZ;
            TimeSec = timeSec;
        }

        public string Phase { get; }
        public string Shape { get; }
        public string Anchor { get; }
        public string Payload { get; }
        public float LengthM { get; }
        public float RadiusM { get; }
        public float Share { get; }
        public float OriginX { get; }
        public float OriginY { get; }
        public float OriginZ { get; }
        public float DirX { get; }
        public float DirZ { get; }
        public float TimeSec { get; }
    }
}
