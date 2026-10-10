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
}
