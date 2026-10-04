using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;

using Dovus.Core.Shared;
namespace Dovus.Core.Mechanic
{
    /// <summary>Kalıp başladıktan sonra çalışacak tek teslim adımı. Skill kimliği yok.</summary>
    public readonly struct DeliveryBeat
    {
        public DeliveryBeat(double atSec, DeliveryBeatKind kind, float power, int count)
        {
            AtSec = atSec;
            Kind = kind;
            Power = power;
            Count = count;
        }

        public double AtSec { get; }
        public DeliveryBeatKind Kind { get; }
        public float Power { get; }
        public int Count { get; }
    }
}
