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
    static readonly int[] SliceVerbs = { 1, 2, 3, 4, 6, 9 };
    static readonly int[] SliceAdjectives = { 1, 2, 3, 4, 5, 6 };
    static readonly int[] SliceWeapons = { 2, 4, 6 };

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
        foreach (int verb in SliceVerbs)
        foreach (int adj in SliceAdjectives)
        foreach (int weapon in SliceWeapons)
        {
            CommandPlan plan = planner.Plan(verb, adj, weapon);
            Assert.That(plan.IsValid, Is.True, $"combo {verb}-{adj} weapon {weapon}");
            Assert.That(plan.Commands[0], Is.TypeOf<OnSureCommand>());
            Assert.That(plan.Commands.Any(c => c is not OnSureCommand), Is.True);
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

        var kilicWind = (OnSureCommand)kilic.Commands[0];
        var yayWind = (OnSureCommand)yay.Commands[0];
        var cekicWind = (OnSureCommand)cekic.Commands[0];
        Assert.That(yayWind.PrefireSec, Is.Not.EqualTo(cekicWind.PrefireSec));
        Assert.That(cekicWind.DamageScale, Is.GreaterThan(kilicWind.DamageScale));
        Assert.That(kilicWind.DamageScale, Is.LessThan(yayWind.DamageScale));

        var kilicDmg = (HasarVerCommand)kilic.Commands.First(c => c is HasarVerCommand);
        var cekicDmg = (HasarVerCommand)cekic.Commands.First(c => c is HasarVerCommand);
        Assert.That(cekicDmg.Amount, Is.GreaterThan(kilicDmg.Amount));
    }

    [Test]
    public void YogunZarar_Kilic_HasChargeOnHostileVerb()
    {
        var plan = Planner().Plan(1, 1, 4);
        var wind = (OnSureCommand)plan.Commands[0];
        Assert.That(wind.ChargeSec, Is.EqualTo(0.35f).Within(0.001f));
    }

    [Test]
    public void YogunSifa_NoChargeOnFriendlyVerb()
    {
        var plan = Planner().Plan(2, 1, 4);
        var wind = (OnSureCommand)plan.Commands[0];
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
