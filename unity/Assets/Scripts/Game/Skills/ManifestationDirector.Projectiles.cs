using Dovus.Core.Combat;
using Dovus.Core.Mechanic;
using Dovus.Game.Boss;
using Dovus.Game.Skills.Mechanics;
using System;
using UnityEngine;

namespace Dovus.Game.Skills
{
    /// <summary>
    /// mermi_sil ailesi (boss PR 2): mermi silme hacimleri, bağ şeridi, delici hat. Spec saf
    /// <see cref="ProjectileEraseRules"/>'tan; burası yalnız dünyaya uygular ve "[Mechanic] json mermi" loglar.
    /// Hacim = mevcut SpawnMechanicVolume diski (yarıçap Body.SizeM, ömür hacmin ömrü).
    /// </summary>
    public sealed partial class ManifestationDirector
    {
        HostileProjectileHost _projectiles;
        readonly int[] _eraseIds = new int[HostileProjectiles.DefaultMaxAlive];

        /// <summary>Mermi sahibi (boss PR 2). Silme/yutma/geri gönderme buradan; yansıma hasarı ApplyReflectedDamage.</summary>
        public void BindProjectiles(HostileProjectileHost host)
        {
            _projectiles = host;
            if (host == null)
                return;
            host.ReflectSink = ApplyProjectileReflect;
            host.InShroud = InProjectileShroud;
        }

        public HostileProjectileHost Projectiles => _projectiles;

        void ApplyProjectileReflect(float projectileDamage)
        {
            float mult = (float)_mechanicsHost.JsonParam("projectile_reflect_mult", 1.0);
            float amount = projectileDamage * mult;
            JsonEffectRuntime.JsonLog($"mermi geri döndü → boss {amount:0.#}");
            ApplyReflectedDamage(amount);
        }

        /// <summary>sis_perdesi: perde hacmindeki dost mermiyle vurulamaz.</summary>
        bool InProjectileShroud(Vector3 at)
        {
            if (_clock == null)
                return false;
            double now = _clock.Director.WorldTimeMs;
            foreach (MechanicVolume v in _mechanicWorld.Volumes)
            {
                if (v.Erase.Mode != EraseMode.Shroud || now >= v.UntilMs)
                    continue;
                if (FlatDistance(at, v.Center) <= v.RadiusM)
                    return true;
            }
            return false;
        }

        EraseSpec ProjectileEraseSpec(MechanicPlan plan) =>
            ProjectileEraseRules.Erases(plan) ? ProjectileEraseRules.For(plan, _mechanicsHost.JsonRules) : default;

        /// <summary>Cast anında: delici hat (9-1) oyuncudan nişan boyunca her mermiyi siler.</summary>
        void BeginProjectileErase(MechanicPlan plan, Vector3 aimDir, Vector3 center)
        {
            if (_projectiles == null || plan == null)
                return;
            EraseSpec spec = ProjectileEraseSpec(plan);
            if (spec.Shape == EraseShape.Line)
                EraseLine(plan, spec, aimDir, center);
            else if (spec.Shape == EraseShape.Disk && spec.Mode != EraseMode.Targeted && plan.Body.BornAt != "sende")
                EraseBodyPath(plan, spec, center);
        }

        /// <summary>
        /// Gövde oyuncudan iniş noktasına giderken değdiği mermilere hacmin modunu uygular (yut/geri gönder/
        /// perde/sil). İniş noktası menzil sonunda olabildiği için kısa ömürlü vuruş hacimleri tek başına
        /// hedefin önündeki mermiye değmez; yol şeridi genişliği gövde boyutu (Body.SizeM).
        /// </summary>
        void EraseBodyPath(MechanicPlan plan, in EraseSpec spec, Vector3 center)
        {
            if (_player == null)
                return;
            Vector3 from = _player.position;
            if (FlatDistance(from, center) < 0.05f)
                return;
            float halfWidth = Mathf.Max(0.05f, (float)plan.Body.SizeM * 0.5f);
            int n = _projectiles.Sim.QuerySegment(from.x, from.z, center.x, center.z, halfWidth, _eraseIds, team: 1);
            if (n == 0)
                return;
            int done = 0;
            float healed = 0f;
            for (int i = 0; i < n; i++)
                if (ApplyEraseMode(_eraseIds[i], spec, ref healed))
                    done++;
            if (healed > 0f && _playerStatus != null)
                _playerStatus.ApplyHeal(healed);
            if (done > 0)
                JsonEffectRuntime.JsonLog($"mermi gövde yolu {spec.Mode} {plan.SkillId}: {done} ({spec})" + (healed > 0f ? $" +{healed:0.#} can" : ""));
        }

        bool ApplyEraseMode(int pid, in EraseSpec spec, ref float healed)
        {
            switch (spec.Mode)
            {
                case EraseMode.Absorb:
                    if (_projectiles.Sim.TryGet(pid, out Projectile pa) && _projectiles.Sim.Delete(pid, ProjectileEventKind.Absorbed))
                    {
                        healed += pa.Damage * spec.Lifesteal;
                        return true;
                    }
                    return false;
                case EraseMode.Reflect:
                    return ReflectProjectile(pid);
                case EraseMode.Shroud:
                    return _projectiles.Sim.DropTarget(pid);
                default:
                    return _projectiles.Sim.Delete(pid, ProjectileEventKind.Erased);
            }
        }

        /// <summary>Gövde düşmana değince delici hat bir kez daha (vuruş noktasına kadar).</summary>
        void ProjectileEraseOnHit(MechanicPlan plan, Vector3 center)
        {
            if (_projectiles == null || plan == null || _player == null)
                return;
            EraseSpec spec = ProjectileEraseSpec(plan);
            if (spec.Shape != EraseShape.Line)
                return;
            Vector3 aim = center - _player.position;
            EraseLine(plan, spec, aim, center);
        }

        void EraseLine(MechanicPlan plan, in EraseSpec spec, Vector3 aimDir, Vector3 center)
        {
            if (_player == null)
                return;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 0.0001f)
                aimDir = _player.forward;
            Vector3 from = _player.position;
            float length = Mathf.Max(spec.LengthM, FlatDistance(from, center));
            Vector3 to = from + aimDir.normalized * length;
            int n = _projectiles.Sim.QuerySegment(from.x, from.z, to.x, to.z, spec.WidthM * 0.5f, _eraseIds, team: 1);
            for (int i = 0; i < n; i++)
                _projectiles.Sim.Delete(_eraseIds[i], ProjectileEventKind.Erased);
            if (n > 0)
                JsonEffectRuntime.JsonLog($"mermi delici hat {plan.SkillId}: {n} silindi ({length:0.#} m)");
        }

        /// <summary>TickMechanics'ten, TickMechanicWorld'den sonra: hacimler ve bağlar mermiye uygulanır.</summary>
        void TickProjectileErase(double worldMs)
        {
            if (_projectiles == null || _projectiles.Sim.AliveCount == 0 && !AnyFollowErase())
                return;
            foreach (MechanicVolume v in _mechanicWorld.Volumes)
            {
                if (v.Erase.IsEmpty || worldMs >= v.UntilMs)
                    continue;
                TickEraseVolume(v, worldMs);
            }
            foreach (MechanicLink link in _mechanicWorld.Links)
            {
                if (worldMs >= link.UntilMs || link.Target == null || _player == null)
                    continue;
                EraseSpec spec = ProjectileEraseSpec(link.Plan);
                if (spec.Shape == EraseShape.Segment)
                    EraseAlongLink(link, spec);
            }
        }

        bool AnyFollowErase()
        {
            foreach (MechanicVolume v in _mechanicWorld.Volumes)
                if (v.Erase.Shape == EraseShape.Follow)
                    return true;
            return false;
        }

        void TickEraseVolume(MechanicVolume v, double worldMs)
        {
            EraseSpec spec = v.Erase;
            string id = v.Plan != null ? v.Plan.SkillId : "hacim";
            if (spec.Shape == EraseShape.Follow && _player != null)
            {
                // surekli_perde: perde oyuncuyla yürür (channel_sec boyunca).
                v.Center = new Vector3(_player.position.x, v.Center.y, _player.position.z);
                if (v.View != null)
                    v.View.transform.position = new Vector3(v.Center.x, v.View.transform.position.y, v.Center.z);
            }
            if (spec.Shape == EraseShape.Segment)
                return; // bag_hatti: bağ şeridi siler, disk değil.

            float radius = spec.GrowTo > 1f
                ? JsonEffectRules.RampedRatio(v.RadiusM, v.StartMs, v.UntilMs, worldMs, spec.GrowTo)
                : v.RadiusM;
            if (spec.GrowTo > 1f && v.View != null)
                v.View.transform.localScale = new Vector3(radius * 2f, v.View.transform.localScale.y, radius * 2f);

            if (spec.Mode == EraseMode.Targeted)
            {
                if (worldMs < v.NextEraseMs)
                    return;
                v.NextEraseMs = worldMs + 1000.0 / Math.Max(0.01, spec.RatePerSec);
                EraseMostUrgent(id);
                return;
            }

            int n = _projectiles.Sim.QueryCircle(v.Center.x, v.Center.z, radius, _eraseIds, team: 1);
            if (n == 0)
                return;
            int done = 0;
            float healed = 0f;
            for (int i = 0; i < n; i++)
                if (ApplyEraseMode(_eraseIds[i], spec, ref healed))
                    done++;
            if (healed > 0f && _playerStatus != null)
                _playerStatus.ApplyHeal(healed);
            if (done > 0)
            {
                JsonEffectRuntime.JsonLog($"mermi {spec.Mode} {id}: {done} ({spec})" + (healed > 0f ? $" +{healed:0.#} can" : ""));
                if (spec.Twice && !v.TwiceDone && _clock != null)
                {
                    // iki_kez: kopya vuruşunda aynı diskte bir kez daha siler.
                    v.TwiceDone = true;
                    Vector3 c = v.Center;
                    float r = radius;
                    After(_clock.Director.WorldTimeMs, (float)spec.TwiceDelaySec, () => EraseDiskOnce(c, r, id));
                }
            }
        }

        void EraseDiskOnce(Vector3 center, float radius, string id)
        {
            if (_projectiles == null)
                return;
            int n = _projectiles.Sim.QueryCircle(center.x, center.z, radius, _eraseIds, team: 1);
            for (int i = 0; i < n; i++)
                _projectiles.Sim.Delete(_eraseIds[i], ProjectileEventKind.Erased);
            JsonEffectRuntime.JsonLog($"mermi iki_kez {id}: kopya vuruşu {n} silindi");
        }

        /// <summary>geri_gonder: mermi boss'a döner (takım 0), en az salvo hızında.</summary>
        bool ReflectProjectile(int id)
        {
            if (!_projectiles.Sim.TryGet(id, out Projectile p))
                return false;
            Vector3 dir = _boss != null
                ? _boss.transform.position - new Vector3(p.X, 0f, p.Z)
                : new Vector3(-p.VX, 0f, -p.VZ);
            float speed = Mathf.Sqrt(p.VX * p.VX + p.VZ * p.VZ);
            float minSpeed = _combat != null ? _combat.Boss.VolleySpeedMps : 7f;
            float mult = speed > 0.0001f ? Mathf.Max(1f, minSpeed / speed) : 1f;
            if (speed <= 0.0001f)
                _projectiles.Sim.SetSpeedMult(id, 0f);
            return _projectiles.Sim.Reflect(id, 0, mult, dir.x, dir.z);
        }

        /// <summary>hedefli: korunan dosta en kısa sürede çarpacak tek mermi (arenanın her yerinde).</summary>
        void EraseMostUrgent(string id)
        {
            Transform guard = _ally != null ? _ally.transform : _player;
            if (guard == null)
                return;
            int best = -1;
            double bestMs = double.PositiveInfinity;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _projectiles.Sim.MaxAlive; i++)
            {
                Projectile p = _projectiles.Sim.Slot(i);
                if (!p.Alive || p.Team != 1)
                    continue;
                double tti = HostileProjectiles.TimeToImpactMs(p, guard.position.x, guard.position.z, 0.5f);
                float dist = FlatDistance(new Vector3(p.X, 0f, p.Z), guard.position);
                if (tti < bestMs || (double.IsPositiveInfinity(bestMs) && double.IsPositiveInfinity(tti) && dist < bestDist))
                {
                    best = p.Id;
                    bestMs = tti;
                    bestDist = dist;
                }
            }
            if (best > 0 && _projectiles.Sim.Delete(best, ProjectileEventKind.Erased))
                JsonEffectRuntime.JsonLog($"mermi hedefli {id}: #{best} silindi (çarpmaya {(double.IsPositiveInfinity(bestMs) ? "∞" : (bestMs / 1000.0).ToString("0.##"))} sn)");
        }

        /// <summary>bag_hatti: oyuncu↔dost bağ şeridini kesen mermiler silinir.</summary>
        void EraseAlongLink(MechanicLink link, in EraseSpec spec)
        {
            Vector3 a = _player.position;
            Vector3 b = link.Target.position;
            int n = _projectiles.Sim.QuerySegment(a.x, a.z, b.x, b.z, spec.WidthM * 0.5f, _eraseIds, team: 1);
            for (int i = 0; i < n; i++)
                _projectiles.Sim.Delete(_eraseIds[i], ProjectileEventKind.LinkErased);
            if (n > 0)
                JsonEffectRuntime.JsonLog($"mermi bag_hatti {link.Plan?.SkillId}: {n} silindi");
        }
    }
}
