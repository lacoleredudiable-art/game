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
