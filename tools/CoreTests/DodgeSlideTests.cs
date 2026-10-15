using Dovus.Core.Dodge;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

/// <summary>O-4.9: engelde kayma matematiği (bütçe, taraf, köşe). Dünya: x ≥ 5 dolu yarım düzlem, kenar payı 0,15.</summary>
[TestFixture]
public class DodgeSlideTests
{
    const float WallX = 5f;
    const float Gap = 0.15f;

    static bool Move(float fx, float fz, float tx, float tz, out float rx, out float rz, out float nx, out float nz)
    {
        nx = -1f;
        nz = 0f;
        rx = tx;
        rz = tz;
        float limit = WallX - Gap;
        if (tx <= limit)
            return false;
        float dx = tx - fx;
        float s = dx > 0.0001f ? (limit - fx) / dx : 0f;
        s = System.Math.Clamp(s, 0f, 1f);
        rx = fx + dx * s;
        rz = fz + (tz - fz) * s;
        return true;
    }

    static bool Contact(float x, float z, float reach, out float nx, out float nz, out float cx, out float cz)
    {
        nx = -1f;
        nz = 0f;
        cx = WallX - Gap;
        cz = z;
        return x > WallX - Gap - 0.05f;
    }

    static DodgeSlide Slide(float dirX, float dirZ, float remaining, DodgeTuning cfg, DodgeSlide.FullFn full = null)
    {
        var s = new DodgeSlide();
        s.Begin(dirX, dirZ, -1f, 0f, WallX - Gap, 0f, remaining, cfg, full);
        return s;
    }

    [Test]
    public void Budget_IsRemaining_TimesMaxOfTangentAndMinRatio()
    {
        var cfg = new DodgeTuning();
        // 60° eğik: t = sin60 ≈ 0,866 → R×0,866
        float dx = 0.5f, dz = 0.8660254f;
        Assert.That(Slide(dx, dz, 2f, cfg).BudgetM, Is.EqualTo(2f * 0.8660254f).Within(1e-4f));
        // 5° eğik: t ≈ 0,087 < SlideMinRatio → R×0,5
        float a = 5f * System.MathF.PI / 180f;
        Assert.That(Slide(System.MathF.Cos(a), System.MathF.Sin(a), 2f, cfg).BudgetM, Is.EqualTo(1f).Within(1e-4f));
    }

    [Test]
    public void Side_FromStickTangentSign_WhenTAtOrAboveEngineChoice()
    {
        var cfg = new DodgeTuning();
        // Duvara +x giden, hafif +z eğimli çubuk: t = sin(15°) = 0,26 ≥ 0,17. Normal (-1,0): CW teğet = (0,1) → +z.
        float a = 15f * System.MathF.PI / 180f;
        Assert.That(Slide(System.MathF.Cos(a), System.MathF.Sin(a), 2f, cfg).Side, Is.EqualTo(1));
        Assert.That(Slide(System.MathF.Cos(a), -System.MathF.Sin(a), 2f, cfg).Side, Is.EqualTo(-1));
    }

    [Test]
    public void Side_BelowEngineChoice_EngineAvoidsFullSide()
    {
        var cfg = new DodgeTuning();
        // Tam dik çubuk (t = 0). +z tarafı dolu (telegraph), -z boş → -1.
        var s = Slide(1f, 0f, 2f, cfg, (x, z) => z > 0.1f);
        Assert.That(s.Side, Is.EqualTo(-1));
        s = Slide(1f, 0f, 2f, cfg, (x, z) => z < -0.1f);
        Assert.That(s.Side, Is.EqualTo(1));
    }

    [Test]
    public void Side_BelowEngineChoice_BothEqual_Clockwise()
    {
        var cfg = new DodgeTuning();
        Assert.That(Slide(1f, 0f, 2f, cfg, (x, z) => false).Side, Is.EqualTo(1));
        Assert.That(Slide(1f, 0f, 2f, cfg, (x, z) => true).Side, Is.EqualTo(1));
    }

    [Test]
    public void ExactNinety_StaysInsideEngineBand_NoSpecialRule()
    {
        var cfg = new DodgeTuning();
        Assert.That(DodgeSlide.TangentT(1f, 0f, -1f, 0f), Is.LessThan(cfg.EngineChoiceT));
    }

    [Test]
    public void Advance_FollowsSurface_KeepsGap_SpendsBudget()
    {
        var cfg = new DodgeTuning();
        var s = Slide(0.5f, 0.8660254f, 2f, cfg);
        float x = WallX - Gap, z = 0f;
        float budget0 = s.BudgetM;
        s.Advance(0.2f, ref x, ref z, cfg, Move, Contact);
        Assert.That(x, Is.EqualTo(WallX - Gap).Within(1e-4f));
        Assert.That(z, Is.EqualTo(0.2f).Within(1e-4f));
        Assert.That(s.BudgetM, Is.EqualTo(budget0 - 0.2f).Within(1e-4f));
    }

    [Test]
    public void Advance_EndsWhenBudgetSpent()
    {
        var cfg = new DodgeTuning();
        var s = Slide(0.5f, 0.8660254f, 0.3f, cfg);
        float x = WallX - Gap, z = 0f;
        for (int i = 0; i < 20 && s.Active; i++)
            s.Advance(0.1f, ref x, ref z, cfg, Move, Contact);
        Assert.That(s.Active, Is.False);
        Assert.That(z, Is.EqualTo(0.3f * 0.8660254f).Within(1e-3f));
    }

    [Test]
    public void Advance_Corner_StallsAndStops()
    {
        var cfg = new DodgeTuning();
        var s = Slide(0.5f, 0.8660254f, 2f, cfg);
        float x = WallX - Gap, z = 0f;
        // İkinci duvar z ≥ 0 yönünü de kapatır (iç köşe): ilerleme yok.
        bool Boxed(float fx, float fz, float tx, float tz, out float rx, out float rz, out float nx, out float nz)
        {
            rx = fx;
            rz = fz;
            nx = 0f;
            nz = -1f;
            return true;
        }

        s.Advance(0.2f, ref x, ref z, cfg, Boxed, null);
        Assert.That(s.Active, Is.False);
    }

    [Test]
    public void Advance_RespectsMaxIterPerStep()
    {
        var cfg = new DodgeTuning { SlideMaxIter = 1 };
        var s = Slide(0.5f, 0.8660254f, 2f, cfg);
        float x = WallX - Gap, z = 0f;
        int calls = 0;
        bool Counting(float fx, float fz, float tx, float tz, out float rx, out float rz, out float nx, out float nz)
        {
            calls++;
            rx = fx + (tx - fx) * 0.5f;
            rz = fz + (tz - fz) * 0.5f;
            nx = -1f;
            nz = 0f;
            return true;
        }

        s.Advance(0.2f, ref x, ref z, cfg, Counting, null);
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void Advance_OnCircle_LargeSteps_KeepEdgeGap()
    {
        // Yarıçapı 3 m daire; adım 0,5 m tegetsel gider, yüzeye geri çekilmezse ~0,04 m açılır.
        const float R = 3f;
        bool Circle(float fx, float fz, float tx, float tz, out float rx, out float rz, out float nx, out float nz)
        {
            rx = tx;
            rz = tz;
            nx = nz = 0f;
            float d = System.MathF.Sqrt(tx * tx + tz * tz);
            if (d >= R)
                return false;
            nx = tx / d;
            nz = tz / d;
            rx = nx * R;
            rz = nz * R;
            return true;
        }

        bool CircleContact(float x, float z, float reach, out float nx, out float nz, out float cx, out float cz)
        {
            float d = System.MathF.Sqrt(x * x + z * z);
            nx = x / d;
            nz = z / d;
            cx = nx * R;
            cz = nz * R;
            return d < R + reach;
        }

        var cfg = new DodgeTuning();
        var s = new DodgeSlide();
        Assert.That(s.Begin(0f, -1f, 0f, 1f, 0f, R, 2f, cfg, null), Is.True);
        float x = 0f, z = R;
        for (int i = 0; i < 4 && s.Active; i++)
        {
            s.Advance(0.5f, ref x, ref z, cfg, Circle, CircleContact);
            Assert.That(System.MathF.Sqrt(x * x + z * z), Is.EqualTo(R).Within(1e-3f), $"adım {i}");
        }
    }
}
