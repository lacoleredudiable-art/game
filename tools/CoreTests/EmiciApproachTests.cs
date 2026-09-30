using System.IO;
using Dovus.Core.Motion;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// Play taraması: 3-2 içinden geçmeli, 1-2 ve 4-2 yerinde kalıp boss'u çekmeli,
/// 6-2 geri çekilirken tek karede 0,15 m'den fazla sıçramamalı.
/// </summary>
[TestFixture]
public class EmiciApproachTests
{
    const float Body = 0.5f;
    const float BossR = 0.85f;
    const float Stop = 0.15f;
    const float Contact = Body + BossR;

    MotionTemplateCatalog _catalog;

    [SetUp]
    public void Load()
    {
        _catalog = MotionTemplateCatalog.FromJson(File.ReadAllText(MotionPath()));
    }

    [Test]
    public void EmiciAdim_DashesThrough_EvenWhileAPullIsHeld()
    {
        Assert.That(_catalog.TryPlay("3-2", out MotionTemplate template), Is.True);
        Assert.That(EmiciApproach.TemplatePassesThrough(template), Is.True);
        Assert.That(EmiciApproach.ShouldHoldCaster("2", template), Is.False);
        Assert.That(EmiciApproach.Freezes(template.Phases[0], holdApproach: true), Is.False);

        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, Body, Stop);
        var boss = new MotionTarget(true, 0f, 3f, BossR, holdApproach: true);
        for (int i = 0; i < 120 && !runner.Finished; i++)
            runner.Tick(1f / 60f, boss, default);

        float fromBoss = MathF.Abs(runner.Z - 3f);
        Assert.That(runner.Z, Is.GreaterThan(3f + Contact), "boss'un arkasına inmeli");
        Assert.That(fromBoss, Is.GreaterThanOrEqualTo(Contact - 0.02f));
        Assert.That(runner.Z, Is.GreaterThan(4f));
    }

    [Test]
    public void EmiciVurusAndKalkan_StayPut_WhileTheBossIsPulledToContact()
    {
        foreach (string id in new[] { "1-2", "4-2" })
        {
            Assert.That(_catalog.TryPlay(id, out MotionTemplate template), Is.True, id);
            Assert.That(EmiciApproach.ShouldHoldCaster("2", template), Is.True, id);
            Assert.That(EmiciApproach.TemplatePassesThrough(template), Is.False, id);

            var runner = new MotionTemplateRunner();
            runner.Begin(template, 0f, 0f, 0f, 0f, 1f, Body, Stop);
            var boss = new MotionTarget(true, 0f, 3f, BossR, holdApproach: true);
            for (int i = 0; i < 180 && !runner.Finished; i++)
                runner.Tick(1f / 60f, boss, default);
            Assert.That(runner.Z, Is.EqualTo(0f).Within(0.02f), id + " oyuncu yürüdü");

            float bossZ = 3f;
            float speed = 0f;
            float sx = 0f;
            float sz = 1f;
            bool pulling = false;
            float maxStep = 0f;
            float homeX = 0f;
            for (int i = 0; i < 90; i++)
            {
                float z0 = bossZ;
                bool already = pulling;
                EmiciPull.Retarget(
                    already, homeX, bossZ, 0f, 0f, Body, BossR,
                    ref sx, ref sz, ref speed,
                    out float tx, out float tz, out pulling);
                EmiciPull.StepToward(ref homeX, ref bossZ, tx, tz, speed, 1f / 60f, out bool arrived);
                maxStep = MathF.Max(maxStep, MathF.Abs(bossZ - z0));
                if (arrived)
                    break;
            }

            float gap = MathF.Abs(bossZ - 0f);
            Assert.That(gap, Is.EqualTo(Contact).Within(0.05f), id + " temas");
            Assert.That(maxStep, Is.LessThanOrEqualTo(0.15f), id + " boss sıçraması");
        }
    }

    [Test]
    public void EmiciBag_RetreatsWithoutAOneFrameJump()
    {
        Assert.That(_catalog.TryPlay("6-2", out MotionTemplate template), Is.True);
        Assert.That(EmiciApproach.ShouldHoldCaster("2", template), Is.False);
        MotionPhase retreat = null;
        for (int i = 0; i < template.Phases.Count; i++)
        {
            if (template.Phases[i].Motion == "retreat")
                retreat = template.Phases[i];
        }
        Assert.That(retreat, Is.Not.Null);
        Assert.That(EmiciApproach.Freezes(retreat, holdApproach: true), Is.False);

        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, Body, Stop);
        var boss = new MotionTarget(true, 0f, 3f, BossR, holdApproach: true);
        float maxStep = 0f;
        for (int i = 0; i < 180 && !runner.Finished; i++)
        {
            float z0 = runner.Z;
            float x0 = runner.X;
            runner.Tick(1f / 60f, boss, default);
            float step = MathF.Sqrt((runner.X - x0) * (runner.X - x0) + (runner.Z - z0) * (runner.Z - z0));
            if (step > maxStep)
                maxStep = step;
        }

        Assert.That(runner.Z, Is.LessThan(-1f), "geri çekilmeli");
        Assert.That(maxStep, Is.LessThanOrEqualTo(0.15f));
    }

    [Test]
    public void EmiciStaySkills_ExpectYerinde_PassThroughDoesNot()
    {
        Assert.That(_catalog.TryPlay("1-2", out MotionTemplate claw), Is.True);
        Assert.That(_catalog.TryPlay("4-2", out MotionTemplate ward), Is.True);
        Assert.That(_catalog.TryPlay("3-2", out MotionTemplate step), Is.True);
        Assert.That(EmiciApproach.SweepStayCategory("2", claw), Is.EqualTo("yerinde"));
        Assert.That(EmiciApproach.SweepStayCategory("2", ward), Is.EqualTo("yerinde"));
        Assert.That(EmiciApproach.SweepStayCategory("2", step), Is.Null);
    }

    [Test]
    public void StationaryEmici_DoesNotTeleportWhenTheBossCrosses()
    {
        foreach (string id in new[] { "5-2", "10-2", "11-2", "12-2" })
        {
            Assert.That(_catalog.TryPlay(id, out MotionTemplate template), Is.True, id);
            var runner = new MotionTemplateRunner();
            runner.Begin(template, 0f, 0f, 0f, 0f, 1f, Body, Stop);
            float maxStep = 0f;
            for (int i = 0; i < 400 && !runner.Finished; i++)
            {
                float u = MathF.Min(1f, i / 70f);
                float bossZ = 3f + (-1f - 3f) * u;
                float x0 = runner.X;
                float z0 = runner.Z;
                runner.Tick(1f / 60f, new MotionTarget(true, 0f, bossZ, BossR), default);
                float step = MathF.Sqrt((runner.X - x0) * (runner.X - x0) + (runner.Z - z0) * (runner.Z - z0));
                if (step > maxStep)
                    maxStep = step;
            }

            Assert.That(maxStep, Is.LessThanOrEqualTo(0.15f), id);
            Assert.That(runner.X, Is.EqualTo(0f).Within(0.05f), id);
            Assert.That(runner.Z, Is.EqualTo(0f).Within(0.05f), id + " yerinde");
        }
    }

    static string MotionPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "unity", "Assets", "Resources", "ElementSystem", "motion-templates.json"));
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }
}
