using System;
using System.Collections.Generic;

namespace Dovus.Core
{
    /// <summary>
    /// JSON'da olmayan bir skill sayısı için tek seferlik uyarı.
    /// Game katmanı <see cref="Warned"/> ile Unity konsoluna bağlar.
    /// </summary>
    public static class DesignWarnings
    {
        static readonly HashSet<string> Seen = new(StringComparer.Ordinal);

        public static event Action<string>? Warned;

        public static void Once(string key, string message)
        {
            if (string.IsNullOrEmpty(key) || !Seen.Add(key))
                return;
            Warned?.Invoke(message);
            System.Diagnostics.Trace.TraceWarning(message);
        }

        public static bool WasWarned(string key) =>
            !string.IsNullOrEmpty(key) && Seen.Contains(key);

        public static void ResetForTests()
        {
            Seen.Clear();
            Warned = null;
        }
    }
}
