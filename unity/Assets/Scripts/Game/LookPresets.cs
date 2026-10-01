using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        static UpscalingFilterSelection _savedUpscalingFilter;
        static bool _savedFsrOverrideSharpness;
        static float _savedFsrSharpness;
        static bool _savedRealtimeReflectionProbes;
        static bool _urpCached;

        static LightShadows _savedSunShadows;
        static float _savedSunStrength;
        static float _savedSunIntensity;
        static Color _savedSunColor;
        static float _savedShadowDistance;
        static int _savedShadowCascades;
        static float _savedSunBias;
        static float _savedSunNormalBias;
        static bool _sunCached;

        static float _savedFogDensity;
        static Color _savedAmbientSky;
        static Color _savedCameraBackground;
        static bool _fogCached;

        static bool _ssaoCached;
        static bool _ssaoWasActive;

        public static char Active { get; private set; } = 'A';

        /// <summary>Aktif ön ayar derinlik dokusu/SSAO gerektiriyor mu (yalnız 'C').</summary>
        public static bool ActiveRequiresDepthTexture => Active == 'C';

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

        /// <summary>Capture log satırı — renderScale, msaa, shadow, fog, SSAO, sharpen, probe.</summary>
        public static string DescribeActiveSettings()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float renderScale = urp != null ? urp.renderScale : 1f;
            int msaa = urp != null ? urp.msaaSampleCount : 0;
            int shadowRes = urp != null ? urp.mainLightShadowmapResolution : 0;
            float shadowDist = QualitySettings.shadowDistance;
            bool ssao = IsSsaoActive();
            bool sharpen = urp != null && urp.upscalingFilter == UpscalingFilterSelection.FSR && urp.fsrOverrideSharpness;
            bool probe = QualitySettings.realtimeReflectionProbes;
            string profile = Active switch
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
            _savedUpscalingFilter = urp.upscalingFilter;
            _savedFsrOverrideSharpness = urp.fsrOverrideSharpness;
            _savedFsrSharpness = urp.fsrSharpness;
            _savedRealtimeReflectionProbes = QualitySettings.realtimeReflectionProbes;
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
            urp.upscalingFilter = _savedUpscalingFilter;
            urp.fsrOverrideSharpness = _savedFsrOverrideSharpness;
            urp.fsrSharpness = _savedFsrSharpness;
            QualitySettings.realtimeReflectionProbes = _savedRealtimeReflectionProbes;
        }

        static void ApplyRenderPipeline(char preset)
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null)
                return;

            urp.renderScale = 1f;
            urp.msaaSampleCount = 4;

            if (preset == 'B')
                urp.mainLightShadowmapResolution = 2048;
            else if (preset == 'C')
                // "Gelişmiş": masaüstünde (mobil değilken) ucuz, daha net gölge — telefonda 2048'de kalır.
                urp.mainLightShadowmapResolution = Application.isMobilePlatform ? 2048 : 4096;
            else
                urp.mainLightShadowmapResolution = _urpCached ? _savedMainShadowRes : urp.mainLightShadowmapResolution;

            // B "Keskin": renderScale=1'de bile çalışan FSR RCAS keskinleştirme (URP 17/Unity 6 destekliyor,
            // bkz. UniversalRenderPipeline.cs InitializeAdditionalCameraData "still consider 100% render
            // scale an upscaling operation"). task-look-v2b problem 2.
            if (preset == 'B')
            {
                urp.upscalingFilter = UpscalingFilterSelection.FSR;
                urp.fsrOverrideSharpness = true;
                urp.fsrSharpness = 0.82f;
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

            // Derinlik dokusu paylaşılan URP asset'ine değil, kameraya özel yazılır (task-look-v2b problem 3):
            // asset'e yazmak Play'den çıkışta kalıcı diff bırakıyordu (m_RequireDepthTexture 0→1).
            ApplyCameraOverrides(Camera.main, ssao);

            // C'de gerçek zamanlı yansıma probu çalışsın: proje Very Low/Low kalite seviyesinde
            // realtimeReflectionProbes kapalı geliyor (QualitySettings.asset) — probu etkisiz kılıyordu.
            QualitySettings.realtimeReflectionProbes = preset == 'C'
                ? true
                : (_urpCached ? _savedRealtimeReflectionProbes : QualitySettings.realtimeReflectionProbes);

            ApplySsaoActive(ssao);
        }

        /// <summary>
        /// SSAO/derinlik gerektiren kameraları paylaşılan URP asset'i yerine kamera başına ayarlar
        /// (<see cref="UniversalAdditionalCameraData.requiresDepthOption"/>) — LookCapture'daki tanı
        /// kameraları da bunu çağırır, böylece asset hiç kirlenmez.
        /// </summary>
        public static void ApplyCameraOverrides(Camera camera, bool requiresDepth)
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
            _savedSunBias = _sun.shadowBias;
            _savedSunNormalBias = _sun.shadowNormalBias;
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
            _sun.shadowBias = _savedSunBias;
            _sun.shadowNormalBias = _savedSunNormalBias;
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
                    _sun.shadowBias = _savedSunBias;
                    _sun.shadowNormalBias = _savedSunNormalBias;
                }
                return;
            }

            _sun.shadows = preset == 'B' ? LightShadows.Hard : LightShadows.Soft;
            _sun.shadowStrength = preset == 'B' ? 0.95f : (_sunCached ? _savedSunStrength : _sun.shadowStrength);
            _sun.intensity = (_sunCached ? _savedSunIntensity : _sun.intensity) * (preset == 'B' ? 1.26f : 1.1f);
            _sun.color = preset == 'B'
                ? new Color(0.92f, 0.95f, 1f)
                : (_sunCached ? _savedSunColor : _sun.color);
            QualitySettings.shadowDistance = preset == 'B' ? 27f : 32f;
            QualitySettings.shadowCascades = 2;
            // B "Keskin": sert, kontak gölgeler için sıkı cascade + düşük bias (acne'siz alt sınır,
            // Unity varsayılanları 0.05/0.4'ten biraz daha sıkı). task-look-v2b problem 2.
            _sun.shadowBias = preset == 'B' ? 0.028f : (_sunCached ? _savedSunBias : _sun.shadowBias);
            _sun.shadowNormalBias = preset == 'B' ? 0.28f : (_sunCached ? _savedSunNormalBias : _sun.shadowNormalBias);
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
            // Gri gökyüzü hedefi |R-B| < 20 olmalı (task-look-v2b problem 1 doğrulaması) — B/C'nin
            // önceki hedef renkleri biraz fazla maviye kaçıyordu (ölçülen |R-B| 24/19).
            cam.backgroundColor = preset switch
            {
                'B' => Color.Lerp(baseBg, new Color(0.68f, 0.705f, 0.73f), 0.92f),
                'C' => Color.Lerp(baseBg, new Color(0.56f, 0.59f, 0.63f), 0.55f),
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

#if UNITY_EDITOR
    /// <summary>
    /// task-look-v2b problem 3: <see cref="LookPresets"/>'in play-mode-içi statik cache'i (CacheUrpIfNeeded)
    /// domain reload sınırları arasında yanlış bir "temel" değeri kalıcı kılabilir (bir önceki koşu düzgün
    /// kapanmadıysa) — ayrıca Play ortasında herhangi bir script derlemesi (ör. "Recompile And Continue
    /// Playing", veya ajan RunCommand araçlarının her çağrıda derleme yapması) sıradan `static` alanları
    /// sıfırlar; bu yüzden anlık görüntü düz `static` alanlarda değil, reload'tan sağ çıkan
    /// <see cref="SessionState"/>'te saklanır. Güvenlik ağı: Play'e girmeden hemen önce paylaşılan URP
    /// asset'ini/QualitySettings'i diskten tazece anlık görüntüle, Play'den çıkarken aynı değerlere geri
    /// yaz ve kirli işaretini temizle — böylece bu dosyadaki hiçbir ön ayar projeye kalıcı diff bırakmaz.
    /// </summary>
    static class LookPresetsAssetGuard
    {
        const string KeyHasSnapshot = "dovus.look.guard.hasSnapshot";
        const string KeyRenderScale = "dovus.look.guard.renderScale";
        const string KeyMsaa = "dovus.look.guard.msaa";
        const string KeyShadowRes = "dovus.look.guard.shadowRes";
        const string KeyUpscalingFilter = "dovus.look.guard.upscalingFilter";
        const string KeyFsrOverride = "dovus.look.guard.fsrOverride";
        const string KeyFsrSharpness = "dovus.look.guard.fsrSharpness";
        const string KeyShadowDistance = "dovus.look.guard.shadowDistance";
        const string KeyShadowCascades = "dovus.look.guard.shadowCascades";
        const string KeyRealtimeProbes = "dovus.look.guard.realtimeProbes";

        [InitializeOnLoadMethod]
        static void Register()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                Snapshot();
            else if (state == PlayModeStateChange.ExitingPlayMode)
                Restore();
        }

        static void Snapshot()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null)
            {
                SessionState.SetBool(KeyHasSnapshot, false);
                return;
            }

            SessionState.SetFloat(KeyRenderScale, urp.renderScale);
            SessionState.SetInt(KeyMsaa, urp.msaaSampleCount);
            SessionState.SetInt(KeyShadowRes, urp.mainLightShadowmapResolution);
            SessionState.SetInt(KeyUpscalingFilter, (int)urp.upscalingFilter);
            SessionState.SetBool(KeyFsrOverride, urp.fsrOverrideSharpness);
            SessionState.SetFloat(KeyFsrSharpness, urp.fsrSharpness);
            SessionState.SetFloat(KeyShadowDistance, QualitySettings.shadowDistance);
            SessionState.SetInt(KeyShadowCascades, QualitySettings.shadowCascades);
            SessionState.SetBool(KeyRealtimeProbes, QualitySettings.realtimeReflectionProbes);
            SessionState.SetBool(KeyHasSnapshot, true);
        }

        static void Restore()
        {
            if (!SessionState.GetBool(KeyHasSnapshot, false))
                return;

            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.renderScale = SessionState.GetFloat(KeyRenderScale, urp.renderScale);
                urp.msaaSampleCount = SessionState.GetInt(KeyMsaa, urp.msaaSampleCount);
                urp.mainLightShadowmapResolution = SessionState.GetInt(KeyShadowRes, urp.mainLightShadowmapResolution);
                urp.upscalingFilter = (UpscalingFilterSelection)SessionState.GetInt(KeyUpscalingFilter, (int)urp.upscalingFilter);
                urp.fsrOverrideSharpness = SessionState.GetBool(KeyFsrOverride, urp.fsrOverrideSharpness);
                urp.fsrSharpness = SessionState.GetFloat(KeyFsrSharpness, urp.fsrSharpness);
                EditorUtility.ClearDirty(urp);
            }

            QualitySettings.shadowDistance = SessionState.GetFloat(KeyShadowDistance, QualitySettings.shadowDistance);
            QualitySettings.shadowCascades = SessionState.GetInt(KeyShadowCascades, QualitySettings.shadowCascades);
            QualitySettings.realtimeReflectionProbes = SessionState.GetBool(KeyRealtimeProbes, QualitySettings.realtimeReflectionProbes);
            SessionState.SetBool(KeyHasSnapshot, false);
        }
    }
#endif
}
