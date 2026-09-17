using System.Collections.Generic;
using Dovus.Core.Combat;
using Dovus.Core.Layers;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// Karabasan invisible_link + Hiçlik tear — sahip görevi kabul kriterleri.
/// </summary>
[TestFixture]
public class SpaceDirectorTests
{
    static SpaceDirector NewDir(int cap = 3) =>
        new(cap, new SpaceLayerTuning());

    [Test]
    public void Link_TwoSeconds_FourTicks_TwelveDamage()
    {
        var dir = NewDir();
        Assert.That(dir.TrySpawnLink(
            "karabasan_hat", 2f,
            0, 0, 0, 1, 0, 0,
            "player", "boss",
            drainPerTick: 3f, healPerTick: 1.5f, maxRangeM: 12f,
            out _), Is.True);

        var ticks = new List<SpaceLinkTick>();
        float drain = 0f;
        // 4 × 0.5s
        for (int i = 0; i < 4; i++)
        {
            ticks.Clear();
            dir.Tick(0.5f, 0, 0, 0, 1, 0, 0, ticks);
            Assert.That(ticks.Count, Is.EqualTo(1), $"tick {i + 1}");
            drain += ticks[0].Drain;
            Assert.That(ticks[0].Heal, Is.EqualTo(1.5f).Within(0.0001f));
        }

        Assert.That(drain, Is.EqualTo(12f).Within(0.0001f));
        Assert.That(dir.ActiveEffects.Count, Is.EqualTo(0), "2s bitince düşmeli");
    }

    [Test]
    public void Link_BeyondMaxRange_Breaks()
    {
        var dir = NewDir();
        Assert.That(dir.TrySpawnLink(
            "karabasan_hat", 2f,
            0, 0, 0, 1, 0, 0,
            "player", "boss", 3f, 1.5f, 12f, out _), Is.True);

        var ticks = new List<SpaceLinkTick>();
        dir.Tick(0.1f, 0, 0, 0, 13f, 0, 0, ticks); // 13m > 12
        Assert.That(dir.ActiveEffects.Count, Is.EqualTo(0));
        Assert.That(ticks.Count, Is.EqualTo(0));
    }

    [Test]
    public void Link_OwnerDamaged_Breaks()
    {
        var dir = NewDir();
        dir.TrySpawnLink("karabasan_hat", 2f, 0, 0, 0, 1, 0, 0, "player", "boss", 3f, 1.5f, 12f, out _);
        Assert.That(dir.BreakLinksOwnedBy("player"), Is.EqualTo(1));
        Assert.That(dir.ActiveEffects.Count, Is.EqualTo(0));
    }

    [Test]
    public void Tear_FirstCross_Thirty_SecondCross_Zero()
    {
        var dir = NewDir();
        Assert.That(dir.TrySpawnTear("hiclik_yarik", 3f, 0, 0, 0, 30f, out var tear), Is.True);

        Assert.That(dir.TryCrossTear(tear.RuntimeId, "boss"), Is.EqualTo(30f).Within(0.0001f));
        Assert.That(dir.TryCrossTear(tear.RuntimeId, "boss"), Is.EqualTo(0f));
        // Dost da kesilir — ayrı actor ilk seferde hasar alır.
        Assert.That(dir.TryCrossTear(tear.RuntimeId, "ally"), Is.EqualTo(30f).Within(0.0001f));
        Assert.That(dir.TryCrossTear(tear.RuntimeId, "ally"), Is.EqualTo(0f));
        Assert.That(dir.TryCrossTear(tear.RuntimeId, "player"), Is.EqualTo(30f).Within(0.0001f));
    }

    [Test]
    public void Tear_LastsThreeSeconds()
    {
        var dir = NewDir();
        dir.TrySpawnTear("hiclik_yarik", 3f, 0, 0, 0, 30f, out _);
        var ticks = new List<SpaceLinkTick>();
        dir.Tick(2.9f, 0, 0, 0, 0, 0, 0, ticks);
        Assert.That(dir.ActiveEffects.Count, Is.EqualTo(1));
        Assert.That(dir.ActiveEffects[0].RemainingSec, Is.EqualTo(0.1f).Within(0.001f));
        dir.Tick(0.1f, 0, 0, 0, 0, 0, 0, ticks);
        Assert.That(dir.ActiveEffects.Count, Is.EqualTo(0));
    }

    [Test]
    public void KindUtil_ParsesJsonTypes()
    {
        Assert.That(SpaceEffectKindUtil.TryParse("invisible_link", out var link), Is.True);
        Assert.That(link, Is.EqualTo(SpaceEffectKind.InvisibleLink));
        Assert.That(SpaceEffectKindUtil.TryParse("tear", out var tear), Is.True);
        Assert.That(tear, Is.EqualTo(SpaceEffectKind.Tear));
    }
}
