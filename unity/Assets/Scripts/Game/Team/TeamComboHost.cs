using Dovus.App.Team;
using Dovus.Core.Border;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Core.Team;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Composition;
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
    public sealed class TeamComboHost : MonoBehaviour
    {
        public static TeamComboHost Instance { get; private set; }

        readonly BorderMode _border = new();
        readonly PortalSystem _portal = new();
        readonly TeamComboSystem _team = new();
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
        public static TeamModifierHub Hub => Instance != null ? Instance.Modifiers : TeamModifierHub.Neutral;
        public int Spawned => _spawned.Count;

        // MonoBehaviour ctor'unda Resources.Load yasak (UnityException) → op tabloları Awake'te JSON'dan bağlanır.
        // JSON yoksa gömülü Legacy tablo kalır (içerik aynı; SkillMechanicTagTests denetler).
        void Awake()
        {
            if (ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design))
            {
                _portal.UseOps(PortalOpTable.FromMotor(design.SkillMotor));
                _team.UseOps(TeamOpTable.FromMotor(design.SkillMotor));
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<TeamComboHost>() != null)
                return;
            var go = new GameObject(nameof(TeamComboHost));
            go.AddComponent<TeamComboHost>();
            DontDestroyOnLoad(go);
        }

        void OnEnable()
        {
            Instance = this;
            Modifiers.Cast += OnCast;
            Modifiers.Roll = () => (float)UnityRng.Default.NextDouble();
        }

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
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

        void Update()
        {
            if (!Bind())
                return;
            float dt = _clock != null ? (float)(_clock.WorldDeltaMs / TeamComboDefaults.SecToMs) : Time.deltaTime;
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

        public void SpawnAlly()
        {
            if (!Bind() || _spawned.Count >= 4 || _player == null)
                return;
            int n = _spawned.Count + 1;
            Vector3 pos = _player.position + new Vector3(TeamComboDefaults.AllySpawnBaseX - n * TeamComboDefaults.AllySpawnStepX, 0f, TeamComboDefaults.AllySpawnZ);
            pos.y = TeamComboDefaults.ActorGroundY;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Dost " + n;
            go.transform.position = pos;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                SharedTint.Apply(renderer, new Color(0.35f, 0.9f, 0.55f));
            var dummy = go.AddComponent<AllyDummyController>();
            int maxHp = _vitals != null ? _vitals.MaxHp : TeamComboDefaults.VitalsMaxHpFallback;
            dummy.Bind(maxHp, TeamComboDefaults.AllyDummyHpRatio);
            var actor = go.AddComponent<TeamActorHost>();
            actor.Id = _nextId++;
            actor.Radius = TeamComboDefaults.TeamActorRadiusM;
            _spawned.Add(go);
            _line = go.name + " geldi";
        }

        public void SetPlayerRatio(float ratio)
        {
            if (!Bind() || _vitals == null)
                return;
            int target = Mathf.Clamp(Mathf.RoundToInt(_vitals.MaxHp * Mathf.Clamp01(ratio)), 1, _vitals.MaxHp);
            if (_vitals.Hp > target)
                _vitals.ApplyDamage(_vitals.Hp - target);
            else
                _vitals.ApplyHeal(target - _vitals.Hp);
            _line = "Can %" + Mathf.RoundToInt(ratio * TeamComboDefaults.HpPercentScale);
        }

        public void CommandCast(TeamActorHost actor, string skillId)
        {
            if (!Bind() || actor == null || string.IsNullOrEmpty(skillId))
                return;
            RefreshActors();
            actor.LastSkillId = skillId;
            Disc boss = BossDisc();
            _border.OnSkill(actor.Id, skillId, actor.HpRatio);
            Body body = ToBody(actor);
            Body target = FirstOther(actor);
            _portal.Cast(skillId, body, target, _bodies, boss);
            TeamPulse pulse = _team.Cast(skillId, actor, FindAlly(target.Id), _allies, boss);
            ApplyPulse(pulse);
            ApplyMoves();
            _line = actor.name + " → " + skillId;
        }

        public void CommandHit(TeamActorHost actor)
        {
            if (!Bind() || actor == null)
                return;
            RefreshActors();
            float x = actor.transform.position.x;
            float z = actor.transform.position.z;
            bool struck = false;
            if (_boss != null)
            {
                float dx = _boss.position.x - x;
                float dz = _boss.position.z - z;
                struck = true;
                x = _boss.position.x;
                z = _boss.position.z;
                if (dx * dx + dz * dz < TeamComboDefaults.NearBossDistSqr)
                    struck = true;
            }
            if (_team.TryRopeMid(out float mx, out float mz))
            {
                x = mx;
                z = mz;
                struck = false;
            }
            TeamPulse pulse = _team.AllyHit(actor, x, z, struck || _team.AttackBroken);
            ApplyPulse(pulse);
            float mult = _team.DamageMult(actor.Id) * _portal.BuffFor(actor.Id).DamageMult * _team.BossIncomingMult;
            _line = actor.name + " vurdu x" + mult.ToString("0.00");
        }

        public void CommandSkillAt(TeamActorHost actor, string skillId)
        {
            if (actor == null)
                return;
            CommandCast(actor, skillId);
            if (_team.TryMine(out float mx, out float mz))
            {
                TeamPulse pulse = _team.AllyUsedSkill(actor, skillId, actor.transform.position.x, actor.transform.position.z);
                if (pulse.MineMult <= 0f)
                {
                    actor.transform.position = new Vector3(mx, 0f, mz);
                    pulse = _team.AllyUsedSkill(actor, skillId, mx, mz);
                }
                ApplyPulse(pulse);
                if (pulse.MineMult > 0f)
                {
                    _line = "Mayın x" + pulse.MineMult.ToString("0");
                    Burst(new Vector3(mx, TeamComboDefaults.MineBurstHeightY, mz), new Color(1f, 0.45f, 0.1f));
                }
            }
        }

        public void SendToMine(TeamActorHost actor)
        {
            if (actor == null || !_team.TryMine(out float x, out float z))
                return;
            actor.transform.position = new Vector3(x, TeamComboDefaults.ActorGroundY, z);
            _line = "Dost mayında";
        }

        public void SendToRope(TeamActorHost actor)
        {
            if (actor == null || !_team.TryRopeMid(out float x, out float z))
                return;
            actor.transform.position = new Vector3(x, TeamComboDefaults.ActorGroundY, z);
            _line = "Dost ipin ortasında";
        }

        public void SendToTurret(TeamActorHost actor)
        {
            if (actor == null || !_team.TryTurret(out float x, out float z))
                return;
            actor.transform.position = new Vector3(x, TeamComboDefaults.ActorGroundY, z);
            _line = "Dost tarete dokunuyor";
        }

        public void TouchTurret(TeamActorHost actor)
        {
            if (actor == null)
                return;
            SendToTurret(actor);
            string skill = _team.TouchTurret(actor);
            _line = string.IsNullOrEmpty(skill) ? "Taret kopyalamadı" : "Taret kopyaladı: " + skill;
            if (!string.IsNullOrEmpty(skill) && _boss != null)
                Burst(_boss.position + Vector3.up, new Color(1f, 0.85f, 0.3f));
        }

        public void PassBall(TeamActorHost from)
        {
            if (from == null)
                return;
            RefreshActors();
            TeamActorHost to = null;
            for (int i = 0; i < _actors.Count; i++)
            {
                if (_actors[i] != from && _actors[i].Id != Modifiers.PlayerActorId)
                {
                    if (to == null || to.Id == _team.BallHolder)
                        to = _actors[i];
                }
            }
            if (to == null)
                return;
            bool ok = _team.PassBall(from, to);
            _line = ok ? "Pas " + from.Id + " → " + to.Id : "Pas olmadı";
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
            _border.OnSkill(player.Id, skillId, player.HpRatio);
            if (_motion == null || !_motion.IsDisplacing)
                _borderReleasePending = true;
            Body caster = ToBody(player);
            Body target = FirstOther(player);
            _portal.Cast(skillId, caster, target, _bodies, boss);
            TeamPulse pulse = _team.Cast(skillId, player, FindAlly(target.Id), _allies, boss);
            ApplyPulse(pulse);
            ApplyMoves();
            // O3: eski ApplyBackStrike (önceki vuruşun zırh sonrası hasarını ikinci kez zırhtan geçiren görünmez
            // üçüncü vuruş) kaldırıldı; 1-10'un sırt vuruşu kalıbın sirtta_kapi fazında. Yalnız görsel kalır.
            if (skillId == "1-10" && _portal.Strike.Active)
            {
                Burst(new Vector3(_portal.Strike.X, TeamComboDefaults.PortalStrikeMarkerY, _portal.Strike.Z), new Color(0.75f, 0.75f, 1f));
            }
            if (_border.Active(player.Id))
                _line = _border.AuraLabel(player.Id);
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

        void RefreshActors()
        {
            _actors.Clear();
            if (_player != null)
            {
                TeamActorHost actor = _player.GetComponent<TeamActorHost>();
                if (actor == null)
                    actor = _player.gameObject.AddComponent<TeamActorHost>();
                actor.Id = Modifiers.PlayerActorId;
                actor.Radius = TeamComboDefaults.TeamActorRadiusM;
                if (_vitals != null && _vitals.MaxHp > 0)
                    actor.HpRatio = (float)_vitals.Hp / _vitals.MaxHp;
                actor.TemplateOwnsPosition = _motion != null && _motion.IsDisplacing;
                _actors.Add(actor);
            }

            System.Collections.Generic.IReadOnlyList<AllyDummyController> dummies = AllyDummyController.Live;
            for (int i = 0; i < dummies.Count; i++)
            {
                AllyDummyController dummy = dummies[i];
                if (dummy == null)
                    continue;
                TeamActorHost actor = dummy.GetComponent<TeamActorHost>();
                if (actor == null)
                {
                    actor = dummy.gameObject.AddComponent<TeamActorHost>();
                    actor.Id = _nextId++;
                    actor.Radius = TeamComboDefaults.TeamActorRadiusM;
                }
                actor.HpRatio = dummy.Ratio;
                _actors.Add(actor);
            }

            _bodies.Clear();
            _allies.Clear();
            for (int i = 0; i < _actors.Count; i++)
            {
                _bodies.Add(ToBody(_actors[i]));
                _allies.Add(_actors[i]);
            }
        }

        void SenseBodies(in Disc boss)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                TeamActorHost actor = _actors[i];
                Body body = ToBody(actor);
                _portal.Sense(body, false, boss, out _);
            }
            if (_boss != null)
            {
                BossReactorController reactor = CachedBossReactor();
                float radius = reactor != null ? reactor.BodyRadiusM : TeamComboDefaults.BossBodyRadiusFallbackM;
                var bossBody = new Body(TeamComboDefaults.BossPortalBodyId, _boss.position.x, _boss.position.y, _boss.position.z, radius, false, true);
                _portal.Sense(bossBody, false, boss, out _);
            }
        }

        void ApplyMoves()
        {
            IReadOnlyList<Placement> moves = _portal.Drain();
            for (int i = 0; i < moves.Count; i++)
                ApplyOne(moves[i]);
        }

        void ApplyOne(Placement move)
        {
            TeamActorHost actor = FindActor(move.ActorId);
            if (actor == null)
                return;
            if (actor.TemplateOwnsPosition && actor.Id == Modifiers.PlayerActorId)
                return;
            if (move.Teleport && actor.Id == Modifiers.PlayerActorId)
                Modifiers.MarkIntentionalTeleport();
            actor.transform.position = new Vector3(move.X, move.Y, move.Z);
            if (!move.TransferDebuffs)
                return;
            AllyDummyController dummy = actor.GetComponent<AllyDummyController>();
            dummy?.EnsureStatusBoard();
            if (dummy != null && dummy.Board != null && _bossStatus != null)
                PortalSystem.MoveHostile(dummy.Board, _bossStatus.Board);
        }

        void ApplyPulse(TeamPulse pulse)
        {
            if (_bossStatus == null)
                return;
            if (pulse.Stunned && pulse.StunSec > 0f)
                _bossStatus.Board.Apply(StatusKind.Stun, pulse.StunSec * TeamComboDefaults.SecToMs, TeamComboDefaults.StunStatusStrength, "takim");
            if (pulse.Burned)
            {
                // S17: yüklenen/panelden değişen tuning (ActorStatusHost.Bind'deki _combat.Status), varsayılan değil.
                _bossStatus.Board.Apply(StatusKind.Burn, TeamComboDefaults.SecToMs, _bossStatus.Tuning.BurnDamagePerSec, "8-6");
            }
            if (pulse.MineMult > TeamComboDefaults.MineMultActiveThreshold)
                _line = "Mayın x" + pulse.MineMult.ToString("0");
        }

        void PushHooks(TeamActorHost player)
        {
            int id = player.Id;
            PortalBuff buff = _portal.BuffFor(id);
            var table = Modifiers.Table;
            table.Set(
                id,
                new ActorModifiers(
                    _border.AttackSpeedMult(id) * _team.AttackSpeedMult(id),
                    _border.DamageMult(id) * _team.DamageMult(id) * buff.DamageMult,
                    _border.LifestealAdd(id),
                    _border.ColumnMoveSpeedMult(id) * _team.MoveSpeedMult(id) * buff.MoveSpeedMult,
                    buff.DamageTakenMult));
            table.BossIncomingMult = _team.BossIncomingMult;
            table.BossStrikeScale = _portal.StrikeScale;
            Modifiers.SetMiss(id, buff.MissChance);
            Modifiers.SetTaken(id, buff.DamageTakenMult);
            for (int i = 0; i < _actors.Count; i++)
            {
                if (_actors[i].Id == id)
                    continue;
                PortalBuff allyBuff = _portal.BuffFor(_actors[i].Id);
                Modifiers.SetMiss(_actors[i].Id, allyBuff.MissChance);
                Modifiers.SetTaken(_actors[i].Id, allyBuff.DamageTakenMult);
            }
        }

        void RefreshAura(TeamActorHost player)
        {
            bool on = _border.Active(player.Id);
            if (on && _aura == null && _player != null)
            {
                _aura = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _aura.name = "BorderAura";
                Collider col = _aura.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);
                _aura.transform.SetParent(_player, false);
                _aura.transform.localPosition = new Vector3(0f, TeamComboDefaults.BorderAuraLocalY, 0f);
                _aura.transform.localScale = new Vector3(TeamComboDefaults.BorderAuraScaleXZ, TeamComboDefaults.BorderAuraScaleY, TeamComboDefaults.BorderAuraScaleXZ);
                Renderer renderer = _aura.GetComponent<Renderer>();
                if (renderer != null)
                    SharedTint.Apply(renderer, new Color(1f, 0.2f, 0.25f, 0.85f));
            }
            if (_aura != null)
                _aura.SetActive(on);
        }

        void RefreshVisuals()
        {
            for (int i = 0; i < _visuals.Count; i++)
            {
                if (_visuals[i] != null)
                    Destroy(_visuals[i]);
            }
            _visuals.Clear();
            IReadOnlyList<DoorView> doors = _portal.Doors;
            for (int i = 0; i < doors.Count; i++)
            {
                DoorView door = doors[i];
                GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                gate.name = "PortalGate";
                Collider col = gate.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);
                gate.transform.position = new Vector3(door.X, TeamComboDefaults.GateMarkerY, door.Z);
                gate.transform.localScale = new Vector3(door.Radius * 2f, TeamComboDefaults.GateThicknessY, door.Radius * 2f);
                Renderer renderer = gate.GetComponent<Renderer>();
                if (renderer != null)
                    SharedTint.Apply(renderer, new Color(0.45f, 0.35f, 1f, 0.9f));
                _visuals.Add(gate);
            }
            if (_team.TryMine(out float mx, out float mz))
                _visuals.Add(Marker("Mine", mx, mz, new Color(1f, 0.4f, 0.15f)));
            if (_team.TryRopeMid(out float rx, out float rz))
                _visuals.Add(Marker("Rope", rx, rz, new Color(0.15f, 0.15f, 0.15f)));
            if (_team.TryTurret(out float tx, out float tz))
                _visuals.Add(Marker("Turret", tx, tz, new Color(0.9f, 0.8f, 0.3f)));
        }

        GameObject Marker(string name, float x, float z, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            go.transform.position = new Vector3(x, TeamComboDefaults.WorldMarkerY, z);
            go.transform.localScale = Vector3.one * TeamComboDefaults.WorldMarkerScale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                SharedTint.Apply(renderer, color);
            return go;
        }

        void Burst(Vector3 pos, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "TeamBurst";
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * TeamComboDefaults.BurstFxScale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                SharedTint.Apply(renderer, color);
            Destroy(go, TeamComboDefaults.BurstFxLifetimeSec);
        }

        TeamActorHost PlayerActor()
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                if (_actors[i].Id == Modifiers.PlayerActorId)
                    return _actors[i];
            }
            return _actors.Count > 0 ? _actors[0] : null;
        }

        TeamActorHost FindActor(int id)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                if (_actors[i].Id == id)
                    return _actors[i];
            }
            return null;
        }

        IAllyPlayer FindAlly(int id) => FindActor(id);

        Body FirstOther(TeamActorHost self)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                if (_actors[i] != self)
                    return ToBody(_actors[i]);
            }
            return default;
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

        Disc BossDisc()
        {
            if (_boss == null)
                return Disc.None;
            BossReactorController reactor = CachedBossReactor();
            float radius = reactor != null && reactor.BodyRadiusM > TeamComboDefaults.BossBodyRadiusMinM ? reactor.BodyRadiusM : TeamComboDefaults.BossBodyRadiusFallbackM;
            return new Disc(true, _boss.position.x, _boss.position.z, radius, PortalSystem.ClearGapM);
        }
    }
}
