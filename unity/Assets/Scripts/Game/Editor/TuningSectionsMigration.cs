#if UNITY_EDITOR
using Dovus.Game.Composition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// PLAN 2B.5b: düz sahne YAML anahtarlarını bölüm serileştirmesine taşır (legacy → sections OnAfterDeserialize).
    /// </summary>
    public static class TuningSectionsMigration
    {
        const string ScenePath = "Assets/Scenes/Prototype.unity";

        [MenuItem("Dovus/Tuning/Migrate scene tuning to sections")]
        public static void MigrateFromMenu()
        {
            if (!MigrateScene(out string error))
            {
                Debug.LogError("[TuningSectionsMigration] " + error);
                EditorUtility.DisplayDialog("Tuning sections migration", error, "Tamam");
            }
            else
                Debug.Log("[TuningSectionsMigration] Sahne kaydedildi: " + ScenePath);
        }

        /// <summary>Batchmode: -executeMethod Dovus.Game.Editor.TuningSectionsMigration.MigrateFromCommandLine</summary>
        public static void MigrateFromCommandLine()
        {
            if (!MigrateScene(out string error))
            {
                Debug.LogError("[TuningSectionsMigration] " + error);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        static bool MigrateScene(out string error)
        {
            error = null;
            if (!System.IO.File.Exists(ScenePath))
            {
                error = "Sahne bulunamadı: " + ScenePath;
                return false;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var boot = Object.FindFirstObjectByType<PrototypeBootstrap>();
            if (boot == null)
            {
                error = "PrototypeBootstrap bileşeni yok.";
                return false;
            }

            boot.SceneTuning.SyncSectionsFromLegacyFlatFields();
            EditorUtility.SetDirty(boot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                error = "SaveScene başarısız.";
                return false;
            }

            return true;
        }
    }
}
#endif
