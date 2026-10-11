using System.Collections;
using System.Collections.Generic;
using Dovus.Core.Boss;
using Dovus.Core.Damage;
using Dovus.Core.Data;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Composition;
using Dovus.Game.Composition.Builders;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using Dovus.Game.Skills.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>Dilim düzeltme E: bayrak kapalıyken eski ölçek (CombatScale, element JSON canı, sabit yaratık canı) aynen kalır.</summary>
    public sealed class RuleEngineV4ScaleFlagOffPlayModeTests
    {
        const float BootSettleSec = 0.5f;
        const string KnownDevToolsNoise = "FeelPlayVerifyController";

        readonly HashSet<GameObject> _preexisting = new();
        readonly List<string> _errors = new();
        GameBootstrapHost _host;
        ManifestationDirector _md;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!_preexisting.Contains(root))
                    Object.Destroy(root);
            }
            foreach (GameObject root in DontDestroyRoots())
            {
                if (!_preexisting.Contains(root))
                    Object.Destroy(root);
            }
            RuleEngineV4Feature.Enabled = false;
            yield return null;
            Application.logMessageReceived -= OnLog;
            Assert.That(_errors, Is.Empty, "konsol hatası:\n" + string.Join("\n", _errors));
        }

        void OnLog(string msg, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;
            if (stack != null && stack.Contains(KnownDevToolsNoise))
                return;
            _errors.Add($"[{type}] {msg}");
        }

        [UnityTest]
        public IEnumerator FlagOff_PlayerAllyBoss_KeepLegacyScale()
        {
            yield return Boot(sliceScene: false);
            PlayerVitalsHost vitals = _md.CachedPlayerVitals();
            vitals.SetDevHp(false);
            int legacyPlayer = LegacyPlayerHp();
            Assert.That(vitals.MaxHp, Is.EqualTo(legacyPlayer), "oyuncu canı eski ölçekte değil");
            Assert.That(_md.MechanicsAlly.MaxHp, Is.EqualTo(legacyPlayer), "AllyDummy canı eski ölçekte değil");
            Assert.That(_md.MechanicsBossVitals.MaxHp, Is.EqualTo(LegacyBossHp()), "boss canı eski ölçekte değil");

            vitals.ApplyHeal(vitals.MaxHp);
            int before = vitals.Hp;
            const float raw = 100f;
            _md._playerStatus.ApplyDamage(raw, dodgeable: false, pierceArmor: true);
            Assert.That(before - vitals.Hp, Is.EqualTo(CombatScale.MagnitudeInt(raw)), "bayrak kapalıyken CombatScale uygulanmadı");
        }

        [UnityTest]
        public IEnumerator FlagOff_SliceScene_MinionsKeepLegacyHp()
        {
            yield return Boot(sliceScene: true);
            foreach (string name in new[] { "SliceMinion1", "SliceMinion2", "SliceMinion3" })
            {
                GameObject go = GameObject.Find(name);
                Assert.That(go, Is.Not.Null, name);
                Assert.That(go.GetComponent<SliceLightMinionHost>().MaxHp,
                    Is.EqualTo(RuleEngineV4SliceBuilderDefaults.LegacyMinionMaxHp), name + " canı");
            }
            Assert.That(GameObject.Find("AllyDummy2").GetComponent<AllyDummyController>().MaxHp,
                Is.EqualTo(LegacyPlayerHp()), "AllyDummy2 canı eski ölçekte değil");
        }

        IEnumerator Boot(bool sliceScene)
        {
            // Dev araç gürültüsü OnLog'da süzülür; diğer hatalar TearDown'da düşer.
            LogAssert.ignoreFailingMessages = true;
            _errors.Clear();
            Application.logMessageReceived += OnLog;
            _preexisting.Clear();
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                _preexisting.Add(root);
            foreach (GameObject root in DontDestroyRoots())
                _preexisting.Add(root);

            var boot = new GameObject("RuleEngineV4ScaleFlagOffBoot");
            boot.SetActive(false);
            _host = boot.AddComponent<GameBootstrapHost>();
            _host.SceneTuning.RuleEngineV4.Enabled = false;
            _host.SceneTuning.RuleEngineV4.SliceScene = sliceScene;
            boot.SetActive(true);
            yield return new WaitForSecondsRealtime(BootSettleSec);

            Assert.That(RuleEngineV4Feature.Enabled, Is.False, "bayrak açık");
            _md = Object.FindAnyObjectByType<ManifestationDirector>();
            Assert.That(_md, Is.Not.Null, "ManifestationDirector");
            if (BuildSelectHud.IsOpen)
                Object.FindAnyObjectByType<BuildSelectHud>().SweepClose();
            if (_md.MechanicsBossDirector != null)
                _md.MechanicsBossDirector.enabled = false;
        }

        int LegacyPlayerHp() =>
            ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design)
                ? Mathf.Max(1, Mathf.RoundToInt(BossCombatProfile.FromDocument(design.Document).PlayerMaxHp))
                : Mathf.Max(1, CombatScale.MagnitudeInt(_host.SceneTuning.Player.PlayerMaxHp));

        static float LegacyBossHp() =>
            ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design)
                ? Mathf.Max(1f, BossCombatProfile.FromDocument(design.Document).BossMaxHp)
                : Mathf.Max(1f, CombatScale.Magnitude(new CombatTuning().Boss.MaxHp));

        static IEnumerable<GameObject> DontDestroyRoots()
        {
            var probe = new GameObject("RuleEngineV4ScaleProbe");
            Object.DontDestroyOnLoad(probe);
            GameObject[] roots = probe.scene.GetRootGameObjects();
            Object.DestroyImmediate(probe);
            foreach (GameObject go in roots)
            {
                if (go != null)
                    yield return go;
            }
        }
    }
}
