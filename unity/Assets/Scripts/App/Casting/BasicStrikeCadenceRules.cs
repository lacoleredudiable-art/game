using Dovus.Core.Shared;

namespace Dovus.App.Casting
{
    /// <summary>Düz vuruş ritmi — süreler silahın prefire_sec + recovery_sec değerlerinden gelir.</summary>
    public static class BasicStrikeCadenceRules
    {
        public static float ClipSec(float prefireSec, float recoverySec) => prefireSec + recoverySec;

        public static double ClipMs(float prefireSec, float recoverySec) =>
            ClipSec(prefireSec, recoverySec) * Units.SecToMs;

        public static double BangAtWorldMs(double nowMs, float prefireSec) =>
            nowMs + prefireSec * Units.SecToMs;

        /// <summary>RunBasic hasat anları arası minimum süre (son hasat → sonraki hasat).</summary>
        public static bool CadenceReady(double nowMs, double lastHarvestMs, float prefireSec, float recoverySec)
        {
            if (lastHarvestMs < 0)
                return true;
            return nowMs - lastHarvestMs >= ClipMs(prefireSec, recoverySec) - 0.5;
        }
    }

}
