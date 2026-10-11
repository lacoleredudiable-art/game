using Dovus.Core.Presentation;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §4 skill uyanışı: yoğunluk 1 → 1,6 (0,1 s), teslime kadar tutulur,
    /// toparlanmada 0,25 s'de 1'e. Yalnız yoğunluk eğrisi: karakter kökünde mesh/renderer yok
    /// (eski sabit silindir "beyaz çubuk" görünüyordu; yerine bir şey çıkmaz).
    /// </summary>
    public sealed class KorAwakenView : MonoBehaviour
    {
        enum Phase : byte { Idle, Rise, Hold, Fade }

        Phase _phase = Phase.Idle;
        float _age;
        float _intensity = VfxPlanDefaults.KorIdleIntensity;

        public float Intensity => _intensity;

        public void Begin(in VfxColorRgb color)
        {
            _phase = Phase.Rise;
            _age = 0f;
            _intensity = VfxPlanDefaults.KorIdleIntensity;
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
                        _phase = Phase.Idle;
                    break;
                }
            }
        }
    }
}
