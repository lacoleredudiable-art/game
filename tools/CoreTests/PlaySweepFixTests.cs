using Dovus.Core;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Casting;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Dovus.Core.Shared;

namespace CoreTests;

/// <summary>
/// Play sweep (Kılıç, 3 m merkez) kalanlarının çekirdek karşılığı.
/// Boss yarıçapı 0,85, oyuncu 0,50, duruş payı 0,15.
/// </summary>
[TestFixture]
public partial class PlaySweepFixTests
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
            Assert.That(_catalog.TryPlay((SkillId)id, out MotionTemplate template), Is.True, id);
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
        Assert.That(_catalog.TryPlay((SkillId)"3-12", out MotionTemplate glide), Is.True);
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
        Assert.That(_catalog.TryPlay((SkillId)"7-1", out MotionTemplate thorn), Is.True);
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
        Assert.That(_catalog.TryPlay((SkillId)"7-5", out MotionTemplate fan), Is.True);
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
        Assert.That(_catalog.TryPlay((SkillId)"3-3", out MotionTemplate hops), Is.True);
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
        Assert.That(clip.State, Is.EqualTo("Dodge"));
    }

}
