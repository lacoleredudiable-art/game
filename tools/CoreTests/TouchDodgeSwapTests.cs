using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using NUnit.Framework;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CoreTests;

/// <summary>
/// Denetim B: dokunmatik kaçış/silah düğmesi (O1, K3), tek PERFECT penceresi (O2),
/// kanallı skillde swap kilidi (O10), kaçış kapısı (S2, S3).
/// </summary>
[TestFixture]
public class TouchDodgeSwapTests
{
    static string Root() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string Game(string rel) =>
        File.ReadAllText(Path.Combine(Root(), "unity", "Assets", "Scripts", rel));

    static string ElementJson() => SliceGrammarTestPaths.ReadSliceGrammarJson();

    // --- O1 / K3: düğme kuralları ---

    [Test]
    public void Swap_WithoutHoldCommand_FiresOnPress()
    {
        Assert.That(TouchButtonGesture.SwapFiresOnPress(0f), Is.True);
        Assert.That(TouchButtonGesture.SwapFiresOnPress(0.4f), Is.False);
    }

    [TestCase(0.05, true)]
    [TestCase(0.20, true)]
    [TestCase(0.39, true)]
    [TestCase(0.41, false)]
    [TestCase(2.00, false)]
    public void Kure_ShortPressSwaps_LongPressIsOrbCommand(double heldSec, bool swaps)
    {
        const float hold = 0.4f;
        Assert.That(TouchButtonGesture.SwapOnRelease(heldSec, hold, cancelled: false), Is.EqualTo(swaps));
        Assert.That(TouchButtonGesture.HoldCommandDue(heldSec, hold), Is.EqualTo(!swaps));
    }

    [Test]
    public void NoPressLength_IsDropped()
    {
        // Her basış süresi ya swap ya küre komutu üretir; 0,18 sn üstü sessizce düşmez.
        for (double t = 0.0; t < 3.0; t += 0.01)
        {
            bool any = TouchButtonGesture.SwapOnRelease(t, 0.4f, false) || TouchButtonGesture.HoldCommandDue(t, 0.4f);
            Assert.That(any, Is.True, $"basış {t:0.00} sn düştü");
        }
    }

    [Test]
    public void Center_FiresOnRelease_WithoutTimeLimit_OnlyMoveThreshold()
    {
        Assert.That(TouchButtonGesture.CenterFiresOnRelease(0f, 12, false), Is.True);
        Assert.That(TouchButtonGesture.CenterFiresOnRelease(12f, 12, false), Is.True);
        Assert.That(TouchButtonGesture.CenterFiresOnRelease(13f, 12, false), Is.False);
        Assert.That(TouchButtonGesture.CenterFiresOnRelease(0f, 12, true), Is.False);
    }

    // --- S2: dodge değerleri JSON player_stats ile aynı ---

    [Test]
    public void DodgeTuning_MatchesJsonPlayerStats()
    {
        string json = ElementJson();
        float dist = float.Parse(Regex.Match(json, "\"dodge_distance_m\":\\s*([0-9.]+)").Groups[1].Value,
            System.Globalization.CultureInfo.InvariantCulture);
        var t = new DodgeTuning();
        Assert.That(t.DistanceM, Is.EqualTo(dist).Within(0.001f));
        // IframeMs artık JSON'a bağlı değil: 15-dodge-tasarimi §3.4 test değeri (0,5 s); element-sistemi.json donduruldu.
        Assert.That(t.IframeMs, Is.EqualTo(500));
        Assert.That(Game("Core/Tuning/DodgeTuning.cs"), Does.Not.Contain("public int CooldownMs"));
        Assert.That(Game("Core/Tuning/DodgeTuning.cs"), Does.Not.Contain("public int TapMaxMs"));
    }

    // --- S3: kaçış kapısı ---

    static PlayerStateMachine LoadStates()
    {
        var motor = SkillMotor.FromJson(ElementJson());
        return new PlayerStateMachine(motor.PlayerStates);
    }

    [TestCase("idle", true)]
    [TestCase("drawing", true)]
    [TestCase("dodging", true)]
    [TestCase("casting", true)]
    [TestCase("stunned", false)]
    [TestCase("dead", false)]
    public void DodgeGate_FollowsStateMachine(string state, bool allowed)
    {
        var sm = LoadStates();
        Assert.That(sm.TryEnter(state), Is.True, state);
        Assert.That(sm.AllowsDodgeGate, Is.EqualTo(allowed), state);
    }

    [Test]
    public void TriggerDodge_UsesStateGate()
    {
        string src = Game("Game/Casting/Input/DodgeTrigger.cs");
        Assert.That(src, Does.Contain("AllowsDodgeGate"));
        Assert.That(src, Does.Not.Contain("IsOnCooldown"));
        Assert.That(src, Does.Not.Contain("WeaponHudRequested"));
        Assert.That(src, Does.Not.Contain("TapMaxMs"));
    }

    // --- O10: kanallı skillde swap kilidi ---

    [Test]
    public void Swap_LockedWhileHolding_UnlessTaggedWindow()
    {
        Assert.That(WeaponSwapCancel.MayBegin(true, false, holding: true, false, false, inWindow: false, tagged: false), Is.False);
        Assert.That(WeaponSwapCancel.MayBegin(true, false, holding: true, false, false, inWindow: true, tagged: false), Is.False);
        Assert.That(WeaponSwapCancel.MayBegin(true, false, holding: false, false, false, inWindow: false, tagged: false), Is.True);
    }

    [Test]
    public void SustainedCastLock_CoversChannel_ClearedByDodge()
    {
        var lk = new SustainedCastLock();
        lk.Begin(1000, 0.0);
        Assert.That(lk.Active(1000), Is.False, "süresiz skill kilitlemez");
        lk.Begin(1000, 1.5);
        Assert.That(lk.Active(2499), Is.True);
        Assert.That(lk.Active(2500), Is.False);
        lk.Begin(3000, 1.0);
        lk.Clear();
        Assert.That(lk.Active(3100), Is.False);
    }

    [Test]
    public void Director_WiresSustainedLock()
    {
        Assert.That(Game("Game/Skills/RuleEngineV4SentenceHost.WeaponSwap.cs"), Does.Contain("bool holding = SustainedSkillActive(worldMs);"));
        Assert.That(Game("Game/Skills/Hosts/MdCastPort.cs"), Does.Contain("NoteSustainedCast(skill)"));
        Assert.That(Game("Game/Skills/RuleEngineV4SentenceHost.zMotionStubs.cs"), Does.Not.Contain("MotionTemplateDriver"));
        Assert.That(Game("Game/Skills/Weapons/OrbController.cs"), Does.Not.Contain("OnWeaponHudButton"));
        Assert.That(Game("Game/Skills/Weapons/OrbController.cs"), Does.Contain("SwapButtonHoldSec"));
    }
}
