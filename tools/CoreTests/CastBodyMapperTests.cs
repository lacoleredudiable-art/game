using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class CastBodyMapperTests
{
    [Test]
    public void VerbSeeds_MatchElementSistemiCores()
    {
        Assert.That(CastBodyMapper.FromVerb(Rune.Ates), Is.EqualTo(CastBodyFamily.Pierce));     // Ateş
        Assert.That(CastBodyMapper.FromVerb(Rune.Su), Is.EqualTo(CastBodyFamily.Channel));    // Su
        Assert.That(CastBodyMapper.FromVerb(Rune.Hava), Is.EqualTo(CastBodyFamily.Sweep));     // Hava
        Assert.That(CastBodyMapper.FromVerb(Rune.Toprak), Is.EqualTo(CastBodyFamily.Guard));     // Toprak
        Assert.That(CastBodyMapper.FromVerb(Rune.Aydinlik), Is.EqualTo(CastBodyFamily.Pierce)); // Aydınlık
        Assert.That(CastBodyMapper.FromVerb(Rune.Karanlik), Is.EqualTo(CastBodyFamily.Channel));  // Karanlık
    }

    [Test]
    public void HighSpreadHava_BecomesChannel()
    {
        var low = new EffectSilhouette(focus: 0.2f, pierce: 0f, spread: 0.4f, lift: 0f);
        var high = new EffectSilhouette(focus: 0.2f, pierce: 0f, spread: 0.9f, lift: 0f);
        Assert.That(CastBodyMapper.FromVerbAndSilhouette(Rune.Hava, low), Is.EqualTo(CastBodyFamily.Sweep));
        Assert.That(CastBodyMapper.FromVerbAndSilhouette(Rune.Hava, high), Is.EqualTo(CastBodyFamily.Channel));
    }

    [Test]
    public void IgneStaysPierce_EvenWithSpread()
    {
        var s = new EffectSilhouette(focus: 0.9f, pierce: 0.8f, spread: 0.95f, lift: 0f);
        Assert.That(CastBodyMapper.FromVerbAndSilhouette(Rune.Ates, s), Is.EqualTo(CastBodyFamily.Pierce));
    }
}
