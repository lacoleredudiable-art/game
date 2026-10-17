using System;

namespace Dovus.Core.Cancel
{
    /// <summary>
    /// Eylemin üç fazı (13 §9): hazırlık, etki, toparlanma. <see cref="None"/> = boşta.
    /// Kural satırları faz maskesi taşır; çalışan eylemin anlık fazı tek bayraktır.
    /// </summary>
    [Flags]
    public enum ActionPhase : byte
    {
        None = 0,
        Windup = 1,
        Active = 2,
        Recovery = 4,
        Any = Windup | Active | Recovery,
    }
}
