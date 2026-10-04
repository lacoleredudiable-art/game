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
using Dovus.Game.Team;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        internal readonly CastPipeline _castPipeline = new();
        internal MdCastPort _castPort;

        internal bool CastPortIsHealSkill(SkillResolution skill) => IsHealSkill(skill);
    }
}
