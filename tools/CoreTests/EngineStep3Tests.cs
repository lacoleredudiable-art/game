using Dovus.Core;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using NUnit.Framework;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace CoreTests;

[TestFixture]
public class EngineStep3Tests
{
    SkillMotor _motor;
    string _json;

    [SetUp]
    public void Load()
    {
        DesignWarnings.ResetForTests();
        _json = File.ReadAllText(JsonPath());
        _motor = SkillMotor.FromJson(_json);
    }

    [Test]
    public void AllySkillRange_IsSixMeters_FromJson_IgnoresWeapon()
    {
        var catalog = SkillNumberCatalog.FromJson(_json);
        Assert.That(catalog.AllySkillRangeM, Is.EqualTo(6f).Within(0.001f));
        Assert.That(DesignWarnings.WasWarned("global_rules.ally_skill_range_m"), Is.False);

        float weaponRange = 0.8f;
        Assert.That(
            CardEffectRules.ResolveRange(true, catalog.AllySkillRangeM, weaponRange),
            Is.EqualTo(6f).Within(0.001f));
        Assert.That(
            CardEffectRules.ResolveRange(false, catalog.AllySkillRangeM, weaponRange),
            Is.EqualTo(weaponRange).Within(0.001f));

        var allies = new[]
        {
            new TargetCandidate(1, TargetRelation.Ally, 5f),
            new TargetCandidate(2, TargetRelation.Ally, 2f)
        };
        TargetResolution selected = TargetingRules.Resolve(
            "self_or_ally", SkillAimMode.Targeted, catalog.AllySkillRangeM, 1, allies, "heal");
        Assert.That(selected.Allowed, Is.True);
        Assert.That(selected.UseSelf, Is.False);
        Assert.That(selected.TargetId, Is.EqualTo(1), "seçili dost, daha yakını ezer");

        TargetResolution nearest = TargetingRules.Resolve(
            "self_or_ally", SkillAimMode.Targeted, catalog.AllySkillRangeM, null, allies, "heal");
        Assert.That(nearest.TargetId, Is.EqualTo(2));

        var far = new[] { new TargetCandidate(1, TargetRelation.Ally, 7f) };
        TargetResolution self = TargetingRules.Resolve(
            "self_or_ally", SkillAimMode.Targeted, catalog.AllySkillRangeM, 1, far, "shield");
        Assert.That(self.UseSelf, Is.True, "menzil dışı dostta kendine düşer");
    }

    [Test]
    public void AllySkillRange_Missing_WarnsOnceAndUsesFallback()
    {
        string broken = Regex.Replace(_json, "\"ally_skill_range_m\":\\s*6,\\s*", "");
        var catalog = SkillNumberCatalog.FromJson(broken);
        Assert.That(catalog.AllySkillRangeM, Is.EqualTo(SkillNumberFallbacks.AllySkillRangeM).Within(0.001f));
        Assert.That(DesignWarnings.WasWarned("global_rules.ally_skill_range_m"), Is.True);

        DesignWarnings.ResetForTests();
        int warnings = 0;
        DesignWarnings.Warned += message =>
        {
            if (message != null && message.Contains("ally_skill_range_m", System.StringComparison.Ordinal))
                warnings++;
        };
        SkillNumberCatalog.FromJson(broken);
        SkillNumberCatalog.FromJson(broken);
        Assert.That(warnings, Is.EqualTo(1));
    }

    [Test]
    public void YogunZaman_CardSaysThirtyPercent_SpeedStaysSeventy()
    {
        SkillResolution card = _motor.Resolve(new[] { 12, 1 });
        TempoCast cast = TempoCast.From(card);
        Assert.That(card.SkillJob, Does.Contain("hızı %30 düşer"));
        Assert.That(card.SkillJob, Does.Not.Contain("%70"));
        Assert.That(cast.EnemySlow, Is.True);
        Assert.That(cast.SlowStrength, Is.EqualTo(0.7f).Within(0.001f), "boss normal hızın %70'i");
        Assert.That(1f - cast.SlowStrength, Is.EqualTo(0.3f).Within(0.001f));
    }

    [Test]
    public void YukselenZaman_HasteLastsThreeSeconds_AndCardSaysSo()
    {
        SkillResolution card = _motor.Resolve(new[] { 12, 8 });
        TempoCast cast = TempoCast.From(card);
        Assert.That(card.SkillJob, Does.Contain("3 sn"));
        Assert.That(card.SkillJob, Does.Contain("+%50"));
        Assert.That(cast.SelfHaste, Is.True);
        Assert.That(cast.HasteStrength, Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(cast.DurationMs, Is.EqualTo(3000).Within(0.01));
        Assert.That(
            card.EngineModifiers["buff_duration_sec"].AsFloat(0f),
            Is.EqualTo(3f).Within(0.001f));
        Assert.That(
            card.EngineModifiers["tempo_duration_sec"].AsFloat(0f),
            Is.EqualTo(3f).Within(0.001f));
    }

    [Test]
    public void SlowAndHasteCards_PercentMatchesTheSpeedTheyApply()
    {
        var mismatches = new System.Text.StringBuilder();
        for (int verb = 1; verb <= 12; verb++)
        {
            for (int adj = 1; adj <= 12; adj++)
            {
                SkillResolution skill = _motor.Resolve(new[] { verb, adj });
                string text = skill.SkillJob ?? string.Empty;
                if (TryPercentBefore(text, "düşer", out int drop))
                {
                    float mult = skill.EngineModifiers["enemy_slow"].AsFloat(0f);
                    int expected = (int)System.Math.Round((1f - mult) * 100f);
                    if (mult <= 0f || mult >= 1f || drop != expected)
                        mismatches.AppendLine($"{skill.SkillId} {skill.DisplayName}: kart %{drop} düşer, hız çarpanı {mult}");
                }

                if (CardEffectRules.WantsSelfHaste(text) && TrySignedPercent(text, out int bonus))
                {
                    float haste = CardEffectRules.HasteMagnitude(
                        text,
                        skill.EngineModifiers["self_haste"].AsFloat(0f),
                        skill.EngineModifiers["enemy_slow"].AsFloat(0f),
                        skill.EngineModifiers["self_damage_buff"].AsFloat(0f));
                    int applied = (int)System.Math.Round((haste - 1f) * 100f);
                    if (bonus != applied)
                        mismatches.AppendLine($"{skill.SkillId} {skill.DisplayName}: kart +%{bonus}, uygulanan +%{applied}");
                }
            }
        }

        Assert.That(mismatches.ToString(), Is.Empty);
    }

    static bool TryPercentBefore(string text, string word, out int percent)
    {
        percent = 0;
        if (string.IsNullOrEmpty(text))
            return false;
        Match match = Regex.Match(text, @"%(\d+)\s*" + word, RegexOptions.CultureInvariant);
        return match.Success
            && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out percent);
    }

    static bool TrySignedPercent(string text, out int percent)
    {
        percent = 0;
        Match match = Regex.Match(text, @"\+%(\d+)", RegexOptions.CultureInvariant);
        return match.Success
            && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out percent);
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
