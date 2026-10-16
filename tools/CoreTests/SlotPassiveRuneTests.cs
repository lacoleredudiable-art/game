using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

using Dovus.Core.Shared;
namespace CoreTests;

[TestFixture]
public class SlotPassiveRuneTests
{
    string _json = null!;
    SkillMotor _motor = null!;

    [SetUp]
    public void SetUp()
    {
        _json = SliceGrammarTestPaths.ReadSliceGrammarJson();
        _motor = SkillMotor.FromJson(_json);
    }

    [Test]
    public void Rune1_Yogunlastirma_RaisesDamageAndPoiseOnLaterCasts()
    {
        SlotPassiveDirector director = Arm(1, out int trigger, out int later);
        Assert.That(director.DamageMultFor(trigger), Is.EqualTo(1f).Within(0.001f));
        Assert.That(director.PoiseDamageMultFor(later), Is.EqualTo(1.2f).Within(0.001f));
        Assert.That(director.DamageMultFor(later), Is.EqualTo(1.35f).Within(0.001f));
        Assert.That(director.HitboxSizeMultFor(later), Is.EqualTo(0.55f).Within(0.001f));
        float poise = SlotPassiveCombat.ScaleOutgoingPoise(15f, skillPoiseMult: 1f, director.PoiseDamageMultFor(later));
        Assert.That(poise, Is.EqualTo(18f).Within(0.001f));
        Assert.That(poise, Is.GreaterThan(15f));
    }

    [Test]
    public void Rune2_Emme_LifestealOutweighsTheSmallDamageCut()
    {
        SlotPassiveDirector director = Arm(2, out _, out int later);
        Assert.That(director.LifestealAddFor(later), Is.EqualTo(0.3f).Within(0.001f));
        Assert.That(director.DamageMultFor(later), Is.EqualTo(0.95f).Within(0.001f));
        float dealt = 100f * director.DamageMultFor(later);
        float healed = dealt * director.LifestealAddFor(later);
        Assert.That(healed, Is.GreaterThan(100f - dealt));
    }

    [Test]
    public void Rune3_Sicrama_BouncesAtJsonDamage()
    {
        SlotPassiveDirector director = Arm(3, out _, out int later);
        Assert.That(director.BounceCountFor(later), Is.EqualTo(2));
        Assert.That(director.BounceDamageMultFor(later), Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(director.HitboxSizeMultFor(later), Is.EqualTo(1.1f).Within(0.001f));
        Assert.That(PassiveBounce.RangeM, Is.EqualTo(6f).Within(0.001f));

        List<PassiveBounceHit> onlyBoss = PassiveBounce.Plan(
            80f, 2, 0.5f, sourceTargetId: 1, candidates: new List<PassiveBounceCandidate>());
        Assert.That(onlyBoss, Has.Count.EqualTo(2));
        Assert.That(onlyBoss[0].TargetId, Is.EqualTo(1));
        Assert.That(onlyBoss[0].Damage, Is.EqualTo(40f).Within(0.001f));
        Assert.That(80f + onlyBoss[0].Damage + onlyBoss[1].Damage, Is.GreaterThan(80f));

        var others = new List<PassiveBounceCandidate>
        {
            new PassiveBounceCandidate(9, 8f),
            new PassiveBounceCandidate(4, 3f),
            new PassiveBounceCandidate(5, 5.5f)
        };
        List<PassiveBounceHit> bounced = PassiveBounce.Plan(80f, 2, 0.5f, 1, others);
        Assert.That(bounced, Has.Count.EqualTo(2));
        Assert.That(bounced[0].TargetId, Is.EqualTo(4));
        Assert.That(bounced[1].TargetId, Is.EqualTo(5));
    }

    [Test]
    public void Rune4_Sabitleme_GivesPoiseAndFieldLifeWithoutRooting()
    {
        SlotPassiveDirector director = Arm(4, out _, out int later);
        Assert.That(director.BlocksPlayerMovement, Is.False);
        Assert.That(director.StringModifier("cast_mobility"), Is.EqualTo(string.Empty));
        Assert.That(director.PoiseDamageMultFor(later), Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(director.LifetimeAddSecFor(later), Is.EqualTo(2f).Within(0.001f));
        Assert.That(SlotPassiveCombat.ScaleOutgoingPoise(20f, 1f, director.PoiseDamageMultFor(later)),
            Is.EqualTo(30f).Within(0.001f));
    }

    [Test]
    public void Rune6_Baglama_SlowsByTheGrammarFractionAndRootsTheEnemy()
    {
        SlotPassiveDirector director = Arm(6, out _, out int later);
        Assert.That(director.SlowSpeedFor(later), Is.EqualTo(0.7f).Within(0.001f));
        Assert.That(director.RootSecondsFor(later), Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(director.BlocksPlayerMovement, Is.False);
    }

    [Test]
    public void Rune9_Odaklama_IgnoresArmorForItsDuration()
    {
        Assert.That(_motor.TryGetRune(9, out RuneDefinition rune), Is.True);
        Assert.That(rune.PassiveDurationDefault, Is.EqualTo(5f).Within(0.001f));
        SlotPassiveDirector director = Arm(9, out int trigger, out int later);
        Assert.That(director.ArmorPenPercentFor(trigger), Is.EqualTo(0f).Within(0.001f));
        Assert.That(director.ArmorPenPercentFor(later), Is.EqualTo(SlotPassiveCombat.IgnoreArmorPierce).Within(0.001f));
        Assert.That(director.DamageMultFor(later), Is.EqualTo(1.15f).Within(0.001f));

        float pen = SlotPassiveCombat.CombineArmorPen(0f, skillIgnoresArmor: true, slotPen: 0f);
        Assert.That(pen, Is.EqualTo(1f).Within(0.001f));
        DamageOutcome open = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 200f,
            Armor = 100f,
            ArmorPenPercent = pen
        });
        DamageOutcome closed = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 200f,
            Armor = 100f
        });
        Assert.That(open.ArmorAfterPen, Is.EqualTo(0f).Within(0.01f));
        Assert.That(open.Amount, Is.GreaterThan(closed.Amount));
        Assert.That(open.Amount, Is.EqualTo(200f).Within(0.01f));
    }

    [Test]
    public void Bug1_TriggeringCast_DoesNotDoubleDamageHitboxOrLifesteal()
    {
        SlotPassiveDirector focus = Arm(1, out int focusTrigger, out int focusLater);
        float skillDamage = 1.35f;
        float skillHitbox = 0.55f;
        Assert.That(skillDamage * focus.DamageMultFor(focusTrigger), Is.EqualTo(1.35f).Within(0.001f));
        Assert.That(skillHitbox * focus.HitboxSizeMultFor(focusTrigger), Is.EqualTo(0.55f).Within(0.001f));
        Assert.That(skillDamage * focus.DamageMultFor(focusLater), Is.EqualTo(1.35f * 1.35f).Within(0.001f));

        SlotPassiveDirector drain = Arm(2, out int drainTrigger, out int drainLater);
        const float skillLifesteal = 0.3f;
        Assert.That(skillLifesteal + drain.LifestealAddFor(drainTrigger), Is.EqualTo(0.3f).Within(0.001f));
        Assert.That(skillLifesteal + drain.LifestealAddFor(drainLater), Is.EqualTo(0.6f).Within(0.001f));
    }

    [Test]
    public void Bug2_BaglamaSlow_IsOneMinusApplySlow()
    {
        SlotPassiveDirector director = Arm(6, out int trigger, out int later);
        Assert.That(director.SlowSpeedFor(trigger), Is.EqualTo(1f).Within(0.001f));
        Assert.That(director.SlowSpeedFor(later), Is.EqualTo(0.7f).Within(0.001f));
        Assert.That(director.SlowSpeedFor(later), Is.GreaterThan(0.3f));
    }

    [Test]
    public void Bug3_Sabitleme_NeverRootsThePlayer()
    {
        SlotPassiveDirector director = Arm(4, out int trigger, out int later);
        Assert.That(director.BlocksPlayerMovement, Is.False);
        Assert.That(director.StringModifier("cast_mobility"), Is.EqualTo(string.Empty));
        Assert.That(director.PoiseDamageMultFor(trigger), Is.EqualTo(1f).Within(0.001f));
        Assert.That(director.LifetimeAddSecFor(trigger), Is.EqualTo(0f).Within(0.001f));
        Assert.That(director.PoiseDamageMultFor(later), Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(director.LifetimeAddSecFor(later), Is.EqualTo(2f).Within(0.001f));
    }

    [Test]
    public void PassiveSlot_DoesNotRequireWeaponCompatibility()
    {
        JsonValue root = MiniJson.Parse(_json);
        Assert.That(PassiveSlotPolicy.RequiresWeaponCompatibility(root), Is.False);
        Assert.That(PassiveSlotPolicy.ShouldArm(true, weaponPassiveEnabled: false, policyRequiresWeapon: false), Is.True);
        Assert.That(PassiveSlotPolicy.ShouldArm(true, weaponPassiveEnabled: false, policyRequiresWeapon: true), Is.False);
        Assert.That(PassiveSlotPolicy.ShouldArm(false, weaponPassiveEnabled: true, policyRequiresWeapon: false), Is.False);
    }

    SlotPassiveDirector Arm(int runeId, out int triggerCast, out int laterCast)
    {
        Assert.That(_motor.TryGetRune(runeId, out RuneDefinition rune), Is.True);
        Assert.That(_motor.TryGetAdjective(runeId.ToString(), out AdjectiveNode adjective), Is.True);
        var director = new SlotPassiveDirector();
        triggerCast = director.OpenCast();
        Assert.That(director.Activate(
            runeId,
            rune.AdjectiveFace,
            rune.PassiveDurationDefault,
            adjective.Engine,
            worldMs: 0), Is.True);
        director.CloseCast();
        laterCast = director.OpenCast();
        return director;
    }

}
