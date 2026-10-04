using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
using Dovus.Game.Data;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Weapons;
using UnityEngine;
namespace Dovus.Game.Skills.Weapons
{
public interface ICannonBlastHost
    {
        Transform Player { get; }
        BossReactorController Boss { get; }
        AllyDummyController Ally { get; }
        KinematicMotorController Motor { get; }
        MotionTemplateBodyHost MotionBody { get; }
        bool RecoilInTemplate { get; }
        void SetRecoilInTemplate(bool value);
        bool CasterRecoilSuppressed { get; set; }
        double WorldTimeMs { get; }
        WeaponCombatProfile EquippedProfile { get; }
        float PlayerBodyRadiusM();
        void StopMotionBody();
        void EndMotionAnim();
        void AbortRecoveringSentence();
        void SetSwapInstantDrawUntil(double worldMs);
        SentenceEngine Engine { get; }
        ActorView Visual { get; }
        WeaponSwapState WeaponSwap { get; }
        GameClockHost Clock { get; }
    }
}
