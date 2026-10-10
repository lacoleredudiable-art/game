// efekt-motoru §0/§4/§8.3: VFX_Kor additive ailesi.
// Shader Graph hedeflenir (URP, GLES-güvenli); bu ortamda HLSL eşdeğeri —
// aynı özellik sözleşmesi: _CoreColor, _Intensity, _FlowOffset, _Dissolve.
// VFX Graph kullanılmaz.
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half _Intensity;
                half _FlowOffset;
                float4 _MainTex_ST;
                half _Dissolve;
                float4 _DissolveTex_ST;
                half _EdgeBoost;
            CBUFFER_END

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_DissolveTex);    SAMPLER(sampler_DissolveTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
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
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alpha = tex.a * input.color.a;
                half noise = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, input.uv).r;
                // Belirme/sönme: _Dissolve yükselince kenardan yanarak yok olur.
                half keep = step(_Dissolve, noise + 0.02h);
                alpha *= keep;
                half3 edge = _CoreColor.rgb * _EdgeBoost;
                half3 col = (_CoreColor.rgb * tex.rgb * input.color.rgb + edge) * _Intensity * alpha;
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
