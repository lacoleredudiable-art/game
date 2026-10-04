using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Boss;
using Dovus.Game.Platform;
using Dovus.Game.Config;
using Dovus.Game.Hud;
using UnityEngine;

namespace Dovus.Game.Skills.Boss
{
    public interface IBossDeathSequenceHost
    {
        GameClockHost Clock { get; }
        GameTuning Colors { get; }
        BossDirector BossDirector { get; }
        BossReactorController Boss { get; }
        BossVitals BossVitals { get; }
        DamageNumberHud DamageHud { get; }

        void NoteShieldBlockIfGuarding();
        void OnJsonShieldBlocked();

        Vector3? BossHitPoint();
        Color? DamageTint();
    }
}
