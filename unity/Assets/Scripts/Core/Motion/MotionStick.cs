using System;
using System.Collections.Generic;
using Dovus.Core;

namespace Dovus.Core.Motion
{
    public readonly struct MotionStick
    {
        public MotionStick(bool held, float moveX, float moveZ)
        {
            Held = held;
            MoveX = moveX;
            MoveZ = moveZ;
        }

        public bool Held { get; }
        public float MoveX { get; }
        public float MoveZ { get; }
    }
}
