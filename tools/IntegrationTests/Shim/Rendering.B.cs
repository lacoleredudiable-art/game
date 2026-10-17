using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    public class Camera : Behaviour
    {
        static readonly List<Camera> s_cameras = new();

        internal static void Register(Camera c) => s_cameras.Add(c);

        public static Camera main
        {
            get
            {
                foreach (Camera c in s_cameras)
                {
                    if (c == null || !c.isActiveAndEnabled) continue;
                    if (c.gameObject.CompareTag("MainCamera")) return c;
                }
                return null;
            }
        }

        public static Camera current => main;
        public static Camera[] allCameras => s_cameras.Where(c => c != null && c.isActiveAndEnabled).ToArray();
        public static int allCamerasCount => allCameras.Length;

        public float fieldOfView { get; set; } = 60f;
        public float nearClipPlane { get; set; } = 0.3f;
        public float farClipPlane { get; set; } = 1000f;
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; } = 5f;
        public float depth { get; set; }
        public int cullingMask { get; set; } = -1;
        public CameraClearFlags clearFlags { get; set; } = CameraClearFlags.Skybox;
        public Color backgroundColor { get; set; }
        public bool allowHDR { get; set; }
        public bool allowMSAA { get; set; }
        public RenderTexture targetTexture { get; set; }
        public Rect rect { get; set; } = new(0, 0, 1, 1);
        public float aspect => (float)Screen.width / Screen.height;
        public int pixelWidth => Screen.width;
        public int pixelHeight => Screen.height;
        public bool useOcclusionCulling { get; set; }
        public Rect pixelRect => new(0, 0, Screen.width, Screen.height);

        public Vector3 WorldToScreenPoint(Vector3 position)
        {
            Vector3 v = WorldToViewportPoint(position);
            return new Vector3(v.x * Screen.width, v.y * Screen.height, v.z);
        }

        public Vector3 WorldToViewportPoint(Vector3 position)
        {
            Vector3 local = transform.InverseTransformPoint(position);
            if (local.z <= 0.0001f) return new Vector3(0.5f, 0.5f, local.z);
            float tan = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            float y = local.y / (local.z * tan);
            float x = local.x / (local.z * tan * aspect);
            return new Vector3(x * 0.5f + 0.5f, y * 0.5f + 0.5f, local.z);
        }

        public Vector3 ScreenToWorldPoint(Vector3 position) => transform.position + transform.forward * position.z;
        public Vector3 ViewportToWorldPoint(Vector3 position) => transform.position + transform.forward * position.z;
        public Vector3 ScreenToViewportPoint(Vector3 position) => new(position.x / Screen.width, position.y / Screen.height, position.z);
        public Vector3 ViewportToScreenPoint(Vector3 position) => new(position.x * Screen.width, position.y * Screen.height, position.z);

        public Ray ScreenPointToRay(Vector3 pos)
        {
            float tan = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            float x = (pos.x / Screen.width * 2f - 1f) * tan * aspect;
            float y = (pos.y / Screen.height * 2f - 1f) * tan;
            Vector3 dir = transform.TransformDirection(new Vector3(x, y, 1f));
            return new Ray(transform.position, dir);
        }

        public Ray ViewportPointToRay(Vector3 pos) =>
            ScreenPointToRay(new Vector3(pos.x * Screen.width, pos.y * Screen.height, pos.z));

        public void Render() { }
        public void ResetAspect() { }
    }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public Camera worldCamera { get; set; }
        public float planeDistance { get; set; } = 100f;
        public int sortingOrder { get; set; }
        public bool overrideSorting { get; set; }
        public bool pixelPerfect { get; set; }
        public float scaleFactor { get; set; } = 1f;
        public float referencePixelsPerUnit { get; set; } = 100f;
        public string sortingLayerName { get; set; } = "Default";
        public Canvas rootCanvas => this;
        public bool isRootCanvas => true;
        public static void ForceUpdateCanvases() { }
    }

    public class CanvasGroup : Behaviour
    {
        public float alpha { get; set; } = 1f;
        public bool blocksRaycasts { get; set; } = true;
        public bool interactable { get; set; } = true;
        public bool ignoreParentGroups { get; set; }
    }

    public class CanvasRenderer : Component
    {
        public void SetAlpha(float a) { }
        public float GetAlpha() => 1f;
        public bool cullTransparentMesh { get; set; }
    }

    public class RectTransform : Transform
    {
        public enum Edge { Left, Right, Top, Bottom }
        public enum Axis { Horizontal, Vertical }

        public Vector2 anchorMin { get; set; } = new(0.5f, 0.5f);
        public Vector2 anchorMax { get; set; } = new(0.5f, 0.5f);
        public Vector2 pivot { get; set; } = new(0.5f, 0.5f);
        public Vector2 sizeDelta { get; set; } = new(100f, 100f);

        public Vector2 anchoredPosition
        {
            get => new(localPosition.x, localPosition.y);
            set => localPosition = new Vector3(value.x, value.y, localPosition.z);
        }

        public Vector3 anchoredPosition3D
        {
            get => localPosition;
            set => localPosition = value;
        }

        public Vector2 offsetMin
        {
            get => anchoredPosition - Vector2.Scale(sizeDelta, pivot);
            set
            {
                Vector2 offset = value - (anchoredPosition - Vector2.Scale(sizeDelta, pivot));
                sizeDelta -= offset;
                anchoredPosition += Vector2.Scale(offset, Vector2.one - pivot);
            }
        }

        public Vector2 offsetMax
        {
            get => anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot);
            set
            {
                Vector2 offset = value - (anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot));
                sizeDelta += offset;
                anchoredPosition += Vector2.Scale(offset, pivot);
            }
        }

        public Rect rect => new(-pivot.x * sizeDelta.x, -pivot.y * sizeDelta.y, sizeDelta.x, sizeDelta.y);

        public void SetSizeWithCurrentAnchors(Axis axis, float size)
        {
            Vector2 s = sizeDelta;
            if (axis == Axis.Horizontal) s.x = size; else s.y = size;
            sizeDelta = s;
        }

        public void SetInsetAndSizeFromParentEdge(Edge edge, float inset, float size) { }

        public void GetWorldCorners(Vector3[] fourCornersArray)
        {
            Rect r = rect;
            fourCornersArray[0] = TransformPoint(new Vector3(r.xMin, r.yMin));
            fourCornersArray[1] = TransformPoint(new Vector3(r.xMin, r.yMax));
            fourCornersArray[2] = TransformPoint(new Vector3(r.xMax, r.yMax));
            fourCornersArray[3] = TransformPoint(new Vector3(r.xMax, r.yMin));
        }

        public void GetLocalCorners(Vector3[] fourCornersArray)
        {
            Rect r = rect;
            fourCornersArray[0] = new Vector3(r.xMin, r.yMin);
            fourCornersArray[1] = new Vector3(r.xMin, r.yMax);
            fourCornersArray[2] = new Vector3(r.xMax, r.yMax);
            fourCornersArray[3] = new Vector3(r.xMax, r.yMin);
        }

        public void ForceUpdateRectTransforms() { }
    }

    public static class RectTransformUtility
    {
        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 screenPoint) => false;
        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 screenPoint, Camera cam) => false;
        public static Vector2 WorldToScreenPoint(Camera cam, Vector3 worldPoint) =>
            cam != null ? (Vector2)cam.WorldToScreenPoint(worldPoint) : new Vector2(worldPoint.x, worldPoint.y);
        public static bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera cam, out Vector2 localPoint)
        {
            localPoint = screenPoint;
            return true;
        }
        public static bool ScreenPointToWorldPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera cam, out Vector3 worldPoint)
        {
            worldPoint = screenPoint;
            return true;
        }
    }

    [Serializable]
    public class Gradient
    {
        public GradientColorKey[] colorKeys { get; set; } = { new(Color.white, 0f), new(Color.white, 1f) };
        public GradientAlphaKey[] alphaKeys { get; set; } = { new(1f, 0f), new(1f, 1f) };
        public GradientMode mode { get; set; }

        public void SetKeys(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys)
        {
            this.colorKeys = colorKeys;
            this.alphaKeys = alphaKeys;
        }

        public Color Evaluate(float time)
        {
            Color c = Color.white;
            if (colorKeys != null && colorKeys.Length > 0) c = colorKeys[0].color;
            if (colorKeys != null)
            {
                for (int i = 1; i < colorKeys.Length; i++)
                {
                    if (time <= colorKeys[i].time)
                    {
                        GradientColorKey a = colorKeys[i - 1], b = colorKeys[i];
                        float t = Mathf.InverseLerp(a.time, b.time, time);
                        c = Color.Lerp(a.color, b.color, t);
                        break;
                    }
                    c = colorKeys[i].color;
                }
            }
            return c;
        }
    }

    public struct GradientColorKey
    {
        public Color color;
        public float time;
        public GradientColorKey(Color col, float time) { color = col; this.time = time; }
    }

    public struct GradientAlphaKey
    {
        public float alpha;
        public float time;
        public GradientAlphaKey(float alpha, float time) { this.alpha = alpha; this.time = time; }
    }

    public struct Keyframe
    {
        public float time, value, inTangent, outTangent;
        public Keyframe(float time, float value) { this.time = time; this.value = value; inTangent = 0f; outTangent = 0f; }
        public Keyframe(float time, float value, float inTangent, float outTangent)
        {
            this.time = time; this.value = value; this.inTangent = inTangent; this.outTangent = outTangent;
        }
    }

    [Serializable]
    public class AnimationCurve
    {
        public Keyframe[] keys { get; set; } = Array.Empty<Keyframe>();
        public AnimationCurve() { }
        public AnimationCurve(params Keyframe[] keys) { this.keys = keys ?? Array.Empty<Keyframe>(); }
        public int length => keys.Length;
        public static AnimationCurve Linear(float timeStart, float valueStart, float timeEnd, float valueEnd) =>
            new(new Keyframe(timeStart, valueStart), new Keyframe(timeEnd, valueEnd));
        public static AnimationCurve EaseInOut(float timeStart, float valueStart, float timeEnd, float valueEnd) =>
            new(new Keyframe(timeStart, valueStart), new Keyframe(timeEnd, valueEnd));
        public static AnimationCurve Constant(float timeStart, float timeEnd, float value) =>
            new(new Keyframe(timeStart, value), new Keyframe(timeEnd, value));
        public int AddKey(float time, float value)
        {
            var list = keys.ToList();
            list.Add(new Keyframe(time, value));
            keys = list.OrderBy(k => k.time).ToArray();
            return keys.Length - 1;
        }
        public float Evaluate(float time)
        {
            if (keys.Length == 0) return 0f;
            if (time <= keys[0].time) return keys[0].value;
            for (int i = 1; i < keys.Length; i++)
            {
                if (time <= keys[i].time)
                    return Mathf.Lerp(keys[i - 1].value, keys[i].value, Mathf.InverseLerp(keys[i - 1].time, keys[i].time, time));
            }
            return keys[keys.Length - 1].value;
        }
    }

    public class GUISkin : Object
    {
        public GUIStyle box { get; set; } = new();
        public GUIStyle label { get; set; } = new();
        public GUIStyle button { get; set; } = new();
        public GUIStyle toggle { get; set; } = new();
        public GUIStyle textField { get; set; } = new();
        public GUIStyle window { get; set; } = new();
    }

    public class GUIStyleState
    {
        public Color textColor { get; set; }
        public Texture2D background { get; set; }
    }

    public class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public GUIStyleState normal { get; set; } = new();
        public GUIStyleState hover { get; set; } = new();
        public GUIStyleState active { get; set; } = new();
        public TextAnchor alignment { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public bool wordWrap { get; set; }
        public bool richText { get; set; }
        public RectOffset padding { get; set; } = new();
        public RectOffset margin { get; set; } = new();
        public Font font { get; set; }
        public float fixedHeight { get; set; }
        public float fixedWidth { get; set; }
        public bool stretchWidth { get; set; }
        public Vector2 CalcSize(GUIContent content) => new(100f, 20f);
    }

    public class GUIContent
    {
        public string text;
        public GUIContent() { }
        public GUIContent(string text) { this.text = text; }
    }

    public class GUILayoutOption { }

    public static class GUI
    {
        public static GUISkin skin { get; set; } = new();
        public static Color color { get; set; } = Color.white;
        public static Color backgroundColor { get; set; } = Color.white;
        public static Color contentColor { get; set; } = Color.white;
        public static bool enabled { get; set; } = true;
        public static int depth { get; set; }
        public static Matrix4x4 matrix { get; set; } = Matrix4x4.identity;
        public static void Box(Rect r, string text) { }
        public static void Box(Rect r, string text, GUIStyle style) { }
        public static void Box(Rect r, GUIContent c) { }
        public static void Label(Rect r, string text) { }
        public static void Label(Rect r, string text, GUIStyle style) { }
        public static void Label(Rect r, GUIContent c, GUIStyle style) { }
        public static bool Button(Rect r, string text) => false;
        public static bool Button(Rect r, string text, GUIStyle style) => false;
        public static bool Toggle(Rect r, bool value, string text) => value;
        public static bool Toggle(Rect r, bool value, string text, GUIStyle style) => value;
        public static float HorizontalSlider(Rect r, float value, float left, float right) => value;
        public static string TextField(Rect r, string text) => text;
        public static void DrawTexture(Rect r, Texture t) { }
    }

    public static class GUILayout
    {
        public static void BeginArea(Rect r) { }
        public static void BeginArea(Rect r, GUIStyle style) { }
        public static void BeginArea(Rect r, string text, GUIStyle style) { }
        public static void EndArea() { }
        public static void BeginHorizontal(params GUILayoutOption[] o) { }
        public static void BeginHorizontal(GUIStyle s, params GUILayoutOption[] o) { }
        public static void EndHorizontal() { }
        public static void BeginVertical(params GUILayoutOption[] o) { }
        public static void BeginVertical(GUIStyle s, params GUILayoutOption[] o) { }
        public static void EndVertical() { }
        public static Vector2 BeginScrollView(Vector2 p, params GUILayoutOption[] o) => p;
        public static void EndScrollView() { }
        public static void Label(string text, params GUILayoutOption[] o) { }
        public static void Label(string text, GUIStyle s, params GUILayoutOption[] o) { }
        public static bool Button(string text, params GUILayoutOption[] o) => false;
        public static bool Button(string text, GUIStyle s, params GUILayoutOption[] o) => false;
        public static bool Toggle(bool value, string text, params GUILayoutOption[] o) => value;
        public static bool Toggle(bool value, string text, GUIStyle s, params GUILayoutOption[] o) => value;
        public static string TextField(string text, params GUILayoutOption[] o) => text;
        public static string TextField(string text, GUIStyle s, params GUILayoutOption[] o) => text;
        public static float HorizontalSlider(float value, float l, float r, params GUILayoutOption[] o) => value;
        public static void Space(float px) { }
        public static void FlexibleSpace() { }
        public static GUILayoutOption Width(float w) => new();
        public static GUILayoutOption Height(float h) => new();
        public static GUILayoutOption MinWidth(float w) => new();
        public static GUILayoutOption MaxWidth(float w) => new();
        public static GUILayoutOption ExpandWidth(bool e) => new();
    }

    // ---------------------------------------------------------------- ses

    public class AudioClip : Object
    {
        public float length { get; private set; }
        public int samples { get; private set; }
        public int channels { get; private set; } = 1;
        public int frequency { get; private set; } = 44100;
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) =>
            new() { name = name, samples = lengthSamples, channels = channels, frequency = frequency, length = (float)lengthSamples / frequency };
        public bool SetData(float[] data, int offsetSamples) => true;
        public bool GetData(float[] data, int offsetSamples) => true;
    }

    public class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public float volume { get; set; } = 1f;
        public float pitch { get; set; } = 1f;
        public bool loop { get; set; }
        public bool playOnAwake { get; set; } = true;
        public float spatialBlend { get; set; }
        public bool mute { get; set; }
        public float time { get; set; }
        public int priority { get; set; } = 128;
        public float minDistance { get; set; } = 1f;
        public float maxDistance { get; set; } = 500f;
        public float dopplerLevel { get; set; } = 1f;
        public bool isPlaying => false;
        public void Play() { }
        public void Play(ulong delay) { }
        public void PlayOneShot(AudioClip clip) { }
        public void PlayOneShot(AudioClip clip, float volumeScale) { }
        public void Stop() { }
        public void Pause() { }
        public void UnPause() { }
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position) { }
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume) { }
    }

    public class AudioListener : Behaviour
    {
        public static float volume { get; set; } = 1f;
        public static bool pause { get; set; }
    }

}

namespace UnityEngine.Rendering
{
    public enum IndexFormat { UInt16, UInt32 }
}
