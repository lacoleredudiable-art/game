namespace Dovus.App.Mechanics
{
    /// <summary>MechanicPortals.TickPortals — oyuncu kapıda mı, geçiş serbest mi.</summary>
    public static class PortalTransitionRules
    {
        public static bool ShouldResetInside(bool inGateA, bool inGateB) => !inGateA && !inGateB;

        public static bool ShouldTeleport(bool insideCooldown, bool inGateA, bool inGateB) =>
            !insideCooldown && (inGateA || inGateB);
    }
}
