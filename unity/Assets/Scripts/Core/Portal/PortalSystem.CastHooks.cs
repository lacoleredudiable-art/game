using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core.Motion;
using Dovus.Core.Status;

namespace Dovus.Core.Portal
{
    public sealed partial class PortalSystem
    {
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
                to.Apply(kind, remain, mag, SkillIds.MirrorPurify);
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
                _wait.Add(new WaitMove(caster.Id, ally.Id, x, y, z, ally.Radius, SkillIds.OpeningHeal, false));
                return;
            }

            _ready.Add(new Placement(ally.Id, x, y, z, (SkillId)SkillIds.OpeningHeal, false));
            HookLanding(caster.X, caster.Z, caster.Radius, ally.X, ally.Z, ally.Radius, boss, out float px, out float py, out float pz);
            if (Moved(caster.X, caster.Y, caster.Z, px, py, pz))
                _ready.Add(new Placement(caster.Id, px, py, pz, (SkillId)SkillIds.OpeningHeal, false));
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
            Emit(caster, x, z, caster.Radius, SkillIds.FixedStep, false, boss);
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
                Emit(ally, ax, az, ally.Radius, SkillIds.FixedStep, false, boss);
                n++;
            }
        }

        void OpenPair(in Body caster, in Disc boss, float life, bool shotsOnly)
        {
            var a = NewDoor(caster.X, caster.Z, SkillIds.MirrorStep, life, shotsOnly, caster.Id);
            var b = NewDoor(caster.X, caster.Z, SkillIds.MirrorStep, life, shotsOnly, caster.Id);
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
            var front = NewDoor(fx, fz, SkillIds.MirrorReflect, MirrorSec, true, caster.Id);
            float bx = boss.Present ? boss.X + dx * (boss.Radius + caster.Radius + boss.Gap + PortalSystemDefaults.BossGapOffsetM) : fx + dx * PortalSystemDefaults.FallbackOffsetM;
            float bz = boss.Present ? boss.Z + dz * (boss.Radius + caster.Radius + boss.Gap + PortalSystemDefaults.BossGapOffsetM) : fz + dz * PortalSystemDefaults.FallbackOffsetM;
            PushOut(ref bx, ref bz, PortalSystemDefaults.DoorPaddingM, boss);
            var back = NewDoor(bx, bz, SkillIds.MirrorReflect, MirrorSec, true, caster.Id);
            front.Link = back.Id;
            back.Link = front.Id;
            // Arkadaki kapıdan çıkan atış boss'un sırtına bakar.
            Face(back, boss.Present ? boss.X - bx : -dx, boss.Present ? boss.Z - bz : -dz);
            Face(front, dx, dz);
            _doors.Add(front);
            _doors.Add(back);
        }

    }
}
