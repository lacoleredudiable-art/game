using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Dovus.Core.Shared;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class SkillIdLiteralTests
{
    static readonly Regex SkillIdLiteral = new(@"""\d+-\d+""", RegexOptions.Compiled);

    static readonly string[] Roots =
    {
        "unity/Assets/Scripts/Core",
        "unity/Assets/Scripts/App",
        "unity/Assets/Scripts/Game",
        "unity/Assets/Scripts/Game/Editor",
    };

    static string RepoRoot =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public void SkillIdPatternLiterals_OnlyInSkillIdsClass()
    {
        string skillIdsPath = Path.Combine(RepoRoot, "unity", "Assets", "Scripts", "Core", "Shared", "SkillIds.cs");
        var offenders = new List<string>();
        foreach (string root in Roots)
        {
            string dir = Path.Combine(RepoRoot, root);
            if (!Directory.Exists(dir))
                continue;
            foreach (string file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.EndsWith("SkillIds.cs", StringComparison.Ordinal))
                    continue;
                if (file.Contains($"{Path.DirectorySeparatorChar}CoreTests{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                    continue;
                string text = File.ReadAllText(file);
                if (SkillIdLiteral.IsMatch(text))
                    offenders.Add(Path.GetRelativePath(RepoRoot, file));
            }
        }

        Assert.That(offenders, Is.Empty,
            () => "skill id string literals outside SkillIds.cs:\n" + string.Join("\n", offenders));
    }

    [Test]
    public void SkillIds_ValuesMatchElementIds()
    {
        Assert.That(SkillIds.DenseStrike, Is.EqualTo("1-1"));
        Assert.That(SkillIds.MirrorSummon, Is.EqualTo("11-10"));
        Assert.That(SkillIds.OpeningAscent, Is.EqualTo("8-6"));
    }
}
