using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dovus.Core.Dodge;
using Dovus.Core.Presentation;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using Dovus.Game.Skills.RuleEngineV4;
using Dovus.Game.Vfx;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>v4/duzeltme-1: dodge çarpışması ve klibi, düz vuruş klibi, kanat ömrü, Yay hareketi, kor silindiri.</summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const string BackstepClip = "Ortak_GeriSicrama";
        const string HammerStrikeClip = "Cekic_VUR";
        const string BowStrikeClip = "Yay_VUR";
        /// <summary>Görev: dodge boyunca ayaklar yerin en çok 3 cm altına inebilir.</summary>
        const float FootSinkMaxM = 0.03f;
        /// <summary>Görev: siluet ekranın en çok %25'ini kaplar.</summary>
        const float SilhouetteScreenMaxFrac = 0.25f;
        const float DissolveEps = 1e-3f;
        const float DodgeGuardSec = 3f;
        const float BasicStrikeWatchSec = 1.5f;
        /// <summary>Kayma engele çarptığında oyuncu yüzeyde durur (gövde + iç pay + kare toleransı).</summary>
        const float SurfaceStopTolM = 0.15f;

        [UnityTest]
        public IEnumerator Dodge_IntoColumnAndCocoon_NeverEntersObstacle()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            var motor = _player.GetComponent<KinematicMotorController>();
            Assert.That(motor, Is.Not.Null, "KinematicMotorController");
            float bodyR = Mathf.Max(KinematicMotorControllerDefaults.CapsuleRadiusFloorM,
                motor.BodyRadiusM * KinematicMotorControllerDefaults.CapsuleRadiusBodyScale);
            float dodgeM = InputController().Combat.Dodge.DistanceM;

            RuleEngineV4SliceArenaColumn col = ArenaSpec.Columns[0];
            SliceArenaCocoonHost cocoon = Cocoons()[0];
            var obstacles = new (string name, Vector3 center, float radius)[]
            {
                ("sütun", new Vector3(col.XM, 0f, col.ZM), col.RadiusM),
                ("koza", Flat(cocoon.transform.position), ArenaSpec.Cocoons[0].RadiusM),
            };
            // 1 m: kayma çarpar. dodgeM + 5 cm: kayma engelin önünde biter, yalnız toparlanma kuyruğu iter.
            float[] gaps = { 1f, dodgeM + PosTolM };
            foreach ((string name, Vector3 center, float radius) in obstacles)
            foreach (float gap in gaps)
            {
                _label = $"dodge → {name} (boşluk {gap:0.00} m)";
                Vector3 outward = Vector3.right;
                float contact = radius + bodyR;
                Place(_player, center + outward * (contact + gap));
                // Çubuksuz dodge bakışın tersine kayar: sırt engele dönük.
                _player.rotation = Quaternion.LookRotation(outward);
                Physics.SyncTransforms();
                Vector3 start = Flat(_player.position);

                float minDist = float.MaxValue;
                yield return DodgeAndWatch(() => minDist = Mathf.Min(minDist, Flat(_player.position - center).magnitude));

                float moved = Flat(_player.position - start).magnitude;
                Debug.Log($"[duzeltme1] {_label}: en yakın merkez={minDist:0.000} m, temas={contact:0.000} m, kayma={moved:0.00} m");
                Assert.That(moved, Is.GreaterThan(PosTolM), $"{_label}: dodge kaymadı");
                Assert.That(minDist, Is.GreaterThanOrEqualTo(contact - PosTolM),
                    $"{_label}: oyuncu engelin içine girdi (merkez {minDist:0.000} m < temas {contact:0.000} m)");
                if (gap < dodgeM)
                    Assert.That(Flat(_player.position - center).magnitude, Is.LessThanOrEqualTo(contact + SurfaceStopTolM),
                        $"{_label}: engelde durmadı");
            }
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator Dodge_RollEndsWithSlide_RiseInRecovery_FeetAboveGround()
        {
            FreezeBoss();
            Equip(Kilic);
            Arrange(Vector3.right);
            yield return new WaitForSecondsRealtime(BootSettleSec);
            Animator anim = PlayerAnimator();
            SkinnedMeshRenderer[] skins = anim.GetComponentsInChildren<SkinnedMeshRenderer>()
                .Where(s => s.enabled && s.sharedMesh != null).ToArray();
            Assert.That(skins, Is.Not.Empty, "oyuncuda skinned mesh yok");
            var baked = new UnityEngine.Mesh();
            float groundY = GroundYUnder(_player);
            float idleFoot = LowestVertexY(skins, baked) - groundY;
            DodgeState dodge = InputController().Dodge;
            int rollHash = Animator.StringToHash(DodgeClipPlayback.RollState);
            int riseHash = Animator.StringToHash(DodgeClipPlayback.RiseState);

            float minFoot = float.MaxValue;
            string minAt = string.Empty;
            double riseAtMs = -1;
            bool sawRoll = false;
            float maxDtMs = 0f;
            yield return DodgeAndWatch(() =>
            {
                maxDtMs = Mathf.Max(maxDtMs, Time.deltaTime * (float)Units.SecToMs);
                AnimatorStateInfo st = anim.GetCurrentAnimatorStateInfo(0);
                sawRoll |= st.shortNameHash == rollHash;
                if (riseAtMs < 0 && st.shortNameHash == riseHash)
                    riseAtMs = _md.MechanicsClock.Director.WorldTimeMs;
                float foot = LowestVertexY(skins, baked) - groundY;
                if (foot < minFoot)
                {
                    minFoot = foot;
                    string state = st.shortNameHash == rollHash ? "Dodge" : st.shortNameHash == riseHash ? "DodgeRise" : st.shortNameHash.ToString();
                    minAt = $"state={state} n={st.normalizedTime:0.00} geçiş={anim.IsInTransition(0)} " +
                            $"kalça={anim.GetBoneTransform(HumanBodyBones.Hips).position.y - groundY:0.00} " +
                            $"görsel={anim.transform.position.y - groundY:0.00} kök={_player.position.y - groundY:0.00}";
                }
            });
            Object.Destroy(baked);

            int pressMs = dodge.PressTimeMs ?? -1;
            double expectRiseMs = pressMs + dodge.SlideEndOffsetMs;
            Debug.Log($"[duzeltme1] dodge: en alçak köşe={minFoot * 1000f:0.0} mm ({minAt}), duruşta={idleFoot * 1000f:0.0} mm (yer {groundY:0.000}), " +
                      $"kalkış={riseAtMs - pressMs:0} ms (kayma sonu {dodge.SlideEndOffsetMs} ms, toparlanma {dodge.RecoveryMs} ms)");
            Assert.That(sawRoll, Is.True, "Dodge (takla) state'i oynamadı");
            Assert.That(riseAtMs, Is.GreaterThanOrEqualTo(0), "DodgeRise (kalkış) toparlanmada oynamadı");
            Assert.That(riseAtMs - expectRiseMs, Is.InRange(-2f * maxDtMs, 2f * maxDtMs),
                "takla kaymayla aynı anda bitmedi");
            Assert.That(minFoot, Is.GreaterThanOrEqualTo(-FootSinkMaxM), $"ayak yerin {-minFoot * 100f:0.0} cm altına indi");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator BasicStrike_PlaysWeaponVurClip_NoTrail()
        {
            FreezeBoss();
            foreach ((int weapon, string clip) in new[] { (Kilic, SwordStrikeClip), (Yay, BowStrikeClip) })
            {
                _label = $"düz vuruş w{weapon}";
                Equip(weapon);
                Arrange(Vector3.right);
                Place(_minions[0].transform, Origin + Vector3.right * 2f);
                Physics.SyncTransforms();
                HexagonInputController input = InputController();
                yield return WaitSentenceIdle(input);
                Animator anim = PlayerAnimator();
                var relay = anim.GetComponent<SkillAnimVfxEventHost>();
                var vfx = _player.GetComponentInChildren<RuleDrivenVfxDirector>();
                Assert.That(relay, Is.Not.Null, "SkillAnimVfxEventHost");
                Assert.That(vfx, Is.Not.Null, "RuleDrivenVfxDirector");
                AnimationClip expected = anim.runtimeAnimatorController.animationClips.First(c => c.name == clip);
                bool hasTrailEvent = expected.events.Any(e => e.functionName.StartsWith("Trail", StringComparison.Ordinal));
                int suppressedBefore = relay.SuppressedTrailEvents;

                input.Pointer.BeginPointer(CenterPos(input));
                input.Pointer.EndPointer(cancelled: false);

                var played = new HashSet<string>();
                bool trail = false;
                float end = Time.realtimeSinceStartup + BasicStrikeWatchSec;
                while (Time.realtimeSinceStartup < end)
                {
                    yield return null;
                    foreach (AnimatorClipInfo ci in anim.GetCurrentAnimatorClipInfo(0))
                    {
                        if (ci.weight > ClipActiveWeight)
                            played.Add(ci.clip.name);
                    }
                    trail |= vfx.TrailOn;
                }
                string last = RuleEngineV4SkillAnimBridge.LastBasicStrikeClip;
                Debug.Log($"[duzeltme1] {_label}: oynayan klip={last}; ağırlıklı klipler={string.Join(",", played)}; " +
                          $"yutulan Trail_On={relay.SuppressedTrailEvents - suppressedBefore}");
                Assert.That(last, Is.EqualTo(clip), $"{_label}: silah setinin klibi oynamadı");
                Assert.That(played, Does.Contain(clip), $"{_label}: {clip} animatörde oynamadı");
                Assert.That(played, Does.Not.Contain(HammerStrikeClip), $"{_label}: eski yumruk/çekiç klibine düştü");
                Assert.That(trail, Is.False, $"{_label}: düz vuruşta kor izi açıldı");
                if (hasTrailEvent)
                    Assert.That(relay.SuppressedTrailEvents, Is.GreaterThan(suppressedBefore),
                        $"{_label}: klibin Trail_On olayı gelmedi (yutma kanıtı yok)");
                yield return WaitSentenceIdle(input);
            }
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator Wing_LifeAndFadesFromJson_MaxTwoLive_QuarterScreen()
        {
            FreezeBoss();
            Equip(Yay);
            RuleEngineV4SkillAnimBinding binding = _catalog.Catalog.SkillAnim.Resolve(3, 3, Yay);
            Assert.That(binding.EntrySilhouetteLifeSec, Is.EqualTo(0.7f).Within(StTol), "JSON life_sec");
            Assert.That(binding.EntrySilhouetteFadeInSec, Is.EqualTo(0.1f).Within(StTol), "JSON fade_in_sec");
            Assert.That(binding.EntrySilhouetteFadeOutSec, Is.EqualTo(0.5f).Within(StTol), "JSON fade_out_sec");
            DragonSilhouetteCardView card = SilhouetteCard();
            Camera cam = Camera.main;
            Assert.That(cam, Is.Not.Null, "ana kamera yok");

            _label = "3-3 yay kanat";
            Arrange(Vector3.right);
            Place(_minions[0].transform, Origin + Vector3.right * 4f);
            Physics.SyncTransforms();
            Assert.That(_cast.TryLaunchCombo(3, 3, out _), Is.True, $"{_label} cast edilmedi");
            float onAt = -1f, fullAt = -1f, fadeAt = -1f, offAt = -1f, maxDt = 0f, maxCover = 0f;
            var corners = new Vector3[4];
            float guard = Time.realtimeSinceStartup + CastTimeoutSec;
            while (Time.realtimeSinceStartup < guard && (onAt < 0f || offAt < 0f))
            {
                yield return null;
                maxDt = Mathf.Max(maxDt, Time.deltaTime);
                if (!card.IsVisible)
                {
                    if (onAt >= 0f && offAt < 0f)
                        offAt = Time.time;
                    continue;
                }
                if (onAt < 0f)
                    onAt = Time.time - Time.deltaTime;
                if (fullAt < 0f && card.Dissolve <= DissolveEps)
                    fullAt = Time.time;
                if (fullAt >= 0f && fadeAt < 0f && card.Dissolve > DissolveEps)
                    fadeAt = Time.time - Time.deltaTime;
                card.GetCorners(corners);
                maxCover = Mathf.Max(maxCover, ScreenCoverage(cam, corners));
            }
            Debug.Log($"[duzeltme1] kanat: görünür={offAt - onAt:0.000} s, tam={fullAt - onAt:0.000} s, " +
                      $"sönme başı={fadeAt - onAt:0.000} s, ekran kaplama={maxCover:P1}");
            Assert.That(card.LastLifeSec, Is.EqualTo(binding.EntrySilhouetteLifeSec).Within(StTol), "kart ömrü JSON'dan değil");
            Assert.That(card.LastFadeInSec, Is.EqualTo(binding.EntrySilhouetteFadeInSec).Within(StTol), "belirme JSON'dan değil");
            Assert.That(card.LastFadeOutSec, Is.EqualTo(binding.EntrySilhouetteFadeOutSec).Within(StTol), "sönme JSON'dan değil");
            float tol = 2f * maxDt + StTol;
            Assert.That(onAt, Is.GreaterThanOrEqualTo(0f), "kanat görünmedi");
            Assert.That(offAt - onAt, Is.EqualTo(binding.EntrySilhouetteLifeSec).Within(tol), "kanat ömrü");
            Assert.That(fullAt - onAt, Is.EqualTo(binding.EntrySilhouetteFadeInSec).Within(tol), "belirme süresi");
            Assert.That(fadeAt - onAt, Is.EqualTo(binding.EntrySilhouetteLifeSec - binding.EntrySilhouetteFadeOutSec).Within(tol),
                "sönme başlangıcı");
            Assert.That(maxCover, Is.GreaterThan(0f), "kanat ekranda değil");
            Assert.That(maxCover, Is.LessThanOrEqualTo(SilhouetteScreenMaxFrac), $"kanat ekranın {maxCover:P1}'ini kaplıyor");
            yield return WaitUntilIdle();

            // Ekranda en çok 2 siluet: uzayan ömürde de üçüncü kart dolu pencerede doğmaz.
            EjderVfxBatchHost batch = VfxBatch();
            var extras = new List<DragonSilhouetteCardView>();
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    var go = new GameObject($"TestSilhouette{i}");
                    var view = go.AddComponent<DragonSilhouetteCardView>();
                    view.Bind(batch);
                    extras.Add(view);
                }
                yield return null;
                Assert.That(DragonSilhouetteCardView.LiveCount, Is.Zero, "önceki kanat hâlâ canlı");
                Vector3 from = _player.position + Vector3.up;
                Vector3 to = from + Vector3.right * 2f;
                var white = new VfxColorRgb(1f, 1f, 1f, 1f);
                void PlayAll()
                {
                    foreach (DragonSilhouetteCardView v in extras)
                        v.PlayAlongLine(from, to, white, binding.EntrySilhouetteLifeSec, binding.EntrySilhouetteCell,
                            binding.EntrySilhouetteFadeInSec, binding.EntrySilhouetteFadeOutSec);
                }

                PlayAll();
                Assert.That(DragonSilhouetteCardView.LiveCount, Is.EqualTo(VfxPlanDefaults.EjderSiluetEkranMax),
                    "ekranda en çok 2 siluet");
                Assert.That(extras[2].PlayCount, Is.Zero, "üçüncü siluet doğdu");
                float windowEnd = Time.time + binding.EntrySilhouetteLifeSec - 2f * maxDt;
                int maxLive = 0;
                while (Time.time < windowEnd)
                {
                    yield return null;
                    extras[2].PlayAlongLine(from, to, white, binding.EntrySilhouetteLifeSec, binding.EntrySilhouetteCell);
                    maxLive = Mathf.Max(maxLive, DragonSilhouetteCardView.LiveCount);
                }
                Assert.That(maxLive, Is.LessThanOrEqualTo(VfxPlanDefaults.EjderSiluetEkranMax),
                    "uzayan ömürde ekranda 2'den çok siluet");
                Assert.That(extras[2].PlayCount, Is.Zero, "ömür bitmeden üçüncü siluet doğdu");
                float after = Time.time + 4f * maxDt + 0.1f;
                while (Time.time < after)
                    yield return null;
                Assert.That(DragonSilhouetteCardView.LiveCount, Is.Zero, "siluetler 0,7 s sonunda sönmedi");
                extras[2].PlayAlongLine(from, to, white, binding.EntrySilhouetteLifeSec, binding.EntrySilhouetteCell);
                Assert.That(extras[2].PlayCount, Is.EqualTo(1), "yer açılınca siluet doğmadı");
            }
            finally
            {
                foreach (DragonSilhouetteCardView v in extras)
                    Object.Destroy(v.gameObject);
            }
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator Yay_Hareket_RopeOnlyWhenMarked_ElseBackstep()
        {
            FreezeBoss();
            Equip(Yay);
            AnimationBridge bridge = _md.AnimationBridge;
            Assert.That(_catalog.Catalog.TryGetWeapon(Yay, out RuleEngineV4Weapon bow), Is.True);

            _label = "3-3 yay";
            Arrange(Vector3.right);
            Place(_minions[0].transform, Origin + Vector3.right * 4f);
            Physics.SyncTransforms();
            Vector3 start = Flat(_player.position);
            Assert.That(_cast.TryLaunchCombo(3, 3, out _), Is.True, $"{_label} cast edilmedi");
            CommandPlan hopPlan = _cast.LastPlan;
            Assert.That(hopPlan.Commands.Any(c => c is MermiFirlatCommand), Is.False, $"{_label}: ip (mermi) atıldı");
            Assert.That(hopPlan.Commands.OfType<KendiniTasiCommand>().Single().Backstep, Is.True, $"{_label}: geri sıçrama yok");
            Assert.That(bridge.LastClipName, Is.EqualTo(BackstepClip), $"{_label} klibi");
            Assert.That(bridge.LastExactClipFound && bridge.LastPlayApplied, Is.True, $"{_label}: geri sıçrama klibi oynamadı");
            yield return WaitUntilIdle();
            Vector3 moved = Flat(_player.position) - start;
            Debug.Log($"[duzeltme1] {_label}: yer değiştirme={moved} ({moved.magnitude:0.00} m)");
            Assert.That(Vector3.Dot(moved, Vector3.right), Is.LessThan(-1f), $"{_label}: hedeften geri sıçramadı");
            Assert.That(moved.magnitude, Is.LessThanOrEqualTo(bow.BackstepM + PosTolM), $"{_label}: sıçrama mesafesi");

            _label = "3-9 yay";
            yield return CastMarkOnColumn();
            Equip(Yay);
            RuleEngineV4SurfaceMarkHost mark = _surfaceMark;
            Vector3 point = Flat(mark.transform.position);
            Place(_player, ColumnCenter() - AimDir * (ArenaSpec.Columns[0].RadiusM + 5f));
            _player.rotation = Quaternion.LookRotation(AimSide);
            Physics.SyncTransforms();
            float before = Flat(_player.position - point).magnitude;
            Assert.That(_cast.TryLaunchCombo(3, 9, out _), Is.True, $"{_label} cast edilmedi");
            CommandPlan ropePlan = _cast.LastPlan;
            Assert.That(ropePlan.Commands.Any(c => c is MermiFirlatCommand), Is.True, $"{_label}: ip atılmadı");
            Assert.That(ropePlan.Commands.OfType<KendiniTasiCommand>().Single().Backstep, Is.False, $"{_label}: uçuş yok");
            Assert.That(bridge.LastClipName, Is.EqualTo(TaklaClip), $"{_label} klibi");
            yield return WaitUntilIdle();
            float after = Flat(_player.position - point).magnitude;
            Debug.Log($"[duzeltme1] {_label}: işarete uzaklık {before:0.00} → {after:0.00} m");
            Assert.That(before - after, Is.GreaterThan(1f), $"{_label}: işarete uçmadı");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator KorAwaken_NoRootCylinder_NothingInItsPlace()
        {
            FreezeBoss();
            Equip(Kilic);
            Arrange(Vector3.right);
            Place(_minions[0].transform, Origin + Vector3.right * 4f);
            Physics.SyncTransforms();
            UnityEngine.Mesh cylinder = PrimitiveMesh.Get(PrimitiveType.Cylinder);
            var vfx = _player.GetComponentInChildren<RuleDrivenVfxDirector>();
            Assert.That(vfx, Is.Not.Null, "RuleDrivenVfxDirector");
            vfx.BeginSkill(KilicAtilSkill(), VfxPlanDefaults.WeaponKeyKilic);
            Assert.That(_cast.TryLaunchCombo(3, 1, out _), Is.True, "3-1 kılıç cast edilmedi");
            KorAwakenView kor = null;
            float peak = 0f;
            var stray = new HashSet<string>();
            float guard = Time.realtimeSinceStartup + CastTimeoutSec;
            while (_cast.IsRunning && Time.realtimeSinceStartup < guard)
            {
                yield return null;
                kor ??= _player.GetComponentInChildren<KorAwakenView>(true);
                if (kor != null)
                    peak = Mathf.Max(peak, kor.Intensity);
                foreach (Renderer r in _player.GetComponentsInChildren<Renderer>(true))
                {
                    var mf = r.GetComponent<MeshFilter>();
                    bool cyl = mf != null && mf.sharedMesh == cylinder;
                    bool onKor = kor != null && r.gameObject == kor.gameObject;
                    if (cyl || onKor || r.name == "KorAwakenGlow")
                        stray.Add(r.name);
                }
            }
            Assert.That(kor, Is.Not.Null, "KorAwakenView yok");
            Assert.That(peak, Is.GreaterThan(VfxPlanDefaults.KorIdleIntensity), "kor uyanışı çalışmadı");
            Assert.That(stray, Is.Empty, "kor uyanışı karakter kökünde görünür nesne çıkardı: " + string.Join(", ", stray));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        IEnumerator DodgeAndWatch(Action perFrame)
        {
            var dodgeMotion = _player.GetComponent<DodgeMotionController>();
            Assert.That(dodgeMotion, Is.Not.Null, "DodgeMotionController");
            DodgeState dodge = InputController().Dodge;
            dodge.Begin((int)_md.MechanicsClock.Director.WorldTimeMs);
            float guard = Time.realtimeSinceStartup + DodgeGuardSec;
            yield return null;
            while (dodge.IsActive((int)_md.MechanicsClock.Director.WorldTimeMs) && Time.realtimeSinceStartup < guard)
            {
                perFrame();
                yield return null;
            }
            perFrame();
            yield return null;
            perFrame();
            Assert.That(dodge.IsActive((int)_md.MechanicsClock.Director.WorldTimeMs), Is.False, "dodge bitmedi");
        }

        static float LowestVertexY(SkinnedMeshRenderer[] skins, UnityEngine.Mesh baked)
        {
            float min = float.MaxValue;
            var verts = new List<Vector3>();
            foreach (SkinnedMeshRenderer s in skins)
            {
                // useScale: köşeler ölçekli; dünyaya yalnız konum + dönüş kalır.
                s.BakeMesh(baked, true);
                baked.GetVertices(verts);
                Matrix4x4 m = Matrix4x4.TRS(s.transform.position, s.transform.rotation, Vector3.one);
                foreach (Vector3 v in verts)
                    min = Mathf.Min(min, m.MultiplyPoint3x4(v).y);
            }
            return min;
        }

        /// <summary>Oyuncunun altındaki ilk oyuncu dışı katı yüzey (oyuncu kökü kapsül merkezinde durur).</summary>
        static float GroundYUnder(Transform player)
        {
            Vector3 from = player.position + Vector3.up;
            RaycastHit[] hits = Physics.RaycastAll(from, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            float y = 0f;
            foreach (RaycastHit h in hits)
            {
                if (h.collider.transform.IsChildOf(player) || h.distance >= best)
                    continue;
                best = h.distance;
                y = h.point.y;
            }
            return y;
        }

        /// <summary>Dünya dörtgeninin ekran alanı / ekran alanı (kamera arkasındaki köşe = tüm ekran).</summary>
        static float ScreenCoverage(Camera cam, Vector3[] corners)
        {
            var p = new Vector2[corners.Length];
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 s = cam.WorldToScreenPoint(corners[i]);
                if (s.z <= 0f)
                    return 1f;
                p[i] = new Vector2(Mathf.Clamp(s.x, 0f, cam.pixelWidth), Mathf.Clamp(s.y, 0f, cam.pixelHeight));
            }
            float area = 0f;
            for (int i = 0; i < p.Length; i++)
            {
                Vector2 a = p[i];
                Vector2 b = p[(i + 1) % p.Length];
                area += a.x * b.y - b.x * a.y;
            }
            return Mathf.Abs(area) * 0.5f / (cam.pixelWidth * (float)cam.pixelHeight);
        }
    }
}
