using System;
using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Passives
{
    public readonly struct PassiveBounceHit
    {
        public PassiveBounceHit(int targetId, float damage)
        {
            TargetId = targetId;
            Damage = damage;
        }

        public int TargetId { get; }
        public float Damage { get; }
    }
}
