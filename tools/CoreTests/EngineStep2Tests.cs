using Dovus.Core;
using Dovus.Core.Combat;
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

    [Test]
    public void SlowReappliedHundredTimes_NeverExceedsCardDuration_ThenExpires()
    {
        JsonValue root = MiniJson.Parse(_json);
        float jsonSec = root["verb_base"]["12"]["tempo_duration_sec"].AsFloat(0f);
        TempoCast yogun = TempoCast.From(_motor.Resolve(new[] { 12, 1 }));
        Assert.That(yogun.DurationMs, Is.EqualTo(jsonSec * 1000.0).Within(0.01));
        Assert.That(yogun.DurationMs, Is.EqualTo(1000).Within(0.01));
        Assert.That(yogun.SlowStrength, Is.EqualTo(0.7f).Within(0.001f));
        Assert.That(yogun.EnemySlow, Is.True);

        var enemy = new StatusBoard();
        enemy.ConfigureMobilityCc(MobilityCcData.FromJson(_json));
        var tuning = new StatusTuning();
        for (int i = 0; i < 100; i++)
        {
            enemy.Tick(20, tuning);
            yogun.Apply(null, enemy, null);
            Assert.That(enemy.TryGet(StatusKind.Slow, out double rem, out float mag, out _), Is.True);
            Assert.That(rem, Is.LessThanOrEqualTo(yogun.DurationMs + 0.01), "yavaşlatma kart süresini aşamaz");
            Assert.That(rem, Is.GreaterThan(0));
            Assert.That(mag, Is.EqualTo(yogun.SlowStrength).Within(0.001f));
        }

        enemy.Tick(yogun.DurationMs, tuning);
        Assert.That(enemy.Has(StatusKind.Slow), Is.False, "yavaşlatma süresi dolunca biter");
        Assert.That(enemy.IsRootImmune, Is.False);
        yogun.Apply(null, enemy, null);
        Assert.That(enemy.Has(StatusKind.Slow), Is.True, "bitince yeniden uygulanabilir");
    }

    [Test]
    public void SlowTwoSources_DoNotAdd_StrongestWins_ThenTheRestExpires()
    {
        TempoCast yogun = TempoCast.From(_motor.Resolve(new[] { 12, 1 }));
        double tableMs = MobilityCcData.FromJson(_json)
            .ResolveCcDurationMs(StatusKind.Slow, 0, yogun.DurationMs);
        var tuning = new StatusTuning();
        Assert.That(tableMs, Is.GreaterThan(yogun.DurationMs));
        Assert.That(tuning.SlowSpeedMult, Is.LessThan(yogun.SlowStrength));

        var board = new StatusBoard();
        board.ConfigureMobilityCc(MobilityCcData.FromJson(_json));
        board.Apply(StatusKind.Slow, yogun.DurationMs, tuning.SlowSpeedMult, "short");
        board.Apply(StatusKind.Slow, tableMs, yogun.SlowStrength, "long");
        Assert.That(board.TryGet(StatusKind.Slow, out double rem, out float mag, out _), Is.True);
        Assert.That(rem, Is.EqualTo(tableMs).Within(0.01), "iki kaynak toplanmaz");
        Assert.That(rem, Is.LessThan(tableMs + yogun.DurationMs));
        Assert.That(mag, Is.EqualTo(tuning.SlowSpeedMult).Within(0.001f), "güçlü yavaşlatma kalır");

        board.Tick(yogun.DurationMs, tuning);
        Assert.That(board.TryGet(StatusKind.Slow, out rem, out mag, out _), Is.True);
        Assert.That(rem, Is.EqualTo(tableMs - yogun.DurationMs).Within(0.01));
        Assert.That(mag, Is.EqualTo(yogun.SlowStrength).Within(0.001f), "kısa kaynak bitince diğeri kalır");

        board.Tick(tableMs - yogun.DurationMs, tuning);
        Assert.That(board.Has(StatusKind.Slow), Is.False);

        board.Apply(StatusKind.Slow, yogun.DurationMs, yogun.SlowStrength, "short");
        board.CleanseHostile();
        board.Tick(200, tuning);
        Assert.That(board.Has(StatusKind.Slow), Is.False, "temizlik sonrası süre geri gelmez");
    }

    [Test]
    public void HasteReapplied_RefreshesAndTwoSourcesDoNotAdd()
    {
        SkillResolution risingSkill = _motor.Resolve(new[] { 12, 8 });
        TempoCast rising = TempoCast.From(risingSkill);
        TempoCast bond = TempoCast.From(_motor.Resolve(new[] { 12, 6 }));
        float buffSec = risingSkill.EngineModifiers["buff_duration_sec"].AsFloat(0f);
        Assert.That(rising.SelfHaste, Is.True);
        Assert.That(rising.DurationMs, Is.EqualTo(3000).Within(0.01));
        Assert.That(buffSec * 1000.0, Is.EqualTo(rising.DurationMs).Within(0.01));
        Assert.That(rising.HasteStrength, Is.GreaterThan(bond.HasteStrength));
        Assert.That(bond.DurationMs, Is.LessThan(rising.DurationMs));

        var self = new StatusBoard();
        self.ConfigureMobilityCc(MobilityCcData.FromJson(_json));
        var tuning = new StatusTuning();
        for (int i = 0; i < 100; i++)
        {
            self.Tick(20, tuning);
            rising.Apply(self, null, null);
            Assert.That(self.TryGet(StatusKind.Haste, out double rem, out float mag, out _), Is.True);
            Assert.That(rem, Is.LessThanOrEqualTo(rising.DurationMs + 0.01));
            Assert.That(mag, Is.EqualTo(rising.HasteStrength).Within(0.001f));
        }

        self.Tick(rising.DurationMs, tuning);
        Assert.That(self.Has(StatusKind.Haste), Is.False);

        self.Apply(StatusKind.Haste, bond.DurationMs, rising.HasteStrength, "short");
        self.Apply(StatusKind.Haste, rising.DurationMs, bond.HasteStrength, "long");
        Assert.That(self.TryGet(StatusKind.Haste, out double left, out float strength, out _), Is.True);
        Assert.That(left, Is.EqualTo(rising.DurationMs).Within(0.01), "iki hız toplanmaz");
        Assert.That(strength, Is.EqualTo(rising.HasteStrength).Within(0.001f));

        self.Tick(bond.DurationMs, tuning);
        Assert.That(self.TryGet(StatusKind.Haste, out left, out strength, out _), Is.True);
        Assert.That(left, Is.EqualTo(rising.DurationMs - bond.DurationMs).Within(0.01));
        Assert.That(strength, Is.EqualTo(bond.HasteStrength).Within(0.001f));

        self.Tick(rising.DurationMs - bond.DurationMs, tuning);
        Assert.That(self.Has(StatusKind.Haste), Is.False);
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
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "unity", "Assets", "Resources", "ElementSystem", "element-sistemi.json"));
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }
}
