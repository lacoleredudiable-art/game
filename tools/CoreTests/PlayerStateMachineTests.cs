using System.Collections.Generic;
using System.IO;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// docs/element-sistemi.json state_machine — PlayerStateMachine + SkillMotor.ParseStateMachine.
/// SentencePhase bağlanmaz; yalnızca capability okuma doğrulanır.
/// </summary>
[TestFixture]
public class PlayerStateMachineTests
{
    static SkillMotor LoadFull()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        }
        Assert.That(File.Exists(path), Is.True, $"element-sistemi.json bulunamadı: {path}");
        return SkillMotor.FromJson(File.ReadAllText(path));
    }

    static PlayerStateNode Require(PlayerStateMachine sm, string id)
    {
        Assert.That(sm.TryGet(id, out PlayerStateNode node), Is.True, $"state yok: {id}");
        return node;
    }

    [Test]
    public void Motor_Parses_NinePlayerStates_AndSixBossStates()
    {
        var motor = LoadFull();
        Assert.That(motor.PlayerStates.Count, Is.EqualTo(9));
        Assert.That(motor.BossStates.Count, Is.EqualTo(6));

        var ids = new List<string>();
        foreach (var s in motor.PlayerStates)
            ids.Add(s.Id);
        Assert.That(ids, Is.EquivalentTo(new[]
        {
            "idle", "drawing", "casting", "recovering", "dodging",
            "stunned", "rooted", "channeling", "dead"
        }));
    }

    [Test]
    public void EveryPlayerState_Reads_CanDraw_CanMove_CanDodge_IFrames()
    {
        var sm = new PlayerStateMachine(LoadFull().PlayerStates);
        Assert.That(sm.Count, Is.EqualTo(9));
        Assert.That(sm.CurrentId, Is.EqualTo("idle"));

        AssertCaps(Require(sm, "idle"),
            canDraw: "true", canMove: "true", canDodge: "true", iFrames: false);

        AssertCaps(Require(sm, "drawing"),
            canDraw: "partial", canMove: "false", canDodge: "true", iFrames: false);

        AssertCaps(Require(sm, "casting"),
            canDraw: "false", canMove: "based_on_cast_mobility", canDodge: "false", iFrames: false);

        AssertCaps(Require(sm, "recovering"),
            canDraw: "true", canMove: "limited", canDodge: "true", iFrames: false);

        // dodging: JSON'da can_dodge yok → "false"; i_frames true
        AssertCaps(Require(sm, "dodging"),
            canDraw: "false", canMove: "dodge_direction", canDodge: "false", iFrames: true);

        AssertCaps(Require(sm, "stunned"),
            canDraw: "false", canMove: "false", canDodge: "false", iFrames: false);

        AssertCaps(Require(sm, "rooted"),
            canDraw: "true", canMove: "false", canDodge: "true", iFrames: false);

        AssertCaps(Require(sm, "channeling"),
            canDraw: "false", canMove: "false", canDodge: "true", iFrames: false);
        Assert.That(Require(sm, "channeling").Interruptible, Is.True);

        AssertCaps(Require(sm, "dead"),
            canDraw: "false", canMove: "false", canDodge: "false", iFrames: false);
    }

    [Test]
    public void TryEnter_Exposes_CurrentCapabilities()
    {
        var sm = new PlayerStateMachine(LoadFull().PlayerStates);
        Assert.That(sm.TryEnter("drawing"), Is.True);
        Assert.That(sm.CurrentId, Is.EqualTo("drawing"));
        Assert.That(sm.CanDraw, Is.EqualTo("partial"));
        Assert.That(sm.CanMove, Is.EqualTo("false"));
        Assert.That(sm.CanDodge, Is.EqualTo("true"));
        Assert.That(sm.IFrames, Is.False);

        Assert.That(sm.TryEnter("dodging"), Is.True);
        Assert.That(sm.IFrames, Is.True);
        Assert.That(sm.CanMove, Is.EqualTo("dodge_direction"));

        Assert.That(sm.TryEnter("no_such_state"), Is.False);
        Assert.That(sm.CurrentId, Is.EqualTo("dodging"));
    }

    [Test]
    public void SyncWorld_Priority_DeadOverDrawing()
    {
        var sm = new PlayerStateMachine(LoadFull().PlayerStates);
        sm.SyncWorld(
            isDead: true, isStunned: false, isDodging: false, isRooted: false,
            isCasting: false, isDrawing: true, isRecovering: false);
        Assert.That(sm.CurrentId, Is.EqualTo("dead"));
        Assert.That(sm.AllowsDraw, Is.False);
        Assert.That(sm.AllowsDodge, Is.False);
    }

    [Test]
    public void SyncWorld_CastingBlocksDodge_DrawingAllowsPartialDraw()
    {
        var sm = new PlayerStateMachine(LoadFull().PlayerStates);
        sm.SyncWorld(false, false, false, false, isCasting: true, false, false);
        Assert.That(sm.CurrentId, Is.EqualTo("casting"));
        Assert.That(sm.AllowsDraw, Is.False);
        Assert.That(sm.AllowsDodge, Is.False);

        sm.SyncWorld(false, false, false, false, false, isDrawing: true, false);
        Assert.That(sm.CurrentId, Is.EqualTo("drawing"));
        Assert.That(sm.AllowsDraw, Is.True);
        Assert.That(sm.BlocksMove, Is.True);
    }

    [Test]
    public void BossStates_Parse_ExitsTo_And_StaggerDuration()
    {
        var motor = LoadFull();
        BossStateNode? staggered = null;
        BossStateNode? idle = null;
        foreach (var b in motor.BossStates)
        {
            if (b.Id == "staggered") staggered = b;
            if (b.Id == "idle") idle = b;
        }
        Assert.That(staggered, Is.Not.Null);
        Assert.That(staggered!.Value.HasDuration, Is.True);
        Assert.That(staggered.Value.DurationSec, Is.EqualTo(7f));
        Assert.That(staggered.Value.ExitsTo, Is.EquivalentTo(new[] { "idle" }));

        Assert.That(idle, Is.Not.Null);
        Assert.That(idle!.Value.ExitsTo, Is.EquivalentTo(new[] { "approach", "telegraph" }));
        Assert.That(idle.Value.HasDuration, Is.False);
    }

    static void AssertCaps(
        PlayerStateNode n, string canDraw, string canMove, string canDodge, bool iFrames)
    {
        Assert.That(n.CanDraw, Is.EqualTo(canDraw), $"{n.Id}.can_draw");
        Assert.That(n.CanMove, Is.EqualTo(canMove), $"{n.Id}.can_move");
        Assert.That(n.CanDodge, Is.EqualTo(canDodge), $"{n.Id}.can_dodge");
        Assert.That(n.IFrames, Is.EqualTo(iFrames), $"{n.Id}.i_frames");
    }
}
