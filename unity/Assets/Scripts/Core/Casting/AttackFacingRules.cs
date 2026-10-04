namespace Dovus.Core.Casting
{
    public static class AttackFacingRules
    {
        public static AttackFaceKind Resolve(bool performingAttack, bool directionalAim, bool hasLockedTarget)
        {
            if (!performingAttack)
                return AttackFaceKind.Movement;
            if (directionalAim)
                return AttackFaceKind.Directional;
            return hasLockedTarget ? AttackFaceKind.LockedTarget : AttackFaceKind.Hold;
        }
    }
}
