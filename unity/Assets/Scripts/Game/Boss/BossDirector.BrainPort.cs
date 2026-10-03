using Dovus.App.Boss;
using Dovus.Core.Combat;
using Dovus.Core.Status;
using UnityEngine;

namespace Dovus.Game.Boss
{
    public sealed partial class BossDirector
    {
        sealed class BossBrainPort : IBossBrainPort
        {
            readonly BossDirector _d;

            public BossBrainPort(BossDirector director)
            {
                _d = director;
            }

            public void HideTelegraphAndClearThreat()
            {
                _d._telegraph?.Hide();
                _d._feel?.ClearThreat();
            }

            public void ClearThreat() => _d._feel?.ClearThreat();

            public int IdleMinMs => _d._combat.Boss.IdleMinMs;
            public int IdleMaxMs => _d._combat.Boss.IdleMaxMs;

            public bool IsPlayerDown => _d._vitals != null && _d._vitals.IsDown;

            public void TryAnnouncePhase2Roar()
            {
                if (_d._phase2Announced || !_d.IsEnraged())
                    return;
                _d._phase2Announced = true;
                _d._visual?.PlayRoar();
                _d.BossPhaseChanged?.Invoke(2);
            }

            public bool IsVisualBusy => _d._visual != null && _d._visual.IsBusy;

            public void SetVisualSpeedZero() => _d._visual?.SetSpeed(0f);

            public void ExtendIdleWait(double worldMs, ref double idleUntilWorldMs)
            {
                idleUntilWorldMs = System.Math.Max(idleUntilWorldMs, worldMs + IdleMinMs);
            }

            public bool ShouldRetarget =>
                _d._targets != null && _d._targets.ShouldRetarget(_d._targetId);

            public void PickTarget() => _d.PickTarget();

            public void Approach(float dtSec) => _d.Approach(dtSec);

            public bool CanStartStandingAttack =>
                _d._bossStatus == null
                || BossAttackControl.Evaluate(_d._bossStatus.Board, BossAttackMotion.Standing).CanStart;

            public bool SelectNextAttack() => _d.SelectNextAttack();

            public bool CanStartWindupGate =>
                _d._bossStatus == null
                || _d._attack == null
                || BossAttackControl.Gate(
                    _d._bossStatus.Board,
                    _d.CurrentMotion(),
                    _d._attack.Kind,
                    staggered: false).CanStart;

            public BossAttack CurrentAttack => _d._attack;

            public float CurrentPhaseSpeed => _d.CurrentPhaseSpeed();

            public float AttackRadiusM => _d.AttackRadiusM;

            public bool IsPounceAirborne =>
                _d._pounceLeapActive && _d._attack != null && _d._attack.Kind == BossAttackKind.Pounce;

            public void ResolveStrike() => _d.ResolveStrike();

            public void LogResolveStrikeException(System.Exception e) => Debug.LogException(e);

            public void AfterStrikeResolved(BossAttackKind kind, float attackRadiusM)
            {
                if (kind is BossAttackKind.WebField or BossAttackKind.Pounce)
                    _d._telegraph?.Hide();
                else
                    _d._telegraph?.Slam(attackRadiusM);
            }

            public void RaiseAttackStruck(BossAttackKind kind) => _d.AttackStruck?.Invoke(kind);

            public void OnRecoveryTelegraph(float fade, float attackRadiusM) =>
                _d._telegraph?.Recover(fade, attackRadiusM);

            public void OnEnterIdleAfterState()
            {
                _d._telegraph?.Hide();
                if (_d._bossVitals == null || !_d._bossVitals.IsDown)
                    _d._visual?.PlayIdle();
            }

            public void OnEnterWindup(double worldMs)
            {
                _d.FaceTarget();
                _d._visual?.SetSpeed(0f);
                if (_d._attack == null)
                    return;
                if (_d._attack.Kind == BossAttackKind.WebField)
                {
                    Transform aim = _d.AimTarget();
                    _d.LastWebFieldTarget = aim != null ? aim.position : _d._reactor.Home;
                    _d._telegraph?.SetWorldAnchor(BossAttackKind.WebField, _d.LastWebFieldTarget);
                }
                else if (_d._attack.Kind == BossAttackKind.Pounce)
                {
                    Vector3 land = new Vector3(_d._attack.LandingX, _d._reactor.Home.y, _d._attack.LandingZ);
                    _d._telegraph?.SetWorldAnchor(BossAttackKind.Pounce, land);
                }
                else
                    _d._telegraph?.ClearWorldAnchor();
                _d._telegraph?.SetShape(_d._attack.ArcHalfAngleDeg);
                _d._visual?.PlayWindup(_d._attack.Kind, _d._attack.WindupMs);
                _d.AttackWindupStarted?.Invoke(_d._attack.Kind);
            }

            public void OnEnterActive(double worldMs)
            {
                _d._visual?.PlaySlam();
                if (_d.IsAglarinQueen()
                    && _d._attack != null
                    && _d._attack.Kind == BossAttackKind.Pounce
                    && _d._motionBody != null
                    && _d._reactor != null
                    && _d._combat != null)
                {
                    _d.BeginPounceLeap();
                }
            }

            public void OnEnterRecovery(double worldMs)
            {
            }

            public void OnWindupTelegraph(float progress01, float attackRadiusM, SlamVariant variant) =>
                _d._telegraph?.SetProgress(progress01, attackRadiusM, variant);

            public void OnWindupThreat(float progress01) => _d._feel?.ShowThreat(progress01);
        }
    }
}
