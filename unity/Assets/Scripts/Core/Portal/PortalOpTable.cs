using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core.Grammar;

namespace Dovus.Core.Portal
{
    public static class PortalOpTable
    {
        public static IReadOnlyDictionary<string, PortalOp> Legacy { get; } = BuildLegacy();

        static Dictionary<string, PortalOp> BuildLegacy() =>
            new Dictionary<string, PortalOp>(StringComparer.Ordinal)
            {
                [SkillIds.MirrorStrike] = PortalOp.BackDoor,
                [SkillIds.OpeningHeal] = PortalOp.Hook,
                [SkillIds.FixedStep] = PortalOp.AnchorOrRecall,
                [SkillIds.MirrorStep] = PortalOp.Pair,
                [SkillIds.DenseAscent] = PortalOp.Gate,
                [SkillIds.RisingAscent] = PortalOp.MirrorGate,
                [SkillIds.MirrorPurify] = PortalOp.Swap,
                [SkillIds.MirrorReflect] = PortalOp.Mirror,
                [SkillIds.RisingSummon] = PortalOp.Sink,
                [SkillIds.MirrorSummon] = PortalOp.GatherTeam,
            };

        public static bool TryLegacy(SkillId skillId, out PortalOp op) =>
            Legacy.TryGetValue(skillId.Value, out op);

        public static PortalOp Resolve(SkillId skillId, IReadOnlyDictionary<string, PortalOp> table)
        {
            if (table != null && table.TryGetValue(skillId.Value, out PortalOp op))
                return op;
            return PortalOp.None;
        }

        public static Dictionary<string, PortalOp> FromMotor(SkillMotor motor)
        {
            if (motor == null)
                throw new ArgumentNullException(nameof(motor));
            var map = new Dictionary<string, PortalOp>(StringComparer.Ordinal);
            motor.ForEachSkill((id, skill) =>
            {
                PortalOp op = new SkillEngineModifiers(skill.Engine).PortalOp();
                if (op != PortalOp.None)
                    map[id] = op;
            });
            return map;
        }
    }
}
