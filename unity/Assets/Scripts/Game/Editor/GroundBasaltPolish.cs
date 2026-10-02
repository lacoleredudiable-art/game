#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>Ground_Basalt: detail breakup + normal strength (committed asset, task polish).</summary>
    public static class GroundBasaltPolish
    {
        const string MatPath = "Assets/Art/DenemeSahnesi/Generated/Ground_Basalt.mat";

        [MenuItem("Dovus/Art/Polish Ground Basalt Material")]
        public static void Polish()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                Debug.LogError("[GroundBasaltPolish] missing " + MatPath);
                return;
            }

            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_DETAIL_MULX2");
            if (mat.HasProperty("_BumpScale"))
                mat.SetFloat("_BumpScale", 0.9f);
            if (mat.HasProperty("_DetailNormalMapScale"))
                mat.SetFloat("_DetailNormalMapScale", 0.85f);

            Vector2 baseSt = new Vector2(0.14285715f, 0.14285715f);
            mat.SetTextureScale("_BaseMap", baseSt);
            mat.SetTextureScale("_MainTex", baseSt);
            mat.SetTextureScale("_BumpMap", baseSt);
            mat.SetTextureScale("_DetailAlbedoMap", new Vector2(3.888889f, 3.888889f));
            mat.SetTextureScale("_DetailNormalMap", new Vector2(5.2f, 5.2f));

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log("[GroundBasaltPolish] updated " + MatPath);
        }
    }
}
#endif
