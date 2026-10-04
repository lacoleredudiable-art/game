using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Composition;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using UnityEngine;

namespace Dovus.Game.Skills.Closing
{
    public interface IClosingStatusHost
    {
        Transform Player { get; }
        AllyDummy Ally { get; }
        ActorStatus PlayerStatus { get; }
        ActorStatus BossStatus { get; }
        CombatTuning Combat { get; }
        MobilityCcData MobilityCc { get; }
        SlotPassiveDirector SlotPassives { get; }
        PassiveFlowRunner PassiveFlows { get; }
        int SlotQueryCastId { get; }
        GameClock Clock { get; }
        MechanicGrammar MechanicEngine { get; }
        BossVitals BossVitals { get; }
        Transform Boss { get; }
        DamageNumberHud DamageHud { get; }
        ElementPaintNode? SelectedElementPaint { get; }
        bool LastFriendlyWasAlly { set; }
        float WeaponFriendlyScale();
        int JsonCleanseCount(SkillResolution skill);
        void ShareFriendlyStatuses(SkillResolution skill, StatusBoard friendlyBoard);
        void ApplyPurgePower(SkillResolution skill, int cleansedCount);
        void ApplyArmorShred(SkillResolution skill, ActorStatus bossStatus);
        bool IsEnemyBody(Transform body);
        Vector3? BossHitPoint();
        Color? DamageTint();
    }
}
