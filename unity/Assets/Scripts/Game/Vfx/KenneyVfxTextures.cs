using Dovus.Core.Grammar;
using Dovus.Game.Config;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// Kenney Particle Pack (CC0) dokuları — <c>Resources/Vfx/Kenney</c>. Materyal/doku önbelleği;
    /// dosya yoksa prosedürel yumuşak nokta (CI / başsız araçlar etkilenmez).
    /// </summary>
    public static class KenneyVfxTextures
    {
        public const string SlamCrackTexture = "scorch_03";

        static PrototypeTuning _tuning;
        static readonly Dictionary<string, Texture2D> Textures = new();
        static readonly Dictionary<long, Material> Materials = new();
        static Texture2D _proceduralDot;

        public static void Configure(PrototypeTuning tuning) => _tuning = tuning;

        public static string TexFire => NameOrDefault(_tuning?.VfxTexFire, "flame_02");
        public static string TexWater => NameOrDefault(_tuning?.VfxTexWater, "circle_03");
        public static string TexAir => NameOrDefault(_tuning?.VfxTexAir, "twirl_01");
        public static string TexEarth => NameOrDefault(_tuning?.VfxTexEarth, "dirt_01");
        public static string TexLight => NameOrDefault(_tuning?.VfxTexLight, "star_04");
        public static string TexDark => NameOrDefault(_tuning?.VfxTexDark, "magic_04");
        public static string TexHit => NameOrDefault(_tuning?.VfxTexHit, "spark_05");
        public static string TexInk => NameOrDefault(_tuning?.VfxTexInk, "light_01");

        public static string ForRune(Rune rune) => rune switch
        {
            Rune.Ates => TexFire,
            Rune.Su => TexWater,
            Rune.Hava => TexAir,
            Rune.Toprak => TexEarth,
            Rune.Aydinlik => TexLight,
            Rune.Karanlik => TexDark,
            _ => TexHit
        };

        /// <summary>Element tint rengine en yakın çekirdek rün dokusu.</summary>
        public static string ClosestElementName(Color tint)
        {
            if (_tuning == null || tint.a <= 0.01f)
                return TexDark;

            float best = float.MaxValue;
            string pick = TexDark;
            Compare(tint, _tuning.ElementFire, TexFire, ref best, ref pick);
            Compare(tint, _tuning.ElementWater, TexWater, ref best, ref pick);
            Compare(tint, _tuning.ElementAir, TexAir, ref best, ref pick);
            Compare(tint, _tuning.ElementEarth, TexEarth, ref best, ref pick);
            Compare(tint, _tuning.ElementLight, TexLight, ref best, ref pick);
            Compare(tint, _tuning.ElementDark, TexDark, ref best, ref pick);
            return pick;
        }

        static void Compare(Color sample, Color reference, string tex, ref float best, ref string pick)
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

        public static Texture2D Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;
            string key = fileName.Trim();
            if (Textures.TryGetValue(key, out Texture2D cached))
                return cached;

            Texture2D tex = Resources.Load<Texture2D>("Vfx/Kenney/" + key);
            Textures[key] = tex;
            return tex;
        }

        public static Material GetParticleMaterial(string fileName, bool additive)
        {
            string name = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim();
            long cacheKey = ((long)(name ?? string.Empty).GetHashCode() << 1) | (additive ? 1L : 0L);
            if (Materials.TryGetValue(cacheKey, out Material mat) && mat != null)
                return mat;

            mat = BuildParticleMaterial(name, additive);
            Materials[cacheKey] = mat;
            return mat;
        }

        static Material BuildParticleMaterial(string fileName, bool additive)
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

        static Texture2D ProceduralDot()
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

        static string NameOrDefault(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
