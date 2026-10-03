#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Dovus.Game;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>Dragon silah FBX → URP Lit + prefab + <see cref="WeaponVisualRegistry"/> (çekiç).</summary>
    public static class DragonWeaponsBind
    {
        const string CekicRoot = "Assets/Art/Weapons/Dragon/Cekic";
        const string CekicFbx = CekicRoot + "/Weapon_Cekic.fbx";
        const string CekicBaseColor = CekicRoot + "/Textures/Cekic_BaseColor.png";
        const string CekicMatPath = CekicRoot + "/Cekic_Mat.mat";
        const string CekicPrefabPath = CekicRoot + "/Weapon_Cekic.prefab";

        const string RegistryDir = "Assets/Resources/Animation";
        const string RegistryPath = RegistryDir + "/WeaponVisualRegistry.asset";

        [MenuItem("Tools/Weapons/Bind Dragon Props")]
        public static void BindAll()
        {
            ConfigureTextureImporter(CekicBaseColor);
            ConfigureFbxImporter(CekicFbx);
            AssetDatabase.Refresh();

            Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(CekicBaseColor);
            Material mat = EnsureCekicMat(baseMap);
            BuildCekicPrefab(mat);
            BindCekicRegistry();

            AssetDatabase.SaveAssets();
            Debug.Log("[DragonWeaponsBind] Weapon_Cekic prefab + registry cekic ready.");
        }

        static void BuildCekicPrefab(Material mat)
        {
            GameObject meshGo = LoadMeshRoot(CekicFbx);
            if (meshGo == null)
                return;

            foreach (Renderer r in meshGo.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = mat;
                r.sharedMaterials = mats;
            }

            var root = new GameObject("Weapon_Cekic");
            meshGo.transform.SetParent(root.transform, false);
            meshGo.transform.localPosition = Vector3.zero;
            meshGo.transform.localRotation = Quaternion.identity;
            meshGo.transform.localScale = Vector3.one;
            SavePrefab(root, CekicPrefabPath);
        }

        static void BindCekicRegistry()
        {
            WeaponVisualRegistry registry = AssetDatabase.LoadAssetAtPath<WeaponVisualRegistry>(RegistryPath);
            if (registry == null)
            {
                Debug.LogError("[DragonWeaponsBind] missing registry " + RegistryPath);
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CekicPrefabPath);
            SetProp(registry, "cekic",
                prefab, Vector3.zero, Vector3.zero, Vector3.one,
                null, Vector3.zero, Vector3.zero, Vector3.one);
            EditorUtility.SetDirty(registry);
        }

        static void SetProp(
            WeaponVisualRegistry registry, string weaponKey,
            GameObject rightPrefab, Vector3 rightPos, Vector3 rightRot, Vector3 rightScale,
            GameObject leftPrefab, Vector3 leftPos, Vector3 leftRot, Vector3 leftScale)
        {
            List<WeaponVisualRegistry.PropEntry> list = registry.Props;
            WeaponVisualRegistry.PropEntry entry = list.Find(p => p != null && p.WeaponKey == weaponKey);
            if (entry == null)
            {
                entry = new WeaponVisualRegistry.PropEntry { WeaponKey = weaponKey };
                list.Add(entry);
            }

            entry.RightHandPrefab = rightPrefab;
            entry.RightLocalPosition = rightPos;
            entry.RightLocalEulerAngles = rightRot;
            entry.RightLocalScale = rightScale;
            entry.LeftHandPrefab = leftPrefab;
            entry.LeftLocalPosition = leftPos;
            entry.LeftLocalEulerAngles = leftRot;
            entry.LeftLocalScale = leftScale;
        }

        static Material EnsureCekicMat(Texture2D baseMap)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(CekicMatPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Universal Render Pipeline/Simple Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, CekicMatPath);
            }

            if (baseMap != null && mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", baseMap);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.12f);
            if (mat.HasProperty("_SpecularHighlights"))
                mat.SetFloat("_SpecularHighlights", 0f);

            mat.DisableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", Color.black);
            if (mat.HasProperty("_EmissionMap"))
                mat.SetTexture("_EmissionMap", null);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void ConfigureFbxImporter(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
                return;

            importer.globalScale = 1f;
            importer.isReadable = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.SaveAndReimport();
        }

        static void ConfigureTextureImporter(string texPath)
        {
            var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer == null)
                return;

            importer.maxTextureSize = 2048;
            importer.sRGBTexture = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            ApplyMobileDefault(importer, "Android");
            ApplyMobileDefault(importer, "iPhone");
            importer.SaveAndReimport();
        }

        static void ApplyMobileDefault(TextureImporter importer, string platform)
        {
            var settings = importer.GetPlatformTextureSettings(platform);
            settings.overridden = true;
            settings.maxTextureSize = 2048;
            settings.format = platform == "Android"
                ? TextureImporterFormat.ETC2_RGBA8
                : TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(settings);
        }

        static GameObject LoadMeshRoot(string assetPath)
        {
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (imported == null)
            {
                Debug.LogError("[DragonWeaponsBind] missing " + assetPath);
                return null;
            }

            return (GameObject)PrefabUtility.InstantiatePrefab(imported);
        }

        static void SavePrefab(GameObject root, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
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
