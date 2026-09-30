using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace UnityEngine
{
    public enum LogType { Error = 0, Assert = 1, Warning = 2, Log = 3, Exception = 4 }

    public enum RuntimePlatform { WindowsEditor = 7, WindowsPlayer = 2, Android = 11, IPhonePlayer = 8, LinuxEditor = 16 }

    public enum SystemLanguage { English = 10, Turkish = 37, Unknown = 42 }

    public enum NetworkReachability { NotReachable, ReachableViaCarrierDataNetwork, ReachableViaLocalAreaNetwork }

    public static class Time
    {
        static double s_time;
        static double s_unscaled;
        static float s_delta;
        static float s_unscaledDelta;

        public static float timeScale { get; set; } = 1f;
        public static float fixedDeltaTime { get; set; } = 0.02f;
        public static float maximumDeltaTime { get; set; } = 1f / 3f;
        public static float maximumParticleDeltaTime { get; set; } = 0.03f;
        public static int captureFramerate { get; set; }

        public static float deltaTime => s_delta;
        public static float unscaledDeltaTime => s_unscaledDelta;
        public static float smoothDeltaTime => s_delta;
        public static float fixedUnscaledDeltaTime => fixedDeltaTime;
        public static float time => (float)s_time;
        public static double timeAsDouble => s_time;
        public static float unscaledTime => (float)s_unscaled;
        public static double unscaledTimeAsDouble => s_unscaled;
        public static float realtimeSinceStartup => (float)s_unscaled;
        public static double realtimeSinceStartupAsDouble => s_unscaled;
        public static float timeSinceLevelLoad => (float)s_time;
        public static float fixedTime => (float)s_time;
        public static bool inFixedTimeStep => false;
        public static int frameCount => World.FrameCount;

        internal static void Advance(float unscaledDt)
        {
            s_unscaledDelta = unscaledDt;
            s_delta = Math.Min(unscaledDt * Math.Max(0f, timeScale), maximumDeltaTime);
            s_unscaled += unscaledDt;
            s_time += s_delta;
        }
    }

    public static class Debug
    {
        public static bool isDebugBuild => true;
        public static bool developerConsoleVisible { get; set; }

        /// <summary>Başsız koşucu: her log buraya da düşer (konsol/dosya).</summary>
        public static Action<string, LogType> Sink;

        static void Emit(string msg, LogType type, string stack = "")
        {
            Sink?.Invoke(string.IsNullOrEmpty(stack) ? msg : msg + "\n" + stack, type);
            Application.RaiseLog(msg, stack, type);
        }

        static string Str(object o) => o == null ? "Null" : o.ToString();

        public static void Log(object message) => Emit(Str(message), LogType.Log);
        public static void Log(object message, Object context) => Emit(Str(message), LogType.Log);
        public static void LogFormat(string format, params object[] args) => Emit(string.Format(format, args), LogType.Log);
        public static void LogFormat(Object context, string format, params object[] args) => Emit(string.Format(format, args), LogType.Log);
        public static void LogWarning(object message) => Emit(Str(message), LogType.Warning);
        public static void LogWarning(object message, Object context) => Emit(Str(message), LogType.Warning);
        public static void LogWarningFormat(string format, params object[] args) => Emit(string.Format(format, args), LogType.Warning);
        public static void LogWarningFormat(Object context, string format, params object[] args) => Emit(string.Format(format, args), LogType.Warning);
        public static void LogError(object message) => Emit(Str(message), LogType.Error);
        public static void LogError(object message, Object context) => Emit(Str(message), LogType.Error);
        public static void LogErrorFormat(string format, params object[] args) => Emit(string.Format(format, args), LogType.Error);
        public static void LogErrorFormat(Object context, string format, params object[] args) => Emit(string.Format(format, args), LogType.Error);
        public static void LogException(Exception exception) =>
            Emit(exception.GetType().Name + ": " + exception.Message, LogType.Exception,
                exception.InnerException != null ? exception.ToString() : exception.StackTrace ?? "");
        public static void LogException(Exception exception, Object context) => LogException(exception);
        public static void LogAssertion(object message) => Emit(Str(message), LogType.Assert);

        public static void Assert(bool condition)
        {
            if (!condition) Emit("Assertion failed", LogType.Assert);
        }

        public static void Assert(bool condition, object message)
        {
            if (!condition) Emit(Str(message), LogType.Assert);
        }

        public static void DrawLine(Vector3 start, Vector3 end) { }
        public static void DrawLine(Vector3 start, Vector3 end, Color color) { }
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration) { }
        public static void DrawRay(Vector3 start, Vector3 dir) { }
        public static void DrawRay(Vector3 start, Vector3 dir, Color color) { }
        public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration) { }
        public static void Break() { }
    }

    public static class Application
    {
        public delegate void LogCallback(string condition, string stackTrace, LogType type);

        public static event LogCallback logMessageReceived;
        public static event LogCallback logMessageReceivedThreaded;

        internal static void RaiseLog(string condition, string stack, LogType type)
        {
            logMessageReceived?.Invoke(condition, stack, type);
            logMessageReceivedThreaded?.Invoke(condition, stack, type);
        }

        public static bool isPlaying => true;
        public static bool isEditor => true;
        public static bool isMobilePlatform => false;
        public static bool isFocused => true;
        public static bool isBatchMode => true;
        public static RuntimePlatform platform => RuntimePlatform.WindowsEditor;
        public static SystemLanguage systemLanguage => SystemLanguage.Turkish;
        public static NetworkReachability internetReachability => NetworkReachability.NotReachable;
        public static string dataPath { get; set; } = "";
        public static string persistentDataPath { get; set; } = Path.Combine(Path.GetTempPath(), "dovus-sweep-v2");
        public static string streamingAssetsPath => Path.Combine(dataPath, "StreamingAssets");
        public static string temporaryCachePath => Path.GetTempPath();
        public static string version => "0.1";
        public static string unityVersion => "6000.4.4f1 (başsız)";
        public static string productName => "Dovus";
        public static string companyName => "Dovus";
        public static string identifier => "com.dovus.game";
        public static int targetFrameRate { get; set; } = -1;
        public static bool runInBackground { get; set; }
        public static void Quit() { }
        public static void Quit(int exitCode) { }
        public static void OpenURL(string url) { }
    }

    public class TextAsset : Object
    {
        readonly string _text;
        public TextAsset() : this("") { }
        public TextAsset(string text) { _text = text ?? ""; }
        public string text => _text;
        public byte[] bytes => Encoding.UTF8.GetBytes(_text);
        public override string ToString() => _text;
    }

    public class Font : Object
    {
        public Font() { }
        public Font(string name) { this.name = name; }
        public static Font CreateDynamicFontFromOSFont(string fontname, int size) => new(fontname);
        public static string[] GetOSInstalledFontNames() => new[] { "Arial" };
    }

    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }

    public static class Resources
    {
        /// <summary>unity/Assets/Resources — başsız koşucu kurar.</summary>
        public static string Root { get; set; } = "";
        static readonly Dictionary<string, TextAsset> s_text = new();
        static readonly string[] TextExt = { ".json", ".txt", ".bytes", ".csv", ".xml", ".yaml", ".md" };

        public static T Load<T>(string path) where T : Object => Load(path, typeof(T)) as T;

        public static Object Load(string path) => Load(path, typeof(Object));

        public static Object Load(string path, Type type)
        {
            if (type == typeof(TextAsset) || type == typeof(Object))
                return LoadText(path);
            return null;
        }

        static TextAsset LoadText(string path)
        {
            if (s_text.TryGetValue(path, out TextAsset cached)) return cached;
            TextAsset found = null;
            if (!string.IsNullOrEmpty(Root))
            {
                foreach (string ext in TextExt)
                {
                    string f = FindIgnoreCase(Root, path + ext);
                    if (f != null)
                    {
                        found = new TextAsset(File.ReadAllText(f, Encoding.UTF8)) { name = Path.GetFileName(path) };
                        break;
                    }
                }
            }
            s_text[path] = found;
            return found;
        }

        /// <summary>Unity Resources yolu büyük/küçük harfe duyarsız; Linux CI dosya sistemi değil.</summary>
        static string FindIgnoreCase(string root, string relative)
        {
            string exact = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(exact))
                return exact;
            string dir = root;
            string[] parts = relative.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                if (!Directory.Exists(dir))
                    return null;
                bool last = i == parts.Length - 1;
                string match = null;
                foreach (string e in last ? Directory.EnumerateFiles(dir) : Directory.EnumerateDirectories(dir))
                {
                    if (string.Equals(Path.GetFileName(e), parts[i], StringComparison.OrdinalIgnoreCase))
                    {
                        match = e;
                        break;
                    }
                }
                if (match == null)
                    return null;
                dir = match;
            }
            return dir;
        }

        public static T[] LoadAll<T>(string path) where T : Object => Array.Empty<T>();
        public static Object[] LoadAll(string path) => Array.Empty<Object>();

        public static T GetBuiltinResource<T>(string path) where T : Object
        {
            if (typeof(T) == typeof(Font)) return new Font(path) as T;
            if (typeof(T) == typeof(Mesh)) return Mesh.BuiltinByName(path) as T;
            return null;
        }

        public static void UnloadAsset(Object o) { }
        public static AsyncOperation UnloadUnusedAssets() => new();
    }

    public class AsyncOperation
    {
        public bool isDone => true;
        public float progress => 1f;
    }

    public static class JsonUtility
    {
        static readonly JsonSerializerOptions Options = new() { IncludeFields = true, WriteIndented = false };
        static readonly JsonSerializerOptions Pretty = new() { IncludeFields = true, WriteIndented = true };

        public static string ToJson(object obj) => ToJson(obj, false);
        public static string ToJson(object obj, bool prettyPrint) =>
            obj == null ? "" : JsonSerializer.Serialize(obj, obj.GetType(), prettyPrint ? Pretty : Options);
        public static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
        public static object FromJson(string json, Type type) => JsonSerializer.Deserialize(json, type, Options);

        public static void FromJsonOverwrite(string json, object objectToOverwrite)
        {
            object src = JsonSerializer.Deserialize(json, objectToOverwrite.GetType(), Options);
            foreach (var f in objectToOverwrite.GetType().GetFields())
                f.SetValue(objectToOverwrite, f.GetValue(src));
        }
    }

    public static class Random
    {
        static System.Random s_rng = new(12345);

        public static void InitState(int seed) => s_rng = new System.Random(seed);
        public static float value => (float)s_rng.NextDouble();
        public static float Range(float minInclusive, float maxInclusive) =>
            minInclusive + (float)s_rng.NextDouble() * (maxInclusive - minInclusive);
        public static int Range(int minInclusive, int maxExclusive) =>
            maxExclusive <= minInclusive ? minInclusive : s_rng.Next(minInclusive, maxExclusive);

        public static Vector2 insideUnitCircle
        {
            get
            {
                float a = value * Mathf.PI * 2f, r = Mathf.Sqrt(value);
                return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
        }

        public static Vector3 onUnitSphere
        {
            get
            {
                float z = Range(-1f, 1f), a = value * Mathf.PI * 2f, r = Mathf.Sqrt(1f - z * z);
                return new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, z);
            }
        }

        public static Vector3 insideUnitSphere => onUnitSphere * Mathf.Pow(value, 1f / 3f);
        public static Quaternion rotation => Quaternion.Euler(Range(0f, 360f), Range(0f, 360f), Range(0f, 360f));
        public static Color ColorHSV() => Color.HSVToRGB(value, value, value);
        public static Color ColorHSV(float hMin, float hMax, float sMin, float sMax, float vMin, float vMax) =>
            Color.HSVToRGB(Range(hMin, hMax), Range(sMin, sMax), Range(vMin, vMax));
    }

    public enum ScreenOrientation { Portrait = 1, PortraitUpsideDown = 2, LandscapeLeft = 3, LandscapeRight = 4, AutoRotation = 5 }

    public struct Resolution
    {
        public int width, height;
        public int refreshRate;
    }

    public static class Screen
    {
        public static int width { get; set; } = 2400;
        public static int height { get; set; } = 1080;
        public static float dpi { get; set; } = 400f;
        public static Rect safeArea => new(0, 0, width, height);
        public static ScreenOrientation orientation { get; set; } = ScreenOrientation.LandscapeLeft;
        public static bool fullScreen { get; set; } = true;
        public static int sleepTimeout { get; set; }
        public static bool autorotateToPortrait { get; set; }
        public static bool autorotateToPortraitUpsideDown { get; set; }
        public static bool autorotateToLandscapeLeft { get; set; } = true;
        public static bool autorotateToLandscapeRight { get; set; } = true;
        public static Resolution currentResolution => new() { width = width, height = height, refreshRate = 60 };
        public static void SetResolution(int w, int h, bool full) { width = w; height = h; }
    }

    public static class SleepTimeout
    {
        public const int NeverSleep = -1;
        public const int SystemSetting = -2;
    }

    public static class SystemInfo
    {
        public static string deviceModel => "Headless";
        public static string deviceName => "Headless";
        public static string operatingSystem => Environment.OSVersion.ToString();
        public static int systemMemorySize => 16384;
        public static int processorCount => Environment.ProcessorCount;
        public static bool supportsVibration => false;
        public static string graphicsDeviceName => "Null";
    }

    public static class Handheld
    {
        public static void Vibrate() { }
    }

    public static class QualitySettings
    {
        public static int vSyncCount { get; set; }
        public static int antiAliasing { get; set; }
        public static float shadowDistance { get; set; } = 50f;
        public static int pixelLightCount { get; set; } = 4;
        public static void SetQualityLevel(int index) { }
        public static void SetQualityLevel(int index, bool applyExpensiveChanges) { }
    }

    public enum FogMode { Linear = 1, Exponential = 2, ExponentialSquared = 3 }

    public static class RenderSettings
    {
        public static Light sun { get; set; }
        public static Rendering.AmbientMode ambientMode { get; set; }
        public static Color ambientSkyColor { get; set; }
        public static Color ambientEquatorColor { get; set; }
        public static Color ambientGroundColor { get; set; }
        public static Color ambientLight { get; set; }
        public static float ambientIntensity { get; set; } = 1f;
        public static bool fog { get; set; }
        public static Color fogColor { get; set; }
        public static float fogDensity { get; set; }
        public static FogMode fogMode { get; set; }
        public static float fogStartDistance { get; set; }
        public static float fogEndDistance { get; set; }
        public static Material skybox { get; set; }
        public static float reflectionIntensity { get; set; } = 1f;
    }

    public static class GUIUtility
    {
        public static string systemCopyBuffer { get; set; } = "";
        public static int hotControl { get; set; }
        public static int keyboardControl { get; set; }
    }
}

namespace UnityEngine.Rendering
{
    public enum AmbientMode { Skybox = 0, Trilight = 1, Flat = 3, Custom = 4 }
    public enum ShadowCastingMode { Off = 0, On = 1, TwoSided = 2, ShadowsOnly = 3 }
    public enum CompareFunction { Disabled, Never, Less, Equal, LessEqual, Greater, NotEqual, GreaterEqual, Always }
    public enum BlendMode { Zero, One, DstColor, SrcColor, OneMinusDstColor, SrcAlpha, OneMinusSrcColor, DstAlpha, OneMinusDstAlpha, SrcAlphaSaturate, OneMinusSrcAlpha }
    public enum CullMode { Off, Front, Back }
    public enum RenderQueue { Background = 1000, Geometry = 2000, AlphaTest = 2450, Transparent = 3000, Overlay = 4000 }
    public enum LightProbeUsage { Off, BlendProbes, UseProxyVolume, CustomProvided }
    public enum ReflectionProbeUsage { Off, BlendProbes, BlendProbesAndSkybox, Simple }
}
