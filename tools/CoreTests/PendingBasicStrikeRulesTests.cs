using Dovus.App.Casting;
using Dovus.Core.Input;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using NUnit.Framework;
using System.Collections.Generic;

namespace CoreTests;

[TestFixture]
public sealed class PendingBasicStrikeRulesTests
{
    [Test]
    public void IsPendingBasic_FlagOrViewWins()
    {
        Assert.That(
            PendingBasicStrikeRules.IsPendingBasic(true, false, null, 1),
            Is.True);
        Assert.That(
            PendingBasicStrikeRules.IsPendingBasic(false, true, null, 1),
            Is.True);
    }

    [Test]
    public void IsPendingBasic_SingleWordMatchesDot()
    {
        var words = new List<SentenceWord> { new(Rune.Saldiri, JumpKind.None, 0) };
        Assert.That(
            PendingBasicStrikeRules.IsPendingBasic(false, false, words, (int)Rune.Saldiri),
            Is.True);
        Assert.That(
            PendingBasicStrikeRules.IsPendingBasic(false, false, words, 99),
            Is.False);
    }

    [Test]
    public void IsPendingBasic_MultiWordNotBasicUnlessFlagged()
    {
        var words = new List<SentenceWord>
        {
            new(Rune.Saldiri, JumpKind.None, 0),
            new(Rune.Patlama, JumpKind.None, 0),
        };
        Assert.That(
            PendingBasicStrikeRules.IsPendingBasic(false, false, words, (int)Rune.Saldiri),
            Is.False);
    }
}
