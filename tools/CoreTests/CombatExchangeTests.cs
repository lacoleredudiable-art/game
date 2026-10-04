using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
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

    [TestCase(149, DodgeGrade.Mukemmel)]
    [TestCase(150, DodgeGrade.Harika)]
    [TestCase(190, DodgeGrade.Harika)]
    [TestCase(191, DodgeGrade.Temiz)]
    [TestCase(220, DodgeGrade.Temiz)]
    [TestCase(221, DodgeGrade.Siyirdi)]
    public void GradeFromGap_ThresholdsAreInclusiveAtBoundaries(int gapMs, DodgeGrade expected)
    {
        Assert.That(_resolver.GradeFromGap(gapMs), Is.EqualTo(expected));
    }

    /// <summary>
    /// Dört derecenin de gerçekten üretilebildiğini `Resolve` üzerinden doğrular.
    /// Eşikleri doğrudan sorgulamak yetmez: eşik i-frame penceresinin dışına düşerse
    /// `GradeFromGap` yine doğru cevabı verir ama oyunda o derece hiç oluşmaz.
    /// </summary>
    [TestCase(60, DodgeGrade.Mukemmel)]
    [TestCase(170, DodgeGrade.Harika)]
    [TestCase(205, DodgeGrade.Temiz)]
    [TestCase(240, DodgeGrade.Siyirdi)]
    public void Resolve_ProducesEveryGrade_WithinIframeWindow(int gapMs, DodgeGrade expected)
    {
        int press = StrikeTime - gapMs;
        _dodge.Begin(press);
        Assert.That(_dodge.IsInvulnerable(StrikeTime), Is.True, $"gap {gapMs} pencerenin içinde olmalı");

        var result = _resolver.Resolve(InVolume(TelegraphStart, StrikeTime, press));

        Assert.That(result.Outcome, Is.EqualTo(ExchangeOutcome.Dodged));
        Assert.That(result.Grade, Is.EqualTo(expected));
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

        var result = _resolver.Resolve(InVolume(TelegraphStart, StrikeTime, press));
        Assert.That(result.Outcome, Is.EqualTo(ExchangeOutcome.Dodged));
    }

    [Test]
    public void IframeExpiredBeforeStrike_Hits()
    {
        int press = StrikeTime - 300;
        _dodge.Begin(press);

        Assert.That(_dodge.IsInvulnerable(StrikeTime), Is.False);

        var result = _resolver.Resolve(InVolume(TelegraphStart, StrikeTime, press));
        Assert.That(result.Outcome, Is.EqualTo(ExchangeOutcome.Hit));
        Assert.That(result.Reason, Is.EqualTo(HitReason.ErkenBastin));
        Assert.That(result.HitReasonText, Is.EqualTo("erken bastın"));
    }

    [Test]
    public void IframeEndIsExclusive_StrikeOnEndIsHit()
    {
        int iframe = new DodgeTuning().IframeMs;
        int press = StrikeTime - iframe;
        _dodge.Begin(press);

        Assert.That(_dodge.IframeEndMs(press), Is.EqualTo(press + iframe));
        Assert.That(_dodge.IsInvulnerable(press + iframe), Is.False);

        var result = _resolver.Resolve(InVolume(TelegraphStart, StrikeTime, press));
        Assert.That(result.Outcome, Is.EqualTo(ExchangeOutcome.Hit));
        Assert.That(result.Reason, Is.EqualTo(HitReason.ErkenBastin));
    }

    [Test]
    public void PressAfterStrike_HitsWithGecKaldin()
    {
        // Vuruş geçtikten sonra basmak "geç kaldın"dır; i-frame penceresi vuruşun
        // sonrasında açıldığı için "erken bastın" demek oyuncuya yanlış sebep gösterir.
        int press = StrikeTime + 50;
        var result = _resolver.Resolve(InVolume(TelegraphStart, StrikeTime, press));

        Assert.That(result.Outcome, Is.EqualTo(ExchangeOutcome.Hit));
        Assert.That(result.Reason, Is.EqualTo(HitReason.GecKaldin));
        Assert.That(result.HitReasonText, Is.EqualTo("geç kaldın"));
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
        var tuning = new DodgeTuning();
        int endOfDuration = tuning.StartupMs + tuning.DurationMs;
        Assert.That(_dodge.GetDisplacementRatio(endOfDuration), Is.EqualTo(1f).Within(1e-6f));
    }

    [Test]
    public void DodgeState_GlideVelocityDecaysAfterDuration()
    {
        _dodge.Begin(0);
        var tuning = new DodgeTuning();
        int moveEnd = tuning.StartupMs + tuning.DurationMs;

        Assert.That(_dodge.GetGlideVelocityRatio(moveEnd), Is.EqualTo(0f).Within(1e-6f));
        Assert.That(_dodge.GetGlideVelocityRatio(moveEnd + 1), Is.GreaterThan(0f));
        Assert.That(_dodge.GetGlideVelocityRatio(moveEnd + 120), Is.EqualTo(0f).Within(1e-6f));
    }
}
