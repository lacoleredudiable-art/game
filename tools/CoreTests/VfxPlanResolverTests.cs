using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Core.Shared;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class VfxPlanResolverTests
{
    static SkillResolution Make(
        string skillId,
        string verbId,
        string verbName,
        string adjId,
        string adjName,
        string action,
        float baseDamage = 10f)
    {
        return SkillResolution.Build(
            elementId: "ates",
            elementName: "Ateş",
            displayName: verbName + " " + adjName,
            skillId: (SkillId)skillId,
            skillJob: "strike",
            verbId: verbId,
            verbName: verbName,
            verbFamily: "motion",
            action: action,
            baseDamage: baseDamage,
            basePoise: 0f,
            hitbox: "dash_line",
            castMobility: "rooted",
            mechanics: System.Array.Empty<string>(),
            adjectiveId: adjId,
            adjectiveName: adjName,
            silhouetteAxis: "horizontal",
            damageMult: 1f,
            hitboxScaleMult: 1f,
            poiseDamageMult: 1f,
            length: 2,
            lengthRole: "Temel",
            lengthCastMult: 1f,
            lengthMobility: "rooted",
            flavorElement: string.Empty);
    }

    [Test]
    public void Kilic_Hareket_Dash_IsZenitsuSlice()
    {
        SkillResolution skill = Make("3-1", "3", "Hareket", "1", "Yoğunlaştırma", "dash");
        VfxPlan plan = VfxPlanResolver.Resolve(skill, "kilic");

        Assert.That(plan.IsEmpty, Is.False);
        Assert.That(plan.IsKilicAtilSlice, Is.True);
        Assert.That(plan.Motif, Is.EqualTo(VfxMotifKind.Wing));
        Assert.That(plan.Carrier, Is.EqualTo(VfxCarrierKind.FlowingEmberTrail));
        Assert.That(plan.Delivery, Is.EqualTo(VfxDeliveryClass.Melee));
        Assert.That(plan.Shape, Is.EqualTo(VfxShapePrimitive.CollapsePoint));
        Assert.That(plan.LightningDashTrail, Is.True);
        Assert.That(plan.WingFootSparks, Is.True);
        Assert.That(plan.DragonTailArcSilhouette, Is.True);
        Assert.That(plan.DragonAtlasCell, Is.EqualTo(VfxPlanDefaults.EjderAtlasCellF));
        Assert.That(plan.TrailLifeSec, Is.EqualTo(VfxPlanDefaults.KilicIzAtilOmurSec).Within(0.001f));
        Assert.That(plan.SilhouetteLifeSec, Is.EqualTo(VfxPlanDefaults.EjderKilicAtilOmurSec).Within(0.001f));
        Assert.That(plan.SkillAwaken, Is.True);
        Assert.That(plan.RuneLetterFlash, Is.True);
        Assert.That(plan.SlashArcOnHit, Is.True);
        Assert.That(plan.ZararClawMarksOnHit, Is.True);
        Assert.That(plan.EdgeStopEmberSpark, Is.True);
        Assert.That(plan.CoreColor.R, Is.EqualTo(VfxPlanDefaults.HareketR).Within(0.001f));
        Assert.That(plan.CoreColor.Intensity, Is.EqualTo(VfxPlanDefaults.HareketIntensity).Within(0.001f));
    }

    [Test]
    public void Kilic_DisplayName_NormalizesToKilicKey()
    {
        SkillResolution skill = Make("3-1", "3", "Hareket", "1", "Yoğun", "dash");
        VfxPlan plan = VfxPlanResolver.Resolve(skill, "Kılıç");
        Assert.That(plan.WeaponKey, Is.EqualTo("kilic"));
        Assert.That(plan.IsKilicAtilSlice, Is.True);
    }

    [Test]
    public void Zarar_Verb_GetsClawMotifAndColors()
    {
        SkillResolution skill = Make("1-1", "1", "Saldırı", "1", "Yoğunlaştırma", "damage");
        VfxPlan plan = VfxPlanResolver.Resolve(skill, "kilic");

        Assert.That(plan.Motif, Is.EqualTo(VfxMotifKind.Claw));
        Assert.That(plan.CoreColor.R, Is.EqualTo(VfxPlanDefaults.ZararR).Within(0.001f));
        Assert.That(plan.CoreColor.Intensity, Is.EqualTo(VfxPlanDefaults.ZararIntensity).Within(0.001f));
        Assert.That(plan.LightningDashTrail, Is.False);
        Assert.That(plan.SlashArcOnHit, Is.True);
        Assert.That(plan.ZararClawMarksOnHit, Is.True);
    }

    [Test]
    public void Yay_Carrier_IsProjectileArrow()
    {
        SkillResolution skill = Make("1-5", "1", "Zarar", "5", "Yayılan", "damage");
        VfxPlan plan = VfxPlanResolver.Resolve(skill, "yay");
        Assert.That(plan.Carrier, Is.EqualTo(VfxCarrierKind.EmberArrow));
        Assert.That(plan.Delivery, Is.EqualTo(VfxDeliveryClass.Projectile));
        Assert.That(plan.Shape, Is.EqualTo(VfxShapePrimitive.ExpandingRing));
        Assert.That(plan.IsKilicAtilSlice, Is.False);
    }

    [Test]
    public void EmptySkill_ReturnsEmptyPlan()
    {
        VfxPlan plan = VfxPlanResolver.Resolve(SkillResolution.Empty, "kilic");
        Assert.That(plan.IsEmpty, Is.True);
    }
}
