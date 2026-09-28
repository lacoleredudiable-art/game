using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// hitbox_vfx.vfx_prefab_naming registry. Art prefabı yoksa geometriyi element rengiyle
    /// görünür kılan collider'sız, hafif primitive üretir. Primitive'in üstüne teslim yoluna göre
    /// somut bir paket efekti giydirilir (<c>Delivery/{executor}/{şekil}</c> → <c>Delivery/{executor}</c>
    /// → <c>Delivery/{şekil}</c>); giydirme varsa primitive yalnız hitbox/collider taşıyıcısı kalır.
    /// </summary>
    public static class HitboxVfxRegistry
    {
        static readonly Dictionary<string, Material> Materials = new();

        public static GameObject Create(
            string key,
            string shape,
            string colorHex,
            Vector3 position,
            Vector3 direction,
            float radiusM,
            float reachM,
            Transform parent,
            float angleDeg = 0f)
        {
            GameObject go = CreateBase(key, shape, colorHex, position, direction, radiusM, reachM, parent, angleDeg,
                out bool fromKey);
            if (fromKey || go == null)
                return go;

            string role = parent != null && parent.TryGetComponent(out SkillExecutor executor)
                ? executor.Kind.ToString()
                : null;
            GameObject dress = SpawnDelivery(role, shape, colorHex, go.transform, direction, radiusM, reachM, parent);
            if (dress != null && shape != "cylinder" && go.TryGetComponent(out MeshRenderer primitive))
                primitive.enabled = false;
            return go;
        }

        static GameObject SpawnDelivery(string role, string shape, string colorHex, Transform anchor,
            Vector3 direction, float radiusM, float reachM, Transform parent)
        {
            VfxLibrary lib = VfxLibrary.Current;
            string key = null;
            foreach (string candidate in new[]
                     {
                         role != null ? $"Delivery/{role}/{shape}" : null,
                         role != null ? $"Delivery/{role}" : null,
                         $"Delivery/{shape}",
                     })
            {
                if (candidate != null && lib.TryResolve(candidate, out _, out _))
                {
                    key = candidate;
                    break;
                }
            }
            if (key == null)
                return null;

            Vector3 flat = new(direction.x, 0f, direction.z);
            Quaternion rot = flat.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(flat.normalized) : Quaternion.identity;
            float size = shape is "cone" or "line" or "capsule" ? Mathf.Max(reachM, radiusM * 2f) : radiusM * 2f;
            GameObject dress = lib.TrySpawn(key, anchor.position, rot, parent, size);
            if (dress == null)
                return null;
            dress.name = key.Replace('/', '_');
            if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out Color tint))
                VfxLibrary.Tint(dress, tint, lib.ImpactTintStrength);
            dress.AddComponent<FollowAnchor>().Anchor = anchor;
            return dress;
        }

        /// <summary>Giydirme, hareket eden primitive'i (mermi) izler; primitive yok olunca kendini siler.</summary>
        sealed class FollowAnchor : MonoBehaviour
        {
            public Transform Anchor;

            void LateUpdate()
            {
                if (Anchor == null)
                {
                    Destroy(gameObject);
                    return;
                }
                transform.position = Anchor.position;
            }
        }

        static GameObject CreateBase(
            string key,
            string shape,
            string colorHex,
            Vector3 position,
            Vector3 direction,
            float radiusM,
            float reachM,
            Transform parent,
            float angleDeg,
            out bool fromKey)
        {
            fromKey = false;
            if (VfxLibrary.Current.TryResolve(key, out GameObject prefab, out _))
            {
                fromKey = true;
                Vector3 look = direction.sqrMagnitude > 0.0001f ? direction : Vector3.forward;
                return Object.Instantiate(prefab, position, Quaternion.LookRotation(look), parent);
            }

            PrimitiveType primitive = shape switch
            {
                "capsule" => PrimitiveType.Capsule,
                "line" => PrimitiveType.Cube,
                "cylinder" => PrimitiveType.Cylinder,
                _ => PrimitiveType.Sphere
            };
            var go = new GameObject(string.IsNullOrEmpty(key) ? "HitboxVfx_Fallback" : key + "_Fallback");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Vector3 forward = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            go.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            go.AddComponent<MeshFilter>().sharedMesh = shape == "cone"
                ? CreateConeMesh(angleDeg)
                : PrimitiveMesh.Get(primitive);
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
            else if (shape == "cone")
            {
                go.transform.localScale = new Vector3(reachM, 1f, reachM);
            }
            else
            {
                float height = shape == "cylinder" ? Mathf.Max(0.05f, radiusM * 0.15f) : radiusM * 2f;
                go.transform.localScale = new Vector3(radiusM * 2f, height, radiusM * 2f);
            }

            Color color = Color.white;
            if (!string.IsNullOrEmpty(colorHex))
                ColorUtility.TryParseHtmlString(colorHex, out color);
            color.a = 0.38f;
            string materialKey = colorHex ?? string.Empty;
            if (!Materials.TryGetValue(materialKey, out Material material) || material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    material = new Material(shader);
                    if (material.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", color);
                    else
                        material.color = color;
                    Materials[materialKey] = material;
                }
            }
            renderer.sharedMaterial = material;
            return go;
        }

        static Mesh CreateConeMesh(float angleDeg)
        {
            float half = Mathf.Clamp(angleDeg > 0f ? angleDeg : 60f, 1f, 359f) * 0.5f;
            const int segments = 12;
            var vertices = new Vector3[segments + 2];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-half, half, i / (float)segments) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Sin(angle), 0.02f, Mathf.Cos(angle));
                if (i == segments)
                    continue;
                int t = i * 3;
                triangles[t] = 0;
                triangles[t + 1] = i + 1;
                triangles[t + 2] = i + 2;
            }
            var mesh = new Mesh { name = "HitboxCone" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
