using Dovus.App.Team;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class ActorStatusTeamMoveSpeedTests
{
    [Test]
    public void TeamMoveSpeedMult_PlayerSlow_DoesNotAffectBossPath()
    {
        const int playerActorId = 1;
        var table = new TeamModifierTable();
        table.Set(playerActorId, new ActorModifiers(1f, 1f, 0f, 0.5f, 1f));

        float playerTeam = ActorStatusTeamMoveSpeed.TeamMoveSpeedMult(table, hasPlayerVitals: true, playerActorId);
        float bossTeam = ActorStatusTeamMoveSpeed.TeamMoveSpeedMult(table, hasPlayerVitals: false, playerActorId);

        Assert.That(playerTeam, Is.EqualTo(0.5f));
        Assert.That(bossTeam, Is.EqualTo(1f));
    }

    [Test]
    public void TableActorId_PlayerUsesConfiguredId_NonPlayerUsesZero()
    {
        Assert.That(ActorStatusTeamMoveSpeed.TableActorId(true, 7), Is.EqualTo(7));
        Assert.That(ActorStatusTeamMoveSpeed.TableActorId(false, 7), Is.EqualTo(0));
    }
}
