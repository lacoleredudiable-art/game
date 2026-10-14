using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dovus.Core.Boss;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Envanter Y17 (13 Ekim, K): dilimde zehir yalnız <c>slice_boss_attacks.poison_breath_*</c> ile gelir.
    /// "Zehirli Ateş" (yanma + zehir ek tiki) kural kitabında yok, dilimde kapalı; başka bir kaynaktan gelen genel
    /// zehir (StatusTuning değerleri) uygulanmaz, [V4-FALLBACK] yazılır.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float SingleBreathWaitSec = 3.5f;
        const float LegacyOnHitSec = 1.5f;

        /// <summary>
        /// Zehir Nefesi oyuncuyu zehirler; vuruş anında oyuncu ayrıca yanar (dilimde yanma kaynağı yok, tahtaya konur).
        /// Toplam = 180 + zehir 60 + yanma; "Zehirli Ateş" ek tiki eklenmez.
        /// </summary>
        [UnityTest]
        public IEnumerator O_6_17_ZehirliAtes_DilimdeKapali()
        {
            StatusTuning st = LiveCombat().Status;
            ActorStatusHost status = _md._playerStatus;
            var drops = new List<int>();
            double burnMs = 0;
            yield return BossAttacksPlayer(BossAttackKind.FireCone, toward: 1.2f, drops, afterHit: () =>
            {
                FreezeBoss();
                Assert.That(status.Board.TryGet(StatusKind.Poison, out double poisonMs, out _, out _), Is.True,
                    "Zehir Nefesi zehirlemedi");
                burnMs = poisonMs;
                status.Board.Apply(StatusKind.Burn, burnMs, st.BurnDamagePerSec);
            }, waitAfterHitSec: SingleBreathWaitSec);

            float burnTotal = (float)(burnMs / 1000.0) * st.BurnDamagePerSec;
            int expected = 180 + Mathf.FloorToInt(60 + burnTotal + 0.001f);
            Assert.That(drops.Sum(), Is.EqualTo(expected).Within(1),
                $"Y17 dilimde Zehirli Ateş kapalı: 180 + zehir 60 + yanma {burnTotal:0.##}; " +
                $"ek tik {st.BurnPoisonComboBonusPerSec}/s; düşüşler: " + string.Join(", ", drops));
        }

        /// <summary>
        /// Ağ Tükürüğü'nün isabet durumu zehir olsaydı (boss JSON'unda 'slow'): mermi oyuncuya çarpınca StatusTuning
        /// zehri uygulanmaz, [V4-FALLBACK] yazılır.
        /// </summary>
        [UnityTest]
        public IEnumerator O_6_15_AgTukurugu_GenelZehir_DilimdeV4Fallback()
        {
            BossDirector bossDir = _md.MechanicsBossDirector;
            var entries = (Dictionary<BossAttackKind, BossAttackEntry>)typeof(BossDirector)
                .GetField("_attackEntriesByKind", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(bossDir);
            Assert.That(entries.TryGetValue(BossAttackKind.Volley, out BossAttackEntry volley), Is.True, "Ağ Tükürüğü girdisi");
            BossOnHitStatus original = volley.OnHitStatus;
            volley.OnHitStatus = new BossOnHitStatus("poison", LegacyOnHitSec);

            bool poisoned = false;
            ActorStatusHost status = _md._playerStatus;
            LogAssert.ignoreFailingMessages = true;
            var drops = new List<int>();
            try
            {
                yield return BossAttacksPlayer(BossAttackKind.Volley, toward: 2f, drops, afterHit: () =>
                    poisoned = status.Board.TryGet(StatusKind.Poison, out _, out _, out _));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                volley.OnHitStatus = original;
            }
            // Beklenen satır; geri kalan konsol hataları TearDown'da yakalanır.
            int fallbacks = _errors.RemoveAll(e => e.Contains("[V4-FALLBACK] BossDirector volley") && e.Contains("poison"));

            Assert.That(drops, Is.Not.Empty, "Ağ Tükürüğü oyuncuya isabet etmedi");
            Assert.That(poisoned || status.Board.TryGet(StatusKind.Poison, out _, out _, out _), Is.False,
                "Y17 dilimde genel zehir (StatusTuning) uygulandı");
            Assert.That(fallbacks, Is.GreaterThan(0), "Y17 dilimde genel zehir isteği [V4-FALLBACK] yazmadı");
        }

        CombatTuning LiveCombat() => (CombatTuning)typeof(BossDirector)
            .GetField("_combat", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(_md.MechanicsBossDirector);
    }
}
