using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public delegate void UnityAction<T0>(T0 arg0);

    public class UnityEvent
    {
        readonly List<UnityAction> _calls = new();
        public void AddListener(UnityAction call) => _calls.Add(call);
        public void RemoveListener(UnityAction call) => _calls.Remove(call);
        public void RemoveAllListeners() => _calls.Clear();
        public void Invoke()
        {
            foreach (UnityAction c in _calls.ToArray()) c();
        }
    }

    public class UnityEvent<T0>
    {
        readonly List<UnityAction<T0>> _calls = new();
        public void AddListener(UnityAction<T0> call) => _calls.Add(call);
        public void RemoveListener(UnityAction<T0> call) => _calls.Remove(call);
        public void RemoveAllListeners() => _calls.Clear();
        public void Invoke(T0 arg)
        {
            foreach (UnityAction<T0> c in _calls.ToArray()) c(arg);
        }
    }
}

namespace UnityEngine.UI
{
    using UIBehaviour = UnityEngine.EventSystems.UIBehaviour;

    public class Graphic : UIBehaviour
    {
        public RectTransform rectTransform => transform as RectTransform;
        public virtual Color color { get; set; } = Color.white;
        public bool raycastTarget { get; set; } = true;
        public virtual Material material { get; set; }
        public Canvas canvas => GetComponentInParent<Canvas>();
        public CanvasRenderer canvasRenderer => GetComponent<CanvasRenderer>();
        public void SetAllDirty() { }
        public void SetVerticesDirty() { }
        public void SetMaterialDirty() { }
        public void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale) { }
        public void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha) { }
    }

    public class MaskableGraphic : Graphic
    {
        public bool maskable { get; set; } = true;
    }

    public class Image : MaskableGraphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
        public enum OriginHorizontal { Left, Right }
        public enum OriginVertical { Bottom, Top }
        public enum Origin90 { BottomLeft, TopLeft, TopRight, BottomRight }
        public enum Origin180 { Bottom, Left, Top, Right }
        public enum Origin360 { Bottom, Right, Top, Left }

        public Sprite sprite { get; set; }
        public Sprite overrideSprite { get; set; }
        public Type type { get; set; }
        public float fillAmount { get; set; } = 1f;
        public bool preserveAspect { get; set; }
        public bool fillClockwise { get; set; } = true;
        public bool fillCenter { get; set; } = true;
        public FillMethod fillMethod { get; set; }
        public int fillOrigin { get; set; }
        public float pixelsPerUnitMultiplier { get; set; } = 1f;
        public bool useSpriteMesh { get; set; }
        public void SetNativeSize() { }
    }

    public class RawImage : MaskableGraphic
    {
        public Texture texture { get; set; }
        public Rect uvRect { get; set; } = new(0, 0, 1, 1);
    }

    public class Text : MaskableGraphic
    {
        public string text { get; set; } = "";
        public Font font { get; set; }
        public int fontSize { get; set; } = 14;
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public bool resizeTextForBestFit { get; set; }
        public int resizeTextMinSize { get; set; }
        public int resizeTextMaxSize { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public float lineSpacing { get; set; } = 1f;
        public bool supportRichText { get; set; } = true;
        public bool alignByGeometry { get; set; }
        public float preferredWidth => (text?.Length ?? 0) * fontSize * 0.5f;
        public float preferredHeight => fontSize * 1.2f;
    }

    public class Shadow : UIBehaviour
    {
        public Vector2 effectDistance { get; set; } = new(1f, -1f);
        public Color effectColor { get; set; } = new(0f, 0f, 0f, 0.5f);
        public bool useGraphicAlpha { get; set; } = true;
    }

    public class Outline : Shadow { }

    public class Selectable : UIBehaviour
    {
        public enum Transition { None, ColorTint, SpriteSwap, Animation }
        public bool interactable { get; set; } = true;
        public Graphic targetGraphic { get; set; }
        public Transition transition { get; set; } = Transition.ColorTint;
        public ColorBlock colors { get; set; } = ColorBlock.defaultColorBlock;
        public Navigation navigation { get; set; }
        public Image image { get => targetGraphic as Image; set => targetGraphic = value; }
        public void Select() { }
    }

    public struct Navigation
    {
        public enum Mode { None = 0, Horizontal = 1, Vertical = 2, Automatic = 3, Explicit = 4 }
        public Mode mode { get; set; }
        public static Navigation defaultNavigation => new() { mode = Mode.Automatic };
    }

    public struct ColorBlock
    {
        public Color normalColor { get; set; }
        public Color highlightedColor { get; set; }
        public Color pressedColor { get; set; }
        public Color selectedColor { get; set; }
        public Color disabledColor { get; set; }
        public float colorMultiplier { get; set; }
        public float fadeDuration { get; set; }

        public static ColorBlock defaultColorBlock => new()
        {
            normalColor = Color.white, highlightedColor = Color.white, pressedColor = Color.white,
            selectedColor = Color.white, disabledColor = Color.white, colorMultiplier = 1f, fadeDuration = 0.1f,
        };
    }

    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEvent { }
        public ButtonClickedEvent onClick { get; set; } = new();
    }

    public class Toggle : Selectable
    {
        public class ToggleEvent : UnityEvent<bool> { }
        bool _isOn;
        public ToggleEvent onValueChanged { get; set; } = new();
        public Graphic graphic { get; set; }
        public ToggleGroup group { get; set; }

        public bool isOn
        {
            get => _isOn;
            set
            {
                if (_isOn == value) return;
                _isOn = value;
                onValueChanged.Invoke(value);
            }
        }

        public void SetIsOnWithoutNotify(bool value) => _isOn = value;
    }

    public class ToggleGroup : UIBehaviour
    {
        public bool allowSwitchOff { get; set; }
    }

    public class Slider : Selectable
    {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
        public class SliderEvent : UnityEvent<float> { }

        float _value;
        public float minValue { get; set; }
        public float maxValue { get; set; } = 1f;
        public bool wholeNumbers { get; set; }
        public RectTransform handleRect { get; set; }
        public RectTransform fillRect { get; set; }
        public Direction direction { get; set; }
        public SliderEvent onValueChanged { get; set; } = new();
        public float normalizedValue
        {
            get => Mathf.Approximately(minValue, maxValue) ? 0f : Mathf.InverseLerp(minValue, maxValue, value);
            set => this.value = Mathf.Lerp(minValue, maxValue, value);
        }

        public float value
        {
            get => _value;
            set
            {
                float v = Mathf.Clamp(value, minValue, maxValue);
                if (wholeNumbers) v = Mathf.Round(v);
                if (_value == v) return;
                _value = v;
                onValueChanged.Invoke(v);
            }
        }

        public void SetValueWithoutNotify(float v) => _value = Mathf.Clamp(v, minValue, maxValue);
    }

    public class InputField : Selectable
    {
        public class SubmitEvent : UnityEvent<string> { }
        public string text { get; set; } = "";
        public SubmitEvent onEndEdit { get; set; } = new();
        public SubmitEvent onValueChanged { get; set; } = new();
    }

    public class Dropdown : Selectable
    {
        public class DropdownEvent : UnityEvent<int> { }
        public int value { get; set; }
        public DropdownEvent onValueChanged { get; set; } = new();
    }

    public class CanvasScaler : UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
        public float scaleFactor { get; set; } = 1f;
        public ScaleMode uiScaleMode { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public Vector2 referenceResolution { get; set; } = new(800, 600);
        public float matchWidthOrHeight { get; set; }
        public float referencePixelsPerUnit { get; set; } = 100f;
        public float dynamicPixelsPerUnit { get; set; } = 1f;
    }

    public class GraphicRaycaster : UIBehaviour
    {
        public bool ignoreReversedGraphics { get; set; } = true;
    }

    public class Mask : UIBehaviour
    {
        public bool showMaskGraphic { get; set; } = true;
    }

    public class RectMask2D : UIBehaviour { }

    public abstract class LayoutGroup : UIBehaviour
    {
        public RectOffset padding { get; set; } = new();
        public TextAnchor childAlignment { get; set; }
    }

    public class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childControlHeight { get; set; } = true;
        public bool childControlWidth { get; set; } = true;
        public bool childForceExpandHeight { get; set; } = true;
        public bool childForceExpandWidth { get; set; } = true;
        public bool childScaleHeight { get; set; }
        public bool childScaleWidth { get; set; }
        public bool reverseArrangement { get; set; }
    }

    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }

    public class GridLayoutGroup : LayoutGroup
    {
        public enum Constraint { Flexible, FixedColumnCount, FixedRowCount }
        public Vector2 cellSize { get; set; } = new(100, 100);
        public Vector2 spacing { get; set; }
        public Constraint constraint { get; set; }
        public int constraintCount { get; set; } = 2;
    }

    public class LayoutElement : UIBehaviour
    {
        public float preferredWidth { get; set; } = -1f;
        public float preferredHeight { get; set; } = -1f;
        public float minWidth { get; set; } = -1f;
        public float minHeight { get; set; } = -1f;
        public float flexibleWidth { get; set; } = -1f;
        public float flexibleHeight { get; set; } = -1f;
        public bool ignoreLayout { get; set; }
    }

    public class ContentSizeFitter : UIBehaviour
    {
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
        public FitMode verticalFit { get; set; }
        public FitMode horizontalFit { get; set; }
    }

    public class AspectRatioFitter : UIBehaviour
    {
        public enum AspectMode { None, WidthControlsHeight, HeightControlsWidth, FitInParent, EnvelopeParent }
        public float aspectRatio { get; set; } = 1f;
        public AspectMode aspectMode { get; set; }
    }

    public class ScrollRect : UIBehaviour
    {
        public enum MovementType { Unrestricted, Elastic, Clamped }
        public enum ScrollbarVisibility { Permanent, AutoHide, AutoHideAndExpandViewport }
        public class ScrollRectEvent : UnityEvent<Vector2> { }
        public RectTransform content { get; set; }
        public RectTransform viewport { get; set; }
        public bool horizontal { get; set; } = true;
        public bool vertical { get; set; } = true;
        public float scrollSensitivity { get; set; } = 1f;
        public MovementType movementType { get; set; }
        public float elasticity { get; set; } = 0.1f;
        public bool inertia { get; set; } = true;
        public float decelerationRate { get; set; } = 0.135f;
        public Vector2 normalizedPosition { get; set; }
        public float verticalNormalizedPosition { get; set; }
        public float horizontalNormalizedPosition { get; set; }
        public Scrollbar verticalScrollbar { get; set; }
        public Scrollbar horizontalScrollbar { get; set; }
        public ScrollRectEvent onValueChanged { get; set; } = new();
        public void StopMovement() { }
    }

    public class Scrollbar : Selectable
    {
        public float value { get; set; }
        public float size { get; set; }
    }

    public static class LayoutRebuilder
    {
        public static void ForceRebuildLayoutImmediate(RectTransform layoutRoot) { }
        public static void MarkLayoutForRebuild(RectTransform rect) { }
    }

    public static class LayoutUtility
    {
        public static float GetPreferredWidth(RectTransform rect) => rect != null ? rect.sizeDelta.x : 0f;
        public static float GetPreferredHeight(RectTransform rect) => rect != null ? rect.sizeDelta.y : 0f;
    }
}

namespace TMPro
{
    public enum TextAlignmentOptions
    {
        TopLeft = 257, Top = 258, TopRight = 260, Left = 513, Center = 514, Right = 516,
        BottomLeft = 1025, Bottom = 1026, BottomRight = 1028, Midline = 4610, MidlineLeft = 4609,
        MidlineRight = 4612, Justified = 520, CenterGeoAligned = 546, Capline = 8706,
    }

    public enum TextOverflowModes { Overflow, Ellipsis, Masking, Truncate, ScrollRect, Page, Linked }
    public enum TextWrappingModes { NoWrap, Normal, PreserveWhitespace, PreserveWhitespaceNoWrap }
    public enum FontStyles { Normal = 0, Bold = 1, Italic = 2, Underline = 4, LowerCase = 8, UpperCase = 16, SmallCaps = 32 }
    public enum FontWeight { Regular = 400, Bold = 700 }
    public enum AtlasPopulationMode { Static, Dynamic }
    public enum VertexGradientMode { Single }

    public class TMP_Asset : UnityEngine.ScriptableObject { }

    public class TMP_FontAsset : TMP_Asset
    {
        public AtlasPopulationMode atlasPopulationMode { get; set; }
        public UnityEngine.Material material { get; set; }
        public List<TMP_FontAsset> fallbackFontAssetTable { get; set; } = new();
        public bool isMultiAtlasTexturesEnabled { get; set; }

        public static TMP_FontAsset CreateFontAsset(UnityEngine.Font font) => CreateInstance<TMP_FontAsset>();
        public static TMP_FontAsset CreateFontAsset(UnityEngine.Font font, int samplingPointSize, int atlasPadding,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode renderMode, int atlasWidth, int atlasHeight,
            AtlasPopulationMode atlasPopulationMode = AtlasPopulationMode.Dynamic, bool enableMultiAtlasSupport = true) =>
            CreateInstance<TMP_FontAsset>();
        public bool HasCharacter(char c) => true;
        public bool HasCharacters(string text) => true;
        public bool TryAddCharacters(string characters) => true;
        public bool TryAddCharacters(string characters, out string missing)
        {
            missing = "";
            return true;
        }
    }

    public static class TMP_Settings
    {
        public static bool enableWordWrapping { get; set; } = true;
        public static TMP_FontAsset defaultFontAsset { get; set; }
        public static List<TMP_FontAsset> fallbackFontAssets { get; set; } = new();
    }

    public class TMP_Text : UnityEngine.UI.MaskableGraphic
    {
        public virtual string text { get; set; } = "";
        public float fontSize { get; set; } = 36f;
        public float fontSizeMin { get; set; }
        public float fontSizeMax { get; set; } = 72f;
        public bool enableAutoSizing { get; set; }
        public TMP_FontAsset font { get; set; }
        public UnityEngine.Material fontSharedMaterial { get; set; }
        public UnityEngine.Material fontMaterial { get; set; }
        public TextAlignmentOptions alignment { get; set; } = TextAlignmentOptions.TopLeft;
        public FontStyles fontStyle { get; set; }
        public float outlineWidth { get; set; }
        public UnityEngine.Color32 outlineColor { get; set; }
        public UnityEngine.Color faceColor { get; set; } = UnityEngine.Color.white;
        public TextOverflowModes overflowMode { get; set; }
        public TextWrappingModes textWrappingMode { get; set; } = TextWrappingModes.Normal;
        public bool enableWordWrapping { get; set; } = true;
        public bool richText { get; set; } = true;
        public bool isRightToLeftText { get; set; }
        public float characterSpacing { get; set; }
        public float lineSpacing { get; set; }
        public float wordSpacing { get; set; }
        public UnityEngine.Vector4 margin { get; set; }
        public int maxVisibleCharacters { get; set; } = 99999;
        public bool autoSizeTextContainer { get; set; }
        public bool extraPadding { get; set; }
        public float alpha { get => color.a; set { UnityEngine.Color c = color; c.a = value; color = c; } }
        public float preferredWidth => (text?.Length ?? 0) * fontSize * 0.5f;
        public float preferredHeight => fontSize * 1.2f;
        public UnityEngine.Bounds textBounds => new(UnityEngine.Vector3.zero, new UnityEngine.Vector3(preferredWidth, preferredHeight, 0f));
        public void SetText(string sourceText) => text = sourceText;
        public void SetText(string sourceText, float arg0) => text = string.Format(sourceText, arg0);
        public void ForceMeshUpdate() { }
        public void ForceMeshUpdate(bool ignoreActiveState, bool forceTextReparsing = false) { }
        public UnityEngine.Vector2 GetPreferredValues() => new(preferredWidth, preferredHeight);
        public UnityEngine.Vector2 GetPreferredValues(string t) => new((t?.Length ?? 0) * fontSize * 0.5f, preferredHeight);
        public UnityEngine.Vector2 GetPreferredValues(string t, float w, float h) => GetPreferredValues(t);
    }

    public class TextMeshProUGUI : TMP_Text { }

    public class TextMeshPro : TMP_Text
    {
        public UnityEngine.Renderer renderer => GetComponent<UnityEngine.MeshRenderer>();
        public int sortingOrder { get; set; }
    }
}

namespace UnityEngine.TextCore.LowLevel
{
    public enum GlyphRenderMode { SMOOTH_HINTED = 4117, SMOOTH = 4118, RASTER_HINTED = 4121, RASTER = 4122, SDF = 4134, SDFAA_HINTED = 4169, SDFAA = 4165 }
}

namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour { }

    public class EventSystem : UIBehaviour
    {
        public static EventSystem current { get; set; }
        public GameObject currentSelectedGameObject => null;
        public bool sendNavigationEvents { get; set; } = true;
        public int pixelDragThreshold { get; set; } = 10;
        public bool IsPointerOverGameObject() => false;
        public bool IsPointerOverGameObject(int pointerId) => false;
        public void SetSelectedGameObject(GameObject go) { }
        public void RaycastAll(PointerEventData eventData, List<RaycastResult> raycastResults) => raycastResults.Clear();

        void OnEnable()
        {
            if (current == null) current = this;
        }

        void OnDisable()
        {
            if (current == this) current = null;
        }
    }

    public struct RaycastResult
    {
        public GameObject gameObject;
    }

    public class BaseInputModule : UIBehaviour { }
    public class StandaloneInputModule : BaseInputModule { }

    public class BaseEventData
    {
        public BaseEventData(EventSystem eventSystem) { }
        public void Use() { }
        public bool used => false;
    }

    public class PointerEventData : BaseEventData
    {
        public enum InputButton { Left, Right, Middle }
        public PointerEventData(EventSystem eventSystem) : base(eventSystem) { }
        public Vector2 position { get; set; }
        public Vector2 delta { get; set; }
        public Vector2 pressPosition { get; set; }
        public int pointerId { get; set; }
        public InputButton button { get; set; }
        public GameObject pointerEnter { get; set; }
        public GameObject pointerPress { get; set; }
        public Camera pressEventCamera => null;
        public Camera enterEventCamera => null;
        public int clickCount { get; set; }
    }

    public interface IEventSystemHandler { }
    public interface IPointerClickHandler : IEventSystemHandler { void OnPointerClick(PointerEventData eventData); }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData eventData); }
    public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData eventData); }
    public interface IPointerEnterHandler : IEventSystemHandler { void OnPointerEnter(PointerEventData eventData); }
    public interface IPointerExitHandler : IEventSystemHandler { void OnPointerExit(PointerEventData eventData); }
    public interface IBeginDragHandler : IEventSystemHandler { void OnBeginDrag(PointerEventData eventData); }
    public interface IDragHandler : IEventSystemHandler { void OnDrag(PointerEventData eventData); }
    public interface IEndDragHandler : IEventSystemHandler { void OnEndDrag(PointerEventData eventData); }
    public interface IScrollHandler : IEventSystemHandler { void OnScroll(PointerEventData eventData); }
}

namespace UnityEngine.InputSystem
{
    public class InputControl
    {
        public string name { get; set; } = "";
        public InputDevice device { get; set; }
    }

    public class InputControl<TValue> : InputControl where TValue : struct
    {
        public TValue ReadValue() => default;
        public TValue value => default;
    }

    public class InputDevice : InputControl
    {
        public bool added => false;
        public int deviceId => 0;
    }

    public class Pointer : InputDevice
    {
        public Controls.Vector2Control position { get; } = new();
        public Controls.Vector2Control delta { get; } = new();
        public Controls.ButtonControl press { get; } = new();
    }

    public class Mouse : Pointer
    {
        public static Mouse current => null;
        public Controls.ButtonControl leftButton { get; } = new();
        public Controls.ButtonControl rightButton { get; } = new();
        public Controls.ButtonControl middleButton { get; } = new();
        public Controls.Vector2Control scroll { get; } = new();
        public void WarpCursorPosition(Vector2 position) { }
    }

    public class Touchscreen : Pointer
    {
        public static Touchscreen current => null;
    }

    public enum Key
    {
        None, Space, Enter, Tab, Backquote, Quote, Semicolon, Comma, Period, Slash, Backslash,
        LeftBracket, RightBracket, Minus, Equals, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P,
        Q, R, S, T, U, V, W, X, Y, Z, Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7,
        Digit8, Digit9, Digit0, LeftShift, RightShift, LeftAlt, RightAlt, LeftCtrl, RightCtrl,
        LeftMeta, RightMeta, ContextMenu, Escape, LeftArrow, RightArrow, UpArrow, DownArrow,
        Backspace, PageDown, PageUp, Home, End, Insert, Delete, CapsLock, NumLock, PrintScreen,
        ScrollLock, Pause, NumpadEnter, NumpadDivide, NumpadMultiply, NumpadPlus, NumpadMinus,
        NumpadPeriod, NumpadEquals, Numpad0, Numpad1, Numpad2, Numpad3, Numpad4, Numpad5, Numpad6,
        Numpad7, Numpad8, Numpad9, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    }

    public class Keyboard : InputDevice
    {
        readonly Dictionary<Key, Controls.KeyControl> _keys = new();
        public static Keyboard current => null;

        public Controls.KeyControl this[Key key]
        {
            get
            {
                if (!_keys.TryGetValue(key, out Controls.KeyControl k)) _keys[key] = k = new Controls.KeyControl();
                return k;
            }
        }

        public Controls.KeyControl spaceKey => this[Key.Space];
        public Controls.KeyControl enterKey => this[Key.Enter];
        public Controls.KeyControl tabKey => this[Key.Tab];
        public Controls.KeyControl escapeKey => this[Key.Escape];
        public Controls.KeyControl backquoteKey => this[Key.Backquote];
        public Controls.KeyControl leftShiftKey => this[Key.LeftShift];
        public Controls.KeyControl rightShiftKey => this[Key.RightShift];
        public Controls.KeyControl leftCtrlKey => this[Key.LeftCtrl];
        public Controls.KeyControl leftAltKey => this[Key.LeftAlt];
        public Controls.KeyControl upArrowKey => this[Key.UpArrow];
        public Controls.KeyControl downArrowKey => this[Key.DownArrow];
        public Controls.KeyControl leftArrowKey => this[Key.LeftArrow];
        public Controls.KeyControl rightArrowKey => this[Key.RightArrow];
        public Controls.KeyControl aKey => this[Key.A];
        public Controls.KeyControl bKey => this[Key.B];
        public Controls.KeyControl cKey => this[Key.C];
        public Controls.KeyControl dKey => this[Key.D];
        public Controls.KeyControl eKey => this[Key.E];
        public Controls.KeyControl fKey => this[Key.F];
        public Controls.KeyControl gKey => this[Key.G];
        public Controls.KeyControl hKey => this[Key.H];
        public Controls.KeyControl iKey => this[Key.I];
        public Controls.KeyControl jKey => this[Key.J];
        public Controls.KeyControl kKey => this[Key.K];
        public Controls.KeyControl lKey => this[Key.L];
        public Controls.KeyControl mKey => this[Key.M];
        public Controls.KeyControl nKey => this[Key.N];
        public Controls.KeyControl oKey => this[Key.O];
        public Controls.KeyControl pKey => this[Key.P];
        public Controls.KeyControl qKey => this[Key.Q];
        public Controls.KeyControl rKey => this[Key.R];
        public Controls.KeyControl sKey => this[Key.S];
        public Controls.KeyControl tKey => this[Key.T];
        public Controls.KeyControl uKey => this[Key.U];
        public Controls.KeyControl vKey => this[Key.V];
        public Controls.KeyControl wKey => this[Key.W];
        public Controls.KeyControl xKey => this[Key.X];
        public Controls.KeyControl yKey => this[Key.Y];
        public Controls.KeyControl zKey => this[Key.Z];
        public Controls.KeyControl digit0Key => this[Key.Digit0];
        public Controls.KeyControl digit1Key => this[Key.Digit1];
        public Controls.KeyControl digit2Key => this[Key.Digit2];
        public Controls.KeyControl digit3Key => this[Key.Digit3];
        public Controls.KeyControl digit4Key => this[Key.Digit4];
        public Controls.KeyControl digit5Key => this[Key.Digit5];
        public Controls.KeyControl digit6Key => this[Key.Digit6];
        public Controls.KeyControl digit7Key => this[Key.Digit7];
        public Controls.KeyControl digit8Key => this[Key.Digit8];
        public Controls.KeyControl digit9Key => this[Key.Digit9];
        public Controls.KeyControl f1Key => this[Key.F1];
        public Controls.KeyControl f2Key => this[Key.F2];
        public Controls.KeyControl f3Key => this[Key.F3];
        public Controls.KeyControl f4Key => this[Key.F4];
        public Controls.KeyControl f5Key => this[Key.F5];
        public Controls.KeyControl f6Key => this[Key.F6];
        public Controls.KeyControl f7Key => this[Key.F7];
        public Controls.KeyControl f8Key => this[Key.F8];
        public Controls.KeyControl f9Key => this[Key.F9];
        public Controls.KeyControl f10Key => this[Key.F10];
        public Controls.KeyControl f11Key => this[Key.F11];
        public Controls.KeyControl f12Key => this[Key.F12];
    }

    public class Gamepad : InputDevice
    {
        public static Gamepad current => null;
    }

    public enum TouchPhase { None, Began, Moved, Ended, Canceled, Stationary }

    public enum InputActionType { Value, Button, PassThrough }

    public class InputAction
    {
        public InputAction() { }
        public InputAction(string name = null, InputActionType type = InputActionType.Value, string binding = null) { }
        public void Enable() { }
        public void Disable() { }
        public bool enabled => false;
        public TValue ReadValue<TValue>() where TValue : struct => default;
        public bool WasPressedThisFrame() => false;
        public bool WasReleasedThisFrame() => false;
        public bool IsPressed() => false;
        public void AddBinding(string path) { }
    }
}

namespace UnityEngine.InputSystem.Controls
{
    public class AxisControl : InputControl<float> { }

    public class ButtonControl : AxisControl
    {
        public bool wasPressedThisFrame => false;
        public bool wasReleasedThisFrame => false;
        public bool isPressed => false;
    }

    public class KeyControl : ButtonControl
    {
        public Key keyCode { get; set; }
    }

    public class Vector2Control : InputControl<Vector2>
    {
        public AxisControl x { get; } = new();
        public AxisControl y { get; } = new();
    }

    public class TouchControl : InputControl
    {
        public Vector2Control position { get; } = new();
    }
}

namespace UnityEngine.InputSystem.UI
{
    public class InputSystemUIInputModule : UnityEngine.EventSystems.BaseInputModule { }
}

namespace UnityEngine.InputSystem.EnhancedTouch
{
    public struct Touch
    {
        public static IReadOnlyList<Touch> activeTouches => Array.Empty<Touch>();
        public static IReadOnlyList<Finger> activeFingers => Array.Empty<Finger>();
        public static event Action<Finger> onFingerDown { add { } remove { } }
        public static event Action<Finger> onFingerUp { add { } remove { } }
        public static event Action<Finger> onFingerMove { add { } remove { } }

        public Finger finger => null;
        public int touchId => 0;
        public UnityEngine.InputSystem.TouchPhase phase => UnityEngine.InputSystem.TouchPhase.None;
        public Vector2 screenPosition => default;
        public Vector2 startScreenPosition => default;
        public Vector2 delta => default;
        public double startTime => 0;
        public double time => 0;
        public bool isInProgress => false;
        public bool began => false;
        public bool ended => false;
        public int tapCount => 0;
    }

    public class Finger
    {
        public int index { get; set; }
        public Vector2 screenPosition { get; set; }
        public Touch currentTouch => default;
        public Touch lastTouch => default;
        public bool isActive => false;
    }

    public static class EnhancedTouchSupport
    {
        public static bool enabled { get; private set; }
        public static void Enable() => enabled = true;
        public static void Disable() => enabled = false;
    }
}

namespace UnityEngine.Rendering
{
    public class VolumeComponent : ScriptableObject
    {
        public bool active { get; set; } = true;
    }

    public class VolumeParameter
    {
        public bool overrideState { get; set; }
    }

    public class VolumeParameter<T> : VolumeParameter
    {
        public T value { get; set; }
        public VolumeParameter() { }
        public VolumeParameter(T value, bool overrideState = false) { this.value = value; this.overrideState = overrideState; }
        public void Override(T x)
        {
            overrideState = true;
            value = x;
        }
        public static implicit operator T(VolumeParameter<T> prop) => prop != null ? prop.value : default;
    }

    public class FloatParameter : VolumeParameter<float>
    {
        public FloatParameter(float value, bool overrideState = false) : base(value, overrideState) { }
    }

    public class ClampedFloatParameter : FloatParameter
    {
        public ClampedFloatParameter(float value, float min, float max, bool overrideState = false) : base(value, overrideState) { }
    }

    public class MinFloatParameter : FloatParameter
    {
        public MinFloatParameter(float value, float min, bool overrideState = false) : base(value, overrideState) { }
    }

    public class ColorParameter : VolumeParameter<Color>
    {
        public ColorParameter(Color value, bool overrideState = false) : base(value, overrideState) { }
        public ColorParameter(Color value, bool hdr, bool showAlpha, bool showEyeDropper, bool overrideState = false) : base(value, overrideState) { }
    }

    public class BoolParameter : VolumeParameter<bool>
    {
        public BoolParameter(bool value, bool overrideState = false) : base(value, overrideState) { }
    }

    public class VolumeProfile : ScriptableObject
    {
        public List<VolumeComponent> components { get; } = new();

        public bool TryGet<T>(out T component) where T : VolumeComponent
        {
            foreach (VolumeComponent c in components)
            {
                if (c is T t)
                {
                    component = t;
                    return true;
                }
            }
            component = null;
            return false;
        }

        public T Add<T>(bool overrides = false) where T : VolumeComponent
        {
            T c = CreateInstance<T>();
            components.Add(c);
            return c;
        }

        public bool Has<T>() where T : VolumeComponent => TryGet<T>(out _);
    }

    public class Volume : MonoBehaviour
    {
        public bool isGlobal { get; set; } = true;
        public float weight { get; set; } = 1f;
        public float priority { get; set; }
        public float blendDistance { get; set; }
        public VolumeProfile profile { get; set; }
        public VolumeProfile sharedProfile { get => profile; set => profile = value; }
    }
}

namespace UnityEngine.Rendering.Universal
{
    public enum CameraRenderType { Base, Overlay }
    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing, TemporalAntiAliasing }
    public enum TonemappingMode { None, Neutral, ACES }
    public enum CameraOverrideOption { UsePipelineSettings, On, Off }

    public class UniversalAdditionalCameraData : MonoBehaviour
    {
        public CameraRenderType renderType { get; set; }
        public List<Camera> cameraStack { get; } = new();
        public bool renderPostProcessing { get; set; }
        public bool renderShadows { get; set; } = true;
        public AntialiasingMode antialiasing { get; set; }
        public bool stopNaN { get; set; }
        public bool dithering { get; set; }
        public CameraOverrideOption requiresDepthOption { get; set; }
    }

    public class UniversalAdditionalLightData : MonoBehaviour { }

    public class Bloom : VolumeComponent
    {
        public MinFloatParameter threshold = new(0.9f, 0f);
        public MinFloatParameter intensity = new(0f, 0f);
        public ClampedFloatParameter scatter = new(0.7f, 0f, 1f);
        public ColorParameter tint = new(Color.white);
    }

    public class ColorAdjustments : VolumeComponent
    {
        public FloatParameter postExposure = new(0f);
        public ClampedFloatParameter contrast = new(0f, -100f, 100f);
        public ColorParameter colorFilter = new(Color.white);
        public ClampedFloatParameter hueShift = new(0f, -180f, 180f);
        public ClampedFloatParameter saturation = new(0f, -100f, 100f);
    }

    public class Vignette : VolumeComponent
    {
        public ColorParameter color = new(Color.black);
        public ClampedFloatParameter intensity = new(0f, 0f, 1f);
        public ClampedFloatParameter smoothness = new(0.2f, 0.01f, 1f);
    }

    public class TonemappingModeParameter : VolumeParameter<TonemappingMode>
    {
        public TonemappingModeParameter(TonemappingMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    public class Tonemapping : VolumeComponent
    {
        public TonemappingModeParameter mode = new(TonemappingMode.None);
    }

    public class ChromaticAberration : VolumeComponent
    {
        public ClampedFloatParameter intensity = new(0f, 0f, 1f);
    }

    public class ScriptableRendererFeature : ScriptableObject
    {
        public bool isActive;
        public void SetActive(bool active) => isActive = active;
    }

    public class ScriptableRendererData : ScriptableObject
    {
        public System.Collections.Generic.List<ScriptableRendererFeature> rendererFeatures =
            new System.Collections.Generic.List<ScriptableRendererFeature>();
    }

    public enum UpscalingFilterSelection { Auto, Linear, FSR, STP }

    public class UniversalRenderPipelineAsset : ScriptableObject
    {
        public float renderScale;
        public int msaaSampleCount;
        public int mainLightShadowmapResolution;
        public bool supportsCameraDepthTexture;
        public ScriptableRendererData[] rendererDataList;
        public UpscalingFilterSelection upscalingFilter;
        public bool fsrOverrideSharpness;
        public float fsrSharpness;
    }
}

namespace UnityEngine.Rendering
{
    public static class GraphicsSettings
    {
        public static ScriptableObject currentRenderPipeline;
    }
}
