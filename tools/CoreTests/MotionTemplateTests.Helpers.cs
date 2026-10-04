using Dovus.Core;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace CoreTests;

public partial class MotionTemplateTests
{

    static CastSample SampleCast(MotionTemplate template, MotionTarget target, float body, float stop)
    {
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, body, stop);
        var hits = new List<MotionHit>();
        var gaps = new List<float>();
        float peakZ = 0f;
        float peakY = 0f;
        var stick = new MotionStick(false, 0f, 0f);
        for (int i = 0; i < 800 && !runner.Finished; i++)
        {
            MotionTick tick = runner.Tick(0.02f, target, stick);
            if (tick.Hits != null)
                hits.AddRange(tick.Hits);
            if (tick.Z > peakZ)
                peakZ = tick.Z;
            if (tick.Y > peakY)
                peakY = tick.Y;
            gaps.Add(MotionHitGeometry.EdgeGap(tick.X, tick.Z, body, target.X, target.Z, target.RadiusM));
        }
        Assert.That(runner.Finished, Is.True, template.Id);
        return new CastSample(runner.X, runner.Y, runner.Z, peakY, peakZ, hits, gaps);
    }

    readonly struct CastSample
    {
        public CastSample(float x, float y, float z, float peakY, float peakZ, List<MotionHit> hits, List<float> gaps)
        {
            X = x;
            Y = y;
            Z = z;
            PeakY = peakY;
            PeakZ = peakZ;
            Hits = hits;
            Gaps = gaps;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float PeakY { get; }
        public float PeakZ { get; }
        public List<MotionHit> Hits { get; }
        public List<float> Gaps { get; }
    }

    static MotionTemplate Ready(string id)
    {
        string path = JsonPath();
        var catalog = MotionTemplateCatalog.FromJson(File.ReadAllText(path));
        Assert.That(catalog.TryGetTemplate(id, out MotionTemplate template), Is.True, id);
        Assert.That(template.Implemented, Is.True, id);
        return template;
    }

    static List<string> Tags(string skill)
    {
        string path = JsonPath();
        var catalog = MotionTemplateCatalog.FromJson(File.ReadAllText(path));
        Assert.That(catalog.TryGet(skill, out MotionBinding binding), Is.True, skill);
        return new List<string>(binding.Tags);
    }

    static Sample Play(MotionTemplate template, float seconds, MotionTarget target, float moveX = 0f, float bodyRadius = 0.5f)
    {
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, bodyRadius, 0.15f);
        var hits = new List<MotionHit>();
        float peakY = 0f;
        float peakZ = 0f;
        float midFaceZ = 1f;
        bool midTaken = false;
        float left = seconds;
        while (left > 0f && !runner.Finished)
        {
            float dt = System.Math.Min(0.02f, left);
            MotionTick tick = runner.Tick(dt, target, new MotionStick(false, moveX, 0f));
            if (tick.Hits != null)
                hits.AddRange(tick.Hits);
            if (tick.Y > peakY)
                peakY = tick.Y;
            if (tick.Z > peakZ)
                peakZ = tick.Z;
            if (!midTaken && runner.Elapsed >= seconds * 0.45f)
            {
                midFaceZ = tick.FaceZ;
                midTaken = true;
            }
            left -= dt;
        }
        return new Sample(runner.X, runner.Y, runner.Z, runner.FaceX, runner.FaceZ, peakY, peakZ, midFaceZ, hits);
    }

    static List<MotionHit> Step(MotionTemplateRunner runner, float seconds, MotionTarget target, bool held)
    {
        var hits = new List<MotionHit>();
        float left = seconds;
        while (left > 0f && !runner.Finished)
        {
            float dt = System.Math.Min(0.02f, left);
            MotionTick tick = runner.Tick(dt, target, new MotionStick(held, 0f, 0f));
            if (tick.Hits != null)
                hits.AddRange(tick.Hits);
            left -= dt;
        }
        return hits;
    }

    readonly struct Sample
    {
        public Sample(float x, float y, float z, float faceX, float faceZ, float peakY, float peakZ, float midFaceZ, List<MotionHit> hits)
        {
            X = x;
            Y = y;
            Z = z;
            FaceX = faceX;
            FaceZ = faceZ;
            PeakY = peakY;
            PeakZ = peakZ;
            MidFaceZ = midFaceZ;
            Hits = hits;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float FaceX { get; }
        public float FaceZ { get; }
        public float PeakY { get; }
        public float PeakZ { get; }
        public float MidFaceZ { get; }
        public List<MotionHit> Hits { get; }
    }

    static string JsonPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "unity", "Assets", "Resources", "ElementSystem", "motion-templates.json"));
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }

    static string RepoRoot()
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", ".."));
    }
}
