using System.Collections.Generic;
using System.Text;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Aktif pasiflerin minik listesi — sol-üst, ActiveModeHud / ReactionReadout'tan ayrı.
    /// Pasif yokken görünmez; birden fazla aynı anda satır satır.
    /// </summary>
    public sealed class PassiveHud : MonoBehaviour
    {
        RectTransform _root;
        Text _label;
        Image _bg;
        readonly StringBuilder _sb = new();

        public void Configure(Transform canvasRoot)
        {
            var go = new GameObject("PassiveHud");
            go.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                go.layer = canvasRoot.gameObject.layer;

            _root = go.AddComponent<RectTransform>();
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0f, 1f);
            _root.anchoredPosition = new Vector2(12f, -72f);
            _root.sizeDelta = new Vector2(220f, 72f);

            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(go.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            _bg = bgGo.AddComponent<Image>();
            _bg.color = new Color(0.05f, 0.12f, 0.18f, 0.55f); // camgöbeği koyu — oyuncu efekti
            _bg.raycastTarget = false;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);

            _label = textGo.AddComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_label.font == null)
                _label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _label.fontStyle = FontStyle.Bold;
            _label.fontSize = 14;
            _label.alignment = TextAnchor.UpperLeft;
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.color = new Color(0.55f, 0.9f, 1f, 0.95f); // camgöbeği
            _label.raycastTarget = false;

            SetVisible(false);
        }

        public void BindBelowPlayer(VitalsHud vitals)
        {
            if (_root == null || vitals == null)
                return;
            float left = PentagonLayoutScreen.SafeLeftInsetPx()
                + PentagonLayoutScreen.DpToPixels(12f);
            float y = vitals.PlayerStackBottomCanvasY
                - PentagonLayoutScreen.DpToPixels(52f);
            _root.anchoredPosition = new Vector2(left, y);
        }

        /// <summary>Aktif pasif listesini yeniler; boşsa gizler.</summary>
        public void Sync(IReadOnlyList<ActivePassive> active, double worldMs)
        {
            if (active == null || active.Count == 0)
            {
                SetVisible(false);
                return;
            }

            _sb.Clear();
            for (int i = 0; i < active.Count; i++)
            {
                ActivePassive a = active[i];
                PassiveNode n = a.Node;
                float remain = Mathf.Max(0f, n.DurationSec - (float)((worldMs - a.SinceMs) / 1000.0));
                if (i > 0) _sb.Append('\n');
                _sb.Append('◆').Append(' ').Append(DisplayName(n.Id));
                _sb.Append(' ').Append(remain.ToString("0")).Append('s');
            }

            _label.text = _sb.ToString();
            float h = Mathf.Max(28f, 8f + active.Count * 18f);
            _root.sizeDelta = new Vector2(220f, h);
            SetVisible(true);
        }

        static string DisplayName(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "?";
            return id.Replace('_', ' ');
        }

        void SetVisible(bool visible)
        {
            if (_label != null) _label.enabled = visible;
            if (_bg != null) _bg.enabled = visible;
        }
    }
}
