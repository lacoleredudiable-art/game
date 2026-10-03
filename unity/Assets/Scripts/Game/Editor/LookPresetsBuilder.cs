#if UNITY_EDITOR
using Dovus.Game.Arena;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game.Editor
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
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.48f);
            bloom.scatter.Override(0.5f);
            bloom.tint.Override(new Color(1f, 0.92f, 0.82f));

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
            // task-look-v2b problem 2: A/B ayrımı modest kaldı (gameplay |A-B| 18.4, hedef >=20) —
            // kontrast/SMH/vinyet daha güçlü, bloom daha çekingen (daha "keskin", daha az yumuşak parlama).
            if (profile.TryGet(out ColorAdjustments colorAdj))
            {
                colorAdj.contrast.Override(27f);
                colorAdj.postExposure.Override(0.2f);
                colorAdj.saturation.Override(-14f);
                colorAdj.colorFilter.Override(new Color(0.87f, 0.92f, 1.05f));
            }

            if (profile.TryGet(out ShadowsMidtonesHighlights smh))
            {
                smh.shadows.Override(new Vector4(0.72f, 0.79f, 1.11f, -0.37f));
                smh.midtones.Override(new Vector4(0.91f, 0.94f, 1.04f, 0.03f));
                smh.highlights.Override(new Vector4(1.1f, 1.08f, 0.95f, 0.15f));
            }

            if (profile.TryGet(out Vignette vignette))
                vignette.intensity.Override(0.36f);

            if (profile.TryGet(out Bloom bloom))
            {
                bloom.threshold.Override(1.3f);
                bloom.intensity.Override(0.28f);
            }
        }

        static void TuneC(VolumeProfile profile)
        {
            // task-look-v2b problem 2: C, B'den belirgin daha "zengin" olsun (hedef gameplay >=12) —
            // ağırlık SSAO/yansıma probuna (runtime, LookPresets/LookPresetController) kayar, burada
            // profil tarafı B'nin keskinliğinden ayrışsın diye daha yüksek pozlama/doygunluk/bloom alır.
            if (profile.TryGet(out ColorAdjustments colorAdj))
            {
                colorAdj.postExposure.Override(0.24f);
                colorAdj.contrast.Override(9f);
                colorAdj.saturation.Override(-15f);
            }

            if (profile.TryGet(out ShadowsMidtonesHighlights smh))
            {
                smh.shadows.Override(new Vector4(0.9f, 0.94f, 1.05f, -0.08f));
                smh.midtones.Override(new Vector4(0.97f, 0.98f, 1.01f, 0.02f));
                smh.highlights.Override(new Vector4(1.03f, 1.02f, 0.99f, 0.05f));
            }

            if (profile.TryGet(out Bloom bloom))
            {
                bloom.threshold.Override(1.05f);
                bloom.intensity.Override(0.52f);
            }

            if (profile.TryGet(out Vignette vignette))
                vignette.intensity.Override(0.16f);
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
            // Alan adları "m_Settings" içinde "m_" ön eki TAŞIMIYOR (bkz. URP 17
            // ScreenSpaceAmbientOcclusionSettings: Downsample/Radius/Intensity/...). Eski yol
            // ("m_Settings.m_Radius" vb.) FindProperty'den null dönüyordu, null kontrolleri de
            // sessizce atlıyordu — SSAO hiçbir zaman bu değerleri almadı, URP varsayılanlarında
            // kaldı (Radius=0.035 çok küçük, creases'te görünmüyordu). task-look-v2b problem 2.
            var down = so.FindProperty("m_Settings.Downsample");
            if (down != null)
                down.boolValue = true;
            var radius = so.FindProperty("m_Settings.Radius");
            if (radius != null)
                radius.floatValue = 0.42f;
            var intensity = so.FindProperty("m_Settings.Intensity");
            if (intensity != null)
                intensity.floatValue = 0.85f;
            var directLighting = so.FindProperty("m_Settings.DirectLightingStrength");
            if (directLighting != null)
                directLighting.floatValue = 0.25f;
            var falloff = so.FindProperty("m_Settings.Falloff");
            if (falloff != null)
                falloff.floatValue = 50f;
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
