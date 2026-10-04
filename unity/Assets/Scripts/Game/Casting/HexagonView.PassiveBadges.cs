using Dovus.Game.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Casting
{
    /// <summary>
    /// Pasif yuvaya konan rünün düğmesinin üstünde küçük "PASİF" rozeti; pasif etkinken kalan
    /// süreyi taşır. Ayrı pasif paneli yerine (telefonda yer kaplıyordu) bilgi rünün üstünde.
    /// </summary>
    public sealed partial class HexagonView
    {
        RectTransform[] _passiveBadges;
        Image[] _passiveBadgeBg;
        Text[] _passiveBadgeText;

        /// <param name="remainSec">≥ 0 ise pasif etkin ve bu kadar sn kaldı; negatifse bekliyor.</param>
        public void SetPassiveBadge(int dot, bool passive, float remainSec)
        {
            if (_dots == null || dot <= 0 || dot >= _dots.Length || _dots[dot] == null)
                return;
            if (_passiveBadges == null)
            {
                _passiveBadges = new RectTransform[_dots.Length];
                _passiveBadgeBg = new Image[_dots.Length];
                _passiveBadgeText = new Text[_dots.Length];
            }
            if (_passiveBadges[dot] == null)
            {
                if (!passive)
                    return;
                CreatePassiveBadge(dot);
            }

            _passiveBadges[dot].gameObject.SetActive(passive);
            if (!passive)
                return;
            HudTheme th = _theme;
            bool active = remainSec >= 0f;
            _passiveBadgeBg[dot].color = active ? th.PassiveBadgeActiveColor : th.PassiveBadgeColor;
            _passiveBadgeText[dot].text = active
                ? "PASİF " + Mathf.Max(0, Mathf.CeilToInt(remainSec)) + "s"
                : "PASİF";
        }

        void CreatePassiveBadge(int dot)
        {
            HudTheme th = _theme;
            var go = new GameObject("PassiveBadge");
            go.transform.SetParent(_dots[dot], false);
            go.layer = _dots[dot].gameObject.layer;
            var rt = go.AddComponent<RectTransform>();
            float w = Mathf.Clamp01(th.PassiveBadgeWidthFrac);
            float h = Mathf.Max(0.05f, th.PassiveBadgeHeightFrac);
            // Diskin üst kenarına oturur, yarısı dışarı taşar; disk büyüyüp küçüldükçe onunla ölçeklenir.
            rt.anchorMin = new Vector2(0.5f - w * 0.5f, 1f - h * 0.55f);
            rt.anchorMax = new Vector2(0.5f + w * 0.5f, 1f + h * 0.45f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bg = go.AddComponent<Image>();
            bg.sprite = CreateRoundedRectSprite(_tuning.Input.CombatTrayCornerRadiusDp);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = th.PanelEdgeColor;
            outline.effectDistance = new Vector2(1f, -1f);

            Text label = CreateLabel(rt, "PASİF");
            label.fontStyle = FontStyle.Bold;
            label.color = th.PrimaryTextColor;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 6;
            label.resizeTextMaxSize = 64;

            _passiveBadges[dot] = rt;
            _passiveBadgeBg[dot] = bg;
            _passiveBadgeText[dot] = label;
        }
    }
}
