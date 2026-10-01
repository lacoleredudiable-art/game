// Deneme sahnesi: ufuk silüeti (uzak dağ şeridi). Unlit, opak, dikey gri degrade.
// UV.x = 0 alt, 1 üst. _FogStrength < 1: sis silüeti tamamen yutmaz, ölçek hissi kalır.
Shader "Dovus/Visual/FogSilhouette"
{
    Properties
    {
        _BottomColor ("Bottom color", Color) = (0.47, 0.51, 0.54, 1)
        _TopColor ("Top color", Color) = (0.62, 0.66, 0.69, 1)
        _FogStrength ("Fog strength", Range(0, 1)) = 0.72
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+10" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BottomColor;
            half4 _TopColor;
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float h = saturate(input.uv.x);
                float3 col = lerp(_BottomColor.rgb, _TopColor.rgb, h * h);
                float3 fogged = MixFog(col, input.fogFactor);
                col = lerp(col, fogged, _FogStrength);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
