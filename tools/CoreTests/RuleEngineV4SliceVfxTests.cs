using System.IO;
using Dovus.Core.Presentation;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4SliceVfxTests
{
    static string JsonPath => Path.GetFullPath(Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "..", "..",
        "unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json"));

    static RuleEngineV4SliceVfx Slice() => RuleEngineV4SliceVfx.FromJson(File.ReadAllText(JsonPath));

    [Test]
    public void Json_BladeTrail_QuarterSecondLife_TapersToTip()
    {
        RuleEngineV4BladeTrailFx t = Slice().BladeTrail;
        Assert.That(t.LifeSec, Is.EqualTo(0.25f).Within(1e-4f));
        Assert.That(t.TailWidthFrac, Is.LessThan(1f));

        t.Shape(0f, out float w0, out float a0);
        Assert.That(w0, Is.EqualTo(1f).Within(1e-4f), "yeni örnek tam kabza→uç");
        Assert.That(a0, Is.EqualTo(1f).Within(1e-4f));

        t.Shape(t.LifeSec * 0.5f, out float wMid, out float aMid);
        Assert.That(wMid, Is.LessThan(w0));
        Assert.That(aMid, Is.LessThan(a0));

        t.Shape(t.LifeSec, out float wEnd, out float aEnd);
        Assert.That(wEnd, Is.EqualTo(t.TailWidthFrac).Within(1e-4f), "kuyruk uca incelir");
        Assert.That(aEnd, Is.EqualTo(0f).Within(1e-4f));
    }

    [Test]
    public void MissingBlock_UsesDefaults()
    {
        RuleEngineV4SliceVfx v = RuleEngineV4SliceVfx.FromJson("{}");
        Assert.That(v.BladeTrail.LifeSec, Is.EqualTo(RuleEngineV4SliceVfxDefaults.BladeTrailLifeSec));
        Assert.That(v.BladeTrail.TailWidthFrac, Is.EqualTo(RuleEngineV4SliceVfxDefaults.BladeTrailTailWidthFrac));
        Assert.That(v.TargetRing.PulseHz, Is.EqualTo(RuleEngineV4SliceVfxDefaults.TargetRingPulseHz));
        Assert.That(v.TargetRing.ReleaseFadeSec, Is.EqualTo(RuleEngineV4SliceVfxDefaults.TargetRingReleaseFadeSec));
    }

    [Test]
    public void Json_TargetRing_PulsesTwoHz_FadesInPointOneSec()
    {
        RuleEngineV4TargetRingFx r = Slice().TargetRing;
        Assert.That(r.PulseHz, Is.EqualTo(2f).Within(1e-5f));
        Assert.That(r.ReleaseFadeSec, Is.EqualTo(0.1f).Within(1e-5f));
        Assert.That(r.PulseScale, Is.GreaterThan(0f).And.LessThan(0.15f), "nabız hafif olmalı");

        float period = 1f / r.PulseHz;
        r.Pulse(0f, out float s0, out float b0);
        r.Pulse(period * 0.5f, out float sPeak, out float bPeak);
        r.Pulse(period, out float s1, out float b1);
        Assert.That(s0, Is.EqualTo(1f).Within(1e-5f), "dipte tam yarıçap");
        Assert.That(sPeak, Is.EqualTo(1f + r.PulseScale).Within(1e-5f), "yarım periyotta tepe");
        Assert.That(s1, Is.EqualTo(1f).Within(1e-4f), "0,5 s'de bir tam nabız");
        Assert.That(b0, Is.EqualTo(r.PulseMinBrightness).Within(1e-5f));
        Assert.That(bPeak, Is.EqualTo(1f).Within(1e-5f));
        Assert.That(b1, Is.EqualTo(b0).Within(1e-4f));

        Assert.That(r.ReleaseAlpha(0f), Is.EqualTo(1f).Within(1e-5f));
        Assert.That(r.ReleaseAlpha(r.ReleaseFadeSec * 0.5f), Is.EqualTo(0.5f).Within(1e-4f));
        Assert.That(r.ReleaseAlpha(r.ReleaseFadeSec), Is.EqualTo(0f).Within(1e-5f), "0,1 s'de sönmüş");
        Assert.That(r.ReleaseAlpha(1f), Is.EqualTo(0f));
    }

    [Test]
    public void NotCast_OnlyManaAndCooldown_NeverMissOrFizzle()
    {
        Assert.That(RuleEngineV4NotCastFx.Applies(RuleEngineV4CastDenial.Mana), Is.True);
        Assert.That(RuleEngineV4NotCastFx.Applies(RuleEngineV4CastDenial.Cooldown), Is.True);
        Assert.That(RuleEngineV4NotCastFx.Applies(RuleEngineV4CastDenial.None), Is.False, "atılan skill (ıska/boşa gitme dahil)");
    }

    [Test]
    public void Json_NotCast_ShakesTwiceThenScattersWithin035()
    {
        RuleEngineV4NotCastFx f = Slice().NotCast;
        Assert.That(f.DurationSec, Is.EqualTo(0.35f).Within(1e-5f));
        Assert.That(f.Shakes, Is.EqualTo(2));
        Assert.That(f.Alpha, Is.GreaterThan(0f).And.LessThan(1f), "sönük");
        Assert.That(f.AshR, Is.EqualTo(f.AshG).Within(0.05f), "kül grisi");
        Assert.That(f.AshG, Is.EqualTo(f.AshB).Within(0.05f), "kül grisi");

        // Titreme penceresinde işaret değişimleri = 2 × salınım sayısı (kendi sıfırları hariç).
        float window = f.DurationSec * f.ShakeUntilFrac;
        const int steps = 700;
        int crossings = 0;
        float prev = 0f;
        for (int i = 1; i < steps; i++)
        {
            f.Sample(window * i / steps, out float shake, out float a, out float scatter);
            Assert.That(scatter, Is.EqualTo(0f), "titrerken dağılmaz");
            Assert.That(a, Is.EqualTo(f.Alpha).Within(1e-5f), "titrerken sönük ama tam");
            Assert.That(System.Math.Abs(shake), Is.LessThanOrEqualTo(f.ShakePx + 1e-4f));
            if (prev != 0f && System.Math.Sign(shake) != System.Math.Sign(prev) && shake != 0f)
                crossings++;
            if (shake != 0f)
                prev = shake;
        }
        Assert.That(crossings, Is.EqualTo(2 * f.Shakes - 1), "iki tam titreme");

        f.Sample(f.DurationSec, out float endShake, out float endAlpha, out float endScatter);
        Assert.That(endShake, Is.EqualTo(0f));
        Assert.That(endScatter, Is.EqualTo(1f).Within(1e-5f), "süre sonunda tam dağılmış");
        Assert.That(endAlpha, Is.EqualTo(0f).Within(1e-5f));
        Assert.That(f.Done(f.DurationSec), Is.True);
        Assert.That(f.Done(f.DurationSec * 0.99f), Is.False);

        Assert.That(f.DustFall(0f), Is.EqualTo(0f));
        Assert.That(f.DustFall(f.DurationSec), Is.EqualTo(f.DustFallPx).Within(1e-4f), "toz aşağı düşer");
        Assert.That(f.DustX(0), Is.EqualTo(-f.DustX(f.DustCount - 1)).Within(1e-4f), "toz harfin altına ortalı");
        Assert.That(f.CrackGap(0f), Is.EqualTo(0f));
        Assert.That(f.CrackGap(window), Is.EqualTo(f.CrackGapPx).Within(1e-4f), "çatlak titreme sonunda tam açık");
        Assert.That(f.CrackGap(f.DurationSec), Is.EqualTo(f.CrackGapPx).Within(1e-4f));
    }

    static readonly string ResourcesDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(JsonPath)!, ".."));

    [Test]
    public void Json_Atlas_GlyphsTop_SilhouettesBelow_RingInFreeSpace()
    {
        RuleEngineV4VfxAtlas a = Slice().Atlas;
        Assert.That(a.SizePx, Is.EqualTo(2048));
        Assert.That(a.Glyphs.WidthPx, Is.EqualTo(2048));
        Assert.That(a.Glyphs.HeightPx, Is.EqualTo(768));
        Assert.That(a.Silhouettes.WidthPx, Is.EqualTo(1024));
        Assert.That(a.Silhouettes.HeightPx, Is.EqualTo(512));
        Assert.That(a.Glyphs.YPx, Is.EqualTo(0), "harfler üstte");
        Assert.That(a.Silhouettes.YPx, Is.EqualTo(a.Glyphs.YPx + a.Glyphs.HeightPx), "siluetler harflerin altında");

        var regions = new[]
        {
            (a.Glyphs.XPx, a.Glyphs.YPx, a.Glyphs.WidthPx, a.Glyphs.HeightPx),
            (a.Silhouettes.XPx, a.Silhouettes.YPx, a.Silhouettes.WidthPx, a.Silhouettes.HeightPx),
            (a.Ring.XPx, a.Ring.YPx, a.Ring.SizePx, a.Ring.SizePx),
            (a.Dust.XPx, a.Dust.YPx, a.Dust.SizePx, a.Dust.SizePx),
        };
        for (int i = 0; i < regions.Length; i++)
        {
            var r = regions[i];
            Assert.That(r.Item1 >= 0 && r.Item2 >= 0 && r.Item1 + r.Item3 <= a.SizePx && r.Item2 + r.Item4 <= a.SizePx,
                Is.True, $"bölge {i} atlas dışında");
            for (int j = i + 1; j < regions.Length; j++)
            {
                var s = regions[j];
                bool overlap = r.Item1 < s.Item1 + s.Item3 && s.Item1 < r.Item1 + r.Item3
                    && r.Item2 < s.Item2 + s.Item4 && s.Item2 < r.Item2 + r.Item4;
                Assert.That(overlap, Is.False, $"bölge {i} ile {j} çakışıyor");
            }
        }
    }

    [Test]
    public void Atlas_CellUv_TopLeftOrigin_FlippedToUnityUv()
    {
        RuleEngineV4VfxAtlas a = Slice().Atlas;
        // Harf 8 = sütun 0, satır 1 → piksel (0, 256..512) üstten.
        AssertUv(a.GlyphUv(8), 0f, 0.75f, 0.125f, 0.125f, "harf 8");
        // Harf 22 = sütun 6, satır 2 → piksel (1536, 512..768).
        AssertUv(a.GlyphUv(22), 0.75f, 0.625f, 0.125f, 0.125f, "harf 22");
        // Siluet D (3) = sütun 3, satır 0 → piksel (768, 768..1024).
        AssertUv(a.SilhouetteUv(3), 0.375f, 0.5f, 0.125f, 0.125f, "siluet D");
        // Siluet F (5) = sütun 1, satır 1 → piksel (256, 1024..1280).
        AssertUv(a.SilhouetteUv(5), 0.125f, 0.375f, 0.125f, 0.125f, "siluet F");
        AssertUv(a.RingUv, 0.5f, 0.375f, 0.25f, 0.25f, "halka");
        AssertUv(a.SilhouetteUv(99), 0.375f, 0.375f, 0.125f, 0.125f, "sınır dışı → son hücre");
        Assert.That(a.RingQuadHalfM(a.RingRadiusFrac), Is.EqualTo(1f).Within(1e-5f));
    }

    [Test]
    public void Atlas_PackedPng_Exists_SourcesKept()
    {
        RuleEngineV4VfxAtlas a = Slice().Atlas;
        string packed = Path.Combine(ResourcesDir, a.Resource.Replace('/', Path.DirectorySeparatorChar) + ".png");
        Assert.That(File.Exists(packed), Is.True, packed);
        using (var fs = File.OpenRead(packed))
        {
            var header = new byte[24];
            Assert.That(fs.Read(header, 0, header.Length), Is.EqualTo(header.Length));
            int w = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            int h = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            Assert.That((w, h), Is.EqualTo((a.SizePx, a.SizePx)), "paketlenmiş atlas boyutu");
        }
        foreach (string src in new[] { "Ejder_Glif_Atlas.png", "Ejder_Siluet_Atlas.png" })
            Assert.That(File.Exists(Path.Combine(Path.GetDirectoryName(packed)!, src)), Is.True, $"kaynak {src} silinmiş");
    }

    static void AssertUv(RuleEngineV4UvRect r, float x, float y, float w, float h, string what)
    {
        Assert.That(r.X, Is.EqualTo(x).Within(1e-6f), what + " x");
        Assert.That(r.Y, Is.EqualTo(y).Within(1e-6f), what + " y");
        Assert.That(r.W, Is.EqualTo(w).Within(1e-6f), what + " w");
        Assert.That(r.H, Is.EqualTo(h).Within(1e-6f), what + " h");
    }

    [Test]
    public void Json_KilicVur_OpensTrailEvents()
    {
        RuleEngineV4Catalog c = RuleEngineV4Catalog.FromJson(File.ReadAllText(JsonPath));
        var plan = new CommandPlan(1, 0, 4, System.Array.Empty<PhysicsCommand>());
        RuleEngineV4SkillAnimBinding b = RuleEngineV4SkillAnimResolver.Resolve(plan, c.SkillAnim);
        Assert.That(b.Clip, Is.EqualTo("Kilic_VUR"));
        Assert.That(b.Events.HasFlag(RuleEngineV4SkillAnimEvent.TrailOn), Is.True);
        Assert.That(b.Events.HasFlag(RuleEngineV4SkillAnimEvent.TrailOff), Is.True);
    }
}
