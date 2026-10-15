using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// O-7.3 / O-7.7: oyuncu duvar dibindeyken kamera 8 yöne ve en alçak, varsayılan, en yüksek eğime döner;
    /// her görüntüde görsel duvar görünür.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const string WallVisualName = "OuterWallVisual";
        const int WallShotDirections = 8;
        const int WallShotW = 640;
        const int WallShotH = 360;
        /// <summary>Duvar açık/kapalı iki çekimde bu kadar farklı piksel yoksa duvar görünmüyor sayılır.</summary>
        const float WallVisibleMinFrac = 0.02f;
        /// <summary>RGB farkı toplamı bunu aşan piksel "farklı" sayılır (yaklaşık 5/255 kanal başına).</summary>
        const float PixelDiffMin = 0.06f;

        [UnityTest]
        public IEnumerator O_7_7_DuvarDibi_SekizYon_DuvarGorunur()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            Camera cam = Camera.main;
            Assert.That(cam, Is.Not.Null, "Camera.main");
            var follow = cam.GetComponent<FollowCameraController>();
            Assert.That(follow, Is.Not.Null, "FollowCameraController");
            var orbit = Object.FindAnyObjectByType<CameraOrbitController>();
            if (orbit != null)
                orbit.enabled = false;
            // Boss yumuşak kilit menzilindeyken yaw boss'a çekilir; kamera 8 yöne dönsün diye çekimde boss hedefi yok.
            Transform bossTarget = follow.BossTarget;
            follow.BossTarget = null;
            Renderer[] wall = GameObject.Find(ArenaRoot).transform.Find(WallVisualName)?.GetComponentsInChildren<Renderer>(true)
                ?? Array.Empty<Renderer>();
            Assert.That(wall, Is.Not.Empty, "görsel duvarda renderer yok");

            var tuningCam = follow.Tuning.Camera;
            float[] pitches = { tuningCam.CameraPitchMinDeg, 0f, tuningCam.CameraPitchMaxDeg };
            var motor = _player.GetComponent<KinematicMotorController>();
            Assert.That(motor, Is.Not.Null, "KinematicMotorController");
            Vector3 stand = Outward * (VisibleInnerFaceM - motor.BodyRadiusM);
            var fails = new List<string>();
            try
            {
                foreach (float pitch in pitches)
                for (int i = 0; i < WallShotDirections; i++)
                {
                    float yaw = i * 360f / WallShotDirections;
                    Place(_player, stand);
                    _player.rotation = Quaternion.Euler(0f, yaw, 0f);
                    Physics.SyncTransforms();
                    follow.OrbitYawDeg = yaw;
                    follow.OrbitPitchDeg = pitch;
                    yield return new WaitForSecondsRealtime(CameraSettleSec);

                    Texture2D on = RenderGameCamera(cam);
                    foreach (Renderer r in wall)
                        r.enabled = false;
                    Texture2D off = RenderGameCamera(cam);
                    foreach (Renderer r in wall)
                        r.enabled = true;
                    float frac = DiffFrac(on, off);
                    string file = $"duvar_dibi_egim{pitch:+0;-0;0}_yon{yaw:000}.png";
                    SaveShot(on, file);
                    Object.Destroy(on);
                    Object.Destroy(off);
                    Debug.Log($"[O-7.7] {file}: kamera {cam.transform.position}, oyuncudan {Flat(cam.transform.position - _player.position).magnitude:0.00} m, "
                        + $"duvarın kapladığı ekran %{frac * 100f:0.0}, kamera mesafesi {follow.ResolvedDistanceM:0.00} m "
                        + $"(çarpışma çekişi {follow.CollisionPulledInM:0.00} m: {CameraBlockers(follow)})");
                    if (frac < WallVisibleMinFrac)
                        fails.Add($"{file}: duvar ekranın yalnız %{frac * 100f:0.0}'ini kaplıyor (en az %{WallVisibleMinFrac * 100f:0})");
                }
            }
            finally
            {
                follow.OrbitPitchDeg = 0f;
                follow.BossTarget = bossTarget;
                if (orbit != null)
                    orbit.enabled = true;
            }

            Assert.That(fails, Is.Empty, string.Join("\n", fails));
        }

        /// <summary>Takip kamerasının istediği konuma giden küre taramasına çarpan collider'lar (tanı).</summary>
        string CameraBlockers(FollowCameraController follow)
        {
            var cam = follow.Tuning.Camera;
            Vector3 pivot = _player.position + Vector3.up * cam.CameraLookHeightM;
            Vector3 offset = Quaternion.Euler(follow.OrbitPitchDeg, follow.OrbitYawDeg, 0f)
                * (cam.CameraShoulderOffset + Vector3.back * cam.CameraDistanceM);
            Vector3 desired = _player.position + offset;
            Vector3 d = desired - pivot;
            RaycastHit[] hits = Physics.SphereCastAll(pivot, cam.CameraCollisionSphereRadiusM, d.normalized, d.magnitude, ~0,
                QueryTriggerInteraction.Ignore);
            var names = hits.Where(h => !h.collider.transform.IsChildOf(_player))
                .OrderBy(h => h.distance)
                .Select(h => $"{h.collider.name}@{h.distance:0.00}")
                .Take(4)
                .ToList();
            return names.Count == 0 ? "yok" : string.Join(", ", names);
        }

        Texture2D RenderGameCamera(Camera cam)
        {
            var rt = new RenderTexture(WallShotW, WallShotH, 24);
            RenderTexture prevTarget = cam.targetTexture;
            int errorsBefore = _errors.Count;
            LogAssert.ignoreFailingMessages = true;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevTarget;
            // Elle Render batchmode'da paket shader içe aktarım hatası basabilir; yakalama oyun hatası sayılmaz.
            if (_errors.Count > errorsBefore)
            {
                Debug.LogWarning("duvar dibi yakalaması sırasında: " + string.Join("\n", _errors.Skip(errorsBefore)));
                _errors.RemoveRange(errorsBefore, _errors.Count - errorsBefore);
            }
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(WallShotW, WallShotH, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, WallShotW, WallShotH), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            Object.Destroy(rt);
            return tex;
        }

        static float DiffFrac(Texture2D a, Texture2D b)
        {
            Color32[] pa = a.GetPixels32();
            Color32[] pb = b.GetPixels32();
            int diff = 0;
            for (int i = 0; i < pa.Length; i++)
            {
                int d = Math.Abs(pa[i].r - pb[i].r) + Math.Abs(pa[i].g - pb[i].g) + Math.Abs(pa[i].b - pb[i].b);
                if (d > PixelDiffMin * 255f)
                    diff++;
            }
            return diff / (float)pa.Length;
        }

        static void SaveShot(Texture2D tex, string file)
        {
            string dir = SessionCaptureDir();
            if (dir == null)
            {
                string[] args = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(args, CaptureArg);
                dir = i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
            }
            if (dir == null)
                return;
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, file), tex.EncodeToPNG());
        }
    }
}
