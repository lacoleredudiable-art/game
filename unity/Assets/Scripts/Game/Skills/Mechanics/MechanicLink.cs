using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Mechanics
{
    public sealed class MechanicLink
        {
            public LineRenderer Line;
            public MechanicPlan Plan;
            public Transform Target;
            public double UntilMs;
            public SkillResolution Skill;
            public ClosingHit Closing;
            public double NextFlowMs;
            public double FlowTickMs;
        }
}
