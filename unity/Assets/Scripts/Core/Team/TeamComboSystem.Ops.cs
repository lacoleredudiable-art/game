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
    public sealed partial class TeamComboSystem
    {
        TeamPulse PlantMine(IAllyPlayer caster, in Disc boss)
        {
            float x = caster.X;
            float z = caster.Z;
            if (boss.Present)
            {
                float dx = boss.X - caster.X;
                float dz = boss.Z - caster.Z;
                float len = MathF.Sqrt(dx * dx + dz * dz);
                if (len > TeamComboSystemDefaults.MinAimDirLen)
                {
                    float u = MathF.Min(TeamComboSystemDefaults.BossOffsetAlongLenRatio, (len - boss.Radius - TriggerRadiusM) / len);
                    if (u < TeamComboSystemDefaults.MinAimDirLen)
                        u = TeamComboSystemDefaults.MinAimDirLen;
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
                BossRadius = boss.Present ? boss.Radius : TeamComboSystemDefaults.FallbackBossRadiusM
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
            float bz = boss.Present ? boss.Z + TeamComboSystemDefaults.BossOffsetForwardM : caster.Z + TeamComboSystemDefaults.BossOffsetForwardM;
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

        TeamPulse LinkPair(SkillId skillId, IAllyPlayer caster, IAllyPlayer target, IReadOnlyList<IAllyPlayer> allies, in Disc boss)
        {
            IAllyPlayer a = target != null && target.Id != caster.Id ? target : FirstOther(caster, allies);
            IAllyPlayer b = SecondOther(caster, a, allies);
            if (a == null || b == null)
                return TeamPulse.None;
            _linkBurnSource = skillId.Value;
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
    }
}
