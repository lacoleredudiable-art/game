using Dovus.Core.Combat;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Mechanics
{
public sealed class MechanicWorldBody
        {
            public GameObject View;
            public double UntilMs;
        }

public sealed class MechanicVolume
        {
            public GameObject View;
            public MechanicPlan Plan;
            public MechanicWorldProfile Profile;
            public Vector3 Center;
            public float RadiusM;
            public double UntilMs;
            public double NextTickMs;
            public double TickMs;
            public double StartMs;
            public SkillResolution Skill;
            public ClosingHit Closing;
            public bool Triggered;
            public double ArmAtMs;
            /// <summary>mermi_sil: bu hacmin düşman mermisine kuralı (boş = yok). MD.Projectiles uygular.</summary>
            public EraseSpec Erase;
            public double NextEraseMs;
            public bool TwiceDone;
        }

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

public sealed class GuardTrigger
        {
            public int Id;
            public MechanicEffect Effect;
            public GameObject View;
            public double UntilMs;
            public bool NeedsHoly;
        }
}
