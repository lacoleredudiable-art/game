using System.IO;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4CastEconomyTests
{
    const string Combo = "1-6";
    const string OtherCombo = "1-2";
    const double T0 = 1000;

    static RuleEngineV4Scale Scale() =>
        RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json")))).Scale;

    static double Ms(double sec) => sec * Units.SecToMs;

    [Test]
    public void ScaleBlock_HoldsSliceEconomyNumbers()
    {
        RuleEngineV4Scale s = Scale();
        Assert.That(s.ManaCostPerSkill, Is.EqualTo(25f));
        Assert.That(s.ManaPool, Is.EqualTo(100f));
        Assert.That(s.CooldownSec, Is.EqualTo(4f));
        Assert.That(s.ManaRegenPerSec, Is.EqualTo(10f));
        Assert.That(s.FizzleCooldownFrac, Is.EqualTo(0.5f));
    }

    [Test]
    public void Cast_SpendsManaAndStartsFullCooldownOnUse()
    {
        RuleEngineV4Scale s = Scale();
        var e = new RuleEngineV4CastEconomy(s);
        Assert.That(e.TryBegin(Combo, T0, out RuleEngineV4CastDenial d), Is.True);
        Assert.That(d, Is.EqualTo(RuleEngineV4CastDenial.None));
        Assert.That(e.Mana(T0), Is.EqualTo(s.ManaPool - s.ManaCostPerSkill));
        Assert.That(e.CooldownRemainingSec(Combo, T0), Is.EqualTo(s.CooldownSec).Within(1e-6));

        double justBefore = T0 + Ms(s.CooldownSec) - 1;
        Assert.That(e.TryBegin(Combo, justBefore, out d), Is.False);
        Assert.That(d, Is.EqualTo(RuleEngineV4CastDenial.Cooldown));
        Assert.That(e.TryBegin(Combo, T0 + Ms(s.CooldownSec), out _), Is.True, "bekleme bitince tekrar çıkar");
    }

    [Test]
    public void Cooldown_IsPerCombo()
    {
        var e = new RuleEngineV4CastEconomy(Scale());
        Assert.That(e.TryBegin(Combo, T0, out _), Is.True);
        Assert.That(e.TryBegin(OtherCombo, T0, out _), Is.True);
    }

    [Test]
    public void NotEnoughMana_SkillDoesNotFire_NothingSpent()
    {
        RuleEngineV4Scale s = Scale();
        var e = new RuleEngineV4CastEconomy(s);
        int casts = (int)(s.ManaPool / s.ManaCostPerSkill);
        for (int i = 0; i < casts; i++)
            Assert.That(e.TryBegin("c" + i, T0, out _), Is.True);
        float left = e.Mana(T0);
        Assert.That(left, Is.LessThan(s.ManaCostPerSkill));

        Assert.That(e.TryBegin(Combo, T0, out RuleEngineV4CastDenial d), Is.False);
        Assert.That(d, Is.EqualTo(RuleEngineV4CastDenial.Mana));
        Assert.That(e.Mana(T0), Is.EqualTo(left), "reddedilen skill mana harcamaz");
        Assert.That(e.CooldownRemainingSec(Combo, T0), Is.Zero, "reddedilen skill bekleme başlatmaz");
    }

    [Test]
    public void Mana_RegensByWorldTime_CappedAtPool()
    {
        RuleEngineV4Scale s = Scale();
        var e = new RuleEngineV4CastEconomy(s);
        Assert.That(e.TryBegin(Combo, T0, out _), Is.True);
        double oneCostSec = s.ManaCostPerSkill / s.ManaRegenPerSec;
        Assert.That(e.Mana(T0 + Ms(oneCostSec / 2)), Is.EqualTo(s.ManaPool - s.ManaCostPerSkill / 2).Within(1e-3));
        Assert.That(e.Mana(T0 + Ms(oneCostSec * 10)), Is.EqualTo(s.ManaPool));
    }

    [Test]
    public void IsFizzle_OnlyNoValidTargetAndPendingExpiry()
    {
        Assert.That(RuleEngineV4CastEconomy.IsFizzle(RuleEngineV4CastOutcome.NoValidTarget), Is.True);
        Assert.That(RuleEngineV4CastEconomy.IsFizzle(RuleEngineV4CastOutcome.PendingExpired), Is.True);
        Assert.That(RuleEngineV4CastEconomy.IsFizzle(RuleEngineV4CastOutcome.Missed), Is.False, "ıska boşa gitme değil");
        Assert.That(RuleEngineV4CastEconomy.IsFizzle(RuleEngineV4CastOutcome.Delivered), Is.False);
    }

    [TestCase(RuleEngineV4CastOutcome.Missed)]
    [TestCase(RuleEngineV4CastOutcome.Delivered)]
    public void MissOrHit_SpendsManaKeepsFullCooldown(RuleEngineV4CastOutcome outcome)
    {
        RuleEngineV4Scale s = Scale();
        var e = new RuleEngineV4CastEconomy(s);
        Assert.That(e.TryBegin(Combo, T0, out _), Is.True);
        e.Settle(Combo, T0, outcome);

        Assert.That(e.Mana(T0), Is.EqualTo(s.ManaPool - s.ManaCostPerSkill));
        Assert.That(e.CooldownRemainingSec(Combo, T0), Is.EqualTo(s.CooldownSec).Within(1e-6), "tam bekleme");
        Assert.That(e.TryBegin(Combo, T0 + Ms(s.CooldownSec * s.FizzleCooldownFrac), out RuleEngineV4CastDenial d), Is.False);
        Assert.That(d, Is.EqualTo(RuleEngineV4CastDenial.Cooldown));
    }

    [TestCase(RuleEngineV4CastOutcome.NoValidTarget)]
    [TestCase(RuleEngineV4CastOutcome.PendingExpired)]
    public void Fizzle_SpendsManaAndAppliesHalfCooldownFromUse(RuleEngineV4CastOutcome outcome)
    {
        RuleEngineV4Scale s = Scale();
        var e = new RuleEngineV4CastEconomy(s);
        Assert.That(e.TryBegin(Combo, T0, out _), Is.True);
        double later = T0 + Ms(s.CooldownSec) * s.FizzleCooldownFrac / 2;
        e.Settle(Combo, T0, outcome);

        Assert.That(e.Mana(T0), Is.EqualTo(s.ManaPool - s.ManaCostPerSkill), "boşa giden skill manayı harcar");
        Assert.That(e.CooldownRemainingSec(Combo, T0),
            Is.EqualTo(s.CooldownSec * s.FizzleCooldownFrac).Within(1e-6));
        Assert.That(e.TryBegin(Combo, later, out RuleEngineV4CastDenial d), Is.False);
        Assert.That(d, Is.EqualTo(RuleEngineV4CastDenial.Cooldown));
        Assert.That(e.TryBegin(Combo, T0 + Ms(s.CooldownSec * s.FizzleCooldownFrac), out _), Is.True,
            "yarım bekleme bitince tekrar çıkar");
    }
}
