using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game
{
    /// <summary>
    /// Dungeon mood: soft mist ufuk (siyah void değil) + hafif bloom.
    /// </summary>
    public static class SceneAtmosphere
    {
        static readonly Color Mist = new Color(0.32f, 0.30f, 0.28f);
        static readonly Color MistDeep = new Color(0.22f, 0.21f, 0.20f);

        public static void Apply(Light sun, Camera camera, PrototypeTuning tuning)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.20f, 0.28f);
            RenderSettings.ambientEquatorColor = new Color(0.22f, 0.16f, 0.12f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.08f, 0.07f);

            // Kapalı salon: skybox değil — soft sis (duvar/tavan dışarıyı keser).
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.08f, 0.07f, 0.09f);
            RenderSettings.fogDensity = 0.012f;

            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.06f, 0.05f, 0.055f);
                camera.farClipPlane = 120f;
            }

            if (sun != null)
            {
                // Demo salonu kendi torch/chandelier ışığını taşır — güneşi bastır.
                sun.intensity = 0.35f;
                sun.color = new Color(0.55f, 0.65f, 0.95f);
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.35f;
                sun.transform.rotation = Quaternion.Euler(25f, -40f, 0f);
            }

            if (tuning != null)
                tuning.CameraOffset = new Vector3(0f, 12f, -14f);

            var fx = new GameObject("PostFX");
            var volume = fx.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.35f);
            bloom.intensity.Override(1.8f);
            bloom.scatter.Override(0.7f);
            bloom.tint.Override(new Color(1f, 0.55f, 0.25f));

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.Override(0.2f);
            colorAdj.contrast.Override(14f);
            colorAdj.saturation.Override(10f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.45f);
        }
    }
}
