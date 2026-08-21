using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class SilhouetteBuilderTests
{
    ManifestationTuning Tuning() => new();

    [Test]
    public void SarsintiAlone_IsRingNotLine()
    {
        var words = new[] { new SentenceWord(Rune.Sarsinti, JumpKind.None, 0) };
        EffectSilhouette s = SilhouetteBuilder.FromWords(words, Tuning());
        Assert.That(s.Focus, Is.LessThan(0.2f));
        Assert.That(s.Spread, Is.LessThan(0.2f));
    }

    [Test]
    public void SarsintiThenIgne_GathersTowardLine()
    {
        var ring = SilhouetteBuilder.FromWords(
            new[] { new SentenceWord(Rune.Sarsinti, JumpKind.None, 0) }, Tuning());
        var line = SilhouetteBuilder.FromWords(
            new[]
            {
                new SentenceWord(Rune.Sarsinti, JumpKind.None, 0),
                new SentenceWord(Rune.Igne, JumpKind.Long, 0)
            }, Tuning());

        Assert.That(line.Focus, Is.GreaterThan(ring.Focus + 0.3f));
        Assert.That(line.Pierce, Is.GreaterThan(ring.Pierce));
    }

    [Test]
    public void SarsintiIgneSuru_AddsSwarmAlongFocusedLine()
    {
        var s = SilhouetteBuilder.FromWords(
            new[]
            {
                new SentenceWord(Rune.Sarsinti, JumpKind.None, 0),
                new SentenceWord(Rune.Igne, JumpKind.Long, 0),
                new SentenceWord(Rune.Suru, JumpKind.Short, 0)
            }, Tuning());

        Assert.That(s.Focus, Is.GreaterThan(0.5f));
        Assert.That(s.Spread, Is.GreaterThan(0.5f));
    }

    [Test]
    public void LivingEffect_MorphsTowardNewAdjective()
    {
        var words = new List<SentenceWord>
        {
            new(Rune.Sarsinti, JumpKind.None, 0)
        };
        var effect = new LivingEffect(Rune.Sarsinti, 0, 0, 0, 1, words, Tuning());
        float focus0 = effect.Current.Focus;

        words.Add(new SentenceWord(Rune.Igne, JumpKind.Long, 0));
        effect.SetWords(words);

        // Birkaç tick: hedefe doğru yükselir, anında sıçramaz
        for (int i = 0; i < 8; i++)
            effect.Tick(0.05f);

        Assert.That(effect.Current.Focus, Is.GreaterThan(focus0 + 0.15f));
        Assert.That(effect.Current.Focus, Is.LessThan(effect.Target.Focus));
        Assert.That(effect.Travel, Is.GreaterThan(0f));
    }

    [Test]
    public void Abort_PreventsClosingBang()
    {
        var words = new[] { new SentenceWord(Rune.Igne, JumpKind.None, 0) };
        var effect = new LivingEffect(Rune.Igne, 0, 0, 0, 1, words, Tuning());
        effect.Abort();
        effect.FireClosingBang();
        Assert.That(effect.PaidClosing, Is.False);
        Assert.That(effect.Phase, Is.EqualTo(LivingEffectPhase.Fading));
    }

    [Test]
    public void ArmClosing_ThenBang_PaysReward()
    {
        var words = new[] { new SentenceWord(Rune.Sarsinti, JumpKind.None, 0) };
        var effect = new LivingEffect(Rune.Sarsinti, 0, 0, 0, 1, words, Tuning());
        effect.ArmClosing(new ClosingHit(Rune.Sarsinti, 1f, 1));
        Assert.That(effect.Phase, Is.EqualTo(LivingEffectPhase.AwaitingClosing));
        effect.FireClosingBang();
        Assert.That(effect.PaidClosing, Is.True);
        Assert.That(effect.Phase, Is.EqualTo(LivingEffectPhase.Banging));
    }
}
