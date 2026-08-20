#if UNITY_EDITOR
using Dovus.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    public static class PrototypeSceneCreator
    {
        const string ScenePath = "Assets/Scenes/Prototype.unity";

        [MenuItem("Dovus/Create Prototype Scene")]
        public static void CreateScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = new GameObject("Bootstrap");
            bootstrap.AddComponent<PrototypeBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            Debug.Log($"Prototype scene saved to {ScenePath}");
        }
    }
}
#endif
