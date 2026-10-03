using Dovus.Core;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

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
    public void Catalog_ReadsBasicStrike_WithoutChangingTemplateCount()
    {
        Assert.That(_catalog.TemplateCount, Is.EqualTo(102));
        Assert.That(_catalog.BasicStrike, Is.Not.Null);
        Assert.That(_catalog.BasicStrike.Phases.Count, Is.EqualTo(1));
        Assert.That(_catalog.BasicStrike.Phases[0].Motion, Is.EqualTo("lunge"));
        Assert.That(_catalog.BasicStrike.Phases[0].DistanceM, Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(_catalog.BasicStrike.Phases[0].Homing, Is.EqualTo("none"));
        Assert.That(MotionAim.IsEnemy(_catalog.BasicStrike.Aim), Is.True);
    }

    [Test]
    public void BasicStrike_Lunge_StopsAtBody_AndCapsDistance()
    {
        var boss = new MotionTarget(true, 0f, 4f, 0.85f);
        var step = Play(_catalog.BasicStrike, 0.12f, boss, bodyRadius: 0.5f);
        Assert.That(MotionHitGeometry.EdgeGap(step.X, step.Z, 0.5f, 0f, 4f, 0.85f), Is.GreaterThan(0.05f));
        float travel = System.MathF.Sqrt(step.X * step.X + step.Z * step.Z);
        Assert.That(travel, Is.LessThanOrEqualTo(0.6f + 0.02f));
    }

    [Test]
    public void Catalog_MapsEveryCombo_AndAllFortyTwoFamilies()
    {
        Assert.That(_catalog.FamilyCount, Is.EqualTo(42));
        Assert.That(_catalog.TemplateCount, Is.EqualTo(102));
        Assert.That(_catalog.SkillCount, Is.EqualTo(144));
        Assert.That(_catalog.CountImplementedFamilies(), Is.EqualTo(42));
        Assert.That(_catalog.CountReadySkills(), Is.EqualTo(144));

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
    public void EverySkillInElementSystem_HasATemplateBinding()
    {
        var docsCatalog = MotionTemplateCatalog.FromJson(
            File.ReadAllText(Path.Combine(RepoRoot(), "docs", "motion-templates.json")));
        using var doc = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Path.Combine(RepoRoot(), "docs", "element-sistemi.json")));
        var entries = doc.RootElement.GetProperty("skills_prose_144").GetProperty("entries");

        var ids = new List<string>();
        foreach (var verb in entries.EnumerateObject())
        foreach (var skill in verb.Value.EnumerateObject())
            ids.Add(skill.Name);

        Assert.That(ids, Has.Count.EqualTo(144));
        var missing = ids.FindAll(id => !docsCatalog.TryGet(id, out MotionBinding binding)
            || binding.Template == null || binding.Template.Phases.Count == 0);
        Assert.That(missing, Is.Empty);
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
        Assert.That(_catalog.TryPlay("10-1", out MotionTemplate parry), Is.True);
        Assert.That(parry.FamilyId, Is.EqualTo(28));
        Assert.That(Tags("3-10"), Does.Contain(MotionTemplateCatalog.TagPortal));
        Assert.That(Tags("3-10"), Does.Contain(MotionTemplateCatalog.TagTakim));
        Assert.That(Tags("5-4"), Does.Contain(MotionTemplateCatalog.TagTakim));
    }

    [Test]
    public void EveryCombo_ResolvesToAnImplementedTemplate()
    {
        int pending = 0;
        DesignWarnings.Warned += message =>
        {
            if (message != null && message.Contains("bekliyor", System.StringComparison.Ordinal))
                pending++;
        };

        for (int verb = 1; verb <= 12; verb++)
        for (int adjective = 1; adjective <= 12; adjective++)
        {
            string id = verb + "-" + adjective;
            Assert.That(_catalog.TryPlay(id, out MotionTemplate template), Is.True, id);
            Assert.That(template.Implemented, Is.True, id);
            Assert.That(template.Phases.Count, Is.GreaterThan(0), id);
            Assert.That(template.Phases[0].Name, Is.Not.EqualTo("bekliyor"), id);
        }

        Assert.That(pending, Is.EqualTo(0));
        Assert.That(_catalog.TryPlay("0-0", out _), Is.False);
        Assert.That(DesignWarnings.WasWarned("motion.missing.0-0"), Is.True);
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
        var boss = new MotionTarget(true, 0f, 3f, 0.85f);
        var side = Play(Ready("yan_yan_sekme"), 1f, boss, bodyRadius: 0.5f);
        Assert.That(side.Hits, Has.Count.EqualTo(2));
        Assert.That(MotionHitGeometry.Overlaps(
            side.Hits[0].OriginX, side.Hits[0].OriginZ, side.Hits[0].DirX, side.Hits[0].DirZ,
            side.Hits[0].LengthM, side.Hits[0].RadiusM, side.Hits[0].Anchor, 0f, 3f, 0.85f), Is.True);
        Assert.That(MotionHitGeometry.Overlaps(
            side.Hits[1].OriginX, side.Hits[1].OriginZ, side.Hits[1].DirX, side.Hits[1].DirZ,
            side.Hits[1].LengthM, side.Hits[1].RadiusM, side.Hits[1].Anchor, 0f, 3f, 0.85f), Is.True);

        var hops = Play(Ready("sekmeli_ziplama"), 1f, new MotionTarget(false, 0f, 0f));
        Assert.That(hops.Hits, Has.Count.EqualTo(2));
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

        var dive = Play(Ready("dalis_patlamasi"), 0.6f, new MotionTarget(true, 0f, 4f, 0.85f), bodyRadius: 0.5f);
        Assert.That(MotionHitGeometry.EdgeGap(dive.X, dive.Z, 0.5f, 0f, 4f, 0.85f), Is.GreaterThan(0.05f),
            "dalış boss'un içine girmez");
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
        var hookBoss = new MotionTarget(true, 0f, 4f, 0.85f);
        var hook = Play(Ready("kanca_cekis"), 1.2f, hookBoss, bodyRadius: 0.5f);
        Assert.That(hook.Z, Is.GreaterThan(4f), "sırtına iner");
        Assert.That(MotionHitGeometry.EdgeGap(hook.X, hook.Z, 0.5f, 0f, 4f, 0.85f), Is.GreaterThan(0.05f));
        Assert.That(hook.Hits, Has.Some.Property("Anchor").EqualTo("behind"));
        MotionHit behind = hook.Hits.Find(h => h.Anchor == "behind");
        Assert.That(MotionHitGeometry.Overlaps(
            behind.OriginX, behind.OriginZ, behind.DirX, behind.DirZ,
            behind.LengthM, behind.RadiusM, behind.Anchor, 0f, 4f, 0.85f), Is.True);

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
        var fuse = Play(Ready("saplanan_fitil"), 2f, new MotionTarget(true, 0f, 4f, 0.85f), bodyRadius: 0.5f);
        MotionHit boom = fuse.Hits.Find(h => h.Payload == "effect");
        MotionHit marker = fuse.Hits.Find(h => h.Payload == "marker");
        Assert.That(marker.TimeSec, Is.LessThan(0.4f));
        Assert.That(boom.TimeSec, Is.GreaterThan(1.4f));
        Assert.That(boom.Anchor, Is.EqualTo("plant"));
        Assert.That(boom.OriginZ, Is.EqualTo(marker.OriginZ).Within(0.05f), "patlama fitilin çakıldığı yerde");
        Assert.That(fuse.Z, Is.LessThan(boom.OriginZ), "fitili saplayıp uzaklaşır");

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
        Assert.That(MotionHitGeometry.Overlaps(
            door.Hits[1].OriginX, door.Hits[1].OriginZ, door.Hits[1].DirX, door.Hits[1].DirZ,
            door.Hits[1].LengthM, door.Hits[1].RadiusM, door.Hits[1].Anchor, 0f, 3f, 0.85f), Is.True);

        var pincer = Play(Ready("onden_arkadan"), 0.5f, new MotionTarget(true, 0f, 3f));
        Assert.That(pincer.Hits, Has.Count.EqualTo(2));
        Assert.That(MotionHitGeometry.Overlaps(
            pincer.Hits[1].OriginX, pincer.Hits[1].OriginZ, pincer.Hits[1].DirX, pincer.Hits[1].DirZ,
            pincer.Hits[1].LengthM, pincer.Hits[1].RadiusM, pincer.Hits[1].Anchor, 0f, 3f, 0.85f), Is.True);

        var twin = Play(Ready("ters_es"), 0.6f, new MotionTarget(true, 0f, 3f));
        Assert.That(twin.Z, Is.EqualTo(0f).Within(0.2f));
        Assert.That(twin.Hits[1].Anchor, Is.EqualTo("target"));
        Assert.That(twin.Hits[1].Payload, Is.EqualTo("none"));
    }

    [Test]
    public void Skill1_11_EchoOpensAtPlayersCurrentPosition_AfterBeingMoved()
    {
        var target = new MotionTarget(true, 0f, 3f);
        var still = new MotionTemplateRunner();
        still.Begin(Ready("golge_yankisi"), 0f, 0f, 0f, 0f, 1f, 0.5f, 0.15f);
        var stillHits = new List<MotionHit>();
        var moved = new MotionTemplateRunner();
        moved.Begin(Ready("golge_yankisi"), 0f, 0f, 0f, 0f, 1f, 0.5f, 0.15f);
        var movedHits = new List<MotionHit>();

        bool shifted = false;
        float shiftedX = 0f, shiftedZ = 0f;
        float stillX = 0f, stillZ = 0f, movedX = 0f, movedZ = 0f;
        for (int i = 0; i < 60 && !moved.Finished; i++)
        {
            if (!shifted && movedHits.Count == 1 && moved.Elapsed >= 0.2f)
            {
                // İlk vuruştan sonra, bekleme fazında oyuncu boss'a 1 m yaklaştı, 0,4 m sola kaydı.
                moved.Rebase(moved.X - 0.4f, moved.Z + 1f);
                shiftedX = moved.X;
                shiftedZ = moved.Z;
                shifted = true;
            }
            MotionTick a = still.Tick(0.02f, target, default);
            if (a.Hits != null && a.Hits.Length > 0)
            {
                stillHits.AddRange(a.Hits);
                stillX = still.X;
                stillZ = still.Z;
            }
            MotionTick b = moved.Tick(0.02f, target, default);
            if (b.Hits != null && b.Hits.Length > 0)
            {
                movedHits.AddRange(b.Hits);
                movedX = moved.X;
                movedZ = moved.Z;
            }
        }

        Assert.That(shifted, Is.True);
        Assert.That(movedHits, Has.Count.EqualTo(2));
        Assert.That(stillHits, Has.Count.EqualTo(2));
        MotionHit echo = movedHits[1];
        MotionHit oldEcho = stillHits[1];
        Assert.That(echo.Anchor, Is.EqualTo("side"));
        // Oyuncu taşındığı yerde kalır; kalıp onu ilk vuruşun yerine geri çekmez.
        Assert.That(movedX, Is.EqualTo(shiftedX).Within(0.01f));
        Assert.That(movedZ, Is.EqualTo(shiftedZ).Within(0.01f));
        // Yankı eski yerde değil, oyuncunun şimdiki yerinde açılır: gövdeye uzaklığı aynı,
        // eski yankı noktasından ise ~1 m uzakta.
        float Dist(float ax, float az, float bx, float bz) => System.MathF.Sqrt((ax - bx) * (ax - bx) + (az - bz) * (az - bz));
        Assert.That(Dist(echo.OriginX, echo.OriginZ, movedX, movedZ),
            Is.EqualTo(Dist(oldEcho.OriginX, oldEcho.OriginZ, stillX, stillZ)).Within(0.02f));
        Assert.That(Dist(echo.OriginX, echo.OriginZ, oldEcho.OriginX, oldEcho.OriginZ), Is.GreaterThan(0.9f));
        // Zamanlama değişmez: ikinci darbe ilkinden ~0,31 sn sonra.
        float gap = echo.TimeSec - movedHits[0].TimeSec;
        Assert.That(gap, Is.EqualTo(stillHits[1].TimeSec - stillHits[0].TimeSec).Within(0.001f));
        Assert.That(gap, Is.EqualTo(0.31f).Within(0.03f));
    }

    [Test]
    public void Rebase_KeepsReturnMarkInWorld()
    {
        var runner = new MotionTemplateRunner();
        runner.Begin(Ready("golge_yankisi"), 2f, 0f, 1f, 0f, 1f, 0.5f, 0.15f);
        runner.Tick(0.05f, new MotionTarget(true, 2f, 4f), default);
        runner.Rebase(runner.X + 1f, runner.Z);
        Assert.That(runner.MarkX, Is.EqualTo(2f));
        Assert.That(runner.MarkZ, Is.EqualTo(1f));
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
    public void EnemyAim_IgnoresSelfEffectTarget()
    {
        Assert.That(_catalog.TryGet("3-6", out MotionBinding hook), Is.True);
        Assert.That(hook.Template.Aim, Is.EqualTo(MotionAim.Enemy));
        Assert.That(MotionAim.TryResolveEnemy(
            hasSelectedEnemy: true, 2f, 5f,
            hasAutoEnemy: true, 0f, 1f,
            out float x, out float z), Is.True);
        Assert.That(x, Is.EqualTo(2f).Within(0.001f));
        Assert.That(z, Is.EqualTo(5f).Within(0.001f));

        Assert.That(MotionAim.TryResolveEnemy(
            false, 0f, 0f, true, 0f, 4f, out x, out z), Is.True);
        Assert.That(z, Is.EqualTo(4f).Within(0.001f), "seçim yoksa menzildeki düşman");
        Assert.That(MotionAim.TryResolveEnemy(false, 9f, 9f, false, 0f, 0f, out _, out _), Is.False);
    }

    [Test]
    public void Lunge_StopsEdgeToEdge_AndDoesNotEnterTheBody()
    {
        MotionTemplate template = Ready("yukle_birak");
        var boss = new MotionTarget(true, 0f, 3f, 0.85f);
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, 0.5f, 0.15f);
        Step(runner, 0.2f, boss, held: false);
        Step(runner, 0.6f, boss, held: false);
        float gap = MotionHitGeometry.EdgeGap(runner.X, runner.Z, 0.5f, 0f, 3f, 0.85f);
        Assert.That(gap, Is.GreaterThan(0.1f), "kenarlar birbirine girmez");

        var close = new MotionTarget(true, 0f, 1.0f, 0.85f);
        var inside = new MotionTemplateRunner();
        inside.Begin(template, 0f, 0f, 0f, 0f, 1f, 0.5f, 0.15f);
        Step(inside, 0.8f, close, held: false);
        Assert.That(inside.Z, Is.LessThan(0.05f), "zaten içerdeyse daha içeri saplanmaz");
    }

    [Test]
    public void CastRange_IsEdgeToEdge_AndIncludesLungeTravel()
    {
        MotionTemplate lunge = Ready("yukle_birak");
        float edge = MotionCastReach.EdgeReachM(lunge);
        Assert.That(edge, Is.GreaterThan(1.7f), "saplama mesafesi menzile girer");
        float gate = MotionCastReach.GateRangeM(edge, 0.5f);
        // Eski kapı ~0.6 m yüzey mesafesiydi; 1 m yüzey (kenar 0.5 m) artık sığar.
        Assert.That(1.0f, Is.LessThan(gate));
        Assert.That(gate, Is.EqualTo(edge + 0.5f).Within(0.001f));
    }

    [Test]
    public void Fuse_StaysAtThePlantedPoint_WhenTheBossMoves()
    {
        MotionTemplate template = Ready("saplanan_fitil");
        var runner = new MotionTemplateRunner();
        var boss = new MotionTarget(true, 0f, 4f, 0.85f);
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, 0.5f, 0.15f);
        MotionHit marker = default;
        bool marked = false;
        float left = 0.5f;
        while (left > 0f && !marked)
        {
            MotionTick tick = runner.Tick(0.02f, boss, default);
            if (tick.Hits != null)
            {
                for (int i = 0; i < tick.Hits.Length; i++)
                {
                    if (tick.Hits[i].Payload == "marker")
                    {
                        marker = tick.Hits[i];
                        marked = true;
                    }
                }
            }
            left -= 0.02f;
        }
        Assert.That(marked, Is.True);
        var walked = new MotionTarget(true, 0f, 9f, 0.85f);
        MotionHit boom = default;
        bool exploded = false;
        left = 2f;
        while (left > 0f && !runner.Finished)
        {
            MotionTick tick = runner.Tick(0.02f, walked, default);
            if (tick.Hits != null)
            {
                for (int i = 0; i < tick.Hits.Length; i++)
                {
                    if (tick.Hits[i].Payload == "effect")
                    {
                        boom = tick.Hits[i];
                        exploded = true;
                    }
                }
            }
            left -= 0.02f;
        }
        Assert.That(exploded, Is.True);
        Assert.That(boom.OriginZ, Is.EqualTo(marker.OriginZ).Within(0.05f));
        Assert.That(boom.OriginZ, Is.LessThan(8f), "patlama boss'un yeni yerinde değil");
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

    [Test]
    public void EveryTemplate_HitsTheStandingTarget_AndNeverEndsInside()
    {
        string elements = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "element-sistemi.json"));
        var grammar = new Dovus.Core.Mechanic.MechanicGrammar(Dovus.Core.Mechanic.MechanicRules.FromJson(elements));
        Assert.That(grammar.Rules.IsValid, Is.True);

        const float body = 0.5f;
        const float bossR = 0.85f;
        const float stop = 0.15f;
        var failures = new List<string>();

        for (int i = 0; i < _catalog.Templates.Count; i++)
        {
            MotionTemplate template = _catalog.Templates[i];
            MotionTarget target = StandingTarget(template, bossR);
            CastSample cast = SampleCast(template, target, body, stop);

            float endGap = MotionHitGeometry.EdgeGap(cast.X, cast.Z, body, target.X, target.Z, bossR);
            if (endGap <= 0.04f)
                failures.Add(template.Id + " bitiş içinde gap=" + endGap.ToString("0.00"));

            bool crosses = false;
            bool behind = false;
            bool overshoot = false;
            bool returns = false;
            bool retreats = false;
            for (int p = 0; p < template.Phases.Count; p++)
            {
                MotionPhase phase = template.Phases[p];
                if (phase.Land == "behind" || phase.OvershootM > 0.01f)
                    crosses = true;
                if (phase.Land == "behind")
                    behind = true;
                if (phase.OvershootM > 0.01f)
                    overshoot = true;
                if (phase.Motion == "return")
                    returns = true;
                if (phase.Motion == "retreat")
                    retreats = true;
            }

            if (!crosses)
            {
                for (int s = 0; s < cast.Gaps.Count; s++)
                {
                    if (cast.Gaps[s] <= 0.04f)
                    {
                        failures.Add(template.Id + " kare " + s + " içinde");
                        break;
                    }
                }
            }

            if (behind && cast.PeakZ <= target.Z)
                failures.Add(template.Id + " arkaya inmeli peak=" + cast.PeakZ.ToString("0.00"));

            if (overshoot && cast.Z <= target.Z)
                failures.Add(template.Id + " öte kenarda bitmeli z=" + cast.Z.ToString("0.00"));

            if (returns)
            {
                if (cast.PeakZ <= 1.2f)
                    failures.Add(template.Id + " işaretten uzaklaşmalı");
                if (MathF.Abs(cast.Z) > 0.2f)
                    failures.Add(template.Id + " işarete dönmeli z=" + cast.Z.ToString("0.00"));
            }
            else if (retreats && !behind && cast.Z >= -0.4f)
            {
                failures.Add(template.Id + " geri çekilmeli z=" + cast.Z.ToString("0.00"));
            }

            if (template.Id == "suzulme" && cast.PeakY < 0.6f)
                failures.Add("suzulme yerden kalkmadı");

            for (int h = 0; h < cast.Hits.Count; h++)
            {
                MotionHit hit = cast.Hits[h];
                if (hit.Payload is "none" or "marker")
                    continue;
                if (hit.Anchor == "side")
                {
                    if (MathF.Abs(hit.OriginX) <= 0.5f)
                        failures.Add(template.Id + " yan " + hit.Phase + " x=" + hit.OriginX.ToString("0.00"));
                    continue;
                }

                if (hit.Anchor is "self")
                    continue;
                if (hit.Anchor is not ("forward" or "shot" or "target" or "behind" or "target_side" or "plant" or "ring"))
                    continue;

                bool overlaps = MotionHitGeometry.Overlaps(
                    hit.OriginX, hit.OriginZ, hit.DirX, hit.DirZ,
                    hit.LengthM, hit.RadiusM, hit.Anchor,
                    target.X, target.Z, bossR);
                if (!overlaps)
                    failures.Add(template.Id + " " + hit.Phase + " " + hit.Anchor
                        + " hedefz=" + target.Z.ToString("0.00"));
            }
        }

        Assert.That(failures.Count, Is.EqualTo(0), string.Join(" | ", failures));

        for (int verb = 1; verb <= 12; verb++)
        for (int adjective = 1; adjective <= 12; adjective++)
        {
            string id = verb + "-" + adjective;
            Assert.That(_catalog.TryGet(id, out MotionBinding binding), Is.True, id);
            var plan = grammar.Compose(verb, adjective, 1);
            bool grammarMoves = false;
            foreach (var effect in plan.Effects)
            {
                if (PositionOwnership.Kind(effect.Atom, effect.Stat) != PositionStepKind.None)
                    grammarMoves = true;
            }
            bool templateMoves = PositionOwnership.MovesPlayer(binding.Template);
            Assert.That(
                PositionOwnershipOracle.PositionWriters(templateMoves, grammarMoves),
                Is.LessThanOrEqualTo(1),
                id);
        }
    }

    static MotionTarget StandingTarget(MotionTemplate template, float bossR)
    {
        float shot = 0f;
        float ring = 0f;
        bool aimed = false;
        bool behind = false;
        bool overshoot = false;
        bool returns = false;
        bool retreats = false;
        for (int i = 0; i < template.Phases.Count; i++)
        {
            MotionPhase phase = template.Phases[i];
            if (phase.Land == "behind")
                behind = true;
            if (phase.OvershootM > 0.01f)
                overshoot = true;
            if (phase.Motion == "return")
                returns = true;
            if (phase.Motion == "retreat")
                retreats = true;
            MotionHitSpec hit = phase.Hit;
            if (hit == null || hit.Payload is "none" or "marker")
                continue;
            if (hit.Anchor == "shot")
                shot = MathF.Max(shot, phase.ShotM * Math.Clamp(hit.At, 0.05f, 1f));
            if (hit.Anchor == "ring")
                ring = MathF.Max(ring, hit.RadiusM);
            if (hit.Anchor is "forward" or "target" or "behind" or "target_side" or "plant")
                aimed = true;
        }

        float z;
        if (returns)
            z = 4f;
        else if (shot > 2.2f && retreats)
            z = MathF.Max(1.9f, shot - 0.6f);
        else if (shot > 2.2f)
            z = shot;
        else if (behind || overshoot)
            z = 3.2f;
        else if (ring > 0.4f && !aimed)
            z = Math.Clamp(ring + bossR - 0.25f, 1.75f, 3f);
        else
            z = 1.9f;
        return new MotionTarget(true, 0f, z, bossR);
    }

    static CastSample SampleCast(MotionTemplate template, MotionTarget target, float body, float stop)
    {
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, body, stop);
        var hits = new List<MotionHit>();
        var gaps = new List<float>();
        float peakZ = 0f;
        float peakY = 0f;
        var stick = new MotionStick(false, 0f, 0f);
        for (int i = 0; i < 800 && !runner.Finished; i++)
        {
            MotionTick tick = runner.Tick(0.02f, target, stick);
            if (tick.Hits != null)
                hits.AddRange(tick.Hits);
            if (tick.Z > peakZ)
                peakZ = tick.Z;
            if (tick.Y > peakY)
                peakY = tick.Y;
            gaps.Add(MotionHitGeometry.EdgeGap(tick.X, tick.Z, body, target.X, target.Z, target.RadiusM));
        }
        Assert.That(runner.Finished, Is.True, template.Id);
        return new CastSample(runner.X, runner.Y, runner.Z, peakY, peakZ, hits, gaps);
    }

    readonly struct CastSample
    {
        public CastSample(float x, float y, float z, float peakY, float peakZ, List<MotionHit> hits, List<float> gaps)
        {
            X = x;
            Y = y;
            Z = z;
            PeakY = peakY;
            PeakZ = peakZ;
            Hits = hits;
            Gaps = gaps;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float PeakY { get; }
        public float PeakZ { get; }
        public List<MotionHit> Hits { get; }
        public List<float> Gaps { get; }
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

    static Sample Play(MotionTemplate template, float seconds, MotionTarget target, float moveX = 0f, float bodyRadius = 0.5f)
    {
        var runner = new MotionTemplateRunner();
        runner.Begin(template, 0f, 0f, 0f, 0f, 1f, bodyRadius, 0.15f);
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

    [Test]
    public void AfterLoadout_FirstCenterTap_IsNotSwallowed()
    {
        Assert.That(BasicStrikeInput.AllowsCenterStrike(
            drawAllowed: false, loadoutJustApplied: true, engineAcceptsStrike: true), Is.True);
        Assert.That(BasicStrikeInput.AllowsCenterStrike(
            drawAllowed: false, loadoutJustApplied: false, engineAcceptsStrike: true), Is.False);
        Assert.That(BasicStrikeInput.DealsDamage(capsuleHit: false, enemyInEdgeReachAtImpact: true), Is.True);
        Assert.That(BasicStrikeInput.ReplaceStaleView(sentenceIsBasic: true, viewIsBasic: false), Is.True);
        Assert.That(BasicStrikeInput.ReplaceStaleView(sentenceIsBasic: true, viewIsBasic: true), Is.False);
    }
}
