#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>CC0 Quaternius silah prop'ları → URP Lit + prefab (cw-2).</summary>
    public static class QuaterniusCc0PropsBind
    {
        public const string PropsRoot = "Assets/Art/Quaternius/Props";

        public const string StaffPrefab = PropsRoot + "/Staff_Quaternius/Staff_Quaternius.prefab";
        public const string OrbPrefab = PropsRoot + "/Orb_PickupSphere_Quaternius/Orb_PickupSphere.prefab";
        public const string TalismanPrefab = PropsRoot + "/Talisman_Necklace_Quaternius/Talisman_Necklace.prefab";
        public const string CannonPrefab = PropsRoot + "/HandCannon_Shotgun_Quaternius/HandCannon_Shotgun.prefab";

        const string StaffObj = PropsRoot + "/Staff_Quaternius/Staff_Quaternius.obj";
        const string OrbObj = PropsRoot + "/Orb_PickupSphere_Quaternius/Orb_PickupSphere_Quaternius.obj";
        const string TalismanObj = PropsRoot + "/Talisman_Necklace_Quaternius/Talisman_Necklace_Quaternius.obj";
        const string CannonObj = PropsRoot + "/HandCannon_Shotgun_Quaternius/HandCannon_Shotgun_Quaternius.obj";

        [MenuItem("Dovus/Art/Import Quaternius CC0 Props")]
        public static void ImportAll()
        {
            EnsureFolder(PropsRoot);
            Material wood = EnsureLitMat(PropsRoot + "/Staff_Quaternius/Mat_Staff_Wood.mat",
                new Color(0.20f, 0.17f, 0.14f), 0f, 0.22f);
            Material woodDark = EnsureLitMat(PropsRoot + "/Staff_Quaternius/Mat_Staff_Crook.mat",
                new Color(0.16f, 0.14f, 0.12f), 0f, 0.2f);
            Material orbGlow = EnsureLitMat(PropsRoot + "/Orb_PickupSphere_Quaternius/Mat_Orb_PaleGlow.mat",
                new Color(0.72f, 0.78f, 0.80f), 0f, 0.55f,
                new Color(0.35f, 0.42f, 0.45f) * 0.25f);
            Material talismanMetal = EnsureLitMat(PropsRoot + "/Talisman_Necklace_Quaternius/Mat_Talisman_Metal.mat",
                new Color(0.48f, 0.46f, 0.44f), 0.55f, 0.4f);
            Material cannonMetal = EnsureLitMat(PropsRoot + "/HandCannon_Shotgun_Quaternius/Mat_Cannon_Metal.mat",
                new Color(0.42f, 0.41f, 0.40f), 0.65f, 0.38f);
            Material cannonWood = EnsureLitMat(PropsRoot + "/HandCannon_Shotgun_Quaternius/Mat_Cannon_Wood.mat",
                new Color(0.19f, 0.16f, 0.13f), 0f, 0.2f);

            ConfigureObjImporter(StaffObj);
            ConfigureObjImporter(OrbObj);
            ConfigureObjImporter(TalismanObj);
            ConfigureObjImporter(CannonObj);
            AssetDatabase.Refresh();

            BuildStaffPrefab(wood, woodDark);
            BuildOrbPrefab(orbGlow);
            BuildTalismanPrefab(talismanMetal);
            BuildCannonPrefab(cannonMetal, cannonWood);

            AssetDatabase.SaveAssets();
            Debug.Log("[Cc0PropsBind] Staff/Orb/Talisman/Cannon prefabs ready under " + PropsRoot);
        }

        static void BuildStaffPrefab(Material wood, Material crook)
        {
            var meshGo = LoadMeshRoot(StaffObj);
            if (meshGo == null)
                return;
            AssignMaterialsByName(meshGo, wood, crook, "Staff", "Crook");
            var root = new GameObject("Staff_Quaternius");
            meshGo.transform.SetParent(root.transform, false);
            OffsetChildToGrip(root, meshGo, gripFractionFromMinY: 0.4f, longAxis: Vector3.up);
            SavePrefab(root, StaffPrefab);
        }

        static void BuildOrbPrefab(Material glow)
        {
            var meshGo = LoadMeshRoot(OrbObj);
            if (meshGo == null)
                return;
            foreach (Renderer r in meshGo.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = glow;
                r.sharedMaterials = mats;
            }

            var root = new GameObject("Orb_PickupSphere");
            meshGo.transform.SetParent(root.transform, false);
            meshGo.transform.localPosition = Vector3.zero;
            SavePrefab(root, OrbPrefab);
        }

        static void BuildTalismanPrefab(Material metal)
        {
            var meshGo = LoadMeshRoot(TalismanObj);
            if (meshGo == null)
                return;
            foreach (Renderer r in meshGo.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = metal;
                r.sharedMaterials = mats;
            }

            var root = new GameObject("Talisman_Necklace");
            meshGo.transform.SetParent(root.transform, false);
            OffsetChildToGrip(root, meshGo, gripFractionFromMinY: 0.72f, longAxis: Vector3.up);
            SavePrefab(root, TalismanPrefab);
        }

        static void BuildCannonPrefab(Material metal, Material wood)
        {
            var meshGo = LoadMeshRoot(CannonObj);
            if (meshGo == null)
                return;
            AssignMaterialsByName(meshGo, metal, wood, "Metal", "Wood");
            var root = new GameObject("HandCannon_Shotgun");
            meshGo.transform.SetParent(root.transform, false);
            OffsetChildToGrip(root, meshGo, gripFractionFromMinX: 0.22f, longAxis: Vector3.right);
            SavePrefab(root, CannonPrefab);
        }

        static GameObject LoadMeshRoot(string objPath)
        {
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            if (imported == null)
            {
                Debug.LogError("[Cc0PropsBind] missing " + objPath);
                return null;
            }

            return (GameObject)PrefabUtility.InstantiatePrefab(imported);
        }

        static void OffsetChildToGrip(
            GameObject root, GameObject meshChild,
            float gripFractionFromMinY = -1f, float gripFractionFromMinX = -1f, Vector3 longAxis = default)
        {
            if (!TryBounds(meshChild, out Bounds b))
                return;
            Vector3 gripWorld;
            if (gripFractionFromMinX >= 0f)
            {
                float t = gripFractionFromMinX;
                gripWorld = new Vector3(
                    Mathf.Lerp(b.min.x, b.max.x, t),
                    b.center.y,
                    b.center.z);
            }
            else
            {
                float t = gripFractionFromMinY >= 0f ? gripFractionFromMinY : 0.4f;
                gripWorld = new Vector3(
                    b.center.x,
                    Mathf.Lerp(b.min.y, b.max.y, t),
                    b.center.z);
            }

            Vector3 gripLocal = root.transform.InverseTransformPoint(gripWorld);
            meshChild.transform.localPosition -= gripLocal;
        }

        static void AssignMaterialsByName(GameObject root, Material a, Material b, string aToken, string bToken)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string n = mats[i] != null ? mats[i].name : string.Empty;
                    if (n.IndexOf(bToken, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        mats[i] = b;
                    else
                        mats[i] = a;
                }

                r.sharedMaterials = mats;
            }
        }

        static void SavePrefab(GameObject root, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static Material EnsureLitMat(string path, Color baseColor, float metallic, float smoothness, Color? emission = null)
        {
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
            if (emission.HasValue && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void ConfigureObjImporter(string objPath)
        {
            var importer = AssetImporter.GetAtPath(objPath) as ModelImporter;
            if (importer == null)
                return;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.globalScale = 1f;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.SaveAndReimport();
        }

        static bool TryBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            if (rs == null || rs.Length == 0)
                return false;
            bounds = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++)
                bounds.Encapsulate(rs[i].bounds);
            return true;
        }

        static void EnsureFolder(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
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
