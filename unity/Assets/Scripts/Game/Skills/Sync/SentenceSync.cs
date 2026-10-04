using Dovus.Core.Grammar;
using Dovus.Core.Status;

namespace Dovus.Game.Skills.Sync
{
    public sealed class SentenceSync
    {
        readonly ISentenceSyncHost _host;

        public SentenceSync(ISentenceSyncHost host) => _host = host;

        public void SyncFromSentence(double worldMs)
        {
            _host.EnsureSkillServices();
            _host.SentenceBridge.SyncBuilding(worldMs);
        }

        public void SyncPlayerStateMachine(double worldMs)
        {
            if (_host.PlayerStates == null)
                return;

            var vitals = _host.CachedPlayerVitals();
            bool isDead = vitals != null && vitals.IsDown;
            bool isStunned = false;
            bool isRooted = false;
            if (_host.PlayerStatus != null)
            {
                var board = _host.PlayerStatus.Board;
                isStunned = board.Has(StatusKind.Stun) || board.Has(StatusKind.Stasis) || board.Has(StatusKind.Fear);
                isRooted = board.Has(StatusKind.Root);
            }

            int worldMsInt = (int)worldMs;
            bool isDodging = _host.Input?.Dodge != null && _host.Input.Dodge.IsActive(worldMsInt);
            bool isCasting = _host.PendingClosingCount > 0 && !_host.SwapDrawUnlocked(worldMs);
            bool isDrawing = _host.Engine != null && _host.Engine.State.Phase == SentencePhase.Building;
            bool isRecovering = _host.Engine != null && _host.Engine.State.Phase == SentencePhase.Recovering;

            _host.PlayerStates.SyncWorld(
                isDead, isStunned, isDodging, isRooted, isCasting, isDrawing, isRecovering);
        }
    }
}
