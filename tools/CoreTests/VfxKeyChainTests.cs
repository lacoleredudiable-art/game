using Dovus.Core.Presentation;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class VfxKeyChainTests
{
    [Test]
    public void FullKey_FallsBackToElementVerb_ThenVerb()
    {
        Assert.That(VfxKeyChain.Expand("VFX_Ateş_3_7"),
            Is.EqualTo(new[] { "VFX_Ateş_3_7", "VFX_Ateş_3", "VFX_3" }));
    }

    [Test]
    public void NonGrammarKey_IsItsOwnChain()
    {
        Assert.That(VfxKeyChain.Expand("FX_HitSpark"), Is.EqualTo(new[] { "FX_HitSpark" }));
        Assert.That(VfxKeyChain.Expand("VFX_Ateş_3"), Is.EqualTo(new[] { "VFX_Ateş_3" }));
    }

    [Test]
    public void Empty_IsEmpty()
    {
        Assert.That(VfxKeyChain.Expand(""), Is.Empty);
        Assert.That(VfxKeyChain.Expand(null!), Is.Empty);
    }
}
