#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Dovus.Game;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>Dragon silah FBX → URP Lit + prefab + <see cref="WeaponVisualRegistry"/>.</summary>
    public static class DragonWeaponsBind
    {
        const string DragonRoot = "Assets/Art/Weapons/Dragon";
        const string RegistryPath = "Assets/Resources/Animation/WeaponVisualRegistry.asset";

        enum DragonHand { Right, Left }

        readonly struct DragonWeaponRow
        {
            public readonly string WeaponKey;
            public readonly string FolderName;
            public readonly DragonHand Hand;
            public readonly bool PreserveOppositeHand;
            public readonly bool DoubleSided;
            public readonly string[] ExtraLeftHandWeaponKeys;

            public DragonWeaponRow(
                string weaponKey, string folderName, DragonHand hand,
                bool preserveOppositeHand = false, bool doubleSided = false,
                string[] extraLeftHandWeaponKeys = null)
            {
                WeaponKey = weaponKey;
                FolderName = folderName;
                Hand = hand;
                PreserveOppositeHand = preserveOppositeHand;
                DoubleSided = doubleSided;
                ExtraLeftHandWeaponKeys = extraLeftHandWeaponKeys ?? System.Array.Empty<string>();
            }
        }

        static readonly DragonWeaponRow[] Weapons =
        {
            new("cekic", "Cekic", DragonHand.Right),
            new("kilic", "Kilic", DragonHand.Right, preserveOppositeHand: true),
            new("kitap", "Kitap", DragonHand.Left),
            new("kalkan", "Kalkan", DragonHand.Left, preserveOppositeHand: true, doubleSided: true,
                extraLeftHandWeaponKeys: new[] { "kilic" }),
        };

        [MenuItem("Tools/Weapons/Bind Dragon Props")]
        public static void BindAll()
        {
            foreach (DragonWeaponRow row in Weapons)
            {
                ConfigureTextureImporter(BaseColorPath(row));
                ConfigureFbxImporter(FbxPath(row));
            }

            AssetDatabase.Refresh();

            WeaponVisualRegistry registry = AssetDatabase.LoadAssetAtPath<WeaponVisualRegistry>(RegistryPath);
            if (registry == null)
            {
                Debug.LogError("[DragonWeaponsBind] missing registry " + RegistryPath);
                return;
            }

            foreach (DragonWeaponRow row in Weapons)
            {
                Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath(row));
                Material mat = EnsureDragonMat(MatPath(row), baseMap, row.DoubleSided);
                GameObject prefab = BuildWeaponPrefab(row, mat);
                if (prefab == null)
                    continue;
                BindRegistryRow(registry, row, prefab);
            }

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            Debug.Log("[DragonWeaponsBind] Dragon weapons bound (" + Weapons.Length + ").");
        }

        static string WeaponRoot(DragonWeaponRow row) => DragonRoot + "/" + row.FolderName;

        static string FbxPath(DragonWeaponRow row) => WeaponRoot(row) + "/Weapon_" + row.FolderName + ".fbx";

        static string BaseColorPath(DragonWeaponRow row) =>
            WeaponRoot(row) + "/Textures/" + row.FolderName + "_BaseColor.png";

        static string MatPath(DragonWeaponRow row) => WeaponRoot(row) + "/" + row.FolderName + "_Mat.mat";

        static string PrefabPath(DragonWeaponRow row) => WeaponRoot(row) + "/Weapon_" + row.FolderName + ".prefab";

        static GameObject BuildWeaponPrefab(DragonWeaponRow row, Material mat)
        {
            GameObject meshGo = LoadMeshRoot(FbxPath(row));
            if (meshGo == null)
                return null;

            foreach (Renderer r in meshGo.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = mat;
                r.sharedMaterials = mats;
            }

            string prefabPath = PrefabPath(row);
            var root = new GameObject("Weapon_" + row.FolderName);
            meshGo.transform.SetParent(root.transform, false);
            meshGo.transform.localPosition = Vector3.zero;
            meshGo.transform.localRotation = Quaternion.identity;
            meshGo.transform.localScale = Vector3.one;
            SavePrefab(root, prefabPath);
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        static void BindRegistryRow(WeaponVisualRegistry registry, DragonWeaponRow row, GameObject prefab)
        {
            if (row.Hand == DragonHand.Right)
            {
                if (row.PreserveOppositeHand)
                    SetRightHand(registry, row.WeaponKey, prefab, Vector3.zero, Vector3.zero, Vector3.one);
                else
                    SetProp(registry, row.WeaponKey,
                        prefab, Vector3.zero, Vector3.zero, Vector3.one,
                        null, Vector3.zero, Vector3.zero, Vector3.one);
                return;
            }

            if (row.PreserveOppositeHand)
            {
                SetLeftHand(registry, row.WeaponKey, prefab, Vector3.zero, Vector3.zero, Vector3.one);
                foreach (string extraKey in row.ExtraLeftHandWeaponKeys)
                    SetLeftHand(registry, extraKey, prefab, Vector3.zero, Vector3.zero, Vector3.one);
                return;
            }

            SetLeftHand(registry, row.WeaponKey, prefab, Vector3.zero, Vector3.zero, Vector3.one);
            SetRightHand(registry, row.WeaponKey, null, Vector3.zero, Vector3.zero, Vector3.one);
        }

        static WeaponVisualRegistry.PropEntry FindOrCreateEntry(WeaponVisualRegistry registry, string weaponKey)
        {
            List<WeaponVisualRegistry.PropEntry> list = registry.Props;
            WeaponVisualRegistry.PropEntry entry = list.Find(p => p != null && p.WeaponKey == weaponKey);
            if (entry != null)
                return entry;
            entry = new WeaponVisualRegistry.PropEntry { WeaponKey = weaponKey };
            list.Add(entry);
            return entry;
        }

        static void SetRightHand(
            WeaponVisualRegistry registry, string weaponKey,
            GameObject prefab, Vector3 pos, Vector3 rot, Vector3 scale)
        {
            WeaponVisualRegistry.PropEntry entry = FindOrCreateEntry(registry, weaponKey);
            entry.RightHandPrefab = prefab;
            entry.RightLocalPosition = pos;
            entry.RightLocalEulerAngles = rot;
            entry.RightLocalScale = scale;
        }

        static void SetLeftHand(
            WeaponVisualRegistry registry, string weaponKey,
            GameObject prefab, Vector3 pos, Vector3 rot, Vector3 scale)
        {
            WeaponVisualRegistry.PropEntry entry = FindOrCreateEntry(registry, weaponKey);
            entry.LeftHandPrefab = prefab;
            entry.LeftLocalPosition = pos;
            entry.LeftLocalEulerAngles = rot;
            entry.LeftLocalScale = scale;
        }

        static void SetProp(
            WeaponVisualRegistry registry, string weaponKey,
            GameObject rightPrefab, Vector3 rightPos, Vector3 rightRot, Vector3 rightScale,
            GameObject leftPrefab, Vector3 leftPos, Vector3 leftRot, Vector3 leftScale)
        {
            WeaponVisualRegistry.PropEntry entry = FindOrCreateEntry(registry, weaponKey);
            entry.RightHandPrefab = rightPrefab;
            entry.RightLocalPosition = rightPos;
            entry.RightLocalEulerAngles = rightRot;
            entry.RightLocalScale = rightScale;
            entry.LeftHandPrefab = leftPrefab;
            entry.LeftLocalPosition = leftPos;
            entry.LeftLocalEulerAngles = leftRot;
            entry.LeftLocalScale = leftScale;
        }

        static Material EnsureDragonMat(string matPath, Texture2D baseMap, bool doubleSided)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Universal Render Pipeline/Simple Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
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
            if (mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", doubleSided ? 0f : 2f);

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
