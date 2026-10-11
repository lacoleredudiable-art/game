// Kaburga kemer gövdesi: Simple Lit ile aynı aydınlatma (UniversalFragmentBlinnPhong, specular kapalı)
// + kemer solması: opak pass'te clip(dither - fade), saydamlık yok (KorDither.hlsl).
// _KOR_EMISSION: kaburga koru ayrı pass değil, emisyon terimi (_KorMask G kanalı, KorTriplanar.hlsl);
//   aynı pikselde hesaplandığı için dither ile birlikte söner.
// _HALF_CLIP: yarım kaburga — nesne uzayında x < _DovusHalfClipX kırpılır; sol yarım aynı mesh'in X aynasıdır.
//   _DovusHalfClipX global (Properties'te yok): kurucu JSON'daki taç bindirmesinden yazar; eşik 0'ın biraz
//   altında olduğundan iki yarım tepede üst üste biner, eğik yarımların arasında yarık kalmaz.
// Malzeme özellikleri bilerek UnityPerMaterial CBUFFER'ında DEĞİL: shader SRP Batcher dışı kalır, böylece
//   aynı mesh+malzemedeki yarım kaburgalar GPU instancing ile birleşir (SRP Batcher açıkken uyumlu shader'da
//   instancing yok sayılır).
Shader "Dovus/Ortam/Ortam_DitherLit"
{
    Properties
    {
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _FadeStrength ("Fade strength", Range(0, 1)) = 0.6
        _FadeScreenCenter ("Fade screen center xy, radius zw", Vector) = (0.5, 0.6, 0.35, 0.45)
        _FadeSoftness ("Fade softness", Range(0.01, 1)) = 0.35
        _FadeNearM ("Fade full until (m)", Float) = 6
        _FadeFarM ("Fade none beyond (m)", Float) = 14

        [Toggle(_HALF_CLIP)] _HalfClip ("Half rib: keep object x >= 0", Float) = 0

        [Toggle(_KOR_EMISSION)] _KorEmission ("Kor emission (triplanar)", Float) = 0
        [HDR] _CoreColor ("Kor color (HDR)", Color) = (4.2, 1.05, 0.18, 1)
        _Intensity ("Kor intensity", Float) = 1
        [NoScaleOffset] _KorMask ("Kor mask RGBA (triplanar)", 2D) = "black" {}
        _MaskChannel ("Mask channel", Vector) = (0, 1, 0, 0)
        _MaskTiling ("Mask tiling (1/m)", Float) = 0.5
        _MaskBlend ("Mask blend sharpness", Range(1, 16)) = 6
        _MaxGlow ("Max glow", Range(0, 1)) = 0.5
        _PulsePeriod ("Pulse period sec (0 = off)", Float) = 60
        _PulseMin ("Pulse min", Range(0, 1)) = 0.6
        _NoiseScale ("Anti-tiling noise (1/m)", Float) = 0.07
        _NoiseFloor ("Anti-tiling noise floor (1 = off)", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
            "UniversalMaterialType" = "SimpleLit"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        float4 _BaseMap_ST;
        half4 _BaseColor;
        half _FadeStrength;
        float4 _FadeScreenCenter;
        half _FadeSoftness;
        float _FadeNearM;
        float _FadeFarM;
        half4 _CoreColor;
        half _Intensity;
        half4 _MaskChannel;
        float _MaskTiling;
        half _MaskBlend;
        half _MaxGlow;
        float _PulsePeriod;
        half _PulseMin;
        float _NoiseScale;
        half _NoiseFloor;
        float _DovusHalfClipX;

        TEXTURE2D(_BaseMap);    SAMPLER(sampler_BaseMap);
        TEXTURE2D(_KorMask);    SAMPLER(sampler_KorMask);

        #include "Assets/Art/Vfx/Shaders/KorDither.hlsl"

        void DovusHalfClip(float objectX)
        {
        #if defined(_HALF_CLIP)
            clip(objectX - _DovusHalfClipX);
        #endif
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ _HALF_CLIP
            #pragma shader_feature_local_fragment _ _KOR_EMISSION
            #pragma multi_compile_fragment _ DOVUS_KOR_LOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Art/Vfx/Shaders/KorTriplanar.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 fogAndVertexLight : TEXCOORD3;
                float objectX : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = nrm.normalWS;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.fogAndVertexLight = half4(ComputeFogFactor(pos.positionCS.z), VertexLighting(pos.positionWS, nrm.normalWS));
                o.objectX = input.positionOS.x;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                DovusHalfClip(input.objectX);
                DovusArchDitherClip(input.positionWS, input.positionCS);

                InputData d = (InputData)0;
                d.positionWS = input.positionWS;
                d.positionCS = input.positionCS;
                d.normalWS = normalize(input.normalWS);
                d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                d.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                d.fogCoord = input.fogAndVertexLight.x;
                d.vertexLighting = input.fogAndVertexLight.yzw;
                d.bakedGI = SampleSH(d.normalWS);
                d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                d.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                s.alpha = 1;
                s.occlusion = 1;
            #if defined(_KOR_EMISSION)
                s.emission = _CoreColor.rgb * (KorTriplanarMask(input.positionWS, d.normalWS) * _Intensity * KorPulse());
            #endif

                half4 c = UniversalFragmentBlinnPhong(d, s);
                c.rgb = MixFog(c.rgb, d.fogCoord);
                c.a = 1;
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma shader_feature_local _ _HALF_CLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float objectX : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                o.positionCS = positionCS;
                o.objectX = input.positionOS.x;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                DovusHalfClip(input.objectX);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ _HALF_CLIP

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float objectX : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.objectX = input.positionOS.x;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                DovusHalfClip(input.objectX);
                DovusArchDitherClip(input.positionWS, input.positionCS);
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
