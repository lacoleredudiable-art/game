using System;
using System.Collections.Generic;
using System.Linq;
using Dovus.Core.Boss;
using NUnit.Framework;

namespace CoreTests;

/// <summary>B8: Kül Ejderi durum makinesi (18-kul-ejderi.md KE-3 - KE-8). Saf mantık; Unity yok.</summary>
[TestFixture]
public sealed class KulEjderiBrainTests
{
    const double Step = 20;

    static KulEjderiBrain NewBrain(int seed = 1, bool enabled = true) =>
        new KulEjderiBrain(new Random(seed)) { Enabled = enabled };

    /// <summary>t0'dan t1'e kadar 20 ms adımlarla ilerletir, olayları toplar.</summary>
    static List<KulEjderiEvent> Run(KulEjderiBrain brain, double t0, double t1)
    {
        var events = new List<KulEjderiEvent>();
        for (double t = t0; t <= t1; t += Step)
        {
            brain.Tick(t);
            brain.DrainEvents(events);
        }

        return events;
    }

    /// <summary>Koşul tutana dek ilerletir; tutmazsa testi başarısız eder. Dönen değer o anki zaman.</summary>
    static double RunUntil(KulEjderiBrain brain, double from, Func<KulEjderiBrain, bool> condition, double limitMs = 900000)
    {
        for (double t = from; t <= from + limitMs; t += Step)
        {
            brain.Tick(t);
            if (condition(brain))
                return t;
        }

        Assert.Fail("Koşul süre sınırında gerçekleşmedi.");
        return from;
    }

    static KulEjderiBrain AwakeBrain(int seed = 1)
    {
        KulEjderiBrain brain = NewBrain(seed);
        Assert.That(brain.Wake(0), Is.True);
        brain.Tick(KulEjderiDefaults.AwakenMs);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Idle));
        brain.DrainEvents(new List<KulEjderiEvent>());
        return brain;
    }

    [Test]
    public void KE_Disabled_brain_ignores_wake_and_tick_so_test_boss_stays_fixed()
    {
        var brain = NewBrain(enabled: false);

        Assert.That(brain.Wake(0), Is.False);
        brain.Tick(100000);
        brain.OnHpRatio(0.1f, 100000);

        var events = new List<KulEjderiEvent>();
        brain.DrainEvents(events);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Sleeping));
        Assert.That(events, Is.Empty);
    }

    [Test]
    public void KE_5_1_Sleeping_boss_never_attacks_until_woken()
    {
        var brain = NewBrain();

        var events = Run(brain, 0, 120000);

        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Sleeping));
        Assert.That(events, Is.Empty);
    }

    [Test]
    public void KE_5_3_Awakening_takes_4s_takes_no_damage_and_first_attack_comes_within_2s()
    {
        var brain = NewBrain();
        brain.Wake(1000);

        brain.Tick(1000 + KulEjderiDefaults.AwakenMs - 1);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Awakening));
        Assert.That(brain.AcceptsDamage, Is.False);

        brain.Tick(1000 + KulEjderiDefaults.AwakenMs);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Idle));
        Assert.That(brain.AcceptsDamage, Is.True);

        double firstAttackAt = 1000 + KulEjderiDefaults.AwakenMs + KulEjderiDefaults.FirstAttackAfterAwakenMs;
        brain.Tick(firstAttackAt - 1);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Idle));
        brain.Tick(firstAttackAt);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Telegraph));
    }

    [Test]
    public void KE_5_4_Attack_cycle_follows_spec_telegraph_active_recovery()
    {
        var brain = AwakeBrain();
        double t = RunUntil(brain, KulEjderiDefaults.AwakenMs, b => b.State == KulEjderiState.Telegraph);
        KulEjderiAttackSpec spec = brain.CurrentAttack!;
        var events = new List<KulEjderiEvent>();
        brain.DrainEvents(events);

        events.AddRange(Run(brain, t, t + spec.TelegraphMs + spec.ActiveMs + spec.RecoveryMs + 100));

        double telegraphAt = events.First(e => e.Kind == KulEjderiEventKind.TelegraphStarted).WorldMs;
        double struckAt = events.First(e => e.Kind == KulEjderiEventKind.Struck).WorldMs;
        double recoveryAt = events.First(e => e.Kind == KulEjderiEventKind.RecoveryStarted).WorldMs;
        Assert.That(struckAt - telegraphAt, Is.EqualTo(spec.TelegraphMs));
        Assert.That(recoveryAt - struckAt, Is.EqualTo(spec.ActiveMs));
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Idle).Or.EqualTo(KulEjderiState.Recovery));
    }

    [Test]
    public void KE_4_2_Gap_after_recovery_is_between_1_5_and_3s()
    {
        var brain = AwakeBrain();
        var events = Run(brain, KulEjderiDefaults.AwakenMs, 200000);

        var recoveryEnds = new List<double>();
        var telegraphStarts = new List<double>();
        foreach (KulEjderiEvent e in events)
        {
            if (e.Kind == KulEjderiEventKind.TelegraphStarted)
                telegraphStarts.Add(e.WorldMs);
        }

        // Her açık kalma bitişi = RecoveryStarted + spec.RecoveryMs
        foreach (KulEjderiEvent e in events.Where(x => x.Kind == KulEjderiEventKind.RecoveryStarted))
            recoveryEnds.Add(e.WorldMs + KulEjderiAttackTable.For(e.Attack!.Value).RecoveryMs);

        Assert.That(recoveryEnds.Count, Is.GreaterThan(5));
        foreach (double end in recoveryEnds)
        {
            double next = telegraphStarts.FirstOrDefault(s => s >= end);
            if (next == 0)
                continue;
            Assert.That(next - end, Is.InRange((double)KulEjderiDefaults.AttackGapMinMs, (double)KulEjderiDefaults.AttackGapMaxMs));
        }
    }

    [Test]
    public void KE_4_2_Same_normal_attack_never_repeats_back_to_back()
    {
        var brain = AwakeBrain(seed: 7);
        var events = Run(brain, KulEjderiDefaults.AwakenMs, 300000);

        var normal = events
            .Where(e => e.Kind == KulEjderiEventKind.TelegraphStarted)
            .Select(e => e.Attack!.Value)
            .ToList();

        Assert.That(normal.Count, Is.GreaterThan(10));
        for (int i = 1; i < normal.Count; i++)
            Assert.That(normal[i], Is.Not.EqualTo(normal[i - 1]), $"{i}. saldırı bir öncekiyle aynı");
    }

    [Test]
    public void KE_4_3_Announced_attack_comes_after_30s_then_every_45_to_60s_and_is_not_interruptible()
    {
        var brain = AwakeBrain(seed: 3);
        double phaseStart = KulEjderiDefaults.AwakenMs;
        var events = Run(brain, phaseStart, phaseStart + 200000);

        var announced = events
            .Where(e => e.Kind == KulEjderiEventKind.TelegraphStarted && e.Attack == KulEjderiAttackId.EmberWave)
            .Select(e => e.WorldMs)
            .ToList();

        Assert.That(announced.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(announced[0], Is.GreaterThanOrEqualTo(phaseStart + KulEjderiDefaults.FirstAnnouncedAfterPhaseStartMs));
        Assert.That(announced[0], Is.LessThan(phaseStart + KulEjderiDefaults.FirstAnnouncedAfterPhaseStartMs + 12000));
        for (int i = 1; i < announced.Count; i++)
            Assert.That(announced[i] - announced[i - 1], Is.GreaterThanOrEqualTo(KulEjderiDefaults.AnnouncedIntervalMinMs));

        var fresh = AwakeBrain(seed: 3);
        double t = RunUntil(fresh, phaseStart, b => b.CurrentAttack != null && b.CurrentAttack.Announced && b.State == KulEjderiState.Telegraph);
        Assert.That(fresh.TryInterrupt(t, fromRange: true), Is.EqualTo(KulEjderiInterruptResult.NotInterruptible));
    }

    [Test]
    public void Interrupt_outside_telegraph_is_rejected()
    {
        var brain = AwakeBrain();

        Assert.That(brain.TryInterrupt(KulEjderiDefaults.AwakenMs, fromRange: false), Is.EqualTo(KulEjderiInterruptResult.NotTelegraphing));
    }

    [Test]
    public void KE_0_Interrupt_cancels_attack_staggers_1_5s_then_45s_resistance_turns_attempts_away()
    {
        var brain = AwakeBrain(seed: 2);
        double t = RunUntil(brain, KulEjderiDefaults.AwakenMs,
            b => b.State == KulEjderiState.Telegraph && b.CurrentAttack!.Interruptible);

        Assert.That(brain.TryInterrupt(t, fromRange: false), Is.EqualTo(KulEjderiInterruptResult.Interrupted));
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Staggered));
        Assert.That(brain.IsOpen, Is.True);
        Assert.That(brain.StateEndMs - t, Is.EqualTo(KulEjderiDefaults.InterruptStaggerMs));
        Assert.That(brain.IsResistant(t + KulEjderiDefaults.InterruptResistanceMs - 1), Is.True);
        Assert.That(brain.IsResistant(t + KulEjderiDefaults.InterruptResistanceMs), Is.False);

        double t2 = RunUntil(brain, t + Step,
            b => b.State == KulEjderiState.Telegraph && b.CurrentAttack!.Interruptible);
        Assert.That(t2, Is.LessThan(t + KulEjderiDefaults.InterruptResistanceMs));
        Assert.That(brain.TryInterrupt(t2, fromRange: false), Is.EqualTo(KulEjderiInterruptResult.Resistant));
    }

    [Test]
    public void KE_6_5_Sky_ember_breath_can_only_be_interrupted_by_ranged_control()
    {
        var brain = AwakeBrain(seed: 5);
        brain.OnHpRatio(0.5f, KulEjderiDefaults.AwakenMs);
        double t = RunUntil(brain, KulEjderiDefaults.AwakenMs,
            b => b.State == KulEjderiState.Telegraph && b.CurrentAttack!.Id == KulEjderiAttackId.SkyEmberBreath);

        Assert.That(brain.Phase, Is.EqualTo(KulEjderiPhase.Phase2));
        Assert.That(brain.TryInterrupt(t, fromRange: false), Is.EqualTo(KulEjderiInterruptResult.OutOfReach));
        Assert.That(brain.TryInterrupt(t, fromRange: true), Is.EqualTo(KulEjderiInterruptResult.Interrupted));
    }

    [Test]
    public void KE_6_1_Phase_threshold_starts_4s_transition_cancels_telegraph_and_enters_phase2()
    {
        var brain = AwakeBrain(seed: 4);
        double t = RunUntil(brain, KulEjderiDefaults.AwakenMs, b => b.State == KulEjderiState.Telegraph);
        brain.DrainEvents(new List<KulEjderiEvent>());

        brain.OnHpRatio(KulEjderiDefaults.Phase2HpRatio, t);

        var events = new List<KulEjderiEvent>();
        brain.DrainEvents(events);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Transition));
        Assert.That(brain.AcceptsDamage, Is.False);
        Assert.That(events.Select(e => e.Kind), Does.Contain(KulEjderiEventKind.Cancelled));
        Assert.That(events.Select(e => e.Kind), Does.Contain(KulEjderiEventKind.TransitionStarted));

        brain.Tick(t + KulEjderiDefaults.TransitionMs - 1);
        Assert.That(brain.Phase, Is.EqualTo(KulEjderiPhase.Phase1));
        brain.Tick(t + KulEjderiDefaults.TransitionMs);
        Assert.That(brain.Phase, Is.EqualTo(KulEjderiPhase.Phase2));
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Idle));
        brain.DrainEvents(events);
        Assert.That(events.Select(e => e.Kind), Does.Contain(KulEjderiEventKind.PhaseChanged));
    }

    [Test]
    public void KE_3_5_One_hp_drop_never_skips_a_phase()
    {
        var brain = AwakeBrain();
        double t = KulEjderiDefaults.AwakenMs;

        brain.OnHpRatio(0.1f, t);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Transition));

        brain.Tick(t + KulEjderiDefaults.TransitionMs);
        Assert.That(brain.Phase, Is.EqualTo(KulEjderiPhase.Phase2));
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Transition), "ikinci eşik ayrı bir geçişle gelir");

        brain.Tick(t + 2 * KulEjderiDefaults.TransitionMs);
        Assert.That(brain.Phase, Is.EqualTo(KulEjderiPhase.Phase3));
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Idle));
    }

    [Test]
    public void KE_7_12_Zero_hp_collapses_for_3s_then_dies_once()
    {
        var brain = AwakeBrain();
        double t = KulEjderiDefaults.AwakenMs;

        brain.OnHpRatio(0f, t);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Dying));
        Assert.That(brain.AcceptsDamage, Is.False);

        var events = Run(brain, t, t + KulEjderiDefaults.DyingMs + 60000);

        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Dead));
        Assert.That(events.Count(e => e.Kind == KulEjderiEventKind.Died), Is.EqualTo(1));
        Assert.That(events.Count(e => e.Kind == KulEjderiEventKind.TelegraphStarted), Is.EqualTo(0));
    }

    [Test]
    public void KE_3_7_Enrage_at_765s_fires_once_and_halves_recovery()
    {
        var brain = AwakeBrain(seed: 9);
        var events = Run(brain, KulEjderiDefaults.AwakenMs, KulEjderiDefaults.EnrageAtMs - 20);
        Assert.That(brain.IsEnraged, Is.False);

        events.AddRange(Run(brain, KulEjderiDefaults.EnrageAtMs, KulEjderiDefaults.EnrageAtMs + 5000));
        Assert.That(brain.IsEnraged, Is.True);
        Assert.That(events.Count(e => e.Kind == KulEjderiEventKind.Enraged), Is.EqualTo(1));

        // Öfkeden sonra başlayan ilk açık kalma yarı sürelidir (önce bir sonraki ön işarete gel).
        double t0 = RunUntil(brain, KulEjderiDefaults.EnrageAtMs + 5000, b => b.State == KulEjderiState.Telegraph, 60000);
        double t = RunUntil(brain, t0, b => b.State == KulEjderiState.Recovery, 60000);
        double expected = brain.CurrentAttack!.RecoveryMs * KulEjderiDefaults.EnrageRecoveryFactor;
        Assert.That(brain.StateEndMs - t, Is.InRange(expected - Step, expected));
    }

    [Test]
    public void KE_5_11_Team_task_in_phase1_allows_only_claw_slam_and_holds_announced_attack()
    {
        var brain = AwakeBrain(seed: 11);
        brain.TeamTaskActive = true;

        var events = Run(brain, KulEjderiDefaults.AwakenMs, KulEjderiDefaults.AwakenMs + 120000);

        var starts = events.Where(e => e.Kind == KulEjderiEventKind.TelegraphStarted).ToList();
        Assert.That(starts.Count, Is.GreaterThan(5));
        Assert.That(starts.All(e => e.Attack == KulEjderiAttackId.ClawSlam), Is.True);
    }

    [Test]
    public void KE_6_12_Team_task_in_phase2_stops_attacks_and_they_resume_right_after()
    {
        var brain = AwakeBrain();
        brain.OnHpRatio(0.5f, KulEjderiDefaults.AwakenMs);
        double t = RunUntil(brain, KulEjderiDefaults.AwakenMs, b => b.Phase == KulEjderiPhase.Phase2 && b.State == KulEjderiState.Idle);
        brain.TeamTaskActive = true;
        brain.DrainEvents(new List<KulEjderiEvent>());

        var during = Run(brain, t, t + 30000);
        Assert.That(during.Count(e => e.Kind == KulEjderiEventKind.TelegraphStarted), Is.EqualTo(0));

        brain.TeamTaskActive = false;
        double resumeAt = t + 30000 + Step;
        brain.Tick(resumeAt);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Telegraph));
    }

    [Test]
    public void KE_8_1_Poise_stagger_cancels_attack_lasts_7s_then_attack_cycle_resumes()
    {
        var brain = AwakeBrain();
        double t = RunUntil(brain, KulEjderiDefaults.AwakenMs, b => b.State == KulEjderiState.Telegraph);

        Assert.That(brain.BeginStagger(t, KulEjderiDefaults.PoiseStaggerMs), Is.True);

        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Staggered));
        Assert.That(brain.CurrentAttack, Is.Null);
        brain.Tick(t + KulEjderiDefaults.PoiseStaggerMs - 1);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Staggered));
        brain.Tick(t + KulEjderiDefaults.PoiseStaggerMs);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Idle));
        RunUntil(brain, t + KulEjderiDefaults.PoiseStaggerMs, b => b.State == KulEjderiState.Telegraph, 10000);
    }

    [Test]
    public void Stagger_is_refused_while_sleeping_or_transitioning()
    {
        var brain = NewBrain();
        Assert.That(brain.BeginStagger(0, KulEjderiDefaults.PoiseStaggerMs), Is.False);

        brain = AwakeBrain();
        brain.OnHpRatio(0.5f, KulEjderiDefaults.AwakenMs);
        Assert.That(brain.BeginStagger(KulEjderiDefaults.AwakenMs, KulEjderiDefaults.PoiseStaggerMs), Is.False);
    }

    [Test]
    public void KE_6_6_Wing_sweep_is_not_interruptible_and_keeps_20s_spacing()
    {
        Assert.That(KulEjderiAttackTable.For(KulEjderiAttackId.WingSweep).Interruptible, Is.False);

        var brain = AwakeBrain(seed: 21);
        brain.OnHpRatio(0.5f, KulEjderiDefaults.AwakenMs);
        var events = Run(brain, KulEjderiDefaults.AwakenMs, KulEjderiDefaults.AwakenMs + 600000);

        var wings = events
            .Where(e => e.Kind == KulEjderiEventKind.Struck && e.Attack == KulEjderiAttackId.WingSweep)
            .Select(e => e.WorldMs)
            .ToList();
        Assert.That(wings.Count, Is.GreaterThan(3));
        for (int i = 1; i < wings.Count; i++)
            Assert.That(wings[i] - wings[i - 1], Is.GreaterThanOrEqualTo(KulEjderiDefaults.WingSweepMinGapMs));
    }

    [Test]
    public void Same_seed_gives_same_event_sequence()
    {
        List<(KulEjderiEventKind, double)> Sequence(int seed) =>
            Run(AwakeBrain(seed), KulEjderiDefaults.AwakenMs, 120000).Select(e => (e.Kind, e.WorldMs)).ToList();

        Assert.That(Sequence(42), Is.EqualTo(Sequence(42)));
    }

    [Test]
    public void TelegraphProgress_runs_zero_to_one_over_the_spec_telegraph()
    {
        var brain = AwakeBrain();
        double t = RunUntil(brain, KulEjderiDefaults.AwakenMs, b => b.State == KulEjderiState.Telegraph);
        int telegraphMs = brain.CurrentAttack!.TelegraphMs;

        Assert.That(brain.TelegraphProgress01(t), Is.EqualTo(0f).Within(0.05f));
        Assert.That(brain.TelegraphProgress01(t + telegraphMs / 2.0), Is.EqualTo(0.5f).Within(0.05f));
    }
}
