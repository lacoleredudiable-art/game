using System.IO;
using System.Linq;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class CoreFileSizeTests
{
    static string CoreRoot =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts", "Core"));

    static void AssertPartialsAtMost500(string relativeDir, string fileStem)
    {
        string dir = Path.Combine(CoreRoot, relativeDir);
        var files = Directory.GetFiles(dir, $"{fileStem}*.cs");
        Assert.That(files.Length, Is.GreaterThan(0), () => $"no files for {fileStem} in {relativeDir}");
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
    public void MotionTemplateRunner_partials_are_at_most_500_lines() =>
        AssertPartialsAtMost500("Motion", "MotionTemplateRunner");

    [Test]
    public void PortalSystem_partials_are_at_most_500_lines() =>
        AssertPartialsAtMost500("Portal", "PortalSystem");

    [Test]
    public void MechanicGrammar_partials_are_at_most_500_lines() =>
        AssertPartialsAtMost500("Mechanic", "MechanicGrammar");

    [Test]
    public void TeamComboSystem_partials_are_at_most_500_lines() =>
        AssertPartialsAtMost500("Team", "TeamComboSystem");

    [Test]
    public void StatusBoard_partials_are_at_most_500_lines() =>
        AssertPartialsAtMost500("Status", "StatusBoard");

    [Test]
    public void SkillMotor_partials_are_at_most_500_lines() =>
        AssertPartialsAtMost500("Grammar", "SkillMotor");
}
