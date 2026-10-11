using System.IO;
using Dovus.Core.Presentation;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4SliceVfxTests
{
    static string JsonPath => Path.GetFullPath(Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "..", "..",
        "unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json"));

    static RuleEngineV4SliceVfx Slice() => RuleEngineV4SliceVfx.FromJson(File.ReadAllText(JsonPath));

    [Test]
    public void Json_BladeTrail_QuarterSecondLife_TapersToTip()
    {
        RuleEngineV4BladeTrailFx t = Slice().BladeTrail;
        Assert.That(t.LifeSec, Is.EqualTo(0.25f).Within(1e-4f));
        Assert.That(t.TailWidthFrac, Is.LessThan(1f));

        t.Shape(0f, out float w0, out float a0);
        Assert.That(w0, Is.EqualTo(1f).Within(1e-4f), "yeni örnek tam kabza→uç");
        Assert.That(a0, Is.EqualTo(1f).Within(1e-4f));

        t.Shape(t.LifeSec * 0.5f, out float wMid, out float aMid);
        Assert.That(wMid, Is.LessThan(w0));
        Assert.That(aMid, Is.LessThan(a0));

        t.Shape(t.LifeSec, out float wEnd, out float aEnd);
        Assert.That(wEnd, Is.EqualTo(t.TailWidthFrac).Within(1e-4f), "kuyruk uca incelir");
        Assert.That(aEnd, Is.EqualTo(0f).Within(1e-4f));
    }

    [Test]
    public void MissingBlock_UsesDefaults()
    {
        RuleEngineV4SliceVfx v = RuleEngineV4SliceVfx.FromJson("{}");
        Assert.That(v.BladeTrail.LifeSec, Is.EqualTo(RuleEngineV4SliceVfxDefaults.BladeTrailLifeSec));
        Assert.That(v.BladeTrail.TailWidthFrac, Is.EqualTo(RuleEngineV4SliceVfxDefaults.BladeTrailTailWidthFrac));
    }

    [Test]
    public void Json_KilicVur_OpensTrailEvents()
    {
        RuleEngineV4Catalog c = RuleEngineV4Catalog.FromJson(File.ReadAllText(JsonPath));
        var plan = new CommandPlan(1, 0, 4, System.Array.Empty<PhysicsCommand>());
        RuleEngineV4SkillAnimBinding b = RuleEngineV4SkillAnimResolver.Resolve(plan, c.SkillAnim);
        Assert.That(b.Clip, Is.EqualTo("Kilic_VUR"));
        Assert.That(b.Events.HasFlag(RuleEngineV4SkillAnimEvent.TrailOn), Is.True);
        Assert.That(b.Events.HasFlag(RuleEngineV4SkillAnimEvent.TrailOff), Is.True);
    }
}
