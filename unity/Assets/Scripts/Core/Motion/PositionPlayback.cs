using System;
using System.Collections.Generic;
using Dovus.Core;

namespace Dovus.Core.Motion
{
    /// <summary>Bu cast'te kalıbın oynatacağı gövde. Konum adımı yoksa kalıbın kendisidir.</summary>
    public readonly struct PositionPlayback
    {
        public PositionPlayback(bool ownsPosition, MotionTemplate template, bool placeReturnMark)
        {
            OwnsPosition = ownsPosition;
            Template = template;
            PlaceReturnMark = placeReturnMark;
        }

        public bool OwnsPosition { get; }
        public MotionTemplate Template { get; }
        public bool PlaceReturnMark { get; }
    }
}
