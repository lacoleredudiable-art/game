using System;
using System.Collections.Generic;
using Dovus.Core.Combat;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class DrawRecognitionTests
{
    const float CornerRadius = 200f;
    const float DotRadius = 34f;
    const float MinSegment = 12f;
    const float Settle = 1.5f;
    const float SpeedPxPerSec = 900f;
    const float NoisePx = 6f;
    const int SeedsPerScenario = 50;

    static readonly float[] DotX = new float[6];
    static readonly float[] DotY = new float[6];

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        for (int i = 0; i < 6; i++)
        {
            double ang = i * Math.PI / 3.0;
            DotX[i] = CornerRadius * (float)Math.Cos(ang);
            DotY[i] = CornerRadius * (float)Math.Sin(ang);
        }
    }

    static List<int> TraceStroke(IReadOnlyList<float> sx, IReadOnlyList<float> sy)
    {
        var tracker = new StrokeDotTracker();
        var step = new List<int>();
        var acc = new List<int>();
        tracker.Begin(sx[0], sy[0], DotRadius, MinSegment, Settle, DotX, DotY, step);
        acc.AddRange(step);
        for (int i = 1; i < sx.Count - 1; i++)
        {
            tracker.Move(sx[i], sy[i], DotX, DotY, step);
            acc.AddRange(step);
        }
        tracker.End(sx[sx.Count - 1], sy[sy.Count - 1], DotX, DotY, step);
        acc.AddRange(step);
        return acc;
    }

    static void SamplePolyline(
        IReadOnlyList<(float x, float y)> waypoints,
        float fps,
        int seed,
        List<float> sx,
        List<float> sy)
    {
        sx.Clear();
        sy.Clear();
        var rng = new Random(seed);
        float dt = 1f / fps;
        float stepLen = SpeedPxPerSec * dt;

        for (int w = 0; w < waypoints.Count - 1; w++)
        {
            float ax = waypoints[w].x;
            float ay = waypoints[w].y;
            float bx = waypoints[w + 1].x;
            float by = waypoints[w + 1].y;
            float dx = bx - ax;
            float dy = by - ay;
            float seg = (float)Math.Sqrt(dx * dx + dy * dy);
            if (seg < 1e-4f)
                continue;
            float ux = dx / seg;
            float uy = dy / seg;
            float px = -uy;
            float py = ux;
            float t = 0f;
            if (sx.Count == 0)
            {
                float n0 = (float)(rng.NextDouble() * 2.0 - 1.0) * NoisePx;
                sx.Add(ax + px * n0);
                sy.Add(ay + py * n0);
            }
            while (t + stepLen < seg)
            {
                t += stepLen;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0) * NoisePx;
                sx.Add(ax + ux * t + px * n);
                sy.Add(ay + uy * t + py * n);
            }

            float nEnd = (float)(rng.NextDouble() * 2.0 - 1.0) * NoisePx;
            sx.Add(bx + px * nEnd);
            sy.Add(by + py * nEnd);
        }
    }

    static float Dist(float ax, float ay, float bx, float by)
    {
        float dx = bx - ax;
        float dy = by - ay;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    static (float x, float y) DotCenter(int dot1Based) =>
        (DotX[dot1Based - 1], DotY[dot1Based - 1]);

    static (float x, float y) PointOnDotRing(int dot1Based, float distFromCenter)
    {
        var (cx, cy) = DotCenter(dot1Based);
        float len = (float)Math.Sqrt(cx * cx + cy * cy);
        if (len < 1e-4f)
            return (cx + distFromCenter, cy);
        float ux = cx / len;
        float uy = cy / len;
        return (cx + ux * distFromCenter, cy + uy * distFromCenter);
    }

    static List<(float x, float y)> Straight123Waypoints()
    {
        return new List<(float, float)>
        {
            DotCenter(1),
            DotCenter(2),
            DotCenter(3)
        };
    }

    static List<(float x, float y)> CornerTurnWaypoints()
    {
        var d1 = DotCenter(1);
        var d2 = DotCenter(2);
        var d3 = DotCenter(3);
        float ax = d2.x - d1.x;
        float ay = d2.y - d1.y;
        float len12 = (float)Math.Sqrt(ax * ax + ay * ay);
        ax /= len12;
        ay /= len12;
        float bx = d3.x - d2.x;
        float by = d3.y - d2.y;
        float len23 = (float)Math.Sqrt(bx * bx + by * by);
        bx /= len23;
        by /= len23;
        float inset = 55f;
        return new List<(float, float)>
        {
            d1,
            (d2.x - ax * inset, d2.y - ay * inset),
            d2,
            (d2.x + bx * inset, d2.y + by * inset),
            d3
        };
    }

    static List<(float x, float y)> RingFinish14Waypoints()
    {
        var d1 = DotCenter(1);
        var d4 = DotCenter(4);
        var end = PointOnDotRing(4, 30f);
        return new List<(float, float)> { d1, d4, end };
    }

    static List<(float x, float y)> FalsePositive14Waypoints()
    {
        var d1 = DotCenter(1);
        var d4 = DotCenter(4);
        float graze = DotRadius * 0.85f;
        var mid = OffsetFromCenterTowardPerp(d1, d4, graze);
        return new List<(float, float)> { d1, mid, d4 };
    }

    static (float x, float y) OffsetFromCenterTowardPerp(
        (float x, float y) a, (float x, float y) b, float targetDistFromDot)
    {
        float mx = (a.x + b.x) * 0.5f;
        float my = (a.y + b.y) * 0.5f;
        float dx = b.x - a.x;
        float dy = b.y - a.y;
        float len = (float)Math.Sqrt(dx * dx + dy * dy);
        float px = -dy / len;
        float py = dx / len;
        float bestX = mx;
        float bestY = my;
        float bestErr = float.MaxValue;
        for (int i = 0; i < 6; i++)
        {
            if (i + 1 == 1 || i + 1 == 4)
                continue;
            float cx = DotX[i];
            float cy = DotY[i];
            for (int s = -1; s <= 1; s += 2)
            {
                float ox = mx + px * s * 80f;
                float oy = my + py * s * 80f;
                float d = Dist(ox, oy, cx, cy);
                float err = Math.Abs(d - targetDistFromDot);
                if (err < bestErr)
                {
                    bestErr = err;
                    bestX = ox;
                    bestY = oy;
                }
            }
        }
        return (bestX, bestY);
    }

    static bool SequenceEquals(IReadOnlyList<int> got, IReadOnlyList<int> expected)
    {
        if (got.Count != expected.Count)
            return false;
        for (int i = 0; i < got.Count; i++)
        {
            if (got[i] != expected[i])
                return false;
        }
        return true;
    }

    static float HitRate(string name, float fps, int seedBase,
        Func<List<(float x, float y)>> waypoints,
        IReadOnlyList<int> expected)
    {
        var sx = new List<float>();
        var sy = new List<float>();
        int hits = 0;
        for (int s = 0; s < SeedsPerScenario; s++)
        {
            SamplePolyline(waypoints(), fps, seedBase + s, sx, sy);
            var got = TraceStroke(sx, sy);
            if (SequenceEquals(got, expected))
                hits++;
        }
        float rate = hits / (float)SeedsPerScenario;
        TestContext.WriteLine($"{name} / {fps} fps / {rate:P1}");
        return rate;
    }

    [Test]
    public void StraightPass_123()
    {
        var expected = new List<int> { 1, 2, 3 };
        float r60 = HitRate("StraightPass", 60f, 1000, Straight123Waypoints, expected);
        float r30 = HitRate("StraightPass", 30f, 2000, Straight123Waypoints, expected);
        Assert.That(r60, Is.EqualTo(1f));
        Assert.That(r30, Is.EqualTo(1f));
    }

    [Test]
    public void CornerTurn_123()
    {
        var expected = new List<int> { 1, 2, 3 };
        float r60 = HitRate("CornerTurn", 60f, 3000, CornerTurnWaypoints, expected);
        float r30 = HitRate("CornerTurn", 30f, 4000, CornerTurnWaypoints, expected);
        Assert.That(r60, Is.EqualTo(1f));
        Assert.That(r30, Is.EqualTo(1f));
    }

    [Test]
    public void RingFinish_14()
    {
        var expected = new List<int> { 1, 4 };
        float r60 = HitRate("RingFinish", 60f, 5000, RingFinish14Waypoints, expected);
        float r30 = HitRate("RingFinish", 30f, 6000, RingFinish14Waypoints, expected);
        Assert.That(r60, Is.EqualTo(1f));
        Assert.That(r30, Is.EqualTo(1f));
    }

    [Test]
    public void FalsePositive_GrazeNeighbors_14Only()
    {
        var expected = new List<int> { 1, 4 };
        var sx = new List<float>();
        var sy = new List<float>();
        int hits = 0;
        for (int s = 0; s < SeedsPerScenario; s++)
        {
            SamplePolyline(FalsePositive14Waypoints(), 60f, 7000 + s, sx, sy);
            var got = TraceStroke(sx, sy);
            if (SequenceEquals(got, expected))
                hits++;
        }
        float rate60 = hits / (float)SeedsPerScenario;
        TestContext.WriteLine($"FalsePositive / 60 fps / {rate60:P1}");
        Assert.That(rate60, Is.EqualTo(1f));

        hits = 0;
        for (int s = 0; s < SeedsPerScenario; s++)
        {
            SamplePolyline(FalsePositive14Waypoints(), 30f, 8000 + s, sx, sy);
            var got = TraceStroke(sx, sy);
            if (SequenceEquals(got, expected))
                hits++;
        }
        float rate30 = hits / (float)SeedsPerScenario;
        TestContext.WriteLine($"FalsePositive / 30 fps / {rate30:P1}");
        Assert.That(rate30, Is.EqualTo(1f));
    }
}
