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
        /// <param name="fixedFog">Verilirse (dilim) sis yalnız buradan yazılır ve kamera arka planı sis rengidir.</param>
        public static void Apply(Light sun, Camera camera, GameTuning tuning, ArenaFog? fixedFog = null)
        {
            tuning ??= new GameTuning();
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = tuning.Arena.AmbientSky;
            RenderSettings.ambientEquatorColor = tuning.Arena.AmbientEquator;
            RenderSettings.ambientGroundColor = tuning.Arena.AmbientGround;

            RenderSettings.skybox = null;
            if (fixedFog.HasValue)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogDensity = fixedFog.Value.Density;
                RenderSettings.fogColor = fixedFog.Value.Color;
            }
            else
            {
                // Açık gri lav ovası (ambiyans portu, PR #43): doğrusal sis arena yarıçapına ölçeklenir.
                CombatAmbienceEnvironment.ConfigureArenaFog(tuning);
            }

            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = fixedFog.HasValue ? fixedFog.Value.Color : tuning.Visuals.BackgroundColor;
                camera.farClipPlane = Mathf.Max(ArenaDefaults.MinFarClipM, tuning.Arena.CameraFarClipM);
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
            look.FixedFog = fixedFog.HasValue;
            look.Initialize(volume, sun, camera);
        }

        static void CreateRimLight(GameTuning tuning)
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
