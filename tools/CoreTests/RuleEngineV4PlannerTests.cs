using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4PlannerTests
{
    static string JsonPath() =>
        Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json"));

    static RuleEngineV4Planner Planner()
    {
        string json = System.IO.File.ReadAllText(JsonPath());
        return new RuleEngineV4Planner(RuleEngineV4Catalog.FromJson(json));
    }

    [Test]
    public void SliceRunes_AreSharedBetweenVerbAndAdjective()
    {
        var catalog = RuleEngineV4Catalog.FromJson(System.IO.File.ReadAllText(JsonPath()));
        Assert.That(catalog.SliceRunes, Is.EquivalentTo(new[] { 1, 2, 3, 4, 6, 9 }));
        Assert.That(RuleEngineV4Slice.Runes, Is.EquivalentTo(catalog.SliceRunes));
    }

    [Test]
    public void SliceRunes_ReadFromJson_ChangingDataChangesRunes()
    {
        string json = System.IO.File.ReadAllText(JsonPath());
        var root = JsonNode.Parse(json)!.AsObject();
        Assert.That(root.ContainsKey("slice_runes"), Is.True, "slice_runes JSON'da yok");
        var alteredRoot = root.DeepClone().AsObject();
        alteredRoot["slice_runes"] = new JsonArray(1, 3);
        string altered = alteredRoot.ToJsonString();
        try
        {
            RuleEngineV4Catalog.FromJson(altered);
            Assert.That(RuleEngineV4Slice.Runes, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(RuleEngineV4Slice.IsSliceCombo(1, 3), Is.True);
            Assert.That(RuleEngineV4Slice.IsSliceCombo(2, 1), Is.False, "2 artık dilimde değil");
        }
        finally
        {
            RuleEngineV4Catalog.FromJson(json);
        }
        Assert.That(RuleEngineV4Slice.Runes, Is.EqualTo(new[] { 1, 2, 3, 4, 6, 9 }));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(9)]
    public void Zaman_BelowMinComboLen_PlansNothing(int adj)
    {
        var catalog = RuleEngineV4Catalog.FromJson(System.IO.File.ReadAllText(JsonPath()));
        Assert.That(catalog.TryGetVerb(12, out RuleEngineV4Verb zaman), Is.True);
        Assert.That(zaman.MinComboLen, Is.EqualTo(3), "zaman min_combo_len");
        var planner = new RuleEngineV4Planner(catalog);
        var twoDots = new RuleEngineV4CastContext(
            hasValidTarget: true, targetInWeaponRange: true, hasUsableMark: true, comboLength: 2);
        foreach (int weapon in RuleEngineV4Slice.Weapons)
        {
            CommandPlan plan = planner.Plan(12, adj, weapon, twoDots);
            Assert.That(plan.IsValid, Is.False, $"12-{adj} w{weapon} iki noktada plan üretti");
            Assert.That(plan.Commands, Is.Empty, $"12-{adj} w{weapon}");
        }
    }

    [Test]
    public void MinComboLen_OnlyGatesVerbsThatDeclareIt()
    {
        var catalog = RuleEngineV4Catalog.FromJson(System.IO.File.ReadAllText(JsonPath()));
        var planner = new RuleEngineV4Planner(catalog);
        var twoDots = new RuleEngineV4CastContext(hasValidTarget: true, targetInWeaponRange: true, comboLength: 2);
        foreach (int verb in catalog.SliceRunes)
        {
            Assert.That(catalog.TryGetVerb(verb, out RuleEngineV4Verb v), Is.True);
            Assert.That(v.MinComboLen, Is.LessThanOrEqualTo(2), $"dilim fiili {verb} iki noktada çalışmalı");
        }
        Assert.That(planner.Plan(1, 2, 4, twoDots).IsValid, Is.True, "1-2 iki noktada plan üretmeli");
        var threeDots = new RuleEngineV4CastContext(
            hasValidTarget: true, targetInWeaponRange: true, hasUsableMark: true, comboLength: 3);
        Assert.That(planner.Plan(12, 2, 4, threeDots).IsValid, Is.True, "12-2 üç noktada kapıdan geçmeli");
    }

    [Test]
    public void SliceWeapons_CekicOut_RulesStayInJson()
    {
        Assert.That(RuleEngineV4Slice.IsSliceWeapon(6), Is.False, "Çekiç dilimde");
        Assert.That(RuleEngineV4Slice.IsSliceWeapon(2), Is.True, "Yay dilimde değil");
        Assert.That(RuleEngineV4Slice.IsSliceWeapon(4), Is.True, "Kılıç dilimde değil");
        var catalog = RuleEngineV4Catalog.FromJson(System.IO.File.ReadAllText(JsonPath()));
        Assert.That(catalog.TryGetWeapon(6, out _), Is.True, "Çekiç silah kuralı JSON'dan silinmiş");
        Assert.That(new RuleEngineV4Planner(catalog).Plan(1, 2, 6).IsValid, Is.True, "Çekiç planı");
        Assert.That(catalog.SkillAnim.Resolve(1, 2, 6).Clip, Is.EqualTo("Cekic_VUR"), "Çekiç skill_anim");
    }

    [Test]
    public void Scale_FromData_PlayerHp1000_BaseDamage100()
    {
        var catalog = RuleEngineV4Catalog.FromJson(System.IO.File.ReadAllText(JsonPath()));
        RuleEngineV4Scale s = catalog.Scale;
        Assert.That(s.PlayerMaxHp, Is.EqualTo(1000));
        Assert.That(s.AllyDummyMaxHp, Is.EqualTo(1000));
        Assert.That(s.LightCreatureMaxHp, Is.EqualTo(400));
        Assert.That(s.BossMaxHp, Is.EqualTo(15000));
        Assert.That(s.BaseDamage, Is.EqualTo(100));
        Assert.That(s.BaseHeal, Is.EqualTo(120));
        Assert.That(s.BasePoise, Is.EqualTo(20));
        Assert.That(s.BossPoiseBar, Is.EqualTo(300));
        Assert.That(s.ManaPool, Is.EqualTo(100));
        Assert.That(s.ManaRegenPerSec, Is.EqualTo(10));
        Assert.That(s.ManaCostPerSkill, Is.EqualTo(25));
        Assert.That(s.CooldownSec, Is.EqualTo(4));
    }

    [Test]
    public void Scale_HundredDamage_IsTenPercentOfPlayerHp()
    {
        RuleEngineV4Scale s = RuleEngineV4Catalog.FromJson(System.IO.File.ReadAllText(JsonPath())).Scale;
        Assert.That(s.BaseDamage / s.PlayerMaxHp, Is.EqualTo(0.1f).Within(1e-6f));
    }

    [Test]
    public void Slice108Combos_ProduceValidCommandLists()
    {
        var planner = Planner();
        foreach (int rune in RuleEngineV4Slice.Runes)
        foreach (int adj in RuleEngineV4Slice.Runes)
        foreach (int weapon in RuleEngineV4Slice.Weapons)
        {
            CommandPlan plan = planner.Plan(rune, adj, weapon);
            Assert.That(plan.IsValid, Is.True, $"combo {rune}-{adj} weapon {weapon}");
            Assert.That(
                plan.Commands[0] is MenzileYuruCommand or OnSureCommand,
                Is.True,
                $"combo {rune}-{adj} w{weapon}");
            Assert.That(plan.Commands.Any(c => c is OnSureCommand), Is.True);
            Assert.That(plan.Commands.Any(c => c is not OnSureCommand && c is not MenzileYuruCommand), Is.True);
            foreach (PhysicsCommand cmd in plan.Commands)
                Assert.That(Enum.IsDefined(typeof(PhysicsCommandKind), cmd.Kind), Is.True);
        }
    }

    [Test]
    public void SameCombo_DifferentWeapons_DifferByRangeAndRhythm()
    {
        var planner = Planner();
        const int verb = 1;
        const int adj = 1;

        CommandPlan kilic = planner.Plan(verb, adj, 4);
        CommandPlan yay = planner.Plan(verb, adj, 2);
        CommandPlan cekic = planner.Plan(verb, adj, 6);

        float kilicRange = DeliveryRange(kilic);
        float yayRange = DeliveryRange(yay);
        float cekicRange = DeliveryRange(cekic);
        Assert.That(yayRange, Is.GreaterThan(kilicRange));
        Assert.That(kilicRange, Is.EqualTo(cekicRange).Within(0.001f));

        var kilicWind = (OnSureCommand)kilic.Commands.First(c => c is OnSureCommand);
        var yayWind = (OnSureCommand)yay.Commands.First(c => c is OnSureCommand);
        var cekicWind = (OnSureCommand)cekic.Commands.First(c => c is OnSureCommand);
        Assert.That(yayWind.PrefireSec, Is.Not.EqualTo(cekicWind.PrefireSec));
        Assert.That(cekicWind.DamageScale, Is.GreaterThan(kilicWind.DamageScale));
        Assert.That(kilicWind.DamageScale, Is.LessThan(yayWind.DamageScale));

        var kilicDmg = (HasarVerCommand)kilic.Commands.First(c => c is HasarVerCommand);
        var cekicDmg = (HasarVerCommand)cekic.Commands.First(c => c is HasarVerCommand);
        Assert.That(cekicDmg.Amount, Is.GreaterThan(kilicDmg.Amount));
    }

    [Test]
    public void TargetOutOfRange_InsertsMenzileYuruBeforeOnSure()
    {
        var ctx = new RuleEngineV4CastContext(true, false);
        CommandPlan plan = Planner().Plan(1, 2, 4, ctx);
        Assert.That(plan.Commands[0], Is.TypeOf<MenzileYuruCommand>());
        Assert.That(plan.Commands[1], Is.TypeOf<OnSureCommand>());
        Assert.That(plan.Target, Is.Not.Null);
    }

    [Test]
    public void NoValidTarget_EmptyPlan()
    {
        var ctx = new RuleEngineV4CastContext(false, false);
        CommandPlan plan = Planner().Plan(1, 2, 4, ctx);
        Assert.That(plan.IsValid, Is.False);
    }

    [Test]
    public void IsaretliHareket_WithoutMark_EmptyPlan()
    {
        var ctx = new RuleEngineV4CastContext(true, true, hasUsableMark: false);
        CommandPlan plan = Planner().Plan(3, 9, 4, ctx);
        Assert.That(plan.IsValid, Is.False);
    }

    [Test]
    public void YayHareket_RopeOnlyWhenMarked_ElseShortBackstep()
    {
        var catalog = RuleEngineV4Catalog.FromJson(System.IO.File.ReadAllText(JsonPath()));
        Assert.That(catalog.TryGetWeapon(2, out RuleEngineV4Weapon yay), Is.True);
        Assert.That(yay.MotionRopeAdjectiveRune, Is.EqualTo(9), "ip yalnız İşaretli-Hareket");
        var planner = new RuleEngineV4Planner(catalog);
        var marked = new RuleEngineV4CastContext(true, true, hasUsableMark: true);

        CommandPlan rope = planner.Plan(3, 9, 2, marked);
        Assert.That(rope.Commands.Any(c => c is MermiFirlatCommand), Is.True, "3-9 ip atmadı");
        Assert.That(rope.Commands.OfType<KendiniTasiCommand>().Single().Backstep, Is.False, "3-9 uçmadı");

        for (int adj = 1; adj <= 12; adj++)
        {
            if (adj == 9 || !catalog.TryGetAdjective(adj, out _))
                continue;
            CommandPlan plan = planner.Plan(3, adj, 2, new RuleEngineV4CastContext(true, false, hasUsableMark: true));
            Assert.That(plan.IsValid, Is.True, $"3-{adj}");
            Assert.That(plan.Commands.Any(c => c is MermiFirlatCommand or MenzileYuruCommand or KilitlenCommand or YapiKurCommand),
                Is.False, $"3-{adj} yay: ip/yürüyüş/kilit/yapı");
            KendiniTasiCommand hop = plan.Commands.OfType<KendiniTasiCommand>().Single();
            Assert.That(hop.Backstep, Is.True, $"3-{adj} geri sıçrama değil");
            Assert.That(hop.MaxDistanceM, Is.EqualTo(yay.BackstepM));
            Assert.That(hop.SpeedMps, Is.EqualTo(yay.BackstepSpeedMps));
            Assert.That(RuleEngineV4CommandAccess.TryBackstep(plan, out float hopSec), Is.True);
            Assert.That(hopSec, Is.EqualTo(yay.BackstepM / yay.BackstepSpeedMps).Within(1e-6f));
        }

        // Kılıç'ta kural yok: Hareket her sıfatta eskisi gibi.
        Assert.That(planner.Plan(3, 3, 4).Commands.OfType<KendiniTasiCommand>().All(k => !k.Backstep), Is.True);
    }

    [Test]
    public void IsaretliArindirma_OnlyPlacesMark()
    {
        CommandPlan plan = Planner().Plan(9, 9, 4);
        Assert.That(plan.Commands.Any(c => c is IsaretKoyCommand), Is.True);
        Assert.That(plan.Commands.Any(c => c is EtkiSokCommand), Is.False);
    }

    [Test]
    public void ExtendedVerbs_KuvvetYansimaCagirmaZaman_ProduceEffects()
    {
        var planner = Planner();
        Assert.That(planner.Plan(5, 1, 4).Commands.Any(c => c is ItCommand), Is.True);
        Assert.That(planner.Plan(10, 2, 4).Commands.Any(c => c is YansitCommand), Is.True);
        Assert.That(planner.Plan(11, 1, 4).Commands.Any(c => c is YardimciCagirCommand), Is.True);
        // Zaman min_combo_len 3: etkisi üç noktalı komboda.
        var threeDots = new RuleEngineV4CastContext(hasValidTarget: true, targetInWeaponRange: true, comboLength: 3);
        Assert.That(planner.Plan(12, 2, 4, threeDots).Commands.Any(c => c is KaydaDonCommand), Is.True);
        Assert.That(planner.Plan(12, 2, 4, threeDots).Commands.Any(c => c is KayitAlCommand), Is.True);
    }

    [Test]
    public void TetikliZarar_IncludesKapanKur()
    {
        CommandPlan plan = Planner().Plan(1, 7, 4);
        Assert.That(plan.Commands.Any(c => c is KapanKurCommand), Is.True);
        Assert.That(plan.Commands.Any(c => c is HasarVerCommand), Is.True);
    }

    [Test]
    public void YogunZarar_Kilic_HasChargeOnHostileVerb()
    {
        var plan = Planner().Plan(1, 1, 4);
        var wind = (OnSureCommand)plan.Commands.First(c => c is OnSureCommand);
        Assert.That(wind.ChargeSec, Is.EqualTo(0.35f).Within(0.001f));
    }

    [Test]
    public void YogunSifa_NoChargeOnFriendlyVerb()
    {
        var plan = Planner().Plan(2, 1, 4);
        var wind = (OnSureCommand)plan.Commands.First(c => c is OnSureCommand);
        Assert.That(wind.ChargeSec, Is.EqualTo(0f));
    }

    static float DeliveryRange(CommandPlan plan)
    {
        foreach (PhysicsCommand c in plan.Commands)
        {
            switch (c)
            {
                case YakinVurusCommand y: return y.RangeM;
                case MermiFirlatCommand m: return m.RangeM;
                case AlanAcCommand a: return a.RadiusM;
            }
        }
        return -1;
    }
}
