using System;
using System.Collections.Generic;

namespace Dovus.Core.Cancel
{
    /// <summary>Silahın düz vuruş zinciri: adımlar + sıfırlanma süresi. Veridir; kod sayı bilmez.</summary>
    public sealed class StrikeComboDef
    {
        readonly StrikeStep[] _steps;

        public StrikeComboDef(IReadOnlyList<StrikeStep> steps, int resetMs)
        {
            if (steps == null || steps.Count == 0)
                throw new ArgumentException("en az bir adım ister", nameof(steps));
            _steps = new StrikeStep[steps.Count];
            for (int i = 0; i < _steps.Length; i++) _steps[i] = steps[i];
            ResetMs = resetMs < 0 ? 0 : resetMs;
        }

        public int StepCount => _steps.Length;
        public StrikeStep Step(int index) => _steps[index];

        /// <summary>Son vuruşun başlangıcından bu kadar sonra dokunulursa zincir 0'dan başlar (O-4.14).</summary>
        public int ResetMs { get; }

        /// <summary>O-4.14 Kılıç: 3 kesiş × 0,40 s, vuruş karesi 0,20 s, sıfırlanma 1,2 s.</summary>
        public static StrikeComboDef Sword()
        {
            var steps = new StrikeStep[CancelDefaults.SwordStrikeCount];
            for (int i = 0; i < steps.Length; i++)
                steps[i] = new StrikeStep(CancelDefaults.SwordStrikeDurationMs, CancelDefaults.SwordStrikeHitMs);
            return new StrikeComboDef(steps, CancelDefaults.StrikeComboResetMs);
        }

        /// <summary>
        /// Tek atışlık silah (Yay: <c>Yay_VUR</c>). Süre ve vuruş karesi belgede yok (kaynakta yok);
        /// çağıran klipten verir.
        /// </summary>
        public static StrikeComboDef Single(int durationMs, int hitMs) =>
            new StrikeComboDef(new[] { new StrikeStep(durationMs, hitMs) }, CancelDefaults.StrikeComboResetMs);
    }
}
