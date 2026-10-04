using Dovus.Core.Element;
using Dovus.Core.Grammar;

namespace Dovus.Core.Manifestation
{
    /// <summary>
    /// ~30ÔÇô40 body clip ailesinden biri. Kombo tablosu de─şil ÔÇö fiil tohumu + sil├╝et eksenleri.
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
    /// Fiil / sil├╝etten Animator body ailesi. 1554 c├╝mle buradan birka├ğ aileye ├ğ├Âker.
    /// Alt─▒gen element id: 1 Ate┼şÔÇĞ6 Toprak (eski ─░─ŞNE/S├£R├£ sil├╝et tohumlar─▒ ge├ğici map).
    /// </summary>
    public static class CastBodyMapper
    {
        /// <summary>Y├╝ksek spread'te Sweep yerine Channel (s├╝r├╝ yo─şunlu─şu).</summary>
        public const float SweepToChannelSpread = 0.75f;

        public static CastBodyFamily FromVerb(Rune verb) => verb switch
        {
            Rune.Ates => CastBodyFamily.Pierce,      // Ate┼ş ÔÇö strike
            Rune.Su => CastBodyFamily.Channel,     // Su ÔÇö mend
            Rune.Hava => CastBodyFamily.Sweep,      // Hava ÔÇö motion
            Rune.Toprak => CastBodyFamily.Guard,      // Toprak ÔÇö guard
            Rune.Aydinlik => CastBodyFamily.Pierce,  // Ayd─▒nl─▒k ÔÇö purge
            Rune.Karanlik => CastBodyFamily.Channel,   // Karanl─▒k ÔÇö stealth/special
            _ => CastBodyFamily.None
        };

        /// <summary>
        /// Fiil tohumu + sil├╝et: S├£R├£ a┼ş─▒r─▒ yay─▒l─▒rsa Channel; y├╝ksek lift Slam'i g├╝├ğlendirir (aile ayn─▒).
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