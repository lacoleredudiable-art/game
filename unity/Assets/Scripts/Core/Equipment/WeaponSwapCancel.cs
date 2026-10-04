using System;
using Dovus.Core.Grammar;
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

        public static bool IsTagged(SkillMotor motor, SkillId skillId)
        {
            if (motor == null || skillId.IsEmpty)
                return false;
            if (!motor.TryGetSkill(skillId.Value, out SkillCatalogEntry entry))
                return false;
            return new SkillEngineModifiers(entry.Engine).SwapCancel();
        }

        public static bool IsTagged(in SkillEngineModifiers engine) =>
            !engine.IsNull && engine.SwapCancel();

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

        /// <summary>Anında sonraki skill yalnız swap_cancel etiketinde.</summary>
        public static bool UnlocksNextSkill(bool inWindow, bool tagged) => inWindow && tagged;
    }
}
