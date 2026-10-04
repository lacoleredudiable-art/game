using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Mechanic;
using Dovus.Game.Boss;
using Dovus.Game.Skills.Mechanics;
using System;
using UnityEngine;
using Dovus.Core.Shared;

namespace Dovus.Game.Skills.Projectiles
{
    public sealed class ProjectileEraser
    {
        readonly IProjectileEraserHost _host;
        readonly int[] _eraseIds = new int[HostileProjectiles.DefaultMaxAlive];

        public ProjectileEraser(IProjectileEraserHost host) => _host = host;

        public EraseSpec ProjectileEraseSpec(MechanicPlan plan) =>
            ProjectileEraseRules.Erases(plan) ? ProjectileEraseRules.For(plan, _host.JsonRules) : default;

        public void BeginProjectileErase(MechanicPlan plan, Vector3 aimDir, Vector3 center)
        {
            if (_host.Projectiles == null || plan == null)
                return;
            EraseSpec spec = ProjectileEraseSpec(plan);
            if (spec.Shape == EraseShape.Line)
                EraseLine(plan, spec, aimDir, center);
            else if (spec.Shape == EraseShape.Disk && spec.Mode != EraseMode.Targeted && plan.Body.BornAt != "sende")
                EraseBodyPath(plan, spec, center);
        }

        public void ProjectileEraseOnHit(MechanicPlan plan, Vector3 center)
        {
            if (_host.Projectiles == null || plan == null || _host.Player == null)
                return;
            EraseSpec spec = ProjectileEraseSpec(plan);
            if (spec.Shape != EraseShape.Line)
                return;
            Vector3 aim = center - _host.Player.position;
            EraseLine(plan, spec, aim, center);
        }

        public void TickProjectileErase(double worldMs)
        {
            if (_host.Projectiles == null || _host.Projectiles.Sim.AliveCount == 0 && !AnyFollowErase())
                return;
            foreach (MechanicVolume v in _host.MechanicWorld.Volumes)
            {
                if (v.Erase.IsEmpty || worldMs >= v.UntilMs)
                    continue;
                TickEraseVolume(v, worldMs);
            }
            foreach (MechanicLink link in _host.MechanicWorld.Links)
            {
                if (worldMs >= link.UntilMs || link.Target == null || _host.Player == null)
                    continue;
                EraseSpec spec = ProjectileEraseSpec(link.Plan);
                if (spec.Shape == EraseShape.Segment)
                    EraseAlongLink(link, spec);
            }
        }

        void EraseBodyPath(MechanicPlan plan, in EraseSpec spec, Vector3 center)
        {
            if (_host.Player == null)
                return;
            Vector3 from = _host.Player.position;
            if (_host.FlatDistance(from, center) < ProjectileEraserDefaults.EraseCenterEpsilonM)
                return;
            float halfWidth = Mathf.Max(ProjectileEraserDefaults.MinRadiusM, (float)plan.Body.SizeM * 0.5f);
            int n = _host.Projectiles.Sim.QuerySegment(from.x, from.z, center.x, center.z, halfWidth, _eraseIds, team: 1);
            if (n == 0)
                return;
            int done = 0;
            float healed = 0f;
            for (int i = 0; i < n; i++)
                if (ApplyEraseMode(_eraseIds[i], spec, ref healed))
                    done++;
            if (healed > 0f && _host.PlayerStatus != null)
                _host.PlayerStatus.ApplyHeal(healed);
            if (done > 0)
                JsonEffectRuntime.JsonLog($"mermi gövde yolu {spec.Mode} {plan.SkillId}: {done} ({spec})" + (healed > 0f ? $" +{healed:0.#} can" : ""));
        }

        bool ApplyEraseMode(int pid, in EraseSpec spec, ref float healed)
        {
            switch (spec.Mode)
            {
                case EraseMode.Absorb:
                    if (_host.Projectiles.Sim.TryGet(pid, out Projectile pa) && _host.Projectiles.Sim.Delete(pid, ProjectileEventKind.Absorbed))
                    {
                        healed += pa.Damage * spec.Lifesteal;
                        return true;
                    }
                    return false;
                case EraseMode.Reflect:
                    return ReflectProjectile(pid);
                case EraseMode.Shroud:
                    return _host.Projectiles.Sim.DropTarget(pid);
                default:
                    return _host.Projectiles.Sim.Delete(pid, ProjectileEventKind.Erased);
            }
        }

        void EraseLine(MechanicPlan plan, in EraseSpec spec, Vector3 aimDir, Vector3 center)
        {
            if (_host.Player == null)
                return;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 0.0001f)
                aimDir = _host.Player.forward;
            Vector3 from = _host.Player.position;
            float length = Mathf.Max(spec.LengthM, _host.FlatDistance(from, center));
            Vector3 to = from + aimDir.normalized * length;
            int n = _host.Projectiles.Sim.QuerySegment(from.x, from.z, to.x, to.z, spec.WidthM * 0.5f, _eraseIds, team: 1);
            for (int i = 0; i < n; i++)
                _host.Projectiles.Sim.Delete(_eraseIds[i], ProjectileEventKind.Erased);
            if (n > 0)
                JsonEffectRuntime.JsonLog($"mermi delici hat {plan.SkillId}: {n} silindi ({length:0.#} m)");
        }

        bool AnyFollowErase()
        {
            foreach (MechanicVolume v in _host.MechanicWorld.Volumes)
                if (v.Erase.Shape == EraseShape.Follow)
                    return true;
            return false;
        }

        void TickEraseVolume(MechanicVolume v, double worldMs)
        {
            EraseSpec spec = v.Erase;
            string id = v.Plan != null ? v.Plan.SkillId : "hacim";
            if (spec.Shape == EraseShape.Follow && _host.Player != null)
            {
                v.Center = new Vector3(_host.Player.position.x, v.Center.y, _host.Player.position.z);
                if (v.View != null)
                    v.View.transform.position = new Vector3(v.Center.x, v.View.transform.position.y, v.Center.z);
            }
            if (spec.Shape == EraseShape.Segment)
                return;

            float radius = spec.GrowTo > 1f
                ? JsonEffectRules.RampedRatio(v.RadiusM, v.StartMs, v.UntilMs, worldMs, spec.GrowTo)
                : v.RadiusM;
            if (spec.GrowTo > 1f && v.View != null)
                v.View.transform.localScale = new Vector3(radius * 2f, v.View.transform.localScale.y, radius * 2f);

            if (spec.Mode == EraseMode.Targeted)
            {
                if (worldMs < v.NextEraseMs)
                    return;
                v.NextEraseMs = worldMs + Units.SecToMs / Math.Max(ProjectileEraserDefaults.EraseRateMinHz, spec.RatePerSec);
                EraseMostUrgent(id);
                return;
            }

            int n = _host.Projectiles.Sim.QueryCircle(v.Center.x, v.Center.z, radius, _eraseIds, team: 1);
            if (n == 0)
                return;
            int done = 0;
            float healed = 0f;
            for (int i = 0; i < n; i++)
                if (ApplyEraseMode(_eraseIds[i], spec, ref healed))
                    done++;
            if (healed > 0f && _host.PlayerStatus != null)
                _host.PlayerStatus.ApplyHeal(healed);
            if (done > 0)
            {
                JsonEffectRuntime.JsonLog($"mermi {spec.Mode} {id}: {done} ({spec})" + (healed > 0f ? $" +{healed:0.#} can" : ""));
                if (spec.Twice && !v.TwiceDone && _host.Clock != null)
                {
                    v.TwiceDone = true;
                    Vector3 c = v.Center;
                    float r = radius;
                    _host.ScheduleAfter(_host.Clock.Director.WorldTimeMs, (float)spec.TwiceDelaySec, () => EraseDiskOnce(c, r, id));
                }
            }
        }

        void EraseDiskOnce(Vector3 center, float radius, string id)
        {
            if (_host.Projectiles == null)
                return;
            int n = _host.Projectiles.Sim.QueryCircle(center.x, center.z, radius, _eraseIds, team: 1);
            for (int i = 0; i < n; i++)
                _host.Projectiles.Sim.Delete(_eraseIds[i], ProjectileEventKind.Erased);
            JsonEffectRuntime.JsonLog($"mermi iki_kez {id}: kopya vuruşu {n} silindi");
        }

        bool ReflectProjectile(int id)
        {
            if (!_host.Projectiles.Sim.TryGet(id, out Projectile p))
                return false;
            Vector3 dir = _host.Boss != null
                ? _host.Boss.transform.position - new Vector3(p.X, 0f, p.Z)
                : new Vector3(-p.VX, 0f, -p.VZ);
            float speed = Mathf.Sqrt(p.VX * p.VX + p.VZ * p.VZ);
            float minSpeed = _host.Combat != null ? _host.Combat.Boss.VolleySpeedMps : ProjectileEraserDefaults.VolleySpeedFallbackMps;
            float mult = speed > 0.0001f ? Mathf.Max(1f, minSpeed / speed) : 1f;
            if (speed <= 0.0001f)
                _host.Projectiles.Sim.SetSpeedMult(id, 0f);
            return _host.Projectiles.Sim.Reflect(id, 0, mult, dir.x, dir.z);
        }

        void EraseMostUrgent(string id)
        {
            Transform guard = _host.Ally != null ? _host.Ally.transform : _host.Player;
            if (guard == null)
                return;
            int best = -1;
            double bestMs = double.PositiveInfinity;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _host.Projectiles.Sim.MaxAlive; i++)
            {
                Projectile p = _host.Projectiles.Sim.Slot(i);
                if (!p.Alive || p.Team != 1)
                    continue;
                double tti = HostileProjectiles.TimeToImpactMs(p, guard.position.x, guard.position.z, 0.5f);
                float dist = _host.FlatDistance(new Vector3(p.X, 0f, p.Z), guard.position);
                if (tti < bestMs || (double.IsPositiveInfinity(bestMs) && double.IsPositiveInfinity(tti) && dist < bestDist))
                {
                    best = p.Id;
                    bestMs = tti;
                    bestDist = dist;
                }
            }
            if (best > 0 && _host.Projectiles.Sim.Delete(best, ProjectileEventKind.Erased))
                JsonEffectRuntime.JsonLog($"mermi hedefli {id}: #{best} silindi (çarpmaya {(double.IsPositiveInfinity(bestMs) ? "∞" : (bestMs / 1000.0).ToString("0.##"))} sn)");
        }

        void EraseAlongLink(MechanicLink link, in EraseSpec spec)
        {
            Vector3 a = _host.Player.position;
            Vector3 b = link.Target.position;
            int n = _host.Projectiles.Sim.QuerySegment(a.x, a.z, b.x, b.z, spec.WidthM * 0.5f, _eraseIds, team: 1);
            for (int i = 0; i < n; i++)
                _host.Projectiles.Sim.Delete(_eraseIds[i], ProjectileEventKind.LinkErased);
            if (n > 0)
                JsonEffectRuntime.JsonLog($"mermi bag_hatti {link.Plan?.SkillId}: {n} silindi");
        }

        public bool InProjectileShroud(Vector3 at)
        {
            if (_host.Clock == null)
                return false;
            double now = _host.Clock.Director.WorldTimeMs;
            foreach (MechanicVolume v in _host.MechanicWorld.Volumes)
            {
                if (v.Erase.Mode != EraseMode.Shroud || now >= v.UntilMs)
                    continue;
                if (_host.FlatDistance(at, v.Center) <= v.RadiusM)
                    return true;
            }
            return false;
        }

        public void ApplyProjectileReflect(float projectileDamage)
        {
            double raw = _host.JsonRules != null ? _host.JsonRules.Param("projectile_reflect_mult") : 0;
            float mult = raw > 0 ? (float)raw : 1f;
            float amount = projectileDamage * mult;
            JsonEffectRuntime.JsonLog($"mermi geri döndü → boss {amount:0.#}");
            _host.ApplyReflectedDamage(amount);
        }
    }
}
