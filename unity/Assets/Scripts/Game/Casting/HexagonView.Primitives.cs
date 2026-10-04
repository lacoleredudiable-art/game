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
        static Sprite _roundedRectSprite;

        static Sprite CreateRoundedRectSprite(float cornerRadiusDp)
        {
            if (_roundedRectSprite != null)
                return _roundedRectSprite;
            const int size = 64;
            float radius = Mathf.Clamp(cornerRadiusDp, HexagonViewDefaults.RoundedRectMinCornerDp, size * HexagonViewDefaults.RoundedRectMaxCornerMult);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x, radius, size - 1 - radius);
                float cy = Mathf.Clamp(y, radius, size - 1 - radius);
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - dist + 0.5f)));
            }
            texture.Apply(false, true);
            int border = Mathf.RoundToInt(radius);
            _roundedRectSprite = Sprite.Create(
                texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            return _roundedRectSprite;
        }

        static void LayoutLink(RectTransform rect, Vector2 from, Vector2 to, float width)
        {
            Vector2 delta = to - from;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, Mathf.Max(1f, width));
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
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

        static RectTransform CreateLayeredDisc(
            string name,
            Sprite faceSprite,
            Sprite discSprite,
            Color color,
            Transform parent,
            out Image image,
            Color rimColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(go.transform, false);
            var shadowRt = shadowGo.AddComponent<RectTransform>();
            shadowRt.anchorMin = Vector2.zero;
            shadowRt.anchorMax = Vector2.one;
            shadowRt.offsetMin = new Vector2(3f, -5f);
            shadowRt.offsetMax = new Vector2(3f, -5f);
            var shadowImg = shadowGo.AddComponent<Image>();
            shadowImg.sprite = discSprite;
            shadowImg.color = new Color(0f, 0f, 0f, 0.45f);
            shadowImg.raycastTarget = false;
            shadowImg.preserveAspect = true;

            var rimGo = new GameObject("Rim");
            rimGo.transform.SetParent(go.transform, false);
            var rimRt = rimGo.AddComponent<RectTransform>();
            rimRt.anchorMin = Vector2.zero;
            rimRt.anchorMax = Vector2.one;
            rimRt.offsetMin = new Vector2(-3f, -3f);
            rimRt.offsetMax = new Vector2(3f, 3f);
            var rimImg = rimGo.AddComponent<Image>();
            rimImg.sprite = discSprite;
            rimImg.color = rimColor.a > HexagonViewDefaults.RimVisibleAlphaEpsilon
                ? rimColor
                : new Color(1f, 1f, 1f, 0.28f);
            rimImg.raycastTarget = false;
            rimImg.preserveAspect = true;

            var faceGo = new GameObject("Face");
            faceGo.transform.SetParent(go.transform, false);
            var faceRt = faceGo.AddComponent<RectTransform>();
            faceRt.anchorMin = Vector2.zero;
            faceRt.anchorMax = Vector2.one;
            faceRt.offsetMin = new Vector2(2f, 2f);
            faceRt.offsetMax = new Vector2(-2f, -2f);
            image = faceGo.AddComponent<Image>();
            image.sprite = faceSprite;
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
            t.font = HudTheme.LegacyFont;
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
            for (int i = 0; i < HexagonViewDefaults.LayerMaskScanMax; i++)
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
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), HexagonViewDefaults.CircleSpritePixelsPerUnit);
        }
    }
}
