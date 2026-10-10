using System.IO;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// Kılıç ATIL VFX tetikleri path-bağımsız: kural_motoru_v4 bayrağı AÇIK/KAPALI aynı plan;
/// shout / NotifyBossStruck / v4 ApplyDamage kabloları kaynakta doğrulanır.
/// </summary>
[TestFixture]
public sealed class RuleDrivenVfxPathTests
{
    static string ScriptsDir() =>
        Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "unity", "Assets", "Scripts"));

    static string Game(string rel) => File.ReadAllText(Path.Combine(ScriptsDir(), "Game", rel));

    static SkillResolution KilicAtil() =>
        SkillResolution.Build(
            elementId: "ates",
            elementName: "Ateş",
            displayName: "Hareket Yoğunlaştırma",
            skillId: (SkillId)"3-1",
            skillJob: "strike",
            verbId: "3",
            verbName: "Hareket",
            verbFamily: "motion",
            action: "dash",
            baseDamage: 10f,
            basePoise: 0f,
            hitbox: "dash_line",
            castMobility: "rooted",
            mechanics: System.Array.Empty<string>(),
            adjectiveId: "1",
            adjectiveName: "Yoğunlaştırma",
            silhouetteAxis: "horizontal",
            damageMult: 1f,
            hitboxScaleMult: 1f,
            poiseDamageMult: 1f,
            length: 2,
            lengthRole: "Temel",
            lengthCastMult: 1f,
            lengthMobility: "rooted",
            flavorElement: string.Empty);

    static void AssertZenitsuTriggers(VfxPlan plan)
    {
        Assert.That(plan.IsEmpty, Is.False);
        Assert.That(plan.IsSwordDashSlice, Is.True);
        // Uyanış (crack awaken)
        Assert.That(plan.SkillAwaken, Is.True);
        // Rün glifi
        Assert.That(plan.RuneLetterFlash, Is.True);
        // ATIL iz + silüet
        Assert.That(plan.LightningDashTrail, Is.True);
        Assert.That(plan.DragonTailArcSilhouette, Is.True);
        // İsabet pençe / kesik
        Assert.That(plan.ZararClawMarksOnHit, Is.True);
        Assert.That(plan.SlashArcOnHit, Is.True);
    }

    [Test]
    public void ZenitsuTriggers_Identical_WithFlagOffAndOn()
    {
        bool previous = RuleEngineV4Feature.Enabled;
        try
        {
            RuleEngineV4Feature.Enabled = false;
            VfxPlan off = VfxPlanResolver.Resolve(KilicAtil(), "kilic");
            AssertZenitsuTriggers(off);

            RuleEngineV4Feature.Enabled = true;
            VfxPlan on = VfxPlanResolver.Resolve(KilicAtil(), "kilic");
            AssertZenitsuTriggers(on);

            Assert.That(on.SkillAwaken, Is.EqualTo(off.SkillAwaken));
            Assert.That(on.RuneLetterFlash, Is.EqualTo(off.RuneLetterFlash));
            Assert.That(on.LightningDashTrail, Is.EqualTo(off.LightningDashTrail));
            Assert.That(on.DragonTailArcSilhouette, Is.EqualTo(off.DragonTailArcSilhouette));
            Assert.That(on.ZararClawMarksOnHit, Is.EqualTo(off.ZararClawMarksOnHit));
            Assert.That(on.SlashArcOnHit, Is.EqualTo(off.SlashArcOnHit));
            Assert.That(on.IsSwordDashSlice, Is.EqualTo(off.IsSwordDashSlice));
        }
        finally
        {
            RuleEngineV4Feature.Enabled = previous;
        }
    }

    [Test]
    public void ShoutPath_WiresBeginSkill_PathIndependent()
    {
        string shout = Game("Skills/Presentation/SkillPresentation.cs");
        Assert.That(shout, Does.Contain("RuleDrivenVfxSink.BeginSkill"));
        Assert.That(shout, Does.Contain("_host.EquippedWeapon"));
    }

    [Test]
    public void NotifyBossStruck_WiresHitClaws_PathIndependent()
    {
        string feel = Game("Skills/ManifestationDirector.Feel.cs");
        Assert.That(feel, Does.Contain("RuleDrivenVfxSink.NotifyBossStrike"));
    }

    [Test]
    public void RuleEngineV4Delivery_ApplyDamage_WiresNotifyBossStruckAndSink()
    {
        string host = Game("Skills/RuleEngineV4/RuleEngineV4WorldHost.cs");
        Assert.That(host, Does.Contain("RuleDrivenVfxSink.NotifyBossStrike"));
        Assert.That(host, Does.Contain("NotifyBossStruck"));
        // Bayrak kontrolü ApplyDamage içinde yok — VFX path'ten bağımsız.
        Assert.That(host, Does.Not.Contain("RuleEngineV4Feature.Enabled"));
    }

    [Test]
    public void AnimEventHost_ExposesTrailImpactEjder_NoHardcodedFrames()
    {
        string host = Game("Vfx/SkillAnimVfxEventHost.cs");
        Assert.That(host, Does.Contain("SetEventMask"));
        Assert.That(host, Does.Contain("Trail_On"));
        Assert.That(host, Does.Contain("Trail_Off"));
        Assert.That(host, Does.Contain("Impact"));
        Assert.That(host, Does.Contain("Ejder"));
        Assert.That(host, Does.Not.Contain("frame"));
        Assert.That(host, Does.Not.Contain("Frame"));

        string director = Game("Vfx/RuleDrivenVfxDirector.cs");
        Assert.That(director, Does.Contain("OnTrailOn"));
        Assert.That(director, Does.Contain("OnImpactAnimEvent"));
        Assert.That(director, Does.Contain("OnEjderAnimEvent"));
        Assert.That(director, Does.Contain("TrailAutoOpenTipSpeedMps"));
    }

    [Test]
    public void MotionTemplateDriver_DoesNotCallBeginSkill_ShoutOwnsArm()
    {
        string motion = Game("Skills/Motion/MotionTemplateDriver.cs");
        Assert.That(motion, Does.Not.Contain("RuleDrivenVfxSink.BeginSkill"));
        Assert.That(motion, Does.Not.Contain(".BeginSkill("));
        Assert.That(motion, Does.Contain("RuleDrivenVfxSink.NotifyHit"));
    }

    [Test]
    public void RuleEngineV4Motion_EndDash_NotifiesMotionEnded()
    {
        string run = Game("Skills/RuleEngineV4/RuleEngineV4WorldCommandRunHost.cs");
        Assert.That(run, Does.Contain("RuleDrivenVfxSink.NotifyMotionEnded"));
    }

    [Test]
    public void RuleEngineV4Cast_PlaysSkillAnimBridge_FromCatalog()
    {
        string cast = Game("Skills/RuleEngineV4/RuleEngineV4CastHost.cs");
        Assert.That(cast, Does.Contain("RuleEngineV4SkillAnimBridge.PlayForPlan"));
        Assert.That(cast, Does.Contain("SkillAnim"));
    }
}
