using System.Collections.Generic;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// O11 (denetim C): vuruş/cast başına materyal birikmesin. <c>renderer.material.color = c</c>
    /// her yeni nesnede bir materyal örneği üretir ve <c>Destroy(go)</c> onu silmez (D3 sızıntısı).
    /// Burada (taban materyal, renk) başına TEK paylaşılan kopya tutulur; renk sayısı sınırlıdır.
    /// Her kare değişen renk için <see cref="Paint"/> MaterialPropertyBlock kullanır (örnek yok).
    /// </summary>
    public static class SharedTint
    {
        static readonly Dictionary<(int baseId, int rgba), Material> Cache = new();
        static readonly Dictionary<string, Material> ByShader = new();
        static readonly Dictionary<int, Material> BaseOfTinted = new();
        static MaterialPropertyBlock _block;

        static int Pack(Color c)
        {
            Color32 q = c;
            return (q.r << 24) | (q.g << 16) | (q.b << 8) | q.a;
        }

        /// <summary>Taban materyalin renklendirilmiş paylaşılan kopyası (renderer.sharedMaterial'a verilir).</summary>
        public static Material Of(Material baseMat, Color color)
        {
            if (baseMat == null)
                return null;
            // Zaten renklendirilmiş kopya verildiyse tabanına dön (zincirleme kopya üretme).
            if (BaseOfTinted.TryGetValue(baseMat.GetInstanceID(), out Material root) && root != null)
                baseMat = root;
            var key = (baseMat.GetInstanceID(), Pack(color));
            if (Cache.TryGetValue(key, out Material cached) && cached != null)
                return cached;
            var mat = new Material(baseMat) { name = baseMat.name + "_tint" };
            SetColor(mat, color);
            Cache[key] = mat;
            BaseOfTinted[mat.GetInstanceID()] = baseMat;
            return mat;
        }

        /// <summary>renderer.material.color = color yerine: paylaşılan kopyayı takar.</summary>
        public static void Apply(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;
            Material tinted = Of(renderer.sharedMaterial, color);
            if (tinted != null)
                renderer.sharedMaterial = tinted;
        }

        /// <summary>Shader adına göre tek paylaşılan materyal (ör. LineRenderer "Sprites/Default").</summary>
        public static Material ForShader(string shaderName)
        {
            if (ByShader.TryGetValue(shaderName, out Material cached) && cached != null)
                return cached;
            Shader shader = AssetLoader.FindShader(shaderName, null);
            if (shader == null)
                return null;
            var mat = new Material(shader) { name = "Shared_" + shaderName };
            ByShader[shaderName] = mat;
            return mat;
        }

        /// <summary>Her kare değişen renk: örnek üretmeden MaterialPropertyBlock ile boyar.</summary>
        public static void Paint(Renderer renderer, Color color)
        {
            if (renderer == null || renderer.sharedMaterial == null)
                return;
            _block ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_block);
            _block.SetColor(renderer.sharedMaterial.HasProperty("_BaseColor") ? "_BaseColor" : "_Color", color);
            renderer.SetPropertyBlock(_block);
        }

        static void SetColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
        }
    }
}
