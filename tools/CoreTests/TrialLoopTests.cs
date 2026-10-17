using Dovus.Core.Trial;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class TrialLoopTests
{
    const float Far = 30f;

    static WeaponLoadout NewLoadout(bool filled = true)
    {
        var l = new WeaponLoadout(new[] { "kilic", "yay", "kalkan" });
        if (filled)
        {
            l.TrySelect(0, "kilic");
            l.TrySelect(1, "yay");
        }
        return l;
    }

    static TrialTick T(double ms, float dist = Far, bool hit = false, bool pDead = false, bool bDead = false, float hp = 1f)
        => new(ms, dist, hit, pDead, bDead, hp);

    static TrialLoop ToFight(out double now, int maxRevives = 0)
    {
        var loop = new TrialLoop(NewLoadout(), maxRevives);
        Assert.That(loop.TryStart(0), Is.True);
        loop.Tick(T(100, 5f));
        now = 100 + 2000;
        loop.Tick(T(now, 5f));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.Fight));
        return loop;
    }

    [Test]
    public void Loadout_RejectsUnknownAndDuplicateWeapons()
    {
        var l = NewLoadout(false);
        Assert.That(l.TrySelect(0, "yok"), Is.False);
        Assert.That(l.TrySelect(2, "kilic"), Is.False);
        Assert.That(l.TrySelect(0, "kilic"), Is.True);
        Assert.That(l.TrySelect(1, "kilic"), Is.False);
        Assert.That(l.IsComplete, Is.False);
        Assert.That(l.TrySelect(1, "yay"), Is.True);
        Assert.That(l.IsComplete, Is.True);
    }

    [Test]
    public void Loadout_SwapExchangesActiveAndReserve()
    {
        var l = NewLoadout();
        Assert.That((l.Active, l.Reserve), Is.EqualTo(("kilic", "yay")));
        Assert.That(l.TrySwap(false), Is.False);
        Assert.That(l.TrySwap(), Is.True);
        Assert.That((l.Active, l.Reserve), Is.EqualTo(("yay", "kilic")));
        Assert.That(l.SwapCount, Is.EqualTo(1));
        l.ResetForAttempt();
        Assert.That(l.Active, Is.EqualTo("kilic"));
    }

    [Test]
    public void Loadout_SwapNeedsTwoWeapons()
    {
        var l = NewLoadout(false);
        l.TrySelect(0, "kilic");
        Assert.That(l.TrySwap(), Is.False);
    }

    [Test]
    public void Start_NeedsCompleteLoadout()
    {
        var loop = new TrialLoop(NewLoadout(false));
        Assert.That(loop.TryStart(0), Is.False);
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.WeaponSelect));
    }

    [Test]
    public void Start_BossAsleepAndFirstAttempt()
    {
        var loop = new TrialLoop(NewLoadout());
        loop.TryStart(0);
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.BossAsleep));
        Assert.That(loop.Attempts, Is.EqualTo(1));
        loop.Tick(T(5000, Far));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.BossAsleep));
    }

    [Test]
    public void Boss_WakesWithinSixMetres_O17_1()
    {
        var loop = new TrialLoop(NewLoadout());
        loop.TryStart(0);
        loop.Tick(T(100, 6.01f));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.BossAsleep));
        loop.Tick(T(200, 6f));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.BossWaking));
    }

    [Test]
    public void Boss_WakesWhenHit_O17_1()
    {
        var loop = new TrialLoop(NewLoadout());
        loop.TryStart(0);
        loop.Tick(T(100, Far, hit: true));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.BossWaking));
    }

    [Test]
    public void Waking_TakesTwoSeconds_PlayerInvulnerable()
    {
        var loop = new TrialLoop(NewLoadout());
        loop.TryStart(0);
        loop.Tick(T(0, 3f));
        Assert.That(loop.PlayerInvulnerable, Is.True);
        Assert.That(loop.CombatActive, Is.False);
        loop.Tick(T(1999, 3f, pDead: true));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.BossWaking));
        loop.Tick(T(2000, 3f));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.Fight));
        Assert.That(loop.PlayerInvulnerable, Is.False);
        Assert.That(loop.CombatActive, Is.True);
    }

    [Test]
    public void PlayerDeath_NoRevive_DeathMenuAfterTwoSeconds_O17_2()
    {
        var loop = ToFight(out double now);
        loop.Tick(T(now + 10, pDead: true, hp: 0.4f));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.PlayerDying));
        Assert.That(loop.LastResult!.Value.Won, Is.False);
        Assert.That(loop.LastResult!.Value.BossHp01, Is.EqualTo(0.4f));
        loop.Tick(T(now + 2009, pDead: true));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.PlayerDying));
        loop.Tick(T(now + 2010, pDead: true));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.DeathMenu));
    }

    [Test]
    public void Retry_BackInArenaWithinThreeSeconds_NewAttempt_O17_2()
    {
        var loop = ToFight(out double now);
        loop.NoteSkillCast();
        loop.Tick(T(now + 10, pDead: true));
        loop.Tick(T(now + 2010, pDead: true));
        Assert.That(loop.TryRetry(now + 2100), Is.True);
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.Retrying));
        loop.Tick(T(now + 2100 + 2999));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.Retrying));
        loop.Tick(T(now + 2100 + 3000));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.BossAsleep));
        Assert.That(loop.Attempts, Is.EqualTo(2));
        Assert.That(loop.Stats.SkillCount, Is.EqualTo(0));
        Assert.That(loop.LastResult, Is.Null);
    }

    [Test]
    public void ChangeWeapons_ReturnsToSelectKeepingChoice()
    {
        var loop = ToFight(out double now);
        loop.Tick(T(now + 10, pDead: true));
        loop.Tick(T(now + 2010, pDead: true));
        Assert.That(loop.TryChangeWeapons(now + 2100), Is.True);
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.WeaponSelect));
        Assert.That(loop.Loadout.TrySelect(1, "kalkan"), Is.True);
        Assert.That(loop.TryStart(now + 3000), Is.True);
        Assert.That(loop.Attempts, Is.EqualTo(2));
        Assert.That(loop.Loadout.Reserve, Is.EqualTo("kalkan"));
    }

    [Test]
    public void Buttons_OnlyInDeathMenuOrResult()
    {
        var loop = ToFight(out double now);
        Assert.That(loop.TryRetry(now), Is.False);
        Assert.That(loop.TryChangeWeapons(now), Is.False);
        Assert.That(loop.TryStart(now), Is.False);
    }

    [Test]
    public void BossDeath_ResultAfterTwoSeconds_WithStats_O17_3()
    {
        var loop = ToFight(out double now);
        loop.NoteSkillCast();
        loop.NoteSkillCast();
        loop.NoteSkillCast();
        loop.NoteDamage("1-1", 100f);
        loop.NoteDamage("3-9", 250f);
        loop.NoteDamage("1-1", 50f);
        loop.Tick(T(now + 60_000, bDead: true, hp: 0f));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.BossDying));
        loop.Tick(T(now + 61_999, bDead: true));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.BossDying));
        loop.Tick(T(now + 62_000, bDead: true));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.Result));

        TrialResult r = loop.LastResult!.Value;
        Assert.That(r.Won, Is.True);
        Assert.That(r.FightSec, Is.EqualTo(60.0).Within(1e-9));
        Assert.That(r.SkillCount, Is.EqualTo(3));
        Assert.That(r.TopCombo, Is.EqualTo("3-9"));
        Assert.That(r.TopComboDamage, Is.EqualTo(250f));
        Assert.That(r.Attempt, Is.EqualTo(1));
        Assert.That(r.BossHp01, Is.EqualTo(0f));
    }

    [Test]
    public void Result_RetryStartsNextAttempt()
    {
        var loop = ToFight(out double now);
        loop.Tick(T(now + 1000, bDead: true));
        loop.Tick(T(now + 3000, bDead: true));
        Assert.That(loop.TryRetry(now + 3100), Is.True);
        loop.Tick(T(now + 6100));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.BossAsleep));
        Assert.That(loop.Attempts, Is.EqualTo(2));
    }

    [Test]
    public void BothDeadSameFrame_BossDeathCounts()
    {
        var loop = ToFight(out double now);
        loop.Tick(T(now + 10, pDead: true, bDead: true));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.BossDying));
        Assert.That(loop.LastResult!.Value.Won, Is.True);
    }

    [Test]
    public void Stats_OnlyCountedDuringFight()
    {
        var loop = new TrialLoop(NewLoadout());
        loop.TryStart(0);
        loop.NoteSkillCast();
        loop.NoteDamage("1-1", 10f);
        Assert.That(loop.Stats.SkillCount, Is.EqualTo(0));
        Assert.That(loop.Stats.TopCombo(out float d), Is.Null);
        Assert.That(d, Is.EqualTo(0f));
    }

    [Test]
    public void TopCombo_TieBreaksAlphabetically()
    {
        var s = new TrialStats();
        s.NoteDamage("b", 5f);
        s.NoteDamage("a", 5f);
        Assert.That(s.TopCombo(out _), Is.EqualTo("a"));
    }

    [Test]
    public void Revive_DefaultZero_DeathIsFinal()
    {
        Assert.That(TrialDefaults.MaxRevives, Is.EqualTo(0));
        var loop = ToFight(out double now);
        loop.Tick(T(now + 10, pDead: true));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.PlayerDying));
        Assert.That(loop.ReviveRequested, Is.False);
    }

    [Test]
    public void Revive_LimitedCount_ThenDeath()
    {
        var loop = ToFight(out double now, maxRevives: 1);
        loop.Tick(T(now + 10, pDead: true));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.Fight));
        Assert.That(loop.ReviveRequested, Is.True);
        Assert.That(loop.RevivesUsed, Is.EqualTo(1));
        // sahne canı yenileyene kadar ölü bayrağı sürerse ikinci hak yanmaz
        loop.Tick(T(now + 20, pDead: true));
        Assert.That(loop.RevivesUsed, Is.EqualTo(1));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.Fight));
        loop.AckRevive();
        loop.Tick(T(now + 30, pDead: true));
        Assert.That(loop.Phase, Is.EqualTo(TrialPhase.PlayerDying));
    }

    [Test]
    public void Revive_CountResetsEachAttempt()
    {
        var loop = ToFight(out double now, maxRevives: 1);
        loop.Tick(T(now + 10, pDead: true));
        loop.AckRevive();
        loop.Tick(T(now + 20, pDead: true));
        loop.Tick(T(now + 2020, pDead: true));
        loop.TryRetry(now + 2100);
        loop.Tick(T(now + 5100));
        Assert.That(loop.RevivesUsed, Is.EqualTo(0));
    }

    [Test]
    public void SwapWeapon_OnlyInFight_ResetEachAttempt()
    {
        var loop = new TrialLoop(NewLoadout());
        loop.TryStart(0);
        Assert.That(loop.TrySwapWeapon(), Is.False);
        loop.Tick(T(10, 1f));
        loop.Tick(T(2010, 1f));
        Assert.That(loop.TrySwapWeapon(), Is.True);
        Assert.That(loop.Loadout.Active, Is.EqualTo("yay"));
        loop.Tick(T(2020, pDead: true));
        loop.Tick(T(4020, pDead: true));
        loop.TryRetry(4100);
        loop.Tick(T(7100));
        Assert.That(loop.Loadout.Active, Is.EqualTo("kilic"));
    }

    [Test]
    public void PhaseChanged_FiresInOrder()
    {
        var seen = new System.Collections.Generic.List<TrialPhase>();
        var loop = new TrialLoop(NewLoadout());
        loop.PhaseChanged += (_, next) => seen.Add(next);
        loop.TryStart(0);
        loop.Tick(T(1, 2f));
        loop.Tick(T(2001, 2f));
        Assert.That(seen, Is.EqualTo(new[] { TrialPhase.BossAsleep, TrialPhase.BossWaking, TrialPhase.Fight }));
    }
}
