#ifndef DOVUS_KOR_TRIPLANAR_INCLUDED
#define DOVUS_KOR_TRIPLANAR_INCLUDED

// Dünya-uzayı kor maskesi (ortam_kor_paket: R zemin pul-çatlak, G kaburga çatlak nabzı, B ocak korları).
// VFX_Kor (_MASKMODE_TRIPLANAR) ve Ortam_DitherLit (_KOR_EMISSION) aynı fonksiyonları çağırır.
// Çağıran tanımlamalı: _KorMask + sampler_KorMask, _MaskChannel, _MaskTiling, _MaskBlend, _MaxGlow,
// _PulsePeriod, _PulseMin, _NoiseScale, _NoiseFloor.
//   Tekrar gizleme: tek triplanar örnek × düşük frekans dünya değer gürültüsü (ALU; ek doku okuması yok).
//   DOVUS_KOR_LOW (düşük segment, global): yalnız Y düzlemsel tek örnek.

float KorHash(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

// Düşük frekans dünya değer gürültüsü: maske döşemesinin tekrarını "canlı" bölgelerle kırar.
half KorValueNoise(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = lerp(KorHash(i), KorHash(i + float3(1, 0, 0)), f.x);
    float b = lerp(KorHash(i + float3(0, 1, 0)), KorHash(i + float3(1, 1, 0)), f.x);
    float c = lerp(KorHash(i + float3(0, 0, 1)), KorHash(i + float3(1, 0, 1)), f.x);
    float d = lerp(KorHash(i + float3(0, 1, 1)), KorHash(i + float3(1, 1, 1)), f.x);
    return (half)lerp(lerp(a, b, f.y), lerp(c, d, f.y), f.z);
}

half KorTriplanarMask(float3 positionWS, half3 normalWS)
{
    float3 p = positionWS * _MaskTiling;
#if defined(DOVUS_KOR_LOW)
    half4 m = SAMPLE_TEXTURE2D(_KorMask, sampler_KorMask, p.xz);
#else
    half3 w = pow(abs(normalWS), _MaskBlend);
    w /= max(w.x + w.y + w.z, 1e-4h);
    half4 m = SAMPLE_TEXTURE2D(_KorMask, sampler_KorMask, p.zy) * w.x
            + SAMPLE_TEXTURE2D(_KorMask, sampler_KorMask, p.xz) * w.y
            + SAMPLE_TEXTURE2D(_KorMask, sampler_KorMask, p.xy) * w.z;
#endif
    half v = min(dot(m, _MaskChannel), _MaxGlow);
    return v * lerp(_NoiseFloor, 1.0h, KorValueNoise(positionWS * _NoiseScale));
}

half KorPulse()
{
    if (_PulsePeriod <= 0.0)
        return 1.0h;
    return lerp(_PulseMin, 1.0h, 0.5h + 0.5h * (half)sin(_Time.y * TWO_PI / _PulsePeriod));
}

#endif
