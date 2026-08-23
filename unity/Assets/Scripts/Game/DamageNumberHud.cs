using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// T12: kapanış hasarı ölçüm aracı. Varsayılan kapalı (§5/§12 — his kanalı değil kumpas).
    /// ShowFrameTimeHud deseninin aynısı: kapalıyken Text disabled (overdraw yok).
    /// </summary>
    public sealed class DamageNumberHud : MonoBehaviour
    {
        PrototypeTuning _tuning;
        Text _text;
        readonly StringBuilder _sb = new StringBuilder(32);

        float _hideAtUnscaled = -1f;
        bool _appliedVisible;

        public void Configure(PrototypeTuning tuning, Transform canvasRoot)
        {
            _tuning = tuning;

            var go = new GameObject("DamageNumberHud");
            go.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                go.layer = canvasRoot.gameObject.layer;

            var rect = go.AddComponent<RectTransform>();
            // Sol-alt: FrameTimeHud'un üstü — ölçüm araçları aynı köşede.
            rect.anchorMin = new Vector2(0.02f, 0.14f);
            rect.anchorMax = new Vector2(0.40f, 0.22f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _text = go.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_text.font == null)
                _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _text.fontSize = 26;
            _text.color = new Color(0.9f, 0.95f, 1f, 0.95f);
            _text.alignment = TextAnchor.LowerLeft;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;
            _text.text = string.Empty;

            _appliedVisible = true;
            ApplyVisibility(false);
        }

        /// <summary>Kapanışın verdiği hasarı yazar — yalnızca ShowDamageNumbers açıksa.</summary>
        public void ShowDamage(float amount)
        {
            if (_tuning == null || !_tuning.ShowDamageNumbers || _text == null)
                return;

            _sb.Clear();
            _sb.Append(amount.ToString("0.#"));
            _text.text = _sb.ToString();
            _hideAtUnscaled = Time.unscaledTime + 1.1f;
            ApplyVisibility(true);
        }

        void ApplyVisibility(bool visible)
        {
            if (visible == _appliedVisible)
                return;

            _appliedVisible = visible;
            _text.enabled = visible;
            if (!visible)
                _text.text = string.Empty;
        }

        void Update()
        {
            if (_text == null || _tuning == null)
                return;

            if (!_tuning.ShowDamageNumbers)
            {
                ApplyVisibility(false);
                _hideAtUnscaled = -1f;
                return;
            }

            if (_hideAtUnscaled < 0f)
            {
                ApplyVisibility(false);
                return;
            }

            if (Time.unscaledTime >= _hideAtUnscaled)
            {
                _hideAtUnscaled = -1f;
                ApplyVisibility(false);
            }
        }
    }
}
