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
                ["1-10"] = PortalOp.BackDoor,
                ["2-6"] = PortalOp.Hook,
                ["3-4"] = PortalOp.AnchorOrRecall,
                ["3-10"] = PortalOp.Pair,
                ["8-1"] = PortalOp.Gate,
                ["8-8"] = PortalOp.MirrorGate,
                ["9-10"] = PortalOp.Swap,
                ["10-10"] = PortalOp.Mirror,
                ["11-8"] = PortalOp.Sink,
                ["11-10"] = PortalOp.GatherTeam,
            };

        public static bool TryLegacy(string skillId, out PortalOp op) =>
            Legacy.TryGetValue(skillId ?? string.Empty, out op);

        public static PortalOp Resolve(string skillId, IReadOnlyDictionary<string, PortalOp> table)
        {
            if (table != null && table.TryGetValue(skillId ?? string.Empty, out PortalOp op))
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
