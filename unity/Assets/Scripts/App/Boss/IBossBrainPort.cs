using System;
using Dovus.Core.Combat;

namespace Dovus.App.Boss
{
    public interface IBossBrainPort
    {
        void HideTelegraphAndClearThreat();
        void ClearThreat();

        int IdleMinMs { get; }
        int IdleMaxMs { get; }

        bool IsPlayerDown { get; }

        /// <summary>Faz 2 duyurusu gerekiyorsa yapar (kükreme sunumu).</summary>
        void TryAnnouncePhase2Roar();

        bool IsVisualBusy { get; }
        void SetVisualSpeedZero();

        void ExtendIdleWait(double worldMs, ref double idleUntilWorldMs);

        bool ShouldRetarget { get; }
        void PickTarget();
        void Approach(float dtSec);

        bool CanStartStandingAttack { get; }
        bool SelectNextAttack();
        bool CanStartWindupGate { get; }

        BossAttack CurrentAttack { get; }
        float CurrentPhaseSpeed { get; }
        float AttackRadiusM { get; }

        bool IsPounceAirborne { get; }
        void ResolveStrike();
        void LogResolveStrikeException(Exception e);
        void AfterStrikeResolved(BossAttackKind kind, float attackRadiusM);
        void RaiseAttackStruck(BossAttackKind kind);

        void OnRecoveryTelegraph(float fade, float attackRadiusM);

        void OnEnterIdleAfterState();
        void OnEnterWindup(double worldMs);
        void OnEnterActive(double worldMs);
        void OnEnterRecovery(double worldMs);

        void OnWindupTelegraph(float progress01, float attackRadiusM, SlamVariant variant);
        void OnWindupThreat(float progress01);
    }
}
