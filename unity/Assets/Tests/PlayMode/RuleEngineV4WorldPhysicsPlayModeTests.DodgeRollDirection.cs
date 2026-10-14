using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Takla yön değiştirir (O-4.5, 15-dodge-tasarimi §3.2): ikinci basışta eski yol kesilir, yeni yol
    /// bulunduğu yerden ikinci basıştaki çubuk yönüne gider. Girdi gerçek yoldan: çubuk + dodge düğmesi.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        [UnityTest]
        public IEnumerator O_4_5_TaklaYonDegistirir_YeniYolIkinciBasistaCubukYonunde()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            Vector3 bossC = Flat(_boss.position);
            Vector3 away = BossToCenter();
            Vector3 lateral = Vector3.Cross(Vector3.up, away).normalized;
            float safe = BossBodyM + PlayerBodyM + InputController().Combat.Dodge.EdgeGapM + BossDodgeStartExtraM;
            Vector3 start = bossC + away * safe;
            var fails = new List<string>();

            var input = InputController();
            var move = BeginScenario("takla yön değişimi", start, lateral, out _, out _);
            input.Charges.Refund(input.Charges.Max);
            Vector3 atPress = start;
            try
            {
                Steer(move, lateral);
                yield return null;
                yield return PressDodgeUntilStarted(input);
                yield return null;
                atPress = Flat(_player.position);
                Steer(move, away);
                PressDodge(input);
                Assert.That(input.Dodge.IsCombined, Is.True, $"{_label}: ikinci basış takla olmadı");
                float guard = Time.realtimeSinceStartup + DodgeGuardSec;
                while (input.Dodge.IsActive(NowWorldMs()) && Time.realtimeSinceStartup < guard)
                {
                    Steer(move, away);
                    yield return null;
                }
            }
            finally
            {
                move.SetScriptedDirection(null);
            }

            Vector3 end = Flat(_player.position);
            float total = input.Combat.Dodge.DistanceM * input.Dodge.DistanceMultiplier;
            float firstLeg = (atPress - start).magnitude;
            float alongLateral = Vector3.Dot(end - start, lateral);
            float alongAway = Vector3.Dot(end - start, away);
            Debug.Log($"[O-4.5] {_label}: ilk bacak {firstLeg:0.000} m, takla bitişi çubuk yönünde {alongAway:0.000} m, " +
                      $"ilk yönde {alongLateral:0.000} m, toplam yol {total:0.000} m");
            if (firstLeg < PosTolM)
                fails.Add($"{_label}: senaryo kurulmadı, ilk dodge yer değiştirmedi ({firstLeg:0.000} m)");
            if (Mathf.Abs(alongLateral - Vector3.Dot(atPress - start, lateral)) > PosTolM)
                fails.Add($"{_label}: takla eski yönde ilerlemeye devam etti ({alongLateral:0.000} m)");
            if (alongAway < total - firstLeg - PosTolM)
                fails.Add($"{_label}: yeni yol çubuk yönünde {alongAway:0.000} m < beklenen {total - firstLeg:0.000} m");
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
        }
    }
}
