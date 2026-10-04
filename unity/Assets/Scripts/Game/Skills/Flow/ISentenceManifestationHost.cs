using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.Skills.Effects;
using Dovus.Game.Skills.Targeting;
using Dovus.Game.Team;
using System.Collections.Generic;
using UnityEngine;

using Dovus.Game.Skills;

namespace Dovus.Game.Skills.Flow
{
    public interface ISentenceManifestationHost
    {
        SentenceEngine Engine { get; }
        GameClock Clock { get; }
        TeamComboAccess TeamAccess { get; }
        CombatTuning Combat { get; }
        GameTuning Colors { get; }
        SkillMotor Skills { get; }
        ActorPose Pose { get; }
        ActorVisual Visual { get; }
        LivingEffectSpawner EffectSpawner { get; }
        SkillAim Aim { get; }

        LivingEffectView BuildingView { get; set; }
        int LastWordCount { get; set; }
        bool PosedForRecovery { get; set; }
        List<PendingClosing> Pending { get; }

        void RefreshBuildingMobility(IReadOnlyList<SentenceWord> words);
        void SyncVisualDelivery();
        void TryBeginBasicStrikeStep();
        SkillResolution ResolveSkillWords(IReadOnlyList<SentenceWord> words);
        WeaponSkillCompatibility WeaponCompatibilityFor(SkillResolution skill);
        bool TryArmSkillTarget(SkillResolution skill);
        void FaceTarget(Transform target);
        void ApplyCastMobility(SkillResolution skill, float durationSec);
    }
}
