using System;
using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Passives
{
    /// <summary>
    /// Pasif yuva sayılarının saf hesabı. ManifestationDirector bunları uygular.
    /// </summary>
    public static class SlotPassiveCombat
    {
        /// <summary>O8 (kullanıcı kararı): ignore_armor = zırhı %100 deler ("Zırh yoksayar").</summary>
        public const float IgnoreArmorPierce = 1f;

        /// <summary>Pasif kopya bir kez, bu güçle. Skill sıfatının duplicate_damage_mult'u ayrıdır.</summary>
        public const float EchoPower = 0.5f;
        public const float EchoDelaySec = 0.3f;

        public static float ScaleOutgoingPoise(float basePoise, float skillPoiseMult, float slotPoiseMult)
        {
            float skill = skillPoiseMult > 0f ? skillPoiseMult : 1f;
            float slot = slotPoiseMult > 0f ? slotPoiseMult : 1f;
            return basePoise * skill * slot;
        }

        public static float CombineArmorPen(float existingPen, bool skillIgnoresArmor, float slotPen)
        {
            float pen = existingPen > 0f ? existingPen : 0f;
            if (skillIgnoresArmor && pen < IgnoreArmorPierce)
                pen = IgnoreArmorPierce;
            if (slotPen > pen)
                pen = slotPen;
            if (pen > 1f)
                pen = 1f;
            return pen;
        }
    }
}
