using Dovus.Core.Combat;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using NUnit.Framework;
using System;
using System.IO;

namespace CoreTests;

/// <summary>hitbox_vfx.fiil_hitbox + mobility_cc.i_frame + Hareket/kendine fiil kuralları.</summary>
[TestFixture]
public class VerbExecutionTests
{
    string _json = null!;
    SkillMotor _motor = null!;
    VerbExecutionData _data = null!;

    [SetUp]
    public void SetUp()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        }
        _json = File.ReadAllText(path);
        _motor = SkillMotor.FromJson(_json);
        _data = VerbExecutionData.FromJson(_json);
    }

    [Test]
    public void VerbHitboxSizesComeFromJson()
    {
        Assert.That(_data.TryGetHitbox(3, out VerbHitboxSpec dash), Is.True);
        Assert.That(dash.Shape, Is.EqualTo("line"));
        Assert.That(dash.SizeA, Is.EqualTo(3f).Within(1e-4));
        Assert.That(dash.SizeB, Is.EqualTo(0.8f).Within(1e-4));
        Assert.That(dash.DurationSec, Is.EqualTo(0.3f).Within(1e-4));
        Assert.That(dash.IsTimed, Is.False);

        Assert.That(_data.TryGetHitbox(7, out VerbHitboxSpec debuff), Is.True);
        Assert.That(debuff.Shape, Is.EqualTo("capsule"));
        Assert.That(debuff.SizeA, Is.EqualTo(3f).Within(1e-4));
        Assert.That(debuff.SizeB, Is.EqualTo(1f).Within(1e-4));

        Assert.That(_data.TryGetHitbox(10, out VerbHitboxSpec reflect), Is.True);
        Assert.That(reflect.IsRadius, Is.True);
        Assert.That(reflect.SizeA, Is.EqualTo(1.5f).Within(1e-4));
        Assert.That(reflect.IsTimed, Is.True);

        Assert.That(_data.TryGetHitbox(11, out VerbHitboxSpec summon), Is.True);
        Assert.That(summon.Shape, Is.EqualTo("point"));
        Assert.That(summon.SizeA, Is.EqualTo(1f).Within(1e-4));

        for (int verb = 1; verb <= 12; verb++)
            Assert.That(_data.TryGetHitbox(verb, out _), Is.True, "fiil " + verb);
    }

    [Test]
    public void IFramesComeFromMobilityCc()
    {
        Assert.That(_data.IFrameMsFor("3-7"), Is.EqualTo(400));
        Assert.That(_data.IFrameMsFor("11-10"), Is.EqualTo(300));
        Assert.That(_data.IFrameMsFor("1-1"), Is.EqualTo(0));
    }

    [Test]
    public void DashUsesJsonDistanceAndIFrame()
    {
        var tuning = new SkillMotionTuning { ArenaHalfSizeM = 100f };
        var ctx = new SkillMotionContext(0f, 0f, 0f, 1f, 50f, 50f, true, 100f);

        SkillMotionPlan plain = SkillMotionMotor.Resolve(_motor.Resolve(new[] { 3, 1 }), ctx, tuning);
        Assert.That(plain.Kind, Is.EqualTo(SkillMotionKind.ForwardDash));
        Assert.That(plain.DestZ, Is.EqualTo(3f).Within(1e-4), "verb_base.3.dash_distance_m");
        Assert.That(plain.IframeMs, Is.EqualTo(0));

        SkillResolution ghost = _motor.Resolve(new[] { 3, 7 });
        SkillMotionPlan ghostPlan = SkillMotionMotor.Resolve(
            ghost, ctx, tuning, _data.IFrameMsFor(ghost.SkillId));
        Assert.That(ghostPlan.IframeMs, Is.EqualTo(400));
    }

    [Test]
    public void SelfVerbHostileAdjectiveGoesToHitTargetNotCaster()
    {
        SkillResolution bindingDash = _motor.Resolve(new[] { 3, 6 });
        var tuning = new StatusTuning();

        var caster = new StatusBoard();
        StatusApplicator.ApplySkill(bindingDash, caster, null, tuning);
        Assert.That(caster.Has(StatusKind.Root), Is.False, "dash kendini köklememeli");
        Assert.That(caster.Has(StatusKind.Slow), Is.False);

        var boss = new StatusBoard();
        StatusApplicator.ApplySkill(bindingDash, new StatusBoard(), boss, tuning);
        Assert.That(boss.Has(StatusKind.Root), Is.True, "3-6 apply_root_sec boss'a");
        Assert.That(boss.Has(StatusKind.Slow), Is.True);
    }

    [Test]
    public void BlurAdjectiveBlindsHitTarget()
    {
        var boss = new StatusBoard();
        StatusApplicator.ApplySkill(_motor.Resolve(new[] { 1, 7 }), new StatusBoard(), boss, new StatusTuning());
        Assert.That(boss.Has(StatusKind.Blind), Is.True, "accuracy_debuff → Blind");
    }

    [Test]
    public void SummonAndReflectEngineCarryDurations()
    {
        JsonValue summon = _motor.Resolve(new[] { 11, 1 }).EngineModifiers;
        Assert.That(summon["minion_count"].AsInt(0), Is.EqualTo(1));
        Assert.That(summon["minion_duration_sec"].AsFloat(0f), Is.EqualTo(5f).Within(1e-4));

        JsonValue reflect = _motor.Resolve(new[] { 10, 1 }).EngineModifiers;
        Assert.That(reflect["reflect_ratio"].AsFloat(0f), Is.EqualTo(0.5f).Within(1e-4));
        Assert.That(reflect["reflect_duration_sec"].AsFloat(0f), Is.EqualTo(2f).Within(1e-4));

        JsonValue copy = _motor.Resolve(new[] { 11, 11 }).EngineModifiers;
        Assert.That(copy["duplicate_cast"].AsBool(false), Is.True);
        Assert.That(copy["duplicate_delay_sec"].AsFloat(0f), Is.EqualTo(0.3f).Within(1e-4));
    }
}
