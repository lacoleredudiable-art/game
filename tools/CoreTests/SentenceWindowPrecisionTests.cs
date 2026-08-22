using Dovus.Core.Grammar;
using Dovus.Core.Time;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// T8 — OnDotTouched/OnDwell dünya saatini yutmuyordu; pencere yalnızca Tick ile eriyordu
/// (~16 ms kare yuvarlaması). Kaç noktanın sığdığı bu hassasiyete bağlı.
/// </summary>
[TestFixture]
public class SentenceWindowPrecisionTests
{
    [Test]
    public void OnDotTouched_ConsumesElapsedWorldTime_WithoutTick()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);
        int window = tuning.CancelWindowForDots(1);

        engine.OnDotTouched(1, 0);
        engine.OnDotTouched(2, window + 1);

        Assert.That(engine.History, Has.Count.EqualTo(1), "pencere Tick olmadan da dolmalı");
        Assert.That(engine.History[0].Words, Has.Count.EqualTo(1));
        Assert.That(engine.History[0].Closing, Is.Not.Null);
        Assert.That(engine.State.Verb, Is.EqualTo(Rune.Suru), "geç dokunuş yeni fiil");
        Assert.That(engine.State.DotCount, Is.EqualTo(1));
    }

    [Test]
    public void OnDotTouched_InsideWindow_StillExtendsSentence()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);
        int window = tuning.CancelWindowForDots(1);

        engine.OnDotTouched(1, 0);
        engine.OnDotTouched(2, window - 1);

        Assert.That(engine.History, Is.Empty);
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Building));
        Assert.That(engine.State.DotCount, Is.EqualTo(2));
        Assert.That(engine.State.ArmedWindowMs, Is.EqualTo(tuning.CancelWindowForDots(2)));
    }

    [Test]
    public void OnDwell_CatchesUpThenFreezes_WithoutTick()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);
        int window = tuning.CancelWindowForDots(1);

        engine.OnDotTouched(1, 0);
        engine.OnDwell(tuning.DwellMs);

        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Building));
        Assert.That(engine.State.Words[0].IntensityStacks, Is.EqualTo(1));
        Assert.That(engine.State.RemainingWindowMs, Is.EqualTo(window), "220 ms yendi, 220 ms iade");
    }

    [Test]
    public void Tick_AfterCatchUp_DoesNotDoubleCount()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);
        int window = tuning.CancelWindowForDots(1);

        engine.OnDotTouched(1, 0);
        engine.OnDwell(100);
        engine.Tick(100);

        // CatchUp 100 ms yedi ve dondurdu (iade 220, tavan 420 → 420).
        // Tick(100) aynı 100 ms'yi tekrar yememeli — aksi halde 320 kalırdı.
        Assert.That(engine.State.RemainingWindowMs, Is.EqualTo(window - 100).Within(0.01));
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Building));
    }
}

/// <summary>
/// §7: yavaş çekim tavanı yükseltmez, tavana ulaşmanı sağlar.
///
/// T8.1 — bu fixture eskiden `holdMs: 10_000, rampMs: 0` ile ölçüyordu, yani ölçtüğü şey
/// spec'in sayıları değil sonsuz bir yavaş çekimdi. Artık `SlowmoTuning` varsayılanları
/// (0.22× / 55 / 190 / 420) kullanılıyor ve testler o sayıların GERÇEKTE ne verdiğini
/// sabitliyor: yavaş çekim pencere erimesini gözle görülür yavaşlatıyor ama 350 ms'lik
/// gerçek boşluklarla 4. noktayı sığdırmaya yetmiyor. "4 nokta" kabul kriteri açık —
/// bkz. docs/durum.md T8.1 kararı (`SlowmoBonusDots` hâlâ 0 ve uygulanmamış).
/// </summary>
[TestFixture]
public class SlowmoSentenceFitTests
{
    const double GapRealMs = 350;
    static readonly int[] Dots = { 5, 1, 2, 5 };

    [Test]
    public void FourDots_DoNotFit_AtRealtimeWith350msGaps()
    {
        var engine = new SentenceEngine();

        PlayDots(engine, new TimeDirector(), triggerSlowmo: false);

        Assert.That(engine.History, Has.Count.EqualTo(1));
        Assert.That(engine.History[0].Words, Has.Count.EqualTo(3), "3. pencere 300 ms, 350 ms dünya yer");
        Assert.That(engine.State.DotCount, Is.EqualTo(1), "4. nokta yeni cümle");
    }

    [Test]
    public void SpecDefaults_SlowTheWindowDrain_ButDoNotBuyAFourthDot()
    {
        var engine = new SentenceEngine();

        PlayDots(engine, new TimeDirector(), triggerSlowmo: true);

        Assert.That(
            engine.History[0].Words,
            Has.Count.EqualTo(3),
            "spec sayılarıyla yavaş çekim ~330 ms dünya kazandırıyor, bir nokta 350 ms istiyor");
    }

    [Test]
    public void SpecDefaults_BuyWorldTime_OnTheFirstGap()
    {
        var tuning = new SlowmoTuning();

        var realtime = new TimeDirector();
        double plain = realtime.Tick(GapRealMs);

        var slowed = new TimeDirector();
        slowed.TriggerSlowmo(tuning.Factor, tuning.RampDownMs, tuning.HoldMs, tuning.RampUpMs);
        double scaled = slowed.Tick(GapRealMs);

        Assert.That(scaled, Is.LessThan(plain * 0.5), "ilk boşluk en az yarıya inmeli");
        Assert.That(scaled, Is.GreaterThan(0d));
    }

    static void PlayDots(SentenceEngine engine, TimeDirector time, bool triggerSlowmo)
    {
        if (triggerSlowmo)
        {
            var tuning = new SlowmoTuning();
            time.TriggerSlowmo(tuning.Factor, tuning.RampDownMs, tuning.HoldMs, tuning.RampUpMs);
        }

        for (int i = 0; i < Dots.Length; i++)
        {
            if (i > 0)
            {
                double worldDt = time.Tick(GapRealMs);
                engine.Tick(worldDt);
            }

            engine.OnDotTouched(Dots[i], time.WorldTimeMs);
        }
    }
}
