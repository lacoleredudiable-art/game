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
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Presentation;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Hud;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Targeting
{
    public interface ISkillAimHost
    {
        Transform Player { get; }
        PlayerTargetingController Targeting { get; }
        BossReactorController Boss { get; }
        KinematicMotorController Motor { get; }
        CombatTuning Combat { get; }
        GameTuning Colors { get; }
        ReactionReadoutHud Readout { get; }
        SkillMotor Skills { get; }
        SkillNumberCatalog SkillNumbers { get; }
        EquipmentItem EquippedWeapon { get; }
        SkillExecutorRouter SkillExecutorRouter { get; }

        bool PerformingAttack { get; }
        bool IsEnemyBody(Transform body);
        float PlayerBodyRadiusM();
        SkillResolution ResolveSkillWords(IReadOnlyList<SentenceWord> words);

        void EnsurePresentationCatalog();
        PresentationCatalog PresentationCatalog { get; }
        MechanicPlan MechanicPlanFor(SkillResolution skill);
        SkillExecutorRoute ApplyMechanicWorldRoute(MechanicPlan plan, SkillExecutorRoute route);

        void ApplyVerbHitboxSizing(
            SkillExecutorKind kind,
            in SkillResolution skill,
            ManifestationTuning tuning,
            float rangeMult,
            bool burst,
            ref float radius,
            ref float range,
            ref float durationSec,
            ref int spawnCount);

        bool TryGetMotionBinding(string skillId, out MotionBinding binding);
    }
}
