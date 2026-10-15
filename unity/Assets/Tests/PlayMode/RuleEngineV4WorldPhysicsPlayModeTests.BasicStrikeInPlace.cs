using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dovus.Core.Grammar;
using Dovus.Game.Casting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Envanter C1: düz vuruş yerinde. Kılıç VUR yerinde iaido, yer değiştirme ≈ 0 m (06 §2.2 Kılıç VUR);
    /// Yay'da ön sürede hareket yok (04 ritim tablosu). Merkeze gerçek dokunuşla 5 düz vuruş, joystick boşta:
    /// her karede oyuncunun başlangıçtan yatay uzaklığı ≤ 2 cm, [V4-FALLBACK] yazılmaz.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const int InPlaceStrikeCount = 5;
        const float InPlaceMaxDriftM = 0.02f;

        [UnityTest]
        public IEnumerator O_4_14_YayDuzVurus_YerindeKalir()
        {
            foreach (float targetM in BasicStrikeTargetDistancesM)
                yield return RunInPlaceStrikes(Yay, targetM);
        }

        [UnityTest]
        public IEnumerator O_4_14_KilicDuzVurus_YerindeKalir()
        {
            foreach (float targetM in BasicStrikeTargetDistancesM)
                yield return RunInPlaceStrikes(Kilic, targetM);
        }

        IEnumerator RunInPlaceStrikes(int weapon, float targetM)
        {
            FreezeBoss();
            Equip(weapon);
            Arrange(Vector3.right);
            Place(_minions[0].transform, Origin + Vector3.right * targetM);
            Physics.SyncTransforms();
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);

            string label = $"w{weapon} hedef {targetM:0} m";
            int basicDot = input.Tuning.Input.BasicStrikeDot;
            int strikes = 0;
            void OnCompleted(CompletedSentence s)
            {
                if (s.Words.Count == 1 && (int)s.Words[0].Rune == basicDot)
                    strikes++;
            }

            int errorsBefore = _errors.Count;
            Vector3 start = Flat(_player.position);
            var driftPerTap = new float[InPlaceStrikeCount + 1];
            void Sample(int tap)
            {
                float d = Flat(_player.position - start).magnitude;
                driftPerTap[tap] = Mathf.Max(driftPerTap[tap], d);
            }

            input.Engine.SentenceCompleted += OnCompleted;
            try
            {
                for (int tap = 1; tap <= InPlaceStrikeCount; tap++)
                {
                    input.Pointer.BeginPointer(CenterPos(input));
                    input.Pointer.EndPointer(cancelled: false);
                    float end = Time.realtimeSinceStartup + InputSettleSec;
                    while (input.Engine.State.Phase != SentencePhase.Idle && Time.realtimeSinceStartup < end)
                    {
                        yield return null;
                        Sample(tap);
                    }
                    float tail = Time.realtimeSinceStartup + TapTailWatchSec;
                    while (Time.realtimeSinceStartup < tail)
                    {
                        yield return null;
                        Sample(tap);
                    }
                }
            }
            finally
            {
                input.Engine.SentenceCompleted -= OnCompleted;
            }

            var fallbacks = new List<string>();
            for (int i = errorsBefore; i < _errors.Count; i++)
            {
                if (_errors[i].Contains(FallbackTag))
                    fallbacks.Add(_errors[i]);
            }
            float maxDrift = driftPerTap.Max();
            string perTap = string.Join(" ", Enumerable.Range(1, InPlaceStrikeCount)
                .Select(i => $"[vuruş {i}: {driftPerTap[i] * 100f:0.0} cm]"));
            Debug.Log($"[C1] {label}: düz vuruş={strikes}/{InPlaceStrikeCount} başlangıçtan kayma {perTap} " +
                      $"[V4-FALLBACK]={fallbacks.Count}\n{string.Join("\n", fallbacks.Distinct())}");
            Assert.That(strikes, Is.EqualTo(InPlaceStrikeCount), $"{label}: {InPlaceStrikeCount} dokunuşun {strikes}'i düz vuruş oldu");
            Assert.That(maxDrift, Is.LessThanOrEqualTo(InPlaceMaxDriftM),
                $"{label}: düz vuruşta oyuncu başlangıçtan {maxDrift * 100f:0.0} cm kaydı {perTap}");
            Assert.That(fallbacks, Is.Empty, $"{label}: {fallbacks.Count} [V4-FALLBACK]:\n" + string.Join("\n", fallbacks.Distinct()));
        }
    }
}
