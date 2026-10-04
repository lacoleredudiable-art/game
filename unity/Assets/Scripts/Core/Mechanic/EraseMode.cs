using System;
using System.Linq;

namespace Dovus.Core.Mechanic
{
    public enum EraseMode : byte
    {
        None,
        Delete,
        Absorb,
        Reflect,
        Shroud,
        Targeted
    }
}
