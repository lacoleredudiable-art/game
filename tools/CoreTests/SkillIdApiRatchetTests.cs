using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class SkillIdApiRatchetTests
{
    const int MaxPublicStringSkillIdParameters = 0;

    static readonly Regex PublicStringSkillIdParam = new(
        @"public [^(]*\([^)]*string skillId",
        RegexOptions.Compiled);

    static string ScriptsRoot =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts"));

    [Test]
    public void PublicApis_DoNotTakeRawStringSkillId()
    {
        int count = 0;
        foreach (string file in Directory.EnumerateFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            count += PublicStringSkillIdParam.Matches(text).Count;
        }

        Assert.That(count, Is.LessThanOrEqualTo(MaxPublicStringSkillIdParameters),
            () => $"found {count} public methods with string skillId (max {MaxPublicStringSkillIdParameters})");
    }
}
