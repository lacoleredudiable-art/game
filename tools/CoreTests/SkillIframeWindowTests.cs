using Dovus.Core.Combat;
using NUnit.Framework;
using System.IO;

namespace CoreTests;

/// <summary>F1: skill hareketi / çağırma anı i-frame'i — donmayan pencere, Stasis değil.</summary>
[TestFixture]
public sealed class SkillIframeWindowTests
{
    [Test]
    public void HitInsideWindow_IsZero_JustAfter_IsFull()
    {
        var w = new SkillIframeWindow();
        w.Open(1000, 400); // 3-7 dash
        Assert.That(w.Filter(1000, 22f), Is.EqualTo(0f));
        Assert.That(w.Filter(1399, 22f), Is.EqualTo(0f));
        Assert.That(w.Filter(1400, 22f), Is.EqualTo(22f), "end is exclusive");
        Assert.That(w.Filter(999, 22f), Is.EqualTo(22f), "before open");
    }

    [Test]
    public void Window_ExtendsButNeverShrinks()
    {
        var w = new SkillIframeWindow();
        w.Open(0, 400);
        w.Open(100, 100);
        Assert.That(w.IsActive(350), Is.True);
        w.Open(300, 220);
        Assert.That(w.IsActive(500), Is.True);
        Assert.That(w.IsActive(520), Is.False);
        w.Clear();
        Assert.That(w.IsActive(10), Is.False);
    }

    [Test]
    public void ZeroDuration_DoesNothing()
    {
        var w = new SkillIframeWindow();
        w.Open(0, 0);
        Assert.That(w.IsActive(0), Is.False);
    }

    static string Root() => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public void SpawnIframe_11_10_NoLongerFreezesPlayer()
    {
        string src = File.ReadAllText(Path.Combine(Root(), "unity", "Assets", "Scripts", "Game", "Skills", "ManifestationDirector.VerbExecution.cs"));
        int at = src.IndexOf("void ApplySpawnIFrame", System.StringComparison.Ordinal);
        Assert.That(at, Is.GreaterThan(0));
        string body = src.Substring(at, src.IndexOf("}", src.IndexOf("{", at), System.StringComparison.Ordinal) - at);
        Assert.That(body, Does.Not.Contain("Stasis"), "11-10 spawn i-frame must not freeze the player");
        Assert.That(body, Does.Contain("OpenSkillIframe"));
    }

    [Test]
    public void MotionIframe_IsApplied_InGame()
    {
        string castPort = File.ReadAllText(Path.Combine(Root(), "unity", "Assets", "Scripts", "Game", "Skills", "ManifestationDirector.zCastPort.cs"));
        Assert.That(castPort, Does.Contain("ApplySkillMotionIframe(skill, motion)"));
        string src = File.ReadAllText(Path.Combine(Root(), "unity", "Assets", "Scripts", "Game", "Skills", "Launch", "CastSideEffects.cs"));
        Assert.That(src, Does.Contain("rig.OpenSkillIframe(plan.IframeMs)"));
    }
}
