using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class SkillIdsRuntimeTests
{
    static readonly Regex SkillIdsReference = new(@"\bSkillIds\.", RegexOptions.Compiled);

    static readonly HashSet<string> AllowedRelativePaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "unity/Assets/Scripts/Core/Shared/SkillIds.cs",
        "unity/Assets/Scripts/Game/DevTools/TeamDebugHud.cs",
    };

    static string RepoRoot =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public void CoreAppGame_Runtime_DoNotReferenceSkillIds()
    {
        string scripts = Path.Combine(RepoRoot, "unity", "Assets", "Scripts");
        var offenders = new List<string>();
        foreach (string root in new[] { "Core", "App", "Game" })
        {
            string dir = Path.Combine(scripts, root);
            if (!Directory.Exists(dir))
                continue;
            foreach (string file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(RepoRoot, file).Replace('\\', '/');
                if (rel.Contains("/Game/Editor/", StringComparison.Ordinal))
                    continue;
                if (rel.Contains("/Game/DevTools/", StringComparison.Ordinal) && !AllowedRelativePaths.Contains(rel))
                    continue;
                if (AllowedRelativePaths.Contains(rel))
                    continue;
                if (SkillIdsReference.IsMatch(File.ReadAllText(file)))
                    offenders.Add(rel);
            }
        }

        Assert.That(offenders, Is.Empty,
            () => "SkillIds. runtime kullanımı (DevTools allowlist dışı):\n" + string.Join("\n", offenders));
    }

    [Test]
    public void JsonSwapCancelTags_MatchExpectedTen()
    {
        SkillMotor motor = SkillMechanicTagTests.LoadMotorPublic();
        var tagged = new List<string>();
        motor.ForEachSkill((id, skill) =>
        {
            if (new Dovus.Core.Grammar.SkillEngineModifiers(skill.Engine).SwapCancel())
                tagged.Add(id);
        });
        tagged.Sort(StringComparer.Ordinal);
        var expected = new List<string>(SkillMechanicTagTests.ExpectedSwapCancelSkillIds);
        expected.Sort(StringComparer.Ordinal);
        Assert.That(tagged, Is.EqualTo(expected));
    }

    [Test]
    public void JsonSustainedTags_CoverFlowingWeaponPassives()
    {
        SkillMotor motor = SkillMechanicTagTests.LoadMotorPublic();
        string[] flowing = { "1-12", "2-12", "5-12", "8-12", "12-12" };
        foreach (string id in flowing)
        {
            Assert.That(motor.TryGetSkill(id, out var entry), Is.True, id);
            Assert.That(new Dovus.Core.Grammar.SkillEngineModifiers(entry.Engine).Sustained(), Is.True, id);
        }
    }

    [Test]
    public void JsonBorderModeTags_CoverBorderTiers()
    {
        SkillMotor motor = SkillMechanicTagTests.LoadMotorPublic();
        Assert.That(motor.TryGetSkill("1-2", out var t20), Is.True);
        Assert.That(Dovus.Core.Border.BorderMode.TryTier(
            new Dovus.Core.Grammar.SkillEngineModifiers(t20.Engine),
            out float th, out _, out _, out _), Is.True);
        Assert.That(th, Is.EqualTo(Dovus.Core.Border.BorderMode.Tier20).Within(0.001f));
        Assert.That(motor.TryGetSkill("12-8", out var col), Is.True);
        var eng = new Dovus.Core.Grammar.SkillEngineModifiers(col.Engine);
        Assert.That(Dovus.Core.Border.BorderModeTier.OpensColumn(eng), Is.True);
    }
}
