using Dovus.Core.Casting;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Presentation;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Data;
using Dovus.Game.Platform;
using Dovus.Game.Skills;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Skills.State;
using UnityEngine;

namespace Dovus.Game.Skills.Launch
{
    /// <summary>Executor fırlatma — CastSessionState + sahne; kapanış/launch davranışı LaunchServicesHost.</summary>
    public interface ISkillExecutorLaunchHost
    {
        CastSessionState Cast { get; }
        Transform Player { get; }
        Transform DirectorTransform { get; }
        GameClockHost Clock { get; }
        CombatTuning Combat { get; }
        PresentationCatalog PresentationCatalog { get; }
        EquipmentItem EquippedWeapon { get; }
        VerbExecutionData VerbData { get; }
        ElementPaintNode? SelectedElementPaint { get; }
        Transform BossTransform { get; }
        MechanicGrammar MechanicEngine { get; }
        ISkillSceneRuntime SceneRuntime { get; }
    }
}
