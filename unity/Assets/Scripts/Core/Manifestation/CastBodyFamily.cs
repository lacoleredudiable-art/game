using Dovus.Core.Grammar;

namespace Dovus.Core.Manifestation
{
    /// <summary>
    /// ~30–40 body clip ailesinden biri. Kombo tablosu değil — fiil tohumu + silüet eksenleri.
    /// docs/animasyon-omurgasi.md
    /// </summary>
    public enum CastBodyFamily : byte
    {
        None = 0,
        Pierce = 1,
        Sweep = 2,
        Slam = 3,
        Channel = 4,
        Guard = 5,
    }

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
            Rune.Ates => CastBodyFamily.Pierce,      // Ateş — strike
            Rune.Su => CastBodyFamily.Channel,     // Su — mend
            Rune.Hava => CastBodyFamily.Sweep,      // Hava — motion
            Rune.Toprak => CastBodyFamily.Guard,      // Toprak — guard
            Rune.Aydinlik => CastBodyFamily.Pierce,  // Aydınlık — purge
            Rune.Karanlik => CastBodyFamily.Channel,   // Karanlık — stealth/special
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
