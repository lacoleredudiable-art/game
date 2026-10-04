using Dovus.Game.Config;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game.Arena
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
            RenderSettings.ambientSkyColor = tuning.Arena.AmbientSky;
            RenderSettings.ambientEquatorColor = tuning.Arena.AmbientEquator;
            RenderSettings.ambientGroundColor = tuning.Arena.AmbientGround;

            // Açık gri lav ovası (ambiyans portu, PR #43): doğrusal sis arena yarıçapına ölçeklenir.
            RenderSettings.skybox = null;
            CombatAmbienceEnvironment.ConfigureArenaFog(tuning);

            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = tuning.Visuals.BackgroundColor;
                camera.farClipPlane = Mathf.Max(50f, tuning.Arena.CameraFarClipM);
                camera.fieldOfView = tuning.Camera.CameraFovDeg;
            }

            if (sun != null)
            {
                sun.intensity = Mathf.Max(0f, tuning.Arena.KeyLightIntensity);
                sun.color = tuning.Arena.KeyLightColor;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = Mathf.Clamp01(tuning.Arena.KeyShadowStrength);
                sun.transform.rotation = Quaternion.Euler(tuning.Arena.KeyLightEuler);
            }

            CreateRimLight(tuning);

            var fx = new GameObject("PostFX");
            var volume = fx.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;

            var look = fx.AddComponent<LookPresetController>();
            look.Initialize(volume, sun, camera);
        }

        static void CreateRimLight(PrototypeTuning tuning)
        {
            if (tuning.Arena.RimLightIntensity <= 0f)
                return;

            var go = new GameObject("CharacterRim");
            var rim = go.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = tuning.Arena.RimLightColor;
            rim.intensity = tuning.Arena.RimLightIntensity;
            rim.shadows = LightShadows.None;
            rim.renderMode = LightRenderMode.ForceVertex;
            go.transform.rotation = Quaternion.Euler(tuning.Arena.RimLightEuler);
        }
    }
}
