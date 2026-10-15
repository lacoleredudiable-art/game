using System.Collections;
using System.Collections.Generic;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Görünen kamera dönüşü: karedeki bütün yazıcılardan sonra kameranın yönü, konumu, oyuncu ve gerçek zaman.
    /// </summary>
    [DefaultExecutionOrder(int.MaxValue)]
    public sealed class CameraTurnProbe : MonoBehaviour
    {
        public struct Sample
        {
            public int Frame;
            public float TimeSec;
            public Quaternion Rot;
            public Vector3 Cam;
            public Vector3 Player;
            public Vector3 ChestViewport;
        }

        public Camera Cam;
        public Transform Player;
        public float ChestUpM;
        public readonly List<Sample> Samples = new();

        void LateUpdate()
        {
            if (Cam == null || Player == null)
                return;
            Vector3 chest = Player.position + Vector3.up * ChestUpM;
            Samples.Add(new Sample
            {
                Frame = Time.frameCount,
                TimeSec = Time.unscaledTime,
                Rot = Cam.transform.rotation,
                Cam = Cam.transform.position,
                Player = Player.position,
                ChestViewport = Cam.WorldToViewportPoint(chest),
            });
        }
    }

    /// <summary>
    /// Boss'un dibinde kamera (D1, Y3): O-5.6 dönüş tavanı (bir saniyede en çok 120°), O-5.7 gövde kenarına 2 m'den
    /// yakınken yaw donar, O-5.9 omuz en çok 1,5 s'de bir değişir, O-5.10 / O-5.11 boss arada kalınca kamera öne
    /// gelmez (oyuncunun göğsüne itilmez), boss modeli %35 opaklığa iner; O-5.15 kamera gövdeye girmez.
    /// Girdi gerçek yoldan (çubuk, kilit düğmesi, elle döndürme açısı); ölçü görünen kameradan ve boss modelinin
    /// çizilen malzemelerinden.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        /// <summary>Yürüme yörüngesini tutan çubuk düzeltmesi (yarıçap sapması başına radyal bileşen, 1/m).</summary>
        const float LapRadialGain = 2f;
        const float LapGuardSec = 20f;
        const float LapSettleSec = 1.5f;
        /// <summary>Dönüş ölçüsünün penceresi: "bir saniyede" (O-5.6 G).</summary>
        const float TurnWindowSec = 1f;
        /// <summary>Kayan pencerede açı toplamının float payı.</summary>
        const float TurnTolDeg = 0.5f;
        const float OpacityTol = 0.01f;
        const float OccluderStandEdgeM = 1f;
        const float OccluderSettleSec = 1.5f;
        const int ShoulderLaps = 2;
        /// <summary>Omuz tarafı bandı: kilit omuz ofsetinin bu kesrinden az yanal sapma taraf değiştirmez.</summary>
        const float ShoulderBandFrac = 0.5f;
        /// <summary>Kamera oyuncuya bu orandan fazla çekilmişse (geri mesafe / istenen) omuz okunmaz.</summary>
        const float ShoulderMinScale = 0.2f;
        const string BossVisualChild = "Visual";
        /// <summary>O-5.7 çıkış testi: dönüş turu ve dışarı yürüyüş eşikleri. Kaynakta yok, test toleransı (tasarım sayısı değil).</summary>
        const float ExitLapMinDeg = 150f;
        const float ExitFreeEdgeM = 6f;
        const float ExitYawExtraTolDeg = 30f;
        const float ExitOffscreenMaxSec = 0.3f;
        const float ExitTailSec = 1.5f;

        float SliceCameraValue(string key) => JsonNumber(SliceCameraJson(), key);

        CameraTurnProbe AddTurnProbe(Camera cam, float chestUpM)
        {
            var probe = new GameObject("CameraTurnProbe").AddComponent<CameraTurnProbe>();
            probe.Cam = cam;
            probe.Player = _player;
            probe.ChestUpM = chestUpM;
            return probe;
        }

        [UnityTest]
        public IEnumerator O_5_6_O_5_7_BossCevresinde2m_TamTur_DonusTavaniAsilmaz_GovdeyeGirmez()
        {
            float maxDps = SliceCameraValue("turn_rate_max_dps");
            float edgeM = SliceCameraValue("boss_yaw_freeze_edge_m");
            float lookM = SliceCameraValue("look_height_m");
            FreezeBoss();
            ParkAll();
            (Camera cam, FollowCameraController follow, _) = GameCamera();
            float minDistM = follow.Tuning.Camera.CameraCollisionMinDistanceM;
            float bossTopY = follow.Tuning.Player.BossVisualHeightM;
            Vector3 bossC = Flat(_boss.position);
            float lapR = BossBodyM + edgeM;
            Vector3 toCenter = BossToCenter();
            Vector3 tangent = Vector3.Cross(Vector3.up, toCenter).normalized;
            MoveInputController move = BeginScenario("boss çevresinde 2 m tur", bossC + toCenter * lapR, tangent, out _, out _);
            yield return new WaitForSecondsRealtime(LapSettleSec);

            CameraTurnProbe probe = AddTurnProbe(cam, lookM);
            float swept = 0f;
            float lastBearing = Bearing(bossC, _player.position);
            float guard = Time.realtimeSinceStartup + LapGuardSec;
            try
            {
                while (Mathf.Abs(swept) < 360f && Time.realtimeSinceStartup < guard)
                {
                    Vector3 radial = Flat(_player.position) - bossC;
                    float r = radial.magnitude;
                    Vector3 outward = r > PosTolM ? radial / r : toCenter;
                    Vector3 along = Vector3.Cross(Vector3.up, outward).normalized;
                    Steer(move, (along + outward * Mathf.Clamp((lapR - r) * LapRadialGain, -1f, 1f)).normalized);
                    yield return null;
                    float bearing = Bearing(bossC, _player.position);
                    swept += Mathf.DeltaAngle(lastBearing, bearing);
                    lastBearing = bearing;
                }
            }
            finally
            {
                move.SetScriptedDirection(null);
            }
            List<CameraTurnProbe.Sample> samples = probe.Samples;
            Object.Destroy(probe.gameObject);

            var fails = new List<string>();
            if (Mathf.Abs(swept) < 360f)
                fails.Add($"tur bitmedi: {LapGuardSec:0} sn'de {swept:0}°");
            JudgeTurnRate(samples, maxDps, fails);
            JudgeOutsideBody(samples, bossC, bossTopY, lookM, minDistM, fails);
            Debug.Log($"[O-5.6] tur {swept:0}°, {samples.Count} kare");
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator O_5_10_O_5_15_BossDibinde_KameraGogseItilmez_GovdeyeGirmez()
        {
            float lookM = SliceCameraValue("look_height_m");
            FreezeBoss();
            ParkAll();
            (Camera cam, FollowCameraController follow, CameraOrbitController orbit) = GameCamera();
            orbit.enabled = false;
            follow.BossTarget = null;
            float minDistM = follow.Tuning.Camera.CameraCollisionMinDistanceM;
            float bossTopY = follow.Tuning.Player.BossVisualHeightM;
            Vector3 bossC = Flat(_boss.position);
            Vector3 toCenter = BossToCenter();
            Place(_player, bossC + toCenter * (BossBodyM + PlayerBodyM));
            _player.rotation = Quaternion.LookRotation(-toCenter);
            Physics.SyncTransforms();
            // Yaw oyuncudan boss'un tersine bakınca kamera boss'un arkasına düşer; tam turda her yönden geçer.
            float intoBossYaw = Mathf.Atan2(toCenter.x, toCenter.z) * Mathf.Rad2Deg;
            follow.OrbitYawDeg = intoBossYaw;
            CameraTurnProbe probe = AddTurnProbe(cam, lookM);
            yield return new WaitForSecondsRealtime(BossFootHoldSec);
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < BossFootSweepSec)
            {
                follow.OrbitYawDeg = intoBossYaw + 360f * (Time.realtimeSinceStartup - start) / BossFootSweepSec;
                yield return null;
            }
            List<CameraTurnProbe.Sample> samples = probe.Samples;
            Object.Destroy(probe.gameObject);

            var fails = new List<string>();
            JudgeOutsideBody(samples, bossC, bossTopY, lookM, minDistM, fails);
            Assert.That(samples.Count, Is.GreaterThan(0), "kamera sondası kare görmedi");
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator O_5_11_BossArada_KameraOneGelmez_Opaklik035()
        {
            float opacity = SliceCameraValue("boss_occluded_opacity");
            float distM = SliceCameraValue("distance_m");
            float lookM = SliceCameraValue("look_height_m");
            FreezeBoss();
            ParkAll();
            (Camera cam, FollowCameraController follow, CameraOrbitController orbit) = GameCamera();
            orbit.enabled = false;
            follow.BossTarget = null;
            Vector3 bossC = Flat(_boss.position);
            Vector3 toCenter = BossToCenter();
            Place(_player, bossC + toCenter * (BossBodyM + OccluderStandEdgeM));
            _player.rotation = Quaternion.LookRotation(toCenter);
            Physics.SyncTransforms();
            Renderer[] model = BossModelRenderers();
            Assert.That(model.Length, Is.GreaterThan(0), "boss modelinde çizilen renderer yok");

            float intoBossYaw = Mathf.Atan2(toCenter.x, toCenter.z) * Mathf.Rad2Deg;
            follow.OrbitYawDeg = intoBossYaw;
            yield return new WaitForSecondsRealtime(OccluderSettleSec);
            float behind = ModelOpacity(model, out string behindWhat);
            float behindDist = (cam.transform.position - (_player.position + Vector3.up * lookM)).magnitude;

            follow.OrbitYawDeg = intoBossYaw + 180f;
            yield return new WaitForSecondsRealtime(OccluderSettleSec);
            float clear = ModelOpacity(model, out string clearWhat);

            var fails = new List<string>();
            if (Mathf.Abs(behind - opacity) > OpacityTol)
                fails.Add($"O-5.11 boss arada: model opaklığı {behind:0.00}, beklenen {opacity:0.00} ({behindWhat})");
            if (behindDist < distM - CameraTolM)
                fails.Add($"O-5.11 boss arada: kamera göğüsten {behindDist:0.00} m, öne geldi (uzaklık {distM:0.0} m)");
            if (Mathf.Abs(clear - 1f) > OpacityTol)
                fails.Add($"O-5.11 boss arada değil: model opaklığı {clear:0.00}, beklenen 1 ({clearWhat})");
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator O_5_9_KilitliTur_OmuzEnCok1_5sndeBirDegisir()
        {
            float minGapSec = SliceCameraValue("shoulder_switch_min_sec");
            float edgeM = SliceCameraValue("boss_yaw_freeze_edge_m");
            FreezeBoss();
            ParkAll();
            (Camera cam, FollowCameraController follow, _) = GameCamera();
            Vector3 bossC = Flat(_boss.position);
            float lapR = BossBodyM + edgeM;
            Vector3 toCenter = BossToCenter();
            Vector3 tangent = Vector3.Cross(Vector3.up, toCenter).normalized;
            MoveInputController move = BeginScenario("kilitli tur", bossC + toCenter * lapR, tangent, out _, out _);
            var view = Object.FindAnyObjectByType<HexagonView>();
            Assert.That(view != null && view.LockOnButton != null, "kilit düğmesi yok");
            view.LockOnButton.onClick.Invoke();
            Assert.That(follow.LockOnActive, Is.True, "kilit düğmesi kilidi açmadı");
            yield return new WaitForSecondsRealtime(LapSettleSec);

            float baseX = follow.Tuning.Camera.CameraShoulderOffset.x;
            float bandM = follow.Tuning.Camera.CameraLockOnShoulderSideM * ShoulderBandFrac;
            var flips = new List<float>();
            float sign = 0f;
            float swept = 0f;
            float lastBearing = Bearing(bossC, _player.position);
            float guard = Time.realtimeSinceStartup + LapGuardSec * ShoulderLaps;
            try
            {
                while (Mathf.Abs(swept) < 360f * ShoulderLaps && Time.realtimeSinceStartup < guard)
                {
                    Vector3 radial = Flat(_player.position) - bossC;
                    float r = radial.magnitude;
                    Vector3 outward = r > PosTolM ? radial / r : toCenter;
                    // İkinci turda yön değişir: omuz seçimi iki yönde de sınanır.
                    float turn = Mathf.Abs(swept) < 360f ? 1f : -1f;
                    Vector3 along = Vector3.Cross(Vector3.up, outward).normalized * turn;
                    Steer(move, (along + outward * Mathf.Clamp((lapR - r) * LapRadialGain, -1f, 1f)).normalized);
                    yield return null;
                    float bearing = Bearing(bossC, _player.position);
                    swept += Mathf.Abs(Mathf.DeltaAngle(lastBearing, bearing));
                    lastBearing = bearing;
                    // Omuz: kameranın yaw çerçevesinde oyuncuya göre yanal konumu. Çarpışma kamerayı ışın boyunca
                    // çektiğinde yanal ofset de küçülür: geri mesafeyle ölçeklenir. Bant, takip gecikmesini değişim saymaz.
                    Quaternion yawRot = Quaternion.Euler(0f, follow.MovementYawDeg, 0f);
                    Vector3 d = cam.transform.position - _player.position;
                    float scale = -Vector3.Dot(d, yawRot * Vector3.forward) / follow.ResolvedDistanceM;
                    float side = scale > ShoulderMinScale ? Vector3.Dot(d, yawRot * Vector3.right) / scale - baseX : 0f;
                    float s = side > bandM ? 1f : side < -bandM ? -1f : sign;
                    if (sign != 0f && s != sign)
                        flips.Add(Time.unscaledTime);
                    sign = s;
                }
            }
            finally
            {
                move.SetScriptedDirection(null);
                follow.LockOnActive = false;
            }

            float minGap = float.MaxValue;
            for (int i = 1; i < flips.Count; i++)
                minGap = Mathf.Min(minGap, flips[i] - flips[i - 1]);
            Debug.Log($"[O-5.9] kilitli {swept:0}° tur, {flips.Count} omuz değişimi, en kısa ara " +
                      $"{(flips.Count > 1 ? minGap.ToString("0.00") : "-")} sn");
            Assert.That(swept, Is.GreaterThanOrEqualTo(360f * ShoulderLaps),
                $"kilitli tur bitmedi: {LapGuardSec * ShoulderLaps:0} sn'de {swept:0}°");
            Assert.That(flips.Count < 2 || minGap >= minGapSec, Is.True,
                $"O-5.9 omuz {flips.Count} kez değişti, en kısa ara {minGap:0.00} sn < {minGapSec:0.0} sn");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        /// <summary>
        /// O-5.7 (yumuşak geçiş): oyuncu 2 m'de boss çevresinde dönüp 5 m'nin ötesine yürüyünce tutulan yaw farkı tek
        /// karede silinmez; kamera boss yönünden fazla dönmez, oyuncu kadrajdan uzun süre çıkmaz. Eşikler test toleransı.
        /// </summary>
        [UnityTest]
        public IEnumerator O_5_7_Boss2mdenCikis_YawSicramaz()
        {
            float edgeM = SliceCameraValue("boss_yaw_freeze_edge_m");
            float lookM = SliceCameraValue("look_height_m");
            FreezeBoss();
            ParkAll();
            (Camera cam, _, _) = GameCamera();
            Vector3 bossC = Flat(_boss.position);
            float lapR = BossBodyM + edgeM;
            Vector3 toCenter = BossToCenter();
            Vector3 tangent = Vector3.Cross(Vector3.up, toCenter).normalized;
            MoveInputController move = BeginScenario("boss 2 m tur + çıkış", bossC + toCenter * lapR, tangent, out _, out _);
            yield return new WaitForSecondsRealtime(LapSettleSec);

            CameraTurnProbe probe = AddTurnProbe(cam, lookM);
            float swept = 0f;
            float lastBearing = Bearing(bossC, _player.position);
            int exitStart = -1;
            float guard = Time.realtimeSinceStartup + LapGuardSec;
            float tailEnd = 0f;
            try
            {
                while (Time.realtimeSinceStartup < guard && (tailEnd == 0f || Time.realtimeSinceStartup < tailEnd))
                {
                    Vector3 radial = Flat(_player.position) - bossC;
                    float r = radial.magnitude;
                    Vector3 outward = r > PosTolM ? radial / r : toCenter;
                    if (Mathf.Abs(swept) < ExitLapMinDeg)
                    {
                        Vector3 along = Vector3.Cross(Vector3.up, outward).normalized;
                        Steer(move, (along + outward * Mathf.Clamp((lapR - r) * LapRadialGain, -1f, 1f)).normalized);
                    }
                    else
                    {
                        if (exitStart < 0)
                            exitStart = probe.Samples.Count;
                        Steer(move, outward);
                        if (tailEnd == 0f && r - BossBodyM > ExitFreeEdgeM)
                            tailEnd = Time.realtimeSinceStartup + ExitTailSec;
                    }
                    yield return null;
                    float bearing = Bearing(bossC, _player.position);
                    swept += Mathf.DeltaAngle(lastBearing, bearing);
                    lastBearing = bearing;
                }
            }
            finally
            {
                move.SetScriptedDirection(null);
            }
            List<CameraTurnProbe.Sample> samples = probe.Samples;
            Object.Destroy(probe.gameObject);

            var fails = new List<string>();
            if (Mathf.Abs(swept) < ExitLapMinDeg || exitStart < 0 || tailEnd == 0f)
                fails.Add($"çıkış senaryosu bitmedi: tur {swept:0}°, çıkış başladı mı {exitStart >= 0}");
            else
            {
                float camYawDelta = 0f;
                float bearingDelta = 0f;
                float offscreenSec = 0f;
                for (int i = Mathf.Max(1, exitStart); i < samples.Count; i++)
                {
                    CameraTurnProbe.Sample a = samples[i - 1];
                    CameraTurnProbe.Sample b = samples[i];
                    camYawDelta += Mathf.DeltaAngle(a.Rot.eulerAngles.y, b.Rot.eulerAngles.y);
                    bearingDelta += Mathf.DeltaAngle(Bearing(bossC, a.Player), Bearing(bossC, b.Player));
                    Vector3 v = b.ChestViewport;
                    if (v.z < 0f || v.x < 0f || v.x > 1f)
                        offscreenSec += b.TimeSec - a.TimeSec;
                }
                if (Mathf.Abs(camYawDelta - bearingDelta) > ExitYawExtraTolDeg)
                    fails.Add($"çıkışta kamera yaw'ı boss yönünden {Mathf.Abs(camYawDelta - bearingDelta):0}° fazla döndü " +
                              $"(kamera {camYawDelta:0}°, boss yönü {bearingDelta:0}°; sınır {ExitYawExtraTolDeg:0}°)");
                if (offscreenSec > ExitOffscreenMaxSec)
                    fails.Add($"çıkışta oyuncu {offscreenSec:0.00} sn kadraj dışında (sınır {ExitOffscreenMaxSec:0.0} sn)");
                Debug.Log($"[O-5.7] çıkış: kamera {camYawDelta:0}°, boss yönü {bearingDelta:0}°, kadraj dışı {offscreenSec:0.00} sn");
            }
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        static float Bearing(Vector3 center, Vector3 p) => Mathf.Atan2(p.x - center.x, p.z - center.z) * Mathf.Rad2Deg;

        /// <summary>
        /// O-5.6: her karede bitmek üzere son bir saniyedeki dönüş toplamı tavanı aşmaz. Pencere başı, pencereye giren ilk
        /// karenin önceki karesidir (o karedeki dönüş de saniyenin içindedir).
        /// </summary>
        static void JudgeTurnRate(List<CameraTurnProbe.Sample> samples, float maxDps, List<string> fails)
        {
            var angles = new float[samples.Count];
            float maxFrameDps = 0f;
            for (int i = 1; i < samples.Count; i++)
            {
                angles[i] = Quaternion.Angle(samples[i - 1].Rot, samples[i].Rot);
                float dt = samples[i].TimeSec - samples[i - 1].TimeSec;
                if (dt > 0f)
                    maxFrameDps = Mathf.Max(maxFrameDps, angles[i] / dt);
            }
            float worst = 0f;
            int worstAt = -1;
            int head = 1;
            float sum = 0f;
            for (int i = 1; i < samples.Count; i++)
            {
                sum += angles[i];
                while (head < i && samples[i].TimeSec - samples[head - 1].TimeSec > TurnWindowSec)
                    sum -= angles[head++];
                if (sum > worst)
                {
                    worst = sum;
                    worstAt = i;
                }
            }
            Debug.Log($"[O-5.6] en kötü saniye {worst:0.0}°, en hızlı kare {maxFrameDps:0} °/s");
            if (worst > maxDps + TurnTolDeg)
                fails.Add($"O-5.6 kamera bir saniyede {worst:0.0}° döndü > {maxDps:0}° " +
                          $"(kare {samples[worstAt].Frame}, en hızlı kare {maxFrameDps:0} °/s)");
        }

        /// <summary>
        /// O-5.15: kamera boss gövdesinin (yarıçap, tepe) içinde değil. O-5.10 / Y3: kamera oyuncunun göğsüne en yakın
        /// mesafeden (1,2 m) fazla yaklaşmaz.
        /// </summary>
        void JudgeOutsideBody(List<CameraTurnProbe.Sample> samples, Vector3 bossC, float bossTopY, float chestUpM,
            float minDistM, List<string> fails)
        {
            int inside = 0;
            int close = 0;
            float nearest = float.MaxValue;
            string nearestAt = "-";
            string insideAt = "-";
            foreach (CameraTurnProbe.Sample s in samples)
            {
                float r = Flat(s.Cam - bossC).magnitude;
                if (r < BossBodyM && s.Cam.y < bossTopY && inside++ == 0)
                    insideAt = $"kare {s.Frame}: boss ekseninden {r:0.00} m";
                float d = (s.Cam - (s.Player + Vector3.up * chestUpM)).magnitude;
                if (d < minDistM - CameraTolM)
                    close++;
                if (d < nearest)
                {
                    nearest = d;
                    nearestAt = $"kare {s.Frame}";
                }
            }
            if (inside > 0)
                fails.Add($"O-5.15 kamera {samples.Count} karenin {inside}'inde boss gövdesinde; ilk {insideAt}");
            if (close > 0)
                fails.Add($"Y3 / O-5.10 kamera {samples.Count} karenin {close}'inde oyuncunun göğsüne {minDistM:0.0} m'den " +
                          $"yakın; en yakın {nearest:0.00} m ({nearestAt})");
        }

        /// <summary>Boss modeli: kökün çizilen gövdesi (model yoksa kapsül) ve <c>Visual</c> altındaki mesh'ler.</summary>
        Renderer[] BossModelRenderers()
        {
            var list = new List<Renderer>();
            Renderer own = _boss.GetComponent<Renderer>();
            if (own != null && own.enabled)
                list.Add(own);
            Transform visual = _boss.Find(BossVisualChild);
            if (visual == null)
                return list.ToArray();
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>())
            {
                if (r.enabled && (r is MeshRenderer || r is SkinnedMeshRenderer))
                    list.Add(r);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Çizilen opaklık: saydam kuyrukta olmayan malzeme opaktır (alfa çizime girmez); saydam malzemede ana renk alfası.
        /// Modelin en opak malzemesi döner.
        /// </summary>
        static float ModelOpacity(Renderer[] model, out string what)
        {
            float most = 0f;
            what = "-";
            foreach (Renderer r in model)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null)
                        continue;
                    bool transparent = m.renderQueue >= (int)RenderQueue.Transparent;
                    float a = !transparent ? 1f : m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").a : m.color.a;
                    if (a >= most)
                    {
                        most = a;
                        what = $"{r.name}/{m.name} kuyruk {m.renderQueue}";
                    }
                }
            }
            return most;
        }
    }
}
