using System;
using System.Collections.Generic;

namespace UnityEngine.LowLevel
{
    public struct PlayerLoopSystem
    {
        public delegate void UpdateFunction();

        public Type type;
        public PlayerLoopSystem[] subSystemList;
        public UpdateFunction updateDelegate;
        public IntPtr updateFunction;
        public IntPtr loopConditionFunction;
    }

    public static class PlayerLoop
    {
        static PlayerLoopSystem s_loop = Default();

        static PlayerLoopSystem Default() => new()
        {
            subSystemList = new[]
            {
                new PlayerLoopSystem { type = typeof(UnityEngine.PlayerLoop.Initialization), subSystemList = Array.Empty<PlayerLoopSystem>() },
                new PlayerLoopSystem { type = typeof(UnityEngine.PlayerLoop.EarlyUpdate), subSystemList = Array.Empty<PlayerLoopSystem>() },
                new PlayerLoopSystem { type = typeof(UnityEngine.PlayerLoop.FixedUpdate), subSystemList = Array.Empty<PlayerLoopSystem>() },
                new PlayerLoopSystem { type = typeof(UnityEngine.PlayerLoop.PreUpdate), subSystemList = Array.Empty<PlayerLoopSystem>() },
                new PlayerLoopSystem { type = typeof(UnityEngine.PlayerLoop.Update), subSystemList = Array.Empty<PlayerLoopSystem>() },
                new PlayerLoopSystem { type = typeof(UnityEngine.PlayerLoop.PreLateUpdate), subSystemList = Array.Empty<PlayerLoopSystem>() },
                new PlayerLoopSystem { type = typeof(UnityEngine.PlayerLoop.PostLateUpdate), subSystemList = Array.Empty<PlayerLoopSystem>() },
            },
        };

        public static PlayerLoopSystem GetCurrentPlayerLoop() => Copy(s_loop);
        public static PlayerLoopSystem GetDefaultPlayerLoop() => Default();
        public static void SetPlayerLoop(PlayerLoopSystem loop) => s_loop = Copy(loop);

        static PlayerLoopSystem Copy(PlayerLoopSystem s)
        {
            if (s.subSystemList != null)
            {
                var list = new PlayerLoopSystem[s.subSystemList.Length];
                for (int i = 0; i < list.Length; i++) list[i] = Copy(s.subSystemList[i]);
                s.subSystemList = list;
            }
            return s;
        }

        /// <summary>Unity'nin PostLateUpdate fazındaki özel alt sistemlerini çağırır.</summary>
        internal static void RunPostLateUpdate()
        {
            foreach (PlayerLoopSystem phase in s_loop.subSystemList)
            {
                if (phase.type != typeof(UnityEngine.PlayerLoop.PostLateUpdate) || phase.subSystemList == null)
                    continue;
                foreach (PlayerLoopSystem sys in phase.subSystemList)
                {
                    try
                    {
                        sys.updateDelegate?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        internal static void Reset() => s_loop = Default();
    }
}

namespace UnityEngine.PlayerLoop
{
    public struct Initialization { }
    public struct EarlyUpdate { }
    public struct FixedUpdate { }
    public struct PreUpdate { }
    public struct Update { }
    public struct PreLateUpdate { }
    public struct PostLateUpdate { }
}

namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single, Additive }

    public static class SceneManager
    {
        public static Scene GetActiveScene() => new Scene();
        public static int sceneCount => 1;
        public static event Action<Scene, LoadSceneMode> sceneLoaded;
        public static event Action<Scene> sceneUnloaded;
        public static void LoadScene(string name) { }
        public static void LoadScene(int index) { }
        internal static void RaiseLoaded() => sceneLoaded?.Invoke(new Scene(), LoadSceneMode.Single);
        internal static void RaiseUnloaded() => sceneUnloaded?.Invoke(new Scene());
    }
}

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class InitializeOnLoadAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class InitializeOnLoadMethodAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class MenuItem : Attribute
    {
        public MenuItem(string itemName) { }
        public MenuItem(string itemName, bool isValidateFunction) { }
        public MenuItem(string itemName, bool isValidateFunction, int priority) { }
    }

    public static class Menu
    {
        public static void SetChecked(string menuPath, bool isChecked) { }
        public static bool GetChecked(string menuPath) => false;
    }

    public static class SessionState
    {
        static readonly Dictionary<string, object> s_values = new();

        public static bool GetBool(string key, bool defaultValue) => s_values.TryGetValue(key, out object v) && v is bool b ? b : defaultValue;
        public static void SetBool(string key, bool value) => s_values[key] = value;
        public static float GetFloat(string key, float defaultValue) => s_values.TryGetValue(key, out object v) && v is float f ? f : defaultValue;
        public static void SetFloat(string key, float value) => s_values[key] = value;
        public static int GetInt(string key, int defaultValue) => s_values.TryGetValue(key, out object v) && v is int i ? i : defaultValue;
        public static void SetInt(string key, int value) => s_values[key] = value;
        public static string GetString(string key, string defaultValue) => s_values.TryGetValue(key, out object v) && v is string s ? s : defaultValue;
        public static void SetString(string key, string value) => s_values[key] = value;
        public static void EraseString(string key) => s_values.Remove(key);
        public static void EraseBool(string key) => s_values.Remove(key);
        public static void EraseFloat(string key) => s_values.Remove(key);
        public static void EraseInt(string key) => s_values.Remove(key);
    }

    public enum PlayModeStateChange { EnteredEditMode, ExitingEditMode, EnteredPlayMode, ExitingPlayMode }

    public static class EditorApplication
    {
        public delegate void CallbackFunction();

        public static event Action<PlayModeStateChange> playModeStateChanged;
        public static CallbackFunction update;
        public static CallbackFunction delayCall;

        static bool s_playing = true;

        public static bool isPlaying
        {
            get => s_playing;
            set
            {
                if (s_playing == value) return;
                s_playing = value;
                playModeStateChanged?.Invoke(value ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.ExitingPlayMode);
            }
        }

        public static bool isPaused { get; set; }
        public static bool isCompiling => false;
        public static bool isPlayingOrWillChangePlaymode => s_playing;
        public static double timeSinceStartup => UnityEngine.Time.realtimeSinceStartupAsDouble;

        internal static void RunUpdate()
        {
            CallbackFunction dc = delayCall;
            delayCall = null;
            dc?.Invoke();
            update?.Invoke();
        }

        internal static void EnterPlay() => playModeStateChanged?.Invoke(PlayModeStateChange.EnteredPlayMode);

        public static void Exit(int code) => Environment.Exit(code);
    }
}

namespace UnityEditor.SceneManagement
{
    public enum OpenSceneMode { Single, Additive, AdditiveWithoutLoading }

    public static class EditorSceneManager
    {
        public static UnityEngine.Scene GetActiveScene() => new();
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() => true;
        public static UnityEngine.Scene OpenScene(string path) => new();
        public static UnityEngine.Scene OpenScene(string path, OpenSceneMode mode) => new();
    }
}

namespace UnityEditor.Animations
{
    public class AnimatorState
    {
        public string name = "";
    }

    public struct ChildAnimatorState
    {
        public AnimatorState state;
    }

    public class AnimatorStateMachine
    {
        public ChildAnimatorState[] states = Array.Empty<ChildAnimatorState>();
        public ChildAnimatorStateMachine[] stateMachines = Array.Empty<ChildAnimatorStateMachine>();
    }

    public struct ChildAnimatorStateMachine
    {
        public AnimatorStateMachine stateMachine;
    }

    public class AnimatorControllerLayer
    {
        public string name = "";
        public AnimatorStateMachine stateMachine = new();
    }

    public class AnimatorController : UnityEngine.RuntimeAnimatorController
    {
        public AnimatorControllerLayer[] layers { get; set; } = Array.Empty<AnimatorControllerLayer>();
    }
}
