using System.Collections;
using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Oyuncu gövdesi (O-1.12, O-1.13): yarıçapın tek kaynağı kural-motoru-v4.json
    /// <c>world_physics.body_radius_player_m</c>. Hareket motoru, dünya fiziği gövdesi ve boss'un hedef kaydı
    /// aynı değeri okur. Gövde kapsülü ayak tabanından başlar; duvara yürüyen gövde iç yüze değip durur
    /// (O-7.2, O-4.9). Girdi gerçek yoldan: çubuk.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        /// <summary>10-olculer O-1.12.</summary>
        const float DocBodyRadiusPlayerM = 0.35f;
        const float BodyRadiusEpsM = 1e-5f;
        const int WallWalkDirections = 8;

        [UnityTest]
        public IEnumerator O_1_12_GovdeYaricapi_TekKaynak()
        {
            yield return null;
            float json = RuleEngineV4WorldPhysicsRuntime.Active.BodyRadiusPlayerM;
            Assert.That(json, Is.EqualTo(DocBodyRadiusPlayerM).Within(BodyRadiusEpsM),
                "kural-motoru-v4.json world_physics.body_radius_player_m O-1.12 değil");

            var fails = new List<string>();
            void Same(string where, float value)
            {
                if (Mathf.Abs(value - json) > BodyRadiusEpsM)
                    fails.Add($"{where}: {value:0.000} m (JSON {json:0.000} m)");
            }

            var motor = _player.GetComponent<KinematicMotorController>();
            Assert.That(motor, Is.Not.Null, "KinematicMotorController");
            Same("hareket motoru KinematicMotorController.BodyRadiusM", motor.BodyRadiusM);
            Same("dünya fiziği gövdesi (oyuncu)", _physics.BodyRadius(_player));
            foreach (Transform ally in _allies)
                Same($"dünya fiziği gövdesi ({ally.name})", _physics.BodyRadius(ally));

            var targets = Object.FindAnyObjectByType<HostileTargetsHost>();
            Assert.That(targets, Is.Not.Null, "HostileTargetsHost");
            int seen = 0;
            foreach (HostileTargetsHost.Entry e in targets.Entries)
            {
                if (e.Transform != _player && System.Array.IndexOf(_allies, e.Transform) < 0)
                    continue;
                seen++;
                Same($"boss hedef kaydı ({e.Transform.name})", e.RadiusM);
            }
            Assert.That(seen, Is.EqualTo(1 + _allies.Length), "boss hedef kaydında oyuncu ve dostlar yok");

            Debug.Log($"[O-1.12] JSON {json:0.000} m; motor {motor.BodyRadiusM:0.000} m; " +
                      $"fizik {_physics.BodyRadius(_player):0.000} m");
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
        }

        /// <summary>
        /// O-7.2, O-4.9: duvara yürüyen oyuncunun merkezi iç yüz − gövde yarıçapında (18 − 0,35 = 17,65 m) durur;
        /// sekiz yönde.
        /// </summary>
        [UnityTest]
        public IEnumerator O_7_2_DuvaraYuruyen_GovdeIcYuzdeDurur()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            float want = VisibleInnerFaceM - PlayerBodyM;
            var fails = new List<string>();
            for (int i = 0; i < WallWalkDirections; i++)
            {
                float deg = 360f / WallWalkDirections * i;
                Vector3 dir = Quaternion.Euler(0f, deg, 0f) * Vector3.right;
                yield return WalkScenario($"duvara yürüme {deg:0}°", dir * ArenaEdgeStartM, dir, fails);
                float stop = Flat(_player.position).magnitude;
                Debug.Log($"[O-7.2] duvara yürüme {deg:0}°: durduğu yarıçap {stop:0.000} m (beklenen {want:0.000} m)");
                if (Mathf.Abs(stop - want) > PosTolM)
                    fails.Add($"duvara yürüme {deg:0}°: {stop:0.000} m'de durdu, beklenen {want:0.000} m ± {PosTolM:0.00}");
            }

            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }
    }
}
