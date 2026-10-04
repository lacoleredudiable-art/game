using System.IO;
using System.Linq;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class ManifestationDirectorSizeTests
{
    static string SkillsDir =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts", "Game", "Skills"));

    [Test]
    public void Each_ManifestationDirector_partial_is_at_most_500_lines()
    {
        var files = Directory.GetFiles(SkillsDir, "ManifestationDirector*.cs");
        Assert.That(files.Length, Is.GreaterThan(0));
        foreach (string path in files)
        {
            int lines = File.ReadAllLines(path).Length;
            Assert.That(
                lines,
                Is.LessThanOrEqualTo(500),
                () => $"{Path.GetFileName(path)} has {lines} lines (cap 500)");
        }
    }

    [Test]
    public void ManifestationDirector_partials_total_ratchet()
    {
        var files = Directory.GetFiles(SkillsDir, "ManifestationDirector*.cs");
        int total = files.Sum(f => File.ReadAllLines(f).Length);
        TestContext.WriteLine($"ManifestationDirector partial total: {total} lines across {files.Length} files");
        Assert.That(total, Is.LessThanOrEqualTo(3375), "ratchet: 2B.24 host extract; yalniz asagi cekilir");
    }
}
