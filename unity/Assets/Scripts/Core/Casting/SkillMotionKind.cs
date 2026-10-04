using System;
using Dovus.Core.Grammar;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Casting
{
    public enum SkillMotionKind : byte
    {
        None = 0,
        ShortBlink,
        ForwardDash,
        ZenitsuPass,
        PlaceMark
    }
}
