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
        public static void Apply(Light sun, Camera camera, PrototypeTuning tuning)
        {
            tuning ??= new PrototypeTuning();
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = tuning.AmbientSky;
            RenderSettings.ambientEquatorColor = tuning.AmbientEquator;
            RenderSettings.ambientGroundColor = tuning.AmbientGround;

            // Açık gri lav ovası (ambiyans portu, PR #43): doğrusal sis arena yarıçapına ölçeklenir.
            RenderSettings.skybox = null;
            CombatAmbienceEnvironment.ConfigureArenaFog(tuning);

            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = tuning.BackgroundColor;
                camera.farClipPlane = Mathf.Max(50f, tuning.CameraFarClipM);
                camera.fieldOfView = tuning.CameraFovDeg;
            }

            if (sun != null)
            {
                sun.intensity = Mathf.Max(0f, tuning.KeyLightIntensity);
                sun.color = tuning.KeyLightColor;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = Mathf.Clamp01(tuning.KeyShadowStrength);
                sun.transform.rotation = Quaternion.Euler(tuning.KeyLightEuler);
            }

            CreateRimLight(tuning);

            var fx = new GameObject("PostFX");
            var volume = fx.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;

            var look = fx.AddComponent<LookPresetController>();
            look.Initialize(volume, sun);
        }

        static void CreateRimLight(PrototypeTuning tuning)
        {
            if (tuning.RimLightIntensity <= 0f)
                return;

            var go = new GameObject("CharacterRim");
            var rim = go.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = tuning.RimLightColor;
            rim.intensity = tuning.RimLightIntensity;
            rim.shadows = LightShadows.None;
            rim.renderMode = LightRenderMode.ForceVertex;
            go.transform.rotation = Quaternion.Euler(tuning.RimLightEuler);
        }
    }
}
