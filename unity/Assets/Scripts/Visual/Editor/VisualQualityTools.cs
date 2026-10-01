using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dovus.Visual.EditorTools
{
    /// <summary>
    /// Görüntü kalitesi araçları (deneme sahnesi PR'ı):
    /// 1) Mobil AA + kalite düzeltmesi: URP asset'inde MSAA 4x ve render scale 1.0; kalite seviyelerinde
    ///    "Very Low"un karakteri bozan ayarları (1 kemik skin ağırlığı, yarım çözünürlük doku, lodBias 0.3)
    ///    düzeltilir. Proje ayarı değişikliğidir: ayrı commit olarak işlenir.
    /// 2) Yalnız deneme sahnesini içeren Android APK (ayrı uygulama kimliği, prototipin üstüne yazmaz).
    /// </summary>
    public static class VisualQualityTools
    {
        const string DenemeAppId = "com.dovus.deneme";
        const string DenemeProductName = "Dovus Deneme";

        [MenuItem("Dovus/Visual/Mobil AA + kalite duzeltmesi (proje ayari)")]
        static void ApplyMenu() => Debug.Log(ApplyMobileQualityFix());

        [MenuItem("Dovus/Visual/Deneme Sahnesi - Android APK al")]
        static void ApkMenu() => Debug.Log(BuildDenemeApk(null, false));

        public static string ApplyMobileQualityFix()
        {
            var log = new List<string>();
            var assets = new HashSet<UniversalRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset def) assets.Add(def);
            for (int i = 0; i < QualitySettings.names.Length; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset q) assets.Add(q);
            foreach (UniversalRenderPipelineAsset a in assets)
            {
                log.Add($"{a.name}: MSAA {a.msaaSampleCount}->4, renderScale {a.renderScale:0.##}->1");
                a.msaaSampleCount = 4;
                a.renderScale = 1f;
                EditorUtility.SetDirty(a);
            }

            UnityEngine.Object[] qsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (qsAssets != null && qsAssets.Length > 0)
            {
                var so = new SerializedObject(qsAssets[0]);
                SerializedProperty levels = so.FindProperty("m_QualitySettings");
                for (int i = 0; levels != null && i < levels.arraySize; i++)
                {
                    SerializedProperty lv = levels.GetArrayElementAtIndex(i);
                    string name = lv.FindPropertyRelative("name")?.stringValue;
                    SerializedProperty skin = lv.FindPropertyRelative("skinWeights");
                    SerializedProperty mip = lv.FindPropertyRelative("globalTextureMipmapLimit");
                    SerializedProperty lod = lv.FindPropertyRelative("lodBias");
                    SerializedProperty aniso = lv.FindPropertyRelative("anisotropicTextures");
                    var changes = new List<string>();
                    if (skin != null && skin.intValue < 4) { changes.Add($"skin {skin.intValue}->4"); skin.intValue = 4; }
                    if (mip != null && mip.intValue != 0) { changes.Add($"mipLimit {mip.intValue}->0"); mip.intValue = 0; }
                    if (lod != null && lod.floatValue < 1f) { changes.Add($"lodBias {lod.floatValue:0.##}->1"); lod.floatValue = 1f; }
                    if (aniso != null && aniso.intValue < 1) { changes.Add("aniso 0->1"); aniso.intValue = 1; }
                    if (changes.Count > 0) log.Add($"quality '{name}': " + string.Join(", ", changes));
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else log.Add("WARN: QualitySettings.asset not loadable");

            AssetDatabase.SaveAssets();
            return "OK mobile quality fix\n" + string.Join("\n", log);
        }

        /// <summary>
        /// Yalnız DenemeSahnesi'ni içeren APK. Aktif hedef Android değilse ve allowTargetSwitch false ise
        /// platform değiştirmez (uzun reimport, başka iş akışını bozar) ve "SKIP" döner.
        /// Ürün adı/uygulama kimliği geçici değiştirilir, build sonunda geri yazılır.
        /// </summary>
        public static string BuildDenemeApk(string outputPath, bool allowTargetSwitch)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!allowTargetSwitch)
                    return "SKIP: active build target is " + EditorUserBuildSettings.activeBuildTarget + " (not Android); APK not built.";
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }
            if (string.IsNullOrEmpty(outputPath))
            {
                string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
                outputPath = Path.Combine(repoRoot, "build", "android", "dovus-deneme.apk");
            }
            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            NamedBuildTarget android = NamedBuildTarget.Android;
            string oldId = PlayerSettings.GetApplicationIdentifier(android);
            string oldName = PlayerSettings.productName;
            try
            {
                PlayerSettings.SetApplicationIdentifier(android, DenemeAppId);
                PlayerSettings.productName = DenemeProductName;
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { DenemeSahnesiBuilder.ScenePath },
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary s = report.summary;
                return s.result == BuildResult.Succeeded
                    ? $"OK APK {s.outputPath} ({s.totalSize / (1024f * 1024f):0.0} MB, {s.totalTime.TotalSeconds:0} s)"
                    : $"FAIL APK {s.result}, errors {s.totalErrors}";
            }
            catch (Exception e)
            {
                return "FAIL APK exception: " + e.Message;
            }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(android, oldId);
                PlayerSettings.productName = oldName;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
