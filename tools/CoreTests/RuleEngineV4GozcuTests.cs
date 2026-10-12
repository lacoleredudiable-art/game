using System.IO;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4GozcuTests
{
    const int Bow = 2;
    const int Sword = 4;
    const int Hammer = 6;

    static string JsonPath => Path.GetFullPath(Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "..", "..",
        "unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json"));

    static RuleEngineV4Gozcu Json() => RuleEngineV4Gozcu.FromJson(File.ReadAllText(JsonPath));

    [Test]
    public void Json_Thresholds_MatchTask()
    {
        RuleEngineV4Gozcu g = Json();
        Assert.That(g.FootBelowGroundMaxM, Is.EqualTo(0.03f).Within(1e-6f), "ayak 3 cm");
        Assert.That(g.BodyPenetrationMaxM, Is.EqualTo(0.05f).Within(1e-6f), "gövde 5 cm");
        Assert.That(g.CameraFovMinDeg, Is.EqualTo(10f), "ana kamera FOV < 10° ihlal");
        Assert.That(g.FallbackLogTag, Is.EqualTo("[V4-FALLBACK]"));
        Assert.That(g.FallbackLogMax, Is.Zero, "fallback > 0 ihlal");
        Assert.That(g.LogIntervalSec, Is.EqualTo(1f), "kural başına saniyede bir satır");
        Assert.That(g.WeaponClipNames[Bow], Is.EqualTo("Yay"));
        Assert.That(g.WeaponClipNames[Sword], Is.EqualTo("Kilic"));
    }

    [Test]
    public void Json_CameraSpeed_O5_6_EffectCover_O9_1()
    {
        RuleEngineV4Gozcu g = Json();
        Assert.That(g.CameraAngularSpeedMaxDps, Is.EqualTo(120f), "O-5.6 kamera 120°/s");
        Assert.That(g.EffectScreenCoverMaxFrac, Is.EqualTo(0.25f).Within(1e-6f), "O-9.1 efekt %25");
    }

    [Test]
    public void MissingBlock_UsesDefaults()
    {
        RuleEngineV4Gozcu g = RuleEngineV4Gozcu.FromJson("{}");
        Assert.That(g.FootBelowGroundMaxM, Is.EqualTo(RuleEngineV4GozcuDefaults.FootBelowGroundMaxM));
        Assert.That(g.BodyPenetrationMaxM, Is.EqualTo(RuleEngineV4GozcuDefaults.BodyPenetrationMaxM));
        Assert.That(g.CameraAngularSpeedMaxDps, Is.EqualTo(RuleEngineV4GozcuDefaults.CameraAngularSpeedMaxDps));
        Assert.That(g.EffectScreenCoverMaxFrac, Is.EqualTo(RuleEngineV4GozcuDefaults.EffectScreenCoverMaxFrac));
        Assert.That(g.LogIntervalSec, Is.EqualTo(RuleEngineV4GozcuDefaults.LogIntervalSec));
        Assert.That(g.CameraFovMinDeg, Is.EqualTo(RuleEngineV4GozcuDefaults.CameraFovMinDeg));
        Assert.That(g.IsClipAllowed("Kilic_VUR", Bow), Is.True, "tablo yoksa klip denetlenmez");
    }

    [TestCase("Kilic_VUR", Sword, true)]
    [TestCase("Kilic_ATIL", Sword, true)]
    [TestCase("Loko_Kilic_Run", Sword, true)]
    [TestCase("Ortak_Takla", Sword, true)]
    [TestCase("Ortak_Yay_Gerin", Sword, true)]
    [TestCase("Buyu_Sifa", Sword, true)]
    [TestCase("Dodge_Roll", Sword, true)]
    [TestCase("Takla", Bow, true)]
    [TestCase("HitReact", Bow, true)]
    [TestCase("CastSweep", Bow, true)]
    [TestCase("Loko_Agir_Idle", Hammer, true)]
    [TestCase("Kilicli_Duruş", Bow, true)]
    [TestCase("Yay_VUR", Sword, false)]
    [TestCase("Loko_Yay_Idle", Sword, false)]
    [TestCase("Cekic_VUR", Sword, false)]
    [TestCase("Yay_DURUS", Bow, true)]
    [TestCase("Loko_Yay_Run", Bow, true)]
    [TestCase("Kilic_VUR", Bow, false)]
    [TestCase("Loko_Kilic_Idle", Bow, false)]
    [TestCase("Loko_kilic_idle", Bow, false)]
    [TestCase("Yay_DURUS", Sword, false)]
    [TestCase("Kilic_VUR", 99, true)]
    public void ClipAllowed_OnlyAnotherWeaponsNameIsViolation(string clip, int weapon, bool allowed)
    {
        Assert.That(Json().IsClipAllowed(clip, weapon), Is.EqualTo(allowed), $"{clip} w{weapon}");
    }

    [Test]
    public void Reporter_SameRuleOncePerInterval_OtherRulesIndependent()
    {
        var r = new GozcuReporter(1.0);
        Assert.That(r.TryReport(GozcuRule.FootBelowGround, "4.0cm", 10, 5.0, out string first), Is.True);
        Assert.That(first, Is.EqualTo("[GOZCU] ayak_zemin_alti 4.0cm kare=10"));
        Assert.That(r.TryReport(GozcuRule.FootBelowGround, "5.0cm", 11, 5.5, out _), Is.False, "1 sn dolmadı");
        Assert.That(r.TryReport(GozcuRule.CameraAngularSpeed, "200dps", 11, 5.5, out _), Is.True, "başka kural kısıtlanmaz");
        Assert.That(r.TryReport(GozcuRule.FootBelowGround, "6.0cm", 40, 6.0, out string again), Is.True, "1 sn sonra tekrar");
        Assert.That(again, Does.EndWith("kare=40"));
        Assert.That(r.Reported, Is.EqualTo(3));
    }

    [Test]
    public void Format_ValueWithoutSpaces_EmptyIsDash()
    {
        Assert.That(GozcuReporter.Format(GozcuRule.BodyInCollider, "6.1cm Column 3", 7),
            Is.EqualTo("[GOZCU] govde_collider_icinde 6.1cm_Column_3 kare=7"));
        Assert.That(GozcuReporter.Format(GozcuRule.Fallback, "", 1), Is.EqualTo("[GOZCU] v4_fallback - kare=1"));
    }
}
