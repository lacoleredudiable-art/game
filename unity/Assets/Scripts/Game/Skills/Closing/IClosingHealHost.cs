using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using Dovus.Core.Manifestation;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.DevTools;
using Dovus.Game.Hud;
using UnityEngine;

namespace Dovus.Game.Skills.Closing
{
    public interface IClosingHealHost
    {
        Transform Player { get; }
        AllyDummyController Ally { get; }
        PlayerVitalsHost CachedPlayerVitals();
        ActorStatusHost PlayerStatus { get; }
        CombatTuning Combat { get; }
        float ClosingChainBonus { get; }
        DamageNumberHud DamageHud { get; }
        ReactionReadoutHud Readout { get; }
        SentenceDebugHud DebugHud { get; }
        bool LastFriendlyWasAlly { set; }
        float WeaponSupportPower(SkillResolution skill);
        float HealBuffMultiplier(SkillResolution skill);
        int FriendlyTargetCap(SkillResolution skill);
        void ConsumeWeaponBonus(StatusBoard board);
        void ApplyHealOverflow(SkillResolution skill, int amount, int healed, bool ally);
    }
}
