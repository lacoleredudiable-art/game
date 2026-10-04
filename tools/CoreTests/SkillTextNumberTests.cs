using Dovus.Core;
using Dovus.Core.Data;
using Dovus.Core.Grammar;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CoreTests;

[TestFixture]
public class SkillTextNumberTests
{
    static readonly string[] ForbiddenEffectNumberSources =
    {
        "unity/Assets/Scripts/Core/Combat/CardEffectRules.cs",
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
