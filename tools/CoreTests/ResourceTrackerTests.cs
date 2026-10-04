using Dovus.Core.Boss;
using Dovus.Core.Shared;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
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
    public void CanAfford_And_Consume_DeductsMana()
    {
        var r = new ResourceTracker();
        Assert.That(r.CanAfford(10f), Is.True);
        r.Consume(10f);
        Assert.That(r.Mana, Is.EqualTo(90f));
        Assert.That(r.CanAfford(91f), Is.False);
    }

    [Test]
    public void Consume_ZeroOrNegative_DoesNotStartDelay()
    {
        var r = new ResourceTracker();
        r.Consume(0f);
        r.Consume(-5f);
        Assert.That(r.Mana, Is.EqualTo(100f));
        Assert.That(r.RegenDelayLeftSec, Is.EqualTo(0f));
    }

    [Test]
    public void Tick_WaitsRegenDelay_ThenRegensAtJsonRate()
    {
        var r = new ResourceTracker();
        r.Consume(40f);
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
        r.Consume(16f);
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
        r.Consume(10f);
        r.Tick(1f);
        Assert.That(r.Mana, Is.EqualTo(50f));
    }

    [Test]
    public void RefillToMax_restores_mana_and_clears_delay()
    {
        var r = new ResourceTracker(100f, 8f, 1.5f);
        r.Consume(40f);
        r.RefillToMax();
        Assert.That(r.Mana, Is.EqualTo(100f));
        Assert.That(r.RegenDelayLeftSec, Is.EqualTo(0f));
    }

    [Test]
    public void Consume_RestartsRegenDelay()
    {
        var r = new ResourceTracker();
        r.Consume(10f);
        r.Tick(1.0f);
        Assert.That(r.RegenDelayLeftSec, Is.EqualTo(0.5f).Within(0.0001f));

        r.Consume(5f);
        Assert.That(r.RegenDelayLeftSec, Is.EqualTo(1.5f));
        Assert.That(r.Mana, Is.EqualTo(85f));
    }

    [Test]
    public void Consume_ClampsAtZero_DoesNotBlock()
    {
        var r = new ResourceTracker();
        r.Consume(40f);
        Assert.That(r.Mana, Is.EqualTo(60f));
        r.Consume(100f);
        Assert.That(r.Mana, Is.EqualTo(0f));
        Assert.That(r.RegenDelayLeftSec, Is.EqualTo(1.5f));
    }

    [Test]
    public void Tick_ZeroDelta_NoRegen()
    {
        var r = new ResourceTracker(maxMana: 100f, regenPerSec: 8f, regenDelayAfterCastSec: 0f);
        r.Consume(50f);
        float before = r.Mana;
        r.Tick(0f);
        Assert.That(r.Mana, Is.EqualTo(before));
    }

    [Test]
    public void EnforceResourceCost_DefaultsTrue_AndCopyFrom()
    {
        var a = new Dovus.Core.Tuning.CombatTuning();
        Assert.That(a.EnforceResourceCost, Is.True);

        a.EnforceResourceCost = false;
        var b = new Dovus.Core.Tuning.CombatTuning();
        b.CopyFrom(a);
        Assert.That(b.EnforceResourceCost, Is.False);

        b.ResetToDefaults();
        Assert.That(b.EnforceResourceCost, Is.True);
    }

    [Test]
    public void EnforceManaCooldown_DefaultsTrue_AndResetToDefaults()
    {
        var c = new Dovus.Core.Tuning.CombatTuning();
        Assert.That(c.EnforceResourceCost, Is.True);
        Assert.That(c.EnforceCooldown, Is.True);

        c.EnforceResourceCost = false;
        c.EnforceCooldown = false;
        c.ResetToDefaults();
        Assert.That(c.EnforceResourceCost, Is.True);
        Assert.That(c.EnforceCooldown, Is.True);
    }
}
