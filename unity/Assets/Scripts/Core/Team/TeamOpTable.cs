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
                [SkillIds.MirrorStep] = TeamOp.Marker,
                [SkillIds.FixedBlast] = TeamOp.Mine,
                [SkillIds.RisingHead] = TeamOp.HangBoss,
                [SkillIds.OpeningVulnerability] = TeamOp.Rope,
                [SkillIds.FocusedVulnerability] = TeamOp.Mark,
                [SkillIds.LeapingAscent] = TeamOp.Ball,
                [SkillIds.OpeningAscent] = TeamOp.Link,
                [SkillIds.MirrorReflect] = TeamOp.Marker,
                [SkillIds.FixedSummon] = TeamOp.Turret,
                [SkillIds.OpeningTime] = TeamOp.HasteRope,
            };

        public static bool TryLegacy(SkillId skillId, out TeamOp op) =>
            Legacy.TryGetValue(skillId.Value, out op);

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
