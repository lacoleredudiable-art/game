using Dovus.Core.Combat;
using Dovus.Game.Boss;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.Hud;
using UnityEngine;

namespace Dovus.Game.Skills.Boss
{
    public interface IBossDeathSequenceHost
    {
        GameClock Clock { get; }
        PrototypeTuning Colors { get; }
        BossDirector BossDirector { get; }
        BossReactor Boss { get; }
        BossVitals BossVitals { get; }
        DamageNumberHud DamageHud { get; }

        void NoteShieldBlockIfGuarding();
        void OnJsonShieldBlocked();

        Vector3? BossHitPoint();
        Color? DamageTint();
    }
}
