using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Casting
{
    /// <summary>
    /// Denetim B ek ??? ??izim geri bildirimi yaz??s??: tan??nan c??mlede r??n adlar?? (alt??n), tan??nmayan
    /// ??izgide "tan??nmad??" (k??rm??z??). ??st noktan??n hemen ??st??nde, k??sa s??re g??r??n??r. Yaz?? yokken
    /// Text kapal??d??r (bo??ta harman/overdraw yok).
    /// </summary>
    public sealed partial class HexagonView
    {
        static readonly Color CaptionOk = new Color(1f, 0.9f, 0.55f, 1f);
        static readonly Color CaptionFail = new Color(1f, 0.35f, 0.32f, 1f);

        Text _drawCaption;
        float _drawCaptionShownAt = HexagonViewDefaults.DrawCaptionHiddenAtSec;
        Color _drawCaptionColor = Color.white;

        void BuildDrawCaption(Transform parent)
        {
            var go = new GameObject("DrawCaption");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            _drawCaption = go.AddComponent<Text>();
            _drawCaption.font = HudTheme.LegacyFont;
            if (_drawCaption.font == null)
                _drawCaption.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _drawCaption.alignment = TextAnchor.MiddleCenter;
            _drawCaption.fontStyle = FontStyle.Bold;
            _drawCaption.horizontalOverflow = HorizontalWrapMode.Overflow;
            _drawCaption.verticalOverflow = VerticalWrapMode.Overflow;
            _drawCaption.raycastTarget = false;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(HexagonViewDefaults.DrawCaptionOutlineOffsetPx, -HexagonViewDefaults.DrawCaptionOutlineOffsetPx);
            _drawCaption.enabled = false;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f);
        }

        /// <summary>HexagonInputController.DrawCaption: metin + tan??nd?? m??.</summary>
        public void ShowDrawCaption(string text, bool recognized)
        {
            if (_drawCaption == null || string.IsNullOrEmpty(text))
                return;
            _drawCaption.text = text;
            _drawCaptionColor = recognized ? CaptionOk : CaptionFail;
            _drawCaptionShownAt = Time.unscaledTime;
            _drawCaption.enabled = true;
            LayoutDrawCaption(Screen.width, Screen.height);
            TickDrawCaption();
        }

        void LayoutDrawCaption(int w, int h)
        {
            if (_drawCaption == null || !_drawCaption.enabled)
                return;
            Vector2 top = HexagonLayoutScreen.DotPx(1, _tuning, w, h);
            float dotR = HexagonLayoutScreen.DotHitRadiusPx(_tuning);
            var rt = _drawCaption.rectTransform;
            rt.anchoredPosition = top + new Vector2(0f, dotR + HexagonLayoutScreen.DpToPixels(8f));
            float size = HexagonLayoutScreen.DpToPixels(HexagonViewDefaults.DrawCaptionFontDp);
            rt.sizeDelta = new Vector2(HexagonLayoutScreen.DpToPixels(260f), size * 1.4f);
            _drawCaption.fontSize = Mathf.RoundToInt(size);
        }

        void TickDrawCaption()
        {
            if (_drawCaption == null || !_drawCaption.enabled)
                return;
            float alpha = DrawFeedback.CaptionAlpha(Time.unscaledTime - _drawCaptionShownAt);
            if (alpha <= 0f)
            {
                _drawCaption.enabled = false;
                return;
            }
            Color c = _drawCaptionColor;
            c.a *= alpha;
            _drawCaption.color = c;
        }
    }
}
