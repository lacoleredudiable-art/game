using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Shared;

namespace Dovus.Core.Team
{
    public static class TeamOpTable
    {
        public static readonly IReadOnlyDictionary<string, TeamOp> Empty =
            new Dictionary<string, TeamOp>(StringComparer.Ordinal);

        public static TeamOp Resolve(SkillId skillId, IReadOnlyDictionary<string, TeamOp> table)
        {
            if (table != null && table.TryGetValue(skillId.Value, out TeamOp op))
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
