using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public static partial class Graphics { public static void Blit(Texture s, RenderTexture d) { } }
}

namespace UnityEngine.UI
{
    public partial class Graphic : UnityEngine.MonoBehaviour { public Color color; public bool raycastTarget; }
    public partial class MaskableGraphic : Graphic { }
    public partial class Image : MaskableGraphic
    {
        public Sprite sprite;
        public Image.Type type;
        public enum Type { Simple, Sliced, Filled }
        public float fillAmount;
        public bool preserveAspect;
        public bool fillClockwise;
        public FillMethod fillMethod;
        public int fillOrigin;
        public RectTransform rectTransform => null;
        public enum FillMethod { Horizontal, Vertical, Radial360 }
        public enum OriginHorizontal { Left, Right }
        public enum Origin360 { Bottom, Right, Top, Left }
    }
    public partial class Text : MaskableGraphic
    {
        public string text;
        public Font font;
        public int fontSize;
        public FontStyle fontStyle;
        public TextAnchor alignment;
        public bool resizeTextForBestFit;
        public int resizeTextMinSize, resizeTextMaxSize;
        public HorizontalWrapMode horizontalOverflow;
        public VerticalWrapMode verticalOverflow;
        public float preferredWidth, preferredHeight;
        public bool supportRichText = true;
        public void SetAllDirty() { }
        public RectTransform rectTransform => null;
    }
    // Wrap modları UnityEngine'de durur.
    // FontStyle UnityEngine'de durur; burada tekrar tanımlanmaz.
    public partial class Outline : Shadow { }
    public partial class Shadow : UnityEngine.MonoBehaviour { public Vector2 effectDistance; public Color effectColor; }
    public partial class Button : Selectable { public ButtonClickedEvent onClick; }
    public partial class ButtonClickedEvent { public void AddListener(Action a) { } }
    public partial class Selectable : UnityEngine.MonoBehaviour { public bool interactable; }
    public partial class Slider : Selectable
    {
        public float value, minValue, maxValue;
        public bool wholeNumbers;
        public Graphic targetGraphic;
        public RectTransform handleRect, fillRect;
        public Direction direction;
        public Transition transition;
        public SliderEvent onValueChanged;
        public void SetValueWithoutNotify(float v) { value = v; }
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
    }
    public partial class SliderEvent { public void AddListener(Action<float> a) { } }
    public partial class Selectable
    {
        public enum Transition { None, ColorTint, SpriteSwap, Animation }
    }
    public partial class Toggle : Selectable { public bool isOn; }
    public partial class CanvasScaler : UnityEngine.MonoBehaviour
    {
        public float scaleFactor;
        public ScaleMode uiScaleMode;
        public Vector2 referenceResolution;
        public float matchWidthOrHeight;
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
    }
    public partial class GraphicRaycaster : UnityEngine.MonoBehaviour { }
    public partial class RectMask2D : UnityEngine.MonoBehaviour { }
    public partial class HorizontalLayoutGroup : UnityEngine.MonoBehaviour { }
    public partial class VerticalLayoutGroup : UnityEngine.MonoBehaviour
    {
        public float spacing;
        public bool childControlHeight, childControlWidth, childForceExpandHeight, childForceExpandWidth;
        public RectOffset padding;
    }
    // RectOffset UnityEngine'de durur.
    public partial class LayoutElement : UnityEngine.MonoBehaviour { public float preferredWidth, preferredHeight; }
    public partial class ContentSizeFitter : UnityEngine.MonoBehaviour
    {
        public FitMode verticalFit;
        public FitMode horizontalFit;
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
    }
    public partial class AspectRatioFitter : UnityEngine.MonoBehaviour
    {
        public float aspectRatio;
        public AspectMode aspectMode;
        public enum AspectMode { None, WidthControlsHeight, HeightControlsWidth, FitInParent, EnvelopeParent }
    }
    public partial class ScrollRect : UnityEngine.MonoBehaviour
    {
        public RectTransform content;
        public bool horizontal, vertical;
        public RectTransform viewport;
        public float scrollSensitivity;
        public MovementType movementType;
        public enum MovementType { Unrestricted, Elastic, Clamped }
    }
    public partial class HorizontalLayoutGroup : UnityEngine.MonoBehaviour
    {
        public float spacing;
        public bool childControlHeight, childControlWidth, childForceExpandHeight, childForceExpandWidth;
    }
    public partial class Image
    {
        public void SetAllDirty() { }
    }
    public partial class Button
    {
        public Graphic targetGraphic;
    }
}

namespace TMPro
{
    public partial class TMP_Text : UnityEngine.UI.MaskableGraphic { public string text; public float fontSize; public UnityEngine.Color color; }
    public partial class TextMeshProUGUI : TMP_Text
    {
        public TextAlignmentOptions alignment;
        public UnityEngine.RectTransform rectTransform => null;
        public TMP_FontAsset font;
        public float outlineWidth;
        public UnityEngine.Color outlineColor;
        public TextOverflowModes overflowMode;
        public TextWrappingModes textWrappingMode;
    }
    public enum TextOverflowModes { Overflow, Ellipsis }
    public enum TextWrappingModes { Normal, NoWrap }
    public static class TMP_Settings
    {
        public static bool enableWordWrapping;
        public static TMP_FontAsset defaultFontAsset;
    }
    public partial class TextMeshPro : TMP_Text { }
    public partial class TMP_FontAsset : UnityEngine.Object { }
    public enum TextAlignmentOptions { Center, Left, Top }
}

namespace UnityEngine.EventSystems
{
    public partial class EventSystem : MonoBehaviour { public static EventSystem current; }
    public partial class BaseInputModule : MonoBehaviour { }
    public partial class StandaloneInputModule : BaseInputModule { }
    public interface IPointerClickHandler { }
    public interface IPointerDownHandler { }
    public interface IPointerUpHandler { }
    public interface IPointerExitHandler { }
    public interface IDragHandler { }
    public partial class PointerEventData { public Vector2 position; }
}

namespace UnityEngine.InputSystem
{
    public partial class InputControl { }
    public partial class InputDevice : InputControl { }
    public partial class Mouse : Pointer
    {
        public static Mouse current => null;
        public ButtonControl leftButton;
        public Vector2Control position;
    }
    public partial class Pointer : InputDevice { }
    public partial class Keyboard : InputDevice
    {
        public static Keyboard current => null;
        public KeyControl spaceKey, qKey, rKey, escapeKey, eKey, aKey, dKey, wKey, sKey, bKey;
        public KeyControl f1Key, f2Key, f8Key, digit1Key, digit2Key, digit3Key, digit4Key, digit5Key, digit6Key;
        public KeyControl downArrowKey, upArrowKey, leftArrowKey, rightArrowKey;
        public KeyControl tabKey; // ff-4: CameraOrbitInput Tab ile lock-on aç/kapa
    }
    public partial class ButtonControl : InputControl
    {
        public bool wasPressedThisFrame => false;
        public bool wasReleasedThisFrame => false;
        public bool isPressed => false;
    }
    public partial class KeyControl : ButtonControl { }
    public partial class Vector2Control : InputControl { public Vector2 ReadValue() => default; }
    public enum TouchPhase { Began, Moved, Ended, Canceled, Stationary }
    public partial class InputAction { }
}

namespace UnityEngine.InputSystem.UI
{
    public partial class InputSystemUIInputModule : UnityEngine.EventSystems.BaseInputModule { }
}

namespace UnityEngine.InputSystem.EnhancedTouch
{
    public static partial class Touch
    {
        public static System.Collections.Generic.IReadOnlyList<Finger> activeTouches =>
            System.Array.Empty<Finger>();
        public static event Action<Finger> onFingerDown;
        public static event Action<Finger> onFingerUp;
        public static event Action<Finger> onFingerMove;
    }

    public struct TouchState
    {
        public UnityEngine.InputSystem.TouchPhase phase;
        public Vector2 position;
        public int fingerId;
    }

    public partial class Finger
    {
        public int index;
        public Vector2 screenPosition;
        public TouchState currentTouch;
    }

    public static partial class EnhancedTouchSupport
    {
        public static void Enable() { }
        public static void Disable() { }
    }
}

namespace UnityEngine.InputSystem.Controls
{
    public partial class ButtonControl : UnityEngine.InputSystem.ButtonControl { }
}

namespace UnityEngine.Rendering.Universal
{
    public partial class UniversalAdditionalCameraData : UnityEngine.MonoBehaviour
    {
        public CameraRenderType renderType;
        public List<Camera> cameraStack;
        public bool renderPostProcessing;
        public CameraOverrideOption requiresDepthOption;
    }
    public enum CameraRenderType { Base, Overlay }
    public enum CameraOverrideOption { UsePipelineSettings, On, Off }
    public partial class Volume : UnityEngine.MonoBehaviour { public bool isGlobal; public float weight; public VolumeProfile profile; }
    public partial class VolumeProfile : UnityEngine.ScriptableObject
    {
        public bool TryGet<T>(out T component) where T : VolumeComponent { component = default; return false; }
        public T Add<T>() where T : VolumeComponent => default;
    }
    public partial class VolumeComponent : UnityEngine.ScriptableObject { }
    public partial class Bloom : VolumeComponent
    {
        public FloatParameter intensity;
        public FloatParameter threshold;
        public ColorParameter tint;
        public FloatParameter scatter;
    }
    public partial class ColorAdjustments : VolumeComponent { public FloatParameter postExposure, contrast, saturation; public ColorParameter colorFilter; }
    public partial class Vignette : VolumeComponent { public FloatParameter intensity; }
    public partial class Tonemapping : VolumeComponent { public TonemappingModeParameter mode; }
    public partial class FloatParameter
    {
        public float value;
        public static implicit operator float(FloatParameter p) => p == null ? 0 : p.value;
        public void Override(float v) { value = v; }
    }
    public partial class Volume
    {
        public float priority;
    }
    public partial class Vignette
    {
        public FloatParameter smoothness;
    }
    public partial class VolumeProfile
    {
        public T Add<T>(bool v) where T : VolumeComponent => default;
    }
    public partial class ColorParameter
    {
        public UnityEngine.Color value;
        public void Override(UnityEngine.Color v) { value = v; }
    }
    // Ambiyans portu: SceneAtmosphere Tonemapping.mode.Override(TonemappingMode.Neutral) kullanıyor.
    public enum TonemappingMode { None, Neutral, ACES }
    public partial class TonemappingModeParameter
    {
        public int value;
        public void Override(TonemappingMode v) { value = (int)v; }
    }
    public partial class ScriptableRendererData : UnityEngine.ScriptableObject
    {
        public System.Collections.Generic.List<ScriptableRendererFeature> rendererFeatures;
    }
    public partial class ScriptableRendererFeature : UnityEngine.ScriptableObject
    {
        public bool isActive;
        public void SetActive(bool active) { isActive = active; }
    }
    public enum UpscalingFilterSelection { Auto, Linear, FSR, STP }
    public partial class UniversalRenderPipelineAsset : UnityEngine.Rendering.RenderPipelineAsset
    {
        public float renderScale;
        public int msaaSampleCount;
        public int mainLightShadowmapResolution;
        public bool supportsCameraDepthTexture;
        public ScriptableRendererData[] rendererDataList;
        public UpscalingFilterSelection upscalingFilter;
        public bool fsrOverrideSharpness;
        public float fsrSharpness;
        protected override UnityEngine.Rendering.RenderPipeline CreatePipeline() => null;
    }
    public partial class ShadowsMidtonesHighlights : VolumeComponent
    {
        public Vector4Parameter shadows, midtones, highlights;
    }
    public partial class Vector4Parameter
    {
        public UnityEngine.Vector4 value;
        public void Override(UnityEngine.Vector4 v) { value = v; }
    }
    public partial class WhiteBalance : VolumeComponent
    {
        public FloatParameter temperature;
    }
}

namespace UnityEngine.Rendering
{
    public partial class GraphicsSettings
    {
        public static UnityEngine.Rendering.RenderPipelineAsset currentRenderPipeline;
    }
}

namespace UnityEngine.TextCore.LowLevel
{
    public enum GlyphRenderMode { SDFAA }
}
