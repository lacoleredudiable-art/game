using System.IO;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// O-5.1–O-5.4, O-5.6, O-5.7, O-5.9, O-5.11: dilim kamerasının ölçüleri kural-motoru-v4.json <c>slice_camera</c>'dan
/// okunur.
/// </summary>
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
    public void O_5_6_O_5_7_O_5_9_O_5_11_Json_SliceCamera_MatchesSpec()
    {
        RuleEngineV4Catalog catalog = RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(JsonPath)));
        RuleEngineV4SliceCamera c = catalog.SliceCamera;
        Assert.That(c.HasTurnRateMaxDps && c.HasBossYawFreezeEdgeM && c.HasBossYawFreeEdgeM
            && c.HasShoulderSwitchMinSec && c.HasBossOccludedOpacity, Is.True);
        Assert.That(c.TurnRateMaxDps, Is.EqualTo(120f).Within(0.001f), "O-5.6 turn_rate_max_dps");
        Assert.That(c.BossYawFreezeEdgeM, Is.EqualTo(2f).Within(0.001f), "O-5.7 boss_yaw_freeze_edge_m");
        Assert.That(c.BossYawFreeEdgeM, Is.EqualTo(5f).Within(0.001f), "O-5.7 boss_yaw_free_edge_m");
        Assert.That(c.ShoulderSwitchMinSec, Is.EqualTo(1.5f).Within(0.001f), "O-5.9 shoulder_switch_min_sec");
        Assert.That(c.BossOccludedOpacity, Is.EqualTo(0.35f).Within(0.001f), "O-5.11 boss_occluded_opacity");
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
        Assert.That(none.HasTurnRateMaxDps || none.HasBossYawFreezeEdgeM || none.HasBossYawFreeEdgeM
            || none.HasShoulderSwitchMinSec || none.HasBossOccludedOpacity, Is.False);
    }
}
