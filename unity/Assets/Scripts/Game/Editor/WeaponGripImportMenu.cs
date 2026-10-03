#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    public static class WeaponGripImportMenu
    {
        const string RegistryPath = "Assets/Resources/Animation/WeaponVisualRegistry.asset";

        [MenuItem("Dovus/Grip/Import From Clipboard JSON")]
        public static void ImportFromClipboard()
        {
            string json = GUIUtility.systemCopyBuffer;
            WeaponVisualRegistry asset = AssetDatabase.LoadAssetAtPath<WeaponVisualRegistry>(RegistryPath);
            if (asset == null)
            {
                Debug.LogError("[WeaponGrip] registry asset yok: " + RegistryPath);
                return;
            }

            if (!WeaponGripClipboardJson.TryApplyToRegistry(json, asset, out int n))
            {
                Debug.LogError("[WeaponGrip] Panoda geçerli tutuş JSON yok.");
                return;
            }
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[WeaponGrip] Registry güncellendi: {n} silah (panodan).");
        }
    }
}
#endif
