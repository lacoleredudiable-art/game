using System;
using Dovus.Core.Shared;

namespace Dovus.Core.Equipment
{
    /// <summary>
    /// Silah değiştirme penceresi. Bitiş duruşu skilin son %30'u;
    /// Silah kesme etiketinde son %50. Süre 0,25 sn, bekleme 1,2 sn (JSON swap).
    /// </summary>
    public static class WeaponSwapCancel
    {
        public const float SwapSec = 0.25f;
        public const float CooldownSec = 1.2f;
        public const float NormalOpenFraction = 0.70f;
        public const float TaggedOpenFraction = 0.50f;

        public static readonly string[] TaggedSkillIds =
        {
            SkillIds.DenseStrike, SkillIds.FixedStrike, SkillIds.SpreadStrike, SkillIds.RisingStrike, SkillIds.FlowingStrike,
            SkillIds.SpreadStep, SkillIds.RisingStep, SkillIds.SpreadBlast, SkillIds.LeapingHead, SkillIds.DenseReflect
        };

        public static bool IsTagged(SkillId skillId)
        {
            if (skillId.IsEmpty)
                return false;
            for (int i = 0; i < TaggedSkillIds.Length; i++)
            {
                if (string.Equals(TaggedSkillIds[i], skillId.Value, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public static float OpenFraction(bool tagged) => tagged ? TaggedOpenFraction : NormalOpenFraction;

        public static bool InWindow(float elapsedSec, float totalSec, bool tagged)
        {
            if (totalSec <= 0.0001f || elapsedSec < 0f)
                return false;
            return elapsedSec / totalSec >= OpenFraction(tagged);
        }

        /// <summary>
        /// Çizim, basılı tutma, kaçınma ve sersemlik her zaman kilitler.
        /// Pencere açıksa cast kilidi kalkar. Kök ve boşta zaten serbesttir.
        /// </summary>
        public static bool MayBegin(
            bool stateAllows,
            bool drawing,
            bool holding,
            bool dodging,
            bool stunned,
            bool inWindow,
            bool tagged)
        {
            if (dodging || stunned || drawing)
                return false;
            if (holding && !(tagged && inWindow))
                return false;
            if (stateAllows)
                return true;
            return inWindow;
        }

        /// <summary>Penceredeki değiştirme kalan animasyonu keser.</summary>
        public static bool CutsRecovery(bool inWindow) => inWindow;

        /// <summary>Anında sonraki skill yalnız 10 Silah kesme etiketinde.</summary>
        public static bool UnlocksNextSkill(bool inWindow, bool tagged) => inWindow && tagged;
    }
}
