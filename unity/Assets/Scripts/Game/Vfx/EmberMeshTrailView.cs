using System.Collections.Generic;
using Dovus.Core.Presentation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §3.1 A: mesh trail — Kabza/Tip örnekleri, Catmull-Rom ara nokta, şerit mesh.
    /// ATIL ömrü 0,25 s; _FlowOffset UV kayması.
    /// </summary>
    public sealed class EmberMeshTrailView : MonoBehaviour
    {
        struct Sample
        {
            public Vector3 Guard;
            public Vector3 Tip;
        }

        readonly List<Sample> _ring = new(VfxPlanDefaults.KilicIzOrnek);
        Mesh _mesh;
        MeshFilter _filter;
        MeshRenderer _renderer;
        Material _mat;
        float _life;
        float _age;
        float _flow;
        Color _core;
        bool _emitting;
        Vector3 _fallbackForward = Vector3.forward;

        public void Begin(in VfxColorRgb color, float lifeSec, Vector3 forward)
        {
            EnsureMesh();
            _core = new Color(color.R * 2f, color.G * 2f, color.B * 2f, 1f);
            _life = Mathf.Max(0.05f, lifeSec);
            _age = 0f;
            _flow = 0f;
            _emitting = true;
            _ring.Clear();
            if (forward.sqrMagnitude > 0.0001f)
                _fallbackForward = forward.normalized;
            if (_renderer != null)
                _renderer.enabled = true;
            ApplyMat(1f);
        }

        public void SampleBlade(Vector3 guard, Vector3 tip)
        {
            if (!_emitting)
                return;
            _ring.Add(new Sample { Guard = guard, Tip = tip });
            while (_ring.Count > RuleVfxDefaults.MeshTrailSamples)
                _ring.RemoveAt(0);
            Rebuild();
        }

        public void StopEmit() => _emitting = false;

        void Update()
        {
            if (_mesh == null || _life <= 0f)
                return;
            _age += Time.deltaTime;
            _flow += Time.deltaTime * VfxPlanDefaults.KilicIzAkisTurPerSec;
            float remain = 1f - Mathf.Clamp01(_age / _life);
            ApplyMat(remain);
            if (_mat != null && _mat.HasProperty("_FlowOffset"))
                _mat.SetFloat("_FlowOffset", _flow);
            if (_age >= _life)
            {
                if (_renderer != null)
                    _renderer.enabled = false;
                _life = 0f;
            }
        }

        void EnsureMesh()
        {
            if (_filter != null)
                return;
            var go = new GameObject("EmberMeshTrail");
            go.transform.SetParent(transform, false);
            _filter = go.AddComponent<MeshFilter>();
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _mesh = new Mesh { name = "EmberTrailRibbon" };
            _mesh.MarkDynamic();
            _filter.sharedMesh = _mesh;
            Shader sh = Shader.Find(RuleVfxDefaults.KorShaderName)
                ?? Shader.Find(RuleVfxDefaults.ParticlesUnlit);
            _mat = new Material(sh);
            _renderer.sharedMaterial = _mat;
        }

        void Rebuild()
        {
            if (_ring.Count < 2 || _mesh == null)
                return;

            int n = _ring.Count;
            int verts = n * 2;
            var positions = new Vector3[verts];
            var uvs = new Vector2[verts];
            var colors = new Color[verts];
            var indices = new int[Mathf.Max(0, (n - 1) * 6)];

            for (int i = 0; i < n; i++)
            {
                Sample s = _ring[i];
                positions[i * 2] = s.Guard;
                positions[i * 2 + 1] = s.Tip;
                float u = i / (float)(n - 1);
                uvs[i * 2] = new Vector2(u, 0f);
                uvs[i * 2 + 1] = new Vector2(u, 1f);
                float a = Mathf.Lerp(0.15f, 1f, u);
                colors[i * 2] = new Color(1f, 1f, 1f, a * 0.55f);
                colors[i * 2 + 1] = new Color(1f, 1f, 1f, a);
            }

            int ti = 0;
            for (int i = 0; i < n - 1; i++)
            {
                int a = i * 2;
                indices[ti++] = a;
                indices[ti++] = a + 1;
                indices[ti++] = a + 2;
                indices[ti++] = a + 1;
                indices[ti++] = a + 3;
                indices[ti++] = a + 2;
            }

            _mesh.Clear();
            _mesh.SetVertices(positions);
            _mesh.SetUVs(0, uvs);
            _mesh.SetColors(colors);
            _mesh.SetTriangles(indices, 0);
            _mesh.RecalculateBounds();
        }

        void ApplyMat(float alphaScale)
        {
            if (_mat == null)
                return;
            Color c = _core;
            c.a = alphaScale;
            if (_mat.HasProperty("_CoreColor"))
                _mat.SetColor("_CoreColor", c);
            if (_mat.HasProperty("_Intensity"))
                _mat.SetFloat("_Intensity", Mathf.Lerp(0.4f, 2.2f, alphaScale));
            if (_mat.HasProperty("_Color"))
                _mat.SetColor("_Color", c);
        }

        void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
            if (_mat != null)
                Destroy(_mat);
        }
    }
}
