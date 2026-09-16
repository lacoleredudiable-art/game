using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Ulti (active_modes) göstergesi — üst-orta banner: mod adı + kalan süre (süresizse
    /// "AKTİF"). 16 Eylül: "skilleri attığımda etkileşim göremiyorum" ve "v5.2.1 hiç aktif
    /// olmadı" raporlarının aynısı ulti için tekrar yaşanmasın diye — ReactionReadout'un
    /// aksine bu SÜREKLİ görünür kalır (mod açıkken sönmez), böylece "gerçekten çalışıyor mu"
    /// sorusu bir daha sorulmaz.
    /// </summary>
    public sealed class ActiveModeHud : MonoBehaviour
    {
        RectTransform _root;
        Text _title;
        Text _timer;
        Image _bg;
        bool _visible;

        public void Configure(Transform canvasRoot)
        {
            var go = new GameObject("ActiveModeHud");
            go.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                go.layer = canvasRoot.gameObject.layer;

            _root = go.AddComponent<RectTransform>();
            _root.anchorMin = new Vector2(0.5f, 1f);
            _root.anchorMax = new Vector2(0.5f, 1f);
            _root.pivot = new Vector2(0.5f, 1f);
            _root.anchoredPosition = new Vector2(0f, -18f);
            _root.sizeDelta = new Vector2(360f, 56f);

            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(go.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            _bg = bgGo.AddComponent<Image>();
            _bg.color = new Color(0f, 0f, 0f, 0.45f);
            _bg.raycastTarget = false;

            _title = CreateText(go.transform, "Title", new Vector2(0f, 0.48f), new Vector2(1f, 1f), 26);
            _timer = CreateText(go.transform, "Timer", new Vector2(0f, 0f), new Vector2(1f, 0.48f), 18);

            SetVisible(false);
        }

        static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Mod aktive olduğunda bir kez çağrılır.</summary>
        public void ShowActivated(string title, string readAs, Color accent)
        {
            SetVisible(true);
            _title.text = title;
            _title.color = accent;
            _timer.text = readAs ?? string.Empty;
            _timer.color = new Color(1f, 1f, 1f, 0.85f);
            _bg.color = new Color(accent.r * 0.25f, accent.g * 0.25f, accent.b * 0.25f, 0.55f);
        }

        /// <summary>Her karede — kalan süre negatifse (süresiz mod) "AKTİF" yazar.</summary>
        public void UpdateRemaining(float remainingSec)
        {
            if (!_visible)
                return;
            _timer.text = remainingSec >= 0f
                ? $"{remainingSec:0.0} sn"
                : "AKTİF";
        }

        public void Hide() => SetVisible(false);

        void SetVisible(bool visible)
        {
            _visible = visible;
            _title.enabled = visible;
            _timer.enabled = visible;
            _bg.enabled = visible;
        }
    }
}
