using Dovus.App.Boss;
using Dovus.Core.Combat;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class BossDirectorAppRulesTests
{
    [Test]
    public void VolleyPattern_BlindWidensSpread()
    {
        Assert.That(VolleyPattern.EffectiveSpreadDeg(20f, false), Is.EqualTo(20f));
        Assert.That(VolleyPattern.EffectiveSpreadDeg(20f, true), Is.EqualTo(30f));
    }

    [Test]
    public void VolleyPattern_ThreeShotFan()
    {
        VolleyLayout layout = VolleyPattern.ComputeLayout(3, 30f);
        Assert.That(layout.Count, Is.EqualTo(3));
        Assert.That(VolleyPattern.YawDegAt(layout, 0), Is.EqualTo(-15f).Within(0.001f));
        Assert.That(VolleyPattern.YawDegAt(layout, 1), Is.EqualTo(0f).Within(0.001f));
        Assert.That(VolleyPattern.YawDegAt(layout, 2), Is.EqualTo(15f).Within(0.001f));
    }

    [Test]
    public void ApproachRules_StopInsideRing()
    {
        ApproachStep step = BossApproachRules.ComputeStep(
            0f, 0f, 1f, 0f,
            bodyRadiusM: 1f, aimRadiusM: 0.5f, stopPadM: 0.35f,
            reversed: false, approachSpeedMps: 2f, moveSpeedMult: 1f, dtSec: 0.1f);
        Assert.That(step.Stop, Is.True);
    }

    [Test]
    public void BossDirectorRules_EnragedAtHalfHp()
    {
        Assert.That(BossDirectorRules.IsEnraged(50f, 100f), Is.True);
        Assert.That(BossDirectorRules.IsEnraged(51f, 100f), Is.False);
    }

    [Test]
    public void BossMechanicStatusMap_FireConeDefaults()
    {
        var board = new StatusBoard();
        var t = new StatusTuning();
        BossMechanicStatusMap.ApplyFireCone(board, t, null);
        Assert.That(board.Has(StatusKind.Burn), Is.True);
        Assert.That(board.Has(StatusKind.GrievousWounds), Is.True);
    }

    [Test]
    public void BossStrikeMath_SignedAngleMatchesUnityStyle()
    {
        float angle = BossStrikeMath.FlatSignedAngleDeg(0f, 1f, 1f, 0f);
        Assert.That(angle, Is.EqualTo(-90f).Within(0.01f));
    }

    [Test]
    public void BossPoiseState_SkipsTinyLeap()
    {
        var state = new BossPoiseState();
        Assert.That(state.TryPlanPounceLeap(0f, 0f, 0.01f, 0f, 0.05f), Is.False);
        Assert.That(state.PounceLeapActive, Is.False);
    }
}
