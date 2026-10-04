using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using UnityEngine;

namespace Dovus.Game.Casting.Input
{
    public sealed class DodgeTrigger
    {
        readonly HexagonInputSession _s;
        readonly System.Action _flushInkBreak;
        readonly System.Action _syncPlayerState;

        public DodgeTrigger(
            HexagonInputSession session,
            System.Action flushInkBreak,
            System.Action syncPlayerState)
        {
            _s = session;
            _flushInkBreak = flushInkBreak;
            _syncPlayerState = syncPlayerState;
        }

        public void TickCharges()
        {
            if (_s.Charges == null)
                return;
            int worldMs = _s.Clock != null ? (int)_s.Clock.Director.WorldTimeMs : 0;
            _s.Charges.RechargeMult = _s.Dodge != null ? _s.Dodge.CooldownMult : 1f;
            _s.Charges.Tick(worldMs);
        }

        public void TriggerDodge()
        {
            int worldMs = _s.Clock != null ? (int)_s.Clock.Director.WorldTimeMs : 0;
            if (_s.Dodge == null)
                return;

            bool dead = _s.Vitals != null && _s.Vitals.IsDown;
            bool stun = false;
            bool freeze = false;
            bool knockdown = false;
            if (_s.Status != null)
            {
                var board = _s.Status.Board;
                stun = board.Has(StatusKind.Stun) || board.Has(StatusKind.Fear);
                freeze = board.Has(StatusKind.Stasis);
                knockdown = board.Has(StatusKind.Knockback);
            }

            _syncPlayerState();
            bool stateAllows = _s.PlayerStates == null || _s.PlayerStates.AllowsDodgeGate;
            if (!stateAllows || !DodgeCancelRules.Allowed(dead, stun, freeze, knockdown))
            {
                _s.Readout?.NoteDenied("dodge yok");
                _s.Syllable?.PlayDenied();
                return;
            }

            if (_s.Charges != null)
            {
                _s.Charges.RechargeMult = _s.Dodge.CooldownMult;
                DodgeChain.PressOutcome outcome = DodgeChain.TryConsumePress(
                    worldMs, _s.Dodge, _s.Charges, _s.Combat.Dodge);
                if (outcome == DodgeChain.PressOutcome.DeniedNoCharge)
                {
                    _s.Readout?.NoteDenied("dodge yok");
                    _s.Syllable?.PlayDenied();
                    return;
                }

                if (outcome == DodgeChain.PressOutcome.UpgradedCombined)
                    return;
            }
            else
                _s.Dodge.Begin(worldMs);

            bool wasBuilding = _s.Engine != null && _s.Engine.State.Phase == SentencePhase.Building;
            _s.Engine?.Abort();
            _flushInkBreak();
            _s.RaiseSkillCancelledByDodge?.Invoke();
            _s.DebugHud?.NoteDodge(wasBuilding);
            if (_s.Mode == FingerMode.Drawing)
                _s.Ink?.RawEnd(false);
            _s.Mode = FingerMode.None;
            _s.ActiveDot = null;
            _s.LastInkPx = null;
        }
    }
}
