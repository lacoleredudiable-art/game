using System;
using System.IO;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Presentation;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using NUnit.Framework;

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
    public void Pair_AtesTopu_Vs_Triple_Yayma_BangRadiusDiffers()
    {
        // 1-1 Keskinlik hitbox_scale 0.3; 1-1-2 Yayma 2.5 (+ expanding_wave shim)
        SkillMotor m = Motor();
        PresentationCatalog cat = Catalog();
        var tuning = new ManifestationTuning();

        LivingEffectPlan pair = SkillWorldPlanner.Build(m.Resolve(new[] { 1, 1 }), cat, tuning);
        LivingEffectPlan triple = SkillWorldPlanner.Build(m.Resolve(new[] { 1, 1, 2 }), cat, tuning);

        Assert.That(pair.HasPlan, Is.True);
        Assert.That(triple.HasPlan, Is.True);
        Assert.That(triple.BangRadiusM, Is.GreaterThan(pair.BangRadiusM * 3f),
            $"pair={pair.BangRadiusM} triple={triple.BangRadiusM}");
        Assert.That(triple.TravelKind, Is.EqualTo(LivingTravelKind.ExpandingRadial));
        Assert.That(triple.Silhouette.Spread, Is.GreaterThan(pair.Silhouette.Spread));
    }

    [Test]
    public void Yayma_HitboxOverride_ExpandingWave_TreatedAsTrajectory()
    {
        SkillResolution r = Motor().Resolve(new[] { 1, 1, 2 });
        Assert.That(r.AdjectiveId, Is.EqualTo("yayma"));
        // JSON düzeltmesi: trajectory_override=expanding_wave, hitbox=radial_burst
        SkillWorldPlanner.ResolveIds(r, Catalog(), out string traj, out string hb);
        Assert.That(traj, Is.EqualTo("expanding_wave"));
        Assert.That(hb, Is.EqualTo("radial_burst"));
    }

    [Test]
    public void LengthEconomy_Triple_ResourceAndCastAndMobility()
    {
        SkillResolution pair = Motor().Resolve(new[] { 1, 1 });
        SkillResolution triple = Motor().Resolve(new[] { 1, 1, 2 });
        Assert.That(triple.LengthResourceCostMult, Is.EqualTo(1.5f).Within(0.01f));
        Assert.That(triple.LengthCastMult, Is.EqualTo(1.4f).Within(0.01f));
        Assert.That(SkillMobility.ResourceCost(triple),
            Is.EqualTo(pair.BaseResourceCost * 1.5f).Within(0.01f));
        Assert.That(SkillMobility.CastTimeMult(triple), Is.EqualTo(1.4f).Within(0.01f));
        Assert.That(SkillMobility.Resolve(triple), Is.EqualTo(SkillMobility.SlowedMove));
    }

    [Test]
    public void LengthEconomy_Quad_IsRooted()
    {
        SkillResolution quad = Motor().Resolve(new[] { 1, 1, 1, 1 });
        Assert.That(quad.LengthCastMult, Is.EqualTo(2.0f).Within(0.01f));
        Assert.That(SkillMobility.Resolve(quad), Is.EqualTo(SkillMobility.Rooted));
    }

    [Test]
    public void FromSkill_UsesAdjectiveAxis_NotIntermediateRunes()
    {
        SkillResolution r = Motor().Resolve(new[] { 1, 1, 2 }); // Alev + Yayma (wave)
        EffectSilhouette s = SilhouetteBuilder.FromSkill(r, new ManifestationTuning());
        Assert.That(s.Spread, Is.GreaterThan(0.3f));
        Assert.That(r.SilhouetteAxis, Is.EqualTo("wave"));
    }

    [Test]
    public void LivingEffect_ApplyPlan_OverridesBangAndTravel()
    {
        var words = new[] { new SentenceWord(Rune.Ates, JumpKind.None, 0) };
        var effect = new LivingEffect(Rune.Ates, 0, 0, 0, 1, words, new ManifestationTuning());
        LivingEffectPlan plan = SkillWorldPlanner.Build(
            Motor().Resolve(new[] { 1, 1, 2 }), Catalog(), new ManifestationTuning());
        effect.ApplyPlan(plan);

        Assert.That(effect.HasSkillPlan, Is.True);
        Assert.That(effect.BangRadiusM, Is.EqualTo(plan.BangRadiusM).Within(0.01f));
        Assert.That(effect.TravelKind, Is.EqualTo(LivingTravelKind.ExpandingRadial));
        effect.Tick(0.2f);
        Assert.That(effect.Travel, Is.GreaterThan(0f));
    }

    [Test]
    public void StatusApplicator_ApplySlow_FromAdjectiveModifiers()
    {
        // 1-4 Lav skill kartı → sıfat agirlik (apply_slow: 0.4)
        SkillResolution skill = Motor().Resolve(new[] { 1, 4 });
        Assert.That(skill.AdjectiveId, Is.EqualTo("agirlik"));
        Assert.That(skill.EngineModifiers.Has("apply_slow"), Is.True);

        var target = new StatusBoard();
        var caster = new StatusBoard();
        StatusApplicator.ApplySkill(skill, caster, target, new StatusTuning());
        Assert.That(target.Has(StatusKind.Slow), Is.True);
    }

    [Test]
    public void StatusApplicator_ApplyPull_Stealth_Confuse_FromModifiers()
    {
        var mods = MiniJson.Parse(
            "{\"apply_pull\":true,\"apply_stealth\":true,\"apply_confuse\":true}");
        var skill = new SkillResolution(
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
