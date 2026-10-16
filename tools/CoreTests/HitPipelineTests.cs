using Dovus.Core.Actors;
using Dovus.Core.Boss;
using Dovus.Core.Hit;
using Dovus.Core.Shared;
using NUnit.Framework;

namespace CoreTests;

/// <summary>Hasar hattı sözleşmesi: bölge, takım, i-frame, tekrar vuruş, ölüm olayı, can barı.</summary>
[TestFixture]
public class HitPipelineTests
{
    static readonly ActorId Player = new("player");
    static readonly ActorId Boss = new("boss");

    static Combatant Dummy(float hp = 100f, ActorTeam team = ActorTeam.Hostile, string id = "dummy") =>
        new(new ActorId(id), team, new HealthPool(hp));

    static HitEvent Swing(float amount, string hitId = "s1", string? zone = null, double rehit = 0d) =>
        new(hitId, Player, ActorTeam.Friendly, amount, zone, rehitIntervalMs: rehit);

    [Test]
    public void Hit_ReducesHp_AndReportsDealt()
    {
        var hub = new DamageHub();
        Combatant t = Dummy();
        HitResult r = hub.Submit(Swing(30f), t, 0d);
        Assert.That(r.Applied, Is.True);
        Assert.That(r.Dealt, Is.EqualTo(30f));
        Assert.That(r.HpBefore, Is.EqualTo(100f));
        Assert.That(r.HpAfter, Is.EqualTo(70f));
        Assert.That(t.Hp, Is.EqualTo(70f));
    }

    [Test]
    public void SameSwing_HitsTargetOnce_NewSwing_HitsAgain()
    {
        var hub = new DamageHub();
        Combatant t = Dummy();
        Assert.That(hub.Submit(Swing(10f), t, 0d).Applied, Is.True);
        HitResult dup = hub.Submit(Swing(10f), t, 16d);
        Assert.That(dup.Applied, Is.False);
        Assert.That(dup.Reason, Is.EqualTo(HitRejectReason.AlreadyHit));
        Assert.That(hub.Submit(Swing(10f, "s2"), t, 32d).Applied, Is.True);
        Assert.That(t.Hp, Is.EqualTo(80f));
    }

    [Test]
    public void SameSwing_HitsTwoDifferentTargets()
    {
        var hub = new DamageHub();
        Combatant a = Dummy(id: "a");
        Combatant b = Dummy(id: "b");
        Assert.That(hub.Submit(Swing(10f), a, 0d).Applied, Is.True);
        Assert.That(hub.Submit(Swing(10f), b, 0d).Applied, Is.True);
    }

    [Test]
    public void RehitInterval_AllowsRepeatOnlyAfterInterval()
    {
        var hub = new DamageHub();
        Combatant t = Dummy();
        Assert.That(hub.Submit(Swing(5f, "field", rehit: 500d), t, 0d).Applied, Is.True);
        Assert.That(hub.Submit(Swing(5f, "field", rehit: 500d), t, 499d).Applied, Is.False);
        Assert.That(hub.Submit(Swing(5f, "field", rehit: 500d), t, 500d).Applied, Is.True);
        Assert.That(t.Hp, Is.EqualTo(90f));
    }

    [Test]
    public void EndHit_ClearsRegistry_SoIdCanBeReused()
    {
        var hub = new DamageHub();
        Combatant t = Dummy();
        hub.Submit(Swing(10f), t, 0d);
        hub.Registry.EndHit("s1");
        Assert.That(hub.Submit(Swing(10f), t, 10d).Applied, Is.True);
    }

    [Test]
    public void EmptyHitId_IsNotDeduplicated()
    {
        var hub = new DamageHub();
        Combatant t = Dummy();
        Assert.That(hub.Submit(Swing(10f, ""), t, 0d).Applied, Is.True);
        Assert.That(hub.Submit(Swing(10f, ""), t, 0d).Applied, Is.True);
    }

    [Test]
    public void SameTeam_IsRejected_UnlessFriendlyFireAllowed()
    {
        var hub = new DamageHub();
        Combatant ally = Dummy(team: ActorTeam.Friendly, id: "ally");
        HitResult r = hub.Submit(Swing(10f), ally, 0d);
        Assert.That(r.Reason, Is.EqualTo(HitRejectReason.SameTeam));
        Assert.That(ally.Hp, Is.EqualTo(100f));

        var ff = new HitEvent("ff", Player, ActorTeam.Friendly, 10f, allowFriendlyFire: true);
        Assert.That(hub.Submit(ff, ally, 0d).Applied, Is.True);
    }

    [Test]
    public void Invulnerable_RejectsDodgeable_ButNotUndodgeable_AndDoesNotConsumeSwing()
    {
        var hub = new DamageHub();
        Combatant t = Dummy(team: ActorTeam.Friendly, id: "p2");
        t.InvulnerableCheck = (now, dodgeable) => dodgeable && now < 200d;
        var boss = new HitEvent("slam", Boss, ActorTeam.Hostile, 20f);
        Assert.That(hub.Submit(boss, t, 100d).Reason, Is.EqualTo(HitRejectReason.Invulnerable));
        var undodgeable = new HitEvent("nuke", Boss, ActorTeam.Hostile, 20f, dodgeable: false);
        Assert.That(hub.Submit(undodgeable, t, 100d).Applied, Is.True);
        // i-frame bitti: yutulan salıncak kaydedilmediği için hâlâ vurabilir
        Assert.That(hub.Submit(boss, t, 250d).Applied, Is.True);
        Assert.That(t.Hp, Is.EqualTo(60f));
    }

    [Test]
    public void Zone_FactorScalesDamage_UnknownZoneIsRejected()
    {
        var hub = new DamageHub();
        var zones = new HurtZoneTable().Add("head", 2f);
        var t = new Combatant(new ActorId("dragon"), ActorTeam.Hostile, new HealthPool(100f), zones);
        HitResult head = hub.Submit(Swing(10f, "a", "head"), t, 0d);
        Assert.That(head.Requested, Is.EqualTo(20f));
        Assert.That(head.ZoneId, Is.EqualTo("head"));
        Assert.That(hub.Submit(Swing(10f, "b"), t, 0d).Requested, Is.EqualTo(10f));
        HitResult bad = hub.Submit(Swing(10f, "c", "wing"), t, 0d);
        Assert.That(bad.Reason, Is.EqualTo(HitRejectReason.UnknownZone));
        Assert.That(t.Hp, Is.EqualTo(70f));
    }

    [Test]
    public void ZeroAmount_AndDeadTarget_AreRejectedWithReason()
    {
        var hub = new DamageHub();
        Combatant t = Dummy(10f);
        Assert.That(hub.Submit(Swing(0f), t, 0d).Reason, Is.EqualTo(HitRejectReason.ZeroAmount));
        hub.Submit(Swing(10f, "kill"), t, 0d);
        Assert.That(hub.Submit(Swing(10f, "late"), t, 1d).Reason, Is.EqualTo(HitRejectReason.TargetDead));
    }

    [Test]
    public void Death_RaisesActorDiedOnce_WithKiller_AndClampsDealt()
    {
        var hub = new DamageHub();
        Combatant t = Dummy(10f);
        int deaths = 0;
        ActorId victim = default;
        ActorId killer = default;
        hub.ActorDied += (v, k, _) => { deaths++; victim = v; killer = k; };

        HitResult r = hub.Submit(Swing(999f), t, 5d);
        hub.Submit(Swing(999f, "again"), t, 6d);

        Assert.That(r.Killed, Is.True);
        Assert.That(r.Dealt, Is.EqualTo(10f));
        Assert.That(t.IsDead, Is.True);
        Assert.That(deaths, Is.EqualTo(1));
        Assert.That(victim, Is.EqualTo(new ActorId("dummy")));
        Assert.That(killer, Is.EqualTo(Player));
    }

    [Test]
    public void Events_HitLandedAndHitRejected_FireOnTheirOwnPaths()
    {
        var hub = new DamageHub();
        Combatant t = Dummy();
        int landed = 0;
        int rejected = 0;
        hub.HitLanded += (_, _) => landed++;
        hub.HitRejected += (_, _) => rejected++;
        hub.Submit(Swing(10f), t, 0d);
        hub.Submit(Swing(10f), t, 1d);
        Assert.That(landed, Is.EqualTo(1));
        Assert.That(rejected, Is.EqualTo(1));
    }

    [Test]
    public void BossVitalsAdapter_RoutesThroughSameContract_AndFiresVitalsDied()
    {
        var hub = new DamageHub();
        var vitals = new BossVitals(50f);
        bool died = false;
        vitals.Died += () => died = true;
        var boss = new BossVitalsDamageable(Boss, vitals);

        HitResult r = hub.Submit(Swing(60f), boss, 0d);

        Assert.That(r.Killed, Is.True);
        Assert.That(r.Dealt, Is.EqualTo(50f));
        Assert.That(died, Is.True);
        Assert.That(boss.IsDead, Is.True);
        Assert.That(boss.Team, Is.EqualTo(ActorTeam.Hostile));
    }

    [Test]
    public void HealthPool_HealCapsAtMax_DeadStaysDeadUntilRevive_MaxHpClipDoesNotKill()
    {
        var h = new HealthPool(100f);
        h.ApplyDamage(30f);
        Assert.That(h.ApplyHeal(50f), Is.EqualTo(30f));
        h.SetMaxHp(40f);
        Assert.That(h.Hp, Is.EqualTo(40f));
        Assert.That(h.IsDead, Is.False);
        h.ApplyDamage(100f);
        Assert.That(h.ApplyHeal(10f), Is.EqualTo(0f));
        Assert.That(h.IsDead, Is.True);
        h.Revive();
        Assert.That(h.Hp, Is.EqualTo(40f));
    }

    [Test]
    public void HealthBar_TrailHoldsThenDrains_AndSnapsOnHeal()
    {
        var bar = new HealthBarModel();
        bar.Update(100f, 100f, 0d);
        bar.Update(50f, 100f, 1000d);
        Assert.That(bar.Ratio, Is.EqualTo(0.5f));
        Assert.That(bar.TrailRatio, Is.EqualTo(1f), "vuruş anında hayalet şerit eski oranda");

        bar.Update(50f, 100f, 1000d + HitDefaults.TrailHoldMs - 1d);
        Assert.That(bar.TrailRatio, Is.EqualTo(1f), "bekleme süresince sabit");

        bar.Update(50f, 100f, 1000d + HitDefaults.TrailHoldMs + 100d);
        Assert.That(bar.TrailRatio, Is.LessThan(1f));
        Assert.That(bar.TrailRatio, Is.GreaterThan(0.5f));

        bar.Update(50f, 100f, 60000d);
        Assert.That(bar.TrailRatio, Is.EqualTo(0.5f), "sonunda ön şeride erir");

        bar.Update(90f, 100f, 60016d);
        Assert.That(bar.TrailRatio, Is.EqualTo(0.9f).Within(0.0001f), "iyileşmede yapışır");
    }
}
