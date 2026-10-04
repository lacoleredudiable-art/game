using Dovus.Core.Mechanic;
using Dovus.Game.Boss;
using Dovus.Game.Skills.Hosts;
using Dovus.Game.Skills.Projectiles;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        internal HostileProjectileHost _projectiles;
        MdProjectileEraserHost _projectileHost;
        ProjectileEraser _projectileEraser;

        public void BindProjectiles(HostileProjectileHost host)
        {
            EnsureProjectileServices();
            _projectiles = host;
            if (host == null)
                return;
            host.ReflectSink = _projectileEraser.ApplyProjectileReflect;
            host.InShroud = _projectileEraser.InProjectileShroud;
        }

        public HostileProjectileHost Projectiles => _projectiles;

        void EnsureProjectileServices()
        {
            if (_projectileEraser != null)
                return;
            _projectileHost = new MdProjectileEraserHost(this);
            _projectileEraser = new ProjectileEraser(_projectileHost);
        }

        internal EraseSpec ProjectileEraseSpec(MechanicPlan plan)
        {
            EnsureProjectileServices();
            return _projectileEraser.ProjectileEraseSpec(plan);
        }

        internal void BeginProjectileErase(MechanicPlan plan, Vector3 aimDir, Vector3 center)
        {
            EnsureProjectileServices();
            _projectileEraser.BeginProjectileErase(plan, aimDir, center);
        }

        internal void ProjectileEraseOnHit(MechanicPlan plan, Vector3 center)
        {
            EnsureProjectileServices();
            _projectileEraser.ProjectileEraseOnHit(plan, center);
        }

        void TickProjectileErase(double worldMs)
        {
            EnsureProjectileServices();
            _projectileEraser.TickProjectileErase(worldMs);
        }
    }
}
