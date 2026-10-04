using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Status;

namespace Dovus.Core.Motion
{
    /// <summary>
    /// Kalıbın duruş/kaçınma hedefi. Atıcı kendisi olamaz: kendine kilitlenince
    /// her kare kenar payı kadar ileri-geri itilir.
    /// </summary>
    public static class MotionDeliveryAim
    {
        public enum Kind
        {
            /// <summary>Dünya hedefi yok; bakış, parmak veya çubuk.</summary>
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

            // Etki hedefi atıcının kendisi. Duruş hedefi o olmaz.
            return Kind.None;
        }

        /// <summary>Homing "track" ve hedefe giden bir faz varsa kalıp işaretli noktaya yürür.</summary>
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
        /// Arkaya iniş yazılmamış ışınlanma gövdenin içine kapanır. Hedef düşmandır;
        /// menzil yetiyorsa çıkış öte kenardır. Dostun konumu bu kalıbın durağı değildir.
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
