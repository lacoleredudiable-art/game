using System;
using System.IO;
using Dovus.Core.Motion;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class GroundingTests
{
    [Test]
    public void Curve_LeavesGroundOnlyWhenTheTemplateSaysSo()
    {
        Assert.That(VerticalCurve.LeavesGround("hop", 0.55f), Is.True);
        Assert.That(VerticalCurve.LeavesGround("leap", 1.35f), Is.True);
        Assert.That(VerticalCurve.LeavesGround("slam", 1.2f), Is.True);
        Assert.That(VerticalCurve.LeavesGround("hover", 1.15f), Is.True);
        Assert.That(VerticalCurve.LeavesGround("channel", 0.85f), Is.True);
        Assert.That(VerticalCurve.LeavesGround("channel", 0f), Is.False);
        Assert.That(VerticalCurve.LeavesGround("pull", 0f), Is.False);
        Assert.That(VerticalCurve.LeavesGround("dash", 0f), Is.False);
        Assert.That(VerticalCurve.LeavesGround("hold", 0.9f), Is.False);

        Assert.That(VerticalCurve.Height("hop", 1f, 0f), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(VerticalCurve.Height("hop", 1f, 0.5f), Is.EqualTo(1f).Within(0.0001f));
        Assert.That(VerticalCurve.Height("hop", 1f, 1f), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(VerticalCurve.Height("slam", 1.2f, 0f), Is.EqualTo(1.2f).Within(0.0001f));
        Assert.That(VerticalCurve.Height("slam", 1.2f, 1f), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(VerticalCurve.Height("hover", 1.15f, 1f), Is.EqualTo(1.15f).Within(0.0001f));
        Assert.That(VerticalCurve.Height("pull", 0f, 0.5f), Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void NamedSkills_DeclareAirborneFromTheTemplate()
    {
        var catalog = MotionTemplateCatalog.FromJson(File.ReadAllText(JsonPath()));
        AssertAir("3-3", catalog, true);
        AssertAir("3-8", catalog, true);
        AssertAir("6-3", catalog, true);
        AssertAir("6-8", catalog, true);
        AssertAir("8-4", catalog, true);
        Assert.That(catalog.TryPlay("8-1", out MotionTemplate door), Is.True);
        Assert.That(door.Phases[0].Airborne, Is.False, "8-1 yerde duran bir tutuş");
    }

    [Test]
    public void Pull_KeepsThePlantedRoot_InsteadOfSinkingToZero()
    {
        var runner = new MotionTemplateRunner();
        runner.Begin(Template("cek", Phase("cek", "pull", 0.4f, distance: 3f)), 0f, 1f, 0f, 0f, 1f);
        var target = new MotionTarget(true, 0f, 4f, 0.85f);
        for (int i = 0; i < 30; i++)
        {
            MotionTick tick = runner.Tick(0.02f, target, default);
            Assert.That(tick.Y, Is.EqualTo(1f).Within(0.001f), "çekme kökü y=0'a gömmemeli");
            Assert.That(tick.Airborne, Is.False);
            if (tick.Finished)
                break;
        }

        runner.Begin(Template("cek", Phase("cek", "pull", 0.3f, distance: 3f)), 0f, 1f, 0f, 0f, 1f);
        var held = new MotionTarget(true, 0f, 4f, 0.85f, holdApproach: true);
        MotionTick frozen = runner.Tick(0.05f, held, default);
        Assert.That(frozen.Y, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void Hop_ArcsAboveThePlantedRoot_AndLandsOnIt()
    {
        var runner = new MotionTemplateRunner();
        runner.Begin(Template("sek", Phase("sek", "hop", 0.4f, height: 1f, distance: 1f)), 0f, 1f, -2f, 0f, 1f);
        float peak = 0f;
        bool sawAir = false;
        MotionTick last = default;
        for (int i = 0; i < 40; i++)
        {
            last = runner.Tick(0.02f, default, default);
            peak = MathF.Max(peak, last.Y);
            if (last.Airborne)
                sawAir = true;
            if (last.Finished)
                break;
        }

        Assert.That(sawAir, Is.True);
        Assert.That(peak, Is.GreaterThan(1.6f));
        Assert.That(last.Finished, Is.True);
        Assert.That(last.Airborne, Is.False);
        Assert.That(last.Y, Is.EqualTo(1f).Within(0.02f));
    }

    [Test]
    public void HoverCancel_EasesToGround_WithoutPopOrSink_AndDoesNotAccumulate()
    {
        var g = new Grounding();
        g.Plant(1f);
        g.Follow(1f + 1.15f);
        GroundSample up = g.Sample(0.35f);
        Assert.That(up.RootY, Is.EqualTo(2.15f).Within(0.001f));
        Assert.That(up.VisualOffsetY, Is.EqualTo(0f), "görsel ofset birikmez");
        Assert.That(up.Airborne, Is.True);
        Assert.That(g.Sample(0.35f).VisualOffsetY, Is.EqualTo(0f));

        g.Cancel();
        Assert.That(g.RootY, Is.EqualTo(2.15f).Within(0.001f), "iptal anında zıplama yok");
        float prev = g.RootY;
        for (int i = 0; i < 9; i++)
        {
            g.Tick(0.02f);
            Assert.That(g.RootY, Is.LessThanOrEqualTo(prev + 0.0001f));
            Assert.That(g.RootY, Is.GreaterThanOrEqualTo(1f - 0.0001f));
            prev = g.RootY;
        }
        g.Tick(0.05f);
        Assert.That(g.RootY, Is.EqualTo(1f).Within(0.0001f));
        Assert.That(g.Landing, Is.False);
        Assert.That(g.GroundY, Is.EqualTo(1f), "sonraki cast zemini havadaki kök sanmasın");

        g.Follow(0f);
        g.Release();
        g.Tick(Grounding.LandSec);
        Assert.That(g.RootY, Is.EqualTo(1f).Within(0.0001f), "gömülmüş kök de zemine çıkar");
    }

    [Test]
    public void Slack_MatchesTheSweepThresholds()
    {
        Assert.That(Grounding.LiveSlackM, Is.EqualTo(0.05f).Within(0.0001f));
        Assert.That(Grounding.SettleSlackM, Is.EqualTo(0.03f).Within(0.0001f));
        Assert.That(Grounding.SettleAfterSec, Is.EqualTo(0.3f).Within(0.0001f));
        Assert.That(Grounding.LandSec, Is.LessThan(Grounding.SettleAfterSec));
        Assert.That(Grounding.WithinSlack(0.04f, 0f, settled: false), Is.True);
        Assert.That(Grounding.WithinSlack(0.04f, 0f, settled: true), Is.False);
        Assert.That(Grounding.WithinSlack(0.03f, 0f, settled: true), Is.True);
    }

    [Test]
    public void SweepPace_ScalesFrameThresholds_AndKeepsThePhysicsStep()
    {
        const float frame = 1f / 60f;
        float jumpAt1 = SweepPace.JumpLimit(frame);
        Assert.That(jumpAt1, Is.EqualTo(SweepPace.JumpMaxSpeedMps * frame + SweepPace.JumpMarginM).Within(0.0001f));
        Assert.That(SweepPace.JumpLimit(frame * 4f), Is.EqualTo(jumpAt1 * 4f).Within(0.0001f));
        Assert.That(jumpAt1 * 4f, Is.LessThanOrEqualTo(SweepPace.JumpLimit(SweepPace.EffectiveFrameSec(frame, 4f)) + 0.0001f));

        Assert.That(SweepPace.GroundSlack(frame), Is.EqualTo(Grounding.LiveSlackM).Within(0.0001f));
        Assert.That(SweepPace.GroundSlack(0f), Is.EqualTo(Grounding.LiveSlackM).Within(0.0001f));
        Assert.That(SweepPace.GroundSlack(frame * 4f), Is.EqualTo(Grounding.LiveSlackM * 4f).Within(0.0001f));
        Assert.That(Grounding.SettleSlackM, Is.EqualTo(0.03f).Within(0.0001f));

        Assert.That(SweepPace.FixedStepSec(0.02f), Is.EqualTo(0.02f));
        Assert.That(SweepPace.MaxDeltaSec(1f / 3f, 4f), Is.EqualTo(4f / 3f).Within(0.0001f));
        Assert.That(SweepPace.SettleWaitSec(0.02f), Is.EqualTo(0.04f).Within(0.0001f));
        Assert.That(SweepPace.SettleWaitSec(0.02f), Is.LessThan(0.35f));
        Assert.That(SweepPace.IdleHoldSec, Is.EqualTo(frame).Within(0.0001f));
        Assert.That(SweepPace.ClampSpeed(4f), Is.EqualTo(4f));
        Assert.That(SweepPace.ClampSpeed(1f), Is.EqualTo(1f));
    }

    [Test]
    public void FootCorrection_UsesParentScale_SoTheBossDoesNotRestFiveCentimetersUp()
    {
        // Boss kapsülü Y ölçeği 1,3; görsel fit'i bunu da içerir. lossyScale ile bölmek
        // dünya hatasının yalnız 1/localScale kadarını indirir.
        const float parentScale = 1.3f;
        const float visualLocalScale = 2.15f;
        const float worldError = 0.11f;
        float lossy = parentScale * visualLocalScale;
        float residualIfLossy = worldError - (worldError / lossy) * parentScale;
        Assert.That(residualIfLossy, Is.GreaterThan(0.05f));

        float local = Grounding.LocalOffsetForWorldError(worldError, parentScale);
        float residual = worldError - local * parentScale;
        Assert.That(residual, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(Grounding.LocalOffsetForWorldError(0.05f, 0f), Is.EqualTo(0.05f).Within(0.0001f));
    }

    static void AssertAir(string id, MotionTemplateCatalog catalog, bool airborne)
    {
        Assert.That(catalog.TryPlay(id, out MotionTemplate template), Is.True, id);
        bool any = false;
        for (int i = 0; i < template.Phases.Count; i++)
        {
            if (template.Phases[i].Airborne)
                any = true;
        }
        Assert.That(any, Is.EqualTo(airborne), id);
    }

    static MotionPhase Phase(string name, string motion, float sec, float distance = 0f, float height = 0f) =>
        new MotionPhase(
            name, motion, sec, "target", "track", "", 0f,
            distance, 0f, 1f, 0f, height, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f, null, null);

    static MotionTemplate Template(string id, MotionPhase phase) =>
        new MotionTemplate(id, id, 1, "test", true, new[] { phase });

    static string JsonPath()
    {
        string root = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", ".."));
        return Path.Combine(root, "unity", "Assets", "Resources", "ElementSystem", "motion-templates.json");
    }
}
