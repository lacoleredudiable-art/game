using System;
using Dovus.Core.Boss;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// B10 (veri kısmı): bölge başına hasar çarpanları (KE-2.8 - KE-2.11, KE-7.8, KE-8.1).
/// Ulaşım (hangi silah hangi bölgeye değer) burada değil, <see cref="KulEjderiHurtboxes"/>'ta: [K] 17 Ekim, her silah
/// her bölgeye vurur.
/// </summary>
[TestFixture]
public sealed class KulEjderiHitZoneTests
{
    [TestCase(KulEjderiPhase.Phase1)]
    [TestCase(KulEjderiPhase.Phase2)]
    [TestCase(KulEjderiPhase.Phase3)]
    public void KE_2_9_Claws_take_60_percent_in_every_phase(KulEjderiPhase phase)
    {
        KulEjderiHitResult r = KulEjderiHitZones.Resolve(KulEjderiHitZone.Claw, phase, false, false);

        Assert.That(r.Reachable, Is.True);
        Assert.That(r.Multiplier, Is.EqualTo(0.6f));
    }

    [TestCase(KulEjderiPhase.Phase1)]
    [TestCase(KulEjderiPhase.Phase2)]
    [TestCase(KulEjderiPhase.Phase3)]
    public void KE_2_8_Head_takes_full_damage_in_every_phase_with_any_weapon(KulEjderiPhase phase)
    {
        KulEjderiHitResult r = KulEjderiHitZones.Resolve(KulEjderiHitZone.Head, phase, false, false);

        Assert.That(r.Reachable, Is.True);
        Assert.That(r.Multiplier, Is.EqualTo(1f));
    }

    [Test]
    public void KE_2_10_Chest_exists_only_in_phase2()
    {
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Chest, KulEjderiPhase.Phase2, false, false).Multiplier, Is.EqualTo(1f));
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Chest, KulEjderiPhase.Phase1, false, false).Reachable, Is.False);
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Chest, KulEjderiPhase.Phase3, false, false).Reachable, Is.False);
    }

    [Test]
    public void KE_2_11_Heart_is_not_a_hit_zone()
    {
        Assert.That(Enum.GetNames(typeof(KulEjderiHitZone)), Does.Not.Contain("Heart"));
    }

    [Test]
    public void KE_8_1_Stagger_multiplies_every_reachable_zone_by_1_5()
    {
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Head, KulEjderiPhase.Phase1, true, false).Multiplier, Is.EqualTo(1.5f));
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Claw, KulEjderiPhase.Phase1, true, false).Multiplier, Is.EqualTo(0.9f).Within(1e-6f));
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Chest, KulEjderiPhase.Phase2, true, false).Multiplier, Is.EqualTo(1.5f));
    }

    [Test]
    public void KE_7_8_Collapse_doubles_every_reachable_zone()
    {
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Head, KulEjderiPhase.Phase3, false, true).Multiplier, Is.EqualTo(2f));
        Assert.That(KulEjderiHitZones.Resolve(KulEjderiHitZone.Claw, KulEjderiPhase.Phase3, false, true).Multiplier, Is.EqualTo(1.2f).Within(1e-6f));
    }

    [Test]
    public void KE_7_8_Open_question_stagger_during_collapse_is_not_resolved_silently()
    {
        // [AÇIK] KE-7.8: çökmede denge dolarsa ×1,5 ile ×2,0 çarpılır mı, büyüğü mü geçer? Karar yok: sessiz seçim yapılmaz.
        Assert.Throws<InvalidOperationException>(() =>
            KulEjderiHitZones.Resolve(KulEjderiHitZone.Head, KulEjderiPhase.Phase3, true, true));
    }
}
