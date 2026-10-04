using System.Diagnostics;

namespace Dovus.Game.Diagnostics
{
    /// <summary>
    /// K2 (denetim C): debug kapısı build türünden AYRI. Editörde ya da <c>DOVUS_DEBUG</c> define'ı ile
    /// alınmış dev APK'da açık; release APK'da (define yok) kapalı: dev HP, test panelleri, debug
    /// katmanları, tuning.json yüklemesi ve DevLog metinleri yok. Development build bayrağı
    /// (profiler vb.) bunu artık açmaz.
    /// </summary>
    public static class DebugConfig
    {
#if UNITY_EDITOR || DOVUS_DEBUG
        public static readonly bool Enabled = true;
#else
        public static readonly bool Enabled = false;
#endif

        /// <summary>O11: release'te çağrı ve metin interpolasyonu derleyici tarafından silinir.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DOVUS_DEBUG")]
        public static void DevLog(string m) => UnityEngine.Debug.Log(m);
    }
}
