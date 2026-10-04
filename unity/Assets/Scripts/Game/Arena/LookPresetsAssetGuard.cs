using Dovus.Game.Assets;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dovus.Game.Arena
{
#if UNITY_EDITOR && !SWEEP_HEADLESS
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
        internal static bool RealtimeProbesFromDisk(int qualityLevel) =>
            ReadRealtimeProbesFromQualityAsset(qualityLevel);

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
        const string KeyQualityLevel = "dovus.look.guard.qualityLevel";
        const string QualitySettingsAssetPath = "ProjectSettings/QualitySettings.asset";

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
            else if (state == PlayModeStateChange.EnteredEditMode)
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

            int qualityLevel = QualitySettings.GetQualityLevel();
            SessionState.SetInt(KeyQualityLevel, qualityLevel);

            SessionState.SetFloat(KeyRenderScale, urp.renderScale);
            SessionState.SetInt(KeyMsaa, urp.msaaSampleCount);
            SessionState.SetInt(KeyShadowRes, urp.mainLightShadowmapResolution);
            SessionState.SetInt(KeyUpscalingFilter, (int)urp.upscalingFilter);
            SessionState.SetBool(KeyFsrOverride, urp.fsrOverrideSharpness);
            SessionState.SetFloat(KeyFsrSharpness, urp.fsrSharpness);
            ReadShadowFromQualityAsset(qualityLevel, out float diskShadowDistance, out int diskShadowCascades);
            SessionState.SetFloat(KeyShadowDistance, diskShadowDistance);
            SessionState.SetInt(KeyShadowCascades, diskShadowCascades);
            SessionState.SetBool(KeyRealtimeProbes, ReadRealtimeProbesFromQualityAsset(qualityLevel));
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

            int qualityLevel = SessionState.GetInt(KeyQualityLevel, QualitySettings.GetQualityLevel());
            float shadowDistance = SessionState.GetFloat(KeyShadowDistance, QualitySettings.shadowDistance);
            int shadowCascades = SessionState.GetInt(KeyShadowCascades, QualitySettings.shadowCascades);
            if (QualitySettings.shadowDistance != shadowDistance)
                QualitySettings.shadowDistance = shadowDistance;
            if (QualitySettings.shadowCascades != shadowCascades)
                QualitySettings.shadowCascades = shadowCascades;
            WriteShadowToQualityAsset(qualityLevel, shadowDistance, shadowCascades);

            bool realtimeProbes = SessionState.GetBool(KeyRealtimeProbes, ReadRealtimeProbesFromQualityAsset(qualityLevel));
            if (QualitySettings.realtimeReflectionProbes != realtimeProbes)
                QualitySettings.realtimeReflectionProbes = realtimeProbes;
            WriteRealtimeProbesToQualityAsset(qualityLevel, realtimeProbes);

            SessionState.SetBool(KeyHasSnapshot, false);
        }

        static bool ReadRealtimeProbesFromQualityAsset(int qualityLevel)
        {
            if (TryQualityLevelProperty(qualityLevel, "realtimeReflectionProbes", out SerializedProperty prop) && prop.propertyType == SerializedPropertyType.Boolean)
                return prop.boolValue;
            return QualitySettings.realtimeReflectionProbes;
        }

        static void ReadShadowFromQualityAsset(int qualityLevel, out float shadowDistance, out int shadowCascades)
        {
            shadowDistance = QualitySettings.shadowDistance;
            shadowCascades = QualitySettings.shadowCascades;
            if (TryQualityLevelProperty(qualityLevel, "shadowDistance", out SerializedProperty distProp) && distProp.propertyType == SerializedPropertyType.Float)
                shadowDistance = distProp.floatValue;
            if (TryQualityLevelProperty(qualityLevel, "shadowCascades", out SerializedProperty cascProp) && cascProp.propertyType == SerializedPropertyType.Integer)
                shadowCascades = cascProp.intValue;
        }

        static void WriteShadowToQualityAsset(int qualityLevel, float shadowDistance, int shadowCascades)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(QualitySettingsAssetPath))
            {
                var so = new SerializedObject(asset);
                SerializedProperty levels = so.FindProperty("m_QualitySettings");
                if (levels == null || qualityLevel < 0 || qualityLevel >= levels.arraySize)
                    continue;
                SerializedProperty level = levels.GetArrayElementAtIndex(qualityLevel);
                SerializedProperty distProp = level.FindPropertyRelative("shadowDistance");
                SerializedProperty cascProp = level.FindPropertyRelative("shadowCascades");
                bool dirty = false;
                if (distProp != null && Mathf.Abs(distProp.floatValue - shadowDistance) > 0.0001f)
                {
                    distProp.floatValue = shadowDistance;
                    dirty = true;
                }

                if (cascProp != null && cascProp.intValue != shadowCascades)
                {
                    cascProp.intValue = shadowCascades;
                    dirty = true;
                }

                if (dirty)
                    so.ApplyModifiedPropertiesWithoutUndo();
                else
                    EditorUtility.ClearDirty(asset);
            }
        }

        static bool TryQualityLevelProperty(int qualityLevel, string relativeName, out SerializedProperty prop)
        {
            prop = null;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(QualitySettingsAssetPath))
            {
                var so = new SerializedObject(asset);
                SerializedProperty levels = so.FindProperty("m_QualitySettings");
                if (levels == null || qualityLevel < 0 || qualityLevel >= levels.arraySize)
                    continue;
                prop = levels.GetArrayElementAtIndex(qualityLevel).FindPropertyRelative(relativeName);
                if (prop != null)
                    return true;
            }

            return false;
        }

        static void WriteRealtimeProbesToQualityAsset(int qualityLevel, bool value)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(QualitySettingsAssetPath))
            {
                var so = new SerializedObject(asset);
                SerializedProperty levels = so.FindProperty("m_QualitySettings");
                if (levels == null || qualityLevel < 0 || qualityLevel >= levels.arraySize)
                    continue;
                SerializedProperty prop = levels.GetArrayElementAtIndex(qualityLevel).FindPropertyRelative("realtimeReflectionProbes");
                if (prop == null || prop.boolValue == value)
                {
                    EditorUtility.ClearDirty(asset);
                    continue;
                }

                prop.boolValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
#endif
}
