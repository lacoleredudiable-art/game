using System.Collections.Generic;
using Dovus.Core.Cancel;
using NUnit.Framework;

namespace CoreTests;

/// <summary>A7: iptal tablosu + girdi tamponu birlikte (zaman parametre, saat yok).</summary>
[TestFixture]
public sealed class ActionArbiterTests
{
    // Skill: hazırlık 200 + etki 100 + toparlanma 700 = 1000 ms. Silah değişimi: 250 ms toparlanma. Dodge: 400 ms.
    static ActionPlan Plan(ActionKind kind, int token, int nowMs) => kind switch
    {
        ActionKind.Skill => new ActionPlan(kind, token, 200, 100, 700),
        ActionKind.WeaponSwap => new ActionPlan(kind, token, 0, 0, 250),
        ActionKind.Dodge => new ActionPlan(kind, token, 0, 0, 400),
        _ => new ActionPlan(kind, token, 200, 0, 200),
    };

    static ActionArbiter Make(CancelOptions? o = null) =>
        new ActionArbiter(CancelRuleDefaults.Build(o), new InputBuffer(), Plan);

    [Test]
    public void Skill_phases_follow_plan_times()
    {
        var a = Make();
        a.Request(ActionKind.Skill, 1, 0);
        Assert.That(a.PhaseAt(0), Is.EqualTo(ActionPhase.Windup));
        Assert.That(a.PhaseAt(199), Is.EqualTo(ActionPhase.Windup));
        Assert.That(a.PhaseAt(200), Is.EqualTo(ActionPhase.Active));
        Assert.That(a.PhaseAt(300), Is.EqualTo(ActionPhase.Recovery));
        Assert.That(a.PhaseAt(999), Is.EqualTo(ActionPhase.Recovery));
        Assert.That(a.PhaseAt(1000), Is.EqualTo(ActionPhase.None));
    }

    [Test]
    public void Dodge_in_windup_cuts_skill_without_cooldown()
    {
        var a = Make();
        a.Request(ActionKind.Skill, 1, 0);
        ArbiterResult r = a.Request(ActionKind.Dodge, 0, 100);
        Assert.That(r.Outcome, Is.EqualTo(ArbiterOutcome.Cancelled));
        Assert.That(r.CutKind, Is.EqualTo(ActionKind.Skill));
        Assert.That(r.Decision.FizzlesRunning, Is.True);
        Assert.That(r.Decision.RefundsRunningCooldown, Is.True);
        Assert.That(a.Running.Kind, Is.EqualTo(ActionKind.Dodge));
    }

    [Test]
    public void Dodge_in_active_is_buffered_and_fires_when_recovery_opens()
    {
        var a = Make();
        a.Request(ActionKind.Skill, 1, 0);
        // etki fazı 200–300: dodge 250'de basıldı → reddedilir, tampona girer (150 ms).
        ArbiterResult r = a.Request(ActionKind.Dodge, 0, 250);
        Assert.That(r.Outcome, Is.EqualTo(ArbiterOutcome.Buffered));
        Assert.That(a.Tick(299).Outcome, Is.EqualTo(ArbiterOutcome.None));
        ArbiterResult fired = a.Tick(300);
        Assert.That(fired.Outcome, Is.EqualTo(ArbiterOutcome.Cancelled));
        Assert.That(fired.FromBuffer, Is.True);
        Assert.That(fired.Plan.Kind, Is.EqualTo(ActionKind.Dodge));
    }

    [Test]
    public void Buffered_input_expires_before_window_opens()
    {
        var a = Make();
        a.Request(ActionKind.Skill, 1, 0);
        a.Request(ActionKind.WeaponSwap, 0, 50); // hazırlıkta swap reddedilir → 150 ms tampon (bitiş 200)
        Assert.That(a.Tick(199).Outcome, Is.EqualTo(ArbiterOutcome.None));
        Assert.That(a.Tick(200).Outcome, Is.EqualTo(ArbiterOutcome.None), "süre doldu, etki fazı hâlâ kesilmez");
        Assert.That(a.Buffer.Count, Is.EqualTo(0));
        Assert.That(a.Tick(300).Outcome, Is.EqualTo(ArbiterOutcome.None), "tampon yok, kimse başlamaz");
    }

    [Test]
    public void Swap_cancels_recovery_then_own_recovery_blocks_next_skill_until_it_ends()
    {
        var a = Make();
        a.Request(ActionKind.Skill, 1, 0);
        ArbiterResult swap = a.Request(ActionKind.WeaponSwap, 0, 400);
        Assert.That(swap.Outcome, Is.EqualTo(ArbiterOutcome.Cancelled));

        // Değişim sürerken (400–650) yeni skill: reddedilir, tampona girer.
        ArbiterResult s2 = a.Request(ActionKind.Skill, 2, 600);
        Assert.That(s2.Outcome, Is.EqualTo(ArbiterOutcome.Buffered));
        ArbiterResult go = a.Tick(650);
        Assert.That(go.Outcome, Is.EqualTo(ArbiterOutcome.Started));
        Assert.That(go.Plan.Token, Is.EqualTo(2));
        Assert.That(go.FromBuffer, Is.True);
    }

    [Test]
    public void Swap_cannot_be_cut_by_dodge()
    {
        var a = Make();
        a.Request(ActionKind.WeaponSwap, 0, 0);
        Assert.That(a.Request(ActionKind.Dodge, 0, 100).Outcome, Is.EqualTo(ArbiterOutcome.Buffered));
        Assert.That(a.Running.Kind, Is.EqualTo(ActionKind.WeaponSwap));
    }

    [Test]
    public void Buffer_picks_highest_priority_when_several_wait()
    {
        var a = Make();
        a.Request(ActionKind.WeaponSwap, 0, 0); // 0–250, kimse kesemez
        a.Request(ActionKind.BasicStrike, 0, 100);
        a.Request(ActionKind.Skill, 5, 110);
        a.Request(ActionKind.Dodge, 0, 120);
        ArbiterResult r = a.Tick(250);
        Assert.That(r.Plan.Kind, Is.EqualTo(ActionKind.Dodge));
    }

    [Test]
    public void Skill_tap_during_skill_recovery_is_buffered_not_cut()
    {
        var a = Make();
        a.Request(ActionKind.Skill, 1, 0);
        ArbiterResult r = a.Request(ActionKind.Skill, 2, 900);
        Assert.That(r.Outcome, Is.EqualTo(ArbiterOutcome.Buffered));
        Assert.That(a.Tick(1000).Plan.Token, Is.EqualTo(2));
    }

    [Test]
    public void Interrupt_drops_running_and_buffer()
    {
        var a = Make();
        a.Request(ActionKind.Skill, 1, 0);
        a.Request(ActionKind.Dodge, 0, 250);
        a.Interrupt();
        Assert.That(a.IsRunning(260), Is.False);
        Assert.That(a.Buffer.Count, Is.EqualTo(0));
        Assert.That(a.Request(ActionKind.Skill, 3, 260).Outcome, Is.EqualTo(ArbiterOutcome.Started));
    }

    [Test]
    public void OnStarted_fires_for_buffered_and_direct_starts()
    {
        var a = Make();
        var seen = new List<ActionKind>();
        a.OnStarted = (p, t) => seen.Add(p.Kind);
        a.Request(ActionKind.WeaponSwap, 0, 0);
        a.Request(ActionKind.Skill, 1, 150);
        a.Tick(250);
        Assert.That(seen, Is.EqualTo(new[] { ActionKind.WeaponSwap, ActionKind.Skill }));
    }
}
