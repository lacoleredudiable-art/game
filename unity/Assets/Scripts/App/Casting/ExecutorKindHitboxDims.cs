using Dovus.Core.Combat;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using System;

namespace Dovus.App.Casting
{
    /// <summary>Executor türüne göre hitbox boyutlarının alan/range/süre/spawn sayısına uygulanması.</summary>
    public static class ExecutorKindHitboxDims
    {
        public static void Apply(
            SkillExecutorKind kind,
            in VerbHitboxSpec spec,
            in HitboxSize size,
            float dashDurationSec,
            float reflectDurationSec,
            float minionDurationSec,
            float lifetimeAdd,
            float slotLifeSec,
            int engineMinionCount,
            ref float radius,
            ref float range,
            ref float durationSec,
            ref int spawnCount)
        {
            switch (kind)
            {
                case SkillExecutorKind.MeleeHitbox:
                    range = size.ReachM;
                    radius = size.RadiusM;
                    break;

                case SkillExecutorKind.Projectile:
                    radius = size.RadiusM;
                    range = size.ReachM;
                    break;

                case SkillExecutorKind.FieldAura:
                    radius = spec.IsRadius ? size.RadiusM : size.ReachM;
                    range = size.ReachM;
                    durationSec += slotLifeSec;
                    break;

                case SkillExecutorKind.Movement:
                    radius = size.RadiusM;
                    durationSec = Math.Max(size.DurationSec, dashDurationSec);
                    break;

                case SkillExecutorKind.SelfState:
                    radius = size.RadiusM;
                    range = size.ReachM;
                    if (reflectDurationSec > 0f)
                        durationSec = reflectDurationSec + lifetimeAdd;
                    break;

                case SkillExecutorKind.Summon:
                    radius = size.RadiusM;
                    range = size.ReachM;
                    if (minionDurationSec > 0f)
                        durationSec = minionDurationSec + lifetimeAdd;
                    spawnCount = Math.Max(1, engineMinionCount);
                    break;
            }
        }
    }
}
