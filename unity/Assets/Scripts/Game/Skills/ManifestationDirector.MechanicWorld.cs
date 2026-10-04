using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        public void BindHostileTargets(HostileTargetsHost targets)
        {
            EnsureMechanicsServices();
            _mechanicWorld.BindHostileTargets(targets);
        }

        void BeginMechanicWorld(MechanicPlan plan, Vector3 aimDir, Vector3 landedAt, double worldMs)
        {
            EnsureMechanicsServices();
            _mechanicWorld.BeginMechanicWorld(plan, aimDir, landedAt, worldMs);
        }

        float RedirectMechanicDamage(float incoming)
        {
            EnsureMechanicsServices();
            return _mechanicWorld.RedirectMechanicDamage(incoming);
        }

        void ReflectFromWorldVolumes(float incoming)
        {
            EnsureMechanicsServices();
            _mechanicWorld.ReflectFromWorldVolumes(incoming);
        }

        SkillExecutorRoute ApplyMechanicWorldRoute(MechanicPlan plan, SkillExecutorRoute route)
        {
            EnsureMechanicsServices();
            return _mechanicWorld.ApplyMechanicWorldRoute(plan, route);
        }

        void PullBossToPlayerContact()
        {
            EnsureMechanicsServices();
            _mechanicWorld.PullBossToPlayerContact();
        }
    }
}
