using Dovus.Core;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using Dovus.Core.Shared;

namespace CoreTests;

public partial class MotionTemplateTests
{
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
        Assert.That(_catalog.TryGet((SkillId)"3-6", out MotionBinding hook), Is.True);
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
}
