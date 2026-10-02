#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>CC0 Quaternius heater shield → URP Lit materials + prefab (task polish).</summary>
    public static class QuaterniusShieldBind
    {
        public const string ShieldDir = "Assets/Art/Quaternius/Props/Shield";
        public const string ObjPath = ShieldDir + "/Shield_Heater_Quaternius.obj";
        public const string PrefabPath = ShieldDir + "/Shield_Heater.prefab";

        [MenuItem("Dovus/Art/Import Quaternius Shield")]
        public static void Import()
        {
            EnsureFolder("Assets/Art/Quaternius/Props");
            EnsureFolder(ShieldDir);

            Material steel = EnsureMat("Mat_Shield_Steel", new Color(0.45f, 0.46f, 0.48f), 0.6f, 0.45f);
            Material lightSteel = EnsureMat("Mat_Shield_LightSteel", new Color(0.50f, 0.52f, 0.54f), 0.55f, 0.42f);
            Material darkWood = EnsureMat("Mat_Shield_DarkWood", new Color(70f / 255f, 62f / 255f, 56f / 255f), 0f, 0.2f);
            Material lightWood = EnsureMat("Mat_Shield_LightWood", new Color(0.20f, 0.17f, 0.15f), 0f, 0.18f);

            ConfigureObjImporter();

            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(ObjPath);
            if (imported == null)
            {
                Debug.LogError("[ShieldBind] OBJ import failed: " + ObjPath);
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(imported);
            instance.name = "Shield_Heater_Mesh";
            foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string n = mats[i] != null ? mats[i].name : string.Empty;
                    if (n.IndexOf("LightSteel", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        mats[i] = lightSteel;
                    else if (n.IndexOf("Steel", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        mats[i] = steel;
                    else if (n.IndexOf("LightWood", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        mats[i] = lightWood;
                    else if (n.IndexOf("DarkWood", System.StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Wood", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        mats[i] = darkWood;
                    else
                        mats[i] = steel;
                }
                r.sharedMaterials = mats;
            }

            var root = new GameObject("Shield_Heater");
            instance.transform.SetParent(root.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[ShieldBind] → " + PrefabPath);
        }

        static Material EnsureMat(string fileName, Color baseColor, float metallic, float smoothness)
        {
            string path = ShieldDir + "/" + fileName + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Universal Render Pipeline/Simple Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void ConfigureObjImporter()
        {
            var importer = AssetImporter.GetAtPath(ObjPath) as ModelImporter;
            if (importer == null)
                return;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.globalScale = 1f;
            importer.SaveAndReimport();
        }

        static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string name = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
