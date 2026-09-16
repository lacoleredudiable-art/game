using System.IO;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// docs/prezentasyon-katmani.json — 16 trajectory × 16 hitbox + 10 animasyon.
/// </summary>
[TestFixture]
public class PresentationCatalogTests
{
    static PresentationCatalog LoadFull()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "prezentasyon-katmani.json"));
        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "prezentasyon-katmani.json"));
        }
        Assert.That(File.Exists(path), Is.True, $"prezentasyon-katmani.json bulunamadı: {path}");
        return PresentationCatalog.FromJson(File.ReadAllText(path));
    }

    [Test]
    public void FullJson_CountsMatchJson_16Trajectories_16Hitboxes_10Animations()
    {
        var catalog = LoadFull();
        Assert.That(catalog.Trajectories.Count, Is.EqualTo(16));
        Assert.That(catalog.Hitboxes.Count, Is.EqualTo(16));
        Assert.That(catalog.Animations.Count, Is.EqualTo(10));

        Assert.That(catalog.TryGetTrajectory("duz", out TrajectoryNode duz), Is.True);
        Assert.That(duz.Name, Is.EqualTo("Düz İlerleme"));
        Assert.That(duz.MotionCurve, Is.EqualTo("linear"));
        Assert.That(duz.SpeedMpsDefault, Is.EqualTo(15f).Within(0.0001f));
        Assert.That(duz.GetBool("affected_by_gravity"), Is.False);
        Assert.That(duz.GetString("vfx_trail_type"), Is.EqualTo("straight"));

        Assert.That(catalog.TryGetHitbox("projectile", out HitboxNode projectile), Is.True);
        Assert.That(projectile.Shape, Is.EqualTo("sphere"));
        Assert.That(projectile.GetFloat("radius_m_default"), Is.EqualTo(0.5f).Within(0.0001f));
    }

    [Test]
    public void Matrix_DuzProjectileCompatible_DuzRaycastIncompatible()
    {
        var catalog = LoadFull();
        Assert.That(
            catalog.TryGetCompatibility("duz", "projectile"),
            Is.EqualTo(CompatibilityResult.Compatible));
        Assert.That(
            catalog.TryGetCompatibility("duz", "raycast"),
            Is.EqualTo(CompatibilityResult.Incompatible));
        Assert.That(
            catalog.TryGetCompatibility("duz", "single_target"),
            Is.EqualTo(CompatibilityResult.Special));
    }

    [Test]
    public void Animation_Channel_DamageAppliedAtFrameIsEveryTickString()
    {
        var catalog = LoadFull();
        Assert.That(catalog.TryGetAnimation("channel", out AnimationFrameNode channel), Is.True);
        Assert.That(channel.TotalFrames, Is.EqualTo(60));
        Assert.That(channel.ActiveFrames, Is.EqualTo(new[] { 16, 55 }));
        Assert.That(channel.CancelWindow, Is.Null);
        Assert.That(channel.DamageAppliedAtFrame.Kind, Is.EqualTo(JsonKind.String));
        Assert.That(channel.DamageAppliedAtFrame.AsString(), Is.EqualTo("every_tick"));
        Assert.That(channel.SpawnVfxAtFrame, Is.EqualTo(16));
        Assert.That(channel.AnimatorState, Is.EqualTo("Channel_Loop"));
    }
}
