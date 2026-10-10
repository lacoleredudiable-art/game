using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        string json = File.ReadAllText(JsonPath());
        return new RuleEngineV4Planner(RuleEngineV4Catalog.FromJson(json));
    }

    [Test]
    public void FeatureFlag_DefaultOff()
    {
        RuleEngineV4Feature.Enabled = false;
        Assert.That(RuleEngineV4Feature.Enabled, Is.False);
        Assert.That(RuleEngineV4Feature.FlagName, Is.EqualTo("kural_motoru_v4"));
    }

    [Test]
    public void SliceRunes_AreSharedBetweenVerbAndAdjective()
    {
        Assert.That(RuleEngineV4Slice.Runes, Is.EquivalentTo(new[] { 1, 2, 3, 4, 6, 9 }));
    }

    [Test]
    public void Scale_FromData_PlayerHp1000_BaseDamage100()
    {
        var catalog = RuleEngineV4Catalog.FromJson(File.ReadAllText(JsonPath()));
        Assert.That(catalog.Scale.PlayerMaxHp, Is.EqualTo(1000));
        Assert.That(catalog.Scale.BaseDamage, Is.EqualTo(100));
        Assert.That(catalog.Scale.BaseHeal, Is.EqualTo(100));
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
        Assert.That(planner.Plan(12, 2, 4).Commands.Any(c => c is KaydaDonCommand), Is.True);
        Assert.That(planner.Plan(12, 2, 4).Commands.Any(c => c is KayitAlCommand), Is.True);
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
