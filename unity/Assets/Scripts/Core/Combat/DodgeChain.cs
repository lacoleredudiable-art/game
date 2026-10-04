using Dovus.Core.Tuning;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Çift basış penceresi: devam eden dodge birleşik dodge'a yükseltilir (Dragon Nest tarzı).
    /// </summary>
    public static class DodgeChain
    {
        public enum PressOutcome
        {
            DeniedNoCharge,
            StartedNew,
            UpgradedCombined
        }

        public static bool CanUpgradeToCombined(int worldMs, DodgeState dodge, DodgeTuning tuning)
        {
            if (dodge == null || tuning == null)
                return false;
            if (dodge.IsCombined)
                return false;
            if (!dodge.PressTimeMs.HasValue)
                return false;

            int press = dodge.PressTimeMs.Value;
            if (worldMs - press > tuning.DoubleTapWindowMs)
                return false;
            if (!dodge.IsActive(worldMs))
                return false;
            return true;
        }

        /// <summary>Hak ve durum kapıları geçildikten sonra çağrılır; yeni dodge'da Begin yapar.</summary>
        public static PressOutcome TryConsumePress(
            int worldMs,
            DodgeState dodge,
            DodgeChargeBank charges,
            DodgeTuning tuning)
        {
            if (dodge == null || charges == null || tuning == null)
                return PressOutcome.DeniedNoCharge;

            charges.Tick(worldMs);

            if (CanUpgradeToCombined(worldMs, dodge, tuning))
            {
                if (!charges.TrySpend(worldMs))
                    return PressOutcome.DeniedNoCharge;
                dodge.PromoteToCombined(worldMs);
                return PressOutcome.UpgradedCombined;
            }

            if (!charges.TrySpend(worldMs))
                return PressOutcome.DeniedNoCharge;

            dodge.Begin(worldMs);
            return PressOutcome.StartedNew;
        }
    }
}
