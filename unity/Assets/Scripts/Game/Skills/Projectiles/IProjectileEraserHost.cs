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
using Dovus.Game.Skills.Mechanics;
using System;
using UnityEngine;

namespace Dovus.Game.Skills.Projectiles
{
    public interface IProjectileEraserHost
    {
        Transform Player { get; }
        AllyDummy Ally { get; }
        BossReactor Boss { get; }
        ActorStatus PlayerStatus { get; }
        GameClock Clock { get; }
        CombatTuning Combat { get; }
        HostileProjectileHost Projectiles { get; }
        MechanicWorldRuntime MechanicWorld { get; }
        MechanicRules JsonRules { get; }
        void ApplyReflectedDamage(float amount);
        float FlatDistance(Vector3 a, Vector3 b);
        void ScheduleAfter(double now, float delaySec, Action run);
    }
}
