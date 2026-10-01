// Deneme sahnesi: ince lav çatlakları ve ejderha gözleri. Unlit, opak, HDR çekirdek rengi (bloom'u besler).
// UV.x = çatlağın enine (0 kenar, 0.5 çekirdek, 1 kenar), UV.y = boyuna metre (nabız gürültüsü için).
// _Intensity betikten (MaterialPropertyBlock) yazılabilir. _FogStrength < 1 parıltının sisi delmesini sağlar.
Shader "Dovus/Visual/LavaEmissive"
{
    Properties
    {
        [HDR] _CoreColor ("Core color (HDR)", Color) = (4.2, 1.05, 0.18, 1)
        _EdgeColor ("Edge color", Color) = (0.06, 0.045, 0.04, 1)
        _CoreWidth ("Core width", Range(0.05, 1)) = 0.45
        _Intensity ("Intensity", Float) = 1
        _PulseAmount ("Pulse amount", Range(0, 1)) = 0.25
        _PulseSpeed ("Pulse speed", Float) = 0.6
        _PulseScale ("Pulse scale (1/m)", Float) = 0.35
        _FogStrength ("Fog strength", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _CoreColor;
            half4 _EdgeColor;
            half _CoreWidth;
            half _Intensity;
            half _PulseAmount;
            half _PulseSpeed;
            half _PulseScale;
            half _FogStrength;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Unlit"
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash11(float x)
            {
                return frac(sin(x * 127.1) * 43758.5453);
            }

            float Noise1(float x)
            {
                float i = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(Hash11(i), Hash11(i + 1.0), f);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float across = 1.0 - abs(input.uv.x * 2.0 - 1.0);
                // v2: keep the hot core at least ~2 px wide so distant cracks don't break into dashes/sparkle
                // (shading aliasing that MSAA cannot fix); energy is partly compensated when widened.
                float aaW = fwidth(across) * 2.0;
                float coreW = max(_CoreWidth, aaW);
                float core = smoothstep(1.0 - coreW, 1.0, across) * lerp(1.0, _CoreWidth / coreW, 0.6);
                float band = smoothstep(0.1, 1.0, across);
                float t = _Time.y * _PulseSpeed;
                float along = input.uv.y * _PulseScale;
                float n = 0.6 * Noise1(along - t) + 0.4 * Noise1(along * 2.7 + t * 1.3 + 11.0);
                float pulse = (1.0 - _PulseAmount) + _PulseAmount * 2.0 * n;
                float3 hot = _CoreColor.rgb * (core * pulse * _Intensity);
                float3 warmEdge = _EdgeColor.rgb + _CoreColor.rgb * (0.12 * band * _Intensity);
                float3 col = warmEdge + hot;
                float3 fogged = MixFog(col, input.fogFactor);
                col = lerp(col, fogged, _FogStrength);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull Off
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                return half4(input.positionCS.z, 0, 0, 0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
