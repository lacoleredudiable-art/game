#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>
    /// <see cref="CombatAmbienceAssets"/> Resources asset'ine Quaternius CC0 kenar prop prefab referansları yazır.
    /// </summary>
    public static class CombatAmbienceAssetsBind
    {
        const string AssetPath = "Assets/Resources/Environment/CombatAmbienceAssets.asset";

        [MenuItem("Dovus/Environment/Bind Combat Ambience Edge Props")]
        public static void BindEdgeProps()
        {
            var list = new List<GameObject>();
            foreach (string path in QuaterniusCc0PropsBind.AmbienceEdgePropPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    list.Add(prefab);
                else
                    Debug.LogWarning("[CombatAmbienceBind] prefab yok (önce Dovus/Art/Import Quaternius CC0 Props): " + path);
            }

            if (list.Count == 0)
            {
                Debug.LogError("[CombatAmbienceBind] Edge prop prefab bulunamadı.");
                return;
            }

            var assets = AssetDatabase.LoadAssetAtPath<CombatAmbienceAssets>(AssetPath);
            if (assets == null)
            {
                Debug.LogError("[CombatAmbienceBind] asset yok: " + AssetPath);
                return;
            }

            assets.EdgeProps = list.ToArray();
            EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CombatAmbienceBind] EdgeProps={assets.EdgeProps.Length} → {AssetPath}");
        }
    }
}
#endif
