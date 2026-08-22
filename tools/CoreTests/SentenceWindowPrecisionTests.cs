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
/// §7: yavaş çekim tavanı yükseltmez, tavana ulaşmanı sağlar — ve kuru "bir mükemmel dodge
/// ≈ iki ekstra nokta".
///
/// Bu fixture iki kez yanlış kurulmuştu: T8'de `holdMs: 10_000, rampMs: 0` ile (yani spec'in
/// sayıları değil sonsuz bir yavaş çekim ölçülüyordu), T8.1'de gerçek varsayılanlarla ama o
/// varsayılanlar ödülü hiç vermediği için "vermiyor" diye sabitlenmişti. T8.2'de süre §7'nin
/// kurundan geriye çözüldü (hold 900, rampUp 600); testler artık ödülün KENDİSİNİ koruyor.
/// Hepsi `SlowmoTuning` varsayılanlarını okur — sayı burada tekrar edilmez.
/// </summary>
[TestFixture]
public class SlowmoSentenceFitTests
{
    static readonly int[] Dots = { 5, 1, 2, 5 };

    // Yavaş çekim yokken 3 kelime çıkan tempo; ödül burada 4. noktayı açar.
    const double BriskGapMs = 350;

    // Yavaş çekim yokken 2 kelime çıkan tempo; §7'nin "iki ekstra nokta" kuru burada ölçülür.
    const double CalmGapMs = 400;

    // Ödülün üst sınırı: burada da 4 çıksaydı yavaş çekim otomatikleşir, beceri bandı silinir.
    const double SlowGapMs = 450;

    [TestCase(BriskGapMs, 3)]
    [TestCase(CalmGapMs, 2)]
    [TestCase(SlowGapMs, 1)]
    public void Realtime_IsTheBaseline(double gapMs, int expectedWords)
    {
        Assert.That(PlayDots(gapMs, slowmo: false), Is.EqualTo(expectedWords));
    }

    [TestCase(BriskGapMs, 4)]
    [TestCase(CalmGapMs, 4)]
    public void Slowmo_ReachesTheFourDotCeiling(double gapMs, int expectedWords)
    {
        Assert.That(
            PlayDots(gapMs, slowmo: true),
            Is.EqualTo(expectedWords),
            "§7 ödülü: yavaş çekimde tavana ulaşılabilmeli");
    }

    [TestCase(CalmGapMs)]
    [TestCase(SlowGapMs)]
    public void Slowmo_BuysTwoExtraDots(double gapMs)
    {
        int plain = PlayDots(gapMs, slowmo: false);
        int slowed = PlayDots(gapMs, slowmo: true);

        Assert.That(slowed - plain, Is.EqualTo(2), "§7 kuru: bir mükemmel dodge ≈ iki ekstra nokta");
    }

    [Test]
    public void Slowmo_DoesNotSaturate_AtTheSlowestCadence()
    {
        Assert.That(
            PlayDots(SlowGapMs, slowmo: true),
            Is.LessThan(4),
            "her tempoda 4 çıkarsa ödül otomatikleşir; süre yukarıdan da sınırlı olmalı");
    }

    [Test]
    public void Slowmo_DoesNotRaiseTheCeiling()
    {
        var tuning = new SlowmoTuning();

        Assert.That(tuning.SlowmoBonusDots, Is.Zero, "§7: tavan yükselmez, yalnızca ulaşılır");
        Assert.That(PlayDots(200, slowmo: true), Is.EqualTo(4), "tavan hâlâ 4 nokta");
    }

    static int PlayDots(double gapMs, bool slowmo)
    {
        var engine = new SentenceEngine();
        var time = new TimeDirector();

        if (slowmo)
        {
            var tuning = new SlowmoTuning();
            time.TriggerSlowmo(tuning.Factor, tuning.RampDownMs, tuning.HoldMs, tuning.RampUpMs);
        }

        for (int i = 0; i < Dots.Length; i++)
        {
            if (i > 0)
                engine.Tick(time.Tick(gapMs));

            engine.OnDotTouched(Dots[i], time.WorldTimeMs);
        }

        // Yavaş tempoda ilk cümle erken kapanır ve kalan noktalar YENİ cümleler açar; ölçtüğümüz
        // şey "bu dört dokunuşla tek bir cümlede kaç kelime tutabildim".
        return engine.History.Count > 0
            ? engine.History[0].Words.Count
            : engine.State.Words.Count;
    }
}
