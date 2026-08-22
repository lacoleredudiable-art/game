using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>Ekrana sabit beşgen noktaları + merkez (Canvas Overlay).</summary>
    public sealed class PentagonView : MonoBehaviour
    {
        PrototypeTuning _tuning;
        RectTransform[] _dots;
        Image[] _dotImages;
        RectTransform _center;
        RectTransform _dodge;
        Canvas _canvas;

        public Canvas Canvas => _canvas;
        public Transform CanvasRoot => _canvas != null ? _canvas.transform : null;

        public void Build(PrototypeTuning tuning)
        {
            _tuning = tuning;

            var canvasGo = new GameObject("PentagonCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var sprite = CreateCircleSprite();
            _dots = new RectTransform[6];
            _dotImages = new Image[6];
            for (int dot = 1; dot <= 5; dot++)
            {
                _dots[dot] = CreateDisc($"Dot{dot}", sprite, DotColor(dot), canvasGo.transform, out _dotImages[dot]);
                var label = CreateLabel(_dots[dot], dot.ToString());
                label.fontSize = 22;
            }

            _center = CreateDisc("Center", sprite, _tuning.PentagonCenterColor, canvasGo.transform, out _);
            CreateLabel(_center, "·").fontSize = 32;

            // Dodge beşgenin dışında, ekrana sabit (§2). §10: kırmızı-turuncu olamaz.
            _dodge = CreateDisc("DodgeButton", sprite, _tuning.DodgeButtonColor, canvasGo.transform, out _);

            Layout();
        }

        void LateUpdate()
        {
            if (_tuning != null)
                Layout();
        }

        void Layout()
        {
            int w = Screen.width;
            int h = Screen.height;
            float dotR = PentagonLayoutScreen.DotHitRadiusPx(_tuning);
            float centerR = PentagonLayoutScreen.CenterHitRadiusPx(_tuning);

            for (int dot = 1; dot <= 5; dot++)
            {
                Vector2 px = PentagonLayoutScreen.DotPx(dot, _tuning, w, h);
                Place(_dots[dot], px, dotR * 2f, w, h);
                if (_dotImages[dot] != null)
                    _dotImages[dot].color = DotColor(dot);
            }

            Vector2 c = PentagonLayoutScreen.CenterPx(_tuning, w, h);
            Place(_center, c, centerR * 2f, w, h);

            Vector2 d = PentagonLayoutScreen.DodgeButtonPx(_tuning, w, h);
            Place(_dodge, d, PentagonLayoutScreen.DodgeButtonRadiusPx(_tuning) * 2f, w, h);
        }

        Color DotColor(int dot)
        {
            Color c = _tuning.PentagonDotColor;
            if (_tuning.IsDotOpen(dot))
                return c;
            // Kapalı rün: soluk — oyuncu neden tepki almadığını görsün (§4).
            return new Color(c.r, c.g, c.b, c.a * 0.28f);
        }

        static void Place(RectTransform rt, Vector2 screenPx, float diameterPx, int screenW, int screenH)
        {
            // Overlay canvas: anchor bottom-left in pixel space via anchoredPosition with stretch off
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(diameterPx, diameterPx);
            rt.anchoredPosition = screenPx;
        }

        static RectTransform CreateDisc(
            string name,
            Sprite sprite,
            Color color,
            Transform parent,
            out Image image)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return rt;
        }

        static Text CreateLabel(RectTransform parent, string text)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null)
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.1f, 0.12f, 0.16f, 0.9f);
            t.raycastTarget = false;
            return t;
        }

        static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float r = (size - 1) * 0.5f;
            Vector2 c = new Vector2(r, r);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01(r - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }
    }
}
