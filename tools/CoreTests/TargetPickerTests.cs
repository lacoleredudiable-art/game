using System;
using System.Collections.Generic;
using System.IO;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

/// <summary>Boss hedef seçimi (oyuncu / dost / dikkat çeken yem) ve dost kalkış kuralları.</summary>
[TestFixture]
public sealed class TargetPickerTests
{
    const int Player = 1, Ally = 2, Decoy = 3, Decoy2 = 4;

    static HostileCandidate P(bool alive = true, bool stealthed = false) =>
        new(Player, TargetKind.Player, 0f, 0f, alive, stealthed, false);

    static HostileCandidate A(bool alive = true, bool stealthed = false) =>
        new(Ally, TargetKind.Ally, -3f, 0f, alive, stealthed, false);

    static HostileCandidate D(int id = Decoy, bool alive = true, bool taunting = true) =>
        new(id, TargetKind.Decoy, 1f, 1f, alive, false, taunting);

    static string KaradulJson()
    {
        string root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        string path = Path.Combine(root, "unity", "Assets", "Resources", "Bosses", "karadul.json");
        Assert.That(File.Exists(path), Is.True, path);
        return File.ReadAllText(path);
    }

    [Test]
    public void TauntingDecoy_AlwaysWins_RegardlessOfRoll()
    {
        var c = new List<HostileCandidate> { P(), A(), D() };
        foreach (double roll in new[] { 0.0, 0.29, 0.5, 0.99 })
            Assert.That(TargetPicker.Pick(c, new TargetingConfig(), roll), Is.EqualTo(Decoy));
    }

    [Test]
    public void NewestTauntingDecoy_Wins()
    {
        var c = new List<HostileCandidate> { P(), D(Decoy), D(Decoy2) };
        Assert.That(TargetPicker.Pick(c, new TargetingConfig(), 0.5), Is.EqualTo(Decoy2));
    }

    [Test]
    public void DeadOrNonTauntingDecoy_IsIgnored()
    {
        var c = new List<HostileCandidate> { P(), A(), D(alive: false), D(Decoy2, taunting: false) };
        int id = TargetPicker.Pick(c, new TargetingConfig(), 0.9);
        Assert.That(id, Is.EqualTo(Player).Or.EqualTo(Ally));
    }

    [Test]
    public void DecoyPriorityOff_DecoyIsNotPicked()
    {
        var cfg = new TargetingConfig { DecoyPriority = false };
        var c = new List<HostileCandidate> { P(), A(), D() };
        for (int i = 0; i < 100; i++)
            Assert.That(TargetPicker.Pick(c, cfg, i / 100.0), Is.Not.EqualTo(Decoy));
    }

    [Test]
    public void StealthedOrDown_AreSkipped()
    {
        var cfg = new TargetingConfig();
        Assert.That(TargetPicker.Pick(new List<HostileCandidate> { P(stealthed: true), A() }, cfg, 0.99), Is.EqualTo(Ally));
        Assert.That(TargetPicker.Pick(new List<HostileCandidate> { P(), A(alive: false) }, cfg, 0.01), Is.EqualTo(Player));
    }

    [Test]
    public void NoValidCandidate_ReturnsMinusOne()
    {
        var cfg = new TargetingConfig();
        Assert.That(TargetPicker.Pick(new List<HostileCandidate>(), cfg, 0.5), Is.EqualTo(-1));
        Assert.That(TargetPicker.Pick(new List<HostileCandidate> { P(stealthed: true), A(alive: false) }, cfg, 0.5), Is.EqualTo(-1));
    }

    [Test]
    public void AllyWeight_IsAboutThirtyPercent_OverSeededRolls()
    {
        var rng = new Random(1234);
        var c = new List<HostileCandidate> { P(), A() };
        var cfg = new TargetingConfig();
        int ally = 0;
        const int n = 10000;
        for (int i = 0; i < n; i++)
            if (TargetPicker.Pick(c, cfg, rng.NextDouble()) == Ally)
                ally++;
        Assert.That(ally / (double)n, Is.EqualTo(0.3).Within(0.02));
    }

    [Test]
    public void SameSeed_SamePicks()
    {
        var c = new List<HostileCandidate> { P(), A() };
        var a = new Random(7);
        var b = new Random(7);
        for (int i = 0; i < 200; i++)
            Assert.That(TargetPicker.Pick(c, new TargetingConfig(), a.NextDouble()),
                Is.EqualTo(TargetPicker.Pick(c, new TargetingConfig(), b.NextDouble())));
    }

    [Test]
    public void ShouldRetarget_OnDeathStealthOrNewDecoy()
    {
        var cfg = new TargetingConfig();
        Assert.That(TargetPicker.ShouldRetarget(new List<HostileCandidate> { P(), A() }, Player, cfg), Is.False);
        Assert.That(TargetPicker.ShouldRetarget(new List<HostileCandidate> { P(), A(alive: false) }, Ally, cfg), Is.True);
        Assert.That(TargetPicker.ShouldRetarget(new List<HostileCandidate> { P(stealthed: true), A() }, Player, cfg), Is.True);
        Assert.That(TargetPicker.ShouldRetarget(new List<HostileCandidate> { P(), A(), D() }, Player, cfg), Is.True);
        Assert.That(TargetPicker.ShouldRetarget(new List<HostileCandidate> { P(), A(), D() }, Decoy, cfg), Is.False);
        Assert.That(TargetPicker.ShouldRetarget(new List<HostileCandidate> { P(), A() }, Decoy, cfg), Is.True, "decoy expired");
    }

    [Test]
    public void KaradulJson_TargetingBlock_IsTheApprovedDefaults()
    {
        TargetingConfig cfg = TargetingConfig.FromJson(MiniJson.Parse(KaradulJson()));
        Assert.That(cfg.AllyWeight, Is.EqualTo(0.3f).Within(1e-6));
        Assert.That(cfg.AllyDamageMult, Is.EqualTo(0.5f).Within(1e-6));
        Assert.That(cfg.AllyReviveSec, Is.EqualTo(8f).Within(1e-6));
        Assert.That(cfg.AllyReviveRatio, Is.EqualTo(0.5f).Within(1e-6));
        Assert.That(cfg.DecoyPriority, Is.True);
    }

    [Test]
    public void MissingTargetingBlock_KeepsDefaults()
    {
        TargetingConfig cfg = TargetingConfig.FromJson(MiniJson.Parse("{\"name\":\"x\"}"));
        Assert.That(cfg.AllyWeight, Is.EqualTo(new TargetingConfig().AllyWeight));
        Assert.That(cfg.DecoyPriority, Is.True);
    }

    [Test]
    public void AllyLife_HalfDamage_ReviveAfterEightSecondsAtHalfHp()
    {
        var cfg = new TargetingConfig();
        Assert.That(AllyLifeRules.AllyRawDamage(22f, cfg), Is.EqualTo(11f).Within(1e-5));
        Assert.That(AllyLifeRules.AllyRawDamage(-1f, cfg), Is.EqualTo(0f));
        Assert.That(AllyLifeRules.ReviveDue(-1, 100000, cfg), Is.False, "not down");
        Assert.That(AllyLifeRules.ReviveDue(1000, 8999, cfg), Is.False);
        Assert.That(AllyLifeRules.ReviveDue(1000, 9000, cfg), Is.True);
        Assert.That(AllyLifeRules.ReviveHp(22, cfg), Is.EqualTo(11));
        Assert.That(AllyLifeRules.ReviveHp(1, cfg), Is.EqualTo(1));
    }
}
