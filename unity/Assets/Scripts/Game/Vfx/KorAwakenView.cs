using Dovus.Core.Presentation;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §4 skill uyanışı: silah çatlak/rün yuvaları _Intensity 1 → 1,6 (0,1 s),
    /// teslime kadar tutulur, toparlanmada 0,25 s'de 1'e. Yalnız görünüm.
    /// </summary>
    public sealed class KorAwakenView : MonoBehaviour
    {
        enum Phase : byte { Idle, Rise, Hold, Fade }

        Material _mat;
        Renderer _glow;
        Phase _phase = Phase.Idle;
        float _age;
        float _intensity = VfxPlanDefaults.KorIdleIntensity;
        Color _core = new Color(
            VfxPlanDefaults.HareketR * RuleVfxArtDefaults.CoreHdrMult,
            VfxPlanDefaults.HareketG * RuleVfxArtDefaults.CoreHdrMult,
            VfxPlanDefaults.HareketB * RuleVfxArtDefaults.CoreHdrMult,
            1f);

        public float Intensity => _intensity;

        public void Begin(in VfxColorRgb color)
        {
            EnsureGlow();
            _core = new Color(
                color.R * RuleVfxArtDefaults.CoreHdrMult,
                color.G * RuleVfxArtDefaults.CoreHdrMult,
                color.B * RuleVfxArtDefaults.CoreHdrMult,
                1f);
            _phase = Phase.Rise;
            _age = 0f;
            _intensity = VfxPlanDefaults.KorIdleIntensity;
            Apply();
        }

        public void SignalDelivery()
        {
            if (_phase == Phase.Idle)
                return;
            _phase = Phase.Fade;
            _age = 0f;
        }

        public void Cancel()
        {
            _phase = Phase.Idle;
            _intensity = VfxPlanDefaults.KorIdleIntensity;
            Apply();
            if (_glow != null)
                _glow.enabled = false;
        }

        void Update()
        {
            if (_phase == Phase.Idle)
                return;
            _age += Time.deltaTime;
            switch (_phase)
            {
                case Phase.Rise:
                {
                    float u = Mathf.Clamp01(_age / VfxPlanDefaults.KorAwakenRiseSec);
                    _intensity = Mathf.Lerp(
                        VfxPlanDefaults.KorIdleIntensity,
                        VfxPlanDefaults.KorAwakenPeak,
                        u);
                    if (u >= 1f)
                    {
                        _phase = Phase.Hold;
                        _age = 0f;
                    }
                    break;
                }
                case Phase.Hold:
                    _intensity = VfxPlanDefaults.KorAwakenPeak;
                    break;
                case Phase.Fade:
                {
                    float u = Mathf.Clamp01(_age / VfxPlanDefaults.KorAwakenFadeSec);
                    _intensity = Mathf.Lerp(
                        VfxPlanDefaults.KorAwakenPeak,
                        VfxPlanDefaults.KorIdleIntensity,
                        u);
                    if (u >= 1f)
                    {
                        _phase = Phase.Idle;
                        if (_glow != null)
                            _glow.enabled = false;
                    }
                    break;
                }
            }

            Apply();
        }

        void EnsureGlow()
        {
            if (_glow != null)
            {
                _glow.enabled = true;
                return;
            }

            var go = new GameObject("KorAwakenGlow");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(
                RuleVfxDefaults.AwakenGlowLocalX,
                RuleVfxDefaults.WeaponTipLocalY * 0.5f,
                RuleVfxDefaults.AwakenGlowLocalZ);
            go.transform.localScale = new Vector3(
                RuleVfxDefaults.AwakenGlowScaleX,
                RuleVfxDefaults.AwakenGlowScaleY,
                RuleVfxDefaults.AwakenGlowScaleZ);
            go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Cylinder);
            _glow = go.AddComponent<MeshRenderer>();
            _glow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _glow.receiveShadows = false;
            Shader sh = Shader.Find(RuleVfxDefaults.KorShaderName)
                ?? Shader.Find(RuleVfxDefaults.ParticlesUnlit);
            _mat = new Material(sh);
            _glow.sharedMaterial = _mat;
        }

        void Apply()
        {
            if (_mat == null)
                return;
            if (_mat.HasProperty("_CoreColor"))
                _mat.SetColor("_CoreColor", _core);
            else if (_mat.HasProperty("_BaseColor"))
                _mat.SetColor("_BaseColor", _core);
            if (_mat.HasProperty("_Intensity"))
                _mat.SetFloat("_Intensity", _intensity);
            if (_mat.HasProperty("_Color"))
            {
                Color c = _core * _intensity;
                c.a = Mathf.Clamp01(_intensity - RuleVfxArtDefaults.AwakenAlphaBias);
                _mat.SetColor("_Color", c);
            }

            if (_glow != null)
                _glow.enabled = _phase != Phase.Idle
                    || _intensity > VfxPlanDefaults.KorIdleIntensity + RuleVfxDefaults.AwakenIdleEpsilon;
        }

        void OnDestroy()
        {
            if (_mat != null)
                Destroy(_mat);
        }
    }
}
