#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>Telefon test APK: EditorBuildSettings sahneleri, development + DOVUS_DEBUG.</summary>
    public static class BuildApk
    {
        const string OutputRoot = @"C:\Users\lacol\_cleanup\apk";

        [MenuItem("Dovus/Build Grip Test APK (Android)")]
        public static void BuildGripTestApkFromMenu()
        {
            string path = BuildGripTestApk();
            if (!string.IsNullOrEmpty(path))
                EditorUtility.RevealInFinder(path);
        }

        /// <returns>APK yolu veya null.</returns>
        public static string BuildGripTestApk()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[BuildApk] Android Build Support yüklü değil.");
                return null;
            }

            string hash = RunGitShortHash();
            Directory.CreateDirectory(OutputRoot);
            string output = Path.Combine(OutputRoot, $"mobilegame-{hash}.apk");
            if (!AndroidBuilder.BuildTo(output, release: false))
            {
                Debug.LogError("[BuildApk] Build başarısız — konsolu kontrol edin.");
                return null;
            }

            long bytes = new FileInfo(output).Length;
            Debug.Log($"[BuildApk] APK hazır: {output} ({bytes / (1024f * 1024f):0.0} MB)");
            return output;
        }

        static string RunGitShortHash()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("git", "rev-parse --short HEAD")
                {
                    WorkingDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..")),
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                string line = proc?.StandardOutput.ReadLine()?.Trim();
                proc?.WaitForExit(5000);
                return string.IsNullOrEmpty(line) ? "local" : line;
            }
            catch
            {
                return "local";
            }
        }
    }
}
#endif
