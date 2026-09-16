using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>Ekrana sabit altıgen noktaları + merkez (Canvas Overlay).</summary>
    public sealed class PentagonView : MonoBehaviour
    {
        PrototypeTuning _tuning;
        RectTransform[] _dots;
        Image[] _dotImages;
        Sprite[] _dotIcons;
        RectTransform _center;
        RectTransform _dodge;
        Canvas _canvas;

        public Canvas Canvas => _canvas;
        public Transform CanvasRoot => _canvas != null ? _canvas.transform : null;

        public void Build(PrototypeTuning tuning, Camera overlayCam)
        {
            _tuning = tuning;

            var canvasGo = new GameObject("PentagonCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            // Overlay canvas her kameranın üstüne biner ve telegrafı ezer (§10).
            // Tek mekanizma: Overlay kamera + Screen Space Camera.
            _canvas.renderMode = overlayCam != null
                ? RenderMode.ScreenSpaceCamera
                : RenderMode.ScreenSpaceOverlay;
            _canvas.worldCamera = overlayCam;
            _canvas.planeDistance = 1.2f;
            _canvas.sortingOrder = 50;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var fallback = CreateCircleSprite();
            int n = Dovus.Core.Grammar.PentagonLayout.DotCount;
            _dots = new RectTransform[n + 1];
            _dotImages = new Image[n + 1];
            _dotIcons = new Sprite[n + 1];
            for (int dot = 1; dot <= n; dot++)
            {
                _dotIcons[dot] = TryCreateIconSprite(dot);
                Sprite icon = _dotIcons[dot] != null ? _dotIcons[dot] : fallback;
                _dots[dot] = CreateDisc($"Dot{dot}", icon, DotColor(dot), canvasGo.transform, out _dotImages[dot]);
                if (_dotIcons[dot] == null)
                {
                    var label = CreateLabel(_dots[dot], dot.ToString());
                    label.fontSize = 22;
                }
            }

            _center = CreateDisc("Center", fallback, _tuning.PentagonCenterColor, canvasGo.transform, out _);
            CreateLabel(_center, "·").fontSize = 32;

            _dodge = CreateDisc("DodgeButton", fallback, _tuning.DodgeButtonColor, canvasGo.transform, out _);
            CreateLabel(_dodge, "⇄").fontSize = 26;

            if (overlayCam != null)
                SetLayerRecursively(canvasGo, FirstLayer(overlayCam.cullingMask));

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
            int n = Dovus.Core.Grammar.PentagonLayout.DotCount;

            for (int dot = 1; dot <= n; dot++)
            {
                Vector2 px = PentagonLayoutScreen.DotPx(dot, _tuning, w, h);
                float mul = _dotIcons != null && _dotIcons[dot] != null
                    ? Mathf.Max(0.5f, _tuning.IconDisplayScale)
                    : 1f;
                Place(_dots[dot], px, dotR * 2f * mul, w, h);
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
            if (_dotIcons != null && _dotIcons[dot] != null)
            {
                float a = _tuning.IsDotOpen(dot) ? 1f : 0.28f;
                return new Color(1f, 1f, 1f, a);
            }

            Color c = _tuning.PentagonDotColor;
            if (_tuning.IsDotOpen(dot))
                return c;
            return new Color(c.r, c.g, c.b, c.a * 0.28f);
        }

        static Sprite TryCreateIconSprite(int dot)
        {
            // element-sistemi.json çekirdek: Ateş Su Hava Toprak Aydınlık Karanlık
            string name = dot switch
            {
                1 => "Concept/icon-fire",
                2 => "Concept/icon-water",
                3 => "Concept/icon-lightning", // Hava — ayrı ikon yok, geçici
                4 => "Concept/icon-earth",
                5 => "Concept/icon-light",
                6 => "Concept/icon-dark",
                _ => null
            };
            if (string.IsNullOrEmpty(name))
                return null;

            var tex = Resources.Load<Texture2D>(name);
            if (tex == null)
                return null;

            // Concept PNG'lerde siyah kare zemin var — yakındaki siyahı alfa yap.
            Texture2D punched = PunchNearBlackToAlpha(tex);
            return Sprite.Create(
                punched,
                new Rect(0f, 0f, punched.width, punched.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        /// <summary>Siyah/koyu kare zemini şeffafa çevirir (ikonlar yuvarlak diskte okunur kalsın).</summary>
        static Texture2D PunchNearBlackToAlpha(Texture2D src)
        {
            int w = src.width;
            int h = src.height;
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px;
            try
            {
                px = src.GetPixels32();
            }
            catch
            {
                // Read/Write kapalı asset — olduğu gibi kullan.
                return src;
            }

            const byte thresh = 28;
            for (int i = 0; i < px.Length; i++)
            {
                Color32 c = px[i];
                if (c.r <= thresh && c.g <= thresh && c.b <= thresh)
                    px[i] = new Color32(0, 0, 0, 0);
            }

            dst.SetPixels32(px);
            dst.Apply(false, true);
            return dst;
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
            image.preserveAspect = true;
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

        static int FirstLayer(int mask)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                    return i;
            }

            return 0;
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
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
