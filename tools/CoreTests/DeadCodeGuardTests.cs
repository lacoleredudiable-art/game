using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

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

    static string ReadProductionScripts()
    {
        var root = ScriptsRoot();
        var parts = new System.Text.StringBuilder();
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, file);
            if (rel.StartsWith("Tests", System.StringComparison.OrdinalIgnoreCase))
                continue;
            parts.Append(File.ReadAllText(file));
        }
        return parts.ToString();
    }

    [Test]
    public void CastPipeline_Events_HaveProductionSubscribers()
    {
        string pipelinePath = Path.Combine(ScriptsRoot(), "App", "Casting", "CastPipeline.cs");
        string pipeline = File.ReadAllText(pipelinePath);
        var events = new List<string>();
        foreach (Match match in Regex.Matches(pipeline, @"public event Action<[^>]+> (\w+);"))
            events.Add(match.Groups[1].Value);

        string production = ReadProductionScripts();
        Assert.Multiple(() =>
        {
            foreach (string name in events)
                Assert.That(production, Does.Contain(name + " +="), $"CastPipeline.{name} has no production subscriber");
        });
    }

    [Test]
    public void ActorRegistry_HasProductionReader()
    {
        string production = ReadProductionScripts();
        Assert.That(
            production,
            Does.Match(@"ActorRegistry\.(Get|TryGet)|BindActorRegistry\("));
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
