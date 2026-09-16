using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game
{
    /// <summary>
    /// Konsept mood: karanlık ambient + bloom. Lav/mesh asset’ten gelir; burada çizgi yok.
    /// </summary>
    public static class SceneAtmosphere
    {
        public static void Apply(Light sun, Camera camera, PrototypeTuning tuning)
        {
            if (sun != null)
            {
                sun.intensity = 0.85f;
                sun.color = new Color(1f, 0.72f, 0.48f);
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.14f, 0.08f, 0.06f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.06f, 0.03f, 0.025f);
            RenderSettings.fogDensity = 0.014f;

            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.04f, 0.025f, 0.02f);
                camera.farClipPlane = 80f;
            }

            if (tuning != null)
                tuning.CameraOffset = new Vector3(0f, 9.5f, -9f);

            var fx = new GameObject("PostFX");
            var volume = fx.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.45f);
            bloom.intensity.Override(2.1f);
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(new Color(1f, 0.55f, 0.25f));

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.Override(0.15f);
            colorAdj.contrast.Override(18f);
            colorAdj.saturation.Override(8f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.5f);
        }
    }
}
