using Dovus.Core.Border;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Motion;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Core.Team;
using NUnit.Framework;
using System.Collections.Generic;
using Dovus.Core.Shared;

namespace CoreTests;

public partial class TeamComboTests
{
    [Test]
    public void Skill_9_10_KeepsEachGroundHeight()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 8f);
        Body caster = Actor(1, 0f, 0f, owns: true, y: 1f);
        Body ally = Actor(2, 0f, 3f, y: 1.2f);
        portal.Cast((SkillId)"9-10", caster, ally, null, boss);
        portal.NotifyTemplateEnded(1, 0f, 1f, 0f, 0.5f, boss);
        IReadOnlyList<Placement> moves = portal.Drain();
        Assert.That(One(moves, 1).Y, Is.EqualTo(1f).Within(0.001f));
        Assert.That(One(moves, 2).Y, Is.EqualTo(1.2f).Within(0.001f));
    }

    [Test]
    public void Skill_2_6_WithoutAllyAimsAtBoss()
    {
        Assert.That(
            MotionDeliveryAim.Choose("effect", "enemy_only", "heal", true, false),
            Is.EqualTo(MotionDeliveryAim.Kind.None),
            "dost yokken kalıp düşmana gider");
        Assert.That(
            MotionDeliveryAim.Choose("effect", "self_or_ally", "heal", true, true),
            Is.EqualTo(MotionDeliveryAim.Kind.Ally));
    }

    [Test]
    public void Skill_10_10_ShotsExitBehindBoss()
    {
        Assert.That(new TeamComboSystem().IsTeamSkill((SkillId)"10-10"), Is.True);
        var portal = new PortalSystem();
        var boss = Boss(0f, 5f);
        portal.Cast((SkillId)"10-10", Actor(1, 0f, 0f), default, null, boss);
        bool sawBack = false;
        foreach (DoorView door in portal.Doors)
        {
            AssertOutside(door.X, door.Z, 0.2f, boss);
            if (door.Z > boss.Z)
                sawBack = true;
        }
        Assert.That(sawBack, Is.True);

        Body shot = Actor(40, 0f, 1.2f);
        Assert.That(portal.Sense(shot, true, boss, out Placement exit), Is.True);
        Assert.That(exit.Z, Is.GreaterThan(boss.Z));
        AssertOutside(exit.X, exit.Z, 0.2f, boss);
        Assert.That(portal.Sense(Actor(1, 0f, 1.2f), false, boss, out _), Is.False, "gövde bu kapıdan geçmez");
    }

    [Test]
    public void Skill_11_8_AllyRisesBeside()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 5f);
        Body caster = Actor(1, 0f, 0f);
        portal.Cast((SkillId)"11-8", caster, Actor(2, 4f, 4f), null, boss);
        portal.Tick(0.6f, caster, null, boss);
        Placement rose = One(portal.Drain(), 2);
        Assert.That(rose.Y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(Dist(rose.X, rose.Z, caster.X, caster.Z), Is.LessThan(2f));
        AssertOutside(rose.X, rose.Z, 0.5f, boss);
        Assert.That(portal.BuffFor(2).DamageMult, Is.EqualTo(1.10f).Within(0.001f));
    }

    [Test]
    public void Skill_11_10_TeamGateGathers()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 6f);
        Body caster = Actor(1, 0f, 0f);
        var allies = new List<Body> { Actor(2, -4f, 1f), Actor(3, 4f, 1f) };
        portal.Cast((SkillId)"11-10", caster, default, allies, boss);
        portal.Tick(1f, caster, allies, boss);
        IReadOnlyList<Placement> moves = portal.Drain();
        Placement came = One(moves, 2);
        Assert.That(came.Y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(Dist(came.X, came.Z, caster.X, caster.Z), Is.LessThan(2.2f));
        AssertOutside(came.X, came.Z, 0.5f, boss);
    }

    [Test]
    public void Skill_11_10_AlliesKeepGroundHeight()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 6f);
        Body caster = Actor(1, 0f, 0f, y: 1.05f);
        var allies = new List<Body>
        {
            Actor(2, -4f, 1f, y: 1.2f),
            Actor(3, 4f, 1f, y: 0.9f)
        };
        portal.Cast((SkillId)"11-10", caster, default, allies, boss);
        portal.Tick(PortalSystem.TeamDelaySec, caster, allies, boss);
        IReadOnlyList<Placement> moves = portal.Drain();
        Assert.That(One(moves, 2).Y, Is.EqualTo(1.2f).Within(0.001f));
        Assert.That(One(moves, 3).Y, Is.EqualTo(0.9f).Within(0.001f));
        Assert.That(moves, Has.None.Matches<Placement>(m => m.ActorId != 2 && m.ActorId != 3));
    }

    [Test]
    public void PortalPass_DoesNotLandInsideBoss()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 0f);
        portal.Cast((SkillId)"9-10", Actor(1, 0f, 0f), Actor(2, 0.2f, 0.2f), null, boss);
        foreach (Placement move in portal.Drain())
            AssertOutside(move.X, move.Z, 0.5f, boss);

        PortalSystem.HookLanding(0f, -6f, 0.5f, 0f, 0.1f, 0.5f, boss, out float x, out float y, out float z);
        Assert.That(y, Is.EqualTo(0f).Within(0.001f));
        AssertOutside(x, z, 0.5f, boss);
        Assert.That(z, Is.LessThanOrEqualTo(0.1f + Gap));
    }

    [Test]
    public void Skill_5_4_MineByAlly()
    {
        var team = new TeamComboSystem();
        var boss = Boss(0f, 6f);
        var caster = Ally(1, 0f, 0f);
        var friend = Ally(2, 1f, 1f);
        team.Cast((SkillId)"5-4", caster, null, new[] { friend }, boss);
        Assert.That(team.TryMine(out float mx, out float mz), Is.True);
        Assert.That(team.AllyUsedSkill(caster, (SkillId)"1-1", mx, mz).MineMult, Is.EqualTo(0f).Within(0.001f));
        TeamPulse byAlly = team.AllyUsedSkill(friend, (SkillId)"1-1", mx, mz);
        Assert.That(byAlly.MineMult, Is.EqualTo(2f).Within(0.001f));
    }

    [Test]
    public void Skill_6_8_AirborneAllyDamage()
    {
        var team = new TeamComboSystem();
        var caster = Ally(1, 0f, 0f);
        var friend = Ally(2, 1f, 0f);
        TeamPulse pulse = team.Cast((SkillId)"6-8", caster, null, new[] { friend }, Boss(0f, 4f));
        Assert.That(pulse.Stunned, Is.True);
        Assert.That(pulse.StunSec, Is.EqualTo(1f).Within(0.001f));
        Assert.That(pulse.AttackBroken, Is.True);
        Assert.That(team.DamageMult(2), Is.EqualTo(1.30f).Within(0.001f));
        Assert.That(team.DamageMult(1), Is.EqualTo(1f).Within(0.001f));
        team.Tick(1.05f, new[] { caster, friend }, Boss(0f, 4f));
        Assert.That(team.DamageMult(2), Is.EqualTo(1f).Within(0.001f));
        Assert.That(team.AttackBroken, Is.False);
    }

    [Test]
    public void Skill_7_6_RopeBurst()
    {
        var team = new TeamComboSystem();
        var boss = Boss(0f, 4f);
        var caster = Ally(1, 0f, 0f);
        var friend = Ally(2, 0f, 1f);
        team.Cast((SkillId)"7-6", caster, null, new[] { friend }, boss);
        Assert.That(team.TryRopeMid(out float mx, out float mz), Is.True);
        Assert.That(team.AllyHit(caster, mx, mz, false).Stunned, Is.False);
        TeamPulse burst = team.AllyHit(friend, mx, mz, false);
        Assert.That(burst.Stunned, Is.True);
        Assert.That(burst.StunSec, Is.EqualTo(1f).Within(0.001f));
        Assert.That(team.BossIncomingMult, Is.EqualTo(1.30f).Within(0.001f));
    }

    [Test]
    public void Skill_7_9_MarkTwoPlayers()
    {
        var team = new TeamComboSystem();
        var boss = Boss(0f, 4f);
        var a = Ally(1, 0f, 0f);
        var b = Ally(2, 1f, 0f);
        team.Cast((SkillId)"7-9", a, null, new[] { b }, boss);
        team.AllyHit(a, boss.X, boss.Z, true);
        team.Tick(2f, new[] { a, b }, boss);
        Assert.That(team.BossIncomingMult, Is.EqualTo(1.30f).Within(0.001f));

        var pair = new TeamComboSystem();
        pair.Cast((SkillId)"7-9", a, null, new[] { b }, boss);
        pair.AllyHit(a, boss.X, boss.Z, true);
        pair.AllyHit(b, boss.X, boss.Z, true);
        pair.Tick(2f, new[] { a, b }, boss);
        Assert.That(pair.BossIncomingMult, Is.EqualTo(1.50f).Within(0.001f));
    }

    [Test]
    public void Skill_8_3_BallThreePasses()
    {
        var team = new TeamComboSystem();
        var a = Ally(1, 0f, 0f);
        var b = Ally(2, 1f, 0f);
        var c = Ally(3, 2f, 0f);
        var d = Ally(4, 3f, 0f);
        var all = new[] { a, b, c, d };
        team.Cast((SkillId)"8-3", a, b, all, Boss(0f, 5f));
        Assert.That(team.BallHolder, Is.EqualTo(2));
        Assert.That(team.DamageMult(2), Is.EqualTo(1.20f).Within(0.001f));
        Assert.That(team.PassBall(b, c), Is.True);
        Assert.That(team.PassBall(c, d), Is.True);
        Assert.That(team.PassBall(d, b), Is.True);
        Assert.That(team.BallPasses, Is.EqualTo(3));
        Assert.That(team.PassBall(b, c), Is.False);
        Assert.That(team.BallHolder, Is.EqualTo(2));
        team.Tick(3.9f, all, Boss(0f, 5f));
        Assert.That(team.DamageMult(2), Is.EqualTo(1.20f).Within(0.001f));
        team.Tick(0.2f, all, Boss(0f, 5f));
        Assert.That(team.BallHolder, Is.EqualTo(0));
    }

    [Test]
    public void Skill_8_6_RopeBurn()
    {
        var team = new TeamComboSystem();
        var boss = Boss(0f, 0f);
        var caster = Ally(1, 0f, -4f);
        var left = Ally(2, -3f, 0f);
        var right = Ally(3, 3f, 0f);
        team.Cast((SkillId)"8-6", caster, left, new[] { left, right }, boss);
        Assert.That(team.DamageMult(2), Is.EqualTo(1.20f).Within(0.001f));
        Assert.That(team.DamageMult(3), Is.EqualTo(1.20f).Within(0.001f));
        TeamPulse pulse = team.Tick(1f, new[] { caster, left, right }, boss);
        Assert.That(pulse.Burned, Is.True);

        var miss = new TeamComboSystem();
        var sideA = Ally(2, -3f, -3f);
        var sideB = Ally(3, -2f, -3f);
        miss.Cast((SkillId)"8-6", caster, sideA, new[] { sideA, sideB }, boss);
        Assert.That(miss.Tick(1f, new[] { caster, sideA, sideB }, boss).Burned, Is.False);
    }

    [Test]
    public void Skill_11_4_TurretCopiesSkill()
    {
        var team = new TeamComboSystem();
        var near = Boss(0f, 4f);
        var caster = Ally(1, 0f, 0f);
        var friend = Ally(2, 0.2f, 0.2f);
        friend.LastSkillId = "1-1";
        team.Cast((SkillId)"11-4", caster, null, new[] { friend }, near);
        team.Tick(1f, new[] { caster, friend }, near);
        Assert.That(team.TurretShots, Is.EqualTo(1));
        Assert.That(team.TouchTurret(friend), Is.EqualTo("1-1"));

        var far = new TeamComboSystem();
        far.Cast((SkillId)"11-4", caster, null, new[] { friend }, Boss(0f, 20f));
        far.Tick(1f, new[] { caster, friend }, Boss(0f, 20f));
        Assert.That(far.TurretShots, Is.EqualTo(0));
    }

    [Test]
    public void Skill_12_6_HasteBreaksWhenApart()
    {
        var team = new TeamComboSystem();
        var caster = Ally(1, 0f, 0f);
        var friend = Ally(2, 2f, 0f);
        var all = new List<IAllyPlayer> { caster, friend };
        team.Cast((SkillId)"12-6", caster, friend, all, Boss(0f, 8f));
        Assert.That(team.MoveSpeedMult(1), Is.EqualTo(1.50f).Within(0.001f));
        Assert.That(team.AttackSpeedMult(2), Is.EqualTo(1.50f).Within(0.001f));
        friend.Z = 0.1f;
        team.Tick(0.2f, all, Boss(0f, 8f));
        Assert.That(team.MoveSpeedMult(1), Is.EqualTo(1.50f).Within(0.001f));
        friend.X = 6f;
        team.Tick(0.2f, all, Boss(0f, 8f));
        Assert.That(team.MoveSpeedMult(1), Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void Skill_2_6_GroundHeightNearSide_IsNotSnapped()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 6f);
        Body caster = Actor(1, 0f, 0f, owns: true, y: 1f);
        Body ally = Actor(2, 0f, 3f, y: 1f);
        portal.Cast((SkillId)"2-6", caster, ally, null, boss);
        portal.NotifyTemplateEnded(1, 0f, 1f, 1.5f, 0.5f, boss);
        IReadOnlyList<Placement> moves = portal.Drain();
        Assert.That(moves, Has.None.Matches<Placement>(m => m.ActorId == 1));
    }

    [Test]
    public void Skill_2_6_ValidTemplateEnd_IsNotSnapped()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 4f);
        Body caster = Actor(1, 0f, 0f, owns: true);
        Body ally = Actor(2, 0f, 3f);
        portal.Cast((SkillId)"2-6", caster, ally, null, boss);
        PortalSystem.HookLanding(0f, 0f, 0.5f, ally.X, ally.Z, ally.Radius, boss, out float x, out float y, out float z);
        portal.NotifyTemplateEnded(1, x, y, z, 0.5f, boss);
        IReadOnlyList<Placement> moves = portal.Drain();
        Assert.That(moves, Has.None.Matches<Placement>(m => m.ActorId == 1));
        Assert.That(One(moves, 2).Teleport, Is.False);
    }

    [Test]
    public void TemplateEnd_WithoutPortal_DoesNotShove()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 1.2f);
        portal.NotifyTemplateEnded(1, 0f, 0f, 1.2f, 0.5f, boss);
        Assert.That(portal.Drain(), Is.Empty);
    }

    [Test]
    public void Clear_DropsPortalAndTeamBetweenCases()
    {
        var portal = new PortalSystem();
        var team = new TeamComboSystem();
        var border = new BorderMode();
        var boss = Boss(0f, 6f);
        portal.Cast((SkillId)"3-10", Actor(1, 0f, 0f, owns: true), default, null, boss);
        portal.NotifyTemplateEnded(1, 0f, 0f, 2f, 0.5f, boss);
        Assert.That(portal.Doors.Count, Is.GreaterThan(0));
        border.OnSkill(1, (SkillId)"1-8", 0.05f);
        team.Cast((SkillId)"5-4", Ally(1, 0f, 0f), null, null, boss);
        portal.Clear();
        team.Clear();
        border.Clear();
        Assert.That(portal.Doors, Is.Empty);
        Assert.That(portal.Drain(), Is.Empty);
        Assert.That(portal.HasAnchor, Is.False);
        Assert.That(border.Active(1), Is.False);
        Assert.That(team.MineAlive, Is.False);
        portal.NotifyTemplateEnded(1, 0f, 0f, 1.2f, 0.5f, boss);
        Assert.That(portal.Drain(), Is.Empty);
    }

    [Test]
    public void SweepJump_OnlyIntentionalTeleportIsExempt()
    {
        const float limit = 0.62f;
        Assert.That(SweepJumpRule.IsIllegalJump(4.66f, limit, false, true), Is.False);
        Assert.That(SweepJumpRule.IsIllegalJump(4.66f, limit, true, false), Is.False);
        Assert.That(SweepJumpRule.IsIllegalJump(4.66f, limit, false, false), Is.True);
        Assert.That(SweepJumpRule.IsIllegalJump(0.4f, limit, false, false), Is.False);

        float ox = 0f;
        float oz = 0f;
        SweepJumpRule.NoteTeleport(ref ox, ref oz, 1f, 0f, false);
        SweepJumpRule.NoteTeleport(ref ox, ref oz, 8.34f, 0f, true);
        Assert.That(9.34f - ox, Is.EqualTo(1f).Within(0.001f), "konum ışın adımını saymaz");
    }
}
