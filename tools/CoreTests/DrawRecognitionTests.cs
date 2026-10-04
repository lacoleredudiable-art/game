using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CoreTests;

/// <summary>
/// Çizim tanıma ölçümü (denetim B ek). Altıgende rün yolları gürültülü, elle çizilmiş gibi üretilir:
/// hedef nişan hatası σ 8 dp, yay ±%12, 1,5 dp titreme, segment 80–200 ms (hızlı: 50–90 ms),
/// %10 kare atlaması, 120 / 60 / 30 fps örnekleme; yarısı merkezden, yarısı ilk noktadan başlar.
/// Geçme = kayıtlı nokta dizisi hedef diziyle BİREBİR aynı (eksik ya da fazla rün yok).
/// Eski kural (yalnız örnek noktası) ile yeni kural (<see cref="StrokeDotTracker"/>) aynı yollarda ölçülür.
/// Ölçü dp: altıgen yarıçapı 104 (telefon tabanı) ve 112, nokta yarıçapı 34 (GameTuning).
/// </summary>
[TestFixture]
public class DrawRecognitionTests
{
    const float DotR = 34f;
    const float MinSeg = 12f;
    const float Settle = 1.5f;
    const int PerShape = 300;

    static readonly (string Name, int[] Seq)[] Shapes =
    {
        ("komşu", new[] { 1, 2 }),
        ("uzak", new[] { 1, 3 }),
        ("karşı", new[] { 1, 4 }),
        ("zincir", new[] { 1, 2, 3 }),
        ("üçgen", new[] { 1, 3, 5 }),
        ("altıgen", new[] { 1, 2, 3, 4 }),
        ("zigzag", new[] { 1, 3, 2, 4 }),
        ("karşı-dön", new[] { 1, 4, 2 }),
    };

    static (float[] X, float[] Y) Dots(float r)
    {
        var x = new float[6];
        var y = new float[6];
        for (int i = 0; i < 6; i++)
        {
            double a = (90 - 60 * i) * Math.PI / 180.0;
            x[i] = (float)(r * Math.Cos(a));
            y[i] = (float)(r * Math.Sin(a));
        }
        return (x, y);
    }

    static double Gauss(Random rng, double sigma)
    {
        double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
        return sigma * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    static List<(float x, float y)> Generate(int[] seq, float[] dx, float[] dy, Random rng, int fps, bool fast)
    {
        const double aim = 8.0;
        (double x, double y) start;
        int first = 0;
        if (rng.NextDouble() < 0.5)
        {
            start = (Gauss(rng, 6), Gauss(rng, 6));
        }
        else
        {
            start = (dx[seq[0] - 1] + Gauss(rng, aim), dy[seq[0] - 1] + Gauss(rng, aim));
            first = 1;
        }

        var segs = new List<((double x, double y) a, (double x, double y) b, double dur, double bow, double dwell)>();
        var cur = start;
        for (int i = first; i < seq.Length; i++)
        {
            var tgt = (dx[seq[i] - 1] + Gauss(rng, aim), dy[seq[i] - 1] + Gauss(rng, aim));
            double dur = fast ? 0.05 + rng.NextDouble() * 0.04 : 0.08 + rng.NextDouble() * 0.12;
            double bow = -0.12 + rng.NextDouble() * 0.24;
            segs.Add((cur, tgt, dur, bow, rng.NextDouble() * 0.04));
            cur = tgt;
        }

        (double x, double y)? Pos(double t)
        {
            foreach (var s in segs)
            {
                if (t <= s.dur)
                {
                    double u = t / s.dur;
                    double mx = (s.a.x + s.b.x) / 2, my = (s.a.y + s.b.y) / 2;
                    double len = Math.Max(1e-6, Math.Sqrt((s.b.x - s.a.x) * (s.b.x - s.a.x) + (s.b.y - s.a.y) * (s.b.y - s.a.y)));
                    double px = -(s.b.y - s.a.y) / len, py = (s.b.x - s.a.x) / len;
                    double cx = mx + px * s.bow * len, cy = my + py * s.bow * len;
                    return ((1 - u) * (1 - u) * s.a.x + 2 * (1 - u) * u * cx + u * u * s.b.x,
                        (1 - u) * (1 - u) * s.a.y + 2 * (1 - u) * u * cy + u * u * s.b.y);
                }
                t -= s.dur;
                if (t <= s.dwell)
                    return s.b;
                t -= s.dwell;
            }
            return null;
        }

        var outPts = new List<(float, float)>();
        double time = 0;
        while (true)
        {
            var p = Pos(time);
            if (p == null)
            {
                var end = segs[segs.Count - 1].b;
                outPts.Add(((float)end.x, (float)end.y));
                break;
            }
            outPts.Add(((float)(p.Value.x + Gauss(rng, 1.5)), (float)(p.Value.y + Gauss(rng, 1.5))));
            time += (1.0 / fps) * (rng.NextDouble() < 0.1 ? 2 : 1);
        }
        return outPts;
    }

    static List<int> RunOld(List<(float x, float y)> s, float[] dx, float[] dy)
    {
        var got = new List<int>();
        int active = 0;
        foreach (var p in s)
        {
            int h = StrokeDotSweep.Inside(p.x, p.y, dx, dy, DotR);
            if (h == 0) { active = 0; continue; }
            if (h != active) { got.Add(h); active = h; }
        }
        return got;
    }

    static List<int> RunNew(List<(float x, float y)> s, float[] dx, float[] dy)
    {
        var got = new List<int>();
        var tmp = new List<int>();
        var tracker = new StrokeDotTracker();
        tracker.Begin(s[0].x, s[0].y, DotR, MinSeg, Settle, dx, dy, tmp);
        got.AddRange(tmp);
        for (int i = 1; i < s.Count; i++)
        {
            tracker.Move(s[i].x, s[i].y, dx, dy, tmp);
            got.AddRange(tmp);
        }
        tracker.End(s[^1].x, s[^1].y, dx, dy, tmp);
        got.AddRange(tmp);
        return got;
    }

    static int[] Rot(int[] seq, int k) => seq.Select(d => ((d - 1 + k) % 6) + 1).ToArray();

    /// <summary>(şekil, koşul) → (eski %, yeni %).</summary>
    static Dictionary<(string, string), (double Old, double New)> Measure()
    {
        var res = new Dictionary<(string, string), (double, double)>();
        foreach (float hexR in new[] { 104f, 112f })
        {
            var (dx, dy) = Dots(hexR);
            foreach (int fps in new[] { 120, 60, 30 })
            foreach (bool fast in new[] { false, true })
            {
                var rng = new Random(1234 + fps + (fast ? 7 : 0) + (int)hexR);
                string cond = $"R{hexR} {fps}fps {(fast ? "hızlı" : "normal")}";
                foreach (var (name, seq) in Shapes)
                {
                    int okOld = 0, okNew = 0;
                    for (int k = 0; k < PerShape; k++)
                    {
                        int[] target = Rot(seq, k % 6);
                        var samples = Generate(target, dx, dy, rng, fps, fast);
                        if (RunOld(samples, dx, dy).SequenceEqual(target)) okOld++;
                        if (RunNew(samples, dx, dy).SequenceEqual(target)) okNew++;
                    }
                    res[(name, cond)] = (100.0 * okOld / PerShape, 100.0 * okNew / PerShape);
                }
            }
        }
        return res;
    }

    static Dictionary<(string, string), (double Old, double New)>? _cache;
    static Dictionary<(string, string), (double Old, double New)> Results => _cache ??= Measure();

    [Test]
    public void PrintPerShapeTable()
    {
        var sb = new StringBuilder();
        var conds = Results.Keys.Select(k => k.Item2).Distinct().ToList();
        sb.AppendLine("| Şekil | " + string.Join(" | ", conds) + " |");
        foreach (var (name, _) in Shapes)
            sb.AppendLine("| " + name + " | " + string.Join(" | ",
                conds.Select(c => $"{Results[(name, c)].Old:0}→{Results[(name, c)].New:0}")) + " |");
        TestContext.Out.WriteLine(sb.ToString());
        Assert.Pass(sb.ToString());
    }

    [Test]
    public void NormalSpeed_EveryShape_AtLeast97Percent()
    {
        foreach (var kv in Results.Where(k => k.Key.Item2.Contains("normal")))
            Assert.That(kv.Value.New, Is.GreaterThanOrEqualTo(97.0), $"{kv.Key.Item1} {kv.Key.Item2}");
    }

    [Test]
    public void FastStrokes_60And120fps_EveryShape_AtLeast97Percent()
    {
        foreach (var kv in Results.Where(k => k.Key.Item2.Contains("hızlı") && !k.Key.Item2.Contains("30fps")))
            Assert.That(kv.Value.New, Is.GreaterThanOrEqualTo(97.0), $"{kv.Key.Item1} {kv.Key.Item2}");
    }

    /// <summary>30 fps + 50–90 ms segment = segment başına 2–3 kare; uç durum. Taban 88, ortalama ≥ 95.</summary>
    [Test]
    public void FastStrokes_30fps_Floor88_Average95()
    {
        var fast = Results.Where(k => k.Key.Item2.Contains("30fps hızlı")).ToList();
        foreach (var kv in fast)
            Assert.That(kv.Value.New, Is.GreaterThanOrEqualTo(88.0), $"{kv.Key.Item1} {kv.Key.Item2}");
        Assert.That(fast.Average(k => k.Value.New), Is.GreaterThanOrEqualTo(95.0));
    }

    [Test]
    public void NewRule_NeverWorseThanOld_ByMoreThanNoise()
    {
        foreach (var kv in Results)
            Assert.That(kv.Value.New, Is.GreaterThanOrEqualTo(kv.Value.Old - 1.5), $"{kv.Key.Item1} {kv.Key.Item2}");
        Assert.That(Results.Average(k => k.Value.New), Is.GreaterThan(Results.Average(k => k.Value.Old)));
    }

    [Test]
    public void FastSwipe_AcrossDot_BetweenSamples_IsRegistered()
    {
        var (dx, dy) = Dots(112f);
        var tracker = new StrokeDotTracker();
        var got = new List<int>();
        var tmp = new List<int>();
        // Merkezden dot 1'in üstünden geçip (örnek yok) dışarı: eski kural kaçırır.
        tracker.Begin(0f, 60f, DotR, MinSeg, Settle, dx, dy, tmp);
        got.AddRange(tmp);
        tracker.Move(0f, 160f, dx, dy, tmp);
        got.AddRange(tmp);
        Assert.That(got, Is.EqualTo(new[] { 1 }));
        Assert.That(RunOld(new List<(float, float)> { (0f, 60f), (0f, 160f) }, dx, dy), Is.Empty);
    }

    [Test]
    public void Graze_OfMiddleDot_OnFarJump_IsNotRegistered()
    {
        var (dx, dy) = Dots(112f);
        var tracker = new StrokeDotTracker();
        var tmp = new List<int>();
        var got = new List<int>();
        tracker.Begin(dx[0], dy[0], DotR, MinSeg, Settle, dx, dy, tmp);
        got.AddRange(tmp);
        // 1→3, dot 2'nin halkasını 30 dp'den sıyırarak (çekirdek 27 dp dışında) geçer.
        float mx = dx[1] - 30f * dx[1] / 112f, my = dy[1] - 30f * dy[1] / 112f;
        tracker.Move(mx, my, dx, dy, tmp);
        got.AddRange(tmp);
        tracker.Move(dx[2], dy[2], dx, dy, tmp);
        got.AddRange(tmp);
        tracker.End(dx[2], dy[2], dx, dy, tmp);
        got.AddRange(tmp);
        Assert.That(got, Is.EqualTo(new[] { 1, 3 }));
    }

    [Test]
    public void SameDot_NotRegisteredTwice_WithoutLeaving()
    {
        var (dx, dy) = Dots(112f);
        var tracker = new StrokeDotTracker();
        var tmp = new List<int>();
        var got = new List<int>();
        tracker.Begin(dx[0], dy[0], DotR, MinSeg, Settle, dx, dy, tmp);
        got.AddRange(tmp);
        for (int i = 0; i < 10; i++)
        {
            tracker.Move(dx[0] + (i % 2) * 3f, dy[0] + 2f, dx, dy, tmp);
            got.AddRange(tmp);
        }
        Assert.That(got, Is.EqualTo(new[] { 1 }));
    }

    // --- Geri bildirim kuralları ---

    [Test]
    public void StrokeEnd_NoAcceptedDot_IsUnrecognized_UnlessDeniedOrCancelled()
    {
        Assert.That(DrawFeedback.OnStrokeEnd(true, 0, false, false), Is.EqualTo(DrawFeedback.StrokeOutcome.Unrecognized));
        Assert.That(DrawFeedback.OnStrokeEnd(true, 1, false, false), Is.EqualTo(DrawFeedback.StrokeOutcome.None));
        Assert.That(DrawFeedback.OnStrokeEnd(true, 0, true, false), Is.EqualTo(DrawFeedback.StrokeOutcome.None), "mana/soğuma/kapalı rün yazısı zaten gösterildi");
        Assert.That(DrawFeedback.OnStrokeEnd(true, 0, false, true), Is.EqualTo(DrawFeedback.StrokeOutcome.None), "panel/kilit iptali sessiz");
        Assert.That(DrawFeedback.OnStrokeEnd(false, 0, false, false), Is.EqualTo(DrawFeedback.StrokeOutcome.None), "merkez/dodge/swap çizim değil");
        Assert.That(DrawFeedback.Unrecognized, Is.EqualTo("şekil tanınmadı"));
    }

    [Test]
    public void RuneChain_JoinsDisplayNames()
    {
        var words = new List<SentenceWord>
        {
            new SentenceWord(Dovus.Core.Element.Rune.Saldiri, JumpKind.None, 0),
            new SentenceWord(Dovus.Core.Element.Rune.Patlama, JumpKind.None, 0),
        };
        Assert.That(DrawFeedback.RuneChain(words), Is.EqualTo("Saldırı → Patlama"));
        Assert.That(DrawFeedback.RuneChain(new List<SentenceWord>()), Is.EqualTo(string.Empty));
    }

    [Test]
    public void Flash_And_Caption_Curves()
    {
        Assert.That(DrawFeedback.FlashMix(0f), Is.EqualTo(1f));
        Assert.That(DrawFeedback.FlashMix(DrawFeedback.FlashFraction), Is.EqualTo(0f));
        Assert.That(DrawFeedback.FlashMix(DrawFeedback.FlashFraction / 2f), Is.EqualTo(0.5f).Within(1e-4));
        Assert.That(DrawFeedback.CaptionAlpha(0.1f), Is.EqualTo(1f));
        Assert.That(DrawFeedback.CaptionAlpha(DrawFeedback.CaptionSec), Is.EqualTo(0f));
        Assert.That(DrawFeedback.CaptionAlpha(DrawFeedback.CaptionSec * 0.875f), Is.EqualTo(0.5f).Within(1e-3));
        Assert.That(DrawFeedback.FailFadeSec, Is.InRange(0.3f, 0.6f), "kısa kırmızı sönme");
    }

    static string Root()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "unity", "Assets")))
            dir = dir.Parent;
        return dir!.FullName;
    }

    static string Game(string rel) =>
        File.ReadAllText(Path.Combine(Root(), "unity", "Assets", "Scripts", rel));

    [Test]
    public void Game_WiresTrackerAndFeedback()
    {
        string session = Game("Game/Casting/Input/HexagonInputSession.cs");
        string stroke = Game("Game/Casting/Input/StrokeCaster.cs");
        string input = Game("Game/Casting/HexagonInputController.cs");
        Assert.That(session, Does.Contain("StrokeDotTracker"));
        Assert.That(stroke, Does.Not.Contain("TryRegisterDotAt"), "nokta kaydı yalnız tarayıcıdan");
        Assert.That(stroke, Does.Contain("DrawFeedback.CaptionFor(outcome)"));
        Assert.That(stroke, Does.Contain("DrawFeedback.ClosedRune"));
        Assert.That(stroke, Does.Contain("_s.Ink?.Break(_s.InkFlashPending)"));
        Assert.That(stroke, Does.Contain("_s.Ink?.RawEnd(false, _s.StrokeAcceptedPx)"));
        Assert.That(input, Does.Contain("TickStrokeSettle()"));
        string ink = Game("Game/Casting/InkTrailView.cs");
        Assert.That(ink, Does.Contain("public void RawBegin("));
        Assert.That(ink, Does.Contain("public void Break(bool flash)"));
        Assert.That(Game("Game/Composition/Builders/HexagonInputBuilder.cs"), Does.Contain("input.DrawCaption += view.ShowDrawCaption;"));
        Assert.That(Game("Game/Casting/HexagonView.cs"), Does.Contain("BuildDrawCaption(canvasGo.transform);"));
    }
}
