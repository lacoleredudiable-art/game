using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace CoreTests;

/// <summary>Boss saldırı türü seçimi (Slam / FireCone / Volley) ve Zehir Tükürüğü verisi.</summary>
[TestFixture]
public sealed class BossAttackKindPickerTests
{
    [Test]
    public void PhaseSets_MatchKaradulPhases()
    {
        Assert.That(BossAttackKindPicker.AllowedFor(false).ToArray(),
            Is.EquivalentTo(new[] { BossAttackKind.Slam, BossAttackKind.Volley }));
        Assert.That(BossAttackKindPicker.AllowedFor(true).ToArray(),
            Is.EquivalentTo(new[] { BossAttackKind.Slam, BossAttackKind.FireCone, BossAttackKind.Volley }));
    }

    [Test]
    public void Picks_AllThreeKinds_OnlyFromAllowed()
    {
        var rng = new Random(7);
        var seen = new HashSet<BossAttackKind>();
        BossAttackKind? last = null;
        int streak = 0;
        for (int i = 0; i < 300; i++)
        {
            BossAttackKind k = BossAttackKindPicker.Pick(last, streak, 2, rng, BossAttackKindPicker.AllowedFor(true));
            seen.Add(k);
            streak = BossAttackKindPicker.NextStreak(last, streak, k);
            last = k;
        }
        Assert.That(seen, Is.EquivalentTo(new[] { BossAttackKind.Slam, BossAttackKind.FireCone, BossAttackKind.Volley }));

        var calm = new HashSet<BossAttackKind>();
        for (int i = 0; i < 200; i++)
            calm.Add(BossAttackKindPicker.Pick(null, 0, 2, rng, BossAttackKindPicker.AllowedFor(false)));
        Assert.That(calm, Does.Not.Contain(BossAttackKind.FireCone));
    }

    [Test]
    public void StreakCap_IsRespected()
    {
        var rng = new Random(1);
        BossAttackKind? last = null;
        int streak = 0, longest = 0;
        for (int i = 0; i < 500; i++)
        {
            BossAttackKind k = BossAttackKindPicker.Pick(last, streak, 2, rng, BossAttackKindPicker.AllowedFor(false));
            streak = BossAttackKindPicker.NextStreak(last, streak, k);
            last = k;
            longest = Math.Max(longest, streak);
        }
        Assert.That(longest, Is.LessThanOrEqualTo(2));
    }

    [Test]
    public void SingleAllowed_AlwaysThatKind()
    {
        var rng = new Random(3);
        ReadOnlySpan<BossAttackKind> only = new[] { BossAttackKind.Volley };
        for (int i = 0; i < 20; i++)
            Assert.That(BossAttackKindPicker.Pick(BossAttackKind.Volley, 5, 2, rng, only), Is.EqualTo(BossAttackKind.Volley));
    }

    [Test]
    public void Volley_IsStandingSpecial_SilenceBlocks_DisarmDoesNot()
    {
        Assert.That(BossAttackControl.MotionOf(BossAttackKind.Volley), Is.EqualTo(BossAttackMotion.Standing));
        var silenced = new StatusBoard();
        silenced.Apply(StatusKind.Silence, 1000, 1f);
        Assert.That(BossAttackControl.Gate(silenced, BossAttackMotion.Standing, BossAttackKind.Volley, false).CanStart, Is.False);
        var disarmed = new StatusBoard();
        disarmed.Apply(StatusKind.Disarm, 1000, 1f);
        if (disarmed.HasDisarm)
            Assert.That(BossAttackControl.Gate(disarmed, BossAttackMotion.Standing, BossAttackKind.Volley, false).CanStart, Is.True);
    }

    [Test]
    public void ApplyVolley_UsesTuning_CountEnraged()
    {
        var t = new BossTuning();
        var a = new BossAttack(t);
        a.ApplyVolley(false);
        Assert.That(a.Kind, Is.EqualTo(BossAttackKind.Volley));
        Assert.That(a.WindupMs, Is.EqualTo(700));
        Assert.That(a.VolleyCount, Is.EqualTo(3));
        Assert.That(a.Damage, Is.EqualTo(6));
        Assert.That(a.ArcHalfAngleDeg, Is.EqualTo(15f));
        a.ApplyVolley(true);
        Assert.That(a.VolleyCount, Is.EqualTo(5));
        a.ApplyVariant(SlamVariant.Yakin);
        Assert.That(a.VolleyCount, Is.EqualTo(0));
        Assert.That(a.Damage, Is.EqualTo(22));
    }

    [Test]
    public void KaradulVolley_MatchesBossTuningDefaults()
    {
        string root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        string json = File.ReadAllText(Path.Combine(root, "unity", "Assets", "Resources", "Bosses", "karadul.json"));
        var fromJson = new BossTuning { VolleyCount = 0, VolleyDamage = 0, VolleyWindupMs = 0 };
        Assert.That(BossVolleyData.Apply(MiniJson.Parse(json), fromJson), Is.True);
        var d = new BossTuning();
        Assert.Multiple(() =>
        {
            Assert.That(fromJson.VolleyWindupMs, Is.EqualTo(d.VolleyWindupMs));
            Assert.That(fromJson.VolleyCount, Is.EqualTo(d.VolleyCount));
            Assert.That(fromJson.VolleyCountEnraged, Is.EqualTo(d.VolleyCountEnraged));
            Assert.That(fromJson.VolleySpreadDeg, Is.EqualTo(d.VolleySpreadDeg));
            Assert.That(fromJson.VolleySpeedMps, Is.EqualTo(d.VolleySpeedMps));
            Assert.That(fromJson.VolleyDamage, Is.EqualTo(d.VolleyDamage));
            Assert.That(fromJson.VolleyRadiusM, Is.EqualTo(d.VolleyRadiusM));
            Assert.That(fromJson.VolleyLifeSec, Is.EqualTo(d.VolleyLifeSec));
        });
    }
}
