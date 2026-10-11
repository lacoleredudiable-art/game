using System;
using System.IO;
using System.Linq;
using Dovus.Game.Arena;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// Ortam (dilim arenası) içe aktarma: FBX ayarları, doku ayarları (Android ASTC), gövde/kor materyalleri ve
    /// <see cref="ArenaVisualRegistry"/>. Kaynak: k3/05_export/ortam (Blender, Forward -Y / Up Z, All Local).
    /// Eksen: varsayılan içe aktarma dik ve gerçek metre (sutun 3.0 m); Bake Axis Conversion kapalı kalır
    /// (açıkken model Y'de 180° döner). Menu: Dovus/Ortam/*. Batch: -executeMethod Dovus.Game.Editor.OrtamAssetSetup.Reimport
    /// </summary>
    public static class OrtamAssetSetup
    {
        internal const string Root = "Assets/Art/Ortam";
        internal const string KorDir = Root + "/_Kor";
        internal const string KorPack = KorDir + "/ortam_kor_paket_2k_RGBA.png";
        internal const string RegistryPath = "Assets/Resources/Arena/ArenaVisualRegistry.asset";
        const string SimpleLit = "Universal Render Pipeline/Simple Lit";
        const string DitherLit = "Dovus/Ortam/Ortam_DitherLit";
        const string VfxKor = "Dovus/Vfx/VFX_Kor";
        const string Android = "Android";
        const int ModelTexMax = 1024;
        const int KorPackMax = 2048;

        /// <summary>JSON model anahtarı → klasör/FBX. kaburga_LOD1 kaburga klasöründe, aynı materyali paylaşır.</summary>
        internal static readonly (string Key, string Folder)[] Models =
        {
            ("sutun", "sutun"), ("koza", "koza"), ("kaburga", "kaburga"), ("kaburga_LOD1", "kaburga"),
            ("runtasi", "runtasi"), ("duvar", "duvar"),
        };

        static readonly (string Folder, string Albedo)[] Bodies =
        {
            ("sutun", "sutun_albedo"), ("koza", "koza_albedo"), ("kaburga", "kaburga_albedo"),
            ("runtasi", "runtasi_albedo_temiz"), ("duvar", "duvar_albedo"),
        };

        internal static string FbxPath(string key, string folder) => $"{Root}/{folder}/{key}.fbx";
        internal static string BodyMaterialPath(string folder) => $"{Root}/{folder}/{folder}_Mat.mat";
        internal const string KozaKorMaterial = Root + "/koza/Kor_Koza.mat";
        internal const string RibKorMaterial = KorDir + "/Kor_Kaburga.mat";
        internal const string FloorKorMaterial = KorDir + "/Kor_Zemin.mat";
        internal const string HearthKorMaterial = KorDir + "/Kor_Ocak.mat";
        const string KozaMask = Root + "/koza/koza_kor_uv.png";
        const string RuneDepthMask = Root + "/runtasi/runtasi_oyma_uv.png";
        const string RuneNormal = Root + "/runtasi/runtasi_oyma_uv_normal.png";

        // Kemer solması: gövde ve kor katmanı aynı değerleri taşır.
        const float FadeStrength = 0.6f;
        static readonly Vector4 FadeScreenCenter = new(0.5f, 0.6f, 0.35f, 0.45f);
        const float FadeSoftness = 0.35f;
        const float FadeNearM = 6f;
        const float FadeFarM = 14f;

        [MenuItem("Dovus/Ortam/Yeniden al (FBX + doku + materyal + kayıt)")]
        public static void Reimport()
        {
            foreach ((string folder, string albedo) in Bodies)
                ConfigureAlbedo($"{Root}/{folder}/{albedo}.png");
            ConfigureMask(KozaMask, TextureImporterFormat.ASTC_6x6);
            ConfigureMask(RuneDepthMask, TextureImporterFormat.ASTC_8x8);
            ConfigureNormalFromGray(RuneNormal);
            bool korPack = File.Exists(KorPack);
            if (korPack)
                ConfigureKorPack(KorPack);
            else
                Debug.LogWarning("[ortam] kor paketi yok: " + KorPack);

            foreach ((string folder, string albedo) in Bodies)
                EnsureBodyMaterial(folder, albedo);
            foreach ((string key, string folder) in Models)
                ConfigureModel(FbxPath(key, folder), AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath(folder)));

            Material kozaKor = EnsureKozaKor();
            Material ribKor = EnsureTriplanarKor(RibKorMaterial, new Vector4(0f, 1f, 0f, 0f), 0.5f, 0.5f, 60f, true);
            Material floorKor = EnsureTriplanarKor(FloorKorMaterial, new Vector4(1f, 0f, 0f, 0f), 0.25f, 1f, 0f, false);
            Material hearthKor = EnsureTriplanarKor(HearthKorMaterial, new Vector4(0f, 0f, 1f, 0f), 0.5f, 1f, 0f, false);
            WriteRegistry(kozaKor, ribKor, floorKor, hearthKor);
            AssetDatabase.SaveAssets();
            Debug.Log("[ortam] yeniden alma tamam" + (korPack ? string.Empty : " (kor paketi eksik)"));
        }

        static void ConfigureModel(string path, Material body)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(path) ?? throw new InvalidOperationException(path + " yok");
            imp.globalScale = 1f;
            imp.useFileScale = true;
            imp.bakeAxisConversion = false;
            imp.isReadable = false;
            imp.importCameras = false;
            imp.importLights = false;
            imp.addCollider = false;
            imp.importVisibility = false;
            imp.importBlendShapes = false;
            imp.importAnimation = false;
            imp.animationType = ModelImporterAnimationType.None;
            imp.generateSecondaryUV = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.SaveAndReimport();
            foreach (Material m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToArray())
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), body);
            imp.SaveAndReimport();
        }

        static TextureImporter Tex(string path) =>
            (TextureImporter)AssetImporter.GetAtPath(path) ?? throw new InvalidOperationException(path + " yok");

        static void SetAndroid(TextureImporter ti, TextureImporterFormat format, int maxSize)
        {
            ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = Android,
                overridden = true,
                maxTextureSize = maxSize,
                format = format,
                compressionQuality = (int)TextureCompressionQuality.Normal,
            });
        }

        static void ConfigureAlbedo(string path)
        {
            TextureImporter ti = Tex(path);
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.None;
            ti.mipmapEnabled = true;
            ti.maxTextureSize = ModelTexMax;
            SetAndroid(ti, TextureImporterFormat.ASTC_6x6, ModelTexMax);
            ti.SaveAndReimport();
        }

        /// <summary>Gri RGB PNG → tek kanal (R8), doğrusal; Android ASTC.</summary>
        static void ConfigureMask(string path, TextureImporterFormat android)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning("[ortam] maske yok, atlandı: " + path);
                return;
            }
            TextureImporter ti = Tex(path);
            ti.textureType = TextureImporterType.SingleChannel;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
            ti.SetTextureSettings(s);
            ti.sRGBTexture = false;
            ti.mipmapEnabled = true;
            ti.maxTextureSize = ModelTexMax;
            TextureImporterPlatformSettings def = ti.GetDefaultPlatformTextureSettings();
            def.format = TextureImporterFormat.R8;
            def.maxTextureSize = ModelTexMax;
            ti.SetPlatformTextureSettings(def);
            SetAndroid(ti, android, ModelTexMax);
            ti.SaveAndReimport();
        }

        static void ConfigureNormalFromGray(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning("[ortam] oyma normal kopyası yok, atlandı: " + path);
                return;
            }
            TextureImporter ti = Tex(path);
            ti.textureType = TextureImporterType.NormalMap;
            ti.convertToNormalmap = true;
            ti.normalmapFilter = TextureImporterNormalFilter.Standard;
            ti.mipmapEnabled = true;
            ti.maxTextureSize = ModelTexMax;
            SetAndroid(ti, TextureImporterFormat.ASTC_6x6, ModelTexMax);
            ti.SaveAndReimport();
        }

        /// <summary>R zemin pul-çatlak, G kaburga çatlak nabzı, B ocak korları; doğrusal, tekrarlı.</summary>
        static void ConfigureKorPack(string path)
        {
            TextureImporter ti = Tex(path);
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = false;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = false;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.mipmapEnabled = true;
            ti.maxTextureSize = KorPackMax;
            SetAndroid(ti, TextureImporterFormat.ASTC_6x6, KorPackMax);
            ti.SaveAndReimport();
        }

        /// <summary>
        /// Gövde: Simple Lit (Blinn-Phong, specular kapalı — mobil bütçe; taş/kemik için PBR farkı görünmez).
        /// Kaburga: Ortam_DitherLit (aynı aydınlatma + kemer solması). Rün taşı: oyma normal map'i, emisyon yok.
        /// </summary>
        static void EnsureBodyMaterial(string folder, string albedo)
        {
            bool rib = folder == "kaburga";
            Material m = LoadOrCreate(BodyMaterialPath(folder), rib ? DitherLit : SimpleLit);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/{folder}/{albedo}.png"));
            m.SetColor("_BaseColor", Color.white);
            if (rib)
            {
                ApplyFade(m);
            }
            else
            {
                m.SetFloat("_SpecularHighlights", 0f);
                m.DisableKeyword("_SPECULAR_COLOR");
                m.DisableKeyword("_SPECGLOSSMAP");
                m.DisableKeyword("_EMISSION");
                var normal = folder == "runtasi" ? AssetDatabase.LoadAssetAtPath<Texture2D>(RuneNormal) : null;
                m.SetTexture("_BumpMap", normal);
                if (normal != null)
                    m.EnableKeyword("_NORMALMAP");
                else
                    m.DisableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(m);
        }

        static Material EnsureKozaKor()
        {
            Material m = LoadOrCreate(KozaKorMaterial, VfxKor);
            m.SetFloat("_MaskMode", 0f);
            m.EnableKeyword("_MASKMODE_UV");
            m.DisableKeyword("_MASKMODE_TRIPLANAR");
            m.SetFloat("_UvMaskChannel", 1f);
            m.EnableKeyword("_UV_MASK_CHANNEL");
            m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(KozaMask));
            m.SetVector("_MaskChannel", new Vector4(1f, 0f, 0f, 0f));
            m.SetFloat("_EdgeBoost", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material EnsureTriplanarKor(string path, Vector4 channel, float tiling, float maxGlow, float pulseSec, bool archDither)
        {
            Material m = LoadOrCreate(path, VfxKor);
            m.SetFloat("_MaskMode", 1f);
            m.EnableKeyword("_MASKMODE_TRIPLANAR");
            m.DisableKeyword("_MASKMODE_UV");
            m.SetTexture("_KorMask", AssetDatabase.LoadAssetAtPath<Texture2D>(KorPack));
            m.SetVector("_MaskChannel", channel);
            m.SetFloat("_MaskTiling", tiling);
            m.SetFloat("_MaskBlend", 6f);
            m.SetFloat("_MaxGlow", maxGlow);
            m.SetFloat("_PulsePeriod", pulseSec);
            m.SetFloat("_PulseMin", 0.6f);
            m.SetFloat("_NoiseScale", 0.07f);
            m.SetFloat("_NoiseFloor", 0.35f);
            m.SetFloat("_EdgeBoost", 0f);
            m.SetFloat("_ArchDither", archDither ? 1f : 0f);
            if (archDither)
            {
                m.EnableKeyword("_ARCH_DITHER");
                ApplyFade(m);
            }
            else
            {
                m.DisableKeyword("_ARCH_DITHER");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static void ApplyFade(Material m)
        {
            m.SetFloat("_FadeStrength", FadeStrength);
            m.SetVector("_FadeScreenCenter", FadeScreenCenter);
            m.SetFloat("_FadeSoftness", FadeSoftness);
            m.SetFloat("_FadeNearM", FadeNearM);
            m.SetFloat("_FadeFarM", FadeFarM);
        }

        static Material LoadOrCreate(string path, string shaderName)
        {
            Shader shader = Shader.Find(shaderName) ?? throw new InvalidOperationException(shaderName + " shader'ı yok");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader)
            {
                m.shader = shader;
            }
            return m;
        }

        static void WriteRegistry(Material kozaKor, Material ribKor, Material floorKor, Material hearthKor)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RegistryPath));
            var reg = AssetDatabase.LoadAssetAtPath<ArenaVisualRegistry>(RegistryPath);
            if (reg == null)
            {
                reg = ScriptableObject.CreateInstance<ArenaVisualRegistry>();
                AssetDatabase.CreateAsset(reg, RegistryPath);
            }
            reg.Models.Clear();
            foreach ((string key, string folder) in Models)
            {
                Material overlay = key switch
                {
                    "koza" => kozaKor,
                    "kaburga" or "kaburga_LOD1" => ribKor,
                    _ => null,
                };
                reg.Models.Add(new ArenaVisualRegistry.ModelEntry
                {
                    Key = key,
                    Model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath(key, folder)),
                    KorOverlay = overlay,
                });
            }
            reg.FloorGlow = floorKor;
            reg.HearthEmbers = hearthKor;
            EditorUtility.SetDirty(reg);
        }
    }
}
