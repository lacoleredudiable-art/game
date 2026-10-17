using Dovus.App.Casting;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Shared;
using Dovus.Core.Manifestation;
using Dovus.Game.Diagnostics;
using Dovus.Game.Skills.Hosts;
using Dovus.Game.Skills.RuleEngineV4;
using Dovus.Game.Team;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class RuleEngineV4SentenceHost
    {
        internal readonly CastPipeline _castPipeline = new();
        internal MdCastPort _castPort;

        internal void EnsureCastPort() => _castPort ??= new MdCastPort(this);

        /// <summary>Composition bootstrap: CastPort + v4 combat (CastHost dahil).</summary>
        public RuleEngineV4CombatContext EnsureCombatContext()
        {
            EnsureCastPort();
            return _castPort.CombatContext;
        }

        internal bool CastPortIsHealSkill(SkillResolution skill) => IsHealSkill(skill);
    }
}
