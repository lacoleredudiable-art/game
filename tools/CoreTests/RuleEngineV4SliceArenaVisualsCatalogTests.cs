using System.IO;
using System.Linq;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4SliceArenaVisualsCatalogTests
{
    static string JsonPath => Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "..", "..",
        "unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json");

    static RuleEngineV4SliceArenaVisuals Visuals() =>
        RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(JsonPath))).SliceArenaVisuals;

    [Test]
    public void Json_SliceArenaVisuals_MatchesSpec()
    {
        RuleEngineV4SliceArenaVisuals v = Visuals();
        Assert.That(v.Enabled, Is.True);
        Assert.That(v.ColumnModel, Is.EqualTo("sutun"));
        Assert.That(v.CocoonModel, Is.EqualTo("koza"));

        RuleEngineV4SliceArenaRibs ribs = v.Ribs;
        Assert.That(ribs.Model, Is.EqualTo("kaburga"));
        Assert.That(ribs.Lod1Model, Is.EqualTo("kaburga_LOD1"));
        Assert.That(ribs.Count, Is.EqualTo(7));
        Assert.That(ribs.Scale, Is.EqualTo(2.3f).Within(1e-4f));
        Assert.That(ribs.SpacingM, Is.EqualTo(5f));
        Assert.That(ribs.Axis, Is.EqualTo("north_to_south"));
        Assert.That(ribs.HeartIndex, Is.EqualTo(0));
        Assert.That(ribs.Seed, Is.EqualTo(1234));
        Assert.That(ribs.Colliders, Is.False);

        Assert.That(v.HasRuneStone, Is.True);
        Assert.That(v.RuneStoneModel, Is.EqualTo("runtasi"));
        Assert.That(v.RuneStone.XM, Is.EqualTo(0f));
        Assert.That(v.RuneStone.ZM, Is.EqualTo(-19f));
        Assert.That(v.RuneStoneCollider, Is.False);
    }

    [Test]
    public void Ribs_Layout_SameSeedSameTransforms_HeartNorthAndThickest()
    {
        RuleEngineV4SliceArenaRibs ribs = Visuals().Ribs;
        var a = ribs.Layout();
        var b = Visuals().Ribs.Layout();
        Assert.That(a.Count, Is.EqualTo(ribs.Count));
        Assert.That(b, Is.EqualTo(a), "aynı seed farklı yerleşim");

        RuleEngineV4SliceArenaPlacement heart = a[ribs.HeartIndex];
        Assert.That(heart.ZM, Is.EqualTo(a.Max(p => p.ZM)), "kalp kaburgası en kuzeyde değil");
        Assert.That(heart.Scale, Is.EqualTo(a.Max(p => p.Scale)), "kalp kaburgası en kalın değil");
        for (int i = 1; i < a.Count; i++)
            Assert.That(a[i - 1].ZM - a[i].ZM, Is.EqualTo(ribs.SpacingM).Within(1e-4f), "kuzeyden güneye aralık");
        Assert.That(a.Sum(p => p.ZM), Is.EqualTo(0f).Within(1e-3f), "dizi arena merkezine ortalı değil");
        foreach (RuleEngineV4SliceArenaPlacement p in a)
        {
            Assert.That(p.XM, Is.EqualTo(0f));
            Assert.That(System.Math.Abs(p.YawDeg), Is.LessThanOrEqualTo(ribs.JitterRotDeg));
        }
    }

    [Test]
    public void Ribs_Layout_DifferentSeed_DifferentJitter()
    {
        const string json = "{\"slice_arena_visuals\":{\"ribs\":{\"count\":3,\"seed\":7}}}";
        const string other = "{\"slice_arena_visuals\":{\"ribs\":{\"count\":3,\"seed\":8}}}";
        var a = RuleEngineV4Catalog.FromJson(json).SliceArenaVisuals.Ribs.Layout();
        var b = RuleEngineV4Catalog.FromJson(other).SliceArenaVisuals.Ribs.Layout();
        Assert.That(b, Is.Not.EqualTo(a));
    }

    [Test]
    public void Json_Ribs_HalvesAndKeepClear_FromJson()
    {
        RuleEngineV4SliceArenaRibs ribs = Visuals().Ribs;
        Assert.That(ribs.KeepClearXM, Is.EqualTo(22f), "keep_clear.x_m");
        Assert.That(ribs.KeepClearBelowM, Is.EqualTo(2.5f), "keep_clear.below_m");

        RuleEngineV4SliceArenaRibHalves h = ribs.Halves;
        Assert.That(h.Enabled, Is.True, "kemer iki yarım kurulmalı");
        Assert.That(h.LeanDeg, Is.EqualTo(RuleEngineV4SliceArenaVisualsDefaults.RibHalfLeanDeg));
        Assert.That(h.ScaleMult, Is.EqualTo(RuleEngineV4SliceArenaVisualsDefaults.RibHalfScaleMult));
        Assert.That(h.PivotYM, Is.EqualTo(RuleEngineV4SliceArenaVisualsDefaults.RibHalfPivotYM));
        Assert.That(h.CrownYM, Is.EqualTo(RuleEngineV4SliceArenaVisualsDefaults.RibHalfCrownYM));
        Assert.That(h.CrownOverlapM, Is.EqualTo(RuleEngineV4SliceArenaVisualsDefaults.RibHalfCrownOverlapM));

        var whole = RuleEngineV4Catalog.FromJson("{\"slice_arena_visuals\":{\"ribs\":{\"count\":1}}}").SliceArenaVisuals.Ribs;
        Assert.That(whole.Halves.Enabled, Is.False, "halves bloğu yoksa tek parça kemer");
    }

    [Test]
    public void Json_NoVisualsBlock_Disabled()
    {
        RuleEngineV4SliceArenaVisuals v = RuleEngineV4Catalog.FromJson("{}").SliceArenaVisuals;
        Assert.That(v.Enabled, Is.False);
        Assert.That(v.HasRuneStone, Is.False);
    }
}
