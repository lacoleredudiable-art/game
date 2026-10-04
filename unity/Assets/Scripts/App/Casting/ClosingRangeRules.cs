using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using System;

namespace Dovus.App.Casting
{
    public static class ClosingRangeRules
    {
        public static bool IsClosingInRange(
            LivingEffect logic,
            float bossX,
            float bossZ,
            Rune closingType,
            float defaultBangRadiusM,
            float logicBangRadiusM)
        {
            if (logic == null)
                return false;
            float dx = bossX - logic.TipX;
            float dz = bossZ - logic.TipZ;
            float reach = defaultBangRadiusM;
            if (logicBangRadiusM > 0f)
                reach = logicBangRadiusM;
            if (closingType == Rune.Aydinlik)
            {
                if (!logic.OverlapsBoss(bossX, bossZ, reach * 0.5f))
                {
                    float radial = PlanarMath.FlatDistance(bossX, bossZ, logic.OriginX, logic.OriginZ);
                    if (radial > logic.TipDistance + reach && radial > reach)
                        return false;
                }

                return true;
            }

            if (dx * dx + dz * dz > reach * reach)
            {
                if (!logic.OverlapsBoss(bossX, bossZ, reach * CastingDefaults.ReachFraction))
                    return false;
            }

            return true;
        }
    }
}
