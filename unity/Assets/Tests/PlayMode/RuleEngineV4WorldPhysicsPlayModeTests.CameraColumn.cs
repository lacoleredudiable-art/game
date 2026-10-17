using System.Collections;
using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// O-5.10 sütun arkası (teşhis 7, [K 2026-10-15] "sütun saydamlaşsın"): sütun kamera ile karakter arasına girince
    /// kamera öne gelmez (mesafesini korur), sütun O-5.11 opaklığıyla saydamlaşır; kamera sütunun içine girmez.
    /// Ölçü görünen kameradan (<see cref="CameraRenderedProbe"/>), oyuncu sahte çubukla sütuna doğru yürür.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float ColumnCamHoldSec = 1.5f;
        const float ColumnCamPressSec = 2f;
        const float ColumnTopCastSideM = 0.7f;
        const float ColumnTopCastAxisM = 3.5f;

        /// <summary>Kamera, oyuncunun sütunun TERSİ yönünde durur: sütun kamera ile oyuncu arasına girer.</summary>
        static float ColumnBehindYaw() => Mathf.Atan2(Outward.x, Outward.z) * Mathf.Rad2Deg;

        struct ColumnFrame
        {
            public Vector3 Cam;
            public Vector3 Aim;
            public bool Faded;
        }

        [UnityTest]
        public IEnumerator O_5_10_SutunaYaslanmis_KameraOneGelmez_SutunSaydam_KarakterGorunur()
        {
            FreezeBoss();
            ParkAll();
            (Camera cam, FollowCameraController follow, CameraOrbitController orbit) = GameCamera();
            orbit.enabled = false;
            follow.BossTarget = null;
            Assert.That(follow.SliceGuard, Is.Not.Null, "dilim kamera kuralları bağlı değil");
            RuleEngineV4SliceArenaColumn col = ArenaSpec.Columns[0];
            Transform colT = ArenaChildren("Columns")[0];
            Vector3 colCenter = new(col.XM, 0f, col.ZM);
            Place(_player, colCenter + Outward * (col.RadiusM + PlayerBodyM));
            _player.rotation = Quaternion.LookRotation(-Outward);
            Physics.SyncTransforms();
            follow.OrbitYawDeg = ColumnBehindYaw();
            float lookM = follow.Tuning.Camera.CameraLookHeightM;
            float minDistM = follow.Tuning.Camera.CameraCollisionMinDistanceM;
            var move = _player.GetComponent<MoveInputController>();
            Assert.That(move, Is.Not.Null, "MoveInputController");
            CameraRenderedProbe probe = AddCameraProbe(cam, lookM);
            var frames = new List<ColumnFrame>();
            try
            {
                yield return new WaitForSecondsRealtime(ColumnCamHoldSec);
                float start = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - start < ColumnCamPressSec)
                {
                    Steer(move, -Outward);
                    yield return null;
                    // Kamera ve saydamlık aynı (önceki) LateUpdate'ten; ikisi birlikte okunur.
                    frames.Add(new ColumnFrame
                    {
                        Cam = cam.transform.position,
                        Aim = _player.position + Vector3.up * lookM,
                        Faded = follow.SliceGuard.IsFadedColumn(colT),
                    });
                }
            }
            finally
            {
                move.SetScriptedDirection(null);
            }

            List<CameraRenderedProbe.Sample> samples = probe.Samples;
            Object.Destroy(probe.gameObject);
            Assert.That(samples.Count, Is.GreaterThan(0), "kamera sondası kare görmedi");
            Assert.That(frames.Count, Is.GreaterThan(0), "test döngüsü kare görmedi");
            var fails = new List<string>();
            int inside = 0;
            int offScreen = 0;
            foreach (CameraRenderedProbe.Sample s in samples)
            {
                if (Flat(s.Cam - colCenter).magnitude < col.RadiusM && s.Cam.y < col.HeightM)
                    inside++;
                if (s.AimViewport.z <= 0f || s.AimViewport.x < 0f || s.AimViewport.x > 1f
                    || s.AimViewport.y < 0f || s.AimViewport.y > 1f)
                    offScreen++;
            }

            int between = 0;
            int notFaded = 0;
            int pulledIn = 0;
            foreach (ColumnFrame f in frames)
            {
                if ((f.Cam - f.Aim).magnitude < minDistM)
                    pulledIn++;
                if (!ColumnBetween(f.Cam, f.Aim))
                    continue;
                between++;
                if (!f.Faded)
                    notFaded++;
            }

            if (inside > 0)
                fails.Add($"kamera {samples.Count} karenin {inside}'inde sütunun içinde");
            if (offScreen > 0)
                fails.Add($"karakter {samples.Count} karenin {offScreen}'inde ekran dışında");
            if (pulledIn > 0)
                fails.Add($"kamera {frames.Count} karenin {pulledIn}'inde karaktere {minDistM:0.0} m'den yakın (sütun öne çekmemeli)");
            if (between == 0)
                fails.Add("senaryo sütunu kamera ile karakter arasına hiç sokmadı");
            if (notFaded > 0)
                fails.Add($"sütun arada kaldığı {between} karenin {notFaded}'inde saydam değil");
            Debug.Log($"[O-5.10] kare {frames.Count}, sütun arada {between}, saydam değil {notFaded}, yakın {pulledIn}");
            Assert.That(fails, Is.Empty, "O-5.10 " + string.Join("; ", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        /// <summary>
        /// Sütun tepesi kıyısı (teşhis 7 §1: görünen tepe çarpışma kapsülünden geniş): kamera sütun yüzünden öne
        /// çekilmez ve sütunun içine girmez.
        /// </summary>
        [UnityTest]
        public IEnumerator O_5_10_SutunTepesiKiyisi_KameraOneGelmez_SutunIcindeDegil()
        {
            FreezeBoss();
            ParkAll();
            (Camera cam, FollowCameraController follow, CameraOrbitController orbit) = GameCamera();
            orbit.enabled = false;
            follow.BossTarget = null;
            RuleEngineV4SliceArenaColumn col = ArenaSpec.Columns[0];
            Vector3 colCenter = new(col.XM, 0f, col.ZM);
            Vector3 stand = colCenter + Outward * ColumnTopCastAxisM + Vector3.forward * ColumnTopCastSideM;
            Place(_player, stand);
            _player.rotation = Quaternion.LookRotation(-Outward);
            Physics.SyncTransforms();
            follow.OrbitYawDeg = ColumnBehindYaw();
            float lookM = follow.Tuning.Camera.CameraLookHeightM;
            float minDistM = follow.Tuning.Camera.CameraCollisionMinDistanceM;
            CameraRenderedProbe probe = AddCameraProbe(cam, lookM);
            yield return new WaitForSecondsRealtime(ColumnCamHoldSec);
            CameraRenderedProbe.Sample s = probe.Last;
            Object.Destroy(probe.gameObject);

            Vector3 aim = _player.position + Vector3.up * lookM;
            Assert.That((s.Cam - aim).magnitude, Is.GreaterThanOrEqualTo(minDistM),
                $"O-5.10 sütun tepesinin kıyısında kamera {s.Cam} karaktere {minDistM:0.0} m'den yakın (eksenden {ColumnTopCastAxisM} m, yanal {ColumnTopCastSideM} m)");
            Assert.That(Flat(s.Cam - colCenter).magnitude < col.RadiusM && s.Cam.y < col.HeightM, Is.False,
                $"O-5.10 kamera {s.Cam} sütunun içinde");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        /// <summary>Kamera→bakış noktası doğrusu bir sütun çarpışıcısını kesiyor mu (oyuncunun çarpışıcısı yok).</summary>
        static bool ColumnBetween(Vector3 camPos, Vector3 aim)
        {
            RaycastHit[] hits = Physics.RaycastAll(camPos, aim - camPos, (aim - camPos).magnitude, ~0,
                QueryTriggerInteraction.Ignore);
            foreach (RaycastHit h in hits)
            {
                Transform t = h.collider.transform;
                if (t.name.StartsWith("ArenaColumn") || (t.parent != null && t.parent.name.StartsWith("ArenaColumn")))
                    return true;
            }

            return false;
        }
    }
}
