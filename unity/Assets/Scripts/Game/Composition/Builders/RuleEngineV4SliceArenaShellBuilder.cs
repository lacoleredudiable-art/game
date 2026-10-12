using System;
using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Arena;
using Dovus.Game.Assets;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.Composition.Builders
{
    /// <summary>
    /// Dilim arenasının kabuğu: 360° kapalı görsel duvar (O-7.3; yüzleri çarpışma segmentleriyle aynı açıda, iç yüz
    /// çarpışma iç yüzünde), duvar ötesinde düz zemin diski ve sabit üstel sis (O-7.7, O-8.7). Collider yok; katman
    /// fizik sorgularının dışında.
    /// </summary>
    public static class RuleEngineV4SliceArenaShellBuilder
    {
        public static void BuildWall(Transform parent, RuleEngineV4SliceArena arena)
        {
            float inner = arena.OuterWallVisualInnerRadiusM;
            float outer = arena.OuterWallVisualOuterRadiusM;
            if (outer <= inner)
            {
                Debug.LogError($"[V4-FALLBACK] RuleEngineV4SliceArenaShellBuilder: slice_arena görsel duvar dış yüzü ({outer} m) "
                    + $"iç yüzden ({inner} m) büyük değil; görsel duvar kurulmadı");
                return;
            }
            if (!arena.HasOuterWallColor)
                Debug.LogError("[V4-FALLBACK] RuleEngineV4SliceArenaShellBuilder: kural-motoru-v4.json slice_arena.outer_wall_color yok; "
                    + "renk RuleEngineV4SliceArenaDefaults'tan");

            var geometry = new RuleEngineV4SliceArenaWall(arena);
            float height = Mathf.Max(RuleEngineV4SliceArenaBuilderDefaults.MinWallHeightM, arena.OuterWallHeightM);
            float rIn = geometry.CornerRadiusM(inner);
            float rOut = geometry.CornerRadiusM(outer);
            var mesh = new FlatMesh();
            for (int i = 0; i < geometry.Segments; i++)
            {
                float a0 = geometry.CornerAngleRad(i - 1);
                float a1 = geometry.CornerAngleRad(i);
                Vector3 normal = Dir(geometry.SegmentAngleRad(i));
                mesh.Quad(At(a0, rIn, 0f), At(a1, rIn, 0f), At(a1, rIn, height), At(a0, rIn, height), -normal);
                mesh.Quad(At(a0, rOut, 0f), At(a1, rOut, 0f), At(a1, rOut, height), At(a0, rOut, height), normal);
                mesh.Quad(At(a0, rIn, height), At(a1, rIn, height), At(a1, rOut, height), At(a0, rOut, height), Vector3.up);
            }

            Color color = ParseColor(arena.OuterWallColorHex, RuleEngineV4SliceArenaDefaults.OuterWallColorHex, "slice_arena.outer_wall_color");
            MeshRenderer renderer = Visual(parent, RuleEngineV4SliceArenaShellBuilderDefaults.WallName, mesh.Build(),
                color, RuleEngineV4SliceArenaShellBuilderDefaults.WallSmoothness);
            renderer.shadowCastingMode = ShadowCastingMode.On;
        }

        /// <summary>
        /// Zemin <paramref name="fadeStartM"/>'den (duvarın dış yüzü) kenara doğru sis rengine solar. Sis 0,012 ile 300 m'de
        /// zeminin ~%3'ünü bırakır; kenarda yüzey zaten sis rengi olduğundan zeminin bittiği çizgi görünmez (O-7.7).
        /// </summary>
        public static void BuildBeyondGround(Transform parent, RuleEngineV4SliceArenaBeyond beyond, float fadeStartM)
        {
            LogIfMissing(beyond, "BuildBeyondGround");
            int segments = RuleEngineV4SliceArenaShellBuilderDefaults.GroundSegments;
            float r = beyond.GroundRadiusM;
            float fade = fadeStartM;
            if (fade >= r)
            {
                Debug.LogError($"[V4-FALLBACK] RuleEngineV4SliceArenaShellBuilder.BuildBeyondGround: zemin yarıçapı ({r} m) "
                    + $"duvar dış yüzünden ({fadeStartM} m) büyük değil; zemin solmadan kuruldu");
                fade = r;
            }
            float y = -RuleEngineV4SliceArenaShellBuilderDefaults.GroundDropM;
            var mesh = new FlatMesh(p => FadeUv(p, fade, r));
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * 2f * Mathf.PI / segments;
                float a1 = (i + 1) * 2f * Mathf.PI / segments;
                mesh.Tri(new Vector3(0f, y, 0f), At(a0, fade, y), At(a1, fade, y), Vector3.up);
                if (fade < r)
                    mesh.Quad(At(a0, fade, y), At(a1, fade, y), At(a1, r, y), At(a0, r, y), Vector3.up);
            }

            Color color = ParseColor(beyond.GroundColorHex, RuleEngineV4SliceArenaVisualsDefaults.BeyondGroundColorHex, "slice_arena_visuals.beyond_wall.ground_color");
            MeshRenderer renderer = Visual(parent, RuleEngineV4SliceArenaShellBuilderDefaults.GroundName, mesh.Build(),
                color, RuleEngineV4SliceArenaShellBuilderDefaults.GroundSmoothness);
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            // Yüzey = zemin × (1 − t) + sis × t; t solma boyunca 0 → 1.
            Material material = renderer.sharedMaterial;
            material.SetTexture(RuleEngineV4SliceArenaShellBuilderDefaults.BaseMapProperty, Ramp(Color.white, Color.black));
            material.EnableKeyword(RuleEngineV4SliceArenaShellBuilderDefaults.EmissionKeyword);
            material.SetColor(RuleEngineV4SliceArenaShellBuilderDefaults.EmissionColorProperty, Fog(beyond).Color);
            material.SetTexture(RuleEngineV4SliceArenaShellBuilderDefaults.EmissionMapProperty, Ramp(Color.black, Color.white));
        }

        /// <summary>Solma oranı t, iki tekselli rampanın teksel merkezleri arasına düşer (kenetli çift doğrusal örnekleme).</summary>
        static Vector2 FadeUv(Vector3 p, float fadeStartM, float edgeM)
        {
            float flat = new Vector2(p.x, p.z).magnitude;
            float t = edgeM > fadeStartM ? Mathf.Clamp01((flat - fadeStartM) / (edgeM - fadeStartM)) : 0f;
            const int texels = RuleEngineV4SliceArenaShellBuilderDefaults.RampTexels;
            return new Vector2((0.5f + t * (texels - 1)) / texels, 0.5f);
        }

        static Texture2D Ramp(Color from, Color to)
        {
            var tex = new Texture2D(RuleEngineV4SliceArenaShellBuilderDefaults.RampTexels, 1, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            tex.SetPixels(new[] { from, to });
            tex.Apply(false, true);
            return tex;
        }

        public static ArenaFog Fog(RuleEngineV4SliceArenaBeyond beyond)
        {
            LogIfMissing(beyond, "Fog");
            Color color = ParseColor(beyond.FogColorHex, RuleEngineV4SliceArenaVisualsDefaults.BeyondFogColorHex, "slice_arena_visuals.beyond_wall.fog_color");
            return new ArenaFog(color, beyond.FogDensity);
        }

        static void LogIfMissing(RuleEngineV4SliceArenaBeyond beyond, string where)
        {
            if (!beyond.HasBlock)
                Debug.LogError($"[V4-FALLBACK] RuleEngineV4SliceArenaShellBuilder.{where}: kural-motoru-v4.json "
                    + "slice_arena_visuals.beyond_wall yok; zemin ve sis RuleEngineV4SliceArenaVisualsDefaults'tan");
        }

        static Color ParseColor(string hex, string fallbackHex, string field)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;
            Debug.LogError($"[V4-FALLBACK] RuleEngineV4SliceArenaShellBuilder: {field} \"{hex}\" renk değil; {fallbackHex} kullanıldı");
            ColorUtility.TryParseHtmlString(fallbackHex, out color);
            return color;
        }

        static MeshRenderer Visual(Transform parent, string name, Mesh mesh, Color color, float smoothness)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = RuleEngineV4SliceArenaVisualsBuilderDefaults.VisualLayer;
            mesh.name = name;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var material = new Material(AssetLoader.FindShader(RuleEngineV4SliceArenaShellBuilderDefaults.LitShader, null)) { name = name };
            material.SetColor(RuleEngineV4SliceArenaShellBuilderDefaults.BaseColorProperty, color);
            material.SetFloat(RuleEngineV4SliceArenaShellBuilderDefaults.SmoothnessProperty, smoothness);
            renderer.sharedMaterial = material;
            return renderer;
        }

        static Vector3 Dir(float angleRad) => new(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));

        static Vector3 At(float angleRad, float radiusM, float y) => Dir(angleRad) * radiusM + Vector3.up * y;

        /// <summary>Düz gölgeli mesh: her yüzün kendi köşeleri ve normali var.</summary>
        sealed class FlatMesh
        {
            readonly List<Vector3> _vertices = new();
            readonly List<Vector3> _normals = new();
            readonly List<Vector2> _uvs = new();
            readonly List<int> _triangles = new();
            readonly Func<Vector3, Vector2> _uv;

            public FlatMesh(Func<Vector3, Vector2> uv = null) => _uv = uv;

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                int i = Add(normal, a, b, c, d);
                bool front = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) > 0f;
                _triangles.AddRange(front ? new[] { i, i + 1, i + 2, i, i + 2, i + 3 } : new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
            {
                int i = Add(normal, a, b, c);
                bool front = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) > 0f;
                _triangles.AddRange(front ? new[] { i, i + 1, i + 2 } : new[] { i, i + 2, i + 1 });
            }

            int Add(Vector3 normal, params Vector3[] corners)
            {
                int start = _vertices.Count;
                foreach (Vector3 v in corners)
                {
                    _vertices.Add(v);
                    _normals.Add(normal);
                    _uvs.Add(_uv != null ? _uv(v) : Vector2.zero);
                }
                return start;
            }

            public Mesh Build()
            {
                var mesh = new Mesh();
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetUVs(0, _uvs);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
