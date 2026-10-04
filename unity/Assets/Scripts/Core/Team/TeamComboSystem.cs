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
    public readonly struct TeamPulse
    {
        public TeamPulse(
            bool stunned,
            float stunSec,
            float bossIncoming,
            float armorSec,
            bool burned,
            bool attackBroken,
            float mineMult,
            string copiedSkill)
        {
            Stunned = stunned;
            StunSec = stunSec;
            BossIncomingMult = bossIncoming;
            ArmorSec = armorSec;
            Burned = burned;
            AttackBroken = attackBroken;
            MineMult = mineMult;
            CopiedSkill = copiedSkill ?? string.Empty;
        }

        public bool Stunned { get; }
        public float StunSec { get; }
        public float BossIncomingMult { get; }
        public float ArmorSec { get; }
        public bool Burned { get; }
        public bool AttackBroken { get; }
        public float MineMult { get; }
        public string CopiedSkill { get; }

        public static TeamPulse None => new TeamPulse(false, 0f, 1f, 0f, false, false, 0f, string.Empty);
    }

    /// <summary>
    /// Takım kombosu: bir oyuncunun bıraktığı işi başka oyuncu bitirir.
    /// Hasar formülü burada yok; yalnız çarpan ve süre döner.
    /// Taret atış aralığı metinde yok — yedek 1 sn (DesignWarnings).
    /// 7-6 zırh süresi metinde yok — StatusTuning.ArmorBreakMs.
    /// 12-6 kopma, bağ kurulurkenki mesafenin üstüdür. Hız JSON'da 0; yedek +%50.
    /// </summary>
    public sealed class TeamComboSystem
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

        public bool IsTeamSkill(string skillId) =>
            TeamOpTable.Resolve(skillId, _ops) != TeamOp.None;

        public TeamComboSystem()
            : this(null)
        {
        }

        public TeamComboSystem(IReadOnlyDictionary<string, TeamOp> ops)
        {
            _ops = ops ?? TeamOpTable.Legacy;
        }

        /// <summary>Op tablosunu sonradan bağla (Unity MonoBehaviour ctor'unda Resources yüklenemez; host Awake'te çağırır).</summary>
        public void UseOps(IReadOnlyDictionary<string, TeamOp> ops) => _ops = ops ?? TeamOpTable.Legacy;

        public TeamPulse Cast(string skillId, IAllyPlayer caster, IAllyPlayer target, IReadOnlyList<IAllyPlayer> allies, in Disc boss)
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
                    return LinkPair(caster, target, allies, boss);
                case TeamOp.Turret:
                    return PlantTurret(caster);
                case TeamOp.HasteRope:
                    return HasteRope(caster, target, allies);
                case TeamOp.Marker:
                default:
                    return TeamPulse.None;
            }
        }

        public TeamPulse AllyUsedSkill(IAllyPlayer ally, string skillId, float x, float z)
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
                if (Dist(ally.X, ally.Z, turret.X, turret.Z) > TriggerRadiusM + ally.Radius + 0.4f)
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

        TeamPulse PlantMine(IAllyPlayer caster, in Disc boss)
        {
            float x = caster.X;
            float z = caster.Z;
            if (boss.Present)
            {
                float dx = boss.X - caster.X;
                float dz = boss.Z - caster.Z;
                float len = MathF.Sqrt(dx * dx + dz * dz);
                if (len > 0.2f)
                {
                    float u = MathF.Min(0.72f, (len - boss.Radius - TriggerRadiusM) / len);
                    if (u < 0.2f)
                        u = 0.2f;
                    x = caster.X + dx / len * len * u;
                    z = caster.Z + dz / len * len * u;
                }
            }
            _mines.Add(new Mine
            {
                Owner = caster.Id,
                X = x,
                Z = z,
                Until = _now + MineLifeSec,
                BossRadius = boss.Present ? boss.Radius : 0.85f
            });
            return TeamPulse.None;
        }

        TeamPulse HangBoss(IAllyPlayer caster)
        {
            _hang = new Hang { Active = true, Owner = caster.Id, Until = _now + HangSec };
            return new TeamPulse(true, HangSec, BossIncomingMult, 0f, false, true, 0f, string.Empty);
        }

        TeamPulse PlantRope(IAllyPlayer caster, in Disc boss)
        {
            float bx = boss.Present ? boss.X : caster.X;
            float bz = boss.Present ? boss.Z + 3f : caster.Z + 3f;
            _ropes.Add(new Rope
            {
                Owner = caster.Id,
                Ax = caster.X,
                Az = caster.Z,
                Bx = bx,
                Bz = bz,
                Until = _now + RopeLifeSec
            });
            return TeamPulse.None;
        }

        TeamPulse ArmMark(IAllyPlayer caster)
        {
            _marks.Add(new Mark
            {
                At = _now + MarkArmSec,
                Hitters = new HashSet<int>()
            });
            return TeamPulse.None;
        }

        void NoteMarkHit(int actorId)
        {
            for (int i = 0; i < _marks.Count; i++)
            {
                Mark mark = _marks[i];
                if (mark.Done || _now >= mark.At)
                    continue;
                mark.Hitters.Add(actorId);
            }
        }

        TeamPulse ThrowBall(IAllyPlayer caster, IAllyPlayer target, IReadOnlyList<IAllyPlayer> allies)
        {
            IAllyPlayer catcher = target != null && target.Id != caster.Id ? target : FirstOther(caster, allies);
            if (catcher == null)
                return TeamPulse.None;
            _ball = new Ball
            {
                Active = true,
                Holder = catcher.Id,
                Passes = 0,
                Until = _now + BallSec
            };
            return TeamPulse.None;
        }

        TeamPulse LinkPair(IAllyPlayer caster, IAllyPlayer target, IReadOnlyList<IAllyPlayer> allies, in Disc boss)
        {
            IAllyPlayer a = target != null && target.Id != caster.Id ? target : FirstOther(caster, allies);
            IAllyPlayer b = SecondOther(caster, a, allies);
            if (a == null || b == null)
                return TeamPulse.None;
            _links.Add(new Link
            {
                A = a.Id,
                B = b.Id,
                Ax = a.X,
                Az = a.Z,
                Bx = b.X,
                Bz = b.Z,
                Until = _now + LinkSec
            });
            return TeamPulse.None;
        }

        TeamPulse PlantTurret(IAllyPlayer caster)
        {
            _turrets.Add(new Turret
            {
                X = caster.X,
                Z = caster.Z,
                Until = _now + TurretSec
            });
            return TeamPulse.None;
        }

        TeamPulse HasteRope(IAllyPlayer caster, IAllyPlayer target, IReadOnlyList<IAllyPlayer> allies)
        {
            IAllyPlayer ally = target != null && target.Id != caster.Id ? target : FirstOther(caster, allies);
            if (ally == null)
                return TeamPulse.None;
            float mult = 1f + SkillNumberFallbacks.SelfHasteBonus;
            if (!DesignWarnings.WasWarned("team.haste"))
            {
                DesignWarnings.Once(
                    "team.haste",
                    "12-6 hızı element-sistemi.json self_haste 0; yedek +%50 kullanıldı.");
            }
            _haste.Add(new HasteRopeState
            {
                A = caster.Id,
                B = ally.Id,
                Length = Dist(caster.X, caster.Z, ally.X, ally.Z),
                Mult = mult
            });
            return TeamPulse.None;
        }

        void RefreshLinkPositions(IReadOnlyList<IAllyPlayer> allies)
        {
            if (allies == null)
                return;
            for (int i = 0; i < _haste.Count; i++)
            {
                HasteRopeState rope = _haste[i];
                if (rope.Broken)
                    continue;
                IAllyPlayer a = Find(allies, rope.A);
                IAllyPlayer b = Find(allies, rope.B);
                if (a == null || b == null)
                    continue;
                float dist = Dist(a.X, a.Z, b.X, b.Z);
                if (dist > rope.Length + MotionFallbacks.Coded.StopGapM)
                    rope.Broken = true;
            }

            for (int i = 0; i < _links.Count; i++)
            {
                Link link = _links[i];
                if (link.Broken || _now >= link.Until)
                    continue;
                IAllyPlayer a = Find(allies, link.A);
                IAllyPlayer b = Find(allies, link.B);
                if (a == null || b == null)
                    continue;
                link.Ax = a.X;
                link.Az = a.Z;
                link.Bx = b.X;
                link.Bz = b.Z;
            }
        }

        void SetArmor(float mult, float sec)
        {
            if (mult > _armorMult || _now >= _armorUntil)
                _armorMult = mult;
            float until = _now + sec;
            if (until > _armorUntil)
                _armorUntil = until;
        }

        static IAllyPlayer FirstOther(IAllyPlayer caster, IReadOnlyList<IAllyPlayer> allies)
        {
            if (allies == null)
                return null;
            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] != null && allies[i].Id != caster.Id)
                    return allies[i];
            }
            return null;
        }

        static IAllyPlayer SecondOther(IAllyPlayer caster, IAllyPlayer first, IReadOnlyList<IAllyPlayer> allies)
        {
            if (allies == null)
                return null;
            for (int i = 0; i < allies.Count; i++)
            {
                IAllyPlayer ally = allies[i];
                if (ally == null || ally.Id == caster.Id)
                    continue;
                if (first != null && ally.Id == first.Id)
                    continue;
                return ally;
            }
            return null;
        }

        static IAllyPlayer Find(IReadOnlyList<IAllyPlayer> allies, int id)
        {
            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] != null && allies[i].Id == id)
                    return allies[i];
            }
            return null;
        }

        static float Dist(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx;
            float dz = az - bz;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        static bool SegmentHits(float ax, float az, float bx, float bz, float cx, float cz, float radius)
        {
            float abx = bx - ax;
            float abz = bz - az;
            float len2 = abx * abx + abz * abz;
            float t = 0f;
            if (len2 > 0.0001f)
                t = Math.Clamp(((cx - ax) * abx + (cz - az) * abz) / len2, 0f, 1f);
            float dx = ax + abx * t - cx;
            float dz = az + abz * t - cz;
            return dx * dx + dz * dz <= radius * radius;
        }

        sealed class Mine
        {
            public int Owner;
            public float X;
            public float Z;
            public float Until;
            public float BossRadius;
            public bool Spent;
        }

        sealed class Rope
        {
            public int Owner;
            public float Ax, Az, Bx, Bz;
            public float Until;
            public bool Spent;
        }

        sealed class Mark
        {
            public float At;
            public bool Done;
            public HashSet<int> Hitters;
        }

        sealed class Link
        {
            public int A;
            public int B;
            public float Ax, Az, Bx, Bz;
            public float Until;
            public float BurnAcc;
            public bool Broken;
        }

        sealed class Turret
        {
            public float X;
            public float Z;
            public float Until;
            public float Acc;
        }

        struct Ball
        {
            public bool Active;
            public int Holder;
            public int Passes;
            public float Until;
        }

        struct Hang
        {
            public bool Active;
            public int Owner;
            public float Until;
        }

        sealed class HasteRopeState
        {
            public int A;
            public int B;
            public float Length;
            public float Mult;
            public bool Broken;
        }
    }
}