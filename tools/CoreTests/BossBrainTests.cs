using System;
using System.Collections.Generic;
using Dovus.App.Boss;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class BossBrainTests
{
    sealed class RecordingPort : IBossBrainPort
    {
        public readonly List<string> Calls = new();

        public BossAttack Attack { get; set; }
        public bool PlayerDown;
        public bool VisualBusy;
        public bool SelectOk = true;
        public bool GateOk = true;
        public bool StandingOk = true;
        public bool RetargetNeeded;
        public bool ThrowOnResolve;
        public int IdleMinMsValue = 50;
        public int IdleMaxMsValue = 50;
        public float PhaseSpeed = 1f;

        public int IdleMinMs => IdleMinMsValue;
        public int IdleMaxMs => IdleMaxMsValue;

        public void HideTelegraphAndClearThreat() => Calls.Add(nameof(HideTelegraphAndClearThreat));
        public void ClearThreat() => Calls.Add(nameof(ClearThreat));

        public bool IsPlayerDown => PlayerDown;

        public void TryAnnouncePhase2Roar() => Calls.Add(nameof(TryAnnouncePhase2Roar));

        public bool IsVisualBusy => VisualBusy;
        public void SetVisualSpeedZero() => Calls.Add(nameof(SetVisualSpeedZero));

        public void ExtendIdleWait(double worldMs, ref double idleUntilWorldMs)
        {
            Calls.Add(nameof(ExtendIdleWait));
            idleUntilWorldMs = Math.Max(idleUntilWorldMs, worldMs + IdleMinMsValue);
        }

        public bool ShouldRetarget => RetargetNeeded;
        public void PickTarget() => Calls.Add(nameof(PickTarget));
        public void Approach(float dtSec) => Calls.Add(nameof(Approach));

        public bool CanStartStandingAttack => StandingOk;
        public bool SelectNextAttack()
        {
            Calls.Add(nameof(SelectNextAttack));
            return SelectOk;
        }

        public bool CanStartWindupGate => GateOk;

        public BossAttack CurrentAttack => Attack;
        public float CurrentPhaseSpeed => PhaseSpeed;
        public float AttackRadiusM => 1f;

        public bool IsPounceAirborne => false;

        public void ResolveStrike()
        {
            Calls.Add(nameof(ResolveStrike));
            if (ThrowOnResolve)
                throw new InvalidOperationException("test");
        }

        public void LogResolveStrikeException(Exception e) => Calls.Add(nameof(LogResolveStrikeException));

        public void AfterStrikeResolved(BossAttackKind kind, float attackRadiusM) =>
            Calls.Add(nameof(AfterStrikeResolved));

        public void RaiseAttackStruck(BossAttackKind kind) => Calls.Add(nameof(RaiseAttackStruck));

        public void OnRecoveryTelegraph(float fade, float attackRadiusM) =>
            Calls.Add(nameof(OnRecoveryTelegraph));

        public void OnEnterIdleAfterState() => Calls.Add(nameof(OnEnterIdleAfterState));
        public void OnEnterWindup(double worldMs) => Calls.Add(nameof(OnEnterWindup));
        public void OnEnterActive(double worldMs) => Calls.Add(nameof(OnEnterActive));
        public void OnEnterRecovery(double worldMs) => Calls.Add(nameof(OnEnterRecovery));

        public void OnWindupTelegraph(float progress01, float attackRadiusM, SlamVariant variant) =>
            Calls.Add(nameof(OnWindupTelegraph));

        public void OnWindupThreat(float progress01) => Calls.Add(nameof(OnWindupThreat));
    }

    static BossAttack SlamAttack(int windupMs = 100, int activeMs = 50, int recoveryMs = 80)
    {
        var tuning = new BossTuning
        {
            WindupMs = windupMs,
            ActiveMs = activeMs,
            RecoveryMs = recoveryMs
        };
        var a = new BossAttack(tuning);
        a.ApplyVariant(SlamVariant.Yakin);
        return a;
    }

    [Test]
    public void FullCycle_IdleWindupActiveRecoveryIdle_CallOrder()
    {
        var port = new RecordingPort { Attack = SlamAttack() };
        var rng = new Random(0x26b_01);
        var brain = new BossBrain(rng, port);

        brain.EnterIdle(0);
        Assert.That(port.Calls, Does.Contain("PickTarget"));
        Assert.That(brain.Phase, Is.EqualTo(BossBrainPhase.Idle));

        port.Calls.Clear();
        brain.Tick(50, 0.016f, 16);
        Assert.That(port.Calls, Does.Contain("SelectNextAttack"));
        Assert.That(port.Calls, Does.Contain("OnEnterWindup"));
        Assert.That(brain.Phase, Is.EqualTo(BossBrainPhase.Windup));

        port.Calls.Clear();
        brain.Tick(50, 0f, 100);
        Assert.That(port.Calls, Does.Contain("OnEnterActive"));
        Assert.That(brain.Phase, Is.EqualTo(BossBrainPhase.Active));

        int telegraphStart = brain.TelegraphStartMs;
        int activeEnd = port.Attack.ActiveEndMs(telegraphStart);
        port.Calls.Clear();
        brain.Tick(activeEnd, 0f, 0);
        Assert.That(port.Calls, Does.Contain("ResolveStrike"));
        Assert.That(port.Calls, Does.Contain("RaiseAttackStruck"));
        Assert.That(port.Calls, Does.Contain("OnEnterRecovery"));
        Assert.That(brain.Phase, Is.EqualTo(BossBrainPhase.Recovery));

        port.Calls.Clear();
        brain.Tick(activeEnd + port.Attack.RecoveryMs, 0f, port.Attack.RecoveryMs);
        Assert.That(port.Calls, Does.Contain("PickTarget"));
        Assert.That(brain.Phase, Is.EqualTo(BossBrainPhase.Idle));
    }

    [Test]
    public void PlayerDown_ExtendsIdleWithoutWindup()
    {
        var port = new RecordingPort { Attack = SlamAttack(), PlayerDown = true };
        var brain = new BossBrain(new Random(1), port);
        brain.EnterIdle(0);
        port.Calls.Clear();
        brain.Tick(10_000, 0.016f, 16);
        Assert.That(port.Calls, Does.Not.Contain("SelectNextAttack"));
        Assert.That(brain.Phase, Is.EqualTo(BossBrainPhase.Idle));
    }

    [Test]
    public void VisualBusy_WaitsWithoutWindup()
    {
        var port = new RecordingPort { Attack = SlamAttack(), VisualBusy = true };
        var brain = new BossBrain(new Random(1), port);
        brain.EnterIdle(0);
        port.Calls.Clear();
        brain.Tick(10_000, 0.016f, 16);
        Assert.That(port.Calls, Does.Contain("SetVisualSpeedZero"));
        Assert.That(port.Calls, Does.Not.Contain("SelectNextAttack"));
    }

    [Test]
    public void SelectFalse_NoWindup()
    {
        var port = new RecordingPort { Attack = SlamAttack(), SelectOk = false };
        var brain = new BossBrain(new Random(1), port);
        brain.EnterIdle(0);
        port.Calls.Clear();
        brain.Tick(50, 0.016f, 16);
        Assert.That(port.Calls, Does.Contain("SelectNextAttack"));
        Assert.That(port.Calls, Does.Not.Contain("OnEnterWindup"));
    }

    [Test]
    public void GateFalse_NoWindup()
    {
        var port = new RecordingPort { Attack = SlamAttack(), GateOk = false };
        var brain = new BossBrain(new Random(1), port);
        brain.EnterIdle(0);
        port.Calls.Clear();
        brain.Tick(50, 0.016f, 16);
        Assert.That(port.Calls, Does.Not.Contain("OnEnterWindup"));
    }

    [Test]
    public void ResolveStrikeException_StillReachesRecovery()
    {
        var port = new RecordingPort { Attack = SlamAttack(), ThrowOnResolve = true };
        var brain = new BossBrain(new Random(1), port);
        brain.EnterIdle(0);
        brain.Tick(50, 0f, 0);
        brain.Tick(50, 0f, 100);
        int activeEnd = port.Attack.ActiveEndMs(brain.TelegraphStartMs);
        port.Calls.Clear();
        brain.Tick(activeEnd, 0f, 0);
        Assert.That(port.Calls, Does.Contain("LogResolveStrikeException"));
        Assert.That(brain.Phase, Is.EqualTo(BossBrainPhase.Recovery));
    }

    [Test]
    public void CancelWindup_OnlyInWindupPhase()
    {
        var port = new RecordingPort { Attack = SlamAttack() };
        var brain = new BossBrain(new Random(1), port);
        brain.EnterIdle(0);
        Assert.That(brain.Phase, Is.Not.EqualTo(BossBrainPhase.Windup));
        brain.Tick(50, 0f, 100);
        Assert.That(brain.Phase, Is.EqualTo(BossBrainPhase.Windup));
        brain.EnterIdle(100);
        Assert.That(brain.Phase, Is.EqualTo(BossBrainPhase.Idle));
    }

    [Test]
    public void EnterIdle_ConsumesOneRngRollPerEnter()
    {
        var port = new RecordingPort { IdleMinMsValue = 3, IdleMaxMsValue = 3 };
        var rng = new Random(42);
        var refRng = new Random(42);
        int roll1 = refRng.Next(3, 4);
        int roll2 = refRng.Next(3, 4);
        var brain = new BossBrain(rng, port);
        brain.EnterIdle(0);
        brain.EnterIdle(100);
        Assert.That(roll1, Is.EqualTo(3));
        Assert.That(roll2, Is.EqualTo(3));
        Assert.That(rng.Next(3, 4), Is.EqualTo(refRng.Next(3, 4)));
    }
}
