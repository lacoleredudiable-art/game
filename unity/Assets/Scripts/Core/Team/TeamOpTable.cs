using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core.Grammar;

namespace Dovus.Core.Team
{
    public static class TeamOpTable
    {
        public static IReadOnlyDictionary<string, TeamOp> Legacy { get; } = BuildLegacy();

        static Dictionary<string, TeamOp> BuildLegacy() =>
            new Dictionary<string, TeamOp>(StringComparer.Ordinal)
            {
                ["3-10"] = TeamOp.Marker,
                ["5-4"] = TeamOp.Mine,
                ["6-8"] = TeamOp.HangBoss,
                ["7-6"] = TeamOp.Rope,
                ["7-9"] = TeamOp.Mark,
                ["8-3"] = TeamOp.Ball,
                ["8-6"] = TeamOp.Link,
                ["10-10"] = TeamOp.Marker,
                ["11-4"] = TeamOp.Turret,
                ["12-6"] = TeamOp.HasteRope,
            };

        public static bool TryLegacy(string skillId, out TeamOp op) =>
            Legacy.TryGetValue(skillId ?? string.Empty, out op);

        public static TeamOp Resolve(string skillId, IReadOnlyDictionary<string, TeamOp> table)
        {
            if (table != null && table.TryGetValue(skillId ?? string.Empty, out TeamOp op))
                return op;
            return TeamOp.None;
        }

        public static Dictionary<string, TeamOp> FromMotor(SkillMotor motor)
        {
            if (motor == null)
                throw new ArgumentNullException(nameof(motor));
            var map = new Dictionary<string, TeamOp>(StringComparer.Ordinal);
            motor.ForEachSkill((id, skill) =>
            {
                TeamOp op = new SkillEngineModifiers(skill.Engine).TeamOp();
                if (op != TeamOp.None)
                    map[id] = op;
            });
            return map;
        }
    }
}
