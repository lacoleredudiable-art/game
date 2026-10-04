using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Shared;

namespace Dovus.Core.Portal
{
    public static class PortalOpTable
    {
        public static readonly IReadOnlyDictionary<string, PortalOp> Empty =
            new Dictionary<string, PortalOp>(StringComparer.Ordinal);

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
