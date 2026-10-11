using System.Collections.Generic;
using Dovus.Core.Presentation;
using Dovus.Core.RuleEngineV4;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §3.1 A: kor izi — her kare silahın Vfx_Kabza / Vfx_Tip dünya konumları arasında
    /// şerit örneği. Örnek kendi ömrüyle yaşlanır: kabza kenarı uca doğru kapanır (kuyrukta incelir)
    /// ve söner. VFX_Kor toplamalı; ayar slice_vfx.blade_trail.
    /// </summary>
    public sealed class EmberMeshTrailView : MonoBehaviour
    {
        struct Sample
        {
            public Vector3 Hilt;
            public Vector3 Tip;
            public float BornSec;
        }

        readonly List<Sample> _samples = new();
        readonly List<Vector3> _positions = new();
        readonly List<Vector2> _uvs = new();
        readonly List<Color> _colors = new();
        readonly List<int> _indices = new();
        GameObject _ribbon;
        Mesh _mesh;
        MeshRenderer _renderer;
        Material _mat;
        RuleEngineV4BladeTrailFx _fx;
        float _clock;
        float _flow;
        bool _emitting;

        public bool Emitting => _emitting;
        public int SampleCount => _samples.Count;
        public bool Visible => _renderer != null && _renderer.enabled;
        public Material TrailMaterial => _mat;
        public Mesh RibbonMesh => _mesh;

        public void Begin(in VfxColorRgb color, RuleEngineV4BladeTrailFx fx)
        {
            EnsureMesh();
            _fx = fx;
            Color core = new Color(
                color.R * RuleVfxArtDefaults.CoreHdrMult,
                color.G * RuleVfxArtDefaults.CoreHdrMult,
                color.B * RuleVfxArtDefaults.CoreHdrMult,
                1f);
            _mat.SetColor("_CoreColor", core);
            _mat.SetFloat("_Intensity", fx.Intensity);
            _flow = 0f;
            _emitting = true;
            _samples.Clear();
            _renderer.enabled = true;
        }

        public void SampleBlade(Vector3 hilt, Vector3 tip)
        {
            if (!_emitting)
                return;
            _samples.Add(new Sample { Hilt = hilt, Tip = tip, BornSec = _clock });
            Rebuild();
        }

        public void StopEmit() => _emitting = false;

        void Update()
        {
            _clock += Time.deltaTime;
            if (_mesh == null || _fx == null || !_renderer.enabled)
                return;
            int dead = 0;
            while (dead < _samples.Count && _clock - _samples[dead].BornSec >= _fx.LifeSec)
                dead++;
            if (dead > 0)
                _samples.RemoveRange(0, dead);
            if (_samples.Count == 0 && !_emitting)
            {
                _mesh.Clear();
                _renderer.enabled = false;
                return;
            }
            _flow += Time.deltaTime * RuleVfxDefaults.TrailFlowTurnsPerSec;
            _mat.SetFloat("_FlowOffset", _flow);
            Rebuild();
        }

        void EnsureMesh()
        {
            if (_ribbon != null)
                return;
            // Örnekler dünya konumu: şerit oyuncuyla taşınmasın diye kök nesnede, birim dönüşümde.
            _ribbon = new GameObject("EmberMeshTrail");
            _ribbon.AddComponent<MeshFilter>().sharedMesh = _mesh = new Mesh { name = "EmberTrailRibbon" };
            _mesh.MarkDynamic();
            _renderer = _ribbon.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            Shader sh = Shader.Find(RuleVfxDefaults.KorShaderName);
            if (sh == null)
                Debug.LogError("[EmberMeshTrail] " + RuleVfxDefaults.KorShaderName + " shader yok.");
            _mat = new Material(sh != null ? sh : Shader.Find(RuleVfxDefaults.ParticlesUnlit));
            _renderer.sharedMaterial = _mat;
            _renderer.enabled = false;
        }

        void Rebuild()
        {
            int n = _samples.Count;
            _mesh.Clear();
            if (n < 2)
                return;
            _positions.Clear();
            _uvs.Clear();
            _colors.Clear();
            _indices.Clear();
            for (int i = 0; i < n; i++)
            {
                Sample s = _samples[i];
                _fx.Shape(_clock - s.BornSec, out float width, out float alpha);
                _positions.Add(Vector3.LerpUnclamped(s.Tip, s.Hilt, width));
                _positions.Add(s.Tip);
                float u = i / (float)(n - 1);
                _uvs.Add(new Vector2(u, 0f));
                _uvs.Add(new Vector2(u, 1f));
                _colors.Add(new Color(1f, 1f, 1f, alpha * _fx.HiltAlpha));
                _colors.Add(new Color(1f, 1f, 1f, alpha));
            }
            for (int i = 0; i < n - 1; i++)
            {
                int a = i * 2;
                _indices.Add(a);
                _indices.Add(a + 1);
                _indices.Add(a + 2);
                _indices.Add(a + 1);
                _indices.Add(a + 3);
                _indices.Add(a + 2);
            }
            _mesh.SetVertices(_positions);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_indices, 0);
            _mesh.RecalculateBounds();
        }

        void OnDestroy()
        {
            if (_ribbon != null)
                Destroy(_ribbon);
            if (_mesh != null)
                Destroy(_mesh);
            if (_mat != null)
                Destroy(_mat);
        }
    }
}
