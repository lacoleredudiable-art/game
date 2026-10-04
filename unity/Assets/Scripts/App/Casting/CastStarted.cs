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
using Dovus.Core.Shared;

namespace Dovus.App.Casting
{
    public readonly struct CastStarted
    {
        public CastStarted(SkillId skillId) => SkillId = skillId;
        public SkillId SkillId { get; }
    }
}
