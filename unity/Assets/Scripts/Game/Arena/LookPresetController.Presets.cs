using Dovus.Game.Assets;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dovus.Game.Arena
{
    /// <summary>
    /// A/B/C g??rsel ??n ayarlar??: Volume profili + URP/??????k/sis ayarlar??. Oynan????a dokunmaz.
    /// </summary>
    public sealed partial class LookPresetController
    {
        const string ResourceFolder = "Look/Look_";

        Volume _volume;
        Light _sun;
        Camera _mainCamera;
        float _savedRenderScale = 1f;
        int _savedMsaa = 1;
        int _savedMainShadowRes = LookPresetsDefaults.SavedMainShadowResPx;
        UpscalingFilterSelection _savedUpscalingFilter;
        bool _savedFsrOverrideSharpness;
        float _savedFsrSharpness;
        bool _savedRealtimeReflectionProbes;
        bool _urpCached;

        LightShadows _savedSunShadows;
        float _savedSunStrength;
        float _savedSunIntensity;
        Color _savedSunColor;
        float _savedShadowDistance;
        int _savedShadowCascades;
        float _savedSunBias;
        float _savedSunNormalBias;
        bool _sunCached;

        float _savedFogDensity;
        Color _savedAmbientSky;
        Color _savedCameraBackground;
        bool _fogCached;

        bool _ssaoCached;
        bool _ssaoWasActive;

        public char ActivePreset { get; private set; } = 'B';

        /// <summary>Aktif ??n ayar derinlik dokusu/SSAO gerektiriyor mu (yaln??z 'C').</summary>
        public bool ActiveRequiresDepthTexture => ActivePreset == 'C';

        public void BindVolume(Volume volume, Light sun)
        {
            _volume = volume;
            _sun = sun;
            CacheUrpIfNeeded();
            CacheSunIfNeeded();
            CacheFogIfNeeded();
        }

        public void ApplyPreset(char preset)
        {
            preset = char.ToUpperInvariant(preset);
            if (preset != 'A' && preset != 'B' && preset != 'C')
                preset = 'B';

            ActivePreset = preset;
            if (_volume != null)
            {
                var profile = AssetLoader.Load<VolumeProfile>($"{ResourceFolder}{PresetSuffix(preset)}", null);
                if (profile != null)
                    _volume.profile = profile;
            }

            ApplyRenderPipeline(preset);
            ApplySun(preset);
            ApplyFog(preset);
            ApplyCameraBackground(preset);
            ApplyPresetExtras(preset);

#if UNITY_EDITOR || DOVUS_DEBUG
            PlayerPrefs.SetString(PlayerPrefsKey, preset.ToString());
            PlayerPrefs.Save();
#endif
        }

        public void RestoreOnExit()
        {
            RestoreSsao();
            RestoreUrp();
            RestoreSun();
            RestoreFog();
            RestoreCameraBackground();
            RestoreExtras();
        }

        /// <summary>Capture log sat??r?? ??? renderScale, msaa, shadow, fog, SSAO, sharpen, probe.</summary>
        public string DescribeActiveSettings()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float renderScale = urp != null ? urp.renderScale : 1f;
            int msaa = urp != null ? urp.msaaSampleCount : 0;
            int shadowRes = urp != null ? urp.mainLightShadowmapResolution : 0;
            float shadowDist = QualitySettings.shadowDistance;
            bool ssao = IsSsaoActive();
            bool sharpen = urp != null && urp.upscalingFilter == UpscalingFilterSelection.FSR && urp.fsrOverrideSharpness;
            bool probe = QualitySettings.realtimeReflectionProbes;
            string profile = ActivePreset switch
            {
                'B' => "Look_B_Keskin",
                'C' => "Look_C_Gelismis",
                _ => "Look_A_Esit",
            };
            return $"{profile} renderScale={renderScale:0.##} msaa={msaa} shadowRes={shadowRes} " +
                   $"shadowDist={shadowDist:0.#} shadowBias={(_sun != null ? _sun.shadowBias : 0f):0.000} " +
                   $"fogDensity={RenderSettings.fogDensity:0.0000} SSAO={(ssao ? "on" : "off")} " +
                   $"sharpen={(sharpen ? "on" : "off")} realtimeProbes={(probe ? "on" : "off")}";
        }

        string PresetSuffix(char preset) => preset switch
        {
            'B' => "B_Keskin",
            'C' => "C_Gelismis",
            _ => "A_Esit",
        };

        void CacheUrpIfNeeded()
        {
            if (_urpCached)
                return;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null)
                return;
            _savedRenderScale = urp.renderScale;
            _savedMsaa = urp.msaaSampleCount;
            _savedMainShadowRes = urp.mainLightShadowmapResolution;
            _savedUpscalingFilter = urp.upscalingFilter;
            _savedFsrOverrideSharpness = urp.fsrOverrideSharpness;
            _savedFsrSharpness = urp.fsrSharpness;
#if UNITY_EDITOR && !SWEEP_HEADLESS
            _savedRealtimeReflectionProbes = LookPresetsAssetGuard.RealtimeProbesFromDisk(QualitySettings.GetQualityLevel());
#else
            _savedRealtimeReflectionProbes = QualitySettings.realtimeReflectionProbes;
#endif
            _urpCached = true;
        }

        void RestoreUrp()
        {
            if (!_urpCached)
                return;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null)
                return;
            urp.renderScale = _savedRenderScale;
            urp.msaaSampleCount = _savedMsaa;
            urp.mainLightShadowmapResolution = _savedMainShadowRes;
            urp.upscalingFilter = _savedUpscalingFilter;
            urp.fsrOverrideSharpness = _savedFsrOverrideSharpness;
            urp.fsrSharpness = _savedFsrSharpness;
            QualitySettings.realtimeReflectionProbes = _savedRealtimeReflectionProbes;
        }

        void ApplyRenderPipeline(char preset)
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null)
                return;

            urp.renderScale = 1f;
            urp.msaaSampleCount = 4;

            if (preset == 'B')
                urp.mainLightShadowmapResolution = LookPresetsDefaults.MainLightShadowMapResPx;
            else if (preset == 'C')
                // "Geli??mi??": masa??st??nde (mobil de??ilken) ucuz, daha net g??lge ??? telefonda 2048'de kal??r.
                urp.mainLightShadowmapResolution = Application.isMobilePlatform ? LookPresetsDefaults.ShadowMapResMobilePx : LookPresetsDefaults.ShadowMapResDesktopPx;
            else
                urp.mainLightShadowmapResolution = _urpCached ? _savedMainShadowRes : urp.mainLightShadowmapResolution;

            // B "Keskin": renderScale=1'de bile ??al????an FSR RCAS keskinle??tirme (URP 17/Unity 6 destekliyor,
            // bkz. UniversalRenderPipeline.cs InitializeAdditionalCameraData "still consider 100% render
            // scale an upscaling operation"). task-look-v2b problem 2.
            if (preset == 'B')
            {
                urp.upscalingFilter = UpscalingFilterSelection.FSR;
                urp.fsrOverrideSharpness = true;
                urp.fsrSharpness = LookPresetsDefaults.FsrSharpness;
            }
            else
            {
                urp.upscalingFilter = _urpCached ? _savedUpscalingFilter : urp.upscalingFilter;
                urp.fsrOverrideSharpness = _urpCached ? _savedFsrOverrideSharpness : urp.fsrOverrideSharpness;
                urp.fsrSharpness = _urpCached ? _savedFsrSharpness : urp.fsrSharpness;
            }

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

            // Derinlik dokusu payla????lan URP asset'ine de??il, kameraya ??zel yaz??l??r (task-look-v2b problem 3):
            // asset'e yazmak Play'den ????k????ta kal??c?? diff b??rak??yordu (m_RequireDepthTexture 0???1).
            ApplyCameraOverrides(_mainCamera, ssao);

            // C'de ger??ek zamanl?? yans??ma probu ??al????s??n: proje Very Low/Low kalite seviyesinde
            // realtimeReflectionProbes kapal?? geliyor (QualitySettings.asset) ??? probu etkisiz k??l??yordu.
            bool targetProbes = preset == 'C'
                ? true
                : (_urpCached ? _savedRealtimeReflectionProbes : QualitySettings.realtimeReflectionProbes);
            if (QualitySettings.realtimeReflectionProbes != targetProbes)
                QualitySettings.realtimeReflectionProbes = targetProbes;

            ApplySsaoActive(ssao);
        }

        /// <summary>
        /// SSAO/derinlik gerektiren kameralar?? payla????lan URP asset'i yerine kamera ba????na ayarlar
        /// (<see cref="UniversalAdditionalCameraData.requiresDepthOption"/>) ??? LookCapture'daki tan??
        /// kameralar?? da bunu ??a????r??r, b??ylece asset hi?? kirlenmez.
        /// </summary>
        public void ApplyCameraOverrides(Camera camera, bool requiresDepth)
        {
            if (camera == null)
                return;
            var camData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
                return;
            camData.requiresDepthOption = requiresDepth
                ? CameraOverrideOption.On
                : CameraOverrideOption.UsePipelineSettings;
        }

        bool IsSsaoActive()
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

        void RestoreSsao()
        {
            if (!_ssaoCached)
                return;
            ApplySsaoActive(_ssaoWasActive);
        }

        void ApplySsaoActive(bool active)
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

        void CacheSunIfNeeded()
        {
            if (_sun == null || _sunCached)
                return;
            _savedSunShadows = _sun.shadows;
            _savedSunStrength = _sun.shadowStrength;
            _savedSunIntensity = _sun.intensity;
            _savedSunColor = _sun.color;
            _savedShadowDistance = QualitySettings.shadowDistance;
            _savedShadowCascades = QualitySettings.shadowCascades;
            _savedSunBias = _sun.shadowBias;
            _savedSunNormalBias = _sun.shadowNormalBias;
            _sunCached = true;
        }

        void RestoreSun()
        {
            if (!_sunCached || _sun == null)
                return;
            _sun.shadows = _savedSunShadows;
            _sun.shadowStrength = _savedSunStrength;
            _sun.intensity = _savedSunIntensity;
            _sun.color = _savedSunColor;
            QualitySettings.shadowDistance = _savedShadowDistance;
            QualitySettings.shadowCascades = _savedShadowCascades;
            _sun.shadowBias = _savedSunBias;
            _sun.shadowNormalBias = _savedSunNormalBias;
        }

        void ApplySun(char preset)
        {
            if (_sun == null)
                return;

            if (preset == 'A')
            {
                _sun.shadows = LightShadows.Soft;
                _sun.shadowStrength = _sunCached ? _savedSunStrength : _sun.shadowStrength;
                _sun.intensity = (_sunCached ? _savedSunIntensity : _sun.intensity) * LookPresetsDefaults.SunIntensityMult;
                _sun.color = _sunCached ? _savedSunColor : _sun.color;
                if (_sunCached)
                {
                    QualitySettings.shadowDistance = _savedShadowDistance;
                    QualitySettings.shadowCascades = _savedShadowCascades;
                    _sun.shadowBias = _savedSunBias;
                    _sun.shadowNormalBias = _savedSunNormalBias;
                }
                return;
            }

            _sun.shadows = preset == 'B' ? LightShadows.Hard : LightShadows.Soft;
            _sun.shadowStrength = preset == 'B' ? LookPresetsDefaults.ShadowStrength : (_sunCached ? _savedSunStrength : _sun.shadowStrength);
            _sun.intensity = (_sunCached ? _savedSunIntensity : _sun.intensity) * (preset == 'B' ? LookPresetsDefaults.SunIntensityPresetBMult : LookPresetsDefaults.SunIntensityPresetDefaultMult);
            _sun.color = preset == 'B'
                ? new Color(0.92f, 0.95f, 1f)
                : (_sunCached ? _savedSunColor : _sun.color);
            QualitySettings.shadowDistance = preset == 'B' ? LookPresetsDefaults.PresetBShadowDistanceM : LookPresetsDefaults.ShadowDistanceDefaultM;
            QualitySettings.shadowCascades = 2;
            // B "Keskin": sert, kontak g??lgeler i??in s??k?? cascade + d??????k bias (acne'siz alt s??n??r,
            // Unity varsay??lanlar?? 0.05/0.4'ten biraz daha s??k??). task-look-v2b problem 2.
            _sun.shadowBias = preset == 'B' ? LookPresetsDefaults.ShadowBias : (_sunCached ? _savedSunBias : _sun.shadowBias);
            _sun.shadowNormalBias = preset == 'B' ? LookPresetsDefaults.ShadowNormalBias : (_sunCached ? _savedSunNormalBias : _sun.shadowNormalBias);
        }

        void CacheFogIfNeeded()
        {
            if (_fogCached)
                return;
            _savedFogDensity = RenderSettings.fogDensity;
            _savedAmbientSky = RenderSettings.ambientSkyColor;
            _savedCameraBackground = MainCameraBackgroundOr(Color.gray);
            _fogCached = true;
        }

        void RestoreCameraBackground()
        {
            if (!_fogCached)
                return;
            if (_mainCamera != null)
                _mainCamera.backgroundColor = _savedCameraBackground;
        }

        void ApplyCameraBackground(char preset)
        {
            if (_mainCamera == null)
                return;
            Color baseBg = _fogCached ? _savedCameraBackground : _mainCamera.backgroundColor;
            // Gri g??ky??z?? hedefi |R-B| < 20 olmal?? (task-look-v2b problem 1 do??rulamas??) ??? B/C'nin
            // ??nceki hedef renkleri biraz fazla maviye ka????yordu (??l????len |R-B| 24/19).
            _mainCamera.backgroundColor = preset switch
            {
                'B' => Color.Lerp(baseBg, new Color(0.68f, 0.705f, 0.73f), LookPresetsDefaults.BaseBgPresetBLerp),
                'C' => Color.Lerp(baseBg, new Color(0.56f, 0.59f, 0.63f), LookPresetsDefaults.BaseBgPresetCLerp),
                _ => Color.Lerp(baseBg, new Color(0.36f, 0.38f, LookPresetsDefaults.DefaultPresetBlueChan), LookPresetsDefaults.DefaultPresetLerpWeight),
            };
        }

        Color MainCameraBackgroundOr(Color fallback) =>
            _mainCamera != null ? _mainCamera.backgroundColor : fallback;

        void RestoreFog()
        {
            if (!_fogCached)
                return;
            RenderSettings.fogDensity = _savedFogDensity;
            RenderSettings.ambientSkyColor = _savedAmbientSky;
        }

        void ApplyFog(char preset)
        {
            float baseDensity = _fogCached ? _savedFogDensity : RenderSettings.fogDensity;
            if (baseDensity <= 0.0001f)
                baseDensity = LookPresetsDefaults.FogBaseDensity;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            Color sky = _fogCached ? _savedAmbientSky : RenderSettings.ambientSkyColor;
            RenderSettings.fogDensity = preset switch
            {
                'B' => baseDensity * LookPresetsDefaults.FogDensityPresetBMult,
                'C' => baseDensity * LookPresetsDefaults.FogDensityPresetCMult,
                _ => baseDensity * LookPresetsDefaults.FogDensityScaleDefault,
            };
            RenderSettings.ambientSkyColor = preset switch
            {
                'B' => Color.Lerp(sky, new Color(0.52f, 0.56f, 0.62f), LookPresetsDefaults.SkyPresetBLerp),
                'C' => Color.Lerp(sky, new Color(0.48f, 0.52f, 0.58f), LookPresetsDefaults.SkyPresetCLerp),
                _ => sky,
            };
        }
    }
}
