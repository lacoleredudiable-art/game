using System.Linq;
using Dovus.Core.Boss;
using NUnit.Framework;

namespace CoreTests;

/// <summary>Kül Ejderi saldırı tablosu, faz kuralları ve eşik koruması (KE-3, KE-5 - KE-7).</summary>
[TestFixture]
public sealed class KulEjderiDataTests
{
    [TestCase(KulEjderiPhase.Phase1)]
    [TestCase(KulEjderiPhase.Phase2)]
    [TestCase(KulEjderiPhase.Phase3)]
    public void Each_phase_has_one_announced_attack_and_normal_weights_sum_to_100(KulEjderiPhase phase)
    {
        var pool = KulEjderiAttackTable.NormalPool(phase);

        Assert.That(pool.Sum(s => s.Weight), Is.EqualTo(100f).Within(1e-4f));
        Assert.That(pool.All(s => s.Phase == phase && !s.Announced), Is.True);
        KulEjderiAttackSpec announced = KulEjderiAttackTable.AnnouncedFor(phase);
        Assert.That(announced.Interruptible, Is.False);
        Assert.That(announced.TelegraphMs, Is.GreaterThanOrEqualTo(3000), "KE-4.3: en az 3 s önce duyuru");
    }

    [Test]
    public void Every_attack_has_design_id_and_positive_timings()
    {
        Assert.That(KulEjderiAttackTable.Specs.Count, Is.EqualTo(12));
        foreach (KulEjderiAttackSpec spec in KulEjderiAttackTable.Specs)
        {
            Assert.That(spec.DesignId, Does.StartWith("KE-"), spec.Id.ToString());
            Assert.That(spec.TelegraphMs, Is.GreaterThan(0), spec.Id.ToString());
            Assert.That(spec.ActiveMs, Is.GreaterThan(0), spec.Id.ToString());
            Assert.That(spec.RecoveryMs, Is.GreaterThan(0), spec.Id.ToString());
            Assert.That(KulEjderiAttackTable.For(spec.Id), Is.SameAs(spec));
        }
    }

    [Test]
    public void KE_5_4_Claw_slam_matches_design_row()
    {
        KulEjderiAttackSpec s = KulEjderiAttackTable.For(KulEjderiAttackId.ClawSlam);

        Assert.That(s.TelegraphMs, Is.EqualTo(1000));
        Assert.That(s.ActiveMs, Is.EqualTo(200));
        Assert.That(s.RecoveryMs, Is.EqualTo(1200));
        Assert.That(s.DamagePct, Is.EqualTo(25f));
        Assert.That(s.RadiusM, Is.EqualTo(4f));
        Assert.That(s.Interruptible, Is.True);
    }

    [Test]
    public void KE_3_3_Phase_for_ratio_uses_60_and_25_percent_thresholds()
    {
        Assert.That(KulEjderiPhaseRules.PhaseForRatio(1f), Is.EqualTo(KulEjderiPhase.Phase1));
        Assert.That(KulEjderiPhaseRules.PhaseForRatio(0.61f), Is.EqualTo(KulEjderiPhase.Phase1));
        Assert.That(KulEjderiPhaseRules.PhaseForRatio(0.60f), Is.EqualTo(KulEjderiPhase.Phase2));
        Assert.That(KulEjderiPhaseRules.PhaseForRatio(0.26f), Is.EqualTo(KulEjderiPhase.Phase2));
        Assert.That(KulEjderiPhaseRules.PhaseForRatio(0.25f), Is.EqualTo(KulEjderiPhase.Phase3));
        Assert.That(KulEjderiPhaseRules.PhaseForRatio(0f), Is.EqualTo(KulEjderiPhase.Phase3));
    }

    [Test]
    public void KE_3_5_Clamp_damage_stops_hp_at_the_phase_threshold()
    {
        // 15000 can, faz 1: eşik 9000. 9500'dan 2000 hasar yalnız 500 işler.
        Assert.That(KulEjderiPhaseRules.ClampDamage(9500f, 15000f, 2000f, KulEjderiPhase.Phase1), Is.EqualTo(500f).Within(0.01f));
        Assert.That(KulEjderiPhaseRules.ClampDamage(12000f, 15000f, 2000f, KulEjderiPhase.Phase1), Is.EqualTo(2000f).Within(0.01f));
        Assert.That(KulEjderiPhaseRules.ClampDamage(9000f, 15000f, 2000f, KulEjderiPhase.Phase1), Is.EqualTo(0f));
        Assert.That(KulEjderiPhaseRules.ClampDamage(4000f, 15000f, 9999f, KulEjderiPhase.Phase2), Is.EqualTo(250f).Within(1e-3f));
        Assert.That(KulEjderiPhaseRules.ClampDamage(500f, 15000f, 9999f, KulEjderiPhase.Phase3), Is.EqualTo(500f).Within(0.01f));
        Assert.That(KulEjderiPhaseRules.ClampDamage(500f, 15000f, 0f, KulEjderiPhase.Phase3), Is.EqualTo(0f));
    }

    [Test]
    public void KE_3_4_Remaining_layers_count_down_from_5_to_0()
    {
        Assert.That(KulEjderiPhaseRules.RemainingLayers(1f), Is.EqualTo(5));
        Assert.That(KulEjderiPhaseRules.RemainingLayers(0.81f), Is.EqualTo(5));
        Assert.That(KulEjderiPhaseRules.RemainingLayers(0.80f), Is.EqualTo(4));
        Assert.That(KulEjderiPhaseRules.RemainingLayers(0.21f), Is.EqualTo(2));
        Assert.That(KulEjderiPhaseRules.RemainingLayers(0.01f), Is.EqualTo(1));
        Assert.That(KulEjderiPhaseRules.RemainingLayers(0f), Is.EqualTo(0));
    }
}
