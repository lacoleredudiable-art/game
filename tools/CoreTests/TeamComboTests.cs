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

[TestFixture]
public partial class TeamComboTests
{
    const float Gap = 0.02f;

    [Test]
    public void Threshold_OnBelow_OffAbove()
    {
        var mode = new BorderMode();
        var eng12 = BorderEngine("1-2");
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
        Assert.That(mode.AttackSpeedMult(1), Is.EqualTo(1f).Within(0.001f));
        Assert.That(mode.LifestealAdd(1), Is.EqualTo(0f).Within(0.001f));
        Assert.That(mode.DamageMult(1), Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void Skill_1_2_LifestealStaysForTheCast()
    {
        var mode = new BorderMode();
        Assert.That(mode.OnSkill(1, (SkillId)"1-2", BorderEngine("1-2"), 0.15f), Is.True);
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
        Assert.That(mode.OnSkill(1, (SkillId)"1-2", BorderEngine("1-2"), 0.50f), Is.False);
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
        high.OnSkill(1, (SkillId)"12-8", BorderEngine("12-8"), 0.50f);
        Assert.That(high.Active(1), Is.False);
        Assert.That(high.ColumnMoveSpeedMult(1), Is.EqualTo(1.20f).Within(0.001f));
        high.Tick(1, 0.50f, 2.5f);
        Assert.That(high.ColumnMoveSpeedMult(1), Is.EqualTo(1.35f).Within(0.001f));
        high.Tick(1, 0.50f, 2.5f);
        Assert.That(high.ColumnMoveSpeedMult(1), Is.EqualTo(1.50f).Within(0.001f));
        high.Tick(1, 0.50f, 0.1f);
        Assert.That(high.ColumnMoveSpeedMult(1), Is.EqualTo(1f).Within(0.001f));

        var low = new BorderMode();
        low.OnSkill(1, (SkillId)"12-8", BorderEngine("12-8"), 0.05f);
        Assert.That(low.AttackSpeedMult(1), Is.EqualTo(1.50f * 1.20f).Within(0.001f));
    }

    [Test]
    public void Skill_1_10_BackDoorOutsideBoss()
    {
        var portal = JsonPortal();
        var boss = Boss(0f, 5f);
        portal.Cast((SkillId)"1-10", Actor(1, 0f, 0f), default, null, boss);
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
        var portal = JsonPortal();
        var boss = Boss(0f, 4f);
        Body caster = Actor(1, 0f, 0f, owns: true, y: 2f);
        Body ally = Actor(2, 0f, 8f);
        portal.Cast((SkillId)"2-6", caster, ally, null, boss);
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
        var portal = JsonPortal();
        var boss = Boss(10f, 10f);
        Body caster = Actor(1, 0f, 0f, owns: true);
        portal.Cast((SkillId)"3-4", caster, default, null, boss);
        Assert.That(portal.HasAnchor, Is.True);
        Assert.That(portal.Drain(), Is.Empty);

        var near = Actor(2, 1f, 0f);
        var far = Actor(3, 8f, 0f);
        var allies = new List<Body> { near, far };
        portal.Cast((SkillId)"3-4", Actor(1, 4f, 0f, owns: true), default, allies, boss);
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
        Assert.That(JsonTeam().IsTeamSkill((SkillId)"3-10"), Is.True);
        var portal = JsonPortal();
        var boss = Boss(0f, 3f);
        portal.Cast((SkillId)"3-10", Actor(1, 0f, 0f, owns: true), default, null, boss);
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
    public void Skill_3_10_CasterStandingInArrivalDoorDoesNotBounce()
    {
        var portal = JsonPortal();
        var boss = Boss(0f, 8f);
        Body caster = Actor(1, 0f, 0f, owns: true, y: 1.1f);
        portal.Cast((SkillId)"3-10", caster, default, null, boss);
        const float landZ = 4.95f;
        portal.NotifyTemplateEnded(1, 0f, 1.1f, landZ, 0.5f, boss);
        Assert.That(portal.Drain(), Is.Empty, "iniş yeni bir ışınlama yazmaz");

        Body standing = Actor(1, 0f, landZ, y: 1.1f);
        Assert.That(portal.Sense(standing, false, boss, out _), Is.False, "varış kapısının içi yeniden giriş değil");
        Assert.That(portal.Drain(), Is.Empty);

        Body outside = Actor(1, 0f, landZ + PortalSystem.DoorRadiusM + 0.6f, y: 1.1f);
        Assert.That(portal.Sense(outside, false, boss, out _), Is.False);
        Assert.That(portal.Sense(standing, false, boss, out Placement back), Is.True, "çıkıp girince kapı çalışır");
        Assert.That(back.Y, Is.EqualTo(1.1f).Within(0.001f));
        Assert.That(back.Z, Is.LessThan(landZ - 1f), "başlangıç kapısına döner");
    }

    [Test]
    public void Skill_8_1_ShrinkGate()
    {
        var portal = JsonPortal();
        var boss = Boss(0f, 8f);
        Body ally = Actor(2, 0f, 4f);
        portal.Cast((SkillId)"8-1", Actor(1, 0f, 0f), ally, null, boss);
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
        var portal = JsonPortal();
        var boss = Boss(0f, 8f);
        portal.Cast((SkillId)"8-1", Actor(1, 0f, 0f), Actor(2, 0f, 4f), null, boss);
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
        var portal = JsonPortal();
        var boss = Boss(0f, 8f);
        portal.Cast((SkillId)"8-8", Actor(1, 0f, 0f), Actor(2, 0f, 4f), null, boss);
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
        var portal = JsonPortal();
        var boss = Boss(0f, 5f);
        Body caster = Actor(1, 0f, 0f, owns: true);
        Body ally = Actor(2, 0f, 5f);
        portal.Cast((SkillId)"9-10", caster, ally, null, boss);
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
        int moved = PortalSystem.MoveHostile(from, to, "9-10");
        Assert.That(moved, Is.EqualTo(1));
        Assert.That(from.Has(StatusKind.Poison), Is.False);
        Assert.That(from.Has(StatusKind.Haste), Is.True);
        Assert.That(to.Has(StatusKind.Poison), Is.True);
    }

}
