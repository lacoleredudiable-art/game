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
using Dovus.Core.Tuning;
using Dovus.Game.Composition;
using System;
using UnityEngine;
namespace Dovus.Game.Skills.Execution
{
public interface ISkillExecutor
    {
        SkillExecutorKind Kind { get; }
        void Execute(in SkillExecutionContext context);
    }
}
