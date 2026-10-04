using Dovus.Core.Shared;

﻿using Dovus.Core;
using Dovus.Core.Data;
using Dovus.Core.Grammar;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CoreTests;

[TestFixture]
public class SkillTextNumberTests
{
    static readonly string[] ForbiddenEffectNumberSources =
    {
        "unity/Assets/Scripts/Core/Status/CardEffectRules.cs",
    };

    static readonly string[] ForbiddenCompatibilityLiteralSources =
    {
        "unity/Assets/Scripts/Game/Hud/SkillPreviewHud.cs",
    };

    [Test]
    public void AllSkillTexts_NumbersMatchEngineValues()
    {
        string json = File.ReadAllText(JsonPath());
        JsonValue root = MiniJson.Parse(json);
        var mismatches = new StringBuilder();

        foreach (JsonValue verbBlock in root["skills"]["by_verb"].AsObject().Values)
        {
            foreach (JsonValue skill in verbBlock["skills"].AsArray())
            {
                string id = skill["id"].AsString();
                JsonValue engine = skill["engine"];
                List<double> engineNumbers = SkillTextNumbers.CollectEngineNumbers(engine);

                CheckField(id, "effect", skill["effect"].AsString(), engineNumbers, mismatches);
                CheckField(id, "prose_mechanic", skill["prose_mechanic"].AsString(), engineNumbers, mismatches);
            }
        }

        Assert.That(mismatches.ToString(), Is.Empty, mismatches.ToString());
    }

    [Test]
    public void AllPassiveSlotTexts_MatchRuneDuration()
    {
        string json = File.ReadAllText(JsonPath());
        JsonValue root = MiniJson.Parse(json);
        var durations = new Dictionary<int, float>();
        var faces = new Dictionary<int, string>();
        foreach (JsonValue rune in root["runes"].AsArray())
        {
            int id = rune["id"].AsInt();
            durations[id] = rune["passive_duration_default"].AsFloat(0f);
            faces[id] = rune["adjective_face"].AsString();
        }

        var mismatches = new StringBuilder();
        foreach (JsonValue verbBlock in root["skills"]["by_verb"].AsObject().Values)
        {
            foreach (JsonValue skill in verbBlock["skills"].AsArray())
            {
                string passive = skill["passive"].AsString();
                string id = skill["id"].AsString();
                int adjId = int.Parse(id.Split('-')[1], CultureInfo.InvariantCulture);
                if (!passive.Contains(" sn pasif: ", StringComparison.Ordinal))
                {
                    // "{N} sn boyunca ..." / "{N} sn: ...": baştaki süre de sıfat rününün süresi olmalı.
                    var lead = System.Text.RegularExpressions.Regex.Match(passive, @"^(\d+(?:\.\d+)?) sn[ :]");
                    if (lead.Success)
                    {
                        string want = SkillTextNumbers.FormatDurationSeconds(durations[adjId]);
                        if (lead.Groups[1].Value != want)
                            mismatches.AppendLine($"{id}: expected leading '{want} sn', got '{passive}'");
                    }
                    continue;
                }

                string adjective = skill.Has("adjective")
                    ? skill["adjective"].AsString()
                    : faces[adjId];
                string expected = SkillTextNumbers.PassiveText(adjective, durations[adjId]);
                if (passive != expected)
                    mismatches.AppendLine($"{id}: expected '{expected}', got '{passive}'");
            }
        }

        Assert.That(mismatches.ToString(), Is.Empty, mismatches.ToString());
    }

    [Test]
    public void SkillPreviewHud_HasNoHardcodedCompatibilityMultipliers()
    {
        string repoRoot = RepoRoot();
        var violations = new StringBuilder();
        foreach (string relative in ForbiddenCompatibilityLiteralSources)
        {
            string path = Path.Combine(repoRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            string text = File.ReadAllText(path);
            if (text.Contains("×1.2", StringComparison.Ordinal))
                violations.AppendLine($"{relative}: sabit ×1.2");
            if (text.Contains("×0.8", StringComparison.Ordinal))
                violations.AppendLine($"{relative}: sabit ×0.8");
            if (text.Contains("Cast ×", StringComparison.Ordinal))
                violations.AppendLine($"{relative}: sabit Cast ×");
        }

        Assert.That(violations.ToString(), Is.Empty, violations.ToString());
    }

    [Test]
    public void GameAndCore_DoNotParseNumbersFromSkillEffectText()
    {
        string repoRoot = RepoRoot();
        var violations = new StringBuilder();
        foreach (string relative in ForbiddenEffectNumberSources)
        {
            string path = Path.Combine(repoRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            string text = File.ReadAllText(path);
            if (text.Contains("PercentBonus", StringComparison.Ordinal))
                violations.AppendLine($"{relative}: PercentBonus kaldırılmalı");
        }

        Assert.That(violations.ToString(), Is.Empty, violations.ToString());
    }

    static void CheckField(
        string skillId,
        string field,
        string text,
        List<double> engineNumbers,
        StringBuilder mismatches)
    {
        foreach (TextNumber number in SkillTextNumbers.Extract(text))
        {
            if (!SkillTextNumbers.Matches(number, engineNumbers))
            {
                mismatches.AppendLine(
                    $"{skillId} {field}: {number.Kind} {number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)} engine'de yok");
            }
        }
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

    static string RepoRoot()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", ".."));
        return path;
    }
}
