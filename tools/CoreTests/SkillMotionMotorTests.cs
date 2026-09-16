using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class SkillMotionMotorTests
{
    static SkillResolution Teleport(string adjectiveId = "sizma") =>
        new(
            "3-2", "Pus", "Pus Adımı", "pus_adimi", "job",
            "kisisel_isinlanma", "Kişisel Işınlanma", "motion", "self_teleport",
            0f, 0f, "self", "free_move", new[] { "stasis" },
            adjectiveId, adjectiveId, "none",
            0.85f, 1f, 1f,
            2, "Temel", 1f, "free_move",
            string.Empty);

    static SkillResolution Dash() =>
        new(
            "3", "Hava", "Hareket", "h", "job",
            "hareket", "Hareket", "motion", "dash",
            0f, 0f, "self", "free_move", new[] { "haste" },
            "tasima", "Taşıma", "none",
            0.9f, 1f, 1f,
            1, "Tekil", 1f, "free_move",
            string.Empty);

    static SkillResolution Lightning() =>
        new(
            "3-5", "Yıldırım", "Şimşek Geçişi", "simsek", "job",
            "zincirleme", "Zincirleme", "strike", "chain",
            45f, 20f, "chain_projectile", "slowed_move", new[] { "stun" },
            "isnlama", "Işınlama", "none",
            0.75f, 1f, 1f,
            2, "Temel", 1f, "free_move",
            string.Empty);

    static SkillMotionTuning T => new();

    [Test]
    public void Teleport_NearBoss_IsZenitsuPass()
    {
        var ctx = new SkillMotionContext(0f, -4f, 0f, 1f, 0f, 5f, true, 12f);
        var plan = SkillMotionMotor.Resolve(Teleport(), ctx, T);
        Assert.That(plan.Kind, Is.EqualTo(SkillMotionKind.ZenitsuPass));
        Assert.That(plan.SlashCommitMult, Is.GreaterThan(0f));
        Assert.That(plan.IframeMs, Is.GreaterThan(0));
        // Boss arkası: caster Z=-4, boss Z=5 → dest Z > 5
        Assert.That(plan.DestZ, Is.GreaterThan(5f));
    }

    [Test]
    public void Teleport_FarFromBoss_IsShortBlink()
    {
        var ctx = new SkillMotionContext(0f, -10f, 0f, 1f, 0f, 5f, true, 12f);
        var plan = SkillMotionMotor.Resolve(Teleport(), ctx, T);
        Assert.That(plan.Kind, Is.EqualTo(SkillMotionKind.ShortBlink));
        Assert.That(plan.DestZ, Is.EqualTo(-10f + T.ShortBlinkDistanceM).Within(0.01f));
    }

    [Test]
    public void Teleport_Sabitleme_PlacesMark()
    {
        var ctx = new SkillMotionContext(1f, 2f, 0f, 1f, 0f, 5f, true, 12f);
        var plan = SkillMotionMotor.Resolve(Teleport("sabitleme"), ctx, T);
        Assert.That(plan.Kind, Is.EqualTo(SkillMotionKind.PlaceMark));
        Assert.That(plan.MarkType, Is.EqualTo(SkillMotionMotor.MarkTypeBeacon));
        Assert.That(plan.DestX, Is.EqualTo(1f));
        Assert.That(plan.DestZ, Is.EqualTo(2f));
    }

    [Test]
    public void Hareket_IsForwardDash()
    {
        var ctx = new SkillMotionContext(0f, 0f, 1f, 0f, 0f, 5f, true, 12f);
        var plan = SkillMotionMotor.Resolve(Dash(), ctx, T);
        Assert.That(plan.Kind, Is.EqualTo(SkillMotionKind.ForwardDash));
        Assert.That(plan.DestX, Is.EqualTo(T.ForwardDashDistanceM).Within(0.01f));
    }

    [Test]
    public void Simsek_NearBoss_Zenitsu()
    {
        var ctx = new SkillMotionContext(0f, 0f, 0f, 1f, 0f, 3f, true, 12f);
        var plan = SkillMotionMotor.Resolve(Lightning(), ctx, T);
        Assert.That(plan.Kind, Is.EqualTo(SkillMotionKind.ZenitsuPass));
    }
}

[TestFixture]
public class StateBridgeBoardTests
{
    [Test]
    public void TwoSameMarks_FormPortalBridge()
    {
        var board = new StateBridgeBoard();
        var t = new SkillMotionTuning { BridgeMaxDistanceM = 20f };
        board.PlaceMark(SkillMotionMotor.MarkTypeBeacon, 0f, 0f, 0, t);
        board.PlaceMark(SkillMotionMotor.MarkTypeBeacon, 5f, 0f, 10, t);
        Assert.That(board.Bridges.Count, Is.EqualTo(1));
        Assert.That(board.Bridges[0].AX, Is.EqualTo(0f));
        Assert.That(board.Bridges[0].BX, Is.EqualTo(5f));
    }

    [Test]
    public void Traverse_WarpsToOtherEnd()
    {
        var board = new StateBridgeBoard();
        var t = new SkillMotionTuning();
        board.PlaceMark(SkillMotionMotor.MarkTypeBeacon, 0f, 0f, 0, t);
        board.PlaceMark(SkillMotionMotor.MarkTypeBeacon, 8f, 0f, 0, t);
        Assert.That(board.TryTraverse(0.1f, 0f, 100, t, out float dx, out float dz), Is.True);
        Assert.That(dx, Is.EqualTo(8f).Within(0.01f));
        Assert.That(dz, Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void DifferentTypes_NoBridge()
    {
        var board = new StateBridgeBoard();
        var t = new SkillMotionTuning();
        board.PlaceMark("ates_alani", 0f, 0f, 0, t);
        board.PlaceMark(SkillMotionMotor.MarkTypeBeacon, 3f, 0f, 0, t);
        Assert.That(board.Bridges.Count, Is.EqualTo(0));
    }
}
