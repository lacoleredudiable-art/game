using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>element_system.selection: hold radial, 6 konum, slow-mo yok, hasarda iptal.</summary>
    public sealed class ElementRadialMenu : MonoBehaviour
    {
        static ElementRadialMenu _instance;
        sealed class HoldSurface : MonoBehaviour,
            IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerExitHandler
        {
            public Action<Vector2> Down;
            public Action<Vector2> Drag;
            public Action<Vector2> Up;
            public void OnPointerDown(PointerEventData e) => Down?.Invoke(e.position);
            public void OnDrag(PointerEventData e) => Drag?.Invoke(e.position);
            public void OnPointerUp(PointerEventData e) => Up?.Invoke(e.position);
            public void OnPointerExit(PointerEventData e) => Drag?.Invoke(e.position);
        }

        ManifestationDirector _director;
        IReadOnlyList<ElementPaintNode> _elements;
        ActorStatus _playerStatus;
        RectTransform _root;
        RectTransform _chip;
        CanvasGroup _group;
        Text _chipLabel;
        readonly List<Image> _wedges = new();
        readonly List<RectTransform> _wedgeRects = new();
        readonly List<Text> _labels = new();
        PrototypeTuning _tuning;
        Rect _appliedSafe;
        float _transitionSec = 0.3f;
        float _openedAt;
        float _closedAt;
        int _hoverIndex = -1;
        bool _holding;
        bool _closing;
        bool _keyboardHold;

        public bool IsOpen => _holding || (_root != null && _root.gameObject.activeSelf);
        public static bool AnyOpen => _instance != null && _instance.IsOpen;
        public static bool HitHoldChip(Vector2 screenPosition) =>
            _instance != null && _instance._chip != null
            && RectTransformUtility.RectangleContainsScreenPoint(_instance._chip, screenPosition);

        public void Configure(
            ManifestationDirector director,
            SkillMotor skills,
            ActorStatus playerStatus,
            PrototypeTuning tuning,
            Transform canvasRoot,
            int transitionMs)
        {
            _director = director;
            _instance = this;
            _elements = skills?.ElementPaints ?? Array.Empty<ElementPaintNode>();
            _playerStatus = playerStatus;
            _transitionSec = Mathf.Max(0.01f, transitionMs / 1000f);
            Build(canvasRoot, tuning ?? new PrototypeTuning());
            if (_playerStatus != null)
                _playerStatus.DamageTaken += OnDamageTaken;
            if (_director != null)
                _director.ElementPaintChanged += OnElementChanged;
            RefreshChip();
        }

        void Build(Transform canvasRoot, PrototypeTuning tuning)
        {
            _tuning = tuning;
            HudTheme theme = HudTheme.Current;
            var layer = new GameObject("ElementRadialMenu");
            layer.transform.SetParent(canvasRoot, false);
            _root = layer.AddComponent<RectTransform>();
            Stretch(_root);
            _group = layer.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _root.gameObject.SetActive(false);

            var chipGo = new GameObject("ElementHoldChip");
            chipGo.transform.SetParent(canvasRoot, false);
            _chip = chipGo.AddComponent<RectTransform>();
            _chip.anchorMin = _chip.anchorMax = Vector2.zero;
            _chip.pivot = new Vector2(0.5f, 0.5f);
            _chip.sizeDelta = new Vector2(
                HexagonLayoutScreen.DpToPixels(tuning.ElementMenuChipWidthDp),
                HexagonLayoutScreen.DpToPixels(tuning.ElementMenuChipHeightDp));
            var chipImage = chipGo.AddComponent<Image>();
            chipImage.color = theme.PanelColor;
            var shadow = chipGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(
                0f, -HexagonLayoutScreen.DpToPixels(theme.PanelShadowDp));
            var outline = chipGo.AddComponent<Outline>();
            outline.effectColor = theme.PanelEdgeColor;
            float outlinePx = HexagonLayoutScreen.DpToPixels(theme.PanelOutlineDp);
            outline.effectDistance = new Vector2(outlinePx, -outlinePx);
            HoldSurface hold = chipGo.AddComponent<HoldSurface>();
            hold.Down = BeginHold;
            hold.Drag = UpdateHover;
            hold.Up = EndHold;
            _chipLabel = CreateText(chipGo.transform, 15);

            float radius = HexagonLayoutScreen.DpToPixels(tuning.ElementMenuRadiusDp);
            float itemWidth = HexagonLayoutScreen.DpToPixels(tuning.ElementMenuItemWidthDp);
            float itemHeight = HexagonLayoutScreen.DpToPixels(tuning.ElementMenuItemHeightDp);
            for (int i = 0; i < _elements.Count; i++)
            {
                float rad = (90f - i * 60f) * Mathf.Deg2Rad;
                var go = new GameObject("Element_" + _elements[i].Id);
                go.transform.SetParent(_root, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(itemWidth, itemHeight);
                rect.anchoredPosition = _chip.anchoredPosition
                    + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
                var image = go.AddComponent<Image>();
                image.color = ParseColor(_elements[i].ColorHex, 0.82f);
                image.raycastTarget = false;
                var wedgeOutline = go.AddComponent<Outline>();
                wedgeOutline.effectColor = new Color(1f, 1f, 1f, 0.26f);
                wedgeOutline.effectDistance = new Vector2(outlinePx, -outlinePx);
                Text label = CreateText(go.transform, 13);
                label.text = _elements[i].Name + "\n" + _elements[i].NamePrefix;
                _wedges.Add(image);
                _wedgeRects.Add(rect);
                _labels.Add(label);
            }
            ApplySafeLayout();
        }

        void Update()
        {
            ApplySafeLayout();
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.eKey.wasPressedThisFrame)
                {
                    _keyboardHold = true;
                    BeginHold(RectTransformUtility.WorldToScreenPoint(null, _chip.position));
                }
                if (_keyboardHold)
                {
                    for (int i = 0; i < Mathf.Min(6, _elements.Count); i++)
                    {
                        KeyControl key = i switch
                        {
                            0 => keyboard.digit1Key,
                            1 => keyboard.digit2Key,
                            2 => keyboard.digit3Key,
                            3 => keyboard.digit4Key,
                            4 => keyboard.digit5Key,
                            _ => keyboard.digit6Key
                        };
                        if (key.wasPressedThisFrame)
                            _hoverIndex = i;
                    }
                    if (keyboard.eKey.wasReleasedThisFrame)
                    {
                        _keyboardHold = false;
                        EndHold(Vector2.zero);
                    }
                }
            }

            if (_root == null || !_root.gameObject.activeSelf)
                return;
            float t = _closing
                ? 1f - Mathf.Clamp01((Time.unscaledTime - _closedAt) / _transitionSec)
                : Mathf.Clamp01((Time.unscaledTime - _openedAt) / _transitionSec);
            _group.alpha = t;
            _root.localScale = Vector3.one * Mathf.Lerp(0.72f, 1f, t);
            RefreshHighlight();
            if (_closing && t <= 0f)
                _root.gameObject.SetActive(false);
        }

        void BeginHold(Vector2 screenPosition)
        {
            if (_elements == null || _elements.Count == 0 || BuildSelectScreen.IsOpen)
                return;
            _holding = true;
            _closing = false;
            _hoverIndex = SelectedIndex();
            _openedAt = Time.unscaledTime;
            _root.gameObject.SetActive(true);
            _group.alpha = 0f;
            UpdateHover(screenPosition);
        }

        void UpdateHover(Vector2 screenPosition)
        {
            if (!_holding || _chip == null)
                return;
            Vector2 center = RectTransformUtility.WorldToScreenPoint(null, _chip.position);
            Vector2 delta = screenPosition - center;
            if (delta.sqrMagnitude < 24f * 24f)
                return;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            _hoverIndex = Mathf.RoundToInt((90f - angle) / 60f);
            _hoverIndex %= _elements.Count;
            if (_hoverIndex < 0)
                _hoverIndex += _elements.Count;
        }

        void EndHold(Vector2 screenPosition)
        {
            if (!_holding)
                return;
            if (screenPosition != Vector2.zero)
                UpdateHover(screenPosition);
            bool completed = Time.unscaledTime - _openedAt >= _transitionSec;
            _holding = false;
            if (completed && _hoverIndex >= 0 && _hoverIndex < _elements.Count)
                _director?.TrySetElementPaint(_elements[_hoverIndex].Id);
            StartClosing();
        }

        public void Interrupt()
        {
            _holding = false;
            _keyboardHold = false;
            _hoverIndex = -1;
            StartClosing();
        }

        void OnDamageTaken(float amount)
        {
            if (amount > 0f && IsOpen)
                Interrupt();
        }

        void StartClosing()
        {
            if (_root == null || !_root.gameObject.activeSelf)
                return;
            _closing = true;
            _closedAt = Time.unscaledTime;
        }

        void OnElementChanged(ElementPaintNode _) => RefreshChip();

        void RefreshChip()
        {
            if (_chipLabel == null)
                return;
            ElementPaintNode? paint = _director?.SelectedElementPaint;
            _chipLabel.text = paint.HasValue ? "ELEMENT · " + paint.Value.Name : "ELEMENT";
            Image image = _chip.GetComponent<Image>();
            if (image != null && paint.HasValue)
            {
                Color element = ParseColor(paint.Value.ColorHex, 0.96f);
                image.color = Color.Lerp(HudTheme.Current.PanelColor, element, 0.38f);
            }
            if (paint.HasValue)
                _chipLabel.text = "ELEMENT  //  " + paint.Value.Name.ToUpperInvariant() + "  ·  BASILI TUT";
        }

        int SelectedIndex()
        {
            ElementPaintNode? selected = _director?.SelectedElementPaint;
            if (!selected.HasValue)
                return 0;
            for (int i = 0; i < _elements.Count; i++)
                if (_elements[i].Id == selected.Value.Id)
                    return i;
            return 0;
        }

        void RefreshHighlight()
        {
            for (int i = 0; i < _wedges.Count; i++)
            {
                Color c = ParseColor(_elements[i].ColorHex, i == _hoverIndex ? 1f : 0.55f);
                _wedges[i].color = c;
                _labels[i].color = i == _hoverIndex ? Color.white : new Color(1f, 1f, 1f, 0.72f);
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
            if (_playerStatus != null)
                _playerStatus.DamageTaken -= OnDamageTaken;
            if (_director != null)
                _director.ElementPaintChanged -= OnElementChanged;
        }

        static Text CreateText(Transform parent, int fontSize)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            Stretch(rect);
            var text = go.AddComponent<Text>();
            text.font = HudTheme.LegacyFont
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        void ApplySafeLayout()
        {
            if (_chip == null || _tuning == null)
                return;
            Rect safe = HexagonLayoutScreen.SafeRectPx();
            if (safe == _appliedSafe)
                return;
            _appliedSafe = safe;
            Vector2 desired = new(
                safe.xMin + safe.width * _tuning.ElementMenuAnchorXNorm,
                safe.yMin + safe.height * _tuning.ElementMenuAnchorYNorm);
            float radius = HexagonLayoutScreen.DpToPixels(_tuning.ElementMenuRadiusDp);
            float xExtent = radius + HexagonLayoutScreen.DpToPixels(_tuning.ElementMenuItemWidthDp) * 0.5f;
            float yExtent = radius + HexagonLayoutScreen.DpToPixels(_tuning.ElementMenuItemHeightDp) * 0.5f;
            float x = safe.width >= xExtent * 2f
                ? Mathf.Clamp(desired.x, safe.xMin + xExtent, safe.xMax - xExtent)
                : safe.center.x;
            float y = safe.height >= yExtent * 2f
                ? Mathf.Clamp(desired.y, safe.yMin + yExtent, safe.yMax - yExtent)
                : safe.center.y;
            Vector2 chipPos = new(x, y);
            _chip.anchoredPosition = chipPos;
            for (int i = 0; i < _wedgeRects.Count; i++)
            {
                float rad = (90f - i * 60f) * Mathf.Deg2Rad;
                _wedgeRects[i].anchoredPosition = chipPos
                    + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
            }
        }

        static Color ParseColor(string hex, float alpha)
        {
            Color color = new(0.25f, 0.75f, 0.9f, alpha);
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color parsed))
                color = parsed;
            color.a = alpha;
            return color;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
