using Dovus.Core.Combat;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// docs/element-sistemi.json global_rules.cooldown_rules — GCD 0.3s, max_concurrent_casts 1.
/// </summary>
[TestFixture]
public class CooldownTrackerTests
{
    [Test]
    public void Defaults_MatchJson_CooldownRules()
    {
        var cd = new CooldownTracker();
        Assert.That(cd.GlobalCooldownSec, Is.EqualTo(0.3f));
        Assert.That(cd.MaxConcurrentCasts, Is.EqualTo(1));
        Assert.That(cd.ActiveCasts, Is.EqualTo(0));
    }

    [Test]
    public void TryStart_SetsVerbAndGlobalCooldown()
    {
        var cd = new CooldownTracker();
        Assert.That(cd.TryStart("kor", verbCooldownSec: 3f, worldMs: 0), Is.True);
        Assert.That(cd.ActiveCasts, Is.EqualTo(1));
        Assert.That(cd.VerbRemainingSec("kor", 0), Is.EqualTo(3f).Within(0.0001f));
        Assert.That(cd.GlobalRemainingSec(0), Is.EqualTo(0.3f).Within(0.0001f));
        Assert.That(cd.VerbRemainingSec("kor", 3000), Is.EqualTo(0f));
    }

    [Test]
    public void GlobalCooldown_BlocksAnyVerb()
    {
        var cd = new CooldownTracker();
        Assert.That(cd.TryStart("a", 1f, 0), Is.True);
        cd.CompleteCast();

        Assert.That(cd.CanStart("b", 200), Is.False, "GCD 0.3s henüz bitmedi");
        Assert.That(cd.TryStart("b", 1f, 200), Is.False);

        Assert.That(cd.CanStart("b", 300), Is.True);
        Assert.That(cd.TryStart("b", 1f, 300), Is.True);
    }

    [Test]
    public void VerbCooldown_BlocksSameVerb_AllowsOtherAfterGcd()
    {
        var cd = new CooldownTracker();
        Assert.That(cd.TryStart("kor", 4f, 0), Is.True);
        cd.CompleteCast();

        Assert.That(cd.TryStart("kor", 4f, 300), Is.False, "aynı fiil soğumada");
        Assert.That(cd.TryStart("dalga", 2f, 300), Is.True, "başka fiil GCD sonrası serbest");
    }

    [Test]
    public void MaxConcurrentCasts_BlocksUntilComplete()
    {
        var cd = new CooldownTracker(globalCooldownSec: 0f, maxConcurrentCasts: 1);
        Assert.That(cd.TryStart("a", 0f, 0), Is.True);
        Assert.That(cd.TryStart("b", 0f, 0), Is.False, "max_concurrent_casts=1");

        cd.CompleteCast();
        Assert.That(cd.TryStart("b", 0f, 0), Is.True);
    }

    [Test]
    public void MaxConcurrentCasts_TwoSlots()
    {
        var cd = new CooldownTracker(globalCooldownSec: 0f, maxConcurrentCasts: 2);
        Assert.That(cd.TryStart("a", 0f, 0), Is.True);
        Assert.That(cd.TryStart("b", 0f, 0), Is.True);
        Assert.That(cd.TryStart("c", 0f, 0), Is.False);
        Assert.That(cd.ActiveCasts, Is.EqualTo(2));

        cd.CompleteCast();
        Assert.That(cd.TryStart("c", 0f, 0), Is.True);
    }

    [Test]
    public void TryStart_NullOrEmpty_Rejected()
    {
        var cd = new CooldownTracker();
        Assert.That(cd.CanStart("", 0), Is.False);
        Assert.That(cd.TryStart("", 1f, 0), Is.False);
        Assert.That(cd.TryStart(null!, 1f, 0), Is.False);
    }
}
