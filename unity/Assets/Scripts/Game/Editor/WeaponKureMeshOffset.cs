#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>Weapon_Kure: mesh holder kaydır; Grip local sabit kalır (prefab).</summary>
    public static class WeaponKureMeshOffset
    {
        const string PrefabPath = "Assets/Art/Weapons/Dragon/Kure/Weapon_Kure.prefab";
        const float AheadM = 0.06f;

        [MenuItem("Dovus/Grip/Bake Kure Mesh Offset (0.06m)")]
        public static void BakeFromMenu()
        {
            if (ApplyToPrefab(out string report))
                Debug.Log(report);
            else
                Debug.LogError(report);
        }

        public static bool ApplyToPrefab(out string report)
        {
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefabRoot == null)
            {
                report = "[KureMesh] prefab yok: " + PrefabPath;
                return false;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefabRoot) as GameObject;
            if (instance == null)
            {
                report = "[KureMesh] instantiate başarısız";
                return false;
            }

            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Transform grip = FindGrip(instance.transform);
            if (grip == null || grip.parent == null || grip.parent == instance.transform)
            {
                Object.DestroyImmediate(instance);
                report = "[KureMesh] Grip/mesh holder hiyerarşisi beklenmiyor";
                return false;
            }

            if (!TryBounds(instance, out Bounds bounds))
            {
                Object.DestroyImmediate(instance);
                report = "[KureMesh] renderer yok";
                return false;
            }

            Transform meshHolder = grip.parent;
            meshHolder.localPosition = new Vector3(0f, 0f, AheadM);
            grip.localPosition = Vector3.zero;

            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
            report = $"[KureMesh] {PrefabPath} meshHolder.z={AheadM:F2}m";
            return true;
        }

        static Transform FindGrip(Transform root)
        {
            if (root.name.Equals("Grip", System.StringComparison.OrdinalIgnoreCase))
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindGrip(root.GetChild(i));
                if (found != null)
                    return found;
            }

            return null;
        }

        static bool TryBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            Renderer[] rs = root.GetComponentsInChildren<Renderer>();
            if (rs == null || rs.Length == 0)
                return false;
            bounds = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++)
                bounds.Encapsulate(rs[i].bounds);
            return bounds.size.sqrMagnitude > 1e-8f;
        }
    }
}
#endif
