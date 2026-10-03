#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>Ayar sahnesi panelinden registry tutuş kaydı.</summary>
    public static class WeaponGripRegistryWriter
    {
        const string RegistryPath = "Assets/Resources/Animation/WeaponVisualRegistry.asset";

        public static bool SaveRegistry(WeaponVisualRegistry runtimeInstance)
        {
            if (runtimeInstance == null)
                return false;

            WeaponVisualRegistry asset = AssetDatabase.LoadAssetAtPath<WeaponVisualRegistry>(RegistryPath);
            if (asset == null)
            {
                Debug.LogError("[WeaponGrip] registry asset yok: " + RegistryPath);
                return false;
            }

            CopyProps(runtimeInstance, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return true;
        }

        static void CopyProps(WeaponVisualRegistry from, WeaponVisualRegistry to)
        {
            if (from?.Props == null || to?.Props == null)
                return;
            for (int i = 0; i < from.Props.Count; i++)
            {
                WeaponVisualRegistry.PropEntry src = from.Props[i];
                if (src == null || string.IsNullOrEmpty(src.WeaponKey))
                    continue;
                WeaponVisualRegistry.PropEntry dst = to.FindProps(src.WeaponKey);
                if (dst == null)
                    continue;
                dst.RightLocalPosition = src.RightLocalPosition;
                dst.RightLocalEulerAngles = src.RightLocalEulerAngles;
                dst.RightLocalScale = src.RightLocalScale;
                dst.LeftLocalPosition = src.LeftLocalPosition;
                dst.LeftLocalEulerAngles = src.LeftLocalEulerAngles;
                dst.LeftLocalScale = src.LeftLocalScale;
            }
        }
    }
}
#endif
