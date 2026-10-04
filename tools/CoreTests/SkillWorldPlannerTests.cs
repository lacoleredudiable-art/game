using Dovus.Core.Element;
using Dovus.Core.Input;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Presentation;
using Dovus.Core.Status;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using NUnit.Framework;
using System;
using System.IO;

using Dovus.Core.Shared;
namespace CoreTests;

[TestFixture]
public class SkillWorldPlannerTests
{
    static string ElementJsonPath()
    {
        string fromTest = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (File.Exists(fromTest)) return fromTest;
        string fromCwd = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "docs", "element-sistemi.json"));
        Assert.That(File.Exists(fromCwd), Is.True, $"element-sistemi.json yok: {fromCwd}");
        return fromCwd;
    }

    static string PresentationJsonPath()
    {
        string fromTest = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "prezentasyon-katmani.json"));
        if (File.Exists(fromTest)) return fromTest;
        string fromCwd = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "docs", "prezentasyon-katmani.json"));
        Assert.That(File.Exists(fromCwd), Is.True, $"prezentasyon-katmani.json yok: {fromCwd}");
        return fromCwd;
    }

    static SkillMotor Motor() => SkillMotor.FromJson(File.ReadAllText(ElementJsonPath()));
    static PresentationCatalog Catalog() =>
        PresentationCatalog.FromJson(File.ReadAllText(PresentationJsonPath()));

    [Test]
    public void HitboxScaleMult_ScalesBangRadius_SameVerbHitbox()
    {
        // v6: 5-1 (Yoğunlaştırma, hitbox_scale_mult 0.55) ve 5-5 (1.8) aynı ground_ring
        // hitbox'ını paylaşır; bang yarıçapı yalnız engine hitbox_scale_mult oranında değişir.
        SkillMotor m = Motor();
        PresentationCatalog cat = Catalog();
        var tuning = new ManifestationTuning();

        SkillResolution small = m.Resolve(new[] { 5, 1 });
        SkillResolution large = m.Resolve(new[] { 5, 5 });
        Assert.That(small.Presentation.Hitbox, Is.EqualTo(large.Presentation.Hitbox));
        LivingEffectPlan a = SkillWorldPlanner.Build(small, cat, tuning);
        LivingEffectPlan b = SkillWorldPlanner.Build(large, cat, tuning);

        Assert.That(a.HasPlan, Is.True);
        Assert.That(b.HasPlan, Is.True);
        float expectedRatio = large.Scaling.HitboxScaleMult / small.Scaling.HitboxScaleMult;
        Assert.That(b.BangRadiusM / a.BangRadiusM, Is.EqualTo(expectedRatio).Within(0.001f),
            $"small={a.BangRadiusM} large={b.BangRadiusM}");
    }

    [Test]
    public void LivingEffect_ApplyPlan_OverridesBangAndTravel()
    {
        var words = new[] { new SentenceWord(Rune.Attack, JumpKind.None, 0) };
        var effect = new LivingEffect(Rune.Attack, 0, 0, 0, 1, words, new ManifestationTuning());
        // v6 1-2: Saldırı (projectile) → düz ilerleyen canlı efekt.
        LivingEffectPlan plan = SkillWorldPlanner.Build(
            Motor().Resolve(new[] { 1, 2 }), Catalog(), new ManifestationTuning());
        effect.ApplyPlan(plan);

        Assert.That(effect.HasSkillPlan, Is.True);
        Assert.That(effect.BangRadiusM, Is.EqualTo(plan.BangRadiusM).Within(0.01f));
        Assert.That(effect.TravelKind, Is.EqualTo(plan.TravelKind));
        effect.Tick(0.2f);
        Assert.That(effect.Travel, Is.GreaterThan(0f));
    }

    [Test]
    public void StatusApplicator_ApplySlow_FromV6EngineModifiers()
    {
        // v6 1-6: Saldırı × Bağlama → engine apply_slow (JSON'dan, elle sayı yok).
        SkillResolution skill = Motor().Resolve(new[] { 1, 6 });
        Assert.That(skill.Engine.Has("apply_slow"), Is.True);

        var target = new StatusBoard();
        var caster = new StatusBoard();
        StatusApplicator.ApplySkill(skill, caster, target, new StatusTuning());
        Assert.That(target.Has(StatusKind.Slow), Is.True);
        Assert.That(caster.Has(StatusKind.Slow), Is.False);
    }

    [Test]
    public void StatusApplicator_ApplyPull_Stealth_Confuse_FromModifiers()
    {
        var mods = MiniJson.Parse(
            "{\"apply_pull\":true,\"apply_stealth\":true,\"apply_confuse\":true}");
        var skill = SkillResolution.Build(
            "t", "Test", "Test", "t", "job",
            "v", "V", "strike", "damage",
            10f, 0f, "single_target", "free_move", Array.Empty<string>(),
            "cekme", "Çekme", "none",
            1f, 1f, 1f,
            2, "Temel", 1f, "free_move",
            string.Empty,
            engineModifiers: mods);

        var target = new StatusBoard();
        var caster = new StatusBoard();
        var result = StatusApplicator.ApplySkill(skill, caster, target, new StatusTuning());

        Assert.That(result.Pull, Is.True);
        Assert.That(caster.Has(StatusKind.Stealth), Is.True);
        Assert.That(target.Has(StatusKind.Blind), Is.True);
        Assert.That(target.Has(StatusKind.Slow), Is.True);
    }
}
