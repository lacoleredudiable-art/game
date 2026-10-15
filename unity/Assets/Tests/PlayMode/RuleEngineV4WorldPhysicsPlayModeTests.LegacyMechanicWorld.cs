using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dovus.Core.Element;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Casting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Envanter C2 (O-9.1): v4 açıkken eski motorun (MechanicWorldRuntime) alan ve duvarı dünyada oluşmaz.
    /// Dilimin 36 kombosu iki silahla gerçek çizimle (sahte dokunuş → CastPipeline) atılır; her cast sonrası sahnede
    /// MechanicWall / MechanicFence / MechanicField / MechanicCloud / MechanicReflector adlı nesne olmamalı.
    /// Gramer planı v4'te de kurulur ve MechanicWorldRuntime her cast'te çağrılır: bu geri dönüş değil, eski motorun
    /// kapalı olduğu normal akıştır; bu yüzden [V4-FALLBACK] yazılmaz (yazılsa Gözcü v4_fallback eşiği 0 kırılır).
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        static readonly string[] LegacyMechanicNames =
        {
            "MechanicWall", "MechanicFence", "MechanicField", "MechanicCloud", "MechanicReflector"
        };

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator O_9_1_EskiMotorAlanDuvar_V4Acikken_Olusmaz()
        {
            FreezeBoss();
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);
            RuneLoadout saved = input.Engine.Loadout;
            Assert.That(input.Engine.TrySetLoadout(new RuneLoadout(RuleEngineV4Slice.Runes.ToArray())), Is.True, "build seçilemedi");
            SetSmokeLoadout(Kilic, Yay);
            var offenders = new StringBuilder();
            var seen = new HashSet<int>();
            int casts = 0;
            int drawn = 0;
            try
            {
                foreach (int weapon in new[] { Kilic, Yay })
                {
                    yield return SmokeSwapTo(input, weapon);
                    foreach (int verb in RuleEngineV4Slice.Runes)
                    foreach (int adj in RuleEngineV4Slice.Runes)
                    {
                        _label = $"kombo {verb}-{adj} w{weapon}";
                        casts++;
                        yield return SmokeReady(input);
                        SmokePlaceMinions();
                        int verbDot = DotForRune(input, verb);
                        int adjDot = DotForRune(input, adj);
                        input.Pointer.BeginPointer(DotPos(input, verbDot));
                        yield return null;
                        if (adjDot == verbDot)
                        {
                            input.Pointer.MovePointer(Vector2.Lerp(DotPos(input, verbDot), CenterPos(input), 0.5f));
                            yield return null;
                        }
                        input.Pointer.MovePointer(DotPos(input, adjDot));
                        yield return null;
                        input.Pointer.EndPointer(cancelled: false);
                        drawn += _cast.IsRunning ? 1 : 0;
                        yield return WaitUntilIdle();
                        yield return WaitWorld(0.2f);

                        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                        {
                            if (!LegacyMechanicNames.Contains(t.name) || !seen.Add(t.gameObject.GetInstanceID()))
                                continue;
                            offenders.AppendLine($"{_label}: sahnede {t.name}");
                        }
                    }
                }
            }
            finally
            {
                input.Engine.TrySetLoadout(saved);
            }

            Debug.Log($"[C2] {casts} cast, çizilen={drawn}, Mechanic* nesne satırı={offenders.Length}");
            Assert.That(drawn, Is.GreaterThan(0), "çizimle hiç kombo atılmadı");
            Assert.That(offenders.ToString(), Is.Empty, "v4 açıkken eski motor alan/duvar üretti:\n" + offenders);
        }
    }
}
