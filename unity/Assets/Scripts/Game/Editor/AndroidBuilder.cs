#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
        const string ReleaseApkName = "dovus-prototip-release.apk";

        /// <summary>
        /// K2 (denetim C): debug kapısının define'ı. Dev APK bu define ile derlenir (DebugConfig açık:
        /// dev HP, test panelleri, tuning.json, DevLog). Release APK'da yok → hepsi kapalı.
        /// </summary>
        public const string DebugDefine = "DOVUS_DEBUG";

        /// <summary>Dev APK (eski menü adı korunur): Development + DOVUS_DEBUG.</summary>
        [MenuItem("Dovus/Build Android APK")]
        public static void BuildFromMenu()
        {
            BuildReport report = Build(DefaultOutputPath(), release: false);
            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(report.summary.outputPath);
        }

        /// <summary>K2: release APK — Development yok, DOVUS_DEBUG yok (normal can, panel yok).</summary>
        [MenuItem("Dovus/Build Android APK (release)")]
        public static void BuildReleaseFromMenu()
        {
            BuildReport report = Build(DefaultOutputPath(release: true), release: true);
            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(report.summary.outputPath);
        }

        /// <summary>
        /// Batchmode girişi: `-executeMethod Dovus.Game.EditorTools.AndroidBuilder.BuildFromCommandLine`.
        /// `-dovusRelease` release APK alır; `-dovusOutput <yol>` çıktı yolu.
        /// </summary>
        public static void BuildFromCommandLine()
        {
            bool release = HasArg("-dovusRelease");
            string output = ArgValue("-dovusOutput") ?? DefaultOutputPath(release);
            BuildReport report = Build(output, release);
            bool ok = report.summary.result == BuildResult.Succeeded;

            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>Otomasyon girişi: verilen yola dev build alır, başarıyı döner (menü/CLI yan etkisi yok).</summary>
        public static bool BuildTo(string outputPath)
        {
            return BuildTo(outputPath, release: false);
        }

        /// <summary>Otomasyon girişi: release=true → release APK (K2).</summary>
        public static bool BuildTo(string outputPath, bool release)
        {
            return Build(outputPath, release).summary.result == BuildResult.Succeeded;
        }

        public static string DefaultOutputPath() => DefaultOutputPath(release: false);

        public static string DefaultOutputPath(bool release)
        {
            // Application.dataPath = <repo>/unity/Assets
            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            return Path.Combine(repoRoot, OutputDirRelativeToRepo, release ? ReleaseApkName : ApkName);
        }

        /// <summary>K2: build türüne göre seçenekler ve ek define'lar (saf; test edilebilir).</summary>
        public static BuildOptions OptionsFor(bool release) =>
            release
                ? BuildOptions.CleanBuildCache
                : BuildOptions.Development | BuildOptions.CleanBuildCache;

        public static string[] DefinesFor(bool release) =>
            release ? Array.Empty<string>() : new[] { DebugDefine };

        /// <summary>
        /// Sahne koddan kuruluyor (AGENTS kural 2), yani hiçbir materyal bir asset'te durmuyor:
        /// materyaller çalışma anında `Shader.Find` ile yaratılıyor. Player build'i yalnızca
        /// asset'lerden REFERANS EDİLEN shader'ları paketler — `Shader.Find` telefonda null döner
        /// ve dünya macenta çıkar (T11 1. oturumda tam bu oldu). Bu yüzden çalışma anında aranan
        /// her shader "Always Included" listesine yazılır.
        /// </summary>
        static readonly string[] RuntimeShaders =
        {
            "Universal Render Pipeline/Lit",     // arena, oyuncu, boss (PrototypeBootstrap)
            "Universal Render Pipeline/Unlit",   // yedek yol
            "Sprites/Default",                   // mürekkep, telegraf, hayalet, iz, tezahür
            "Universal Render Pipeline/Particles/Unlit", // FeelVfx, CastFlash, LivingEffectView, LavaDecor, BillboardVfx
        };

        static BuildReport Build(string outputPath, bool release)
        {
            ApplyPlayerSettings();
            EnsureAlwaysIncludedShaders();

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
                // Dev: ölçüm turu geliştirme build'i istiyor (görev metni). Script debugging ve
                // profiler bağlantısı AÇILMADI: ikisi de kare süresini kendileri şişirip
                // "60 fps'e yakın mı" sorusunu ölçülemez hale getirir.
                // Release (K2): Development yok, DOVUS_DEBUG yok → DebugConfig kapalı.
                // CleanBuildCache: incremental Bee bazen eski Data files bırakıyor (telefon APK).
                options = OptionsFor(release),
                extraScriptingDefines = DefinesFor(release)
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[T11] APK hazır ({(release ? "release" : "dev")}): {summary.outputPath} " +
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

            // §2 girdi düzeni yatay: sol yarı çubuk, sağ yarı altıgen. Dikey çevrilirse iki
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

        static void EnsureAlwaysIncludedShaders()
        {
            var settings = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/GraphicsSettings.asset");
            if (settings == null)
            {
                Debug.LogWarning("[T11] GraphicsSettings.asset okunamadı; shader listesi güncellenmedi.");
                return;
            }

            var serialized = new SerializedObject(settings);
            SerializedProperty list = serialized.FindProperty("m_AlwaysIncludedShaders");
            if (list == null || !list.isArray)
            {
                Debug.LogWarning("[T11] m_AlwaysIncludedShaders bulunamadı; shader listesi güncellenmedi.");
                return;
            }

            var present = new List<UnityEngine.Object>();
            for (int i = 0; i < list.arraySize; i++)
                present.Add(list.GetArrayElementAtIndex(i).objectReferenceValue);

            bool changed = false;
            foreach (string name in RuntimeShaders)
            {
                Shader shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning($"[T11] Shader bulunamadı, listeye eklenemedi: {name}");
                    continue;
                }

                if (present.Contains(shader))
                    continue;

                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                list.GetArrayElementAtIndex(index).objectReferenceValue = shader;
                present.Add(shader);
                changed = true;
                Debug.Log($"[T11] Always Included Shaders'a eklendi: {name}");
            }

            if (changed)
            {
                serialized.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }

        static bool HasArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
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
