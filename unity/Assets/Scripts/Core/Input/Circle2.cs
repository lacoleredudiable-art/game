using System;

namespace Dovus.Core.Input
{
    public readonly struct Circle2
    {
        public Circle2(float x, float y, float radius)
        {
            X = x;
            Y = y;
            Radius = radius;
        }

        public float X { get; }
        public float Y { get; }
        public float Radius { get; }
    }
}
