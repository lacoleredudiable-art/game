using System.IO;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using NUnit.Framework;

namespace CoreTests;

/// <summary>O-5.1–O-5.4: dilim kamerasının dört ölçüsü kural-motoru-v4.json <c>slice_camera</c>'dan okunur.</summary>
[TestFixture]
public sealed class RuleEngineV4SliceCameraCatalogTests
{
    static string JsonPath => Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "..", "..",
        "unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json");

    [Test]
    public void O_5_1_O_5_4_Json_SliceCamera_MatchesSpec()
    {
        RuleEngineV4Catalog catalog = RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(JsonPath)));
        RuleEngineV4SliceCamera c = catalog.SliceCamera;
        Assert.That(c.HasDistanceM && c.HasPitchDeg && c.HasLookHeightM && c.HasFovDeg, Is.True);
        Assert.That(c.DistanceM, Is.EqualTo(6.0f).Within(0.001f), "O-5.1 distance_m");
        Assert.That(c.PitchDeg, Is.EqualTo(30f).Within(0.001f), "O-5.2 pitch_deg");
        Assert.That(c.LookHeightM, Is.EqualTo(1.2f).Within(0.001f), "O-5.3 look_height_m");
        Assert.That(c.FovDeg, Is.EqualTo(55f).Within(0.001f), "O-5.4 fov_deg");
    }

    [Test]
    public void O_5_1_O_5_4_MissingField_HasFalse_NoDefault()
    {
        RuleEngineV4SliceCamera c = RuleEngineV4SliceCamera.FromJson(
            MiniJson.Parse("{\"slice_camera\":{\"distance_m\":6,\"fov_deg\":\"55\"}}"));
        Assert.That(c.HasDistanceM, Is.True);
        Assert.That(c.HasPitchDeg, Is.False, "pitch_deg yok");
        Assert.That(c.HasLookHeightM, Is.False, "look_height_m yok");
        Assert.That(c.HasFovDeg, Is.False, "fov_deg sayı değil");
        Assert.That(c.PitchDeg, Is.Zero);

        RuleEngineV4SliceCamera none = RuleEngineV4SliceCamera.FromJson(MiniJson.Parse("{}"));
        Assert.That(none.HasDistanceM || none.HasPitchDeg || none.HasLookHeightM || none.HasFovDeg, Is.False);
    }
}
