using Dovus.Core.Combat;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// docs/element-sistemi.json global_rules.resource_system — max_mana 100, regen 8/s, delay 1.5s.
/// </summary>
[TestFixture]
public class ResourceTrackerTests
{
    [Test]
    public void Defaults_MatchJson_ResourceSystem()
    {
        var r = new ResourceTracker();
        Assert.That(r.MaxMana, Is.EqualTo(100f));
        Assert.That(r.Mana, Is.EqualTo(100f));
        Assert.That(r.RegenPerSec, Is.EqualTo(8f));
        Assert.That(r.RegenDelayAfterCastSec, Is.EqualTo(1.5f));
    }

    [Test]
    public void CanAfford_And_Spend_DeductsMana()
    {
        var r = new ResourceTracker();
        Assert.That(r.CanAfford(10f), Is.True);
        Assert.That(r.Spend(10f), Is.True);
        Assert.That(r.Mana, Is.EqualTo(90f));
        Assert.That(r.CanAfford(91f), Is.False);
        Assert.That(r.Spend(91f), Is.False);
        Assert.That(r.Mana, Is.EqualTo(90f), "reddedilen Spend mana değiştirmez");
    }

    [Test]
    public void Spend_ZeroOrNegative_DoesNotStartDelay()
    {
        var r = new ResourceTracker();
        Assert.That(r.Spend(0f), Is.True);
        Assert.That(r.Spend(-5f), Is.True);
        Assert.That(r.Mana, Is.EqualTo(100f));
        Assert.That(r.RegenDelayLeftSec, Is.EqualTo(0f));
    }

    [Test]
    public void Tick_WaitsRegenDelay_ThenRegensAtJsonRate()
    {
        var r = new ResourceTracker();
        Assert.That(r.Spend(40f), Is.True);
        Assert.That(r.Mana, Is.EqualTo(60f));

        r.Tick(1.5f);
        Assert.That(r.Mana, Is.EqualTo(60f), "delay bitene kadar yenilenme yok");
        Assert.That(r.RegenDelayLeftSec, Is.EqualTo(0f).Within(0.0001f));

        r.Tick(1f);
        Assert.That(r.Mana, Is.EqualTo(68f).Within(0.0001f), "regen_per_sec=8");
    }

    [Test]
    public void Tick_PartialDelay_ThenLeftoverAppliesRegen()
    {
        var r = new ResourceTracker();
        r.Spend(16f);
        r.Tick(1.0f);
        Assert.That(r.Mana, Is.EqualTo(84f));
        Assert.That(r.RegenDelayLeftSec, Is.EqualTo(0.5f).Within(0.0001f));

        // 0.5s delay + 0.5s regen → +4 mana
        r.Tick(1.0f);
        Assert.That(r.Mana, Is.EqualTo(88f).Within(0.0001f));
        Assert.That(r.RegenDelayLeftSec, Is.EqualTo(0f));
    }

    [Test]
    public void Tick_CapsAtMaxMana()
    {
        var r = new ResourceTracker(maxMana: 50f, regenPerSec: 100f, regenDelayAfterCastSec: 0f);
        r.Spend(10f);
        r.Tick(1f);
        Assert.That(r.Mana, Is.EqualTo(50f));
    }

    [Test]
    public void Spend_RestartsRegenDelay()
    {
        var r = new ResourceTracker();
        r.Spend(10f);
        r.Tick(1.0f);
        Assert.That(r.RegenDelayLeftSec, Is.EqualTo(0.5f).Within(0.0001f));

        r.Spend(5f);
        Assert.That(r.RegenDelayLeftSec, Is.EqualTo(1.5f));
        Assert.That(r.Mana, Is.EqualTo(85f));
    }
}
