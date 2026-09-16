using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dovus.Game
{
    /// <summary>
    /// Statik arena parçalarını bake’e hazır işaretler.
    /// Editör: Dovus → Mark Arena Static For Lightmaps
    /// </summary>
    public static class ArenaLightmapUtility
    {
        public const string MenuPath = "Dovus/Mark Selection Static For Lightmaps";

#if UNITY_EDITOR
        [MenuItem(MenuPath)]
        static void MarkSelection()
        {
            foreach (var go in Selection.gameObjects)
                MarkRecursive(go);
            Debug.Log("Dovus: seçim ContributeGI + BatchingStatic işaretlendi. Window → Rendering → Lighting → Generate Lighting.");
        }

        static void MarkRecursive(GameObject go)
        {
            GameObjectUtility.SetStaticEditorFlags(
                go,
                StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
            for (int i = 0; i < go.transform.childCount; i++)
                MarkRecursive(go.transform.GetChild(i).gameObject);
        }
#endif
    }
}
