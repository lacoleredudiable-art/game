using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
using Dovus.Game.Skills;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    public sealed class PrototypeSceneSmokeTests
    {
        readonly List<string> _collected = new();

        [UnityTest]
        public IEnumerator PrototypeScene_BootsAndRunsFiveSeconds()
        {
            LogAssert.ignoreFailingMessages = true;

            void OnLog(string condition, string stackTrace, LogType type)
            {
                if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                    return;

                string line = type == LogType.Exception || type == LogType.Assert
                    ? $"[{type}] {condition}\n{stackTrace}"
                    : $"[{type}] {condition}";
                _collected.Add(line);
            }

            Application.logMessageReceived += OnLog;

            SceneManager.LoadScene("Prototype");
            yield return new WaitForSecondsRealtime(5f);

            Application.logMessageReceived -= OnLog;

            Assert.That(UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>(), Is.Not.Null, "PrototypeBootstrap");
            Assert.That(UnityEngine.Object.FindAnyObjectByType<ManifestationDirector>(), Is.Not.Null, "ManifestationDirector");
            Assert.That(UnityEngine.Object.FindAnyObjectByType<BossDirector>(), Is.Not.Null, "BossDirector");
            Assert.That(UnityEngine.Object.FindAnyObjectByType<PlayerVitals>(), Is.Not.Null, "PlayerVitals");
            Assert.That(UnityEngine.Object.FindAnyObjectByType<HexagonInput>(), Is.Not.Null, "HexagonInput");

            int exceptionCount = 0;
            foreach (string line in _collected)
            {
                if (line.StartsWith("[Exception]", StringComparison.Ordinal) ||
                    line.StartsWith("[Assert]", StringComparison.Ordinal))
                    exceptionCount++;
                else if (line.StartsWith("[Error]", StringComparison.Ordinal))
                    TestContext.Out.WriteLine(line);
            }

            WriteSmokeLog(_collected);

            Assert.That(exceptionCount, Is.EqualTo(0), "PlayMode smoke: Exception/Assert log count");
        }

        static void WriteSmokeLog(IReadOnlyList<string> lines)
        {
            string path = ResolveSmokeOutPath();
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var sb = new StringBuilder();
            foreach (string line in lines)
                sb.AppendLine(line);
            File.WriteAllText(path, sb.ToString());
        }

        static string ResolveSmokeOutPath()
        {
            string fromArg = GetCommandLineArgValue("-smokeOut");
            if (!string.IsNullOrWhiteSpace(fromArg))
                return Path.GetFullPath(fromArg);

            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "playmode-smoke.txt"));
        }

        static string GetCommandLineArgValue(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.Ordinal))
                    return args[i + 1];
            }

            return null;
        }
    }
}
