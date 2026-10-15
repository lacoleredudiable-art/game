using Dovus.Core.Team;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class TeamTaskTests
{
    static TeamTaskZone[] Stones() => new[]
    {
        new TeamTaskZone(0, -9f, 4f, 2f),
        new TeamTaskZone(1, 9f, 4f, 2f),
        new TeamTaskZone(2, 0f, -10f, 2f),
    };

    static TeamTaskMember M(int id, float x, float z, bool alive = true) => new TeamTaskMember(id, x, z, alive);

    static TeamTaskMember[] AllOnStones() => new[] { M(1, -9, 4), M(2, 9, 4), M(3, 0, -10) };

    static TeamTask Start(TeamTaskSpec spec)
    {
        var t = new TeamTask();
        t.Start(spec, 0.0);
        return t;
    }

    [Test]
    public void SleepSeal_OnePlayerLightsStonesInSequence()
    {
        var t = Start(TeamTaskSpec.SleepSeal(Stones()));
        double now = 0;
        // Tek kişi sırayla üç taşı yakar: her taşta 2 s + yürüyüş araları (toplam 15 s içinde).
        foreach (var (x, z) in new[] { (-9f, 4f), (9f, 4f), (0f, -10f) })
        {
            for (int i = 0; i < 21; i++)
            {
                now += 0.1;
                t.Tick(now, new[] { M(1, x, z) });
            }
            now += 1.0; // yürüyüş
            t.Tick(now, new[] { M(1, 100, 100) });
        }
        Assert.That(t.Status, Is.EqualTo(TeamTaskStatus.Succeeded));
        Assert.That(t.Progress, Is.EqualTo(3));
    }

    [Test]
    public void SleepSeal_LeavingResetsHold()
    {
        var t = Start(TeamTaskSpec.SleepSeal(Stones()));
        t.Tick(1.0, new[] { M(1, -9, 4) });
        t.Tick(1.5, new[] { M(1, -9, 4) }); // 1,5 s birikti
        t.Tick(3.0, new[] { M(1, 50, 50) }); // çıktı: sıfır
        t.Tick(4.0, new[] { M(1, -9, 4) });
        Assert.That(t.IsLit(0), Is.False);
        Assert.That(t.HoldProgress(0), Is.EqualTo(1.0).Within(1e-9));
    }

    [Test]
    public void SleepSeal_FailsAfterDeadline()
    {
        var t = Start(TeamTaskSpec.SleepSeal(Stones()));
        t.Tick(14.9, new[] { M(1, 50, 50) });
        Assert.That(t.Status, Is.EqualTo(TeamTaskStatus.Running));
        t.Tick(15.0, new[] { M(1, 50, 50) });
        Assert.That(t.Status, Is.EqualTo(TeamTaskStatus.Failed));
    }

    [Test]
    public void SleepSeal_DeadPlayerDoesNotLight()
    {
        var t = Start(TeamTaskSpec.SleepSeal(Stones()));
        t.Tick(1.0, new[] { M(1, -9, 4, alive: false) });
        t.Tick(5.0, new[] { M(1, -9, 4, alive: false) });
        Assert.That(t.IsLit(0), Is.False);
    }

    [Test]
    public void HeartSeal_RequiresAllThreeAtOnce()
    {
        var t = Start(TeamTaskSpec.HeartSeal(Stones()));
        t.Tick(1.0, AllOnStones());
        t.Tick(2.9, AllOnStones());
        Assert.That(t.Status, Is.EqualTo(TeamTaskStatus.Running));
        Assert.That(t.SyncHoldSec, Is.EqualTo(2.9).Within(1e-9));
        t.Tick(3.1, AllOnStones());
        Assert.That(t.Status, Is.EqualTo(TeamTaskStatus.Succeeded));
    }

    [Test]
    public void HeartSeal_OneLeavingResetsEveryone()
    {
        var t = Start(TeamTaskSpec.HeartSeal(Stones()));
        t.Tick(1.0, AllOnStones());
        t.Tick(2.5, AllOnStones());
        t.Tick(3.0, new[] { M(1, -9, 4), M(2, 9, 4), M(3, 50, 50) });
        Assert.That(t.SyncHoldSec, Is.EqualTo(0.0));
        Assert.That(t.Progress, Is.EqualTo(2));
    }

    [Test]
    public void HeartSeal_FailsAtTenSeconds()
    {
        var t = Start(TeamTaskSpec.HeartSeal(Stones()));
        t.Tick(10.0, new[] { M(1, -9, 4) });
        Assert.That(t.Status, Is.EqualTo(TeamTaskStatus.Failed));
    }

    [Test]
    public void AshRain_AllInShadowAtDeadline_Succeeds()
    {
        // Gölge şeridi: merkez (0,0), yarım genişlik 1,5, yarım uzunluk 3, yaw 0.
        var strip = new TeamTaskZone(0, 0f, 0f, 1.5f, 3f, 0f);
        var t = Start(TeamTaskSpec.AshRain(new[] { strip }));
        t.Tick(4.0, new[] { M(1, 0, 0), M(2, 1, 2) });
        Assert.That(t.Status, Is.EqualTo(TeamTaskStatus.Running));
        Assert.That(t.Progress, Is.EqualTo(2));
        t.Tick(8.0, new[] { M(1, 0, 0), M(2, 1, 2) });
        Assert.That(t.Status, Is.EqualTo(TeamTaskStatus.Succeeded));
    }

    [Test]
    public void AshRain_ExposedPlayerIsListedAndDeadAreIgnored()
    {
        var strip = new TeamTaskZone(0, 0f, 0f, 1.5f, 3f, 0f);
        var t = Start(TeamTaskSpec.AshRain(new[] { strip }));
        t.Tick(8.0, new[] { M(1, 0, 0), M(2, 10, 10), M(3, 10, 10, alive: false) });
        Assert.That(t.Status, Is.EqualTo(TeamTaskStatus.Failed));
        Assert.That(t.ExposedMemberIds, Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void BoxZone_RespectsYaw()
    {
        // 3 m geniş (yarım 1,5), 6 m uzun (yarım 3) şerit; 90° dönünce uzun kenar X eksenine yatar.
        var flat = new TeamTaskZone(0, 0f, 0f, 1.5f, 3f, 0f);
        var turned = new TeamTaskZone(0, 0f, 0f, 1.5f, 3f, (float)System.Math.PI / 2f);
        Assert.That(flat.Contains(0f, 2.5f), Is.True);
        Assert.That(flat.Contains(2.5f, 0f), Is.False);
        Assert.That(turned.Contains(2.5f, 0f), Is.True);
        Assert.That(turned.Contains(0f, 2.5f), Is.False);
    }

    [Test]
    public void FitToParticipants_ReducesHeartSealToNearestStones()
    {
        var spec = TeamTaskSpec.HeartSeal(Stones());
        var fitted = TeamTaskPlanner.FitToParticipants(spec, new[] { M(1, 8, 3), M(2, 7, 3) });
        Assert.That(fitted.Zones.Count, Is.EqualTo(2));
        Assert.That(fitted.Zones[0].Id, Is.EqualTo(1));
        // Üç kişi: değişmez. Uyku Mührü: değişmez.
        Assert.That(TeamTaskPlanner.FitToParticipants(spec, AllOnStones()), Is.SameAs(spec));
        var sleep = TeamTaskSpec.SleepSeal(Stones());
        Assert.That(TeamTaskPlanner.FitToParticipants(sleep, new[] { M(1, 0, 0) }), Is.SameAs(sleep));
    }

    [Test]
    public void AssignAllies_GivesDistinctNearestStones_AndPlayerKeepsHis()
    {
        var t = Start(TeamTaskSpec.HeartSeal(Stones()));
        var player = M(1, -8, 3); // sol taşa yakın
        var allies = new[] { M(2, 8, 3), M(3, -7, 2) }; // 3 numaralı da sol taşa yakın ama oyuncuya ayrıldı
        var map = TeamTaskPlanner.AssignAllies(t, allies, player);
        Assert.That(map[2], Is.EqualTo(1));
        Assert.That(map[3], Is.EqualTo(2));
    }

    [Test]
    public void AssignAllies_SleepSealSkipsLitStones()
    {
        var t = Start(TeamTaskSpec.SleepSeal(Stones()));
        t.Tick(1.0, new[] { M(1, -9, 4) });
        t.Tick(3.0, new[] { M(1, -9, 4) }); // sol taş yandı
        var map = TeamTaskPlanner.AssignAllies(t, new[] { M(2, -8, 3) }, null);
        Assert.That(map[2], Is.Not.EqualTo(0));
    }

    [Test]
    public void Penalty_FollowsDifficulty()
    {
        var train = TeamTaskPenalty.Resolve(TeamTaskKind.SleepSeal, TeamDifficulty.Training);
        Assert.That(TeamTaskPenalty.TeamDamage(1000, train), Is.EqualTo(0));
        Assert.That(train.Wipe, Is.False);

        var normal = TeamTaskPenalty.Resolve(TeamTaskKind.SleepSeal, TeamDifficulty.Normal);
        Assert.That(TeamTaskPenalty.TeamDamage(1000, normal), Is.EqualTo(400));

        var hardHeart = TeamTaskPenalty.Resolve(TeamTaskKind.HeartSeal, TeamDifficulty.Hard);
        Assert.That(TeamTaskPenalty.TeamDamage(1000, hardHeart), Is.EqualTo(700));
        Assert.That(hardHeart.BossHpResetRatio, Is.EqualTo(0.25f));
        Assert.That(TeamTaskPenalty.Resolve(TeamTaskKind.SleepSeal, TeamDifficulty.Hard).BossHpResetRatio, Is.EqualTo(0f));

        Assert.That(TeamTaskPenalty.Resolve(TeamTaskKind.HeartSeal, TeamDifficulty.Hell).Wipe, Is.True);
    }

    [Test]
    public void Penalty_AshRainExposedDieExceptInTraining()
    {
        Assert.That(TeamTaskPenalty.Resolve(TeamTaskKind.AshRain, TeamDifficulty.Training).ExposedDie, Is.False);
        Assert.That(TeamTaskPenalty.Resolve(TeamTaskKind.AshRain, TeamDifficulty.Normal).ExposedDie, Is.True);
        Assert.That(TeamTaskPenalty.Resolve(TeamTaskKind.SleepSeal, TeamDifficulty.Normal).ExposedDie, Is.False);
    }

    [Test]
    public void Schedule_FiresHpThresholdsOnceAndWaitsWhileBusy()
    {
        var s = TeamTaskSchedule.KulEjderi();
        Assert.That(s.Poll(0.90f, -1, false), Is.Null);
        Assert.That(s.Poll(0.84f, -1, true), Is.Null);               // meşgul: bekler
        Assert.That(s.Poll(0.84f, -1, false), Is.EqualTo(TeamTaskKind.SleepSeal));
        Assert.That(s.Poll(0.84f, -1, false), Is.Null);              // bir kez
        Assert.That(s.Poll(0.69f, -1, false), Is.EqualTo(TeamTaskKind.SleepSeal));
        Assert.That(s.Poll(0.49f, -1, false), Is.EqualTo(TeamTaskKind.AshRain));
        Assert.That(s.Poll(0.34f, -1, false), Is.EqualTo(TeamTaskKind.AshRain));
    }

    [Test]
    public void Schedule_HeartSealAtFiveSecondsThenEveryForty()
    {
        var s = TeamTaskSchedule.KulEjderi();
        for (int i = 0; i < 4; i++) // can eşikli dört görev önce tüketilir
            Assert.That(s.Poll(0.20f, -1, false), Is.Not.Null);
        Assert.That(s.Poll(0.20f, 4.9, false), Is.Null);
        Assert.That(s.Poll(0.20f, 5.0, false), Is.EqualTo(TeamTaskKind.HeartSeal));
        Assert.That(s.Poll(0.20f, 44.9, false), Is.Null);
        Assert.That(s.Poll(0.20f, 45.0, false), Is.EqualTo(TeamTaskKind.HeartSeal));
    }
}
