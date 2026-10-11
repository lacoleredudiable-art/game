using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// k3 silah modeli kuralı: silah onu tutan elin Grip noktasına takılır; orijin tutuş noktasında,
    /// uzun eksen +Z (saptan başa), ölçek 1; Vfx_Kabza orijinde, Vfx_Tip +Z ucunda.
    /// Yalnız ölçer ve yazdırır; FBX'i düzeltmez.
    /// </summary>
    public static class K3WeaponModelAudit
    {
        static readonly string[] Models = { "Kilic", "Yay" };
        const float PosTolM = 0.005f;

        [MenuItem("Dovus/k3/Silah model kuralı (ölç, yazdır)")]
        public static string Run()
        {
            var sb = new StringBuilder();
            foreach (string name in Models)
                sb.AppendLine(Audit($"{K3AssetSetup.Root}/Silahlar/{name}.fbx", name));
            Debug.Log("[k3silah]\n" + sb);
            return sb.ToString();
        }

        static string Audit(string path, string name)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = Object.Instantiate(model);
            try
            {
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                Transform root = go.transform;
                Bounds b = MeshBounds(root);
                Transform hilt = K3AssetSetup.FindDeep(root, "Vfx_Kabza");
                Transform tip = K3AssetSetup.FindDeep(root, "Vfx_Tip");
                Vector3 hiltPos = hilt != null ? root.InverseTransformPoint(hilt.position) : Vector3.positiveInfinity;
                Vector3 tipPos = tip != null ? root.InverseTransformPoint(tip.position) : Vector3.positiveInfinity;
                Vector3 size = b.size;
                string longAxis = size.z >= size.x && size.z >= size.y ? "+Z" : size.y >= size.x ? "Y" : "X";
                Vector3 shaft = tipPos - hiltPos;
                float tipFromLong = Vector3.Angle(Vector3.forward, shaft);
                bool scaleOk = root.localScale == Vector3.one && Mathf.Approximately(importer.globalScale, 1f);
                bool hiltOk = hiltPos.magnitude <= PosTolM;
                bool axisOk = longAxis == "+Z";
                bool tipOk = Mathf.Abs(tipPos.z - b.max.z) <= PosTolM;
                return $"{name}: ölçek={root.localScale.x:0.###} globalScale={importer.globalScale:0.###} [{Ok(scaleOk)}] | " +
                       $"mesh min={b.min:F3} max={b.max:F3} boyut={size:F3} uzun eksen={longAxis} [{Ok(axisOk)}] | " +
                       $"Vfx_Kabza={hiltPos:F4} [{Ok(hiltOk)}] | Vfx_Tip={tipPos:F4} +Z ucunda, +Z'den {tipFromLong:0.0}° [{Ok(tipOk)}]";
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static string Ok(bool ok) => ok ? "uyar" : "UYMAZ";

        static Bounds MeshBounds(Transform root)
        {
            var points = root.GetComponentsInChildren<MeshFilter>(true)
                .SelectMany(mf => mf.sharedMesh.vertices.Select(v => root.InverseTransformPoint(mf.transform.TransformPoint(v))))
                .ToArray();
            var b = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 p in points)
                b.Encapsulate(p);
            return b;
        }
    }
}
