using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Kapanış hasarı his kanalı. Varsayılan açık (ShowDamageNumbers).
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
            // Boss üst-orta — his kanalı; sol-alt kumpas değil.
            rect.anchorMin = new Vector2(0.35f, 0.78f);
            rect.anchorMax = new Vector2(0.65f, 0.88f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _text = go.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_text.font == null)
                _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _text.fontSize = 42;
            _text.fontStyle = FontStyle.Bold;
            _text.color = new Color(1f, 0.92f, 0.55f, 1f);
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;
            _text.text = string.Empty;

            _appliedVisible = true;
            ApplyVisibility(false);
        }

        const int NormalFontSize = 42;
        const int CritFontSize = 58;

        /// <summary>Kapanışın verdiği hasarı yazar — yalnızca ShowDamageNumbers açıksa.
        /// Negatif amount = heal (+N). Crit: sarı + büyük.</summary>
        public void ShowDamage(float amount, bool isCrit = false)
        {
            if (_tuning == null || !_tuning.ShowDamageNumbers || _text == null)
                return;

            _sb.Clear();
            if (amount < 0f)
            {
                _sb.Append('+');
                _sb.Append((-amount).ToString("0.#"));
                _text.color = new Color(0.45f, 1f, 0.7f);
                _text.fontSize = NormalFontSize;
            }
            else if (isCrit)
            {
                _sb.Append('-');
                _sb.Append(amount.ToString("0.#"));
                _text.color = new Color(1f, 0.92f, 0.2f, 1f); // sarı
                _text.fontSize = CritFontSize;
            }
            else
            {
                _sb.Append('-');
                _sb.Append(amount.ToString("0.#"));
                _text.color = Color.white;
                _text.fontSize = NormalFontSize;
            }
            _text.text = _sb.ToString();
            _hideAtUnscaled = Time.unscaledTime + 1.25f;
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
