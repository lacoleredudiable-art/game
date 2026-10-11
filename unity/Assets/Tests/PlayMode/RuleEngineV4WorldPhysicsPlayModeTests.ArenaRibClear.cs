using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Composition.Builders;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Kaburga ayakları arena dışına basar: |x| &lt; keep_clear.x_m bandında yerden keep_clear.below_m altında kemik
    /// yok. Sahnedeki her kaburga mesh'i (LOD0 + LOD1) shader'ın yarım kesimiyle (nesne x ≥ _DovusHalfClipX) ve
    /// yükseklik düzlemiyle kırpılır; kalan çokgenlerin dünya |x| en küçüğü ölçülür. Ayak izi haritası footprint.txt.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float SpecKeepClearXM = 22f;
        const float SpecKeepClearBelowM = 2.5f;
        const int FootprintHalfXM = 40;
        const int FootprintHalfZM = 25;
        const string FootprintFile = "footprint.txt";

        [UnityTest]
        [PrebuildSetup(typeof(RibMeshCache))]
        public IEnumerator ArenaVisuals_Ribs_KeepClearBand_NothingBelowClearHeight_HalvesMeetAtCrown()
        {
            yield return null;
            if (!Application.isEditor)
                Assert.Ignore("Read/Write kapalı mesh verisi yalnız editörde okunur");
            RuleEngineV4SliceArenaRibs spec = VisualsSpec.Ribs;
            Assert.That(spec.KeepClearXM, Is.EqualTo(SpecKeepClearXM), "JSON keep_clear.x_m");
            Assert.That(spec.KeepClearBelowM, Is.EqualTo(SpecKeepClearBelowM), "JSON keep_clear.below_m");
            Assert.That(spec.Halves.Enabled, "JSON ribs.halves yok: kemer iki yarım kurulmalı");
            float clipX = Shader.GetGlobalFloat(RuleEngineV4SliceArenaVisualsBuilderDefaults.HalfClipShaderProperty);

            var low = new List<Vector3>();
            foreach (Transform rib in Ribs())
            {
                Transform right = rib.Find(RuleEngineV4SliceArenaVisualsBuilderDefaults.HalfRightName);
                Transform left = rib.Find(RuleEngineV4SliceArenaVisualsBuilderDefaults.HalfLeftName);
                Assert.That(right != null && left != null, $"{rib.name}: sol/sağ yarım yok");

                HalfExtent r = ClipHalf(right, rib, clipX, spec.KeepClearBelowM, low);
                HalfExtent l = ClipHalf(left, rib, clipX, spec.KeepClearBelowM, low);
                Assert.That(Mathf.Min(r.MinWorldY, l.MinWorldY), Is.LessThanOrEqualTo(0f), $"{rib.name}: ayak yere değmiyor");
                // Tepede birleşme: her yarım kemer düzleminde orta çizgiyi geçer (bindirme), yalnız bant üstünde.
                Assert.That(r.MinRibX, Is.LessThan(0f), $"{rib.name}: sağ yarım tepede ortaya ulaşmıyor");
                Assert.That(l.MaxRibX, Is.GreaterThan(0f), $"{rib.name}: sol yarım tepede ortaya ulaşmıyor");
            }

            float minAbsX = low.Count == 0 ? float.PositiveInfinity : low.Min(p => Mathf.Abs(p.x));
            string map = FootprintMap(low, spec);
            WriteFootprint(map);
            Debug.Log($"[kaburga-bant] {spec.KeepClearBelowM} m altı en yakın |x| = {minAbsX:0.00} m (sınır {spec.KeepClearXM} m)\n{map}");
            Assert.That(minAbsX, Is.GreaterThanOrEqualTo(spec.KeepClearXM),
                $"{spec.KeepClearBelowM} m altında |x| < {spec.KeepClearXM} m bandında kemik var");
        }

        struct HalfExtent
        {
            public float MinWorldY;
            public float MinRibX;
            public float MaxRibX;
        }

        /// <summary>Yarımın tüm mesh'leri: kesim → dünya; bant yüksekliği altı <paramref name="low"/>'a eklenir.</summary>
        static HalfExtent ClipHalf(Transform half, Transform rib, float clipX, float belowM, List<Vector3> low)
        {
            var e = new HalfExtent { MinWorldY = float.PositiveInfinity, MinRibX = float.PositiveInfinity, MaxRibX = float.NegativeInfinity };
            Matrix4x4 worldToRib = rib.worldToLocalMatrix;
            var poly = new List<Vector3>(8);
            foreach (MeshFilter mf in half.GetComponentsInChildren<MeshFilter>())
            {
                (Vector3[] v, int[] t) = ReadMesh(mf.sharedMesh);
                Matrix4x4 toWorld = mf.transform.localToWorldMatrix;
                for (int i = 0; i < t.Length; i += 3)
                {
                    poly.Clear();
                    poly.Add(v[t[i]]);
                    poly.Add(v[t[i + 1]]);
                    poly.Add(v[t[i + 2]]);
                    List<Vector3> kept = ClipPolygon(poly, p => p.x - clipX);
                    if (kept.Count == 0)
                        continue;
                    for (int k = 0; k < kept.Count; k++)
                    {
                        Vector3 w = toWorld.MultiplyPoint3x4(kept[k]);
                        kept[k] = w;
                        float ribX = worldToRib.MultiplyPoint3x4(w).x;
                        e.MinWorldY = Mathf.Min(e.MinWorldY, w.y);
                        e.MinRibX = Mathf.Min(e.MinRibX, ribX);
                        e.MaxRibX = Mathf.Max(e.MaxRibX, ribX);
                    }
                    low.AddRange(ClipPolygon(kept, p => belowM - p.y));
                }
            }
            return e;
        }

        static Dictionary<string, (Vector3[] Vertices, int[] Triangles)> _ribMeshes;

        /// <summary>Read/Write kapalı mesh play modunda okunamaz; veri <see cref="RibMeshCache"/> ile edit modunda alınır.</summary>
        static (Vector3[] Vertices, int[] Triangles) ReadMesh(Mesh mesh)
        {
            _ribMeshes ??= RibMeshCache.Load();
            Assert.That(_ribMeshes.TryGetValue(mesh.name, out var data), $"{mesh.name}: önbellekte mesh verisi yok");
            Assert.That(data.Vertices.Length, Is.EqualTo(mesh.vertexCount), $"{mesh.name}: önbellek eski");
            return data;
        }

        /// <summary>Sutherland–Hodgman: <paramref name="inside"/> ≥ 0 yarı uzayı kalır.</summary>
        static List<Vector3> ClipPolygon(List<Vector3> poly, Func<Vector3, float> inside)
        {
            var o = new List<Vector3>(poly.Count + 2);
            for (int i = 0; i < poly.Count; i++)
            {
                Vector3 a = poly[i];
                Vector3 b = poly[(i + 1) % poly.Count];
                float fa = inside(a);
                float fb = inside(b);
                if (fa >= 0f)
                    o.Add(a);
                if ((fa >= 0f) != (fb >= 0f))
                    o.Add(Vector3.Lerp(a, b, fa / (fa - fb)));
            }
            return o;
        }

        /// <summary>1 m ızgara: # kemik (bant yüksekliği altı), | bant sınırı, C sütun, K koza, W duvar, H ocak, R rün taşı.</summary>
        string FootprintMap(List<Vector3> low, RuleEngineV4SliceArenaRibs spec)
        {
            int w = FootprintHalfXM * 2 + 1;
            int h = FootprintHalfZM * 2 + 1;
            var grid = new char[h, w];
            for (int z = 0; z < h; z++)
            {
                for (int x = 0; x < w; x++)
                    grid[z, x] = Mathf.Abs(x - FootprintHalfXM) == Mathf.RoundToInt(spec.KeepClearXM) ? '|' : '.';
            }
            void Mark(float xm, float zm, char c)
            {
                int gx = Mathf.RoundToInt(xm) + FootprintHalfXM;
                int gz = FootprintHalfZM - Mathf.RoundToInt(zm);
                if (gx >= 0 && gx < w && gz >= 0 && gz < h)
                    grid[gz, gx] = c;
            }
            foreach (Vector3 p in low)
                Mark(p.x, p.z, '#');
            foreach (RuleEngineV4SliceArenaColumn c in ArenaSpec.Columns)
                Mark(c.XM, c.ZM, 'C');
            foreach (RuleEngineV4SliceArenaCocoon c in ArenaSpec.Cocoons)
                Mark(c.XM, c.ZM, 'K');
            foreach (RuleEngineV4SliceArenaPlacement p in VisualsSpec.Walls)
                Mark(p.XM, p.ZM, 'W');
            Mark(VisualsSpec.Hearth.XM, VisualsSpec.Hearth.ZM, 'H');
            Mark(VisualsSpec.RuneStone.XM, VisualsSpec.RuneStone.ZM, 'R');

            var sb = new StringBuilder();
            sb.AppendLine($"# kaburga ayak izi: yerden {spec.KeepClearBelowM} m altı, 1 m hücre, x -{FootprintHalfXM}..{FootprintHalfXM} (sağ +x), z +{FootprintHalfZM} (kuzey, üst)..-{FootprintHalfZM}");
            sb.AppendLine("# # kemik  | bant sınırı |x|=" + spec.KeepClearXM + "  C sütun  K koza  W duvar  H ocak  R rün taşı");
            for (int z = 0; z < h; z++)
            {
                for (int x = 0; x < w; x++)
                    sb.Append(grid[z, x]);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        static void WriteFootprint(string map)
        {
            string dir = SessionCaptureDir() ?? Application.temporaryCachePath;
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, FootprintFile), map);
        }
    }

    /// <summary>
    /// Play modundan önce (edit modu) kaburga LOD0/LOD1 mesh'lerinin köşe + üçgen verisini geçici dosyaya yazar;
    /// import ayarı (Read/Write kapalı) değişmez. Mesh'ler ArenaVisualRegistry'deki kaburga girdilerinden.
    /// </summary>
    public sealed class RibMeshCache : IPrebuildSetup
    {
        const string RegistryResource = "Arena/ArenaVisualRegistry";
        const string FileName = "ortam-rib-mesh-cache.bin";
        static readonly string[] RibKeys =
        {
            RuleEngineV4SliceArenaVisualsDefaults.RibModel, RuleEngineV4SliceArenaVisualsDefaults.RibLod1Model,
        };

        static string CachePath => Path.Combine(Application.temporaryCachePath, FileName);

        public void Setup()
        {
#if UNITY_EDITOR
            var registry = Resources.Load<Dovus.Game.Arena.ArenaVisualRegistry>(RegistryResource);
            using var w = new BinaryWriter(File.Create(CachePath));
            foreach (string key in RibKeys)
            {
                Mesh mesh = registry.Find(key).Model.GetComponentInChildren<MeshFilter>().sharedMesh;
                Vector3[] v = mesh.vertices;
                int[] t = mesh.triangles;
                w.Write(mesh.name);
                w.Write(v.Length);
                foreach (Vector3 p in v)
                {
                    w.Write(p.x);
                    w.Write(p.y);
                    w.Write(p.z);
                }
                w.Write(t.Length);
                foreach (int i in t)
                    w.Write(i);
            }
#endif
        }

        public static Dictionary<string, (Vector3[] Vertices, int[] Triangles)> Load()
        {
            var meshes = new Dictionary<string, (Vector3[], int[])>();
            Assert.That(File.Exists(CachePath), "kaburga mesh önbelleği yok (PrebuildSetup çalışmadı): " + CachePath);
            using var r = new BinaryReader(File.OpenRead(CachePath));
            for (int k = 0; k < RibKeys.Length; k++)
            {
                string name = r.ReadString();
                var v = new Vector3[r.ReadInt32()];
                for (int i = 0; i < v.Length; i++)
                    v[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                var t = new int[r.ReadInt32()];
                for (int i = 0; i < t.Length; i++)
                    t[i] = r.ReadInt32();
                meshes[name] = (v, t);
            }
            return meshes;
        }
    }
}
