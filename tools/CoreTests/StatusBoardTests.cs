using System.IO;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class StatusBoardTests
{
    StatusTuning Tuning() => new();

    // SkillMotorTests.LoadFull ile aynı desen — gerçek verbleri (Kor, zirh_eritme vb.)
    // görmek için embedded fallback yetmiyor (sadece 1-2 Buhar bileşiği var).
    static SkillMotor LoadFullMotor()
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

    [OneTimeSetUp]
    public void RebuildReactionTableFromFullJson()
    {
        // StatusReactionTable artık elle liste değil — motor.StatusInteractions'tan türetilir.
        StatusReactionTable.Rebuild(LoadFullMotor().StatusInteractions);
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
        Assert.That(board.AbsorbDamage(4f), Is.EqualTo(0f).Within(0.01f));
        Assert.That(board.ShieldRemaining, Is.EqualTo(6f).Within(0.01f));
        Assert.That(board.AbsorbDamage(20f), Is.EqualTo(14f).Within(0.01f));
        Assert.That(board.Has(StatusKind.Shield), Is.False);
    }

    [Test]
    public void Stasis_IsInvulnerable_AbsorbsAll()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Stasis, 700, 1f);
        Assert.That(board.IsInvulnerable, Is.True);
        Assert.That(board.AbsorbDamage(99f), Is.EqualTo(0f));
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
    public void Applicator_Buhar_AppliesBlindToTarget()
    {
        var motor = SkillMotor.CreateDefault();
        SkillResolution skill = motor.Resolve(new[] { 1, 2 }); // Buhar Perdesi
        var caster = new StatusBoard();
        var target = new StatusBoard();
        var result = StatusApplicator.ApplySkill(skill, caster, target, Tuning());
        Assert.That(result.Knockback, Is.False);
        Assert.That(target.HasBlind, Is.True);
        Assert.That(caster.HasBlind, Is.False);
    }

    [Test]
    public void Applicator_SelfFamily_TargetsCaster()
    {
        // Gömülü saldiri is damage/projectile → target. Use cleanse verb via synthetic resolution:
        var skill = SkillMotor.CreateDefault().Resolve(new[] { 5 }); // Aydınlık arındırma — purge family self
        var caster = new StatusBoard();
        var target = new StatusBoard();
        target.Apply(StatusKind.Root, 1000, 1f);
        StatusApplicator.ApplySkill(skill, caster, target, Tuning());
        // Aydınlık mechanics = cleanse → self board cleansed; target root remains if self-targeted
        Assert.That(StatusApplicator.IsSelfTargeted(skill), Is.True);
    }

    // --- 16 Eylül: durum etkileşim tablosu (docs/element-sistemi.json status_interaction_table) ---

    [Test]
    public void Reaction_BurnThenArmorBreak_AmplifiesArmorBreak()
    {
        // "Erimiş Zırh": burn zaten üstündeyken armor_break gelince ×1.5 + 2 sn.
        var board = new StatusBoard();
        var t = Tuning();
        board.Apply(StatusKind.Burn, 2000, t.BurnDamagePerSec);
        board.Apply(StatusKind.ArmorBreak, 3000, t.ArmorBreakDamageTakenMult);
        Assert.That(board.IncomingDamageMult, Is.EqualTo(t.ArmorBreakDamageTakenMult * 1.5f).Within(0.001f));
    }

    [Test]
    public void Reaction_OrderIndependent_ArmorBreakThenBurn_SameResult()
    {
        // Kural sırasız çalışmalı: önce armor_break, sonra burn gelse de aynı tepki.
        var board = new StatusBoard();
        var t = Tuning();
        board.Apply(StatusKind.ArmorBreak, 3000, t.ArmorBreakDamageTakenMult);
        board.Apply(StatusKind.Burn, 2000, t.BurnDamagePerSec);
        Assert.That(board.IncomingDamageMult, Is.EqualTo(t.ArmorBreakDamageTakenMult * 1.5f).Within(0.001f));
    }

    [Test]
    public void Reaction_HasteThenSlow_BothHalved()
    {
        // "Nötrleşme": ikisi de %50 azalır.
        var board = new StatusBoard();
        var t = Tuning();
        board.Apply(StatusKind.Haste, 2000, t.HasteSpeedMult);
        board.Apply(StatusKind.Slow, 1500, t.SlowSpeedMult);
        // MoveSpeedMult = slow.Magnitude * haste.Magnitude (BlocksMovement false burada)
        float expected = (t.SlowSpeedMult * 0.5f) * (t.HasteSpeedMult * 0.5f);
        Assert.That(board.MoveSpeedMult, Is.EqualTo(expected).Within(0.001f));
    }

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
    public void Reaction_GrievousWoundsPlusBurn_HealMultChangesTo0_7()
    {
        // "Kavurucu Yara": heal_reduction 0.5 → 0.7 (mutlak set, çarpan değil).
        var board = new StatusBoard();
        var t = Tuning();
        board.Apply(StatusKind.GrievousWounds, 2500, t.GrievousHealMult);
        Assert.That(board.HealEffectivenessMult, Is.EqualTo(0.5f).Within(0.001f));
        board.Apply(StatusKind.Burn, 2000, t.BurnDamagePerSec);
        Assert.That(board.HealEffectivenessMult, Is.EqualTo(0.7f).Within(0.001f));
    }

    [Test]
    public void Applicator_StunPlusKnockbackSameCast_ExtendsStunDuration()
    {
        // "Savrulma Sersemliği": aynı vuruşta stun+knockback → stun süresi +1 sn. Hiçbir gerçek
        // fiilde ikisi birden yok (bkz. element-sistemi.json) — sentetik SkillResolution ile
        // StatusApplicator'ın özel dalını tek başına test ediyoruz.
        var skill = new SkillResolution(
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
    public void Applicator_ReportsTriggeredReaction_ForRealVerb()
    {
        // "skilleri attığımda bir etkileşim göremiyorum" (16 Eylül) — Kor (1-3, Ateş
        // zirh_eritme) mechanics=[armor_break, burn] tek cast'te "Erimiş Zırh"ı tetikler.
        // ApplySkill artık bunu Result.TriggeredReactions ile bildiriyor (Game katmanı
        // ReactionReadout'a yazsın diye).
        var skill = LoadFullMotor().Resolve(new[] { 1, 3 }); // Kor
        Assert.That(skill.Mechanics, Does.Contain("armor_break"));
        Assert.That(skill.Mechanics, Does.Contain("burn"));

        var caster = new StatusBoard();
        var target = new StatusBoard();
        var result = StatusApplicator.ApplySkill(skill, caster, target, Tuning());

        Assert.That(result.TriggeredReactions.Count, Is.EqualTo(1));
        Assert.That(result.TriggeredReactions[0].Name, Is.EqualTo("Erimiş Zırh"));
    }

    [Test]
    public void Board_ReactionTriggeredEvent_FiresWithCorrectRule()
    {
        var board = new StatusBoard();
        var t = Tuning();
        StatusReactionRule? fired = null;
        board.ReactionTriggered += r => fired = r;

        board.Apply(StatusKind.Haste, 2000, t.HasteSpeedMult);
        Assert.That(fired, Is.Null); // henüz eşleşen ikinci status yok

        board.Apply(StatusKind.Slow, 1500, t.SlowSpeedMult);
        Assert.That(fired, Is.Not.Null);
        Assert.That(fired!.Value.Name, Is.EqualTo("Nötrleşme"));
    }

    [Test]
    public void StatusKind_ParsesPoison()
    {
        Assert.That(StatusKindUtil.TryParse("poison", out StatusKind kind), Is.True);
        Assert.That(kind, Is.EqualTo(StatusKind.Poison));
        Assert.That(StatusKindUtil.IsDebuff(StatusKind.Poison), Is.True);
    }
}
