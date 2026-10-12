using System.Collections;
using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Skills.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Boss gövdesi (O-6.10, O-4.9; 02-dunya-fizigi D-2): dilimde boss yarıçapının tek kaynağı kural-motoru-v4.json
    /// <c>world_physics.body_radius_boss_m</c>. Dünya fiziği gövdesi, dodge'un boss kenarı, hedefleme ve menzil
    /// ölçümü (TargetableHost, alan, mermi) ve boss'un hareket gövdesi aynı değeri okur; menzil gövde kenarından
    /// ölçülür. Boss'a doğru dodge ve takla gövde kenarından kenar payı kala durur. Girdi gerçek yoldan: dodge
    /// düğmesine dokunuş, çubuk.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        /// <summary>10-olculer O-6.10.</summary>
        const float DocBodyRadiusBossM = 2.5f;
        /// <summary>Dodge başlangıcı durma mesafesinin bu kadar ötesinde; dodge (3,8 m) bu farkı aşar.</summary>
        const float BossDodgeStartExtraM = 1f;
        /// <summary>Kayma bittikten sonra toparlanma kareleri de ölçülür.</summary>
        const int BossDodgeSettleFrames = 10;
        /// <summary>Menzil senaryosu: alan yarıçapı ve kenarlar arası boşluk (gövde kenarı menzilde, eski 0,85 m'lik çarpışıcı dışında).</summary>
        const float BossRangeProbeAreaM = 2f;
        const float BossRangeProbeInsideM = 0.1f;

        [UnityTest]
        public IEnumerator O_6_10_BossYaricapi_TekKaynak()
        {
            FreezeBoss();
            ParkAll();
            yield return null;
            RuleEngineV4WorldPhysics wp = RuleEngineV4WorldPhysicsRuntime.Active;
            float json = wp.BodyRadiusBossM;
            Assert.That(wp.HasBodyRadiusBossM, Is.True, "kural-motoru-v4.json world_physics.body_radius_boss_m yok");
            Assert.That(json, Is.EqualTo(DocBodyRadiusBossM).Within(BodyRadiusEpsM),
                "kural-motoru-v4.json world_physics.body_radius_boss_m O-6.10 değil");

            var fails = new List<string>();
            void Same(string where, float value)
            {
                if (Mathf.Abs(value - json) > BodyRadiusEpsM)
                    fails.Add($"{where}: {value:0.000} m (JSON {json:0.000} m)");
            }

            Same("dünya fiziği gövdesi (boss)", _physics.BodyRadius(_boss));
            var reactor = _boss.GetComponent<BossReactorController>();
            Same("BossReactorController.BodyRadiusM (dodge boss kenarı, boss yaklaşması, yaylım, yansıyan mermi)",
                reactor.BodyRadiusM);
            var motionBody = _boss.GetComponent<MotionTemplateBodyHost>();
            Assert.That(motionBody, Is.Not.Null, "boss MotionTemplateBodyHost");
            Same("boss hareket gövdesi MotionTemplateBodyHost", motionBody.BodyRadiusM);
            var mark = _boss.GetComponent<TargetableHost>();
            Assert.That(mark, Is.Not.Null, "boss TargetableHost");
            Same("hedef halkası TargetableHost.MarkerRadius", mark.MarkerRadius(0f));

            // D-2: menzil kenardan. Çapraz yön çarpışıcının sınır kutusunun köşesine bakar.
            Vector3 c = _boss.position;
            float probe = json + BossDodgeStartExtraM;
            foreach (float deg in new[] { 0f, 45f })
            {
                Vector3 from = c + Quaternion.Euler(0f, deg, 0f) * Vector3.forward * probe;
                Same($"hedefleme/menzil TargetableHost.DistanceFrom ({deg:0}°), merkez − kenar",
                    probe - mark.DistanceFrom(from));
                Same($"menzil EdgeDistance ({deg:0}°), merkez − kenar", probe - _physics.EdgeDistance(from, _boss));
            }

            // Alan ve mermi: boss'un gövde kenarı menzilin içinde, eski çarpışıcı (0,85 m) dışında.
            Vector3 toCenter = BossToCenter();
            float casterR = _physics.BodyRadius(_player);
            float areaCenterDist = json + casterR + BossRangeProbeAreaM - BossRangeProbeInsideM;
            Place(_player, Flat(c) + toCenter * areaCenterDist);
            _player.rotation = Quaternion.LookRotation(-toCenter);
            Physics.SyncTransforms();
            List<Transform> area = _physics.CollectAreaHits(_player, BossRangeProbeAreaM, 4);
            if (!area.Contains(_boss))
                fails.Add($"alan {BossRangeProbeAreaM:0.0} m: kenarlar arası {BossRangeProbeAreaM - BossRangeProbeInsideM:0.00} m'deki boss vurulmadı");

            Vector3 origin = _player.position + Vector3.up * RuleEngineV4UnitySceneDefaults.ProjectileOriginHeightM;
            float projR = RuleEngineV4WorldPhysicsDefaults.ProjectileRadiusM;
            float missileRange = areaCenterDist - json - projR + BossRangeProbeInsideM;
            bool hit = _physics.SphereCastFirstTarget(origin, -toCenter, missileRange, projR, out Transform first);
            if (!hit || first != _boss)
                fails.Add($"mermi {missileRange:0.00} m: gövde kenarı {missileRange - BossRangeProbeInsideM:0.00} m'deki boss'a değmedi " +
                          $"(ilk değen {(first != null ? first.name : "yok")})");

            Debug.Log($"[O-6.10] JSON {json:0.000} m; fizik {_physics.BodyRadius(_boss):0.000} m; reaktör {reactor.BodyRadiusM:0.000} m; " +
                      $"hareket gövdesi {motionBody.BodyRadiusM:0.000} m; halka {mark.MarkerRadius(0f):0.000} m; " +
                      $"kenar mesafesi {mark.DistanceFrom(c + Vector3.forward * probe):0.000} m @ merkez {probe:0.000} m");
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
        }

        [UnityTest]
        public IEnumerator O_6_10_O_4_9_BossaDodgeVeTakla_GovdeKenarindaDurur()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            float stop = BossBodyM + PlayerBodyM + InputController().Combat.Dodge.EdgeGapM;
            Vector3 start = Flat(_boss.position) + BossToCenter() * (stop + BossDodgeStartExtraM);
            var fails = new List<string>();
            yield return BossDodgeScenario("boss'a doğru dodge tek basış", start, roll: false, stop, fails);
            yield return BossDodgeScenario("boss'a doğru dodge takla", start, roll: true, stop, fails);
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        /// <summary>
        /// Oyuncu boss'a bakarak başlar, çubuk boss'a doğru; dodge düğmesine dokunulur (takla: ikinci dokunuş).
        /// Kayma ve toparlanma boyunca her karede boss merkezine uzaklık ölçülür.
        /// Ölçüt: en yakın an ≥ boss gövdesi + oyuncu gövdesi + kenar payı (O-6.10, O-1.12, O-4.9); bitiş o mesafede.
        /// </summary>
        IEnumerator BossDodgeScenario(string label, Vector3 start, bool roll, float stop, List<string> fails)
        {
            HexagonInputController input = InputController();
            Vector3 bossC = Flat(_boss.position);
            Vector3 toBoss = (bossC - start).normalized;
            MoveInputController move = BeginScenario(label, start, toBoss, out BodyWatch watch, out var obstacles);
            input.Charges.Refund(input.Charges.Max);
            float nearest = Flat(_player.position - bossC).magnitude;
            void Measure()
            {
                nearest = Mathf.Min(nearest, Flat(_player.position - bossC).magnitude);
                Sample(watch, obstacles);
            }

            try
            {
                Steer(move, toBoss);
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
                    Steer(move, toBoss);
                    Measure();
                    yield return null;
                }
                // Yürüme boss'u engel saymaz; dodge bitince çubuk bırakılır ki yalnız dodge ölçülsün.
                move.SetScriptedDirection(null);
                for (int i = 0; i < BossDodgeSettleFrames; i++)
                {
                    Measure();
                    yield return null;
                }
                Measure();
            }
            finally
            {
                move.SetScriptedDirection(null);
            }

            float final = Flat(_player.position - bossC).magnitude;
            float moved = Vector3.Dot(Flat(_player.position - start), toBoss);
            Debug.Log($"[O-6.10] {_label}: boss merkezine en yakın {nearest:0.000} m, bitiş {final:0.000} m " +
                      $"(durma {stop:0.000} m = boss {BossBodyM:0.00} + oyuncu {PlayerBodyM:0.00} + kenar payı), " +
                      $"boss'a doğru {moved:0.00} m");
            if (moved < PosTolM)
                fails.Add($"{_label}: senaryo kurulmadı, dodge boss'a doğru gitmedi ({moved:0.00} m)");
            if (nearest < stop - PosTolM)
                fails.Add($"{_label}: boss merkezine {nearest:0.000} m yaklaştı < {stop:0.000} m " +
                          $"({(stop - nearest) * 100f:0.0} cm fazla)");
            if (final > stop + PosTolM)
                fails.Add($"{_label}: durma mesafesine {final - stop:0.000} m kala durdu");
            Judge(watch, start, fails);
        }
    }
}
