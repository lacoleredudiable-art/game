using Dovus.Core;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using NUnit.Framework;
using System.IO;

namespace CoreTests;

[TestFixture]
public class RootAndSkillNumberTests
{
    [SetUp]
    public void ResetWarnings() => DesignWarnings.ResetForTests();

    [Test]
    public void Root_SameSourceRefreshes_DifferentSourcesKeepLongest_ThenExpires()
    {
        var board = new StatusBoard();
        var tuning = new StatusTuning();
        board.Apply(StatusKind.Root, 1000, 1f, "zone");
        board.Tick(400, tuning);
        board.Apply(StatusKind.Root, 1000, 1f, "zone");
        Assert.That(board.TryGet(StatusKind.Root, out double remaining, out _, out _), Is.True);
        Assert.That(remaining, Is.EqualTo(1000).Within(0.01), "aynı kaynak süreyi eklemez");

        board.Apply(StatusKind.Root, 2500, 1f, "skill");
        Assert.That(board.TryGet(StatusKind.Root, out remaining, out _, out _), Is.True);
        Assert.That(remaining, Is.EqualTo(2500).Within(0.01));

        board.Tick(1000, tuning);
        Assert.That(board.TryGet(StatusKind.Root, out remaining, out _, out _), Is.True);
        Assert.That(remaining, Is.EqualTo(1500).Within(0.01));

        board.Tick(1500, tuning);
        Assert.That(board.Has(StatusKind.Root), Is.False);
        Assert.That(board.IsRootImmune, Is.True);
    }

    [Test]
    public void Root_ImmunityBlocksReapply_ThenAllowsAgain()
    {
        var board = new StatusBoard();
        var tuning = new StatusTuning();
        board.Apply(StatusKind.Root, 200, 1f, "a");
        board.Tick(200, tuning);
        Assert.That(board.Has(StatusKind.Root), Is.False);
        board.Apply(StatusKind.Root, 200, 1f, "b");
        Assert.That(board.Has(StatusKind.Root), Is.False, "kök biter bitmez kısa bağışıklık");
        board.Tick(SkillNumberFallbacks.RootImmunityMs, tuning);
        Assert.That(board.IsRootImmune, Is.False);
        board.Apply(StatusKind.Root, 200, 1f, "b");
        Assert.That(board.Has(StatusKind.Root), Is.True);
    }

    [Test]
    public void SkillNumbers_ComeFromCanonicalJson()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "unity", "Assets", "Resources", "ElementSystem", "element-sistemi.json"));
        Assert.That(File.Exists(path), Is.True, path);
        string json = File.ReadAllText(path);
        JsonValue root = MiniJson.Parse(json);
        var catalog = SkillNumberCatalog.FromJson(json);

        float damage = root["verb_base"]["1"]["base_damage"].AsFloat(0f);
        float cooldown = root["verb_base"]["6"]["base_cooldown"].AsFloat(0f);
        float mana = root["verb_base"]["6"]["base_cost"].AsFloat(0f);
        float duration = root["verb_base"]["6"]["cc_duration_sec"].AsFloat(0f);
        float gcd = root["global_rules"]["cooldown_rules"]["global_cooldown_sec"].AsFloat(0f);
        float maxMana = root["global_rules"]["resource_system"]["max_mana"].AsFloat(0f);

        Assert.That(catalog.VerbDamageReference, Is.EqualTo(damage).Within(0.001f));
        Assert.That(catalog.TryGetVerb(6, out float d6, out float c6, out float m6, out float dur6, out float range6, out float radius6), Is.True);
        Assert.That(d6, Is.EqualTo(root["verb_base"]["6"]["base_damage"].AsFloat(0f)).Within(0.001f));
        Assert.That(c6, Is.EqualTo(cooldown).Within(0.001f));
        Assert.That(m6, Is.EqualTo(mana).Within(0.001f));
        Assert.That(dur6, Is.EqualTo(duration).Within(0.001f));
        Assert.That(range6, Is.EqualTo(4f).Within(0.001f), "kontrol konisi 4m");
        Assert.That(radius6, Is.GreaterThan(0f));
        Assert.That(catalog.GlobalCooldownSec, Is.EqualTo(gcd).Within(0.001f));
        Assert.That(catalog.MaxMana, Is.EqualTo(maxMana).Within(0.001f));
        Assert.That(catalog.RootImmunityMs, Is.EqualTo(SkillNumberFallbacks.RootImmunityMs).Within(0.001));
        Assert.That(DesignWarnings.WasWarned("mobility_cc.root_immunity_sec"), Is.True);

        var tuning = new StatusTuning();
        int oldRoot = tuning.RootMs;
        catalog.ApplyCcDurations(tuning);
        int jsonRootMs = (int)System.Math.Round(root["mobility_cc"]["cc_priority"].AsArray()[1]["duration_sec"].AsFloat(0f) * 1000.0);
        Assert.That(tuning.RootMs, Is.EqualTo(jsonRootMs));
        Assert.That(tuning.RootMs, Is.Not.EqualTo(oldRoot));

        SkillMotor motor = SkillMotor.FromJson(json);
        SkillResolution skill = motor.Resolve(new[] { 6, 6 });
        Assert.That(skill.EngineModifiers["cc_duration_sec"].AsFloat(0f), Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(skill.BaseCooldownSec, Is.EqualTo(skill.EngineModifiers["base_cooldown"].AsFloat(0f)).Within(0.001f));
        Assert.That(skill.BaseResourceCost, Is.EqualTo(skill.EngineModifiers["base_cost"].AsFloat(0f)).Within(0.001f));
        Assert.That(skill.BaseDamage, Is.EqualTo(skill.EngineModifiers["base_damage"].AsFloat(0f)).Within(0.001f));

        var board = new StatusBoard();
        StatusApplicator.ApplySkill(skill, new StatusBoard(), board, new StatusTuning());
        Assert.That(board.TryGet(StatusKind.Root, out double rootMs, out _, out _), Is.True);
        Assert.That(rootMs, Is.EqualTo(1500).Within(0.01));
    }
}
