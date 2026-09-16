using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Sol yarıdaki dinamik sanal çubuğun görseli. <see cref="MoveInput"/> fonksiyonel olarak
    /// zaten çalışıyordu (dokun-sürükle hareket ediyordu) ama hiçbir görsel yoktu — oyuncu
    /// nereye basacağını göremiyordu (16 Eylül bug raporu). Bu sınıf yalnızca çizer;
    /// hareket mantığına dokunmaz.
    /// </summary>
    public sealed class JoystickView : MonoBehaviour
    {
        MoveInput _input;
        PrototypeTuning _tuning;
        RectTransform _base;
        RectTransform _knob;
        bool _visible;

        public void Build(MoveInput input, PrototypeTuning tuning, Camera overlayCam)
        {
            _input = input;
            _tuning = tuning;

            var canvasGo = new GameObject("JoystickCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = overlayCam != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = overlayCam;
            canvas.planeDistance = 1.2f;
            // Pentagon canvas'ı (50) altında dursun ama diğer HUD'ların üstünde olsun.
            canvas.sortingOrder = 45;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            Sprite disc = CreateCircleSprite();
            _base = CreateDisc("StickBase", disc, new Color(1f, 1f, 1f, 0.16f), canvasGo.transform, out _);
            _knob = CreateDisc("StickKnob", disc, new Color(1f, 1f, 1f, 0.42f), canvasGo.transform, out _);

            if (overlayCam != null)
                SetLayerRecursively(canvasGo, FirstLayer(overlayCam.cullingMask));

            SetVisible(false);
        }

        void LateUpdate()
        {
            if (_input == null || _tuning == null || _base == null)
                return;

            SetVisible(_input.IsActive);
            if (!_input.IsActive)
                return;

            float baseR = PentagonLayoutScreen.DpToPixels(_tuning.JoystickMaxRadiusDp);
            Place(_base, _input.OriginPx, baseR * 2f);
            Place(_knob, _input.OriginPx + _input.KnobOffsetPx, baseR * 0.9f);
        }

        void SetVisible(bool visible)
        {
            if (visible == _visible)
                return;
            _visible = visible;
            if (_base != null) _base.gameObject.SetActive(visible);
            if (_knob != null) _knob.gameObject.SetActive(visible);
        }

        static void Place(RectTransform rt, Vector2 screenPx, float diameterPx)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(diameterPx, diameterPx);
            rt.anchoredPosition = screenPx;
        }

        static RectTransform CreateDisc(string name, Sprite sprite, Color color, Transform parent, out Image image)
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
