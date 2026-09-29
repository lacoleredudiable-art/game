using System;
using System.Collections.Generic;
using System.IO;
using Dovus.Core;
using Dovus.Core.Combat;
using Dovus.Core.Execution;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// Play sweep (Kılıç, 3 m merkez) kalanlarının çekirdek karşılığı.
/// Boss yarıçapı 0,85, oyuncu 0,50, duruş payı 0,15.
/// </summary>
[TestFixture]
public class PlaySweepFixTests
{
    const float Body = 0.5f;
    const float BossR = 0.85f;
    const float Stop = 0.15f;
    const float Contact = Body + BossR;

    MotionTemplateCatalog _catalog;
    string _elements;

    [SetUp]
    public void Load()
    {
        DesignWarnings.ResetForTests();
        _catalog = MotionTemplateCatalog.FromJson(File.ReadAllText(MotionPath()));
        _elements = File.ReadAllText(ElementPath());
    }

    static readonly string[] SelfJitter =
    {
        "3-3", "3-7", "3-8", "3-12",
        "4-8", "4-9",
        "8-8", "8-9", "8-10", "8-11", "8-12",
        "9-7", "9-8", "9-9", "9-10", "9-11", "9-12",
        "10-5", "10-7", "10-8", "10-9", "10-11", "10-12",
        "11-6", "11-8", "11-10", "11-11"
    };

    [Test]
    public void SelfAim_IsNeverTheCaster_ForTheTwentySeven()
    {
        Assert.That(SelfJitter.Length, Is.EqualTo(27));
        var failures = new List<string>();
        foreach (string id in SelfJitter)
        {
            Assert.That(_catalog.TryPlay(id, out MotionTemplate template), Is.True, id);
            VerbFace(id, out string mode, out string action);
            bool ally = true;
            MotionDeliveryAim.Kind kind = MotionDeliveryAim.Choose(
                template.Aim,
                mode,
                action,
                MotionDeliveryAim.MovesTowardMarked(template),
                ally);
            MotionTarget target = Resolve(template, kind);
            if (!target.Has)
            {
                failures.Add(id + " hedef yok");
                continue;
            }
            // Atıcı (0,0) ve onunla birlikte yürüyen hedef değil.
            if (MathF.Abs(target.X) < 0.01f && MathF.Abs(target.Z) < 0.01f)
                failures.Add(id + " atıcıya kilitli");

            Trace run = Play(template, target, followCaster: false);
            if (run.Reversals >= 3)
                failures.Add(id + " titreme " + run.Reversals);
            if (run.MinBoss < Contact - 0.05f)
                failures.Add(id + " gövde " + run.MinBoss.ToString("0.00"));
        }

        Assert.That(failures, Is.Empty, string.Join(" | ", failures));
    }

    [Test]
    public void FollowingCaster_Jitters_EnemyTarget_DoesNot()
    {
        Assert.That(_catalog.TryPlay("3-12", out MotionTemplate glide), Is.True);
        Trace self = Play(glide, new MotionTarget(true, 0f, 0f, Body), followCaster: true);
        Trace enemy = Play(glide, new MotionTarget(true, 0f, 3f, BossR), followCaster: false);
        Assert.That(self.Reversals, Is.GreaterThanOrEqualTo(3), "kendi hedefi ileri-geri iter");
        Assert.That(enemy.Reversals, Is.LessThan(3));
        Assert.That(enemy.MinBoss, Is.GreaterThanOrEqualTo(Contact - 0.05f));
    }

    [Test]
    public void BehindHops_StayOutsideTheBoss()
    {
        AssertOutside("3-6");
        AssertOutside("6-12");
    }

    [Test]
    public void Thorn_HitsTheBossItFlewThrough()
    {
        Assert.That(_catalog.TryPlay("7-1", out MotionTemplate thorn), Is.True);
        Trace run = Play(thorn, new MotionTarget(true, 0f, 3f, BossR), followCaster: false);
        bool hit = false;
        foreach (MotionHit h in run.Hits)
        {
            if (h.Payload is "none" or "marker")
                continue;
            if (MotionHitGeometry.Overlaps(
                    h.OriginX, h.OriginZ, h.DirX, h.DirZ,
                    h.LengthM, h.RadiusM, h.Anchor, 0f, 3f, BossR))
                hit = true;
        }
        Assert.That(hit, Is.True, "diken uçuş hattında boss'a değer");
    }

    [Test]
    public void Fan_KeepsTheOpeningAim()
    {
        Assert.That(_catalog.TryPlay("7-5", out MotionTemplate fan), Is.True);
        Trace run = Play(fan, new MotionTarget(true, 0f, 3f, BossR), followCaster: false);
        MotionHit? shot = null;
        foreach (MotionHit h in run.Hits)
        {
            if (h.Anchor == "forward")
                shot = h;
        }
        Assert.That(shot.HasValue, Is.True);
        Assert.That(shot.Value.DirZ, Is.GreaterThan(0.8f), "yelpaze nişanı dönüşle kaçmaz");
    }

    [Test]
    public void RangeGate_AllowsTheFourCastsAtThreeMeters()
    {
        var data = VerbExecutionData.FromJson(_elements);
        // engine hitbox_scale_mult (skill) tablo size_mult'tan büyük olabiliyor.
        AssertCast(data, "1-3", verb: 1, adjective: 3, engineScale: 1.1f);
        AssertCast(data, "1-4", verb: 1, adjective: 4, engineScale: 0f);
        AssertCast(data, "5-1", verb: 5, adjective: 1, engineScale: 0.55f);
        AssertCast(data, "6-1", verb: 6, adjective: 1, engineScale: 0.55f);
    }

    [Test]
    public void JsonReach_CoversTheThreeMissesAtThreeMeters()
    {
        var data = VerbExecutionData.FromJson(_elements);
        Assert.That(Reaches(data, 1, 5, 1.8f), Is.True, "1-5");
        Assert.That(Reaches(data, 7, 1, 0.55f), Is.True, "7-1");
        Assert.That(Reaches(data, 7, 5, 1.8f), Is.True, "7-5");
    }

    [Test]
    public void EmiciHeal_DrainsTheBoss_AndHealsFromThePlan()
    {
        var grammar = new MechanicGrammar(MechanicRules.FromJson(_elements));
        MechanicPlan plan = grammar.Compose(2, 2, 4);
        DrainNumbers.Read(plan, out float damage, out float heal);
        Assert.That(damage, Is.GreaterThan(20f), "düşman canı eksi");
        Assert.That(heal, Is.GreaterThan(20f), "aktarılan can");
        Assert.That(DrainNumbers.TryShare(plan, 0.25f, out float part, out float partHeal), Is.True);
        Assert.That(part * 4f, Is.EqualTo(damage).Within(0.05f));
        Assert.That(partHeal * 4f, Is.EqualTo(heal).Within(0.05f));
    }

    [Test]
    public void EmiciPull_GoesToContactInFront_NotPastTheBoss()
    {
        // Boss 3 m ötede. Girdap merkezi daha da ötede olsa da varış temas noktasıdır.
        EmiciPull.ContactPoint(0f, 0f, 0f, 3f, Body, BossR, 0f, 1f, out float x, out float z);
        float contact = Body + BossR;
        Assert.That(z, Is.EqualTo(contact).Within(0.02f));
        Assert.That(x, Is.EqualTo(0f).Within(0.02f));
        Assert.That(EmiciPull.MovesTowardPlayer(0f, 3f, 0f, 0f, x, z), Is.True);
        Assert.That(EmiciPull.MovesTowardPlayer(0f, 3f, 0f, 0f, 0f, 8f), Is.False, "öteki merkez uzağa iter");

        // İç içeyken kararlı yön korunur, tek karede karşı tarafa fırlamaz.
        EmiciPull.ContactPoint(0f, 2.9f, 0f, 3f, Body, BossR, 0f, 1f, out _, out float near);
        Assert.That(near, Is.GreaterThan(2.9f));
        Assert.That(EmiciPull.Distance(0f, 3f, 0f, near), Is.LessThan(2f));
    }

    [Test]
    public void PullEase_DoesNotJumpAFrame()
    {
        float x = 0f;
        float z = 3f;
        float px = x;
        float pz = z;
        for (int i = 1; i <= 24; i++)
        {
            float u = i / 24f;
            DisplacementEase.Sample(0f, 3f, 0f, Body + BossR, u, out x, out z);
            float step = MathF.Sqrt((x - px) * (x - px) + (z - pz) * (z - pz));
            Assert.That(step, Is.LessThan(0.7f), "kare " + i);
            px = x;
            pz = z;
        }
        Assert.That(z, Is.EqualTo(Body + BossR).Within(0.05f));
    }

    [Test]
    public void TrackedTargetTeleport_DoesNotFlingThePlayer()
    {
        var dash = new MotionPhase(
            "gec", "dash", 0.28f, "target", "track", string.Empty, 0f,
            3f, 0f, 0f, 0f, 0f, 0f, 0f, 1.15f, 0f, 0f, 0f,
            0f, 0f, null, null);
        var template = new MotionTemplate("deneme", "deneme", 1, "deneme", true, new[] { dash }, MotionAim.Enemy);
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, Body, Stop);
        var boss = new MotionTarget(true, 0f, 3f, BossR);
        float worst = 0f;
        for (int i = 0; i < 8; i++)
            runner.Tick(1f / 60f, boss, default);
        // Boss bir karede 8 m ışınlansa da oyuncu 40 m/s üstüne çıkmaz.
        var flung = new MotionTarget(true, 0f, 11f, BossR);
        float z0 = runner.Z;
        runner.Tick(1f / 60f, flung, default);
        worst = MathF.Abs(runner.Z - z0);
        Assert.That(worst, Is.LessThan(0.8f), "hedef sıçraması oyuncuyu fırlatmaz");
    }

    [Test]
    public void HoppingStep_LandsBehind_ForEveryBossRadius()
    {
        Assert.That(_catalog.TryPlay("3-3", out MotionTemplate hops), Is.True);
        foreach (float bossR in new[] { 0.70f, 0.85f, 0.93f })
        {
            var runner = new MotionTemplateRunner();
            var boss = new MotionTarget(true, 0f, 3f, bossR);
            runner.Begin(hops, 0f, 0f, 0f, 0f, 1f, Body, Stop);
            float min = 99f;
            while (!runner.Finished)
            {
                runner.Tick(1f / 60f, boss, default);
                float d = MathF.Sqrt(runner.X * runner.X + (runner.Z - 3f) * (runner.Z - 3f));
                if (d < min)
                    min = d;
            }
            Assert.That(runner.Z, Is.GreaterThan(3.3f), "r=" + bossR.ToString("0.00") + " z=" + runner.Z.ToString("0.00"));
            Assert.That(min, Is.GreaterThanOrEqualTo(Body + bossR - 0.05f), "r=" + bossR);
        }
    }

    [Test]
    public void WeaponRange_CannotShortenTemplateTravel()
    {
        Assert.That(MotionTravel.Protect(1.35f, 0.5f), Is.EqualTo(1.35f));
        Assert.That(MotionTravel.Protect(1.35f, 7.5f), Is.EqualTo(7.5f));
    }

    [Test]
    public void FastArc_UsesDashPose_InsteadOfSpeedingTheRun()
    {
        Assert.That(LocoBlend.TemplatePlaybackCap, Is.EqualTo(2f));
        Assert.That(LocoBlend.MatchPlayback(19.1f, 2.24f), Is.EqualTo(2f));
        Assert.That(LocoBlend.NeedsDashPose(19.1f, 2.24f), Is.True);
        Assert.That(LocoBlend.NeedsDashPose(4f, 2.24f), Is.False);
        Assert.That(LocoBlend.PresentationKey("dash", 19.1f, 2.24f), Is.EqualTo(LocoBlend.DashFastKey));
        Assert.That(LocoBlend.PresentationKey("lunge", 19.1f, 2.24f), Is.EqualTo("lunge"));
        Assert.That(MotionAnimTable.IsLocomotionKey(LocoBlend.DashFastKey), Is.False);
        Assert.That(MotionAnimTable.IsKnown(LocoBlend.DashFastKey), Is.True);
        MotionAnimClip clip = _catalog.Anims.Resolve(LocoBlend.DashFastKey, 0, 0);
        Assert.That(clip.Fallback, Is.False);
        Assert.That(clip.State, Is.EqualTo("CastPierce"));
    }

    [Test]
    public void EmiciPull_HoldsTheCaster_AndRetargetsEveryTick()
    {
        var lunge = new MotionPhase(
            "vur", "lunge", 0.4f, "target", "track", string.Empty, 0f,
            1.5f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, null, null);
        var template = new MotionTemplate("emici", "emici", 1, "emici", true, new[] { lunge }, MotionAim.Enemy);
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, Body, Stop);
        var far = new MotionTarget(true, 0f, 3f, BossR);
        for (int i = 0; i < 4; i++)
            runner.Tick(1f / 60f, far, default);
        float advanced = runner.Z;
        Assert.That(advanced, Is.GreaterThan(0.05f));

        var inside = new MotionTarget(true, 0f, 0.4f, BossR, holdApproach: true);
        float held = runner.Z;
        for (int i = 0; i < 20; i++)
        {
            runner.Tick(1f / 60f, inside, default);
            Assert.That(runner.Z, Is.EqualTo(held).Within(0.001f), "çekme sürerken oyuncu yürümez");
        }

        var release = new MotionTarget(true, 0f, 3f, BossR);
        float z0 = runner.Z;
        runner.Tick(1f / 60f, release, default);
        Assert.That(runner.Z, Is.GreaterThanOrEqualTo(z0 - 0.02f), "bırakınca geri sekmez");
        Assert.That(MathF.Abs(runner.Z - z0), Is.LessThan(0.35f), "bırakınca tek karede fırlamaz");

        float playerZ = 0f;
        float bossZ = 3f;
        float speed = 0f;
        float sx = 0f;
        float sz = 1f;
        bool pulling = false;
        float locked = 0f;
        for (int i = 0; i < 40; i++)
        {
            bool active = pulling;
            EmiciPull.Retarget(
                active, 0f, bossZ, 0f, playerZ, Body, BossR,
                ref sx, ref sz, ref speed,
                out float tx, out float tz, out pulling);
            if (active)
                Assert.That(speed, Is.EqualTo(locked).Within(0.001f), "hız her kare sıfırlanmaz");
            else
                locked = speed;
            float bx = 0f;
            EmiciPull.StepToward(ref bx, ref bossZ, tx, tz, speed, 1f / 60f, out _);
            float gap = MathF.Abs(bossZ - playerZ);
            Assert.That(gap, Is.GreaterThanOrEqualTo(Contact - 0.03f), "kare " + i);
        }
        Assert.That(MathF.Abs(bossZ - playerZ), Is.LessThan(Contact + 0.08f));

        playerZ = 0.55f;
        float before = MathF.Abs(bossZ - playerZ);
        EmiciPull.Retarget(
            true, 0f, bossZ, 0f, playerZ, Body, BossR,
            ref sx, ref sz, ref speed,
            out float movedX, out float movedZ, out _);
        Assert.That(speed, Is.EqualTo(locked).Within(0.001f));
        Assert.That(MathF.Abs(movedZ - playerZ), Is.EqualTo(Contact).Within(0.05f));
        float stepX = 0f;
        float stepZ = bossZ;
        EmiciPull.StepToward(ref stepX, ref stepZ, movedX, movedZ, speed, 1f / 60f, out _);
        Assert.That(MathF.Abs(stepZ - playerZ), Is.GreaterThan(before - 0.001f));
        Assert.That(MathF.Abs(stepZ - bossZ), Is.LessThan(0.2f), "iç içe kare tek karede ışınlanmaz");
        Assert.That(EmiciPull.VortexActs(true, false), Is.False);
        Assert.That(EmiciPull.VortexActs(true, true), Is.True);
        Assert.That(EmiciPull.VortexActs(false, true), Is.False);
    }

    [Test]
    public void AllyHook_StopsAtContact_Grounded_AndAroundTheBoss()
    {
        var far = new MotionPhase(
            "cek", "pull", 0.34f, "target", "track", string.Empty, 0f,
            40f, 0f, 0f, 0f, 5f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, null, null);
        var near = new MotionPhase(
            "cek", "pull", 0.34f, "target", "none", string.Empty, 0f,
            2f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, null, null);
        var ally = new MotionTarget(true, 0f, 6f, Body, false, true, 0f, 2f, BossR);
        float farZ = RunPull(far, ally, 5f, out float farY, out float farMin);
        float nearZ = RunPull(near, ally, 5f, out float nearY, out float nearMin);
        Assert.That(farZ, Is.EqualTo(nearZ).Within(0.08f), "silah menzili kancayı uzatmaz");
        Assert.That(farY, Is.EqualTo(0f).Within(0.001f));
        Assert.That(nearY, Is.EqualTo(0f).Within(0.001f));
        float sep = Body + Body + Stop;
        Assert.That(farZ, Is.LessThan(6f - Body));
        Assert.That(MathF.Abs(farZ - (6f - sep)), Is.LessThan(0.35f));
        Assert.That(farMin, Is.GreaterThanOrEqualTo(Body + BossR + Stop - 0.08f));
        Assert.That(nearMin, Is.GreaterThanOrEqualTo(Body + BossR + Stop - 0.08f));
    }

    [Test]
    public void WeaponSwapScale_DoesNotChangeBehindLanding()
    {
        Assert.That(_catalog.TryPlay("3-3", out MotionTemplate hops), Is.True);
        var swordSteps = new[] { new GrammarPositionStep("yer_degistir", 3f) };
        var staffSteps = new[] { new GrammarPositionStep("yer_degistir", 7.5f) };
        PositionPlayback sword = PositionOwnership.Prepare(hops, swordSteps, 0.28f, 1.2f);
        PositionPlayback staff = PositionOwnership.Prepare(hops, staffSteps, 0.28f, 1.2f);
        Assert.That(staff.Template.Phases.Count, Is.EqualTo(hops.Phases.Count));
        Assert.That(staff.Template.Phases[0].ForwardM, Is.EqualTo(hops.Phases[0].ForwardM));
        Assert.That(staff.Template.Phases[2].Land, Is.EqualTo("behind"));
        Assert.That(sword.Template.Phases[2].ForwardM, Is.EqualTo(staff.Template.Phases[2].ForwardM));

        float swordZ = FinishZ(sword.Template, 0.85f);
        float staffZ = FinishZ(staff.Template, 0.85f);
        Assert.That(staffZ, Is.EqualTo(swordZ).Within(0.001f));
        Assert.That(staffZ, Is.GreaterThan(3.3f));

        var cross = new MotionPhase(
            "gec", "hop", 0.2f, "travel", "none", string.Empty, 0f,
            0f, 4f, 1f, 0f, 0.2f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, null, null);
        var land = new MotionPhase(
            "in", "hop", 0.2f, "travel", "none", string.Empty, 0f,
            0.4f, 0.4f, 1f, 0f, 0.2f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, null, null, "behind");
        var crossed = new MotionTemplate(
            "capraz", "capraz", 3, "capraz", true, new[] { cross, land }, MotionAim.Effect);
        Assert.That(FinishZ(crossed, BossR), Is.GreaterThan(3.3f), "boss'u geçen ara adım inişi öne çevirmez");
    }

    static float RunPull(MotionPhase phase, MotionTarget ally, float startY, out float endY, out float minBoss)
    {
        var template = new MotionTemplate("kanca", "kanca", 7, "kanca", true, new[] { phase }, MotionAim.Effect);
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, startY, 0f, 0f, 1f, Body, Stop);
        minBoss = 99f;
        while (!runner.Finished)
        {
            runner.Tick(1f / 60f, ally, default);
            float d = MathF.Sqrt(runner.X * runner.X + (runner.Z - 2f) * (runner.Z - 2f));
            if (d < minBoss)
                minBoss = d;
        }
        endY = runner.Y;
        return runner.Z;
    }

    static float FinishZ(MotionTemplate template, float bossR)
    {
        var runner = new MotionTemplateRunner();
        var boss = new MotionTarget(true, 0f, 3f, bossR);
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, Body, Stop);
        while (!runner.Finished)
            runner.Tick(1f / 60f, boss, default);
        return runner.Z;
    }

    [Test]
    public void Pull_Eases_AndDoesNotOverlap()
    {
        DisplacementEase.Sample(0f, 0f, 3f, 0f, 0f, out float x, out float z);
        Assert.That(x, Is.EqualTo(0f).Within(0.001f));
        DisplacementEase.Sample(0f, 0f, 3f, 0f, 1f, out x, out z);
        Assert.That(x, Is.EqualTo(3f).Within(0.001f));
        DisplacementEase.Sample(0f, 0f, 3f, 0f, 0.5f, out x, out _);
        Assert.That(x, Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(x, Is.LessThan(3f), "orta kare henüz bitiş değil");

        float px = 0f, pz = 0f;
        DisplacementEase.KeepSeparated(ref px, ref pz, 0f, 0f, Contact);
        Assert.That(MathF.Sqrt(px * px + pz * pz), Is.GreaterThanOrEqualTo(Contact - 0.001f));

        var board = new StatusBoard();
        Assert.That(ForcedDisplacement.Allows(null), Is.True);
        Assert.That(ForcedDisplacement.Allows(board), Is.True);
        board.Apply(StatusKind.Stasis, 500, 1f);
        Assert.That(ForcedDisplacement.Allows(board), Is.False);
    }

    void AssertOutside(string id)
    {
        Assert.That(_catalog.TryPlay(id, out MotionTemplate template), Is.True, id);
        Trace run = Play(template, new MotionTarget(true, 0f, 3f, BossR), followCaster: false);
        Assert.That(run.MinBoss, Is.GreaterThanOrEqualTo(Contact - 0.05f), id + " " + run.MinBoss.ToString("0.00"));
    }

    void AssertCast(VerbExecutionData data, string id, int verb, int adjective, float engineScale)
    {
        Assert.That(_catalog.TryPlay(id, out MotionTemplate template), Is.True, id);
        float edge = Edge(data, verb, adjective, engineScale, template);
        Assert.That(
            MotionCastReach.CenterInReach(3f, Body, BossR, edge),
            Is.True,
            id + " kenar " + edge.ToString("0.00"));
    }

    bool Reaches(VerbExecutionData data, int verb, int adjective, float engineScale)
    {
        float edge = Edge(data, verb, adjective, engineScale, template: null);
        return MotionCastReach.CenterInReach(3f, Body, BossR, edge);
    }

    static float Edge(VerbExecutionData data, int verb, int adjective, float engineScale, MotionTemplate template)
    {
        Assert.That(data.TryGetHitbox(verb, out VerbHitboxSpec spec), Is.True);
        float scale = HitboxSizing.AdjectiveScale(data.AdjectiveSizeMult(adjective), engineScale);
        float reach = HitboxSizing.Resolve(spec, 1f, scale).ReachM;
        return MotionCastReach.ComboEdgeReach(reach, template);
    }

    static MotionTarget Resolve(MotionTemplate template, MotionDeliveryAim.Kind kind)
    {
        if (kind == MotionDeliveryAim.Kind.Ally && !MotionDeliveryAim.SwapsPastBody(template))
            return new MotionTarget(true, -2.2f, 0.4f, Body);
        return new MotionTarget(true, 0f, 3f, BossR);
    }

    static void VerbFace(string id, out string mode, out string action)
    {
        int verb = int.Parse(id.Split('-')[0]);
        (mode, action) = verb switch
        {
            3 => ("self_only", "dash"),
            4 => ("self_only", "shield"),
            8 => ("self_or_ally", "buff"),
            9 => ("self_or_ally", "cleanse"),
            10 => ("self_only", "reflect"),
            11 => ("self_only", "summon"),
            _ => ("enemy_only", "damage")
        };
    }

    static Trace Play(MotionTemplate template, MotionTarget target, bool followCaster)
    {
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, Body, Stop);
        var hits = new List<MotionHit>();
        float min = 99f;
        int reversals = 0;
        float prevX = 0f, prevZ = 0f;
        bool hasStep = false;
        for (int i = 0; i < 2400 && !runner.Finished; i++)
        {
            float x0 = runner.X;
            float z0 = runner.Z;
            MotionTarget aim = target;
            if (followCaster)
                aim = new MotionTarget(true, runner.X, runner.Z, Body);
            MotionTick tick = runner.Tick(1f / 60f, aim, default);
            float dx = runner.X - x0;
            float dz = runner.Z - z0;
            float boss = MathF.Sqrt(runner.X * runner.X + (runner.Z - 3f) * (runner.Z - 3f));
            if (boss < min)
                min = boss;
            float step = MathF.Sqrt(dx * dx + dz * dz);
            if (hasStep && step > 0.2f)
            {
                float prev = MathF.Sqrt(prevX * prevX + prevZ * prevZ);
                if (prev > 0.2f && (dx * prevX + dz * prevZ) / (step * prev) < -0.5f)
                    reversals++;
            }
            if (step > 0.02f)
            {
                prevX = dx;
                prevZ = dz;
                hasStep = true;
            }
            if (tick.Hits != null)
                hits.AddRange(tick.Hits);
        }
        return new Trace(min, reversals, hits);
    }

    readonly struct Trace
    {
        public Trace(float minBoss, int reversals, List<MotionHit> hits)
        {
            MinBoss = minBoss;
            Reversals = reversals;
            Hits = hits;
        }

        public float MinBoss { get; }
        public int Reversals { get; }
        public List<MotionHit> Hits { get; }
    }

    static string MotionPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "unity", "Assets", "Resources", "ElementSystem", "motion-templates.json"));
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }

    static string ElementPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "docs", "element-sistemi.json"));
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }
}
