using System.IO;
using System.Collections.Generic;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class TargetingRulesTests
{
    static SkillMotor LoadMotor()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        return SkillMotor.FromJson(File.ReadAllText(path));
    }

    [Test]
    public void SelectedEnemyWinsOverNearerAutoTarget()
    {
        var candidates = new List<TargetCandidate>
        {
            new(10, TargetRelation.Enemy, 2f),
            new(20, TargetRelation.Enemy, 4f)
        };

        TargetResolution result = TargetingRules.Resolve(
            "enemy_only", SkillAimMode.Targeted, 5f, 20, candidates);

        Assert.That(result.Allowed, Is.True);
        Assert.That(result.TargetId, Is.EqualTo(20));
    }

    [Test]
    public void SelectedCompatibleTargetOutOfRangeRefusesInsteadOfRetargeting()
    {
        var candidates = new List<TargetCandidate>
        {
            new(10, TargetRelation.Enemy, 2f),
            new(20, TargetRelation.Enemy, 8f)
        };

        TargetResolution result = TargetingRules.Resolve(
            "enemy_only", SkillAimMode.Targeted, 5f, 20, candidates);

        Assert.That(result.Allowed, Is.False);
        Assert.That(result.Failure, Is.EqualTo(TargetFailure.OutOfRange));
    }

    [Test]
    public void EnemySkillWithoutSelectionAutoTargetsNearestInRange()
    {
        var candidates = new List<TargetCandidate>
        {
            new(10, TargetRelation.Enemy, 4f),
            new(20, TargetRelation.Enemy, 2f),
            new(30, TargetRelation.Enemy, 7f)
        };

        TargetResolution result = TargetingRules.Resolve(
            "enemy_only", SkillAimMode.Targeted, 5f, null, candidates);

        Assert.That(result.Allowed, Is.True);
        Assert.That(result.TargetId, Is.EqualTo(20));
    }

    [Test]
    public void EnemySkillWithoutEnemyInRangeIsRefused()
    {
        var candidates = new List<TargetCandidate>
        {
            new(10, TargetRelation.Enemy, 6f),
            new(20, TargetRelation.Ally, 1f)
        };

        TargetResolution result = TargetingRules.Resolve(
            "enemy_only", SkillAimMode.Targeted, 5f, null, candidates);

        Assert.That(result.Allowed, Is.False);
        Assert.That(result.Failure, Is.EqualTo(TargetFailure.NoTarget));
    }

    [Test]
    public void FriendlySkillUsesSelectedAllyThenNearestThenSelf()
    {
        var candidates = new List<TargetCandidate>
        {
            new(20, TargetRelation.Ally, 3f),
            new(21, TargetRelation.Ally, 1.5f),
            new(30, TargetRelation.Enemy, 1f)
        };

        TargetResolution selected = TargetingRules.Resolve(
            "self_or_ally", SkillAimMode.Targeted, 5f, 20, candidates);
        TargetResolution nearest = TargetingRules.Resolve(
            "self_or_ally", SkillAimMode.Targeted, 5f, 30, candidates);

        Assert.That(selected.Allowed, Is.True);
        Assert.That(selected.UseSelf, Is.False);
        Assert.That(selected.TargetId, Is.EqualTo(20));
        Assert.That(nearest.Allowed, Is.True);
        Assert.That(nearest.TargetId, Is.EqualTo(21), "seçili düşman dost skill'ini kendine çevirmez");

        var farSelected = new List<TargetCandidate>
        {
            new(20, TargetRelation.Ally, 8f),
            new(21, TargetRelation.Ally, 2f)
        };
        TargetResolution other = TargetingRules.Resolve(
            "self_or_ally", SkillAimMode.Targeted, 5f, 20, farSelected);
        Assert.That(other.TargetId, Is.EqualTo(21));

        var none = new List<TargetCandidate> { new(20, TargetRelation.Ally, 9f) };
        TargetResolution self = TargetingRules.Resolve(
            "self_or_ally", SkillAimMode.Targeted, 5f, 20, none);
        Assert.That(self.UseSelf, Is.True);

        TargetResolution shield = TargetingRules.Resolve(
            "self_only", SkillAimMode.Targeted, 5f, null, candidates, "shield");
        Assert.That(shield.UseSelf, Is.False);
        Assert.That(shield.TargetId, Is.EqualTo(21));
    }

    [Test]
    public void DashFallbackIsDirectionalWhileOtherSkillsDefaultTargeted()
    {
        SkillMotor motor = LoadMotor();
        SkillResolution dash = motor.Resolve(new[] { 3, 1 });
        SkillResolution strike = motor.Resolve(new[] { 1, 1 });

        Assert.That(TargetingRules.AimMode(dash), Is.EqualTo(SkillAimMode.Directional));
        Assert.That(TargetingRules.AimMode(strike), Is.EqualTo(SkillAimMode.Targeted));
        Assert.That(TargetingRules.IsHoming(TargetingRules.AimMode(strike)), Is.True);
        Assert.That(TargetingRules.IsHoming(TargetingRules.AimMode(dash)), Is.False);
    }
}
