using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.EditMode
{
    /// <summary>
    /// Prototype sahnesi, GameTuning'de SliceScene açıkken Play modunda
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
        const string SliceOnKey = "Dovus.RuleEngineV4.PlaySliceOn";
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

        static void SetSliceScene(bool on)
        {
            Type hostType = Type.GetType(HostType);
            Assert.That(hostType, Is.Not.Null, HostType);
            Object host = Object.FindAnyObjectByType(hostType);
            Assert.That(host, Is.Not.Null, "Prototype sahnesinde GameBootstrapHost yok");
            var so = new SerializedObject(host);
            SerializedProperty slice = so.FindProperty("_tuning.RuleEngineV4.SliceScene");
            Assert.That(slice, Is.Not.Null, "_tuning.RuleEngineV4.SliceScene");
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
        public IEnumerator PrototypeScene_SliceSceneOn_PlayModeAddsNoConsoleErrors()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SetSliceScene(false);
            BeginCapture();
            yield return new EnterPlayMode();
            double offEnd = EditorApplication.timeSinceStartup + PlaySec;
            while (EditorApplication.timeSinceStartup < offEnd)
                yield return null;
            yield return new ExitPlayMode();
            SessionState.SetString(BaselineKey, EndCapture());

            SetSliceScene(true);
            BeginCapture();
            yield return new EnterPlayMode();
            double onEnd = EditorApplication.timeSinceStartup + PlaySec;
            while (EditorApplication.timeSinceStartup < onEnd)
                yield return null;
            Type hostType = Type.GetType(HostType);
            Object host = hostType != null ? Object.FindAnyObjectByType(hostType) : null;
            bool sliceOn = false;
            if (host != null)
            {
                var so = new SerializedObject(host);
                SerializedProperty slice = so.FindProperty("_tuning.RuleEngineV4.SliceScene");
                sliceOn = slice != null && slice.boolValue;
            }
            SessionState.SetBool(SliceOnKey, sliceOn);
            yield return new ExitPlayMode();
            string withSlice = EndCapture();
            SetSliceScene(false);

            string[] baseline = Lines(SessionState.GetString(BaselineKey, string.Empty));
            bool sliceSceneOn = SessionState.GetBool(SliceOnKey, false);
            SessionState.EraseString(BaselineKey);
            SessionState.EraseBool(SliceOnKey);
            string[] added = Lines(withSlice).Except(baseline).ToArray();
            foreach (string line in baseline)
                TestContext.Out.WriteLine("SliceScene kapalıyken de var: " + line);

            Assert.That(sliceSceneOn, Is.True, "Play modunda SliceScene açılmadı");
            Assert.That(added, Is.Empty, "SliceScene açıkken yeni konsol hatası:\n" + string.Join("\n", added));
        }
    }
}
