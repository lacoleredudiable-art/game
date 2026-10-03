using Dovus.Core.Combat;
using Dovus.Core.Tuning;
using NUnit.Framework;
using System.Collections.Generic;

namespace CoreTests;

[TestFixture]
public sealed class BossAttackKindTests
{
    [Test]
    public void ApplyWebField_UsesTuning_NoDamage_FullArc()
    {
        var t = new BossTuning();
        var a = new BossAttack(t);
        a.ApplyWebField();
        Assert.Multiple(() =>
        {
            Assert.That(a.Kind, Is.EqualTo(BossAttackKind.WebField));
            Assert.That(a.WindupMs, Is.EqualTo(900));
            Assert.That(a.RadiusM, Is.EqualTo(3.0f));
            Assert.That(a.ArcHalfAngleDeg, Is.EqualTo(180f));
            Assert.That(a.Damage, Is.EqualTo(0));
        });
    }

    [Test]
    public void ApplyPounce_UsesTuning_LandingStored()
    {
        var t = new BossTuning();
        var a = new BossAttack(t);
        a.SetLanding(2.5f, -7f);
        a.ApplyPounce();
        Assert.Multiple(() =>
        {
            Assert.That(a.Kind, Is.EqualTo(BossAttackKind.Pounce));
            Assert.That(a.WindupMs, Is.EqualTo(1000));
            Assert.That(a.RadiusM, Is.EqualTo(3.0f));
            Assert.That(a.ArcHalfAngleDeg, Is.EqualTo(180f));
            Assert.That(a.Damage, Is.EqualTo(14));
            Assert.That(a.LandingX, Is.EqualTo(2.5f));
            Assert.That(a.LandingZ, Is.EqualTo(-7f));
        });
    }

    [Test]
    public void FromIds_MapsKnown_SkipsUnknown()
    {
        var mapped = BossAttackKindPicker.FromIds(new List<string>
        {
            "slam", "fire_cone", "volley", "web_field", "pounce", "leg_slam", "unknown"
        });
        Assert.That(mapped, Is.EqualTo(new[]
        {
            BossAttackKind.Slam,
            BossAttackKind.FireCone,
            BossAttackKind.Volley,
            BossAttackKind.WebField,
            BossAttackKind.Pounce
        }));
        Assert.That(BossAttackKindPicker.FromIds(null), Is.Empty);
    }

    [Test]
    public void PounceInRange_RespectsMinMax()
    {
        var t = new BossTuning();
        Assert.That(BossAttackKindPicker.PounceInRange(3.99f, t), Is.False);
        Assert.That(BossAttackKindPicker.PounceInRange(4f, t), Is.True);
        Assert.That(BossAttackKindPicker.PounceInRange(12f, t), Is.True);
        Assert.That(BossAttackKindPicker.PounceInRange(12.01f, t), Is.False);
    }

    [Test]
    public void MotionOf_And_IsSpecial()
    {
        Assert.That(BossAttackControl.MotionOf(BossAttackKind.WebField), Is.EqualTo(BossAttackMotion.Standing));
        Assert.That(BossAttackControl.MotionOf(BossAttackKind.Pounce), Is.EqualTo(BossAttackMotion.Leap));
        Assert.That(BossAttackControl.IsSpecial(BossAttackKind.WebField), Is.True);
        Assert.That(BossAttackControl.IsSpecial(BossAttackKind.Pounce), Is.False);
    }

    [Test]
    public void AllowedFor_KaradulPhases_Unchanged()
    {
        Assert.That(BossAttackKindPicker.AllowedFor(false).ToArray(),
            Is.EquivalentTo(new[] { BossAttackKind.Slam, BossAttackKind.Volley }));
        Assert.That(BossAttackKindPicker.AllowedFor(true).ToArray(),
            Is.EquivalentTo(new[] { BossAttackKind.Slam, BossAttackKind.FireCone, BossAttackKind.Volley }));
    }
}
