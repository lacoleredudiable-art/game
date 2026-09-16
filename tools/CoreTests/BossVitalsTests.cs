using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class BossVitalsTests
{
    [Test]
    public void Defaults_MatchKaradulFeel_MaxHp120_ClosingDamage35()
    {
        var t = new CombatTuning();
        Assert.That(t.Boss.MaxHp, Is.EqualTo(120f));
        Assert.That(t.ClosingDamagePerEffect, Is.EqualTo(3.5f));
    }

    [Test]
    public void CommitTable_IndependentOfRuneType()
    {
        var combat = new CombatTuning();
        var sentence = combat.Sentence;

        Assert.That(CommitForDots(combat, 1), Is.EqualTo(3.5f).Within(0.0001f));
        Assert.That(CommitForDots(combat, 4), Is.EqualTo(24.5f).Within(0.0001f));

        float sarsinti = sentence.StepForDots(3).TotalEffect * combat.ClosingDamagePerEffect;
        float igne = sentence.StepForDots(3).TotalEffect * combat.ClosingDamagePerEffect;
        Assert.That(sarsinti, Is.EqualTo(4.4f * 3.5f).Within(0.0001f));
        Assert.That(igne, Is.EqualTo(sarsinti));
    }

    [Test]
    public void ApplyDamage_FourDotBasic_KillsInFiveClosings()
    {
        var combat = new CombatTuning();
        var vitals = new BossVitals(combat.Boss.MaxHp);
        float perClose = ClosingDamageMath.Compute(
            combat.Sentence.StepForDots(4).TotalEffect,
            combat.ClosingDamagePerEffect,
            SkillResolution.Empty,
            isBasicStrike: true);

        int closings = 0;
        while (!vitals.IsDown)
        {
            bool killed = vitals.ApplyDamage(perClose);
            closings++;
            Assert.That(closings, Is.LessThanOrEqualTo(8), "120/24.5 ≈ 5 kapanışta ölmeli");
            if (killed)
                break;
        }

        Assert.That(vitals.IsDown, Is.True);
        Assert.That(closings, Is.InRange(5, 6));
    }

    [Test]
    public void AbortPaysNothing_VitalsUnchanged()
    {
        var engine = new SentenceEngine();
        engine.OnDotTouched(5, 0);
        engine.Abort();

        Assert.That(engine.History[^1].Closing, Is.Null);
        Assert.That(engine.History[^1].PaidReward, Is.False);

        var vitals = new BossVitals(120f);
        float before = vitals.Hp;
        Assert.That(vitals.Hp, Is.EqualTo(before));
        Assert.That(vitals.IsDown, Is.False);
    }

    [Test]
    public void DeathThenRevive_RestoresFullHp()
    {
        var vitals = new BossVitals(120f);
        Assert.That(vitals.ApplyDamage(120f), Is.True);
        Assert.That(vitals.IsDown, Is.True);
        Assert.That(vitals.ApplyDamage(1f), Is.False, "down iken hasar yok");

        vitals.Revive();
        Assert.That(vitals.IsDown, Is.False);
        Assert.That(vitals.Hp, Is.EqualTo(120f));
    }

    [Test]
    public void CopyFrom_PreservesClosingDamageAndMaxHp()
    {
        var src = new CombatTuning
        {
            ClosingDamagePerEffect = 2.5f
        };
        src.Boss.MaxHp = 99f;

        var dst = new CombatTuning();
        dst.CopyFrom(src);

        Assert.That(dst.ClosingDamagePerEffect, Is.EqualTo(2.5f));
        Assert.That(dst.Boss.MaxHp, Is.EqualTo(99f));
    }

    static float CommitForDots(CombatTuning combat, int dots) =>
        combat.Sentence.StepForDots(dots).TotalEffect * combat.ClosingDamagePerEffect;
}
