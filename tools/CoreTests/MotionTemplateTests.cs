using Dovus.Core;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using Dovus.Core.Shared;

namespace CoreTests;

[TestFixture]
public partial class MotionTemplateTests
{
    MotionTemplateCatalog _catalog;

    [SetUp]
    public void Load()
    {
        DesignWarnings.ResetForTests();
        _catalog = MotionTemplateCatalog.FromJson(File.ReadAllText(JsonPath()));
    }

    [Test]
    public void Catalog_ReadsBasicStrike_WithoutChangingTemplateCount()
    {
        Assert.That(_catalog.TemplateCount, Is.EqualTo(102));
        Assert.That(_catalog.BasicStrike, Is.Not.Null);
        Assert.That(_catalog.BasicStrike.Phases.Count, Is.EqualTo(1));
        Assert.That(_catalog.BasicStrike.Phases[0].Motion, Is.EqualTo("lunge"));
        Assert.That(_catalog.BasicStrike.Phases[0].DistanceM, Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(_catalog.BasicStrike.Phases[0].Homing, Is.EqualTo("none"));
        Assert.That(MotionAim.IsEnemy(_catalog.BasicStrike.Aim), Is.True);
    }

    [Test]
    public void BasicStrike_Lunge_StopsAtBody_AndCapsDistance()
    {
        var boss = new MotionTarget(true, 0f, 4f, 0.85f);
        var step = Play(_catalog.BasicStrike, 0.12f, boss, bodyRadius: 0.5f);
        Assert.That(MotionHitGeometry.EdgeGap(step.X, step.Z, 0.5f, 0f, 4f, 0.85f), Is.GreaterThan(0.05f));
        float travel = System.MathF.Sqrt(step.X * step.X + step.Z * step.Z);
        Assert.That(travel, Is.LessThanOrEqualTo(0.6f + 0.02f));
    }

    [Test]
    public void Catalog_MapsEveryCombo_AndAllFortyTwoFamilies()
    {
        Assert.That(_catalog.FamilyCount, Is.EqualTo(42));
        Assert.That(_catalog.TemplateCount, Is.EqualTo(102));
        Assert.That(_catalog.SkillCount, Is.EqualTo(144));
        Assert.That(_catalog.CountImplementedFamilies(), Is.EqualTo(42));
        Assert.That(_catalog.CountReadySkills(), Is.EqualTo(144));

        for (int verb = 1; verb <= 12; verb++)
        for (int adjective = 1; adjective <= 12; adjective++)
        {
            string id = verb + "-" + adjective;
            Assert.That(_catalog.TryGet((SkillId)id, out MotionBinding binding), Is.True, id);
            Assert.That(binding.Template.Phases.Count, Is.GreaterThan(0), id);
        }

        string docs = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "motion-templates.json"));
        Assert.That(docs, Is.EqualTo(File.ReadAllText(JsonPath())));
    }

    [Test]
    public void EverySkillInElementSystem_HasATemplateBinding()
    {
        var docsCatalog = MotionTemplateCatalog.FromJson(
            File.ReadAllText(Path.Combine(RepoRoot(), "docs", "motion-templates.json")));
        using var doc = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Path.Combine(RepoRoot(), "docs", "element-sistemi.json")));
        var entries = doc.RootElement.GetProperty("skills_prose_144").GetProperty("entries");

        var ids = new List<string>();
        foreach (var verb in entries.EnumerateObject())
        foreach (var skill in verb.Value.EnumerateObject())
            ids.Add(skill.Name);

        Assert.That(ids, Has.Count.EqualTo(144));
        var missing = ids.FindAll(id => !docsCatalog.TryGet((SkillId)id, out MotionBinding binding)
            || binding.Template == null || binding.Template.Phases.Count == 0);
        Assert.That(missing, Is.Empty);
    }

    [Test]
    public void Catalog_StoresTags_WithoutNeedingTheFamilyToBePlayable()
    {
        Assert.That(Tags("1-1"), Does.Contain(MotionTemplateCatalog.TagSilah));
        Assert.That(Tags("1-2"), Does.Contain(MotionTemplateCatalog.TagSinir));
        Assert.That(_catalog.TryGet((SkillId)"1-2", out MotionBinding claw), Is.True);
        Assert.That(claw.SinirThreshold, Is.EqualTo(0.2f).Within(0.001f));
        Assert.That(_catalog.TryGet((SkillId)"1-8", out MotionBinding lift), Is.True);
        Assert.That(lift.SinirThreshold, Is.EqualTo(0.1f).Within(0.001f));
        Assert.That(lift.HasTag(MotionTemplateCatalog.TagSilah), Is.True);
        Assert.That(Tags("1-10"), Does.Contain(MotionTemplateCatalog.TagPortal));
        Assert.That(Tags("4-10"), Does.Not.Contain(MotionTemplateCatalog.TagPortal));
        Assert.That(Tags("6-8"), Does.Contain(MotionTemplateCatalog.TagTakim));
        Assert.That(Tags("10-1"), Does.Contain(MotionTemplateCatalog.TagSilah));
        Assert.That(_catalog.TryPlay((SkillId)"10-1", out MotionTemplate parry), Is.True);
        Assert.That(parry.FamilyId, Is.EqualTo(28));
        Assert.That(Tags("3-10"), Does.Contain(MotionTemplateCatalog.TagPortal));
        Assert.That(Tags("3-10"), Does.Contain(MotionTemplateCatalog.TagTakim));
        Assert.That(Tags("5-4"), Does.Contain(MotionTemplateCatalog.TagTakim));
    }

    [Test]
    public void EveryCombo_ResolvesToAnImplementedTemplate()
    {
        int pending = 0;
        DesignWarnings.Warned += message =>
        {
            if (message != null && message.Contains("bekliyor", System.StringComparison.Ordinal))
                pending++;
        };

        for (int verb = 1; verb <= 12; verb++)
        for (int adjective = 1; adjective <= 12; adjective++)
        {
            string id = verb + "-" + adjective;
            Assert.That(_catalog.TryPlay((SkillId)id, out MotionTemplate template), Is.True, id);
            Assert.That(template.Implemented, Is.True, id);
            Assert.That(template.Phases.Count, Is.GreaterThan(0), id);
            Assert.That(template.Phases[0].Name, Is.Not.EqualTo("bekliyor"), id);
        }

        Assert.That(pending, Is.EqualTo(0));
        Assert.That(_catalog.TryPlay((SkillId)"0-0", out _), Is.False);
        Assert.That(DesignWarnings.WasWarned("motion.missing.0-0"), Is.True);
    }

    [Test]
    public void MissingPhaseDuration_WarnsOnce_AndUsesFallback()
    {
        const string json = """
        {"fallbacks":{"phase_sec":0.41,"step_m":1.2,"hit_length_m":1.5,"hit_radius_m":0.4,"max_hold_sec":0.9,"walk_mps":1.6,"height_m":0.9,"gap_m":0.9},
         "families":[{"id":1,"name":"Yükle ve bırak","implemented":true}],
         "templates":[{"id":"x","name":"x","family":1,"combos":[{"id":"1-1","tags":[]}],
           "phases":[{"name":"bos","motion":"hold"}]}]}
        """;
        int warnings = 0;
        DesignWarnings.Warned += message =>
        {
            if (message != null && message.Contains("faz süresi", System.StringComparison.Ordinal))
                warnings++;
        };
        var catalog = MotionTemplateCatalog.FromJson(json);
        MotionTemplateCatalog.FromJson(json);
        Assert.That(warnings, Is.EqualTo(1));
        Assert.That(catalog.TryPlay((SkillId)"1-1", out MotionTemplate template), Is.True);
        Assert.That(template.Phases[0].DurationSec, Is.EqualTo(0.41f).Within(0.001f));
    }
}


[TestFixture]
public class BasicStrikeSlotTests
{
    [Test]
    public void CenterStrike_UsesAttackRune_WhateverSitsInSlotOne()
    {
        var loadout = new RuneLoadout(new[] { 12, 1, 8, 6, 2, 5 });
        var slotted = new SentenceEngine(loadout: loadout);
        slotted.OnDotTouched(1, 0);
        Assert.That(slotted.State.Words[0].Rune, Is.EqualTo(Rune.Time), "slot 1 artık Zaman");

        var strike = new SentenceEngine(loadout: loadout);
        Assert.That(strike.BeginBasicStrike(1, 0), Is.True);
        Assert.That(strike.State.Words[0].Rune, Is.EqualTo(Rune.Attack));
        Assert.That(strike.State.Words[0].Slot, Is.EqualTo(0));
        strike.Commit();
        Assert.That(strike.History[0].Words[0].Rune, Is.EqualTo(Rune.Attack));
        Assert.That(strike.History[0].Words.Count, Is.EqualTo(1));

        Assert.That(strike.State.Phase, Is.EqualTo(SentencePhase.Recovering));
        Assert.That(strike.BeginBasicStrike(1, 50), Is.True, "toparlanma kilidini keser");
        Assert.That(strike.State.Words[0].Rune, Is.EqualTo(Rune.Attack));
    }

    [Test]
    public void AfterLoadout_FirstCenterTap_IsNotSwallowed()
    {
        Assert.That(BasicStrikeInput.AllowsCenterStrike(
            drawAllowed: false, loadoutJustApplied: true, engineAcceptsStrike: true), Is.True);
        Assert.That(BasicStrikeInput.AllowsCenterStrike(
            drawAllowed: false, loadoutJustApplied: false, engineAcceptsStrike: true), Is.False);
        Assert.That(BasicStrikeInput.DealsDamage(capsuleHit: false, enemyInEdgeReachAtImpact: true), Is.True);
        Assert.That(BasicStrikeInput.ReplaceStaleView(sentenceIsBasic: true, viewIsBasic: false), Is.True);
        Assert.That(BasicStrikeInput.ReplaceStaleView(sentenceIsBasic: true, viewIsBasic: true), Is.False);
    }
}
