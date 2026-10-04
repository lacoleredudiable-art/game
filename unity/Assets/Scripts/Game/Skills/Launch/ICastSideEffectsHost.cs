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
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Composition;
using Dovus.Game.Data;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Core.Mechanic;
using Dovus.Game.DevTools;
using Dovus.Game.Hud;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Launch
{
    public interface ICastSideEffectsHost
    {
        PlayerResource PlayerResource { get; }
        ActorStatus PlayerStatus { get; }
        MobilityCcData MobilityCc { get; }
        EquipmentItem EquippedWeapon { get; }
        GameClock Clock { get; }
        CombatTuning Combat { get; }
        SkillMotor Skills { get; }
        SkillFactory SkillFactory { get; }
        HexagonView HexagonView { get; }
        PlayerCooldown PlayerCooldown { get; }
        Transform Player { get; }
        Transform BossTransform { get; }
        BossVitals BossVitals { get; }
        GameTuning Colors { get; }
        VerbExecutionData VerbData { get; }
        ElementPaintNode? SelectedElementPaint { get; }
        Skill LastFactorySkill { get; set; }
        MechanicPlan LastMechanicPlan { get; }
        SentenceDebugHud DebugHud { get; }

        bool TryTakeFreeMana();
        float WeaponCooldownMult();
        SkillResolution ResolveSkillWords(IReadOnlyList<SentenceWord> words);
    }
}
