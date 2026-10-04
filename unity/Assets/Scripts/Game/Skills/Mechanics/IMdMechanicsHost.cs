using Dovus.Core.Grammar;
using Dovus.Game.Platform;
using Dovus.Core.Mechanic;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Skills.State;
using Dovus.Game.Team;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Skills.Mechanics
{
    /// <summary>Sahne kökü + durum nesneleri; setter'lar SkillWorldState / CastSessionState üzerinde.</summary>
    public interface IMdMechanicsHost
    {
        SkillWorldState World { get; }
        CastSessionState Cast { get; }
        Transform Player { get; }
        AllyDummyController Ally { get; }
        BossReactorController Boss { get; }
        ActorStatusHost BossStatus { get; }
        ActorStatusHost PlayerStatus { get; }
        GameClockHost Clock { get; }
        CombatTuning Combat { get; }
        MechanicGrammar MechanicEngine { get; }
        MechanicRules JsonRules { get; }
        double JsonNow { get; }
        Transform DirectorTransform { get; }
        TeamComboAccess TeamAccess { get; }
        PlaceholderFactory Placeholders { get; }
    }
}
