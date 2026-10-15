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
    /// O-5.10 sütun arkası (teşhis 7): kamera sütunun arkasında/içinde kalmaz, karakter ile kamera arasına sütun girmez.
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

        [UnityTest]
        public IEnumerator O_5_10_SutunaYaslanmis_KameraSutunIcindeDegil_KarakterGorunur()
        {
            FreezeBoss();
            ParkAll();
            (Camera cam, FollowCameraController follow, CameraOrbitController orbit) = GameCamera();
            orbit.enabled = false;
            follow.BossTarget = null;
            RuleEngineV4SliceArenaColumn col = ArenaSpec.Columns[0];
            Vector3 colCenter = new(col.XM, 0f, col.ZM);
            Place(_player, colCenter + Outward * (col.RadiusM + PlayerBodyM));
            _player.rotation = Quaternion.LookRotation(-Outward);
            Physics.SyncTransforms();
            follow.OrbitYawDeg = ColumnBehindYaw();
            float lookM = follow.Tuning.Camera.CameraLookHeightM;
            var move = _player.GetComponent<MoveInputController>();
            Assert.That(move, Is.Not.Null, "MoveInputController");
            CameraRenderedProbe probe = AddCameraProbe(cam, lookM);
            try
            {
                yield return new WaitForSecondsRealtime(ColumnCamHoldSec);
                float start = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - start < ColumnCamPressSec)
                {
                    Steer(move, -Outward);
                    yield return null;
                }
            }
            finally
            {
                move.SetScriptedDirection(null);
            }

            List<CameraRenderedProbe.Sample> samples = probe.Samples;
            Object.Destroy(probe.gameObject);
            Assert.That(samples.Count, Is.GreaterThan(0), "kamera sondası kare görmedi");
            var fails = new List<string>();
            int inside = 0;
            int blocked = 0;
            int offScreen = 0;
            foreach (CameraRenderedProbe.Sample s in samples)
            {
                if (Flat(s.Cam - colCenter).magnitude < col.RadiusM && s.Cam.y < col.HeightM)
                    inside++;
                Vector3 aim = _player.position + Vector3.up * lookM;
                if (ColumnBetween(s.Cam, aim))
                    blocked++;
                if (s.AimViewport.z <= 0f || s.AimViewport.x < 0f || s.AimViewport.x > 1f
                    || s.AimViewport.y < 0f || s.AimViewport.y > 1f)
                    offScreen++;
            }

            if (inside > 0)
                fails.Add($"kamera {samples.Count} karenin {inside}'inde sütunun içinde");
            if (blocked > 0)
                fails.Add($"kamera ile karakter arasına {samples.Count} karenin {blocked}'inde sütun girdi");
            if (offScreen > 0)
                fails.Add($"karakter {samples.Count} karenin {offScreen}'inde ekran dışında");
            Assert.That(fails, Is.Empty, "O-5.10 " + string.Join("; ", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator O_5_10_SutunTepesindenGecenIsin_CarpismaSilindirleOrtusur()
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
            CameraRenderedProbe probe = AddCameraProbe(cam, lookM);
            yield return new WaitForSecondsRealtime(ColumnCamHoldSec);
            CameraRenderedProbe.Sample s = probe.Last;
            Object.Destroy(probe.gameObject);

            Vector3 aim = _player.position + Vector3.up * lookM;
            Assert.That(ColumnBetween(s.Cam, aim), Is.False,
                $"O-5.10 sütun tepesinin kıyısında kamera {s.Cam} ile karakter arasına sütun girdi (eksenden {ColumnTopCastAxisM} m, yanal {ColumnTopCastSideM} m)");
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
