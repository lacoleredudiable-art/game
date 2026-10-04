using Dovus.Core.Element;
using Dovus.Core.Grammar;

namespace Dovus.Core.Manifestation
{
    /// <summary>
    /// Fiil / silüetten Animator body ailesi. 1554 cümle buradan birkaç aileye çöker.
    /// Altıgen element id: 1 Ateş…6 Toprak (eski İĞNE/SÜRÜ silüet tohumları geçici map).
    /// </summary>
    public static class CastBodyMapper
    {
        /// <summary>Yüksek spread'te Sweep yerine Channel (sürü yoğunluğu).</summary>
        public const float SweepToChannelSpread = 0.75f;

        public static CastBodyFamily FromVerb(Rune verb) => verb switch
        {
            Rune.Attack => CastBodyFamily.Pierce,      // Ateş — strike
            Rune.Heal => CastBodyFamily.Channel,     // Su — mend
            Rune.Move => CastBodyFamily.Sweep,      // Hava — motion
            Rune.Defense => CastBodyFamily.Guard,      // Toprak — guard
            Rune.Burst => CastBodyFamily.Pierce,  // Aydınlık — purge
            Rune.Control => CastBodyFamily.Channel,   // Karanlık — stealth/special
            _ => CastBodyFamily.None
        };

        /// <summary>
        /// Fiil tohumu + silüet: SÜRÜ aşırı yayılırsa Channel; yüksek lift Slam'i güçlendirir (aile aynı).
        /// </summary>
        public static CastBodyFamily FromVerbAndSilhouette(Rune verb, EffectSilhouette s)
        {
            CastBodyFamily family = FromVerb(verb);
            if (family == CastBodyFamily.Sweep && s.Spread >= SweepToChannelSpread)
                return CastBodyFamily.Channel;
            return family;
        }
    }
}
