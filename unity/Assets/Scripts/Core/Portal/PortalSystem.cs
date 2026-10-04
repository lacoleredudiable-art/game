using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core.Motion;
using Dovus.Core.Status;

namespace Dovus.Core.Portal
{
    /// <summary>
    /// Portal etiketli 10 skill. Işın, kalıp sürerken yazılmaz; kalıp bitince ya da kalıp yokken uygulanır.
    /// Çıkış boss gövdesinin içinde kalamaz.
    /// Kapı yarıçapı: element-sistemi.json portal_trigger_radius_m = 1.
    /// </summary>
    public sealed partial class PortalSystem
    {
        public const float DoorRadiusM = 1f;
        public const float HookRangeM = 4f;
        public const float AnchorLifeSec = 5f;
        public const float AnchorAllyM = 3f;
        public const float TwoDoorSec = 4f;
        public const float MirrorSec = 3f;
        public const float GateSec = 6f;
        public const float ShrinkSpeedAdd = 0.30f;
        public const float ShrinkMiss = 0.30f;
        public const float BossShrinkSec = 2f;
        public const float BossStrikeScale = 0.7f;
        public const float GrowDamageAdd = 0.30f;
        public const float GrowTakenOff = 0.25f;
        public const float GrowSlow = 0.20f;
        public const float RiseDelaySec = 0.6f;
        public const float RiseDamageAdd = 0.10f;
        public const float RiseBuffSec = 3f;
        public const float TeamDelaySec = 1f;

        public static float BesideM => MotionFallbacks.Coded.StepM;
        public static float ClearGapM => MotionFallbacks.Coded.StopGapM;

        readonly List<Door> _doors = new();
        readonly List<Placement> _ready = new();
        readonly List<WaitMove> _wait = new();
        readonly List<Buff> _buffs = new();
        readonly List<Rise> _rises = new();
        readonly List<Gather> _gathers = new();
        readonly HashSet<long> _inside = new();
        readonly List<DoorView> _views = new();
        IReadOnlyDictionary<string, PortalOp> _ops;

        Anchor _anchor;
        int _seq = 1;
        float _now;
        int _hookActor = -1;
        float _hookFromX;
        float _hookFromZ;
        float _hookAllyX;
        float _hookAllyZ;
        float _hookAllyR;
        float _hookCasterR;

        public BackStrike Strike { get; private set; }
        public bool BossNarrow => NarrowLeft > 0f;
        public float NarrowLeft { get; private set; }
        public float StrikeScale => BossNarrow ? BossStrikeScale : 1f;
        public bool HasAnchor => _anchor.Alive && _now < _anchor.Until;

        public bool IsPortalSkill(SkillId skillId) =>
            PortalOpTable.Resolve(skillId, _ops) != PortalOp.None;

        public PortalSystem()
            : this(null)
        {
        }

        public PortalSystem(IReadOnlyDictionary<string, PortalOp> ops)
        {
            _ops = ops ?? PortalOpTable.Legacy;
        }

        /// <summary>Op tablosunu sonradan bağla (Unity MonoBehaviour ctor'unda Resources yüklenemez; host Awake'te çağırır).</summary>
        public void UseOps(IReadOnlyDictionary<string, PortalOp> ops) => _ops = ops ?? PortalOpTable.Legacy;

        public IReadOnlyList<DoorView> Doors
        {
            get
            {
                _views.Clear();
                for (int i = 0; i < _doors.Count; i++)
                {
                    Door d = _doors[i];
                    _views.Add(new DoorView(d.Id, d.Link, d.X, d.Z, DoorRadiusM, (SkillId)d.Skill, d.Until - _now, d.ShotsOnly));
                }
                return _views;
            }
        }

        public void Cast(SkillId skillId, in Body caster, in Body target, IReadOnlyList<Body> allies, in Disc boss)
        {
            PortalOp op = PortalOpTable.Resolve(skillId, _ops);
            switch (op)
            {
                case PortalOp.BackDoor:
                    OpenBackDoor(caster, boss);
                    break;
                case PortalOp.Hook:
                    ArmHook(caster, target, boss);
                    break;
                case PortalOp.AnchorOrRecall:
                    AnchorOrRecall(caster, allies, boss);
                    break;
                case PortalOp.Pair:
                    OpenPair(caster, boss, TwoDoorSec, false);
                    break;
                case PortalOp.Gate:
                    OpenGate(caster, target, boss, false);
                    break;
                case PortalOp.MirrorGate:
                    OpenGate(caster, target, boss, true);
                    break;
                case PortalOp.Swap:
                    Swap(caster, target, boss);
                    break;
                case PortalOp.Mirror:
                    OpenMirror(caster, boss);
                    break;
                case PortalOp.Sink:
                    Sink(caster, target, boss);
                    break;
                case PortalOp.GatherTeam:
                    GatherTeam(caster, allies);
                    break;
            }
        }

        public IReadOnlyList<Placement> Drain()
        {
            if (_ready.Count == 0)
                return Array.Empty<Placement>();
            Placement[] copy = _ready.ToArray();
            _ready.Clear();
            return copy;
        }

        public void NotifyTemplateEnded(int actorId, float x, float y, float z, float radius, in Disc boss)
        {
            float cx = x;
            float cy = y;
            float cz = z;
            if (_hookActor == actorId)
            {
                float r = radius > 0f ? radius : _hookCasterR;
                bool already = HookAlreadyLanded(x, y, z, r, boss);
                _hookActor = -1;
                if (!already)
                {
                    HookLanding(_hookFromX, _hookFromZ, r, _hookAllyX, _hookAllyZ, _hookAllyR, boss, out cx, out cy, out cz);
                    if (Moved(x, y, z, cx, cy, cz))
                        _ready.Add(new Placement(actorId, cx, cy, cz, (SkillId)SkillIds.OpeningHeal, false));
                }
            }

            for (int i = 0; i < _doors.Count; i++)
            {
                Door d = _doors[i];
                if (d.PendingArrival && d.Owner == actorId)
                {
                    d.X = cx;
                    d.Z = cz;
                    d.PendingArrival = false;
                    FaceAway(d, boss);
                }
            }

            for (int i = _wait.Count - 1; i >= 0; i--)
            {
                if (_wait[i].WaitFor != actorId)
                    continue;
                WaitMove w = _wait[i];
                float px = w.X;
                float pz = w.Z;
                PushOut(ref px, ref pz, w.Radius, boss);
                _ready.Add(new Placement(w.ActorId, px, w.Y, pz, (SkillId)w.Skill, w.Transfer, w.Teleport));
                _wait.RemoveAt(i);
            }

            // İniş kapının içinde biter. Bu kapıya yeni giriş sayılmaz; çıkıp
            // tekrar girmeden karşı kapıya ışınlanmaz (3-10 dash sonu).
            RememberStandingInDoor(actorId, cx, cz);
        }

        void RememberStandingInDoor(int actorId, float x, float z)
        {
            for (int i = 0; i < _doors.Count; i++)
            {
                Door door = _doors[i];
                if (door.PendingArrival || _now >= door.Until)
                    continue;
                if (Dist(x, z, door.X, door.Z) > DoorRadiusM)
                    continue;
                _inside.Add(Key(actorId, door.Id));
            }
        }

        public bool Sense(in Body body, bool projectile, in Disc boss, out Placement move)
        {
            move = default;
            for (int i = _doors.Count - 1; i >= 0; i--)
            {
                Door door = _doors[i];
                if (door.PendingArrival)
                    continue;
                if (_now >= door.Until)
                    continue;
                float dist = Dist(body.X, body.Z, door.X, door.Z);
                long key = Key(body.Id, door.Id);
                if (dist > DoorRadiusM + (projectile ? PortalSystemDefaults.DoorPaddingM : 0f))
                {
                    _inside.Remove(key);
                    continue;
                }

                if (_inside.Contains(key))
                    continue;
                _inside.Add(key);

                if (door.Gate)
                {
                    ApplyGate(body, door);
                    continue;
                }

                if (door.ShotsOnly && !projectile)
                    continue;
                if (door.Link == 0)
                    continue;

                Door exit = Find(door.Link);
                if (exit == null || exit.PendingArrival)
                    continue;

                _inside.Add(Key(body.Id, exit.Id));
                float ox = exit.X + exit.Fx * (DoorRadiusM + body.Radius + PortalSystemDefaults.ExitPaddingM);
                float oz = exit.Z + exit.Fz * (DoorRadiusM + body.Radius + PortalSystemDefaults.ExitPaddingM);
                PushOut(ref ox, ref oz, body.Radius, boss);
                if (body.IsBoss)
                    continue;
                var place = new Placement(body.Id, ox, body.Y, oz, (SkillId)door.Skill, false, true);
                if (body.TemplateOwns && !projectile)
                    _wait.Add(new WaitMove(body.Id, body.Id, ox, body.Y, oz, body.Radius, door.Skill, false, true));
                else
                {
                    _ready.Add(place);
                    move = place;
                    return true;
                }
            }
            return false;
        }

        public PortalBuff BuffFor(int actorId)
        {
            float move = 1f;
            float damage = 1f;
            float taken = 1f;
            float miss = 0f;
            for (int i = 0; i < _buffs.Count; i++)
            {
                Buff b = _buffs[i];
                if (b.ActorId != actorId || _now >= b.Until)
                    continue;
                move *= b.Move;
                damage *= b.Damage;
                taken *= b.Taken;
                if (b.Miss > miss)
                    miss = b.Miss;
            }
            return new PortalBuff(move, damage, taken, miss, false);
        }

        public void Tick(float dt, in Body caster, IReadOnlyList<Body> allies, in Disc boss)
        {
            if (dt > 0f)
                _now += dt;
            if (NarrowLeft > 0f)
            {
                NarrowLeft -= dt;
                if (NarrowLeft < 0f)
                    NarrowLeft = 0f;
            }

            for (int i = _doors.Count - 1; i >= 0; i--)
            {
                if (_now >= _doors[i].Until)
                    _doors.RemoveAt(i);
            }

            if (_anchor.Alive && _now >= _anchor.Until)
                _anchor.Alive = false;

            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                if (_now >= _buffs[i].Until)
                    _buffs.RemoveAt(i);
            }

            for (int i = 0; i < _rises.Count; i++)
            {
                Rise r = _rises[i];
                if (r.Done)
                    continue;
                if (_now < r.At)
                    continue;
                r.Done = true;
                float x = caster.X + BesideM;
                float z = caster.Z;
                PushOut(ref x, ref z, r.Radius, boss);
                _ready.Add(new Placement(r.ActorId, x, r.Y, z, (SkillId)SkillIds.RisingSummon, false, true));
                _buffs.Add(new Buff(r.ActorId, _now + RiseBuffSec, 1f, 1f + RiseDamageAdd, 1f, 0f));
            }

            for (int i = 0; i < _gathers.Count; i++)
            {
                Gather g = _gathers[i];
                if (g.Done || _now < g.At)
                    continue;
                g.Done = true;
                int n = 0;
                if (allies == null)
                    continue;
                for (int a = 0; a < allies.Count; a++)
                {
                    Body ally = allies[a];
                    if (ally.Id == caster.Id || ally.IsBoss || g.Skip.Contains(ally.Id))
                        continue;
                    float ang = n * PortalSystemDefaults.PortalAngleStepMult;
                    float x = caster.X + MathF.Cos(ang) * BesideM;
                    float z = caster.Z + MathF.Sin(ang) * BesideM;
                    PushOut(ref x, ref z, ally.Radius, boss);
                    _ready.Add(new Placement(ally.Id, x, ally.Y, z, (SkillId)SkillIds.MirrorSummon, false, true));
                    n++;
                }
            }
        }

        public static bool Overlaps(float x, float z, float radius, in Disc boss)
        {
            if (!boss.Present)
                return false;
            float need = radius + boss.Radius + boss.Gap;
            return Dist(x, z, boss.X, boss.Z) < need - 0.001f;
        }

        public static void PushOut(ref float x, ref float z, float radius, in Disc boss)
        {
            if (!boss.Present)
                return;
            float dx = x - boss.X;
            float dz = z - boss.Z;
            float dist = MathF.Sqrt(dx * dx + dz * dz);
            float need = radius + boss.Radius + boss.Gap;
            if (dist >= need)
                return;
            if (dist < 0.001f)
            {
                x = boss.X + need;
                z = boss.Z;
                return;
            }
            float s = need / dist;
            x = boss.X + dx * s;
            z = boss.Z + dz * s;
        }

        /// <summary>Kapı, çapa, kanca ve buff kalmaz. Bir sonraki vaka temiz başlar.</summary>
        public void Clear()
        {
            _doors.Clear();
            _ready.Clear();
            _wait.Clear();
            _buffs.Clear();
            _rises.Clear();
            _gathers.Clear();
            _inside.Clear();
            _views.Clear();
            _anchor = default;
            _hookActor = -1;
            _now = 0f;
            NarrowLeft = 0f;
            Strike = default;
        }
    }
}
