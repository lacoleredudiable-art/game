using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Tuning;
using Dovus.Core.Mechanic;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Composition;
using Dovus.Game.Data;
using Dovus.Game.Skills.Mechanics;
using Dovus.Game.Skills.Projectiles;
using System;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        sealed class ProjectileEraserHost : IProjectileEraserHost
        {
            readonly ManifestationDirector _md;

            internal ProjectileEraserHost(ManifestationDirector md) => _md = md;

            public Transform Player => _md._player;
            public AllyDummyController Ally => _md._ally;
            public BossReactorController Boss => _md._boss;
            public ActorStatusHost PlayerStatus => _md._playerStatus;
            public GameClockHost Clock => _md._clock;
            public CombatTuning Combat => _md._combat;
            public HostileProjectileHost Projectiles => _md._projectiles;
            public MechanicWorldRuntime MechanicWorld
            {
                get
                {
                    _md.EnsureMechanicsServices();
                    return _md._mechanicWorld;
                }
            }
            public MechanicRules JsonRules
            {
                get
                {
                    _md.EnsureMechanicsServices();
                    return _md._mechanicsHost.JsonRules;
                }
            }
            public void ApplyReflectedDamage(float amount) => _md.ApplyReflectedDamage(amount);
            public float FlatDistance(Vector3 a, Vector3 b) => ManifestationDirector.FlatDistance(a, b);
            public void ScheduleAfter(double now, float delaySec, Action run) => _md.After(now, delaySec, run);
        }
    }
}
