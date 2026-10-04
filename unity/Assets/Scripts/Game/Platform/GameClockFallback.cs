using Dovus.Core;
using Dovus.Core.Shared;

namespace Dovus.Game.Platform
{
    /// <summary>Saat enjekte edilmediğinde Time.deltaTime yedeği; ilk kullanımda uyarı.</summary>
    public static class GameClockFallback
    {
        public static float DeltaSec(GameClockHost clock, string warnKey)
        {
            if (clock != null)
                return (float)(clock.WorldDeltaMs / Units.SecToMs);
            DesignWarnings.Once(
                warnKey,
                "Saat bağlı değil; Time.deltaTime yedeği kullanılıyor.");
            return UnityEngine.Time.deltaTime;
        }

        public static double WorldDeltaMs(GameClockHost clock, string warnKey)
        {
            if (clock != null)
                return clock.WorldDeltaMs;
            DesignWarnings.Once(
                warnKey,
                "Saat bağlı değil; Time.deltaTime yedeği kullanılıyor.");
            return UnityEngine.Time.deltaTime * Units.SecToMs;
        }
    }
}
