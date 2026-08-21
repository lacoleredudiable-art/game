using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Noktalar arası mürekkep izi — mor→camgöbeği (§10). LineRenderer, kısa ömür.
    /// Ekrana sabit overlay kamera uzayı (1 unit = 1 px).
    /// </summary>
    public sealed class InkTrail : MonoBehaviour
    {
        PrototypeTuning _tuning;
        PentagonOverlayCamera _overlay;
        readonly List<Trail> _trails = new List<Trail>(8);
        Material _lineMaterial;
        int _layer;

        struct Trail
        {
            public LineRenderer Line;
            public float BornUnscaled;
            public float LifeSec;
        }

        public void Configure(PrototypeTuning tuning, PentagonOverlayCamera overlay, int layer)
        {
            _tuning = tuning;
            _overlay = overlay;
            _layer = layer;
            EnsureMaterial();
        }

        public void AddSegment(Vector2 screenFrom, Vector2 screenTo)
        {
            if (_tuning == null || _overlay == null)
                return;

            EnsureMaterial();
            var go = new GameObject("InkSegment");
            go.transform.SetParent(transform, false);
            go.layer = _layer;

            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = _lineMaterial;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            float width = PentagonLayoutScreen.DpToPixels(_tuning.InkWidthDp);
            line.startWidth = width;
            line.endWidth = width * 0.85f;
            line.startColor = _tuning.InkPurple;
            line.endColor = _tuning.InkCyan;
            line.sortingOrder = 10;

            line.SetPosition(0, _overlay.ScreenToWorld(screenFrom));
            line.SetPosition(1, _overlay.ScreenToWorld(screenTo));

            _trails.Add(new Trail
            {
                Line = line,
                BornUnscaled = Time.unscaledTime,
                LifeSec = _tuning.InkLingerSec
            });
        }

        void Update()
        {
            if (_tuning == null)
                return;

            float now = Time.unscaledTime;
            for (int i = _trails.Count - 1; i >= 0; i--)
            {
                Trail t = _trails[i];
                if (t.Line == null)
                {
                    _trails.RemoveAt(i);
                    continue;
                }

                float u = (now - t.BornUnscaled) / Mathf.Max(0.01f, t.LifeSec);
                if (u >= 1f)
                {
                    Destroy(t.Line.gameObject);
                    _trails.RemoveAt(i);
                    continue;
                }

                float alpha = 1f - u;
                Color a = _tuning.InkPurple;
                Color b = _tuning.InkCyan;
                a.a *= alpha;
                b.a *= alpha;
                t.Line.startColor = a;
                t.Line.endColor = b;
            }
        }

        void EnsureMaterial()
        {
            if (_lineMaterial != null)
                return;

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            _lineMaterial = new Material(shader != null ? shader : Shader.Find("Hidden/Internal-Colored"));
            if (_lineMaterial.HasProperty("_Color"))
                _lineMaterial.SetColor("_Color", Color.white);
        }
    }
}
