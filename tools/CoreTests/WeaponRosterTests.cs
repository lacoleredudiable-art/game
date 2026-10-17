using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using NUnit.Framework;
using System.IO;

namespace CoreTests;

/// <summary>Dilim silahları (Kılıç + Yay) — veri ve pasifler.</summary>
[TestFixture]
public class WeaponRosterTests
{
    EquipmentCatalog _catalog = null!;
    string _elements = null!;

    [SetUp]
    public void Load()
    {
        _elements = SliceGrammarTestPaths.ReadSliceGrammarJson();
        _catalog = EquipmentCatalog.FromJson(_elements);
    }

    [Test]
    public void LoadsSliceWeapons_WithDocStats()
    {
        Assert.That(_catalog.Items.Count, Is.EqualTo(2));
        Expect(2, "Yay", "ranged", 0.8f, 0.7f, 0.6f, 14.1f, 0f, new[] { 1, 2, 3, 4, 6, 9 });
        Expect(4, "Kılıç", "medium", 1f, 0.9f, 1.1f, 6f, 0f, new[] { 1, 2, 3, 4, 6, 9 });
    }

    [Test]
    public void BasicAttack_MatchesSliceWeapons()
    {
        Assert.That(W(4).Profile.ArcDeg, Is.EqualTo(144f));
        Assert.That(W(4).Profile.BasicHits, Is.EqualTo(3));
        Assert.That(W(2).Profile.BasicIntervalSec, Is.EqualTo(0.55f).Within(1e-5f));
        Assert.That(W(4).Profile.BasicIntervalSec, Is.EqualTo(0.40f).Within(1e-5f));
        Assert.That(W(2).Profile.BasicKind, Is.EqualTo("volley"));
    }

    [Test]
    public void Passives_ForBowAndSword()
    {
        Assert.That(Eval(2, sinceMoved: 0.2f).CritChanceAdd, Is.EqualTo(0.20f).Within(0.0001f));
        Assert.That(Eval(2, sinceMoved: 1.5f).CritChanceAdd, Is.EqualTo(0f));

        WeaponPassiveMods arc = Eval(4, support: true);
        Assert.That(arc.ArcAllies, Is.True);
        Assert.That(arc.ArcDeg, Is.EqualTo(144f));
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

        EquipmentItem bow = W(2);
        state.ArmSwapBonus(bow.Profile.Id, bow.Profile.SwapBonusId, 0, 2f);
        state.TryConsumeBonus(bow.Profile.Id, 10, out WeaponSwapBonusSpec ignore, bow.Profile);
        Assert.That(ignore.IgnoreArmor, Is.True);
    }

    void Expect(int id, string name, string type, float damage, float cast, float poise, float reach, float armor, int[] verbs)
    {
        EquipmentItem item = W(id);
        Assert.That(item.Name, Is.EqualTo(name));
        Assert.That(item.Type, Is.EqualTo(type));
        Assert.That(item.DamageMult, Is.EqualTo(damage));
        Assert.That(item.CastTimeMult, Is.EqualTo(cast));
        Assert.That(item.PoiseMult, Is.EqualTo(poise));
        Assert.That(item.Profile.ReachM, Is.EqualTo(reach).Within(0.02f));
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
}
