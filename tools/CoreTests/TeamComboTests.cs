using Dovus.Core.Border;
using Dovus.Core.Portal;
using Dovus.Core.Team;
using NUnit.Framework;
using Dovus.Core.Shared;

namespace CoreTests;

/// <summary>Dilim: sınır modu (border) — portal/takım skill testleri kaldırıldı.</summary>
[TestFixture]
public partial class TeamComboTests
{
    const float Gap = 0.02f;

    [Test]
    public void Threshold_OnBelow_OffAbove()
    {
        var mode = new BorderMode();
        var eng12 = Tier20BorderEngine();
        Assert.That(mode.OnSkill(1, (SkillId)"1-2", eng12, 0.20f), Is.False);
        Assert.That(mode.Active(1), Is.False);
        Assert.That(mode.OnSkill(1, (SkillId)"1-2", eng12, 0.19f), Is.True);
        Assert.That(mode.AttackSpeedMult(1), Is.EqualTo(1.30f).Within(0.001f));
        Assert.That(mode.LifestealAdd(1), Is.EqualTo(0.25f).Within(0.001f));
        Assert.That(mode.DamageMult(1), Is.EqualTo(1.20f).Within(0.001f));
        mode.Tick(1, 0.20f, 0.1f);
        Assert.That(mode.Active(1), Is.True, "eşiğe eşitken açık kalır");
        Assert.That(mode.Tick(1, 0.31f, 0.1f), Is.False, "skill bitmeden kapanmaz");
        Assert.That(mode.Active(1), Is.True);
        mode.EndCast(1);
        Assert.That(mode.Tick(1, 0.21f, 0.1f), Is.True);
        Assert.That(mode.Active(1), Is.False);
    }

    [Test]
    public void Skill_1_2_LifestealStaysForTheCast()
    {
        var mode = new BorderMode();
        var eng = Tier20BorderEngine();
        Assert.That(mode.OnSkill(1, (SkillId)"1-2", eng, 0.19f), Is.True);
        Assert.That(mode.LifestealAdd(1), Is.EqualTo(0.25f).Within(0.001f));
        mode.Tick(1, Gap, 0.1f);
        Assert.That(mode.LifestealAdd(1), Is.EqualTo(0.25f).Within(0.001f));
    }

    [Test]
    public void Skill_1_2_BorderAt20() => AssertTier("1-2", 0.19f, 0.20f, 1.30f, 0.25f, 1.20f);

    [Test]
    public void Clear_DropsPortalAndTeamBetweenCases()
    {
        PortalSystem portal = JsonPortal();
        TeamComboSystem team = JsonTeam();
        Assert.That(portal.IsPortalSkill((SkillId)"1-1"), Is.False);
        Assert.That(team.IsTeamSkill((SkillId)"1-1"), Is.False);
        portal.Clear();
        team.Clear();
    }
}
