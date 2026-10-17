using Dovus.Game.Boss;
using UnityEngine;

namespace Dovus.Game.Hud
{
    /// <summary>Boss hasar sayısı için kafa/tepe dünya noktası (O-10.3).</summary>
    public static class BossDamageAnchor
    {
        public static bool TryGetHeadTopWorldY(BossReactorController boss, out float worldY)
        {
            worldY = boss != null ? boss.transform.position.y : 0f;
            if (boss == null)
                return false;
            if (TryRendererTopY(boss.gameObject, out worldY))
                return true;
            worldY = boss.transform.position.y + boss.BodyRadiusM * DamageNumberHudDefaults.BossHeadFallbackHeightMul;
            return false;
        }

        public static Vector3 HitPoint(BossReactorController boss, Vector3 impactOriginFlat)
        {
            Vector3 center = boss.transform.position;
            Vector3 dir = impactOriginFlat - center;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f)
                dir = Vector3.forward;
            dir.Normalize();
            TryGetHeadTopWorldY(boss, out float headY);
            float horiz = Mathf.Min(
                boss.BodyRadiusM * DamageNumberHudDefaults.BossHitHorizontalBodyRadiusFrac,
                DamageNumberHudDefaults.BossHitHorizontalOffsetMaxM);
            return new Vector3(center.x, headY + DamageNumberHudDefaults.BossHeadTopExtraOffsetM, center.z) + dir * horiz;
        }

        static bool TryRendererTopY(GameObject root, out float topY)
        {
            topY = root.transform.position.y;
            bool found = false;
            Mesh baked = null;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;
                Bounds b = renderer.bounds;
                if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
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
                    topY = b.max.y;
                    found = true;
                }
                else if (b.max.y > topY)
                {
                    topY = b.max.y;
                }
            }

            if (baked != null)
                Object.Destroy(baked);
            return found;
        }
    }
}
