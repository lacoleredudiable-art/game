using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Composition;
using Dovus.Game.Hud;
using Dovus.Game.Team;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Passives
{
    public interface ISlotPassiveRuntimeHost
    {
        SlotPassiveDirector SlotPassives { get; }
        SentenceEngine Engine { get; }
        SkillMotor Skills { get; }
        GameClock Clock { get; }
        BossVitals BossVitals { get; }
        DamageNumberHud DamageHud { get; }
        ReactionReadout Readout { get; }
        PassiveHud PassiveHud { get; }

        int SlotQueryCastId { get; set; }

        bool IsHealSkill(SkillResolution skill);
        void ApplyClosingHeal(ClosingHit closing, SkillResolution skill, float power, float chain);
        float ApplyClosingDamage(
            ClosingHit closing,
            SkillResolution skill,
            bool isBasicStrike,
            float slash,
            float power,
            float chain);

        Vector3? BossHitPoint();
        Color? DamageTint();
    }
}
