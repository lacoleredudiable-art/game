using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// <c>Resources/Fonts/HudFont.ttf</c> → statik TMP SDF varlığı (Türkçe + ASCII önceden
    /// basılı). Runtime atlas büyümez, repo'da gürültü yapmaz.
    /// </summary>
    public static class HudFontAssetBuilder
    {
        const string FontPath = "Assets/Resources/Fonts/HudFont.ttf";
        const string AssetPath = "Assets/Resources/Fonts/HudFont SDF.asset";

        [MenuItem("Dovus/UI/Build HUD Font Asset")]
        public static void Build()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                Debug.LogError("[HudFont] font yok: " + FontPath);
                return;
            }

            AssetDatabase.DeleteAsset(AssetPath);
            TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(
                font, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            fa.name = "HudFont SDF";

            var chars = new System.Text.StringBuilder();
            for (char c = (char)32; c < 127; c++)
                chars.Append(c);
            chars.Append("çÇğĞıİöÖşŞüÜâÂîÎûÛ—–…·×%°•");
            fa.TryAddCharacters(chars.ToString(), out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning("[HudFont] fontta olmayan karakterler: " + missing);

            AssetDatabase.CreateAsset(fa, AssetPath);
            fa.atlasTextures[0].name = "HudFont Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = "HudFont Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            fa.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetPath);
            Debug.Log("[HudFont] oluşturuldu: " + AssetPath);
        }
    }
}
