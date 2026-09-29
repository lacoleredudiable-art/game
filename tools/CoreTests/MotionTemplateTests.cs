using System.Collections.Generic;
using System.IO;
using Dovus.Core;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class MotionTemplateTests
{
    MotionTemplateCatalog _catalog;

    [SetUp]
    public void Load()
    {
        DesignWarnings.ResetForTests();
        _catalog = MotionTemplateCatalog.FromJson(File.ReadAllText(JsonPath()));
    }

    [Test]
    public void Catalog_MapsEveryCombo_AndTheFirstFourteenFamilies()
    {
        Assert.That(_catalog.FamilyCount, Is.EqualTo(42));
        Assert.That(_catalog.TemplateCount, Is.EqualTo(101));
        Assert.That(_catalog.SkillCount, Is.EqualTo(144));
        Assert.That(_catalog.CountImplementedFamilies(), Is.EqualTo(14));
        Assert.That(_catalog.CountReadySkills(), Is.EqualTo(49));

        for (int verb = 1; verb <= 12; verb++)
        for (int adjective = 1; adjective <= 12; adjective++)
        {
            string id = verb + "-" + adjective;
            Assert.That(_catalog.TryGet(id, out MotionBinding binding), Is.True, id);
            Assert.That(binding.Template.Phases.Count, Is.GreaterThan(0), id);
        }

        string docs = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "motion-templates.json"));
        Assert.That(docs, Is.EqualTo(File.ReadAllText(JsonPath())));
    }

    [Test]
    public void Catalog_StoresTags_WithoutNeedingTheFamilyToBePlayable()
    {
        Assert.That(Tags("1-1"), Does.Contain(MotionTemplateCatalog.TagSilah));
        Assert.That(Tags("1-2"), Does.Contain(MotionTemplateCatalog.TagSinir));
        Assert.That(_catalog.TryGet("1-2", out MotionBinding claw), Is.True);
        Assert.That(claw.SinirThreshold, Is.EqualTo(0.2f).Within(0.001f));
        Assert.That(_catalog.TryGet("1-8", out MotionBinding lift), Is.True);
        Assert.That(lift.SinirThreshold, Is.EqualTo(0.1f).Within(0.001f));
        Assert.That(lift.HasTag(MotionTemplateCatalog.TagSilah), Is.True);
        Assert.That(Tags("1-10"), Does.Contain(MotionTemplateCatalog.TagPortal));
        Assert.That(Tags("4-10"), Does.Not.Contain(MotionTemplateCatalog.TagPortal));
        Assert.That(Tags("6-8"), Does.Contain(MotionTemplateCatalog.TagTakim));
        Assert.That(Tags("10-1"), Does.Contain(MotionTemplateCatalog.TagSilah));
        Assert.That(_catalog.TryPlay("10-1", out _), Is.False, "aile 28 henüz oynanmaz");
    }

    [Test]
    public void PendingFamily_WarnsOnce_AndDoesNotReplaceTheOldSkill()
    {
        int warnings = 0;
        DesignWarnings.Warned += message =>
        {
            if (message != null && message.Contains("bekliyor", System.StringComparison.Ordinal))
                warnings++;
        };
        Assert.That(_catalog.TryPlay("2-2", out _), Is.False);
        Assert.That(_catalog.TryPlay("7-12", out _), Is.False);
        Assert.That(warnings, Is.EqualTo(1));
        Assert.That(DesignWarnings.WasWarned("motion.pending.15"), Is.True);
    }

    [Test]
    public void MissingPhaseDuration_WarnsOnce_AndUsesFallback()
    {
        const string json = """
        {"fallbacks":{"phase_sec":0.41,"step_m":1.2,"hit_length_m":1.5,"hit_radius_m":0.4,"max_hold_sec":0.9,"walk_mps":1.6,"height_m":0.9,"gap_m":0.9},
         "families":[{"id":1,"name":"Yükle ve bırak","implemented":true}],
         "templates":[{"id":"x","name":"x","family":1,"combos":[{"id":"1-1","tags":[]}],
           "phases":[{"name":"bos","motion":"hold"}]}]}
        """;
        int warnings = 0;
        DesignWarnings.Warned += message =>
        {
            if (message != null && message.Contains("faz süresi", System.StringComparison.Ordinal))
                warnings++;
        };
        var catalog = MotionTemplateCatalog.FromJson(json);
        MotionTemplateCatalog.FromJson(json);
        Assert.That(warnings, Is.EqualTo(1));
        Assert.That(catalog.TryPlay("1-1", out MotionTemplate template), Is.True);
        Assert.That(template.Phases[0].DurationSec, Is.EqualTo(0.41f).Within(0.001f));
    }

    [Test]
    public void Family01_ChargeHolds_ThenLungesTowardTheTarget()
    {
        MotionTemplate template = Ready("yukle_birak");
        var runner = new MotionTemplateRunner();
        var target = new MotionTarget(true, 0f, 4f);
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f);
        Step(runner, 0.5f, target, held: true);
        Assert.That(runner.Z, Is.LessThan(0.2f), "bırakmadan saplama yok");
        Assert.That(runner.Finished, Is.False);
        var hits = Step(runner, 0.3f, target, held: false);
        Assert.That(runner.Z, Is.GreaterThan(1.2f));
        Assert.That(runner.FaceZ, Is.GreaterThan(0.8f));
        Assert.That(hits, Has.Count.EqualTo(1));
        Assert.That(hits[0].Share, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void Family02_ThreeClawsHome_AndChannelWalks()
    {
        var claws = Play(Ready("kan_ceken_pence"), 1f, new MotionTarget(true, 2f, 3f));
        Assert.That(claws.Hits, Has.Count.EqualTo(3));
        Assert.That(claws.X, Is.GreaterThan(0.4f), "pençeler hedefe yana kayar");
        float share = 0f;
        for (int i = 0; i < claws.Hits.Count; i++)
            share += claws.Hits[i].Share;
        Assert.That(share, Is.EqualTo(1f).Within(0.02f));

        var channel = Play(Ready("basili_seri"), 1f, new MotionTarget(true, 0f, 4f), moveX: 1f);
        Assert.That(channel.Hits.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(channel.X, Is.GreaterThan(1.2f), "basılı seri yürür");
    }

    [Test]
    public void Family03_SideHopCrosses_ThreeBounces_StompThenAway()
    {
        var side = Play(Ready("yan_yan_sekme"), 1f, new MotionTarget(true, 0f, 3f));
        Assert.That(side.Hits, Has.Count.EqualTo(2));
        Assert.That(side.Hits[0].OriginX, Is.LessThan(-0.5f));
        Assert.That(side.Hits[1].OriginX, Is.GreaterThan(0.5f));

        var hops = Play(Ready("sekmeli_ziplama"), 1f, new MotionTarget(false, 0f, 0f));
        Assert.That(hops.Hits, Has.Count.EqualTo(3));
        Assert.That(hops.Z, Is.GreaterThan(2f));

        var stomp = Play(Ready("basip_sekme"), 1f, new MotionTarget(true, 0f, 4f));
        Assert.That(stomp.PeakY, Is.GreaterThan(0.6f));
        Assert.That(stomp.PeakZ, Is.GreaterThan(stomp.Z + 0.8f), "basınca sekip uzaklaşır");
    }

    [Test]
    public void Family04_SlamLands_LeapThenCrash_DiveReachesTarget()
    {
        var slam = Play(Ready("yere_cakilan"), 0.6f, new MotionTarget(true, 0f, 3f));
        Assert.That(slam.PeakY, Is.GreaterThan(0.5f));
        Assert.That(slam.Y, Is.LessThan(0.2f));
        Assert.That(slam.Hits, Has.Count.EqualTo(1));

        var crash = Play(Ready("sicrayip_cakilma"), 1f, new MotionTarget(false, 0f, 0f));
        Assert.That(crash.PeakY, Is.GreaterThan(0.8f));
        Assert.That(crash.Y, Is.LessThan(0.2f));
        Assert.That(crash.Z, Is.GreaterThan(2f));

        var dive = Play(Ready("dalis_patlamasi"), 0.6f, new MotionTarget(true, 0f, 4f));
        Assert.That(dive.Z, Is.EqualTo(4f).Within(0.35f));
        Assert.That(dive.Hits, Has.Count.EqualTo(1));
    }

    [Test]
    public void Family05_SpinStaysPut_FanSweeps_CracksAreThree()
    {
        var spin = Play(Ready("donen_kesik"), 0.55f, new MotionTarget(true, 0f, 3f));
        Assert.That(spin.X, Is.EqualTo(0f).Within(0.2f));
        Assert.That(spin.Z, Is.EqualTo(0f).Within(0.2f));
        Assert.That(spin.MidFaceZ, Is.LessThan(-0.4f), "dönüşün ortasında sırtı dönük");
        Assert.That(spin.FaceZ, Is.GreaterThan(0.7f), "tam tur başa döner");
        Assert.That(spin.Hits[0].Anchor, Is.EqualTo("ring"));

        var fan = Play(Ready("onde_yelpaze"), 0.8f, new MotionTarget(true, 0f, 3f));
        Assert.That(fan.Z, Is.EqualTo(0f).Within(0.2f));
        Assert.That(fan.FaceX, Is.GreaterThan(0.4f));
        Assert.That(fan.Hits, Has.Count.EqualTo(1));

        var cracks = Play(Ready("yer_yarigi"), 1f, new MotionTarget(true, 0f, 4f));
        Assert.That(cracks.Hits, Has.Count.EqualTo(3));
    }

    [Test]
    public void Family06_ChainThrowThenPull_LassoStays()
    {
        var chain = Play(Ready("zincirli_firlatma"), 1f, new MotionTarget(true, 0f, 4f));
        Assert.That(chain.Hits, Has.Count.EqualTo(2));
        Assert.That(chain.Hits[0].Anchor, Is.EqualTo("shot"));
        Assert.That(chain.Hits[0].OriginZ, Is.GreaterThan(2f));
        Assert.That(chain.Z, Is.GreaterThan(0.8f));

        var lasso = Play(Ready("silaha_kement"), 1f, new MotionTarget(true, 0f, 4f));
        Assert.That(lasso.Hits, Has.Count.EqualTo(1));
        Assert.That(lasso.Z, Is.EqualTo(0f).Within(0.2f), "kement atan yerinde kalır");
    }

    [Test]
    public void Family07_HookLandsPastTarget_YankStepsBack()
    {
        var hook = Play(Ready("kanca_cekis"), 1f, new MotionTarget(true, 0f, 4f));
        Assert.That(hook.Z, Is.GreaterThan(4f), "sırtına iner");
        Assert.That(hook.Hits[0].Anchor, Is.EqualTo("behind"));

        var yank = Play(Ready("bossu_cek"), 0.6f, new MotionTarget(true, 0f, 3f));
        Assert.That(yank.Z, Is.LessThan(-1f));

        var strip = Play(Ready("sokup_cekme"), 0.6f, new MotionTarget(true, 0f, 3f));
        Assert.That(strip.Hits, Has.Count.EqualTo(2));
        Assert.That(strip.Z, Is.LessThan(-0.5f));

        var ally = Play(Ready("dostu_kanca"), 0.7f, new MotionTarget(true, 0f, 4f));
        Assert.That(ally.Z, Is.GreaterThan(1.5f));
        Assert.That(ally.Hits, Has.Count.EqualTo(1));
    }

    [Test]
    public void Family08_SmokeCrouchThenExit_DashLeavesTheStart()
    {
        var smoke = Play(Ready("dumandan_cikis"), 0.6f, new MotionTarget(true, 0f, 3f));
        Assert.That(smoke.Hits, Has.Count.EqualTo(1));
        Assert.That(smoke.Hits[0].TimeSec, Is.GreaterThan(0.28f));
        Assert.That(smoke.Z, Is.GreaterThan(1f));

        var vanish = Play(Ready("duman_kaybolma"), 0.7f, new MotionTarget(true, 2f, 0f));
        Assert.That(vanish.Z, Is.GreaterThan(2f), "baktığı yöne atılır, hedefe yapışmaz");
    }

    [Test]
    public void Family09_UppercutRises_GeyserAtTarget_HangInAir()
    {
        var upper = Play(Ready("havaya_kaldirma"), 0.4f, new MotionTarget(true, 0f, 3f));
        Assert.That(upper.PeakY, Is.GreaterThan(0.6f));
        Assert.That(upper.Hits, Has.Count.EqualTo(1));

        var geyser = Play(Ready("yerden_fiskirma"), 0.6f, new MotionTarget(true, 0f, 3f));
        Assert.That(geyser.PeakY, Is.GreaterThan(0.5f));
        Assert.That(geyser.Hits[0].Anchor, Is.EqualTo("target"));

        var hang = Play(Ready("havada_asma"), 0.7f, new MotionTarget(true, 0f, 3f));
        Assert.That(hang.Y, Is.GreaterThan(0.8f), "sıçradıktan sonra havada kalır");
    }

    [Test]
    public void Family10_FuseIsDelayed_MarkThenStrike()
    {
        var fuse = Play(Ready("saplanan_fitil"), 2f, new MotionTarget(true, 0f, 4f));
        Assert.That(fuse.Hits, Has.Count.EqualTo(1));
        Assert.That(fuse.Hits[0].TimeSec, Is.GreaterThan(1.4f));
        Assert.That(fuse.Z, Is.LessThan(0f), "fitili saplayıp uzaklaşır");

        var mark = Play(Ready("isarete_vur"), 0.5f, new MotionTarget(true, 0f, 3f));
        Assert.That(mark.Hits, Has.Count.EqualTo(1));
        Assert.That(mark.Hits[0].TimeSec, Is.GreaterThan(0.22f));

        var blast = Play(Ready("isaret_patlamasi"), 2.2f, new MotionTarget(true, 0f, 4f));
        Assert.That(blast.Hits[0].TimeSec, Is.GreaterThan(1.4f));
        Assert.That(blast.Hits[0].Anchor, Is.EqualTo("target"));

        var guard = Play(Ready("koruyucu_isaret"), 0.4f, new MotionTarget(true, 0f, 2f));
        Assert.That(guard.Hits, Has.Count.EqualTo(1));
        Assert.That(guard.Z, Is.LessThan(1.2f));
    }

    [Test]
    public void Family11_FrontAndBehind_TwinMark()
    {
        var door = Play(Ready("sirtta_kapi"), 0.5f, new MotionTarget(true, 0f, 3f));
        Assert.That(door.Hits, Has.Count.EqualTo(2));
        Assert.That(door.Hits[0].OriginZ, Is.LessThan(3f));
        Assert.That(door.Hits[1].Anchor, Is.EqualTo("behind"));
        Assert.That(door.Hits[1].OriginZ, Is.GreaterThan(3f));

        var pincer = Play(Ready("onden_arkadan"), 0.5f, new MotionTarget(true, 0f, 3f));
        Assert.That(pincer.Hits, Has.Count.EqualTo(2));
        Assert.That(pincer.Hits[1].OriginZ, Is.GreaterThan(3f));

        var twin = Play(Ready("ters_es"), 0.6f, new MotionTarget(true, 0f, 3f));
        Assert.That(twin.Z, Is.EqualTo(0f).Within(0.2f));
        Assert.That(twin.Hits[1].Anchor, Is.EqualTo("target"));
        Assert.That(twin.Hits[1].Payload, Is.EqualTo("none"));
    }

    [Test]
    public void Family12_EchoAfterAThird_CourierWaits_ClonesFlank()
    {
        var echo = Play(Ready("golge_yankisi"), 0.7f, new MotionTarget(true, 0f, 3f));
        Assert.That(echo.Hits, Has.Count.EqualTo(2));
        float gap = echo.Hits[1].TimeSec - echo.Hits[0].TimeSec;
        Assert.That(gap, Is.InRange(0.25f, 0.4f));
        Assert.That(echo.Hits[1].Anchor, Is.EqualTo("side"));
        Assert.That(echo.Hits[1].OriginX, Is.LessThan(-0.8f));

        var courier = Play(Ready("golge_ulak"), 1.6f, new MotionTarget(true, 0f, 2f));
        Assert.That(courier.Hits, Has.Count.EqualTo(2));
        Assert.That(courier.Hits[1].TimeSec - courier.Hits[0].TimeSec, Is.GreaterThan(0.9f));

        var clones = Play(Ready("ayna_klonlar"), 0.7f, new MotionTarget(true, 0f, 3f));
        Assert.That(clones.Hits, Has.Count.EqualTo(2));
        Assert.That(clones.Hits[0].OriginX, Is.LessThan(-0.8f));
        Assert.That(clones.Hits[1].OriginX, Is.GreaterThan(0.8f));
        Assert.That(clones.X, Is.EqualTo(0f).Within(0.2f));
    }

    [Test]
    public void Family13_ShortTouch_AndStepOntoTheTarget()
    {
        var touch = Play(Ready("tek_dokunus"), 0.4f, new MotionTarget(true, 0f, 2f));
        Assert.That(touch.Hits, Has.Count.EqualTo(1));
        Assert.That(touch.Z, Is.LessThan(1f));
        Assert.That(touch.Z, Is.GreaterThan(0.2f));

        var nerve = Play(Ready("sinir_noktasi"), 0.5f, new MotionTarget(true, 0f, 3f));
        Assert.That(nerve.Z, Is.GreaterThan(2f));
        Assert.That(nerve.Hits, Has.Count.EqualTo(1));
    }

    [Test]
    public void Family14_ThrowLeavesTheBody_AndTheShotTravels()
    {
        var shot = Play(Ready("diken_firlatma"), 0.5f, new MotionTarget(true, 0f, 6f));
        Assert.That(shot.Z, Is.EqualTo(0f).Within(0.2f));
        Assert.That(shot.Hits, Has.Count.EqualTo(1));
        Assert.That(shot.Hits[0].Anchor, Is.EqualTo("shot"));
        Assert.That(shot.Hits[0].OriginZ, Is.GreaterThan(4f));
        Assert.That(shot.FaceZ, Is.GreaterThan(0.8f));
    }

    static MotionTemplate Ready(string id)
    {
        string path = JsonPath();
        var catalog = MotionTemplateCatalog.FromJson(File.ReadAllText(path));
        Assert.That(catalog.TryGetTemplate(id, out MotionTemplate template), Is.True, id);
        Assert.That(template.Implemented, Is.True, id);
        return template;
    }

    static List<string> Tags(string skill)
    {
        string path = JsonPath();
        var catalog = MotionTemplateCatalog.FromJson(File.ReadAllText(path));
        Assert.That(catalog.TryGet(skill, out MotionBinding binding), Is.True, skill);
        return new List<string>(binding.Tags);
    }

    static Sample Play(MotionTemplate template, float seconds, MotionTarget target, float moveX = 0f)
    {
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f);
        var hits = new List<MotionHit>();
        float peakY = 0f;
        float peakZ = 0f;
        float midFaceZ = 1f;
        bool midTaken = false;
        float left = seconds;
        while (left > 0f && !runner.Finished)
        {
            float dt = System.Math.Min(0.02f, left);
            MotionTick tick = runner.Tick(dt, target, new MotionStick(false, moveX, 0f));
            if (tick.Hits != null)
                hits.AddRange(tick.Hits);
            if (tick.Y > peakY)
                peakY = tick.Y;
            if (tick.Z > peakZ)
                peakZ = tick.Z;
            if (!midTaken && runner.Elapsed >= seconds * 0.45f)
            {
                midFaceZ = tick.FaceZ;
                midTaken = true;
            }
            left -= dt;
        }
        return new Sample(runner.X, runner.Y, runner.Z, runner.FaceX, runner.FaceZ, peakY, peakZ, midFaceZ, hits);
    }

    static List<MotionHit> Step(MotionTemplateRunner runner, float seconds, MotionTarget target, bool held)
    {
        var hits = new List<MotionHit>();
        float left = seconds;
        while (left > 0f && !runner.Finished)
        {
            float dt = System.Math.Min(0.02f, left);
            MotionTick tick = runner.Tick(dt, target, new MotionStick(held, 0f, 0f));
            if (tick.Hits != null)
                hits.AddRange(tick.Hits);
            left -= dt;
        }
        return hits;
    }

    readonly struct Sample
    {
        public Sample(float x, float y, float z, float faceX, float faceZ, float peakY, float peakZ, float midFaceZ, List<MotionHit> hits)
        {
            X = x;
            Y = y;
            Z = z;
            FaceX = faceX;
            FaceZ = faceZ;
            PeakY = peakY;
            PeakZ = peakZ;
            MidFaceZ = midFaceZ;
            Hits = hits;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float FaceX { get; }
        public float FaceZ { get; }
        public float PeakY { get; }
        public float PeakZ { get; }
        public float MidFaceZ { get; }
        public List<MotionHit> Hits { get; }
    }

    static string JsonPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "unity", "Assets", "Resources", "ElementSystem", "motion-templates.json"));
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }

    static string RepoRoot()
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", ".."));
    }
}

[TestFixture]
public class BasicStrikeSlotTests
{
    [Test]
    public void CenterStrike_UsesAttackRune_WhateverSitsInSlotOne()
    {
        var loadout = new RuneLoadout(new[] { 12, 1, 8, 6, 2, 5 });
        var slotted = new SentenceEngine(loadout: loadout);
        slotted.OnDotTouched(1, 0);
        Assert.That(slotted.State.Words[0].Rune, Is.EqualTo(Rune.Zaman), "slot 1 artık Zaman");

        var strike = new SentenceEngine(loadout: loadout);
        Assert.That(strike.BeginBasicStrike(1, 0), Is.True);
        Assert.That(strike.State.Words[0].Rune, Is.EqualTo(Rune.Saldiri));
        Assert.That(strike.State.Words[0].Slot, Is.EqualTo(0));
        strike.Commit();
        Assert.That(strike.History[0].Words[0].Rune, Is.EqualTo(Rune.Saldiri));
        Assert.That(strike.History[0].Words.Count, Is.EqualTo(1));

        Assert.That(strike.State.Phase, Is.EqualTo(SentencePhase.Recovering));
        Assert.That(strike.BeginBasicStrike(1, 50), Is.True, "toparlanma kilidini keser");
        Assert.That(strike.State.Words[0].Rune, Is.EqualTo(Rune.Saldiri));
    }
}
