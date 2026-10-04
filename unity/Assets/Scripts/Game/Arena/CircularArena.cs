using Dovus.Game.Actors;
using UnityEngine;

namespace Dovus.Game.Arena
{
    /// <summary>
    /// Tavansız daire zindan: disk zemin + yüksek çevre duvarı (avize/sütun/iç duvar yok).
    /// Duvar parçalarına BoxCollider eklenir — KinematicMotor push-out bunlara dayanır.
    /// </summary>
    public static class CircularArena
    {
        const int WallSegments = 64;

        public static GameObject Build(
            float radiusM,
            float wallHeightM,
            float wallThicknessM,
            Color floorColor,
            Color wallColor)
        {
            float r = Mathf.Max(4f, radiusM);
            float h = Mathf.Max(4f, wallHeightM);
            float thick = Mathf.Max(0.4f, wallThicknessM);

            var root = new GameObject("Arena_Circle");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            BuildFloor(root.transform, r, floorColor);
            BuildWallRing(root.transform, r, h, thick, wallColor);
            return root;
        }

        /// <summary>
        /// Ambiyans portu: duvar görselini saklar, BoxCollider'lara (KinematicMotor push-out)
        /// dokunmaz. Açık ova görünümü için "Walls" render'ları kapatılır, fiziksel sınır aynı kalır.
        /// </summary>
        public static void SetWallRenderersVisible(GameObject arenaRoot, bool visible)
        {
            if (arenaRoot == null)
                return;
            Transform walls = arenaRoot.transform.Find("Walls");
            if (walls == null)
                return;
            foreach (MeshRenderer r in walls.GetComponentsInChildren<MeshRenderer>(true))
                r.enabled = visible;
        }

        static void BuildFloor(Transform parent, float radiusM, Color color)
        {
            // Unity Cylinder: çap 1, yükseklik 2 — düz disk için Y küçültülür.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Floor";
            floor.transform.SetParent(parent, false);
            floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(radiusM * 2f, 0.05f, radiusM * 2f);
            Object.Destroy(floor.GetComponent<Collider>());
            ApplyLit(floor, color, receiveShadows: true, castShadows: false);
        }

        static void BuildWallRing(
            Transform parent, float radiusM, float wallHeightM, float thicknessM, Color color)
        {
            var walls = new GameObject("Walls");
            walls.transform.SetParent(parent, false);

            float chord = 2f * radiusM * Mathf.Tan(Mathf.PI / WallSegments);
            float midR = radiusM + thicknessM * 0.5f;

            for (int i = 0; i < WallSegments; i++)
            {
                float ang = (i + 0.5f) * (Mathf.PI * 2f / WallSegments);
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = $"Wall_{i:00}";
                seg.transform.SetParent(walls.transform, false);
                seg.transform.localPosition = new Vector3(
                    Mathf.Sin(ang) * midR,
                    wallHeightM * 0.5f,
                    Mathf.Cos(ang) * midR);
                seg.transform.localRotation = Quaternion.Euler(0f, ang * Mathf.Rad2Deg, 0f);
                seg.transform.localScale = new Vector3(thicknessM, wallHeightM, chord * 1.05f);
                ApplyLit(seg, color, receiveShadows: true, castShadows: true);
                // CreatePrimitive BoxCollider bırakılır — WallColliderFit'e gerek yok.
            }
        }

        static void ApplyLit(GameObject go, Color color, bool receiveShadows, bool castShadows)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null)
                return;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.08f);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0f);
            rend.sharedMaterial = mat;
            rend.receiveShadows = receiveShadows;
            rend.shadowCastingMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
