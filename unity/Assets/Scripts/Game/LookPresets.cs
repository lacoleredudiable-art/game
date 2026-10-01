using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game
{
    /// <summary>
    /// A/B/C görsel ön ayarları: Volume profili + URP/ışık/sis ayarları. Oynanışa dokunmaz.
    /// </summary>
    public static class LookPresets
    {
        public const string PlayerPrefsKey = "dovus.look";

        const string ResourceFolder = "Look/Look_";

        static Volume _volume;
        static Light _sun;
        static LookPresetController _owner;

        static float _savedRenderScale = 1f;
        static int _savedMsaa = 1;
        static int _savedMainShadowRes = 1024;
        static bool _savedDepthTexture;
        static bool _urpCached;

        static LightShadows _savedSunShadows;
        static float _savedSunStrength;
        static float _savedSunIntensity;
        static Color _savedSunColor;
        static float _savedShadowDistance;
        static int _savedShadowCascades;
        static bool _sunCached;

        static float _savedFogDensity;
        static Color _savedAmbientSky;
        static Color _savedCameraBackground;
        static bool _fogCached;

        static bool _ssaoCached;
        static bool _ssaoWasActive;

        public static char Active { get; private set; } = 'A';

        public static void Bind(LookPresetController owner, Volume volume, Light sun)
        {
            _owner = owner;
            _volume = volume;
            _sun = sun;
            CacheUrpIfNeeded();
            CacheSunIfNeeded();
            CacheFogIfNeeded();
        }

        public static void Apply(char preset)
        {
            preset = char.ToUpperInvariant(preset);
            if (preset != 'A' && preset != 'B' && preset != 'C')
                preset = 'A';

            Active = preset;
            if (_volume != null)
            {
                var profile = Resources.Load<VolumeProfile>($"{ResourceFolder}{PresetSuffix(preset)}");
                if (profile != null)
                    _volume.profile = profile;
            }

            ApplyRenderPipeline(preset);
            ApplySun(preset);
            ApplyFog(preset);
            ApplyCameraBackground(preset);
            _owner?.ApplyPresetExtras(preset);

#if UNITY_EDITOR || DOVUS_DEBUG
            PlayerPrefs.SetString(PlayerPrefsKey, preset.ToString());
            PlayerPrefs.Save();
#endif
        }

        public static void RestoreOnExit()
        {
            RestoreSsao();
            RestoreUrp();
            RestoreSun();
            RestoreFog();
            RestoreCameraBackground();
            _owner?.RestoreExtras();
        }

        /// <summary>Capture log satırı — renderScale, msaa, shadow, fog, SSAO.</summary>
        public static string DescribeActiveSettings()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float renderScale = urp != null ? urp.renderScale : 1f;
            int msaa = urp != null ? urp.msaaSampleCount : 0;
            int shadowRes = urp != null ? urp.mainLightShadowmapResolution : 0;
            float shadowDist = QualitySettings.shadowDistance;
            bool ssao = IsSsaoActive();
            string profile = Active switch
            {
                'B' => "Look_B_Keskin",
                'C' => "Look_C_Gelismis",
                _ => "Look_A_Esit",
            };
            return $"{profile} renderScale={renderScale:0.##} msaa={msaa} shadowRes={shadowRes} " +
                   $"shadowDist={shadowDist:0.#} fogDensity={RenderSettings.fogDensity:0.0000} SSAO={(ssao ? "on" : "off")}";
        }

        static string PresetSuffix(char preset) => preset switch
        {
            'B' => "B_Keskin",
            'C' => "C_Gelismis",
            _ => "A_Esit",
        };

        static void CacheUrpIfNeeded()
        {
            if (_urpCached)
                return;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null)
                return;
            _savedRenderScale = urp.renderScale;
            _savedMsaa = urp.msaaSampleCount;
            _savedMainShadowRes = urp.mainLightShadowmapResolution;
            _savedDepthTexture = urp.supportsCameraDepthTexture;
            _urpCached = true;
        }

        static void RestoreUrp()
        {
            if (!_urpCached)
                return;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null)
                return;
            urp.renderScale = _savedRenderScale;
            urp.msaaSampleCount = _savedMsaa;
            urp.mainLightShadowmapResolution = _savedMainShadowRes;
            urp.supportsCameraDepthTexture = _savedDepthTexture;
        }

        static void ApplyRenderPipeline(char preset)
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null)
                return;

            urp.renderScale = 1f;
            urp.msaaSampleCount = 4;

            if (preset == 'B' || preset == 'C')
                urp.mainLightShadowmapResolution = 2048;
            else
                urp.mainLightShadowmapResolution = _urpCached ? _savedMainShadowRes : urp.mainLightShadowmapResolution;

            bool ssao = preset == 'C';
            if (!_ssaoCached && urp.rendererDataList != null && urp.rendererDataList.Length > 0)
            {
                var rendererData = urp.rendererDataList[0];
                if (rendererData != null)
                {
                    foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
                    {
                        if (feature != null && feature.GetType().Name.Contains("ScreenSpaceAmbientOcclusion"))
                        {
                            _ssaoWasActive = feature.isActive;
                            _ssaoCached = true;
                            break;
                        }
                    }
                }
            }
            if (ssao)
                urp.supportsCameraDepthTexture = true;
            else
                urp.supportsCameraDepthTexture = _urpCached ? _savedDepthTexture : urp.supportsCameraDepthTexture;

            ApplySsaoActive(ssao);
        }

        static bool IsSsaoActive()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null || urp.rendererDataList == null || urp.rendererDataList.Length == 0)
                return false;
            var data = urp.rendererDataList[0];
            if (data == null)
                return false;
            foreach (ScriptableRendererFeature feature in data.rendererFeatures)
            {
                if (feature != null && feature.GetType().Name.Contains("ScreenSpaceAmbientOcclusion"))
                    return feature.isActive;
            }
            return false;
        }

        static void RestoreSsao()
        {
            if (!_ssaoCached)
                return;
            ApplySsaoActive(_ssaoWasActive);
        }

        static void ApplySsaoActive(bool active)
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null || urp.rendererDataList == null || urp.rendererDataList.Length == 0)
                return;
            var data = urp.rendererDataList[0];
            if (data == null)
                return;
            foreach (ScriptableRendererFeature feature in data.rendererFeatures)
            {
                if (feature == null || !feature.GetType().Name.Contains("ScreenSpaceAmbientOcclusion"))
                    continue;
                feature.SetActive(active);
            }
        }

        static void CacheSunIfNeeded()
        {
            if (_sun == null || _sunCached)
                return;
            _savedSunShadows = _sun.shadows;
            _savedSunStrength = _sun.shadowStrength;
            _savedSunIntensity = _sun.intensity;
            _savedSunColor = _sun.color;
            _savedShadowDistance = QualitySettings.shadowDistance;
            _savedShadowCascades = QualitySettings.shadowCascades;
            _sunCached = true;
        }

        static void RestoreSun()
        {
            if (!_sunCached || _sun == null)
                return;
            _sun.shadows = _savedSunShadows;
            _sun.shadowStrength = _savedSunStrength;
            _sun.intensity = _savedSunIntensity;
            _sun.color = _savedSunColor;
            QualitySettings.shadowDistance = _savedShadowDistance;
            QualitySettings.shadowCascades = _savedShadowCascades;
        }

        static void ApplySun(char preset)
        {
            if (_sun == null)
                return;

            if (preset == 'A')
            {
                _sun.shadows = LightShadows.Soft;
                _sun.shadowStrength = _sunCached ? _savedSunStrength : _sun.shadowStrength;
                _sun.intensity = (_sunCached ? _savedSunIntensity : _sun.intensity) * 0.82f;
                _sun.color = _sunCached ? _savedSunColor : _sun.color;
                if (_sunCached)
                {
                    QualitySettings.shadowDistance = _savedShadowDistance;
                    QualitySettings.shadowCascades = _savedShadowCascades;
                }
                return;
            }

            _sun.shadows = preset == 'B' ? LightShadows.Hard : LightShadows.Soft;
            _sun.shadowStrength = preset == 'B' ? 0.95f : (_sunCached ? _savedSunStrength : _sun.shadowStrength);
            _sun.intensity = (_sunCached ? _savedSunIntensity : _sun.intensity) * (preset == 'B' ? 1.26f : 1.1f);
            _sun.color = preset == 'B'
                ? new Color(0.92f, 0.95f, 1f)
                : (_sunCached ? _savedSunColor : _sun.color);
            QualitySettings.shadowDistance = preset == 'B' ? 26f : 32f;
            QualitySettings.shadowCascades = 2;
        }

        static void CacheFogIfNeeded()
        {
            if (_fogCached)
                return;
            _savedFogDensity = RenderSettings.fogDensity;
            _savedAmbientSky = RenderSettings.ambientSkyColor;
            var cam = Camera.main;
            _savedCameraBackground = cam != null ? cam.backgroundColor : Color.gray;
            _fogCached = true;
        }

        static void RestoreCameraBackground()
        {
            if (!_fogCached)
                return;
            var cam = Camera.main;
            if (cam != null)
                cam.backgroundColor = _savedCameraBackground;
        }

        static void ApplyCameraBackground(char preset)
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            Color baseBg = _fogCached ? _savedCameraBackground : cam.backgroundColor;
            cam.backgroundColor = preset switch
            {
                'B' => Color.Lerp(baseBg, new Color(0.66f, 0.70f, 0.76f), 0.92f),
                'C' => Color.Lerp(baseBg, new Color(0.56f, 0.60f, 0.66f), 0.55f),
                _ => Color.Lerp(baseBg, new Color(0.36f, 0.38f, 0.42f), 0.42f),
            };
        }

        static void RestoreFog()
        {
            if (!_fogCached)
                return;
            RenderSettings.fogDensity = _savedFogDensity;
            RenderSettings.ambientSkyColor = _savedAmbientSky;
        }

        static void ApplyFog(char preset)
        {
            float baseDensity = _fogCached ? _savedFogDensity : RenderSettings.fogDensity;
            if (baseDensity <= 0.0001f)
                baseDensity = 0.0032f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            Color sky = _fogCached ? _savedAmbientSky : RenderSettings.ambientSkyColor;
            RenderSettings.fogDensity = preset switch
            {
                'B' => baseDensity * 0.38f,
                'C' => baseDensity * 0.28f,
                _ => baseDensity * 1.05f,
            };
            RenderSettings.ambientSkyColor = preset switch
            {
                'B' => Color.Lerp(sky, new Color(0.52f, 0.56f, 0.62f), 0.35f),
                'C' => Color.Lerp(sky, new Color(0.48f, 0.52f, 0.58f), 0.25f),
                _ => sky,
            };
        }
    }
}
