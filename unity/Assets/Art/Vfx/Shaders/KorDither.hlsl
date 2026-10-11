#ifndef DOVUS_KOR_DITHER_INCLUDED
#define DOVUS_KOR_DITHER_INCLUDED

// Kemer solması (dunya-ortam, 11 Ekim): kameraya yakın + kadrajın üst-ortasındaki kemer parçası
// yarı saydam DEĞİL, opak pass'te 4x4 Bayer dither ile kırpılır: clip(dither - fade).
// Gövde (Ortam_DitherLit) ve toplamalı kor katmanı (VFX_Kor, _ARCH_DITHER) bu dosyayı aynı
// malzeme parametreleriyle çağırır → aynı pikseller söner, havada kor çizgisi kalmaz.
// Çağıran UnityPerMaterial'da tanımlamalı: _FadeStrength, _FadeScreenCenter (xy merkez, zw yarıçap),
// _FadeSoftness, _FadeNearM, _FadeFarM.

float DovusBayer2(float2 a)
{
    a = floor(a);
    return frac(a.x * 0.5 + a.y * a.y * 0.75);
}

// 0..15/16 basamakları; yalnız ALU (dizi/bit işlemi yok, GLES-güvenli).
float DovusBayer4(float2 pixel)
{
    return DovusBayer2(0.5 * pixel) * 0.25 + DovusBayer2(pixel);
}

half DovusArchFade(float3 positionWS, float4 positionCS)
{
    float2 uv = GetNormalizedScreenSpaceUV(positionCS);
    float2 d = (uv - _FadeScreenCenter.xy) / max(_FadeScreenCenter.zw, float2(1e-3, 1e-3));
    half screenMask = 1.0h - smoothstep(1.0h - _FadeSoftness, 1.0h, (half)length(d));
    float dist = distance(GetCameraPositionWS(), positionWS);
    half nearMask = 1.0h - smoothstep(_FadeNearM, _FadeFarM, dist);
    return _FadeStrength * screenMask * nearMask;
}

void DovusArchDitherClip(float3 positionWS, float4 positionCS)
{
    // (i + 0.5) / 16: fade 0 hiçbir pikseli kırpmaz, fade 0.6 → 16 pikselden 6'sı kalır (%37.5).
    clip(DovusBayer4(positionCS.xy) + 0.03125 - DovusArchFade(positionWS, positionCS));
}

#endif
