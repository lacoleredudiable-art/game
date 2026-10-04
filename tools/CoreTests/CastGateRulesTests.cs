using Dovus.App.Casting;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class CastGateRulesTests
{
    [Test]
    public void SentenceStart_BlocksWhenManaInsufficient()
    {
        var block = CastGateRules.SentenceStartBlockReason(
            SentencePhase.Idle, 0, 2,
            enforceResourceCost: true, hasResource: true, canAffordCost: false,
            enforceCooldown: true, hasCooldown: true, canStartGlobal: true);
        Assert.That(block, Is.EqualTo(CastGateRules.SentenceStartBlock.InsufficientMana));
        Assert.That(CastGateRules.TryAllowSentenceStart(
            SentencePhase.Idle, 0, 2,
            true, true, false,
            true, true, true), Is.False);
    }

    [Test]
    public void SentenceStart_BlocksOnGlobalCooldown()
    {
        var block = CastGateRules.SentenceStartBlockReason(
            SentencePhase.Recovering, 0, 2,
            enforceResourceCost: true, hasResource: true, canAffordCost: true,
            enforceCooldown: true, hasCooldown: true, canStartGlobal: false);
        Assert.That(block, Is.EqualTo(CastGateRules.SentenceStartBlock.GlobalCooldown));
    }

    [Test]
    public void ComboCooldown_BlocksSecondDotWhenComboOnCooldown()
    {
        Assert.That(CastGateRules.TryAllowComboCooldownForNextDot(
            enforceCooldown: true, hasCooldown: true,
            SentencePhase.Building, wordCount: 1,
            skillIsEmpty: false, comboKeyEmpty: false, canStartCombo: false), Is.False);
        Assert.That(CastGateRules.TryAllowComboCooldownForNextDot(
            true, true, SentencePhase.Building, 1,
            false, false, true), Is.True);
    }

    [Test]
    public void SentenceStart_AllowsWhenEnforcementOff()
    {
        Assert.That(CastGateRules.TryAllowSentenceStart(
            SentencePhase.Idle, 0, 2,
            enforceResourceCost: false, hasResource: false, canAffordCost: false,
            enforceCooldown: false, hasCooldown: false, canStartGlobal: false), Is.True);
        Assert.That(CastGateRules.TryAllowComboCooldownForNextDot(
            enforceCooldown: false, hasCooldown: false,
            SentencePhase.Building, 1,
            skillIsEmpty: false, comboKeyEmpty: false, canStartCombo: false), Is.True);
    }
}
