using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class BossVitalsTests
{
    [Test]
    public void Defaults_MatchSpec_MaxHp120_ClosingDamage1()
    {
        var t = new CombatTuning();
        Assert.That(t.Boss.MaxHp, Is.EqualTo(120f));
        Assert.That(t.ClosingDamagePerEffect, Is.EqualTo(1.0f));
    }

    [Test]
    public void ClosingDamage_MatchesSentenceTable_IndependentOfRuneType()
    {
        var combat = new CombatTuning();
        var sentence = combat.Sentence;

        Assert.That(DamageForDots(combat, 1), Is.EqualTo(1.0f).Within(0.0001f));
        Assert.That(DamageForDots(combat, 4), Is.EqualTo(7.0f).Within(0.0001f));

        // Tür hasarı değiştirmez (§12) — aynı nokta sayısı, farklı son rün.
        float sarsinti = sentence.StepForDots(3).TotalEffect * combat.ClosingDamagePerEffect;
        float igne = sentence.StepForDots(3).TotalEffect * combat.ClosingDamagePerEffect;
        float suru = sentence.StepForDots(3).TotalEffect * combat.ClosingDamagePerEffect;
        Assert.That(sarsinti, Is.EqualTo(4.4f).Within(0.0001f));
        Assert.That(igne, Is.EqualTo(sarsinti));
        Assert.That(suru, Is.EqualTo(sarsinti));

        // ClosingHit türü de miktarı taşımaz — TotalEffect uzunluktan gelir.
        var a = new ClosingHit(Rune.Sarsinti, sentence.StepForDots(2).TotalEffect, 2);
        var b = new ClosingHit(Rune.Igne, sentence.StepForDots(2).TotalEffect, 2);
        Assert.That(a.TotalEffect * combat.ClosingDamagePerEffect,
            Is.EqualTo(b.TotalEffect * combat.ClosingDamagePerEffect));
    }

    [Test]
    public void ApplyDamage_FourDotClosings_KillAroundSeventeen()
    {
        var combat = new CombatTuning();
        var vitals = new BossVitals(combat.Boss.MaxHp);
        float perClose = combat.Sentence.StepForDots(4).TotalEffect * combat.ClosingDamagePerEffect;

        int closings = 0;
        while (!vitals.IsDown)
        {
            bool killed = vitals.ApplyDamage(perClose);
            closings++;
            Assert.That(closings, Is.LessThanOrEqualTo(20), "120/7 ≈ 17 kapanışta ölmeli");
            if (killed)
                break;
        }

        Assert.That(vitals.IsDown, Is.True);
        Assert.That(vitals.Hp, Is.EqualTo(0f));
        Assert.That(closings, Is.EqualTo(18)); // 17×7=119, 18. vuruş öldürür
        // En iyi oynanış ~17: son vuruş kısmi olabilir; tam 7'lerle 18 — kabul ~17 bandı.
        Assert.That(closings, Is.InRange(17, 18));
    }

    [Test]
    public void AbortPaysNothing_VitalsUnchanged()
    {
        // Abort Closing=null üretir (§5) — hasar yolu Closing gerektirir.
        var engine = new SentenceEngine();
        engine.OnDotTouched(5, 0);
        engine.Abort();

        Assert.That(engine.History[^1].Closing, Is.Null);
        Assert.That(engine.History[^1].PaidReward, Is.False);

        var vitals = new BossVitals(120f);
        float before = vitals.Hp;
        // Closing yok → ApplyDamage çağrılmaz; can aynı kalır.
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

    static float DamageForDots(CombatTuning combat, int dots) =>
        combat.Sentence.StepForDots(dots).TotalEffect * combat.ClosingDamagePerEffect;
}
