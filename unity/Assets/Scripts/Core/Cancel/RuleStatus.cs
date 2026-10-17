using System;

namespace Dovus.Core.Cancel
{
    /// <summary>Kuralın belgedeki durumu (AGENTS.md: [K] karar, [Ö] öneri; belgede olmayan = boşluk).</summary>
    public enum RuleStatus : byte
    {
        /// <summary>Belgede [K] ya da kâğıt kuralı.</summary>
        Decided = 0,
        /// <summary>Belgede [Ö]; onay bekler. Değer önerilen değerdir, seçenekle değişir.</summary>
        Proposed = 1,
        /// <summary>Belgede yok; en güvenli davranış (reddet, tampona al) yazıldı. Kullanıcıya sorulur.</summary>
        Gap = 2,
    }
}
