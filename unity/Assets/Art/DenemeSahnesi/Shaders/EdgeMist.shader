// Deneme sahnesi: oyun alanı kenarında kalınlaşan sis bandı. Saydam, ZWrite kapalı, tek çizim.
// UV.x = 0 alt (yoğun), 1 üst (şeffaf); UV.y = halka boyunca 0..1.
Shader "Dovus/Visual/EdgeMist"
{
    Properties
    {
        _MistColor ("Mist color", Color) = (0.72, 0.745, 0.76, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.38
        _Falloff ("Vertical falloff", Range(0.5, 4)) = 1.6
        _Drift ("Drift speed", Float) = 0.004
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _MistColor;
            half _Alpha;
            half _Falloff;
            half _Drift;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Unlit"
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

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

            float Wave(float x)
            {
                return 0.5 + 0.5 * sin(x);
            }

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
                float v = pow(saturate(1.0 - input.uv.x), _Falloff);
                float u = input.uv.y * 6.2831853;
                float t = _Time.y * _Drift * 6.2831853;
                float breakup = 0.65 + 0.35 * (0.6 * Wave(u * 7.0 + t * 3.0) + 0.4 * Wave(u * 19.0 - t * 5.0 + 1.7));
                float a = saturate(_Alpha * v * breakup);
                float3 col = MixFog(_MistColor.rgb, input.fogFactor);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
