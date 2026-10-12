using System.Collections;
using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Hareket komutlarının süpürmesi (O-1.13, O-4.9): KendiniTasi yolu boss'un gövdesine
    /// (kural-motoru-v4.json <c>world_physics.body_radius_boss_m</c>) girmez; gövde kenarları arasında kenar payı
    /// (0,15 m) kala durur. Girdi gerçek yoldan: kombo fırlatma; ölçüm her karede oyuncu ile boss merkezi arası.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        /// <summary>Boss gövdesine temas mesafesinin ötesinde başlangıç payı; silah menzili bu farkı aşar.</summary>
        const float BossDashStartExtraM = 1f;
        /// <summary>9-9 işareti konurken oyuncu ile dost arası.</summary>
        const float AllyMarkStandM = 2f;

        float BossBodyM => RuleEngineV4WorldPhysicsRuntime.Active.BodyRadiusBossM;

        /// <summary>Boss merkezinden arena merkezine (sütunsuz taraf).</summary>
        Vector3 BossToCenter()
        {
            Vector3 c = -Flat(_boss.position);
            return c.sqrMagnitude > PosTolM * PosTolM ? c.normalized : Vector3.back;
        }

        [UnityTest]
        public IEnumerator O_4_9_Kilic33_BossaAtilir_GovdeyeGirmez()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            Vector3 bossC = Flat(_boss.position);
            Vector3 start = bossC + BossToCenter() * (BossBodyM + PlayerBodyM + BossDashStartExtraM);
            var fails = new List<string>();
            yield return BossSweepScenario("Kılıç 3-3 boss'a doğru", start, 3, 3, _boss, fails);
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        /// <summary>İşaret yalnız dosta / yüzeye konur (9-9); işaretli dost boss'un arkasında, çekilme yolu gövdeden geçer.</summary>
        [UnityTest]
        public IEnumerator O_4_9_Yay39_BossunArkasindakiIsarete_GovdeyeGirmez()
        {
            FreezeBoss();
            Equip(Yay);
            ParkAll();
            Vector3 bossC = Flat(_boss.position);
            Vector3 toCenter = BossToCenter();
            float clear = BossBodyM + PlayerBodyM + BossDashStartExtraM;
            Vector3 allyAt = bossC - toCenter * clear;
            Place(_allies[0], allyAt);
            Place(_player, allyAt - toCenter * AllyMarkStandM);
            _player.rotation = Quaternion.LookRotation(toCenter);
            Physics.SyncTransforms();
            _label = "9-9 boss'un arkasındaki dosta işaret";
            _cast.ResetEconomy();
            yield return Cast(9, 9);
            var fails = new List<string>();
            if (_cast.LastFocus != _allies[0])
                fails.Add($"{_label}: senaryo kurulmadı, işaret dosta konmadı (odak={(_cast.LastFocus != null ? _cast.LastFocus.name : "yok")})");

            Vector3 start = bossC + toCenter * clear;
            yield return BossSweepScenario("Yay 3-9 boss'un arkasındaki işarete", start, 3, 9, _allies[0], fails);
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        /// <summary>
        /// Oyuncu boss'a bakarak başlar; kombo bitene kadar her karede boss merkezine uzaklık ölçülür.
        /// Ölçüt: en yakın an ≥ boss gövdesi + oyuncu gövdesi (O-1.12); bitişte kenarlar arası ≤ kenar payı (O-4.9).
        /// </summary>
        IEnumerator BossSweepScenario(
            string label, Vector3 start, int verb, int adj, Transform expectFocus, List<string> fails)
        {
            Vector3 bossC = Flat(_boss.position);
            Vector3 toBoss = (bossC - start).normalized;
            BeginScenario(label, start, toBoss, out BodyWatch watch, out var obstacles);
            float contact = BossBodyM + PlayerBodyM;
            float gap = InputController().Combat.Dodge.EdgeGapM;
            _cast.ResetEconomy();
            if (!_cast.TryLaunchCombo(verb, adj, out _))
            {
                fails.Add($"{_label}: atılmadı (red={_cast.LastDenial})");
                yield break;
            }
            float nearest = Flat(_player.position - bossC).magnitude;
            float end = Time.realtimeSinceStartup + CastTimeoutSec;
            while (_cast.IsRunning && Time.realtimeSinceStartup < end)
            {
                nearest = Mathf.Min(nearest, Flat(_player.position - bossC).magnitude);
                Sample(watch, obstacles);
                yield return null;
            }
            yield return null;
            nearest = Mathf.Min(nearest, Flat(_player.position - bossC).magnitude);
            Sample(watch, obstacles);

            Transform focus = _cast.LastFocus;
            float final = Flat(_player.position - bossC).magnitude;
            float moved = Vector3.Dot(Flat(_player.position - start), toBoss);
            Debug.Log($"[O-4.9] {_label}: odak={(focus != null ? focus.name : "yok")}, boss merkezine en yakın {nearest:0.000} m, " +
                      $"bitiş {final:0.000} m (temas {contact:0.000} m, kenar payı {gap:0.00} m), boss'a doğru {moved:0.00} m");
            if (focus != expectFocus)
                fails.Add($"{_label}: senaryo kurulmadı, odak {(focus != null ? focus.name : "yok")} (beklenen {expectFocus.name})");
            if (_cast.IsRunning)
                fails.Add($"{_label}: {CastTimeoutSec} sn içinde bitmedi");
            if (moved < PosTolM)
                fails.Add($"{_label}: senaryo kurulmadı, oyuncu boss'a doğru gitmedi ({moved:0.00} m)");
            if (nearest < contact - PosTolM)
                fails.Add($"{_label}: oyuncu boss gövdesine girdi: merkezler arası {nearest:0.000} m < {contact:0.000} m " +
                          $"({(contact - nearest) * 100f:0.0} cm içeride)");
            if (final > contact + gap + PosTolM)
                fails.Add($"{_label}: boss gövdesine {final - contact:0.000} m kala durdu (kenar payı {gap:0.00} m)");
            Judge(watch, start, fails);
        }
    }
}
