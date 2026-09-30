using System.IO;
using System.Linq;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

/// <summary>Pasiflerin yarım bağlanan parçaları: yay, sayaç, sersem, kutsal, blok, süre, poise.</summary>
[TestFixture]
public class WeaponPassiveFixTests
{
    EquipmentCatalog _catalog;
    string _elements;

    [SetUp]
    public void Load()
    {
        _elements = File.ReadAllText(ElementPath());
        _catalog = EquipmentCatalog.FromJson(_elements);
    }

    [Test]
    public void SwordArc_HitsInside144_AndAlliesOnlyWhenAsked()
    {
        Assert.That(MeleeArc.StrikeConnects(capsuleHit: false, edgeInReach: true, 70f, 144f), Is.True);
        Assert.That(MeleeArc.StrikeConnects(capsuleHit: true, edgeInReach: false, 80f, 144f), Is.False);
        Assert.That(MeleeArc.StrikeConnects(capsuleHit: false, edgeInReach: true, 80f, 0f), Is.True);
        Assert.That(MeleeArc.Hits(true, 20f, 144f, designated: true, ally: false, arcAllies: false), Is.True);
        Assert.That(MeleeArc.Hits(true, 20f, 144f, designated: false, ally: true, arcAllies: true), Is.True);
        Assert.That(MeleeArc.Hits(true, 20f, 144f, designated: false, ally: true, arcAllies: false), Is.False);

        WeaponPassiveMods slash = Query(4, 1, enabled: true);
        WeaponPassiveMods ward = Query(4, 9, enabled: true);
        WeaponPassiveMods off = Query(4, 1, enabled: false);
        Assert.That(slash.ArcDeg, Is.EqualTo(144f));
        Assert.That(slash.ArcAllies, Is.False);
        Assert.That(ward.ArcAllies, Is.True);
        Assert.That(ward.ArcDeg, Is.EqualTo(144f));
        Assert.That(off.ArcDeg, Is.EqualTo(0f));
    }

    [Test]
    public void Book_ThirdCastIsBonus_AndStaleChainResets()
    {
        var state = new WeaponPassiveState();
        float gap = W(3).Profile.Passive.ChainGapSec;
        float Mult(double now) => Query(3, 7, enabled: true, chain: state.EffectiveChain(now, gap)).DamageMult;

        Assert.That(state.NoteSkill(0, gap), Is.EqualTo(1));
        Assert.That(Mult(0), Is.EqualTo(1f));
        Assert.That(state.NoteSkill(500, gap), Is.EqualTo(2));
        Assert.That(Mult(500), Is.EqualTo(1f));
        Assert.That(state.NoteSkill(1000, gap), Is.EqualTo(3));
        Assert.That(Mult(1000), Is.EqualTo(1.3f));
        Assert.That(state.ChainCount, Is.EqualTo(3));
        Assert.That(state.EffectiveChain(1000 + gap * 1000.0 + 1, gap), Is.EqualTo(0));
        Assert.That(state.ChainCount, Is.EqualTo(0));
        Assert.That(Mult(4000), Is.EqualTo(1f));
    }

    [Test]
    public void Hammer_StunSecondsComeFromJson_AndMissDoesNotStartCooldown()
    {
        var grammar = new MechanicGrammar(MechanicRules.FromJson(_elements));
        MechanicPlan plan = grammar.Compose(1, 0, 6);
        MechanicEffect stun = plan.Effects.First(e => e.Has("sersem"));
        double fromJson = grammar.Rules.WeaponPassiveNum(6, "stun_sec", -1);
        Assert.That(stun.DurationSec, Is.EqualTo(fromJson).Within(0.001));
        Assert.That(stun.DurationSec, Is.EqualTo(W(6).Profile.Passive.StunSec).Within(0.001));
        Assert.That(Query(6, 1, enabled: true, harmful: true).StunSec, Is.EqualTo((float)fromJson));

        var state = new WeaponPassiveState();
        Assert.That(state.HammerReady(0), Is.True);
        bool commit = WeaponPassiveRules.CommitStunOnLand(
            state.HammerReady(0), alreadyHadStun: true, applied: false);
        Assert.That(commit, Is.False);
        Assert.That(state.HammerReady(100), Is.True);
        state.CommitHammer(100, W(6).Profile.Passive.IcdSec);
        Assert.That(state.HammerReady(4099), Is.False);
        Assert.That(state.HammerReady(4100), Is.True);
    }

    [Test]
    public void Talisman_ScalesHealShieldAndBuff_WithoutVerbGate()
    {
        WeaponCombatProfile charm = W(9).Profile;
        Assert.That(WeaponPassiveRules.HolyMagnitude(charm), Is.EqualTo(1.2f));
        Assert.That(Query(9, 1, enabled: false).HealMult, Is.EqualTo(1.2f));
        Assert.That(Query(9, 4, enabled: false, support: true).HealMult, Is.EqualTo(1.2f));
        Assert.That(WeaponPassiveRules.ScaleFriendlyMagnitude(50f, 1.2f), Is.EqualTo(60f).Within(0.001f));
        Assert.That(WeaponPassiveRules.HolyMagnitude(W(4).Profile), Is.EqualTo(1f));

        var tuning = new StatusTuning();
        var board = new StatusBoard();
        StatusApplicator.ApplySkill(ShieldSkill(), board, null, tuning, null, 1.2f);
        Assert.That(board.ShieldRemaining, Is.EqualTo(tuning.ShieldAbsorb * 1.2f).Within(0.01f));
    }

    [Test]
    public void Shield_CounterWindow_IsSpentOnTheFirstHit()
    {
        var state = new WeaponPassiveState();
        state.NoteBlock(0, W(10).Profile.Passive.WindowSec);
        Assert.That(state.CounterArmed(500), Is.True);
        Assert.That(Query(10, 4, enabled: true, blocked: true).DamageMult, Is.EqualTo(1.2f));
        Assert.That(state.TryConsumeBlock(500), Is.True);
        Assert.That(state.CounterArmed(600), Is.False);
        Assert.That(state.TryConsumeBlock(700), Is.False);
        Assert.That(Query(10, 4, enabled: true, blocked: false).DamageMult, Is.EqualTo(1f));
    }

    [Test]
    public void Staff_LongerDuration_AddsTicksAtTheSameShare()
    {
        float tick = 1f;
        float baseDuration = 3f;
        float extended = baseDuration * W(8).Profile.Passive.DurationMult;
        float share = SustainedField.PerTickShare(baseDuration, tick);
        int baseTicks = SustainedField.TickCount(baseDuration, tick);
        int extraTicks = SustainedField.TickCount(extended, tick);
        Assert.That(baseTicks * share, Is.EqualTo(1f).Within(0.001f));
        Assert.That(extraTicks, Is.GreaterThan(baseTicks));
        Assert.That(extraTicks * share, Is.GreaterThan(1.25f));
    }

    [Test]
    public void SwapBonuses_FeedArcAndPoise()
    {
        var state = new WeaponPassiveState();
        EquipmentItem sword = W(4);
        state.ArmSwapBonus(sword.Profile.Id, sword.Profile.SwapBonusId, 0, sword.Profile.SwapBonus.WindowSec);
        Assert.That(state.PeekBonus(sword.Profile.Id, 100, out WeaponSwapBonusSpec arc, sword.Profile), Is.True);
        float liveArc = arc.ArcDeg > 0f ? arc.ArcDeg : sword.Profile.Passive.ArcDeg;
        Assert.That(liveArc, Is.EqualTo(180f));
        Assert.That(MeleeArc.InFront(89f, liveArc), Is.True);
        Assert.That(MeleeArc.InFront(89f, 144f), Is.False);

        EquipmentItem hammer = W(6);
        state.ArmSwapBonus(hammer.Profile.Id, hammer.Profile.SwapBonusId, 0, 2f);
        Assert.That(state.PeekBonus(hammer.Profile.Id, 100, out WeaponSwapBonusSpec poise, hammer.Profile), Is.True);
        Assert.That(
            WeaponPassiveRules.OutgoingPoise(10f, 1f, hammer.PoiseMult, 1f),
            Is.EqualTo(18f).Within(0.001f));
        Assert.That(
            WeaponPassiveRules.OutgoingPoise(10f, 1f, hammer.PoiseMult, poise.PoiseMult),
            Is.EqualTo(36f).Within(0.001f));
    }

    [Test]
    public void Fist_BackstabLabel_IsNotASecondMultiplier()
    {
        var grammar = new MechanicGrammar(MechanicRules.FromJson(_elements));
        MechanicPlan plan = grammar.Compose(1, 1, 1);
        Assert.That(plan.Body.Traits.Contains("arkadan_x1.5"), Is.False);
        Assert.That(Query(1, 1, enabled: true, harmful: true, behind: 0f).DamageMult, Is.EqualTo(1.5f));
        Assert.That(Query(1, 1, enabled: true, harmful: true, behind: 80f).DamageMult, Is.EqualTo(1f));
    }

    WeaponPassiveMods Query(
        int weaponId,
        int verb,
        bool enabled = true,
        bool harmful = false,
        bool support = false,
        bool blocked = false,
        int chain = 0,
        float behind = 180f)
    {
        var query = new WeaponPassiveQuery(
            verb, enabled, harmful, false, behind, 99f, blocked, chain, 0f, support);
        return WeaponPassiveRules.Evaluate(W(weaponId).Profile, query);
    }

    EquipmentItem W(int id) => _catalog.FindWeapon(id);

    static SkillResolution ShieldSkill() =>
        new(
            "1", "Ates", "Kalkan", "4-1", "",
            "4", "Kalkan", "guard", "shield",
            0f, 0f, "self", "", new[] { "shield" },
            "1", "Sert", "",
            1f, 1f, 1f,
            2, "pair", 1f, "",
            "");

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
}
