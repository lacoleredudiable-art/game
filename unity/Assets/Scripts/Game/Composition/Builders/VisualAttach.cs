using Dovus.Game.Config;
using Dovus.Game.DevTools;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Composition.Builders
{
    static class VisualAttach
    {
        public static GameObject ResolvePlayerVisualPrefab(GameObject sceneDefault) =>
            Resources.Load<GameObject>("PlayerVisualOverride") ?? sceneDefault;

        public static GameObject ResolveBossVisualPrefab(PrototypeTuning tuning, GameObject sceneDefault)
        {
            if (tuning.Boss.ActiveBossId != "aglarin_kralicesi")
                return sceneDefault;
            return Resources.Load<GameObject>("Bosses/Visuals/AglarinKralicesi") ?? sceneDefault;
        }

        public static void Attach(
            GameObject root,
            GameObject prefab,
            float targetHeightM,
            float groundY,
            float animSpeed,
            out Animator animator)
        {
            animator = null;
            if (root == null || prefab == null)
                return;

            var visual = Object.Instantiate(prefab, root.transform, false);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            Vector3 parentScale = root.transform.lossyScale;
            visual.transform.localScale = new Vector3(
                1f / Mathf.Max(0.0001f, parentScale.x),
                1f / Mathf.Max(0.0001f, parentScale.y),
                1f / Mathf.Max(0.0001f, parentScale.z));

            animator = visual.GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.Update(0f);

            if (TryGetRendererBounds(visual, out Bounds initial, posed: false) && initial.size.y > 0.01f)
            {
                float fit = Mathf.Max(0.1f, targetHeightM) / initial.size.y;
                visual.transform.localScale *= fit;
                if (TryGetRendererBounds(visual, out Bounds fitted, posed: true))
                    visual.transform.position += Vector3.up * (groundY - fitted.min.y);
                DebugConfig.DevLog(
                    $"[VisualScale] {root.name} target={targetHeightM:0.00}m "
                    + $"source={initial.size.y:0.00}m fit={fit:0.000}");
            }

            if (animator != null)
                animator.speed = Mathf.Clamp(animSpeed, 0.25f, 3f);

            var capsuleRend = root.GetComponent<Renderer>();
            if (capsuleRend != null)
                capsuleRend.enabled = false;
        }

        public static bool TryGetRendererBounds(GameObject root, out Bounds bounds, bool posed)
        {
            bounds = default;
            bool found = false;
            Mesh baked = null;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;
                Bounds b = renderer.bounds;
                if (posed && renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                {
                    baked ??= new Mesh();
                    skinned.BakeMesh(baked, true);
                    Matrix4x4 toWorld = skinned.transform.localToWorldMatrix;
                    Vector3[] verts = baked.vertices;
                    if (verts.Length > 0)
                    {
                        b = new Bounds(toWorld.MultiplyPoint3x4(verts[0]), Vector3.zero);
                        for (int i = 1; i < verts.Length; i++)
                            b.Encapsulate(toWorld.MultiplyPoint3x4(verts[i]));
                    }
                }
                if (!found)
                {
                    bounds = b;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(b);
                }
            }
            if (baked != null)
                Object.Destroy(baked);
            return found;
        }

        public static GameObject CreateCapsule(string name, Vector3 position, float radius, float height, Color color)
        {
            var capsule = CreateMeshObject(name, PrimitiveType.Capsule);
            capsule.transform.position = position;
            capsule.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            ApplyColor(capsule, color);
            return capsule;
        }

        public static GameObject CreateMeshObject(string name, PrimitiveType type)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(type);
            go.AddComponent<MeshRenderer>();
            return go;
        }

        public static Vector3 ClampSpawnXZ(Vector3 worldPos, float maxRadiusM)
        {
            if (maxRadiusM <= 0.01f)
                return worldPos;
            var xz = new Vector2(worldPos.x, worldPos.z);
            float maxR = maxRadiusM;
            if (xz.sqrMagnitude > maxR * maxR)
            {
                xz = xz.normalized * maxR;
                worldPos.x = xz.x;
                worldPos.z = xz.y;
            }
            return worldPos;
        }

        public static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                return;

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            renderer.sharedMaterial = material;
        }
    }
}
