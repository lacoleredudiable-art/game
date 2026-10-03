#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>Ayar sahnesi: düz zemin, kukla, tutuş paneli. Sahne kökünde PrototypeBootstrap + settingsScene.</summary>
    public static class SettingsSceneMenu
    {
        const string ScenePath = "Assets/Scenes/Settings.unity";

        [MenuItem("Dovus/Create Settings Scene (Ayar Sahnesi)")]
        public static void CreateSettingsScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("SettingsBootstrap");
            var boot = root.AddComponent<PrototypeBootstrap>();
            var so = new SerializedObject(boot);
            so.FindProperty("_settingsScene").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            Debug.Log($"[Dovus] Ayar sahnesi kaydedildi: {ScenePath}. Play ile açın veya Build Settings'e eklendi.");
        }

        static void AddSceneToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var entry in scenes)
            {
                if (entry.path == path)
                    return;
            }

            var list = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
                list[i] = scenes[i];
            list[^1] = new EditorBuildSettingsScene(path, true);
            EditorBuildSettings.scenes = list;
        }
    }
}
#endif
