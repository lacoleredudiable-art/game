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

        // §5 Cümle + §3 dwell
        Assert.Multiple(() =>
        {
            Assert.That(t.Sentence.MaxSentenceDots, Is.EqualTo(4));
            Assert.That(t.Sentence.VerbWindowMs, Is.EqualTo(420));
            Assert.That(t.Sentence.Adjective1WindowMs, Is.EqualTo(360));
            Assert.That(t.Sentence.Adjective2WindowMs, Is.EqualTo(300));
            Assert.That(t.Sentence.DwellMs, Is.EqualTo(220));
            Assert.That(t.Sentence.DwellMaxStacks, Is.EqualTo(2));
        });

        // §5 cümle eğrisi tablosu
        Assert.Multiple(() =>
        {
            Assert.That(t.Sentence.Dot1DurationSec, Is.EqualTo(0.25f));
            Assert.That(t.Sentence.Dot1TotalEffect, Is.EqualTo(1.0f));
            Assert.That(t.Sentence.Dot1EffectPerSecond, Is.EqualTo(4.0f));
            Assert.That(t.Sentence.Dot1RecoverySec, Is.EqualTo(0.18f));

            Assert.That(t.Sentence.Dot2DurationSec, Is.EqualTo(0.50f));
            Assert.That(t.Sentence.Dot2TotalEffect, Is.EqualTo(2.4f));
            Assert.That(t.Sentence.Dot2EffectPerSecond, Is.EqualTo(4.8f));
            Assert.That(t.Sentence.Dot2RecoverySec, Is.EqualTo(0.26f));

            Assert.That(t.Sentence.Dot3DurationSec, Is.EqualTo(0.80f));
            Assert.That(t.Sentence.Dot3TotalEffect, Is.EqualTo(4.4f));
            Assert.That(t.Sentence.Dot3EffectPerSecond, Is.EqualTo(5.5f));
            Assert.That(t.Sentence.Dot3RecoverySec, Is.EqualTo(0.38f));

            Assert.That(t.Sentence.Dot4DurationSec, Is.EqualTo(1.20f));
            Assert.That(t.Sentence.Dot4TotalEffect, Is.EqualTo(7.0f));
            Assert.That(t.Sentence.Dot4EffectPerSecond, Is.EqualTo(5.8f));
            Assert.That(t.Sentence.Dot4RecoverySec, Is.EqualTo(0.55f));
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
            Assert.That(t.Grade.ReactionDisplaySec, Is.EqualTo(0.45f));
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

        Assert.That(t.Feel, Is.Not.Null);
    }
}
