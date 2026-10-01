#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game.EditorTools
{
    public static class LookPresetsBuilder
    {
        const string LookFolder = "Assets/Resources/Look";
        const string UrpPath = "Assets/Settings/URP.asset";
        const string RendererPath = "Assets/Settings/UniversalRenderer.asset";
        const string QualityPath = "ProjectSettings/QualitySettings.asset";

        [MenuItem("Dovus/Look/Build Presets")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(LookFolder);
            BuildVolumeProfiles();
            ApplyMobileQualityFixes();
            EnsureUrpMsaa();
            EnsureSsaoFeature();
            FixGroundTextureImports();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LookPresets] Build Presets tamamlandı.");
        }

        static void BuildVolumeProfiles()
        {
            SaveProfile("Look_A_Esit", BuildBaseProfile());
            var b = BuildBaseProfile();
            SetBloomThreshold(b, 1.12f);
            AddCoolGrade(b);
            StrengthenB(b);
            SaveProfile("Look_B_Keskin", b);
            var c = BuildBaseProfile();
            SetBloomThreshold(c, 1.12f);
            AddCoolGrade(c);
            TuneC(c);
            SaveProfile("Look_C_Gelismis", c);
        }

        static VolumeProfile BuildBaseProfile()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.Override(-8f);

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.Override(0.15f);
            colorAdj.contrast.Override(-14f);
            colorAdj.saturation.Override(-35f);
            colorAdj.colorFilter.Override(new Color(0.95f, 0.975f, 1f));

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.65f);
            bloom.scatter.Override(0.55f);
            bloom.tint.Override(new Color(1f, 0.86f, 0.72f));

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.12f);
            vignette.smoothness.Override(0.45f);

            return profile;
        }

        static void SetBloomThreshold(VolumeProfile profile, float threshold)
        {
            if (profile.TryGet(out Bloom bloom))
                bloom.threshold.Override(threshold);
        }

        static void AddCoolGrade(VolumeProfile profile)
        {
            var smh = profile.Add<ShadowsMidtonesHighlights>(true);
            smh.shadows.Override(new Vector4(0.98f, 1f, 1.02f, 0f));
            smh.midtones.Override(new Vector4(0.96f, 0.98f, 1f, 0f));
            smh.highlights.Override(new Vector4(1f, 1f, 1f, 0f));
        }

        static void StrengthenB(VolumeProfile profile)
        {
            if (profile.TryGet(out ColorAdjustments colorAdj))
            {
                colorAdj.contrast.Override(-2f);
                colorAdj.postExposure.Override(0.06f);
                colorAdj.colorFilter.Override(new Color(0.92f, 0.96f, 1f));
            }

            if (profile.TryGet(out ShadowsMidtonesHighlights smh))
                smh.shadows.Override(new Vector4(0.9f, 0.94f, 1.03f, -0.1f));

            if (profile.TryGet(out Vignette vignette))
                vignette.intensity.Override(0.2f);
        }

        static void TuneC(VolumeProfile profile)
        {
            if (profile.TryGet(out ColorAdjustments colorAdj))
            {
                colorAdj.postExposure.Override(0.02f);
                colorAdj.contrast.Override(-6f);
            }
        }

        static void SaveProfile(string name, VolumeProfile profile)
        {
            string path = $"{LookFolder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            profile.name = name;
            AssetDatabase.CreateAsset(profile, path);
            foreach (VolumeComponent component in profile.components)
            {
                if (component == null)
                    continue;
                component.name = name + "_" + component.GetType().Name;
                component.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                if (!AssetDatabase.Contains(component))
                    AssetDatabase.AddObjectToAsset(component, profile);
            }
        }

        static void ApplyMobileQualityFixes()
        {
            var quality = AssetDatabase.LoadAllAssetsAtPath(QualityPath);
            if (quality == null || quality.Length == 0)
                return;

            var so = new SerializedObject(quality[0]);
            var settings = so.FindProperty("m_QualitySettings");
            if (settings == null || !settings.isArray)
                return;

            string[] names = { "Very Low", "Low", "Medium", "High" };
            for (int i = 0; i < settings.arraySize && i < names.Length; i++)
            {
                var entry = settings.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("name").stringValue != names[i])
                    continue;
                entry.FindPropertyRelative("skinWeights").intValue = 4;
                entry.FindPropertyRelative("globalTextureMipmapLimit").intValue = 0;
                entry.FindPropertyRelative("lodBias").floatValue = 1f;
                entry.FindPropertyRelative("anisotropicTextures").intValue = 1;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureUrpMsaa()
        {
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpPath);
            if (urp == null)
                return;
            urp.msaaSampleCount = 4;
            urp.renderScale = 1f;
            EditorUtility.SetDirty(urp);
        }

        static void EnsureSsaoFeature()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
                return;

            ScreenSpaceAmbientOcclusion ssao = null;
            foreach (var feature in renderer.rendererFeatures)
            {
                if (feature is ScreenSpaceAmbientOcclusion existing)
                {
                    ssao = existing;
                    break;
                }
            }

            if (ssao == null)
            {
                ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssao.name = "ScreenSpaceAmbientOcclusion";
                AssetDatabase.AddObjectToAsset(ssao, renderer);
                renderer.rendererFeatures.Add(ssao);
            }

            ssao.SetActive(false);
            var so = new SerializedObject(ssao);
            var down = so.FindProperty("m_Settings.m_Downsample");
            if (down != null)
                down.boolValue = true;
            var radius = so.FindProperty("m_Settings.m_Radius");
            if (radius != null)
                radius.floatValue = 0.22f;
            var intensity = so.FindProperty("m_Settings.m_Intensity");
            if (intensity != null)
                intensity.floatValue = 0.35f;
            var samples = so.FindProperty("m_Settings.m_SampleCount");
            if (samples != null)
                samples.intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(renderer);
        }

        static void FixGroundTextureImports()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Art/DenemeSahnesi/Generated/Ground_Basalt.mat");
            if (mat == null)
                return;

            TryFixTexture(mat, "_BaseMap");
            TryFixTexture(mat, "_BumpMap");
            TryFixTexture(mat, "_DetailAlbedoMap");
            TryFixTexture(mat, "_DetailNormalMap");
        }

        static void TryFixTexture(Material mat, string prop)
        {
            if (!mat.HasProperty(prop))
                return;
            var tex = mat.GetTexture(prop) as Texture2D;
            if (tex == null)
                return;
            string path = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(path))
                return;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;
            importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, 1024);
            importer.anisoLevel = Mathf.Max(importer.anisoLevel, 1);
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
    }
}
#endif
