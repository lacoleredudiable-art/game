#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.Editor
{
    /// <summary>Kenney PNG import ayarları — mobil alpha, clamp, 256 Android.</summary>
    public static class KenneyParticlesBind
    {
        const string Folder = "Assets/Resources/Vfx/Kenney";

        [MenuItem("Dovus/Art/Bind Kenney Particles")]
        public static void Bind()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder });
            int n = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Compressed;

                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                android.overridden = true;
                android.maxTextureSize = 256;
                android.format = TextureImporterFormat.ASTC_6x6;
                android.compressionQuality = 50;
                importer.SetPlatformTextureSettings(android);

                importer.SaveAndReimport();
                n++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[KenneyParticlesBind] Reimported {n} texture(s) under {Folder}.");
        }
    }
}
#endif
