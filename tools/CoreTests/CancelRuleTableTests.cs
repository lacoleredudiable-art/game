using Dovus.Core.Cancel;
using NUnit.Framework;

namespace CoreTests;

/// <summary>13 §9 iptal kuralları: hangi eylem hangi fazda neyi keser.</summary>
[TestFixture]
public sealed class CancelRuleTableTests
{
    static CancelDecision Eval(CancelRuleTable t, ActionKind running, ActionPhase phase, ActionKind incoming) =>
        t.Evaluate(true, running, phase, incoming);

    [Test]
    public void Idle_anything_starts()
    {
        var t = CancelRuleDefaults.Build();
        foreach (ActionKind k in new[] { ActionKind.Dodge, ActionKind.WeaponSwap, ActionKind.Skill, ActionKind.BasicStrike })
            Assert.That(t.Evaluate(false, ActionKind.Skill, ActionPhase.None, k).Verdict, Is.EqualTo(CancelVerdict.Start));
    }

    [Test]
    public void Windup_is_cut_by_dodge_only()
    {
        var t = CancelRuleDefaults.Build();
        CancelDecision d = Eval(t, ActionKind.Skill, ActionPhase.Windup, ActionKind.Dodge);
        Assert.That(d.Verdict, Is.EqualTo(CancelVerdict.Cancel));
        Assert.That(d.FizzlesRunning, Is.True, "hazırlıkta kesilen skill atılmaz");
        Assert.That(d.RefundsRunningCooldown, Is.False, "Q2: iade yok, bekleme süresi yanar");

        foreach (ActionKind k in new[] { ActionKind.Skill, ActionKind.WeaponSwap, ActionKind.BasicStrike })
            Assert.That(Eval(t, ActionKind.Skill, ActionPhase.Windup, k).Verdict, Is.EqualTo(CancelVerdict.Deny), k.ToString());
    }

    [Test]
    public void Active_is_cut_by_dodge_only()
    {
        var t = CancelRuleDefaults.Build();
        CancelDecision d = Eval(t, ActionKind.Skill, ActionPhase.Active, ActionKind.Dodge);
        Assert.That(d.Verdict, Is.EqualTo(CancelVerdict.Cancel), "Q1: dodge etki anında da hemen keser");
        Assert.That(d.FizzlesRunning, Is.False, "etki zaten doğdu");
        Assert.That(d.RefundsRunningCooldown, Is.False);
        foreach (ActionKind k in new[] { ActionKind.Skill, ActionKind.WeaponSwap, ActionKind.BasicStrike })
            Assert.That(Eval(t, ActionKind.Skill, ActionPhase.Active, k).Verdict, Is.EqualTo(CancelVerdict.Deny), k.ToString());
    }

    [Test]
    public void Recovery_is_cut_by_dodge_and_swap_but_not_by_strike_or_skill()
    {
        var t = CancelRuleDefaults.Build();
        Assert.That(Eval(t, ActionKind.Skill, ActionPhase.Recovery, ActionKind.Dodge).Verdict, Is.EqualTo(CancelVerdict.Cancel));
        Assert.That(Eval(t, ActionKind.Skill, ActionPhase.Recovery, ActionKind.WeaponSwap).Verdict, Is.EqualTo(CancelVerdict.Cancel));
        CancelDecision strike = Eval(t, ActionKind.Skill, ActionPhase.Recovery, ActionKind.BasicStrike);
        Assert.That(strike.Verdict, Is.EqualTo(CancelVerdict.Deny), "Q3: düz vuruş toparlanmayı kesmez");
        Assert.That(strike.BufferMs, Is.EqualTo(CancelDefaults.BufferMs), "toparlanma bitince başlasın");
        CancelDecision skill = Eval(t, ActionKind.Skill, ActionPhase.Recovery, ActionKind.Skill);
        Assert.That(skill.Verdict, Is.EqualTo(CancelVerdict.Deny));
        Assert.That(skill.BufferMs, Is.EqualTo(CancelDefaults.BufferMs), "sıradaki skill tamponla bekler");
    }

    [Test]
    public void Recovery_cut_keeps_effects_and_cooldown()
    {
        var t = CancelRuleDefaults.Build();
        CancelDecision d = Eval(t, ActionKind.Skill, ActionPhase.Recovery, ActionKind.Dodge);
        Assert.That(d.FizzlesRunning, Is.False, "etki geçti");
        Assert.That(d.RefundsRunningCooldown, Is.False, "etki geçtiyse CD yanar");
    }

    [Test]
    public void WeaponSwap_own_recovery_cannot_be_cancelled_and_skill_strike_are_dropped()
    {
        var t = CancelRuleDefaults.Build();
        foreach (ActionKind k in new[] { ActionKind.Skill, ActionKind.BasicStrike })
            Assert.That(Eval(t, ActionKind.WeaponSwap, ActionPhase.Recovery, k).BufferMs, Is.EqualTo(0), $"Q4: swap sırasında {k} çalışmaz, saklanmaz");
        foreach (ActionKind k in new[] { ActionKind.Dodge, ActionKind.Skill, ActionKind.WeaponSwap, ActionKind.BasicStrike })
            foreach (ActionPhase p in new[] { ActionPhase.Windup, ActionPhase.Active, ActionPhase.Recovery })
                Assert.That(Eval(t, ActionKind.WeaponSwap, p, k).Verdict, Is.EqualTo(CancelVerdict.Deny), $"{k}/{p}");
    }

    [Test]
    public void Dodge_cuts_basic_strike_in_every_phase()
    {
        var t = CancelRuleDefaults.Build();
        foreach (ActionPhase p in new[] { ActionPhase.Windup, ActionPhase.Active, ActionPhase.Recovery })
            Assert.That(Eval(t, ActionKind.BasicStrike, p, ActionKind.Dodge).Verdict, Is.EqualTo(CancelVerdict.Cancel));
    }

    [Test]
    public void Y24_strike_tap_during_strike_is_queued_for_the_clip_length()
    {
        var t = CancelRuleDefaults.Build();
        CancelDecision d = Eval(t, ActionKind.BasicStrike, ActionPhase.Windup, ActionKind.BasicStrike);
        Assert.That(d.Verdict, Is.EqualTo(CancelVerdict.Deny));
        Assert.That(d.BufferMs, Is.EqualTo(CancelDefaults.StrikeQueueMs));
    }

    [Test]
    public void Options_flip_the_rules()
    {
        var o = new CancelOptions
        {
            WindupDodgeRefundsCooldown = true,
            DodgeCancelsSkillActive = false,
            SwapCancelsSkillRecovery = false,
            StrikeCancelsSkillRecovery = true,
            SkillCancelsStrike = false,
            SwapCancelsStrikeRecovery = false,
            BlockedInputBufferMs = 120,
            BufferMs = 90,
        };
        var t = CancelRuleDefaults.Build(o);
        Assert.That(Eval(t, ActionKind.Skill, ActionPhase.Windup, ActionKind.Dodge).RefundsRunningCooldown, Is.True);
        Assert.That(Eval(t, ActionKind.Skill, ActionPhase.Active, ActionKind.Dodge).Verdict, Is.EqualTo(CancelVerdict.Deny));
        CancelDecision swap = Eval(t, ActionKind.Skill, ActionPhase.Recovery, ActionKind.WeaponSwap);
        Assert.That(swap.Verdict, Is.EqualTo(CancelVerdict.Deny));
        Assert.That(swap.BufferMs, Is.EqualTo(90));
        Assert.That(Eval(t, ActionKind.Skill, ActionPhase.Recovery, ActionKind.BasicStrike).Verdict, Is.EqualTo(CancelVerdict.Cancel));
        Assert.That(Eval(t, ActionKind.BasicStrike, ActionPhase.Windup, ActionKind.Skill).Verdict, Is.EqualTo(CancelVerdict.Deny));
        Assert.That(Eval(t, ActionKind.BasicStrike, ActionPhase.Recovery, ActionKind.WeaponSwap).Verdict, Is.EqualTo(CancelVerdict.Deny));
        Assert.That(Eval(t, ActionKind.Dodge, ActionPhase.Recovery, ActionKind.Skill).BufferMs, Is.EqualTo(120));
    }

    [Test]
    public void Skill_cuts_basic_strike_Q3_before_hit_fizzles_after_hit_lands()
    {
        var t = CancelRuleDefaults.Build();
        CancelDecision early = Eval(t, ActionKind.BasicStrike, ActionPhase.Windup, ActionKind.Skill);
        Assert.That(early.Verdict, Is.EqualTo(CancelVerdict.Cancel));
        Assert.That(early.FizzlesRunning, Is.True, "vuruş karesinden önce: düz vuruş etki etmez");
        CancelDecision late = Eval(t, ActionKind.BasicStrike, ActionPhase.Recovery, ActionKind.Skill);
        Assert.That(late.Verdict, Is.EqualTo(CancelVerdict.Cancel));
        Assert.That(late.FizzlesRunning, Is.False, "vuruş karesinden sonra: etki eder, animasyon skill'e geçer");
    }

    [Test]
    public void Swap_during_strike_Q4_cuts_only_after_hit_frame()
    {
        var t = CancelRuleDefaults.Build();
        CancelDecision early = Eval(t, ActionKind.BasicStrike, ActionPhase.Windup, ActionKind.WeaponSwap);
        Assert.That(early.Verdict, Is.EqualTo(CancelVerdict.Deny));
        Assert.That(early.BufferMs, Is.EqualTo(CancelDefaults.BufferMs));
        Assert.That(Eval(t, ActionKind.BasicStrike, ActionPhase.Recovery, ActionKind.WeaponSwap).Verdict, Is.EqualTo(CancelVerdict.Cancel));
    }

    [Test]
    public void Dodge_blocks_skill_and_strike_without_buffering_Q4()
    {
        var t = CancelRuleDefaults.Build();
        foreach (ActionKind k in new[] { ActionKind.Skill, ActionKind.BasicStrike })
            foreach (ActionPhase p in new[] { ActionPhase.Windup, ActionPhase.Active, ActionPhase.Recovery })
            {
                CancelDecision d = Eval(t, ActionKind.Dodge, p, k);
                Assert.That(d.Verdict, Is.EqualTo(CancelVerdict.Deny), $"{k}/{p}");
                Assert.That(d.BufferMs, Is.EqualTo(0), $"{k}/{p}");
            }
    }

    [Test]
    public void Priority_order_is_dodge_then_swap_then_skill_then_strike()
    {
        var t = CancelRuleDefaults.Build();
        Assert.That(t.PriorityOf(ActionKind.Dodge), Is.LessThan(t.PriorityOf(ActionKind.WeaponSwap)));
        Assert.That(t.PriorityOf(ActionKind.WeaponSwap), Is.LessThan(t.PriorityOf(ActionKind.Skill)));
        Assert.That(t.PriorityOf(ActionKind.Skill), Is.LessThan(t.PriorityOf(ActionKind.BasicStrike)));
    }

    [Test]
    public void Every_kind_pair_in_every_running_phase_has_a_rule()
    {
        // Tablo eksiksiz: satırsız çift sessizce reddedilmez, burada yakalanır (O-3.11 mantığı).
        var t = CancelRuleDefaults.Build();
        foreach (ActionKind run in new[] { ActionKind.Dodge, ActionKind.WeaponSwap, ActionKind.Skill, ActionKind.BasicStrike })
        foreach (ActionKind inc in new[] { ActionKind.Dodge, ActionKind.WeaponSwap, ActionKind.Skill, ActionKind.BasicStrike })
        foreach (ActionPhase p in new[] { ActionPhase.Windup, ActionPhase.Active, ActionPhase.Recovery })
            Assert.That(Eval(t, run, p, inc).HasRule, Is.True, $"{run}/{p} -> {inc}");
    }
}
