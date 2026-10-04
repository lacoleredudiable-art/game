using System;
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
using Dovus.Core.Mechanic;

namespace Dovus.App.Casting
{
    public readonly struct CastStarted
    {
        public CastStarted(string skillId) => SkillId = skillId ?? string.Empty;
        public string SkillId { get; }
    }
}
