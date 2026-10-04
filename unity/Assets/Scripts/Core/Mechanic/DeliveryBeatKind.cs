using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;

using Dovus.Core.Shared;
namespace Dovus.Core.Mechanic
{
    public enum DeliveryBeatKind
    {
        SpawnActors = 1,
        Resolve = 2,
        Detonate = 3,
        Duplicate = 4,
        RepeatPrevious = 5,
        FieldTick = 6,
        Bounce = 7,
        GlideHaste = 8,
        Pincer = 9
    }
}
