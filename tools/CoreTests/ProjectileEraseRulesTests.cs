using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dovus.Core.Mechanic;
using NUnit.Framework;

namespace CoreTests;

/// <summary>mermi_sil ailesi: her plan boş olmayan bir silme spec'i üretir, her mod ayrı bir spec'e gider.</summary>
[TestFixture]
public sealed class ProjectileEraseRulesTests
{
    MechanicGrammar _grammar = null!;
    readonly List<MechanicPlan> _erasers = new();

    [OneTimeSetUp]
    public void Load()
    {
        string root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        _grammar = new MechanicGrammar(MechanicRules.FromJson(File.ReadAllText(Path.Combine(root, "docs", "element-sistemi.json"))));
        foreach (MechanicWeapon w in _grammar.Rules.Weapons)
            for (int v = 1; v <= 12; v++)
                for (int a = 1; a <= 12; a++)
                {
                    MechanicPlan p = _grammar.Compose(v, a, w);
                    if (ProjectileEraseRules.Erases(p))
                        _erasers.Add(p);
                }
    }

    [Test]
    public void All23Skills_On10Weapons_HaveNonEmptySpec()
    {
        Assert.That(_grammar.Rules.Weapons.Count, Is.EqualTo(10));
        Assert.That(_erasers.Select(p => p.SkillId).Distinct().Count(), Is.EqualTo(23));
        Assert.That(_erasers.Count, Is.EqualTo(230));
        foreach (MechanicPlan p in _erasers)
        {
            EraseSpec s = ProjectileEraseRules.For(p, _grammar.Rules);
            Assert.That(s.IsEmpty, Is.False, $"{p.SkillId}/{p.WeaponName}");
            Assert.That(s.RadiusM, Is.GreaterThan(0f), $"{p.SkillId}/{p.WeaponName}");
        }
    }

    EraseSpec SpecFor(string mode)
    {
        MechanicPlan p = _erasers.First(x => x.Effects.Any(e => e.Stat == "mermi_sil" && e.Has(mode)));
        return ProjectileEraseRules.For(p, _grammar.Rules);
    }

    [Test]
    public void EachMode_MapsToDistinctSpec()
    {
        EraseSpec yut = SpecFor("yut");
        Assert.That(yut.Mode, Is.EqualTo(EraseMode.Absorb));
        Assert.That(yut.Lifesteal, Is.EqualTo(0.3f).Within(1e-4), "adjective_mods.2.lifesteal");

        EraseSpec engel = SpecFor("engel");
        Assert.That(engel.Anchored, Is.True);
        Assert.That(engel.Shape, Is.EqualTo(EraseShape.Disk));

        EraseSpec geri = SpecFor("geri_gonder");
        Assert.That(geri.Mode, Is.EqualTo(EraseMode.Reflect));
        Assert.That(geri.ReflectMult, Is.EqualTo(1f));

        EraseSpec delici = SpecFor("delici");
        Assert.That(delici.Shape, Is.EqualTo(EraseShape.Line));
        Assert.That(delici.WidthM, Is.EqualTo(1f));

        Assert.That(SpecFor("sis_perdesi").Mode, Is.EqualTo(EraseMode.Shroud));

        EraseSpec hedefli = SpecFor("hedefli");
        Assert.That(hedefli.Mode, Is.EqualTo(EraseMode.Targeted));
        Assert.That(hedefli.RatePerSec, Is.EqualTo(2f));

        Assert.That(SpecFor("yukselen_perde").GrowTo, Is.EqualTo(1.5f).Within(1e-4));
        Assert.That(SpecFor("surekli_perde").Shape, Is.EqualTo(EraseShape.Follow));
        Assert.That(SpecFor("bag_hatti").Shape, Is.EqualTo(EraseShape.Segment));

        EraseSpec twice = SpecFor("iki_kez");
        Assert.That(twice.Twice, Is.True);
        Assert.That(twice.Mode, Is.EqualTo(EraseMode.Delete));

        var all = new[] { "yut", "engel", "geri_gonder", "delici", "sis_perdesi", "hedefli", "yukselen_perde", "surekli_perde", "bag_hatti", "alan" }
            .Select(m => (SpecFor(m).Shape, SpecFor(m).Mode, SpecFor(m).Anchored, SpecFor(m).GrowTo > 1f)).ToList();
        Assert.That(all.Distinct().Count(), Is.EqualTo(all.Count), "every mode maps to a distinct spec");
    }

    [Test]
    public void NewParams_ArePresentInJson()
    {
        Assert.That(_grammar.Rules.Param("erase_line_width_m"), Is.EqualTo(1.0));
        Assert.That(_grammar.Rules.Param("targeted_erase_per_sec"), Is.EqualTo(2.0));
        Assert.That(_grammar.Rules.Param("projectile_reflect_mult"), Is.EqualTo(1.0));
    }

    [Test]
    public void DecoySkills_PullAggro_On10Weapons()
    {
        foreach (string id in new[] { "3-11", "4-11", "9-11", "10-7", "10-11" })
        {
            string[] va = id.Split('-');
            foreach (MechanicWeapon w in _grammar.Rules.Weapons)
            {
                MechanicPlan p = _grammar.Compose(int.Parse(va[0]), int.Parse(va[1]), w);
                MechanicEffect decoy = p.Find("yem_kopya");
                Assert.That(decoy, Is.Not.Null, $"{id}/{w.Name}");
                Assert.That(decoy!.Has("dikkat_ceker"), Is.True, $"{id}/{w.Name}");
                Assert.That(p.Effects.Count(e => e.Stat == "yem_kopya"), Is.EqualTo(1), $"{id}/{w.Name}: one decoy");
            }
        }
        // Diğer Kopyalama skill'leri yem bırakmaz (bayrak yalnız onaylı dört skill'de).
        Assert.That(_grammar.Compose(1, 11, _grammar.Rules.Weapons[0]).Find("yem_kopya"), Is.Null);
    }
}
