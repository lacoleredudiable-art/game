using Dovus.App.Casting;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class ClosingHealRulesTests
{
    static SkillResolution Skill(string verbFamily, string action, float baseHeal = 0f) =>
        new SkillResolution(
            "1", "Su", "T", "t", "job",
            "iyilestirme", "İyileştirme", verbFamily, action,
            0f, 0f, "self_aura", "free_move", new[] { "regen" },
            "yayma", "Yayma", "wave",
            0.8f, 1f, 1f,
            2, "Temel", 1f, "free_move",
            string.Empty,
            baseHeal: baseHeal);

    [Test]
    public void IsHealSkill_MendFamily()
    {
        Assert.That(ClosingHealRules.IsHealSkill(Skill("mend", "damage")), Is.True);
    }

    [Test]
    public void IsHealSkill_HealAction()
    {
        Assert.That(ClosingHealRules.IsHealSkill(Skill("other", "heal")), Is.True);
        Assert.That(ClosingHealRules.IsHealSkill(Skill("strike", "damage")), Is.False);
    }

    [Test]
    public void ComputeHealAmount_UsesBaseHealWhenPositive()
    {
        int amt = ClosingHealRules.ComputeHealAmount(
            totalEffect: 10f,
            Skill("mend", "heal", baseHeal: 42f),
            effectScale: 1f,
            chainBonus: 1f,
            closingDamagePerEffect: 3.5f,
            weaponSupportPower: 1.2f);
        Assert.That(amt, Is.EqualTo(50));
    }

    [Test]
    public void ComputeHealAmount_FromTotalEffect()
    {
        int amt = ClosingHealRules.ComputeHealAmount(
            totalEffect: 4f,
            Skill("mend", "heal"),
            effectScale: 1f,
            chainBonus: 2f,
            closingDamagePerEffect: 3.5f,
            weaponSupportPower: 1f);
        Assert.That(amt, Is.EqualTo(28));
    }

    [Test]
    public void AdjectiveLifesteal_EmptySkill()
    {
        Assert.That(ClosingHealRules.AdjectiveLifesteal(SkillResolution.Empty), Is.EqualTo(0f));
    }
}
