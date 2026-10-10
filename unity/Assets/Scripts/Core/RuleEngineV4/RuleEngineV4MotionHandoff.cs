using System;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Yeni hareket eskiyi keser; hızın yarısı taşınır, tavan 1.5x.</summary>
    public static class RuleEngineV4MotionHandoff
    {
        public static float EffectiveSpeedMps(float commandSpeedMps, float incomingSpeedMps)
        {
            float baseSpeed = Math.Max(0f, commandSpeedMps);
            float blended = baseSpeed + Math.Max(0f, incomingSpeedMps) * RuleEngineV4WorldPhysicsDefaults.MotionCarryRatio;
            float cap = baseSpeed * RuleEngineV4WorldPhysicsDefaults.MotionSpeedMaxMult;
            return Math.Min(blended, cap);
        }
    }
}
