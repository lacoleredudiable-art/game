using System;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Vfx;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// Ortak efekt atlası (tools/pack-vfx-atlas.py çıktısı) içe aktarma + tek VFX_Kor materyali.
    /// Doku: ASTC 6×6 (Android/iOS), sRGB kapalı, mipmap açık. Materyal: _VERTEX_DISSOLVE, renk köşe renginde.
    /// </summary>
    public static class EjderVfxAtlasSetup
    {
        const string ResourcesRoot = "Assets/Resources/";
        const string Android = "Android";
        const string Ios = "iPhone";
        const float EdgeBoost = 0.3f;

        [MenuItem("Dovus/Vfx/Ortak efekt atlası: içe aktar + materyal")]
        public static void Setup()
        {
            RuleEngineV4VfxAtlas layout = RuleVfxSettings.Slice.Atlas;
            string texPath = ResourcesRoot + layout.Resource + ".png";
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureAtlas(texPath, layout.SizePx);
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath)
                ?? throw new InvalidOperationException(texPath + " yok (önce python tools/pack-vfx-atlas.py)");
            EnsureMaterial(ResourcesRoot + layout.Material + ".mat", atlas);
            AssetDatabase.SaveAssets();
            Debug.Log("[vfx-atlas] " + texPath + " + " + layout.Material + " hazır");
        }

        static void ConfigureAtlas(string path, int sizePx)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path) ?? throw new InvalidOperationException(path + " yok");
            ti.textureType = TextureImporterType.Default;
            ti.textureShape = TextureImporterShape.Texture2D;
            ti.sRGBTexture = false;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = sizePx;
            ti.npotScale = TextureImporterNPOTScale.None;
            foreach (string platform in new[] { Android, Ios })
            {
                ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = platform,
                    overridden = true,
                    maxTextureSize = sizePx,
                    format = TextureImporterFormat.ASTC_6x6,
                    compressionQuality = (int)TextureCompressionQuality.Normal,
                });
            }
            ti.SaveAndReimport();
        }

        static void EnsureMaterial(string path, Texture2D atlas)
        {
            Shader sh = Shader.Find(RuleVfxDefaults.KorShaderName)
                ?? throw new InvalidOperationException(RuleVfxDefaults.KorShaderName + " yok");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = sh;
            mat.SetColor("_CoreColor", Color.white);
            mat.SetFloat("_Intensity", 1f);
            mat.SetFloat("_Dissolve", 0f);
            mat.SetFloat("_EdgeBoost", EdgeBoost);
            mat.SetFloat("_FlowOffset", 0f);
            mat.SetTexture("_MainTex", atlas);
            mat.SetTextureScale("_MainTex", Vector2.one);
            mat.SetTextureOffset("_MainTex", Vector2.zero);
            mat.SetTexture("_DissolveTex", atlas);
            mat.SetFloat("_VertexDissolve", 1f);
            mat.EnableKeyword("_VERTEX_DISSOLVE");
            mat.SetFloat("_MaskMode", 0f);
            mat.EnableKeyword("_MASKMODE_UV");
            mat.DisableKeyword("_MASKMODE_TRIPLANAR");
            mat.enableInstancing = false;
            EditorUtility.SetDirty(mat);
        }
    }
}
