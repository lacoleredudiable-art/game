using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class MdHostInterfaceSizeTests
{
    static string SkillsDir =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts", "Game", "Skills"));

    static int CountInterfaceMembers(string fileName)
    {
        string path = Path.Combine(SkillsDir, fileName);
        string text = File.ReadAllText(path);
        int props = Regex.Matches(text, @"^\s+\w+.*\{\s*get", RegexOptions.Multiline).Count;
        int methods = Regex.Matches(text, @"^\s+\w+.*\([^;]*\)\s*;", RegexOptions.Multiline).Count;
        return props + methods;
    }

    [Test]
    public void IMdMechanicsHost_has_at_most_15_members() =>
        Assert.That(CountInterfaceMembers(Path.Combine("Mechanics", "IMdMechanicsHost.cs")), Is.LessThanOrEqualTo(15));

    [Test]
    public void ISkillExecutorLaunchHost_has_at_most_15_members() =>
        Assert.That(CountInterfaceMembers(Path.Combine("Launch", "ISkillExecutorLaunchHost.cs")), Is.LessThanOrEqualTo(15));

    [Test]
    public void IWeaponPassiveRuntimeHost_has_at_most_15_members() =>
        Assert.That(CountInterfaceMembers(Path.Combine("Weapons", "IWeaponPassiveRuntimeHost.cs")), Is.LessThanOrEqualTo(15));
}
