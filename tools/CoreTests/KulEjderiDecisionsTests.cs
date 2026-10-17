using System;
using System.Collections.Generic;
using System.Linq;
using Dovus.Core.Boss;
using Dovus.Core.Team;
using NUnit.Framework;

namespace CoreTests;

/// <summary>18-kul-ejderi.md sürüm 1.1, "Verilen kararlar (17 Ekim, [K])": durum makinesi, görev zamanlayıcısı, ceza.</summary>
[TestFixture]
public sealed class KulEjderiDecisionsTests
{
    const double Step = 20;

    static KulEjderiBrain NewBrain(int seed = 1) => new KulEjderiBrain(new Random(seed)) { Enabled = true };

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

    /// <summary>Uyanmış, faz 3'te bekleyen boss; dönen değer faz 3'ün ilk Idle anı.</summary>
    static double Phase3Brain(KulEjderiBrain brain)
    {
        brain.Wake(0);
        brain.Tick(KulEjderiDefaults.AwakenMs);
        brain.OnHpRatio(0.2f, KulEjderiDefaults.AwakenMs);
        double t = RunUntil(brain, KulEjderiDefaults.AwakenMs,
            b => b.Phase == KulEjderiPhase.Phase3 && b.State == KulEjderiState.Idle);
        brain.DrainEvents(new List<KulEjderiEvent>());
        return t;
    }

    // ---- KE-5.3 uyanış: ilk giriş 4 s, "Tekrar" 2 s; tetik Z −2; oyuncular hasar almaz

    [Test]
    public void KE_5_3_Retry_awakening_takes_2s()
    {
        var brain = NewBrain();
        Assert.That(brain.Wake(1000, isRetry: true), Is.True);

        brain.Tick(1000 + KulEjderiDefaults.AwakenRetryMs - 1);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Awakening));
        brain.Tick(1000 + KulEjderiDefaults.AwakenRetryMs);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Idle));
    }

    [Test]
    public void KE_5_3_First_entry_awakening_still_takes_4s()
    {
        Assert.That(KulEjderiDefaults.AwakenMs, Is.EqualTo(4000));
        Assert.That(KulEjderiDefaults.AwakenRetryMs, Is.EqualTo(2000));
    }

    [Test]
    public void KE_5_3_Players_take_no_damage_during_awakening()
    {
        var brain = NewBrain();
        Assert.That(brain.PlayersProtected, Is.False);
        brain.Wake(0);
        Assert.That(brain.PlayersProtected, Is.True);
        brain.Tick(KulEjderiDefaults.AwakenMs);
        Assert.That(brain.PlayersProtected, Is.False);
    }

    [TestCase(-2.01f, false)]
    [TestCase(-2f, false)]
    [TestCase(-1.99f, true)]
    [TestCase(5f, true)]
    public void KE_5_2_Wake_line_is_z_minus_2(float playerZ, bool crosses)
    {
        Assert.That(KulEjderiBrain.CrossesWakeLine(playerZ), Is.EqualTo(crosses));
    }

    // ---- KE-7.8 Kor Mührü ödülü: 12 s çökme

    [Test]
    public void KE_7_8_Collapse_lasts_12s_keeps_boss_open_then_attacks_resume()
    {
        var brain = NewBrain();
        double t = Phase3Brain(brain);

        Assert.That(brain.BeginCollapse(t), Is.True);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Collapsed));
        Assert.That(brain.IsOpen, Is.True);
        Assert.That(brain.AcceptsDamage, Is.True);

        var events = Run(brain, t, t + KulEjderiDefaults.HeartSealCollapseMs - Step);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Collapsed));
        Assert.That(events.Any(e => e.Kind == KulEjderiEventKind.TelegraphStarted), Is.False, "çökmede saldırı yok");

        brain.Tick(t + KulEjderiDefaults.HeartSealCollapseMs);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Idle));
        var end = new List<KulEjderiEvent>();
        brain.DrainEvents(end);
        Assert.That(end.Count(e => e.Kind == KulEjderiEventKind.CollapseEnded), Is.EqualTo(1));
    }

    [Test]
    public void KE_7_8_Collapse_is_only_the_phase3_reward()
    {
        var brain = NewBrain();
        brain.Wake(0);
        brain.Tick(KulEjderiDefaults.AwakenMs);
        Assert.That(brain.BeginCollapse(KulEjderiDefaults.AwakenMs), Is.False);
        Assert.That(brain.State, Is.EqualTo(KulEjderiState.Idle));
    }

    [Test]
    public void KE_7_8_Open_question_poise_stagger_during_collapse_is_not_resolved_silently()
    {
        var brain = NewBrain();
        double t = Phase3Brain(brain);
        brain.BeginCollapse(t);

        Assert.Throws<InvalidOperationException>(() => brain.BeginStagger(t + 1000, KulEjderiDefaults.PoiseStaggerMs));
    }

    [Test]
    public void KE_7_8_Sword_and_bow_get_the_same_collapse_reward()
    {
        // Çökmede baş yere iner (Poz_Sendeleme): Kılıç gövdeye doğrudan değer, çarpan iki silahta aynı.
        KulEjderiPose pose = KulEjderiHurtboxes.PoseFor(KulEjderiPhase.Phase3, slumped: true);
        var p = new KulEjderiPoint(0f, 0f, 6f);
        Assert.That(KulEjderiHurtboxes.Probe(KulEjderiHitZone.Head, pose, p, 6f).ViaColumn, Is.False);
        Assert.That(KulEjderiHurtboxes.Probe(KulEjderiHitZone.Head, pose, p, 14.1f).ViaColumn, Is.False);
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Head, KulEjderiPhase.Phase3, false, true).Multiplier,
            Is.EqualTo(KulEjderiDefaults.CollapseDamageMultiplier));
    }

    // ---- KE-7.6 faz 3 sabit 40 s döngü

    [Test]
    public void KE_7_6_Phase3_crater_heart_runs_on_fixed_40s_grid()
    {
        var brain = NewBrain(seed: 5);
        double t = Phase3Brain(brain);
        double phaseStart = brain.PhaseStartMs;
        var events = Run(brain, t, phaseStart + 210000);

        var hearts = events
            .Where(e => e.Kind == KulEjderiEventKind.TelegraphStarted && e.Attack == KulEjderiAttackId.CraterHeart)
            .Select(e => e.WorldMs)
            .ToList();
        double firstSlot = phaseStart + KulEjderiDefaults.Phase3CycleStartMs + KulEjderiDefaults.Phase3CraterHeartAtMs;
        Assert.That(hearts.Count, Is.EqualTo(5));
        for (int k = 0; k < hearts.Count; k++)
        {
            double slot = firstSlot + k * KulEjderiDefaults.Phase3CycleMs;
            Assert.That(hearts[k], Is.GreaterThanOrEqualTo(slot), $"{k}. Kraterin Kalbi yuvasından önce");
            // yuva anında süren saldırı ve D-7 dodge dolumu bekletebilir; bir sonraki yuva kaymaz
            Assert.That(hearts[k], Is.LessThan(slot + 12000), $"{k}. Kraterin Kalbi yuvasından çok sonra");
        }
    }

    [Test]
    public void KE_7_6_Heart_seal_stays_on_grid_when_delayed()
    {
        var s = TeamTaskSchedule.KulEjderi();
        for (int i = 0; i < 4; i++) // can eşikli dört görev önce tüketilir
            Assert.That(s.Poll(0.20f, -1, false), Is.Not.Null);
        Assert.That(s.Poll(0.20f, 5.0, false), Is.EqualTo(TeamTaskKind.HeartSeal));
        Assert.That(s.Poll(0.20f, 45.0, true), Is.Null);                         // meşgul
        Assert.That(s.Poll(0.20f, 50.0, false), Is.EqualTo(TeamTaskKind.HeartSeal)); // geç başladı
        Assert.That(s.Poll(0.20f, 84.9, false), Is.Null);
        Assert.That(s.Poll(0.20f, 85.0, false), Is.EqualTo(TeamTaskKind.HeartSeal), "döngü 5 + 40k'da kalır");
    }

    [Test]
    public void KE_7_6_Heart_seal_cycle_matches_boss_cycle()
    {
        Assert.That(TeamTaskDefaults.HeartSealFirstAtPhaseSec * 1000.0, Is.EqualTo(KulEjderiDefaults.Phase3CycleStartMs));
        Assert.That(TeamTaskDefaults.HeartSealRepeatSec * 1000.0, Is.EqualTo(KulEjderiDefaults.Phase3CycleMs));
    }

    // ---- KE-6.13 Kül Yağmuru'nda takım cezası yok

    [TestCase(TeamDifficulty.Normal)]
    [TestCase(TeamDifficulty.Hard)]
    public void KE_6_13_Ash_rain_has_no_team_penalty_but_exposed_still_die(TeamDifficulty difficulty)
    {
        TeamTaskPenaltyResult p = TeamTaskPenalty.Resolve(TeamTaskKind.AshRain, difficulty);

        Assert.That(TeamTaskPenalty.TeamDamage(1000, p), Is.EqualTo(0));
        Assert.That(p.BossHpResetRatio, Is.EqualTo(0f));
        Assert.That(p.ExposedDie, Is.True);
        Assert.That(p.Wipe, Is.False);
    }

    [Test]
    public void KE_6_13_Hell_still_wipes_on_ash_rain()
    {
        Assert.That(TeamTaskPenalty.Resolve(TeamTaskKind.AshRain, TeamDifficulty.Hell).Wipe, Is.True);
    }

    // ---- KE-6.6 Kanat Savuruşu beklemedeyken ağırlık öbür ikisine eşit bölünür

    [Test]
    public void KE_6_6_Gated_wing_sweep_weight_splits_equally_to_other_two()
    {
        Assert.That(KulEjderiDefaults.ClawSlamHighWeight, Is.EqualTo(KulEjderiDefaults.SkyEmberBreathWeight));

        int claw = 0, sky = 0;
        for (int seed = 1; seed <= 400; seed++)
        {
            var brain = NewBrain(seed);
            brain.Wake(0);
            brain.Tick(KulEjderiDefaults.AwakenMs);
            brain.OnHpRatio(0.5f, KulEjderiDefaults.AwakenMs);
            var events = Run(brain, KulEjderiDefaults.AwakenMs, 120000);
            int wing = events.FindIndex(e => e.Kind == KulEjderiEventKind.Struck && e.Attack == KulEjderiAttackId.WingSweep);
            if (wing < 0)
                continue;
            KulEjderiEvent next = events.Skip(wing + 1).FirstOrDefault(e => e.Kind == KulEjderiEventKind.TelegraphStarted);
            if (next.Attack == KulEjderiAttackId.ClawSlamHigh) claw++;
            else if (next.Attack == KulEjderiAttackId.SkyEmberBreath) sky++;
            else if (next.Attack == KulEjderiAttackId.WingSweep) Assert.Fail("Kanat Savuruşu 20 s dolmadan geldi");
        }

        int total = claw + sky;
        Assert.That(total, Is.GreaterThan(200));
        Assert.That(claw / (double)total, Is.InRange(0.42, 0.58));
    }
}
