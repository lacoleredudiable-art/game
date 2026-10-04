using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core.Motion;
using Dovus.Core.Status;

namespace Dovus.Core.Portal
{
    public sealed partial class PortalSystem
    {
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
    }
}
