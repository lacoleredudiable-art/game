using System.Collections.Generic;

namespace Dovus.Core.Mechanic
{
    /// <summary>
    /// Koruyucu tetik (2-9 can, 4-9 kalkan, 8-9 hasar_buff) kalıbın anlık
    /// ödemesinin üstüne binmez. Aynı tetik bir kez öder.
    /// </summary>
    public static class GuardTriggerDelivery
    {
        public static bool Owns(MechanicPlan plan, string stat)
        {
            if (plan == null || string.IsNullOrEmpty(stat))
                return false;
            for (int i = 0; i < plan.Effects.Count; i++)
            {
                MechanicEffect effect = plan.Effects[i];
                if (effect.Stat == stat && effect.Has("koruyucu_tetik") && effect.Amount > 0)
                    return true;
            }
            return false;
        }

        public static bool AllowImmediate(MechanicPlan plan, string stat) => !Owns(plan, stat);

        /// <summary>Tetik kimliği başına bir ödeme.</summary>
        public sealed class Once
        {
            readonly HashSet<int> _spent = new HashSet<int>();

            public bool TryApply(int triggerId) => _spent.Add(triggerId);
        }
    }
}
