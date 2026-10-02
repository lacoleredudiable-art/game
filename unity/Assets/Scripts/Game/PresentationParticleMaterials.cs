using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game
{
    /// <summary>
    /// Runtime parçacık materyalleri — URP shader'ı Resources'tan referanslanır (Android strip önlemi).
    /// </summary>
    public static class PresentationParticleMaterials
    {
        const string AnchorResourcePath = "Presentation/ParticlesUnlitAnchor";

        static Material _alphaTextured;
        static Material _additiveTextured;
        static Texture2D _dot;

        public static Material AlphaTextured => _alphaTextured ??= Build(false);
        public static Material AdditiveTextured => _additiveTextured ??= Build(true);

        static Material Build(bool additive)
        {
            Shader shader = ResolveShader();
            var m = new Material(shader) { name = additive ? "FxParticle_Add" : "FxParticle_Alpha" };
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", additive ? 2f : 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                m.SetInt("_ZWrite", 0);
            }

            m.renderQueue = (int)RenderQueue.Transparent;
            Texture2D dot = SoftDotTexture();
            if (m.HasProperty("_BaseMap"))
                m.SetTexture("_BaseMap", dot);
            m.mainTexture = dot;
            return m;
        }

        static Shader ResolveShader()
        {
            var anchor = Resources.Load<Material>(AnchorResourcePath);
            if (anchor != null && anchor.shader != null && anchor.shader.name != "Hidden/InternalErrorShader")
                return anchor.shader;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Sprites/Default");
            return shader != null ? shader : Shader.Find("Hidden/Internal-Colored");
        }

        static Texture2D SoftDotTexture()
        {
            if (_dot != null)
                return _dot;
            const int size = 32;
            _dot = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                name = "FxSoftDot"
            };
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                float a = Mathf.Clamp01(1f - d);
                _dot.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }

            _dot.Apply(false, true);
            return _dot;
        }
    }
}
