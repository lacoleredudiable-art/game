using System;
using System.Collections.Generic;
using UnityEngine.Events;

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


    public enum TouchPhase { None, Began, Moved, Ended, Canceled, Stationary }

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


    public class ColorParameter : VolumeParameter<Color>
    {
        public ColorParameter(Color value, bool overrideState = false) : base(value, overrideState) { }
        public ColorParameter(Color value, bool hdr, bool showAlpha, bool showEyeDropper, bool overrideState = false) : base(value, overrideState) { }
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
