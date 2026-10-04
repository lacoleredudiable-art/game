using System;

namespace Dovus.Core.Equipment
{
    /// <summary>
    /// Yakın vuruş yayı. Açı 0 ise yay kapısı yok (eski kapsül kuralı durur).
    /// </summary>
    public static class MeleeArc
    {
        public static bool FriendlyVerb(int verbId) => verbId is 2 or 4 or 8 or 9;

        public static bool InFront(float deltaDeg, float arcDeg) =>
            arcDeg <= 0f || WeaponPassiveRules.AngleInArc(deltaDeg, arcDeg);

        /// <summary>
        /// Menzilde ve yayın içinde. ArcAllies açıksa kilit hedef olmayan dost da girer.
        /// </summary>
        public static bool Hits(
            bool inRange,
            float deltaDeg,
            float arcDeg,
            bool designated,
            bool ally,
            bool arcAllies)
        {
            if (!inRange || !InFront(deltaDeg, arcDeg))
                return false;
            if (arcDeg <= 0f)
                return designated;
            return designated || (arcAllies && ally);
        }

        /// <summary>
        /// Kapsül ıskalasa da kenar menzili + yay içi vurur.
        /// Yayın dışı kapsül teması vurmaz. Yay yoksa eski kural.
        /// </summary>
        public static bool StrikeConnects(bool capsuleHit, bool edgeInReach, float deltaDeg, float arcDeg)
        {
            if (!(capsuleHit || edgeInReach))
                return false;
            return InFront(deltaDeg, arcDeg);
        }
    }
}
