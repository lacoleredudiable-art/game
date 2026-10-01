using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game
{
    /// <summary>
    /// A/B/C görsel ön ayarları: Volume profili + URP/ışık ayarları. Oynanışa dokunmaz.
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
        static float _savedShadowDistance;
        static int _savedShadowCascades;
        static bool _sunCached;

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
            _owner?.RestoreExtras();
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
                return;
            }

            _sun.shadows = LightShadows.Soft;
            QualitySettings.shadowDistance = 35f;
            QualitySettings.shadowCascades = 2;
        }
    }
}
