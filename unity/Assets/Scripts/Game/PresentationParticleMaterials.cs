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

        // Mermi gölgesi/parıltısı gibi elle çizilen quad'lar da paylaşır: yumuşak nokta kalmalı.
        public static Material AlphaTextured => _alphaTextured ??= KenneyVfxTextures.GetParticleMaterial(null, false);
        public static Material AdditiveTextured => _additiveTextured ??= KenneyVfxTextures.GetParticleMaterial(null, true);

        public static Shader ResolveShaderPublic() => ResolveShader();

        static Shader ResolveShader()
        {
            var anchor = Resources.Load<Material>(AnchorResourcePath);
            if (anchor != null && anchor.shader != null && anchor.shader.name != "Hidden/InternalErrorShader")
                return anchor.shader;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Sprites/Default");
            return shader != null ? shader : Shader.Find("Hidden/Internal-Colored");
        }

    }
}
