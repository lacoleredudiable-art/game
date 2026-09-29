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
