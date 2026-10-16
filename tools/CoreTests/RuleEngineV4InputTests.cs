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
    static SentenceEngine SliceEngine(out SentenceTuning tuning)
    {
        tuning = new SentenceTuning { MaxSentenceDots = RuleEngineV4CastContext.VerbAdjectiveComboLen };
        return new SentenceEngine(tuning);
    }

    static int[] DotsOf(System.Collections.Generic.IReadOnlyList<SentenceWord> words) =>
        words.Select(w => w.Dot).ToArray();

    [Test]
    public void SentenceDotCap_IsVerbAdjectiveComboLen()
    {
        Assert.That(RuleEngineV4CastContext.VerbAdjectiveComboLen, Is.EqualTo(2));
    }

    [Test]
    public void SecondDot_ResolvesImmediately_NoWindowWait()
    {
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
    public void CenterReleaseWithOneDot_AbortsWithoutClosing()
    {
        var engine = SliceEngine(out _);
        engine.OnDotTouched(1, 0);

        Assert.That(RuleEngineV4CastContext.ShouldAbortSentenceOnCenterRelease(releasedOnCenter: false, engine.State), Is.False);
        Assert.That(RuleEngineV4CastContext.ShouldAbortSentenceOnCenterRelease(releasedOnCenter: true, engine.State), Is.True);

        engine.Abort();
        Assert.That(engine.History, Has.Count.EqualTo(1));
        Assert.That(engine.History[0].Phase, Is.EqualTo(SentencePhase.Aborted));
        Assert.That(engine.History[0].Closing, Is.Null, "iptal skill üretmemeli");
    }

    [Test]
    public void AfterSecondDot_CenterReleaseDoesNotCancel()
    {
        var engine = SliceEngine(out _);
        engine.OnDotTouched(1, 0);
        engine.OnDotTouched(2, 10);

        Assert.That(RuleEngineV4CastContext.ShouldAbortSentenceOnCenterRelease(true, engine.State), Is.False,
            "2. noktadan sonra iptal yok");
        Assert.That(engine.History[0].Phase, Is.EqualTo(SentencePhase.Resolved));
    }

    [Test]
    public void CenterRelease_NoBuildingSentence_DoesNotAbort()
    {
        var idle = SliceEngine(out _);
        Assert.That(RuleEngineV4CastContext.ShouldAbortSentenceOnCenterRelease(true, idle.State), Is.False, "çizim yok");
    }

    [Test]
    public void CenterTapWithOneDot_Confirms()
    {
        var engine = SliceEngine(out _);
        engine.OnDotTouched(1, 0);

        engine.Commit();

        Assert.That(engine.History, Has.Count.EqualTo(1));
        Assert.That(engine.History[0].Phase, Is.EqualTo(SentencePhase.Resolved));
        Assert.That(engine.History[0].Closing!.Value.DotCount, Is.EqualTo(1));
    }

    [Test]
    public void CastGate_AtCapacity_StillStartsNewSentence()
    {
        Assert.That(CastGateRules.WouldStartSentence(SentencePhase.Building, 2, 2), Is.True);
    }

    [Test]
    public void Drawing_DoesNotBlockMove_RootedStillBlocks()
    {
        var sm = new PlayerStateMachine(new[]
        {
            new PlayerStateNode("idle", "true", "true", "true", false),
            new PlayerStateNode(PlayerStateMachine.DrawingStateId, "partial", "false", "true", false),
            new PlayerStateNode("rooted", "true", "false", "true", false),
        });

        sm.SyncWorld(false, false, false, false, false, isDrawing: true, false);
        Assert.That(sm.BlocksMove, Is.False, "v4: çizerken tam hız");

        sm.SyncWorld(false, false, false, isRooted: true, false, false, false);
        Assert.That(sm.BlocksMove, Is.True, "kök durumu yine kökler");
    }
}
