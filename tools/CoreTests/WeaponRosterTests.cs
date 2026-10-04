using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using NUnit.Framework;
using System;
using System.IO;
using Dovus.Core.Shared;

namespace CoreTests;

/// <summary>10 silah: veri, pasif, değiştirme, kalıp mesafesi.</summary>
[TestFixture]
public class WeaponRosterTests
{
    EquipmentCatalog _catalog;
    string _elements;
    MotionTemplateCatalog _motion;

    [SetUp]
    public void Load()
    {
        _elements = File.ReadAllText(ElementPath());
        _catalog = EquipmentCatalog.FromJson(_elements);
        _motion = MotionTemplateCatalog.FromJson(File.ReadAllText(MotionPath()));
    }

    [Test]
    public void LoadsTenWeapons_WithDocStats()
    {
        Assert.That(_catalog.Items.Count, Is.EqualTo(10));
        Expect(1, "Yumruk", "melee", 0.8f, 0.7f, 1.3f, 1.2f, 0f, new[] { 1, 3, 5, 6 });
        Expect(4, "Kılıç", "medium", 1f, 0.9f, 1.1f, 2.4f, 0f, new[] { 1, 3, 5, 9 });
        Expect(6, "Çekiç", "melee", 1.5f, 1.5f, 1.8f, 2.4f, 15f, new[] { 1, 4, 5, 6 });
        Expect(10, "Kalkan", "melee", 0.5f, 0.8f, 2f, 1.2f, 25f, new[] { 4, 6, 8, 10 });
        Expect(2, "Yay", "ranged", 0.8f, 0.7f, 0.6f, 20f, 0f, new[] { 1, 3, 7, 9 });
        Expect(7, "Top", "ranged", 1.8f, 2f, 1.5f, 25f, 10f, new[] { 5, 6, 7, 12 });
        Expect(8, "Asa", "ranged", 0.9f, 1.3f, 0.7f, 21f, 0f, new[] { 2, 5, 8, 12 });
        Expect(9, "Tılsım", "ranged", 0.6f, 1.1f, 0.5f, 24f, 0f, new[] { 2, 8, 9, 11 });
        Expect(3, "Büyü Kitabı", "ranged", 0.6f, 0.6f, 0.5f, 13f, 0f, new[] { 7, 10, 11, 12 });
        Expect(5, "Küre", "ranged", 1f, 1f, 0.8f, 10f, 0f, new[] { 3, 4, 10, 11 });
    }

    [Test]
    public void BasicAttack_MatchesEachWeapon()
    {
        Assert.That(W(1).Profile.BasicHits, Is.EqualTo(3));
        Assert.That(W(1).Profile.HitShape, Is.EqualTo("point"));
        Assert.That(W(4).Profile.ArcDeg, Is.EqualTo(144f));
        Assert.That(W(4).Profile.BasicHits, Is.EqualTo(3));
        Assert.That(W(6).Profile.BasicKind, Is.EqualTo("slam"));
        Assert.That(W(10).Profile.BasicKind, Is.EqualTo("bash_block"));
        Assert.That(W(2).Profile.BasicIntervalSec, Is.EqualTo(0.5f));
        Assert.That(W(7).Profile.BasicIntervalSec, Is.EqualTo(1.5f));
        Assert.That(W(7).Profile.BasicRadiusM, Is.EqualTo(3f));
        Assert.That(W(8).Profile.BasicRadiusM, Is.EqualTo(1f));
        Assert.That(W(9).Profile.BasicAllyHeal, Is.EqualTo(5f));
        Assert.That(W(9).Profile.EffectTravels, Is.False);
        Assert.That(W(3).Profile.CooldownMult, Is.EqualTo(0.75f));
        Assert.That(W(5).Profile.OrbPlaceM, Is.EqualTo(8f));
        Assert.That(W(5).Profile.OrbSpellM, Is.EqualTo(10f));
        Assert.That(W(5).Profile.OrbMoveSec, Is.EqualTo(0.4f));
        Assert.That(W(5).Profile.OrbCooldownSec, Is.EqualTo(2f));
    }

    [Test]
    public void BaseArmor_HeavyModerate_LightZero()
    {
        Assert.That(W(1).BaseArmor, Is.EqualTo(0f));
        Assert.That(W(2).BaseArmor, Is.EqualTo(0f));
        Assert.That(W(3).BaseArmor, Is.EqualTo(0f));
        Assert.That(W(4).BaseArmor, Is.EqualTo(0f));
        Assert.That(W(5).BaseArmor, Is.EqualTo(0f));
        Assert.That(W(8).BaseArmor, Is.EqualTo(0f));
        Assert.That(W(9).BaseArmor, Is.EqualTo(0f));
        Assert.That(W(6).BaseArmor, Is.EqualTo(15f));
        Assert.That(W(7).BaseArmor, Is.EqualTo(10f));
        Assert.That(W(10).BaseArmor, Is.EqualTo(25f));
    }

    [Test]
    public void EachPassive_FiresOnlyWhenTheDocSays()
    {
        WeaponPassiveMods back = Eval(1, harmful: true, behind: 40f);
        Assert.That(back.DamageMult, Is.EqualTo(1.5f));
        Assert.That(Eval(1, harmful: true, behind: 80f).DamageMult, Is.EqualTo(1f));
        Assert.That(Eval(1, enabled: false, harmful: true, behind: 0f).DamageMult, Is.EqualTo(1f));

        WeaponPassiveMods arc = Eval(4, support: true);
        Assert.That(arc.ArcAllies, Is.True);
        Assert.That(arc.ArcDeg, Is.EqualTo(144f));
        Assert.That(WeaponPassiveRules.AngleInArc(70f, 144f), Is.True);
        Assert.That(WeaponPassiveRules.AngleInArc(80f, 144f), Is.False);

        WeaponPassiveMods slam = Eval(6, harmful: true);
        Assert.That(slam.Stun, Is.True);
        Assert.That(slam.StunSec, Is.EqualTo(0.5f));
        var hammer = new WeaponPassiveState();
        Assert.That(hammer.HammerReady(0), Is.True);
        hammer.CommitHammer(0, W(6).Profile.Passive.IcdSec);
        Assert.That(hammer.HammerReady(3000), Is.False);
        Assert.That(hammer.HammerReady(4000), Is.True);

        Assert.That(Eval(10, blocked: false).DamageMult, Is.EqualTo(1f));
        Assert.That(Eval(10, blocked: true).DamageMult, Is.EqualTo(1.2f));

        Assert.That(Eval(2, sinceMoved: 0.2f).CritChanceAdd, Is.EqualTo(0.20f).Within(0.0001f));
        Assert.That(Eval(2, sinceMoved: 1.5f).CritChanceAdd, Is.EqualTo(0f));

        Assert.That(Eval(7, sinceMoved: 0.6f).DamageMult, Is.EqualTo(1.5f));
        Assert.That(Eval(7, sinceMoved: 0.2f).DamageMult, Is.EqualTo(1f));

        Assert.That(Eval(8, sustained: true).DurationMult, Is.EqualTo(1.3f));
        Assert.That(Eval(8, sustained: false).DurationMult, Is.EqualTo(1f));

        Assert.That(WeaponPassiveRules.SupportPower(W(9).Profile, true, 2, 0.6f), Is.EqualTo(1.2f));
        Assert.That(WeaponPassiveRules.SupportPower(W(9).Profile, true, 1, 0.6f), Is.EqualTo(1.2f));
        Assert.That(WeaponPassiveRules.SupportPower(W(9).Profile, false, 4, 0.48f), Is.EqualTo(1.2f));
        Assert.That(WeaponPassiveRules.SupportPower(W(4).Profile, true, 2, 0.9f), Is.EqualTo(0.9f));

        var book = new WeaponPassiveState();
        Assert.That(book.NoteSkill(0, 2f), Is.EqualTo(1));
        Assert.That(book.NoteSkill(500, 2f), Is.EqualTo(2));
        int third = book.NoteSkill(1000, 2f);
        Assert.That(Eval(3, chain: third).DamageMult, Is.EqualTo(1.3f));
        Assert.That(book.NoteSkill(4000, 2f), Is.EqualTo(1));

        Assert.That(Eval(5, orbAngle: 100f).DamageMult, Is.EqualTo(1.2f));
        Assert.That(Eval(5, orbAngle: 40f).DamageMult, Is.EqualTo(1f));
    }

    [Test]
    public void Swap_TakesQuarterSecond_AndCooldownIs1_2()
    {
        var rules = WeaponSwapRules.FromJson(_elements);
        Assert.That(rules.AnimationSec, Is.EqualTo(WeaponSwapCancel.SwapSec).Within(0.0001f));
        Assert.That(rules.CooldownSec, Is.EqualTo(WeaponSwapCancel.CooldownSec).Within(0.0001f));
        var state = new WeaponSwapState(rules);
        state.SetLoadout(W(4), W(2));
        Assert.That(state.TryBegin(0, true), Is.EqualTo(WeaponSwapResult.Started));
        Assert.That(state.Tick(249), Is.False);
        Assert.That(state.Active.Name, Is.EqualTo("Kılıç"));
        Assert.That(state.Tick(250), Is.True);
        Assert.That(state.Active.Name, Is.EqualTo("Yay"));
        double readyMs = rules.CooldownSec * 1000.0;
        Assert.That(state.TryBegin(readyMs - 1, true), Is.EqualTo(WeaponSwapResult.OnCooldown));
        Assert.That(state.TryBegin(readyMs, true), Is.EqualTo(WeaponSwapResult.Started));
    }

    [Test]
    public void SwapCancel_WindowAndInstantChain_FollowTheTenTags()
    {
        SkillMotor motor = SkillMechanicTagTests.LoadMotorPublic();
        Assert.That(SkillMechanicTagTests.ExpectedSwapCancelSkillIds, Has.Length.EqualTo(10));
        foreach (string id in SkillMechanicTagTests.ExpectedSwapCancelSkillIds)
        {
            Assert.That(motor.TryGetSkill(id, out SkillCatalogEntry entry), Is.True, id);
            Assert.That(WeaponSwapCancel.IsTagged(new SkillEngineModifiers(entry.Engine)), Is.True, id);
            Assert.That(_motion.TryGet((SkillId)id, out MotionBinding binding), Is.True, id);
            Assert.That(binding.HasTag(MotionTemplateCatalog.TagSilah), Is.True, id);
            Assert.That(WeaponSwapCancel.InWindow(0.49f, 1f, true), Is.False, id);
            Assert.That(WeaponSwapCancel.InWindow(0.50f, 1f, true), Is.True, id);
            Assert.That(WeaponSwapCancel.UnlocksNextSkill(true, true), Is.True);
        }
        Assert.That(WeaponSwapCancel.InWindow(0.69f, 1f, false), Is.False);
        Assert.That(WeaponSwapCancel.InWindow(0.70f, 1f, false), Is.True);
        Assert.That(WeaponSwapCancel.UnlocksNextSkill(true, false), Is.False);
        Assert.That(WeaponSwapCancel.CutsRecovery(true), Is.True);
        Assert.That(WeaponSwapCancel.MayBegin(false, drawing: true, false, false, false, true, true), Is.False);
        Assert.That(WeaponSwapCancel.MayBegin(false, false, holding: true, false, false, true, false), Is.False);
        Assert.That(WeaponSwapCancel.MayBegin(false, false, holding: true, false, false, true, true), Is.True);
        Assert.That(WeaponSwapCancel.MayBegin(false, false, false, dodging: true, false, true, true), Is.False);
        Assert.That(WeaponSwapCancel.MayBegin(false, false, false, false, stunned: true, true, true), Is.False);
        Assert.That(WeaponSwapCancel.MayBegin(false, false, false, false, false, inWindow: true, false), Is.True);
    }

    [Test]
    public void SwapBonus_FirstHitWithinTwoSeconds()
    {
        var state = new WeaponPassiveState();
        EquipmentItem sword = W(4);
        state.ArmSwapBonus(sword.Profile.Id, sword.Profile.SwapBonusId, 0, sword.Profile.SwapBonus.WindowSec);
        Assert.That(state.TryConsumeBonus(sword.Profile.Id, 1999, out WeaponSwapBonusSpec bonus, sword.Profile), Is.True);
        Assert.That(bonus.ArcDeg, Is.EqualTo(180f));
        Assert.That(state.TryConsumeBonus(sword.Profile.Id, 2000, out _, sword.Profile), Is.False);

        EquipmentItem shield = W(10);
        state.ArmSwapBonus(shield.Profile.Id, shield.Profile.SwapBonusId, 0, 2f);
        Assert.That(state.PeekBonus(shield.Profile.Id, 100, out WeaponSwapBonusSpec peek, shield.Profile), Is.True);
        Assert.That(peek.Shield, Is.EqualTo(15f));
        Assert.That(peek.ShieldSec, Is.EqualTo(3f));

        EquipmentItem bow = W(2);
        state.ArmSwapBonus(bow.Profile.Id, bow.Profile.SwapBonusId, 0, 2f);
        state.TryConsumeBonus(bow.Profile.Id, 10, out WeaponSwapBonusSpec ignore, bow.Profile);
        Assert.That(ignore.IgnoreArmor, Is.True);

        EquipmentItem book = W(3);
        state.ArmSwapBonus(book.Profile.Id, book.Profile.SwapBonusId, 0, 2f);
        state.TryConsumeBonus(book.Profile.Id, 10, out WeaponSwapBonusSpec free, book.Profile);
        Assert.That(free.FreeMana, Is.True);

        EquipmentItem orb = W(5);
        Assert.That(orb.Profile.SwapBonus.SpawnsAtLastHit, Is.True);
        EquipmentItem cannon = W(7);
        Assert.That(cannon.Profile.SwapBonus.CountsAsStill, Is.True);
        Assert.That(cannon.Profile.SwapBonus.DamageMult, Is.EqualTo(1.5f));
        EquipmentItem staff = W(8);
        Assert.That(staff.Profile.SwapBonus.AreaMult, Is.EqualTo(1.25f));
        EquipmentItem charm = W(9);
        Assert.That(charm.Profile.SwapBonus.Cleanse, Is.True);
        EquipmentItem hammer = W(6);
        Assert.That(hammer.Profile.SwapBonus.PoiseMult, Is.EqualTo(2f));
        EquipmentItem fist = W(1);
        Assert.That(fist.Profile.SwapBonus.CountsAsBackstab, Is.True);
    }

    [Test]
    public void Orb_PlacesWithinEightMetres_AndRespectsCooldown()
    {
        var orb = new OrbAnchor();
        EquipmentItem item = W(5);
        orb.SnapToHand(0f, 0f);
        Assert.That(orb.TryPlace(0f, 0f, 0f, 20f, 0, item.Profile), Is.True);
        orb.Tick(400, 0f, 0f);
        Assert.That(orb.Z, Is.EqualTo(8f).Within(0.01f));
        Assert.That(orb.TryPlace(0f, 0f, 1f, 1f, 1000, item.Profile), Is.False);
        Assert.That(orb.TryRecall(0f, 0f, 2000, item.Profile), Is.True);
        orb.Tick(2400, 0f, 0f);
        Assert.That(orb.AtHand, Is.True);
    }

    [Test]
    public void TemplateTravel_IsIdentical_ForAllTenWeapons()
    {
        Assert.That(_motion.TryPlay((SkillId)"3-3", out MotionTemplate hops), Is.True);
        var grammar = new MechanicGrammar(MechanicRules.FromJson(_elements));
        float authored = FinishZ(hops);
        float? shared = null;
        foreach (MechanicWeapon weapon in grammar.Rules.Weapons)
        {
            MechanicPlan plan = grammar.Compose(3, 3, weapon.Id);
            double dash = plan.Find("kendini_tasi").Amount;
            var steps = new[] { new GrammarPositionStep("yer_degistir", dash) };
            PositionPlayback play = PositionOwnership.Prepare(hops, steps, 0.28f, 1.2f);
            Assert.That(play.Template.Phases[0].ForwardM, Is.EqualTo(hops.Phases[0].ForwardM), weapon.Name);
            Assert.That(play.Template.Phases[0].DistanceM, Is.EqualTo(hops.Phases[0].DistanceM), weapon.Name);
            float end = FinishZ(play.Template);
            Assert.That(end, Is.EqualTo(authored).Within(0.001f), weapon.Name + " dash=" + dash);
            shared ??= end;
            Assert.That(end, Is.EqualTo(shared.Value).Within(0.001f), weapon.Name);
        }
    }

    void Expect(int id, string name, string type, float damage, float cast, float poise, float reach, float armor, int[] verbs)
    {
        EquipmentItem item = W(id);
        Assert.That(item.Name, Is.EqualTo(name));
        Assert.That(item.Type, Is.EqualTo(type));
        Assert.That(item.DamageMult, Is.EqualTo(damage));
        Assert.That(item.CastTimeMult, Is.EqualTo(cast));
        Assert.That(item.PoiseMult, Is.EqualTo(poise));
        Assert.That(item.Profile.ReachM, Is.EqualTo(reach));
        Assert.That(item.BaseArmor, Is.EqualTo(armor));
        Assert.That(item.CompatibleVerbs, Is.EqualTo(verbs));
        Assert.That(item.Profile.Passive.Id, Is.Not.Empty);
    }

    EquipmentItem W(int id) => _catalog.FindWeapon(id);

    WeaponPassiveMods Eval(
        int weaponId,
        bool enabled = true,
        bool harmful = false,
        bool sustained = false,
        bool blocked = false,
        bool support = false,
        float behind = 180f,
        float sinceMoved = 99f,
        int chain = 0,
        float orbAngle = 0f)
    {
        EquipmentItem item = W(weaponId);
        int verb = item.CompatibleVerbs.Length > 0 ? item.CompatibleVerbs[0] : 1;
        if (support)
            verb = 2;
        var query = new WeaponPassiveQuery(
            verb, enabled, harmful, sustained, behind, sinceMoved, blocked, chain, orbAngle,
            support || WeaponPassiveRules.IsSupportVerb(verb));
        return WeaponPassiveRules.Evaluate(item.Profile, query);
    }

    static float FinishZ(MotionTemplate template)
    {
        var runner = new MotionTemplateRunner();
        var boss = new MotionTarget(true, 0f, 3f, 0.85f);
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, 0.5f, 0.15f);
        int guard = 0;
        while (!runner.Finished && guard++ < 600)
            runner.Tick(1f / 60f, boss, default);
        return runner.Z;
    }

    static string ElementPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        return path;
    }

    static string MotionPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "motion-templates.json"));
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "motion-templates.json"));
        return path;
    }
}
