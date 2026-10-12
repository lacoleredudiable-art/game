using System.Collections;
using System.Collections.Generic;
using Dovus.Core.Shared;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Config.Sections;
using Dovus.Game.Skills.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Görünen kamera: karedeki bütün yazıcılardan sonra (en son LateUpdate) kameranın konumu, FOV'u, verilen
    /// noktanın ekrandaki yeri ve boss konumu.
    /// </summary>
    [DefaultExecutionOrder(int.MaxValue)]
    public sealed class CameraRenderedProbe : MonoBehaviour
    {
        public struct Sample
        {
            public int Frame;
            public Vector3 Cam;
            public float Fov;
            public Vector3 AimViewport;
            public Vector3 Boss;
        }

        public Camera Cam;
        public Transform Player;
        public Transform Boss;
        public float AimUpM;
        public readonly List<Sample> Samples = new();

        public Sample Last => Samples.Count > 0 ? Samples[Samples.Count - 1] : default;

        void LateUpdate()
        {
            if (Cam == null || Player == null || Boss == null)
                return;
            Samples.Add(new Sample
            {
                Frame = Time.frameCount,
                Cam = Cam.transform.position,
                Fov = Cam.fieldOfView,
                AimViewport = Cam.WorldToViewportPoint(Player.position + Vector3.up * AimUpM),
                Boss = Boss.position,
            });
        }
    }

    /// <summary>Update evresinin sonunda boss konumu (konum sahibi Update'te yazar).</summary>
    [DefaultExecutionOrder(int.MaxValue)]
    public sealed class BossUpdateEndProbe : MonoBehaviour
    {
        public Transform Boss;
        public readonly Dictionary<int, Vector3> ByFrame = new();

        void Update()
        {
            if (Boss != null)
                ByFrame[Time.frameCount] = Boss.position;
        }
    }

    /// <summary>
    /// Kamera ölçüleri tek kaynak (O-5.1–O-5.4; 10-olculer §18 Ç-2, Ç-3, Ç-6): dilimde mesafe, eğim, bakış yüksekliği
    /// ve FOV kural-motoru-v4.json <c>slice_camera</c>'dan; sahne ve CameraSettings varsayılanı okunmaz.
    /// Kilit mesafesi boss gövdesinin kenarından (O-5.8); kamera boss gövdesine girmez (O-5.15); boss konumunu
    /// karede tek yazıcı yazar (kural 5). Ölçü görünen kameradan (<see cref="CameraRenderedProbe"/>).
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        /// <summary>Prototype.unity GameBootstrapHost'unda serileşmiş kamera değerleri (okunmamaları gerekir).</summary>
        const float PrototypeSceneCameraDistanceM = 3.4f;
        const float PrototypeSceneCameraLookHeightM = 0.35f;
        const float PrototypeSceneCameraFovDeg = 56f;
        const float CameraTolM = 0.05f;
        const float CameraFovTolDeg = 0.05f;
        const float CameraPitchTolDeg = 0.05f;
        const float CameraViewportTol = 0.01f;
        /// <summary>Kilit açıkken oyuncu boss merkezinden bu kadar uzakta (gövde kenarından 7,5 m).</summary>
        const float LockOnStandFromCenterM = 10f;
        static readonly Vector3 LockOnStandDir = new Vector3(-2f, 0f, -1f).normalized;
        const float LockOnSettleSec = 3f;
        /// <summary>Boss dibinde kameranın boss yönüne bakıp beklediği süre ve ardından tam turun süresi.</summary>
        const float BossFootHoldSec = 2f;
        const float BossFootSweepSec = 6f;
        const int BossWriterFrames = 60;
        const float BossWriterTolM = 0.001f;

        JsonValue SliceCameraJson()
        {
            var text = Resources.Load<TextAsset>(RuleEngineV4CatalogAccess.JsonResource);
            Assert.That(text, Is.Not.Null, "kural-motoru-v4.json");
            JsonValue node = MiniJson.Parse(text.text)["slice_camera"];
            Assert.That(node.Kind, Is.EqualTo(JsonKind.Object), "kural-motoru-v4.json slice_camera yok");
            return node;
        }

        static float JsonNumber(JsonValue node, string key)
        {
            JsonValue v = node[key];
            Assert.That(v.Kind, Is.EqualTo(JsonKind.Number), $"slice_camera.{key} yok");
            return v.AsFloat();
        }

        (Camera cam, FollowCameraController follow, CameraOrbitController orbit) GameCamera()
        {
            Camera cam = Camera.main;
            Assert.That(cam, Is.Not.Null, "Camera.main");
            var follow = cam.GetComponent<FollowCameraController>();
            Assert.That(follow, Is.Not.Null, "FollowCameraController");
            var orbit = Object.FindAnyObjectByType<CameraOrbitController>();
            Assert.That(orbit, Is.Not.Null, "CameraOrbitController");
            return (cam, follow, orbit);
        }

        CameraRenderedProbe AddCameraProbe(Camera cam, float aimUpM)
        {
            var probe = new GameObject("CameraRenderedProbe").AddComponent<CameraRenderedProbe>();
            probe.Cam = cam;
            probe.Player = _player;
            probe.Boss = _boss;
            probe.AimUpM = aimUpM;
            return probe;
        }

        [UnityTest]
        public IEnumerator O_5_1_O_5_4_KameraOlculeri_TekKaynaktan()
        {
            JsonValue spec = SliceCameraJson();
            float distM = JsonNumber(spec, "distance_m");
            float pitchDeg = JsonNumber(spec, "pitch_deg");
            float lookM = JsonNumber(spec, "look_height_m");
            float fovDeg = JsonNumber(spec, "fov_deg");

            FreezeBoss();
            ParkAll();
            (Camera cam, FollowCameraController follow, _) = GameCamera();
            // Yumuşak kilitte yaw boss'a çekilir; ölçü düz arkadan olsun.
            follow.BossTarget = null;
            Place(_player, Origin);
            _player.rotation = Quaternion.identity;
            Physics.SyncTransforms();
            // Canlı ayarlar sahnedeki değerlere çekilir: dilim yine JSON'u göstermeli.
            follow.Tuning.Camera.CameraDistanceM = PrototypeSceneCameraDistanceM;
            follow.Tuning.Camera.CameraLookHeightM = PrototypeSceneCameraLookHeightM;
            follow.Tuning.Camera.CameraFovDeg = PrototypeSceneCameraFovDeg;
            CameraRenderedProbe probe = AddCameraProbe(cam, lookM);
            yield return new WaitForSecondsRealtime(CameraSettleSec);
            CameraRenderedProbe.Sample s = probe.Last;
            Object.Destroy(probe.gameObject);

            Vector3 expected = _player.position + Quaternion.Euler(follow.OrbitPitchDeg, follow.MovementYawDeg, 0f)
                * (follow.Tuning.Camera.CameraShoulderOffset + Vector3.back * distM);
            var fails = new List<string>();
            if (Mathf.Abs(s.Fov - fovDeg) > CameraFovTolDeg)
                fails.Add($"O-5.4 FOV {s.Fov:0.00}°, beklenen {fovDeg:0.00}°");
            if (Mathf.Abs(follow.OrbitPitchDeg - pitchDeg) > CameraPitchTolDeg)
                fails.Add($"O-5.2 eğim {follow.OrbitPitchDeg:0.00}°, beklenen {pitchDeg:0.00}°");
            if ((s.Cam - expected).magnitude > CameraTolM)
                fails.Add($"O-5.1 kamera {s.Cam}, {distM:0.0} m mesafede beklenen {expected} " +
                          $"(fark {(s.Cam - expected).magnitude:0.000} m)");
            Vector2 aim = new(s.AimViewport.x, s.AimViewport.y);
            if ((aim - new Vector2(0.5f, 0.5f)).magnitude > CameraViewportTol || s.AimViewport.z <= 0f)
                fails.Add($"O-5.3 oyuncunun {lookM:0.0} m yukarısı ekranda {s.AimViewport}, beklenen orta");
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator O_5_8_KilitMesafesi_BossGovdeKenarindan()
        {
            float distM = JsonNumber(SliceCameraJson(), "distance_m");
            FreezeBoss();
            ParkAll();
            (Camera cam, FollowCameraController follow, _) = GameCamera();
            Vector3 bossC = Flat(_boss.position);
            Place(_player, bossC + LockOnStandDir * LockOnStandFromCenterM);
            _player.rotation = Quaternion.LookRotation(-LockOnStandDir);
            Physics.SyncTransforms();
            var view = Object.FindAnyObjectByType<HexagonView>();
            Assert.That(view != null && view.LockOnButton != null, "kilit düğmesi yok");
            view.LockOnButton.onClick.Invoke();
            Assert.That(follow.LockOnActive, Is.True, "kilit düğmesi kilidi açmadı");
            CameraRenderedProbe probe = AddCameraProbe(cam, 0f);
            yield return new WaitForSecondsRealtime(LockOnSettleSec);
            CameraRenderedProbe.Sample s = probe.Last;
            Object.Destroy(probe.gameObject);

            CameraSettings c = follow.Tuning.Camera;
            float centerSep = Flat(_player.position - s.Boss).magnitude;
            float edgeSep = centerSep - BossBodyM;
            float Expected(float sep) => Mathf.Clamp(
                distM + Mathf.Min(sep * c.CameraLockOnDistancePerSepM, c.CameraLockOnMaxExtraDistanceM),
                c.CameraLockOnMinDistanceM,
                c.CameraLockOnMaxDistanceM);
            Vector3 back = Quaternion.Euler(follow.OrbitPitchDeg, follow.MovementYawDeg, 0f) * Vector3.back;
            float measured = Vector3.Dot(s.Cam - _player.position, back) + c.CameraShoulderOffset.z;
            Assert.That(Mathf.Abs(measured - Expected(edgeSep)), Is.LessThanOrEqualTo(CameraTolM),
                $"O-5.8 kilit mesafesi {measured:0.000} m; gövde kenarından ({edgeSep:0.00} m) beklenen " +
                $"{Expected(edgeSep):0.000} m, merkezden ({centerSep:0.00} m) olsaydı {Expected(centerSep):0.000} m");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator O_5_15_OyuncuBossDibinde_KameraGovdeyeGirmez_HerKare()
        {
            FreezeBoss();
            ParkAll();
            (Camera cam, FollowCameraController follow, CameraOrbitController orbit) = GameCamera();
            orbit.enabled = false;
            follow.BossTarget = null;
            Vector3 bossC = Flat(_boss.position);
            Vector3 toCenter = BossToCenter();
            Place(_player, bossC + toCenter * (BossBodyM + PlayerBodyM));
            _player.rotation = Quaternion.LookRotation(-toCenter);
            Physics.SyncTransforms();
            // Kamera oyuncunun arkasında: yaw oyuncudan boss'un tersine bakınca kamera boss'un içine düşer.
            float intoBossYaw = Mathf.Atan2(toCenter.x, toCenter.z) * Mathf.Rad2Deg;
            float bossTopY = follow.Tuning.Player.BossVisualHeightM;
            follow.OrbitYawDeg = intoBossYaw;
            CameraRenderedProbe probe = AddCameraProbe(cam, 0f);
            yield return new WaitForSecondsRealtime(BossFootHoldSec);
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < BossFootSweepSec)
            {
                follow.OrbitYawDeg = intoBossYaw + 360f * (Time.realtimeSinceStartup - start) / BossFootSweepSec;
                yield return null;
            }
            List<CameraRenderedProbe.Sample> samples = probe.Samples;
            Object.Destroy(probe.gameObject);

            int inside = 0;
            float worstPen = 0f;
            string worst = "-";
            foreach (CameraRenderedProbe.Sample s in samples)
            {
                float r = Flat(s.Cam - s.Boss).magnitude;
                if (r >= BossBodyM || s.Cam.y >= bossTopY)
                    continue;
                inside++;
                if (BossBodyM - r > worstPen)
                {
                    worstPen = BossBodyM - r;
                    worst = $"kare {s.Frame}: kamera {s.Cam}, boss ekseninden {r:0.00} m";
                }
            }
            Assert.That(samples.Count, Is.GreaterThan(0), "kamera sondası kare görmedi");
            Assert.That(inside, Is.Zero,
                $"O-5.15 kamera {samples.Count} karenin {inside}'inde boss gövdesinin (yarıçap {BossBodyM:0.0} m, " +
                $"tepe {bossTopY:0.0} m) içinde; en derin {worstPen:0.00} m, {worst}");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator Kural5_DondurulmusBoss_UpdateSonrasiIkinciYaziciYok()
        {
            FreezeBoss();
            ParkAll();
            (Camera cam, _, _) = GameCamera();
            var update = new GameObject("BossUpdateEndProbe").AddComponent<BossUpdateEndProbe>();
            update.Boss = _boss;
            CameraRenderedProbe rendered = AddCameraProbe(cam, 0f);
            for (int i = 0; i < BossWriterFrames; i++)
                yield return null;
            Object.Destroy(update.gameObject);
            Object.Destroy(rendered.gameObject);

            int moved = 0;
            string first = "-";
            foreach (CameraRenderedProbe.Sample s in rendered.Samples)
            {
                if (!update.ByFrame.TryGetValue(s.Frame, out Vector3 afterUpdate))
                    continue;
                float d = Flat(s.Boss - afterUpdate).magnitude;
                if (d <= BossWriterTolM)
                    continue;
                if (moved++ == 0)
                    first = $"kare {s.Frame}: Update sonu {afterUpdate}, çizilen {s.Boss} ({d:0.000} m)";
            }
            Assert.That(rendered.Samples.Count, Is.GreaterThan(0), "kamera sondası kare görmedi");
            Assert.That(moved, Is.Zero,
                $"boss konumu {rendered.Samples.Count} karenin {moved}'inde LateUpdate'te ikinci kez yazıldı " +
                $"(kamera boss'u LateUpdate'te okur); ilk: {first}");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }
    }
}
