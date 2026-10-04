using Dovus.App.Casting;
using Dovus.Core.Casting;
using Dovus.Core.Dodge;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

/// <summary>PLAN 2B.21 — mana/CD/dodge kapıları (App/Core; ölçülen varsayılanlar).</summary>
[TestFixture]
public class InputComboGateTests
{
    [Test]
    public void CombatTuning_Defaults_EnforceResourceCostAndCooldown_AreTrue()
    {
        var combat = new CombatTuning();
        Assert.That(combat.EnforceResourceCost, Is.True);
        Assert.That(combat.EnforceCooldown, Is.True);
    }

    [Test]
    public void DodgeTuning_Defaults_TwoCharges_SixSecondRecharge()
    {
        var dodge = new DodgeTuning();
        Assert.That(dodge.MaxCharges, Is.EqualTo(2));
        Assert.That(dodge.ChargeRechargeMs, Is.EqualTo(6000));

        var bank = new DodgeChargeBank(dodge);
        bank.TrySpend(0);
        bank.TrySpend(0);
        bank.Tick(5999);
        Assert.That(bank.Ready, Is.Zero);
        bank.Tick(6000);
        Assert.That(bank.Ready, Is.EqualTo(1));
    }

    [Test]
    public void CastGateRules_BuildingAtMaxDots_WouldStartSentence_TrueForGateBypass()
    {
        Assert.That(
            CastGateRules.WouldStartSentence(SentencePhase.Building, wordCount: 2, maxSentenceDots: 2),
            Is.True);
        Assert.That(
            CastGateRules.TryAllowSentenceStart(
                SentencePhase.Building, 2, 2,
                enforceResourceCost: true, hasResource: true, canAffordCost: false,
                enforceCooldown: true, hasCooldown: true, canStartGlobal: true),
            Is.False,
            "kapasite dolu olsa da mana kapısı fiil başlatmada hâlâ geçerli");
    }

    [Test]
    public void CooldownTracker_WithEnforceOn_BlocksComboSecondDot()
    {
        var cd = new CooldownTracker(globalCooldownSec: 0.3f, maxConcurrentCasts: 1);
        const string key = "test-combo";
        Assert.That(cd.TryStart(key, 5f, worldMs: 0), Is.True);
        cd.CompleteCast();

        Assert.That(
            CastGateRules.TryAllowComboCooldownForNextDot(
                enforceCooldown: true, hasCooldown: true,
                SentencePhase.Building, wordCount: 1,
                skillIsEmpty: false, comboKeyEmpty: false, canStartCombo: cd.CanStart(key, 100)),
            Is.False);
    }
}
