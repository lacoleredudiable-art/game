using Dovus.App.Casting;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Tuning;
using NUnit.Framework;
using System.Linq;

namespace CoreTests;

/// <summary>G: dilim girdisi — cümle tavanı 2, fazla nokta yok sayılır, merkezde kalkış iptal, çizerken hareket serbest.</summary>
[TestFixture]
public class RuleEngineV4InputTests
{
    const int LegacyMaxDots = 4;

    bool _previous;

    [SetUp]
    public void SetUp() => _previous = RuleEngineV4Feature.Enabled;

    [TearDown]
    public void TearDown() => RuleEngineV4Feature.Enabled = _previous;

    static SentenceEngine SliceEngine(out SentenceTuning tuning)
    {
        tuning = new SentenceTuning { MaxSentenceDots = RuleEngineV4Feature.SentenceDotCap(LegacyMaxDots) };
        return new SentenceEngine(tuning);
    }

    static int[] DotsOf(System.Collections.Generic.IReadOnlyList<SentenceWord> words) =>
        words.Select(w => w.Dot).ToArray();

    [Test]
    public void SentenceDotCap_FlagOn_IsVerbAdjective_FlagOff_Legacy()
    {
        RuleEngineV4Feature.Enabled = true;
        Assert.That(RuleEngineV4Feature.SentenceDotCap(LegacyMaxDots),
            Is.EqualTo(RuleEngineV4CastContext.VerbAdjectiveComboLen));
        RuleEngineV4Feature.Enabled = false;
        Assert.That(RuleEngineV4Feature.SentenceDotCap(LegacyMaxDots), Is.EqualTo(LegacyMaxDots));
    }

    [Test]
    public void FlagOn_SecondDot_ResolvesImmediately_NoWindowWait()
    {
        RuleEngineV4Feature.Enabled = true;
        var engine = SliceEngine(out _);

        engine.OnDotTouched(1, 0);
        Assert.That(engine.History, Is.Empty);
        engine.OnDotTouched(2, 10);

        Assert.That(engine.History, Has.Count.EqualTo(1), "2. noktada kombo o an çözülmeli");
        Assert.That(engine.History[0].Phase, Is.EqualTo(SentencePhase.Resolved));
        Assert.That(DotsOf(engine.History[0].Words), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(engine.History[0].Closing!.Value.DotCount, Is.EqualTo(2));
        Assert.That(engine.State.Phase, Is.Not.EqualTo(SentencePhase.Building));
    }

    [Test]
    public void FlagOff_AtCapacity_ResolvesAndThirdDotStartsNewSentence()
    {
        RuleEngineV4Feature.Enabled = false;
        var engine = new SentenceEngine(new SentenceTuning { MaxSentenceDots = 2 });

        engine.OnDotTouched(1, 0);
        engine.OnDotTouched(2, 10);
        Assert.That(engine.History, Has.Count.EqualTo(1), "eski davranış: tavanda anında çözülür");
        engine.OnDotTouched(3, 20);
        Assert.That(DotsOf(engine.State.Words), Is.EqualTo(new[] { 3 }));
    }

    [Test]
    public void FlagOn_CenterReleaseWithOneDot_AbortsWithoutClosing()
    {
        RuleEngineV4Feature.Enabled = true;
        var engine = SliceEngine(out _);
        engine.OnDotTouched(1, 0);

        Assert.That(RuleEngineV4Feature.CenterReleaseAbortsSentence(releasedOnCenter: false, engine.State), Is.False);
        Assert.That(RuleEngineV4Feature.CenterReleaseAbortsSentence(releasedOnCenter: true, engine.State), Is.True);

        engine.Abort();
        Assert.That(engine.History, Has.Count.EqualTo(1));
        Assert.That(engine.History[0].Phase, Is.EqualTo(SentencePhase.Aborted));
        Assert.That(engine.History[0].Closing, Is.Null, "iptal skill üretmemeli");
    }

    [Test]
    public void FlagOn_AfterSecondDot_CenterReleaseDoesNotCancel()
    {
        RuleEngineV4Feature.Enabled = true;
        var engine = SliceEngine(out _);
        engine.OnDotTouched(1, 0);
        engine.OnDotTouched(2, 10);

        Assert.That(RuleEngineV4Feature.CenterReleaseAbortsSentence(true, engine.State), Is.False,
            "2. noktadan sonra iptal yok");
        Assert.That(engine.History[0].Phase, Is.EqualTo(SentencePhase.Resolved));
    }

    [Test]
    public void CenterRelease_NoSentenceOrFlagOff_DoesNotAbort()
    {
        RuleEngineV4Feature.Enabled = true;
        var idle = SliceEngine(out _);
        Assert.That(RuleEngineV4Feature.CenterReleaseAbortsSentence(true, idle.State), Is.False, "çizim yok");

        RuleEngineV4Feature.Enabled = false;
        var engine = new SentenceEngine(new SentenceTuning());
        engine.OnDotTouched(1, 0);
        Assert.That(RuleEngineV4Feature.CenterReleaseAbortsSentence(true, engine.State), Is.False, "bayrak kapalı");
    }

    [Test]
    public void FlagOn_CenterTapWithOneDot_Confirms()
    {
        RuleEngineV4Feature.Enabled = true;
        var engine = SliceEngine(out _);
        engine.OnDotTouched(1, 0);

        engine.Commit();

        Assert.That(engine.History, Has.Count.EqualTo(1));
        Assert.That(engine.History[0].Phase, Is.EqualTo(SentencePhase.Resolved));
        Assert.That(engine.History[0].Closing!.Value.DotCount, Is.EqualTo(1));
    }

    [Test]
    public void LaunchAndStrokeRules_OnlyWhenFlagOn()
    {
        RuleEngineV4Feature.Enabled = true;
        Assert.That(RuleEngineV4Feature.LaunchOnResolve, Is.True);
        Assert.That(RuleEngineV4Feature.IgnoreDotsAfterCapInStroke, Is.True);
        RuleEngineV4Feature.Enabled = false;
        Assert.That(RuleEngineV4Feature.LaunchOnResolve, Is.False);
        Assert.That(RuleEngineV4Feature.IgnoreDotsAfterCapInStroke, Is.False);
    }

    [Test]
    public void CastGate_AtCapacity_StillStartsNewSentence()
    {
        RuleEngineV4Feature.Enabled = true;
        Assert.That(CastGateRules.WouldStartSentence(SentencePhase.Building, 2, 2), Is.True);
    }

    [Test]
    public void Drawing_BlocksMove_OnlyWhenFlagOff()
    {
        var sm = new PlayerStateMachine(new[]
        {
            new PlayerStateNode("idle", "true", "true", "true", false),
            new PlayerStateNode(PlayerStateMachine.DrawingStateId, "partial", "false", "true", false),
            new PlayerStateNode("rooted", "true", "false", "true", false),
        });

        sm.SyncWorld(false, false, false, false, false, isDrawing: true, false);
        RuleEngineV4Feature.Enabled = false;
        Assert.That(sm.BlocksMove, Is.True, "eski davranış: çizerken kök");
        RuleEngineV4Feature.Enabled = true;
        Assert.That(sm.BlocksMove, Is.False, "v4: çizerken tam hız");

        sm.SyncWorld(false, false, false, isRooted: true, false, false, false);
        Assert.That(sm.BlocksMove, Is.True, "kök durumu yine kökler");
    }
}
