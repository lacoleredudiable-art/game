using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class CombatTuningDefaultsTests
{
    [Test]
    public void DefaultValues_MatchDovusSistemiSpec()
    {
        var t = new CombatTuning();

        // §6 Dodge
        Assert.Multiple(() =>
        {
            Assert.That(t.Dodge.StartupMs, Is.EqualTo(20));
            Assert.That(t.Dodge.IframeStartMs, Is.EqualTo(0));
            Assert.That(t.Dodge.IframeMs, Is.EqualTo(260));
            Assert.That(t.Dodge.DistanceM, Is.EqualTo(3.8f));
            Assert.That(t.Dodge.DurationMs, Is.EqualTo(260));
            Assert.That(t.Dodge.CurveExp, Is.EqualTo(3.2f));
            Assert.That(t.Dodge.GlideTailMs, Is.EqualTo(220));
            Assert.That(t.Dodge.CooldownMs, Is.EqualTo(420));
        });

        // §2 merkez dokunma (dodge girdisi)
        Assert.Multiple(() =>
        {
            Assert.That(t.Dodge.TapMaxMs, Is.EqualTo(180));
            Assert.That(t.Dodge.TapMaxMoveDp, Is.EqualTo(12));
        });

        // §5 Cümle + §3 bekletme
        Assert.Multiple(() =>
        {
            Assert.That(t.Sentence.MaxSentenceDots, Is.EqualTo(4));
            Assert.That(t.Sentence.CancelWindowMs, Is.EqualTo(new[] { 420, 360, 300 }));
            Assert.That(t.Sentence.DwellMs, Is.EqualTo(220));
            Assert.That(t.Sentence.DwellMaxStacks, Is.EqualTo(2));
        });

        // §5 cümle eğrisi tablosu
        var expected = new[]
        {
            (dots: 1, duration: 0.25f, effect: 1.0f, recovery: 0.18f),
            (dots: 2, duration: 0.50f, effect: 2.4f, recovery: 0.26f),
            (dots: 3, duration: 0.80f, effect: 4.4f, recovery: 0.38f),
            (dots: 4, duration: 1.20f, effect: 7.0f, recovery: 0.55f),
        };

        Assert.Multiple(() =>
        {
            foreach (var (dots, duration, effect, recovery) in expected)
            {
                var step = t.Sentence.StepForDots(dots);
                Assert.That(step.DurationSec, Is.EqualTo(duration), $"{dots} nokta süresi");
                Assert.That(step.TotalEffect, Is.EqualTo(effect), $"{dots} nokta etkisi");
                Assert.That(step.RecoverySec, Is.EqualTo(recovery), $"{dots} nokta toparlanması");
            }
        });

        // §7 Yavaş çekim
        Assert.Multiple(() =>
        {
            Assert.That(t.Slowmo.Factor, Is.EqualTo(0.22f));
            Assert.That(t.Slowmo.RampDownMs, Is.EqualTo(55));
            Assert.That(t.Slowmo.HoldMs, Is.EqualTo(190));
            Assert.That(t.Slowmo.RampUpMs, Is.EqualTo(420));
            Assert.That(t.Slowmo.AudioLowpassHz, Is.EqualTo(700));
            Assert.That(t.Slowmo.SlowmoMinGrade, Is.EqualTo(DodgeGrade.Temiz));
            Assert.That(t.Slowmo.SlowmoBonusDots, Is.EqualTo(0));
        });

        // §6 Derecelendirme
        Assert.Multiple(() =>
        {
            Assert.That(t.Grade.MukemmelGapMaxMs, Is.EqualTo(110));
            Assert.That(t.Grade.HarikaGapMaxMs, Is.EqualTo(200));
            Assert.That(t.Grade.TemizGapMaxMs, Is.EqualTo(320));
        });

        // §11 Boss
        Assert.Multiple(() =>
        {
            Assert.That(t.Boss.WindupMs, Is.EqualTo(640));
            Assert.That(t.Boss.ActiveMs, Is.EqualTo(90));
            Assert.That(t.Boss.RecoveryMs, Is.EqualTo(720));
            Assert.That(t.Boss.RadiusM, Is.EqualTo(5.4f));
            Assert.That(t.Boss.Damage, Is.EqualTo(22));
            Assert.That(t.Boss.IdleMinMs, Is.EqualTo(700));
            Assert.That(t.Boss.IdleMaxMs, Is.EqualTo(1500));
            Assert.That(t.Boss.ApproachSpeedMps, Is.EqualTo(2.2f));
            Assert.That(t.Boss.RespawnMaxSec, Is.EqualTo(2.0f));
        });

        // §8 His katmanı başlangıç sayıları
        Assert.Multiple(() =>
        {
            Assert.That(t.Feel.HitstopPerfectMs, Is.EqualTo(90));
            Assert.That(t.Feel.HitstopPlayerHitMs, Is.EqualTo(130));
            Assert.That(t.Feel.ImpactFrameMs, Is.EqualTo(33));
            Assert.That(t.Feel.PostHitSilenceMs, Is.EqualTo(120));
            Assert.That(t.Feel.AfterimageCount, Is.EqualTo(7));
        });
    }

    /// <summary>
    /// Saniyedeki etki türetilir, saklanmaz — §5 eğrisinin şekli kasıtlı: uzatmanın
    /// karşılığı var ama kazanç düzleşiyor.
    /// </summary>
    [Test]
    public void EffectPerSecond_IsDerived_AndFlattensAsSentenceGrows()
    {
        var s = new SentenceTuning();

        Assert.That(s.StepForDots(1).EffectPerSecond, Is.EqualTo(4.0f).Within(0.01f));
        Assert.That(s.StepForDots(2).EffectPerSecond, Is.EqualTo(4.8f).Within(0.01f));
        Assert.That(s.StepForDots(3).EffectPerSecond, Is.EqualTo(5.5f).Within(0.01f));
        Assert.That(s.StepForDots(4).EffectPerSecond, Is.EqualTo(5.83f).Within(0.01f));

        var gains = new[]
        {
            s.StepForDots(2).EffectPerSecond - s.StepForDots(1).EffectPerSecond,
            s.StepForDots(3).EffectPerSecond - s.StepForDots(2).EffectPerSecond,
            s.StepForDots(4).EffectPerSecond - s.StepForDots(3).EffectPerSecond,
        };

        Assert.That(gains[0], Is.GreaterThan(0f), "uzatmanın karşılığı olmalı");
        Assert.That(gains[1], Is.LessThan(gains[0]), "kazanç azalan getirili olmalı");
        Assert.That(gains[2], Is.LessThan(gains[1]), "dördüncü nokta neredeyse hiçbir şey katmaz");
    }

    [Test]
    public void CancelWindow_ShrinksThenCloses()
    {
        var s = new SentenceTuning();

        Assert.That(s.CancelWindowForDots(1), Is.EqualTo(420));
        Assert.That(s.CancelWindowForDots(2), Is.EqualTo(360));
        Assert.That(s.CancelWindowForDots(3), Is.EqualTo(300));
        Assert.That(s.CancelWindowForDots(4), Is.EqualTo(0), "sıfat yuvası dolduysa pencere yok");
    }
}
