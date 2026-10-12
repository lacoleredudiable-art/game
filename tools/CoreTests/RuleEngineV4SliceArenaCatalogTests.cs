using System.IO;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4SliceArenaCatalogTests
{
    static string JsonPath => Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "..", "..",
        "unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json");

    [Test]
    public void Json_SliceArena_MatchesSpec()
    {
        RuleEngineV4Catalog catalog = RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(JsonPath)));
        RuleEngineV4SliceArena arena = catalog.SliceArena;
        Assert.That(arena.Columns.Count, Is.EqualTo(3));
        Assert.That(arena.Columns[0].XM, Is.EqualTo(-9f));
        Assert.That(arena.Columns[0].ZM, Is.EqualTo(4f));
        Assert.That(arena.Columns[0].RadiusM, Is.EqualTo(1f));
        Assert.That(arena.Columns[0].HeightM, Is.EqualTo(3f));
        Assert.That(arena.OuterWallCollisionRadiusM, Is.EqualTo(18f));
        Assert.That(arena.OuterWallVisualOuterRadiusM, Is.EqualTo(22f));
        Assert.That(arena.Cocoons.Count, Is.EqualTo(2));
        Assert.That(arena.Cocoons[0].XM, Is.EqualTo(-12f));
        Assert.That(arena.Cocoons[0].ZM, Is.EqualTo(-6f));
        Assert.That(arena.Cocoons[0].RadiusM, Is.EqualTo(0.6f).Within(0.001f));
    }

    [Test]
    public void Json_Cocoons_CapsuleRadius06_Height18()
    {
        RuleEngineV4Catalog catalog = RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(JsonPath)));
        foreach (RuleEngineV4SliceArenaCocoon c in catalog.SliceArena.Cocoons)
        {
            Assert.That(c.RadiusM, Is.EqualTo(0.6f).Within(0.001f), "radius_m");
            Assert.That(c.HeightM, Is.EqualTo(1.8f).Within(0.001f), "height_m");
        }
    }

    /// <summary>O-7.2: oynanış sınırı dış duvarın iç yüzü (48 segment, dış köşeler 18 m).</summary>
    [Test]
    public void O_7_2_Json_OuterWall_InnerFaceRadius()
    {
        RuleEngineV4Catalog catalog = RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(JsonPath)));
        Assert.That(catalog.SliceArena.HasOuterWallGeometry, Is.True);
        var wall = new RuleEngineV4SliceArenaWall(catalog.SliceArena);
        Assert.That(wall.Segments, Is.EqualTo(48));
        Assert.That(wall.InnerFaceRadiusM, Is.EqualTo(16.406f).Within(0.001f));
        Assert.That(wall.CenterRadiusM + wall.DepthM * 0.5f, Is.EqualTo(wall.InnerFaceRadiusM + wall.DepthM).Within(1e-4f));
    }

    [Test]
    public void O_7_2_Json_MissingOuterWall_FlagFalse()
    {
        const string json = "{\"slice_arena\":{\"outer_wall_segments\":48}}";
        Assert.That(RuleEngineV4Catalog.FromJson(json).SliceArena.HasOuterWallGeometry, Is.False);
        Assert.That(RuleEngineV4Catalog.FromJson("{}").SliceArena.HasOuterWallGeometry, Is.False);
    }

    [Test]
    public void Json_CocoonHeight_ReadFromData()
    {
        const string json = "{\"slice_arena\":{\"cocoons\":[{\"x_m\":1,\"z_m\":2,\"radius_m\":0.5,\"height_m\":2.5,\"max_hp\":10}]}}";
        RuleEngineV4SliceArena arena = RuleEngineV4Catalog.FromJson(json).SliceArena;
        Assert.That(arena.Cocoons[0].HeightM, Is.EqualTo(2.5f).Within(0.001f));
    }
}
