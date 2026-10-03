using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Motion;
using Dovus.Core.Time;
using Dovus.Core.Tuning;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace CoreTests;

/// <summary>
/// Yerel 1440 tarama: Top geri tepmesi, düz atış, menzil kapanışı, yumruk kancası, 6-2, küre 8-11.
/// </summary>
[TestFixture]
public class SweepWeaponFixTests
{
    const float Body = 0.5f;
    const float BossR = 0.85f;

    MotionTemplateCatalog _catalog;

    [SetUp]
    public void Load()
    {
        string path = Path.Combine(
            FindRepo(), "unity", "Assets", "Resources", "ElementSystem", "motion-templates.json");
        _catalog = MotionTemplateCatalog.FromJson(File.ReadAllText(path));
    }

    [Test]
    public void CannonRecoil_IsATemplatePhase_NotASecondWriter()
    {
        Assert.That(_catalog.TryPlay("5-6", out MotionTemplate shot), Is.True);
        MotionTemplate with = CannonRecoilMotion.Append(shot, 0.5f);
        Assert.That(CannonRecoilMotion.Contains(with), Is.True);
        Assert.That(with.Phases.Count, Is.EqualTo(shot.Phases.Count + 1));
        MotionPhase last = with.Phases[with.Phases.Count - 1];
        Assert.That(last.Motion, Is.EqualTo("retreat"));
        Assert.That(last.DistanceM, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(last.DurationSec, Is.EqualTo(CannonRecoilMotion.DurationSec).Within(0.001f));

        var boss = new MotionTarget(true, 0f, 6f, BossR);
        float plain = FinishZ(shot, boss);
        float recoiled = FinishZ(with, boss);
        Assert.That(plain - recoiled, Is.EqualTo(0.5f).Within(0.08f));
        Assert.That(CannonRecoilMotion.Append(with, 0.5f), Is.SameAs(with));
    }

    [Test]
    public void CannonBasic_ExplodesOnTheBoss_NotAtMaxRange()
    {
        bool hit = CannonShot.TryImpact(
            0f, 0f, 0f, 1f, 25f, 0f, 3f, BossR,
            out float x, out float z, out float dist);
        Assert.That(hit, Is.True);
        Assert.That(x, Is.EqualTo(0f).Within(0.02f));
        Assert.That(dist, Is.EqualTo(3f - BossR).Within(0.05f));
        Assert.That(z, Is.EqualTo(dist).Within(0.02f));
        Assert.That(dist, Is.LessThan(12f));

        var words = new[] { new SentenceWord(Rune.Ates, JumpKind.None, 0) };
        var effect = new LivingEffect(Rune.Ates, 0f, 0f, 0f, 1f, words, new ManifestationTuning());
        effect.StopAt(dist);
        effect.Tick(1f);
        Assert.That(effect.TipDistance, Is.EqualTo(dist).Within(0.02f));
        Assert.That(effect.TipZ, Is.LessThan(4f));
    }

    [Test]
    public void RangeGate_ClosePhaseOnlyWhenEdgeCannotReachThreeMeters()
    {
        float shortEdge = 1f;
        float swordEdge = 4f;
        float need = CastApproach.Meters(3f, Body, BossR, shortEdge, 0f);
        Assert.That(need, Is.GreaterThan(0.2f));
        Assert.That(CastApproach.Meters(3f, Body, BossR, swordEdge, 0f), Is.EqualTo(0f).Within(0.001f));
        Assert.That(CastApproach.Meters(3f, Body, BossR, shortEdge, need), Is.EqualTo(0f).Within(0.001f));

        string[] gated = { "1-3", "1-4", "5-1", "5-4", "7-4" };
        for (int i = 0; i < gated.Length; i++)
        {
            Assert.That(_catalog.TryPlay(gated[i], out MotionTemplate template), Is.True, gated[i]);
            float already = MotionCastReach.ClosingApproachM(template);
            float meters = CastApproach.Meters(3f, Body, BossR, shortEdge, already);
            MotionTemplate closed = CastApproach.Prepend(template, meters);
            if (meters > 0.05f)
            {
                Assert.That(closed.Phases[0].Name, Is.EqualTo(CastApproach.PhaseName), gated[i]);
                Assert.That(closed.Phases[0].DurationSec, Is.GreaterThanOrEqualTo(CastApproach.MinSec), gated[i]);
            }
            else
            {
                Assert.That(closed.Phases.Count, Is.EqualTo(template.Phases.Count), gated[i]);
            }
        }
    }

    [Test]
    public void FistHook_StaysInsideAuthoredDistanceAndTime()
    {
        Assert.That(_catalog.TryPlay("2-6", out MotionTemplate hook), Is.True);
        Assert.That(_catalog.TryPlay("2-9", out MotionTemplate mark), Is.True);
        float hookSec = Sum(hook);
        float markSec = Sum(mark);
        var ally = new MotionTarget(true, -3.2f, -1.2f, Body);
        Finish(hook, ally, out float hx, out float hz, out float hElapsed);
        Finish(mark, ally, out float mx, out float mz, out float mElapsed);
        Assert.That(hElapsed, Is.LessThanOrEqualTo(hookSec + 0.05f));
        Assert.That(mElapsed, Is.LessThanOrEqualTo(markSec + 0.05f));
        float hookTravel = System.MathF.Sqrt(hx * hx + (hz - 2f) * (hz - 2f));
        Assert.That(hookTravel, Is.GreaterThan(2f), "kanca temasa kadar gider, süre uzamaz");
        float markTravel = System.MathF.Sqrt(mx * mx + (mz - 2f) * (mz - 2f));
        Assert.That(markTravel, Is.LessThanOrEqualTo(0.9f));
    }

    [Test]
    public void FistHook_RetreatDoesNotEraseAHitThatStartedInRange()
    {
        float edge = 2f;
        float start = 3f;
        float afterRetreat = 3.72f;
        Assert.That(MotionCastReach.CenterInReach(afterRetreat, Body, BossR, edge), Is.False);
        float closer = MotionCastReach.CloserCenter(afterRetreat, start);
        Assert.That(MotionCastReach.CenterInReach(closer, Body, BossR, edge), Is.True);
    }

    [Test]
    public void OrbCourier_SidestepIsTheOnlyDisplacement()
    {
        Assert.That(_catalog.TryPlay("8-11", out MotionTemplate courier), Is.True);
        Assert.That(_catalog.TryPlay("2-11", out MotionTemplate heal), Is.True);
        Assert.That(courier.Phases.Count, Is.EqualTo(heal.Phases.Count));
        PositionPlayback playback = PositionOwnership.Prepare(
            courier, System.Array.Empty<GrammarPositionStep>(), 0.28f, 1.2f);
        Assert.That(playback.Template.Phases.Count, Is.EqualTo(courier.Phases.Count));
        var runner = new MotionTemplateRunner();
        var boss = new MotionTarget(true, 0f, 5f, BossR);
        runner.Begin(playback.Template, 0f, 0f, 2f, 0f, 1f, Body, 0.15f);
        while (!runner.Finished)
            runner.Tick(1f / 60f, boss, default);
        float moved = System.MathF.Sqrt(runner.X * runner.X + (runner.Z - 2f) * (runner.Z - 2f));
        float authored = System.MathF.Sqrt(1.4f * 1.4f + 0.2f * 0.2f);
        Assert.That(moved, Is.EqualTo(authored).Within(0.08f));
    }

    [Test]
    public void EmiciCannon_IsNotRecoiled_AndHeldSimStays()
    {
        Assert.That(_catalog.TryPlay("1-2", out MotionTemplate pull), Is.True);
        Assert.That(CannonRecoilMotion.Applies("ballistic", 0.5f, 40f, "2", pull), Is.False);
        Assert.That(CannonRecoilMotion.Applies("ballistic", 0.5f, 40f, "1", pull), Is.True);

        var held = new MotionTarget(true, 0f, 0f, BossR, true);
        var runner = new MotionTemplateRunner();
        runner.Begin(pull, 0f, 0f, 3f, 0f, -1f, Body, 0.15f);
        int guard = 0;
        while (!runner.Finished && guard++ < 300)
            runner.Tick(1f / 60f, held, default);
        float moved = System.MathF.Sqrt(runner.X * runner.X + (runner.Z - 3f) * (runner.Z - 3f));
        Assert.That(moved, Is.LessThan(0.05f));
    }

    [Test]
    public void Retreat_EndsOutsideTheBoss()
    {
        var phase = new MotionPhase(
            "geri_tepme", "retreat", 0.12f, "travel", "none", "", 0f,
            0.5f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, null, null);
        var template = new MotionTemplate("tepme", "tepme", 0, "tepme", true, new[] { phase });
        var boss = new MotionTarget(true, 0f, 0f, BossR);
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, Body, 0.15f);
        int guard = 0;
        while (!runner.Finished && guard++ < 30)
            runner.Tick(1f / 60f, boss, default);
        float dist = System.MathF.Sqrt(runner.X * runner.X + runner.Z * runner.Z);
        Assert.That(dist, Is.GreaterThanOrEqualTo(Body + BossR + 0.15f - 0.02f));
    }

    [Test]
    public void ShortWeaponSlam_ClosesToTheJsonEdge_SwordDoesNotLunge()
    {
        Assert.That(_catalog.TryPlay("1-4", out MotionTemplate slam), Is.True);
        // Fiil 1 kapsül 1,5 m × silah menzil 0,5 × sıfat 4 boy 1 = 0,75. Kılıç menzil 1 → 1,5.
        const float fistEdge = 0.75f;
        const float swordEdge = 1.5f;
        float fist = MotionCastReach.ApproachMeters(3f, Body, BossR, fistEdge, slam);
        float sword = MotionCastReach.ApproachMeters(3f, Body, BossR, swordEdge, slam);
        Assert.That(sword, Is.EqualTo(0f).Within(0.001f));
        float closing = MotionCastReach.ClosingApproachM(slam);
        Assert.That(MotionCastReach.CenterInReach(3f - fist - closing, Body, BossR, fistEdge), Is.True);
        float inflated = System.Math.Max(fistEdge, MotionCastReach.EdgeReachM(slam));
        float old = CastApproach.Meters(3f, Body, BossR, inflated, closing);
        Assert.That(MotionCastReach.CenterInReach(3f - old - closing, Body, BossR, fistEdge), Is.False);
    }

    [Test]
    public void RemotePull_ClampedHitchDoesNotDumpChannelHits()
    {
        Assert.That(FrameDelta.ClampMs(16), Is.EqualTo(16).Within(0.001));
        Assert.That(FrameDelta.ClampMs(11000), Is.EqualTo(FrameDelta.MaxFrameMs).Within(0.001));
        Assert.That(_catalog.TryPlay("2-2", out MotionTemplate pull), Is.True);

        List<float> dumped = HitTimes(pull, 11f, 11f);
        Assert.That(dumped.Count, Is.EqualTo(4));
        Assert.That(dumped[3] - dumped[0], Is.LessThan(0.05f));

        float step = (float)(FrameDelta.MaxFrameMs / 1000.0);
        List<float> spread = HitTimes(pull, step, 1f / 60f);
        Assert.That(spread.Count, Is.EqualTo(4));
        Assert.That(spread[3] - spread[0], Is.GreaterThan(1f));
        Assert.That(spread[3], Is.LessThanOrEqualTo(Sum(pull) + 0.05f));
    }

    static List<float> HitTimes(MotionTemplate template, float firstDt, float laterDt)
    {
        var times = new List<float>();
        var runner = new MotionTemplateRunner();
        var boss = new MotionTarget(true, 0f, 0f, BossR);
        runner.Begin(template, 0f, 0f, 3f, 0f, -1f, Body, 0.15f);
        bool first = true;
        int guard = 0;
        while (!runner.Finished && guard++ < 800)
        {
            MotionTick tick = runner.Tick(first ? firstDt : laterDt, boss, default);
            first = false;
            for (int i = 0; i < tick.Hits.Length; i++)
                times.Add(runner.Elapsed);
        }
        return times;
    }

    static float FinishZ(MotionTemplate template, MotionTarget boss)
    {
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 3f, 0f, 1f, Body, 0.15f);
        while (!runner.Finished)
            runner.Tick(1f / 60f, boss, default);
        return runner.Z;
    }

    static float Sum(MotionTemplate template)
    {
        float sum = 0f;
        for (int i = 0; i < template.Phases.Count; i++)
            sum += template.Phases[i].DurationSec;
        return sum;
    }

    static void Finish(MotionTemplate template, MotionTarget aim, out float x, out float z, out float elapsed)
    {
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 2f, 0f, 1f, Body, 0.15f);
        int guard = 0;
        while (!runner.Finished && guard++ < 600)
            runner.Tick(1f / 60f, aim, default);
        x = runner.X;
        z = runner.Z;
        elapsed = runner.Elapsed;
    }

    static string FindRepo()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "element-sistemi.json")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return "/workspace";
    }
}
