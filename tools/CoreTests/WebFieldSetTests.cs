using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class WebFieldSetTests
{
    [Test]
    public void Capacity_DropsOldest()
    {
        var set = new WebFieldSet(2, 3f, 10_000, 0f);
        set.Add(10f, 0f, 0);
        set.Add(20f, 0f, 1);
        set.Add(30f, 0f, 2);
        Assert.That(set.Count, Is.EqualTo(2));
        Assert.That(set.Contains(10f, 0f, 2), Is.False);
        Assert.That(set.Contains(20f, 0f, 2), Is.True);
        Assert.That(set.Contains(30f, 0f, 2), Is.True);
    }

    [Test]
    public void Life_PrunesExpired()
    {
        var set = new WebFieldSet(3, 2f, 100, 0f);
        set.Add(0f, 0f, 0);
        Assert.That(set.Contains(0f, 0f, 50), Is.True);
        Assert.That(set.Contains(0f, 0f, 100), Is.False);
        Assert.That(set.Count, Is.EqualTo(0));
    }

    [Test]
    public void CenterPush_MovesInsideRingOutward()
    {
        var set = new WebFieldSet(3, 3f, 10_000, 4f);
        set.Add(2f, 0f, 0);
        Assert.That(set.Contains(4f, 0f, 0), Is.True);
        Assert.That(set.Contains(0.5f, 0f, 0), Is.False);
    }

    [Test]
    public void Contains_UsesRadius()
    {
        var set = new WebFieldSet(3, 2f, 10_000, 0f);
        set.Add(0f, 0f, 0);
        Assert.That(set.Contains(1.9f, 0f, 0), Is.True);
        Assert.That(set.Contains(2.1f, 0f, 0), Is.False);
    }
}
