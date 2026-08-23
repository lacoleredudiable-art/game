using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Noktalar arası mürekkep izi — mor→camgöbeği (§10). LineRenderer, kısa ömür.
    /// Ekrana sabit overlay kamera uzayı (1 unit = 1 px).
    ///
    /// Bir cümle = bir şerit. <see cref="Break"/> cümle sınırında aktif şeridi bırakır
    /// (kendi ömrüyle söner); sonraki <see cref="AddSegment"/> yeni şerit açar — gradient
    /// yeniden mor'dan başlar. Böylece §5 "cümlenin nerede bittiği görülür" tutulur.
    /// </summary>
    public sealed class InkTrail : MonoBehaviour
    {
        PrototypeTuning _tuning;
        PentagonOverlayCamera _overlay;
        readonly List<Trail> _trails = new List<Trail>(8);
        Material _lineMaterial;
        int _layer;

        LineRenderer _active;

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

        /// <summary>
        /// Aktif şeridi kapatır. Eski noktalar listede kalır ve Update'te söner; yeni cümle
        /// sıfırdan şerit açar. Cümle bitmeden çağrılırsa (Abort) de aynı — iz kopar.
        /// </summary>
        public void Break()
        {
            if (_active == null)
                return;

            // Ömür Break'ten başlar (§5: sınırda söner). Born çizim başı olsaydı yavaş
            // çekimde uzun şerit anında yok olurdu.
            _trails.Add(new Trail
            {
                Line = _active,
                BornUnscaled = Time.unscaledTime,
                LifeSec = _tuning != null ? _tuning.InkLingerSec : 0.4f
            });
            _active = null;
        }

        public void AddSegment(Vector2 screenFrom, Vector2 screenTo)
        {
            if (_tuning == null || _overlay == null)
                return;

            EnsureMaterial();

            if (_active == null)
            {
                _active = CreateLine();
                _active.positionCount = 2;
                _active.SetPosition(0, _overlay.ScreenToWorld(screenFrom));
                _active.SetPosition(1, _overlay.ScreenToWorld(screenTo));
                ApplyActiveColors(1f);
                return;
            }

            int n = _active.positionCount;
            _active.positionCount = n + 1;
            _active.SetPosition(n, _overlay.ScreenToWorld(screenTo));
            ApplyActiveColors(1f);
        }

        void Update()
        {
            if (_tuning == null)
                return;

            float now = Time.unscaledTime;

            // Aktif şerit henüz Break edilmedi — cümle sürerken tam görünür (alfa 1).
            // Ömür Break'ten sonra başlar; burada yalnızca renk taze tutulur.
            if (_active != null)
                ApplyActiveColors(1f);

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

        void ApplyActiveColors(float alpha)
        {
            if (_active == null || _tuning == null)
                return;

            Color a = _tuning.InkPurple;
            Color b = _tuning.InkCyan;
            a.a *= alpha;
            b.a *= alpha;
            _active.startColor = a;
            _active.endColor = b;
        }

        LineRenderer CreateLine()
        {
            var go = new GameObject("InkRibbon");
            go.transform.SetParent(transform, false);
            go.layer = _layer;

            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = _lineMaterial;
            line.useWorldSpace = true;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            float width = PentagonLayoutScreen.DpToPixels(_tuning.InkWidthDp);
            line.startWidth = width;
            line.endWidth = width * 0.85f;
            line.sortingOrder = 10;
            return line;
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
