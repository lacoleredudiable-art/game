using Dovus.Core.Combat;
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
        _json = File.ReadAllText(ElementPath());
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
    public void Rune5_Yayma_WidensHitboxAndRaisesTargetCap()
    {
        SlotPassiveDirector director = Arm(5, out _, out int later);
        Assert.That(director.HitboxSizeMultFor(later), Is.EqualTo(1.8f).Within(0.001f));
        Assert.That(director.MaxTargetsFor(later), Is.EqualTo(5));
        Assert.That(director.DamageMultFor(later), Is.EqualTo(0.85f).Within(0.001f));
        Assert.That(director.HitboxSizeMultFor(later), Is.GreaterThan(1f / director.DamageMultFor(later)));
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
    public void Rune7_Bulandirma_ExtendsTheAccuracyDebuffByLifetimeAdd()
    {
        SlotPassiveDirector director = Arm(7, out _, out int later);
        Assert.That(director.HasAccuracyDebuff(later), Is.True);
        Assert.That(director.AccuracyLifetimeAddSecFor(later), Is.EqualTo(3f).Within(0.001f));
        Assert.That(director.LifetimeAddSecFor(later), Is.EqualTo(3f).Within(0.001f));
    }

    [Test]
    public void Rune8_Yukseltme_BuffsOutgoingDamage()
    {
        SlotPassiveDirector director = Arm(8, out _, out int later);
        Assert.That(director.DamageMultFor(later), Is.EqualTo(1.1f).Within(0.001f));
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
    public void Rune10_Aynalama_ReflectsAShareOfIncomingDamage()
    {
        SlotPassiveDirector director = Arm(10, out _, out int later);
        Assert.That(director.ReflectRatioAddFor(later), Is.EqualTo(0.3f).Within(0.001f));
        Assert.That(100f * director.ReflectRatioAddFor(later), Is.EqualTo(30f).Within(0.001f));
    }

    [Test]
    public void Rune11_Kopyalama_EchoesTheNextCastOnceAtHalfPower()
    {
        Assert.That(_motor.TryGetRune(11, out RuneDefinition rune), Is.True);
        Assert.That(rune.PassiveDurationDefault, Is.EqualTo(5f).Within(0.001f));
        SlotPassiveDirector director = Arm(11, out int trigger, out int later);
        Assert.That(director.TryConsumeEcho(trigger, out _, out _), Is.False);
        Assert.That(director.TryConsumeEcho(later, out float delay, out float power), Is.True);
        Assert.That(delay, Is.EqualTo(0.3f).Within(0.001f));
        Assert.That(power, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(director.TryConsumeEcho(later, out _, out _), Is.False);
        Assert.That(80f * power, Is.EqualTo(40f).Within(0.001f));
    }

    [Test]
    public void Rune12_Akis_DoTOutweighsTheChannelTax()
    {
        SlotPassiveDirector director = Arm(12, out _, out int later);
        Assert.That(director.DamageMultFor(later), Is.EqualTo(0.7f).Within(0.001f));
        Assert.That(director.ChannelSecFor(later), Is.EqualTo(3f).Within(0.001f));
        Assert.That(director.TickRateMultFor(later), Is.EqualTo(1.2f).Within(0.001f));

        float dealt = 100f * director.DamageMultFor(later);
        Assert.That(PassiveFlowMath.TryPlan(
            director.ChannelSecFor(later),
            director.TickRateMultFor(later),
            dealt,
            baseTickSec: 1f,
            PassiveFlowMath.DefaultTickFraction,
            out PassiveFlowPlan plan), Is.True);
        Assert.That(plan.TickCount, Is.EqualTo(3));
        Assert.That(dealt + plan.TotalDamage, Is.GreaterThan(100f));

        var runner = new PassiveFlowRunner();
        runner.Start(plan, worldMs: 0);
        Assert.That(runner.Collect(0), Is.EqualTo(0f).Within(0.001f));
        Assert.That(runner.Collect(plan.TickSec * 1000.0), Is.EqualTo(plan.TickDamage).Within(0.001f));
        Assert.That(runner.Collect(4000), Is.EqualTo(plan.TickDamage * 2f).Within(0.001f));
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
            adjective.EngineModifiers,
            worldMs: 0), Is.True);
        director.CloseCast();
        laterCast = director.OpenCast();
        return director;
    }

    static string ElementPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "docs", "element-sistemi.json"));
        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..",
                "docs", "element-sistemi.json"));
        }
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }
}
