using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Motion;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Team
{
    /// <summary>
    /// Takım kombosu: bir oyuncunun bıraktığı işi başka oyuncu bitirir.
    /// Hasar formülü burada yok; yalnız çarpan ve süre döner.
    /// Taret atış aralığı metinde yok — yedek 1 sn (DesignWarnings).
    /// 7-6 zırh süresi metinde yok — StatusTuning.ArmorBreakMs.
    /// 12-6 kopma, bağ kurulurkenki mesafenin üstüdür. Hız JSON'da 0; yedek +%50.
    /// </summary>
    public sealed partial class TeamComboSystem
    {
        public const float MineLifeSec = 6f;
        public const float MinePower = 2f;
        public const float HangSec = 1f;
        public const float HangDamageAdd = 0.30f;
        public const float RopeLifeSec = 4f;
        public const float RopeStunSec = 1f;
        public const float RopeArmorDrop = 0.30f;
        public const float MarkArmSec = 2f;
        public const float MarkArmorSec = 4f;
        public const float MarkArmor = 0.30f;
        public const float MarkArmorPair = 0.50f;
        public const float BallBuff = 0.20f;
        public const float BallSec = 4f;
        public const int BallMaxPass = 3;
        public const float LinkBuff = 0.20f;
        public const float LinkSec = 5f;
        public const float TurretSec = 8f;
        public const float TurretRangeM = 8f;
        public const float TurretShotSec = 1f;
        public const float TriggerRadiusM = 1f;

        readonly List<Mine> _mines = new();
        readonly List<Rope> _ropes = new();
        readonly List<Mark> _marks = new();
        readonly List<Link> _links = new();
        readonly List<Turret> _turrets = new();
        IReadOnlyDictionary<string, TeamOp> _ops;
        Ball _ball;
        Hang _hang;
        float _now;
        float _armorUntil;
        float _armorMult = 1f;
        bool _warnedShot;

        public void Clear()
        {
            _mines.Clear();
            _ropes.Clear();
            _marks.Clear();
            _links.Clear();
            _turrets.Clear();
            _haste.Clear();
            _ball = default;
            _hang = default;
            _now = 0f;
            _armorUntil = 0f;
            _armorMult = 1f;
            TurretShots = 0;
            LastCopiedSkill = string.Empty;
            LastMineMult = 0f;
            _linkBurnSource = string.Empty;
        }

        public float BossIncomingMult => _now < _armorUntil ? _armorMult : 1f;
        public bool AttackBroken => _hang.Active && _now < _hang.Until;
        public int BallPasses => _ball.Passes;
        public int BallHolder => _ball.Active && _now < _ball.Until ? _ball.Holder : 0;
        public int TurretShots { get; private set; }
        public string LastCopiedSkill { get; private set; } = string.Empty;
        public float LastMineMult { get; private set; }
        public bool MineAlive
        {
            get
            {
                for (int i = 0; i < _mines.Count; i++)
                {
                    if (!_mines[i].Spent && _now < _mines[i].Until)
                        return true;
                }
                return false;
            }
        }

        public bool TryMine(out float x, out float z)
        {
            for (int i = 0; i < _mines.Count; i++)
            {
                if (_mines[i].Spent || _now >= _mines[i].Until)
                    continue;
                x = _mines[i].X;
                z = _mines[i].Z;
                return true;
            }
            x = 0f;
            z = 0f;
            return false;
        }

        public bool TryRopeMid(out float x, out float z)
        {
            for (int i = 0; i < _ropes.Count; i++)
            {
                if (_ropes[i].Spent || _now >= _ropes[i].Until)
                    continue;
                x = (_ropes[i].Ax + _ropes[i].Bx) * 0.5f;
                z = (_ropes[i].Az + _ropes[i].Bz) * 0.5f;
                return true;
            }
            x = 0f;
            z = 0f;
            return false;
        }

        public bool TryTurret(out float x, out float z)
        {
            for (int i = 0; i < _turrets.Count; i++)
            {
                if (_now >= _turrets[i].Until)
                    continue;
                x = _turrets[i].X;
                z = _turrets[i].Z;
                return true;
            }
            x = 0f;
            z = 0f;
            return false;
        }

        public bool IsTeamSkill(SkillId skillId) =>
            TeamOpTable.Resolve(skillId, _ops) != TeamOp.None;

        public TeamComboSystem()
            : this(null)
        {
        }

        string _linkBurnSource = string.Empty;

        public string LinkBurnSourceSkillId => _linkBurnSource;

        public TeamComboSystem(IReadOnlyDictionary<string, TeamOp> ops)
        {
            _ops = ops ?? TeamOpTable.Empty;
        }

        /// <summary>Op tablosunu sonradan bağla (Unity MonoBehaviour ctor'unda Resources yüklenemez; host Awake'te çağırır).</summary>
        public void UseOps(IReadOnlyDictionary<string, TeamOp> ops) => _ops = ops ?? TeamOpTable.Empty;

        public TeamPulse Cast(SkillId skillId, IAllyPlayer caster, IAllyPlayer target, IReadOnlyList<IAllyPlayer> allies, in Disc boss)
        {
            if (caster == null || !IsTeamSkill(skillId))
                return TeamPulse.None;
            TeamOp op = TeamOpTable.Resolve(skillId, _ops);
            switch (op)
            {
                case TeamOp.Mine:
                    return PlantMine(caster, boss);
                case TeamOp.HangBoss:
                    return HangBoss(caster);
                case TeamOp.Rope:
                    return PlantRope(caster, boss);
                case TeamOp.Mark:
                    return ArmMark(caster);
                case TeamOp.Ball:
                    return ThrowBall(caster, target, allies);
                case TeamOp.Link:
                    return LinkPair(skillId, caster, target, allies, boss);
                case TeamOp.Turret:
                    return PlantTurret(caster);
                case TeamOp.HasteRope:
                    return HasteRope(caster, target, allies);
                case TeamOp.Marker:
                default:
                    return TeamPulse.None;
            }
        }

        public TeamPulse AllyUsedSkill(IAllyPlayer ally, SkillId skillId, float x, float z)
        {
            if (ally == null)
                return TeamPulse.None;
            TeamPulse pulse = TeamPulse.None;
            for (int i = 0; i < _mines.Count; i++)
            {
                Mine mine = _mines[i];
                if (mine.Spent || _now >= mine.Until)
                    continue;
                if (mine.Owner == ally.Id)
                    continue;
                if (Dist(x, z, mine.X, mine.Z) > TriggerRadiusM + ally.Radius)
                    continue;
                mine.Spent = true;
                LastMineMult = MinePower;
                pulse = new TeamPulse(false, 0f, BossIncomingMult, 0f, false, false, MinePower, string.Empty);
            }
            return pulse;
        }

        public TeamPulse AllyHit(IAllyPlayer ally, float x, float z, bool struckBoss)
        {
            if (ally == null)
                return TeamPulse.None;
            if (struckBoss)
                NoteMarkHit(ally.Id);

            for (int i = 0; i < _ropes.Count; i++)
            {
                Rope rope = _ropes[i];
                if (rope.Spent || _now >= rope.Until || rope.Owner == ally.Id)
                    continue;
                float mx = (rope.Ax + rope.Bx) * 0.5f;
                float mz = (rope.Az + rope.Bz) * 0.5f;
                if (Dist(x, z, mx, mz) > TriggerRadiusM + ally.Radius)
                    continue;
                rope.Spent = true;
                float dur = new StatusTuning().ArmorBreakMs / 1000f;
                SetArmor(1f + RopeArmorDrop, dur);
                return new TeamPulse(true, RopeStunSec, BossIncomingMult, dur, false, false, 0f, string.Empty);
            }
            return TeamPulse.None;
        }

        public bool PassBall(IAllyPlayer from, IAllyPlayer to)
        {
            if (from == null || to == null || !_ball.Active || _now >= _ball.Until)
                return false;
            if (from.Id != _ball.Holder || to.Id == from.Id)
                return false;
            if (_ball.Passes >= BallMaxPass)
                return false;
            _ball.Passes++;
            _ball.Holder = to.Id;
            _ball.Until = _now + BallSec;
            return true;
        }

        public string TouchTurret(IAllyPlayer ally)
        {
            if (ally == null || string.IsNullOrEmpty(ally.LastSkillId))
                return string.Empty;
            for (int i = 0; i < _turrets.Count; i++)
            {
                Turret turret = _turrets[i];
                if (_now >= turret.Until)
                    continue;
                if (Dist(ally.X, ally.Z, turret.X, turret.Z) > TriggerRadiusM + ally.Radius + TeamComboSystemDefaults.TriggerRadiusPaddingM)
                    continue;
                LastCopiedSkill = ally.LastSkillId;
                return LastCopiedSkill;
            }
            return string.Empty;
        }

        public TeamPulse Tick(float dt, IReadOnlyList<IAllyPlayer> allies, in Disc boss)
        {
            if (dt > 0f)
                _now += dt;
            bool burned = false;
            bool stunned = false;
            string copied = string.Empty;
            RefreshLinkPositions(allies);

            for (int i = 0; i < _marks.Count; i++)
            {
                Mark mark = _marks[i];
                if (mark.Done || _now < mark.At)
                    continue;
                mark.Done = true;
                float drop = mark.Hitters.Count >= 2 ? MarkArmorPair : MarkArmor;
                SetArmor(1f + drop, MarkArmorSec);
            }

            for (int i = 0; i < _links.Count; i++)
            {
                Link link = _links[i];
                if (link.Broken || _now >= link.Until)
                    continue;
                if (!boss.Present)
                    continue;
                if (!SegmentHits(link.Ax, link.Az, link.Bx, link.Bz, boss.X, boss.Z, boss.Radius))
                {
                    link.BurnAcc = 0f;
                    continue;
                }
                link.BurnAcc += dt;
                if (link.BurnAcc >= 1f)
                {
                    link.BurnAcc -= 1f;
                    burned = true;
                }
            }

            if (!_warnedShot)
            {
                _warnedShot = true;
                DesignWarnings.Once(
                    "team.turret.rate",
                    "144 kombo taret atış aralığı vermez; yedek 1 sn kullanıldı.");
            }

            for (int i = 0; i < _turrets.Count; i++)
            {
                Turret turret = _turrets[i];
                if (_now >= turret.Until || !boss.Present)
                    continue;
                if (Dist(turret.X, turret.Z, boss.X, boss.Z) > TurretRangeM)
                    continue;
                turret.Acc += dt;
                if (turret.Acc < TurretShotSec)
                    continue;
                turret.Acc -= TurretShotSec;
                TurretShots++;
            }

            return new TeamPulse(stunned, 0f, BossIncomingMult, 0f, burned, AttackBroken, 0f, copied);
        }

        public float DamageMult(int actorId)
        {
            float m = 1f;
            if (AttackBroken && actorId != _hang.Owner && actorId != 0)
                m *= 1f + HangDamageAdd;
            if (_ball.Active && _now < _ball.Until && actorId == _ball.Holder)
                m *= 1f + BallBuff;
            for (int i = 0; i < _links.Count; i++)
            {
                Link link = _links[i];
                if (link.Broken || _now >= link.Until)
                    continue;
                if (actorId == link.A || actorId == link.B)
                    m *= 1f + LinkBuff;
            }
            return m;
        }

        public float MoveSpeedMult(int actorId) => HasteFor(actorId);

        public float AttackSpeedMult(int actorId) => HasteFor(actorId);

        readonly List<HasteRopeState> _haste = new();

        float HasteFor(int actorId)
        {
            for (int i = 0; i < _haste.Count; i++)
            {
                if (_haste[i].Broken)
                    continue;
                if (_haste[i].A == actorId || _haste[i].B == actorId)
                    return _haste[i].Mult;
            }
            return 1f;
        }

    }
}
