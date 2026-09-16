using Dovus.Core.Combat;
using Dovus.Core.Status;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// docs/element-sistemi.json manipulation_layers.reality_layer —
/// revive_block / partial_erase / full_erase (yalnızca shields).
/// </summary>
[TestFixture]
public class RealityEffectDirectorTests
{
    [Test]
    public void ReviveBlock_FlagTracksDurationFromWorldMs()
    {
        var dir = new RealityEffectDirector();
        const double startMs = 1000;
        const float durationSec = RealityEffectDirector.DefaultReviveBlockSec; // JSON: 5

        Assert.That(dir.IsReviveBlocked(startMs), Is.False);
        dir.ApplyReviveBlock(startMs, durationSec);

        Assert.That(dir.IsReviveBlocked(startMs), Is.True);
        Assert.That(dir.ReviveBlockRemainingSec(startMs), Is.EqualTo(durationSec).Within(0.0001f));

        double midMs = startMs + durationSec * 1000.0 / 2.0;
        Assert.That(dir.IsReviveBlocked(midMs), Is.True);
        Assert.That(dir.ReviveBlockRemainingSec(midMs), Is.EqualTo(durationSec / 2f).Within(0.0001f));

        double justBefore = startMs + durationSec * 1000.0 - 1;
        Assert.That(dir.IsReviveBlocked(justBefore), Is.True);

        double atEnd = startMs + durationSec * 1000.0;
        Assert.That(dir.IsReviveBlocked(atEnd), Is.False);
        Assert.That(dir.ReviveBlockRemainingSec(atEnd), Is.EqualTo(0f));
    }

    [Test]
    public void ReviveBlock_OverlappingKeepsLongerExpiry()
    {
        var dir = new RealityEffectDirector();
        dir.ApplyReviveBlock(0, 5f);
        dir.ApplyReviveBlock(1000, 2f); // 3s'de biter — önceki 5s'i kısaltmamalı
        Assert.That(dir.IsReviveBlocked(4000), Is.True);
        Assert.That(dir.IsReviveBlocked(5000), Is.False);

        dir.ApplyReviveBlock(3000, 5f); // 8s'e uzar
        Assert.That(dir.IsReviveBlocked(7000), Is.True);
        Assert.That(dir.IsReviveBlocked(8000), Is.False);
    }

    [Test]
    public void PartialErase_RemovesShieldHasteDamageReduction_LeavesOthers()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Shield, 5000, 20f);
        board.Apply(StatusKind.Haste, 5000, 1.3f);
        board.Apply(StatusKind.DamageReduction, 5000, 0.7f);
        board.Apply(StatusKind.Regen, 5000, 2f);
        board.Apply(StatusKind.Burn, 5000, 3f);

        var dir = new RealityEffectDirector();
        dir.ApplyPartialErase(board);

        Assert.That(board.Has(StatusKind.Shield), Is.False);
        Assert.That(board.Has(StatusKind.Haste), Is.False);
        Assert.That(board.Has(StatusKind.DamageReduction), Is.False);
        Assert.That(board.Has(StatusKind.Regen), Is.True, "partial_erase Regen'e dokunmaz");
        Assert.That(board.Has(StatusKind.Burn), Is.True, "partial_erase debuff'a dokunmaz");
    }

    [Test]
    public void PartialErase_FromJsonTargetIds()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Shield, 2000, 10f);
        board.Apply(StatusKind.Haste, 2000, 1.2f);
        board.Apply(StatusKind.DamageReduction, 2000, 0.8f);
        board.Apply(StatusKind.Regen, 2000, 1f);

        var dir = new RealityEffectDirector();
        dir.ApplyPartialErase(board, new[] { "shield", "haste", "damage_reduction" });

        Assert.That(board.Has(StatusKind.Shield), Is.False);
        Assert.That(board.Has(StatusKind.Haste), Is.False);
        Assert.That(board.Has(StatusKind.DamageReduction), Is.False);
        Assert.That(board.Has(StatusKind.Regen), Is.True);
    }

    [Test]
    public void FullErase_RemovesShieldOnly()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Shield, 5000, 15f);
        board.Apply(StatusKind.Haste, 5000, 1.3f);
        board.Apply(StatusKind.DamageReduction, 5000, 0.7f);
        board.Apply(StatusKind.Regen, 5000, 2f);

        var dir = new RealityEffectDirector();
        dir.ApplyFullErase(board);

        Assert.That(board.Has(StatusKind.Shield), Is.False);
        Assert.That(board.Has(StatusKind.Haste), Is.True, "full_erase yalnızca shields");
        Assert.That(board.Has(StatusKind.DamageReduction), Is.True);
        Assert.That(board.Has(StatusKind.Regen), Is.True);
    }

    [Test]
    public void FullErase_JsonTargets_MapsShields_IgnoresMinionsSummons()
    {
        Assert.That(RealityEffectDirector.TryMapEraseTarget("shields", out StatusKind shields), Is.True);
        Assert.That(shields, Is.EqualTo(StatusKind.Shield));
        Assert.That(RealityEffectDirector.TryMapEraseTarget("minions", out _), Is.False);
        Assert.That(RealityEffectDirector.TryMapEraseTarget("summons", out _), Is.False);

        var board = new StatusBoard();
        board.Apply(StatusKind.Shield, 3000, 8f);
        board.Apply(StatusKind.Haste, 3000, 1.1f);

        var dir = new RealityEffectDirector();
        dir.ApplyFullErase(board, new[] { "minions", "summons", "shields" });

        Assert.That(board.Has(StatusKind.Shield), Is.False);
        Assert.That(board.Has(StatusKind.Haste), Is.True);
    }

    [Test]
    public void RemoveKinds_DoesNotAlterCleanseHostile()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Root, 1000, 1f);
        board.Apply(StatusKind.Burn, 1000, 1f);
        board.Apply(StatusKind.Haste, 1000, 1.3f);

        board.CleanseHostile();
        Assert.That(board.Has(StatusKind.Root), Is.False);
        Assert.That(board.Has(StatusKind.Burn), Is.False);
        Assert.That(board.Has(StatusKind.Haste), Is.True);
    }
}
