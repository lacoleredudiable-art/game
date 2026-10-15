using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dovus.Core.Boss;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Config;
using Dovus.Game.DevTools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Adım 2.3 (envanter A2): dilimde can ve boss saldırı hasarı yalnız kural-motoru-v4.json'dan.
    /// O-13.1 oyuncu 1000, O-6.11 boss 15000, O-6.14–O-6.18 boss saldırı hasarları. Boss gerçek beyin → ön işaret →
    /// vuruş yolundan saldırır; ölçülen, oyuncu canından düşen miktardır.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        static readonly Regex PanelHpFallback = new(@"\[V4-FALLBACK\].*PlayerMaxHp");
        const float BossStrikeTimeoutSec = 25f;
        const float SettleAfterHitSec = 1.2f;
        const float PoisonClearTimeoutSec = 5f;
        const string PanelHpRow = "Row_Oyuncu can tavanı";

        [UnityTest]
        public IEnumerator O_13_1_OyuncuCani_1000()
        {
            FreezeBoss();
            yield return null;
            RuleEngineV4Scale s = _catalog.Catalog.Scale;
            PlayerVitalsHost vitals = AuthoredPlayerVitals();
            Assert.That(vitals.MaxHp, Is.EqualTo(1000).And.EqualTo(Mathf.RoundToInt(s.PlayerMaxHp)),
                "O-13.1 oyuncu canı scale.player_max_hp değil (PlayerSettings 22 / boss JSON player_hp 22 okunmamalı)");
            foreach (Transform ally in _allies)
                Assert.That(ally.GetComponent<AllyDummyController>().MaxHp,
                    Is.EqualTo(Mathf.RoundToInt(s.AllyDummyMaxHp)), ally.name + " dost canı scale.ally_dummy_max_hp değil");
        }

        [UnityTest]
        public IEnumerator O_6_11_BossCani_15000()
        {
            FreezeBoss();
            yield return null;
            RuleEngineV4Scale s = _catalog.Catalog.Scale;
            Assert.That(_md.MechanicsBossVitals.MaxHp, Is.EqualTo(15000f).And.EqualTo(s.BossMaxHp),
                "O-6.11 boss canı scale.boss_max_hp değil (boss JSON total_hp 120 / BossTuning.MaxHp okunmamalı)");
        }

        [UnityTest]
        public IEnumerator O_6_14_BacakCakmasi_OyuncudanDuser_220()
        {
            var drops = new List<int>();
            yield return BossAttacksPlayer(BossAttackKind.Slam, toward: 1.2f, drops);
            Assert.That(drops, Is.Not.Empty, "Bacak Çakması oyuncuya isabet etmedi");
            Assert.That(drops[0], Is.EqualTo(220), "O-6.14 Bacak Çakması hasarı; düşüşler: " + string.Join(", ", drops));
        }

        [UnityTest]
        public IEnumerator O_6_15_AgTukurugu_MermiBasinaDuser_60()
        {
            var hits = new List<float>();
            ActorStatusHost status = _md._playerStatus;
            void OnTaken(float amount) => hits.Add(amount);
            status.DamageTaken += OnTaken;
            var drops = new List<int>();
            try
            {
                yield return BossAttacksPlayer(BossAttackKind.Volley, toward: 2f, drops);
            }
            finally
            {
                status.DamageTaken -= OnTaken;
            }

            Assert.That(hits, Is.Not.Empty, "Ağ Tükürüğü mermisi oyuncuya isabet etmedi");
            Assert.That(hits, Is.All.EqualTo(60f).Within(0.01f), "O-6.15 mermi başına hasar; isabetler: " + string.Join(", ", hits));
            Assert.That(drops.Sum(), Is.EqualTo(60 * hits.Count),
                $"O-6.15 {hits.Count} mermi oyuncu canından {drops.Sum()} düşürdü; düşüşler: " + string.Join(", ", drops));
        }

        [UnityTest]
        public IEnumerator O_6_17_ZehirNefesi_OyuncudanDuser_180_Zehir3sx20()
        {
            var hits = new List<float>();
            ActorStatusHost status = _md._playerStatus;
            void OnTaken(float amount) => hits.Add(amount);
            status.DamageTaken += OnTaken;
            var drops = new List<int>();
            double poisonMs = 0;
            float poisonPerSec = 0f;
            try
            {
                yield return BossAttacksPlayer(BossAttackKind.FireCone, toward: 1.2f, drops, afterHit: () =>
                    status.Board.TryGet(StatusKind.Poison, out poisonMs, out poisonPerSec, out _));
            }
            finally
            {
                status.DamageTaken -= OnTaken;
            }

            Assert.That(hits, Is.Not.Empty, "Zehir Nefesi oyuncuya isabet etmedi");
            Assert.That(hits[0], Is.EqualTo(180f).Within(0.01f), "O-6.17 Zehir Nefesi vuruş hasarı");
            Assert.That(drops[0], Is.GreaterThanOrEqualTo(180), "O-6.17 vuruş canı düşürmedi");
            Assert.That(poisonPerSec, Is.EqualTo(20f).Within(0.01f), "O-6.17 zehir saniyede 20 değil");
            Assert.That(poisonMs, Is.EqualTo(3000.0).Within(100.0), "O-6.17 zehir 3 s değil");
            Debug.Log($"[O-6.17] zehir sonrası toplam can kaybı {drops.Sum()} (vuruş 180 + zehir; belge 180 + 60)");
        }

        /// <summary>Y9: zehir tikinin küsuratı birikir; 3 s × 20 = 60 can, kare sayısından bağımsız.</summary>
        [UnityTest]
        public IEnumerator O_6_17_ZehirNefesi_ZehirToplami_60()
        {
            var drops = new List<int>();
            // Zehir 3 s sürerken boss ikinci nefesi atmasın: tek nefesin toplamı ölçülür.
            yield return BossAttacksPlayer(BossAttackKind.FireCone, toward: 1.2f, drops, afterHit: FreezeBoss,
                waitAfterHitSec: 3.5f);
            Assert.That(drops.Sum(), Is.EqualTo(180 + 60),
                "O-6.17 vuruş 180 + zehir 3 s × 20; düşüşler: " + string.Join(", ", drops));
        }

        /// <summary>O-6.17 koni 8 m (§6.3: mesafe boss merkezinden): eksende merkezden 7,5 m'deki oyuncu vurulur.</summary>
        [UnityTest]
        public IEnumerator O_6_17_ZehirNefesi_Koni_8m_MerkezdenOlculur()
        {
            var drops = new List<int>();
            yield return PoisonBreathAt(centerDistM: 7.5f, offAxisDeg: 0f, drops);
            Assert.That(drops, Is.Not.Empty, "O-6.17 merkezden 7,5 m'deki oyuncu konide değil (koni 8 m olmalı)");
            Assert.That(drops[0], Is.GreaterThanOrEqualTo(180), "O-6.17 Zehir Nefesi vuruşu canı düşürmedi");
        }

        /// <summary>O-6.17 koni 60° TAM açı (§6.3: eksenden ±30°): 24°'deki oyuncu vurulur, 36°'deki vurulmaz.</summary>
        [UnityTest]
        public IEnumerator O_6_17_ZehirNefesi_Koni_60_TamAci()
        {
            var inside = new List<int>();
            yield return PoisonBreathAt(centerDistM: 5f, offAxisDeg: 24f, inside);
            Assert.That(inside, Is.Not.Empty, "O-6.17 eksenden 24°'deki oyuncu vurulmadı (yarı açı 30° olmalı)");

            var outside = new List<int>();
            yield return PoisonBreathAt(centerDistM: 5f, offAxisDeg: 36f, outside);
            Assert.That(outside, Is.Empty,
                "O-6.17 eksenden 36°'deki oyuncu vuruldu (60° tam açı: eksenden ±30°); düşüşler: " + string.Join(", ", outside));
        }

        /// <summary>
        /// Boss yalnız Zehir Nefesi seçer. Ön işaret başlayınca (boss o an oyuncuya döner, sonra dönmez) oyuncu boss
        /// merkezinden <paramref name="centerDistM"/> m, bakış ekseninden <paramref name="offAxisDeg"/>° uzağa kayar;
        /// vuruştan sonra oyuncu canındaki düşüşler toplanır.
        /// </summary>
        IEnumerator PoisonBreathAt(float centerDistM, float offAxisDeg, List<int> drops)
        {
            ParkAll();
            // Önceki senaryonun zehri bitmeden yeni ölçüm başlamaz.
            float poisonEnd = Time.realtimeSinceStartup + PoisonClearTimeoutSec;
            while (_md._playerStatus.Board.TryGet(StatusKind.Poison, out _, out _, out _)
                   && Time.realtimeSinceStartup < poisonEnd)
                yield return null;
            BossDirector bossDir = _md.MechanicsBossDirector;
            Assert.That(bossDir, Is.Not.Null, "BossDirector");
            bossDir.enabled = true;
            var reactor = _boss.GetComponent<BossReactorController>();
            if (reactor != null)
                reactor.enabled = true;
            bossDir.BindTargets(null);
            ForceAttackKinds(bossDir, BossAttackKind.FireCone);

            Vector3 bossPos = bossDir.transform.position;
            Vector3 toCenter = Flat(-bossPos);
            Vector3 dir = toCenter.sqrMagnitude > 1f ? toCenter.normalized : Flat(bossDir.transform.forward).normalized;
            Place(_player, bossPos + dir * (_physics.BodyRadius(_boss) + _physics.BodyRadius(_player) + 1.2f));
            _player.rotation = Quaternion.LookRotation(-dir);
            Physics.SyncTransforms();

            PlayerVitalsHost vitals = AuthoredPlayerVitals();
            vitals.ApplyHeal(vitals.MaxHp);
            bool moved = false;
            bool struck = false;
            void OnWindup(BossAttackKind k)
            {
                if (k != BossAttackKind.FireCone || moved)
                    return;
                moved = true;
                Vector3 axis = Flat(bossDir.transform.forward).normalized;
                Vector3 to = Quaternion.Euler(0f, offAxisDeg, 0f) * axis;
                Place(_player, bossDir.transform.position + to * centerDistM);
                _player.rotation = Quaternion.LookRotation(-to);
                Physics.SyncTransforms();
            }
            void OnStruck(BossAttackKind k)
            {
                if (k == BossAttackKind.FireCone && moved)
                    struck = true;
            }
            bossDir.AttackWindupStarted += OnWindup;
            bossDir.AttackStruck += OnStruck;
            try
            {
                int prev = vitals.Hp;
                float end = Time.realtimeSinceStartup + BossStrikeTimeoutSec;
                float stopAt = float.MaxValue;
                while (Time.realtimeSinceStartup < end && Time.realtimeSinceStartup < stopAt)
                {
                    yield return null;
                    if (struck && stopAt == float.MaxValue)
                        stopAt = Time.realtimeSinceStartup + SettleAfterHitSec;
                    int hp = vitals.Hp;
                    if (hp < prev)
                        drops.Add(prev - hp);
                    prev = hp;
                }
            }
            finally
            {
                bossDir.AttackWindupStarted -= OnWindup;
                bossDir.AttackStruck -= OnStruck;
            }
            FreezeBoss();
            Assert.That(struck, Is.True, "Zehir Nefesi vurmadı (ön işaret ya da vuruş görülmedi)");
        }

        [UnityTest]
        public IEnumerator O_6_18_Sicrayis_OyuncudanDuser_140()
        {
            var drops = new List<int>();
            yield return BossAttacksPlayer(BossAttackKind.Pounce, toward: 8f, drops);
            Assert.That(drops, Is.Not.Empty, "Sıçrayış oyuncuya isabet etmedi");
            Assert.That(drops[0], Is.EqualTo(140), "O-6.18 Sıçrayış hasarı; düşüşler: " + string.Join(", ", drops));
        }

        /// <summary>Dilimde panelin "Oyuncu can tavanı" kaydırıcısı, SIFIRLA ve AĞIR hazır ayarı canı değiştirmez.</summary>
        [UnityTest]
        public IEnumerator O_13_1_Dilimde_PanelVeHazirAyar_CaniDegistirmez()
        {
            FreezeBoss();
            GuardTuningFile();
            TuningPanelHud panel = Object.FindAnyObjectByType<TuningPanelHud>(FindObjectsInactive.Include);
            Assert.That(panel, Is.Not.Null, "TuningPanelHud");
            PlayerVitalsHost vitals = AuthoredPlayerVitals();
            int maxHp = Mathf.RoundToInt(_catalog.Catalog.Scale.PlayerMaxHp);

            Transform row = panel.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == PanelHpRow);
            Assert.That(row, Is.Not.Null, PanelHpRow);
            Slider slider = row.GetComponentInChildren<Slider>(true);
            // O-17.8 (Y10): dilimde kaydırıcı gri ve dinleyicisiz; değer yazılsa da can değişmez ve [V4-FALLBACK] yazılmaz.
            Assert.That(slider.interactable, Is.False, "dilimde can kaydırıcısı gri değil");
            slider.value = Mathf.Approximately(slider.value, 40f) ? 30f : 40f;
            yield return null;
            Assert.That(vitals.MaxHp, Is.EqualTo(maxHp), "panel kaydırıcısı dilimde canı değiştirdi");
            Assert.That(_errors.Where(e => PanelHpFallback.IsMatch(e)), Is.Empty, "gri kaydırıcı [V4-FALLBACK] yazdırdı");

            LogAssert.Expect(LogType.Error, PanelHpFallback);
            PanelButton(panel, "SIFIRLA").onClick.Invoke();
            yield return null;
            Assert.That(vitals.MaxHp, Is.EqualTo(maxHp), "SIFIRLA dilimde canı değiştirdi");

            LogAssert.Expect(LogType.Error, PanelHpFallback);
            PanelButton(panel, "AĞIR").onClick.Invoke();
            yield return null;
            Assert.That(vitals.MaxHp, Is.EqualTo(maxHp), "AĞIR hazır ayarı dilimde canı değiştirdi");

            _errors.RemoveAll(e => PanelHpFallback.IsMatch(e));
        }

        bool _tuningFileGuarded;
        byte[] _tuningFileBytes;

        /// <summary>Panel değişikliği geliştiricinin tuning.json'una kaydedilmesin: test sonunda dosya geri yüklenir.</summary>
        void GuardTuningFile()
        {
            string path = TuningConfig.FilePath;
            _tuningFileBytes = File.Exists(path) ? File.ReadAllBytes(path) : null;
            _tuningFileGuarded = true;
        }

        /// <summary>Test yarıda kesilse de çalışır (iteratörün finally'si kesilen UnityTest'te çalışmaz).</summary>
        [TearDown]
        public void RestoreTuningFile()
        {
            if (!_tuningFileGuarded)
                return;
            _tuningFileGuarded = false;
            TuningPanelHud panel;
            while ((panel = Object.FindAnyObjectByType<TuningPanelHud>(FindObjectsInactive.Include)) != null)
                Object.DestroyImmediate(panel.gameObject);
            string path = TuningConfig.FilePath;
            if (_tuningFileBytes != null)
                File.WriteAllBytes(path, _tuningFileBytes);
            else if (File.Exists(path))
                File.Delete(path);
        }

        static Button PanelButton(TuningPanelHud panel, string label)
        {
            Button b = panel.GetComponentsInChildren<Button>(true).FirstOrDefault(x => x.name == "Button_" + label);
            Assert.That(b, Is.Not.Null, "panel düğmesi " + label);
            return b;
        }

        /// <summary>
        /// Boss yalnız <paramref name="kind"/> saldırısını seçer ve yalnız oyuncuyu hedefler; oyuncu boss gövde kenarından
        /// <paramref name="toward"/> m uzakta, boss'a dönük durur, dodge'a basmaz. İlk isabetten sonra kısa bekleyip boss durur.
        /// <paramref name="drops"/>: kare kare oyuncu canındaki her düşüş.
        /// </summary>
        IEnumerator BossAttacksPlayer(
            BossAttackKind kind,
            float toward,
            List<int> drops,
            System.Action afterHit = null,
            float waitAfterHitSec = SettleAfterHitSec)
        {
            ParkAll();
            BossDirector bossDir = _md.MechanicsBossDirector;
            Assert.That(bossDir, Is.Not.Null, "BossDirector");
            bossDir.BindTargets(null);
            ForceAttackKinds(bossDir, kind);

            Vector3 bossPos = _boss.position;
            Vector3 toCenter = Flat(-bossPos);
            Vector3 dir = toCenter.sqrMagnitude > 1f ? toCenter.normalized : Flat(_boss.forward).normalized;
            float bossR = _physics.BodyRadius(_boss);
            Place(_player, bossPos + dir * (bossR + _physics.BodyRadius(_player) + toward));
            _player.rotation = Quaternion.LookRotation(-dir);
            Physics.SyncTransforms();

            PlayerVitalsHost vitals = AuthoredPlayerVitals();
            vitals.ApplyHeal(vitals.MaxHp);
            Assert.That(vitals.Hp, Is.EqualTo(vitals.MaxHp), "oyuncu dolu canla başlamıyor");

            int prev = vitals.Hp;
            float end = Time.realtimeSinceStartup + BossStrikeTimeoutSec;
            float stopAt = float.MaxValue;
            while (Time.realtimeSinceStartup < end && Time.realtimeSinceStartup < stopAt)
            {
                yield return null;
                int hp = vitals.Hp;
                if (hp < prev)
                {
                    drops.Add(prev - hp);
                    if (stopAt == float.MaxValue)
                    {
                        afterHit?.Invoke();
                        stopAt = Time.realtimeSinceStartup + waitAfterHitSec;
                    }
                }
                prev = hp;
            }
            FreezeBoss();
        }

        static void ForceAttackKinds(BossDirector bossDir, BossAttackKind kind)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            foreach (string field in new[] { "_phase1AttackKinds", "_phase2AttackKinds" })
            {
                FieldInfo f = typeof(BossDirector).GetField(field, flags);
                Assert.That(f, Is.Not.Null, "BossDirector." + field);
                f.SetValue(bossDir, new[] { kind });
            }
        }
    }
}
