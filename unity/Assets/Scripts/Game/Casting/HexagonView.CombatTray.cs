using Dovus.Core.Input;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Game.Actors;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Casting
{
    public sealed partial class HexagonView : MonoBehaviour
    {
        void BuildCombatTrayBackdrop(Transform parent)
        {
            HudTheme th = _theme;
            var go = new GameObject("CombatRuneTray");
            go.transform.SetParent(parent, false);
            _tray = go.AddComponent<RectTransform>();
            var image = go.AddComponent<Image>();
            image.sprite = CreateRoundedRectSprite(_tuning.Input.CombatTrayCornerRadiusDp);
            image.type = Image.Type.Sliced;
            image.color = th.PanelSoftColor;
            image.raycastTarget = false;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -HexagonLayoutScreen.DpToPixels(th.PanelShadowDp));
            var outline = go.AddComponent<Outline>();
            outline.effectColor = th.PanelEdgeColor;
            float outlinePx = HexagonLayoutScreen.DpToPixels(th.PanelOutlineDp);
            outline.effectDistance = new Vector2(outlinePx, -outlinePx);

            var edgeGo = new GameObject("AccentEdge");
            edgeGo.transform.SetParent(go.transform, false);
            _trayEdge = edgeGo.AddComponent<RectTransform>();
            _trayEdge.anchorMin = new Vector2(0f, 1f);
            _trayEdge.anchorMax = new Vector2(1f, 1f);
            _trayEdge.pivot = new Vector2(0.5f, 1f);
            _trayEdge.sizeDelta = new Vector2(0f, HexagonLayoutScreen.DpToPixels(th.TrayAccentHeightDp));
            var edge = edgeGo.AddComponent<Image>();
            edge.color = th.SkillNeutralColor;
            edge.raycastTarget = false;

            _trayTitle = CreateLabel(_tray, "RÜN ZİNCİRİ  ·  ÇİZ / BIRAK");
            _trayTitle.fontStyle = FontStyle.Bold;
            _trayTitle.fontSize = Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(th.RuneTrayTitleDp));
            _trayTitle.alignment = TextAnchor.UpperLeft;
            _trayTitle.color = th.ControlCaptionColor;
            _trayTitle.rectTransform.offsetMin = new Vector2(HexagonLayoutScreen.DpToPixels(th.TrayTitleInsetDp), 0f);
            _trayTitle.rectTransform.offsetMax = new Vector2(0f, -HexagonLayoutScreen.DpToPixels(th.TrayTitleTopDp));

            _trayLinks = new RectTransform[Dovus.Core.Input.HexagonLayout.DotCount];
            for (int i = 0; i < _trayLinks.Length; i++)
            {
                var linkGo = new GameObject("HexLink" + (i + 1));
                linkGo.transform.SetParent(parent, false);
                _trayLinks[i] = linkGo.AddComponent<RectTransform>();
                var link = linkGo.AddComponent<Image>();
                Color c = th.PanelEdgeColor;
                c.a *= HexagonViewDefaults.TrayLinkAlphaMult;
                link.color = c;
                link.raycastTarget = false;
            }
        }

        void LayoutCombatTray(Vector2 center, float dotRadius)
        {
            if (_tray == null)
                return;
            float padding = HexagonLayoutScreen.DpToPixels(_tuning.Input.CombatTrayPaddingDp);
            float header = HexagonLayoutScreen.DpToPixels(_tuning.Input.CombatTrayHeaderHeightDp);
            float radius = HexagonLayoutScreen.RadiusPx(_tuning);
            float width = (radius + dotRadius + padding) * 2f;
            float height = width + header;
            Rect safe = HexagonLayoutScreen.SafeRectPx();
            float x = Mathf.Clamp(center.x, safe.xMin + width * 0.5f, safe.xMax - width * 0.5f);
            float y = Mathf.Clamp(center.y + header * 0.5f, safe.yMin + height * 0.5f, safe.yMax - height * 0.5f);
            _tray.anchorMin = Vector2.zero;
            _tray.anchorMax = Vector2.zero;
            _tray.pivot = new Vector2(0.5f, 0.5f);
            _tray.sizeDelta = new Vector2(width, height);
            _tray.anchoredPosition = new Vector2(x, y);
            _tray.SetAsFirstSibling();

            if (_trayLinks == null)
                return;
            for (int i = 0; i < _trayLinks.Length; i++)
            {
                Vector2 a = HexagonLayoutScreen.DotPx(i + 1, _tuning, Screen.width, Screen.height);
                Vector2 b = HexagonLayoutScreen.DotPx((i + 1) % _trayLinks.Length + 1, _tuning, Screen.width, Screen.height);
                LayoutLink(_trayLinks[i], a, b, HexagonLayoutScreen.DpToPixels(_tuning.Input.CombatTrayLinkWidthDp));
            }
        }
    }
}
