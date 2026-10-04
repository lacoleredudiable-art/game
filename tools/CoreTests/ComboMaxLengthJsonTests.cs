using System.IO;
using Dovus.Core.Shared;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

/// <summary>PLAN 2B.21 — combo_system.current_max_length JSON ↔ SkillMotor (açık değer 2).</summary>
[TestFixture]
public class ComboMaxLengthJsonTests
{
    const int ExpectedCurrentMaxLength = 2;

    static string DocsJsonPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        }

        Assert.That(File.Exists(path), Is.True, $"element-sistemi.json bulunamadı: {path}");
        return path;
    }

    [Test]
    public void ElementSistemiJson_combo_system_current_max_length_IsExplicit2()
    {
        JsonValue root = MiniJson.Parse(File.ReadAllText(DocsJsonPath()));
        int fromJson = root["combo_system"]["current_max_length"].AsInt(-1);
        Assert.That(fromJson, Is.EqualTo(ExpectedCurrentMaxLength));
    }

    [Test]
    public void SkillMotor_MaxComboLength_EqualsParsedJsonField_NotDefault4()
    {
        string json = File.ReadAllText(DocsJsonPath());
        int fromJson = MiniJson.Parse(json)["combo_system"]["current_max_length"].AsInt(-1);
        Assert.That(fromJson, Is.EqualTo(ExpectedCurrentMaxLength));

        SkillMotor motor = SkillMotor.FromJson(json);
        Assert.That(motor.MaxComboLength, Is.EqualTo(ExpectedCurrentMaxLength));
        Assert.That(motor.MaxComboLength, Is.Not.EqualTo(4), "SentenceTuning varsayılanı 4; JSON 2 olmalı");
    }
}
