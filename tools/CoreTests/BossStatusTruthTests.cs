using System.IO;
using System.Linq;
using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// Boss hasarı, Dev HP, kör/sessiz/silahsız/zayıf, poise, gizlilik, koruyucu şifa, yansıma/paylaşım.
/// </summary>
[TestFixture]
public class BossStatusTruthTests
{
    SkillMotor _motor = null!;
    MechanicGrammar _grammar = null!;

    [OneTimeSetUp]
    public void Load()
    {
        string path = ElementPath();
        string json = File.ReadAllText(path);
        _motor = SkillMotor.FromJson(json);
        _grammar = new MechanicGrammar(MechanicRules.FromJson(json));
    }

    [Test]
    public void BossTuning_UsesKaradulDamage_NotZero()
    {
        var boss = new BossTuning();
        Assert.That(boss.Damage, Is.EqualTo(22));
        Assert.That(boss.FireConeDamage, Is.EqualTo(18));
        var attack = new BossAttack(boss);
        Assert.That(attack.Damage, Is.EqualTo(22));
        attack.ApplyFireCone();
        Assert.That(attack.Damage, Is.EqualTo(18));
    }

    [Test]
    public void DevHp_OnIsOneBillion_OffIsNormal()
    {
        Assert.That(DevPlayerHp.Pool, Is.EqualTo(1_000_000_000));
        Assert.That(DevPlayerHp.Resolve(true, 400_000), Is.EqualTo(1_000_000_000));
        Assert.That(DevPlayerHp.Resolve(false, 400_000), Is.EqualTo(400_000));
        Assert.That(DevPlayerHp.Resolve(false, 0), Is.EqualTo(1));
    }

    [Test]
    public void Blind_MissesWhenRollIsUnderAccuracy_HitsOtherwise()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Blind, 1400, 0.3f);
        Assert.That(board.BlindMissChance, Is.EqualTo(0.3f).Within(0.001f));
        Assert.That(BossStatusMath.Misses(board, 0f), Is.True);
        Assert.That(BossStatusMath.Misses(board, 0.29f), Is.True);
        Assert.That(BossStatusMath.Misses(board, 0.3f), Is.False);
        Assert.That(BossStatusMath.Misses(board, 0.95f), Is.False);

        board.Tick(1400, new StatusTuning());
        Assert.That(board.HasBlind, Is.False);
        Assert.That(BossStatusMath.Misses(board, 0f), Is.False);
    }

    [Test]
    public void X7_AccuracyDebuff_BecomesBlindChance_NotASureMiss()
    {
        var boss = new StatusBoard();
        StatusApplicator.ApplySkill(_motor.Resolve(new[] { 1, 7 }), new StatusBoard(), boss, new StatusTuning());
        Assert.That(boss.HasBlind, Is.True);
        Assert.That(boss.BlindMissChance, Is.EqualTo(0.3f).Within(0.001f));

        MechanicEffect kor = _grammar.Compose(1, 7, 1).Find("kor", "dusman");
        Assert.That(kor, Is.Not.Null);
        Assert.That(kor!.Amount, Is.EqualTo(0.3).Within(0.001));
        Assert.That(kor.DurationSec, Is.GreaterThan(0));
        Assert.That(BossStatusMath.BlindChanceFromAccuracy((float)kor.Amount), Is.EqualTo(0.3f).Within(0.001f));
    }

    [Test]
    public void Rune7_Bulandirma_BlindUsesAccuracyAndLifetime()
    {
        Assert.That(_motor.TryGetAdjective("7", out AdjectiveNode adjective), Is.True);
        var director = new SlotPassiveDirector();
        director.OpenCast();
        Assert.That(_motor.TryGetRune(7, out RuneDefinition rune), Is.True);
        director.Activate(7, rune.AdjectiveFace, rune.PassiveDurationDefault, adjective.EngineModifiers, 0);
        director.CloseCast();
        int later = director.OpenCast();
        Assert.That(director.AccuracyDebuffFor(later), Is.EqualTo(0.3f).Within(0.001f));
        double ms = BossStatusMath.BlindDurationMs(1400, director.AccuracyLifetimeAddSecFor(later));
        Assert.That(ms, Is.EqualTo(4400d).Within(0.01));
        var boss = new StatusBoard();
        boss.Apply(StatusKind.Blind, ms, BossStatusMath.BlindChanceFromAccuracy(director.AccuracyDebuffFor(later)));
        Assert.That(boss.BlindMissChance, Is.EqualTo(0.3f).Within(0.001f));
        Assert.That(BossStatusMath.Misses(boss, 0.1f), Is.True);
    }

    [Test]
    public void Silence_BlocksSpecials_Disarm_BlocksBasics()
    {
        var silenced = new StatusBoard();
        silenced.Apply(StatusKind.Silence, 3000, 1f);
        BossAttackGate cone = BossAttackControl.Gate(silenced, BossAttackMotion.Standing, BossAttackKind.FireCone, staggered: false);
        BossAttackGate slam = BossAttackControl.Gate(silenced, BossAttackMotion.Standing, BossAttackKind.Slam, staggered: false);
        Assert.That(cone.CanStart, Is.False);
        Assert.That(cone.CancelWindup, Is.True);
        Assert.That(slam.CanStart, Is.True);

        silenced.Tick(3000, new StatusTuning());
        Assert.That(
            BossAttackControl.Gate(silenced, BossAttackMotion.Standing, BossAttackKind.FireCone, false).CanStart,
            Is.True);

        var disarmed = new StatusBoard();
        disarmed.Apply(StatusKind.Disarm, 1000, 1f);
        Assert.That(
            BossAttackControl.Gate(disarmed, BossAttackMotion.Standing, BossAttackKind.Slam, false).CanStart,
            Is.False);
        Assert.That(
            BossAttackControl.Gate(disarmed, BossAttackMotion.Standing, BossAttackKind.FireCone, false).CanStart,
            Is.True);
    }

    [Test]
    public void Weaken_ReducesBossOutgoingDamage()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Weaken, 2500, 0.85f);
        Assert.That(BossStatusMath.OutgoingDamage(22f, board), Is.EqualTo(18.7f).Within(0.001f));
        Assert.That(BossStatusMath.OutgoingDamage(18f, board), Is.EqualTo(15.3f).Within(0.001f));
        board.Tick(2500, new StatusTuning());
        Assert.That(BossStatusMath.OutgoingDamage(22f, board), Is.EqualTo(22f).Within(0.001f));
    }

    [Test]
    public void HasarBuffOnEnemy_BecomesWeaken_For8_2_And8_10()
    {
        MechanicEffect drain = _grammar.Compose(8, 2, 4).Find("hasar_buff", "dusman");
        Assert.That(drain, Is.Not.Null);
        Assert.That(drain!.Amount, Is.LessThan(0));
        Assert.That(BossStatusMath.TryEnemyDamageDebuff(drain.Amount, drain.DurationSec, out float drainMult, out double drainMs), Is.True);
        Assert.That(drainMult, Is.LessThan(1f));
        Assert.That(drainMs, Is.GreaterThan(0));

        MechanicEffect mirror = _grammar.Compose(8, 10, 4).Find("hasar_buff", "dusman");
        Assert.That(mirror, Is.Not.Null);
        Assert.That(mirror!.Has("ters_kopya"), Is.True);
        Assert.That(BossStatusMath.TryEnemyDamageDebuff(mirror.Amount, mirror.DurationSec, out float mirrorMult, out _), Is.True);
        Assert.That(mirrorMult, Is.LessThan(1f));
        Assert.That(BossStatusMath.WeakenOutgoingMult(-0.2), Is.EqualTo(0.8f).Within(0.001f));
    }

    [Test]
    public void Poise_StaggersAtZero_ThenResets()
    {
        var poise = new BossPoise(100f);
        Assert.That(poise.ApplyHit(40f, 1.5f), Is.False);
        Assert.That(poise.Current, Is.EqualTo(60f).Within(0.001f));
        Assert.That(poise.IsStaggered, Is.False);

        float heavy = WeaponPassiveRules.OutgoingPoise(15f, skillMult: 1f, weaponMult: 1.8f, bonusMult: 2f * 1.5f);
        Assert.That(heavy, Is.EqualTo(81f).Within(0.001f));
        var bar = new BossPoise(100f);
        Assert.That(bar.ApplyHit(heavy, 1.5f), Is.False);
        Assert.That(bar.ApplyHit(100f - heavy, 1.5f), Is.True);
        Assert.That(bar.IsStaggered, Is.True);
        Assert.That(bar.Current, Is.EqualTo(0f).Within(0.001f));
        Assert.That(
            BossAttackControl.Gate(new StatusBoard(), BossAttackMotion.Standing, BossAttackKind.Slam, staggered: true).CanStart,
            Is.False);

        bar.Tick(1.49f);
        Assert.That(bar.IsStaggered, Is.True);
        bar.Tick(0.02f);
        Assert.That(bar.IsStaggered, Is.False);
        Assert.That(bar.Current, Is.EqualTo(100f).Within(0.001f));
    }

    [Test]
    public void Stealth_DoesNotAbsorbDamage_GroundHits_AimedMisses()
    {
        Assert.That(BossStatusMath.DamageInvulnerable(stasis: false), Is.False);
        Assert.That(BossStatusMath.DamageInvulnerable(stasis: true), Is.True);
        Assert.That(BossStatusMath.VolumeHits(targetStealthed: true, inVolume: true, arcHalfAngleDeg: 180f), Is.True);
        Assert.That(BossStatusMath.VolumeHits(targetStealthed: true, inVolume: true, arcHalfAngleDeg: 40f), Is.False);
        Assert.That(BossStatusMath.VolumeHits(targetStealthed: true, inVolume: false, arcHalfAngleDeg: 180f), Is.False);
        Assert.That(BossStatusMath.VolumeHits(targetStealthed: false, inVolume: true, arcHalfAngleDeg: 40f), Is.True);

        DamageOutcome hit = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 22f,
            ScaleMagnitudes = true,
            Invulnerable = BossStatusMath.DamageInvulnerable(false)
        });
        Assert.That(hit.Amount, Is.EqualTo(22f * CombatScale.DamageAndHp).Within(0.01f));
    }

    [Test]
    public void GuardHeal_ScalesThroughTheSamePipelineAsApplyHeal()
    {
        const float raw = 35f;
        DamageOutcome scaled = DamagePipeline.Resolve(new DamageQuery
        {
            Heal = true,
            HealPower = raw,
            HealMultiplier = 1f,
            ScaleMagnitudes = true
        });
        Assert.That(scaled.Amount, Is.EqualTo(raw * CombatScale.DamageAndHp).Within(0.01f));
        Assert.That(scaled.Amount, Is.GreaterThan(raw * 100f));
    }

    [Test]
    public void ShieldReflectAndRedirect_FireWhenBossDamageLands()
    {
        DamageOutcome soaked = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 22f,
            Shield = 50f,
            CanCrit = false,
            ScaleMagnitudes = true
        });
        Assert.That(soaked.Amount, Is.EqualTo(0f).Within(0.01f), "kalkan 50, slam 22'yi yutar");
        Assert.That(soaked.ShieldAbsorbed, Is.EqualTo(22f).Within(0.01f));
        float reflectBase = BossStatusMath.ReflectBase(soaked.Amount, soaked.ShieldAbsorbed, scaled: true);
        Assert.That(reflectBase, Is.EqualTo(22f * CombatScale.DamageAndHp).Within(0.01f));
        Assert.That(reflectBase * 0.3f, Is.GreaterThan(1000f), "yansıma kalkan yutsa da boss'a gider");

        float left = BossStatusMath.SplitShare(88_000f, 0.5f, out float shared);
        Assert.That(shared, Is.EqualTo(44_000f).Within(0.01f));
        Assert.That(left, Is.EqualTo(44_000f).Within(0.01f));
        left = BossStatusMath.SplitShare(left, 0.25f, out float routed);
        Assert.That(routed, Is.EqualTo(11_000f).Within(0.01f));
        Assert.That(left, Is.EqualTo(33_000f).Within(0.01f));
    }

    [Test]
    public void ElementStatus_BurnAndWeakenHitTheBoss_SelfStatusesDoNot()
    {
        ElementPaintNode fire = _motor.ElementPaints.First(e => e.Id == 1);
        Assert.That(fire.Status, Is.EqualTo("burn"));
        Assert.That(fire.StatusDurationSec, Is.EqualTo(4f).Within(0.001f));
        Assert.That(ElementBossStatusRules.TryForBoss(fire.Status, fire.StatusEffect, fire.StatusDurationSec, out ElementBossStatus burn), Is.True);
        Assert.That(burn.Kind, Is.EqualTo(StatusKind.Burn));
        Assert.That(burn.DurationMs, Is.EqualTo(4000d).Within(0.01));
        Assert.That(burn.Magnitude, Is.EqualTo(3f).Within(0.001f));

        ElementPaintNode dark = _motor.ElementPaints.First(e => e.Id == 6);
        Assert.That(dark.Status, Is.EqualTo("weaken"));
        Assert.That(ElementBossStatusRules.TryForBoss(dark.Status, dark.StatusEffect, dark.StatusDurationSec, out ElementBossStatus weaken), Is.True);
        Assert.That(weaken.Kind, Is.EqualTo(StatusKind.Weaken));
        Assert.That(weaken.DurationMs, Is.EqualTo(3000d).Within(0.01));
        Assert.That(weaken.Magnitude, Is.EqualTo(0.85f).Within(0.001f));

        ElementPaintNode water = _motor.ElementPaints.First(e => e.Id == 2);
        Assert.That(ElementBossStatusRules.TryForBoss(water.Status, water.StatusEffect, water.StatusDurationSec, out _), Is.False);
        ElementPaintNode light = _motor.ElementPaints.First(e => e.Id == 5);
        Assert.That(ElementBossStatusRules.TryForBoss(light.Status, light.StatusEffect, light.StatusDurationSec, out _), Is.False);
    }

    static string ElementPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "docs", "element-sistemi.json"));
        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..",
                "docs", "element-sistemi.json"));
        }
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }
}
