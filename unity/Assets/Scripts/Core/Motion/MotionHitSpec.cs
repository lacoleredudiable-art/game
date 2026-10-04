using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Motion
{
    public sealed class MotionHitSpec
    {
        public MotionHitSpec(
            string shape,
            string anchor,
            float lengthM,
            float radiusM,
            float at,
            float share,
            string payload,
            float everySec)
        {
            Shape = shape ?? "capsule";
            Anchor = anchor ?? "forward";
            LengthM = lengthM;
            RadiusM = radiusM;
            At = at;
            Share = share;
            Payload = payload ?? "damage";
            EverySec = everySec;
        }

        public string Shape { get; }
        public string Anchor { get; }
        public float LengthM { get; }
        public float RadiusM { get; }
        public float At { get; }
        public float Share { get; }
        public string Payload { get; }
        public float EverySec { get; }
    }
}
