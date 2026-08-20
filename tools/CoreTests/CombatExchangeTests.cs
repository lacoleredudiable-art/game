using Dovus.Core.Combat;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class CombatExchangeTests
{
    const int TelegraphStart = 1000;
    const int StrikeTime = TelegraphStart + 640;

    ExchangeResolver _resolver = null!;
    DodgeState _dodge = null!;
    BossAttack _boss = null!;

    [SetUp]
    public void SetUp()
    {
        _resolver = new ExchangeResolver();
        _dodge = new DodgeState();
        _boss = new BossAttack();
    }

    static ExchangeInput InVolume(int telegraphStart, int strikeTime, int? dodgePress) =>
        new()
        {
            TelegraphStartMs = telegraphStart,
            StrikeTimeMs = strikeTime,
            DodgePressMs = dodgePress,
            InEffectVolume = true
        };

    [TestCase(110, DodgeGrade.Mukemmel)]
    [TestCase(111, DodgeGrade.Harika)]
    [TestCase(200, DodgeGrade.Harika)]
    [TestCase(201, DodgeGrade.Temiz)]
    [TestCase(320, DodgeGrade.Temiz)]
    [TestCase(321, DodgeGrade.Siyirdi)]
    public void GradeFromGap_ThresholdsAreInclusiveAtBoundaries(int gapMs, DodgeGrade expected)
    {
        Assert.That(_resolver.GradeFromGap(gapMs), Is.EqualTo(expected));
    }

    [Test]
    public void Resolve_GradeUsesGapWhenIframeCoversStrike()
    {
        int press = StrikeTime - 150;
        var result = _resolver.Resolve(InVolume(TelegraphStart, StrikeTime, press));

        Assert.That(result.Outcome, Is.EqualTo(ExchangeOutcome.Dodged));
        Assert.That(result.Grade, Is.EqualTo(DodgeGrade.Harika));
        Assert.That(result.GapMs, Is.EqualTo(150));
    }

    [Test]
    public void Reaction_IsDodgePressMinusTelegraphStart()
    {
        int press = StrikeTime - 150;
        var result = _resolver.Resolve(InVolume(TelegraphStart, StrikeTime, press));

        Assert.That(result.ReactionMs, Is.EqualTo(press - TelegraphStart));
        Assert.That(result.ReactionMs, Is.EqualTo(490));
    }

    [Test]
    public void IframeCoversStrike_Dodges()
    {
        int press = StrikeTime - 100;
        _dodge.Begin(press);

        Assert.That(_dodge.IsInvulnerable(StrikeTime), Is.True);
        Assert.That(_resolver.IsInvulnerableAtStrike(press, StrikeTime), Is.True);

        var result = _resolver.Resolve(InVolume(TelegraphStart, StrikeTime, press));
        Assert.That(result.Outcome, Is.EqualTo(ExchangeOutcome.Dodged));
    }

    [Test]
    public void IframeExpiredBeforeStrike_Hits()
    {
        int press = StrikeTime - 300;
        _dodge.Begin(press);

        Assert.That(_dodge.IsInvulnerable(StrikeTime), Is.False);
        Assert.That(_resolver.IsInvulnerableAtStrike(press, StrikeTime), Is.False);

        var result = _resolver.Resolve(InVolume(TelegraphStart, StrikeTime, press));
        Assert.That(result.Outcome, Is.EqualTo(ExchangeOutcome.Hit));
        Assert.That(result.Reason, Is.EqualTo(HitReason.ErkenBastin));
        Assert.That(result.HitReasonText, Is.EqualTo("erken bastın"));
    }

    [Test]
    public void IframeEndIsExclusive_StrikeOnEndIsHit()
    {
        int press = StrikeTime - 260;
        _dodge.Begin(press);

        Assert.That(_dodge.IframeEndMs(press), Is.EqualTo(press + 260));
        Assert.That(_dodge.IsInvulnerable(press + 260), Is.False);

        var result = _resolver.Resolve(InVolume(TelegraphStart, StrikeTime, press));
        Assert.That(result.Outcome, Is.EqualTo(ExchangeOutcome.Hit));
        Assert.That(result.Reason, Is.EqualTo(HitReason.ErkenBastin));
    }

    [Test]
    public void NoDodgePress_HitsWithGecKaldin()
    {
        var result = _resolver.Resolve(InVolume(TelegraphStart, StrikeTime, null));

        Assert.That(result.Outcome, Is.EqualTo(ExchangeOutcome.Hit));
        Assert.That(result.Reason, Is.EqualTo(HitReason.GecKaldin));
        Assert.That(result.HitReasonText, Is.EqualTo("geç kaldın"));
    }

    [Test]
    public void OutsideVolume_IsSafeWithoutGrade()
    {
        var input = new ExchangeInput
        {
            TelegraphStartMs = TelegraphStart,
            StrikeTimeMs = StrikeTime,
            DodgePressMs = StrikeTime - 50,
            InEffectVolume = false
        };

        var result = _resolver.Resolve(input);
        Assert.That(result.Outcome, Is.EqualTo(ExchangeOutcome.Safe));
        Assert.That(result.Grade, Is.Null);
    }

    [Test]
    public void DodgeCurve_IsMonotonicAndReachesFullDistanceAtOne()
    {
        float prev = 0f;
        for (int i = 0; i <= 100; i++)
        {
            float u = i / 100f;
            float s = _dodge.EvaluateCurve(u);
            Assert.That(s, Is.GreaterThanOrEqualTo(prev - 1e-6f), $"u={u}");
            prev = s;
        }

        Assert.That(_dodge.EvaluateCurve(1f), Is.EqualTo(1f).Within(1e-6f));
    }

    [Test]
    public void BossAttack_StrikeAtWindupEnd()
    {
        Assert.That(_boss.StrikeTimeMs(TelegraphStart), Is.EqualTo(TelegraphStart + 640));
        Assert.That(_boss.ActiveEndMs(TelegraphStart), Is.EqualTo(TelegraphStart + 730));
        Assert.That(_boss.RecoveryEndMs(TelegraphStart), Is.EqualTo(TelegraphStart + 1450));
    }

    [Test]
    public void BossAttack_EffectVolume_UsesDistanceAndAngle()
    {
        Assert.That(_boss.IsInEffectVolume(5.4f, 0f), Is.True);
        Assert.That(_boss.IsInEffectVolume(5.41f, 0f), Is.False);

        Assert.That(_boss.IsInEffectVolume(3f, 45f, arcHalfAngleDeg: 90f), Is.True);
        Assert.That(_boss.IsInEffectVolume(3f, 95f, arcHalfAngleDeg: 90f), Is.False);
    }

    [Test]
    public void DodgeState_DisplacementReachesOneAfterDuration()
    {
        _dodge.Begin(0);
        int endOfDuration = 20 + 260;
        Assert.That(_dodge.GetDisplacementRatio(endOfDuration), Is.EqualTo(1f).Within(1e-6f));
    }

    [Test]
    public void DodgeState_GlideVelocityDecaysAfterDuration()
    {
        _dodge.Begin(0);
        int moveEnd = 20 + 260;

        Assert.That(_dodge.GetGlideVelocityRatio(moveEnd), Is.EqualTo(0f).Within(1e-6f));
        Assert.That(_dodge.GetGlideVelocityRatio(moveEnd + 1), Is.GreaterThan(0f));
        Assert.That(_dodge.GetGlideVelocityRatio(moveEnd + 220), Is.EqualTo(0f).Within(1e-6f));
    }
}
