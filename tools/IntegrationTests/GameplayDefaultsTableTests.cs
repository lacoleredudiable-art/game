using Dovus.Game.Boss;
using Dovus.Game.Skills.Closing;
using Dovus.Game.Skills.Mechanics;
using Dovus.Game.Team;
using NUnit.Framework;

namespace IntegrationTests;

[TestFixture]
public class GameplayDefaultsTableTests
{
    [Test]
    public void ClosingDamageDefaults_MatchLegacyLiterals()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ClosingDamageDefaults.StrikeKnockBase, Is.EqualTo(1.85f));
            Assert.That(ClosingDamageDefaults.DisruptKnockMult, Is.EqualTo(0.12f));
            Assert.That(ClosingDamageDefaults.ControlPinSec, Is.EqualTo(0.7f));
            Assert.That(ClosingDamageDefaults.ScarScaleBase, Is.EqualTo(0.7f));
        });
    }

    [Test]
    public void TeamComboDefaults_MatchLegacyLiterals()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TeamComboDefaults.AllyDummyHpRatio, Is.EqualTo(0.7f));
            Assert.That(TeamComboDefaults.TeamActorRadiusM, Is.EqualTo(0.5f));
            Assert.That(TeamComboDefaults.BossBodyRadiusFallbackM, Is.EqualTo(0.85f));
        });
    }

    [Test]
    public void MechanicWorldAndTelegraphDefaults_MatchLegacyLiterals()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MechanicWorldDefaults.MinThicknessM, Is.EqualTo(0.05f));
            Assert.That(MechanicWorldDefaults.ExecutorFieldAlphaFallback, Is.EqualTo(0.6f));
            Assert.That(AttackTelegraphViewDefaults.DefaultRadiusM, Is.EqualTo(3.2f));
            Assert.That(BossTelegraphViewDefaults.HotDiscAlpha, Is.EqualTo(0.34f));
        });
    }
}
