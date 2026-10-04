using Dovus.Core;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dovus.Core.Shared;
namespace CoreTests;

[TestFixture]
public class PositionOwnershipTests
{
    MotionTemplateCatalog _motion = null!;
    MechanicGrammar _grammar = null!;
    JsonValue _prose;

    [SetUp]
    public void Load()
    {
        DesignWarnings.ResetForTests();
        string root = RepoRoot();
        _motion = MotionTemplateCatalog.FromJson(File.ReadAllText(
            Path.Combine(root, "unity", "Assets", "Resources", "ElementSystem", "motion-templates.json")));
        string elements = File.ReadAllText(Path.Combine(root, "docs", "element-sistemi.json"));
        var rules = MechanicRules.FromJson(elements);
        Assert.That(rules.IsValid, Is.True);
        _grammar = new MechanicGrammar(rules);
        _prose = MiniJson.Parse(elements)["skills_prose_144"];
    }

    [Test]
    public void GrammarPositionSteps_AreOnlyThreeMovementSkills()
    {
        var found = new SortedSet<string>(StringComparer.Ordinal);
        foreach (MechanicWeapon weapon in _grammar.Rules.Weapons)
        {
            for (int verb = 1; verb <= 12; verb++)
            for (int adjective = 1; adjective <= 12; adjective++)
            {
                MechanicPlan plan = _grammar.Compose(verb, adjective, weapon);
                string stat = PlayerPositionStat(plan);
                if (stat == null)
                    continue;
                found.Add(plan.SkillId);
                if (plan.SkillId == "3-4")
                    Assert.That(stat, Is.EqualTo(PositionOwnership.ReturnMark));
                if (plan.SkillId == "3-6")
                    Assert.That(stat, Is.EqualTo(PositionOwnership.Displace));
                if (plan.SkillId == "3-9")
                    Assert.That(stat, Is.EqualTo(PositionOwnership.Behind));
            }
        }

        Assert.That(found, Is.EqualTo(new[] { "3-4", "3-6", "3-9" }));
        Assert.That(SkillName("3-4"), Is.EqualTo("Sabit Adım"));
        Assert.That(SkillName("3-6"), Is.EqualTo("Bağlayıcı Adım"));
        Assert.That(SkillName("3-9"), Is.EqualTo("Odaklı Adım"));
    }

    [Test]
    public void NoCast_MovesThePlayerTwice()
    {
        for (int verb = 1; verb <= 12; verb++)
        for (int adjective = 1; adjective <= 12; adjective++)
        {
            string id = verb + "-" + adjective;
            Assert.That(_motion.TryGet(id, out MotionBinding binding), Is.True, id);
            MechanicPlan plan = _grammar.Compose(verb, adjective, 1);
            bool grammarMoves = PlayerPositionStat(plan) != null;
            bool templateMoves = PositionOwnership.MovesPlayer(binding.Template);
            Assert.That(
                PositionOwnershipOracle.PositionWriters(templateMoves, grammarMoves),
                Is.LessThanOrEqualTo(1),
                id);
            if (!templateMoves || !grammarMoves)
                continue;

            PositionPlayback playback = PositionOwnership.Prepare(
                binding.Template,
                Steps(plan),
                _motion.Fallbacks.PhaseSec,
                _motion.Fallbacks.StepM);
            Assert.That(playback.OwnsPosition, Is.True, id);
            Assert.That(
                PositionOwnershipOracle.SuppressesMove(true, "konum", PlayerPositionStat(plan)),
                Is.True,
                id);
        }

        Assert.That(PositionOwnershipOracle.PositionWriters(templateMovesPlayer: true, grammarMovesPlayer: true), Is.EqualTo(1));
        Assert.That(PositionOwnershipOracle.PositionWriters(templateMovesPlayer: false, grammarMovesPlayer: true), Is.EqualTo(1));
        Assert.That(PositionOwnershipOracle.PositionWriters(templateMovesPlayer: false, grammarMovesPlayer: false), Is.EqualTo(0));
    }

    [Test]
    public void BaglayiciAdim_LandsBehindOnce_GrammarSwapIsNotApplied()
    {
        Assert.That(_motion.TryPlay("3-6", out MotionTemplate hook), Is.True);
        MechanicPlan plan = _grammar.Compose(3, 6, 1);
        Assert.That(PlayerPositionStat(plan), Is.EqualTo(PositionOwnership.Displace));

        PositionPlayback playback = PositionOwnership.Prepare(
            hook, Steps(plan), _motion.Fallbacks.PhaseSec, _motion.Fallbacks.StepM);
        Assert.That(playback.OwnsPosition, Is.True);
        Assert.That(playback.Template.Phases.Count, Is.EqualTo(hook.Phases.Count), "kalıp zaten arkaya iner");
        Assert.That(ReferenceEquals(playback.Template, hook), Is.True);

        const float bossZ = 4f;
        const float body = 0.5f;
        const float bossR = 0.85f;
        var samples = Tick(playback.Template, new MotionTarget(true, 0f, bossZ, bossR), body, 0.15f);
        float endZ = samples[^1].Z;
        Assert.That(endZ, Is.GreaterThan(bossZ), "boss'un arkası");
        Assert.That(MotionHitGeometry.EdgeGap(0f, endZ, body, 0f, bossZ, bossR), Is.GreaterThan(0.05f));
        Assert.That(samples[^1].Z, Is.EqualTo(samples.Max(s => s.Z)).Within(0.05f), "arkaya indikten sonra geri ışınlanmaz");

        float mirrorZ = bossZ + (bossZ - endZ);
        Assert.That(MathF.Abs(endZ - mirrorZ), Is.GreaterThan(1f));
        Assert.That(endZ, Is.Not.EqualTo(mirrorZ).Within(0.4f));
    }

    [Test]
    public void OdakliAdim_LandsBehindOnce_GrammarTeleportIsNotApplied()
    {
        Assert.That(_motion.TryPlay("3-9", out MotionTemplate blink), Is.True);
        Assert.That(blink.FamilyId, Is.EqualTo(23));
        MechanicPlan plan = _grammar.Compose(3, 9, 1);
        Assert.That(PlayerPositionStat(plan), Is.EqualTo(PositionOwnership.Behind));

        PositionPlayback playback = PositionOwnership.Prepare(
            blink, Steps(plan), _motion.Fallbacks.PhaseSec, _motion.Fallbacks.StepM);
        Assert.That(playback.OwnsPosition, Is.True);
        Assert.That(playback.Template.Phases.Count(p => p.Land == "behind"), Is.EqualTo(1));
        Assert.That(ReferenceEquals(playback.Template, blink), Is.True, "kalıp zaten arkaya iner, ikinci iniş eklenmez");

        const float bossZ = 4f;
        const float body = 0.5f;
        const float bossR = 0.85f;
        var samples = Tick(playback.Template, new MotionTarget(true, 0f, bossZ, bossR), body, 0.15f);
        float endZ = samples[^1].Z;
        Assert.That(endZ, Is.GreaterThan(bossZ), "tam arkada biter");
        Assert.That(MotionHitGeometry.EdgeGap(0f, endZ, body, 0f, bossZ, bossR), Is.GreaterThan(0.05f));
        Assert.That(samples[^1].Z, Is.EqualTo(samples.Max(s => s.Z)).Within(0.05f), "arkaya indikten sonra geri ışınlanmaz");
        AssertCleanBehind(blink, 4.5f);
        AssertCleanBehind(blink, 2.5f);
        float reach = MotionCastReach.EdgeReachM(blink);
        float gate = MotionCastReach.GateRangeM(reach, 0.5f);
        Assert.That(gate, Is.GreaterThan(4.5f - 0.85f), "4,5 m merkezden kilitlenir");
    }

    [Test]
    public void SabitAdim_CloseDash_StopsAtTheBossEdge()
    {
        Assert.That(_motion.TryPlay("3-4", out MotionTemplate pinned), Is.True);
        const float bossZ = 1.7f;
        const float body = 0.5f;
        const float bossR = 0.85f;
        const float gap = 0.15f;
        var target = new MotionTarget(true, 0f, bossZ, bossR);
        var runner = new MotionTemplateRunner();
        runner.Begin(pinned, 0f, 0f, 0f, 0f, 1f, body, gap);
        float minGap = 99f;
        for (int i = 0; i < 50 && !runner.Finished; i++)
        {
            runner.Tick(0.02f, target, default);
            float edge = MotionHitGeometry.EdgeGap(runner.X, runner.Z, body, 0f, bossZ, bossR);
            if (edge < minGap)
                minGap = edge;
            if (runner.Elapsed > 0.5f)
                break;
        }
        Assert.That(minGap, Is.GreaterThan(0.04f), "yakın dash boss'un içine girmez");
        Assert.That(runner.Z, Is.LessThan(bossZ - 0.4f), "öte yana geçmeden kenarda durur");
    }

    [Test]
    public void SabitAdim_DashesOut_ThenDashesBackToTheMark()
    {
        Assert.That(_motion.TryPlay("3-4", out MotionTemplate pinned), Is.True);
        Assert.That(pinned.FamilyId, Is.EqualTo(24));
        MechanicPlan plan = _grammar.Compose(3, 4, 1);
        Assert.That(PlayerPositionStat(plan), Is.EqualTo(PositionOwnership.ReturnMark));

        PositionPlayback playback = PositionOwnership.Prepare(
            pinned, Steps(plan), _motion.Fallbacks.PhaseSec, _motion.Fallbacks.StepM);
        Assert.That(playback.OwnsPosition, Is.True);
        Assert.That(playback.PlaceReturnMark, Is.True);
        Assert.That(playback.Template.Phases.Count(p => p.Motion == "return"), Is.EqualTo(1));
        Assert.That(ReferenceEquals(playback.Template, pinned), Is.True, "dönüş kalıbın içinde, ikinci faz eklenmez");

        const float bossZ = 4f;
        const float body = 0.5f;
        const float bossR = 0.85f;
        var runner = new MotionTemplateRunner();
        runner.Begin(playback.Template, 0f, 0f, 0f, 0f, 1f, body, 0.15f);
        Assert.That(runner.ReturnMarkPlaced, Is.True);
        Assert.That(runner.MarkZ, Is.EqualTo(0f).Within(0.001f));

        var target = new MotionTarget(true, 0f, bossZ, bossR);
        var stick = new MotionStick(false, 0f, 0f);
        float peakZ = 0f;
        float zBeforeReturn = 0f;
        bool sawReturn = false;
        float firstReturnStep = 99f;
        for (int i = 0; i < 400 && !runner.Finished; i++)
        {
            float beforeZ = runner.Z;
            float beforeT = runner.Elapsed;
            runner.Tick(0.02f, target, stick);
            if (runner.Z > peakZ)
                peakZ = runner.Z;
            if (!sawReturn && beforeT < 2.3f && runner.Elapsed >= 2.3f)
            {
                zBeforeReturn = beforeZ;
                sawReturn = true;
            }
            if (sawReturn && firstReturnStep > 90f && runner.Elapsed > 2.38f)
                firstReturnStep = MathF.Abs(runner.Z - zBeforeReturn);
        }

        Assert.That(runner.Finished, Is.True);
        Assert.That(peakZ, Is.GreaterThan(1.5f), "çapadan uzağa atılır");
        Assert.That(sawReturn, Is.True);
        Assert.That(firstReturnStep, Is.LessThan(1.2f), "dönüş tek karede ışınlanma değil");
        Assert.That(runner.Z, Is.EqualTo(0f).Within(0.15f), "işarete geri atılır");
        Assert.That(MotionHitGeometry.EdgeGap(runner.X, runner.Z, body, 0f, bossZ, bossR), Is.GreaterThan(0.05f));
    }

    [Test]
    public void MovingTemplate_SuppressesDisplace_AndUsesGrammarDistanceWhenTheCurveIsMissing()
    {
        var emptyDash = Moving(Phase("git", "dash", 0.2f, 0f));
        var step = new GrammarPositionStep(PositionOwnership.Displace, 2.4);
        PositionPlayback filled = PositionOwnership.Prepare(emptyDash, new[] { step }, 0.28f, 1.2f);
        Assert.That(filled.OwnsPosition, Is.True);
        Assert.That(PositionOwnershipOracle.SuppressesMove(true, "konum", PositionOwnership.Displace), Is.True);
        Assert.That(filled.Template.Phases[0].DistanceM, Is.EqualTo(2.4f).Within(0.001f));
        Assert.That(filled.Template.Phases.Count, Is.EqualTo(1));

        var samples = Tick(filled.Template, new MotionTarget(false, 0f, 0f), 0.5f, 0.15f);
        Assert.That(samples[^1].Z, Is.EqualTo(2.4f).Within(0.08f));
        Assert.That(samples[^1].Z, Is.EqualTo(samples.Max(s => s.Z)).Within(0.05f));
    }

    [Test]
    public void MovingTemplate_SuppressesBehindTeleport_AndAddsOneBehindLandingIfMissing()
    {
        var dash = Moving(Phase("git", "dash", 0.2f, 2f));
        var step = new GrammarPositionStep(PositionOwnership.Behind, 3.0);
        PositionPlayback playback = PositionOwnership.Prepare(dash, new[] { step }, 0.28f, 1.2f);
        Assert.That(playback.OwnsPosition, Is.True);
        Assert.That(PositionOwnershipOracle.SuppressesMove(true, "konum", PositionOwnership.Behind), Is.True);
        Assert.That(playback.Template.Phases.Count(p => p.Land == "behind"), Is.EqualTo(1));

        const float bossZ = 4f;
        var samples = Tick(playback.Template, new MotionTarget(true, 0f, bossZ, 0.85f), 0.5f, 0.15f);
        Assert.That(samples[^1].Z, Is.GreaterThan(bossZ));
        Assert.That(samples[^1].Z, Is.EqualTo(samples.Max(s => s.Z)).Within(0.05f));
    }

    [Test]
    public void ReturnToMark_PlacesTheMark_AndDashesBackWithoutEnteringTheBoss()
    {
        var dash = Moving(Phase("git", "dash", 0.2f, 3f));
        var step = new GrammarPositionStep(PositionOwnership.ReturnMark, 3.0);
        PositionPlayback playback = PositionOwnership.Prepare(dash, new[] { step }, 0.28f, 1.2f);
        Assert.That(playback.PlaceReturnMark, Is.True);
        Assert.That(playback.Template.Phases[^1].Motion, Is.EqualTo("return"));
        Assert.That(PositionOwnershipOracle.SuppressesMove(true, "konum", PositionOwnership.ReturnMark), Is.True);

        const float bossZ = 1.5f;
        const float body = 0.5f;
        const float bossR = 0.85f;
        const float gap = 0.15f;
        float separation = body + bossR + gap;
        var target = new MotionTarget(true, 0f, bossZ, bossR);
        var runner = new MotionTemplateRunner();
        runner.Begin(playback.Template, 0f, 0f, 0f, 0f, 1f, body, gap);
        Assert.That(runner.ReturnMarkPlaced, Is.True);
        Assert.That(runner.MarkZ, Is.EqualTo(0f).Within(0.001f));

        var stick = new MotionStick(false, 0f, 0f);
        float minReturnGap = 99f;
        float zAtDashEnd = 0f;
        bool sawReturn = false;
        float firstReturnStep = 99f;
        for (int i = 0; i < 80 && !runner.Finished; i++)
        {
            float beforeZ = runner.Z;
            float beforeT = runner.Elapsed;
            runner.Tick(0.02f, target, stick);
            if (beforeT < 0.2f - 0.001f && runner.Elapsed >= 0.2f - 0.001f)
                zAtDashEnd = beforeZ;
            if (runner.Elapsed > 0.21f)
            {
                if (!sawReturn)
                {
                    firstReturnStep = MathF.Abs(runner.Z - zAtDashEnd);
                    sawReturn = true;
                }
                float dz = runner.Z - bossZ;
                minReturnGap = MathF.Min(minReturnGap, MathF.Abs(dz));
            }
        }

        Assert.That(runner.Finished, Is.True);
        Assert.That(sawReturn, Is.True);
        Assert.That(zAtDashEnd, Is.GreaterThan(2.5f));
        Assert.That(firstReturnStep, Is.LessThan(1f), "dönüş tek karede ışınlanma değil");
        Assert.That(minReturnGap, Is.GreaterThanOrEqualTo(separation - 0.05f));
        Assert.That(runner.Z, Is.EqualTo(0f).Within(0.08f), "işarete döner");
    }

    [Test]
    public void StillTemplate_LetsGrammarMove_AndNonPositionStepsStay()
    {
        var still = new MotionTemplate(
            "dur", "dur", 1, "yerinde", true,
            new[] { Phase("dur", "hold", 0.2f, 0f) });
        var swap = new GrammarPositionStep(PositionOwnership.Displace, 3);
        PositionPlayback playback = PositionOwnership.Prepare(still, new[] { swap }, 0.28f, 1.2f);
        Assert.That(playback.OwnsPosition, Is.False);
        Assert.That(ReferenceEquals(playback.Template, still), Is.True);
        Assert.That(PositionOwnershipOracle.SuppressesMove(false, "konum", PositionOwnership.Displace), Is.False);
        Assert.That(PositionOwnershipOracle.SuppressesMove(false, "konum", PositionOwnership.Behind), Is.False);
        Assert.That(PositionOwnershipOracle.SuppressesMove(false, "konum", PositionOwnership.ReturnMark), Is.False);

        Assert.That(_motion.TryPlay("3-9", out MotionTemplate focused), Is.True);
        Assert.That(PositionOwnership.MovesPlayer(focused), Is.True);
        Assert.That(_motion.TryPlay("3-4", out MotionTemplate pinned), Is.True);
        Assert.That(PositionOwnership.MovesPlayer(pinned), Is.True);

        var moving = Moving(Phase("git", "dash", 0.2f, 2f));
        Assert.That(PositionOwnership.MovesPlayer(moving), Is.True);
        Assert.That(PositionOwnership.Kind("konum", "cek"), Is.EqualTo(PositionStepKind.None));
        Assert.That(PositionOwnership.Kind("konum", "it"), Is.EqualTo(PositionStepKind.None));
        Assert.That(PositionOwnership.Kind("konum", "portal"), Is.EqualTo(PositionStepKind.None));
        Assert.That(PositionOwnership.Kind("hiz", "hareket"), Is.EqualTo(PositionStepKind.None));
        Assert.That(PositionOwnership.Kind("deger", "can"), Is.EqualTo(PositionStepKind.None));
        Assert.That(PositionOwnershipOracle.SuppressesMove(true, "konum", "cek"), Is.False);
        Assert.That(PositionOwnershipOracle.SuppressesMove(true, "hiz", "hareket"), Is.False);
    }

    [Test]
    public void SuppressedPositionStep_IsLoggedOncePerSkill()
    {
        int logs = 0;
        DesignWarnings.Warned += _ => logs++;
        PositionOwnership.LogSuppressed("3-6", PositionOwnership.Displace);
        PositionOwnership.LogSuppressed("3-6", PositionOwnership.Behind);
        Assert.That(logs, Is.EqualTo(1));
        Assert.That(DesignWarnings.WasWarned("motion.pos.3-6"), Is.True);
        PositionOwnership.LogSuppressed("3-9", PositionOwnership.Behind);
        Assert.That(logs, Is.EqualTo(2));
    }

    static string PlayerPositionStat(MechanicPlan plan)
    {
        foreach (MechanicEffect effect in plan.Effects)
        {
            if (PositionOwnership.Kind(effect.Atom, effect.Stat) != PositionStepKind.None)
                return effect.Stat;
        }
        return null;
    }

    static GrammarPositionStep[] Steps(MechanicPlan plan)
    {
        var list = new List<GrammarPositionStep>();
        foreach (MechanicEffect effect in plan.Effects)
        {
            if (PositionOwnership.Kind(effect.Atom, effect.Stat) == PositionStepKind.None)
                continue;
            list.Add(new GrammarPositionStep(effect.Stat, effect.Amount));
        }
        return list.ToArray();
    }

    string SkillName(string id) => _prose["entries"]["3"][id]["name"].AsString();

    static MotionTemplate Moving(params MotionPhase[] phases) =>
        new MotionTemplate("deneme", "deneme", 1, "deneme", true, phases);

    static MotionPhase Phase(string name, string motion, float sec, float distance) =>
        new MotionPhase(
            name, motion, sec, "travel", "none", string.Empty, 0f,
            distance, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, null, null);

    static void AssertCleanBehind(MotionTemplate template, float bossZ)
    {
        const float body = 0.5f;
        const float bossR = 0.85f;
        var target = new MotionTarget(true, 0f, bossZ, bossR);
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, body, 0.15f);
        float minGap = 99f;
        var stick = new MotionStick(false, 0f, 0f);
        for (int i = 0; i < 80 && !runner.Finished; i++)
        {
            runner.Tick(0.02f, target, stick);
            float edge = MotionHitGeometry.EdgeGap(runner.X, runner.Z, body, 0f, bossZ, bossR);
            if (edge < minGap)
                minGap = edge;
        }
        Assert.That(runner.Finished, Is.True, bossZ.ToString("0.0"));
        Assert.That(runner.Z, Is.GreaterThan(bossZ), "arkada biter " + bossZ.ToString("0.0"));
        Assert.That(MotionHitGeometry.EdgeGap(runner.X, runner.Z, body, 0f, bossZ, bossR), Is.GreaterThan(0.05f));
        Assert.That(minGap, Is.GreaterThan(0.04f), "gövdenin içinden geçmez " + bossZ.ToString("0.0"));
    }

    static List<MotionTick> Tick(MotionTemplate template, MotionTarget target, float body, float gap)
    {
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, body, gap);
        var stick = new MotionStick(false, 0f, 0f);
        var samples = new List<MotionTick>();
        for (int i = 0; i < 400 && !runner.Finished; i++)
        {
            MotionTick tick = runner.Tick(0.02f, target, stick);
            samples.Add(tick);
            if (tick.Finished)
                break;
        }
        Assert.That(samples, Is.Not.Empty);
        Assert.That(runner.Finished, Is.True);
        return samples;
    }

    static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", ".."));
}
