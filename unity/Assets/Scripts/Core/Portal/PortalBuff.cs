using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core.Motion;
using Dovus.Core.Status;

namespace Dovus.Core.Portal
{
    public readonly struct PortalBuff
    {
        public PortalBuff(float move, float damage, float taken, float miss, bool narrow)
        {
            MoveSpeedMult = move;
            DamageMult = damage;
            DamageTakenMult = taken;
            MissChance = miss;
            NarrowStrikes = narrow;
        }

        public float MoveSpeedMult { get; }
        public float DamageMult { get; }
        public float DamageTakenMult { get; }
        public float MissChance { get; }
        public bool NarrowStrikes { get; }

        public static PortalBuff None => new PortalBuff(1f, 1f, 1f, 0f, false);
    }
}
