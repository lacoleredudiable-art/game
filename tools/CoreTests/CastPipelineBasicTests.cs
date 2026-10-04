using Dovus.App.Casting;
using Dovus.Core.Grammar;
using NUnit.Framework;
using System.Collections.Generic;

namespace CoreTests;

[TestFixture]
public class CastPipelineBasicTests
{
    /// <summary>ManifestationDirector.FireClosing düz vuruş dalı (2.4b öncesi) çağrı sırası — isabet yolu.</summary>
    static readonly string[] ExpectedBasicHitOrder =
    {
        nameof(IBasicStrikePort<object>.ResolveImpactTarget),
        nameof(IBasicStrikePort<object>.ResetClosingChainBonus),
        nameof(IBasicStrikePort<object>.ResolveSkill),
        nameof(IBasicStrikePort<object>.IsHealSkill),
        nameof(IBasicStrikePort<object>.WorldTimeMs),
        nameof(IBasicStrikePort<object>.BasicCadenceReady),
        nameof(IBasicStrikePort<object>.SetLastBasicStrikeMs),
        nameof(IBasicStrikePort<object>.BasicHitsNow),
        nameof(IBasicStrikePort<object>.BasicStrikeReachM),
        nameof(IBasicStrikePort<object>.IsBossInStrikeCapsule),
        nameof(IBasicStrikePort<object>.EvaluateBasicInReach),
        nameof(IBasicStrikePort<object>.BasicStrikeArcDeg),
        nameof(IBasicStrikePort<object>.BasicStrikeYawDeg),
        nameof(IBasicStrikePort<object>.HasLivingLogic),
        nameof(IBasicStrikePort<object>.ApplyBossClosingBasic),
        nameof(IBasicStrikePort<object>.ApplyBasicStrikeDamage),
        nameof(IBasicStrikePort<object>.ScheduleBasicSubHits),
        nameof(IBasicStrikePort<object>.ApplyBasicExtras),
        nameof(IBasicStrikePort<object>.TryLandWeaponStunBasic),
        nameof(IBasicStrikePort<object>.SpawnClosingImpact),
        nameof(IBasicStrikePort<object>.HasLivingLogic),
        nameof(IBasicStrikePort<object>.TryCannonBlast),
    };

    static readonly string[] ExpectedBasicMissOrder =
    {
        nameof(IBasicStrikePort<object>.ResolveImpactTarget),
        nameof(IBasicStrikePort<object>.ResetClosingChainBonus),
        nameof(IBasicStrikePort<object>.ResolveSkill),
        nameof(IBasicStrikePort<object>.IsHealSkill),
        nameof(IBasicStrikePort<object>.WorldTimeMs),
        nameof(IBasicStrikePort<object>.BasicCadenceReady),
        nameof(IBasicStrikePort<object>.SetLastBasicStrikeMs),
        nameof(IBasicStrikePort<object>.BasicHitsNow),
        nameof(IBasicStrikePort<object>.BasicStrikeReachM),
        nameof(IBasicStrikePort<object>.IsBossInStrikeCapsule),
        nameof(IBasicStrikePort<object>.EvaluateBasicInReach),
        nameof(IBasicStrikePort<object>.BasicStrikeArcDeg),
        nameof(IBasicStrikePort<object>.BasicStrikeYawDeg),
        nameof(IBasicStrikePort<object>.SpawnClosingImpact),
        nameof(IBasicStrikePort<object>.HasLivingLogic),
        nameof(IBasicStrikePort<object>.TryCannonBlast),
    };

    sealed class RecordingBasicPort : IBasicStrikePort<int>
    {
        public readonly List<string> Calls = new();
        public SkillResolution ResolveResult = SkillResolution.Empty;
        public bool HealSkill;
        public bool CadenceReady = true;
        public bool HasLogic = true;
        public bool StrikeConnects;

        void Record([System.Runtime.CompilerServices.CallerMemberName] string name = "") =>
            Calls.Add(name);

        public void ResolveImpactTarget(int ctx) => Record();
        public void ResetClosingChainBonus() => Record();
        public SkillResolution ResolveSkill(int ctx)
        {
            Record();
            return ResolveResult;
        }

        public bool IsHealSkill(SkillResolution skill)
        {
            Record();
            return HealSkill;
        }

        public void ApplyClosingStatuses(int ctx, SkillResolution skill) => Record();
        public void ApplyClosingHeal(int ctx, SkillResolution skill) => Record();

        public double WorldTimeMs()
        {
            Record();
            return 1000;
        }

        public bool BasicCadenceReady(double now)
        {
            Record();
            return CadenceReady;
        }

        public void SetLastBasicStrikeMs(double now) => Record();
        public int BasicHitsNow()
        {
            Record();
            return 1;
        }

        public float BasicStrikeReachM()
        {
            Record();
            return 5f;
        }

        public bool IsBossInStrikeCapsule(int ctx, float reachM)
        {
            Record();
            return StrikeConnects;
        }

        public bool EvaluateBasicInReach(float reachM)
        {
            Record();
            return StrikeConnects;
        }

        public float BasicStrikeArcDeg()
        {
            Record();
            return 0f;
        }

        public float BasicStrikeYawDeg()
        {
            Record();
            return 0f;
        }

        public bool HasLivingLogic(int ctx)
        {
            Record();
            return HasLogic;
        }

        public void ApplyBossClosingBasic(int ctx) => Record();
        public float ApplyBasicStrikeDamage(int ctx, float effectScale)
        {
            Record();
            return 10f;
        }

        public void ScheduleBasicSubHits(int ctx, int hits, float reach) => Record();
        public void ApplyBasicExtras(float dealt, int hits) => Record();
        public void TryLandWeaponStunBasic() => Record();
        public void SpawnClosingImpact(int ctx) => Record();
        public void TryCannonBlast(int ctx) => Record();
    }

    [Test]
    public void RunBasic_HitPath_MatchesManifestationDirectorCallOrder()
    {
        var port = new RecordingBasicPort { StrikeConnects = true };
        var outcome = new CastPipeline().RunBasic(0, port);
        Assert.That(outcome.Connected, Is.True);
        Assert.That(outcome.Dealt, Is.EqualTo(10f));
        Assert.That(port.Calls, Is.EqualTo(ExpectedBasicHitOrder));
    }

    [Test]
    public void RunBasic_HealShortCircuitsBeforeCadence()
    {
        var port = new RecordingBasicPort { HealSkill = true };
        var outcome = new CastPipeline().RunBasic(0, port);
        Assert.That(outcome.Healed, Is.True);
        Assert.That(port.Calls, Is.EqualTo(new[]
        {
            nameof(IBasicStrikePort<object>.ResolveImpactTarget),
            nameof(IBasicStrikePort<object>.ResetClosingChainBonus),
            nameof(IBasicStrikePort<object>.ResolveSkill),
            nameof(IBasicStrikePort<object>.IsHealSkill),
            nameof(IBasicStrikePort<object>.ApplyClosingStatuses),
            nameof(IBasicStrikePort<object>.ApplyClosingHeal),
        }));
    }

    [Test]
    public void RunBasic_CadenceDenied_StopsBeforeStrike()
    {
        var port = new RecordingBasicPort { CadenceReady = false };
        var outcome = new CastPipeline().RunBasic(0, port);
        Assert.That(outcome.DeniedCadence, Is.True);
        Assert.That(port.Calls, Is.EqualTo(new[]
        {
            nameof(IBasicStrikePort<object>.ResolveImpactTarget),
            nameof(IBasicStrikePort<object>.ResetClosingChainBonus),
            nameof(IBasicStrikePort<object>.ResolveSkill),
            nameof(IBasicStrikePort<object>.IsHealSkill),
            nameof(IBasicStrikePort<object>.WorldTimeMs),
            nameof(IBasicStrikePort<object>.BasicCadenceReady),
        }));
    }

    [Test]
    public void RunBasic_Miss_ImpactAndCannonOnlyAfterConnectChecks()
    {
        var port = new RecordingBasicPort { StrikeConnects = false };
        var outcome = new CastPipeline().RunBasic(0, port);
        Assert.That(outcome.Connected, Is.False);
        Assert.That(port.Calls, Is.EqualTo(ExpectedBasicMissOrder));
    }

    [Test]
    public void RunBasic_NoLogic_SkipsBossBasicAndCannon()
    {
        var port = new RecordingBasicPort { StrikeConnects = true, HasLogic = false };
        new CastPipeline().RunBasic(0, port);
        Assert.That(port.Calls, Does.Not.Contain(nameof(IBasicStrikePort<object>.ApplyBossClosingBasic)));
        Assert.That(port.Calls, Does.Not.Contain(nameof(IBasicStrikePort<object>.TryCannonBlast)));
        Assert.That(port.Calls, Does.Contain(nameof(IBasicStrikePort<object>.ApplyBasicStrikeDamage)));
        Assert.That(port.Calls, Does.Contain(nameof(IBasicStrikePort<object>.SpawnClosingImpact)));
    }
}
