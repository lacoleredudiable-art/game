using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Core.Status;

namespace Dovus.Core.Motion
{
    /// <summary>
    /// Kal─▒b─▒n duru┼ş/ka├ğ─▒nma hedefi. At─▒c─▒ kendisi olamaz: kendine kilitlenince
    /// her kare kenar pay─▒ kadar ileri-geri itilir.
    /// </summary>
    public static class MotionDeliveryAim
    {
        public enum Kind
        {
            /// <summary>D├╝nya hedefi yok; bak─▒┼ş, parmak veya ├ğubuk.</summary>
            None = 0,
            Enemy = 1,
            Ally = 2
        }

        public static Kind Choose(
            string templateAim,
            string targetMode,
            string action,
            bool movesTowardMarked,
            bool hasDistinctAlly)
        {
            if (MotionAim.IsEnemy(templateAim))
                return Kind.Enemy;

            bool allySkill = CardEffectRules.PrefersAlly(targetMode, action);
            if (allySkill && movesTowardMarked && hasDistinctAlly)
                return Kind.Ally;

            // Etki hedefi at─▒c─▒n─▒n kendisi. Duru┼ş hedefi o olmaz.
            return Kind.None;
        }

        /// <summary>Homing "track" ve hedefe giden bir faz varsa kal─▒p i┼şaretli noktaya y├╝r├╝r.</summary>
        public static bool MovesTowardMarked(MotionTemplate template)
        {
            if (template == null)
                return false;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                MotionPhase phase = template.Phases[i];
                if (phase.Homing != "track")
                    continue;
                switch (phase.Motion)
                {
                    case "dash":
                    case "leap":
                    case "lunge":
                    case "pull":
                    case "blink":
                    case "hop":
                    case "sidestep":
                    case "slam":
                    case "throw":
                        return true;
                    case "channel" when phase.DriftM > 0.05f:
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Arkaya ini┼ş yaz─▒lmam─▒┼ş ─▒┼ş─▒nlanma g├Âvdenin i├ğine kapan─▒r. Hedef d├╝┼şmand─▒r;
        /// menzil yetiyorsa ├ğ─▒k─▒┼ş ├Âte kenard─▒r. Dostun konumu bu kal─▒b─▒n dura─ş─▒ de─şildir.
        /// </summary>
        public static bool SwapsPastBody(MotionTemplate template)
        {
            if (template == null)
                return false;
            bool blink = false;
            bool behind = false;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                MotionPhase phase = template.Phases[i];
                if (phase.Motion == "blink")
                    blink = true;
                if (phase.Land == "behind")
                    behind = true;
            }
            return blink && !behind;
        }
    }
}