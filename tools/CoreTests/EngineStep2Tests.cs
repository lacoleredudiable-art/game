using Dovus.Core;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using NUnit.Framework;
using System.IO;

using Dovus.Core.Shared;
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
    public void CardNamedEffectIsTheKindApplied()
    {
        var tuning = new StatusTuning();
        var mismatches = new System.Text.StringBuilder();
        int[] slice = { 1, 2, 3, 4, 6, 9 };
        foreach (int verb in slice)
        {
            foreach (int adj in slice)
            {
                SkillResolution skill = _motor.Resolve(new[] { verb, adj });
                StatusKind named = CardEffectRules.ExclusiveKind(skill.Identity.SkillJob);
                if (named == StatusKind.None)
                    continue;

                var caster = new StatusBoard();
                var enemy = new StatusBoard();
                StatusBoard hostile = CardEffectRules.HarmfulHitsEnemy(
                    skill.Targeting.Mode, skill.Presentation.Action, skill.Identity.SkillJob)
                    ? enemy
                    : null;
                StatusApplicator.ApplySkill(skill, caster, hostile, tuning);
                if (string.Equals(skill.Presentation.Action, "tempo", System.StringComparison.OrdinalIgnoreCase))
                    TempoCast.From(skill).Apply(caster, enemy, null);

                bool Has(StatusKind kind) => caster.Has(kind) || enemy.Has(kind);
                if (!Has(named))
                    mismatches.AppendLine($"{skill.Identity.Id} {skill.Identity.DisplayName}: kart {named}, uygulanmadı ({skill.Identity.SkillJob})");
                if (named == StatusKind.Stun && Has(StatusKind.Root))
                    mismatches.AppendLine($"{skill.Identity.Id} {skill.Identity.DisplayName}: stun yerine kök de var");
                if (named == StatusKind.Root && Has(StatusKind.Stun))
                    mismatches.AppendLine($"{skill.Identity.Id} {skill.Identity.DisplayName}: kök yerine sersemlik var");
                if (named == StatusKind.Slow && Has(StatusKind.Blind))
                    mismatches.AppendLine($"{skill.Identity.Id} {skill.Identity.DisplayName}: yavaş yerine kör");
                if (named == StatusKind.Haste && (enemy.Has(StatusKind.Slow) || enemy.Has(StatusKind.Root)))
                    mismatches.AppendLine($"{skill.Identity.Id} {skill.Identity.DisplayName}: hız yerine düşmana yavaş/kök");
                if (named == StatusKind.Blind && Has(StatusKind.Slow) && !CardEffectRules.NamesSlow(skill.Identity.SkillJob))
                    mismatches.AppendLine($"{skill.Identity.Id} {skill.Identity.DisplayName}: kör yerine yavaş");
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

    [Test]
    public void ReapplyRefreshesDuration_ForOtherStatuses()
    {
        var tuning = new StatusTuning();
        StatusKind[] kinds =
        {
            StatusKind.Stun, StatusKind.Silence, StatusKind.Knockback, StatusKind.Fear, StatusKind.Stasis,
            StatusKind.Blind, StatusKind.Disarm, StatusKind.Taunt,
            StatusKind.Burn, StatusKind.Poison, StatusKind.ArmorBreak, StatusKind.Weaken, StatusKind.GrievousWounds,
            StatusKind.Shield, StatusKind.DamageReduction, StatusKind.Regen, StatusKind.Stealth
        };
        foreach (StatusKind kind in kinds)
        {
            var board = new StatusBoard();
            board.ConfigureMobilityCc(MobilityCcData.FromJson(_json));
            float magnitude = kind == StatusKind.Shield ? 10f : 0.5f;
            board.Apply(kind, 1000, magnitude);
            board.Tick(250, tuning);
            board.Apply(kind, 1000, magnitude);
            Assert.That(board.TryGet(kind, out double rem, out float mag, out _), Is.True, kind.ToString());
            Assert.That(rem, Is.EqualTo(1000).Within(0.01), kind + " süresi eklenmez");
            Assert.That(mag, Is.EqualTo(magnitude).Within(0.001f), kind + " gücü toplanmaz");
            board.Tick(1000, tuning);
            Assert.That(board.Has(kind), Is.False, kind + " süresi dolunca biter");
        }
    }

    static string JsonPath()
    {
        string path = Path.GetFullPath(SliceGrammarTestPaths.SliceGrammarJsonPath());
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }
}
