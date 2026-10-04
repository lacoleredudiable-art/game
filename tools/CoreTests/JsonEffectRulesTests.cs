using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using Dovus.Core.Shared;

namespace CoreTests;

/// <summary>
/// JSON etkileri: element-sistemi.json mod/anahtarlarının saf kuralları (JsonEffectRules)
/// ve bu PR'ın Core uçları (TemplateDelivery itme/kıskaç, dünya profili, cleanse_count, shield_absorb).
/// </summary>
[TestFixture]
public class JsonEffectRulesTests
{
    string _root = null!;
    string _json = null!;
    SkillMotor _motor = null!;
    MechanicGrammar _grammar = null!;
    MotionTemplateCatalog _motion = null!;
    EquipmentCatalog _equipment = null!;

    [OneTimeSetUp]
    public void Load()
    {
        _root = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        if (!File.Exists(Path.Combine(_root, "docs", "element-sistemi.json")))
            _root = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        _json = File.ReadAllText(Path.Combine(_root, "docs", "element-sistemi.json"));
        _motor = SkillMotor.FromJson(_json);
        _grammar = new MechanicGrammar(MechanicRules.FromJson(_json));
        _motion = MotionTemplateCatalog.FromJson(File.ReadAllText(Path.Combine(_root, "docs", "motion-templates.json")));
        _equipment = EquipmentCatalog.FromJson(_json);
        Assert.That(_grammar.Rules.IsValid, Is.True);
    }

    MechanicPlan Plan(int verb, int adjective, int weapon = 4) => _grammar.Compose(verb, adjective, weapon);

    TemplateDeliveryOrder Order(int verb, int adjective, int weapon = 4)
    {
        string id = verb + "-" + adjective;
        Assert.That(_motion.TryPlay((SkillId)id, out MotionTemplate template), Is.True, id);
        var skill = _motor.Resolve(new[] { verb, adjective });
        return TemplateDelivery.Build(Plan(verb, adjective, weapon), skill.Engine, template, _grammar.Rules, 1f);
    }

    static bool HasBeat(TemplateDeliveryOrder order, DeliveryBeatKind kind) =>
        order.Schedule().Any(b => b.Kind == kind);

    // ---- P0: JSON ----
    [Test]
    public void Json_NewParamsAndArmorAdd_Present()
    {
        MechanicRules r = _grammar.Rules;
        Assert.That(r.Param("it_push_m"), Is.EqualTo(2.0).Within(1e-9));
        Assert.That(r.Param("it_hard_mult"), Is.EqualTo(1.25).Within(1e-9));
        Assert.That(r.Param("trap_arm_sec"), Is.EqualTo(0.5).Within(1e-9));
        Assert.That(r.Param("mine_arm_sec"), Is.EqualTo(1.0).Within(1e-9));
        Assert.That(r.Param("payload_min_life_sec"), Is.EqualTo(3.0).Within(1e-9));
        var skill = _motor.Resolve(new[] { 3, 4 });
        Assert.That(skill.Engine.Field("armor_add").AsFloat(0f), Is.EqualTo(25f));
        Assert.That(skill.Engine.Field("buff_duration_sec").AsFloat(0f), Is.EqualTo(3f));
    }

    [Test]
    public void Json_ResourcesCopy_IsByteIdentical()
    {
        byte[] docs = File.ReadAllBytes(Path.Combine(_root, "docs", "element-sistemi.json"));
        byte[] res = File.ReadAllBytes(Path.Combine(_root, "unity", "Assets", "Resources", "ElementSystem", "element-sistemi.json"));
        Assert.That(res, Is.EqualTo(docs));
    }

    // ---- A: push ----
    [TestCase(5, 1, 2.5f)]
    [TestCase(5, 5, 2.0f)]
    [TestCase(3, 5, 2.0f)]
    [TestCase(5, 12, 0f)]
    [TestCase(5, 4, 0f)]
    [TestCase(1, 1, 0f)]
    public void PushMeters_FollowsItModes(int verb, int adjective, float expected)
    {
        Assert.That(JsonEffectRules.PushMeters(Plan(verb, adjective), _grammar.Rules), Is.EqualTo(expected).Within(1e-4));
    }

    [Test]
    public void Order_CarriesPushM_AndLandingWave()
    {
        Assert.That(Order(5, 1).PushM, Is.EqualTo(2.5f).Within(1e-4));
        Assert.That(JsonEffectRules.LandingWavePush(Plan(3, 5)), Is.True);
        Assert.That(JsonEffectRules.LandingWavePush(Plan(3, 1)), Is.False);
    }

    [Test]
    public void LiftsBoss_OnHavayaAt()
    {
        Assert.That(JsonEffectRules.LiftsBoss(Plan(1, 8)), Is.True);
        Assert.That(JsonEffectRules.LiftsBoss(Plan(1, 1)), Is.False);
    }

    // ---- H2: pincer ----
    [Test]
    public void Pincer_OnlyWhenTemplateHitsOnce()
    {
        TemplateDeliveryOrder o = Order(5, 10);
        Assert.That(o.Pincer, Is.True);
        Assert.That(HasBeat(o, DeliveryBeatKind.Pincer), Is.True);
        Assert.That(o.PincerShare, Is.EqualTo(0.5f).Within(1e-4));
        Assert.That(Order(1, 10).Pincer, Is.False, "1-10 template already hits front and back");
    }

    // ---- B: armor ----
    [Test]
    public void StolenArmor_ScalesBossBaseArmor()
    {
        Assert.That(JsonEffectRules.StolenArmorFlat(0.05, 100f), Is.EqualTo(5f).Within(1e-4));
        Assert.That(JsonEffectRules.StolenArmorFlat(0, 100f), Is.EqualTo(0f));
        MechanicPlan plan = Plan(7, 2);
        Assert.That(plan.Effects.Any(e => e.Atom == "deger" && e.Stat == "zirh" && e.Target == "kendin" && e.Has("aktarim")), Is.True);
    }

    // ---- C: reflect family ----
    [Test]
    public void ParryWindow_ConsumesOnce()
    {
        var parry = new ParryWindow();
        parry.Arm(1000, 1f);
        Assert.That(parry.TryConsume(500, 100f, out float reflected), Is.True);
        Assert.That(reflected, Is.EqualTo(100f).Within(1e-4));
        Assert.That(parry.TryConsume(600, 100f, out _), Is.False);
        parry.Arm(1000, 0.5f);
        Assert.That(parry.TryConsume(1200, 100f, out _), Is.False, "window closed");
    }

    [Test]
    public void ReflectModes_Detected()
    {
        Assert.That(JsonEffectRules.IsParry(Plan(10, 9, 10)), Is.True);
        Assert.That(JsonEffectRules.ParryRatio(Plan(10, 9, 10)), Is.GreaterThan(0f));
        Assert.That(JsonEffectRules.IsSplitReflect(Plan(10, 10, 10)), Is.True);
        Assert.That(JsonEffectRules.IsWorldMirror(Plan(10, 4, 10)), Is.True);
        Assert.That(JsonEffectRules.IsRampReflect(Plan(10, 8, 10)), Is.True);
        Assert.That(MechanicWorldProfile.From(Plan(10, 4, 10)).Reflector, Is.True);
    }

    [Test]
    public void SplitAndRamp_Math()
    {
        JsonEffectRules.SplitReflect(10f, out float a, out float b);
        Assert.That(a + b, Is.EqualTo(10f).Within(1e-4));
        Assert.That(a, Is.EqualTo(5f).Within(1e-4));
        Assert.That(JsonEffectRules.RampedRatio(0.5f, 0, 1000, -10, 1.5), Is.EqualTo(0.5f).Within(1e-4));
        Assert.That(JsonEffectRules.RampedRatio(0.5f, 0, 1000, 500, 1.5), Is.EqualTo(0.625f).Within(1e-4));
        Assert.That(JsonEffectRules.RampedRatio(0.5f, 0, 1000, 2000, 1.5), Is.EqualTo(0.75f).Within(1e-4));
    }

    [Test]
    public void Hidden_OnGizli()
    {
        Assert.That(JsonEffectRules.HiddenSec(Plan(10, 7, 10), 2.0), Is.GreaterThan(0));
        Assert.That(JsonEffectRules.HiddenSec(Plan(10, 1, 10), 2.0), Is.EqualTo(0));
    }

    // ---- D: statuses ----
    [Test]
    public void StatusAdd_And_Cleanse_Counts()
    {
        Assert.That(Plan(9, 2).Effects.Any(e => e.Stat == "durum_ekle" && e.Target == "dusman"), Is.True);
        Assert.That(JsonEffectRules.StatusAddCount(0.4), Is.EqualTo(1));
        Assert.That(JsonEffectRules.StatusAddCount(2), Is.EqualTo(2));
        Assert.That(JsonEffectRules.CleanseCount(1, Plan(9, 1)), Is.EqualTo(1));
        Assert.That(JsonEffectRules.CleanseCount(1, Plan(9, 9)), Is.EqualTo(int.MaxValue), "tumunu_sil");
        Assert.That(JsonEffectRules.CleanseCount(0, null), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void StatusBoard_CleanseHostile_Count_RemovesHardCcFirst()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Slow, 3000, 0.5f);
        board.Apply(StatusKind.Stun, 1000, 1f);
        board.Apply(StatusKind.Weaken, 3000, 0.8f);
        Assert.That(board.CleanseHostile(1), Is.EqualTo(1));
        Assert.That(board.Has(StatusKind.Stun), Is.False);
        Assert.That(board.Has(StatusKind.Slow), Is.True);
        Assert.That(board.Has(StatusKind.Weaken), Is.True);
        Assert.That(board.CleanseHostile(int.MaxValue), Is.EqualTo(2));
        Assert.That(board.CleanseHostile(1), Is.EqualTo(0));
    }

    [Test]
    public void StatusApplicator_Cleanse_UsesEngineCount()
    {
        var skill = _motor.Resolve(new[] { 9, 1 });
        var caster = new StatusBoard();
        caster.Apply(StatusKind.Stun, 1000, 1f);
        caster.Apply(StatusKind.Slow, 3000, 0.5f);
        var result = StatusApplicator.ApplySkill(skill, caster, new StatusBoard(), new StatusTuning(), cleanseCount: 1);
        Assert.That(result.CleansedCount, Is.EqualTo(1));
    }

    [Test]
    public void StatusApplicator_Shield_UsesJsonShieldAbsorb()
    {
        var skill = _motor.Resolve(new[] { 4, 1 });
        Assert.That(skill.Engine.Field("shield_absorb").AsFloat(0f), Is.EqualTo(50f));
        var caster = new StatusBoard();
        var target = new StatusBoard();
        StatusApplicator.ApplySkill(skill, caster, target, new StatusTuning { ShieldAbsorb = 10f });
        StatusBoard board = StatusApplicator.IsSelfTargeted(skill) ? caster : target;
        Assert.That(board.ShieldRemaining, Is.EqualTo(50f).Within(1e-3));
    }

    [Test]
    public void Purge_And_MirroredDebuff()
    {
        Assert.That(JsonEffectRules.PurgeGrantsPower(Plan(9, 8)), Is.True);
        Assert.That(JsonEffectRules.PurgePower(3, 0.1), Is.EqualTo(0.3f).Within(1e-4));
        Assert.That(JsonEffectRules.MirroredEnemyDebuff(Plan(8, 10)), Is.Not.Null);
    }

    // ---- E: friendlies ----
    [Test]
    public void FriendlyCap_And_HealTargets()
    {
        Assert.That(JsonEffectRules.FriendlyCap(0), Is.EqualTo(1));
        Assert.That(JsonEffectRules.FriendlyCap(5), Is.EqualTo(5));

        JsonEffectRules.SelectHealTargets(true, true, 0.5f, 0.8f, false, false, 1, out bool ally, out bool self);
        Assert.That((ally, self), Is.EqualTo((true, false)), "cap 1 → lowest ratio");
        JsonEffectRules.SelectHealTargets(true, true, 0.6f, 0.6f, false, false, 1, out ally, out self);
        Assert.That((ally, self), Is.EqualTo((false, true)), "tie → self");
        JsonEffectRules.SelectHealTargets(true, true, 0.5f, 0.8f, false, false, 5, out ally, out self);
        Assert.That((ally, self), Is.EqualTo((true, true)), "max_targets 5 → both");
        JsonEffectRules.SelectHealTargets(true, true, 0.5f, 0.8f, false, true, 1, out ally, out self);
        Assert.That((ally, self), Is.EqualTo((false, true)), "prefer self, cap 1");
        JsonEffectRules.SelectHealTargets(false, true, 1f, 0.8f, false, false, 1, out ally, out self);
        Assert.That((ally, self), Is.EqualTo((false, true)));
    }

    [Test]
    public void Bounce_And_Overflow()
    {
        Assert.That(JsonEffectRules.IsFriendlyBounce(Plan(2, 3)), Is.True);
        Assert.That(JsonEffectRules.NextBounceIsAlly(false), Is.True);
        Assert.That(JsonEffectRules.NextBounceIsAlly(true), Is.False);
        Assert.That(JsonEffectRules.Overflows(Plan(2, 1), "can"), Is.True);
        Assert.That(JsonEffectRules.Overflows(Plan(4, 1), "kalkan"), Is.True);
        Assert.That(JsonEffectRules.Overflows(Plan(8, 1), "hasar_buff"), Is.True);
        Assert.That(JsonEffectRules.OverflowHeal(30, 12), Is.EqualTo(18));
        Assert.That(JsonEffectRules.OverflowHeal(30, 40), Is.EqualTo(0));
    }

    // ---- F/G: world payload ----
    [Test]
    public void PayloadKinds()
    {
        Assert.That(JsonEffectRules.HasPayload(Plan(2, 4), VolumePayloadKind.Totem), Is.True);
        Assert.That(JsonEffectRules.HasPayload(Plan(5, 4), VolumePayloadKind.Trap), Is.True);
        Assert.That(JsonEffectRules.HasPayload(Plan(1, 7), VolumePayloadKind.CloudTick), Is.True);
        Assert.That(JsonEffectRules.HasPayload(Plan(2, 8), VolumePayloadKind.Growing), Is.True);
        Assert.That(JsonEffectRules.NeedsPayloadVolume(Plan(1, 1)), Is.False);
        Assert.That(MechanicWorldProfile.From(Plan(2, 4)).Payload, Is.True);
        Assert.That(MechanicWorldProfile.From(Plan(5, 4)).Trap, Is.True);
    }

    [Test]
    public void TrapArm_MineAndFence()
    {
        Assert.That(JsonEffectRules.TrapArmSec(Plan(5, 4, 7).Body, _grammar.Rules), Is.EqualTo(1.0).Within(1e-9), "Top → mayin");
        Assert.That(JsonEffectRules.TrapArmSec(Plan(5, 4, 4).Body, _grammar.Rules), Is.EqualTo(0.5).Within(1e-9));
        Assert.That(JsonEffectRules.TrapRepeats(Plan(5, 4, 8).Body), Is.True, "Asa → cit");
        Assert.That(JsonEffectRules.TrapRepeats(Plan(5, 4, 4).Body), Is.False);
        Assert.That(JsonEffectRules.LandingFieldOnly(Plan(1, 12, 7).Body), Is.True, "Top x-12 → inen_akis_alani");
    }

    [Test]
    public void PayloadScale_And_Growth()
    {
        Assert.That(JsonEffectRules.PayloadScale(VolumePayloadKind.Totem, 0.33, 0.5, 1f), Is.EqualTo(0.33f).Within(1e-4));
        Assert.That(JsonEffectRules.PayloadScale(VolumePayloadKind.Trap, 0.33, 0.5, 1f), Is.EqualTo(0.5f).Within(1e-4));
        Assert.That(JsonEffectRules.PayloadScale(VolumePayloadKind.Growing, 0.33, 0.5, 1.5f), Is.EqualTo(0.495f).Within(1e-4));
        Assert.That(JsonEffectRules.PayloadScale(VolumePayloadKind.None, 0.33, 0.5, 1f), Is.EqualTo(0f));
        Assert.That(JsonEffectRules.AnyActorGrows(Plan(11, 8)), Is.True);
    }

    [Test]
    public void Pierce_Link_Pincer_Flags()
    {
        Assert.That(JsonEffectRules.PiercesDefenses(new MechanicBody { Permeability = "delici" }), Is.True);
        Assert.That(JsonEffectRules.PiercesDefenses(new MechanicBody()), Is.False);
        Assert.That(JsonEffectRules.LinkFlowsDamage(Plan(1, 6)), Is.True);
        Assert.That(JsonEffectRules.WantsPincer(Plan(5, 10)), Is.True);
    }

    // ---- I1: aoe ----
    [Test]
    public void Aoe_ConnectsInsideRadiusPlusBoss()
    {
        Assert.That(JsonEffectRules.AoeConnects(true, 2.5f, 1.5f, 2f, 0.85f), Is.True);
        Assert.That(JsonEffectRules.AoeConnects(true, 3.0f, 1.5f, 2f, 0.85f), Is.False);
        Assert.That(JsonEffectRules.AoeConnects(false, 0f, 1.5f, 2f, 0.85f), Is.False);
    }

    // ---- J: weapon basics ----
    [Test]
    public void Basic_SubHits_Cadence_Label()
    {
        Assert.That(JsonEffectRules.BasicSubHitScale(3), Is.EqualTo(1f / 3f).Within(1e-5));
        Assert.That(JsonEffectRules.BasicSubHitScale(0), Is.EqualTo(1f));
        Assert.That(JsonEffectRules.BasicSubHitDelaySec(2, 0.2f), Is.EqualTo(0.4).Within(1e-6));
        Assert.That(JsonEffectRules.BasicReady(100, 0, 0.2f, 3, false), Is.True, "toggle off → never gated");
        Assert.That(JsonEffectRules.BasicReady(500, 0, 0.2f, 3, true), Is.False);
        Assert.That(JsonEffectRules.BasicReady(600, 0, 0.2f, 3, true), Is.True);
        Assert.That(JsonEffectRules.BasicReady(0, -1, 0.2f, 3, true), Is.True, "first strike");
        Assert.That(JsonEffectRules.BasicKindLabel("combo", 3), Is.EqualTo("combo ×3"));
        Assert.That(JsonEffectRules.BasicKindLabel("seal", 1), Is.EqualTo("seal"));
        Assert.That(JsonEffectRules.BasicKindLabel(null, 1), Is.EqualTo(string.Empty));
    }

    [Test]
    public void Basic_ProfileFields_ParsedFromJson()
    {
        WeaponCombatProfile yumruk = _equipment.FindWeapon(1).Profile;
        Assert.That(yumruk.BasicHits, Is.EqualTo(3));
        Assert.That(yumruk.BasicIntervalSec, Is.EqualTo(0.2f).Within(1e-4));
        Assert.That(yumruk.BasicKind, Is.EqualTo("combo"));
        Assert.That(_equipment.FindWeapon(9).Profile.BasicAllyHeal, Is.EqualTo(5f));
    }

    // ---- K1: toggle default ----
    [Test]
    public void ManaCooldownToggle_DefaultsOn()
    {
        var c = new CombatTuning();
        Assert.That(c.EnforceResourceCost, Is.True);
        Assert.That(c.EnforceCooldown, Is.True);
    }
}
