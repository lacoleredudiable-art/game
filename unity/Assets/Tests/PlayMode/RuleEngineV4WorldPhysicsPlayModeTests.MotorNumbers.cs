using System.Collections;
using System.Collections.Generic;
using Dovus.Core.Element;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Skills.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Envanter D3 (09 "Bilinen sapmalar"): ritim hasar çarpanı bir kez (O-13.5, K15), Yoğun şarjı ön sürenin içinde
    /// (K01), Yoğun Koruma 0,45 s (K17). Kombo gerçek çizimle (PointerRouter → altıgen noktaları) atılır; ölçülen,
    /// dünyada görünen sonuçtur: düşmanın canı, boss'un denge çubuğu, oyuncunun duruş penceresi.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const int ZararVerb = 1;
        const int KorumaVerb = 4;
        const int KontrolVerb = 6;
        const int YogunAdj = 1;
        const int GudumluAdj = 6;
        /// <summary>K01 tablosu: Kılıç ön süre 0,20 s, Yoğun şarjı 0,35 s; ritim çarpanı 0,40 / 0,55.</summary>
        const float KilicPrefireSec = 0.20f;
        const float YogunChargeSec = 0.35f;
        const float KilicRhythmMult = 0.40f / 0.55f;
        /// <summary>K17: Yoğun Koruma duruşu 0,45 s.</summary>
        const float YogunGuardSec = 0.45f;
        const float FrameTolSec = 0.06f;

        /// <summary>Verilen fiil ve sıfat rünlerinin noktalarından geçen çizim; parmak hemen kalkar.</summary>
        IEnumerator DrawCombo(int verbRune, int adjRune)
        {
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);
            yield return DrawThroughDots(input, DotForRune(input, verbRune), DotForRune(input, adjRune));
            input.Pointer.EndPointer(cancelled: false);
            Assert.That(_cast.IsRunning, Is.True, $"{verbRune}-{adjRune} çizimle çıkmadı");
        }

        /// <summary>
        /// Build seçim ekranının onayladığı yol (BuildSelectHud → TrySetLoadout): altıgene dilim rünleri dizilir.
        /// Açılıştaki varsayılan build dilim rünlerinin hepsini taşımıyor (ör. Koruma 4 yok).
        /// </summary>
        void UseSliceBuild()
        {
            var runes = new List<int>(RuleEngineV4Slice.Runes);
            Assert.That(InputController().TrySetLoadout(new RuneLoadout(runes)), Is.True, "dilim build'i yüklenmedi");
        }

        /// <summary>
        /// O-13.5: Kılıç Güdümlü Zarar (1-6) düşmandan taban hasar × 0,73 düşürür (ritim çarpanı bir kez). 1-6 seçildi
        /// çünkü açılış build'inde çizilebilen dilim Zarar kombosu; Güdümlü 3 kilit vuruşu (D7) gelince beklenen toplam değişir.
        /// </summary>
        [UnityTest]
        public IEnumerator O_13_5_RitimCarpani_BirKez_Kilic_1_6()
        {
            ArrangeComboTarget();
            SliceLightMinionHost target = _minions[0];
            int before = target.Hp;
            Assert.That(_catalog.Catalog.TryGetWeapon(Kilic, out RuleEngineV4Weapon sword), Is.True);
            float rhythm = sword.DamageScale(_catalog.Catalog.Globals.RitmReferenceTotalSec);
            Assert.That(rhythm, Is.EqualTo(KilicRhythmMult).Within(0.001f), "K01 Kılıç ritim çarpanı");

            yield return DrawCombo(ZararVerb, GudumluAdj);
            yield return WaitUntilIdle();

            int lost = before - target.Hp;
            int expected = Mathf.RoundToInt(_catalog.Catalog.Scale.BaseDamage * rhythm);
            Assert.That(lost, Is.EqualTo(expected),
                $"O-13.5 hasar ritim çarpanıyla bir kez çarpılmalı: beklenen {expected}, düşen {lost} " +
                $"(iki kez çarpılsa {Mathf.RoundToInt(_catalog.Catalog.Scale.BaseDamage * rhythm * rhythm)})");
        }

        /// <summary>
        /// Hücre 6-1 (Yoğun Kontrol), K01: Yoğun şarjı ön sürenin içindedir. Kılıç'ta parmak kalkışından boss'un denge
        /// çubuğunun düşmesine 0,20 + 0,35 = 0,55 s geçer (şarj ikinci kez beklenirse 0,90 s).
        /// </summary>
        [UnityTest]
        public IEnumerator O_Hucre6_1_YogunSarji_BirKez_Kilic_OnSure055()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            Vector3 bossPos = _boss.position;
            Vector3 toPlayer = Flat(Origin - bossPos).normalized;
            float edgeGap = Mathf.Min(1f, WeaponRange(Kilic) * 0.5f);
            Place(_player, bossPos + toPlayer * (_physics.BodyRadius(_boss) + _physics.BodyRadius(_player) + edgeGap));
            _player.rotation = Quaternion.LookRotation(-toPlayer);
            Physics.SyncTransforms();
            BossDirector bossDir = _md.MechanicsBossDirector;
            float poiseBefore = bossDir.PoiseRatio;

            yield return DrawCombo(KontrolVerb, YogunAdj);
            double releaseMs = NowMs;
            double dropMs = -1;
            float end = Time.realtimeSinceStartup + CastTimeoutSec;
            while (dropMs < 0 && Time.realtimeSinceStartup < end)
            {
                yield return null;
                if (bossDir.PoiseRatio < poiseBefore)
                    dropMs = NowMs;
            }
            yield return WaitUntilIdle();

            Assert.That(dropMs, Is.GreaterThanOrEqualTo(0), "6-1 boss'a denge hasarı vermedi");
            double sec = (dropMs - releaseMs) / 1000.0;
            Assert.That(sec, Is.EqualTo(KilicPrefireSec + YogunChargeSec).Within(FrameTolSec),
                $"K01 ön süre (şarj dahil) 0,55 s olmalı; ölçülen {sec:0.000} s");
        }

        /// <summary>Hücre 4-1 (Yoğun Koruma), K17: duruş penceresi 0,45 s.</summary>
        [UnityTest]
        public IEnumerator O_Hucre4_1_YogunKoruma_Sure045()
        {
            ArrangeComboTarget();
            UseSliceBuild();
            RuleEngineV4WorldSession session = _md.RuleEngineV4Session;
            Assert.That(session, Is.Not.Null, "RuleEngineV4Session");

            yield return DrawCombo(KorumaVerb, YogunAdj);
            double onMs = -1;
            double offMs = -1;
            float end = Time.realtimeSinceStartup + CastTimeoutSec;
            while (offMs < 0 && Time.realtimeSinceStartup < end)
            {
                yield return null;
                double now = NowMs;
                bool active = session.GuardActive(now, _player);
                if (active && onMs < 0)
                    onMs = now;
                else if (!active && onMs >= 0)
                    offMs = now;
            }
            yield return WaitUntilIdle();

            Assert.That(onMs, Is.GreaterThanOrEqualTo(0), "4-1 duruş açılmadı");
            Assert.That(offMs, Is.GreaterThanOrEqualTo(0), "4-1 duruş kapanmadı");
            double sec = (offMs - onMs) / 1000.0;
            Assert.That(sec, Is.EqualTo(YogunGuardSec).Within(FrameTolSec), $"K17 Yoğun Koruma 0,45 s; ölçülen {sec:0.000} s");
        }
    }
}
