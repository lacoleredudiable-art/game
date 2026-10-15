using System.IO;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4CatalogWorldPhysicsTests
{
    static string JsonPath => Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "..", "..",
        "unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json");

    [SetUp]
    public void SetUp() => RuleEngineV4WorldPhysicsRuntime.Bind(RuleEngineV4WorldPhysics.Default);

    [Test]
    public void Json_WorldPhysics_MatchesDunyaFizigiD1()
    {
        string json = File.ReadAllText(Path.GetFullPath(JsonPath));
        RuleEngineV4Catalog catalog = RuleEngineV4Catalog.FromJson(json);
        Assert.That(catalog.WorldPhysics.BodyRadiusPlayerM, Is.EqualTo(0.35f));
        Assert.That(catalog.WorldPhysics.BodyRadiusBossM, Is.EqualTo(2.5f));
        Assert.That(catalog.WorldPhysics.BodyRadiusCreatureM, Is.EqualTo(0.5f));
        Assert.That(catalog.WorldPhysics.ProjectileRadiusM, Is.EqualTo(0.25f));
        Assert.That(RuleEngineV4WorldPhysicsRuntime.Active.DashSpeedMps, Is.EqualTo(14f));
        Assert.That(catalog.WorldPhysics.ProjectileColumnHalfHeightM, Is.EqualTo(50f));
    }

    /// <summary>O-6.10: boss gövde yarıçapı JSON'da yoksa katalog bunu söyler (oyun [V4-FALLBACK] yazar).</summary>
    [Test]
    public void O_6_10_BossYaricapi_JsondaYoksa_Isaretlenir()
    {
        string json = File.ReadAllText(Path.GetFullPath(JsonPath));
        Assert.That(RuleEngineV4Catalog.FromJson(json).WorldPhysics.HasBodyRadiusBossM, Is.True);
        const string key = "\"body_radius_boss_m\": 2.5,";
        Assert.That(json, Does.Contain(key), "JSON biçimi değişti");
        RuleEngineV4Catalog missing = RuleEngineV4Catalog.FromJson(json.Replace(key, string.Empty));
        Assert.That(missing.WorldPhysics.HasBodyRadiusBossM, Is.False);
        Assert.That(RuleEngineV4WorldPhysics.Default.HasBodyRadiusBossM, Is.False);
    }
}
