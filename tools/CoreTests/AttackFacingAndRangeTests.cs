using System.IO;
using Dovus.Core;
using Dovus.Core.Combat;
using Dovus.Core.Execution;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class AttackFacingAndRangeTests
{
    [SetUp]
    public void ResetWarnings() => DesignWarnings.ResetForTests();

    [Test]
    public void BasicStrikeRange_ComesFromVerbHitbox_NotTheOldFallback()
    {
        string json = File.ReadAllText(JsonPath());
        var catalog = SkillNumberCatalog.FromJson(json);
        var boxes = VerbExecutionData.FromJson(json);
        Assert.That(boxes.TryGetHitbox(1, out VerbHitboxSpec spec), Is.True);
        float reach = HitboxSizing.Resolve(spec, 1f, 1f).ReachM;

        Assert.That(reach, Is.EqualTo(1.5f).Within(0.001f), "fiil 1 kapsülü 1.5 m");
        Assert.That(catalog.RangeM(1), Is.EqualTo(reach).Within(0.001f));
        Assert.That(DesignWarnings.WasWarned("verb_base.1.hitbox"), Is.False);

        var tuning = new ManifestationTuning();
        Assert.That(tuning.BasicStrikeRangeM, Is.EqualTo(SkillNumberFallbacks.RangeM).Within(0.001f));
        catalog.ApplyBasicStrikeRange(tuning);
        Assert.That(tuning.BasicStrikeRangeM, Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(tuning.BasicStrikeRangeM, Is.Not.EqualTo(SkillNumberFallbacks.RangeM));
        Assert.That(tuning.BasicStrikeRadiusM, Is.EqualTo(0.5f).Within(0.001f), "1.5m × 0.5m: 0.5 yarıçap");
    }

    [Test]
    public void BasicStrikeCapsule_StartsAtBodyEdge_ReachIsEdgeToEdge()
    {
        const float body = 0.5f, reach = 1.5f, radius = 0.5f;
        StrikeCapsule.Segment(body, reach, radius, out float near, out float far);
        Assert.That(near - radius, Is.EqualTo(body).Within(0.001f), "arka uç gövde kenarında");
        Assert.That(far + radius, Is.EqualTo(body + reach).Within(0.001f), "ön uç kenardan 1.5 m");

        Assert.That(StrikeCapsule.EdgeInReach(body + 1.0f, body, reach), Is.True, "yakından vurur");
        Assert.That(StrikeCapsule.EdgeInReach(body + 2.0f, body, reach), Is.False, "2 m'den ıskalar");
        Assert.That(StrikeCapsule.CenterRange(body, reach), Is.EqualTo(2f).Within(0.001f));
    }

    [Test]
    public void HitboxSecondNumber_IsRadius_NotDiameter()
    {
        var boxes = VerbExecutionData.FromJson(File.ReadAllText(JsonPath()));
        Assert.That(boxes.TryGetHitbox(3, out VerbHitboxSpec line), Is.True);
        Assert.That(boxes.TryGetHitbox(7, out VerbHitboxSpec capsule), Is.True);
        Assert.That(HitboxSizing.Resolve(line, 1f, 1f).RadiusM, Is.EqualTo(0.8f).Within(0.001f));
        Assert.That(HitboxSizing.Resolve(capsule, 1f, 1f).RadiusM, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void BasicStrikeRange_MissingHitbox_WarnsOnceAndKeepsFallback()
    {
        string json = File.ReadAllText(JsonPath());
        int marker = json.IndexOf("\"fiil_hitbox\"", System.StringComparison.Ordinal);
        int entry = json.IndexOf("\"1\":", marker, System.StringComparison.Ordinal);
        int next = json.IndexOf("\"2\":", entry, System.StringComparison.Ordinal);
        Assert.That(marker, Is.GreaterThan(0));
        Assert.That(next, Is.GreaterThan(entry));
        string broken = json.Remove(entry, next - entry);

        var catalog = SkillNumberCatalog.FromJson(broken);
        Assert.That(catalog.RangeM(1), Is.EqualTo(SkillNumberFallbacks.RangeM).Within(0.001f));
        Assert.That(DesignWarnings.WasWarned("verb_base.1.hitbox"), Is.True);

        DesignWarnings.ResetForTests();
        int warnings = 0;
        DesignWarnings.Warned += message =>
        {
            if (message != null && message.Contains("verb_base.1", System.StringComparison.Ordinal))
                warnings++;
        };
        SkillNumberCatalog.FromJson(broken);
        SkillNumberCatalog.FromJson(broken);
        Assert.That(warnings, Is.EqualTo(1), "eksik menzil bir kez uyarır");
        Assert.That(DesignWarnings.WasWarned("verb_base.1.hitbox"), Is.True);
    }

    [Test]
    public void AttackFacing_LocksTarget_HoldsWithoutOne_DashStaysDirectional()
    {
        Assert.That(
            AttackFacingRules.Resolve(false, false, true),
            Is.EqualTo(AttackFaceKind.Movement),
            "saldırı yokken hareket kendi yönünü kullanır");

        Assert.That(
            AttackFacingRules.Resolve(true, false, true),
            Is.EqualTo(AttackFaceKind.LockedTarget));
        Assert.That(
            AttackFacingRules.Resolve(true, false, false),
            Is.EqualTo(AttackFaceKind.Hold),
            "hedef yoksa çubuğa dönülmez");
        Assert.That(
            AttackFacingRules.Resolve(true, true, true),
            Is.EqualTo(AttackFaceKind.Directional),
            "dash seçili hedefe yapışmaz");
    }

    static string JsonPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "unity", "Assets", "Resources", "ElementSystem", "element-sistemi.json"));
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }
}
