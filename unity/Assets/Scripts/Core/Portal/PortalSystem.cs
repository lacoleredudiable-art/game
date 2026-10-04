using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core.Motion;
using Dovus.Core.Status;

namespace Dovus.Core.Portal
{
    public readonly struct Disc
    {
        public Disc(bool present, float x, float z, float radius, float gap)
        {
            Present = present;
            X = x;
            Z = z;
            Radius = radius > 0f ? radius : 0f;
            Gap = gap > 0f ? gap : 0f;
        }

        public bool Present { get; }
        public float X { get; }
        public float Z { get; }
        public float Radius { get; }
        public float Gap { get; }

        public static Disc None => new Disc(false, 0f, 0f, 0f, 0f);
    }

    public readonly struct Body
    {
        public Body(int id, float x, float y, float z, float radius, bool templateOwns, bool isBoss)
        {
            Id = id;
            X = x;
            Y = y;
            Z = z;
            Radius = radius > 0f ? radius : 0.5f;
            TemplateOwns = templateOwns;
            IsBoss = isBoss;
        }

        public int Id { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float Radius { get; }
        public bool TemplateOwns { get; }
        public bool IsBoss { get; }
    }

    public readonly struct Placement
    {
        public Placement(int actorId, float x, float y, float z, string skillId, bool transferDebuffs, bool teleport = false)
        {
            ActorId = actorId;
            X = x;
            Y = y;
            Z = z;
            SkillId = skillId ?? string.Empty;
            TransferDebuffs = transferDebuffs;
            Teleport = teleport;
        }

        public int ActorId { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public string SkillId { get; }
        public bool TransferDebuffs { get; }
        /// <summary>Yer değiştirme, çapa dönüşü veya kapı geçişi. Tarama yalnız bu kareyi ışın sayar.</summary>
        public bool Teleport { get; }
    }

    public readonly struct BackStrike
    {
        public BackStrike(bool active, float x, float z, float dirX, float dirZ)
        {
            Active = active;
            X = x;
            Z = z;
            DirX = dirX;
            DirZ = dirZ;
        }

        public bool Active { get; }
        public float X { get; }
        public float Z { get; }
        public float DirX { get; }
        public float DirZ { get; }
    }

    public readonly struct DoorView
    {
        public DoorView(int id, int linkId, float x, float z, float radius, string skillId, float lifeSec, bool projectilesOnly)
        {
            Id = id;
            LinkId = linkId;
            X = x;
            Z = z;
            Radius = radius;
            SkillId = skillId ?? string.Empty;
            LifeSec = lifeSec;
            ProjectilesOnly = projectilesOnly;
        }

        public int Id { get; }
        public int LinkId { get; }
        public float X { get; }
        public float Z { get; }
        public float Radius { get; }
        public string SkillId { get; }
        public float LifeSec { get; }
        public bool ProjectilesOnly { get; }
    }

    public readonly struct PortalBuff
    {
        public PortalBuff(float move, float damage, float taken, float miss, bool narrow)
        {
            MoveSpeedMult = move;
            DamageMult = damage;
            DamageTakenMult = taken;
            MissChance = miss;
            NarrowStrikes = narrow;
        }

        public float MoveSpeedMult { get; }
        public float DamageMult { get; }
        public float DamageTakenMult { get; }
        public float MissChance { get; }
        public bool NarrowStrikes { get; }

        public static PortalBuff None => new PortalBuff(1f, 1f, 1f, 0f, false);
    }

    /// <summary>
    /// Portal etiketli 10 skill. Işın, kalıp sürerken yazılmaz; kalıp bitince ya da kalıp yokken uygulanır.
    /// Çıkış boss gövdesinin içinde kalamaz.
    /// Kapı yarıçapı: element-sistemi.json portal_trigger_radius_m = 1.
    /// </summary>
    public sealed class PortalSystem
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

        public bool IsPortalSkill(string skillId) =>
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
                    _views.Add(new DoorView(d.Id, d.Link, d.X, d.Z, DoorRadiusM, d.Skill, d.Until - _now, d.ShotsOnly));
                }
                return _views;
            }
        }

        public void Cast(string skillId, in Body caster, in Body target, IReadOnlyList<Body> allies, in Disc boss)
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
                        _ready.Add(new Placement(actorId, cx, cy, cz, "2-6", false));
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
                _ready.Add(new Placement(w.ActorId, px, w.Y, pz, w.Skill, w.Transfer, w.Teleport));
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
                var place = new Placement(body.Id, ox, body.Y, oz, door.Skill, false, true);
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
                _ready.Add(new Placement(r.ActorId, x, r.Y, z, "11-8", false, true));
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
                    _ready.Add(new Placement(ally.Id, x, ally.Y, z, "11-10", false, true));
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

        /// <summary>
        /// Kalıp oyuncuyu dostun berisine ve boss'un dışına indirdiyse tekrar oturtma.
        /// Zemin yüksekliği (ayaklar ~1 m) kancayı yeniden tetiklemez.
        /// Dostun ötesinde ya da boss'un içindeyse kanca düzeltmesi gerekir.
        /// </summary>
        bool HookAlreadyLanded(float x, float y, float z, float radius, in Disc boss)
        {
            _ = y;
            if (Overlaps(x, z, radius, boss))
                return false;
            float dx = _hookAllyX - _hookFromX;
            float dz = _hookAllyZ - _hookFromZ;
            float alongAlly = dx * dx + dz * dz;
            float along = (x - _hookFromX) * dx + (z - _hookFromZ) * dz;
            return along <= alongAlly + PortalSystemDefaults.MinDistM;
        }

        /// <summary>
        /// Oyuncu dostun yakın kenarında, yerde biter. Dostun ötesine geçmez, boss'un içine inmez.
        /// </summary>
        public static void HookLanding(
            float casterX, float casterZ, float casterR,
            float allyX, float allyZ, float allyR,
            in Disc boss,
            out float x, out float y, out float z)
        {
            float ux = 0f;
            float uz = 1f;
            float dx = allyX - casterX;
            float dz = allyZ - casterZ;
            float len = MathF.Sqrt(dx * dx + dz * dz);
            if (len > 0.001f)
            {
                ux = dx / len;
                uz = dz / len;
            }

            float sep = casterR + allyR + ClearGapM;
            x = allyX - ux * sep;
            z = allyZ - uz * sep;
            y = 0f;
            KeepOnNearSide(casterX, casterZ, allyX, allyZ, ref x, ref z);
            PushOut(ref x, ref z, casterR, boss);
            KeepOnNearSide(casterX, casterZ, allyX, allyZ, ref x, ref z);
            PushOut(ref x, ref z, casterR, boss);
        }

        /// <summary>
        /// Dost, atan kişinin yanına en çok 4 m gelir. Boss'un ötesine veya içine inmez. Yerde biter.
        /// </summary>
        public static void AllyPullLanding(
            float casterX, float casterZ,
            float allyX, float allyZ, float allyR,
            in Disc boss,
            out float x, out float y, out float z)
        {
            float dx = allyX - casterX;
            float dz = allyZ - casterZ;
            float len = MathF.Sqrt(dx * dx + dz * dz);
            float ux = 0f;
            float uz = 1f;
            if (len > 0.001f)
            {
                ux = dx / len;
                uz = dz / len;
            }

            float dist = MathF.Min(HookRangeM, MathF.Max(0f, len - allyR - ClearGapM));
            x = casterX + ux * dist;
            z = casterZ + uz * dist;
            y = 0f;
            if (boss.Present)
            {
                float bx = boss.X - casterX;
                float bz = boss.Z - casterZ;
                float bl = MathF.Sqrt(bx * bx + bz * bz);
                if (bl > PortalSystemDefaults.ExitPaddingM)
                {
                    float bux = bx / bl;
                    float buz = bz / bl;
                    float along = (x - casterX) * bux + (z - casterZ) * buz;
                    float stop = bl - (boss.Radius + allyR + boss.Gap);
                    if (stop < 0f)
                        stop = 0f;
                    if (along > stop)
                    {
                        x = casterX + bux * stop;
                        z = casterZ + buz * stop;
                    }
                }
            }
            PushOut(ref x, ref z, allyR, boss);
        }

        static void KeepOnNearSide(float casterX, float casterZ, float allyX, float allyZ, ref float x, ref float z)
        {
            float dx = allyX - casterX;
            float dz = allyZ - casterZ;
            float len = MathF.Sqrt(dx * dx + dz * dz);
            if (len < 0.001f)
                return;
            float ux = dx / len;
            float uz = dz / len;
            float along = (x - casterX) * ux + (z - casterZ) * uz;
            if (along > len)
            {
                x = casterX + ux * len;
                z = casterZ + uz * len;
            }
        }

        public static int MoveHostile(StatusBoard from, StatusBoard to)
        {
            if (from == null || to == null)
                return 0;
            var stolen = new List<StatusKind>();
            foreach (StatusKind kind in from.ActiveKinds)
            {
                if (StatusKindUtil.IsHardCc(kind) || StatusKindUtil.IsSoftCc(kind) || StatusKindUtil.IsDebuff(kind))
                    stolen.Add(kind);
            }

            int n = 0;
            for (int i = 0; i < stolen.Count; i++)
            {
                StatusKind kind = stolen[i];
                if (!from.TryGet(kind, out double remain, out float mag, out _))
                    continue;
                if (remain <= 0)
                    continue;
                to.Apply(kind, remain, mag, "9-10");
                n++;
            }

            if (stolen.Count > 0)
                from.RemoveKinds(stolen);
            return n;
        }

        void OpenBackDoor(in Body caster, in Disc boss)
        {
            if (!boss.Present)
                return;
            float dx = boss.X - caster.X;
            float dz = boss.Z - caster.Z;
            float len = MathF.Sqrt(dx * dx + dz * dz);
            float ux = 0f;
            float uz = 1f;
            if (len > 0.001f)
            {
                ux = dx / len;
                uz = dz / len;
            }

            float x = boss.X + ux * (boss.Radius + caster.Radius + boss.Gap + PortalSystemDefaults.BossGapOffsetM);
            float z = boss.Z + uz * (boss.Radius + caster.Radius + boss.Gap + PortalSystemDefaults.BossGapOffsetM);
            PushOut(ref x, ref z, PortalSystemDefaults.DoorPaddingM, boss);
            float dirX = boss.X - x;
            float dirZ = boss.Z - z;
            float dlen = MathF.Sqrt(dirX * dirX + dirZ * dirZ);
            if (dlen > 0.001f)
            {
                dirX /= dlen;
                dirZ /= dlen;
            }
            Strike = new BackStrike(true, x, z, dirX, dirZ);
        }

        void ArmHook(in Body caster, in Body ally, in Disc boss)
        {
            if (ally.Id == 0 || ally.Id == caster.Id)
                return;
            AllyPullLanding(caster.X, caster.Z, ally.X, ally.Z, ally.Radius, boss, out float x, out float y, out float z);
            if (caster.TemplateOwns)
            {
                _hookActor = caster.Id;
                _hookFromX = caster.X;
                _hookFromZ = caster.Z;
                _hookAllyX = ally.X;
                _hookAllyZ = ally.Z;
                _hookAllyR = ally.Radius;
                _hookCasterR = caster.Radius;
                _wait.Add(new WaitMove(caster.Id, ally.Id, x, y, z, ally.Radius, "2-6", false));
                return;
            }

            _ready.Add(new Placement(ally.Id, x, y, z, "2-6", false));
            HookLanding(caster.X, caster.Z, caster.Radius, ally.X, ally.Z, ally.Radius, boss, out float px, out float py, out float pz);
            if (Moved(caster.X, caster.Y, caster.Z, px, py, pz))
                _ready.Add(new Placement(caster.Id, px, py, pz, "2-6", false));
        }

        void AnchorOrRecall(in Body caster, IReadOnlyList<Body> allies, in Disc boss)
        {
            if (HasAnchor)
            {
                Recall(caster, allies, boss);
                _anchor.Alive = false;
                return;
            }

            _anchor = new Anchor
            {
                Alive = true,
                X = caster.X,
                Z = caster.Z,
                Until = _now + AnchorLifeSec
            };
        }

        void Recall(in Body caster, IReadOnlyList<Body> allies, in Disc boss)
        {
            float x = _anchor.X;
            float z = _anchor.Z;
            PushOut(ref x, ref z, caster.Radius, boss);
            Emit(caster, x, z, caster.Radius, "3-4", false, boss);
            if (allies == null)
                return;
            int n = 1;
            for (int i = 0; i < allies.Count; i++)
            {
                Body ally = allies[i];
                if (ally.Id == caster.Id || ally.IsBoss)
                    continue;
                if (Dist(ally.X, ally.Z, caster.X, caster.Z) > AnchorAllyM)
                    continue;
                float ax = _anchor.X + MathF.Cos(n) * BesideM * 0.5f;
                float az = _anchor.Z + MathF.Sin(n) * BesideM * 0.5f;
                PushOut(ref ax, ref az, ally.Radius, boss);
                Emit(ally, ax, az, ally.Radius, "3-4", false, boss);
                n++;
            }
        }

        void OpenPair(in Body caster, in Disc boss, float life, bool shotsOnly)
        {
            var a = NewDoor(caster.X, caster.Z, "3-10", life, shotsOnly, caster.Id);
            var b = NewDoor(caster.X, caster.Z, "3-10", life, shotsOnly, caster.Id);
            a.Link = b.Id;
            b.Link = a.Id;
            b.PendingArrival = caster.TemplateOwns;
            if (!caster.TemplateOwns)
            {
                float x = caster.X;
                float z = caster.Z + BesideM * 2f;
                PushOut(ref x, ref z, caster.Radius, boss);
                b.X = x;
                b.Z = z;
            }
            Face(a, b.X - a.X, b.Z - a.Z);
            Face(b, a.X - b.X, a.Z - b.Z);
            _doors.Add(a);
            _doors.Add(b);
        }

        void OpenMirror(in Body caster, in Disc boss)
        {
            float dx = 0f;
            float dz = 1f;
            if (boss.Present)
            {
                dx = boss.X - caster.X;
                dz = boss.Z - caster.Z;
                float len = MathF.Sqrt(dx * dx + dz * dz);
                if (len > 0.001f)
                {
                    dx /= len;
                    dz /= len;
                }
            }

            float fx = caster.X + dx * (caster.Radius + DoorRadiusM);
            float fz = caster.Z + dz * (caster.Radius + DoorRadiusM);
            PushOut(ref fx, ref fz, PortalSystemDefaults.DoorPaddingM, boss);
            var front = NewDoor(fx, fz, "10-10", MirrorSec, true, caster.Id);
            float bx = boss.Present ? boss.X + dx * (boss.Radius + caster.Radius + boss.Gap + PortalSystemDefaults.BossGapOffsetM) : fx + dx * PortalSystemDefaults.FallbackOffsetM;
            float bz = boss.Present ? boss.Z + dz * (boss.Radius + caster.Radius + boss.Gap + PortalSystemDefaults.BossGapOffsetM) : fz + dz * PortalSystemDefaults.FallbackOffsetM;
            PushOut(ref bx, ref bz, PortalSystemDefaults.DoorPaddingM, boss);
            var back = NewDoor(bx, bz, "10-10", MirrorSec, true, caster.Id);
            front.Link = back.Id;
            back.Link = front.Id;
            // Arkadaki kapıdan çıkan atış boss'un sırtına bakar.
            Face(back, boss.Present ? boss.X - bx : -dx, boss.Present ? boss.Z - bz : -dz);
            Face(front, dx, dz);
            _doors.Add(front);
            _doors.Add(back);
        }

        void OpenGate(in Body caster, in Body target, in Disc boss, bool grow)
        {
            Body who = target.Id != 0 && target.Id != caster.Id ? target : caster;
            float dx = boss.Present ? boss.X - who.X : caster.X - who.X;
            float dz = boss.Present ? boss.Z - who.Z : caster.Z - who.Z;
            float len = MathF.Sqrt(dx * dx + dz * dz);
            if (len < 0.001f)
            {
                dx = 0f;
                dz = 1f;
                len = 1f;
            }
            float x = who.X + dx / len * (who.Radius + PortalSystemDefaults.StandOffM);
            float z = who.Z + dz / len * (who.Radius + PortalSystemDefaults.StandOffM);
            PushOut(ref x, ref z, PortalSystemDefaults.DoorPaddingM, boss);
            Door door = NewDoor(x, z, grow ? "8-8" : "8-1", GateSec, false, caster.Id);
            door.Gate = true;
            door.Grow = grow;
            _doors.Add(door);
        }

        void ApplyGate(in Body body, Door door)
        {
            if (body.IsBoss)
            {
                if (!door.Grow)
                    NarrowLeft = BossShrinkSec;
                return;
            }

            if (door.Grow)
            {
                _buffs.Add(new Buff(
                    body.Id,
                    _now + GateSec,
                    1f - GrowSlow,
                    1f + GrowDamageAdd,
                    1f - GrowTakenOff,
                    0f));
            }
            else
            {
                _buffs.Add(new Buff(
                    body.Id,
                    _now + GateSec,
                    1f + ShrinkSpeedAdd,
                    1f,
                    1f,
                    ShrinkMiss));
            }
        }

        void Swap(in Body caster, in Body ally, in Disc boss)
        {
            if (ally.Id == 0 || ally.Id == caster.Id || ally.IsBoss)
                return;
            float ax = ally.X;
            float az = ally.Z;
            float cx = caster.X;
            float cz = caster.Z;
            PushOut(ref ax, ref az, caster.Radius, boss);
            PushOut(ref cx, ref cz, ally.Radius, boss);
            bool wait = caster.TemplateOwns;
            if (wait)
            {
                _wait.Add(new WaitMove(caster.Id, caster.Id, ax, caster.Y, az, caster.Radius, "9-10", false, true));
                _wait.Add(new WaitMove(caster.Id, ally.Id, cx, ally.Y, cz, ally.Radius, "9-10", true, true));
            }
            else
            {
                _ready.Add(new Placement(caster.Id, ax, caster.Y, az, "9-10", false, true));
                _ready.Add(new Placement(ally.Id, cx, ally.Y, cz, "9-10", true, true));
            }
        }

        void Sink(in Body caster, in Body ally, in Disc boss)
        {
            if (ally.Id == 0 || ally.Id == caster.Id)
                return;
            _rises.Add(new Rise
            {
                ActorId = ally.Id,
                At = _now + RiseDelaySec,
                Radius = ally.Radius,
                Y = ally.Y
            });
        }

        void GatherTeam(in Body caster, IReadOnlyList<Body> allies)
        {
            var g = new Gather
            {
                At = _now + TeamDelaySec,
                Skip = new HashSet<int>()
            };
            _gathers.Add(g);
            var big = NewDoor(caster.X + BesideM, caster.Z, "11-10", TeamDelaySec + PortalSystemDefaults.DoorPaddingM, false, caster.Id);
            _doors.Add(big);
            if (allies == null)
                return;
            for (int i = 0; i < allies.Count; i++)
            {
                Body ally = allies[i];
                if (ally.Id == caster.Id || ally.IsBoss)
                    continue;
                _doors.Add(NewDoor(ally.X, ally.Z, "11-10", TeamDelaySec, false, caster.Id));
            }
        }

        void Emit(in Body body, float x, float z, float radius, string skill, bool transfer, in Disc boss)
        {
            PushOut(ref x, ref z, radius, boss);
            if (body.TemplateOwns)
                _wait.Add(new WaitMove(body.Id, body.Id, x, body.Y, z, radius, skill, transfer, true));
            else
                _ready.Add(new Placement(body.Id, x, body.Y, z, skill, transfer, true));
        }

        Door NewDoor(float x, float z, string skill, float life, bool shots, int owner)
        {
            return new Door
            {
                Id = _seq++,
                X = x,
                Z = z,
                Skill = skill,
                Until = _now + life,
                ShotsOnly = shots,
                Owner = owner,
                Fx = 0f,
                Fz = 1f
            };
        }

        Door Find(int id)
        {
            for (int i = 0; i < _doors.Count; i++)
            {
                if (_doors[i].Id == id)
                    return _doors[i];
            }
            return null;
        }

        static void Face(Door door, float x, float z)
        {
            float len = MathF.Sqrt(x * x + z * z);
            if (len < 0.001f)
                return;
            door.Fx = x / len;
            door.Fz = z / len;
        }

        static void FaceAway(Door door, in Disc boss)
        {
            if (!boss.Present)
                return;
            Face(door, door.X - boss.X, door.Z - boss.Z);
        }

        static bool Moved(float x, float y, float z, float x2, float y2, float z2) =>
            MathF.Abs(x - x2) > PortalSystemDefaults.PortalPosEpsilonM || MathF.Abs(y - y2) > PortalSystemDefaults.PortalPosEpsilonM || MathF.Abs(z - z2) > PortalSystemDefaults.PortalPosEpsilonM;

        static float Dist(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx;
            float dz = az - bz;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        static long Key(int actor, int door) => ((long)actor << PortalSystemDefaults.ActorDoorKeyShiftBits) ^ (uint)door;

        sealed class Door
        {
            public int Id;
            public int Link;
            public float X;
            public float Z;
            public float Fx;
            public float Fz;
            public string Skill;
            public float Until;
            public bool ShotsOnly;
            public bool Gate;
            public bool Grow;
            public bool PendingArrival;
            public int Owner;
        }

        struct Anchor
        {
            public bool Alive;
            public float X;
            public float Z;
            public float Until;
        }

        readonly struct WaitMove
        {
            public WaitMove(int waitFor, int actorId, float x, float y, float z, float radius, string skill, bool transfer, bool teleport = false)
            {
                WaitFor = waitFor;
                ActorId = actorId;
                X = x;
                Y = y;
                Z = z;
                Radius = radius;
                Skill = skill;
                Transfer = transfer;
                Teleport = teleport;
            }

            public int WaitFor { get; }
            public int ActorId { get; }
            public float X { get; }
            public float Y { get; }
            public float Z { get; }
            public float Radius { get; }
            public string Skill { get; }
            public bool Transfer { get; }
            public bool Teleport { get; }
        }

        readonly struct Buff
        {
            public Buff(int actorId, float until, float move, float damage, float taken, float miss)
            {
                ActorId = actorId;
                Until = until;
                Move = move;
                Damage = damage;
                Taken = taken;
                Miss = miss;
            }

            public int ActorId { get; }
            public float Until { get; }
            public float Move { get; }
            public float Damage { get; }
            public float Taken { get; }
            public float Miss { get; }
        }

        sealed class Rise
        {
            public int ActorId;
            public float At;
            public float Radius;
            public float Y;
            public bool Done;
        }

        sealed class Gather
        {
            public float At;
            public bool Done;
            public HashSet<int> Skip;
        }
    }
}
