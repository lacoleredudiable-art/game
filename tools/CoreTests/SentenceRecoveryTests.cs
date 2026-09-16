using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// T6.2 — erken kapanış (Commit) ve toparlanma kilidi (dovus-sistemi.md §5:
/// "Düz vuruş ve erken kapanış" + "Toparlanma girdi kilididir").
/// </summary>
[TestFixture]
public class SentenceRecoveryTests
{
    static double RecoveryMsFor(SentenceTuning tuning, int dots) =>
        tuning.StepForDots(dots).RecoverySec * 1000.0;

    [Test]
    public void Commit_WhileBuilding_PaysForCurrentLength_NotAbort()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);

        engine.OnDotTouched(5, 0);
        engine.OnDotTouched(1, 10);
        engine.Commit();

        Assert.That(engine.History, Has.Count.EqualTo(1));
        CompletedSentence done = engine.History[0];
        Assert.That(done.Phase, Is.EqualTo(SentencePhase.Resolved));
        Assert.That(done.PaidReward, Is.True, "merkez öder, dodge batırır (§5)");
        Assert.That(done.Closing!.Value.DotCount, Is.EqualTo(2));
        Assert.That(done.Closing!.Value.TotalEffect, Is.EqualTo(tuning.StepForDots(2).TotalEffect));
        Assert.That(done.Closing!.Value.Type, Is.EqualTo(Rune.Ates), "kapanışın türü son rün");
    }

    [Test]
    public void Commit_WhenIdleOrRecovering_DoesNothing()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);

        engine.Commit();
        Assert.That(engine.History, Is.Empty);
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Idle));

        engine.OnDotTouched(5, 0);
        engine.Commit();
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Recovering));

        // Kilit sürerken merkez tap ikinci bir ödeme üretmez; düz vuruşu girdi katmanı
        // OnDotTouched + Commit ile kurar (bir sonraki test).
        engine.Commit();
        Assert.That(engine.History, Has.Count.EqualTo(1));
    }

    [Test]
    public void EveryClosingPath_EntersRecovery_WithSection5Duration()
    {
        var tuning = new SentenceTuning();

        // 1) Erken kapanış (Commit)
        var byCommit = new SentenceEngine(tuning);
        byCommit.OnDotTouched(5, 0);
        byCommit.OnDotTouched(1, 10);
        byCommit.OnDotTouched(2, 20);
        byCommit.Commit();
        Assert.That(byCommit.State.Phase, Is.EqualTo(SentencePhase.Recovering));
        Assert.That(byCommit.State.RemainingRecoveryMs, Is.EqualTo(RecoveryMsFor(tuning, 3)));

        // 2) Dördüncü nokta
        var byFourth = new SentenceEngine(tuning);
        for (int i = 0; i < 4; i++)
            byFourth.OnDotTouched(i + 1, i * 10);
        Assert.That(byFourth.State.Phase, Is.EqualTo(SentencePhase.Recovering));
        Assert.That(byFourth.State.RemainingRecoveryMs, Is.EqualTo(RecoveryMsFor(tuning, 4)));

        // 3) Pencere zaman aşımı
        var byTimeout = new SentenceEngine(tuning);
        byTimeout.OnDotTouched(5, 0);
        byTimeout.Tick(tuning.CancelWindowForDots(1));
        Assert.That(byTimeout.State.Phase, Is.EqualTo(SentencePhase.Recovering));
        Assert.That(byTimeout.State.RemainingRecoveryMs, Is.EqualTo(RecoveryMsFor(tuning, 1)));
    }

    [Test]
    public void Recovery_MeltsInWorldTime_ThenIdle_WithNothingCuttingIt()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);
        double lockMs = RecoveryMsFor(tuning, 2);

        engine.OnDotTouched(5, 0);
        engine.OnDotTouched(1, 10);
        engine.Commit();

        // Kilidin yarısı: hâlâ kilitli, kalan süre eriyor
        engine.Tick(lockMs * 0.5);
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Recovering));
        Assert.That(engine.State.RemainingRecoveryMs, Is.EqualTo(lockMs * 0.5).Within(0.001));

        // Kilit dolunca kendiliğinden Idle; ödenmiş kapanış okunabilir kalır
        engine.Tick(lockMs * 0.5);
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Idle));
        Assert.That(engine.State.RemainingRecoveryMs, Is.Zero);
        Assert.That(engine.State.LastClosing, Is.Not.Null);
        Assert.That(engine.History, Has.Count.EqualTo(1), "kilit dolması yeni cümle başlatmaz");
    }

    [Test]
    public void NewVerb_DuringRecovery_CutsLock_AndStartsSentence()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);

        engine.OnDotTouched(5, 0);
        engine.Commit();
        Assert.That(engine.State.RemainingRecoveryMs, Is.EqualTo(RecoveryMsFor(tuning, 1)));

        engine.OnDotTouched(2, 50);

        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Building));
        Assert.That(engine.State.RemainingRecoveryMs, Is.Zero, "köşeye basmak kilidi keser (§5)");
        Assert.That(engine.State.Verb, Is.EqualTo(Rune.Su));
        Assert.That(engine.State.RemainingWindowMs, Is.EqualTo(tuning.CancelWindowForDots(1)));
    }

    /// <summary>Düz vuruş: girdi katmanı OnDotTouched + Commit'i aynı karede çağırır (§5).</summary>
    [Test]
    public void BasicStrike_DuringRecovery_CutsLock_AndPaysOneDot()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);
        const int strikeDot = 5;

        engine.OnDotTouched(1, 0);
        engine.OnDotTouched(2, 10);
        engine.OnDotTouched(5, 20);
        engine.Commit();
        Assert.That(engine.State.RemainingRecoveryMs, Is.EqualTo(RecoveryMsFor(tuning, 3)));

        engine.OnDotTouched(strikeDot, 30);
        engine.Commit();

        Assert.That(engine.History, Has.Count.EqualTo(2));
        ClosingHit strike = engine.History[1].Closing!.Value;
        Assert.That(strike.DotCount, Is.EqualTo(1));
        Assert.That(strike.TotalEffect, Is.EqualTo(tuning.StepForDots(1).TotalEffect));
        Assert.That(strike.Type, Is.EqualTo(Rune.Aydinlik));
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Recovering));
        Assert.That(
            engine.State.RemainingRecoveryMs,
            Is.EqualTo(RecoveryMsFor(tuning, 1)),
            "kesilen 0.38 sn yerine 1 noktanın 0.18 sn'si — ödül kesilen süredir (§12)");
    }

    [Test]
    public void Dodge_DuringRecovery_CutsLock_ButKeepsPaidClosing()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);

        engine.OnDotTouched(5, 0);
        engine.OnDotTouched(1, 10);
        engine.Commit();
        ClosingHit paid = engine.State.LastClosing!.Value;

        engine.Abort();

        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Idle));
        Assert.That(engine.State.RemainingRecoveryMs, Is.Zero, "toparlanmada dodge ücretsiz");
        Assert.That(engine.History, Has.Count.EqualTo(1), "iptal kaydı eklenmez");
        Assert.That(engine.History[0].Phase, Is.EqualTo(SentencePhase.Resolved));
        Assert.That(engine.State.LastClosing, Is.Not.Null);
        Assert.That(engine.State.LastClosing!.Value.TotalEffect, Is.EqualTo(paid.TotalEffect));
    }

    [Test]
    public void Dodge_WhileBuilding_StillSinksTheInvestment()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);

        engine.OnDotTouched(5, 0);
        engine.OnDotTouched(1, 10);
        engine.Abort();

        Assert.That(engine.History, Has.Count.EqualTo(1));
        Assert.That(engine.History[0].PaidReward, Is.False);
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Aborted));
        Assert.That(engine.State.RemainingRecoveryMs, Is.Zero, "ödenmemiş cümle kilit doğurmaz");
    }

    [Test]
    public void Dwell_DoesNotStackDuringRecovery()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);

        engine.OnDotTouched(5, 0);
        engine.Commit();
        engine.OnDwell(10);

        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Recovering));
        Assert.That(engine.State.Words[0].IntensityStacks, Is.Zero);
    }
}
