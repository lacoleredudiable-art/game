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
    public sealed class GuardTrigger
        {
            public int Id;
            public MechanicEffect Effect;
            public GameObject View;
            public double UntilMs;
            public bool NeedsHoly;
        }
}
