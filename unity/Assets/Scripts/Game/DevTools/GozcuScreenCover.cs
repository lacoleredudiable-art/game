using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.DevTools
{
    /// <summary>
    /// Gözcü için tek efektin ekran kaplaması (0..1): sınır kutusu ince yay/kıvılcımı abartır, bu yüzden
    /// mesh üçgenlerinin ve parçacık disklerinin ekran alanı toplanır (üst üste binme sayılır, 1'de kırpılır).
    /// Okunamayan mesh'te sınır kutusu dikdörtgenine düşer.
    /// </summary>
    public sealed class GozcuScreenCover
    {
        const int MaxParticles = 2048;

        readonly List<Vector3> _verts = new();
        readonly List<int> _tris = new();
        readonly Vector3[] _corners = new Vector3[8];
        readonly Vector2[] _tri = new Vector2[3];
        readonly List<Vector2> _polyA = new();
        readonly List<Vector2> _polyB = new();
        ParticleSystem.Particle[] _particles;
        Mesh _baked;

        public float Measure(Camera cam, Renderer r)
        {
            if (r is ParticleSystemRenderer)
            {
                var ps = r.GetComponent<ParticleSystem>();
                return ps != null ? Particles(cam, ps) : 0f;
            }
            // BakeMesh yerel çizgide konumu katmaz (yalnız dönüş/ölçek); iz her zaman dünya uzayında.
            if (r is LineRenderer line)
                return line.useWorldSpace
                    ? Baked(cam, m => line.BakeMesh(m, cam, false), Matrix4x4.identity)
                    : Baked(cam, m => line.BakeMesh(m, cam, true), Matrix4x4.Translate(line.transform.position));
            if (r is TrailRenderer trail)
                return Baked(cam, m => trail.BakeMesh(m, cam, false), Matrix4x4.identity);
            var filter = r.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh != null && mesh.isReadable)
                return Triangles(cam, mesh, r.transform.localToWorldMatrix);
            return BoundsRect(cam, r.bounds);
        }

        float Baked(Camera cam, System.Action<Mesh> bake, Matrix4x4 toWorld)
        {
            _baked ??= new Mesh();
            _baked.Clear();
            bake(_baked);
            return Triangles(cam, _baked, toWorld);
        }

        float Triangles(Camera cam, Mesh mesh, Matrix4x4 toWorld)
        {
            mesh.GetVertices(_verts);
            float area = 0f;
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                if (mesh.GetTopology(sub) != MeshTopology.Triangles)
                    continue;
                mesh.GetTriangles(_tris, sub);
                for (int i = 0; i + 2 < _tris.Count; i += 3)
                {
                    bool front = true;
                    for (int k = 0; k < 3 && front; k++)
                    {
                        Vector3 vp = cam.WorldToViewportPoint(toWorld.MultiplyPoint3x4(_verts[_tris[i + k]]));
                        front = vp.z > cam.nearClipPlane;
                        _tri[k] = new Vector2(vp.x, vp.y);
                    }
                    if (!front)
                        continue;
                    area += ClippedArea();
                    if (area >= 1f)
                        return 1f;
                }
            }
            return area;
        }

        /// <summary>Üçgen viewport karesine (0..1) kırpılır (Sutherland–Hodgman), kalan çokgenin alanı.</summary>
        float ClippedArea()
        {
            _polyA.Clear();
            _polyA.AddRange(_tri);
            ClipEdge(_polyA, _polyB, 0, 0f, true);
            ClipEdge(_polyB, _polyA, 0, 1f, false);
            ClipEdge(_polyA, _polyB, 1, 0f, true);
            ClipEdge(_polyB, _polyA, 1, 1f, false);
            float twice = 0f;
            for (int i = 0; i < _polyA.Count; i++)
            {
                Vector2 a = _polyA[i];
                Vector2 b = _polyA[(i + 1) % _polyA.Count];
                twice += a.x * b.y - b.x * a.y;
            }
            return Mathf.Abs(twice) * 0.5f;
        }

        static void ClipEdge(List<Vector2> input, List<Vector2> output, int axis, float bound, bool keepAbove)
        {
            output.Clear();
            for (int i = 0; i < input.Count; i++)
            {
                Vector2 cur = input[i];
                Vector2 prev = input[(i + input.Count - 1) % input.Count];
                bool curIn = keepAbove ? cur[axis] >= bound : cur[axis] <= bound;
                bool prevIn = keepAbove ? prev[axis] >= bound : prev[axis] <= bound;
                if (curIn != prevIn)
                {
                    float t = (bound - prev[axis]) / (cur[axis] - prev[axis]);
                    output.Add(Vector2.Lerp(prev, cur, t));
                }
                if (curIn)
                    output.Add(cur);
            }
        }

        float Particles(Camera cam, ParticleSystem ps)
        {
            _particles ??= new ParticleSystem.Particle[MaxParticles];
            int n = ps.GetParticles(_particles);
            bool local = ps.main.simulationSpace == ParticleSystemSimulationSpace.Local;
            float tanHalf = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float area = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = local ? ps.transform.TransformPoint(_particles[i].position) : _particles[i].position;
                Vector3 vp = cam.WorldToViewportPoint(p);
                if (vp.z <= cam.nearClipPlane)
                    continue;
                // Disk yarıçapı viewport biriminde: dikeyde 2·z·tan(fov/2) = 1.
                float ry = _particles[i].GetCurrentSize(ps) * 0.5f / (2f * vp.z * tanHalf);
                float rx = ry / Mathf.Max(0.01f, cam.aspect);
                float w = Mathf.Clamp01(vp.x + rx) - Mathf.Clamp01(vp.x - rx);
                float h = Mathf.Clamp01(vp.y + ry) - Mathf.Clamp01(vp.y - ry);
                area += w * h * (Mathf.PI * 0.25f);
                if (area >= 1f)
                    return 1f;
            }
            return area;
        }

        float BoundsRect(Camera cam, Bounds b)
        {
            Vector3 c = b.center, e = b.extents;
            int k = 0;
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                _corners[k++] = c + new Vector3(e.x * sx, e.y * sy, e.z * sz);
            float minX = 1f, minY = 1f, maxX = 0f, maxY = 0f;
            bool any = false;
            for (int i = 0; i < _corners.Length; i++)
            {
                Vector3 vp = cam.WorldToViewportPoint(_corners[i]);
                if (vp.z <= cam.nearClipPlane)
                    continue;
                any = true;
                minX = Mathf.Min(minX, Mathf.Clamp01(vp.x));
                maxX = Mathf.Max(maxX, Mathf.Clamp01(vp.x));
                minY = Mathf.Min(minY, Mathf.Clamp01(vp.y));
                maxY = Mathf.Max(maxY, Mathf.Clamp01(vp.y));
            }
            return any ? Mathf.Max(0f, maxX - minX) * Mathf.Max(0f, maxY - minY) : 0f;
        }
    }
}
