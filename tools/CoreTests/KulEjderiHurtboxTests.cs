using Dovus.Core.Boss;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// KE-2.6 / KE-2.12: vuruş kutuları. Menzil parçanın en yakın yüzeyine üç boyutlu ölçülür; yüksek bölgelerin altında
/// yere inen vuruş sütunu vardır ([K] 17 Ekim: her silah her bölgeye vurur). Sütundan gelen vuruşta kor hilali çıkar,
/// vuruş noktası gerçek gövdenin üstündedir (havada değil).
/// </summary>
[TestFixture]
public sealed class KulEjderiHurtboxTests
{
    const float Sword = 6f;   // O-2 Kılıç menzili (02 D-1)
    const float Bow = 14.1f;  // KE-2.6 örneğindeki Yay menzili

    static KulEjderiPoint Ground(float x, float z) => new KulEjderiPoint(x, 0f, z);

    [Test]
    public void KE_2_12_Pose_follows_phase_and_slump()
    {
        Assert.That(KulEjderiHurtboxes.PoseFor(KulEjderiPhase.Phase1, false), Is.EqualTo(KulEjderiPose.Lying));
        Assert.That(KulEjderiHurtboxes.PoseFor(KulEjderiPhase.Phase2, false), Is.EqualTo(KulEjderiPose.Reared));
        Assert.That(KulEjderiHurtboxes.PoseFor(KulEjderiPhase.Phase3, false), Is.EqualTo(KulEjderiPose.ChestDown));
        Assert.That(KulEjderiHurtboxes.PoseFor(KulEjderiPhase.Phase2, true), Is.EqualTo(KulEjderiPose.Slumped));
    }

    [Test]
    public void KE_2_8_Sword_reaches_phase2_head_through_column_and_gets_crescent()
    {
        // Başın altında, iki pençenin arasında (KE-2.4 iç kenar X ≈ ±5,9)
        KulEjderiReach r = KulEjderiHurtboxes.Probe(KulEjderiHitZone.Head, KulEjderiPose.Reared, Ground(0f, 12f), Sword);

        Assert.That(r.Reachable, Is.True);
        Assert.That(r.ViaColumn, Is.True, "gövde 10 m yukarıda: kor hilali gerekir");
        Assert.That(r.HitPoint.Y, Is.EqualTo(KulEjderiDefaults.HeadRearedMinY).Within(1e-4f), "vuruş noktası başın alt yüzünde");
        Assert.That(r.HitPoint.Z, Is.EqualTo(12f).Within(1e-4f));
    }

    [Test]
    public void KE_2_6_Phase2_head_from_z2_bow_reaches_body_sword_does_not()
    {
        KulEjderiPoint p = Ground(0f, 2f);

        KulEjderiReach sword = KulEjderiHurtboxes.Probe(KulEjderiHitZone.Head, KulEjderiPose.Reared, p, Sword);
        Assert.That(sword.Reachable, Is.False, "sütuna yatay 6,5 m: Kılıç başın altına girmeli");

        KulEjderiReach bow = KulEjderiHurtboxes.Probe(KulEjderiHitZone.Head, KulEjderiPose.Reared, p, Bow);
        Assert.That(bow.Reachable, Is.True);
        Assert.That(bow.ViaColumn, Is.False, "Yay gövdenin en yakın yüzeyine doğrudan ulaşır");
        Assert.That(bow.DistanceM, Is.LessThan(Bow));
    }

    [Test]
    public void KE_2_10_Sword_reaches_phase2_chest_at_arena_edge()
    {
        KulEjderiReach r = KulEjderiHurtboxes.Probe(KulEjderiHitZone.Chest, KulEjderiPose.Reared, Ground(0f, 17f), Sword);

        Assert.That(r.Reachable, Is.True);
        Assert.That(r.ViaColumn, Is.True);
        Assert.That(r.HitPoint.Y, Is.EqualTo(KulEjderiDefaults.ChestRearedMinY).Within(1e-4f));
        Assert.That(r.HitPoint.Z, Is.EqualTo(KulEjderiDefaults.ChestRearedMinZ).Within(1e-4f));
    }

    [Test]
    public void KE_2_8_Direct_touch_in_phase1_needs_no_crescent()
    {
        KulEjderiReach r = KulEjderiHurtboxes.Probe(KulEjderiHitZone.Head, KulEjderiPose.Lying, Ground(0f, 5f), Sword);

        Assert.That(r.Reachable, Is.True);
        Assert.That(r.ViaColumn, Is.False);
    }

    [Test]
    public void KE_2_9_Claw_probe_picks_the_nearer_claw()
    {
        KulEjderiReach east = KulEjderiHurtboxes.Probe(KulEjderiHitZone.Claw, KulEjderiPose.Reared, Ground(12f, 13.7f), Sword);
        Assert.That(east.Reachable, Is.True);
        Assert.That(east.HitPoint.X, Is.GreaterThan(0f));
        Assert.That(east.ViaColumn, Is.False, "pençe yerde: sütun yok");
    }

    [Test]
    public void KE_2_10_Chest_has_no_hurtbox_outside_phase2_poses()
    {
        Assert.That(KulEjderiHurtboxes.Probe(KulEjderiHitZone.Chest, KulEjderiPose.Lying, Ground(0f, 17f), Bow).Reachable, Is.False);
        Assert.That(KulEjderiHurtboxes.Probe(KulEjderiHitZone.Chest, KulEjderiPose.ChestDown, Ground(0f, 17f), Bow).Reachable, Is.False);
    }

    [TestCase(KulEjderiPose.Reared, 0f, 12f)]
    [TestCase(KulEjderiPose.ChestDown, 3f, 10f)]
    [TestCase(KulEjderiPose.Lying, -4f, 4f)]
    public void KE_2_6_Hit_point_lies_on_real_head_not_in_air(KulEjderiPose pose, float x, float z)
    {
        KulEjderiReach r = KulEjderiHurtboxes.Probe(KulEjderiHitZone.Head, pose, Ground(x, z), Sword);
        Assert.That(r.Reachable, Is.True);

        KulEjderiBox body = KulEjderiHurtboxes.Body(KulEjderiHitZone.Head, pose, 0)!.Value;
        Assert.That(body.Contains(r.HitPoint), Is.True, "vuruş noktası gerçek başın kutusunda");
    }
}
