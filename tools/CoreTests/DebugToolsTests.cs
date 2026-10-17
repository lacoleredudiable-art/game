using Dovus.App.Boss;
using Dovus.Core.Boss;
using NUnit.Framework;
using System.IO;

namespace CoreTests;

/// <summary>
/// C14 debug araçları (boss faz atlama, debug kapısı) ve C15 kare hızı hedefi (60 fps, tek ayar).
/// Faz atlama BossVitals'ın tek hasar yolundan geçer; Game kodu kaynak metniyle kontrol edilir.
/// </summary>
[TestFixture]
public class DebugToolsTests
{
    static string Root() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string Game(string rel) =>
        File.ReadAllText(Path.Combine(Root(), "unity", "Assets", "Scripts", "Game", rel));

    [Test]
    public void SkipToHpRatio_LowersToEnrageThreshold_AndFiresHpChanged()
    {
        var vitals = new BossVitals(120f);
        int changes = 0;
        float seenBefore = -1f, seenAfter = -1f;
        vitals.HpChanged += (before, after) =>
        {
            changes++;
            seenBefore = before;
            seenAfter = after;
        };

        Assert.That(vitals.SkipToHpRatio(BossDirectorRules.EnrageHpRatio), Is.True);

        Assert.That(vitals.Hp, Is.EqualTo(60f));
        Assert.That(changes, Is.EqualTo(1));
        Assert.That(seenBefore, Is.EqualTo(120f));
        Assert.That(seenAfter, Is.EqualTo(60f));
        Assert.That(BossDirectorRules.IsEnraged(vitals.Hp, vitals.MaxHp), Is.True);
    }

    [Test]
    public void SkipToHpRatio_NeverKills_LeavesOneHp()
    {
        var vitals = new BossVitals(120f);
        bool died = false;
        vitals.Died += () => died = true;

        Assert.That(vitals.SkipToHpRatio(0f), Is.True);

        Assert.That(vitals.Hp, Is.EqualTo(1f));
        Assert.That(vitals.IsDown, Is.False);
        Assert.That(died, Is.False);
    }

    [Test]
    public void SkipToHpRatio_NoOp_WhenAlreadyAtOrBelowTarget()
    {
        var vitals = new BossVitals(120f);
        vitals.ApplyDamage(80f);

        Assert.That(vitals.SkipToHpRatio(BossDirectorRules.EnrageHpRatio), Is.False);
        Assert.That(vitals.Hp, Is.EqualTo(40f));
    }

    [Test]
    public void SkipToHpRatio_NoOp_WhenBossDown()
    {
        var vitals = new BossVitals(120f);
        vitals.ApplyDamage(120f);

        Assert.That(vitals.SkipToHpRatio(BossDirectorRules.EnrageHpRatio), Is.False);
        Assert.That(vitals.IsDown, Is.True);
    }

    [Test]
    public void DebugSkipToPhase2_IsGatedByDebugConfig_AndUsesSharedThreshold()
    {
        string src = Game("Boss/BossDebugTools.cs");
        Assert.That(src, Does.Contain("if (!DebugConfig.Enabled || vitals == null)"));
        Assert.That(src, Does.Contain("vitals.SkipToHpRatio(BossDirectorRules.EnrageHpRatio)"));
        Assert.That(src, Does.Contain("DebugConfig.DevLog("));
    }

    [Test]
    public void TargetFrameRate_DefaultIs60_AndAppliedAtBoot()
    {
        string hud = Game("Config/Sections/HudSettings.cs");
        Assert.That(hud, Does.Contain("public int TargetFrameRateHz = 60;"));

        string boot = Game("Composition/GameBootstrapHost.cs");
        Assert.That(boot, Does.Contain("ApplyFrameRateTarget();"));
        Assert.That(boot, Does.Contain("Application.targetFrameRate = Mathf.Max(1, _tuning.Hud.TargetFrameRateHz);"));
    }
}
