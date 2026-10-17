using Dovus.Core.Cancel;
using NUnit.Framework;

namespace CoreTests;

/// <summary>A5 / O-4.14 düz vuruş zinciri + Y24 sıraya alma.</summary>
[TestFixture]
public sealed class BasicStrikeComboTests
{
    [Test]
    public void O_4_14_Sword_cycles_three_steps_with_hit_frame_at_200()
    {
        var c = new BasicStrikeCombo(StrikeComboDef.Sword());
        ActionPlan p = c.Begin(0);
        Assert.That(p.WindupMs, Is.EqualTo(CancelDefaults.SwordStrikeHitMs));
        Assert.That(p.TotalMs, Is.EqualTo(CancelDefaults.SwordStrikeDurationMs));
        Assert.That(c.NextStep, Is.EqualTo(1));
        c.Begin(400);
        c.Begin(800);
        Assert.That(c.NextStep, Is.EqualTo(0), "üçüncüden sonra başa sarar");
    }

    [Test]
    public void O_4_14_Chain_resets_after_reset_window()
    {
        var c = new BasicStrikeCombo(StrikeComboDef.Sword());
        c.Begin(0);
        c.Begin(400);
        Assert.That(c.NextStep, Is.EqualTo(2));
        c.Begin(400 + CancelDefaults.StrikeComboResetMs + 1);
        Assert.That(c.NextStep, Is.EqualTo(1), "pencere aşıldı: yeni zincirin ilk adımı oynandı");
    }

    [Test]
    public void O_4_14_Tap_exactly_at_reset_window_keeps_chain()
    {
        var c = new BasicStrikeCombo(StrikeComboDef.Sword());
        c.Begin(0);
        c.Begin(CancelDefaults.StrikeComboResetMs);
        Assert.That(c.NextStep, Is.EqualTo(2));
    }

    [Test]
    public void Single_step_weapon_always_plays_step_zero()
    {
        var c = new BasicStrikeCombo(StrikeComboDef.Single(500, 250));
        Assert.That(c.Begin(0).TotalMs, Is.EqualTo(500));
        Assert.That(c.Begin(500).TotalMs, Is.EqualTo(500));
        Assert.That(c.NextStep, Is.EqualTo(0));
    }

    [Test]
    public void Weave_skill_start_resets_counter_and_can_be_switched_off()
    {
        var c = new BasicStrikeCombo(StrikeComboDef.Sword());
        c.Begin(0);
        c.NotifyStarted(new ActionPlan(ActionKind.Skill, 1, 0, 0, 100));
        Assert.That(c.NextStep, Is.EqualTo(0));

        c.WeaveResets = false;
        c.Begin(200);
        c.NotifyStarted(new ActionPlan(ActionKind.Skill, 1, 0, 0, 100));
        Assert.That(c.NextStep, Is.EqualTo(1));
    }

    [Test]
    public void Strike_start_does_not_reset_its_own_chain()
    {
        var c = new BasicStrikeCombo(StrikeComboDef.Sword());
        ActionPlan p = c.Begin(0);
        c.NotifyStarted(p);
        Assert.That(c.NextStep, Is.EqualTo(1));
    }

    [Test]
    public void SetDef_switches_weapon_and_resets()
    {
        var c = new BasicStrikeCombo(StrikeComboDef.Sword());
        c.Begin(0);
        c.SetDef(StrikeComboDef.Single(500, 250));
        Assert.That(c.NextStep, Is.EqualTo(0));
    }

    [Test]
    public void Y24_taps_during_clip_queue_one_and_play_in_order_through_arbiter()
    {
        var combo = new BasicStrikeCombo(StrikeComboDef.Sword());
        var a = new ActionArbiter(
            CancelRuleDefaults.Build(), new InputBuffer(),
            (kind, token, now) => combo.Begin(now));

        Assert.That(a.Request(ActionKind.BasicStrike, 0, 0).Outcome, Is.EqualTo(ArbiterOutcome.Started));
        // 1. klip sürerken iki dokunuş: tür başına tek girdi → yalnız bir tanesi sırada.
        Assert.That(a.Request(ActionKind.BasicStrike, 0, 100).Outcome, Is.EqualTo(ArbiterOutcome.Buffered));
        Assert.That(a.Request(ActionKind.BasicStrike, 0, 200).Outcome, Is.EqualTo(ArbiterOutcome.Buffered));
        Assert.That(a.Buffer.Count, Is.EqualTo(1));

        Assert.That(a.Tick(399).Outcome, Is.EqualTo(ArbiterOutcome.None));
        ArbiterResult second = a.Tick(400);
        Assert.That(second.Outcome, Is.EqualTo(ArbiterOutcome.Started));
        Assert.That(second.FromBuffer, Is.True);
        Assert.That(combo.NextStep, Is.EqualTo(2), "ikinci kesiş oynadı");
    }

    [Test]
    public void Dodge_cuts_strike_and_weave_skill_resets_counter()
    {
        var combo = new BasicStrikeCombo(StrikeComboDef.Sword());
        var a = new ActionArbiter(
            CancelRuleDefaults.Build(), new InputBuffer(),
            (kind, token, now) => kind == ActionKind.BasicStrike
                ? combo.Begin(now)
                : new ActionPlan(kind, token, 0, 0, 300));
        a.OnStarted = (p, t) => combo.NotifyStarted(p);

        a.Request(ActionKind.BasicStrike, 0, 0);
        Assert.That(a.Request(ActionKind.Dodge, 0, 50).Outcome, Is.EqualTo(ArbiterOutcome.Cancelled));
        Assert.That(combo.NextStep, Is.EqualTo(1), "dodge sayacı sıfırlamaz");

        a.Request(ActionKind.Skill, 4, 400);
        Assert.That(combo.NextStep, Is.EqualTo(0), "skill başlayınca düz vuruş sayacı sıfırlandı");
    }
}
