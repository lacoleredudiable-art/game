using System;
using System.Collections.Generic;
using Dovus.App.Actors;
using Dovus.App.Boss;
using Dovus.App.Casting;
using Dovus.App.Sweep;
using Dovus.App.Team;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Shared;
using Dovus.Core.Tuning;
using Dovus.Core.Grammar;
using Dovus.Core.Element;
using Dovus.Core.Equipment;
using NUnit.Framework;

namespace CoreTests;

/// <summary>App katmanı senaryoları — SweepV2'nin örneklediği kararların shim'siz doğrulaması.</summary>
[TestFixture]
public class AppSimulationTests
{
    [Test]
    public void SweepCatalog_144Combos_PerWeapon_OrderStable()
    {
        var a = SweepComboCatalog.OrderedCombos("Kılıç");
        var b = SweepComboCatalog.OrderedCombos("Kılıç");
        Assert.That(a.Count, Is.EqualTo(144));
        for (int i = 0; i < a.Count; i++)
        {
            Assert.That(a[i].Verb, Is.EqualTo(b[i].Verb));
            Assert.That(a[i].Adj, Is.EqualTo(b[i].Adj));
            Assert.That(a[i].Weapon, Is.EqualTo(b[i].Weapon));
        }
    }

    [Test]
    public void SweepCsvFormat_MatchesPlaySweepQuoting()
    {
        Assert.That(SweepCsvFormat.Quote("a\"b"), Is.EqualTo("\"a'b\""));
        Assert.That(SweepCsvFormat.Bit(true), Is.EqualTo("1"));
        Assert.That(SweepCsvFormat.Number(1.5f), Is.EqualTo("1.50"));
    }

    [Test]
    public void PlayerDown_ThenRevive_RespawnWindowMatchesSweepExpectation()
    {
        var health = new PlayerHealth();
        health.Bind(20, 1f);
        health.ApplyHpLoss(20, 0f, 4f);
        Assert.That(health.IsDown, Is.True);
        Assert.That(health.RespawnInSec(2f), Is.EqualTo(2f).Within(0.001f));
        Assert.That(health.ReviveDue(4f), Is.True);
        health.Revive();
        Assert.That(health.IsDown, Is.False);
        Assert.That(health.Hp, Is.EqualTo(20));
    }

    [Test]
    public void TeamModifiers_TakenMult_ThenPlayerLoss_ScalesEffectiveHp()
    {
        const int playerId = 1;
        var table = new TeamModifierTable();
        table.SetTaken(playerId, 2f);
        var health = new PlayerHealth();
        health.Bind(100, 1f);
        float raw = 10f;
        float scaled = raw * table.DamageTakenMult(playerId);
        health.ApplyHpLoss((int)Math.Round(scaled), 0f, 3f);
        Assert.That(health.Hp, Is.EqualTo(80));
    }

    [Test]
    public void ClosingHeal_ComputeThenApply_RoundsLikeVitals()
    {
        var skill = SkillResolution.Build(
            "1", "Ateş", "Test", "2-3", "job",
            "mend", "Mend", "heal", "heal",
            0f, 0f, "projectile", "free_move", Array.Empty<string>(),
            "y", "Y", "focus",
            1f, 1f, 1f,
            1, "Temel", 1f, "free_move",
            string.Empty,
            isComplete: true,
            baseHeal: 12f);
        int heal = ClosingHealRules.ComputeHealAmount(10f, skill, 1f, 1f, 1f, 1f);
        var health = new PlayerHealth();
        health.Bind(30, 0.5f);
        int applied = health.ApplyHeal(heal);
        Assert.That(heal, Is.GreaterThan(0));
        Assert.That(applied, Is.EqualTo(heal));
        Assert.That(health.Hp, Is.EqualTo(27));
    }

    [Test]
    public void CastPipeline_BasicHit_EmitsBasicOutcomeWithDamage()
    {
        var port = new SimBasicPort { StrikeConnects = true, Dealt = 7.5f };
        var pipeline = new CastPipeline();
        BasicOutcome? captured = null;
        pipeline.BasicCompleted += o => captured = o;
        var outcome = pipeline.RunBasic(0, port);
        Assert.That(outcome.Connected, Is.True);
        Assert.That(outcome.Dealt, Is.EqualTo(7.5f).Within(0.001f));
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured.Value.Dealt, Is.EqualTo(7.5f).Within(0.001f));
    }

    [Test]
    public void CastPipeline_SkillDenied_SkipsExecutorPath()
    {
        var port = new SimCastPort
        {
            ResolveResult = SkillResolution.Build(
                "1", "Ateş", "X", "t", "job",
                "saldiri", "Saldırı", "strike", "damage",
                1f, 1f, "projectile", "free_move", Array.Empty<string>(),
                "y", "Y", "focus",
                1f, 1f, 1f,
                1, "Temel", 1f, "free_move",
                string.Empty,
                isComplete: false)
        };
        var outcome = new CastPipeline().RunSkill(0, port);
        Assert.That(outcome.Denied, Is.True);
        Assert.That(port.Calls, Does.Not.Contain(nameof(ICastPort<object>.OpenSlotCast)));
    }

    [Test]
    public void BossAttackSelector_SameSeed_RepeatsAttackKinds()
    {
        var phase1 = new[] { BossAttackKind.Slam, BossAttackKind.Volley, BossAttackKind.Pounce };
        var phase2 = new[] { BossAttackKind.Slam, BossAttackKind.FireCone, BossAttackKind.Volley, BossAttackKind.WebField };
        const int seed = 0x2_6a_01;
        BossAttackKind first = RunSelectorPick(seed, phase1, phase2);
        BossAttackKind second = RunSelectorPick(seed, phase1, phase2);
        Assert.That(second, Is.EqualTo(first));
    }

    static BossAttackKind RunSelectorPick(int seed, BossAttackKind[] phase1, BossAttackKind[] phase2)
    {
        var rng = new Random(seed);
        var selector = new BossAttackSelector();
        selector.TrySelect(false, phase1, phase2, _ => true, 2, 2, rng, out BossAttackChoice choice);
        return choice.Kind;
    }

    [Test]
    public void BossBrain_PlayerDown_IdleDoesNotWindup()
    {
        var port = new SimBossPort { PlayerDown = true, Attack = SimBossPort.Slam() };
        var brain = new BossBrain(new Random(9), port);
        brain.EnterIdle(0);
        port.Calls.Clear();
        brain.Tick(50_000, 0.016f, 16);
        Assert.That(brain.Phase, Is.EqualTo(BossBrainPhase.Idle));
        Assert.That(port.Calls, Does.Not.Contain(nameof(IBossBrainPort.SelectNextAttack)));
    }

    [Test]
    public void TeamModifier_MissRoll_RespectsConfiguredChance()
    {
        var table = new TeamModifierTable { Roll = () => 0.2f };
        table.SetMiss(3, 0.5f);
        Assert.That(table.TryMiss(3), Is.True);
        table.SetMiss(3, 0.1f);
        Assert.That(table.TryMiss(3), Is.False);
    }

    sealed class SimBasicPort : IBasicStrikePort<int>
    {
        public bool StrikeConnects;
        public float Dealt = 7.5f;
        public SkillResolution ResolveResult = SkillResolution.Empty;

        public void ResolveImpactTarget(int ctx) { }
        public void ResetClosingChainBonus() { }
        public SkillResolution ResolveSkill(int ctx) => ResolveResult;
        public bool IsHealSkill(SkillResolution skill) => false;
        public void ApplyClosingStatuses(int ctx, SkillResolution skill) { }
        public void ShoutSkill(SkillResolution skill, int ctx) { }
        public void ApplyClosingHeal(int ctx, SkillResolution skill) { }
        public double WorldTimeMs() => 0;
        public bool BasicCadenceReady(double now) => true;
        public void NoteDeniedCadence() { }
        public void SetLastBasicStrikeMs(double now) { }
        public int BasicHitsNow() => 1;
        public float BasicStrikeReachM() => 5f;
        public bool IsBossInStrikeCapsule(int ctx, float reachM) => StrikeConnects;
        public bool EvaluateBasicInReach(float reachM) => StrikeConnects;
        public float BasicStrikeArcDeg() => 0f;
        public float BasicStrikeYawDeg() => 0f;
        public bool HasLivingLogic(int ctx) => false;
        public void ApplyBossClosingBasic(int ctx) { }
        public float ApplyBasicStrikeDamage(int ctx, float effectScale) => Dealt;
        public void ScheduleBasicSubHits(int ctx, int hits, float reach) { }
        public void ApplyBasicExtras(float dealt, int hits) { }
        public void TryLandWeaponStunBasic() { }
        public void SpawnClosingImpact(int ctx) { }
        public void TryCannonBlast(int ctx) { }
    }

    sealed class SimCastPort : ICastPort<int>
    {
        public readonly List<string> Calls = new();
        public SkillResolution ResolveResult = SkillResolution.Empty;
        void Record(string name) => Calls.Add(name);
        public void ResetClosingChainBonus() => Record(nameof(ResetClosingChainBonus));
        public SkillResolution ResolveSkill(int ctx)
        {
            Record(nameof(ResolveSkill));
            return ResolveResult;
        }

        public void NoteDeniedNeedsTwoRunes() => Record(nameof(NoteDeniedNeedsTwoRunes));
        public void NoteWeaponCast(SkillResolution skill) => Record(nameof(NoteWeaponCast));
        public int OpenSlotCast()
        {
            Record(nameof(OpenSlotCast));
            return 1;
        }
        public WeaponSkillCompatibility Compatibility(SkillResolution skill) => WeaponSkillCompatibility.Neutral;
        public void PublishCompatibility(WeaponSkillCompatibility compatibility) => Record(nameof(PublishCompatibility));
        public bool ShouldArmPassive(WeaponSkillCompatibility compatibility) => false;
        public void TryTriggerPassive(int ctx) { }
        public void ApplyResourceCost(SkillResolution skill) => Record(nameof(ApplyResourceCost));
        public SkillMotionPlan ResolveMotion(SkillResolution skill) => SkillMotionPlan.None;
        public void ApplyMotionIframe(SkillResolution skill, in SkillMotionPlan motion) { }
        public bool TryBeginMotionTemplate(SkillResolution skill, int ctx) => false;
        public void NoteSustainedCast(SkillResolution skill) { }
        public void NotifyCast(string skillId) => Record(nameof(NotifyCast));
        public SkillExecutorRoute Route(SkillResolution skill) => new(SkillExecutorKind.MeleeHitbox, false, string.Empty);
        public SkillExecutorRoute ApplyMechanicWorldRoute(SkillResolution skill, SkillExecutorRoute route) => route;
        public void SetLastExecutorKind(SkillExecutorKind kind) { }
        public void ApplySelfCastEffects(SkillResolution skill) { }
        public void NoteJsonCast(SkillResolution skill, int ctx) { }
        public void BeginMechanicPlan(int ctx, SkillResolution skill) { }
        public void ArmTemplateDelivery(SkillResolution skill, int ctx, in SkillMotionPlan motion) { }
        public bool TryLaunchExecutor(SkillExecutorKind kind, int ctx, SkillResolution skill, in SkillMotionPlan motion) => false;
        public void ScheduleFollowUps(SkillExecutorKind kind, int ctx, SkillResolution skill, in SkillMotionPlan motion) { }
        public float ApplyFallbackDelivery(int ctx, SkillResolution skill, in SkillMotionPlan motion, in SkillExecutorRoute route) => 0f;
        public void ShoutSkill(SkillResolution skill, int ctx) { }
        public void ApplyCooldown(SkillResolution skill, int ctx, bool cosmeticIfDisabled) { }
        public void AnnotateMotion(SkillResolution skill, in SkillMotionPlan motion) { }
        public void SpawnClosingImpact(int ctx) { }
        public void SetLastResolvedSkillId(string skillId) { }
        public bool IsHealSkill(SkillResolution skill) => false;
        public void SetLastSkillEffectApplied(bool applied) { }
        public void LogSmokeOneOne(SkillResolution skill, bool effectApplied, float dealt) { }
        public void TrySchedulePassiveEcho(int ctx, SkillResolution skill, in SkillMotionPlan motion) { }
        public void CloseSlotCast() => Record(nameof(CloseSlotCast));
        public void ResetSlotQueryCastId() => Record(nameof(ResetSlotQueryCastId));
    }

    sealed class SimBossPort : IBossBrainPort
    {
        public readonly List<string> Calls = new();
        public BossAttack Attack { get; set; }
        public bool PlayerDown;
        public int IdleMinMsValue = 50;
        public int IdleMaxMsValue = 50;

        public int IdleMinMs => IdleMinMsValue;
        public int IdleMaxMs => IdleMaxMsValue;
        public void HideTelegraphAndClearThreat() => Calls.Add(nameof(HideTelegraphAndClearThreat));
        public void ClearThreat() => Calls.Add(nameof(ClearThreat));
        public bool IsPlayerDown => PlayerDown;
        public void TryAnnouncePhase2Roar() => Calls.Add(nameof(TryAnnouncePhase2Roar));
        public bool IsVisualBusy => false;
        public void SetVisualSpeedZero() => Calls.Add(nameof(SetVisualSpeedZero));
        public void ExtendIdleWait(double worldMs, ref double idleUntilWorldMs) => Calls.Add(nameof(ExtendIdleWait));
        public bool ShouldRetarget => false;
        public void PickTarget() => Calls.Add(nameof(PickTarget));
        public void Approach(float dtSec) => Calls.Add(nameof(Approach));
        public bool CanStartStandingAttack => true;
        public bool SelectNextAttack()
        {
            Calls.Add(nameof(SelectNextAttack));
            return true;
        }

        public bool CanStartWindupGate => true;
        public BossAttack CurrentAttack => Attack;
        public float CurrentPhaseSpeed => 1f;
        public float AttackRadiusM => 1f;
        public bool IsPounceAirborne => false;
        public void ResolveStrike() => Calls.Add(nameof(ResolveStrike));
        public void LogResolveStrikeException(Exception e) { }
        public void AfterStrikeResolved(BossAttackKind kind, float attackRadiusM) { }
        public void RaiseAttackStruck(BossAttackKind kind) => Calls.Add(nameof(RaiseAttackStruck));
        public void OnRecoveryTelegraph(float fade, float attackRadiusM) { }
        public void OnEnterIdleAfterState() => Calls.Add(nameof(OnEnterIdleAfterState));
        public void OnEnterWindup(double worldMs) => Calls.Add(nameof(OnEnterWindup));
        public void OnEnterActive(double worldMs) => Calls.Add(nameof(OnEnterActive));
        public void OnEnterRecovery(double worldMs) => Calls.Add(nameof(OnEnterRecovery));
        public void OnWindupTelegraph(float progress01, float attackRadiusM, SlamVariant variant) { }
        public void OnWindupThreat(float progress01) { }

        public static BossAttack Slam()
        {
            var tuning = new BossTuning { WindupMs = 100, ActiveMs = 50, RecoveryMs = 80 };
            var a = new BossAttack(tuning);
            a.ApplyVariant(SlamVariant.Near);
            return a;
        }
    }
}
