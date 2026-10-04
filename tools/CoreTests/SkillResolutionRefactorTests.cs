using System.IO;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class SkillResolutionRefactorTests
{
    [Test]
    public void GenerateSnapshot_WritesBaselineFile()
    {
        string path = SkillResolutionSnapshotUtil.SnapshotPath();
        SkillResolutionSnapshotUtil.WriteSnapshotFile(path, SkillResolutionSnapshotUtil.LoadMotor());
        Assert.That(File.Exists(path), Is.True);
    }

    [Test]
    public void All144Combos_MatchStringSnapshot()
    {
        string path = SkillResolutionSnapshotUtil.SnapshotPath();
        Assert.That(File.Exists(path), Is.True, "skill-resolution-snapshot.tsv eksik; önce GenerateSnapshot çalıştır.");
        SkillMotor motor = SkillResolutionSnapshotUtil.LoadMotor();
        string[] blocks = File.ReadAllText(path).Split(new[] { "---\n", "---\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        Assert.That(blocks.Length, Is.EqualTo(144));
        int index = 0;
        for (int verb = 1; verb <= 12; verb++)
        for (int adj = 1; adj <= 12; adj++)
        {
            SkillResolution skill = motor.Resolve(new[] { verb, adj });
            string expected = blocks[index].Trim();
            string actualComboHeader = $"combo\t{verb}\t{adj}";
            Assert.That(expected.StartsWith(actualComboHeader, System.StringComparison.Ordinal), Is.True, $"block {index}");
            string expectedBody = expected.Substring(expected.IndexOf('\n') + 1);
            string actual = SkillResolutionSnapshotUtil.FlattenForAssert(skill).Replace("\r\n", "\n");
            string expectedNorm = expectedBody.Replace("\r\n", "\n");
            Assert.That(actual, Is.EqualTo(expectedNorm));
            index++;
        }
    }
}
