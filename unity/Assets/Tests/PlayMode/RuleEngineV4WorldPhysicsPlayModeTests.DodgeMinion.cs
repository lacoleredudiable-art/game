using System.Collections;
using System.Collections.Generic;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Dodge minyonun gövdesine girmez (O-4.9, O-7.4, O-4.4a; teşhis 6): minyon tetik çarpışıcılıdır, kapsül süpürmesi
    /// görmez; dodge boss'ta olduğu gibi gövde kenarında durur. Girdi gerçek yoldan: çubuk + dodge düğmesine dokunuş.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        /// <summary>Minyon oyuncunun bu kadar önünde (dodge 3,8 m; merkezin içinden geçerdi).</summary>
        const float DodgeMinionAheadM = 1.5f;
        /// <summary>İçeriden başlayan senaryoda oyuncu minyon merkezine bu kadar yakın.</summary>
        const float DodgeMinionInsideM = 0.3f;

        [UnityTest]
        public IEnumerator O_4_9_Dodge_MinyonaGirmez()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            Transform minion = _minions[0].transform;
            Vector3 start = Origin;
            Vector3 dir = Vector3.right;
            Place(minion, start + dir * DodgeMinionAheadM);
            float stop = PlayerBodyM + CreatureBodyM;
            var fails = new List<string>();
            yield return DodgeAtMinion("minyona doğru dodge", start, dir, minion, stop, fails);
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator O_4_9_Dodge_MinyonIcindenBaslarsa_TekKaredeAtmaz()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            Transform minion = _minions[0].transform;
            Vector3 dir = Vector3.right;
            Place(minion, Origin + dir * DodgeMinionInsideM);
            var fails = new List<string>();
            // Merkezden uzaklaşan yönde dodge: içeriden dışarı sürekli çıkar, tek karede yapışma olmaz.
            yield return DodgeAtMinion("minyon içinden dışarı dodge", Origin, -dir, minion, DodgeMinionInsideM, fails);
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        IEnumerator DodgeAtMinion(string label, Vector3 start, Vector3 dir, Transform minion, float stop, List<string> fails)
        {
            HexagonInputController input = InputController();
            MoveInputController move = BeginScenario(label, start, dir, out BodyWatch watch, out var obstacles);
            input.Charges.Refund(input.Charges.Max);
            Vector3 minionC = Flat(minion.position);
            float nearest = Flat(_player.position - minionC).magnitude;
            float firstDist = nearest;
            Vector3 prev = Flat(_player.position);
            float maxStep = 0f;
            void Measure()
            {
                nearest = Mathf.Min(nearest, Flat(_player.position - minionC).magnitude);
                Vector3 now = Flat(_player.position);
                maxStep = Mathf.Max(maxStep, (now - prev).magnitude);
                prev = now;
                Sample(watch, obstacles);
            }

            try
            {
                Steer(move, dir);
                yield return null;
                yield return PressDodgeUntilStarted(input);
                float guard = Time.realtimeSinceStartup + DodgeGuardSec;
                while (input.Dodge.IsActive(NowWorldMs()) && Time.realtimeSinceStartup < guard)
                {
                    Steer(move, dir);
                    Measure();
                    yield return null;
                }
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

            float final = Flat(_player.position - minionC).magnitude;
            Debug.Log($"[O-4.9] {_label}: minyon merkezine en yakın {nearest:0.000} m, bitiş {final:0.000} m, " +
                      $"başlangıç {firstDist:0.000} m, en büyük kare adımı {maxStep:0.000} m " +
                      $"(durma {stop:0.000} m = oyuncu {PlayerBodyM:0.00} + minyon {CreatureBodyM:0.00})");
            if (firstDist < stop)
            {
                // İçeriden başlayış: derinleşmez, kare adımı dodge'un kendi hızını aşmaz (yapışma yok).
                if (nearest < firstDist - PosTolM)
                    fails.Add($"{_label}: içeriden başlayan dodge minyona {firstDist - nearest:0.000} m daha girdi");
            }
            else if (nearest < stop - PosTolM)
            {
                fails.Add($"{_label}: minyon merkezine {nearest:0.000} m yaklaştı < {stop:0.000} m");
            }
        }
    }
}
