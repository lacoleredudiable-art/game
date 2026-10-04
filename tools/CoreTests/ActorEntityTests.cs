using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Dovus.App.Actors;
using Dovus.Core.Actors;
using Dovus.Core.Boss;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Shared;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class ActorEntityTests
{
    static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public void ActorRegistry_RegisterAndResolve()
    {
        var registry = new ActorRegistry();
        var health = new PlayerHealth();
        health.Bind(10);
        var player = new PlayerActor(ActorDefaults.PlayerId, ActorTeam.Friendly, health);
        registry.Register(player);

        Assert.That(registry.TryGet(ActorDefaults.PlayerId, out Actor found), Is.True);
        Assert.That(found, Is.SameAs(player));
        Assert.That(registry.Get(ActorDefaults.BossId), Is.Null);
    }

    [Test]
    public void TargetingRules_Resolve_UsesStableActorTargetKeys()
    {
        int bossKey = ActorTargetKey.FromActorId(ActorDefaults.BossId);
        var candidates = new List<TargetCandidate>
        {
            new TargetCandidate(bossKey, TargetRelation.Enemy, 4f, true),
        };
        TargetResolution result = TargetingRules.Resolve(
            "enemy_only",
            SkillAimMode.Targeted,
            10f,
            bossKey,
            candidates);
        Assert.That(result.Allowed, Is.True);
        Assert.That(result.TargetId, Is.EqualTo(bossKey));
    }

    [Test]
    public void AppAndCore_Sources_DoNotReferenceUnityTransform()
    {
        string scripts = Path.Combine(RepoRoot(), "unity", "Assets", "Scripts");
        var problems = new List<string>();
        foreach (string root in new[] { "App", "Core" })
        {
            string dir = Path.Combine(scripts, root);
            if (!Directory.Exists(dir))
                continue;
            foreach (string file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                if (text.Contains("Transform", StringComparison.Ordinal))
                    problems.Add(Path.GetRelativePath(scripts, file).Replace('\\', '/'));
            }
        }

        Assert.That(problems, Is.Empty,
            "App/Core Unity Transform yasak:" + Environment.NewLine + string.Join(Environment.NewLine, problems));
    }

    static readonly Regex GameTransformTargetField = new(
        @"Transform\s+_?[tT]arget\w*",
        RegexOptions.Compiled);

    /// <summary>2B.15a ölçümü 47; hedef alanları ActorId'ye kayarken ratchet.</summary>
    const int MaxGameTransformTargetFieldHits = 47;

    [Test]
    public void Game_TransformTargetFieldCount_BelowReportBaseline()
    {
        string gameDir = Path.Combine(RepoRoot(), "unity", "Assets", "Scripts", "Game");
        int count = 0;
        foreach (string file in Directory.EnumerateFiles(gameDir, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            count += GameTransformTargetField.Matches(text).Count;
        }

        Assert.That(count, Is.LessThan(MaxGameTransformTargetFieldHits),
            $"Transform hedef alanı sayısı {count}, tavan (exclusive) {MaxGameTransformTargetFieldHits}");
    }
}
