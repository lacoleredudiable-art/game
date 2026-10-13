namespace Dovus.Game.Cameras
{
    /// <summary>CameraBossFade: URP/Lit saydam kurulumu (oynanış sayısı değil, shader alan adları ve değerleri).</summary>
    public static class CameraBossFadeDefaults
    {
        public const string FadeShaderName = "Universal Render Pipeline/Lit";
        /// <summary>VisualAttach modeli bu adla kökün altına koyar.</summary>
        public const string ModelChildName = "Visual";
        public const string FadedNameSuffix = "_Saydam";
        /// <summary>Kaynak ana doku adayları: URP Lit, Synty Generic_Basic, eski Standard.</summary>
        public static readonly string[] SourceTextureProps = { "_BaseMap", "_Albedo_Map", "_MainTex" };
        public const string BaseMapProp = "_BaseMap";
        public const string BaseColorProp = "_BaseColor";
        public const string LegacyColorProp = "_Color";
        public const string SurfaceProp = "_Surface";
        public const string BlendProp = "_Blend";
        public const string SrcBlendProp = "_SrcBlend";
        public const string DstBlendProp = "_DstBlend";
        public const string SrcBlendAlphaProp = "_SrcBlendAlpha";
        public const string DstBlendAlphaProp = "_DstBlendAlpha";
        public const string ZWriteProp = "_ZWrite";
        public const string AlphaClipProp = "_AlphaClip";
        public const string TransparentKeyword = "_SURFACE_TYPE_TRANSPARENT";
        public const string AlphaTestKeyword = "_ALPHATEST_ON";
        public const string RenderTypeTag = "RenderType";
        public const string RenderTypeTransparent = "Transparent";
        /// <summary>URP Lit _Surface: 1 = saydam.</summary>
        public const float SurfaceTransparent = 1f;
        /// <summary>URP Lit _Blend: 0 = alfa karışımı.</summary>
        public const float BlendAlpha = 0f;
        public const float Off = 0f;
    }
}
