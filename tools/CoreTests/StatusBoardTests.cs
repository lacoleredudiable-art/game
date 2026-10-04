using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using NUnit.Framework;
using System.IO;

namespace CoreTests;

[TestFixture]
public class StatusBoardTests
{
    StatusTuning Tuning() => new();

    static SkillMotor LoadV6Motor()
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
        return SkillMotor.FromJson(File.ReadAllText(path));
    }

    [Test]
    public void Stun_BlocksMoveAndCast()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Stun, 800, 1f);
        Assert.That(board.BlocksMovement, Is.True);
        Assert.That(board.BlocksCast, Is.True);
        Assert.That(board.BlocksBossAttack, Is.True);
    }

    [Test]
    public void Root_BlocksMove_AllowsCast()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Root, 1200, 1f);
        Assert.That(board.BlocksMovement, Is.True);
        Assert.That(board.BlocksCast, Is.False);
    }

    [Test]
    public void Tick_ExpiresStatus()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Slow, 100, 0.5f);
        Assert.That(board.Has(StatusKind.Slow), Is.True);
        board.Tick(150, Tuning());
        Assert.That(board.Has(StatusKind.Slow), Is.False);
    }

    [Test]
    public void Burn_TicksDamage()
    {
        var board = new StatusBoard();
        var t = Tuning();
        board.Apply(StatusKind.Burn, 1000, t.BurnDamagePerSec);
        float dmg = board.Tick(500, t);
        Assert.That(dmg, Is.EqualTo(t.BurnDamagePerSec * 0.5f).Within(0.01f));
    }

    [Test]
    public void Shield_AbsorbsThenBreaks()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Shield, 2000, 10f);
        board.ConsumeShield(4f);
        Assert.That(board.ShieldRemaining, Is.EqualTo(6f).Within(0.01f));
        board.ConsumeShield(6f);
        Assert.That(board.Has(StatusKind.Shield), Is.False);
    }

    [Test]
    public void Stasis_IsInvulnerable_AbsorbsAll()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Stasis, 700, 1f);
        Assert.That(board.IsInvulnerable, Is.True);
        Assert.That(board.Has(StatusKind.Stasis), Is.True);
    }

    [Test]
    public void Cleanse_RemovesHostileKeepsBuff()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Root, 1000, 1f);
        board.Apply(StatusKind.Burn, 1000, 1f);
        board.Apply(StatusKind.Haste, 1000, 1.3f);
        board.CleanseHostile();
        Assert.That(board.Has(StatusKind.Root), Is.False);
        Assert.That(board.Has(StatusKind.Burn), Is.False);
        Assert.That(board.Has(StatusKind.Haste), Is.True);
    }

    [Test]
    public void Applicator_V6CcSkill_AppliesRootToTargetNotCaster()
    {
        // 6-2: Kontrol fiili (engine action=cc, cc_kind=root) — hedef tahtasına gider.
        SkillResolution skill = LoadV6Motor().Resolve(new[] { 6, 2 });
        Assert.That(skill.Mechanics, Does.Contain("root"));
        var caster = new StatusBoard();
        var target = new StatusBoard();
        var result = StatusApplicator.ApplySkill(skill, caster, target, Tuning());
        Assert.That(result.Knockback, Is.False);
        Assert.That(target.Has(StatusKind.Root), Is.True);
        Assert.That(caster.Has(StatusKind.Root), Is.False);
    }

    [Test]
    public void Applicator_V6CleanseSkill_IsSelfTargeted_AndCleansesCaster()
    {
        // 9-1: Arındırma fiili (hitbox self_or_ally, action=cleanse) — caster'ı temizler.
        SkillResolution skill = LoadV6Motor().Resolve(new[] { 9, 1 });
        Assert.That(StatusApplicator.IsSelfTargeted(skill), Is.True);
        var caster = new StatusBoard();
        var target = new StatusBoard();
        caster.Apply(StatusKind.Root, 1000, 1f);
        target.Apply(StatusKind.Root, 1000, 1f);
        var result = StatusApplicator.ApplySkill(skill, caster, target, Tuning());
        Assert.That(result.Cleansed, Is.True);
        Assert.That(caster.Has(StatusKind.Root), Is.False);
        Assert.That(target.Has(StatusKind.Root), Is.True);
    }

    // --- StatusBoard'a gömülü sabit kombolar (tablo değil, StatusTuning): burn+poison, shield+burn ---

    [Test]
    public void Reaction_BurnPlusPoison_ExtraTickDamage()
    {
        // "Zehirli Ateş": burn+poison aynı anda → +2 hasar/sn (BurnPoisonComboBonusPerSec).
        var board = new StatusBoard();
        var t = Tuning();
        board.Apply(StatusKind.Burn, 3000, t.BurnDamagePerSec);
        board.Apply(StatusKind.Poison, 3000, t.PoisonDamagePerSec);
        float dmg = board.Tick(1000, t);
        float expected = t.BurnDamagePerSec + t.PoisonDamagePerSec + t.BurnPoisonComboBonusPerSec;
        Assert.That(dmg, Is.EqualTo(expected).Within(0.01f));
    }

    [Test]
    public void Reaction_ShieldPlusBurn_ShieldAlsoDrains()
    {
        // "Yanan Kalkan": burn tick'i kalkanı da (shield_amount × 0.5) aşındırır.
        var board = new StatusBoard();
        var t = Tuning();
        board.Apply(StatusKind.Shield, 5000, 20f);
        board.Apply(StatusKind.Burn, 3000, t.BurnDamagePerSec);
        board.Tick(1000, t);
        float burnTick = t.BurnDamagePerSec; // 1 sn
        float expectedShield = 20f - burnTick * t.ShieldBurnDrainRatio;
        Assert.That(board.ShieldRemaining, Is.EqualTo(expectedShield).Within(0.01f));
    }

    [Test]
    public void Applicator_StunPlusKnockbackSameCast_ExtendsStunDuration()
    {
        // "Savrulma Sersemliği": aynı vuruşta stun+knockback → stun süresi +1 sn. Hiçbir gerçek
        // fiilde ikisi birden yok (bkz. element-sistemi.json) — sentetik SkillResolution ile
        // StatusApplicator'ın özel dalını tek başına test ediyoruz.
        var skill = SkillResolution.Build(
            elementId: "test", elementName: "Test", displayName: "Test", skillId: "t", skillJob: "job",
            verbId: "test_verb", verbName: "Test", verbFamily: "control", action: "stun_knockback",
            baseDamage: 20f, basePoise: 30f, hitbox: "single_target", castMobility: "free_move",
            mechanics: new[] { "stun", "knockback" },
            adjectiveId: "yogunlastirma", adjectiveName: "Yoğunlaştırma", silhouetteAxis: "focus",
            damageMult: 0.85f, hitboxScaleMult: 1f, poiseDamageMult: 1f,
            length: 2, lengthRole: "Temel", lengthCastMult: 1f, lengthMobility: "free_move",
            flavorElement: string.Empty);

        var t = Tuning();
        var caster = new StatusBoard();
        var target = new StatusBoard();
        StatusApplicator.ApplySkill(skill, caster, target, t);

        Assert.That(target.Has(StatusKind.Stun), Is.True);
        // StunMs + StunKnockbackDurationAddMs bittiğinde hâlâ stun'lı olmalı (uzamış süre kanıtı).
        target.Tick(t.StunMs + t.StunKnockbackDurationAddMs - 100, t);
        Assert.That(target.Has(StatusKind.Stun), Is.True);
        target.Tick(200, t);
        Assert.That(target.Has(StatusKind.Stun), Is.False);
    }

    [Test]
    public void TryGet_ReturnsRemainingMagnitudeAndTotal()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Burn, 2000, 3.5f);
        Assert.That(board.TryGet(StatusKind.Burn, out double rem, out float mag, out double total), Is.True);
        Assert.That(rem, Is.EqualTo(2000).Within(0.01));
        Assert.That(mag, Is.EqualTo(3.5f));
        Assert.That(total, Is.EqualTo(2000).Within(0.01));
        Assert.That(board.TryGet(StatusKind.Slow, out _, out _, out _), Is.False);
    }

    [Test]
    public void StatusKind_ParsesPoison()
    {
        Assert.That(StatusKindUtil.TryParse("poison", out StatusKind kind), Is.True);
        Assert.That(kind, Is.EqualTo(StatusKind.Poison));
        Assert.That(StatusKindUtil.IsDebuff(StatusKind.Poison), Is.True);
    }

    [Test]
    public void Stealth_AbsorbsAllDamage_AndParses()
    {
        Assert.That(StatusKindUtil.TryParse("stealth", out StatusKind kind), Is.True);
        Assert.That(kind, Is.EqualTo(StatusKind.Stealth));
        Assert.That(StatusKindUtil.IsBuff(StatusKind.Stealth), Is.True);

        var board = new StatusBoard();
        board.Apply(StatusKind.Stealth, 4000, 1f);
        Assert.That(board.IsStealthed, Is.True);
    }

    [Test]
    public void Shield_UsesJsonSizedAbsorbDefaults()
    {
        var t = Tuning();
        Assert.That(t.ShieldMs, Is.EqualTo(5000));
        Assert.That(t.ShieldAbsorb, Is.EqualTo(50f));

        var board = new StatusBoard();
        board.Apply(StatusKind.Shield, t.ShieldMs, t.ShieldAbsorb);
        board.ConsumeShield(30f);
        Assert.That(board.ShieldRemaining, Is.EqualTo(20f).Within(0.01f));
        board.ConsumeShield(20f);
        Assert.That(board.Has(StatusKind.Shield), Is.False);
    }

}
