using System.IO;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Layers;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// manipulation_layers.zone_layer — ZoneDirector yaşam döngüsü (görsel yok).
/// Soft-cap = StateBridgeBoard MaxMarks deseni; movement tipleri konum API'sinde ayrışır.
/// </summary>
[TestFixture]
public class ZoneDirectorTests
{
    const float Radius = 2f;

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

    static ZoneDirector FromMotor() => new(LoadFull().MaxActiveZones);

    [Test]
    public void Motor_MaxActiveZones_IsFive()
    {
        Assert.That(LoadFull().MaxActiveZones, Is.EqualTo(5));
    }

    [Test]
    public void TrySpawn_WhenFull_DropsOldest()
    {
        var dir = FromMotor();
        Assert.That(dir.MaxActiveZones, Is.EqualTo(5));

        Assert.That(dir.TrySpawn("Kaya", ZoneMovement.Static, 10f, 0, 0, 0, Radius, out var first), Is.True);
        for (int i = 1; i < 5; i++)
            Assert.That(dir.TrySpawn("Çamur", ZoneMovement.Static, 8f, i, 0, 0, Radius, out _), Is.True);

        Assert.That(dir.ActiveZones.Count, Is.EqualTo(5));
        Assert.That(dir.ActiveZones[0].Id, Is.EqualTo(first.Id));

        Assert.That(dir.TrySpawn("Toz", ZoneMovement.Static, 8f, 9, 0, 0, Radius, out var newest), Is.True);
        Assert.That(dir.ActiveZones.Count, Is.EqualTo(5));
        Assert.That(dir.ActiveZones[0].Id, Is.Not.EqualTo(first.Id), "en eski düşmeli");
        Assert.That(dir.ActiveZones[4].Id, Is.EqualTo(newest.Id));
    }

    [Test]
    public void Tick_WhenRemainingHitsZero_RemovesZone()
    {
        var dir = new ZoneDirector(5);
        Assert.That(dir.TrySpawn("Pınar", ZoneMovement.Static, 10f, 1, 0, 2, Radius, out var z), Is.True);

        dir.Tick(4f);
        Assert.That(dir.ActiveZones.Count, Is.EqualTo(1));
        Assert.That(dir.ActiveZones[0].RemainingSec, Is.EqualTo(6f).Within(0.001f));

        dir.Tick(6f);
        Assert.That(dir.ActiveZones.Count, Is.EqualTo(0));
        Assert.That(dir.Remove(z.Id), Is.False);
    }

    [Test]
    public void MoveZone_PlayerDirected_UpdatesPosition()
    {
        var dir = new ZoneDirector(5);
        Assert.That(dir.TrySpawn("Buhar", ZoneMovement.PlayerDirected, 8f, 0, 0, 0, Radius, out var z), Is.True);

        Assert.That(dir.MoveZone(z.Id, 3f, 1f, 4f), Is.True);
        Assert.That(dir.ActiveZones[0].X, Is.EqualTo(3f).Within(0.001f));
        Assert.That(dir.ActiveZones[0].Y, Is.EqualTo(1f).Within(0.001f));
        Assert.That(dir.ActiveZones[0].Z, Is.EqualTo(4f).Within(0.001f));

        // follow API bu tipte çalışmaz
        Assert.That(dir.SetFollowTarget(z.Id, 9f, 0, 9f), Is.False);
        Assert.That(dir.ActiveZones[0].X, Is.EqualTo(3f).Within(0.001f));
    }

    [Test]
    public void MoveZone_Static_DoesNotMove()
    {
        var dir = new ZoneDirector(5);
        Assert.That(dir.TrySpawn("Kaya", ZoneMovement.Static, 10f, 1f, 0, 1f, Radius, out var z), Is.True);

        Assert.That(dir.MoveZone(z.Id, 5f, 0, 5f), Is.False);
        Assert.That(dir.SetFollowTarget(z.Id, 5f, 0, 5f), Is.False);
        Assert.That(dir.ActiveZones[0].X, Is.EqualTo(1f).Within(0.001f));
        Assert.That(dir.ActiveZones[0].Z, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void SetFollowTarget_FollowTarget_UpdatesPosition_MoveZoneDoesNot()
    {
        var dir = new ZoneDirector(5);
        Assert.That(dir.TrySpawn("Kor", ZoneMovement.FollowTarget, 4f, 0, 0, 0, Radius, out var z), Is.True);

        Assert.That(dir.MoveZone(z.Id, 2f, 0, 2f), Is.False, "follow_target MoveZone dinlemez");
        Assert.That(dir.ActiveZones[0].X, Is.EqualTo(0f).Within(0.001f));

        Assert.That(dir.SetFollowTarget(z.Id, 7f, 0.5f, -3f), Is.True);
        Assert.That(dir.ActiveZones[0].X, Is.EqualTo(7f).Within(0.001f));
        Assert.That(dir.ActiveZones[0].Y, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(dir.ActiveZones[0].Z, Is.EqualTo(-3f).Within(0.001f));
    }

    [Test]
    public void EmptyMovement_NormalizesToStatic()
    {
        // lav_halkasi JSON'da movement alanı yok → ParseZones boş string verir
        var dir = new ZoneDirector(5);
        Assert.That(dir.TrySpawn("Lav", "", 10f, 0, 0, 0, Radius, out var z), Is.True);
        Assert.That(z.Movement, Is.EqualTo(ZoneMovement.Static));
        Assert.That(dir.MoveZone(z.Id, 1, 0, 1), Is.False);
    }

    [Test]
    public void TrySpawn_RejectsNonPositiveDurationOrRadius()
    {
        var dir = new ZoneDirector(5);
        Assert.That(dir.TrySpawn("Kaya", ZoneMovement.Static, 0f, 0, 0, 0, Radius, out _), Is.False);
        Assert.That(dir.TrySpawn("Kaya", ZoneMovement.Static, 5f, 0, 0, 0, 0f, out _), Is.False);
        Assert.That(dir.ActiveZones.Count, Is.EqualTo(0));
    }

    [Test]
    public void TrySpawn_StoresCcKind_AndSurvivesTickMove()
    {
        var dir = new ZoneDirector(5);
        Assert.That(
            dir.TrySpawn("Kaya", ZoneMovement.Static, 10f, 0, 0, 0, Radius, out var z, "root"),
            Is.True);
        Assert.That(z.CcKind, Is.EqualTo("root"));
        Assert.That(dir.ActiveZones[0].CcKind, Is.EqualTo("root"));

        dir.Tick(1f);
        Assert.That(dir.ActiveZones[0].CcKind, Is.EqualTo("root"));
        Assert.That(dir.ActiveZones[0].RemainingSec, Is.EqualTo(9f).Within(0.001f));
    }

    [Test]
    public void TrySpawn_UnknownCcKind_BecomesEmpty()
    {
        var dir = new ZoneDirector(5);
        Assert.That(
            dir.TrySpawn("Lav", ZoneMovement.Static, 10f, 0, 0, 0, Radius, out var z, "burn"),
            Is.True);
        Assert.That(z.CcKind, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Remove_ById_Works()
    {
        var dir = new ZoneDirector(5);
        dir.TrySpawn("Sis", ZoneMovement.Static, 8f, 0, 0, 0, Radius, out var a);
        dir.TrySpawn("Mühür", ZoneMovement.Static, 8f, 1, 0, 0, Radius, out var b);
        Assert.That(dir.Remove(a.Id), Is.True);
        Assert.That(dir.ActiveZones.Count, Is.EqualTo(1));
        Assert.That(dir.ActiveZones[0].Id, Is.EqualTo(b.Id));
    }
}
