using System;
using System.Collections;
using System.Linq;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.EditMode
{
    /// <summary>
    /// Prototype sahnesi, GameTuning'de RuleEngineV4.Enabled + SliceScene açıkken Play modunda
    /// bayraklar kapalı oynatıma göre yeni konsol hatası yazmamalı. Bayraklar yalnız bellekteki
    /// sahnede değişir; sahne kaydedilmez. Önceden var olan hatalar TestContext çıktısına yazılır.
    /// </summary>
    public sealed class RuleEngineV4PrototypeSceneEditModeTests
    {
        const string ScenePath = "Assets/Scenes/Prototype.unity";
        const string HostType = "Dovus.Game.Composition.GameBootstrapHost, Dovus.Game.Composition";
        const string CaptureKey = "Dovus.RuleEngineV4.PlayCapture";
        const string LogKey = "Dovus.RuleEngineV4.PlayLog";
        const string BaselineKey = "Dovus.RuleEngineV4.PlayBaseline";
        const string V4OnKey = "Dovus.RuleEngineV4.PlayV4On";
        const double PlaySec = 6.0;

        [InitializeOnLoadMethod]
        static void HookAfterDomainReload()
        {
            if (SessionState.GetBool(CaptureKey, false))
                Hook();
        }

        static void Hook()
        {
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (!SessionState.GetBool(CaptureKey, false))
                return;
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;
            SessionState.SetString(LogKey, SessionState.GetString(LogKey, string.Empty) + $"[{type}] {msg}\n");
        }

        static void SetFlags(bool on)
        {
            Type hostType = Type.GetType(HostType);
            Assert.That(hostType, Is.Not.Null, HostType);
            Object host = Object.FindAnyObjectByType(hostType);
            Assert.That(host, Is.Not.Null, "Prototype sahnesinde GameBootstrapHost yok");
            var so = new SerializedObject(host);
            SerializedProperty enabled = so.FindProperty("_tuning.RuleEngineV4.Enabled");
            SerializedProperty slice = so.FindProperty("_tuning.RuleEngineV4.SliceScene");
            Assert.That(enabled, Is.Not.Null, "_tuning.RuleEngineV4.Enabled");
            Assert.That(slice, Is.Not.Null, "_tuning.RuleEngineV4.SliceScene");
            enabled.boolValue = on;
            slice.boolValue = on;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BeginCapture()
        {
            SessionState.SetString(LogKey, string.Empty);
            SessionState.SetBool(CaptureKey, true);
            Hook();
            LogAssert.ignoreFailingMessages = true;
        }

        static string EndCapture()
        {
            SessionState.SetBool(CaptureKey, false);
            Application.logMessageReceived -= OnLog;
            string log = SessionState.GetString(LogKey, string.Empty);
            SessionState.EraseString(LogKey);
            return log;
        }

        static string[] Lines(string log) =>
            log.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();

        [UnityTest]
        public IEnumerator PrototypeScene_V4FlagsOn_PlayModeAddsNoConsoleErrors()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SetFlags(false);
            BeginCapture();
            yield return new EnterPlayMode();
            double offEnd = EditorApplication.timeSinceStartup + PlaySec;
            while (EditorApplication.timeSinceStartup < offEnd)
                yield return null;
            yield return new ExitPlayMode();
            SessionState.SetString(BaselineKey, EndCapture());

            SetFlags(true);
            BeginCapture();
            yield return new EnterPlayMode();
            double onEnd = EditorApplication.timeSinceStartup + PlaySec;
            while (EditorApplication.timeSinceStartup < onEnd)
                yield return null;
            SessionState.SetBool(V4OnKey, RuleEngineV4Feature.Enabled);
            yield return new ExitPlayMode();
            string withV4 = EndCapture();
            SetFlags(false);
            RuleEngineV4Feature.Enabled = false;

            string[] baseline = Lines(SessionState.GetString(BaselineKey, string.Empty));
            bool v4On = SessionState.GetBool(V4OnKey, false);
            SessionState.EraseString(BaselineKey);
            SessionState.EraseBool(V4OnKey);
            string[] added = Lines(withV4).Except(baseline).ToArray();
            foreach (string line in baseline)
                TestContext.Out.WriteLine("bayraklar kapalıyken de var: " + line);

            Assert.That(v4On, Is.True, "Play modunda RuleEngineV4Feature.Enabled açılmadı");
            Assert.That(added, Is.Empty, "v4 bayraklarıyla yeni konsol hatası:\n" + string.Join("\n", added));
        }
    }
}
