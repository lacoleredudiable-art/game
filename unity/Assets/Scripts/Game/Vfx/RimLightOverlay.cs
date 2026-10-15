using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// SI-3.7: görselin her renderer'ına son slot olarak kenar ışığı materyali ekler (yalnız oyuncu).
    /// Gövde materyaline ve HitFlashView'in yazdığı özelliklere dokunmaz; özellikleri _Rim… adlıdır.
    /// </summary>
    public static class RimLightOverlay
    {
        /// <summary>Eklenen materyal (tek örnek, tüm renderer'lar paylaşır); shader yoksa null ve LogError.</summary>
        public static Material Apply(GameObject visual)
        {
            if (visual == null)
                return null;
            Shader shader = AssetLoader.FindShader(RimLightDefaults.ShaderName, null);
            if (shader == null)
            {
                Debug.LogError($"[V4-FALLBACK] RimLightOverlay.Apply: {RimLightDefaults.ShaderName} shader'ı bulunamadı; kenar ışığı yok");
                return null;
            }

            var material = new Material(shader) { name = "RimOverlay" };
            material.SetColor("_RimColor", RimLightDefaults.RimColor);
            material.SetFloat("_RimPower", RimLightDefaults.Power);
            material.SetFloat("_RimIntensity", RimLightDefaults.Intensity);

            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer))
                    continue;
                Material[] body = r.sharedMaterials;
                var mats = new Material[body.Length + 1];
                body.CopyTo(mats, 0);
                mats[body.Length] = material;
                r.sharedMaterials = mats;
            }
            return material;
        }
    }
}
