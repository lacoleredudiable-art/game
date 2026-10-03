#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// Batchmode: eksik MonoBehaviour script referanslarını sahne, prefab ve Resources .asset dosyalarında sayar.
    /// </summary>
    public static class MissingScriptAudit
    {
        public static void Run()
        {
            var perPath = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int total = 0;

            try
            {
                foreach (string scenePath in Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.TopDirectoryOnly)
                             .Select(NormalizeAssetPath))
                {
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    int count = 0;
                    foreach (GameObject root in scene.GetRootGameObjects())
                        count += CountMissingScriptsInHierarchy(root);
                    if (count > 0)
                    {
                        perPath[scenePath] = count;
                        total += count;
                    }
                }

                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                    try
                    {
                        int count = CountMissingScriptsInHierarchy(root);
                        if (count > 0)
                        {
                            perPath[prefabPath] = count;
                            total += count;
                        }
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }

                foreach (string guid in AssetDatabase.FindAssets("", new[] { "Assets/Resources" }))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                        continue;

                    int count = CountMissingScriptsOnMainAsset(assetPath);
                    if (count > 0)
                    {
                        perPath[assetPath] = count;
                        total += count;
                    }
                }
            }
            finally
            {
                WriteReport(perPath, total);
                EditorApplication.Exit(total == 0 ? 0 : 2);
            }
        }

        static int CountMissingScriptsInHierarchy(GameObject go)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            foreach (Transform child in go.transform)
                count += CountMissingScriptsInHierarchy(child.gameObject);
            return count;
        }

        static int CountMissingScriptsOnMainAsset(string assetPath)
        {
            UnityEngine.Object main = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (main == null)
                return 1;

            if (main is ScriptableObject so)
                return MonoScript.FromScriptableObject(so) == null ? 1 : 0;

            if (main is MonoBehaviour mb)
                return MonoScript.FromMonoBehaviour(mb) == null ? 1 : 0;

            return 0;
        }

        static void WriteReport(Dictionary<string, int> perPath, int total)
        {
            string outPath = ResolveAuditOutPath();
            string? dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var sb = new StringBuilder();
            foreach (var kv in perPath.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"{kv.Key} {kv.Value}");
            sb.Append($"TOPLAM missing={total}");

            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"MissingScriptAudit: {total} missing → {outPath}");
        }

        static string ResolveAuditOutPath()
        {
            string? fromArg = GetCommandLineArgValue("-auditOut");
            if (!string.IsNullOrWhiteSpace(fromArg))
                return Path.GetFullPath(fromArg);

            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "missing-script-audit.txt"));
        }

        static string? GetCommandLineArgValue(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.Ordinal))
                    return args[i + 1];
            }

            return null;
        }

        static string NormalizeAssetPath(string path) => path.Replace('\\', '/');
    }
}
#endif
