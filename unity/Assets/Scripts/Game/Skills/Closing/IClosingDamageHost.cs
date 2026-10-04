using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Game.Actors;
using Dovus.Core.Tuning;
using Dovus.Game.Boss;
using Dovus.Game.Composition;
using Dovus.Game.Feel;
using Dovus.Game.Hud;
using Dovus.Core.Status;
using Dovus.Game.Team;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Skills.Closing
{
    public interface IClosingDamageHost
    {
        Transform Player { get; }
        BossReactorController Boss { get; }
        BossVitals BossVitals { get; }
        BossDirector BossDirector { get; }
        ActorStatusHost BossStatus { get; }
        CombatTuning Combat { get; }
        GameClockHost Clock { get; }
        TeamComboAccess TeamAccess { get; }
        KinematicMotorController Motor { get; }
        DamageNumberHud DamageHud { get; }
        GroundScarFieldView Scars { get; }
        SlotPassiveDirector SlotPassives { get; }
        int SlotQueryCastId { get; }
        float ClosingChainBonus { get; }
        bool JsonTickDamage { get; }
        float LastHitX { get; }
        float LastHitZ { get; }
        float LastClosingDamageDealt { get; set; }
        ClosingStatusApplier StatusApplier { get; }
        DamageOutcome ComputeOutgoingHit(
            ClosingHit closing,
            SkillResolution skill,
            bool isBasicStrike,
            float slashCommitMult,
            float effectScale,
            float? chainBonusOverride);
        PlayerVitalsHost CachedPlayerVitals();
        Vector3? BossHitPoint();
        Color? DamageTint();
        void TryConsumeCounterWindow();
        void RememberHitPoint(Vector3? point);
        void TryCannonBlast(float x, float z);
        void ConsumeWeaponBonus(StatusBoard board);
        void NotifyBossStruck(bool isCrit, bool allowHitstop);
        void NoteImpactOrigin(LivingEffect logic);
        float PlayerBodyRadiusM();
    }
}
