using System.IO;
using Dovus.Core;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class EngineStep2Tests
{
    SkillMotor _motor;
    string _json;

    [SetUp]
    public void Load()
    {
        DesignWarnings.ResetForTests();
        _json = File.ReadAllText(JsonPath());
        _motor = SkillMotor.FromJson(_json);
    }

    [Test]
    public void RootBlocksOnlyMovementAttacks_StunBlocksAllAndCancelsWindup()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Root, 1000, 1f, "a");

        Assert.That(BossAttackControl.Evaluate(board, BossAttackMotion.Charge).CanStart, Is.False);
        Assert.That(BossAttackControl.Evaluate(board, BossAttackMotion.Leap).CanStart, Is.False);
        Assert.That(BossAttackControl.Evaluate(board, BossAttackMotion.Dash).CanStart, Is.False);
        BossAttackGate standing = BossAttackControl.Evaluate(board, BossAttackMotion.Standing);
        Assert.That(standing.CanStart, Is.True);
        Assert.That(standing.CancelWindup, Is.False);
        Assert.That(BossAttackControl.MotionOf(BossAttackKind.Slam), Is.EqualTo(BossAttackMotion.Standing));
        Assert.That(BossAttackControl.MotionOf(BossAttackKind.FireCone), Is.EqualTo(BossAttackMotion.Standing));

        board.Clear();
        board.Apply(StatusKind.Stun, 1000, 1f);
        BossAttackGate stunned = BossAttackControl.Evaluate(board, BossAttackMotion.Standing);
        Assert.That(stunned.CanStart, Is.False);
        Assert.That(stunned.CancelWindup, Is.True);
        Assert.That(BossAttackControl.Evaluate(board, BossAttackMotion.Charge).CanStart, Is.False);
    }

    [Test]
    public void SlowScalesBossPhasesLikeMovement_AndStunHidesRoot()
    {
        var board = new StatusBoard();
        board.ConfigureMobilityCc(MobilityCcData.FromJson(_json));
        board.Apply(StatusKind.Slow, 2000, 0.4f);
        BossAttackGate slowed = BossAttackControl.Evaluate(board, BossAttackMotion.Standing);
        Assert.That(slowed.CanStart, Is.True);
        Assert.That(slowed.PhaseSpeed, Is.EqualTo(0.4f).Within(0.001f));
        Assert.That(board.ActionSpeedMult, Is.EqualTo(board.MoveSpeedMult).Within(0.001f));

        board.Apply(StatusKind.Stun, 1000, 1f);
        board.Apply(StatusKind.Root, 1000, 1f, "hidden");
        Assert.That(board.HasEffective(StatusKind.Root), Is.False, "stun varken kök görünmez");
        Assert.That(BossAttackControl.Evaluate(board, BossAttackMotion.Standing).CanStart, Is.False);
    }

    [Test]
    public void BossStunDoesNotStackForever_ThenImmunityBlocksReapply()
    {
        var board = new StatusBoard();
        board.ConfigureMobilityCc(MobilityCcData.FromJson(_json));
        board.EnableAttackLockImmunity();
        var tuning = new StatusTuning();
        board.Apply(StatusKind.Stun, 1000, 1f);
        board.Apply(StatusKind.Stun, 1000, 1f);
        Assert.That(board.TryGet(StatusKind.Stun, out double left, out _, out _), Is.True);
        Assert.That(left, Is.EqualTo(1000).Within(0.01), "sersemlik süresi eklenmez");

        board.Tick(1000, tuning);
        Assert.That(board.Has(StatusKind.Stun), Is.False);
        Assert.That(board.IsAttackLockImmune, Is.True);
        board.Apply(StatusKind.Stun, 1000, 1f);
        Assert.That(board.Has(StatusKind.Stun), Is.False);
        Assert.That(BossAttackControl.Evaluate(board, BossAttackMotion.Standing).CanStart, Is.True);

        board.Tick(SkillNumberFallbacks.RootImmunityMs, tuning);
        Assert.That(board.IsAttackLockImmune, Is.False);
        board.Apply(StatusKind.Stun, 500, 1f);
        Assert.That(board.Has(StatusKind.Stun), Is.True);
    }

    [Test]
    public void AllySkillHarmDoesNotHitBoss_UnlessCardTargetsTheAttacker()
    {
        var tuning = new StatusTuning();
        SkillResolution heal = _motor.Resolve(new[] { 2, 6 });
        var caster = new StatusBoard();
        var boss = new StatusBoard();
        StatusApplicator.ApplySkill(heal, caster, boss, tuning);
        Assert.That(boss.Has(StatusKind.Root), Is.False);
        Assert.That(boss.Has(StatusKind.Slow), Is.False);

        SkillResolution shield = _motor.Resolve(new[] { 4, 7 });
        var boss2 = new StatusBoard();
        StatusApplicator.ApplySkill(shield, caster, boss2, tuning);
        Assert.That(boss2.Has(StatusKind.Slow), Is.True, "vurana yavaşlatma");
        Assert.That(boss2.Has(StatusKind.Blind), Is.False);
    }

    [Test]
    public void CardNamedEffectIsTheKindApplied()
    {
        var tuning = new StatusTuning();
        var mismatches = new System.Text.StringBuilder();
        for (int verb = 1; verb <= 12; verb++)
        {
            for (int adj = 1; adj <= 12; adj++)
            {
                SkillResolution skill = _motor.Resolve(new[] { verb, adj });
                StatusKind named = CardEffectRules.ExclusiveKind(skill.SkillJob);
                if (named == StatusKind.None)
                    continue;

                var caster = new StatusBoard();
                var enemy = new StatusBoard();
                StatusBoard hostile = CardEffectRules.HarmfulHitsEnemy(
                    skill.TargetMode, skill.Action, skill.SkillJob)
                    ? enemy
                    : null;
                StatusApplicator.ApplySkill(skill, caster, hostile, tuning);
                if (string.Equals(skill.Action, "tempo", System.StringComparison.OrdinalIgnoreCase))
                    TempoCast.From(skill).Apply(caster, enemy, null);

                bool Has(StatusKind kind) => caster.Has(kind) || enemy.Has(kind);
                if (!Has(named))
                    mismatches.AppendLine($"{skill.SkillId} {skill.DisplayName}: kart {named}, uygulanmadı ({skill.SkillJob})");
                if (named == StatusKind.Stun && Has(StatusKind.Root))
                    mismatches.AppendLine($"{skill.SkillId} {skill.DisplayName}: stun yerine kök de var");
                if (named == StatusKind.Root && Has(StatusKind.Stun))
                    mismatches.AppendLine($"{skill.SkillId} {skill.DisplayName}: kök yerine sersemlik var");
                if (named == StatusKind.Slow && Has(StatusKind.Blind))
                    mismatches.AppendLine($"{skill.SkillId} {skill.DisplayName}: yavaş yerine kör");
                if (named == StatusKind.Haste && (enemy.Has(StatusKind.Slow) || enemy.Has(StatusKind.Root)))
                    mismatches.AppendLine($"{skill.SkillId} {skill.DisplayName}: hız yerine düşmana yavaş/kök");
                if (named == StatusKind.Blind && Has(StatusKind.Slow) && !CardEffectRules.NamesSlow(skill.SkillJob))
                    mismatches.AppendLine($"{skill.SkillId} {skill.DisplayName}: kör yerine yavaş");
            }
        }

        Assert.That(mismatches.ToString(), Is.Empty);
    }

    [Test]
    public void KnownCardMismatches_NowMatch()
    {
        var tuning = new StatusTuning();
        SkillResolution stun = _motor.Resolve(new[] { 6, 1 });
        var boss = new StatusBoard();
        StatusApplicator.ApplySkill(stun, new StatusBoard(), boss, tuning);
        Assert.That(stun.Mechanics, Does.Contain("stun"));
        Assert.That(stun.Mechanics, Does.Not.Contain("root"));
        Assert.That(boss.Has(StatusKind.Stun), Is.True);
        Assert.That(boss.Has(StatusKind.Root), Is.False);
        Assert.That(boss.TryGet(StatusKind.Stun, out double ms, out _, out _), Is.True);
        Assert.That(ms, Is.EqualTo(1500).Within(0.01));

        TempoCast rising = TempoCast.From(_motor.Resolve(new[] { 12, 8 }));
        Assert.That(rising.SelfHaste, Is.True);
        Assert.That(rising.EnemySlow, Is.False);
        Assert.That(rising.HasteStrength, Is.EqualTo(1.5f).Within(0.001f));

        TempoCast bond = TempoCast.From(_motor.Resolve(new[] { 12, 6 }));
        Assert.That(bond.SelfHaste, Is.True);
        Assert.That(bond.AllyHaste, Is.True);
        Assert.That(bond.EnemySlow, Is.False);
        Assert.That(bond.HasteStrength, Is.EqualTo(1.3f).Within(0.001f));
    }

    [Test]
    public void TempoSlowReadsJson_AndWarnsOnceWhenMissing()
    {
        TempoSyncRules.Read(1.5, 0.4f, out double ms, out float strength);
        Assert.That(ms, Is.EqualTo(1500).Within(0.01));
        Assert.That(strength, Is.EqualTo(0.4f).Within(0.001f));
        Assert.That(DesignWarnings.WasWarned("tempo_sync"), Is.False);

        TempoSyncRules.Read(0, 0f, out ms, out strength);
        Assert.That(ms, Is.EqualTo(SkillNumberFallbacks.TempoSyncFallbackMs).Within(0.01));
        Assert.That(strength, Is.EqualTo(SkillNumberFallbacks.TempoSyncFallbackStrength).Within(0.001f));
        Assert.That(DesignWarnings.WasWarned("tempo_sync"), Is.True);

        DesignWarnings.ResetForTests();
        TempoSyncRules.Read(0, 0f, out _, out _);
        TempoSyncRules.Read(0, 0f, out _, out _);
        Assert.That(DesignWarnings.WasWarned("tempo_sync"), Is.True);
    }

    static string JsonPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "unity", "Assets", "Resources", "ElementSystem", "element-sistemi.json"));
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }
}
