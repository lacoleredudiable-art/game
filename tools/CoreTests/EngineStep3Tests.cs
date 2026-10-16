using Dovus.Core;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
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
        _json = SliceGrammarTestPaths.ReadSliceGrammarJson();
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
    public void SlowAndHasteCards_PercentMatchesTheSpeedTheyApply()
    {
        var mismatches = new System.Text.StringBuilder();
        for (int verb = 1; verb <= 12; verb++)
        {
            for (int adj = 1; adj <= 12; adj++)
            {
                SkillResolution skill = _motor.Resolve(new[] { verb, adj });
                string text = skill.Identity.SkillJob ?? string.Empty;
                if (TryPercentBefore(text, "düşer", out int drop))
                {
                    float mult = skill.Engine.Field("enemy_slow").AsFloat(0f);
                    int expected = (int)System.Math.Round((1f - mult) * 100f);
                    if (mult <= 0f || mult >= 1f || drop != expected)
                        mismatches.AppendLine($"{skill.Identity.Id} {skill.Identity.DisplayName}: kart %{drop} düşer, hız çarpanı {mult}");
                }

                if (CardEffectRules.WantsSelfHaste(text) && TrySignedPercent(text, out int bonus))
                {
                    float haste = CardEffectRules.HasteMagnitude(
                        text,
                        skill.Engine.Field("self_haste").AsFloat(0f),
                        skill.Engine.Field("enemy_slow").AsFloat(0f),
                        skill.Engine.Field("self_damage_buff").AsFloat(0f));
                    int applied = (int)System.Math.Round((haste - 1f) * 100f);
                    if (bonus != applied)
                        mismatches.AppendLine($"{skill.Identity.Id} {skill.Identity.DisplayName}: kart +%{bonus}, uygulanan +%{applied}");
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

}
