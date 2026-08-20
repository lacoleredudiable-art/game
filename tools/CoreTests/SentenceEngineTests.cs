using System.Collections.Generic;
using System.Linq;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class SentenceEngineTests
{
    SentenceEngine CreateEngine() => new SentenceEngine(new SentenceTuning());

    static int[] DotsOf(CompletedSentence s) => s.Words.Select(w => w.Dot).ToArray();

    // Kriter 1
    [Test]
    public void FiveThenOne_DiffersFrom_OneThenFive()
    {
        var a = CreateEngine();
        a.OnDotTouched(5, 0);
        a.OnDotTouched(1, 10);
        a.Tick(500);

        var b = CreateEngine();
        b.OnDotTouched(1, 0);
        b.OnDotTouched(5, 10);
        b.Tick(500);

        Assert.That(a.History, Has.Count.EqualTo(1));
        Assert.That(b.History, Has.Count.EqualTo(1));

        CompletedSentence sa = a.History[0];
        CompletedSentence sb = b.History[0];

        Assert.That(sa.Verb, Is.EqualTo(Rune.Sarsinti));
        Assert.That(sb.Verb, Is.EqualTo(Rune.Igne));
        Assert.That(DotsOf(sa), Is.EqualTo(new[] { 5, 1 }));
        Assert.That(DotsOf(sb), Is.EqualTo(new[] { 1, 5 }));
        Assert.That(sa.Verb, Is.Not.EqualTo(sb.Verb));
    }

    // Kriter 2
    [Test]
    public void TenDots_SplitsIntoThreeSentences()
    {
        var engine = CreateEngine();
        int[] sequence = { 1, 2, 3, 4, 5, 1, 2, 3, 4, 5 };
        double t = 0;
        foreach (int dot in sequence)
        {
            engine.OnDotTouched(dot, t);
            t += 50;
        }

        // (1,2,3,4) ve (5,1,2,3) çözülmüş; (4,5…) hâlâ kuruluyor
        Assert.That(engine.History, Has.Count.EqualTo(2));
        Assert.That(DotsOf(engine.History[0]), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        Assert.That(DotsOf(engine.History[1]), Is.EqualTo(new[] { 5, 1, 2, 3 }));

        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Building));
        Assert.That(engine.State.Words.Select(w => w.Dot).ToArray(), Is.EqualTo(new[] { 4, 5 }));
        Assert.That(engine.State.Verb, Is.EqualTo(Rune.Zehir));
    }

    // Kriter 3
    [Test]
    public void WindowExpiry_AutoResolvesWithClosing()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);

        engine.OnDotTouched(1, 0);
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Building));
        Assert.That(engine.State.RemainingWindowMs, Is.EqualTo(tuning.CancelWindowForDots(1)));

        engine.Tick(tuning.CancelWindowForDots(1));
        Assert.That(engine.State.Phase, Is.EqualTo(SentencePhase.Resolved));
        Assert.That(engine.History, Has.Count.EqualTo(1));
        Assert.That(engine.History[0].Closing, Is.Not.Null);
        Assert.That(engine.History[0].Closing!.Value.Type, Is.EqualTo(Rune.Igne));
        Assert.That(engine.History[0].Closing!.Value.TotalEffect, Is.EqualTo(tuning.StepForDots(1).TotalEffect));
    }

    // Kriter 4
    [Test]
    public void Abort_ProducesNoReward()
    {
        var engine = CreateEngine();
        engine.OnDotTouched(5, 0);
        engine.OnDotTouched(1, 10);
        engine.Abort();

        Assert.That(engine.History, Has.Count.EqualTo(1));
        CompletedSentence done = engine.History[0];
        Assert.That(done.Phase, Is.EqualTo(SentencePhase.Aborted));
        Assert.That(done.Closing, Is.Null);
        Assert.That(done.PaidReward, Is.False);
        Assert.That(engine.State.LastClosing, Is.Null);
    }

    // Kriter 7 (ödül tablosu — Abort'tan bağımsız)
    [Test]
    public void Rewards_MatchSection5Table()
    {
        var tuning = new SentenceTuning();

        for (int length = 1; length <= 4; length++)
        {
            var engine = new SentenceEngine(tuning);
            for (int i = 0; i < length; i++)
                engine.OnDotTouched(i + 1, i * 10);

            if (length < 4)
                engine.Tick(10_000);

            Assert.That(engine.History, Has.Count.EqualTo(1), $"length {length}");
            Assert.That(engine.History[0].Closing, Is.Not.Null);
            Assert.That(
                engine.History[0].Closing!.Value.TotalEffect,
                Is.EqualTo(tuning.StepForDots(length).TotalEffect));
            Assert.That(engine.History[0].Closing!.Value.DotCount, Is.EqualTo(length));
            Assert.That(
                engine.History[0].Closing!.Value.Type,
                Is.EqualTo((Rune)length));
        }
    }
}

[TestFixture]
public class PentagonLayoutTests
{
    // Kriter 5
    [Test]
    public void EachDot_HasTwoNeighborsAndTwoFar()
    {
        for (int dot = 1; dot <= 5; dot++)
        {
            PentagonLayout.GetNeighbors(dot, out int n0, out int n1);
            PentagonLayout.GetFarDots(dot, out int f0, out int f1);

            var neighbors = new HashSet<int> { n0, n1 };
            var far = new HashSet<int> { f0, f1 };

            Assert.That(neighbors, Has.Count.EqualTo(2), $"dot {dot} neighbors");
            Assert.That(far, Has.Count.EqualTo(2), $"dot {dot} far");
            Assert.That(neighbors.Overlaps(far), Is.False);
            Assert.That(neighbors.Contains(dot), Is.False);
            Assert.That(far.Contains(dot), Is.False);

            for (int other = 1; other <= 5; other++)
            {
                if (other == dot) continue;
                JumpKind kind = PentagonLayout.ClassifyJump(dot, other);
                if (neighbors.Contains(other))
                    Assert.That(kind, Is.EqualTo(JumpKind.Short));
                else
                    Assert.That(kind, Is.EqualTo(JumpKind.Long));
            }

            Assert.That(PentagonLayout.ClassifyJump(dot, dot), Is.EqualTo(JumpKind.Repeat));
        }
    }
}

[TestFixture]
public class DwellTests
{
    // Kriter 6
    [Test]
    public void Dwell_MaxTwoStacks_DoesNotSpendAdjectiveSlot()
    {
        var tuning = new SentenceTuning();
        var engine = new SentenceEngine(tuning);

        engine.OnDotTouched(1, 0);
        engine.OnDwell(tuning.DwellMs);
        engine.OnDwell(tuning.DwellMs * 2);
        engine.OnDwell(tuning.DwellMs * 3); // üçüncü yok sayılır

        Assert.That(engine.State.DotCount, Is.EqualTo(1), "dwell sıfat yuvası harcamaz");
        Assert.That(engine.State.Words[0].IntensityStacks, Is.EqualTo(tuning.DwellMaxStacks));
        Assert.That(engine.State.AdjectiveCount, Is.EqualTo(0));

        // Hâlâ 3 sıfat yuvası açık
        engine.OnDotTouched(2, 100);
        engine.OnDotTouched(3, 200);
        engine.OnDotTouched(4, 300);

        Assert.That(engine.History, Has.Count.EqualTo(1));
        Assert.That(engine.History[0].Words, Has.Count.EqualTo(4));
        Assert.That(engine.History[0].Words[0].IntensityStacks, Is.EqualTo(2));
        Assert.That(engine.History[0].Phase, Is.EqualTo(SentencePhase.Resolved));
    }
}
