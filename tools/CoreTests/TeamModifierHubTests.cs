using Dovus.App.Team;
using Dovus.Core.Grammar;
using Dovus.Core.Portal;
using Dovus.Core.Shared;
using Dovus.Core.Team;
using Dovus.Game.Team;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CoreTests;

[TestFixture]
public class TeamModifierHubTests
{
    [Test]
    public void Hub_DefaultMultipliers_AreNeutral()
    {
        var hub = new TeamModifierHub();
        Assert.That(hub.AttackSpeedMult, Is.EqualTo(1f).Within(1e-5f));
        Assert.That(hub.DamageMult, Is.EqualTo(1f).Within(1e-5f));
        Assert.That(hub.LifestealAdd, Is.EqualTo(0f).Within(1e-5f));
        Assert.That(hub.BossIncomingMult, Is.EqualTo(1f).Within(1e-5f));
        Assert.That(hub.BossStrikeScale, Is.EqualTo(1f).Within(1e-5f));
        Assert.That(hub.PlayerDamageTakenMult, Is.EqualTo(1f).Within(1e-5f));
        Assert.That(hub.IntentionalTeleport, Is.False);
    }

    [Test]
    public void Hub_ResetModifiers_ClearsTableAndTeleportFlag()
    {
        var hub = new TeamModifierHub();
        hub.DamageMult = 2f;
        hub.BossIncomingMult = 0.5f;
        hub.MarkIntentionalTeleport();
        hub.SetMiss(3, 0.9f);
        hub.ResetModifiers();
        Assert.That(hub.DamageMult, Is.EqualTo(1f).Within(1e-5f));
        Assert.That(hub.BossIncomingMult, Is.EqualTo(1f).Within(1e-5f));
        Assert.That(hub.IntentionalTeleport, Is.False);
        Assert.That(hub.TryMiss(3), Is.False);
    }

    [Test]
    public void Hub_TryMiss_UsesDeterministicRoll()
    {
        var hub = new TeamModifierHub { PlayerActorId = 7 };
        hub.Roll = () => 0.2f;
        hub.SetMiss(7, 0.25f);
        Assert.That(hub.TryMiss(7), Is.True);
        hub.Roll = () => 0.3f;
        Assert.That(hub.TryMiss(7), Is.False);
    }

    [Test]
    public void IsPortalAndTeamSkill_InstanceMatchesJsonOps()
    {
        SkillMotor motor = SkillMechanicTagTests.LoadMotorPublic();
        PortalSystem portal = new PortalSystem(PortalOpTable.FromMotor(motor));
        TeamComboSystem team = new TeamComboSystem(TeamOpTable.FromMotor(motor));
        foreach (string id in SkillMechanicTagTests.ExpectedPortalOps.Keys)
            Assert.That(portal.IsPortalSkill((SkillId)id), Is.True, id);
        foreach (string id in SkillMechanicTagTests.ExpectedTeamOps.Keys)
            Assert.That(team.IsTeamSkill((SkillId)id), Is.True, id);
        Assert.That(portal.IsPortalSkill((SkillId)"1-1"), Is.False);
        Assert.That(team.IsTeamSkill((SkillId)"1-1"), Is.False);
    }

    [Test]
    public void SourceGate_NoPortalBorderTeamHooks_OrStaticSkillPredicates()
    {
        string scripts = Path.Combine(RepoRoot(), "unity", "Assets", "Scripts");
        string all = string.Concat(Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));
        Assert.That(all, Does.Not.Contain("PortalBorderTeamHooks"));
        Assert.That(Regex.Matches(all, @"static\s+bool\s+IsPortalSkill\b").Count, Is.Zero);
        Assert.That(Regex.Matches(all, @"static\s+bool\s+IsTeamSkill\b").Count, Is.Zero);
    }

    static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
}
