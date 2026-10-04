using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Presentation;
using Dovus.Core.Tuning;
using Dovus.Game.Data;
using Dovus.Game.Actors;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Boss;
using Dovus.Game.Config;
using Dovus.Game.Skills.Targeting;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Effects
{
    public interface ILivingEffectSpawnerHost
    {
        Transform Player { get; }
        Transform DirectorTransform { get; }
        SentenceEngine Engine { get; }
        CombatTuning Combat { get; }
        PrototypeTuning Colors { get; }
        ActorPose Pose { get; }
        ActorVisual Visual { get; }
        BossReactor Boss { get; }
        BossVitals BossVitals { get; }
        GroundScarField Scars { get; }
        SkillMotor Skills { get; }

        LivingEffectView BuildingView { get; set; }
        SkillAim Aim { get; }

        SkillResolution ResolveSkillWords(IReadOnlyList<SentenceWord> words);
        void EnsurePresentationCatalog();
        PresentationCatalog PresentationCatalog { get; }
        void SyncVisualDelivery();
        void StopBasicCannonAtFirstBody(LivingEffect logic, Vector3 origin, Vector3 facing);
        void RefreshBuildingMobility(IReadOnlyList<SentenceWord> words);

        void DestroyUnityObject(Object obj);
        void ClearClosingStamp(LivingEffect logic);
    }
}
