using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Execution
{
    /// <summary>
    /// Paket mesh'i olmayan fiil maddeleri için koddan üretilen düşük poligonlu katı parça.
    /// Şekil adı veridir (<see cref="VfxLibrary.Entry.ChunkShape"/>); oranlar sanat verisidir,
    /// ölçü <see cref="ComposedSkillVfxView"/> tarafından gramer boyutuna oturtulur.
    /// </summary>
    public static class ProceduralChunkMesh
    {
        static readonly Dictionary<string, Mesh> Cache = new();

        public static Mesh Get(string shape)
        {
            if (string.IsNullOrEmpty(shape))
                return null;
            if (Cache.TryGetValue(shape, out Mesh cached) && cached != null)
                return cached;
            var b = new Builder();
            switch (shape)
            {
                case "leaf": Leaf(b); break;
                case "arrow": Arrow(b); break;
                case "burst": Burst(b); break;
                case "ring": Ring(b); break;
                case "blob": Blob(b); break;
                case "crystal": Crystal(b); break;
                case "star": Star(b); break;
                case "obelisk": Obelisk(b); break;
                case "hourglass": Hourglass(b); break;
                default: return null;
            }
            Mesh mesh = b.ToMesh("Chunk_" + shape);
            Cache[shape] = mesh;
            return mesh;
        }

        static void Leaf(Builder b)
        {
            Vector3 tip = new(0f, 1f, 0f), low = new(0f, -0.1f, 0f);
            Vector3 r = new(0.4f, 0.4f, 0f), l = new(-0.4f, 0.4f, 0f);
            Vector3 f = new(0f, 0.4f, 0.12f), k = new(0f, 0.4f, -0.12f);
            Vector3 c = new(0f, 0.4f, 0f);
            Vector3[] ring = { tip, r, low, l };
            for (int i = 0; i < 4; i++)
            {
                b.Tri(f, ring[i], ring[(i + 1) % 4], c);
                b.Tri(k, ring[i], ring[(i + 1) % 4], c);
            }
        }

        static void Arrow(Builder b)
        {
            const float headBase = 0.35f, headTip = 1f, headR = 0.35f;
            Vector3 tip = new(0f, 0f, headTip), hc = new(0f, 0f, (headBase + headTip) * 0.5f);
            Vector3[] hb =
            {
                new(headR, 0f, headBase), new(0f, headR * 0.5f, headBase),
                new(-headR, 0f, headBase), new(0f, -headR * 0.5f, headBase),
            };
            for (int i = 0; i < 4; i++)
            {
                b.Tri(tip, hb[i], hb[(i + 1) % 4], hc);
                b.Tri(new Vector3(0f, 0f, headBase), hb[i], hb[(i + 1) % 4], hc);
            }
            b.Box(new Vector3(0f, 0f, -0.1f), new Vector3(0.09f, 0.09f, 0.45f));
        }

        static void Burst(Builder b)
        {
            Icosahedron(out Vector3[] v, out int[] t);
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = v[t[i]] * 0.55f, c = v[t[i + 1]] * 0.55f, d = v[t[i + 2]] * 0.55f;
                Vector3 apex = (a + c + d) / 3f * 1.9f;
                b.Tri(apex, a, c, Vector3.zero);
                b.Tri(apex, c, d, Vector3.zero);
                b.Tri(apex, d, a, Vector3.zero);
            }
        }

        static void Ring(Builder b)
        {
            const int major = 14, minor = 6;
            const float R = 0.42f, r = 0.12f;
            Vector3 P(int i, int j, out Vector3 center)
            {
                float u = i * Mathf.PI * 2f / major, w = j * Mathf.PI * 2f / minor;
                center = new Vector3(Mathf.Cos(u) * R, Mathf.Sin(u) * R + R + r, 0f);
                Vector3 radial = new(Mathf.Cos(u), Mathf.Sin(u), 0f);
                return center + radial * (Mathf.Cos(w) * r) + Vector3.forward * (Mathf.Sin(w) * r);
            }
            for (int i = 0; i < major; i++)
            for (int j = 0; j < minor; j++)
            {
                Vector3 a = P(i, j, out Vector3 ca), c = P(i + 1, j, out Vector3 cb);
                Vector3 d = P(i + 1, j + 1, out _), e = P(i, j + 1, out _);
                Vector3 mid = (ca + cb) * 0.5f;
                b.Tri(a, c, d, mid);
                b.Tri(a, d, e, mid);
            }
        }

        static void Blob(Builder b)
        {
            Icosahedron(out Vector3[] v, out int[] t);
            var mids = new Dictionary<(int, int), Vector3>();
            Vector3 Mid(int x, int y)
            {
                var key = x < y ? (x, y) : (y, x);
                if (!mids.TryGetValue(key, out Vector3 m))
                    mids[key] = m = (v[x] + v[y]).normalized;
                return m;
            }
            Vector3 Lump(Vector3 p)
            {
                float n = 1f + 0.18f * Mathf.Sin(p.x * 7.1f + p.y * 3.3f) * Mathf.Cos(p.z * 5.7f - p.y * 2.1f);
                p *= 0.5f * n;
                p.y = p.y * 0.65f + 0.3f;
                return p;
            }
            Vector3 c = new(0f, 0.3f, 0f);
            for (int i = 0; i < t.Length; i += 3)
            {
                int x = t[i], y = t[i + 1], z = t[i + 2];
                Vector3 a = Lump(v[x]), d = Lump(v[y]), e = Lump(v[z]);
                Vector3 ab = Lump(Mid(x, y)), bc = Lump(Mid(y, z)), ca = Lump(Mid(z, x));
                b.Tri(a, ab, ca, c);
                b.Tri(ab, d, bc, c);
                b.Tri(ca, bc, e, c);
                b.Tri(ab, bc, ca, c);
            }
        }

        static void Crystal(Builder b)
        {
            const int sides = 6;
            const float rad = 0.28f, bottom = 0.15f, top = 1.1f;
            Vector3 up = new(0f, 1.5f, 0f), down = Vector3.zero, c = new(0f, 0.7f, 0f);
            for (int i = 0; i < sides; i++)
            {
                Vector3 Dir(int k) => new(Mathf.Cos(k * Mathf.PI * 2f / sides) * rad, 0f,
                    Mathf.Sin(k * Mathf.PI * 2f / sides) * rad);
                Vector3 lo0 = Dir(i) + Vector3.up * bottom, lo1 = Dir(i + 1) + Vector3.up * bottom;
                Vector3 hi0 = Dir(i) + Vector3.up * top, hi1 = Dir(i + 1) + Vector3.up * top;
                b.Tri(lo0, lo1, hi1, c);
                b.Tri(lo0, hi1, hi0, c);
                b.Tri(up, hi0, hi1, c);
                b.Tri(down, lo0, lo1, c);
            }
        }

        static void Star(Builder b)
        {
            const int points = 5;
            const float outer = 0.6f, inner = 0.25f, depth = 0.14f;
            Vector3 c = new(0f, outer, 0f);
            Vector3 f = c + Vector3.forward * depth, k = c - Vector3.forward * depth;
            var ring = new Vector3[points * 2];
            for (int i = 0; i < ring.Length; i++)
            {
                float a = i * Mathf.PI / points + Mathf.PI * 0.5f;
                float rr = i % 2 == 0 ? outer : inner;
                ring[i] = c + new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, 0f);
            }
            for (int i = 0; i < ring.Length; i++)
            {
                Vector3 p = ring[i], q = ring[(i + 1) % ring.Length];
                Vector3 inside = c + (p + q - 2f * c) * 0.25f;
                b.Tri(f, p, q, inside);
                b.Tri(k, p, q, inside);
            }
        }

        static void Obelisk(Builder b)
        {
            const float baseH = 0.3f, topH = 0.17f, height = 1.2f, apex = 1.5f;
            Vector3 c = new(0f, 0.7f, 0f);
            Vector3 Corner(int i, float h, float y) =>
                new((i == 0 || i == 3 ? -1f : 1f) * h, y, (i < 2 ? -1f : 1f) * h);
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                Vector3 lo0 = Corner(i, baseH, 0f), lo1 = Corner(j, baseH, 0f);
                Vector3 hi0 = Corner(i, topH, height), hi1 = Corner(j, topH, height);
                b.Tri(lo0, lo1, hi1, c);
                b.Tri(lo0, hi1, hi0, c);
                b.Tri(new Vector3(0f, apex, 0f), hi0, hi1, c);
                b.Tri(Vector3.zero, lo0, lo1, c);
            }
        }

        static void Hourglass(Builder b)
        {
            const int sides = 8;
            const float rad = 0.42f, pinch = 0.07f, height = 1.1f;
            float mid = height * 0.5f;
            Vector3 Ring(int k, float r, float y) =>
                new(Mathf.Cos(k * Mathf.PI * 2f / sides) * r, y, Mathf.Sin(k * Mathf.PI * 2f / sides) * r);
            for (int i = 0; i < sides; i++)
            {
                Vector3 b0 = Ring(i, rad, 0f), b1 = Ring(i + 1, rad, 0f);
                Vector3 m0 = Ring(i, pinch, mid), m1 = Ring(i + 1, pinch, mid);
                Vector3 t0 = Ring(i, rad, height), t1 = Ring(i + 1, rad, height);
                Vector3 lowC = new(0f, mid * 0.5f, 0f), highC = new(0f, mid * 1.5f, 0f);
                b.Tri(b0, b1, m1, lowC);
                b.Tri(b0, m1, m0, lowC);
                b.Tri(m0, m1, t1, highC);
                b.Tri(m0, t1, t0, highC);
                b.Tri(Vector3.zero, b0, b1, lowC);
                b.Tri(new Vector3(0f, height, 0f), t0, t1, highC);
            }
        }

        static void Icosahedron(out Vector3[] v, out int[] t)
        {
            float p = (1f + Mathf.Sqrt(5f)) * 0.5f;
            v = new[]
            {
                new Vector3(-1, p, 0), new Vector3(1, p, 0), new Vector3(-1, -p, 0), new Vector3(1, -p, 0),
                new Vector3(0, -1, p), new Vector3(0, 1, p), new Vector3(0, -1, -p), new Vector3(0, 1, -p),
                new Vector3(p, 0, -1), new Vector3(p, 0, 1), new Vector3(-p, 0, -1), new Vector3(-p, 0, 1),
            };
            for (int i = 0; i < v.Length; i++)
                v[i] = v[i].normalized;
            t = new[]
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
        }

        /// <summary>Düz gölgeli üçgen biriktirici; her üçgen verilen iç noktadan dışa bakar.</summary>
        sealed class Builder
        {
            readonly List<Vector3> _v = new();

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 inside)
            {
                Vector3 n = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(n, (a + b + c) / 3f - inside) < 0f)
                    (b, c) = (c, b);
                _v.Add(a);
                _v.Add(b);
                _v.Add(c);
            }

            public void Box(Vector3 center, Vector3 half)
            {
                Vector3 V(int i) => center + Vector3.Scale(half,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                int[][] faces =
                {
                    new[] { 0, 1, 3, 2 }, new[] { 4, 5, 7, 6 }, new[] { 0, 1, 5, 4 },
                    new[] { 2, 3, 7, 6 }, new[] { 0, 2, 6, 4 }, new[] { 1, 3, 7, 5 },
                };
                foreach (int[] f in faces)
                {
                    Tri(V(f[0]), V(f[1]), V(f[2]), center);
                    Tri(V(f[0]), V(f[2]), V(f[3]), center);
                }
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(_v);
                var idx = new int[_v.Count];
                for (int i = 0; i < idx.Length; i++)
                    idx[i] = i;
                mesh.SetTriangles(idx, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
