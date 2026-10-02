using System.Collections.Generic;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// İsabet anında gövdeyi kısa süre beyaza çeker. Materyal kopyalanmaz: renk
    /// <see cref="MaterialPropertyBlock"/> ile yazılır, bitince blok temizlenir.
    /// Süre/güç <see cref="FeelTuning.HitFlashMs"/> / <see cref="FeelTuning.HitFlashStrength"/>.
    /// </summary>
    public sealed class HitFlash : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        struct Slot
        {
            public Renderer Renderer;
            public int PropertyId;
            public Color Original;
        }

        readonly List<Slot> _slots = new();
        MaterialPropertyBlock _block;
        FeelTuning _feel;
        Color _tint = Color.white;
        float _startUnscaled = -1f;
        int _durationMs = 90;
        bool _active;

        public int RendererCount => _slots.Count;
        public bool IsFlashing => _active;

        public void Bind(FeelTuning feel)
        {
            _feel = feel;
            CollectRenderers();
        }

        /// <summary>Görsel prefab sonradan değişirse (ör. model takası) yeniden toplanır.</summary>
        public void CollectRenderers()
        {
            Clear();
            _slots.Clear();
            var renderers = GetComponentsInChildren<Renderer>(false);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer)
                    continue;
                Material m = r.sharedMaterial;
                if (m == null)
                    continue;
                int id = m.HasProperty(BaseColorId) ? BaseColorId
                    : m.HasProperty(ColorId) ? ColorId
                    : m.HasProperty(EmissionId) ? EmissionId
                    : 0;
                if (id == 0)
                    continue;
                _slots.Add(new Slot { Renderer = r, PropertyId = id, Original = m.GetColor(id) });
            }
        }

        public void Flash() => Flash(Color.white);

        public void Flash(Color tint)
        {
            Flash(tint, -1);
        }

        public void Flash(Color tint, int durationMsOverride)
        {
            if (_feel == null || _feel.HitFlashStrength <= 0f || _slots.Count == 0)
                return;
            int ms = durationMsOverride > 0 ? durationMsOverride : _feel.HitFlashMs;
            if (ms <= 0)
                return;
            _tint = tint;
            _durationMs = ms;
            _startUnscaled = Time.unscaledTime;
            _active = true;
        }

        void LateUpdate()
        {
            if (!_active)
                return;
            float dur = Mathf.Max(0.001f, _durationMs / 1000f);
            float t = (Time.unscaledTime - _startUnscaled) / dur;
            if (t >= 1f)
            {
                Clear();
                return;
            }

            float k = _feel.HitFlashStrength * (1f - t) * (1f - t);
            _block ??= new MaterialPropertyBlock();
            for (int i = 0; i < _slots.Count; i++)
            {
                Slot s = _slots[i];
                if (s.Renderer == null)
                    continue;
                s.Renderer.GetPropertyBlock(_block);
                Color target = s.PropertyId == EmissionId ? _tint * 2f : _tint * 1.6f;
                _block.SetColor(s.PropertyId, Color.Lerp(s.Original, target, k));
                s.Renderer.SetPropertyBlock(_block);
            }
        }

        void Clear()
        {
            if (!_active)
                return;
            _active = false;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Renderer != null)
                    _slots[i].Renderer.SetPropertyBlock(null);
            }
        }

        void OnDisable() => Clear();
    }
}
