#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>
    /// T11: Android APK üretimi. Ayarlar burada kodla yazılıyor ki build tek tıkla (ya da
    /// batchmode'da tek komutla) tekrarlanabilir olsun; Build Settings penceresinde elle
    /// tıklanan bir kutu ertesi gün kimse hatırlamadığı için başka bir sonuç verir.
    /// </summary>
    public static class AndroidBuilder
    {
        const string ApplicationId = "com.dovus.prototip";
        const string OutputDirRelativeToRepo = "build/android";
        const string ApkName = "dovus-prototip.apk";

        [MenuItem("Dovus/Build Android APK")]
        public static void BuildFromMenu()
        {
            BuildReport report = Build(DefaultOutputPath());
            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(report.summary.outputPath);
        }

        /// <summary>Batchmode girişi: `-executeMethod Dovus.Game.EditorTools.AndroidBuilder.BuildFromCommandLine`.</summary>
        public static void BuildFromCommandLine()
        {
            string output = ArgValue("-dovusOutput") ?? DefaultOutputPath();
            BuildReport report = Build(output);
            bool ok = report.summary.result == BuildResult.Succeeded;

            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>Otomasyon girişi: verilen yola build alır, başarıyı döner (menü/CLI yan etkisi yok).</summary>
        public static bool BuildTo(string outputPath)
        {
            return Build(outputPath).summary.result == BuildResult.Succeeded;
        }

        public static string DefaultOutputPath()
        {
            // Application.dataPath = <repo>/unity/Assets
            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            return Path.Combine(repoRoot, OutputDirRelativeToRepo, ApkName);
        }

        static BuildReport Build(string outputPath)
        {
            ApplyPlayerSettings();

            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new InvalidOperationException(
                    "Build Settings'te açık sahne yok. Dovus → Create Prototype Scene ile sahneyi kur.");

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                // Ölçüm turu geliştirme build'i istiyor (görev metni). Script debugging ve
                // profiler bağlantısı AÇILMADI: ikisi de kare süresini kendileri şişirip
                // "60 fps'e yakın mı" sorusunu ölçülemez hale getirir.
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[T11] APK hazır: {summary.outputPath} " +
                          $"({summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime.TotalSeconds:0} sn)");
            }
            else
            {
                Debug.LogError($"[T11] Build başarısız: {summary.result}, {summary.totalErrors} hata.");
            }

            return report;
        }

        static void ApplyPlayerSettings()
        {
            var android = NamedBuildTarget.Android;

            PlayerSettings.companyName = "Dovus";
            PlayerSettings.productName = "Dovus Prototip";
            PlayerSettings.SetApplicationIdentifier(android, ApplicationId);

            // Görev metni: IL2CPP + ARM64. Mono/ARMv7 ölçümü telefonun gerçek performansını
            // temsil etmez, üstelik Play Store ARM64 istiyor.
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // Development build'in IL2CPP tarafı varsayılan olarak Debug derlenir; bu, C++
            // optimizasyonu kapalı bir oyun demek. Kare bütçesini ölçeceğimiz için Release.
            PlayerSettings.SetIl2CppCompilerConfiguration(android, Il2CppCompilerConfiguration.Release);

            // 24 denendi, Unity 6 sessizce 25'e çekti (bu sürümün tabanı). Kodda gerçekten
            // ne shipliyorsak o yazsın diye 25.
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.useCustomKeystore = false;

            // §2 girdi düzeni yatay: sol yarı çubuk, sağ yarı beşgen. Dikey çevrilirse iki
            // yarı da başparmak yayının dışına çıkar.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Kare süresi tavanı koddan yazılıyor (PrototypeBootstrap.ApplyFrameRateTarget);
            // burada yalnızca 32 bit ekran ve tek APK garantisi.
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;

            AssetDatabase.SaveAssets();
        }

        static string ArgValue(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
        }
    }
}
#endif
