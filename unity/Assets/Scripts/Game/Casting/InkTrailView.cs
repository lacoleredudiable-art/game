using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Config;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Casting
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
    public sealed class InkTrailView : MonoBehaviour
    {
        GameTuning _tuning;
        HexagonOverlayCameraView _overlay;
        readonly List<Trail> _trails = new List<Trail>(8);
        Material _lineMaterial;
        int _layer;

        LineRenderer _active;

        public const int RawMax = 128;
        static readonly Color FlashColor = new Color(1f, 0.93f, 0.62f, 1f);
        static readonly Color FailColor = new Color(1f, 0.28f, 0.25f, 1f);
        LineRenderer _raw;
        readonly Vector3[] _rawPts = new Vector3[RawMax];
        readonly Vector2[] _rawScreenPx = new Vector2[RawMax];
        readonly Vector2[] _rawSnapFromPx = new Vector2[RawMax];
        readonly Vector2[] _rawSnapToPx = new Vector2[RawMax];
        int _rawCount;
        Vector2 _rawLastPx;
        bool _rawLive;
        float _rawFadeBorn = -1f;
        float _rawFadeLife;
        bool _rawFailed;
        bool _rawSnapping;
        float _rawSnapBorn;
        float _rawBaseStartWidth;
        float _rawBaseEndWidth;
        static readonly Color RawHeadWhite = Color.white;
        /// <summary>spec'te yok — varsayılan</summary>
        const float RawSnapSec = 0.12f;

        struct Trail
        {
            public LineRenderer Line;
            public float BornUnscaled;
            public float LifeSec;
            public bool Flash;
            public float BaseWidth;
        }

        public void Configure(GameTuning tuning, HexagonOverlayCameraView overlay, int layer)
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

            float life = _tuning != null ? _tuning.Input.InkLingerSec : InkTrailViewDefaults.DefaultLingerSec;

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
                _raw.sortingOrder = 11;
            }
            ApplyRawWidthScale();
            _raw.enabled = true;
            _rawCount = 0;
            _rawLive = true;
            _rawFadeBorn = -1f;
            _rawFailed = false;
            _rawSnapping = false;
            PushRaw(screenPx);
            ApplyRaw(1f);
        }

        /// <summary>Parmak örneği; ~4 dp altı adımlar atlanır, 128 noktayı aşınca seyreltilir.</summary>
        public void RawAppend(Vector2 screenPx)
        {
            if (!_rawLive || _raw == null)
                return;
            if (Vector2.Distance(screenPx, _rawLastPx) < HexagonLayoutScreen.DpToPixels(InkTrailViewDefaults.RawMinStepDp))
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
        public void RawEnd(bool failed) => RawEnd(failed, null);

        /// <summary>Çizim bitti; başarılıda isteğe bağlı tanınan yola oturma.</summary>
        public void RawEnd(bool failed, IReadOnlyList<Vector2> snapPathScreenPx)
        {
            if (_raw == null || !_rawLive)
                return;
            _rawLive = false;
            _rawFailed = failed;
            _rawSnapping = false;
            if (failed)
            {
                if (_rawCount == 1)
                    PushRaw(_rawLastPx + new Vector2(0.5f, 0.5f));
                else if (_rawCount >= 2)
                    ApplyFailDispersion();
                _raw.startWidth = _rawBaseStartWidth * InkTrailViewDefaults.FailWidthMult;
                _raw.endWidth = _rawBaseEndWidth * InkTrailViewDefaults.FailWidthMult;
                _rawFadeBorn = Time.unscaledTime;
                _rawFadeLife = DrawFeedback.FailFadeSec;
                ApplyRaw(1f);
                return;
            }

            if (snapPathScreenPx != null && snapPathScreenPx.Count >= 2 && _rawCount >= 1)
            {
                for (int i = 0; i < _rawCount; i++)
                {
                    _rawSnapFromPx[i] = _rawScreenPx[i];
                    _rawSnapToPx[i] = ClosestPointOnPolyline(snapPathScreenPx, _rawScreenPx[i]);
                }
                _rawSnapping = true;
                _rawSnapBorn = Time.unscaledTime;
                _rawFadeBorn = -1f;
                ApplyRaw(1f);
                return;
            }

            _rawFadeBorn = Time.unscaledTime;
            _rawFadeLife = DrawFeedback.RawFadeSec;
            ApplyRaw(1f);
        }

        void PushRaw(Vector2 screenPx)
        {
            _rawScreenPx[_rawCount] = screenPx;
            _rawPts[_rawCount++] = _overlay.ScreenToWorld(screenPx);
            _rawLastPx = screenPx;
            _raw.positionCount = _rawCount;
            for (int i = 0; i < _rawCount; i++)
                _raw.SetPosition(i, _rawPts[i]);
        }

        void ApplyRawWidthScale()
        {
            float w = HexagonLayoutScreen.DpToPixels(_tuning.Input.InkWidthDp);
            float scale = _tuning.Input.InkRawWidthScale > 0f ? _tuning.Input.InkRawWidthScale : InkTrailViewDefaults.DefaultRawWidthScale;
            _rawBaseStartWidth = w * scale;
            _rawBaseEndWidth = w * scale;
            _raw.startWidth = _rawBaseStartWidth;
            _raw.endWidth = _rawBaseEndWidth;
        }

        void ApplyRaw(float alpha)
        {
            if (_rawFailed)
            {
                Color c = FailColor;
                c.a *= alpha;
                _raw.startColor = c;
                _raw.endColor = c;
                return;
            }

            float glow = _tuning.Input.InkRawGlow > 0f ? _tuning.Input.InkRawGlow : 1f;
            Color head = Color.Lerp(_tuning.Visuals.InkCyan, RawHeadWhite, InkTrailViewDefaults.RawHeadLerp * glow);
            head.a = InkTrailViewDefaults.RawHeadAlpha * alpha;
            Color tail = _tuning.Visuals.InkPurple;
            tail.a = InkTrailViewDefaults.RawTailAlpha * alpha;
            _raw.startColor = tail;
            _raw.endColor = head;
        }

        void ApplyFailDispersion()
        {
            for (int i = 0; i < _rawCount; i++)
            {
                Vector2 p = _rawScreenPx[i];
                Vector2 tangent;
                if (i == 0)
                    tangent = _rawScreenPx[1] - _rawScreenPx[0];
                else if (i == _rawCount - 1)
                    tangent = _rawScreenPx[i] - _rawScreenPx[i - 1];
                else
                    tangent = _rawScreenPx[i + 1] - _rawScreenPx[i - 1];
                if (tangent.sqrMagnitude < InkTrailViewDefaults.TangentEpsilonSqr)
                    tangent = Vector2.right;
                tangent.Normalize();
                Vector2 normal = new Vector2(-tangent.y, tangent.x);
                float ampDp = FailDispersionAmpDp(i);
                float sign = (HashU32(i) & 1) == 0 ? 1f : -1f;
                Vector2 off = normal * (sign * HexagonLayoutScreen.DpToPixels(ampDp));
                _rawScreenPx[i] = p + off;
                _rawPts[i] = _overlay.ScreenToWorld(_rawScreenPx[i]);
                _raw.SetPosition(i, _rawPts[i]);
            }
        }

        static float FailDispersionAmpDp(int index)
        {
            float t = (HashU32(index) & 0xffff) / InkTrailViewDefaults.HashNormalizeDiv;
            return InkTrailViewDefaults.FailDispersionBaseDp + t * InkTrailViewDefaults.FailDispersionRangeDp;
        }

        static uint HashU32(int index)
        {
            unchecked
            {
                return (uint)(index * InkTrailViewDefaults.HashMultiplier + InkTrailViewDefaults.HashAdd);
            }
        }

        static Vector2 ClosestPointOnPolyline(IReadOnlyList<Vector2> path, Vector2 p)
        {
            Vector2 best = path[0];
            float bestD2 = (p - best).sqrMagnitude;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector2 a = path[i];
                Vector2 b = path[i + 1];
                Vector2 ab = b - a;
                float len2 = ab.sqrMagnitude;
                float t = len2 > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
                Vector2 q = a + ab * t;
                float d2 = (p - q).sqrMagnitude;
                if (d2 < bestD2)
                {
                    bestD2 = d2;
                    best = q;
                }
            }
            return best;
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
            if (_rawSnapping)
            {
                float su = (now - _rawSnapBorn) / Mathf.Max(0.001f, RawSnapSec);
                float st = su >= 1f ? 1f : su;
                for (int i = 0; i < _rawCount; i++)
                {
                    Vector2 px = Vector2.Lerp(_rawSnapFromPx[i], _rawSnapToPx[i], st);
                    _rawScreenPx[i] = px;
                    _rawPts[i] = _overlay.ScreenToWorld(px);
                    _raw.SetPosition(i, _rawPts[i]);
                }
                ApplyRaw(1f);
                if (su >= 1f)
                {
                    _rawSnapping = false;
                    _rawFadeBorn = now;
                    _rawFadeLife = DrawFeedback.RawFadeSec;
                }
                return;
            }
            if (_rawFadeBorn < 0f)
                return;
            float u = (now - _rawFadeBorn) / Mathf.Max(InkTrailViewDefaults.FadeMinLifeSec, _rawFadeLife);
            if (u >= 1f)
            {
                _raw.enabled = false;
                _rawCount = 0;
                _raw.positionCount = 0;
                return;
            }
            ApplyRaw(_rawFailed ? InkTrailViewDefaults.FailWidthMult * (1f - u) : 1f - u);
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

                float u = (now - t.BornUnscaled) / Mathf.Max(InkTrailViewDefaults.FadeMinLifeSec, t.LifeSec);
                if (u >= 1f)
                {
                    Destroy(t.Line.gameObject);
                    _trails.RemoveAt(i);
                    continue;
                }

                float alpha = 1f - u;
                Color a = _tuning.Visuals.InkPurple;
                Color b = _tuning.Visuals.InkCyan;
                if (t.Flash)
                {
                    float mix = DrawFeedback.FlashMix(u);
                    a = Color.Lerp(a, FlashColor, mix);
                    b = Color.Lerp(b, FlashColor, mix);
                    float wv = t.BaseWidth * (1f + InkTrailViewDefaults.FlashWidthMix * mix);
                    t.Line.startWidth = wv;
                    t.Line.endWidth = wv * InkTrailViewDefaults.LineEndWidthMult;
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

            Color a = _tuning.Visuals.InkPurple;
            Color b = _tuning.Visuals.InkCyan;
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
            float width = HexagonLayoutScreen.DpToPixels(_tuning.Input.InkWidthDp);
            line.startWidth = width;
            line.endWidth = width * InkTrailViewDefaults.LineEndWidthMult;
            line.sortingOrder = 10;
            return line;
        }

        void EnsureMaterial()
        {
            if (_lineMaterial != null)
                return;

            var shader = AssetLoader.FindShader("Sprites/Default", null);
            if (shader == null)
                shader = AssetLoader.FindShader("Universal Render Pipeline/Unlit", null);
            if (shader == null)
                shader = AssetLoader.FindShader("Unlit/Color", null);
            _lineMaterial = new Material(shader != null ? shader : AssetLoader.FindShader("Hidden/Internal-Colored", null));
            if (_lineMaterial.HasProperty("_Color"))
                _lineMaterial.SetColor("_Color", Color.white);

            Texture2D inkTex = KenneyVfxTextures.Load(KenneyVfxTextures.TexInk);
            if (inkTex != null)
            {
                if (_lineMaterial.HasProperty("_BaseMap"))
                    _lineMaterial.SetTexture("_BaseMap", inkTex);
                _lineMaterial.mainTexture = inkTex;
            }
        }
    }
}
