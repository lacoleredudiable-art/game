using System.Collections;
using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Skills.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Arena sınırı tek kaynak (O-7.2, O-4.9, O-4.11; 10-olculer §18 Ç-1): dilimde yürüme, dodge, takla ve
    /// Hareket kombosu dış duvarın iç yüzünün (kural-motoru-v4.json <c>slice_arena</c> →
    /// <see cref="RuleEngineV4SliceArenaWall"/>) içinde kalır, gövde hiçbir engelin içine girmez;
    /// sahnedeki <c>ArenaHalfSizeM</c> okunmaz. Girdi gerçek yoldan: dodge düğmesine dokunuş, çubuk, kombo.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        /// <summary>Prototype.unity GameBootstrapHost'unda serileşmiş ArenaHalfSizeM.</summary>
        const float PrototypeSceneArenaHalfSizeM = 50f;
        const float ArenaEdgeStartM = 16f;
        const float ArenaWalkSec = 3f;
        const float ObstacleDodgeGapM = 1f;
        /// <summary>3-3 hedefi en yakın düşman olsun diye koza 2'den uzak, hedef duvarın hemen önünde.</summary>
        const float WallSkillStartM = 11f;
        const float WallSkillTargetM = 15.5f;
        const float BehindColumnM = 2.5f;
        const float MarkCasterM = 15f;
        const float MarkTargetM = 19f;
        /// <summary>Dodge yer değiştirmesi ile istenen yön arasında en çok 30°.</summary>
        const float DodgeDirMinCos = 0.866f;

        static readonly Vector3 Outward = Vector3.right;

        sealed class BodyWatch
        {
            public float MaxR;
            public float WorstPenM;
            public string WorstAt = "-";
        }

        static float PlayerBodyM => RuleEngineV4WorldPhysicsRuntime.Active.BodyRadiusPlayerM;

        float ArenaLimitM => new RuleEngineV4SliceArenaWall(ArenaSpec).InnerFaceRadiusM - PlayerBodyM;

        [UnityTest]
        public IEnumerator O_7_2_ArenaSiniri_TekKaynak_IcYuz()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            var motor = _player.GetComponent<KinematicMotorController>();
            Assert.That(motor, Is.Not.Null, "KinematicMotorController");
            motor.Tuning.Arena.ArenaHalfSizeM = PrototypeSceneArenaHalfSizeM;
            var fails = new List<string>();
            Vector3 start = Outward * ArenaEdgeStartM;

            foreach (bool wallOn in new[] { true, false })
            {
                ArenaWallCollision().SetActive(wallOn);
                Physics.SyncTransforms();
                string round = wallOn ? "duvar açık" : "duvar çarpışması kapalı";
                yield return DodgeScenario($"{round}: dodge tek basış", start, Outward, roll: false, fails);
                yield return DodgeScenario($"{round}: dodge takla", start, Outward, roll: true, fails);
                yield return WalkScenario($"{round}: yürüme {ArenaWalkSec:0} sn", start, Outward, fails);
            }

            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator O_4_9_Engeller_DuvarAcik_GovdeGirmez_SinirAsilmaz()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            var fails = new List<string>();

            RuleEngineV4SliceArenaColumn col = ArenaSpec.Columns[0];
            Vector3 colCenter = new(col.XM, 0f, col.ZM);
            Vector3 colStart = colCenter + Outward * (col.RadiusM + PlayerBodyM + ObstacleDodgeGapM);
            yield return DodgeScenario("sütuna doğru dodge tek basış", colStart, -Outward, roll: false, fails);
            yield return DodgeScenario("sütuna doğru dodge takla", colStart, -Outward, roll: true, fails);

            Vector3 cocoonCenter = Flat(Cocoons()[0].transform.position);
            Vector3 cocoonStart = cocoonCenter + Outward * (ArenaSpec.Cocoons[0].RadiusM + PlayerBodyM + ObstacleDodgeGapM);
            yield return DodgeScenario("kozaya doğru dodge", cocoonStart, -Outward, roll: false, fails);

            Equip(Kilic);
            ParkAll();
            Place(_minions[0].transform, Outward * WallSkillTargetM);
            yield return CastScenario("Kılıç 3-3 duvara doğru", Outward * WallSkillStartM, Outward, 3, 3,
                _minions[0].transform, fails);

            // Sütun oyuncu ile hedef arasında; hedef boss'tan yakın.
            ParkAll();
            Place(_minions[0].transform, colCenter - Outward * BehindColumnM);
            yield return CastScenario("Kılıç 3-3 sütuna doğru", colStart, -Outward, 3, 3, _minions[0].transform, fails);

            // Duvar ötesindeki işaret: duvar çarpışması kapalıyken 9-9 dosta konur, duvar geri açılır.
            ParkAll();
            GameObject wall = ArenaWallCollision();
            wall.SetActive(false);
            Place(_player, Outward * MarkCasterM);
            _player.rotation = Quaternion.LookRotation(Outward);
            Place(_allies[0], Outward * MarkTargetM);
            Physics.SyncTransforms();
            _label = "9-9 duvar ötesindeki dosta işaret";
            _cast.ResetEconomy();
            yield return Cast(9, 9);
            Transform marked = _cast.LastFocus;
            wall.SetActive(true);
            Physics.SyncTransforms();
            if (marked != _allies[0])
                fails.Add($"{_label}: işaret dosta konmadı (odak={(marked != null ? marked.name : "yok")})");
            Equip(Yay);
            yield return CastScenario("Yay 3-9 duvar ötesindeki işarete doğru", Outward * MarkCasterM, Outward, 3, 9,
                _allies[0], fails);

            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        static GameObject ArenaWallCollision() =>
            GameObject.Find(ArenaRoot).transform.Find("OuterWallCollision").gameObject;

        IEnumerator DodgeScenario(string label, Vector3 start, Vector3 dir, bool roll, List<string> fails)
        {
            HexagonInputController input = InputController();
            MoveInputController move = BeginScenario(label, start, dir, out BodyWatch watch, out var obstacles);
            input.Charges.Refund(input.Charges.Max);
            try
            {
                Steer(move, dir);
                yield return null;
                yield return PressDodgeUntilStarted(input);
                if (roll)
                {
                    yield return null;
                    PressDodge(input);
                    Assert.That(input.Dodge.IsCombined, Is.True, $"{_label}: ikinci basış takla olmadı");
                }
                float guard = Time.realtimeSinceStartup + DodgeGuardSec;
                while (input.Dodge.IsActive(NowWorldMs()) && Time.realtimeSinceStartup < guard)
                {
                    Steer(move, dir);
                    Sample(watch, obstacles);
                    yield return null;
                }
                Sample(watch, obstacles);
            }
            finally
            {
                move.SetScriptedDirection(null);
            }
            Vector3 moved = Flat(_player.position - start);
            if (moved.magnitude > PosTolM && Vector3.Dot(moved.normalized, dir) < DodgeDirMinCos)
                fails.Add($"{_label}: senaryo kurulmadı, dodge istenen yöne gitmedi (yer değiştirme {moved})");
            Judge(watch, start, fails);
        }

        IEnumerator WalkScenario(string label, Vector3 start, Vector3 dir, List<string> fails)
        {
            MoveInputController move = BeginScenario(label, start, dir, out BodyWatch watch, out var obstacles);
            try
            {
                double endMs = _md.MechanicsClock.Director.WorldTimeMs + ArenaWalkSec * 1000.0;
                float guard = Time.realtimeSinceStartup + ArenaWalkSec * 4f + 2f;
                while (_md.MechanicsClock.Director.WorldTimeMs < endMs && Time.realtimeSinceStartup < guard)
                {
                    Steer(move, dir);
                    Sample(watch, obstacles);
                    yield return null;
                }
                Sample(watch, obstacles);
            }
            finally
            {
                move.SetScriptedDirection(null);
            }
            Judge(watch, start, fails);
        }

        IEnumerator CastScenario(
            string label, Vector3 start, Vector3 facing, int verb, int adj, Transform expectFocus, List<string> fails)
        {
            BeginScenario(label, start, facing, out BodyWatch watch, out var obstacles);
            _cast.ResetEconomy();
            if (!_cast.TryLaunchCombo(verb, adj, out _))
            {
                fails.Add($"{_label}: atılmadı (red={_cast.LastDenial})");
                yield break;
            }
            float end = Time.realtimeSinceStartup + CastTimeoutSec;
            while (_cast.IsRunning && Time.realtimeSinceStartup < end)
            {
                Sample(watch, obstacles);
                yield return null;
            }
            Sample(watch, obstacles);
            yield return null;
            Sample(watch, obstacles);
            Transform focus = _cast.LastFocus;
            Debug.Log($"[O-4.9] {_label}: odak={(focus != null ? focus.name : "yok")}");
            if (focus != expectFocus)
                fails.Add($"{_label}: senaryo kurulmadı, odak {(focus != null ? focus.name : "yok")} (beklenen {expectFocus.name})");
            if (_cast.IsRunning)
                fails.Add($"{_label}: {CastTimeoutSec} sn içinde bitmedi");
            Judge(watch, start, fails);
        }

        MoveInputController BeginScenario(
            string label, Vector3 start, Vector3 dir,
            out BodyWatch watch, out (string name, Vector3 center, float radius)[] obstacles)
        {
            _label = label;
            var move = _player.GetComponent<MoveInputController>();
            Assert.That(move, Is.Not.Null, "MoveInputController");
            Place(_player, start);
            _player.rotation = Quaternion.LookRotation(dir);
            Physics.SyncTransforms();
            obstacles = Obstacles();
            watch = new BodyWatch();
            Sample(watch, obstacles);
            return move;
        }

        /// <summary>Çubuk kameraya göredir; dünya yönü takip kamerasının yaw'ı kadar geri döndürülür.</summary>
        static void Steer(MoveInputController move, Vector3 worldDir)
        {
            var follow = Object.FindAnyObjectByType<FollowCameraController>();
            float yaw = follow != null ? follow.MovementYawDeg : 0f;
            Vector3 local = Quaternion.Euler(0f, -yaw, 0f) * worldDir;
            move.SetScriptedDirection(new Vector2(local.x, local.z));
        }

        IEnumerator PressDodgeUntilStarted(HexagonInputController input)
        {
            float guard = Time.realtimeSinceStartup + DodgeGuardSec;
            PressDodge(input);
            while (!input.Dodge.IsActive(NowWorldMs()) && Time.realtimeSinceStartup < guard)
            {
                yield return new WaitForSecondsRealtime(SmokeFrameSec);
                PressDodge(input);
            }
            Assert.That(input.Dodge.IsActive(NowWorldMs()), Is.True, $"{_label}: dodge başlamadı");
        }

        static void PressDodge(HexagonInputController input)
        {
            Vector2 px = HexagonLayoutScreen.DodgeButtonPx(input.Tuning, Screen.width, Screen.height);
            input.Pointer.BeginPointer(px);
            input.Pointer.EndPointer(cancelled: false);
        }

        (string name, Vector3 center, float radius)[] Obstacles()
        {
            var list = new List<(string, Vector3, float)>();
            foreach (RuleEngineV4SliceArenaColumn c in ArenaSpec.Columns)
                list.Add(($"sütun({c.XM:0},{c.ZM:0})", new Vector3(c.XM, 0f, c.ZM), c.RadiusM));
            SliceArenaCocoonHost[] cocoons = Cocoons();
            for (int i = 0; i < cocoons.Length; i++)
            {
                if (!cocoons[i].IsBroken)
                    list.Add(($"koza{i + 1}", Flat(cocoons[i].transform.position), ArenaSpec.Cocoons[i].RadiusM));
            }
            list.Add(("boss", Flat(_boss.position), _physics.BodyRadius(_boss)));
            return list.ToArray();
        }

        void Sample(BodyWatch watch, (string name, Vector3 center, float radius)[] obstacles)
        {
            Vector3 p = Flat(_player.position);
            watch.MaxR = Mathf.Max(watch.MaxR, p.magnitude);
            foreach ((string name, Vector3 center, float radius) in obstacles)
            {
                float pen = radius + PlayerBodyM - (p - center).magnitude;
                if (pen > watch.WorstPenM)
                {
                    watch.WorstPenM = pen;
                    watch.WorstAt = name;
                }
            }
        }

        void Judge(BodyWatch watch, Vector3 start, List<string> fails)
        {
            float limit = ArenaLimitM;
            float moved = Flat(_player.position - start).magnitude;
            Debug.Log($"[O-7.2] {_label}: merkeze en uzak {watch.MaxR:0.000} m (sınır {limit:0.000} m), " +
                      $"en derin engel girişi {watch.WorstPenM * 100f:0.0} cm @{watch.WorstAt}, yer değiştirme {moved:0.00} m " +
                      $"({start.x:0.00},{start.z:0.00}) → ({_player.position.x:0.00},{_player.position.z:0.00})");
            if (watch.MaxR > limit)
                fails.Add($"{_label}: merkeze {watch.MaxR:0.000} m > sınır {limit:0.000} m");
            if (watch.WorstPenM > 0f)
                fails.Add($"{_label}: gövde {watch.WorstAt} içine {watch.WorstPenM * 100f:0.0} cm girdi");
        }
    }
}
