using Dovus.Core.Equipment;
using Dovus.Core.Tuning;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Presentation;
using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Platform;
using Dovus.Game.Data;
using Dovus.Game.Diagnostics;
using Dovus.Game.Audio;
using Dovus.Game.Feel;
using Dovus.Game.Hud;
using Dovus.Game.Vfx;
using System.Collections.Generic;

namespace Dovus.Game.Skills.Presentation
{
    public interface ISkillPresentationHost
    {
        ActorView Visual { get; }
        AnimationBridge AnimationBridge { get; }
        SkillMotor Skills { get; }
        EquipmentItem EquippedWeapon { get; }
        AnimationDatabase AnimationDatabase { get; }
        GameClockHost Clock { get; }
        CombatTuning Combat { get; }
        FollowCameraController Camera { get; }
        SfxDirector Sfx { get; }
        ISentenceDebugSink DebugHud { get; }
        ElementPaintNode? SelectedElementPaint { get; }
        Skill LastFactorySkill { get; }
        MechanicPlan LastMechanicPlan { get; }
        HashSet<string> MissingAnimationBindings { get; }

        string LastAnimationTypeId { set; }
        string LastAnimationState { set; }
        bool LastAnimationPlayApplied { set; }
        string LastAnimationClip { set; }
        bool LastAnimationUsedFallback { set; }

        void SyncVisualDelivery();
        void StartCastVfxTimer(SkillResolution skill, IReadOnlyList<SentenceWord> words);
    }
}
