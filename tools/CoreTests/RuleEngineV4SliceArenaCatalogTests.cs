using System;
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

    /// <summary>
    /// O-7.2: çarpışma duvarının iç yüzü görünen duvarın iç yüzüyle aynı (18 m), görünmez duvar yok;
    /// segmentler dışa doğru kurulur, boşluksuz birleşir ve görünen dış yüzü (O-7.3) aşmaz.
    /// </summary>
    [Test]
    public void O_7_2_Json_OuterWall_InnerFace_EqualsVisibleInnerFace()
    {
        RuleEngineV4Catalog catalog = RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(JsonPath)));
        RuleEngineV4SliceArena arena = catalog.SliceArena;
        Assert.That(arena.HasOuterWallGeometry, Is.True);
        var wall = new RuleEngineV4SliceArenaWall(arena);
        Assert.That(wall.Segments, Is.EqualTo(48));
        Assert.That(wall.InnerFaceRadiusM, Is.EqualTo(arena.OuterWallVisualInnerRadiusM).Within(0.001f), "sınır");
        Assert.That(wall.CenterRadiusM - wall.DepthM * 0.5f, Is.EqualTo(arena.OuterWallVisualInnerRadiusM).Within(0.001f),
            "segment iç yüzü");
        float outerApothem = wall.CenterRadiusM + wall.DepthM * 0.5f;
        float halfStep = MathF.PI / wall.Segments;
        Assert.That(wall.ChordM * 0.5f, Is.GreaterThanOrEqualTo(outerApothem * MathF.Tan(halfStep) - 1e-4f),
            "segmentler dış köşede birleşmiyor");
        Assert.That(MathF.Sqrt(outerApothem * outerApothem + wall.ChordM * wall.ChordM * 0.25f),
            Is.LessThanOrEqualTo(arena.OuterWallVisualOuterRadiusM), "çarpışma görünen dış yüzü aşıyor");
    }

    [Test]
    public void O_7_2_Json_MissingOuterWall_FlagFalse()
    {
        const string json = "{\"slice_arena\":{\"outer_wall_segments\":48}}";
        Assert.That(RuleEngineV4Catalog.FromJson(json).SliceArena.HasOuterWallGeometry, Is.False);
        Assert.That(RuleEngineV4Catalog.FromJson("{}").SliceArena.HasOuterWallGeometry, Is.False);
    }

    /// <summary>O-7.3: görsel duvar köşeleri segment yüzleri arasında; köşeden köşeye yüz, çarpışma yüzüyle aynı düzlem.</summary>
    [Test]
    public void O_7_3_Json_VisualWall_ColorAndCornersOnCollisionFaces()
    {
        RuleEngineV4SliceArena arena = RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(JsonPath))).SliceArena;
        Assert.That(arena.HasOuterWallColor, Is.True);
        Assert.That(arena.OuterWallColorHex, Is.EqualTo("#3A4048"));
        Assert.That(arena.OuterWallHeightM, Is.EqualTo(4f));
        var wall = new RuleEngineV4SliceArenaWall(arena);
        float r = wall.CornerRadiusM(arena.OuterWallVisualInnerRadiusM);
        for (int i = 0; i < wall.Segments; i++)
        {
            float face = wall.SegmentAngleRad(i);
            foreach (float corner in new[] { wall.CornerAngleRad(i - 1), wall.CornerAngleRad(i) })
                Assert.That(r * MathF.Cos(corner - face), Is.EqualTo(arena.OuterWallVisualInnerRadiusM).Within(1e-3f),
                    $"segment {i}: köşe iç yüz düzleminde değil");
        }
    }

    [Test]
    public void O_7_3_Json_MissingWallColor_FlagFalse()
    {
        RuleEngineV4SliceArena arena = RuleEngineV4Catalog.FromJson("{\"slice_arena\":{}}").SliceArena;
        Assert.That(arena.HasOuterWallColor, Is.False);
        Assert.That(arena.OuterWallColorHex, Is.EqualTo(RuleEngineV4SliceArenaDefaults.OuterWallColorHex));
    }

    /// <summary>O-7.7 / O-8.7: duvar ötesi zemin 300 m, üstel sis 0,012 #4A5260.</summary>
    [Test]
    public void O_8_7_Json_BeyondWall_GroundAndFog()
    {
        RuleEngineV4SliceArenaBeyond beyond = RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(JsonPath)))
            .SliceArenaVisuals.Beyond;
        Assert.That(beyond.HasBlock, Is.True);
        Assert.That(beyond.GroundRadiusM, Is.EqualTo(300f));
        Assert.That(beyond.FogDensity, Is.EqualTo(0.012f).Within(1e-6f));
        Assert.That(beyond.FogColorHex, Is.EqualTo("#4A5260"));
        Assert.That(beyond.GroundColorHex, Is.EqualTo("#5B5A57"));
    }

    [Test]
    public void O_8_7_Json_MissingBeyondWall_FlagFalse()
    {
        Assert.That(RuleEngineV4Catalog.FromJson("{\"slice_arena_visuals\":{}}").SliceArenaVisuals.Beyond.HasBlock, Is.False);
        Assert.That(RuleEngineV4Catalog.FromJson("{}").SliceArenaVisuals.Beyond.HasBlock, Is.False);
    }

    [Test]
    public void Json_CocoonHeight_ReadFromData()
    {
        const string json = "{\"slice_arena\":{\"cocoons\":[{\"x_m\":1,\"z_m\":2,\"radius_m\":0.5,\"height_m\":2.5,\"max_hp\":10}]}}";
        RuleEngineV4SliceArena arena = RuleEngineV4Catalog.FromJson(json).SliceArena;
        Assert.That(arena.Cocoons[0].HeightM, Is.EqualTo(2.5f).Within(0.001f));
    }
}
