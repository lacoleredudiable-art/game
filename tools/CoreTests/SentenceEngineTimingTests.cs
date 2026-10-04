using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// PLAN 2B.21 — kombo pencere / MaxSentenceDots ölçümü (bugünkü davranış; 2C düzeltmesi öncesi kilidi).
/// </summary>
[TestFixture]
public class SentenceEngineTimingTests
{
    static SentenceTuning TuningWithMaxDots(int maxDots)
    {
        var t = new SentenceTuning();
        t.MaxSentenceDots = maxDots;
        return t;
    }

    [Test]
    public void MaxSentenceDots2_SecondDotResolvesImmediately_NoWindowWait()
    {
        var tuning = TuningWithMaxDots(2);
        var engine = new SentenceEngine(tuning);

        engine.OnDotTouched(1, 0);
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Building));
        Assert.That(engine.History, Is.Empty);

        engine.OnDotTouched(2, 0);

        Assert.That(engine.History, Has.Count.EqualTo(1), "2. rün aynı dünya zamanda kapanış üretmeli");
        Assert.That(engine.History[0].Closing, Is.Not.Null);
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Recovering));
        Assert.That(engine.State.RemainingWindowMs, Is.EqualTo(0));
    }

    [Test]
    public void MaxSentenceDots4_AfterSecondDot_WindowIs360Ms_ThenResolvesOnTick()
    {
        var tuning = TuningWithMaxDots(4);
        var engine = new SentenceEngine(tuning);

        engine.OnDotTouched(1, 0);
        engine.OnDotTouched(2, 0);

        Assert.That(engine.History, Is.Empty, "kapasite 4: 2. rün sonrası pencere beklenir");
        Assert.That(engine.State.RemainingWindowMs, Is.EqualTo(tuning.CancelWindowForDots(2)));
        Assert.That(tuning.CancelWindowForDots(2), Is.EqualTo(360));

        engine.Tick(359);
        Assert.That(engine.History, Is.Empty);

        engine.Tick(1);
        Assert.That(engine.History, Has.Count.EqualTo(1));
        Assert.That(engine.History[0].Closing!.Value.DotCount, Is.EqualTo(2));
    }

    [Test]
    public void DwellStack_RestoresCancelWindow_AfterPartialElapsed()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);
        int windowAfterVerb = tuning.CancelWindowForDots(1);

        engine.OnDotTouched(1, 0);
        engine.Tick(100);
        Assert.That(engine.State.RemainingWindowMs, Is.EqualTo(windowAfterVerb - 100).Within(0.01));

        engine.OnDwell(100);
        Assert.That(engine.State.RemainingWindowMs, Is.EqualTo(windowAfterVerb).Within(0.01),
            "dwell yığını pencereyi dondurur (CancelWindowMs[0]=420)");
    }

    [Test]
    public void AbortWhileBuilding_OneDot_NoClosingReward()
    {
        var engine = new SentenceEngine(new SentenceTuning());
        engine.OnDotTouched(1, 0);
        engine.Abort();

        Assert.That(engine.History, Has.Count.EqualTo(1));
        Assert.That(engine.History[0].Phase, Is.EqualTo(SentencePhase.Aborted));
        Assert.That(engine.History[0].Closing, Is.Null);
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Aborted));
    }

    [Test]
    public void InvalidDot_IsIgnored_PhaseStaysIdle()
    {
        var engine = new SentenceEngine(new SentenceTuning());
        engine.OnDotTouched(0, 0);
        engine.OnDotTouched(99, 0);

        Assert.That(engine.History, Is.Empty);
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Idle));
        Assert.That(engine.State.DotCount, Is.Zero);
    }

    [Test]
    public void Recovering_NewDot_StartsBuilding_HistoryKeepsResolvedClosing()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);

        engine.OnDotTouched(1, 0);
        engine.Tick(tuning.CancelWindowForDots(1));
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Recovering));
        Assert.That(engine.State.LastClosing, Is.Not.Null);

        engine.OnDotTouched(2, tuning.CancelWindowForDots(1) + 1);
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Building));
        Assert.That(engine.State.LastClosing, Is.Null, "StartVerb yeni fiilde LastClosing sıfırlar (ölçüm)");
        Assert.That(engine.History, Has.Count.EqualTo(1));
        Assert.That(engine.History[0].Closing, Is.Not.Null);
    }
}
