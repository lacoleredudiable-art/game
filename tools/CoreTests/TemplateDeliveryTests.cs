using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace CoreTests;

/// <summary>
/// Kalıp teslim kuyruğu: skill kimliğiyle beyaz liste yok.
/// Her DEAD-TEMPLATE etkisi planda ve zamanında bir adım olarak durur.
/// </summary>
[TestFixture]
public class TemplateDeliveryTests
{
    SkillMotor _motor = null!;
    MechanicGrammar _grammar = null!;
    MotionTemplateCatalog _motion = null!;

    [OneTimeSetUp]
    public void Load()
    {
        string root = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        string elements = Path.Combine(root, "docs", "element-sistemi.json");
        if (!File.Exists(elements))
        {
            root = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
            elements = Path.Combine(root, "docs", "element-sistemi.json");
        }

        string json = File.ReadAllText(elements);
        _motor = SkillMotor.FromJson(json);
        _grammar = new MechanicGrammar(MechanicRules.FromJson(json));
        _motion = MotionTemplateCatalog.FromJson(File.ReadAllText(Path.Combine(root, "docs", "motion-templates.json")));
        Assert.That(_grammar.Rules.IsValid, Is.True);
    }

    TemplateDeliveryOrder Order(int verb, int adjective, int weapon = 1)
    {
        string id = verb + "-" + adjective;
        Assert.That(_motion.TryPlay(id, out MotionTemplate template), Is.True, id);
        MechanicPlan plan = _grammar.Compose(verb, adjective, weapon);
        var skill = _motor.Resolve(new[] { verb, adjective });
        return TemplateDelivery.Build(plan, skill.Engine, template, _grammar.Rules, 1f);
    }

    MechanicPlan Plan(int verb, int adjective, int weapon = 1) =>
        _grammar.Compose(verb, adjective, weapon);

    static bool HasBeat(TemplateDeliveryOrder order, DeliveryBeatKind kind)
    {
        foreach (DeliveryBeat beat in order.Schedule())
        {
            if (beat.Kind == kind)
                return true;
        }
        return false;
    }

    static DeliveryBeat Beat(TemplateDeliveryOrder order, DeliveryBeatKind kind)
    {
        foreach (DeliveryBeat beat in order.Schedule())
        {
            if (beat.Kind == kind)
                return beat;
        }
        Assert.Fail("adım yok: " + kind);
        return default;
    }

    static bool Mode(MechanicPlan plan, string stat, string mode)
    {
        MechanicEffect effect = plan.Effects.Find(e => e.Stat == stat);
        return effect != null && effect.Has(mode);
    }

    [Test]
    public void Summons_ScheduleActors_WithHitDamageAndDuration()
    {
        for (int adjective = 1; adjective <= 12; adjective++)
        {
            if (adjective == 11)
                continue;
            TemplateDeliveryOrder order = Order(11, adjective);
            Assert.That(order.SpawnActors, Is.True, "11-" + adjective);
            Assert.That(HasBeat(order, DeliveryBeatKind.SpawnActors), Is.True, "11-" + adjective);
            Assert.That(order.ActorCount, Is.GreaterThanOrEqualTo(1), "11-" + adjective);
            Assert.That(order.ActorDurationSec, Is.GreaterThan(1f), "11-" + adjective);
            Assert.That(order.ActorHitDamage, Is.EqualTo(6f).Within(0.01f), "11-" + adjective);
        }

        TemplateDeliveryOrder clone = Order(11, 11);
        Assert.That(clone.SpawnActors, Is.True);
        Assert.That(Plan(11, 11).Effects.Exists(e => e.Stat == "klon"), Is.True);
        Assert.That(Mode(Plan(11, 11), "klon", "senin_kopyan"), Is.True);
    }

    [Test]
    public void SummonModes_SurviveOnThePlan()
    {
        Assert.That(Mode(Plan(11, 2), "aktor_yarat", "can_emen"), Is.True);
        Assert.That(Order(11, 2).CanDrain, Is.True);
        Assert.That(Mode(Plan(11, 3), "aktor_yarat", "ziplayan"), Is.True);
        Assert.That(Mode(Plan(11, 4), "aktor_yarat", "taret"), Is.True);
        Assert.That(Mode(Plan(11, 5), "aktor_yarat", "halka"), Is.True);
        Assert.That(Mode(Plan(11, 6), "aktor_yarat", "bagli_muhafiz"), Is.True);
        Assert.That(Mode(Plan(11, 7), "aktor_yarat", "gorunmez"), Is.True);
        Assert.That(Mode(Plan(11, 9), "aktor_yarat", "suikastci"), Is.True);
        Assert.That(Mode(Plan(11, 10), "aktor_yarat", "ayna_klon"), Is.True);

        foreach (MechanicWeapon weapon in _grammar.Rules.Weapons)
        {
            MechanicPlan plan = Plan(11, 1, weapon.Id);
            MechanicEffect actor = plan.Effects.Find(e => e.Stat == "aktor_yarat");
            Assert.That(actor, Is.Not.Null, weapon.Name);
            bool armed = false;
            foreach (string mode in actor.Modes)
            {
                if (mode.StartsWith("silahla:"))
                    armed = true;
            }
            Assert.That(armed, Is.True, weapon.Name);
        }
    }

    [Test]
    public void Duplicates_ScheduleASecondCast_AndTwelveElevenRepeatsThePrevious()
    {
        int[] verbs = { 1, 2, 4, 5, 6, 7, 8, 9, 10, 12 };
        foreach (int verb in verbs)
        {
            TemplateDeliveryOrder order = Order(verb, 11);
            Assert.That(_motion.TryPlay(verb + "-11", out MotionTemplate template), Is.True);
            float first = System.Math.Max(0f, TemplateDelivery.FirstEffectHitSec(template));
            Assert.That(order.DuplicateAtSec, Is.EqualTo(first + 0.3f).Within(0.02f), verb + "-11");
            // Kopya nabzı kalıpta ya da kuyrukta: ilk vuruştan ~0,3 sn sonra tam bir kez.
            int pulses = order.Duplicate ? 1 : 0;
            if (!order.Duplicate)
            {
                float t = 0f;
                foreach (MotionPhase phase in template.Phases)
                {
                    if (phase.Hit != null && phase.Hit.Payload is not ("none" or "marker")
                        && System.Math.Abs(t + phase.DurationSec * phase.Hit.At - order.DuplicateAtSec) <= 0.05f)
                        pulses++;
                    t += phase.DurationSec;
                }
            }
            else
            {
                DeliveryBeat beat = Beat(order, DeliveryBeatKind.Duplicate);
                Assert.That(beat.AtSec, Is.EqualTo(order.DuplicateAtSec).Within(0.001), verb + "-11");
            }
            Assert.That(pulses, Is.EqualTo(1), verb + "-11");
            Assert.That(Mode(Plan(verb, 11), "iki_kez", "iki_kez") || Plan(verb, 11).Effects.Exists(e => e.Has("iki_kez")),
                Is.True, verb + "-11");
        }

        TemplateDeliveryOrder echo = Order(12, 11);
        Assert.That(echo.RepeatPrevious, Is.True);
        Assert.That(echo.OpeningPulse, Is.True);
        Assert.That(HasBeat(echo, DeliveryBeatKind.RepeatPrevious), Is.True);
        Assert.That(HasBeat(echo, DeliveryBeatKind.Resolve), Is.True);
        Assert.That(TemplateDelivery.HasEffectHit(_motion.TryPlay("12-11", out MotionTemplate yanki) ? yanki : null), Is.False);
    }

    [Test]
    public void MarksDetonateAfterDelay_AndRiseWaits()
    {
        int hammer = 1;
        foreach (MechanicWeapon weapon in _grammar.Rules.Weapons)
        {
            if (weapon.Path == "yere_vurus")
                hammer = weapon.Id;
        }

        // 1-9 ve 5-9 işareti çekiçin eklediği hız atomundan doğar.
        foreach (int verb in new[] { 1, 5 })
        {
            TemplateDeliveryOrder order = Order(verb, 9, hammer);
            Assert.That(order.DelayedMark, Is.True, verb + "-9");
            Assert.That(order.ActivationDelaySec, Is.EqualTo(2f).Within(0.01f), verb + "-9");
            Assert.That(Beat(order, DeliveryBeatKind.Detonate).AtSec, Is.EqualTo(2f).Within(0.01f), verb + "-9");
        }

        foreach (int verb in new[] { 6, 7, 12 })
        {
            TemplateDeliveryOrder order = Order(verb, 9);
            Assert.That(order.DelayedMark, Is.True, verb + "-9");
            Assert.That(order.ActivationDelaySec, Is.EqualTo(2f).Within(0.01f), verb + "-9");
            Assert.That(Beat(order, DeliveryBeatKind.Detonate).AtSec, Is.EqualTo(2f).Within(0.01f), verb + "-9");
            Assert.That(order.Homing, Is.EqualTo(Plan(verb, 9).Body.Homing), verb + "-9");
        }

        TemplateDeliveryOrder rise = Order(1, 8);
        Assert.That(rise.RiseDelay, Is.True);
        Assert.That(rise.ActivationDelaySec, Is.EqualTo(0.6f).Within(0.01f));
        Assert.That(Beat(rise, DeliveryBeatKind.Detonate).AtSec, Is.EqualTo(0.6f).Within(0.01f));
    }

    [Test]
    public void Homing_TracksThrowPhases_WithoutMovingTheCaster()
    {
        Assert.That(_motion.TryPlay("1-6", out MotionTemplate bomb), Is.True);
        Assert.That(bomb.Phases[0].Motion, Is.EqualTo("throw"));
        MotionPhase shot = bomb.Phases[0].WithHoming("none");
        var bare = bomb.WithPhases(new[] { shot });
        MotionTemplate tracked = HomingDelivery.TrackShots(bare);
        Assert.That(tracked.Phases[0].Homing, Is.EqualTo("track"));
        Assert.That(tracked.Phases[0].Motion, Is.EqualTo("throw"));
    }

    [Test]
    public void Bounce_AddsOneWeakerHit()
    {
        TemplateDeliveryOrder order = Order(1, 3);
        Assert.That(order.BounceExtra, Is.EqualTo(1));
        Assert.That(order.BounceDamageMult, Is.EqualTo(0.5f).Within(0.01f));
        DeliveryBeat beat = Beat(order, DeliveryBeatKind.Bounce);
        Assert.That(beat.Power, Is.EqualTo(0.5f).Within(0.01f));
        Assert.That(beat.AtSec, Is.GreaterThan(0.05f));

        TemplateDeliveryOrder hops = Order(3, 3);
        Assert.That(hops.BounceExtra, Is.EqualTo(1));
        Assert.That(hops.BounceDamageMult, Is.EqualTo(0.5f).Within(0.01f));
    }

    [Test]
    public void DashDistance_CapsTravel_AndShortDashStaysShort()
    {
        Assert.That(_motion.TryPlay("3-1", out MotionTemplate dash), Is.True);
        Assert.That(dash.Phases[0].DistanceM, Is.EqualTo(1.65f).Within(0.01f));
        MotionTemplate same = DashDistance.Apply(dash, 3f * 0.55f);
        Assert.That(same.Phases[0].DistanceM, Is.EqualTo(1.65f).Within(0.01f));

        MotionTemplate capped = DashDistance.Apply(dash, 3f);
        var runner = new MotionTemplateRunner();
        runner.Begin(capped, 0f, 0f, 0f, 0f, 1f, 0.5f, 0.15f);
        var far = new MotionTarget(true, 0f, 8f, 0.85f);
        for (int i = 0; i < 40 && !runner.Finished; i++)
            runner.Tick(0.02f, far, default);
        Assert.That(runner.Finished, Is.True);
        Assert.That(runner.Z, Is.EqualTo(3f).Within(0.2f), "8 m'den dash 3 m'de durur");
        Assert.That(runner.Z, Is.LessThan(4f));
    }

    [Test]
    public void FlowFields_TickWhenTheTemplateDoesNot()
    {
        TemplateDeliveryOrder ring = Order(6, 12);
        TemplateDeliveryOrder stream = Order(8, 12);
        Assert.That(ring.FieldTicks, Is.True);
        Assert.That(stream.FieldTicks, Is.True);
        Assert.That(HasBeat(ring, DeliveryBeatKind.FieldTick), Is.True);
        Assert.That(HasBeat(stream, DeliveryBeatKind.FieldTick), Is.True);
        Assert.That(Beat(ring, DeliveryBeatKind.FieldTick).Power, Is.EqualTo(0.33f).Within(0.02f));

        TemplateDeliveryOrder channel = Order(1, 12);
        Assert.That(channel.FieldTicks, Is.False, "kalıp zaten every_sec ile vuruyor");
        Assert.That(HasBeat(channel, DeliveryBeatKind.FieldTick), Is.False);
    }

    [Test]
    public void Vortex_PullsOnlyOffensiveDrains()
    {
        int[] pull = { 1, 3, 5, 6, 7 };
        int[] calm = { 2, 4, 8, 9, 10, 11, 12 };
        foreach (int verb in pull)
            Assert.That(Plan(verb, 2).Body.Pull, Is.True, verb + "-2 çekmeli");
        foreach (int verb in calm)
            Assert.That(Plan(verb, 2).Body.Pull, Is.False, verb + "-2 çekmemeli");
    }

    [Test]
    public void ReflectDrain_KeepsYansitAndAbsorb()
    {
        foreach (MechanicWeapon weapon in _grammar.Rules.Weapons)
        {
            MechanicPlan plan = Plan(10, 2, weapon.Id);
            Assert.That(plan.Effects.Exists(e => e.Stat == "yansit"), Is.True, weapon.Name);
            MechanicEffect absorb = plan.Effects.Find(e => e.Stat == "em" && e.Has("cana_cevir"));
            Assert.That(absorb, Is.Not.Null, weapon.Name);
            Assert.That(absorb.Target, Is.EqualTo("kendin"), weapon.Name);
        }
    }

    [Test]
    public void StepTemplates_SplitShortDashLockAndTwoHops()
    {
        Assert.That(_motion.TryPlay("3-1", out MotionTemplate shortDash), Is.True);
        Assert.That(_motion.TryPlay("3-9", out MotionTemplate locked), Is.True);
        Assert.That(_motion.TryPlay("3-3", out MotionTemplate hops), Is.True);
        Assert.That(shortDash.Id, Is.Not.EqualTo(locked.Id));
        Assert.That(shortDash.Phases[0].Motion, Is.EqualTo("dash"));
        Assert.That(shortDash.Phases[0].Land, Is.Not.EqualTo("behind"));
        Assert.That(locked.FamilyId, Is.EqualTo(23));
        int behind = 0;
        for (int i = 0; i < locked.Phases.Count; i++)
        {
            if (locked.Phases[i].Land == "behind")
                behind++;
        }
        Assert.That(behind, Is.EqualTo(1));
        Assert.That(hops.Phases.Count, Is.EqualTo(2));
        Assert.That(hops.Phases[^1].Land, Is.EqualTo("behind"));

        Assert.That(_motion.TryPlay("3-4", out MotionTemplate pin), Is.True);
        Assert.That(_motion.TryPlay("3-12", out MotionTemplate glide), Is.True);
        Assert.That(TemplateDelivery.HasEffectHit(pin), Is.False);
        Assert.That(TemplateDelivery.HasEffectHit(glide), Is.False);
        Assert.That(Order(3, 4).OpeningPulse, Is.True);
        TemplateDeliveryOrder flow = Order(3, 12);
        Assert.That(flow.OpeningPulse, Is.True);
        Assert.That(flow.GlideHaste, Is.True);
        Assert.That(flow.GlideMagnitude, Is.GreaterThan(1f));
        Assert.That(HasBeat(flow, DeliveryBeatKind.GlideHaste), Is.True);
    }

    [Test]
    public void Duplicate_CountsFromTheFirstHit_AndTemplateEchoIsNotDoubled()
    {
        // 1-11 kalıbı yankıyı kendisi vuruyor (0,11 → 0,42 sn); kuyruk üçüncü nabız eklemez.
        TemplateDeliveryOrder echo = Order(1, 11);
        Assert.That(echo.Duplicate, Is.False);
        Assert.That(HasBeat(echo, DeliveryBeatKind.Duplicate), Is.False);

        Assert.That(_motion.TryPlay("6-11", out MotionTemplate twoFast), Is.True);
        float first = TemplateDelivery.FirstEffectHitSec(twoFast);
        TemplateDeliveryOrder copy = Order(6, 11);
        Assert.That(copy.Duplicate, Is.True);
        Assert.That(copy.DuplicateAtSec, Is.EqualTo(first + copy.DuplicateDelaySec).Within(0.001f));
        Assert.That(Beat(copy, DeliveryBeatKind.Duplicate).AtSec, Is.EqualTo(copy.DuplicateAtSec).Within(0.001));
    }

    [Test]
    public void Knockback_NeverOnPullingPlans()
    {
        Assert.That(Order(5, 2).BossKnockback, Is.False, "5-2 girdabı çeker, itmez");
        Assert.That(Order(1, 2).BossKnockback, Is.False, "1-2 emici çeker");
        Assert.That(Order(1, 6).BossKnockback, Is.True, "1-6 vuruşu iter");
    }

    [Test]
    public void EmiciLunge_HoldsTheCasterEvenWhenThePlanDoesNotPull()
    {
        Assert.That(_motion.TryPlay("4-2", out MotionTemplate lunge), Is.True);
        Assert.That(EmiciApproach.ShouldHoldCaster("2", lunge), Is.True);
        Assert.That(Plan(4, 2).Body.Pull, Is.False, "4-2 boss'u çekmez ama oyuncu yerinde kalır");
        Assert.That(EmiciApproach.SweepStayCategory("2", lunge), Is.EqualTo("yerinde"));
    }

    [TestCase(3f, 1f / 60f)]
    [TestCase(7.06f, 1f / 60f)]
    [TestCase(7.06f, 1f / 15f)]
    public void StraightDash_WithoutOvershoot_StopsAtTheBodyEdge(float dashM, float dt)
    {
        Assert.That(_motion.TryPlay("3-7", out MotionTemplate smoke), Is.True);
        MotionTemplate stretched = DashDistance.Apply(smoke, dashM);
        const float body = 0.5f;
        const float gap = 0.15f;
        const float bossR = 0.85f;
        var target = new MotionTarget(true, 0f, 3f, bossR);
        var runner = new MotionTemplateRunner();
        runner.Begin(stretched, 0f, 0f, 0f, 0f, 1f, body, gap);
        float separation = body + bossR + gap;
        float px = runner.X;
        float pz = runner.Z;
        for (int i = 0; i < 400 && !runner.Finished; i++)
        {
            runner.Tick(dt, target, default);
            float step = System.MathF.Sqrt((runner.X - px) * (runner.X - px) + (runner.Z - pz) * (runner.Z - pz));
            Assert.That(runner.Z, Is.LessThan(3f), "boss'un karşı yüzüne geçmez");
            Assert.That(step, Is.LessThanOrEqualTo(dashM * dt / smoke.Phases[0].DurationSec + 0.01f), "tek kare sıçrama yok");
            px = runner.X;
            pz = runner.Z;
        }
        Assert.That(runner.Finished, Is.True);
        Assert.That(3f - runner.Z, Is.EqualTo(separation).Within(0.02f));
    }

    [Test]
    public void TrackDash_TargetSnapsCloserMidPhase_NeverEndsInsideSeparation()
    {
        // 12-11: kuyruk atılma sürerken boss'u geri sarar; hedef her karede canlı okunur.
        Assert.That(_motion.TryPlay("12-11", out MotionTemplate rewind), Is.True);
        const float body = 0.5f;
        const float gap = 0.15f;
        const float bossR = 0.85f;
        var runner = new MotionTemplateRunner();
        runner.Begin(rewind, 0f, 0f, 0f, 0f, 1f, body, gap);
        float bossZ = 6.35f;
        bool snapped = false;
        for (int i = 0; i < 600 && !runner.Finished; i++)
        {
            if (!snapped && runner.Z > 0.8f)
            {
                bossZ = runner.Z + body + bossR;
                snapped = true;
            }
            runner.Tick(1f / 15f, new MotionTarget(true, 0f, bossZ, bossR), default);
            if (snapped)
                Assert.That(bossZ - runner.Z, Is.GreaterThanOrEqualTo(body + bossR - 0.01f), "gövdeye girmez");
        }
        Assert.That(snapped, Is.True);
        Assert.That(runner.Finished, Is.True);
    }

    [Test]
    public void ActorSpacing_PushesAPointOutOfTheBoss()
    {
        float x = 0.2f;
        float z = 0.1f;
        ActorSpacing.PushOutside(ref x, ref z, 0f, 0f, 1.5f);
        float dist = System.MathF.Sqrt(x * x + z * z);
        Assert.That(dist, Is.EqualTo(1.5f).Within(0.01f));

        float ox = 3f;
        float oz = 4f;
        ActorSpacing.PushOutside(ref ox, ref oz, 0f, 0f, 1f);
        Assert.That(ox, Is.EqualTo(3f).Within(0.001f));
        Assert.That(oz, Is.EqualTo(4f).Within(0.001f));
    }
}
