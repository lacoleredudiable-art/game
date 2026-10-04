using Dovus.Game.Weapons;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// Paladin (J Nordstrom) → gitignored <c>Resources/PlayerVisualOverride.prefab</c>.
    /// Menu: Dovus/Mixamo/Bind Paladin Player
    /// </summary>
    public static class PaladinPlayerBind
    {
        const string PaladinFbx = "Assets/Art/Mixamo/Characters/Paladin/Paladin.fbx";
        const string MaterialsDir = "Assets/Art/Mixamo/Characters/Paladin/Materials";
        const string ResourcesDir = "Assets/Art/Mixamo/Characters/Paladin/Resources";
        const string OutPrefab = ResourcesDir + "/PlayerVisualOverride.prefab";
        const string PlayerCtrl = MixamoAnimatorBind.PlayerCtrl;

        [MenuItem("Dovus/Mixamo/Bind Paladin Player")]
        public static void Bind()
        {
            if (!File.Exists(PaladinFbx))
            {
                Debug.Log("[PaladinBind] FBX yok — atlandı: " + PaladinFbx);
                return;
            }

            BrightenUrpmaterials();

            EnsureFolder(ResourcesDir);

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(PaladinFbx);
            if (fbx == null)
            {
                Debug.LogError("[PaladinBind] FBX import edilemedi: " + PaladinFbx);
                return;
            }

            var root = new GameObject("PlayerVisual_Paladin");
            var mesh = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            mesh.name = "Mesh";
            mesh.transform.SetParent(root.transform, false);
            mesh.transform.localPosition = Vector3.zero;
            mesh.transform.localRotation = Quaternion.identity;
            mesh.transform.localScale = Vector3.one;

            var anim = mesh.GetComponentInChildren<Animator>(true);
            if (anim == null)
                anim = mesh.AddComponent<Animator>();
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerCtrl);
            if (ctrl != null)
            {
                anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            else
                Debug.LogWarning("[PaladinBind] " + PlayerCtrl + " yok — önce Dovus/Synty/Bind Mixamo Animator.");

            var grip = root.AddComponent<WeaponGripView>();
            grip.SetMixamoDefaults();

            AssignUrpmaterialsToRenderers(mesh);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, OutPrefab);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PaladinBind] PlayerVisualOverride → " + OutPrefab + " (Resources.Load PlayerVisualOverride).");
        }

        static void BrightenUrpmaterials()
        {
            if (!AssetDatabase.IsValidFolder(MaterialsDir))
                return;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialsDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("_URP.mat", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                    continue;
                if (mat.HasProperty("_Metallic"))
                    mat.SetFloat("_Metallic", 0.08f);
                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", 0.38f);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", Color.white);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.055f, 0.052f, 0.048f));
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                EditorUtility.SetDirty(mat);
            }
        }

        static void AssignUrpmaterialsToRenderers(GameObject meshRoot)
        {
            var body = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/Paladin_Paladin_J_Nordstrom_URP.mat");
            var helm = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/Paladin_Paladin_J_Nordstrom_Helmet_URP.mat");
            foreach (var r in meshRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null)
                    continue;
                string n = r.gameObject.name;
                if (helm != null && n.IndexOf("Helmet", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    r.sharedMaterial = helm;
                else if (body != null)
                    r.sharedMaterial = body;
            }
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
