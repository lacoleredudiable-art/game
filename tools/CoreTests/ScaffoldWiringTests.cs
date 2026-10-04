using Dovus.App.Actors;
using Dovus.App.Casting;
using Dovus.App.Team;
using Dovus.Core.Actors;
using Dovus.Core.Casting;
using Dovus.Core.Shared;
using Dovus.Core.Tuning;
using Dovus.Game.Team;
using NUnit.Framework;
using System.IO;

namespace CoreTests;

[TestFixture]
public sealed class ScaffoldWiringTests
{
    static string ScriptsRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts"));

    [Test]
    public void TeamModifierHub_BindActorRegistry_UsesPlayerRegistryId()
    {
        var registry = new ActorRegistry();
        var health = new PlayerHealth();
        health.Bind(10);
        registry.Register(new PlayerActor(ActorDefaults.PlayerId, ActorTeam.Friendly, health));
        var hub = new TeamModifierHub { PlayerActorId = 99 };
        hub.BindActorRegistry(registry);
        Assert.That(hub.PlayerActorId, Is.EqualTo(ActorTargetKey.FromActorId(ActorDefaults.PlayerId)));
    }

    [Test]
    public void SkillNumberTuningApplier_WritesBasicStrikeRangeFromVerbOne()
    {
        string json = File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..")),
            "docs",
            "element-sistemi.json"));
        var catalog = SkillNumberCatalog.FromJson(json);
        var tuning = new ManifestationTuning();
        SkillNumberTuningApplier.ApplyBasicStrikeRange(catalog, tuning);
        Assert.That(tuning.BasicStrikeRangeM, Is.EqualTo(catalog.RangeM(1)).Within(0.0001f));
        Assert.That(tuning.BasicStrikeRadiusM, Is.EqualTo(catalog.RadiusM(1)).Within(0.0001f));
    }

    [Test]
    public void CastPipeline_DoesNotExpose_RemovedLifecycleEvents()
    {
        string pipelinePath = Path.Combine(ScriptsRoot(), "App", "Casting", "CastPipeline.cs");
        string pipeline = File.ReadAllText(pipelinePath);
        Assert.Multiple(() =>
        {
            Assert.That(pipeline, Does.Not.Contain("Started;"));
            Assert.That(pipeline, Does.Not.Contain("Completed;"));
            Assert.That(pipeline, Does.Not.Contain("BasicCompleted;"));
        });
    }
}
