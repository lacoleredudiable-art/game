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
    static float RunPull(MotionPhase phase, MotionTarget ally, float startY, out float endY, out float minBoss)
    {
        var template = new MotionTemplate("kanca", "kanca", 7, "kanca", true, new[] { phase }, MotionAim.Effect);
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, startY, 0f, 0f, 1f, Body, Stop);
        minBoss = 99f;
        while (!runner.Finished)
        {
            runner.Tick(1f / 60f, ally, default);
            float d = MathF.Sqrt(runner.X * runner.X + (runner.Z - 2f) * (runner.Z - 2f));
            if (d < minBoss)
                minBoss = d;
        }
        endY = runner.Y;
        return runner.Z;
    }

    static float FinishZ(MotionTemplate template, float bossR)
    {
        var runner = new MotionTemplateRunner();
        var boss = new MotionTarget(true, 0f, 3f, bossR);
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, Body, Stop);
        while (!runner.Finished)
            runner.Tick(1f / 60f, boss, default);
        return runner.Z;
    }

    [Test]
    public void Pull_Eases_AndDoesNotOverlap()
    {
        DisplacementEase.Sample(0f, 0f, 3f, 0f, 0f, out float x, out float z);
        Assert.That(x, Is.EqualTo(0f).Within(0.001f));
        DisplacementEase.Sample(0f, 0f, 3f, 0f, 1f, out x, out z);
        Assert.That(x, Is.EqualTo(3f).Within(0.001f));
        DisplacementEase.Sample(0f, 0f, 3f, 0f, 0.5f, out x, out _);
        Assert.That(x, Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(x, Is.LessThan(3f), "orta kare henüz bitiş değil");

        float px = 0f, pz = 0f;
        DisplacementEase.KeepSeparated(ref px, ref pz, 0f, 0f, Contact);
        Assert.That(MathF.Sqrt(px * px + pz * pz), Is.GreaterThanOrEqualTo(Contact - 0.001f));

        var board = new StatusBoard();
        Assert.That(ForcedDisplacement.Allows(null), Is.True);
        Assert.That(ForcedDisplacement.Allows(board), Is.True);
        board.Apply(StatusKind.Stasis, 500, 1f);
        Assert.That(ForcedDisplacement.Allows(board), Is.False);
    }

    void AssertOutside(string id)
    {
        Assert.That(_catalog.TryPlay(id, out MotionTemplate template), Is.True, id);
        Trace run = Play(template, new MotionTarget(true, 0f, 3f, BossR), followCaster: false);
        Assert.That(run.MinBoss, Is.GreaterThanOrEqualTo(Contact - 0.05f), id + " " + run.MinBoss.ToString("0.00"));
    }

    void AssertCast(VerbExecutionData data, string id, int verb, int adjective, float engineScale)
    {
        Assert.That(_catalog.TryPlay(id, out MotionTemplate template), Is.True, id);
        float edge = Edge(data, verb, adjective, engineScale, template);
        Assert.That(
            MotionCastReach.CenterInReach(3f, Body, BossR, edge),
            Is.True,
            id + " kenar " + edge.ToString("0.00"));
    }

    bool Reaches(VerbExecutionData data, int verb, int adjective, float engineScale)
    {
        float edge = Edge(data, verb, adjective, engineScale, template: null);
        return MotionCastReach.CenterInReach(3f, Body, BossR, edge);
    }

    static float Edge(VerbExecutionData data, int verb, int adjective, float engineScale, MotionTemplate template)
    {
        Assert.That(data.TryGetHitbox(verb, out VerbHitboxSpec spec), Is.True);
        float scale = HitboxSizing.AdjectiveScale(data.AdjectiveSizeMult(adjective), engineScale);
        float reach = HitboxSizing.Resolve(spec, 1f, scale).ReachM;
        return MotionCastReach.ComboEdgeReach(reach, template);
    }

    static MotionTarget Resolve(MotionTemplate template, MotionDeliveryAim.Kind kind)
    {
        if (kind == MotionDeliveryAim.Kind.Ally && !MotionDeliveryAim.SwapsPastBody(template))
            return new MotionTarget(true, -2.2f, 0.4f, Body);
        return new MotionTarget(true, 0f, 3f, BossR);
    }

    static void VerbFace(string id, out string mode, out string action)
    {
        int verb = int.Parse(id.Split('-')[0]);
        (mode, action) = verb switch
        {
            3 => ("self_only", "dash"),
            4 => ("self_only", "shield"),
            8 => ("self_or_ally", "buff"),
            9 => ("self_or_ally", "cleanse"),
            10 => ("self_only", "reflect"),
            11 => ("self_only", "summon"),
            _ => ("enemy_only", "damage")
        };
    }

    static Trace Play(MotionTemplate template, MotionTarget target, bool followCaster)
    {
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, Body, Stop);
        var hits = new List<MotionHit>();
        float min = 99f;
        int reversals = 0;
        float prevX = 0f, prevZ = 0f;
        bool hasStep = false;
        for (int i = 0; i < 2400 && !runner.Finished; i++)
        {
            float x0 = runner.X;
            float z0 = runner.Z;
            MotionTarget aim = target;
            if (followCaster)
                aim = new MotionTarget(true, runner.X, runner.Z, Body);
            MotionTick tick = runner.Tick(1f / 60f, aim, default);
            float dx = runner.X - x0;
            float dz = runner.Z - z0;
            float boss = MathF.Sqrt(runner.X * runner.X + (runner.Z - 3f) * (runner.Z - 3f));
            if (boss < min)
                min = boss;
            float step = MathF.Sqrt(dx * dx + dz * dz);
            if (hasStep && step > 0.2f)
            {
                float prev = MathF.Sqrt(prevX * prevX + prevZ * prevZ);
                if (prev > 0.2f && (dx * prevX + dz * prevZ) / (step * prev) < -0.5f)
                    reversals++;
            }
            if (step > 0.02f)
            {
                prevX = dx;
                prevZ = dz;
                hasStep = true;
            }
            if (tick.Hits != null)
                hits.AddRange(tick.Hits);
        }
        return new Trace(min, reversals, hits);
    }

    readonly struct Trace
    {
        public Trace(float minBoss, int reversals, List<MotionHit> hits)
        {
            MinBoss = minBoss;
            Reversals = reversals;
            Hits = hits;
        }

        public float MinBoss { get; }
        public int Reversals { get; }
        public List<MotionHit> Hits { get; }
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

    static string ElementPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "docs", "element-sistemi.json"));
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }
}
