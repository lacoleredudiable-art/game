using System.Collections.Generic;
using Dovus.Core.Combat;
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
    ///
    /// Denetim B ek (çizim geri bildirimi): parmağın HAM izi çizim boyunca ince bir çizgiyle görünür
    /// (<see cref="RawBegin"/>/<see cref="RawAppend"/>); tanınan cümle şeridi noktalara oturmuş hâliyle
    /// beyaz-altın parlar (<see cref="Break(bool)"/>), tanınmayan çizgi kırmızı söner
    /// (<see cref="RawEnd(bool)"/>).
    /// </summary>
    public sealed class InkTrail : MonoBehaviour
    {
        PrototypeTuning _tuning;
        HexagonOverlayCamera _overlay;
        readonly List<Trail> _trails = new List<Trail>(8);
        Material _lineMaterial;
        int _layer;

        LineRenderer _active;

        public const int RawMax = 128;
        static readonly Color FlashColor = new Color(1f, 0.93f, 0.62f, 1f);
        static readonly Color FailColor = new Color(1f, 0.28f, 0.25f, 1f);
        LineRenderer _raw;
        readonly Vector3[] _rawPts = new Vector3[RawMax];
        int _rawCount;
        Vector2 _rawLastPx;
        bool _rawLive;
        float _rawFadeBorn = -1f;
        float _rawFadeLife;
        bool _rawFailed;

        struct Trail
        {
            public LineRenderer Line;
            public float BornUnscaled;
            public float LifeSec;
            public bool Flash;
            public float BaseWidth;
        }

        public void Configure(PrototypeTuning tuning, HexagonOverlayCamera overlay, int layer)
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
        public void Break() => Break(false);

        /// <param name="flash">Tanınan cümle: şerit beyaz-altın parlayıp (kalın) normal renge iner, uzun söner.</param>
        public void Break(bool flash)
        {
            if (_active == null)
                return;

            float life = _tuning != null ? _tuning.InkLingerSec : 0.4f;

            // Ömür Break'ten başlar (§5: sınırda söner). Born çizim başı olsaydı yavaş
            // çekimde uzun şerit anında yok olurdu.
            _trails.Add(new Trail
            {
                Line = _active,
                BornUnscaled = Time.unscaledTime,
                LifeSec = flash ? life * DrawFeedback.FlashLifeScale : life,
                Flash = flash,
                BaseWidth = _active.startWidth
            });
            _active = null;
        }

        /// <summary>Çizim başladı: ham iz parmağı izler (cümle şeridinden bağımsız).</summary>
        public void RawBegin(Vector2 screenPx)
        {
            if (_tuning == null || _overlay == null)
                return;
            EnsureMaterial();
            if (_raw == null)
            {
                _raw = CreateLine();
                _raw.gameObject.name = "InkRaw";
                _raw.startWidth *= 0.6f;
                _raw.endWidth = _raw.startWidth;
                _raw.sortingOrder = 9;
            }
            _raw.enabled = true;
            _rawCount = 0;
            _rawLive = true;
            _rawFadeBorn = -1f;
            _rawFailed = false;
            PushRaw(screenPx);
            ApplyRaw(1f);
        }

        /// <summary>Parmak örneği; ~4 dp altı adımlar atlanır, 128 noktayı aşınca seyreltilir.</summary>
        public void RawAppend(Vector2 screenPx)
        {
            if (!_rawLive || _raw == null)
                return;
            if (Vector2.Distance(screenPx, _rawLastPx) < HexagonLayoutScreen.DpToPixels(4f))
                return;
            if (_rawCount >= RawMax)
            {
                // Seyrelt: her ikinci noktayı at (ilk ve son korunur).
                int w = 1;
                for (int r = 2; r < _rawCount; r += 2)
                    _rawPts[w++] = _rawPts[r];
                _rawCount = w;
            }
            PushRaw(screenPx);
        }

        /// <summary>Çizim bitti: tanınmadıysa kırmızı söner, değilse kısa sürede söner.</summary>
        public void RawEnd(bool failed)
        {
            if (_raw == null || !_rawLive)
                return;
            _rawLive = false;
            _rawFailed = failed;
            _rawFadeBorn = Time.unscaledTime;
            _rawFadeLife = failed ? DrawFeedback.FailFadeSec : DrawFeedback.RawFadeSec;
            if (failed && _rawCount == 1)
                PushRaw(_rawLastPx + new Vector2(0.5f, 0.5f));
        }

        void PushRaw(Vector2 screenPx)
        {
            _rawPts[_rawCount++] = _overlay.ScreenToWorld(screenPx);
            _rawLastPx = screenPx;
            _raw.positionCount = _rawCount;
            for (int i = 0; i < _rawCount; i++)
                _raw.SetPosition(i, _rawPts[i]);
        }

        void ApplyRaw(float alpha)
        {
            Color a = _rawFailed ? FailColor : _tuning.InkCyan;
            Color b = _rawFailed ? FailColor : _tuning.InkPurple;
            a.a *= 0.6f * alpha;
            b.a *= 0.35f * alpha;
            _raw.startColor = b;
            _raw.endColor = a;
        }

        void TickRaw(float now)
        {
            if (_raw == null || !_raw.enabled)
                return;
            if (_rawLive)
            {
                ApplyRaw(1f);
                return;
            }
            float u = (now - _rawFadeBorn) / Mathf.Max(0.01f, _rawFadeLife);
            if (u >= 1f)
            {
                _raw.enabled = false;
                _rawCount = 0;
                _raw.positionCount = 0;
                return;
            }
            ApplyRaw(_rawFailed ? 1.6f * (1f - u) : 1f - u);
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
            TickRaw(now);

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
                if (t.Flash)
                {
                    float mix = DrawFeedback.FlashMix(u);
                    a = Color.Lerp(a, FlashColor, mix);
                    b = Color.Lerp(b, FlashColor, mix);
                    float wv = t.BaseWidth * (1f + 0.6f * mix);
                    t.Line.startWidth = wv;
                    t.Line.endWidth = wv * 0.85f;
                    // Parlarken tam görünür; sönme parlama bitince başlar.
                    alpha = u <= DrawFeedback.FlashFraction ? 1f : 1f - (u - DrawFeedback.FlashFraction) / (1f - DrawFeedback.FlashFraction);
                }
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
            float width = HexagonLayoutScreen.DpToPixels(_tuning.InkWidthDp);
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
