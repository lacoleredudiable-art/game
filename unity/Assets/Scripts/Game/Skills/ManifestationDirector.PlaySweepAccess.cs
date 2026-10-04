#if UNITY_EDITOR
using Dovus.Core.Boss;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using Dovus.Game.Team;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    /// <summary>Play Sweep / SweepV2: reflection yerine tipli erişim.</summary>
    public sealed partial class ManifestationDirector
    {
        public TeamComboAccess SweepTeamAccess => _team;

        public HexagonInputController SweepInput => _input;
        public Transform SweepPlayer => _player;
        public BossReactorController SweepBoss => _boss;
        public BossVitals SweepBossVitals => _bossVitals;
        public BossDirector SweepBossDirector => _bossDirector;
        public ActorStatusHost SweepPlayerStatus => _playerStatus;
        public ActorStatusHost SweepBossStatus => _bossStatus;
        public AllyDummyController SweepAlly => _ally;
        public GameClockHost SweepClock => _clock;
        public SkillMotor SweepSkills => _skills;
        public HostileProjectileHost SweepProjectiles => _projectiles;
        public MotionTemplateBodyHost SweepMotionBody => _motionBody;
        public PlayerTargetingController SweepTargeting => _targeting;
        public CombatTuning SweepCombat => _combat;
        public Transform SweepTemplateAim => _templateAim;

        public bool SweepPerformingAttack => PerformingAttack;

        public IReadOnlyList<PendingClosing> SweepPending => _pending;

        public void SweepClearPending() => _pending.Clear();

        public float SweepClosingChainBonus => _closingChainBonus;

        public void SweepSetClosingChainBonus(float value) => _closingChainBonus = value;

        public MechanicGrammar SweepMechanicEngine => MechanicEngine;

        public MechanicPlan SweepMechanicPlanFor(SkillResolution skill) => MechanicPlanFor(skill);

        public MotionTemplateCatalog SweepMotionCatalog => MotionCatalog;

        public PositionPlayback SweepPreparePositionPlayback(SkillResolution skill, MotionTemplate template) =>
            PreparePositionPlayback(skill, template);

        public float SweepBossBodyRadius() => BossBodyRadius();

        public float SweepPlayerBodyRadiusM() => PlayerBodyRadiusM();

        public float SweepColliderRadius(Transform body) => ColliderRadius(body);

        public int SweepMechanicWorldLeftoverCount() => MechanicWorldLeftoverCount();

        public void SweepClearMechanicWorld() => ClearMechanicWorldSweep();
    }
}
#endif
