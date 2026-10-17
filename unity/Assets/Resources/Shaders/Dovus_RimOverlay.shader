// SI-3.7: oyuncu karakteri ve silahı için Fresnel kenar ışığı (ikinci materyal slotu, toplamalı).
// Özellik adları _Rim… ile başlar: HitFlashView (_BaseColor/_Color/_EmissionColor) bu materyale dokunmaz.
// Resources/ altında: Shader.Find derlemede de bulunsun diye (stripping'e karşı).
Shader "Dovus/Characters/RimOverlay"
{
    Properties
    {
        [HDR] _RimColor ("Rim color", Color) = (0.811, 0.847, 0.878, 1)
        _RimPower ("Rim power", Float) = 3
        _RimIntensity ("Rim intensity", Float) = 0.35
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
        Cull Back

        Pass
        {
            Name "RimOverlay"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _RimColor;
                half _RimPower;
                half _RimIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.normalWS = (half3)TransformObjectToWorldNormal(input.normalOS);
                o.viewDirWS = GetWorldSpaceViewDir(p.positionWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                half3 v = (half3)normalize(i.viewDirWS);
                half fres = pow(saturate(1.0h - saturate(dot(n, v))), _RimPower);
                return half4(_RimColor.rgb * (fres * _RimIntensity), 1.0h);
            }
            ENDHLSL
        }
    }
}
