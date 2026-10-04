using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using NUnit.Framework;
using System.IO;

namespace CoreTests;

/// <summary>docs/element-sistemi.json weapon_skill_interaction.swap + state_machine can_swap.</summary>
[TestFixture]
public class WeaponSwapTests
{
    static string Json()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        }
        Assert.That(File.Exists(path), Is.True, $"element-sistemi.json bulunamadı: {path}");
        return File.ReadAllText(path);
    }

    static EquipmentItem Weapon(string id) => new(id, id, EquipmentSlot.Weapon, string.Empty);

    static WeaponSwapState Loaded(out WeaponSwapRules rules)
    {
        rules = WeaponSwapRules.FromJson(Json());
        var state = new WeaponSwapState(rules);
        state.SetLoadout(Weapon("a"), Weapon("b"));
        return state;
    }

    [Test]
    public void Rules_ReadFromJson()
    {
        var rules = WeaponSwapRules.FromJson(Json());
        Assert.That(rules.Enabled, Is.True);
        Assert.That(rules.WeaponsCarried, Is.EqualTo(2));
        Assert.That(rules.CooldownSec, Is.EqualTo(1.2f).Within(1e-4));
        Assert.That(rules.AnimationSec, Is.EqualTo(0.25f).Within(1e-4));
        Assert.That(rules.CancelsCombo, Is.True);
        Assert.That(rules.DodgeCancelsSwap, Is.True);
    }

    [Test]
    public void Swap_CompletesAfterAnimation_ThenCooldownBlocks()
    {
        var s = Loaded(out var rules);
        Assert.That(s.TryBegin(0, true), Is.EqualTo(WeaponSwapResult.Started));
        Assert.That(s.Tick(rules.AnimationSec * 1000.0 - 1), Is.False);
        Assert.That(s.Active!.Id, Is.EqualTo("a"));
        Assert.That(s.Tick(rules.AnimationSec * 1000.0), Is.True);
        Assert.That(s.Active!.Id, Is.EqualTo("b"));
        Assert.That(s.Reserve!.Id, Is.EqualTo("a"));

        Assert.That(s.TryBegin(rules.CooldownSec * 1000.0 - 1, true), Is.EqualTo(WeaponSwapResult.OnCooldown));
        Assert.That(s.TryBegin(rules.CooldownSec * 1000.0, true), Is.EqualTo(WeaponSwapResult.Started));
    }

    [Test]
    public void Swap_BlockedByState_AndMissingSecondWeapon()
    {
        var s = Loaded(out _);
        Assert.That(s.TryBegin(0, false), Is.EqualTo(WeaponSwapResult.StateBlocked));

        var single = new WeaponSwapState(WeaponSwapRules.FromJson(Json()));
        single.SetLoadout(Weapon("a"), null);
        Assert.That(single.TryBegin(0, true), Is.EqualTo(WeaponSwapResult.NoSecondWeapon));
    }

    [Test]
    public void Dodge_CancelsSwap_WeaponUnchanged_CooldownKept()
    {
        var s = Loaded(out var rules);
        s.TryBegin(0, true);
        Assert.That(s.CancelByDodge(), Is.True);
        Assert.That(s.Tick(rules.AnimationSec * 1000.0), Is.False);
        Assert.That(s.Active!.Id, Is.EqualTo("a"));
        Assert.That(s.TryBegin(100, true), Is.EqualTo(WeaponSwapResult.OnCooldown));
    }

    [Test]
    public void StateMachine_CanSwap_FollowsJson()
    {
        var sm = new PlayerStateMachine(SkillMotor.FromJson(Json()).PlayerStates);
        Assert.That(sm.TryEnter("idle") && sm.AllowsSwap, Is.True);
        Assert.That(sm.TryEnter("recovering") && sm.AllowsSwap, Is.True);
        Assert.That(sm.TryEnter("drawing") && !sm.AllowsSwap, Is.True);
        Assert.That(sm.TryEnter("casting") && !sm.AllowsSwap, Is.True);
        Assert.That(sm.TryEnter("dodging") && !sm.AllowsSwap, Is.True);
    }
}
