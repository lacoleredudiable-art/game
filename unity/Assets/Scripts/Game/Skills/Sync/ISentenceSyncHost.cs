using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Combat;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
using Dovus.Game.Skills.Flow;

namespace Dovus.Game.Skills.Sync
{
    public interface ISentenceSyncHost
    {
        SentenceEngine Engine { get; }
        GameClock Clock { get; }
        HexagonInput Input { get; }
        PlayerStateMachine PlayerStates { get; }
        ActorStatus PlayerStatus { get; }
        int PendingClosingCount { get; }

        PlayerVitals CachedPlayerVitals();
        bool SwapDrawUnlocked(double worldMs);

        void EnsureSkillServices();
        SentenceManifestationBridge SentenceBridge { get; }
    }
}
