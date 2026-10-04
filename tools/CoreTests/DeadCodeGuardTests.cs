using NUnit.Framework;
using System.IO;

namespace CoreTests;

/// <summary>PLAN 2B.1: kaldırılan ölü üyelerin Scripts altına geri sızmasını engeller.</summary>
[TestFixture]
public sealed class DeadCodeGuardTests
{
    static string ScriptsRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts"));

    static string ReadAllScriptsText()
    {
        var root = ScriptsRoot();
        var parts = new System.Text.StringBuilder();
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            parts.Append(File.ReadAllText(file));
        return parts.ToString();
    }

    [Test]
    public void ActorModifiers_DoesNotExpose_MissChance()
    {
        string path = Path.Combine(ScriptsRoot(), "App", "Team", "ActorModifiers.cs");
        string text = File.ReadAllText(path);
        Assert.That(text, Does.Not.Contain("MissChance"));
    }

    [Test]
    public void Scripts_DoNotReference_RemovedDeadMembers()
    {
        string all = ReadAllScriptsText();
        Assert.Multiple(() =>
        {
            Assert.That(all, Does.Not.Contain("HasModifierFor"));
            Assert.That(all, Does.Not.Contain("BossVolleyData"));
            Assert.That(all, Does.Not.Contain("SetFloorMaterial"));
            Assert.That(all, Does.Not.Contain("LogWeaponPropVerification"));
            Assert.That(all, Does.Not.Contain("AnimationDamageSchedule"));
        });
    }

    [Test]
    public void Scripts_DoNotReference_RemovedHudAndVfxHelpers()
    {
        string all = ReadAllScriptsText();
        Assert.Multiple(() =>
        {
            Assert.That(all, Does.Not.Contain("GradeLabel("));
            Assert.That(all, Does.Not.Contain("KenneyVfxTextures.ForRune"));
            Assert.That(all, Does.Not.Contain("DamageFrameReached"));
        });
    }
}
