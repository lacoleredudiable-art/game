using System.IO;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4HoldToTargetTests
{
    static RuleEngineV4Globals Globals() =>
        RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json")))).Globals;

    [Test]
    public void Json_HoldToTargetSec_IsPointTwo()
    {
        Assert.That(Globals().HoldToTargetSec, Is.EqualTo(0.2f));
    }

    [Test]
    public void PickOpen_OnlyAfterThreshold()
    {
        float hold = Globals().HoldToTargetSec;
        Assert.That(RuleEngineV4HoldToTarget.PickOpen(hold * 0.5, hold), Is.False);
        Assert.That(RuleEngineV4HoldToTarget.PickOpen(hold, hold), Is.True);
    }

    [Test]
    public void FastRelease_LaunchesDefault_EvenOnCenterOrTarget()
    {
        Assert.That(RuleEngineV4HoldToTarget.Decide(false, false, true, true, false),
            Is.EqualTo(RuleEngineV4HeldRelease.LaunchDefault));
        Assert.That(RuleEngineV4HoldToTarget.Decide(false, false, false, false, true),
            Is.EqualTo(RuleEngineV4HeldRelease.LaunchDefault));
    }

    [Test]
    public void PickOpen_ReleaseOnTarget_LaunchesAtPicked()
    {
        Assert.That(RuleEngineV4HoldToTarget.Decide(true, false, false, false, true),
            Is.EqualTo(RuleEngineV4HeldRelease.LaunchAtPicked));
    }

    [Test]
    public void PickOpen_ReleaseOnCenter_Cancels()
    {
        Assert.That(RuleEngineV4HoldToTarget.Decide(true, false, true, true, false),
            Is.EqualTo(RuleEngineV4HeldRelease.Cancel));
    }

    [Test]
    public void PickOpen_ReleaseOnEmptyOrHexagonDot_LaunchesAuto()
    {
        Assert.That(RuleEngineV4HoldToTarget.Decide(true, false, false, false, false),
            Is.EqualTo(RuleEngineV4HeldRelease.LaunchAuto));
        Assert.That(RuleEngineV4HoldToTarget.Decide(true, false, false, true, true),
            Is.EqualTo(RuleEngineV4HeldRelease.LaunchAuto));
    }

    [Test]
    public void CancelledPointer_AlwaysCancels()
    {
        Assert.That(RuleEngineV4HoldToTarget.Decide(false, true, false, false, true),
            Is.EqualTo(RuleEngineV4HeldRelease.Cancel));
    }
}
