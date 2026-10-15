using System;
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

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Dodge engele çarpınca yüzey boyunca kayar (O-4.9; 15-dodge-tasarimi). Girdi gerçek yoldan: çubuk + dodge
    /// düğmesine dokunuş. Ölçüt: gövde hiçbir karede engele girmez, çarpma noktasından sonra yüzey boyunca
    /// ≥ bütçe (R × max(t, SlideMinRatio)) kadar yol alır, çubuğun teğet işaretli tarafa gider, i-frame sürer.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float SlideContactSlackM = 0.03f;
        /// <summary>Kayma bütçesinin alt sınırı çarpma kestirimi (kare aralığı, süzülme) yüzünden bu oranla gevşek okunur.</summary>
        const float SlideBudgetFloorRatio = 0.6f;
        const float SlideGapDriftM = 0.08f;

        [UnityTest]
        public IEnumerator O_4_9_BossaEgikDodge_GovdeKenarindaYuzeyBoyuncaKayar()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            var fails = new List<string>();
            Vector3 bossC = Flat(_boss.position);
            float stop = BossBodyM + PlayerBodyM + InputController().Combat.Dodge.EdgeGapM;
            Vector3 away = BossToCenter();
            Vector3 lateral = Vector3.Cross(Vector3.up, away).normalized;
            Vector3 start = bossC + away * (stop + BossDodgeStartExtraM + 0.5f);
            foreach (float sign in new[] { 1f, -1f })
            {
                Vector3 aim = bossC + lateral * sign * stop * 0.45f;
                yield return SlideScenario(
                    $"boss'a eğik dodge ({(sign > 0 ? "sol" : "sağ")} yan)", start, (aim - start).normalized,
                    p => (p - bossC).magnitude - stop, p => (p - bossC).normalized, fails);
            }

            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator O_4_9_SutunaVeKozayaEgikDodge_YuzeyBoyuncaKayar()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            var fails = new List<string>();
            Vector3 lateral = Vector3.Cross(Vector3.up, Outward).normalized;

            var col = ArenaSpec.Columns[0];
            Vector3 colC = new(col.XM, 0f, col.ZM);
            float colStop = col.RadiusM + PlayerBodyM + InputController().Combat.Dodge.EdgeGapM;
            Vector3 colStart = colC + Outward * (col.RadiusM + PlayerBodyM + ObstacleDodgeGapM);
            Vector3 colAim = colC + lateral * col.RadiusM * 0.4f;
            yield return SlideScenario("sütuna eğik dodge", colStart, (colAim - colStart).normalized,
                p => (p - colC).magnitude - colStop, p => (p - colC).normalized, fails);

            Vector3 cocC = Flat(Cocoons()[0].transform.position);
            float cocR = ArenaSpec.Cocoons[0].RadiusM;
            float cocStop = cocR + PlayerBodyM + InputController().Combat.Dodge.EdgeGapM;
            Vector3 cocStart = cocC + Outward * (cocR + PlayerBodyM + ObstacleDodgeGapM);
            Vector3 cocAim = cocC + lateral * cocR * 0.4f;
            yield return SlideScenario("kozaya eğik dodge", cocStart, (cocAim - cocStart).normalized,
                p => (p - cocC).magnitude - cocStop, p => (p - cocC).normalized, fails);

            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator O_4_9_ArenaDuvarinaEgikDodge_DuvarBoyuncaKayar()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            var fails = new List<string>();
            Vector3 start = Outward * ArenaEdgeStartM;
            foreach (float deg in new[] { 40f, -40f })
            {
                Vector3 dir = Quaternion.Euler(0f, deg, 0f) * Outward;
                yield return SlideScenario($"arena duvarına {deg:0}° eğik dodge", start, dir,
                    p => ArenaLimitM - InputController().Combat.Dodge.EdgeGapM - p.magnitude, p => -p.normalized, fails);
            }

            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        /// <summary>
        /// clearance(p): gövde merkezinin engelin durma çizgisine uzaklığı (≤ ~0 = temas). normalAt(p): engelden dışa bakan normal.
        /// </summary>
        IEnumerator SlideScenario(
            string label, Vector3 start, Vector3 dir,
            Func<Vector3, float> clearance, Func<Vector3, Vector3> normalAt, List<string> fails)
        {
            HexagonInputController input = InputController();
            MoveInputController move = BeginScenario(label, start, dir, out BodyWatch watch, out var obstacles);
            input.Charges.Refund(input.Charges.Max);
            var motion = _player.GetComponent<DodgeMotionController>();
            Vector3 contact = Vector3.zero;
            bool touched = false;
            bool iframeLost = false;
            float worstDriftM = 0f;
            int pressMs = 0;

            void Measure()
            {
                Sample(watch, obstacles);
                Vector3 p = Flat(_player.position);
                if (!touched && clearance(p) <= SlideContactSlackM)
                {
                    touched = true;
                    contact = p;
                }
                // EdgeGapM kayma boyunca korunur: durma çizgisinden ne içeri ne dışarı sapar.
                if (touched)
                    worstDriftM = Mathf.Max(worstDriftM, Mathf.Abs(clearance(p)));
                int now = NowWorldMs();
                if (pressMs > 0 && now - pressMs < input.Combat.Dodge.IframeMs - 40 && !input.Dodge.IsInvulnerable(now))
                    iframeLost = true;
            }

            try
            {
                Steer(move, dir);
                yield return null;
                yield return PressDodgeUntilStarted(input);
                pressMs = input.Dodge.PressTimeMs ?? 0;
                float guard = Time.realtimeSinceStartup + DodgeGuardSec;
                while (input.Dodge.IsActive(NowWorldMs()) && Time.realtimeSinceStartup < guard)
                {
                    Steer(move, dir);
                    Measure();
                    yield return null;
                }
                Measure();
            }
            finally
            {
                move.SetScriptedDirection(null);
            }

            Vector3 end = Flat(_player.position);
            float total = input.Combat.Dodge.DistanceM * input.Dodge.DistanceMultiplier;
            if (!touched)
            {
                fails.Add($"{_label}: senaryo kurulmadı, dodge engele değmedi");
                Judge(watch, start, fails);
                yield break;
            }

            Vector3 n = normalAt(contact);
            float dn = Vector3.Dot(dir, n);
            float t = Mathf.Sqrt(Mathf.Max(0f, 1f - dn * dn));
            float remaining = Mathf.Max(0f, total - (contact - start).magnitude);
            var cfg = input.Combat.Dodge;
            float budget = remaining * Mathf.Max(t, cfg.SlideMinRatio);
            float slid = (end - contact).magnitude;
            Vector3 tangent = Vector3.Cross(Vector3.up, n).normalized;
            float wantSign = Mathf.Sign(Vector3.Dot(dir, tangent));
            float gotSign = Mathf.Sign(Vector3.Dot(end - contact, tangent));
            Debug.Log($"[O-4.9] {_label}: kenar sapması {worstDriftM:0.000} m, çarpma ({contact.x:0.00},{contact.z:0.00}), t {t:0.00}, kalan yol {remaining:0.00} m, " +
                      $"bütçe {budget:0.00} m, kayılan {slid:0.00} m, taraf {(gotSign > 0 ? "+" : "-")} (çubuk {(wantSign > 0 ? "+" : "-")}), " +
                      $"kayma={(motion != null && motion.SlideSide != 0 ? "evet" : "hayır")}");
            if (slid < budget * SlideBudgetFloorRatio)
                fails.Add($"{_label}: yüzey boyunca {slid:0.00} m kaydı < bütçe {budget:0.00} m × {SlideBudgetFloorRatio:0.0}");
            if (t >= cfg.EngineChoiceT && wantSign != gotSign)
                fails.Add($"{_label}: çubuğun teğet işaretine ({wantSign:+0;-0}) değil ters tarafa ({gotSign:+0;-0}) kaydı");
            if (worstDriftM > SlideGapDriftM)
                fails.Add($"{_label}: kayarken engel kenar payından {worstDriftM:0.000} m saptı (izin {SlideGapDriftM:0.00} m)");
            if (iframeLost)
                fails.Add($"{_label}: engelde kayarken i-frame IframeMs dolmadan düştü");
            Judge(watch, start, fails);
        }
    }
}
