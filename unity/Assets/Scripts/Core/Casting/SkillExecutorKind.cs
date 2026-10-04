using System;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;

namespace Dovus.Core.Casting
{
    public enum SkillExecutorKind
    {
        Fallback,
        MeleeHitbox,
        Projectile,
        FieldAura,
        Movement,
        SelfState,
        Summon
    }
}
