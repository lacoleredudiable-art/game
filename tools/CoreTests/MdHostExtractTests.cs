using System.IO;
using System.Linq;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class MdHostExtractTests
{
    static string HostsDir =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts", "Game", "Skills", "Hosts"));

    static string SkillsDir =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts", "Game", "Skills"));

    [Test]
    public void Extracted_hosts_are_not_ManifestationDirector_partials()
    {
        var hostFiles = Directory.GetFiles(HostsDir, "Md*.cs");
        Assert.That(hostFiles.Length, Is.GreaterThanOrEqualTo(8));
        foreach (string path in hostFiles)
        {
            string text = File.ReadAllText(path);
            Assert.That(text, Does.Not.Contain("partial class ManifestationDirector"), Path.GetFileName(path));
            Assert.That(text, Does.Contain("namespace Dovus.Game.Skills.Hosts"), Path.GetFileName(path));
        }
    }

    [Test]
    public void ManifestationDirector_has_no_duplicate_delivery_arm_fields()
    {
        foreach (string path in Directory.GetFiles(SkillsDir, "ManifestationDirector*.cs"))
        {
            string text = File.ReadAllText(path);
            Assert.That(text, Does.Not.Contain("_deliverySkill"), Path.GetFileName(path));
            Assert.That(text, Does.Not.Contain("_deliveryPending"), Path.GetFileName(path));
        }
    }

    [Test]
    public void Md_host_files_are_under_600_lines_each()
    {
        foreach (string path in Directory.GetFiles(HostsDir, "Md*.cs"))
        {
            int lines = File.ReadAllLines(path).Length;
            Assert.That(lines, Is.LessThanOrEqualTo(600), () => $"{Path.GetFileName(path)} has {lines} lines");
        }
    }
}
