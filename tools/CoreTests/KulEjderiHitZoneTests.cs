using Dovus.Core.Boss;
using NUnit.Framework;

namespace CoreTests;

/// <summary>B10 (veri kısmı): bölge başına hasar çarpanları (KE-2.8 - KE-2.11, KE-8.1).</summary>
[TestFixture]
public sealed class KulEjderiHitZoneTests
{
    const float Melee = 3f;
    const float Ranged = 20f;

    [TestCase(KulEjderiPhase.Phase1)]
    [TestCase(KulEjderiPhase.Phase2)]
    [TestCase(KulEjderiPhase.Phase3)]
    public void KE_2_9_Claws_take_60_percent_in_every_phase(KulEjderiPhase phase)
    {
        KulEjderiHitResult r = KulEjderiHitZones.Resolve(KulEjderiHitZone.Claw, phase, false, false, Melee);

        Assert.That(r.Reachable, Is.True);
        Assert.That(r.Multiplier, Is.EqualTo(0.6f));
    }

    [Test]
    public void KE_2_8_Head_takes_full_damage_in_phase1_and_phase3()
    {
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Head, KulEjderiPhase.Phase1, false, false, Melee).Multiplier, Is.EqualTo(1f));
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Head, KulEjderiPhase.Phase3, false, false, Melee).Multiplier, Is.EqualTo(1f));
    }

    [Test]
    public void KE_2_8_Phase2_head_is_reachable_only_with_range_14_or_when_staggered()
    {
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Head, KulEjderiPhase.Phase2, false, false, Melee).Reachable, Is.False);
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Head, KulEjderiPhase.Phase2, false, false, 13.9f).Reachable, Is.False);

        KulEjderiHitResult ranged = KulEjderiHitZones.Resolve(KulEjderiHitZone.Head, KulEjderiPhase.Phase2, false, false, 14f);
        Assert.That(ranged.Multiplier, Is.EqualTo(1f));

        KulEjderiHitResult staggered = KulEjderiHitZones.Resolve(KulEjderiHitZone.Head, KulEjderiPhase.Phase2, true, false, Melee);
        Assert.That(staggered.Reachable, Is.True);
    }

    [Test]
    public void KE_2_10_Chest_exists_only_in_phase2()
    {
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Chest, KulEjderiPhase.Phase2, false, false, Melee).Multiplier, Is.EqualTo(1f));
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Chest, KulEjderiPhase.Phase1, false, false, Melee).Reachable, Is.False);
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Chest, KulEjderiPhase.Phase3, false, false, Melee).Reachable, Is.False);
    }

    [Test]
    public void KE_2_11_Heart_is_double_when_open_and_closed_otherwise()
    {
        KulEjderiHitResult open = KulEjderiHitZones.Resolve(KulEjderiHitZone.Heart, KulEjderiPhase.Phase3, false, true, Melee);
        Assert.That(open.Multiplier, Is.EqualTo(2f));

        KulEjderiHitResult closed = KulEjderiHitZones.Resolve(KulEjderiHitZone.Heart, KulEjderiPhase.Phase3, false, false, Ranged);
        Assert.That(closed.Closed, Is.True);
        Assert.That(closed.TakesDamage, Is.False);

        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Heart, KulEjderiPhase.Phase1, false, true, Melee).Reachable, Is.False);
    }

    [Test]
    public void KE_8_1_Stagger_multiplies_every_reachable_zone_by_1_5_but_not_a_closed_heart()
    {
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Head, KulEjderiPhase.Phase1, true, false, Melee).Multiplier, Is.EqualTo(1.5f));
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Claw, KulEjderiPhase.Phase1, true, false, Melee).Multiplier, Is.EqualTo(0.9f).Within(1e-6f));
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Heart, KulEjderiPhase.Phase3, true, true, Melee).Multiplier, Is.EqualTo(3f));
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Heart, KulEjderiPhase.Phase3, true, false, Melee).Multiplier, Is.EqualTo(0f));
    }
}
