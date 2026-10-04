using Dovus.Core;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Casting;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace CoreTests;

public partial class PlaySweepFixTests
{
    [Test]
    public void EmiciPull_HoldsTheCaster_AndRetargetsEveryTick()
    {
        var lunge = new MotionPhase(
            "vur", "lunge", 0.4f, "target", "track", string.Empty, 0f,
            1.5f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, null, null);
        var template = new MotionTemplate("emici", "emici", 1, "emici", true, new[] { lunge }, MotionAim.Enemy);
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, Body, Stop);
        var far = new MotionTarget(true, 0f, 3f, BossR);
        for (int i = 0; i < 4; i++)
            runner.Tick(1f / 60f, far, default);
        float advanced = runner.Z;
        Assert.That(advanced, Is.GreaterThan(0.05f));

        var inside = new MotionTarget(true, 0f, 0.4f, BossR, holdApproach: true);
        float held = runner.Z;
        for (int i = 0; i < 20; i++)
        {
            runner.Tick(1f / 60f, inside, default);
            Assert.That(runner.Z, Is.EqualTo(held).Within(0.001f), "çekme sürerken oyuncu yürümez");
        }

        var release = new MotionTarget(true, 0f, 3f, BossR);
        float z0 = runner.Z;
        runner.Tick(1f / 60f, release, default);
        Assert.That(runner.Z, Is.GreaterThanOrEqualTo(z0 - 0.02f), "bırakınca geri sekmez");
        Assert.That(MathF.Abs(runner.Z - z0), Is.LessThan(0.35f), "bırakınca tek karede fırlamaz");

        float playerZ = 0f;
        float bossZ = 3f;
        float speed = 0f;
        float sx = 0f;
        float sz = 1f;
        bool pulling = false;
        float locked = 0f;
        for (int i = 0; i < 40; i++)
        {
            bool active = pulling;
            EmiciPull.Retarget(
                active, 0f, bossZ, 0f, playerZ, Body, BossR,
                ref sx, ref sz, ref speed,
                out float tx, out float tz, out pulling);
            if (active)
                Assert.That(speed, Is.EqualTo(locked).Within(0.001f), "hız her kare sıfırlanmaz");
            else
                locked = speed;
            float bx = 0f;
            EmiciPull.StepToward(ref bx, ref bossZ, tx, tz, speed, 1f / 60f, out _);
            float gap = MathF.Abs(bossZ - playerZ);
            Assert.That(gap, Is.GreaterThanOrEqualTo(Contact - 0.03f), "kare " + i);
        }
        Assert.That(MathF.Abs(bossZ - playerZ), Is.LessThan(Contact + 0.08f));

        playerZ = 0.55f;
        float before = MathF.Abs(bossZ - playerZ);
        EmiciPull.Retarget(
            true, 0f, bossZ, 0f, playerZ, Body, BossR,
            ref sx, ref sz, ref speed,
            out float movedX, out float movedZ, out _);
        Assert.That(speed, Is.EqualTo(locked).Within(0.001f));
        Assert.That(MathF.Abs(movedZ - playerZ), Is.EqualTo(Contact).Within(0.05f));
        float stepX = 0f;
        float stepZ = bossZ;
        EmiciPull.StepToward(ref stepX, ref stepZ, movedX, movedZ, speed, 1f / 60f, out _);
        Assert.That(MathF.Abs(stepZ - playerZ), Is.GreaterThan(before - 0.001f));
        Assert.That(MathF.Abs(stepZ - bossZ), Is.LessThan(0.2f), "iç içe kare tek karede ışınlanmaz");
        Assert.That(EmiciPull.VortexActs(true, false), Is.False);
        Assert.That(EmiciPull.VortexActs(true, true), Is.True);
        Assert.That(EmiciPull.VortexActs(false, true), Is.False);
    }

    [Test]
    public void AllyHook_StopsAtContact_Grounded_AndAroundTheBoss()
    {
        var far = new MotionPhase(
            "cek", "pull", 0.34f, "target", "track", string.Empty, 0f,
            40f, 0f, 0f, 0f, 5f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, null, null);
        var near = new MotionPhase(
            "cek", "pull", 0.34f, "target", "none", string.Empty, 0f,
            2f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, null, null);
        var ally = new MotionTarget(true, 0f, 6f, Body, false, true, 0f, 2f, BossR);
        float farZ = RunPull(far, ally, 5f, out float farY, out float farMin);
        float nearZ = RunPull(near, ally, 5f, out float nearY, out float nearMin);
        Assert.That(farZ, Is.EqualTo(nearZ).Within(0.08f), "silah menzili kancayı uzatmaz");
        // Çekme height_m ile havalanmaz ve mutlak y=0'a gömülmez; başlangıç kökü zemindir.
        Assert.That(farY, Is.EqualTo(5f).Within(0.001f));
        Assert.That(nearY, Is.EqualTo(5f).Within(0.001f));
        float sep = Body + Body + Stop;
        Assert.That(farZ, Is.LessThan(6f - Body));
        Assert.That(MathF.Abs(farZ - (6f - sep)), Is.LessThan(0.35f));
        Assert.That(farMin, Is.GreaterThanOrEqualTo(Body + BossR + Stop - 0.08f));
        Assert.That(nearMin, Is.GreaterThanOrEqualTo(Body + BossR + Stop - 0.08f));
    }

    [Test]
    public void WeaponSwapScale_DoesNotChangeBehindLanding()
    {
        Assert.That(_catalog.TryPlay("3-3", out MotionTemplate hops), Is.True);
        var swordSteps = new[] { new GrammarPositionStep("yer_degistir", 3f) };
        var staffSteps = new[] { new GrammarPositionStep("yer_degistir", 7.5f) };
        PositionPlayback sword = PositionOwnership.Prepare(hops, swordSteps, 0.28f, 1.2f);
        PositionPlayback staff = PositionOwnership.Prepare(hops, staffSteps, 0.28f, 1.2f);
        Assert.That(staff.Template.Phases.Count, Is.EqualTo(hops.Phases.Count));
        Assert.That(staff.Template.Phases[0].ForwardM, Is.EqualTo(hops.Phases[0].ForwardM));
        Assert.That(staff.Template.Phases[^1].Land, Is.EqualTo("behind"));
        Assert.That(sword.Template.Phases[^1].ForwardM, Is.EqualTo(staff.Template.Phases[^1].ForwardM));

        float swordZ = FinishZ(sword.Template, 0.85f);
        float staffZ = FinishZ(staff.Template, 0.85f);
        Assert.That(staffZ, Is.EqualTo(swordZ).Within(0.001f));
        Assert.That(staffZ, Is.GreaterThan(3.3f));

        var cross = new MotionPhase(
            "gec", "hop", 0.2f, "travel", "none", string.Empty, 0f,
            0f, 4f, 1f, 0f, 0.2f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, null, null);
        var land = new MotionPhase(
            "in", "hop", 0.2f, "travel", "none", string.Empty, 0f,
            0.4f, 0.4f, 1f, 0f, 0.2f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, null, null, "behind");
        var crossed = new MotionTemplate(
            "capraz", "capraz", 3, "capraz", true, new[] { cross, land }, MotionAim.Effect);
        Assert.That(FinishZ(crossed, BossR), Is.GreaterThan(3.3f), "boss'u geçen ara adım inişi öne çevirmez");
    }
}
