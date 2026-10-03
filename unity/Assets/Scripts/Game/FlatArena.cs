using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Ayar sahnesi: düz kare zemin, düşük kenar duvarı (push-out). Daire salon yok.
    /// </summary>
    public static class FlatArena
    {
        public static GameObject Build(float halfSizeM, Color floorColor, Color wallColor)
        {
            float half = Mathf.Max(6f, halfSizeM);
            var root = new GameObject("Arena_Flat");
            root.transform.position = Vector3.zero;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(half * 2f, 0.1f, half * 2f);
            Object.Destroy(floor.GetComponent<Collider>());
            ApplyLit(floor, floorColor, receiveShadows: true, castShadows: false);

            BuildWall(root.transform, wallColor, new Vector3(0f, 1f, half), new Vector3(half * 2f, 2f, 0.4f));
            BuildWall(root.transform, wallColor, new Vector3(0f, 1f, -half), new Vector3(half * 2f, 2f, 0.4f));
            BuildWall(root.transform, wallColor, new Vector3(half, 1f, 0f), new Vector3(0.4f, 2f, half * 2f));
            BuildWall(root.transform, wallColor, new Vector3(-half, 1f, 0f), new Vector3(0.4f, 2f, half * 2f));
            return root;
        }

        static void BuildWall(Transform parent, Color color, Vector3 center, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = center;
            wall.transform.localScale = scale;
            ApplyLit(wall, color, receiveShadows: true, castShadows: true);
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
            rend.sharedMaterial = mat;
            rend.receiveShadows = receiveShadows;
            rend.shadowCastingMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
