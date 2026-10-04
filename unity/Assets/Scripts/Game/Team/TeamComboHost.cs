using Dovus.App.Team;
using Dovus.Core.Border;
using Dovus.Core.Grammar;
using Dovus.Core.Shared;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Core.Team;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Platform;
using Dovus.Game.Data;
using Dovus.Game.Platform;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Team
{
    /// <summary>
    /// Sınır, portal ve takım kombosunu sahneye bağlar.
    /// Kalıp sürerken oyuncunun yerini yazmaz; bitince uygular.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed partial class TeamComboHost : MonoBehaviour
    {
        readonly BorderMode _border = new();
        readonly PortalSystem _portal = new();
        readonly TeamComboSystem _team = new();
        SkillMotor _skillMotor;
        readonly List<TeamActorHost> _actors = new();
        readonly List<Body> _bodies = new();
        readonly List<IAllyPlayer> _allies = new();
        readonly List<GameObject> _visuals = new();
        readonly List<GameObject> _spawned = new();

        Transform _player;
        Transform _boss;
        MotionTemplateBodyHost _motion;
        PlayerVitalsHost _vitals;
        ActorStatusHost _bossStatus;
        GameClockHost _clock;
        GameObject _aura;
        bool _wasOwning;
        bool _borderReleasePending;
        string _line = "Takım menüsü hazır";
        int _nextId = 2;

        public string Line => _line;
        public BorderMode Border => _border;
        public PortalSystem Portal => _portal;
        public TeamComboSystem Team => _team;
        public TeamModifierHub Modifiers { get; } = new TeamModifierHub();
        public int Spawned => _spawned.Count;

        // MonoBehaviour ctor'unda Resources.Load yasak (UnityException) → op tabloları Awake'te JSON'dan bağlanır.
        void Awake()
        {
            if (ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design))
            {
                _skillMotor = design.SkillMotor;
                _portal.UseOps(PortalOpTable.FromMotor(_skillMotor));
                _team.UseOps(TeamOpTable.FromMotor(_skillMotor));
            }
        }

        SkillEngineModifiers EngineFor(SkillId skillId) =>
            _skillMotor != null && _skillMotor.TryGetSkill(skillId.Value, out SkillCatalogEntry entry) ? new SkillEngineModifiers(entry.Engine) : default;

        void OnEnable()
        {
            Modifiers.Cast += OnCast;
            Modifiers.Roll = () => (float)UnityRng.Default.NextDouble();
        }

        void OnDisable()
        {
            Modifiers.Cast -= OnCast;
            Modifiers.ResetModifiers();
        }

        /// <summary>Tarama vakaları arasında kapı, ışın ve dost buff'ı kalmasın.</summary>
        public void ResetCase()
        {
            _portal.Clear();
            _team.Clear();
            _border.Clear();
            _wasOwning = false;
            _borderReleasePending = false;
            Modifiers.ResetModifiers();
        }

        bool Bind()
        {
            if (_player == null)
            {
                _vitals = FindAnyObjectByType<PlayerVitalsHost>();
                if (_vitals != null)
                    _player = _vitals.transform;
            }
            if (_player == null)
                return false;
            if (_motion == null)
                _motion = _player.GetComponent<MotionTemplateBodyHost>();
            if (_boss == null)
            {
                BossReactorController reactor = FindAnyObjectByType<BossReactorController>();
                if (reactor != null)
                {
                    _boss = reactor.transform;
                    _bossStatus = reactor.GetComponent<ActorStatusHost>();
                }
            }
            if (_clock == null)
                _clock = FindAnyObjectByType<GameClockHost>();
            return true;
        }

        void Update()
        {
            if (!Bind())
                return;
            float dt = _clock != null ? (float)(_clock.WorldDeltaMs / Units.SecToMs) : Time.deltaTime;
            if (dt < 0f)
                dt = 0f;
            RefreshActors();
            TeamActorHost player = PlayerActor();
            if (player == null)
                return;

            if (_borderReleasePending)
            {
                _border.EndCast(player.Id);
                _borderReleasePending = false;
            }
            _border.Tick(player.Id, player.HpRatio, dt);
            for (int i = 0; i < _actors.Count; i++)
            {
                if (_actors[i] != player)
                    _border.Tick(_actors[i].Id, _actors[i].HpRatio, dt);
            }

            Disc boss = BossDisc();
            Body caster = ToBody(player);
            _portal.Tick(dt, caster, _bodies, boss);
            TeamPulse pulse = _team.Tick(dt, _allies, boss);
            ApplyPulse(pulse);

            bool owns = _motion != null && _motion.IsDisplacing;
            player.TemplateOwnsPosition = owns;
            if (_wasOwning && !owns)
            {
                Vector3 p = _player.position;
                _portal.NotifyTemplateEnded(player.Id, p.x, p.y, p.z, player.Radius, boss);
                // Skill bitti. Bu karedeki Tick aura'yı tuttu; sonraki kare eşiğe bakar.
                _border.EndCast(player.Id);
            }
            _wasOwning = owns;

            SenseBodies(boss);
            ApplyMoves();
            PushHooks(player);
            RefreshAura(player);
            RefreshVisuals();
        }

        void OnCast(string skillId)
        {
            if (!Bind())
                return;
            RefreshActors();
            TeamActorHost player = PlayerActor();
            if (player == null)
                return;
            player.LastSkillId = skillId;
            player.TemplateOwnsPosition = _motion != null && _motion.IsDisplacing;
            Disc boss = BossDisc();
            var typedId = (SkillId)skillId;
            _border.OnSkill(player.Id, typedId, EngineFor(typedId), player.HpRatio);
            if (_motion == null || !_motion.IsDisplacing)
                _borderReleasePending = true;
            Body caster = ToBody(player);
            Body target = FirstOther(player);
            _portal.Cast(typedId, caster, target, _bodies, boss);
            TeamPulse pulse = _team.Cast(typedId, player, FindAlly(target.Id), _allies, boss);
            ApplyPulse(pulse);
            ApplyMoves();
            // O3: eski ApplyBackStrike (önceki vuruşun zırh sonrası hasarını ikinci kez zırhtan geçiren görünmez
            // üçüncü vuruş) kaldırıldı; 1-10'un sırt vuruşu kalıbın sirtta_kapi fazında. Yalnız görsel kalır.
            if (EngineFor(typedId).PortalOp() == PortalOp.BackDoor && _portal.Strike.Active)
            {
                Burst(new Vector3(_portal.Strike.X, TeamComboDefaults.PortalStrikeMarkerY, _portal.Strike.Z), new Color(0.75f, 0.75f, 1f));
            }
            if (_border.Active(player.Id))
                _line = _border.AuraLabel(player.Id);
        }


        Transform _reactorOwner;
        BossReactorController _reactorCache;

        /// <summary>O11: her kare GetComponent yerine boss başına bir kez.</summary>
        BossReactorController CachedBossReactor()
        {
            if (_boss == null)
                return null;
            if (_reactorOwner != _boss || _reactorCache == null)
            {
                _reactorOwner = _boss;
                _reactorCache = _boss.GetComponent<BossReactorController>();
            }
            return _reactorCache;
        }

        static Body ToBody(TeamActorHost actor) =>
            new Body(
                actor.Id,
                actor.transform.position.x,
                actor.transform.position.y,
                actor.transform.position.z,
                actor.Radius,
                actor.TemplateOwnsPosition,
                false);

        void ApplyPulse(TeamPulse pulse)
        {
            if (_bossStatus == null)
                return;
            if (pulse.Stunned && pulse.StunSec > 0f)
                _bossStatus.Board.Apply(StatusKind.Stun, pulse.StunSec * Units.SecToMs, TeamComboDefaults.StunStatusStrength, "takim");
            if (pulse.Burned)
            {
                // S17: yüklenen/panelden değişen tuning (ActorStatusHost.Bind'deki _combat.Status), varsayılan değil.
                // Yanik yalniz baglanti (team_op link) kurulduktan sonra gelir; kaynak o cast'in skill kimligi.
                _bossStatus.Board.Apply(StatusKind.Burn, Units.SecToMs, _bossStatus.Tuning.BurnDamagePerSec, _team.LinkBurnSourceSkillId);
            }
            if (pulse.MineMult > TeamComboDefaults.MineMultActiveThreshold)
                _line = "Mayın x" + pulse.MineMult.ToString("0");
        }

        Disc BossDisc()
        {
            if (_boss == null)
                return Disc.None;
            BossReactorController reactor = CachedBossReactor();
            float radius = reactor != null && reactor.BodyRadiusM > TeamComboDefaults.BossBodyRadiusMinM ? reactor.BodyRadiusM : CombatFallbacks.BossBodyRadiusFallbackM;
            return new Disc(true, _boss.position.x, _boss.position.z, radius, PortalSystem.ClearGapM);
        }
    }
}
