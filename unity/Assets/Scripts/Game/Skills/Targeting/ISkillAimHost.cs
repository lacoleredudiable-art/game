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
using Dovus.Core.Motion;
using Dovus.Core.Shared;
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
        ISkillRepository SkillNumbers { get; }
        EquipmentItem EquippedWeapon { get; }

        bool PerformingAttack { get; }
        bool IsEnemyBody(Transform body);
        float PlayerBodyRadiusM();
        SkillResolution ResolveSkillWords(IReadOnlyList<SentenceWord> words);

        void EnsurePresentationCatalog();
        PresentationCatalog PresentationCatalog { get; }

        bool TryGetMotionBinding(SkillId skillId, out MotionBinding binding);
    }
}
