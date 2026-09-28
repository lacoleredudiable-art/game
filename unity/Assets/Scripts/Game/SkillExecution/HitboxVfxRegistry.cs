using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// hitbox_vfx.vfx_prefab_naming registry. Art prefabı yoksa geometriyi element rengiyle
    /// görünür kılan collider'sız, hafif primitive üretir.
    /// </summary>
    public static class HitboxVfxRegistry
    {
        public static GameObject Create(
            string key,
            string shape,
            string colorHex,
            Vector3 position,
            Vector3 direction,
            float radiusM,
            float reachM,
            Transform parent)
        {
            GameObject prefab = !string.IsNullOrEmpty(key)
                ? Resources.Load<GameObject>("Vfx/Hitbox/" + key)
                : null;
            if (prefab != null)
                return Object.Instantiate(prefab, position, Quaternion.LookRotation(direction), parent);

            PrimitiveType primitive = shape switch
            {
                "capsule" => PrimitiveType.Capsule,
                "line" => PrimitiveType.Cube,
                "cone" => PrimitiveType.Cylinder,
                "cylinder" => PrimitiveType.Cylinder,
                _ => PrimitiveType.Sphere
            };
            var go = new GameObject(string.IsNullOrEmpty(key) ? "HitboxVfx_Fallback" : key + "_Fallback");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Vector3 forward = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            go.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(primitive);
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();

            if (shape is "capsule" or "line")
            {
                go.transform.position += forward * reachM * 0.5f;
                go.transform.localScale = shape == "line"
                    ? new Vector3(radiusM * 2f, radiusM * 0.25f, reachM)
                    : new Vector3(radiusM * 2f, reachM * 0.5f, radiusM * 2f);
                if (shape == "capsule")
                    go.transform.rotation *= Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                float height = shape is "cone" or "cylinder" ? Mathf.Max(0.05f, radiusM * 0.15f) : radiusM * 2f;
                go.transform.localScale = new Vector3(radiusM * 2f, height, radiusM * 2f);
            }

            Color color = Color.white;
            if (!string.IsNullOrEmpty(colorHex))
                ColorUtility.TryParseHtmlString(colorHex, out color);
            color.a = 0.38f;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                var material = new Material(shader);
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                else
                    material.color = color;
                renderer.sharedMaterial = material;
            }
            return go;
        }
    }
}
