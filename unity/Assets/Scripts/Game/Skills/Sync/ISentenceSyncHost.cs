using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using Dovus.Game.Skills.Flow;

namespace Dovus.Game.Skills.Sync
{
    public interface ISentenceSyncHost
    {
        SentenceEngine Engine { get; }
        GameClockHost Clock { get; }
        HexagonInputController Input { get; }
        PlayerStateMachine PlayerStates { get; }
        ActorStatusHost PlayerStatus { get; }
        int PendingClosingCount { get; }

        PlayerVitalsHost CachedPlayerVitals();
        bool SwapDrawUnlocked(double worldMs);

        void EnsureSkillServices();
        SentenceManifestationBridge SentenceBridge { get; }
    }
}
