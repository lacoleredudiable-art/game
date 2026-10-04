using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Assets
{
    /// <summary>
    /// Tek giriş: <see cref="Resources.Load"/> / <see cref="Shader.Find"/> — eksik asset için bir kez uyarı + çağıranın yedek değeri.
    /// </summary>
    public sealed class AssetLoader
    {
        static readonly HashSet<string> WarnedResourcePaths = new(StringComparer.Ordinal);
        static readonly HashSet<string> WarnedShaderNames = new(StringComparer.Ordinal);

        public static T Load<T>(string path, T fallback) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
                return fallback;

            T loaded = Resources.Load<T>(path);
            if (loaded != null)
                return loaded;

            if (WarnOnceResource(path, typeof(T).Name))
                LogMissing($"[asset] eksik: {path} ({typeof(T).Name}) → yedek");

            return fallback;
        }

        public static T[] LoadAll<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
                return Array.Empty<T>();

            T[] loaded = Resources.LoadAll<T>(path);
            if (loaded != null && loaded.Length > 0)
                return loaded;

            if (WarnOnceResource(path, typeof(T).Name + "[]"))
                LogMissing($"[asset] eksik: {path} ({typeof(T).Name}[]) → yedek");

            return loaded ?? Array.Empty<T>();
        }

        public static Shader FindShader(string name, Shader fallback)
        {
            if (string.IsNullOrEmpty(name))
                return fallback;

            Shader shader = Shader.Find(name);
            if (shader != null)
                return shader;

            if (WarnOnceShader(name))
                LogMissing($"[asset] eksik: {name} (Shader) → yedek");

            return fallback;
        }

        static bool WarnOnceResource(string path, string typeLabel)
        {
            string key = path + "\0" + typeLabel;
            if (!WarnedResourcePaths.Add(key))
                return false;
            return true;
        }

        static bool WarnOnceShader(string name) => WarnedShaderNames.Add(name);

        static void LogMissing(string message)
        {
#if SWEEP_HEADLESS
            return;
#else
            Debug.LogWarning(message);
#endif
        }
    }
}
