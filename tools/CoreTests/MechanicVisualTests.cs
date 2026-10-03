using Dovus.Core.Mechanic;
using Dovus.Core.Tuning;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace CoreTests;

/// <summary>Skill görseli gramerden doğar: madde (fiil) × yol (silah) × silüet (sıfat) — tablo yok.</summary>
[TestFixture]
public class MechanicVisualTests
{
    MechanicGrammar _grammar = null!;
    readonly SkillVisualTuning _tuning = new();

    [OneTimeSetUp]
    public void SetUp()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        _grammar = new MechanicGrammar(MechanicRules.FromJson(File.ReadAllText(path)));
    }

    VisualRecipe V(int verb, int adjective, MechanicWeapon weapon) =>
        MechanicVisualComposer.Compose(_grammar.Compose(verb, adjective, weapon), _tuning);

    [Test]
    public void EveryWeapon_Yields_AtLeast120_DistinctVisuals()
    {
        foreach (MechanicWeapon w in _grammar.Rules.Weapons)
        {
            int distinct = (from v in Enumerable.Range(1, 12)
                            from s in Enumerable.Range(1, 12)
                            select V(v, s, w).Signature()).Distinct().Count();
            int shapes = (from v in Enumerable.Range(1, 12)
                          from s in Enumerable.Range(1, 12)
                          let sig = V(v, s, w).Signature()
                          select sig.Substring(sig.IndexOf('|'))).Distinct().Count();
            TestContext.Out.WriteLine($"silah {w.Name}: {distinct}/144 farklı görsel, maddeden bağımsız {shapes} dizilim");
            Assert.That(distinct, Is.GreaterThanOrEqualTo(120), $"silah {w.Name}");
        }

        int total = (from w in _grammar.Rules.Weapons
                     from v in Enumerable.Range(1, 12)
                     from s in Enumerable.Range(1, 12)
                     select V(v, s, w).Signature()).Distinct().Count();
        TestContext.Out.WriteLine($"toplam: {total}/1440");
    }

    [Test]
    public void SameVerbSameAdjective_DifferentWeapon_ChangesMotion()
    {
        var motions = _grammar.Rules.Weapons.Select(w => V(1, 1, w).Motion).Distinct().Count();
        Assert.That(motions, Is.GreaterThan(1));
    }

    [Test]
    public void Adjective_ChangesSilhouette_ForEveryVerbAndWeapon()
    {
        var bland = (from w in _grammar.Rules.Weapons
                     from v in Enumerable.Range(1, 12)
                     let adjSigs = Enumerable.Range(1, 12).Select(s => V(v, s, w).Signature()).Distinct().Count()
                     where adjSigs < 8
                     select $"{v}/{w.Name}:{adjSigs}").ToList();
        Assert.That(bland, Is.Empty, "bir fiilin 12 sıfatı en az 8 farklı silüet vermeli");
    }

    [Test]
    public void SizeComesFromGrammarBody()
    {
        MechanicPlan dense = _grammar.Compose(1, 1, _grammar.Rules.Weapons[0]);
        MechanicPlan spread = _grammar.Compose(1, 5, _grammar.Rules.Weapons[0]);
        Assert.That(dense.Body.SizeM, Is.LessThan(spread.Body.SizeM));
        Assert.That(V(1, 1, _grammar.Rules.Weapons[0]).PieceSizeM,
            Is.LessThanOrEqualTo(V(1, 5, _grammar.Rules.Weapons[0]).PieceSizeM));
    }

    [Test]
    public void Cloud_AddsHaze_WithoutErasingWeaponLayout()
    {
        foreach (MechanicWeapon w in _grammar.Rules.Weapons)
        for (int v = 1; v <= 12; v++)
        for (int s = 1; s <= 12; s++)
        {
            MechanicPlan plan = _grammar.Compose(v, s, w);
            VisualRecipe r = MechanicVisualComposer.Compose(plan, _tuning);
            Assert.That(r.Pieces.Any(p => !p.Haze), $"{v}-{s}/{w.Name}: katı parça kalmalı");
            Assert.That(r.Pieces.Any(p => p.Haze), Is.EqualTo(plan.Body.Cloud), $"{v}-{s}/{w.Name}");
        }
    }
}
