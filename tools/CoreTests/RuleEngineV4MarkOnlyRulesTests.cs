using System.IO;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4MarkOnlyRulesTests
{
    const int Kilic = 4;
    const int Yay = 2;

    static RuleEngineV4Catalog Catalog() =>
        RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json"))));

    static CommandPlan MarkPlan(RuleEngineV4Catalog c, int verb, int adj, int weapon) =>
        new RuleEngineV4Planner(c).Plan(verb, adj, weapon, new RuleEngineV4CastContext(
            hasValidTarget: true, targetInWeaponRange: true, hasUsableMark: true));

    [Test]
    public void IsaretKoy_PlacesMark_WithinFriendlyCap()
    {
        RuleEngineV4Catalog c = Catalog();
        CommandPlan plan = MarkPlan(c, 9, 9, Yay);
        Assert.That(RuleEngineV4MarkOnlyRules.PlacesMark(plan), Is.True);
        Assert.That(RuleEngineV4MarkOnlyRules.MarkPlaceRangeM(plan), Is.LessThanOrEqualTo(c.Globals.FriendlyRangeCapM));
        Assert.That(RuleEngineV4MarkOnlyRules.PlacesMark(MarkPlan(c, 3, 9, Kilic)), Is.False);
    }

    [TestCase(Kilic)]
    [TestCase(Yay)]
    public void OnlyMovingPlanWithoutDamageOrHeal_UsesSurfaceMark(int weapon)
    {
        RuleEngineV4Catalog c = Catalog();
        Assert.That(RuleEngineV4MarkOnlyRules.MayUseSurfaceMark(MarkPlan(c, 3, 9, weapon)), Is.True, "3-9");
        foreach (int verb in new[] { 1, 2, 4, 6, 9 })
            Assert.That(RuleEngineV4MarkOnlyRules.MayUseSurfaceMark(MarkPlan(c, verb, 9, weapon)), Is.False, $"{verb}-9");
    }

    [Test]
    public void TargetsMark_OnlyForMarkSeekingResolution()
    {
        RuleEngineV4Catalog c = Catalog();
        Assert.That(RuleEngineV4MarkOnlyRules.TargetsMark(MarkPlan(c, 3, 9, Kilic).Target), Is.True, "3-9");
        Assert.That(RuleEngineV4MarkOnlyRules.TargetsMark(MarkPlan(c, 9, 9, Kilic).Target), Is.False, "9-9 koyar");
        Assert.That(RuleEngineV4MarkOnlyRules.TargetsMark(MarkPlan(c, 3, 2, Kilic).Target), Is.False, "3-2");
    }

    [Test]
    public void SurfaceBeatsLiving_OnlyWhenNearerOrLivingOutOfRange()
    {
        Assert.That(RuleEngineV4MarkOnlyRules.SurfaceBeatsLiving(3f, livingInRange: false, livingDistanceM: 1f), Is.True);
        Assert.That(RuleEngineV4MarkOnlyRules.SurfaceBeatsLiving(3f, livingInRange: true, livingDistanceM: 4f), Is.True);
        Assert.That(RuleEngineV4MarkOnlyRules.SurfaceBeatsLiving(3f, livingInRange: true, livingDistanceM: 2f), Is.False);
    }
}
