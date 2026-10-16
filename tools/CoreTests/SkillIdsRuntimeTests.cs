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
    public void JsonSwapCancelTags_MatchSliceGrammar()
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

}
