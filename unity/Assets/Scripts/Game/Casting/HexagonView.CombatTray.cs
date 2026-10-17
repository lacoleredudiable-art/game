using Dovus.Core.Input;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Game.Actors;
using Dovus.Game.Config;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Casting
{
    public sealed partial class HexagonView
    {
        void BuildCombatTrayBackdrop(Transform parent)
        {
            HudTheme th = _theme;
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
