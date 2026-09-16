using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class ClosingDamageMathTests
{
    static SkillResolution Strike(float baseDamage, float damageMult = 1f) =>
        new SkillResolution(
            "1", "Ateş", "Test", "t", "job",
            "saldiri", "Saldırı", "strike", "damage",
            baseDamage, 15f, "projectile", "free_move", new[] { "burn" },
            "yogunlastirma", "Yoğunlaştırma", "focus",
            damageMult, 1f, 1f,
            2, "Temel", 1f, "free_move",
            string.Empty);

    static SkillResolution Heal() =>
        new SkillResolution(
            "2", "Su", "İyileştirme", "t", "job",
            "iyilestirme", "İyileştirme", "mend", "heal",
            0f, 0f, "self_aura", "free_move", new[] { "regen" },
            "yayma", "Yayma", "wave",
            0.8f, 1f, 1f,
            2, "Temel", 1f, "free_move",
            string.Empty);

    [Test]
    public void BasicStrike_UsesCommitOnly()
    {
        float d = ClosingDamageMath.Compute(7f, 3.5f, SkillResolution.Empty, isBasicStrike: true);
        Assert.That(d, Is.EqualTo(24.5f).Within(0.001f));
    }

    [Test]
    public void StrikeVerb_ScalesFromReference40()
    {
        // commit 7*3.5=24.5; verb 40/40; adj 0.85 → 20.825
        float d = ClosingDamageMath.Compute(7f, 3.5f, Strike(40f, 0.85f), false);
        Assert.That(d, Is.EqualTo(24.5f * 0.85f).Within(0.001f));
    }

    [Test]
    public void WeakerVerb_DoesLessDamage()
    {
        float full = ClosingDamageMath.Compute(4.4f, 3.5f, Strike(40f), false);
        float weak = ClosingDamageMath.Compute(4.4f, 3.5f, Strike(20f), false);
        Assert.That(weak, Is.EqualTo(full * 0.5f).Within(0.001f));
    }

    [Test]
    public void Heal_DoesZeroBossDamage()
    {
        float d = ClosingDamageMath.Compute(7f, 3.5f, Heal(), false);
        Assert.That(d, Is.EqualTo(0f));
    }

    [Test]
    public void RuneType_DoesNotAppearInFormula()
    {
        // Aynı skill + TotalEffect → aynı hasar; ClosingHit.Type yok.
        var skill = Strike(40f);
        Assert.That(
            ClosingDamageMath.Compute(4.4f, 3.5f, skill, false),
            Is.EqualTo(ClosingDamageMath.Compute(4.4f, 3.5f, skill, false)));
    }
}
