using System;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;

namespace Dovus.App.Boss
{
    public sealed class BossBrain
    {
        readonly Random _rng;
        readonly IBossBrainPort _port;

        BossBrainPhase _phase = BossBrainPhase.Idle;
        double _phaseStartedWorldMs;
        double _phaseElapsedMs;
        double _idleUntilWorldMs;
        int _telegraphStartMs;
        int _strikeWorldMs;
        bool _strikeResolved;

        public BossBrain(Random rng, IBossBrainPort port)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _port = port ?? throw new ArgumentNullException(nameof(port));
        }

        public BossBrainPhase Phase => _phase;
        public double PhaseStartedWorldMs => _phaseStartedWorldMs;
        public double PhaseElapsedMs => _phaseElapsedMs;
        public int TelegraphStartMs => _telegraphStartMs;
        public int StrikeWorldMs => _strikeWorldMs;
        public bool StrikeResolved => _strikeResolved;

        public void EnterIdle(double worldMs)
        {
            _phase = BossBrainPhase.Idle;
            _phaseStartedWorldMs = worldMs;
            int lo = Math.Min(_port.IdleMinMs, _port.IdleMaxMs);
            int hi = Math.Max(_port.IdleMinMs, _port.IdleMaxMs);
            int wait = _rng.Next(lo, hi + 1);
            _idleUntilWorldMs = worldMs + wait;
            _port.PickTarget();
            _port.OnEnterIdleAfterState();
        }

        public void ForceIdleWithoutEnter(bool markStrikeResolved)
        {
            _phase = BossBrainPhase.Idle;
            if (markStrikeResolved)
                _strikeResolved = true;
        }

        public void EnsurePhaseIdleIfNot()
        {
            if (_phase != BossBrainPhase.Idle)
                _phase = BossBrainPhase.Idle;
        }

        public void Tick(double worldMs, float dtSec, double worldDtMs)
        {
            switch (_phase)
            {
                case BossBrainPhase.Idle:
                    TickIdle(worldMs, dtSec);
                    break;
                case BossBrainPhase.Windup:
                    TickWindup(worldMs, worldDtMs);
                    break;
                case BossBrainPhase.Active:
                    TickActive(worldMs);
                    break;
                case BossBrainPhase.Recovery:
                    TickRecovery(worldMs, worldDtMs);
                    break;
            }
        }

        void TickIdle(double worldMs, float dtSec)
        {
            _port.HideTelegraphAndClearThreat();

            if (_port.IsPlayerDown)
            {
                _idleUntilWorldMs = worldMs + _port.IdleMinMs;
                return;
            }

            _port.TryAnnouncePhase2Roar();

            if (_port.IsVisualBusy)
            {
                _port.SetVisualSpeedZero();
                _port.ExtendIdleWait(worldMs, ref _idleUntilWorldMs);
                return;
            }

            if (_port.ShouldRetarget)
                _port.PickTarget();

            _port.Approach(dtSec);

            if (worldMs >= _idleUntilWorldMs)
            {
                if (!_port.CanStartStandingAttack)
                    return;
                if (!_port.SelectNextAttack())
                    return;
                if (!_port.CanStartWindupGate)
                    return;
                EnterWindup(worldMs);
            }
        }

        void TickWindup(double worldMs, double worldDtMs)
        {
            BossAttack attack = _port.CurrentAttack;
            if (attack == null)
                return;

            _phaseElapsedMs += Math.Max(0, worldDtMs) * _port.CurrentPhaseSpeed;
            float p = attack.WindupMs > 0
                ? (float)(_phaseElapsedMs / attack.WindupMs)
                : 1f;
            _port.OnWindupTelegraph(p, _port.AttackRadiusM, attack.Variant);
            _port.OnWindupThreat(p);

            if (_phaseElapsedMs >= attack.WindupMs)
                EnterActive(worldMs);
        }

        void TickActive(double worldMs)
        {
            BossAttack attack = _port.CurrentAttack;
            bool airborne = _port.IsPounceAirborne;
            if (!_strikeResolved && !airborne)
            {
                _strikeResolved = true;
                try
                {
                    _port.ResolveStrike();
                }
                catch (Exception e)
                {
                    _port.LogResolveStrikeException(e);
                }

                if (attack != null)
                {
                    _port.AfterStrikeResolved(attack.Kind, _port.AttackRadiusM);
                    _port.RaiseAttackStruck(attack.Kind);
                }
            }

            if (attack != null && _strikeResolved && worldMs >= attack.ActiveEndMs(_telegraphStartMs))
                EnterRecovery(worldMs);
        }

        void TickRecovery(double worldMs, double worldDtMs)
        {
            _port.ClearThreat();
            BossAttack attack = _port.CurrentAttack;
            _phaseElapsedMs += Math.Max(0, worldDtMs) * _port.CurrentPhaseSpeed;
            float fade = attack != null && attack.RecoveryMs > 0
                ? 1f - (float)(_phaseElapsedMs / attack.RecoveryMs)
                : 0f;
            _port.OnRecoveryTelegraph(fade, _port.AttackRadiusM);

            if (attack != null && _phaseElapsedMs >= attack.RecoveryMs)
                EnterIdle(worldMs);
        }

        void EnterWindup(double worldMs)
        {
            _phase = BossBrainPhase.Windup;
            _phaseStartedWorldMs = worldMs;
            _phaseElapsedMs = 0;
            _strikeWorldMs = 0;
            _telegraphStartMs = (int)worldMs;
            _strikeResolved = false;
            _port.OnEnterWindup(worldMs);
        }

        void EnterActive(double worldMs)
        {
            _phase = BossBrainPhase.Active;
            _phaseStartedWorldMs = worldMs;
            _phaseElapsedMs = 0;
            _strikeWorldMs = (int)worldMs;
            _port.OnEnterActive(worldMs);
        }

        void EnterRecovery(double worldMs)
        {
            _phase = BossBrainPhase.Recovery;
            _phaseStartedWorldMs = worldMs;
            _phaseElapsedMs = 0;
            _port.OnEnterRecovery(worldMs);
        }
    }
}
