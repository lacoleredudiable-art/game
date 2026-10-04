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
    public readonly struct CastOutcome
    {
        // Unity'de Dovus.App ayrı assembly: init için IsExternalInit polyfill'i yok (Core'unki internal) → ctor.
        public CastOutcome(
            SkillId skillId,
            bool executorStarted,
            bool templateOwnsDelivery,
            float dealt,
            bool effectApplied,
            bool denied)
        {
            SkillId = skillId;
            ExecutorStarted = executorStarted;
            TemplateOwnsDelivery = templateOwnsDelivery;
            Dealt = dealt;
            EffectApplied = effectApplied;
            Denied = denied;
        }

        public SkillId SkillId { get; }
        public bool ExecutorStarted { get; }
        public bool TemplateOwnsDelivery { get; }
        public float Dealt { get; }
        public bool EffectApplied { get; }
        public bool Denied { get; }
    }
}
