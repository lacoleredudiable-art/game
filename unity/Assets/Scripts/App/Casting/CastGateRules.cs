using Dovus.Core.Grammar;

namespace Dovus.App.Casting
{
    /// <summary>HexagonInputController cümle başlatma / CD / hedef kapıları — saf karar (Unity yok).</summary>
    public static class CastGateRules
    {
        public enum SentenceStartBlock
        {
            None,
            InsufficientMana,
            GlobalCooldown
        }

        public static bool WouldStartSentence(SentencePhase phase, int wordCount, int maxSentenceDots)
        {
            if (phase == SentencePhase.Idle
                || phase == SentencePhase.Recovering
                || phase == SentencePhase.Resolved
                || phase == SentencePhase.Aborted)
                return true;

            if (phase == SentencePhase.Building && wordCount >= maxSentenceDots)
                return true;

            return false;
        }

        public static bool CanAffordVerbDot(bool enforceResourceCost, bool hasResource, bool canAffordCost)
        {
            if (!enforceResourceCost)
                return true;
            if (!hasResource)
                return true;
            return canAffordCost;
        }

        public static bool CanGlobalCooldownGate(bool enforceCooldown, bool hasCooldown, bool canStartGlobal)
        {
            if (!enforceCooldown)
                return true;
            if (!hasCooldown)
                return true;
            return canStartGlobal;
        }

        public static SentenceStartBlock SentenceStartBlockReason(
            SentencePhase phase,
            int wordCount,
            int maxSentenceDots,
            bool enforceResourceCost,
            bool hasResource,
            bool canAffordCost,
            bool enforceCooldown,
            bool hasCooldown,
            bool canStartGlobal)
        {
            if (!WouldStartSentence(phase, wordCount, maxSentenceDots))
                return SentenceStartBlock.None;
            if (!CanAffordVerbDot(enforceResourceCost, hasResource, canAffordCost))
                return SentenceStartBlock.InsufficientMana;
            if (!CanGlobalCooldownGate(enforceCooldown, hasCooldown, canStartGlobal))
                return SentenceStartBlock.GlobalCooldown;
            return SentenceStartBlock.None;
        }

        public static bool TryAllowSentenceStart(
            SentencePhase phase,
            int wordCount,
            int maxSentenceDots,
            bool enforceResourceCost,
            bool hasResource,
            bool canAffordCost,
            bool enforceCooldown,
            bool hasCooldown,
            bool canStartGlobal)
        {
            return SentenceStartBlockReason(
                phase, wordCount, maxSentenceDots,
                enforceResourceCost, hasResource, canAffordCost,
                enforceCooldown, hasCooldown, canStartGlobal) == SentenceStartBlock.None;
        }

        public static bool TryAllowComboCooldownForNextDot(
            bool enforceCooldown,
            bool hasCooldown,
            SentencePhase phase,
            int wordCount,
            bool skillIsEmpty,
            bool comboKeyEmpty,
            bool canStartCombo)
        {
            if (!enforceCooldown)
                return true;
            if (!hasCooldown)
                return true;
            if (phase != SentencePhase.Building || wordCount != 1)
                return true;
            if (skillIsEmpty)
                return true;
            if (comboKeyEmpty)
                return true;
            return canStartCombo;
        }

        public static bool TryAllowProspectiveTarget(
            bool hasTargetGate,
            SentencePhase phase,
            int wordCount,
            bool hasSkills,
            bool skillIsEmpty,
            bool targetGateAllows)
        {
            if (!hasTargetGate || phase != SentencePhase.Building || wordCount != 1)
                return true;
            if (!hasSkills)
                return true;
            return skillIsEmpty || targetGateAllows;
        }
    }
}
