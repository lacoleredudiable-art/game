using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// F1 + küçük köşe düğmesi: debug panellerini göster/gizle. Oyun HUD'una dokunmaz.
    /// </summary>
    public sealed class DebugPanelsController : MonoBehaviour
    {
#if UNITY_EDITOR || DOVUS_DEBUG
        GameObject _toggleGo;

        void Awake()
        {
            DebugPanelsChrome.SetVisible(false);
            BuildCornerToggle();
        }

        void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame)
                Toggle();
        }

        void Toggle()
        {
            DebugPanelsChrome.SetVisible(!DebugPanelsChrome.Visible);
            RefreshToggleLabel();
        }

        void BuildCornerToggle()
        {
            var go = new GameObject("DebugPanelsToggle");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            go.AddComponent<GraphicRaycaster>();

            var rect = new GameObject("Btn").AddComponent<RectTransform>();
            rect.SetParent(go.transform, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(8f, -8f);
            rect.sizeDelta = new Vector2(52f, 22f);

            var img = rect.gameObject.AddComponent<Image>();
            img.color = new Color(0.2f, 0.24f, 0.28f, 0.45f);
            var btn = rect.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(Toggle);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(rect, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (label.font == null)
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 11;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.85f, 0.9f, 0.95f, 0.85f);
            label.raycastTarget = false;
            label.text = "DBG";

            _toggleGo = go;
            RefreshToggleLabel();
        }

        void RefreshToggleLabel()
        {
            if (_toggleGo == null)
                return;
            var text = _toggleGo.GetComponentInChildren<Text>();
            if (text != null)
                text.text = DebugPanelsChrome.Visible ? "DBG−" : "DBG+";
        }
#endif
    }
}
