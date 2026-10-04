using System.Diagnostics;

namespace Dovus.Game.DevTools
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

        /// <summary>Dev HP (oyuncu havuzu 1e9). Yalnız debug'da etkili; gramer debug panelinden açılıp kapanır.</summary>
        public static bool DevHp = true;

        /// <summary>
        /// O6: "%50 canla başla" heal testi. Varsayılan KAPALI. Açıkken oyuncu ve dost %50 canla doğar
        /// ve dev HP kapanır (yoksa 1e9 havuz %50'yi siler ve test ters çalışırdı).
        /// </summary>
        public static bool HalfHpStart = false;

        public static bool DevHpActive => Enabled && DevHp && !HalfHpStart;

        public static float StartHpRatio => Enabled && HalfHpStart ? 0.5f : 1f;

        /// <summary>O11: release'te çağrı ve metin interpolasyonu derleyici tarafından silinir.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DOVUS_DEBUG")]
        public static void DevLog(string m) => UnityEngine.Debug.Log(m);
    }
}
