using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class DrawFeedbackTests
{
    [Test]
    public void OnStrokeEnd_NotDrawing_ReturnsNone()
    {
        Assert.That(DrawFeedback.OnStrokeEnd(false, 0, DrawFeedback.DenialKind.None, false, 0f),
            Is.EqualTo(DrawFeedback.StrokeOutcome.None));
    }

    [Test]
    public void OnStrokeEnd_Cancelled_ReturnsNone()
    {
        Assert.That(DrawFeedback.OnStrokeEnd(true, 0, DrawFeedback.DenialKind.None, true, 100f),
            Is.EqualTo(DrawFeedback.StrokeOutcome.None));
    }

    [Test]
    public void OnStrokeEnd_AcceptedDots_ReturnsNone()
    {
        Assert.That(DrawFeedback.OnStrokeEnd(true, 1, DrawFeedback.DenialKind.None, false, 100f),
            Is.EqualTo(DrawFeedback.StrokeOutcome.None));
    }

    [Test]
    public void OnStrokeEnd_CooldownDenial_ReturnsCooldown()
    {
        Assert.That(DrawFeedback.OnStrokeEnd(true, 0, DrawFeedback.DenialKind.Cooldown, false, 100f),
            Is.EqualTo(DrawFeedback.StrokeOutcome.Cooldown));
    }

    [Test]
    public void OnStrokeEnd_OtherDenial_ReturnsNone()
    {
        Assert.That(DrawFeedback.OnStrokeEnd(true, 0, DrawFeedback.DenialKind.Other, false, 100f),
            Is.EqualTo(DrawFeedback.StrokeOutcome.None));
    }

    [Test]
    public void OnStrokeEnd_TooShortStroke_ReturnsTooShort()
    {
        Assert.That(DrawFeedback.OnStrokeEnd(true, 0, DrawFeedback.DenialKind.None, false, 10f),
            Is.EqualTo(DrawFeedback.StrokeOutcome.TooShort));
        Assert.That(DrawFeedback.OnStrokeEnd(true, 0, DrawFeedback.DenialKind.None, false,
                DrawFeedback.TooShortDp - 0.01f),
            Is.EqualTo(DrawFeedback.StrokeOutcome.TooShort));
    }

    [Test]
    public void OnStrokeEnd_LongEnoughUnrecognized_ReturnsUnrecognized()
    {
        Assert.That(DrawFeedback.OnStrokeEnd(true, 0, DrawFeedback.DenialKind.None, false, DrawFeedback.TooShortDp),
            Is.EqualTo(DrawFeedback.StrokeOutcome.Unrecognized));
        Assert.That(DrawFeedback.OnStrokeEnd(true, 0, DrawFeedback.DenialKind.None, false, 500f),
            Is.EqualTo(DrawFeedback.StrokeOutcome.Unrecognized));
    }

    [Test]
    public void OnStrokeEnd_LegacyOverload_DenialShown_DelegatesToOther()
    {
        Assert.That(DrawFeedback.OnStrokeEnd(true, 0, true, false),
            Is.EqualTo(DrawFeedback.StrokeOutcome.None));
    }

    [Test]
    public void OnStrokeEnd_LegacyOverload_NoDenial_DelegatesToUnrecognized()
    {
        Assert.That(DrawFeedback.OnStrokeEnd(true, 0, false, false),
            Is.EqualTo(DrawFeedback.StrokeOutcome.Unrecognized));
    }

    [Test]
    public void CaptionFor_MapsOutcomes()
    {
        Assert.That(DrawFeedback.CaptionFor(DrawFeedback.StrokeOutcome.None), Is.EqualTo(string.Empty));
        Assert.That(DrawFeedback.CaptionFor(DrawFeedback.StrokeOutcome.TooShort), Is.EqualTo(DrawFeedback.TooShort));
        Assert.That(DrawFeedback.CaptionFor(DrawFeedback.StrokeOutcome.Unrecognized),
            Is.EqualTo(DrawFeedback.Unrecognized));
        Assert.That(DrawFeedback.CaptionFor(DrawFeedback.StrokeOutcome.Cooldown),
            Is.EqualTo(DrawFeedback.OnCooldown));
    }
}
