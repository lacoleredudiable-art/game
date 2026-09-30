using System.Collections.Generic;
using Dovus.Core.Border;
using Dovus.Core.Combat;
using Dovus.Core.Motion;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Core.Team;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class PortalBorderTeamTests
{
    const float Gap = 0.02f;

    [Test]
    public void Threshold_OnBelow_OffAbove()
    {
        var mode = new BorderMode();
        Assert.That(mode.OnSkill(1, "1-2", 0.20f), Is.False);
        Assert.That(mode.Active(1), Is.False);
        Assert.That(mode.OnSkill(1, "1-2", 0.19f), Is.True);
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
        Assert.That(mode.AttackSpeedMult(1), Is.EqualTo(1f).Within(0.001f));
        Assert.That(mode.LifestealAdd(1), Is.EqualTo(0f).Within(0.001f));
        Assert.That(mode.DamageMult(1), Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void Skill_1_2_LifestealStaysForTheCast()
    {
        var mode = new BorderMode();
        Assert.That(mode.OnSkill(1, "1-2", 0.15f), Is.True);
        Assert.That(mode.Active(1), Is.True);
        Assert.That(mode.LifestealAdd(1), Is.EqualTo(BorderMode.Tier20Life).Within(0.001f));
        Assert.That(mode.Tick(1, 0.31f, 0.05f), Is.False);
        Assert.That(mode.Active(1), Is.True, "can %31 olsa da skill bitene kadar aura açık");
        mode.EndCast(1);
        Assert.That(mode.Active(1), Is.True);
        Assert.That(mode.Tick(1, 0.31f, 0.05f), Is.True);
        Assert.That(mode.Active(1), Is.False);
    }

    [Test]
    public void Skill_1_2_BorderAt20()
    {
        AssertTier("1-2", 0.19f, 0.20f, 1.30f, 0.25f, 1.20f);
        var mode = new BorderMode();
        Assert.That(mode.OnSkill(1, "1-2", 0.50f), Is.False);
    }

    [Test]
    public void Skill_1_8_BorderAt10() =>
        AssertTier("1-8", 0.09f, 0.10f, 1.50f, 0.40f, 1.35f);

    [Test]
    public void Skill_3_2_BorderAt20() =>
        AssertTier("3-2", 0.19f, 0.20f, 1.30f, 0.25f, 1.20f);

    [Test]
    public void Skill_8_2_BorderAt20() =>
        AssertTier("8-2", 0.19f, 0.20f, 1.30f, 0.25f, 1.20f);

    [Test]
    public void Skill_12_8_BorderAt10_AndRisingColumn()
    {
        AssertTier("12-8", 0.09f, 0.10f, 1.50f, 0.40f, 1.35f);
        var high = new BorderMode();
        high.OnSkill(1, "12-8", 0.50f);
        Assert.That(high.Active(1), Is.False);
        Assert.That(high.ColumnActive(1), Is.True);
        Assert.That(high.ColumnAttackSpeedMult(1), Is.EqualTo(1.20f).Within(0.001f));
        high.Tick(1, 0.50f, 2.5f);
        Assert.That(high.ColumnAttackSpeedMult(1), Is.EqualTo(1.35f).Within(0.001f));
        high.Tick(1, 0.50f, 2.5f);
        Assert.That(high.ColumnAttackSpeedMult(1), Is.EqualTo(1.50f).Within(0.001f));
        high.Tick(1, 0.50f, 0.1f);
        Assert.That(high.ColumnActive(1), Is.False);

        var low = new BorderMode();
        low.OnSkill(1, "12-8", 0.05f);
        Assert.That(low.AttackSpeedMult(1), Is.EqualTo(1.50f * 1.20f).Within(0.001f));
    }

    [Test]
    public void Skill_1_10_BackDoorOutsideBoss()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 5f);
        portal.Cast("1-10", Actor(1, 0f, 0f), default, null, boss);
        Assert.That(portal.Strike.Active, Is.True);
        AssertOutside(portal.Strike.X, portal.Strike.Z, 0.2f, boss);
        float toward = (boss.X - portal.Strike.X) * portal.Strike.DirX
            + (boss.Z - portal.Strike.Z) * portal.Strike.DirZ;
        Assert.That(toward, Is.GreaterThan(0.5f));
        Assert.That(portal.Drain(), Is.Empty, "sırt kapısı oyuncuyu ışınlamaz");
    }

    [Test]
    public void Skill_2_6_HookGroundedBesideAllyNotThroughBoss()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 4f);
        Body caster = Actor(1, 0f, 0f, owns: true, y: 2f);
        Body ally = Actor(2, 0f, 8f);
        portal.Cast("2-6", caster, ally, null, boss);
        Assert.That(portal.Drain(), Is.Empty, "kalıp bitene kadar yer değişmez");

        portal.NotifyTemplateEnded(1, 0f, 2f, 9f, 0.5f, boss);
        IReadOnlyList<Placement> moves = portal.Drain();
        Placement player = One(moves, 1);
        Placement pulled = One(moves, 2);
        Assert.That(player.Y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(player.Z, Is.LessThan(ally.Z), "dostun ötesine geçmez");
        AssertOutside(player.X, player.Z, 0.5f, boss);
        float beside = Dist(player.X, player.Z, ally.X, ally.Z);
        Assert.That(beside, Is.LessThan(2.2f), "dostun yanında biter");
        Assert.That(pulled.Y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(Dist(pulled.X, pulled.Z, caster.X, caster.Z), Is.LessThanOrEqualTo(PortalSystem.HookRangeM + Gap));
        Assert.That(pulled.Z, Is.LessThan(boss.Z), "boss'un ötesine geçmez");
        AssertOutside(pulled.X, pulled.Z, 0.5f, boss);
    }

    [Test]
    public void Skill_3_4_AnchorRecall()
    {
        var portal = new PortalSystem();
        var boss = Boss(10f, 10f);
        Body caster = Actor(1, 0f, 0f, owns: true);
        portal.Cast("3-4", caster, default, null, boss);
        Assert.That(portal.HasAnchor, Is.True);
        Assert.That(portal.Drain(), Is.Empty);

        var near = Actor(2, 1f, 0f);
        var far = Actor(3, 8f, 0f);
        var allies = new List<Body> { near, far };
        portal.Cast("3-4", Actor(1, 4f, 0f, owns: true), default, allies, boss);
        IReadOnlyList<Placement> now = portal.Drain();
        Assert.That(now, Has.None.Matches<Placement>(m => m.ActorId == 1));
        Placement recalled = One(now, 2);
        Assert.That(recalled.Y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(Dist(recalled.X, recalled.Z, 0f, 0f), Is.LessThan(PortalSystem.BesideM));
        Assert.That(now, Has.None.Matches<Placement>(m => m.ActorId == 3));

        portal.NotifyTemplateEnded(1, 4f, 0f, 0f, 0.5f, boss);
        Placement back = One(portal.Drain(), 1);
        Assert.That(back.Y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(Dist(back.X, back.Z, 0f, 0f), Is.LessThan(0.2f));
        AssertOutside(back.X, back.Z, 0.5f, boss);
    }

    [Test]
    public void Skill_3_10_TwoDoorsPassAlliesAndShots()
    {
        Assert.That(TeamComboSystem.IsTeamSkill("3-10"), Is.True);
        var portal = new PortalSystem();
        var boss = Boss(0f, 3f);
        portal.Cast("3-10", Actor(1, 0f, 0f, owns: true), default, null, boss);
        portal.NotifyTemplateEnded(1, 0f, 0f, 6f, 0.5f, boss);
        portal.Drain();

        Body ally = Actor(2, 0f, 0f);
        Assert.That(portal.Sense(ally, false, boss, out Placement pass), Is.True);
        Assert.That(Dist(pass.X, pass.Z, 0f, 6f), Is.LessThan(3f));
        AssertOutside(pass.X, pass.Z, ally.Radius, boss);
        Assert.That(pass.Y, Is.EqualTo(0f).Within(0.001f));

        Body shot = Actor(90, 0f, 0.2f);
        Assert.That(portal.Sense(shot, true, boss, out Placement shotOut), Is.True);
        AssertOutside(shotOut.X, shotOut.Z, 0.2f, boss);
        Assert.That(Dist(shotOut.X, shotOut.Z, 0f, 0f), Is.GreaterThan(2f));
    }

    [Test]
    public void Skill_8_1_ShrinkGate()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 8f);
        Body ally = Actor(2, 0f, 4f);
        portal.Cast("8-1", Actor(1, 0f, 0f), ally, null, boss);
        DoorView gate = portal.Doors[0];
        Body walking = Actor(2, gate.X, gate.Z);
        portal.Sense(walking, false, boss, out _);
        PortalBuff buff = portal.BuffFor(2);
        Assert.That(buff.MoveSpeedMult, Is.EqualTo(1.30f).Within(0.001f));
        Assert.That(buff.MissChance, Is.EqualTo(0.30f).Within(0.001f));
        portal.Tick(0.1f, Actor(1, 0f, 0f), null, boss);
        Assert.That(portal.BuffFor(2).MoveSpeedMult, Is.EqualTo(1.30f).Within(0.001f));

        Body bossBody = new Body(9, gate.X, 0f, gate.Z, 0.85f, false, true);
        portal.Sense(bossBody, false, boss, out _);
        Assert.That(portal.NarrowLeft, Is.EqualTo(PortalSystem.BossShrinkSec).Within(0.001f));
        Assert.That(portal.BossNarrow, Is.True);
        Assert.That(portal.StrikeScale, Is.EqualTo(0.7f).Within(0.001f));
    }

    [Test]
    public void Skill_8_1_BossStrikesThirtyPercentSmaller()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 8f);
        portal.Cast("8-1", Actor(1, 0f, 0f), Actor(2, 0f, 4f), null, boss);
        DoorView gate = portal.Doors[0];
        portal.Sense(new Body(9, gate.X, 0f, gate.Z, 0.85f, false, true), false, boss, out _);

        const float radius = 5.4f;
        const float length = 7f;
        const float width = 1.4f;
        BossStrikeShape small = BossStrikeShape.Scale(radius, length, width, portal.StrikeScale);
        Assert.That(small.Radius, Is.EqualTo(radius * 0.7f).Within(0.001f));
        Assert.That(small.Length, Is.EqualTo(length * 0.7f).Within(0.001f));
        Assert.That(small.Width, Is.EqualTo(width * 0.7f).Within(0.001f));
        Assert.That(small.CircleHits(radius * 0.75f), Is.False);
        Assert.That(small.CircleHits(radius * 0.6f), Is.True);
        Assert.That(small.LineHits(length * 0.8f, 0f), Is.False);
        Assert.That(small.LineHits(length * 0.5f, width * 0.3f), Is.True);

        var slam = new BossAttack();
        float live = slam.RadiusM * 0.85f;
        Assert.That(slam.IsInEffectVolume(live, 0f), Is.True);
        Assert.That(
            slam.IsInEffectVolume(BossStrikeShape.DistanceForVolume(live, portal.StrikeScale), 0f),
            Is.False);

        portal.Tick(PortalSystem.BossShrinkSec, Actor(1, 0f, 0f), null, boss);
        Assert.That(portal.BossNarrow, Is.False);
        Assert.That(portal.StrikeScale, Is.EqualTo(1f).Within(0.001f));
        BossStrikeShape full = BossStrikeShape.Scale(radius, length, width, portal.StrikeScale);
        Assert.That(full.CircleHits(radius * 0.75f), Is.True);
        Assert.That(slam.IsInEffectVolume(BossStrikeShape.DistanceForVolume(live, portal.StrikeScale), 0f), Is.True);
    }

    [Test]
    public void Skill_8_8_GrowGate()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 8f);
        portal.Cast("8-8", Actor(1, 0f, 0f), Actor(2, 0f, 4f), null, boss);
        DoorView gate = portal.Doors[0];
        portal.Sense(Actor(2, gate.X, gate.Z), false, boss, out _);
        PortalBuff buff = portal.BuffFor(2);
        Assert.That(buff.DamageMult, Is.EqualTo(1.30f).Within(0.001f));
        Assert.That(buff.DamageTakenMult, Is.EqualTo(0.75f).Within(0.001f));
        Assert.That(buff.MoveSpeedMult, Is.EqualTo(0.80f).Within(0.001f));
    }

    [Test]
    public void Skill_9_10_SwapAndDebuffs()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 5f);
        Body caster = Actor(1, 0f, 0f, owns: true);
        Body ally = Actor(2, 0f, 5f);
        portal.Cast("9-10", caster, ally, null, boss);
        Assert.That(portal.Drain(), Is.Empty);
        portal.NotifyTemplateEnded(1, 0f, 0f, 0f, 0.5f, boss);
        IReadOnlyList<Placement> moves = portal.Drain();
        Placement a = One(moves, 1);
        Placement b = One(moves, 2);
        AssertOutside(a.X, a.Z, 0.5f, boss);
        AssertOutside(b.X, b.Z, 0.5f, boss);
        Assert.That(b.TransferDebuffs, Is.True);
        Assert.That(a.Teleport, Is.True);
        Assert.That(b.Teleport, Is.True);
        Assert.That(a.Y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(b.Y, Is.EqualTo(0f).Within(0.001f));

        var from = new StatusBoard();
        var to = new StatusBoard();
        from.Apply(StatusKind.Poison, 2000, 3f, "test");
        from.Apply(StatusKind.Haste, 2000, 1.2f, "test");
        int moved = PortalSystem.MoveHostile(from, to);
        Assert.That(moved, Is.EqualTo(1));
        Assert.That(from.Has(StatusKind.Poison), Is.False);
        Assert.That(from.Has(StatusKind.Haste), Is.True);
        Assert.That(to.Has(StatusKind.Poison), Is.True);
    }

    [Test]
    public void Skill_9_10_KeepsEachGroundHeight()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 8f);
        Body caster = Actor(1, 0f, 0f, owns: true, y: 1f);
        Body ally = Actor(2, 0f, 3f, y: 1.2f);
        portal.Cast("9-10", caster, ally, null, boss);
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
        Assert.That(TeamComboSystem.IsTeamSkill("10-10"), Is.True);
        var portal = new PortalSystem();
        var boss = Boss(0f, 5f);
        portal.Cast("10-10", Actor(1, 0f, 0f), default, null, boss);
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
        portal.Cast("11-8", caster, Actor(2, 4f, 4f), null, boss);
        Assert.That(portal.IsSunk(2), Is.True);
        portal.Tick(0.6f, caster, null, boss);
        Placement rose = One(portal.Drain(), 2);
        Assert.That(rose.Y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(Dist(rose.X, rose.Z, caster.X, caster.Z), Is.LessThan(2f));
        AssertOutside(rose.X, rose.Z, 0.5f, boss);
        Assert.That(portal.BuffFor(2).DamageMult, Is.EqualTo(1.10f).Within(0.001f));
        Assert.That(portal.IsSunk(2), Is.False);
    }

    [Test]
    public void Skill_11_10_TeamGateOptOut()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 6f);
        Body caster = Actor(1, 0f, 0f);
        var allies = new List<Body> { Actor(2, -4f, 1f), Actor(3, 4f, 1f) };
        portal.Cast("11-10", caster, default, allies, boss);
        portal.OptOut(3);
        portal.Tick(1f, caster, allies, boss);
        IReadOnlyList<Placement> moves = portal.Drain();
        Placement came = One(moves, 2);
        Assert.That(moves, Has.None.Matches<Placement>(m => m.ActorId == 3));
        Assert.That(came.Y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(Dist(came.X, came.Z, caster.X, caster.Z), Is.LessThan(2.2f));
        AssertOutside(came.X, came.Z, 0.5f, boss);
    }

    [Test]
    public void PortalPass_DoesNotLandInsideBoss()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 0f);
        portal.Cast("9-10", Actor(1, 0f, 0f), Actor(2, 0.2f, 0.2f), null, boss);
        foreach (Placement move in portal.Drain())
            AssertOutside(move.X, move.Z, 0.5f, boss);

        PortalSystem.HookLanding(0f, -6f, 0.5f, 0f, 0.1f, 0.5f, boss, out float x, out float y, out float z);
        Assert.That(y, Is.EqualTo(0f).Within(0.001f));
        AssertOutside(x, z, 0.5f, boss);
        Assert.That(z, Is.LessThanOrEqualTo(0.1f + Gap));
    }

    [Test]
    public void Skill_5_4_MineByAllyOrBoss()
    {
        var team = new TeamComboSystem();
        var boss = Boss(0f, 6f);
        var caster = Ally(1, 0f, 0f);
        var friend = Ally(2, 1f, 1f);
        team.Cast("5-4", caster, null, new[] { friend }, boss);
        Assert.That(team.TryMine(out float mx, out float mz), Is.True);
        Assert.That(team.AllyUsedSkill(caster, "1-1", mx, mz).MineMult, Is.EqualTo(0f).Within(0.001f));
        TeamPulse byAlly = team.AllyUsedSkill(friend, "1-1", mx, mz);
        Assert.That(byAlly.MineMult, Is.EqualTo(2f).Within(0.001f));

        var again = new TeamComboSystem();
        again.Cast("5-4", caster, null, new[] { friend }, boss);
        again.TryMine(out mx, out mz);
        Assert.That(again.BossStepped(mx, mz).MineMult, Is.EqualTo(2f).Within(0.001f));

        var stale = new TeamComboSystem();
        stale.Cast("5-4", caster, null, new[] { friend }, boss);
        stale.Tick(6.1f, new[] { caster, friend }, boss);
        stale.TryMine(out _, out _);
        Assert.That(stale.BossStepped(0f, 3f).MineMult, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void Skill_6_8_AirborneAllyDamage()
    {
        var team = new TeamComboSystem();
        var caster = Ally(1, 0f, 0f);
        var friend = Ally(2, 1f, 0f);
        TeamPulse pulse = team.Cast("6-8", caster, null, new[] { friend }, Boss(0f, 4f));
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
        team.Cast("7-6", caster, null, new[] { friend }, boss);
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
        team.Cast("7-9", a, null, new[] { b }, boss);
        team.AllyHit(a, boss.X, boss.Z, true);
        team.Tick(2f, new[] { a, b }, boss);
        Assert.That(team.BossIncomingMult, Is.EqualTo(1.30f).Within(0.001f));

        var pair = new TeamComboSystem();
        pair.Cast("7-9", a, null, new[] { b }, boss);
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
        team.Cast("8-3", a, b, all, Boss(0f, 5f));
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
        team.Cast("8-6", caster, left, new[] { left, right }, boss);
        Assert.That(team.DamageMult(2), Is.EqualTo(1.20f).Within(0.001f));
        Assert.That(team.DamageMult(3), Is.EqualTo(1.20f).Within(0.001f));
        TeamPulse pulse = team.Tick(1f, new[] { caster, left, right }, boss);
        Assert.That(pulse.Burned, Is.True);

        var miss = new TeamComboSystem();
        var sideA = Ally(2, -3f, -3f);
        var sideB = Ally(3, -2f, -3f);
        miss.Cast("8-6", caster, sideA, new[] { sideA, sideB }, boss);
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
        team.Cast("11-4", caster, null, new[] { friend }, near);
        team.Tick(1f, new[] { caster, friend }, near);
        Assert.That(team.TurretShots, Is.EqualTo(1));
        Assert.That(team.TouchTurret(friend), Is.EqualTo("1-1"));

        var far = new TeamComboSystem();
        far.Cast("11-4", caster, null, new[] { friend }, Boss(0f, 20f));
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
        team.Cast("12-6", caster, friend, all, Boss(0f, 8f));
        Assert.That(team.MoveSpeedMult(1), Is.EqualTo(1.50f).Within(0.001f));
        Assert.That(team.AttackSpeedMult(2), Is.EqualTo(1.50f).Within(0.001f));
        friend.Z = 0.1f;
        team.Tick(0.2f, all, Boss(0f, 8f));
        Assert.That(team.RopeLinked(1), Is.True);
        friend.X = 6f;
        team.Tick(0.2f, all, Boss(0f, 8f));
        Assert.That(team.RopeLinked(1), Is.False);
        Assert.That(team.MoveSpeedMult(1), Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void Skill_2_6_GroundHeightNearSide_IsNotSnapped()
    {
        var portal = new PortalSystem();
        var boss = Boss(0f, 6f);
        Body caster = Actor(1, 0f, 0f, owns: true, y: 1f);
        Body ally = Actor(2, 0f, 3f, y: 1f);
        portal.Cast("2-6", caster, ally, null, boss);
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
        portal.Cast("2-6", caster, ally, null, boss);
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
        portal.Cast("3-10", Actor(1, 0f, 0f, owns: true), default, null, boss);
        portal.NotifyTemplateEnded(1, 0f, 0f, 2f, 0.5f, boss);
        Assert.That(portal.Doors.Count, Is.GreaterThan(0));
        border.OnSkill(1, "1-8", 0.05f);
        team.Cast("5-4", Ally(1, 0f, 0f), null, null, boss);
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

    static void AssertTier(string skill, float below, float threshold, float attack, float life, float damage)
    {
        var mode = new BorderMode();
        Assert.That(mode.OnSkill(1, skill, threshold), Is.False, skill + " eşikte açılmaz");
        Assert.That(mode.OnSkill(1, skill, below), Is.True, skill);
        Assert.That(mode.Threshold(1), Is.EqualTo(threshold).Within(0.001f));
        Assert.That(mode.BorderAttackSpeedMult(1), Is.EqualTo(attack).Within(0.001f));
        Assert.That(mode.LifestealAdd(1), Is.EqualTo(life).Within(0.001f));
        Assert.That(mode.DamageMult(1), Is.EqualTo(damage).Within(0.001f));
        Assert.That(mode.AuraLabel(1), Does.Contain("Sınır"));
    }

    static void AssertOutside(float x, float z, float radius, in Disc boss)
    {
        Assert.That(PortalSystem.Overlaps(x, z, radius, boss), Is.False,
            "boss içinde: " + x.ToString("0.00") + "," + z.ToString("0.00"));
    }

    static Placement One(IReadOnlyList<Placement> moves, int id)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i].ActorId == id)
                return moves[i];
        }
        Assert.Fail("yerleşim yok: " + id);
        return default;
    }

    static float Dist(float ax, float az, float bx, float bz)
    {
        float dx = ax - bx;
        float dz = az - bz;
        return System.MathF.Sqrt(dx * dx + dz * dz);
    }

    static Disc Boss(float x, float z) => new Disc(true, x, z, 0.85f, PortalSystem.ClearGapM);

    static Body Actor(int id, float x, float z, bool owns = false, float y = 0f) =>
        new Body(id, x, y, z, 0.5f, owns, false);

    static FakeAlly Ally(int id, float x, float z) => new FakeAlly { Id = id, X = x, Z = z };

    sealed class FakeAlly : IAllyPlayer
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Radius { get; set; } = 0.5f;
        public float HpRatio { get; set; } = 1f;
        public string LastSkillId { get; set; } = string.Empty;
        public bool TemplateOwnsPosition { get; set; }
    }
}
