using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Arena
{
    /// <summary>
    /// Arena dışında siyah void yerine taş zemin + soft sis ufku.
    /// </summary>
    public static class ArenaHorizon
    {
        public static void Build(Transform arenaRoot, float walkHalfM)
        {
            if (arenaRoot == null)
                return;

            // Arena scale'inden bağımsız dünya kökü — parent scale ufku ezmesin.
            var root = new GameObject("ArenaHorizon");
            root.transform.SetParent(null, false);
            root.transform.position = arenaRoot.position;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            float half = Mathf.Max(ArenaHorizonDefaults.MinHorizonHalfM, walkHalfM);

            // Unity Plane = 10×10 m; scale 1 → 10 m kenar.
            CreatePlane(root.transform, "SurroundGround", half * ArenaHorizonDefaults.SurroundGroundHalfMult, -ArenaHorizonDefaults.FarPlaneYOffsetM,
                new Color(0.40f, 0.38f, 0.36f));
            CreatePlane(root.transform, "FarGround", half * ArenaHorizonDefaults.MidPlaneExtentMult, -ArenaHorizonDefaults.NearPlaneYOffsetM,
                new Color(0.33f, 0.31f, 0.30f));
        }

        static void CreatePlane(Transform parent, string name, float halfExtentM, float y, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            float s = (halfExtentM * 2f) / ArenaHorizonDefaults.UnityPlaneEdgeM;
            go.transform.localScale = new Vector3(s, 1f, s);
            Object.Destroy(go.GetComponent<Collider>());

            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = MakeMat(color);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = true;
        }

        static Material MakeMat(Color color)
        {
            var shader = AssetLoader.FindShader("Universal Render Pipeline/Lit", null);
            if (shader == null)
                shader = AssetLoader.FindShader("Standard", null);
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", ArenaHorizonDefaults.GroundMaterialSmoothness);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0f);
            return mat;
        }
    }
}
