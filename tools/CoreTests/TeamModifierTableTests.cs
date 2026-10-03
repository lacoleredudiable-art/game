using Dovus.App.Team;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class TeamModifierTableTests
{
    [Test]
    public void For_UnknownActor_ReturnsDefaults()
    {
        var table = new TeamModifierTable();
        ActorModifiers mods = table.For(99);
        Assert.That(mods.AttackSpeedMult, Is.EqualTo(1f));
        Assert.That(mods.DamageMult, Is.EqualTo(1f));
        Assert.That(mods.LifestealAdd, Is.EqualTo(0f));
        Assert.That(mods.MoveSpeedMult, Is.EqualTo(1f));
        Assert.That(mods.DamageTakenMult, Is.EqualTo(1f));
        Assert.That(mods.MissChance, Is.EqualTo(0f));
        Assert.That(table.BossIncomingMult, Is.EqualTo(1f));
        Assert.That(table.BossStrikeScale, Is.EqualTo(1f));
    }

    [Test]
    public void Set_For_RoundTrip()
    {
        var table = new TeamModifierTable();
        var custom = new ActorModifiers(1.2f, 1.5f, 0.1f, 0.9f, 1.1f);
        table.Set(1, custom);
        ActorModifiers read = table.For(1);
        Assert.That(read.AttackSpeedMult, Is.EqualTo(1.2f));
        Assert.That(read.DamageMult, Is.EqualTo(1.5f));
        Assert.That(read.LifestealAdd, Is.EqualTo(0.1f));
        Assert.That(read.MoveSpeedMult, Is.EqualTo(0.9f));
        Assert.That(read.DamageTakenMult, Is.EqualTo(1.1f));
    }

    [Test]
    public void Reset_ClearsActorsMissTakenAndBossGlobals()
    {
        var table = new TeamModifierTable();
        table.Set(1, new ActorModifiers(2f, 2f, 1f, 2f, 2f));
        table.Set(2, new ActorModifiers(3f, 3f, 0f, 3f, 3f));
        table.BossIncomingMult = 1.5f;
        table.BossStrikeScale = 2f;
        table.SetMiss(1, 0.25f);
        table.SetTaken(2, 1.5f);
        table.Reset();
        Assert.That(table.For(1).AttackSpeedMult, Is.EqualTo(1f));
        Assert.That(table.For(2).DamageMult, Is.EqualTo(1f));
        Assert.That(table.BossIncomingMult, Is.EqualTo(1f));
        Assert.That(table.BossStrikeScale, Is.EqualTo(1f));
        Assert.That(table.DamageTakenMult(2), Is.EqualTo(1f));
        Assert.That(table.TryMiss(1), Is.False);
    }

    [Test]
    public void SetMiss_NonPositive_RemovesEntry()
    {
        var table = new TeamModifierTable();
        table.SetMiss(1, 0.5f);
        table.SetMiss(1, 0f);
        Assert.That(table.TryMiss(1), Is.False);
        table.SetMiss(1, -0.1f);
        Assert.That(table.TryMiss(1), Is.False);
    }

    [Test]
    public void SetTaken_NearOneOrNonPositive_NormalizesOrRemoves()
    {
        var table = new TeamModifierTable();
        table.SetTaken(1, 1.5f);
        Assert.That(table.DamageTakenMult(1), Is.EqualTo(1.5f));
        table.SetTaken(1, 1.00005f);
        Assert.That(table.DamageTakenMult(1), Is.EqualTo(1f));
        table.SetTaken(2, 0f);
        Assert.That(table.DamageTakenMult(2), Is.EqualTo(1f));
        table.SetTaken(3, -1f);
        Assert.That(table.DamageTakenMult(3), Is.EqualTo(1f));
    }

    [Test]
    public void TryMiss_RollStrictlyLessThanChance()
    {
        var table = new TeamModifierTable { Roll = () => 0.2f };
        table.SetMiss(1, 0.2f);
        Assert.That(table.TryMiss(1), Is.False);
        table.SetMiss(1, 0.21f);
        Assert.That(table.TryMiss(1), Is.True);
    }

    [Test]
    public void TryMiss_NoRoll_UsesOneSoNeverMisses()
    {
        var table = new TeamModifierTable();
        table.SetMiss(1, 0.99f);
        Assert.That(table.TryMiss(1), Is.False);
    }

    [Test]
    public void TwoActors_DoNotShareModifierSlots()
    {
        var table = new TeamModifierTable();
        table.Set(1, new ActorModifiers(1.1f, 1f, 0f, 1f, 1f));
        table.Set(2, new ActorModifiers(2.2f, 1f, 0f, 1f, 1f));
        table.SetMiss(1, 0.1f);
        table.SetMiss(2, 0.5f);
        Assert.That(table.For(1).AttackSpeedMult, Is.EqualTo(1.1f));
        Assert.That(table.For(2).AttackSpeedMult, Is.EqualTo(2.2f));
        table.Set(1, new ActorModifiers(9f, 1f, 0f, 1f, 1f));
        Assert.That(table.For(2).AttackSpeedMult, Is.EqualTo(2.2f));
    }
}
