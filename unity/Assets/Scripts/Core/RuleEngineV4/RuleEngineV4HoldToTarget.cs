namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Basılı tutarak hedef seçme kuralı (RuleEngineV4).</summary>
    public static class RuleEngineV4HoldToTarget
    {
        /// <summary>2. rün konduktan beri geçen süre eşiği geçti mi.</summary>
        public static bool PickOpen(double heldSec, float holdToTargetSec) =>
            heldSec >= holdToTargetSec;

        public static RuleEngineV4HeldRelease Decide(
            bool pickOpen,
            bool pointerCancelled,
            bool releasedOnCenter,
            bool releasedOnHexagon,
            bool hasPickedTarget)
        {
            if (pointerCancelled)
                return RuleEngineV4HeldRelease.Cancel;
            if (!pickOpen)
                return RuleEngineV4HeldRelease.LaunchDefault;
            if (releasedOnCenter)
                return RuleEngineV4HeldRelease.Cancel;
            if (!releasedOnHexagon && hasPickedTarget)
                return RuleEngineV4HeldRelease.LaunchAtPicked;
            return RuleEngineV4HeldRelease.LaunchAuto;
        }
    }
}
