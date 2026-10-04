using Dovus.Core.Boss;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Passives;
using Dovus.Core.Manifestation;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Diagnostics;
using Dovus.Game.Hud;
using Dovus.Game.Platform;
using Dovus.Game.Skills.State;
using Dovus.Game.Team;
using Dovus.Game.Data;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        internal readonly SkillWorldState SkillWorld = new();
        internal readonly CastSessionState CastSession = new();

        internal Transform MechanicsPlayer => _player;
        internal AllyDummyController MechanicsAlly => _ally;
        internal BossReactorController MechanicsBoss => _boss;
        internal ActorStatusHost MechanicsBossStatus => _bossStatus;
        internal ActorStatusHost MechanicsPlayerStatus => _playerStatus;
        internal BossVitals MechanicsBossVitals => _bossVitals;
        internal BossDirector MechanicsBossDirector => _bossDirector;
        internal GameClockHost MechanicsClock => _clock;
        internal TeamComboAccess MechanicsTeam => _team;
        internal CombatTuning MechanicsCombat => _combat;
        internal SkillMotor MechanicsSkills => _skills;
        internal SlotPassiveDirector MechanicsSlotPassives => _slotPassives;
        internal ReactionReadoutHud MechanicsReadout => _readout;
        internal DamageNumberHud MechanicsDamageHud => _damageHud;
        internal ISentenceDebugSink MechanicsDebugHud => _debugHud;
        internal KinematicMotorController MechanicsMotor => _motor;
        internal EquipmentItem MechanicsEquippedWeapon => _equippedWeapon;

        internal bool TemplateOwnsPositionFlag => _templateOwnsPosition;
        internal SkillResolution TemplateSkillRef => _templateSkill;
        internal float TemplateChainRef => _templateChain;

        internal void ScheduleMechanicPortalAfter(double now, float delaySec, System.Action run) =>
            _mechanicPortals.ScheduleAfter(now, delaySec, run);

        internal void SetDeliveryArm(SkillResolution skill, PendingClosing pending)
        {
            CastSession.DeliverySkill = skill;
            CastSession.DeliveryPending = pending;
            _deliverySkill = skill;
            _deliveryPending = pending;
        }
    }
}
