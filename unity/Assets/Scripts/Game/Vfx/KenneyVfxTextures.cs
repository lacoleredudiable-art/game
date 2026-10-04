using Dovus.Game.Config;
using System.Collections.Generic;
using Dovus.Game.Assets;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// Kenney Particle Pack (CC0) dokuları — <c>Resources/Vfx/Kenney</c>. Materyal/doku önbelleği;
    /// dosya yoksa prosedürel yumuşak nokta (CI / başsız araçlar etkilenmez).
    /// </summary>
    public sealed class KenneyVfxTextures
    {
        public const string SlamCrackTexture = "scorch_03";

        readonly GameTuning _tuning;
        readonly Dictionary<string, Texture2D> Textures = new();
        readonly Dictionary<long, Material> Materials = new();
        Texture2D _proceduralDot;

        public KenneyVfxTextures(GameTuning tuning) => _tuning = tuning;

        public string TexFire => NameOrDefault(_tuning?.Visuals.VfxTexFire, "flame_02");
        public string TexWater => NameOrDefault(_tuning?.Visuals.VfxTexWater, "circle_03");
        public string TexAir => NameOrDefault(_tuning?.Visuals.VfxTexAir, "twirl_01");
        public string TexEarth => NameOrDefault(_tuning?.Visuals.VfxTexEarth, "dirt_01");
        public string TexLight => NameOrDefault(_tuning?.Visuals.VfxTexLight, "star_04");
        public string TexDark => NameOrDefault(_tuning?.Visuals.VfxTexDark, "magic_04");
        public string TexHit => NameOrDefault(_tuning?.Visuals.VfxTexHit, "spark_05");
        public string TexInk => NameOrDefault(_tuning?.Visuals.VfxTexInk, "light_01");

        /// <summary>Element tint rengine en yakın çekirdek rün dokusu.</summary>
        public string ClosestElementName(Color tint)
        {
            if (_tuning == null || tint.a <= VfxDefaults.MinTintVisibleAlpha)
                return TexDark;

            float best = float.MaxValue;
            string pick = TexDark;
            Compare(tint, _tuning.Visuals.ElementFire, TexFire, ref best, ref pick);
            Compare(tint, _tuning.Visuals.ElementWater, TexWater, ref best, ref pick);
            Compare(tint, _tuning.Visuals.ElementAir, TexAir, ref best, ref pick);
            Compare(tint, _tuning.Visuals.ElementEarth, TexEarth, ref best, ref pick);
            Compare(tint, _tuning.Visuals.ElementLight, TexLight, ref best, ref pick);
            Compare(tint, _tuning.Visuals.ElementDark, TexDark, ref best, ref pick);
            return pick;
        }

        void Compare(Color sample, Color reference, string tex, ref float best, ref string pick)
        {
            float d = (sample.r - reference.r) * (sample.r - reference.r)
                + (sample.g - reference.g) * (sample.g - reference.g)
                + (sample.b - reference.b) * (sample.b - reference.b);
            if (d < best)
            {
                best = d;
                pick = tex;
            }
        }

        public Texture2D Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;
            string key = fileName.Trim();
            if (Textures.TryGetValue(key, out Texture2D cached))
                return cached;

            Texture2D tex = AssetLoader.Load<Texture2D>("Vfx/Kenney/" + key, null);
            Textures[key] = tex;
            return tex;
        }

        public Material GetParticleMaterial(string fileName, bool additive)
        {
            string name = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim();
            long cacheKey = ((long)(name ?? string.Empty).GetHashCode() << 1) | (additive ? VfxDefaults.OpaqueCacheKeyBit : VfxDefaults.CacheKeyAdditiveBit);
            if (Materials.TryGetValue(cacheKey, out Material mat) && mat != null)
                return mat;

            mat = BuildParticleMaterial(name, additive);
            Materials[cacheKey] = mat;
            return mat;
        }

        Material BuildParticleMaterial(string fileName, bool additive)
        {
            Shader shader = PresentationParticleMaterials.ResolveShaderPublic();
            var m = new Material(shader) { name = "KenneyFx_" + (fileName ?? "dot") + (additive ? "_Add" : "_Alpha") };
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", additive ? 2f : 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                m.SetInt("_ZWrite", 0);
            }

            if (m.HasProperty("_Cull"))
                m.SetInt("_Cull", (int)CullMode.Off);

            m.renderQueue = (int)RenderQueue.Transparent;
            Texture2D tex = Load(fileName) ?? ProceduralDot();
            if (m.HasProperty("_BaseMap"))
                m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
            return m;
        }

        Texture2D ProceduralDot()
        {
            if (_proceduralDot != null)
                return _proceduralDot;
            const int size = 32;
            _proceduralDot = new Texture2D(size, size, TextureFormat.RGBA32, false)
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
                _proceduralDot.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }

            _proceduralDot.Apply(false, true);
            return _proceduralDot;
        }

        string NameOrDefault(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
