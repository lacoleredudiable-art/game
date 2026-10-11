// efekt-motoru §0/§4/§8.3: VFX_Kor additive ailesi.
// Shader Graph hedeflenir (URP, GLES-güvenli); bu ortamda HLSL eşdeğeri —
// aynı özellik sözleşmesi: _CoreColor, _Intensity, _FlowOffset, _Dissolve.
// VFX Graph kullanılmaz.
//
// _MaskMode UV (varsayılan): mevcut davranış aynen (_MainTex atlas/maske, UV akışı, dissolve).
//   _UV_MASK_CHANNEL: tek kanallı (R8) UV maskesi → dot(tex, _MaskChannel) alfa olur (koza).
// _MaskMode TRIPLANAR: _KorMask dünya konumu + dünya normaliyle 3 eksenden örneklenir (ortam_kor_paket:
//   R zemin pul-çatlak, G kaburga çatlak nabzı, B ocak korları), kanal _MaskChannel ile seçilir.
//   Tekrar gizleme: tek triplanar örnek × düşük frekans dünya değer gürültüsü (ALU; ek doku okuması yok).
//   DOVUS_KOR_LOW (düşük segment, global): yalnız Y düzlemsel tek örnek.
// _ARCH_DITHER: kemer solması, gövdeyle aynı dither maskesi (KorDither.hlsl).
Shader "Dovus/Vfx/VFX_Kor"
{
    Properties
    {
        [HDR] _CoreColor ("Core color (HDR)", Color) = (4.2, 1.05, 0.18, 1)
        _Intensity ("Intensity", Float) = 1
        _FlowOffset ("Flow offset (UV.x)", Float) = 0
        _MainTex ("Atlas / mask", 2D) = "white" {}
        _Dissolve ("Dissolve (0..1)", Range(0, 1)) = 0
        _DissolveTex ("Dissolve noise", 2D) = "gray" {}
        _EdgeBoost ("Edge color boost", Range(0, 1)) = 0.3
        [Toggle] _Additive ("Additive blend", Float) = 1

        [KeywordEnum(UV, Triplanar)] _MaskMode ("Mask mode", Float) = 0
        [Toggle(_UV_MASK_CHANNEL)] _UvMaskChannel ("UV: single-channel mask", Float) = 0
        [NoScaleOffset] _KorMask ("Kor mask RGBA (triplanar)", 2D) = "black" {}
        _MaskChannel ("Mask channel", Vector) = (1, 0, 0, 0)
        _MaskTiling ("Mask tiling (1/m)", Float) = 0.25
        _MaskBlend ("Mask blend sharpness", Range(1, 16)) = 6
        _MaxGlow ("Max glow", Range(0, 1)) = 1
        _PulsePeriod ("Pulse period sec (0 = off)", Float) = 0
        _PulseMin ("Pulse min", Range(0, 1)) = 0.6
        _NoiseScale ("Anti-tiling noise (1/m)", Float) = 0.07
        _NoiseFloor ("Anti-tiling noise floor (1 = off)", Range(0, 1)) = 1

        [Toggle(_ARCH_DITHER)] _ArchDither ("Arch dither fade", Float) = 0
        _FadeStrength ("Fade strength", Range(0, 1)) = 0.6
        _FadeScreenCenter ("Fade screen center xy, radius zw", Vector) = (0.5, 0.6, 0.35, 0.45)
        _FadeSoftness ("Fade softness", Range(0.01, 1)) = 0.35
        _FadeNearM ("Fade full until (m)", Float) = 6
        _FadeFarM ("Fade none beyond (m)", Float) = 14
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "VFX_Kor"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local _MASKMODE_UV _MASKMODE_TRIPLANAR
            #pragma shader_feature_local_fragment _ _UV_MASK_CHANNEL
            #pragma shader_feature_local _ _ARCH_DITHER
            #pragma multi_compile_fragment _ DOVUS_KOR_LOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half _Intensity;
                half _FlowOffset;
                float4 _MainTex_ST;
                half _Dissolve;
                float4 _DissolveTex_ST;
                half _EdgeBoost;
                half4 _MaskChannel;
                float _MaskTiling;
                half _MaskBlend;
                half _MaxGlow;
                float _PulsePeriod;
                half _PulseMin;
                float _NoiseScale;
                half _NoiseFloor;
                half _FadeStrength;
                float4 _FadeScreenCenter;
                half _FadeSoftness;
                float _FadeNearM;
                float _FadeFarM;
            CBUFFER_END

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_DissolveTex);    SAMPLER(sampler_DissolveTex);
            TEXTURE2D(_KorMask);        SAMPLER(sampler_KorMask);

            #include "Assets/Art/Vfx/Shaders/KorDither.hlsl"

            #if defined(_MASKMODE_TRIPLANAR) || defined(_ARCH_DITHER)
                #define KOR_NEEDS_WORLD 1
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            #if defined(_MASKMODE_TRIPLANAR)
                float3 normalOS : NORMAL;
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            #if defined(KOR_NEEDS_WORLD)
                float3 positionWS : TEXCOORD1;
            #endif
            #if defined(_MASKMODE_TRIPLANAR)
                half3 normalWS : TEXCOORD2;
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.uv.x += _FlowOffset;
                o.color = input.color;
            #if defined(KOR_NEEDS_WORLD)
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            #endif
            #if defined(_MASKMODE_TRIPLANAR)
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
            #endif
                return o;
            }

            #if defined(_MASKMODE_TRIPLANAR)
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

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
            #if defined(_ARCH_DITHER)
                DovusArchDitherClip(input.positionWS, input.positionCS);
            #endif
            #if defined(_MASKMODE_TRIPLANAR)
                half mask = KorTriplanarMask(input.positionWS, normalize(input.normalWS)) * input.color.a;
                half3 tri = _CoreColor.rgb * input.color.rgb * mask * _Intensity * KorPulse();
                return half4(tri, mask);
            #else
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            #if defined(_UV_MASK_CHANNEL)
                tex = half4(1.0h, 1.0h, 1.0h, dot(tex, _MaskChannel));
            #endif
                half alpha = tex.a * input.color.a;
                half noise = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, input.uv).r;
                // Belirme/sönme: _Dissolve yükselince kenardan yanarak yok olur.
                half keep = step(_Dissolve, noise + 0.02h);
                alpha *= keep;
                half3 edge = _CoreColor.rgb * _EdgeBoost;
                half3 col = (_CoreColor.rgb * tex.rgb * input.color.rgb + edge) * _Intensity * alpha;
                return half4(col, alpha);
            #endif
            }
            ENDHLSL
        }
    }
    FallBack Off
}
