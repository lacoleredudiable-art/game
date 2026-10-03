using Dovus.Core.Tuning;
using Dovus.Game.DevTools;
using UnityEngine;

namespace Dovus.Game.Feel
{
    /// <summary>Kısa dokunsal geri bildirim — editör/başsız no-op, Android'de ms süreli titreşim.</summary>
    public static class FeelHaptics
    {
        static FeelTuning _feel;

        public static void Configure(FeelTuning feel) => _feel = feel;

        public static void Pulse(int durationMs)
        {
            if (durationMs <= 0)
                return;
            if (_feel != null && !_feel.FeelHapticsEnabled)
                return;
            TryShortVibrate(durationMs);
        }

        static void TryShortVibrate(long durationMs)
        {
            if (durationMs <= 0)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator == null)
                    return;

                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                int sdk = version.GetStatic<int>("SDK_INT");
                if (sdk >= 26)
                {
                    using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                    using var effect = effectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot", durationMs, -1);
                    vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", durationMs);
                }
            }
            catch (System.Exception)
            {
                // Emülatör / izin yok — sessizce atla.
            }
#else
            DebugConfig.DevLog($"[FeelHaptic] Pulse({durationMs}ms)");
#endif
        }
    }
}
